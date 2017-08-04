/*
** $Id: lgc.c,v 2.108 2010/12/29 18:00:23 roberto Exp roberto $
** Garbage Collector
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KopiLua
{
	using lu_int32 = System.UInt32;
	using l_mem = System.Int32;
	using lu_mem = System.UInt32;
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using lu_byte = System.Byte;
	using Instruction = System.UInt32;

	public partial class Lua
	{

        /* how much to allocate before next GC step */
		private const int GCSTEPSIZE	= 1024; //FIXME: uint->int

        /* maximum number of elements to sweep in each single step */
		private const int GCSWEEPMAX		= 40;

		/* cost of sweeping one element */
		private const int GCSWEEPCOST	= 1;
		
		/* maximum number of finalizers to call in each GC step */
		private const int GCFINALIZENUM	= 4;

		/* cost of marking the root set */
		private const int GCROOTCOST = 10;

		/* cost of atomic step */
		private const int GCATOMICCOST = 1000;

		/* basic cost to traverse one object (to be added to the links the
		   object may have) */
		private const int TRAVCOST = 5;


		/*
		** standard negative debt for GC; a reasonable "time" to wait before
		** starting a new cycle
		*/
		private static int stddebt(global_State g) { return (-(l_mem)(gettotalbytes(g)/100) * g.gcpause); }


		/*
		** 'makewhite' erases all color bits plus the old bit and then
		** sets only the current white bit
		*/
		public static byte maskcolors	= (byte)(~(bit2mask(BLACKBIT, OLDBIT) | WHITEBITS)); //FIXME: cast_byte() removed 
		public static void makewhite(global_State g, GCObject x) {
		   gch(x).marked = (byte)(gch(x).marked & maskcolors | luaC_white(g)); }

		public static void white2gray(GCObject x) { resetbits(ref gch(x).marked, WHITEBITS); }
		public static void black2gray(GCObject x) { resetbit(ref gch(x).marked, BLACKBIT); }

		public static void stringmark(TString s) {if (s != null) resetbits(ref s.tsv.marked, WHITEBITS);} //FIXME: &&->if

		public static bool isfinalized(GCObject x) { return testbit(gch(x).marked, FINALIZEDBIT); }
        public static bool isfinalized(GCheader u) { return testbit(u.marked, FINALIZEDBIT); } //FIXME:added

		public static void checkdeadkey(Node n) { lua_assert(!ttisdeadkey(gkey(n)) || ttisnil(gval(n)));} 


		private static void checkconsistency(TValue obj) {
  			return lua_longassert(!iscollectable(obj) || righttt(obj)); }


		public static void markvalue(global_State g, TValue o)  { checkconsistency(o);
			if (valiswhite(o)) reallymarkobject(g,gcvalue(o)); }

		public static void markobject(global_State g, object t) { if (t != null && iswhite(obj2gco(t)))
				reallymarkobject(g, obj2gco(t)); }

		//static void reallymarkobject (global_State *g, GCObject *o);


		/*
		** {======================================================
		** Generic functions
		** =======================================================
		*/


		/*
		** link table 'h' into list pointed by 'p'
		*/
		private static void linktable (Table h, ref GCObject p) { h.gclist = p; p = obj2gco(h); }
		
		
		/*
		** mark a table entry as dead (therefore removing it from the table)
		*/
		private static void removeentry (Node n) {
		  lua_assert(ttisnil(gval(n)));
		  if (iscollectable(gkey(n)))
			setdeadvalue(gkey(n));  /* dead key; remove it */
		}


		/*
		** tells whether a key or value can be cleared from a weak
		** table. Non-collectable objects are never removed from weak
		** tables. Strings behave as `values', so are never removed too. for
		** other objects: if really collected, cannot keep them; for objects
		** being finalized, keep them in keys, but not in values
		*/
		private static int iscleared (TValue o, int iskey) {
		  if (!iscollectable(o)) return 0;
		  else if (ttisstring(o)) {
		    stringmark(rawtsvalue(o));  /* strings are `values', so are never weak */
		    return 0;
		  }
		  else return iswhite(gcvalue(o)) || (iskey == 0 && isfinalized(gcvalue(o))) ? 1 : 0;
		}


		/*
		** barrier that moves collector forward, that is, mark the white object
		** being pointed by a black object.
		*/
		public static void luaC_barrier_ (lua_State L, GCObject o, GCObject v) {
		  global_State g = G(L);
		  lua_assert(isblack(o) && iswhite(v) && !isdead(g, v) && !isdead(g, o));
		  lua_assert(isgenerational(g) || g.gcstate != GCSpause);
		  lua_assert(gch(o).tt != LUA_TTABLE);
		  if (keepinvariant(g))  /* must keep invariant? */
		    reallymarkobject(g, v);  /* restore invariant */
		  else {  /* sweep phase */
		    lua_assert(issweepphase(g));
		    makewhite(g, o);  /* mark main obj. as white to avoid other barriers */
		  }
		}


		/*
		** barrier that moves collector backward, that is, mark the black object
		** pointing to a white object as gray again. (Current implementation
		** only works for tables; access to 'gclist' is not uniform across
		** different types.)
		*/
		public static void luaC_barrierback_ (lua_State L, GCObject o) {
		  global_State g = G(L);
		  lua_assert(isblack(o) && !isdead(g, o));
		  black2gray(o);  /* make object gray (again) */
		  gco2t(o).gclist = g.grayagain;
		  g.grayagain = o;
		}


		/*
		** barrier for prototypes. When creating first closure (cache is
		** NULL), use a forward barrier; this may be the only closure of the
		** prototype (if it is a "regular" function, with a single instance)
		** and the prototype may be big, so it is better to avoid traversing
		** it again. Otherwise, use a backward barrier, to avoid marking all
		** possible instances.
		*/
		public static void luaC_barrierproto_ (lua_State L, Proto p, Closure c) {
		  global_State g = G(L);
		  lua_assert(isblack(obj2gco(p)));
		  if (p.cache == null) {  /* first time? */
		    luaC_objbarrier(L, p, c);
		  }
		  else {  /* use a backward barrier */
		    black2gray(obj2gco(p));  /* make prototype gray (again) */
		    p.gclist = g.grayagain;
		    g.grayagain = obj2gco(p);
		  }
		}


		/*
		** check color (and invariants) for an upvalue that was closed,
		** i.e., moved into the 'allgc' list
		*/
		public static void luaC_checkupvalcolor (global_State g, UpVal uv) {
		  GCObject o = obj2gco(uv);
		  lua_assert(!isblack(o));  /* open upvalues are never black */
		  if (isgray(o)) {
		    if (keepinvariant(g)) {
		      resetoldbit(o);  /* see MOVE OLD rule */
		      gray2black(o);  /* it is being visited now */
		      markvalue(g, uv.v);
		    }
		    else {
		      lua_assert(issweepphase(g));
		      makewhite(g, o);
		    }
		  }
		}

        //FIXME:refer to luaM_new
        //public static object luaM_realloc_<T>(lua_State L)
		//T new_obj = (T)System.Activator.CreateInstance(typeof(T));
		//AddTotalBytes(L, nsize);
		/*
		** create a new collectable object (with given type and size) and link
		** it to '*list'. 'offset' tells how many bytes to allocate before the
		** object itself (used only by states).
		*/
		public static GCObject luaC_newobj<T> (lua_State L, int tt, uint sz, GCObjectRef list,
		                       int offset) {
		  global_State g = G(L);
		  GCObject o = obj2gco(luaM_newobject<T>(L/*, tt, sz*/)/* + offset*/); //FIXME:???no offset
		  if (o is TString) //FIXME:added
		  {
		  	int len_plus_1 = (int)sz - GetUnmanagedSize(typeof(TString));
		  	((TString) o).str = new CharPtr(new char[len_plus_1]);
		  }
		  if (list == null)
		  	list = new AllGCRef(g);  /* standard list for collectable objects */ //FIXME:changed, new PtrRef
		  gch(o).marked = luaC_white(g);
		  gch(o).tt = (byte)tt; //FIXME:(byte)
		  gch(o).next = list.get();
		  list.set(o);
		  return o;
		}

		/* }====================================================== */



		/*
		** {======================================================
		** Mark functions
		** =======================================================
		*/


		/*
		** mark an object. Userdata and closed upvalues are visited and turned
		** black here. Strings remain gray (it is the same as making them
		** black). Other objects are marked gray and added to appropriate list
		** to be visited (and turned black) later. (Open upvalues are already
		** linked in 'headuv' list.)
		*/
		private static void reallymarkobject (global_State g, GCObject o) {
		  lua_assert(iswhite(o) && !isdead(g, o));
		  white2gray(o);
		  switch (gch(o).tt) {
			case LUA_TSTRING: {
			  return;  /* for strings, gray is as good as black */
			}
			case LUA_TUSERDATA: {
			  Table mt = gco2u(o).metatable;
			  markobject(g, mt);
			  markobject(g, gco2u(o).env);
              gray2black(o);  /* all pointers marked */
			  return;
			}
			case LUA_TUPVAL: {
			  UpVal uv = gco2uv(o);
			  markvalue(g, uv.v);
			  if (uv.v == uv.u.value_)  /* closed? (open upvalues remain gray) */
				gray2black(o);  /* make it black */
			  return;
			}
			case LUA_TFUNCTION: {
			  gco2cl(o).c.gclist = g.gray;
			  g.gray = o;
			  break;
			}
			case LUA_TTABLE: {
			  linktable(gco2t(o), ref g.gray);
			  break;
			}
			case LUA_TTHREAD: {
			  gco2th(o).gclist = g.gray;
			  g.gray = o;
			  break;
			}
			case LUA_TPROTO: {
			  gco2p(o).gclist = g.gray;
			  g.gray = o;
			  break;
			}
			default: lua_assert(0); break; //FIXME: add break
		  }
		}


		/*
		** mark tag methods for basic types
		*/
		private static void markmt (global_State g) {
		  int i;
		  for (i=0; i < LUA_NUMTAGS; i++)
		     markobject(g, g.mt[i]);
		}


		/*
		** mark all objects in list of being-finalized
		*/
		private static void markbeingfnz (global_State g) {
		  GCObject o;
		  for (o = g.tobefnz; o != null; o = gch(o).next) {
		    makewhite(g, o);
		    reallymarkobject(g, o);
		  }
		}


		/*
		** mark all values stored in marked open upvalues. (See comment in
		** 'lstate.h'.)
		*/
		private static void remarkupvals (global_State g) {
		  UpVal uv;
		  for (uv = g.uvhead.u.l.next; uv != g.uvhead; uv = uv.u.l.next) {
		    if (isgray(obj2gco(uv)))
		      markvalue(g, uv.v);
		  }
		}


		/*
		** mark root set and reset all gray lists, to start a new
		** incremental (or full) collection
		*/
		private static void markroot (global_State g) {
		  g.gray = g.grayagain = null;
		  g.weak = g.allweak = g.ephemeron = null;
		  markobject(g, g.mainthread);
		  markvalue(g, g.l_registry);
		  markmt(g);
		  markbeingfnz(g);  /* mark any finalizing object left from previous cycle */
		}
	
		/* }====================================================== */


		/*
		** {======================================================
		** Traverse functions
		** =======================================================
		*/

		private static void traverseweakvalue (global_State g, Table h) {
		  Node n;//, limit = gnode(h, sizenode(h)); //FIXME:removed, overflow
		  //for (n = gnode(h, 0); n < limit; n++) { //FIXME:changed, see below
		  for (int ni = 0; ni < sizenode(h); ni++) {
		  	n = gnode(h, ni);
		    
		  	checkdeadkey(n);
		    if (ttisnil(gval(n)))  /* entry is empty? */
		      removeentry(n);  /* remove it */
		    else {
		      lua_assert(!ttisnil(gkey(n)));
		      markvalue(g, gkey(n));  /* mark key */
		    }
		  }
		  linktable(h, ref g.weak);  /* link into appropriate list */
		}


		private static int traverseephemeron (global_State g, Table h) {
		  int marked = 0;  /* true if an object is marked in this traversal */
		  int hasclears = 0;  /* true if table has unmarked pairs */
		  Node n;// limit = gnode(h, sizenode(h)); //FIXME:removed, overflow
		  int i;
		  /* traverse array part (numeric keys are 'strong') */
		  for (i = 0; i < h.sizearray; i++) {
		    if (valiswhite(h.array[i])) {
		      marked = 1;
		      reallymarkobject(g, gcvalue(h.array[i]));
		    }
		  }
		  /* traverse hash part */
		  //for (n = gnode(h, 0); n < limit; n++) { //FIXME:changed, see below
		  for (int ni = 0; ni < sizenode(h); ni++) {
		  	n = gnode(h, ni);		  
		  	
		    checkdeadkey(n);
		    if (ttisnil(gval(n)))  /* entry is empty? */
		      removeentry(n);  /* remove it */
		    else if (valiswhite(gval(n))) {  /* value not marked yet? */
		      if (iscleared(gkey(n), 1) != 0)  /* key is not marked (yet)? */
		        hasclears = 1;  /* may have to propagate mark from key to value */
		      else {  /* key is marked, so mark value */
		        marked = 1;  /* value was not marked */
		        reallymarkobject(g, gcvalue(gval(n)));
		      }
		    }
		  }
		  if (hasclears != 0)  /* does table have unmarked pairs? */
		    linktable(h, ref g.ephemeron);  /* will have to propagate again */
		  else  /* nothing to propagate */
		    linktable(h, ref g.weak);  /* avoid convergence phase  */
		  return marked;
		}


		private static void traversestrongtable (global_State g, Table h) {
          Node n;// limit = gnode(h, sizenode(h)); //FIXME:removed, overflow
		  int i;
		  for (i = 0; i < h.sizearray; i++)  /* traverse array part */
		    markvalue(g, h.array[i]);
		  //for (n = gnode(h, 0); n < limit; n++) {  /* traverse hash part */ //FIXME:changed, see below
		  for (int ni = 0; ni < sizenode(h); ni++) {
		  	n = gnode(h, ni);
		  	
		    checkdeadkey(n);
		    if (ttisnil(gval(n)))  /* entry is empty? */
		      removeentry(n);  /* remove it */
		    else {
		      lua_assert(!ttisnil(gkey(n)));
		      markvalue(g, gkey(n));  /* mark key */
		      markvalue(g, gval(n));  /* mark value */
		    }
		  }
		}


		private static int traversetable (global_State g, Table h) {
		  TValue mode = gfasttm(g, h.metatable, TMS.TM_MODE);
		  markobject(g, h.metatable);
		  //FIXME:??? modify mode!=0 to mode!=null, avoid lua_TValue.operator int() exception
		  if (mode != null && ttisstring(mode)) {  /* is there a weak mode? */ 
		  	int weakkey = (strchr(svalue(mode), 'k') != null) ? 1 : 0;
		    int weakvalue = (strchr(svalue(mode), 'v') != null) ? 1 : 0;
		    if (weakkey != 0 || weakvalue != 0) {  /* is really weak? */
		      black2gray(obj2gco(h));  /* keep table gray */
		      if (weakkey == 0) {  /* strong keys? */
		        traverseweakvalue(g, h);
		        return TRAVCOST + sizenode(h);
		      }
		      else if (weakvalue == 0) {  /* strong values? */
		        traverseephemeron(g, h);
		        return TRAVCOST + sizenode(h);
		      }
		      else {
		        linktable(h, ref g.allweak);  /* nothing to traverse now */
		        return TRAVCOST;
		      }
		    }  /* else go through */
		  }
		  traversestrongtable(g, h);
          return TRAVCOST + h.sizearray + (2 * sizenode(h));
		}

		//FIXME:<----------------------------
		private static int traverseproto (global_State g, Proto f) {
		  int i;
		  if (f.cache != null && iswhite(obj2gco(f.cache)))
		    f.cache = null;  /* allow cache to be collected */
		  stringmark(f.source);
		  for (i = 0; i < f.sizek; i++)  /* mark literals */
			markvalue(g, f.k[i]);
		  for (i = 0; i < f.sizeupvalues; i++)  /* mark upvalue names */
			stringmark(f.upvalues[i].name);
		  for (i = 0; i < f.sizep; i++)  /* mark nested protos */
		    markobject(g, f.p[i]);
		  for (i = 0; i < f.sizelocvars; i++)  /* mark local-variable names */
			stringmark(f.locvars[i].varname);
          return TRAVCOST + f.sizek + f.sizeupvalues + f.sizep + f.sizelocvars;
		}


		private static int traverseclosure (global_State g, Closure cl) {
		  if (cl.c.isC != 0) {
			int i;
			for (i=0; i<cl.c.nupvalues; i++)  /* mark its upvalues */
			  markvalue(g, cl.c.upvalue[i]);
		  }
		  else {
			int i;
			lua_assert(cl.l.nupvalues == cl.l.p.sizeupvalues);
			markobject(g, cl.l.p);  /* mark its prototype */
			for (i=0; i<cl.l.nupvalues; i++)  /* mark its upvalues */
			  markobject(g, cl.l.upvals[i]);
		  }
          return TRAVCOST + cl.c.nupvalues;
		}


		private static int traversestack (global_State g, lua_State L) {
		  StkId[] o_ = L.stack; //FIXME:???o_
		  if (o_ == null) //FIXME:???o_
		    return 1;  /* stack not completely built yet */
		  StkId o = new lua_TValue(o_); //FIXME:o_->o
		  for (; o < L.top; /*StkId.inc(ref o)*/o = o + 1) {//FIXME:L.stack->new StkId(L.stack[0]) //FIXME:don't use StackId.inc(), overflow ([-1])
		    markvalue(g, o);
		    
		    //------------------------
		    if (o >= L.top - 1) 
		    {
		    	break;//FIXME:added, o + 1 will overflow
		    }
		    //------------------------
		  }
		  if (g.gcstate == GCSatomic) {  /* final traversal? */
		  	StkId limMinus1 = L.stack[L.stacksize-1];  /* real end of stack */ //FIXME:L.stack[L.stacksize] will overvlow, changed it
		  	for (; o <= limMinus1; /*StkId.inc(ref o)*/o = o + 1) { /* clear not-marked stack slice */ //FIXME:overflow, changed 'o < lim' to 'o <= limMinus1'
		      setnilvalue(o);
			  
		      //------------------------
		      if (o >= L.top - 1)
			  {
			  	break;//FIXME:added, o + 1 will overflow
			  }
		      //------------------------
		  	}
		  }
		  return TRAVCOST + (int)(o - L.stack);
		}


		/*
		** traverse one gray object, turning it to black (except for threads,
		** which are always gray).
		** Returns number of values traversed.
		*/
		private static int propagatemark (global_State g) {
		  GCObject o = g.gray;
		  lua_assert(isgray(o));
		  gray2black(o);
		  switch (gch(o).tt) {
			case LUA_TTABLE: {
			  Table h = gco2t(o);
			  g.gray = h.gclist;
			  return traversetable(g, h);
			}
			case LUA_TFUNCTION: {
			  Closure cl = gco2cl(o);
			  g.gray = cl.c.gclist;
			  return traverseclosure(g, cl);
			}
			case LUA_TTHREAD: {
			  lua_State th = gco2th(o);
			  g.gray = th.gclist;
			  th.gclist = g.grayagain;
			  g.grayagain = o;
			  black2gray(o);
			  return traversestack(g, th);
			}
			case LUA_TPROTO: {
			  Proto p = gco2p(o);
			  g.gray = p.gclist;
			  return traverseproto(g, p);
			}
			default: lua_assert(0); return 0;
		  }
		}


		private static void propagateall (global_State g) {
		  while (g.gray != null) propagatemark(g);
		}


		private static void traverselistofgrays (global_State g, ref GCObject l) {
		  lua_assert(g.gray == null);  /* no grays left */
		  g.gray = l;  /* now 'l' is new gray list */
		  l = null;
		  propagateall(g);
		}
		
		//FIXME:-------------------->

		private static void convergeephemerons (global_State g) {
		  int changed;
		  do {
		    GCObject w;
		    GCObject next = g.ephemeron;  /* get ephemeron list */
		    g.ephemeron = null;  /* tables will return to this list when traversed */
		    changed = 0;
		    while ((w = next) != null) {
		      next = gco2t(w).gclist;
		      if (traverseephemeron(g, gco2t(w)) != 0) {  /* traverse marked some value? */
			    propagateall(g);  /* propagate changes */
		        changed = 1;  /* will have to revisit all ephemeron tables */
		      }
		    }
		  } while (changed != 0);
		}

        //FIXME:<--------------------------

		/* }====================================================== */


		/*
		** {======================================================
		** Sweep Functions
		** =======================================================
		*/


		/*
		** clear collected entries from all weaktables in list 'l'
		*/
		private static void cleartable (GCObject l) {
		  for (; l != null; l = gco2t(l).gclist) {
			Table h = gco2t(l);
			Node n; // limit = gnode(h, sizenode(h)); //FIXME:removed, overflow
		    int i;
		    for (i = 0; i < h.sizearray; i++) {
			  TValue o = h.array[i];
			  if (iscleared(o, 0) != 0)  /* value was collected? */
			    setnilvalue(o);  /* remove value */
		    }
			//for (n = gnode(h, 0); n < limit; n++) {//FIXME:changed, see below
			for (int ni = 0; ni < sizenode(h); ni++) {
			  n = gnode(h, ni);		 
			  
			  if (!ttisnil(gval(n)) &&  /* non-empty entry? */
				  (iscleared(gkey(n), 1) != 0 || iscleared(gval(n), 0) != 0)) {
				setnilvalue(gval(n));  /* remove value ... */
				removeentry(n);  /* and remove entry from table */
			  }
			}
		  }
		}


		private static void freeobj (lua_State L, GCObject o) {
		  switch (gch(o).tt) {
			case LUA_TPROTO: luaF_freeproto(L, gco2p(o)); break;
			case LUA_TFUNCTION: luaF_freeclosure(L, gco2cl(o)); break;
			case LUA_TUPVAL: luaF_freeupval(L, gco2uv(o)); break;
			case LUA_TTABLE: luaH_free(L, gco2t(o)); break;
			case LUA_TTHREAD: luaE_freethread(L, gco2th(o)); break;
    		case LUA_TUSERDATA: 
				//luaM_freemem(L, o, sizeudata(gco2u(o)));
				SubtractTotalBytes(L, sizeudata(gco2u(o))); //FIXME:
				luaM_freemem(L, gco2u(o)); //FIXME:???
				break;
			case LUA_TSTRING: {
			  G(L).strt.nuse--;
			  //luaM_freemem(L, o, sizestring(gco2ts(o)));
			  SubtractTotalBytes(L, sizestring(gco2ts(o))); //FIXME:???
			  luaM_freemem(L, gco2ts(o)); //FIXME:???
			  break;
			}
			default: lua_assert(0); break;
		  }
		}



		public static void sweepwholelist(lua_State L, GCObjectRef p) { sweeplist(L, p, MAX_LUMEM); }
		//static GCObject **sweeplist (lua_State *L, GCObject **p, lu_mem count);


		/*
		** sweep the (open) upvalues of a thread and resize its stack and
		** list of call-info structures.
		*/
		private static void sweepthread (lua_State L, lua_State L1) {
		  if (L1.stack == null) return;  /* stack not completely built yet */
		  sweepwholelist(L, new OpenValRef(L1));  /* sweep open upvalues */ //FIXME:???
		  luaE_freeCI(L1);  /* free extra CallInfo slots */
		  /* should not change the stack during an emergency gc cycle */
		  if (G(L).gckind != KGC_EMERGENCY)
		      luaD_shrinkstack(L1);
		}

		//private static GCObject nullp = null; //FIXME:see nullp in global_State
		
		/*
		** sweep at most 'count' elements from a list of GCObjects erasing dead
		** objects, where a dead (not alive) object is one marked with the "old"
		** (non current) white and not fixed.
		** In non-generational mode, change all non-dead objects back to white,
		** preparing for next collection cycle.
		** In generational mode, keep black objects black, and also mark them as
		** old; stop when hitting an old object, as all objects after that
		** one will be old too.
		** When object is a thread, sweep its list of open upvalues too.
		*/
		private static GCObjectRef sweeplist (lua_State L, GCObjectRef p, lu_mem count) {
		  global_State g = G(L);
		  int ow = otherwhite(g);
		  int toclear, toset;  /* bits to clear and to set in all live objects */
		  int tostop;  /* stop sweep when this is true */
          l_mem debt = g.GCdebt;  /* current debt */
		  if (isgenerational(g)) {  /* generational mode? */
		    toclear = ~0;  /* clear nothing */
		    toset = bitmask(OLDBIT);  /* set the old bit of all surviving objects */
		    tostop = bitmask(OLDBIT);  /* do not sweep old generation */
		  }
		  else {  /* normal mode */
		    toclear = maskcolors;  /* clear all color bits + old bit */
		    toset = luaC_white(g);  /* make object white */
		    tostop = 0;  /* do not stop */
		  }
		  while (p.get() != null && count-- > 0) {
		  	GCObject curr = p.get();
		    int marked = gch(curr).marked;
		    if (isdeadm(ow, marked)) {  /* is 'curr' dead? */
		      p.set(gch(curr).next);  /* remove 'curr' from list */
		      freeobj(L, curr);  /* erase 'curr' */
		    }
		    else {
		      if (gch(curr).tt == LUA_TTHREAD)
		        sweepthread(L, gco2th(curr));  /* sweep thread's upvalues */
		      if (testbits((byte)marked, tostop)) { //FIXME:(byte)
		        //static GCObject *nullp = NULL; //FIXME:moved, see upper
		        p = new NullpRef(g);  /* stop sweeping this list */
                break;
		      }
		      /* update marks */
		      gch(curr).marked = cast_byte((marked & toclear) | toset);
		      p = new NextRef(gch(curr));  /* go to next element */
		    }
		  }
          luaE_setdebt(g, debt);  /* sweeping should not change debt */
		  return p;
		}

		/* }====================================================== */


		/*
		** {======================================================
		** Finalization
		** =======================================================
		*/

		private static void checkSizes (lua_State L) {
		  global_State g = G(L);
		  if (g.gckind != KGC_EMERGENCY) {  /* do not change sizes in emergency */
		    int hs = g.strt.size / 2;  /* half the size of the string table */
		    if (g.strt.nuse < (lu_int32)(hs))  /* using less than that half? */
			  luaS_resize(L, hs);  /* halve its size */
			luaZ_freebuffer(L, g.buff);  /* free concatenation buffer */
		  }
		}


	    private static GCObject udata2finalize (global_State g) {
		  GCObject o = g.tobefnz;  /* get first element */
		  lua_assert(isfinalized(o));
		  g.tobefnz = gch(o).next;  /* remove it from 'tobefnz' list */
		  gch(o).next = g.allgc;  /* return it to 'allgc' list */
		  g.allgc = o;
		  resetbit(ref gch(o).marked, SEPARATED);  /* mark that it is not in 'tobefnz' */
		  lua_assert(!isold(o)); /* see MOVE OLD rule */
		  if (!keepinvariant(g))  /* not keeping invariant? */
		    makewhite(g, o);  /* "sweep" object */
		  return o;
		}


		private static void dothecall (lua_State L, object ud) {
		  //UNUSED(ud); //FIXME:removed
		  luaD_call(L, L.top - 2, 0, 0);
		}


		private static void GCTM (lua_State L, int propagateerrors) {
		  global_State g = G(L);
		  /*const*/ TValue tm;
		  TValue v = new TValue();
		  setgcovalue(L, v, udata2finalize(g));
		  tm = luaT_gettmbyobj(L, v, TM_GC);
		  if (tm != null && ttisfunction(tm)) {  /* is there a finalizer? */
            int status;
			lu_byte oldah = L.allowhook;
			int running  = g.gcrunning;
			L.allowhook = 0;  /* stop debug hooks during GC tag method */
			g.gcrunning = 0;  /* avoid GC steps */
			setobj2s(L, L.top, tm);  /* push finalizer... */
			setobj2s(L, L.top + 1, v);  /* ... and its argument */
			L.top += 2;  /* and (next line) call the finalizer */
		    status = luaD_pcall(L, dothecall, null, savestack(L, L.top - 2), 0);
		    L.allowhook = oldah;  /* restore hooks */
		    g.gcrunning = running;  /* restore state */
		    if (status != LUA_OK && propagateerrors != 0) {  /* error while running __gc? */
		      if (status == LUA_ERRRUN) {  /* is there an error msg.? */
		        luaO_pushfstring(L, "error in __gc tag method (%s)",
		                                        lua_tostring(L, -1));
		        status = LUA_ERRGCMM;  /* error in __gc metamethod */
		      }
		      luaD_throw(L, status);  /* re-send error */
		    }
		  }
		}


		/*
		** move all unreachable objects that need finalization from list 'finobj'
		** to list 'tobefnz'
		*/
		public static void luaC_separateudata (lua_State L, int all) {
		  global_State g = G(L);
		  GCObject **p = &g->finobj;
		  GCObject curr;
		  GCObjectRef lastnext = new TobefnzRef(g); //FIXME:??????next???
		  /* find last 'next' field in 'tobefnz' list (to add elements in its end) */
		  while (lastnext.get() != null) 
		    lastnext = new NextRef(gch(lastnext.get())); //FIXME:next???PtrRef???
		  while ((curr = p.get()) != null) {  /* traverse all finalizable objects */
		    lua_assert(!isfinalized(curr));
		    lua_assert(testbit(gch(curr).marked, SEPARATED));
		    if (!(all != 0 || iswhite(curr)))  /* not being collected? */
		    	p = new NextRef(gch(curr));  /* don't bother with it */
		    else {
		      l_setbit(ref gch(curr).marked, FINALIZEDBIT); /* won't be finalized again */
		      p.set(gch(curr).next);  /* remove 'curr' from 'finobj' list */
		      gch(curr).next = lastnext.get();  /* link at the end of 'tobefnz' list */
		      lastnext.set(curr);
		      lastnext = new NextRef(gch(curr));
		    }
		  }
		}


		/*
		** if object 'o' has a finalizer, remove it from 'allgc' list (must
		** search the list to find it) and link it in 'finobj' list.
		*/
		public static void luaC_checkfinalizer (lua_State L, GCObject o, Table mt) {
		  global_State g = G(L);
		  if (testbit(gch(o).marked, SEPARATED) || /* obj. is already separated... */
		      isfinalized(o) ||                        /* ... or is finalized... */
		      gfasttm(g, mt, TMS.TM_GC) == null)                /* or has no finalizer? */
		    return;  /* nothing to be done */
		  else {  /* move 'o' to 'finobj' list */
		    GCObjectRef p;
		    for (p = new AllGCRef(g); p.get() != o; p = new NextRef(gch(p.get()))) ;
		    p.set(gch(o).next);  /* remove 'o' from root list */
		    gch(o).next = g.finobj;  /* link it in list 'finobj' */
		    g.finobj = o;
		    l_setbit(ref gch(o).marked, SEPARATED);  /* mark it as such */
		    resetoldbit(o);  /* see MOVE OLD rule */
		  }
		}

		/* }====================================================== */


		/*
		** {======================================================
		** GC control
		** =======================================================
		*/


		private static readonly int sweepphases =
				(bitmask(GCSsweepstring) | bitmask(GCSsweepudata) | bitmask(GCSsweep));

		/*
		** change GC mode
		*/
		public static void luaC_changemode (lua_State L, int mode) {
		  global_State g = G(L);
		  if (mode == g.gckind) return;  /* nothing to change */
		  if (mode == KGC_GEN) {  /* change to generational mode */
		    /* make sure gray lists are consistent */
		    luaC_runtilstate(L, bitmask(GCSpropagate));
		    g.lastmajormem = gettotalbytes(g);
		    g.gckind = KGC_GEN;
		  }
		  else {  /* change to incremental mode */
		    /* sweep all objects to turn them back to white
		       (as white has not changed, nothing extra will be collected) */
		    g.sweepstrgc = 0;
		    g.gcstate = GCSsweepstring;
		    g.gckind = KGC_NORMAL;
		    luaC_runtilstate(L, ~sweepphases);
		  }
		}


		/*
		** call all pending finalizers */
		private static void callallpendingfinalizers (lua_State L, int propagateerrors) {
		  global_State g = G(L);
		  while (g.tobefnz!=null) {
            resetoldbit(g.tobefnz);
		  	GCTM(L, propagateerrors);
		  }
		}


		public static void luaC_freeallobjects (lua_State L) {
		  global_State g = G(L);
		  int i;
		  callallpendingfinalizers(L, 0);
		  /* following "white" makes all objects look dead */
		  g.currentwhite = (byte)WHITEBITS; //FIXME:added, (byte)
		  g.gckind = KGC_NORMAL;
		  sweepwholelist(L, &g->finobj); //FIXME:changed
		  lua_assert(g.finobj == null);
		  sweepwholelist(L, new AllGCRef(g)); //FIXME:changed
		  lua_assert(g.allgc == null);
		  for (i = 0; i < g.strt.size; i++)  /* free all string lists */
		    sweepwholelist(L, new ArrayRef(g.strt.hash, i)); //FIXME:changed
		  lua_assert(g.strt.nuse == 0);
		}


		private static void atomic (lua_State L) {
		  global_State g = G(L);
		  lua_assert(!iswhite(obj2gco(g.mainthread)));
		  markobject(g, L);  /* mark running thread */
		  /* registry and global metatables may be changed by API */
		  markvalue(g, g.l_registry);
		  markmt(g);  /* mark basic metatables */
		  /* remark occasional upvalues of (maybe) dead threads */
		  remarkupvals(g);
		  /* traverse objects caught by write barrier and by 'remarkupvals' */
		  propagateall(g);
		  traverselistofgrays(g, ref g.weak);  /* remark weak tables */
		  traverselistofgrays(g, ref g.ephemeron);  /* remark ephemeron tables */
  		  traverselistofgrays(g, ref g.grayagain);  /* remark gray again */
          convergeephemerons(g);
          /* at this point, all strongly accessible objects are marked. */
		  luaC_separateudata(L, 0);  /* separate userdata to be finalized */
		  markbeingfnz(g);  /* mark userdata that will be finalized */
		  propagateall(g);  /* remark, to propagate `preserveness' */
		  convergeephemerons(g);
		  /* remove collected objects from weak tables */
		  cleartable(g.weak);
		  cleartable(g.ephemeron);
		  cleartable(g.allweak);
		  g.sweepstrgc = 0;  /* prepare to sweep strings */
		  g.gcstate = GCSsweepstring;
		  g.currentwhite = cast_byte(otherwhite(g));  /* flip current white */
		  /*lua_checkmemory(L);*/
		}

		
		private static l_mem singlestep (lua_State L) {
		  global_State g = G(L);
		  switch (g.gcstate) {
			case GCSpause: {
		      if (!isgenerational(g))
		        markroot(g);  /* start a new collection */
		      /* in any case, root must be marked */
		      lua_assert(!iswhite(obj2gco(g.mainthread))
		              && !iswhite(gcvalue(g.l_registry)));
		      g.gcstate = GCSpropagate;
		      return GCROOTCOST;
			}
			case GCSpropagate: {
			  if (g.gray != null)
				return propagatemark(g);
			  else {  /* no more `gray' objects */
		        g.gcstate = GCSatomic;  /* finish mark phase */
		        atomic(L);
		        return GCATOMICCOST;
			  }
			}
			case GCSsweepstring: {
		  	  if (g.sweepstrgc < g.strt.size) {
		  		sweepwholelist(L, new ArrayRef(g.strt.hash, g.sweepstrgc++));
		        return GCSWEEPCOST;
		      }
		      else {  /* no more strings to sweep */
		  		g.sweepgc = &g->finobj;  /* prepare to sweep finalizable objects */
		        g.gcstate = GCSsweepudata;
		        return 0;
		      }
			}
			case GCSsweepudata: {
		  	  if (g.sweepgc.get() != null) {
		        g.sweepgc = sweeplist(L, g.sweepgc, GCSWEEPMAX);
		        return GCSWEEPMAX*GCSWEEPCOST;
		      }
		      else {
		  	    g.sweepgc = new AllGCRef(g);  /* go to next phase */ //FIXME:added, new PtrRef
		        g.gcstate = GCSsweep;
		        return GCSWEEPCOST;
		      }
		    }
			case GCSsweep: {
		  	  if (g.sweepgc.get() != null) {
		        g.sweepgc = sweeplist(L, g.sweepgc, GCSWEEPMAX);
		        return GCSWEEPMAX*GCSWEEPCOST;
		      }
		      else {
		        /* sweep main thread */
		        //GCObject mt = obj2gco(g.mainthread);//FIXME:added
		        g.mt_ = obj2gco(g.mainthread);
		        sweeplist(L, new MtRef(g), 1); //FIXME:changed, new PtrRef
		        g.mt_ = null; //FIXME:added
		        checkSizes(L);
		        g.gcstate = GCSpause;  /* finish collection */
		        return GCSWEEPCOST;
		      }
			}
			default: lua_assert(0); return 0;
		  }
		}


		/*
		** advances the garbage collector until it reaches a state allowed
		** by 'statemask'
		*/
		public static void luaC_runtilstate (lua_State L, int statesmask) {
		  global_State g = G(L);
		  while (!testbit((byte)statesmask, g.gcstate)) //FIXME:(byte)
		    singlestep(L);
		}


		private static void generationalcollection (lua_State L) {
		  global_State g = G(L);
		  if (g.lastmajormem == 0) {  /* signal for another major collection? */
		    luaC_fullgc(L, 0);  /* perform a full regular collection */
		    g.lastmajormem = gettotalbytes(g);  /* update control */
		  }
		  else {
		    luaC_runtilstate(L, ~bitmask(GCSpause));  /* run complete cycle */
		    luaC_runtilstate(L, bitmask(GCSpause));
		    if (gettotalbytes(g) > g.lastmajormem/100 * g.gcmajorinc)
		      g.lastmajormem = 0;  /* signal for a major collection */
		  }
		  luaE_setdebt(g, stddebt(g));
		}


		private static void step (lua_State L) {
		  global_State g = G(L);
		  l_mem lim = g.gcstepmul;  /* how much to work */
		  do {  /* always perform at least one single step */
		    lim -= singlestep(L);
		  } while (lim > 0 && g.gcstate != GCSpause);
		  if (g.gcstate != GCSpause)
		    luaE_setdebt(g, g.GCdebt - GCSTEPSIZE);
		  else
		    luaE_setdebt(g, stddebt(g));
		}


		/*
		** performs a basic GC step even if the collector is stopped
		*/
		public static void luaC_forcestep (lua_State L) {
          global_State g = G(L);
		  int i;
		  if (isgenerational(g)) generationalcollection(L);
		  else step(L);
		  for (i = 0; i < GCFINALIZENUM && g.tobefnz!=null; i++)
		    GCTM(L, 1);  /* Call a few pending finalizers */
		}


		/*
		** performs a basic GC step only if collector is running
		*/
		private static void luaC_step (lua_State L) {
		  if (G(L).gcrunning) luaC_forcestep(L);
		}


		/*
		** performs a full GC cycle; if "isemergency", does not call
		** finalizers (which could change stack positions)
		*/
		public static void luaC_fullgc (lua_State L, int isemergency) {
		  global_State g = G(L);
		  int origkind = g.gckind;
		  lua_assert(origkind != KGC_EMERGENCY);
		  if (isemergency==0)   /* do not run finalizers during emergency GC */
		    callallpendingfinalizers(L, 1);
		  if (keepinvariant(g)) {  /* marking phase? */
		    /* must sweep all objects to turn them back to white
		       (as white has not changed, nothing will be collected) */
		    g.sweepstrgc = 0;
		    g.gcstate = GCSsweepstring;
		  }
		  g.gckind = isemergency!=0 ? (byte)KGC_EMERGENCY : (byte)KGC_NORMAL; //FIXME:added, (byte)
		  /* finish any pending sweep phase to start a new cycle */
		  luaC_runtilstate(L, bitmask(GCSpause));
		  /* run entire collector */
		  luaC_runtilstate(L, ~bitmask(GCSpause));
		  luaC_runtilstate(L, bitmask(GCSpause));
		  if (origkind == KGC_GEN) {  /* generational mode? */
		    /* generational mode must always start in propagate phase */
		    luaC_runtilstate(L, bitmask(GCSpropagate));
		  }
		  g.gckind = (byte)origkind; //FIXME:added, (byte)
		  luaE_setdebt(g, stddebt(g));
		  if (isemergency==0)   /* do not run finalizers during emergency GC */
		    callallpendingfinalizers(L, 1);
		}

        /* }====================================================== */


	}
}

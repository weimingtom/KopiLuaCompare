/*
** $Id: lgc.c,v 2.133 2012/05/31 21:28:59 roberto Exp $
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

        /*
		** cost of sweeping one element (the size of a small object divided
		** by some adjust for the sweep speed)
		*/
		private static uint GCSWEEPCOST = (uint)((GetUnmanagedSize(typeof(TString)) + 4) / 4);

        /* maximum number of elements to sweep in each single step */
        private static uint GCSWEEPMAX		= (uint)((int)((GCSTEPSIZE / GCSWEEPCOST) / 4));

		/* maximum number of finalizers to call in each GC step */
		private const int GCFINALIZENUM	= 4;

		/*
		** macro to adjust 'stepmul': 'stepmul' is actually used like
		** 'stepmul / STEPMULADJ' (value chosen by tests)
		*/
		private const int STEPMULADJ = 200;

		/*
		** macro to adjust 'pause': 'pause' is actually used like
		** 'pause / PAUSEADJ' (value chosen by tests)
		*/
		private const int PAUSEADJ = 200;




		/*
		** standard negative debt for GC; a reasonable "time" to wait before
		** starting a new cycle
		*/
		private static int stddebtest(global_State g, uint e)	{ return (-(l_mem)((e)/PAUSEADJ) * g.gcpause); }
		private static int stddebt(global_State g) { return stddebtest(g, gettotalbytes(g)); }


		/*
		** 'makewhite' erases all color bits plus the old bit and then
		** sets only the current white bit
		*/
		public static byte maskcolors	= (byte)(~(bit2mask(BLACKBIT, OLDBIT) | WHITEBITS)); //FIXME: cast_byte() removed 
		public static void makewhite(global_State g, GCObject x) {
		   gch(x).marked = (byte)(gch(x).marked & maskcolors | luaC_white(g)); }

		public static void white2gray(GCObject x) { resetbits(ref gch(x).marked, WHITEBITS); }
		public static void black2gray(GCObject x) { resetbit(ref gch(x).marked, BLACKBIT); }

		public static bool isfinalized(GCObject x) { return testbit(gch(x).marked, FINALIZEDBIT); }
        public static bool isfinalized(GCheader u) { return testbit(u.marked, FINALIZEDBIT); } //FIXME:added

		public static void checkdeadkey(Node n) { lua_assert(!ttisdeadkey(gkey(n)) || ttisnil(gval(n)));} 


		private static void checkconsistency(TValue obj) {
  			lua_longassert(!iscollectable(obj) || righttt(obj)); }


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
		** one after last element in a hash array
		*/
		//#define gnodelast(h)	gnode(h, cast(size_t, sizenode(h)))


		/*
		** link table 'h' into list pointed by 'p'
		*/
		private static void linktable (Table h, ref GCObject p) { h.gclist = p; p = obj2gco(h); }
		
		
		/*
		** if key is not marked, mark its entry as dead (therefore removing it
		** from the table)
		*/
		private static void removeentry (Node n) {
		  lua_assert(ttisnil(gval(n)));
		  if (valiswhite(gkey(n)))
			setdeadvalue(gkey(n));  /* unused and unmarked key; remove it */
		}


		/*
		** tells whether a key or value can be cleared from a weak
		** table. Non-collectable objects are never removed from weak
		** tables. Strings behave as `values', so are never removed too. for
		** other objects: if really collected, cannot keep them; for objects
		** being finalized, keep them in keys, but not in values
		*/
		private static int iscleared (global_State g, TValue o) {
		  if (!iscollectable(o)) return 0;
		  else if (ttisstring(o)) {
		    markobject(g, rawtsvalue(o));  /* strings are `values', so are never weak */
		    return 0;
		  }
		  else return iswhite(gcvalue(o)) ? 1 : 0;
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
		  lua_assert(isblack(o) && !isdead(g, o) && gch(o).tt == LUA_TTABLE);
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
		  object raw = luaM_newobject<T>(L/*, novariant(tt), sz*/);
		  GCObject o = obj2gco(raw/* + offset*/); //FIXME:???no offset
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
		** mark an object. Userdata, strings, and closed upvalues are visited
		** and turned black here. Other objects are marked gray and added
		** to appropriate list to be visited (and turned black) later. (Open
		** upvalues are already linked in 'headuv' list.)
		*/
		private static void reallymarkobject (global_State g, GCObject o) {
		  lu_mem size;
		  white2gray(o);
		  switch (gch(o).tt) {
			case LUA_TSHRSTR:
    		case LUA_TLNGSTR: {
		  	  size = (uint)sizestring(gco2ts(o));
      		  break;  /* nothing else to mark; make it black */
			}
			case LUA_TUSERDATA: {
			  Table mt = gco2u(o).metatable;
			  markobject(g, mt);
			  markobject(g, gco2u(o).env);
              gray2black(o);  /* all pointers marked */
			  size = sizeudata(gco2u(o));
      		  break;
			}
			case LUA_TUPVAL: {
			  UpVal uv = gco2uv(o);
			  markvalue(g, uv.v);
			  if (uv.v != uv.u.value_)   /* open? */
				return;  /* open upvalues remain gray */ 
				size = (uint)GetUnmanagedSize(typeof(UpVal));
      		  break;
			}
			case LUA_TLCL: {
		      gco2lcl(o).gclist = g.gray;
		      g.gray = o;
		      return;
		    }
			case LUA_TCCL: {
			  gco2ccl(o).gclist = g.gray;
			  g.gray = o;
			  return;
			}
			case LUA_TTABLE: {
			  linktable(gco2t(o), ref g.gray);
			  return;
			}
			case LUA_TTHREAD: {
			  gco2th(o).gclist = g.gray;
			  g.gray = o;
			  return;
			}
			case LUA_TPROTO: {
			  gco2p(o).gclist = g.gray;
			  g.gray = o;
			  return;
			}
			default: lua_assert(0);  return; //FIXME: add break
		  }
		  gray2black(o);
  		  g.GCmemtrav += size;
		}


		/*
		** mark metamethods for basic types
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
		  Node n;//, limit = gnodelast(h); //FIXME:removed, overflow
		  /* if there is array part, assume it may have white values (do not
		     traverse it just to check) */
		  int hasclears = (h.sizearray > 0)?1:0;
		  //for (n = gnode(h, 0); n < limit; n++) { //FIXME:changed, see below
		  for (int ni = 0; ni < sizenode(h); ni++) { //FIXME:changed, gnodelast(h) to sizenode(h)
		  	n = gnode(h, ni);
		    
		  	checkdeadkey(n);
		    if (ttisnil(gval(n)))  /* entry is empty? */
		      removeentry(n);  /* remove it */
		    else {
		      lua_assert(!ttisnil(gkey(n)));
		      markvalue(g, gkey(n));  /* mark key */
		      if (hasclears==0 && iscleared(g, gval(n))!=0)  /* is there a white value? */
		        hasclears = 1;  /* table will have to be cleared */
		    }
		  }
          if (hasclears!=0)
		    linktable(h, ref g.weak);  /* has to be cleared later */
		  else  /* no white values */
		    linktable(h, ref g.grayagain);  /* no need to clean */
		}


		private static int traverseephemeron (global_State g, Table h) {
		  int marked = 0;  /* true if an object is marked in this traversal */
		  int hasclears = 0;  /* true if table has white keys */
		  int prop = 0;  /* true if table has entry "white-key -> white-value" */
		  Node n;// limit = gnodelast(h); //FIXME:removed, overflow
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
		  for (int ni = 0; ni < sizenode(h); ni++) { //FIXME:changed, gnodelast(h) to sizenode(h)
		  	n = gnode(h, ni);		  
		  	
		    checkdeadkey(n);
		    if (ttisnil(gval(n)))  /* entry is empty? */
		      removeentry(n);  /* remove it */
		    else if (iscleared(g, gkey(n))!=0) {  /* key is not marked (yet)? */
		      hasclears = 1;  /* table must be cleared */
		      if (valiswhite(gval(n)))  /* value not marked yet? */
		        prop = 1;  /* must propagate again */
		    }
		    else if (valiswhite(gval(n))) {  /* value not marked yet? */
		      marked = 1;
		      reallymarkobject(g, gcvalue(gval(n)));  /* mark it now */
		    }
		  }
		  if (prop!=0)
		    linktable(h, ref g.ephemeron);  /* have to propagate again */
		  else if (hasclears!=0)  /* does table have white keys? */
		    linktable(h, ref g.allweak);  /* may have to clean white keys */
		  else  /* no white keys */
		    linktable(h, ref g.grayagain);  /* no need to clean */
		  return marked;
		}


		private static void traversestrongtable (global_State g, Table h) {
          Node n;// limit = gnodelast(h); //FIXME:removed, overflow
		  int i;
		  for (i = 0; i < h.sizearray; i++)  /* traverse array part */
		    markvalue(g, h.array[i]);
		  //for (n = gnode(h, 0); n < limit; n++) {  /* traverse hash part */ //FIXME:changed, see below
		  for (int ni = 0; ni < sizenode(h); ni++) { //FIXME:changed, gnodelast(h) to sizenode(h)
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

		private static bool __traversetable_test(TValue mode, ref CharPtr weakkey, ref CharPtr weakvalue) {
		   weakkey = strchr(svalue(mode), 'k');
		   weakvalue = strchr(svalue(mode), 'v');
	       return weakkey != null || weakvalue != null;
		}
		private static lu_mem traversetable (global_State g, Table h) {
		  CharPtr weakkey = null, weakvalue = null;
		  TValue mode = gfasttm(g, h.metatable, TMS.TM_MODE);
		  markobject(g, h.metatable);
		  //FIXME:??? modify mode!=0 to mode!=null, avoid lua_TValue.operator int() exception
		  if (mode != null && ttisstring(mode) &&  /* is there a weak mode? */
		      __traversetable_test(mode, ref weakkey, ref weakvalue)) {  /* is really weak? */ //FIXME: see __traversetable_test 
		      black2gray(obj2gco(h));  /* keep table gray */
		      if (weakkey == null)  /* strong keys? */
		        traverseweakvalue(g, h);
		      else if (weakvalue == null)  /* strong values? */
		        traverseephemeron(g, h);
		      else   /* all weak */
		        linktable(h, ref g.allweak);  /* nothing to traverse now */
		  }
		  else  /* not weak */
    		traversestrongtable(g, h); 
		  traversestrongtable(g, h);
		  return (uint)(GetUnmanagedSize(typeof(Table)) + GetUnmanagedSize(typeof(TValue)) * h.sizearray +
		                GetUnmanagedSize(typeof(Node)) * sizenode(h));
		}

		//FIXME:<----------------------------
		private static int traverseproto (global_State g, Proto f) {
		  int i;
		  if (f.cache != null && iswhite(obj2gco(f.cache)))
		    f.cache = null;  /* allow cache to be collected */
		  markobject(g, f.source);
		  for (i = 0; i < f.sizek; i++)  /* mark literals */
			markvalue(g, f.k[i]);
		  for (i = 0; i < f.sizeupvalues; i++)  /* mark upvalue names */
			markobject(g, f.upvalues[i].name);
		  for (i = 0; i < f.sizep; i++)  /* mark nested protos */
		    markobject(g, f.p[i]);
		  for (i = 0; i < f.sizelocvars; i++)  /* mark local-variable names */
			markobject(g, f.locvars[i].varname);
			return GetUnmanagedSize(typeof(Proto)) + GetUnmanagedSize(typeof(Instruction)) * f.sizecode +
			             GetUnmanagedSize(typeof(Proto)) * f.sizep + //FIXME:Proto *
 			             GetUnmanagedSize(typeof(TValue)) * f.sizek +
			             GetUnmanagedSize(typeof(int)) * f.sizelineinfo +
			             GetUnmanagedSize(typeof(LocVar)) * f.sizelocvars +
			             GetUnmanagedSize(typeof(Upvaldesc)) * f.sizeupvalues;
		}


		private static lu_mem traverseCclosure (global_State g, CClosure cl) {
	      int i;
		  for (i=0; i<cl.nupvalues; i++)  /* mark its upvalues */
			  markvalue(g, cl.upvalue[i]);
          return sizeLclosure(cl.nupvalues);
		}

		private static lu_mem traverseLclosure (global_State g, LClosure cl) {
		  int i;
		  markobject(g, cl.p);  /* mark its prototype */
		  for (i = 0; i < cl.nupvalues; i++)  /* mark its upvalues */
		    markobject(g, cl.upvals[i]);
		  return sizeLclosure(cl.nupvalues);
		}

		private static lu_mem traversestack (global_State g, lua_State th) {
		  StkId[] o_ = th.stack; //FIXME:???o_
		  if (o_ == null) //FIXME:???o_
		    return 1;  /* stack not completely built yet */
		  StkId o = new lua_TValue(o_); //FIXME:o_->o
		  for (; o < th.top; /*StkId.inc(ref o)*/o = o + 1) {//FIXME:L.stack->new StkId(L.stack[0]) //FIXME:don't use StackId.inc(), overflow ([-1])
		    markvalue(g, o);
		    
		    //------------------------
		    if (o >= th.top - 1) 
		    {
		    	break;//FIXME:added, o + 1 will overflow
		    }
		    //------------------------
		  }
		  if (g.gcstate == GCSatomic) {  /* final traversal? */
		  	StkId limMinus1 = th.stack[th.stacksize-1];  /* real end of stack */ //FIXME:L.stack[L.stacksize] will overvlow, changed it
		  	for (; o <= limMinus1; /*StkId.inc(ref o)*/o = o + 1) { /* clear not-marked stack slice */ //FIXME:overflow, changed 'o < lim' to 'o <= limMinus1'
		      setnilvalue(o);
			  
		      //------------------------
		      if (o >= th.top - 1)
			  {
			  	break;//FIXME:added, o + 1 will overflow
			  }
		      //------------------------
		  	}
		  }
		  return (uint)(GetUnmanagedSize(typeof(lua_State)) + GetUnmanagedSize(typeof(TValue)) * th.stacksize);
		}


		/*
		** traverse one gray object, turning it to black (except for threads,
		** which are always gray).
		** Returns number of values traversed.
		*/
		private static void propagatemark (global_State g) {
		  lu_mem size;
		  GCObject o = g.gray;
		  lua_assert(isgray(o));
		  gray2black(o);
		  switch (gch(o).tt) {
			case LUA_TTABLE: {
			  Table h = gco2t(o);
			  g.gray = h.gclist;  /* remove from 'gray' list */
			  size = traversetable(g, h);
			  break;
			}
			case LUA_TLCL: {
			  LClosure cl = gco2lcl(o);
			  g.gray = cl.gclist;  /* remove from 'gray' list */
			  size = traverseLclosure(g, cl);
			  break;
			}
		    case LUA_TCCL: {
		      CClosure cl = gco2ccl(o);
		      g.gray = cl.gclist;  /* remove from 'gray' list */
		      size = traverseCclosure(g, cl);
		      break;
		    }			
			case LUA_TTHREAD: {
			  lua_State th = gco2th(o);
			  g.gray = th.gclist;  /* remove from 'gray' list */
			  th.gclist = g.grayagain;
			  g.grayagain = o;  /* insert into 'grayagain' list */
			  black2gray(o);
			  size = traversestack(g, th);
			  break;
			}
			case LUA_TPROTO: {
			  Proto p = gco2p(o);
			  g.gray = p.gclist;  /* remove from 'gray' list */
			  size = (uint)traverseproto(g, p);
			  break;
			}
			default: lua_assert(0); return;
		  }
		  g.GCmemtrav += size;
		}


		private static void propagateall (global_State g) {
		  while (g.gray != null) propagatemark(g);
		}


		private static void propagatelist (global_State g, GCObject l) {
		  lua_assert(g.gray == null);  /* no grays left */
		  g.gray = l;
		  propagateall(g);  /* traverse all elements from 'l' */
		}

		/*
		** retraverse all gray lists. Because tables may be reinserted in other
		** lists when traversed, traverse the original lists to avoid traversing
		** twice the same table (which is not wrong, but inefficient)
		*/
		private static void retraversegrays (global_State g) {
		  GCObject weak = g.weak;  /* save original lists */
		  GCObject grayagain = g.grayagain;
		  GCObject ephemeron = g.ephemeron;
		  g.weak = g.grayagain = g.ephemeron = null;
		  propagateall(g);  /* traverse main gray list */
		  propagatelist(g, grayagain);
		  propagatelist(g, weak);
		  propagatelist(g, ephemeron);
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
		** clear entries with unmarked keys from all weaktables in list 'l' up
		** to element 'f'
		*/
		private static void clearkeys (global_State g, GCObject l, GCObject f) {
		  for (; l != f; l = gco2t(l).gclist) {
		    Table h = gco2t(l);
		    Node n;// limit = gnodelast(h); //FIXME:removed, overflow
		    //for (n = gnode(h, 0); n < limit; n++) {  /* traverse hash part */ //FIXME:changed, see below
		    for (int ni = 0; ni < sizenode(h); ni++) { //FIXME:changed, gnodelast(h) to sizenode(h)
		      n = gnode(h, ni);
			
			  if (!ttisnil(gval(n)) && (iscleared(g, gkey(n)))!=0) {
		        setnilvalue(gval(n));  /* remove value ... */
		        removeentry(n);  /* and remove entry from table */
		      }
		    }
		  }
		}


		/*
		** clear entries with unmarked values from all weaktables in list 'l' up
		** to element 'f'
		*/		
		private static void clearvalues (global_State g, GCObject l, GCObject f) {
		  for (; l != null; l = gco2t(l).gclist) {
			Table h = gco2t(l);
			Node n; // limit = gnode(h, sizenode(h)); //FIXME:removed, overflow
		    int i;
		    for (i = 0; i < h.sizearray; i++) {
			  TValue o = h.array[i];
			  if (iscleared(g, o) != 0)  /* value was collected? */
			    setnilvalue(o);  /* remove value */
		    }
			//for (n = gnode(h, 0); n < limit; n++) {//FIXME:changed, see below
			for (int ni = 0; ni < sizenode(h); ni++) { //FIXME:changed, gnodelast(h) to sizenode(h)
			  n = gnode(h, ni);		 
			  
			  if (!ttisnil(gval(n)) && iscleared(g, gval(n)) != 0) {
				setnilvalue(gval(n));  /* remove value ... */
				removeentry(n);  /* and remove entry from table */
			  }
			}
		  }
		}


		private static void freeobj (lua_State L, GCObject o) {
		  switch (gch(o).tt) {
			case LUA_TPROTO: luaF_freeproto(L, gco2p(o)); break;
			case LUA_TLCL: {
			  luaM_freemem(L, o, sizeLclosure(gco2lcl(o).nupvalues));
		      break;
		    }
		    case LUA_TCCL: {
			  luaM_freemem(L, o, sizeCclosure(gco2ccl(o).nupvalues));
		      break;
		    }
			case LUA_TUPVAL: luaF_freeupval(L, gco2uv(o)); break;
			case LUA_TTABLE: luaH_free(L, gco2t(o)); break;
			case LUA_TTHREAD: luaE_freethread(L, gco2th(o)); break;
    		case LUA_TUSERDATA: 
				//luaM_freemem(L, o, sizeudata(gco2u(o)));
				luaM_freemem(L, gco2u(o), sizeudata(gco2u(o))); //FIXME:???
				break;
			case LUA_TSHRSTR:
			  G(L).strt.nuse--;
			  goto case LUA_TLNGSTR; /* go through */ //FIXME:TODO
    		case LUA_TLNGSTR: {
			  //luaM_freemem(L, o, sizestring(gco2ts(o)));
			  luaM_freemem(L, gco2ts(o), (uint)sizestring(gco2ts(o))); //FIXME:???
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
		      if (testbits((byte)marked, tostop))
		        return null;  /* stop sweeping this list */			
		      if (gch(curr).tt == LUA_TTHREAD)
		        sweepthread(L, gco2th(curr));  /* sweep thread's upvalues */
		      /* update marks */
		      gch(curr).marked = cast_byte((marked & toclear) | toset);
		      p = new NextRef(gch(curr));  /* go to next element */
		    }
		  }
		  return (p.get() == null) ? null : p;
		}

		/*
		** sweep a list until a live object (or end of list)
		*/
		static GCObjectRef sweeptolive (lua_State L, GCObjectRef p, ref int n) {
		  GCObjectRef old = p;
		  int i = 0;
		  do {
		    i++;
		    p = sweeplist(L, p, 1);
		  } while (p == old);
		  n += i; //if (n) *n += i; //FIXME:int * -> ref int
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
		  tm = luaT_gettmbyobj(L, v, TMS.TM_GC);
		  if (tm != null && ttisfunction(tm)) {  /* is there a finalizer? */
            int status;
			lu_byte oldah = L.allowhook;
			int running  = g.gcrunning;
			L.allowhook = 0;  /* stop debug hooks during GC metamethod */
			g.gcrunning = 0;  /* avoid GC steps */
			setobj2s(L, L.top, tm);  /* push finalizer... */
			setobj2s(L, L.top + 1, v);  /* ... and its argument */
			L.top += 2;  /* and (next line) call the finalizer */
		    status = luaD_pcall(L, dothecall, null, savestack(L, L.top - 2), 0);
		    L.allowhook = oldah;  /* restore hooks */
		    g.gcrunning = (byte)running;  /* restore state */ //FIXME:changed, (byte)
		    if (status != LUA_OK && propagateerrors != 0) {  /* error while running __gc? */
		      if (status == LUA_ERRRUN) {  /* is there an error object? */
			    CharPtr msg = (ttisstring(L.top - 1))
                            ? svalue(L.top - 1)
                            : "no message";
		        luaO_pushfstring(L, "error in __gc metamethod (%s)", msg);
		        status = LUA_ERRGCMM;  /* error in __gc metamethod */
		      }
		      luaD_throw(L, status);  /* re-throw error */
		    }
		  }
		}


		/*
		** move all unreachable objects (or 'all' objects) that need
		** finalization from list 'finobj' to list 'tobefnz' (to be finalized)
		*/
		private static void separatetobefnz (lua_State L, int all) {
		  global_State g = G(L);
		  GCObjectRef p = new FinobjRef(g);
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
		    int null_ = 0;
		    GCheader ho = gch(o);
		    if (g.sweepgc == ho.next) {  /* avoid removing current sweep object */
		      lua_assert(issweepphase(g));
		      g.sweepgc = sweeptolive(L, g.sweepgc, ref null_);
		    }
		    /* search for pointer pointing to 'o' */			
		    for (p = new AllGCRef(g); p.get() != o; p = new NextRef(gch(p.get()))) { /* empty */ }
		    p.set(ho.next);  /* remove 'o' from root list */
		    ho.next = g.finobj;  /* link it in list 'finobj' */
		    g.finobj = o;
		    l_setbit(ref ho.marked, SEPARATED);  /* mark it as such */
		    if (!keepinvariant(g))  /* not keeping invariant? */
		      makewhite(g, o);  /* "sweep" object */
		    else			
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
		** enter first sweep phase (strings) and prepare pointers for other
		** sweep phases.  The calls to 'sweeptolive' make pointers point to an
		** object inside the list (instead of to the header), so that the real
		** sweep do not need to skip objects created between "now" and the start
		** of the real sweep.
		** Returns how many objects it sweeped.
		*/
		private static int entersweep (lua_State L) {
		  global_State g = G(L);
		  int n = 0;
		  g.gcstate = GCSsweepstring;
		  lua_assert(g.sweepgc == null && g.sweepfin == null);
		  /* prepare to sweep strings, finalizable objects, and regular objects */
		  g.sweepstrgc = 0;
		  g.sweepfin = sweeptolive(L, new FinobjRef(g), ref n);
		  g.sweepgc = sweeptolive(L, new AllGCRef(g), ref n);
		  return n;
		}


		/*
		** change GC mode
		*/
		public static void luaC_changemode (lua_State L, int mode) {
		  global_State g = G(L);
		  if (mode == g.gckind) return;  /* nothing to change */
		  if (mode == KGC_GEN) {  /* change to generational mode */
		    /* make sure gray lists are consistent */
		    luaC_runtilstate(L, bitmask(GCSpropagate));
		    g.GCestimate = gettotalbytes(g);
		    g.gckind = KGC_GEN;
		  }
		  else {  /* change to incremental mode */
		    /* sweep all objects to turn them back to white
		       (as white has not changed, nothing extra will be collected) */
		    g.gckind = KGC_NORMAL;
			entersweep(L);
		    luaC_runtilstate(L, ~sweepphases);
		  }
		}


		/*
		** call all pending finalizers 
		*/
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
		  separatetobefnz(L, 1);  /* separate all objects with finalizers */
		  lua_assert(g.finobj == null);
		  callallpendingfinalizers(L, 0);
		  g.currentwhite = (byte)WHITEBITS; /* this "white" makes all objects look dead */ //FIXME:added, (byte)
		  g.gckind = KGC_NORMAL;
		  sweepwholelist(L, new FinobjRef(g));  /* finalizers can create objs. in 'finobj' */ //FIXME:changed
		  sweepwholelist(L, new AllGCRef(g)); //FIXME:changed
		  for (i = 0; i < g.strt.size; i++)  /* free all string lists */
		    sweepwholelist(L, new ArrayRef(g.strt.hash, i)); //FIXME:changed
		  lua_assert(g.strt.nuse == 0);
		}


		private static l_mem atomic (lua_State L) {
		  global_State g = G(L);
		  l_mem work = (int)(-g.GCmemtrav);  /* start counting work */
          GCObject origweak, origall;
		  lua_assert(!iswhite(obj2gco(g.mainthread)));
		  markobject(g, L);  /* mark running thread */
		  /* registry and global metatables may be changed by API */
		  markvalue(g, g.l_registry);
		  markmt(g);  /* mark basic metatables */
		  /* remark occasional upvalues of (maybe) dead threads */
		  remarkupvals(g);
		  propagateall(g);  /* propagate changes */
		  work += (int)g.GCmemtrav;  /* stop counting (do not (re)count grays) */
		  /* traverse objects caught by write barrier and by 'remarkupvals' */
		  retraversegrays(g);
		  work -= (int)g.GCmemtrav;  /* restart counting */
          convergeephemerons(g);
          /* at this point, all strongly accessible objects are marked. */
		  /* clear values from weak tables, before checking finalizers */
		  clearvalues(g, g.weak, null);
		  clearvalues(g, g.allweak, null);
		  origweak = g.weak; origall = g.allweak;
		  work += (int)g.GCmemtrav;  /* stop counting (objects being finalized) */
		  separatetobefnz(L, 0);  /* separate objects to be finalized */
		  markbeingfnz(g);  /* mark objects that will be finalized */
		  propagateall(g);  /* remark, to propagate `preserveness' */
		  work -= (int)g.GCmemtrav;  /* restart counting */
		  convergeephemerons(g);
		  /* at this point, all resurrected objects are marked. */
		  /* remove dead objects from weak tables */
		  clearkeys(g, g.ephemeron, null);  /* clear keys from all ephemeron tables */
		  clearkeys(g, g.allweak, null);  /* clear keys from all allweak tables */
		  /* clear values from resurrected weak tables */
		  clearvalues(g, g.weak, origweak);
		  clearvalues(g, g.allweak, origall);
		  g.currentwhite = cast_byte(otherwhite(g));  /* flip current white */
		  work += (int)g.GCmemtrav;  /* complete counting */
		  return work;  /* estimate of memory marked by 'atomic' */
		}

		
		private static lu_mem singlestep (lua_State L) {
		  global_State g = G(L);
		  switch (g.gcstate) {
			case GCSpause: {
			  g.GCmemtrav = 0;  /* start to count memory traversed */
		      if (!isgenerational(g))
		        markroot(g);  /* start a new collection */
		      /* in any case, root must be marked at this point */
		      lua_assert(!iswhite(obj2gco(g.mainthread))
		              && !iswhite(gcvalue(g.l_registry)));
		      g.gcstate = GCSpropagate;
		      return g.GCmemtrav;
			}
			case GCSpropagate: {
			  if (g.gray != null) {
			    lu_mem oldtrav = g.GCmemtrav;
				propagatemark(g);
				return g.GCmemtrav - oldtrav;  /* memory traversed in this step */
			  }
			  else {  /* no more `gray' objects */
			    lu_mem work;
        		int sw;
		        g.gcstate = GCSatomic;  /* finish mark phase */
				g.GCestimate = g.GCmemtrav;  /* save what was counted */;
				work = (uint)atomic(L);  /* add what was traversed by 'atomic' */
				g.GCestimate += work;  /* estimate of total memory traversed */ 
        		sw = entersweep(L);
        		return (uint)(work + sw * GCSWEEPCOST);
			  }
			}
			case GCSsweepstring: {
			  int i;
		  	  for (i = 0; i < GCSWEEPMAX && g.sweepstrgc + i < g.strt.size; i++)
		  		sweepwholelist(L, new ArrayRef(g.strt.hash, g.sweepstrgc + i));
		      g.sweepstrgc += i;
		      if (g.sweepstrgc >= g.strt.size)  /* no more strings to sweep? */
		        g.gcstate = GCSsweepudata;
		      return (uint)(i * GCSWEEPCOST);
			}
			case GCSsweepudata: {
		  	  if (g.sweepfin!=null) {
		        g.sweepfin = sweeplist(L, g.sweepfin, GCSWEEPMAX);
		        return GCSWEEPMAX*GCSWEEPCOST;
		      }
		      else {
		        g.gcstate = GCSsweep;
		        return 0;
		      }
		    }
			case GCSsweep: {
		  	  if (g.sweepgc != null) {
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
		  if (g.GCestimate == 0) {  /* signal for another major collection? */
		    luaC_fullgc(L, 0);  /* perform a full regular collection */
		    g.GCestimate = gettotalbytes(g);  /* update control */
		  }
		  else {
		    lu_mem estimate = g.GCestimate;
		    luaC_runtilstate(L, ~bitmask(GCSpause));  /* run complete cycle */
		    luaC_runtilstate(L, bitmask(GCSpause));
		    if (gettotalbytes(g) > (estimate / 100) * g.gcmajorinc)
		      g.GCestimate = 0;  /* signal for a major collection */
		  }
		  luaE_setdebt(g, stddebt(g));
		}


		private static void incstep (lua_State L) {
		  global_State g = G(L);
		  l_mem debt = g.GCdebt;
		  int stepmul = g.gcstepmul;
		  if (stepmul < 40) stepmul = 40;  /* avoid ridiculous low values */
		  /* convert debt from Kb to 'work units' (avoid zero debt and overflows) */
		  debt = (debt / STEPMULADJ) + 1;
		  debt = (debt < MAX_LMEM / stepmul) ? debt * stepmul : MAX_LMEM;
		  do {  /* always perform at least one single step */
		    lu_mem work = singlestep(L);  /* do some work */
		    debt -= (int)work;
		  } while (debt > -GCSTEPSIZE && g.gcstate != GCSpause);
		  if (g.gcstate == GCSpause)
		    debt = stddebtest(g, g.GCestimate);  /* pause until next cycle */
		  else
		    debt = (debt / stepmul) * STEPMULADJ;  /* convert 'work units' to Kb */
		  luaE_setdebt(g, stddebt(g));
		}


		/*
		** performs a basic GC step even if the collector is stopped
		*/
		public static void luaC_forcestep (lua_State L) {
          global_State g = G(L);
		  int i;
		  if (isgenerational(g)) generationalcollection(L);
		  else incstep(L);
		  /* run a few finalizers (or all of them at the end of a collect cycle) */
		  for (i = 0; g.tobefnz != null && (i < GCFINALIZENUM || g.gcstate == GCSpause); i++)
		    GCTM(L, 1);  /* call one finalizer */
		}


		/*
		** performs a basic GC step only if collector is running
		*/
		private static void luaC_step (lua_State L) {
		  if (G(L).gcrunning!=0) luaC_forcestep(L);
		}


		/*
		** performs a full GC cycle; if "isemergency", does not call
		** finalizers (which could change stack positions)
		*/
		public static void luaC_fullgc (lua_State L, int isemergency) {
		  global_State g = G(L);
		  int origkind = g.gckind;
		  int someblack = keepinvariant(g)?1:0;
		  lua_assert(origkind != KGC_EMERGENCY);
		  if (isemergency!=0)   /* do not run finalizers during emergency GC */
		    g.gckind = KGC_EMERGENCY;
		  else {
    		g.gckind = KGC_NORMAL;	
			callallpendingfinalizers(L, 1);
		  }
		  if (someblack!=0) {  /* may there be some black objects? */
		    /* must sweep all objects to turn them back to white
		       (as white has not changed, nothing will be collected) */
		    entersweep(L);
		  }
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

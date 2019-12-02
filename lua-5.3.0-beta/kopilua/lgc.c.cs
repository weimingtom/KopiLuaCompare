/*
** $Id: lgc.c,v 2.196 2014/10/03 12:54:37 roberto Exp $
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
		** internal state for collector while inside the atomic phase. The
		** collector should never be in this state while running regular code.
		*/
		public const int GCSinsideatomic = (GCSpause + 1);

        /*
		** cost of sweeping one element (the size of a small object divided
		** by some adjust for the sweep speed)
		*/
		private static uint GCSWEEPCOST = (uint)((GetUnmanagedSize(typeof(TString)) + 4) / 4);

        /* maximum number of elements to sweep in each single step */
        private static uint GCSWEEPMAX		= (uint)((int)((GCSTEPSIZE / GCSWEEPCOST) / 4));

		/* cost of calling one finalizer */
		private static uint GCFINALIZECOST	= GCSWEEPCOST;

		/*
		** macro to adjust 'stepmul': 'stepmul' is actually used like
		** 'stepmul / STEPMULADJ' (value chosen by tests)
		*/
		private const int STEPMULADJ = 200;


		/*
		** macro to adjust 'pause': 'pause' is actually used like
		** 'pause / PAUSEADJ' (value chosen by tests)
		*/
		private const int PAUSEADJ = 100;







		/*
		** 'makewhite' erases all color bits then sets only the current white
		** bit
		*/
		public static byte maskcolors	= (byte)(~(bitmask(BLACKBIT) | WHITEBITS)); //FIXME: cast_byte() removed 
		public static void makewhite(global_State g, GCObject x) {
		   x.marked = (byte)(x.marked & maskcolors | luaC_white(g)); }

		public static void white2gray(GCObject x) { resetbits(ref x.marked, WHITEBITS); }
		public static void black2gray(GCObject x) { resetbit(ref x.marked, BLACKBIT); }

		public static bool valiswhite(TValue x) { return (iscollectable(x) && iswhite(gcvalue(x))); }

		public static void checkdeadkey(Node n) { lua_assert(!ttisdeadkey(gkey(n)) || ttisnil(gval(n)));} 


		private static void checkconsistency(TValue obj) {
  			lua_longassert(!iscollectable(obj) || righttt(obj)); }


		public static void markvalue(global_State g, TValue o)  { checkconsistency(o);
			if (valiswhite(o)) reallymarkobject(g,gcvalue(o)); }

		public static void markobject(global_State g, GCObject t) 
			{ if (t != null && iswhite(t)) reallymarkobject(g, obj2gco(t)); }

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
		** link collectable object 'o' into list pointed by 'p'
		*/
		private static void linkgclist (Table o, ref GCObject p) { o.gclist = p; p = obj2gco(o); }
		private static void linkgclist (LClosure o, ref GCObject p) { o.gclist = p; p = obj2gco(o); }
		private static void linkgclist (CClosure o, ref GCObject p) { o.gclist = p; p = obj2gco(o); }
		private static void linkgclist (lua_State o, ref GCObject p) { o.gclist = p; p = obj2gco(o); }
		private static void linkgclist (Proto o, ref GCObject p) { o.gclist = p; p = obj2gco(o); }
		
		
		/*
		** if key is not marked, mark its entry as dead (therefore removing it
		** from the table)
		*/
		private static void removeentry (Node n) {
		  lua_assert(ttisnil(gval(n)));
		  if (valiswhite(gkey(n)))
			setdeadvalue(wgkey(n));  /* unused and unmarked key; remove it */
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
		    markobject(g, tsvalue(o));  /* strings are `values', so are never weak */
		    return 0;
		  }
		  else return iswhite(gcvalue(o)) ? 1 : 0;
		}


		/*
		** barrier that moves collector forward, that is, mark the white object
		** being pointed by a black object. (If in sweep phase, clear the black
		** object to white [sweep it] to avoid other barrier calls for this
		** same object.)
		*/
		public static void luaC_barrier_ (lua_State L, GCObject o, GCObject v) {
		  global_State g = G(L);
		  lua_assert(isblack(o) && iswhite(v) && !isdead(g, v) && !isdead(g, o));
		  if (keepinvariant(g))  /* must keep invariant? */
		    reallymarkobject(g, v);  /* restore invariant */
		  else {  /* sweep phase */
		    lua_assert(issweepphase(g));
		    makewhite(g, o);  /* mark main obj. as white to avoid other barriers */
		  }
		}


		/*
		** barrier that moves collector backward, that is, mark the black object
		** pointing to a white object as gray again.
		*/
		public static void luaC_barrierback_ (lua_State L, Table t) {
		  global_State g = G(L);
		  lua_assert(isblack(t) && !isdead(g, t));
		  black2gray(t);  /* make table gray (again) */
		  linkgclist(t, ref g.grayagain);
		}


		/*
		** barrier for assignments to closed upvalues. Because upvalues are
		** shared among closures, it is impossible to know the color of all
		** closures pointing to it. So, we assume that the object being assigned
		** must be marked.
		*/
		private static void luaC_upvalbarrier_ (lua_State L, UpVal uv) {
		  global_State g = G(L);
		  GCObject o = gcvalue(uv.v);
		  lua_assert(!upisopen(uv));  /* ensured by macro luaC_upvalbarrier */
		  if (keepinvariant(g))
		    markobject(g, o);
		}


		public static void luaC_fix (lua_State L, GCObject o) {
		  global_State g = G(L);
		  lua_assert(g.allgc == o);  /* object must be 1st in 'allgc' list! */
		  white2gray(o);  /* they will be gray forever */
		  g.allgc = o.next;  /* remove object from 'allgc' list */
		  o.next = g.fixedgc;  /* link it to 'fixedgc' list */
		  g.fixedgc = o;
		}

        //FIXME:refer to luaM_new
        //public static object luaM_realloc_<T>(lua_State L)
		//T new_obj = (T)System.Activator.CreateInstance(typeof(T));
		//AddTotalBytes(L, nsize);
		/*
		** create a new collectable object (with given type and size) and link
		** it to 'allgc' list.
		*/
		public static GCObject luaC_newobj<T> (lua_State L, int tt, uint sz) {
		  global_State g = G(L);
		  //FIXME:???
		  //throw new Exception();
		  GCObject o = (GCObject)(object)luaM_newobject<T>(L/*, novariant(tt), sz*/);
		  if (o is TString) //FIXME:added
		  {
		  	int len_plus_1 = (int)sz - GetUnmanagedSize(typeof(TString));
		  	((TString) o).str = new CharPtr(new char[len_plus_1]);
		  }
		  o.marked = luaC_white(g);
		  o.tt = (byte)tt; //FIXME:(byte)
		  o.next = g.allgc;
		  g.allgc = o;
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
		 reentry:
		  white2gray(o);
		  switch (o.tt) {
			case LUA_TSHRSTR:
    		case LUA_TLNGSTR: {
		      gray2black(o);
		      g.GCmemtrav = (uint)(g.GCmemtrav + sizestring(gco2ts(o))); //g.GCmemtrav += sizestring(gco2ts(o)); //FIXME:
		      break;
			}
			case LUA_TUSERDATA: {
		  	  TValue uvalue = new TValue();
		      markobject(g, gco2u(o).metatable);  /* mark its metatable */
		      gray2black(o);
		      g.GCmemtrav += sizeudata(gco2u(o));
		      getuservalue(g.mainthread, gco2u(o), uvalue);
		      if (valiswhite(uvalue)) {  /* markvalue(g, &uvalue); */
		        o = gcvalue(uvalue);
		        goto reentry;
		      }
      		  break;
			}
			case LUA_TLCL: {
		      linkgclist(gco2lcl(o), ref g.gray);
		      break;
		    }
			case LUA_TCCL: {
			  linkgclist(gco2ccl(o), ref g.gray);
			  break;
			}
			case LUA_TTABLE: {
			  linkgclist(gco2t(o), ref g.gray);
			  break;
			}
			case LUA_TTHREAD: {
			  linkgclist(gco2th(o), ref g.gray);
			  break;
			}
			case LUA_TPROTO: {
			  linkgclist(gco2p(o), ref g.gray);
			  break;
			}
			default: lua_assert(0); break;
		  }
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
		  for (o = g.tobefnz; o != null; o = o.next)
		    markobject(g, o);
		}


		/*
		** Mark all values stored in marked open upvalues from non-marked threads.
		** (Values from marked threads were already marked when traversing the
		** thread.) Remove from the list threads that no longer have upvalues and
		** not-marked threads.
		*/
		private static void remarkupvals (global_State g) {
		  lua_State thread;
		  lua_StateRef p = new TwupsRef(g);
		  while ((thread = p.get()) != null) {
		    lua_assert(!isblack(thread));  /* threads are never black */
		    if (isgray(thread) && thread.openupval != null)
		      p = new TwupsStateRef(thread);  /* keep marked thread with upvalues in the list */
		    else {  /* thread is not marked or without upvalues */
		      UpVal uv;
		      p.set(thread.twups);  /* remove thread from the list */
		      thread.twups = thread;  /* mark that it is out of list */
		      for (uv = thread.openupval; uv != null; uv = uv.u.open.next) {
		        if (uv.u.open.touched!=0) {
		          markvalue(g, uv.v);  /* remark upvalue's value */
		          uv.u.open.touched = 0;
		        }
		      }
		    }
		  }
		}


		/*
		** mark root set and reset all gray lists, to start a new collection

		*/
		private static void restartcollection (global_State g) {
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

		/*
		** Traverse a table with weak values and link it to proper list. During
		** propagate phase, keep it in 'grayagain' list, to be revisited in the
		** atomic phase. In the atomic phase, if table has any white value,
		** put it in 'weak' list, to be cleared.
		*/
		private static void traverseweakvalue (global_State g, Table h) {
		  Node n;//, limit = gnodelast(h); //FIXME:removed, overflow
		  /* if there is array part, assume it may have white values (it is not
     		 worth traversing it now just to check) */
		  int hasclears = (h.sizearray > 0)?1:0;
		  //for (n = gnode(h, 0); n < limit; n++) {  /* traverse hash part */ //FIXME:changed, see below
		  for (int ni = 0; ni < sizenode(h); ni++) {  /* traverse hash part */ //FIXME:changed, gnodelast(h) to sizenode(h)
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
          if (g.gcstate == GCSpropagate)
		    linkgclist(h, ref g.grayagain);  /* must retraverse it in atomic phase */
		  else if (0!=hasclears)
		    linkgclist(h, ref g.weak);  /* has to be cleared later */
		}


		/*
		** Traverse an ephemeron table and link it to proper list. Returns true
		** iff any object was marked during this traversal (which implies that
		** convergence has to continue). During propagation phase, keep table
		** in 'grayagain' list, to be visited again in the atomic phase. In
		** the atomic phase, if table has any white->white entry, it has to
		** be revisited during ephemeron convergence (as that key may turn
		** black). Otherwise, if it has any white key, table has to be cleared
		** (in the atomic phase).
		*/
		private static int traverseephemeron (global_State g, Table h) {
		  int marked = 0;  /* true if an object is marked in this traversal */
		  int hasclears = 0;  /* true if table has white keys */
		  int hasww = 0;  /* true if table has entry "white-key -> white-value" */
		  Node n;// limit = gnodelast(h); //FIXME:removed, overflow
		  uint i;
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
		        hasww = 1;  /* white-white entry */
		    }
		    else if (valiswhite(gval(n))) {  /* value not marked yet? */
		      marked = 1;
		      reallymarkobject(g, gcvalue(gval(n)));  /* mark it now */
		    }
		  }
		  /* link table into proper list */
		  if (g.gcstate == GCSpropagate)
		    linkgclist(h, ref g.grayagain);  /* must retraverse it in atomic phase */
		  else if (0!=hasww)  /* table has white->white entries? */
		    linkgclist(h, ref g.ephemeron);  /* have to propagate again */
		  else if (0!=hasclears)  /* table has white keys? */
		    linkgclist(h, ref g.allweak);  /* may have to clean white keys */
		  return marked;
		}


		private static void traversestrongtable (global_State g, Table h) {
          Node n;// limit = gnodelast(h); //FIXME:removed, overflow
		  uint i;
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
		      black2gray(h);  /* keep table gray */
		      if (weakkey == null)  /* strong keys? */
		        traverseweakvalue(g, h);
		      else if (weakvalue == null)  /* strong values? */
		        traverseephemeron(g, h);
		      else   /* all weak */
		        linkgclist(h, ref g.allweak);  /* nothing to traverse now */
		  }
		  else  /* not weak */
    		traversestrongtable(g, h); 
		  traversestrongtable(g, h);
		  return (uint)(GetUnmanagedSize(typeof(Table)) + GetUnmanagedSize(typeof(TValue)) * h.sizearray +
		                GetUnmanagedSize(typeof(Node)) * (uint)sizenode(h));
		}

		//FIXME:<----------------------------
		private static int traverseproto (global_State g, Proto f) {
		  int i;
		  if (f.cache != null && iswhite(f.cache))
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

		/*
		** open upvalues point to values in a thread, so those values should
		** be marked when the thread is traversed except in the atomic phase
		** (because then the value cannot be changed by the thread and the
		** thread may not be traversed again)
		*/
		private static lu_mem traverseLclosure (global_State g, LClosure cl) {
		  int i;
		  markobject(g, cl.p);  /* mark its prototype */
		  for (i = 0; i < cl.nupvalues; i++) {  /* mark its upvalues */
		    UpVal uv = cl.upvals[i];
		    if (uv != null) {
		      if (upisopen(uv) && g.gcstate != GCSinsideatomic)
		        uv.u.open.touched = 1;  /* can be marked in 'remarkupvals' */
		      else
		        markvalue(g, uv.v);
		    }
		  }
		  return sizeLclosure(cl.nupvalues);
		}

		private static lu_mem traversethread (global_State g, lua_State th) {
		  StkId[] o_ = th.stack; //FIXME:???o_
		  if (o_ == null) //FIXME:???o_
		    return 1;  /* stack not completely built yet */
		  lua_assert(g.gcstate == GCSinsideatomic ||
		             th.openupval == null || isintwups(th));			
		  StkId o = new lua_TValue(o_); //FIXME:o_->o
		  for (; o < th.top; /*StkId.inc(ref o)*/o = o + 1) {  /* mark live elements in the stack */ //FIXME:L.stack->new StkId(L.stack[0]) //FIXME:don't use StackId.inc(), overflow ([-1])
		    markvalue(g, o);
		    
		    //------------------------
		    if (o >= th.top - 1) 
		    {
		    	break;//FIXME:added, o + 1 will overflow
		    }
		    //------------------------
		  }
		  if (g.gcstate == GCSinsideatomic) {  /* final traversal? */
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
		    /* 'remarkupvals' may have removed thread from 'twups' list */ 
		    if (!isintwups(th) && th.openupval != null) {
		      th.twups = g.twups;  /* link it back to the list */
		      g.twups = th;
		    }			
		  }
		  else if (g.gckind != KGC_EMERGENCY)
		    luaD_shrinkstack(th); /* do not change stack in emergency cycle */
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
		  switch (o.tt) {
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
			  linkgclist(th, ref g.grayagain);  /* insert into 'grayagain' list */
			  black2gray(o);
			  size = traversethread(g, th);
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
		
		//FIXME:-------------------->

		private static void convergeephemerons (global_State g) {
		  int changed;
		  do {
		    GCObject w;
		    GCObject next = g.ephemeron;  /* get ephemeron list */
		    g.ephemeron = null;  /* tables may return to this list when traversed */
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
		    uint i;
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


		public static void luaC_upvdeccount (lua_State L, UpVal uv) {
		  lua_assert(uv.refcount > 0);
		  uv.refcount--;
		  if (uv.refcount == 0 && !upisopen(uv))
		    luaM_free(L, uv);
		}


		private static void freeLclosure (lua_State L, LClosure cl) {
		  int i;
		  for (i = 0; i < cl.nupvalues; i++) {
		    UpVal uv = cl.upvals[i];
		    if (uv!=null)
		      luaC_upvdeccount(L, uv);
		  }
		  luaM_freemem(L, cl, sizeLclosure(cl.nupvalues));
		}


		private static void freeobj (lua_State L, GCObject o) {
		  switch (o.tt) {
			case LUA_TPROTO: luaF_freeproto(L, gco2p(o)); break;
			case LUA_TLCL: {
			  freeLclosure(L, gco2lcl(o));
		      break;
		    }
		    case LUA_TCCL: {
			  luaM_freemem(L, o, sizeCclosure(gco2ccl(o).nupvalues));
		      break;
		    }
			case LUA_TTABLE: luaH_free(L, gco2t(o)); break;
			case LUA_TTHREAD: luaE_freethread(L, gco2th(o)); break;
    		case LUA_TUSERDATA: 
				//luaM_freemem(L, o, sizeudata(gco2u(o)));
				luaM_freemem(L, gco2u(o), sizeudata(gco2u(o))); //FIXME:???
				break;
			case LUA_TSHRSTR:
			  luaS_remove(L, gco2ts(o));  /* remove it from hash table */
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
		** sweep at most 'count' elements from a list of GCObjects erasing dead
		** objects, where a dead (not alive) object is one marked with the "old"
		** (non current) white and not fixed; change all non-dead objects back
		** to white, preparing for next collection cycle.
		** When object is a thread, sweep its list of open upvalues too.
		*/
		private static GCObjectRef sweeplist (lua_State L, GCObjectRef p, lu_mem count) {
		  global_State g = G(L);
		  int ow = otherwhite(g);
		  int white = luaC_white(g);  /* current white */
		  while (p.get() != null && count-- > 0) {
		  	GCObject curr = p.get();
		    int marked = curr.marked;
		    if (isdeadm(ow, marked)) {  /* is 'curr' dead? */
		      p.set(curr.next);  /* remove 'curr' from list */
		      freeobj(L, curr);  /* erase 'curr' */
		    }
		    else {  /* update marks */
		      curr.marked = cast_byte((marked & maskcolors) | white);
		      p = new NextRef(curr);  /* go to next element */
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

		/*
		** If possible, free concatenation buffer and shrink string table
		*/
		private static void checkSizes (lua_State L, global_State g) {
		  if (g.gckind != KGC_EMERGENCY) {
		    l_mem olddebt = g.GCdebt;
			luaZ_freebuffer(L, g.buff);  /* free concatenation buffer */
		    if (g.strt.nuse < g.strt.size / 4)  /* string table too big? */
		      luaS_resize(L, g.strt.size / 2);  /* shrink it a little */
		    g.GCestimate = (uint)(g.GCestimate + (g.GCdebt - olddebt)); //FIXME: g.GCestimate += g.GCdebt - olddebt;  /* update estimate */
		  }
		}


	    private static GCObject udata2finalize (global_State g) {
		  GCObject o = g.tobefnz;  /* get first element */
		  lua_assert(tofinalize(o));
		  g.tobefnz = o.next;  /* remove it from 'tobefnz' list */
		  o.next = g.allgc;  /* return it to 'allgc' list */
		  g.allgc = o;
		  resetbit(ref o.marked, FINALIZEDBIT);  /* object is "normal" again */
		  if (issweepphase(g))
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
		** call a few (up to 'g->gcfinnum') finalizers
		*/
		private static int runafewfinalizers (lua_State L) {
		  global_State g = G(L);
		  uint i;
		  lua_assert(null==g.tobefnz || g.gcfinnum > 0);
		  for (i = 0; g.tobefnz!=null && i < g.gcfinnum; i++)
		    GCTM(L, 1);  /* call one finalizer */
		  g.gcfinnum = (null==g.tobefnz) ? 0  /* nothing more to finalize? */
		                    : g.gcfinnum * 2;  /* else call a few more next time */
		  return (int)i;
		}

		/*
		** call all pending finalizers
		*/
		private static void callallpendingfinalizers (lua_State L, int propagateerrors) {
		  global_State g = G(L);
		  while (g.tobefnz!=null)
		    GCTM(L, propagateerrors);
		}


		/*
		** find last 'next' field in list 'p' list (to add elements in its end)
		*/
		private static GCObjectRef findlast (GCObjectRef p) {
		  while (p.get() != null)
		  	p = new NextRef(p.get());
		  return p;
		}


		/*
		** move all unreachable objects (or 'all' objects) that need
		** finalization from list 'finobj' to list 'tobefnz' (to be finalized)
		*/
		private static void separatetobefnz (global_State g, int all) {
		  GCObject curr;
		  GCObjectRef p = new FinobjRef(g);
		  GCObjectRef lastnext = findlast(new TobefnzRef(g));
		  while ((curr = p.get()) != null) {  /* traverse all finalizable objects */
		    lua_assert(tofinalize(curr));
		    if (!(iswhite(curr) || all!=0))  /* not being collected? */
		      p = new NextRef(curr);  /* don't bother with it */
		    else {
		      p.set(curr.next);  /* remove 'curr' from 'finobj' list */
		      curr.next = lastnext.get();  /* link at the end of 'tobefnz' list */
		      lastnext.set(curr);
		      lastnext = new NextRef(curr);
		    }
		  }
		}


		/*
		** if object 'o' has a finalizer, remove it from 'allgc' list (must
		** search the list to find it) and link it in 'finobj' list.
		*/
		public static void luaC_checkfinalizer (lua_State L, GCObject o, Table mt) {
		  global_State g = G(L);
		  if (tofinalize(o) ||                 /* obj. is already marked... */
      	      gfasttm(g, mt, TMS.TM_GC) == null)   /* or has no finalizer? */
		    return;  /* nothing to be done */
		  else {  /* move 'o' to 'finobj' list */
		    GCObjectRef p;
		    int null_ = 0;
			if (issweepphase(g)) {
      		  makewhite(g, o);  /* "sweep" object 'o' */
		      if (g.sweepgc == o.next)  /* shoud not remove 'sweepgc' object */
		        g.sweepgc = sweeptolive(L, g.sweepgc, ref null_);  /* change 'sweepgc' */
		    }
		    /* search for pointer pointing to 'o' */			
		    for (p = new AllGCRef(g); p.get() != o; p = new NextRef(p.get())) { /* empty */ }
		    p.set(o.next);  /* remove 'o' from 'allgc' list */
		    o.next = g.finobj;  /* link it in 'finobj' list */
		    g.finobj = o;
		    l_setbit(ref o.marked, FINALIZEDBIT);  /* mark it as such */
		  }
		}

		/* }====================================================== */



		/*
		** {======================================================
		** GC control
		** =======================================================
		*/

		/*
		** set a reasonable "time" to wait before starting a new GC cycle;
		** cycle will start when memory use hits threshold
		*/
		static void setpause (global_State g) {
		  l_mem threshold, debt;
		  l_mem estimate = (int)(g.GCestimate / PAUSEADJ);  /* adjust 'estimate' */
		  threshold = (g.gcpause < MAX_LMEM / estimate)  /* overflow? */
		            ? estimate * g.gcpause  /* no overflow */
		            : MAX_LMEM;  /* overflow; truncate to maximum */
		  debt = (int)(gettotalbytes(g) - threshold);
		  luaE_setdebt(g, debt);
		}


		/*
		** Enter first sweep phase.
		** The call to 'sweeptolive' makes pointer point to an object inside
		** the list (instead of to the header), so that the real sweep do not
		** need to skip objects created between "now" and the start of the real
		** sweep.
		** Returns how many objects it swept.
		*/
		private static int entersweep (lua_State L) {
		  global_State g = G(L);
		  int n = 0;
		  g.gcstate = GCSswpallgc;
		  lua_assert(g.sweepgc == null);
		  g.sweepgc = sweeptolive(L, new AllGCRef(g), ref n);
		  return n;
		}


		public static void luaC_freeallobjects (lua_State L) {
		  global_State g = G(L);
		  separatetobefnz(g, 1);  /* separate all objects with finalizers */
		  lua_assert(g.finobj == null);
		  callallpendingfinalizers(L, 0);
		  lua_assert(g.tobefnz == null);
		  g.currentwhite = (byte)WHITEBITS; /* this "white" makes all objects look dead */ //FIXME:added, (byte)
		  g.gckind = KGC_NORMAL;
		  sweepwholelist(L, new FinobjRef(g)); //FIXME:changed
		  sweepwholelist(L, new AllGCRef(g)); //FIXME:changed
		  sweepwholelist(L, new FixedgcRef(g));  /* collect fixed objects */
		  lua_assert(g.strt.nuse == 0);
		}


		private static l_mem atomic (lua_State L) {
		  global_State g = G(L);
		  l_mem work;
          GCObject origweak, origall;
		  GCObject grayagain = g.grayagain;  /* save original list */
  		  lua_assert(g.ephemeron == null && g.weak == null);
		  lua_assert(!iswhite(g.mainthread));
		  g.gcstate = GCSinsideatomic;
		  g.GCmemtrav = 0;  /* start counting work */
		  markobject(g, L);  /* mark running thread */
		  /* registry and global metatables may be changed by API */
		  markvalue(g, g.l_registry);
		  markmt(g);  /* mark global metatables */
		  /* remark occasional upvalues of (maybe) dead threads */
		  remarkupvals(g);
		  propagateall(g);  /* propagate changes */
		  work = (int)g.GCmemtrav;  /* stop counting (do not recount gray-agains) */
		  g.gray = grayagain;
  		  propagateall(g);  /* traverse 'grayagain' list */
		  g.GCmemtrav = 0;  /* restart counting */
          convergeephemerons(g);
          /* at this point, all strongly accessible objects are marked. */
		  /* Clear values from weak tables, before checking finalizers */
		  clearvalues(g, g.weak, null);
		  clearvalues(g, g.allweak, null);
		  origweak = g.weak; origall = g.allweak;
		  work += (int)g.GCmemtrav;  /* stop counting (objects being finalized) */
		  separatetobefnz(g, 0);  /* separate objects to be finalized */
		  g.gcfinnum = 1;  /* there may be objects to be finalized */
		  markbeingfnz(g);  /* mark objects that will be finalized */
		  propagateall(g);  /* remark, to propagate 'resurrection' */
		  g.GCmemtrav = 0;  /* restart counting */
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

		
		private static lu_mem sweepstep (lua_State L, global_State g,
		                         int nextstate, GCObjectRef nextlist) {
		  if (g.sweepgc != null) {
		    l_mem olddebt = g.GCdebt;
		    g.sweepgc = sweeplist(L, g.sweepgc, GCSWEEPMAX);
		    g.GCestimate = (uint)(g.GCestimate + g.GCdebt - olddebt);//g.GCestimate += g.GCdebt - olddebt;  /* update estimate */
		    if (g.sweepgc != null)  /* is there still something to sweep? */
		      return (GCSWEEPMAX * GCSWEEPCOST);
		  }
		  /* else enter next state */
		  g.gcstate = (byte)nextstate;
		  g.sweepgc = nextlist;
		  return 0;
		}


		private static lu_mem singlestep (lua_State L) {
		  global_State g = G(L);
		  switch (g.gcstate) {
			case GCSpause: {
		      g.GCmemtrav = (uint)(g.strt.size * 4); //FIXME:??? sizeof(GCObject*);
		      restartcollection(g);
		      g.gcstate = GCSpropagate;
		      return g.GCmemtrav;
			}
			case GCSpropagate: {
		  	  g.GCmemtrav = 0;
		      lua_assert(g.gray!=null);
		      propagatemark(g);
		       if (g.gray == null)  /* no more `gray' objects? */
		        g.gcstate = GCSatomic;  /* finish propagate phase */
		      return g.GCmemtrav;  /* memory traversed in this step */
			}
			case GCSatomic: {
		      lu_mem work;
		      int sw;
		      propagateall(g);  /* make sure gray list is empty */
		      work = (uint)atomic(L);  /* work is what was traversed by 'atomic' */
		      sw = entersweep(L);
			  g.GCestimate = gettotalbytes(g);  /* first estimate */;
		      return (uint)(work + sw * GCSWEEPCOST);
		    }
		    case GCSswpallgc: {  /* sweep "regular" objects */
		  	  return sweepstep(L, g, GCSswpfinobj, new FinobjRef(g));
		    }
		    case GCSswpfinobj: {  /* sweep objects with finalizers */
		  	  return sweepstep(L, g, GCSswptobefnz, new TobefnzRef(g));
		    }
		    case GCSswptobefnz: {  /* sweep objects to be finalized */
		      return sweepstep(L, g, GCSswpend, null);
		    }
		    case GCSswpend: {  /* finish sweeps */
		      makewhite(g, g.mainthread);  /* sweep main thread */
		      checkSizes(L, g);
		      g.gcstate = GCScallfin;
		      return 0;
		    }
		    case GCScallfin: {  /* call remaining finalizers */
		      if (g.tobefnz!=null && g.gckind != KGC_EMERGENCY) {
		        int n = runafewfinalizers(L);
		        return (uint)(n * GCFINALIZECOST);
		      }
		      else {  /* emergency mode or no more finalizers */
		        g.gcstate = GCSpause;  /* finish collection */
		        return 0;
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


		/*
		** get GC debt and convert it from Kb to 'work units' (avoid zero debt
		** and overflows)
		*/
		private static l_mem getdebt (global_State g) {
		  l_mem debt = g.GCdebt;
		  int stepmul = g.gcstepmul;
		  debt = (debt / STEPMULADJ) + 1;
		  debt = (debt < MAX_LMEM / stepmul) ? debt * stepmul : MAX_LMEM;
		  return debt;
		}

		/*
		** performs a basic GC step when collector is running
		*/
		public static void luaC_step (lua_State L) {
		  global_State g = G(L);
		  l_mem debt = getdebt(g);  /* GC deficit (be paid now) */
		  if (0==g.gcrunning) {  /* not running? */
		    luaE_setdebt(g, -GCSTEPSIZE * 10);  /* avoid being called too often */
		    return;
		  }
		  do {  /* repeat until pause or enough "credit" (negative debt) */
		    lu_mem work = singlestep(L);  /* perform one single step */
		    debt -= (int)work;
		  } while (debt > -GCSTEPSIZE && g.gcstate != GCSpause);
		  if (g.gcstate == GCSpause)
		  	setpause(g);  /* pause until next cycle */
		  else {
		    debt = (debt / g.gcstepmul) * STEPMULADJ;  /* convert 'work units' to Kb */
		    luaE_setdebt(g, debt);
		    runafewfinalizers(L);
		  }
		}


		/*
		** Performs a full GC cycle; if "isemergency", set a flag to avoid
		** some operations which could change the interpreter state in some
		** unexpected ways (running finalizers and shrinking some structures).
		** Before running the collection, check 'keepinvariant'; if it is true,
		** there may be some objects marked as black, so the collector has
		** to sweep all objects to turn them back to white (as white has not
		** changed, nothing will be collected).
		*/
		public static void luaC_fullgc (lua_State L, int isemergency) {
		  global_State g = G(L);
		  lua_assert(g.gckind == KGC_NORMAL);
		  if (isemergency!=0) g.gckind = KGC_EMERGENCY;  /* set flag */
		  if (keepinvariant(g)) {  /* black objects? */
		    entersweep(L); /* sweep everything to turn them back to white */
		  }
		  /* finish any pending sweep phase to start a new cycle */
		  luaC_runtilstate(L, bitmask(GCSpause));
		  luaC_runtilstate(L, ~bitmask(GCSpause));  /* start new collection */
		  luaC_runtilstate(L, bitmask(GCScallfin));  /* run up to finalizers */
		  /* estimate must be correct after a full GC cycle */
		  lua_assert(g.GCestimate == gettotalbytes(g));
		  luaC_runtilstate(L, bitmask(GCSpause));  /* finish collection */
		  g.gckind = KGC_NORMAL;
		  setpause(g);
		}

        /* }====================================================== */


	}
}

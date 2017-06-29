/*
** $Id: lgc.c,v 2.66 2009/12/16 16:42:58 roberto Exp roberto $
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

		public const uint GCSTEPSIZE	= 1024;
		public const int GCSWEEPMAX		= 40;
		public const int GCSWEEPCOST	= 10;
		public const int GCFINALIZECOST	= 100;


		public static byte maskcolors	= (byte)(~(bitmask(BLACKBIT)|WHITEBITS));

		public static void makewhite(global_State g, GCObject x)
		{
		   gch(x).marked = (byte)(gch(x).marked & maskcolors | luaC_white(g));
		}

		public static void white2gray(GCObject x) { resetbits(ref gch(x).marked, WHITEBITS); }
		public static void black2gray(GCObject x) { resetbit(ref gch(x).marked, BLACKBIT); }

		public static void stringmark(TString s) {resetbits(ref s.tsv.marked, WHITEBITS);}

		public static bool isfinalized(Udata_uv u) { return testbit(u.marked, FINALIZEDBIT); }
        public static bool isfinalized(GCheader u) { return testbit(u.marked, FINALIZEDBIT); } //FIXME:added

		public static void checkdeadkey(n) { lua_assert(!ttisdeadkey(gkey(n)) || ttisnil(gval(n)));} 

		public static void markvalue(global_State g, TValue o) 
		{
			checkconsistency(o);
			if (valiswhite(o))
				reallymarkobject(g,gcvalue(o));
		}

		public static void markobject(global_State g, object t)
		{
			if (t != null && iswhite(obj2gco(t)))
				reallymarkobject(g, obj2gco(t));
		}

		//static void reallymarkobject (global_State *g, GCObject *o);


		/*
		** {======================================================
		** Generic functions
		** =======================================================
		*/

		private static void linktable (Table h, ref GCObject p) {
		  h.gclist = p;
		  p = obj2gco(h);
		}
		
		
		private static void removeentry (Node n) {
		  lua_assert(ttisnil(gval(n)));
		  if (iscollectable(gkey(n)))
			setdeadvalue(gkey(n));  /* dead key; remove it */
		}


		/*
		** The next function tells whether a key or value can be cleared from
		** a weak table. Non-collectable objects are never removed from weak
		** tables. Strings behave as `values', so are never removed too. for
		** other objects: if really collected, cannot keep them; for objects
		** being finalized, keep them in keys, but not in values
		*/
		private static int iscleared (TValue o, int iskey) {
		  if (!iscollectable(o)) return 0;
		  if (ttisstring(o)) {
		    stringmark(rawtsvalue(o));  /* strings are `values', so are never weak */
		    return 0;
		  }
		  return iswhite(gcvalue(o)) ||
		  	(ttisuserdata(o) && (iskey == 0 && isfinalized(uvalue(o)))) ? 1 : 0;
		}


		public static void luaC_barrierf (lua_State L, GCObject o, GCObject v) {
		  global_State g = G(L);
		  lua_assert(isblack(o) && iswhite(v) && !isdead(g, v) && !isdead(g, o));
		  lua_assert(g.gcstate != GCSfinalize && g.gcstate != GCSpause);
		  lua_assert(gch(o).tt != LUA_TTABLE);
		  /* must keep invariant? */
		  if (g.gcstate == GCSpropagate)
			reallymarkobject(g, v);  /* restore invariant */
		  else  /* don't mind */
			makewhite(g, o);  /* mark as white just to avoid other barriers */
		}


		public static void luaC_barrierback(lua_State L, Table t)
		{
		  global_State g = G(L);
		  GCObject o = obj2gco(t);
		  lua_assert(isblack(o) && !isdead(g, o));
		  lua_assert(g.gcstate != GCSfinalize && g.gcstate != GCSpause);
		  black2gray(o);  /* make table gray (again) */
		  t.gclist = g.grayagain;
		  g.grayagain = o;
		}


		/*
		** create a new collectable object and link it to '*list'
		*/
		GCObject luaC_newobj (lua_State L, int tt, size_t sz, GCObject[] list,
		                       int offset) {
		  global_State g = G(L);
		  GCObject o = obj2gco(cast(char *, luaM_newobject(L, tt, sz)) + offset);
		  if (list == null)
		    list = &g.rootgc;  /* standard list for collectable objects */
		  gch(o).marked = luaC_white(g);
		  gch(o).tt = tt;
		  gch(o).next = *list;
		  list[0] = o;
		  return o;
		}


		public static void luaC_linkupval (lua_State L, UpVal uv) {
		  global_State g = G(L);
		  GCObject o = obj2gco(uv);
		  gch(o).next = g.rootgc;  /* link upvalue into `rootgc' list */
		  g.rootgc = o;
		  if (isgray(o)) {
			if (g.gcstate == GCSpropagate) {
			  gray2black(o);  /* closed upvalues need barrier */
			  luaC_barrier(L, uv, uv.v);
			}
			else {  /* sweep phase: sweep it (turning it into white) */
			  makewhite(g, o);
			  lua_assert(g.gcstate != GCSfinalize && g.gcstate != GCSpause);
			}
		  }
		}

		/* }====================================================== */



		/*
		** {======================================================
		** Mark functions
		** =======================================================
		*/

		private static void reallymarkobject (global_State g, GCObject o) {
		  lua_assert(iswhite(o) && !isdead(g, o));
		  white2gray(o);
		  switch (gch(o).tt) {
			case LUA_TSTRING: {
			  return;
			}
			case LUA_TUSERDATA: {
			  Table mt = gco2u(o).metatable;
			  gray2black(o);  /* udata are never gray */
			  markobject(g, mt);
			  markobject(g, gco2u(o).env);
			  return;
			}
			case LUA_TUPVAL: {
			  UpVal uv = gco2uv(o);
			  markvalue(g, uv.v);
			  if (uv.v == uv.u.value)  /* closed? */
				gray2black(o);  /* open upvalues are never black */
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
			default: lua_assert(0); break;
		  }
		}


		private static void markmt (global_State g) {
		  int i;
		  for (i=0; i<NUM_TAGS; i++)
		     markobject(g, g.mt[i]);
		}


		private static void markbeingfnz (global_State g) {
		  GCObject o;
		  for (o = g.tobefnz; o != null; o = gch(o).next) {
		    lua_assert(testbit(gch(o).marked, SEPARATED));
		    makewhite(g, o);
		    reallymarkobject(g, o);
		  }
		}


		/* mark root set */
		private static void markroot (lua_State L) {
		  global_State g = G(L);
		  g.gray = null;
		  g.grayagain = null;
		  g.weak = g.ephemeron = g.allweak = null;
		  markobject(g, g.mainthread);
		  /* make global table and registry to be traversed before main stack */
		  markobject(g, g.l_gt);
		  markvalue(g, &g.l_registry);
		  markmt(g);
		  markbeingfnz(g);  /* mark any finalizing object left from previous cycle */
		  g.gcstate = GCSpropagate;
		}


		private static void remarkupvals (global_State g) {
		  UpVal uv;
		  for (uv = g.uvhead.u.l.next; uv != g.uvhead; uv = uv.u.l.next) {
			lua_assert(uv.u.l.next.u.l.prev == uv && uv.u.l.prev.u.l.next == uv);
			if (isgray(obj2gco(uv)))
			  markvalue(g, uv.v);
		  }
		}		
		
		/* }====================================================== */


		/*
		** {======================================================
		** Traverse functions
		** =======================================================
		*/

		private static void traverseweakvalue (global_State g, Table h) {
		  int i = sizenode(h);
		  while (i-- != 0) {
		    Node n = gnode(h, i);
		    checkdeadkey(n);
		    if (ttisnil(gval(n)))
		      removeentry(n);  /* remove empty entries */
		    else {
		      lua_assert(!ttisnil(gkey(n)));
		      markvalue(g, gkey(n));
		    }
		  }
		  linktable(h, ref g.weak);
		}


		private static int traverseephemeron (global_State g, Table h) {
		  int marked = 0;
		  int hasclears = 0;
		  int i = h.sizearray;
		  while (i-- != 0) {  /* mark array part (numeric keys are 'strong') */
		    if (valiswhite(h.array[i])) {
		      marked = 1;
		      reallymarkobject(g, gcvalue(h.array[i]));
		    }
		  }
		  i = sizenode(h);
		  while (i-- != 0) {
		    Node n = gnode(h, i);
		    checkdeadkey(n);
		    if (ttisnil(gval(n)))  /* entry is empty? */
		      removeentry(n);  /* remove it */
		    else if (valiswhite(gval(n))) {
		      /* value is not marked yet */
		      if (iscleared(key2tval(n), 1) != 0)  /* key is not marked (yet)? */
		        hasclears = 1;  /* may have to propagate mark from key to value */
		      else {  /* mark value only if key is marked */
		        marked = 1;  /* some mark changed status */
		        reallymarkobject(g, gcvalue(gval(n)));
		      }
		    }
		  }
		  if (hasclears != 0)
		    linktable(h, ref g.ephemeron);
		  else  /* nothing to propagate */
		    linktable(h, ref g.weak);  /* avoid convergence phase  */
		  return marked;
		}


		private static void traversestrongtable (global_State g, Table h) {
		  int i;
		  i = h.sizearray;
		  while (i-- != 0)
		    markvalue(g, h.array[i]);
		  i = sizenode(h);
		  while (i-- != 0) {
		    Node n = gnode(h, i);
		    checkdeadkey(n);
		    if (ttisnil(gval(n)))
		      removeentry(n);  /* remove empty entries */
		    else {
		      lua_assert(!ttisnil(gkey(n)));
		      markvalue(g, gkey(n));
		      markvalue(g, gval(n));
		    }
		  }
		}


		private static void traversetable (global_State g, Table h) {
		  TValue mode = gfasttm(g, h.metatable, TMS.TM_MODE);
		  markobject(g, h.metatable);
		  //FIXME:??? modify mode!=0 to mode!=null, avoid lua_TValue.operator int() exception
		  if (mode != null && ttisstring(mode)) {  /* is there a weak mode? */ 
		  	int weakkey = (strchr(svalue(mode), 'k') != null) ? 1 : 0;
		    int weakvalue = (strchr(svalue(mode), 'v') != null) ? 1 : 0;
		    if (weakkey != 0 || weakvalue != 0) {  /* is really weak? */
		      black2gray(obj2gco(h));  /* keep table gray */
		      if (weakkey == 0)  /* strong keys? */
		        traverseweakvalue(g, h);
		      else if (weakvalue == 0)  /* strong values? */
		        traverseephemeron(g, h);
		      else
		        linktable(h, ref g.allweak);  /* nothing to traverse now */
		      return;
		    }  /* else go through */
		  }
		  traversestrongtable(g, h);
		}



		//FIXME:<----------------------------

		/*
		** All marks are conditional because a GC may happen while the
		** prototype is still being created
		*/
		private static void traverseproto (global_State g, Proto f) {
		  int i;
		  if (f.source != null) stringmark(f.source);
		  for (i=0; i<f.sizek; i++)  /* mark literals */
			markvalue(g, f.k[i]);
		  for (i=0; i<f.sizeupvalues; i++) {  /* mark upvalue names */
			if (f.upvalues[i].name != null)
			  stringmark(f.upvalues[i].name);
		  }
		  for (i=0; i<f.sizep; i++)  /* mark nested protos */
			  markobject(g, f.p[i]);
		  for (i=0; i<f.sizelocvars; i++) {  /* mark local-variable names */
			if (f.locvars[i].varname != null)
			  stringmark(f.locvars[i].varname);
		  }
		}


		private static void traverseclosure (global_State g, Closure cl) {
		  markobject(g, cl.c.env);
		  if (cl.c.isC != 0) {
			int i;
			for (i=0; i<cl.c.nupvalues; i++)  /* mark its upvalues */
			  markvalue(g, cl.c.upvalue[i]);
		  }
		  else {
			int i;
			lua_assert(cl.l.nupvalues == cl.l.p.sizeupvalues);
			markobject(g, cl.l.p);
			for (i=0; i<cl.l.nupvalues; i++)  /* mark its upvalues */
			  markobject(g, cl.l.upvals[i]);
		  }
		}


		private static void traversestack (global_State g, lua_State L) {
		  StkId o;
		  if (L.stack == null)
		    return;  /* stack not completely built yet */
		  for (o = new lua_TValue(L.stack); o < L.top; lua_TValue.inc(ref o)) //FIXME:L.stack->new StkId(L.stack[0])
		    markvalue(g, o);
		  if (g.gcstate == GCSatomic) {  /* final traversal? */
            StkId lim = L.stack + L.stacksize;  /* real end of stack */
		  	for (; o <= lim; StkId.inc(ref o))  /* clear not-marked stack slice */
		      setnilvalue(o);
		  }
		}


		/*
		** traverse one gray object, turning it to black.
		** Returns `quantity' traversed.
		*/
		private static l_mem propagatemark (global_State g) {
		  GCObject o = g.gray;
		  lua_assert(isgray(o));
		  gray2black(o);
		  switch (gch(o).tt) {
			case LUA_TTABLE: {
			  Table h = gco2t(o);
			  g.gray = h.gclist;
			  traversetable(g, h);
			  return	GetUnmanagedSize(typeof(Table)) + GetUnmanagedSize(typeof(TValue)) * h.sizearray +
						GetUnmanagedSize(typeof(Node)) * sizenode(h);
			}
			case LUA_TFUNCTION: {
			  Closure cl = gco2cl(o);
			  g.gray = cl.c.gclist;
			  traverseclosure(g, cl);
			  return (cl.c.isC != 0) ? sizeCclosure(cl.c.nupvalues) :
								   sizeLclosure(cl.l.nupvalues);
			}
			case LUA_TTHREAD: {
			  lua_State th = gco2th(o);
			  g.gray = th.gclist;
			  th.gclist = g.grayagain;
			  g.grayagain = o;
			  black2gray(o);
			  traversestack(g, th);
			  return	GetUnmanagedSize(typeof(lua_State)) + GetUnmanagedSize(typeof(TValue)) * th.stacksize;
			}
			case LUA_TPROTO: {
			  Proto p = gco2p(o);
			  g.gray = p.gclist;
			  traverseproto(g, p);
			  return	GetUnmanagedSize(typeof(Proto)) + GetUnmanagedSize(typeof(Instruction)) * p.sizecode +
						GetUnmanagedSize(typeof(Proto)) * p.sizep +
						GetUnmanagedSize(typeof(TValue)) * p.sizek + 
						GetUnmanagedSize(typeof(int)) * p.sizelineinfo +
						GetUnmanagedSize(typeof(LocVar)) * p.sizelocvars +
						GetUnmanagedSize(typeof(TString)) * p.sizeupvalues;
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
		    GCObject next = g.ephemeron;
		    g.ephemeron = null;
		    changed = 0;
		    while ((w = next) != null) {
		      next = gco2t(w).gclist;
		      if (traverseephemeron(g, gco2t(w)) != 0) {
		        changed = 1;
		        propagateall(g);
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

		/* clear collected entries from weaktables */
		private static void cleartable (GCObject l) {
		  while (l != null) {
			Table h = gco2t(l);
			int i = h.sizearray;
		    while (i--!= 0) {
			  TValue o = h.array[i];
			  if (iscleared(o, 0) != 0)  /* value was collected? */
			    setnilvalue(o);  /* remove value */
		    }
			i = sizenode(h);
			while (i-- != 0) {
			  Node n = gnode(h, i);
			  if (!ttisnil(gval(n)) &&  /* non-empty entry? */
				  (iscleared(key2tval(n), 1) != 0 || iscleared(gval(n), 0) != 0)) {
				setnilvalue(gval(n));  /* remove value ... */
				removeentry(n);  /* remove entry from Table */
			  }
			}
			l = h.gclist;
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


		private static void sweepthread (lua_State L, lua_State L1, int alive) {
		  if (L1.stack == null) return;  /* stack not completely built yet */
		  sweepwholelist(L, new PtrRef(L1.openupval));  /* sweep open upvalues */ //FIXME:???
		  luaE_freeCI(L1);  /* free extra CallInfo slots */
		  /* should not change the stack during an emergency gc cycle */
		  if (alive != 0 && G(L).gckind != KGC_EMERGENCY)
		      luaD_shrinkstack(L1);
		}


		private static GCObjectRef sweeplist (lua_State L, GCObjectRef p, lu_mem count) {
		  GCObject curr;
		  global_State g = G(L);
		  int deadmask = otherwhite(g);
		  while ((curr = p.get()) != null && count-- > 0) {
            int alive = (gch(curr).marked ^ WHITEBITS) & deadmask;
		  	if (gch(curr).tt == LUA_TTHREAD)
			  sweepthread(L, gco2th(curr), alive);
			if (alive != 0) {
			  lua_assert(isdead(g, curr) || testbit(gch(curr).marked, FIXEDBIT));
			  makewhite(g, curr);  /* make it white (for next cycle) */
			  p = new NextRef(gch(curr));
			}
			else {  /* must erase `curr' */
			  lua_assert(isdead(g, curr) || deadmask == bitmask(SFIXEDBIT));
			  p.set( gch(curr).next );  /* remove 'curr' from list */
			  freeobj(L, curr);
			}
		  }
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
		  if (g.strt.nuse < (lu_int32)(g.strt.size)) {
		    /* string-table size could be the smaller power of 2 larger than 'nuse' */
		    int size = 1 << luaO_ceillog2((uint)(g.strt.nuse)); //FIXME:???
		    if (size < g.strt.size)  /* current table too large? */
		      luaS_resize(L, size);  /* shrink it */
		  }
		  luaZ_freebuffer(L, g.buff);
		}


	    private static Udata udata2finalize (global_State g) {
		  GCObject o = g.tobefnz;  /* get first element */
		  g.tobefnz = gch(o).next;  /* remove it from 'tobefnz' list */
		  gch(o).next = g.rootgc;  /* return it to `root' list */
		  g.rootgc = o;
		  lua_assert(isfinalized(gch(o)));
		  resetbit(ref gch(o).marked, SEPARATED);  /* mark it as such */
		  makewhite(g, o);
		  return rawgco2u(o);
		}


		private static void dothecall (lua_State L, object ud) {
		  //UNUSED(ud);
		  luaD_call(L, L.top - 2, 0, 0);
		}


		private static void GCTM (lua_State L, int propagateerrors) {
		  global_State g = G(L);
		  Udata udata = udata2finalize(g);
		  /*const*/ TValue tm = gfasttm(g, udata.uv.metatable, TMS.TM_GC);
		  if (tm != null && ttisfunction(tm)) {
            int status;
			lu_byte oldah = L.allowhook;
			lu_mem oldt = (lu_mem)g.GCthreshold;
			L.allowhook = 0;  /* stop debug hooks during GC tag method */
			g.GCthreshold = 2*g.totalbytes;  /* avoid GC steps */
			setobj2s(L, L.top, tm);
			setuvalue(L, L.top+1, udata);
			L.top += 2;
		    status = luaD_pcall(L, dothecall, null, savestack(L, L.top - 2), 0);
		    L.allowhook = oldah;  /* restore hooks */
		    g.GCthreshold = oldt;  /* restore threshold */
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


		/* move 'dead' udata that need finalization to list 'tobefnz' */
		public static void luaC_separateudata (lua_State L, int all) {
		  global_State g = G(L);
		  GCObjectRef p = new NextRef(g.mainthread);
		  GCObject curr;
		  GCObjectRef lastnext = new PtrRef(g.tobefnz); //FIXME:??????
		  /* find last 'next' field in 'tobefnz' list (to insert elements in its end) */
		  while (lastnext.get() != null) lastnext = new PtrRef(gch(lastnext.get()).next);
		  while ((curr = p.get()) != null) {  /* traverse all finalizable objects */
		    lua_assert(gch(curr).tt == LUA_TUSERDATA && !isfinalized(gco2u(curr)));
		    lua_assert(testbit(gch(curr).marked, SEPARATED));
		    if (!(all != 0 || iswhite(curr)))  /* not being collected? */
		    	p = new NextRef(gch(curr).next);  /* don't bother with it */
		    else {
		      l_setbit(ref gch(curr).marked, FINALIZEDBIT); /* won't be finalized again */
		      p.set(gch(curr).next);  /* remove 'curr' from 'rootgc' list */
		      /* link 'curr' at the end of 'tobefnz' list */
		      gch(curr).next = lastnext.get();
		      lastnext.set(curr);
		      lastnext = new PtrRef(gch(curr).next);
		    }
		  }
		}


		public static void luaC_checkfinalizer (lua_State L, Udata u) {
		  global_State g = G(L);
		  if (testbit(u.uv.marked, SEPARATED) || /* userdata is already separated... */
		      isfinalized(u.uv) ||                        /* ... or is finalized... */
		      gfasttm(g, u.uv.metatable, TMS.TM_GC) == null)  /* or has no finalization? */
		    return;  /* nothing to be done */
		  else {  /* move 'u' to 2nd part of root list */
		    GCObjectRef p;
		    for (p = new RootGCRef(g); p.get() != obj2gco(u); p.set(gch(p.get()).next))
		    	lua_assert(p.get() != obj2gco(g.mainthread));  /* 'u' must be in this list */
		    p.set(u.uv.next);  /* remove 'u' from root list */
		    u.uv.next = g.mainthread.next;  /* re-link it in list */
		    g.mainthread.next = obj2gco(u);
		    l_setbit(ref u.uv.marked, SEPARATED);  /* mark it as such */
		  }
		}

		/* }====================================================== */


		/*
		** {======================================================
		** GC control
		** =======================================================
		*/

		public static void luaC_freeallobjects (lua_State L) {
		  global_State g = G(L);
		  int i;
		  while (g.tobefnz) GCTM(L, 0);  /* Call all pending finalizers */
		  /* following "white" makes all objects look dead */
		  g.currentwhite = (byte)((WHITEBITS | bitmask(SFIXEDBIT)) & 0xff);
		  sweepwholelist(L, new RootGCRef(g));
		  lua_assert(g.rootgc == obj2gco(g.mainthread) &&
		             g.mainthread.next == null);
		  for (i = 0; i < g.strt.size; i++)  /* free all string lists */
		  	sweepwholelist(L, new ArrayRef(g.strt.hash, i));
		  lua_assert(g.strt.nuse == 0);
		}


		private static void atomic (lua_State L) {
		  global_State g = G(L);
          g.gcstate = GCSatomic;
		  lua_assert(!iswhite(obj2gco(g.mainthread)));
		  markobject(g, L);  /* mark running thread */
		  /* registry and global metatables may be changed by API */
		  markvalue(g, &g.l_registry);
		  markmt(g);  /* mark basic metatables */
		  /* remark occasional upvalues of (maybe) dead threads */
		  remarkupvals(g);
		  /* traverse objects caught by write barrier and by 'remarkupvals' */
		  propagateall(g);
		  /* at this point, all strongly accessible objects are marked.
		     Start marking weakly accessible objects. */
		  traverselistofgrays(g, &g.weak);  /* remark weak tables */
		  traverselistofgrays(g, ref g.ephemeron);  /* remark ephemeron tables */
  		  traverselistofgrays(g, ref g.grayagain);  /* remark gray again */
          convergeephemerons(g);
		  luaC_separateudata(L, 0);  /* separate userdata to be finalized */
		  markbeingfnz(g);  /* mark userdata that will be finalized */
		  propagateall(g);  /* remark, to propagate `preserveness' */
		  convergeephemerons(g);
		  /* remove collected objects from weak tables */
		  cleartable(g.weak);
		  cleartable(g.ephemeron);
		  cleartable(g.allweak);
		  g.currentwhite = cast_byte(otherwhite(g));  /* flip current white */
		  g.sweepstrgc = 0;  /* go to sweep phase */
		  g.gcstate = GCSsweepstring;
		}

		
		private static l_mem singlestep (lua_State L) {
		  global_State g = G(L);
		  /*lua_checkmemory(L);*/
		  switch (g.gcstate) {
			case GCSpause: {
			  markroot(L);  /* start a new collection */
			  return 0;
			}
			case GCSpropagate: {
			  if (g.gray != null)
				return propagatemark(g);
			  else {  /* no more `gray' objects */
				atomic(L);  /* finish mark phase */
				return 0;
			  }
			}
			case GCSsweepstring: {
		  	  sweepwholelist(L, new ArrayRef(g.strt.hash, g.sweepstrgc++));
			  if (g.sweepstrgc >= g.strt.size) {  /* nothing more to sweep? */
		  	  	g.sweepgc = new RootGCRef(g);
				g.gcstate = GCSsweep;  /* sweep all other objects */
              }
			  return GCSWEEPCOST;
			}
			case GCSsweep: {
		  	  g.sweepgc = sweeplist(L, g.sweepgc, GCSWEEPMAX);
			  if (g.sweepgc.get() == null)  /* nothing more to sweep? */
				g.gcstate = GCSfinalize;  /* end sweep phase */
			  return GCSWEEPMAX*GCSWEEPCOST;
			}
			case GCSfinalize: {
			  if (g.tobefnz != null) {
				GCTM(L, 1);
				return GCFINALIZECOST;
			  }
			  else {
		  		checkSizes(L);
				g.gcstate = GCSpause;  /* end collection */
				return 0;
			  }
			}
			default: lua_assert(0); return 0;
		  }
		}

		public static void luaC_step (lua_State L) {
		  global_State g = G(L);
		  l_mem lim = (l_mem)((GCSTEPSIZE / 100) * g.gcstepmul);  /* how much to work */
          lu_mem debt = g.totalbytes - g.GCthreshold;
          lua_assert(g.gckind == KGC_NORMAL);
		  do {  /* always perform at least one single step */
			lim -= singlestep(L);
		  } while (lim > 0 && g.gcstate != GCSpause);
		  g.GCthreshold =  (g.gcstate != GCSpause)
		                       ? g.totalbytes + GCSTEPSIZE
		                       : (g.totalbytes/100) * g.gcpause;
		  /* compensate if GC is "behind schedule" (has some debt to pay) */
		  if (g.GCthreshold > debt) g.GCthreshold -= debt;
		}


		/*
		** advances the garbage collector until it reaches a state allowed
		** by 'statemask'
		*/
		public static void luaC_runtilstate (lua_State L, int statesmask) {
		  global_State g = G(L);
		  while (!testbit(statesmask, g->gcstate))
		    singlestep(L);
		}


		/*
		** performs a full GC cycle; if "isememrgency", does not call
		** finalizers (which could change stack positions)
		*/
		public static void luaC_fullgc (lua_State L, int isemergency) {
		  global_State g = G(L);
		  lua_assert(g.gckind == KGC_NORMAL);
		  g.gckind = (byte)(isemergency != 0 ? KGC_EMERGENCY : KGC_FORCED);
		  if (g->gcstate == GCSpropagate) {  /* marking phase? */
		    /* must sweep all objects to turn them back to white
		       (as white does not change, nothing will be collected) */
		    g.sweepstrgc = 0;
		    g.gcstate = GCSsweepstring;
		  }
		  /* finish any pending sweep phase */
		  luaC_runtilstate(L, ~bit2mask(GCSsweepstring, GCSsweep));
		  markroot(L);  /* start a new collection */
		  /* run collector up to finalizers */
		  luaC_runtilstate(L, bitmask(GCSfinalize));
		  g.gckind = KGC_NORMAL;
		  if (!isemergency)   /* do not run finalizers during emergency GC */
		   luaC_runtilstate(L, ~bitmask(GCSfinalize));
		  g.GCthreshold = (g.totalbytes/100) * g.gcpause;
		}

        /* }====================================================== */


	}
}

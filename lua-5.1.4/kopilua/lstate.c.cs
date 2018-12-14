/*
** $Id: lstate.c,v 2.36.1.2 2008/01/03 15:20:39 roberto Exp $
** Global State
** See Copyright Notice in lua.h
*/

using System;
using System.Runtime.InteropServices;

namespace KopiLua
{
	using lu_byte = System.Byte;
	using lu_int32 = System.Int32;
	using lu_mem = System.UInt32;
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using ptrdiff_t = System.Int32;
	using Instruction = System.UInt32;

	public partial class Lua
	{


		public static int state_size(object x) { return Marshal.SizeOf(x) + LUAI_EXTRASPACE; }
		/*
		public static lu_byte fromstate(object l)
		{
			return (lu_byte)(l - LUAI_EXTRASPACE);
		}
		*/
		public static lua_State tostate(object l)
		{
			debug_assert(LUAI_EXTRASPACE == 0, "LUAI_EXTRASPACE not supported");
			return (lua_State)l;
		}


		/*
		** Main thread combines a thread state and the global state
		*/
		public class LG : lua_State {
		  public lua_State l {get {return this;}}
		  public global_State g = new global_State();
		};
		  


		private static void stack_init (lua_State L1, lua_State L) {
		  /* initialize CallInfo array */
		  L1.base_ci = luaM_newvector<CallInfo>(L, BASIC_CI_SIZE);
		  L1.ci = L1.base_ci[0];
		  L1.size_ci = BASIC_CI_SIZE;
		  L1.end_ci = L1.base_ci[L1.size_ci - 1];
		  /* initialize stack array */
		  L1.stack = luaM_newvector<TValue>(L, BASIC_STACK_SIZE + EXTRA_STACK);
		  L1.stacksize = BASIC_STACK_SIZE + EXTRA_STACK;
		  L1.top = L1.stack[0];
		  L1.stack_last = L1.stack[L1.stacksize - EXTRA_STACK - 1];
		  /* initialize first ci */
		  L1.ci.func = L1.top;
		  setnilvalue(StkId.inc(ref L1.top));  /* `function' entry for this `ci' */
		  L1.base_ = L1.ci.base_ = L1.top;
		  L1.ci.top = L1.top + LUA_MINSTACK;
		}


		private static void freestack (lua_State L, lua_State L1) {
		  luaM_freearray(L, L1.base_ci);
		  luaM_freearray(L, L1.stack);
		}


		/*
		** open parts that may cause memory-allocation errors
		*/
		private static void f_luaopen (lua_State L, object ud) {
		  global_State g = G(L);
		  //UNUSED(ud);
		  stack_init(L, L);  /* init stack */
		  sethvalue(L, gt(L), luaH_new(L, 0, 2));  /* table of globals */
		  sethvalue(L, registry(L), luaH_new(L, 0, 2));  /* registry */
		  luaS_resize(L, MINSTRTABSIZE);  /* initial size of string table */
		  luaT_init(L);
		  luaX_init(L);
		  luaS_fix(luaS_newliteral(L, MEMERRMSG));
		  g.GCthreshold = 4*g.totalbytes;
		}


		private static void preinit_state (lua_State L, global_State g) {
		  G_set(L, g);
		  L.stack = null;
		  L.stacksize = 0;
		  L.errorJmp = null;
		  L.hook = null;
		  L.hookmask = 0;
		  L.basehookcount = 0;
		  L.allowhook = 1;
		  resethookcount(L);
		  L.openupval = null;
		  L.size_ci = 0;
		  L.nCcalls = L.baseCcalls = 0;
		  L.status = 0;
		  L.base_ci = null;
		  L.ci = null;
		  L.savedpc = new InstructionPtr();
		  L.errfunc = 0;
		  setnilvalue(gt(L));
		}


		private static void close_state (lua_State L) {
		  global_State g = G(L);
		  luaF_close(L, L.stack[0]);  /* close all upvalues for this thread */
		  luaC_freeall(L);  /* collect all objects */
		  lua_assert(g.rootgc == obj2gco(L));
		  lua_assert(g.strt.nuse == 0);
		  luaM_freearray(L, G(L).strt.hash);
		  luaZ_freebuffer(L, g.buff);
		  freestack(L, L);
		  lua_assert(g.totalbytes == GetUnmanagedSize(typeof(LG)));
		  //g.frealloc(g.ud, fromstate(L), (uint)state_size(typeof(LG)), 0);
		}


		private static lua_State luaE_newthread (lua_State L) {
		  //lua_State L1 = tostate(luaM_malloc(L, state_size(typeof(lua_State))));
		  lua_State L1 = luaM_new<lua_State>(L);
		  luaC_link(L, obj2gco(L1), LUA_TTHREAD);
		  preinit_state(L1, G(L));
		  stack_init(L1, L);  /* init stack */
		  setobj2n(L, gt(L1), gt(L));  /* share table of globals */
		  L1.hookmask = L.hookmask;
		  L1.basehookcount = L.basehookcount;
		  L1.hook = L.hook;
		  resethookcount(L1);
		  lua_assert(iswhite(obj2gco(L1)));
		  return L1;
		}


		private static void luaE_freethread (lua_State L, lua_State L1) {
		  luaF_close(L1, L1.stack[0]);  /* close all upvalues for this thread */
		  lua_assert(L1.openupval == null);
		  luai_userstatefree(L1);
		  freestack(L, L1);
		  //luaM_freemem(L, fromstate(L1));
		}


		public static lua_State lua_newstate (lua_Alloc f, object ud) {
		  int i;
		  lua_State L;
		  global_State g;
		  //object l = f(ud, null, 0, (uint)state_size(typeof(LG)));
		  object l = f(typeof(LG));
		  if (l == null) return null;
		  L = tostate(l);
		  g = (L as LG).g;
		  L.next = null;
		  L.tt = LUA_TTHREAD;
		  g.currentwhite = (lu_byte)bit2mask(WHITE0BIT, FIXEDBIT);
		  L.marked = luaC_white(g);
		  lu_byte marked = L.marked;	// can't pass properties in as ref
		  set2bits(ref marked, FIXEDBIT, SFIXEDBIT);
		  L.marked = marked;
		  preinit_state(L, g);
		  g.frealloc = f;
		  g.ud = ud;
		  g.mainthread = L;
		  g.uvhead.u.l.prev = g.uvhead;
		  g.uvhead.u.l.next = g.uvhead;
		  g.GCthreshold = 0;  /* mark it as unfinished state */
		  g.strt.size = 0;
		  g.strt.nuse = 0;
		  g.strt.hash = null;
		  setnilvalue(registry(L));
		  luaZ_initbuffer(L, g.buff);
		  g.panic = null;
		  g.gcstate = GCSpause;
		  g.rootgc = obj2gco(L);
		  g.sweepstrgc = 0;
		  g.sweepgc = new RootGCRef(g);
		  g.gray = null;
		  g.grayagain = null;
		  g.weak = null;
		  g.tmudata = null;
		  g.totalbytes = (uint)GetUnmanagedSize(typeof(LG));
		  g.gcpause = LUAI_GCPAUSE;
		  g.gcstepmul = LUAI_GCMUL;
		  g.gcdept = 0;
		  for (i=0; i<NUM_TAGS; i++) g.mt[i] = null;
		  if (luaD_rawrunprotected(L, f_luaopen, null) != 0) {
			/* memory allocation error: free partial state */
			close_state(L);
			L = null;
		  }
		  else
			luai_userstateopen(L);
		  return L;
		}


		private static void callallgcTM (lua_State L, object ud) {
		  //UNUSED(ud);
		  luaC_callGCTM(L);  /* call GC metamethods for all udata */
		}


		public static void lua_close (lua_State L) {
		  L = G(L).mainthread;  /* only the main thread can be closed */
		  lua_lock(L);
		  luaF_close(L, L.stack[0]);  /* close all upvalues for this thread */
		  luaC_separateudata(L, 1);  /* separate udata that have GC metamethods */
		  L.errfunc = 0;  /* no error function during GC metamethods */
		  do {  /* repeat until no more errors */
			L.ci = L.base_ci[0];
			L.base_ = L.top = L.ci.base_;
			L.nCcalls = L.baseCcalls = 0;
		  } while (luaD_rawrunprotected(L, callallgcTM, null) != 0);
		  lua_assert(G(L).tmudata == null);
		  luai_userstateclose(L);
		  close_state(L);
		}

	}
}

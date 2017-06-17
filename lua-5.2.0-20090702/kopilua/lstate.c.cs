/*
** $Id: lstate.c,v 2.55 2009/06/01 19:09:26 roberto Exp roberto $
** Global State
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

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
			Debug.Assert(LUAI_EXTRASPACE == 0, "LUAI_EXTRASPACE not supported");
			return (lua_State)l;
		}


		/*
		** Main thread combines a thread state and the global state
		*/
		public class LG : lua_State {
		  public lua_State l {get {return this;}}
		  public global_State g = new global_State();
		};
		


		/*
		** maximum number of nested calls made by error-handling function
		*/
		public const int LUAI_EXTRACALLS = 10;


		public static CallInfo luaE_extendCI (lua_State L) {
		  CallInfo ci = luaM_new<CallInfo>(L); //FIXME:???
		  lua_assert(L.ci.next == null);
		  L.ci.next = ci;
		  ci.previous = L.ci;
		  ci.next = null;
		  if (++L.nci >= LUAI_MAXCALLS) {
		    if (L.nci == LUAI_MAXCALLS)  /* overflow? */
		      luaG_runerror(L, "stack overflow");
		    if (L.nci >= LUAI_MAXCALLS + LUAI_EXTRACALLS)  /* again? */
		      luaD_throw(L, LUA_ERRERR);  /* error while handling overflow */
		  }
		  return ci;
		}


		public static void luaE_freeCI (lua_State L) {
		  CallInfo ci = L.ci;
		  CallInfo next = ci.next;
		  ci.next = null;
		  while ((ci = next) != null) {
		    next = ci.next;
		    luaM_free(L, ci);
		    L.nci--;
		  }
		}


		private static void stack_init (lua_State L1, lua_State L) {
		  int i;
		  /* initialize stack array */
		  L1.stack = luaM_newvector<TValue>(L, BASIC_STACK_SIZE + EXTRA_STACK);
		  L1.stacksize = BASIC_STACK_SIZE + EXTRA_STACK;
		  for (i = 0; i < BASIC_STACK_SIZE + EXTRA_STACK; i++)
		  	setnilvalue(L1.stack[i]);  /* erase new stack */
		  L1.top = L1.stack[0];
		  L1.stack_last = L1.stack[L1.stacksize - EXTRA_STACK - 1];
		  /* initialize first ci */
		  L1.ci.func = L1.top;
		  setnilvalue(StkId.inc(ref L1.top));  /* 'function' entry for this 'ci' */
		  L1.ci.top = L1.top + LUA_MINSTACK;
          L1.ci.callstatus = 0;
		}


		private static void freestack (lua_State L) {
		  L.ci = L.base_ci;  /* reset 'ci' list */
		  luaE_freeCI(L);
		  lua_assert(L.nci == 0);
		  luaM_freearray(L, L.stack);
		}


		/*
		** open parts that may cause memory-allocation errors
		*/
		private static void f_luaopen (lua_State L, object ud) {
		  global_State g = G(L);
		  //UNUSED(ud);
		  stack_init(L, L);  /* init stack */
		  sethvalue(L, gt(L), luaH_new(L));  /* table of globals */
		  sethvalue(L, registry(L), luaH_new(L));  /* registry */
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
		  L.nny = 1;
		  L.status = LUA_OK;
		  L.base_ci.next = L.base_ci.previous = null;
		  L.ci = L.base_ci;
		  L.nci = 0;
		  L.errfunc = 0;
		  setnilvalue(gt(L));
		}


		private static void close_state (lua_State L) {
		  global_State g = G(L);
		  luaF_close(L, L.stack[0]);  /* close all upvalues for this thread */
		  luaC_freeall(L);  /* collect all objects */
		  luaM_freearray(L, G(L).strt.hash);
		  luaZ_freebuffer(L, g.buff);
		  freestack(L);
		  lua_assert(g.totalbytes == GetUnmanagedSize(typeof(LG)));
		  //g.frealloc(g.ud, fromstate(L), (uint)state_size(typeof(LG)), 0);
		}


		private static lua_State lua_newthread (lua_State L) {
		  lua_State L1;
		  lua_lock(L);
		  luaC_checkGC(L);
		  //lua_State L1 = tostate(luaM_malloc(L, state_size(typeof(lua_State))));
		  L1 = luaM_new<lua_State>(L);
		  luaC_link(L, obj2gco(L1), LUA_TTHREAD);
		  setthvalue(L, L.top, L1);
		  api_incr_top(L);
		  preinit_state(L1, G(L));
		  stack_init(L1, L);  /* init stack */
		  setobj2n(L, gt(L1), gt(L));  /* share table of globals */
		  L1.hookmask = L.hookmask;
		  L1.basehookcount = L.basehookcount;
		  L1.hook = L.hook;
		  resethookcount(L1);
		  lua_assert(iswhite(obj2gco(L1)));
		  lua_unlock(L);
		  luai_userstatethread(L, L1);
		  return L1;
		}


		private static void luaE_freethread (lua_State L, lua_State L1) {
		  luaF_close(L1, L1.stack[0]);  /* close all upvalues for this thread */
		  lua_assert(L1.openupval == null);
		  luai_userstatefree(L1);
		  freestack(L1);
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
		  g.gckind = KGC_NORMAL;
		  g.nCcalls = 0;
		  lu_byte marked = L.marked;	// can't pass properties in as ref ???//FIXME:???
		  set2bits(ref marked, FIXEDBIT, SFIXEDBIT);
		  L.marked = marked; //remove this //FIXME:???
		  preinit_state(L, g);
		  g.frealloc = f;
		  g.ud = ud;
		  g.mainthread = L;
		  g.uvhead.u.l.prev = g.uvhead;
		  g.uvhead.u.l.next = g.uvhead;
		  g.GCthreshold = MAX_LUMEM;  /* mark it as unfinished state */
		  g.strt.size = 0;
		  g.strt.nuse = 0;
		  g.strt.hash = null;
		  setnilvalue(registry(L));
		  luaZ_initbuffer(L, g.buff);
		  g.panic = null;
          g.version = lua_version(null);
		  g.gcstate = GCSpause;
		  g.rootgc = obj2gco(L);
		  g.sweepstrgc = 0;
		  g.sweepgc = new RootGCRef(g);
		  g.gray = null;
		  g.grayagain = null;
		  g.weak = g.ephemeron = g.allweak = null;
		  g.tobefnz = null;
		  g.totalbytes = (uint)GetUnmanagedSize(typeof(LG));
		  g.gcpause = LUAI_GCPAUSE;
		  g.gcstepmul = LUAI_GCMUL;
		  g.gcdept = 0;
		  for (i=0; i<NUM_TAGS; i++) g.mt[i] = null;
		  if (luaD_rawrunprotected(L, f_luaopen, null) != LUA_OK) {
			/* memory allocation error: free partial state */
			close_state(L);
			L = null;
		  }
		  else
			luai_userstateopen(L);
		  return L;
		}


		public static void lua_close (lua_State L) {
		  L = G(L).mainthread;  /* only the main thread can be closed */
		  lua_lock(L);
		  luaF_close(L, L.stack[0]);  /* close all upvalues for this thread */
		  luaC_separateudata(L, 1);  /* separate all udata with GC metamethods */
          lua_assert(L.next == null);
		  luaC_callAllGCTM(L);  /* call GC metamethods for all udata */
		  luai_userstateclose(L);
		  close_state(L);
		}

	}
}

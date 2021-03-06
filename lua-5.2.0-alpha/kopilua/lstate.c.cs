/*
** $Id: lstate.c,v 2.85 2010/04/30 18:36:22 roberto Exp roberto $
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
		//#if !defined(LUAI_GCPAUSE)
		private const int LUAI_GCPAUSE = 200;  /* 200% */
		//#endif

		//#if !defined(LUAI_GCMAJOR)
		private const int LUAI_GCMAJOR = 200;  /* 200% */
		//#endif

		//#if !defined(LUAI_GCMUL)
		private const int LUAI_GCMUL = 200; /* GC runs 'twice the speed' of memory allocation */
		//#endif


		private const string MEMERRMSG = "not enough memory";


		/*
		** thread state + extra space
		*/
		public class LX {
		#if LUAI_EXTRASPACE
		  public char[] buff = new char[LUAI_EXTRASPACE];
		#endif
		  public lua_State l = new lua_State();
		};


		/*
		** Main thread combines a thread state and the global state
		*/
		public class LG : lua_State {
		  public LX l = new LX();
		  public global_State g = new global_State();
		};
		

        //FIXME:???not implemented
        private static LX fromstate(lua_State L) { 
        	throw new Exception("not implemented"); //FIXME:???
        	return /*((LX)((lu_byte[])(L) - offsetof(LX, l)))*/ null; 
        } 



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
		  return ci;
		}


		public static void luaE_freeCI (lua_State L) {
		  CallInfo ci = L.ci;
		  CallInfo next = ci.next;
		  ci.next = null;
		  while ((ci = next) != null) {
		    next = ci.next;
		    luaM_free(L, ci);
		  }
		}


		private static void stack_init (lua_State L1, lua_State L) {
		  int i; CallInfo ci;
		  /* initialize stack array */
		  L1.stack = luaM_newvector<TValue>(L, BASIC_STACK_SIZE);
		  L1.stacksize = BASIC_STACK_SIZE;
		  for (i = 0; i < BASIC_STACK_SIZE; i++)
		  	setnilvalue(L1.stack[i]);  /* erase new stack */
		  L1.top = L1.stack[0];
		  L1.stack_last = L1.stack[L1.stacksize - EXTRA_STACK];
		  /* initialize first ci */
		  ci = L1.base_ci;
		  ci.next = ci.previous = null;
		  ci.callstatus = 0;
		  ci.func = L1.top;
		  setnilvalue(StkId.inc(ref L1.top));  /* 'function' entry for this 'ci' */
		  ci.top = L1.top + LUA_MINSTACK;
          L1.ci = ci;
		}


		private static void freestack (lua_State L) {
		  if (L.stack == null)
		    return;  /* stack not completely built yet */
		  L.ci = L.base_ci;  /* free the entire 'ci' list */
		  luaE_freeCI(L);
		  luaM_freearray(L, L.stack);  /* free stack array */
		}


		/*
		** Create registry table and its predefined values
		*/
		private static void init_registry (lua_State L, global_State g) {
		  TValue mt = new TValue();
		  /* create registry */
		  Table registry = luaH_new(L);
		  sethvalue(L, g.l_registry, registry);
		  luaH_resize(L, registry, LUA_RIDX_LAST, 0);
		  /* registry[LUA_RIDX_MAINTHREAD] = L */
		  setthvalue(L, mt, L);
		  setobj2t(L, luaH_setint(L, registry, LUA_RIDX_MAINTHREAD), mt);
		  /* registry[LUA_RIDX_GLOBALS] = table of globals */
  		  sethvalue(L, mt, luaH_new(L));
		  setobj2t(L, luaH_setint(L, registry, LUA_RIDX_GLOBALS), mt);
		}


		/*
		** open parts of the state that may cause memory-allocation errors
		*/
		private static void f_luaopen (lua_State L, object ud) {
		  global_State g = G(L);
		  //UNUSED(ud);
		  stack_init(L, L);  /* init stack */
		  init_registry(L, g);
		  luaS_resize(L, MINSTRTABSIZE);  /* initial size of string table */
		  luaT_init(L);
		  luaX_init(L);
		  /* pre-create memory-error message */
		  g.memerrmsg = luaS_newliteral(L, MEMERRMSG);
		  luaS_fix(g.memerrmsg);  /* it should never be collected */
		  g.GCdebt = 0;
		}


		/*
		** preinitialize a state with consistent values without allocating
		** any memory (to avoid errors)
		*/
		private static void preinit_state (lua_State L, global_State g) {
		  G_set(L, g);
		  L.stack = null;
          L.ci = null;
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
		  L.errfunc = 0;
		}


		private static void close_state (lua_State L) {
		  global_State g = G(L);
		  luaF_close(L, L.stack[0]);  /* close all upvalues for this thread */
		  luaC_freeallobjects(L);  /* collect all objects */
		  luaM_freearray(L, G(L).strt.hash);
		  luaZ_freebuffer(L, g.buff);
		  freestack(L);
		  lua_assert(g.totalbytes == GetUnmanagedSize(typeof(LG)));
		  //g.frealloc(g.ud, fromstate(L), (uint)GetUnmanagedSize(typeof(LG)), 0); //FIXME:???deleted
		}


		private static lua_State lua_newthread (lua_State L) {
		  lua_State L1;
		  lua_lock(L);
		  luaC_checkGC(L);
		  L1 = luaC_newobj<lua_State>(L, LUA_TTHREAD, (uint)GetUnmanagedSize(typeof(LX)), null, /*offsetof(LX, l)*/0).th; //FIXME:???
		  setthvalue(L, L.top, L1);
		  api_incr_top(L);
		  preinit_state(L1, G(L));
		  L1.hookmask = L.hookmask;
		  L1.basehookcount = L.basehookcount;
		  L1.hook = L.hook;
		  resethookcount(L1);
		  luai_userstatethread(L, L1);
		  stack_init(L1, L);  /* init stack */
		  lua_unlock(L);
		  return L1;
		}


		private static void luaE_freethread (lua_State L, lua_State L1) {
          LX l = fromstate(L1);
		  luaF_close(L1, L1.stack[0]);  /* close all upvalues for this thread */
		  lua_assert(L1.openupval == null);
		  luai_userstatefree(L, L1);
		  freestack(L1);
		  //luaM_free(L, l); //FIXME:added
		}


		public static lua_State lua_newstate (lua_Alloc f, object ud) {
		  int i;
		  lua_State L;
		  global_State g;
		  LG l = (LG)f(typeof(LG)); //FIXME:(LG)(f(ud, null, LUA_TTHREAD, (uint)(GetUnmanagedSize(typeof(LG))))); //FIXME:???not sync, LUA_TTHREAD
		  if (l == null) return null;
		  L = l.l.l;
		  g = l.g;
		  L.next = null;
		  L.tt = LUA_TTHREAD;
		  g.currentwhite = (lu_byte)bit2mask(WHITE0BIT, FIXEDBIT);
		  L.marked = luaC_white(g);
		  g.gckind = KGC_NORMAL;
		  g.nCcalls = 0;
		  lu_byte marked = L.marked;	// can't pass properties in as ref ???//FIXME:??? //FIXME:added
		  L.marked = marked; //remove this //FIXME:??? //FIXME:added
		  preinit_state(L, g);
		  g.frealloc = f;
		  g.ud = ud;
		  g.mainthread = L;
		  g.uvhead.u.l.prev = g.uvhead;
		  g.uvhead.u.l.next = g.uvhead;
		  stopgc(g);  /* no GC while building state */
  		  g.lastmajormem = 0;
		  g.strt.size = 0;
		  g.strt.nuse = 0;
		  g.strt.hash = null;
		  setnilvalue(g.l_registry);
		  luaZ_initbuffer(L, g.buff);
		  g.panic = null;
          g.version = lua_version(null);
		  g.gcstate = GCSpause;
		  g.allgc = null;
  		  g.udgc = null;
		  g.tobefnz = null;
		  g.gray = g.grayagain = null;
		  g.weak = g.ephemeron = g.allweak = null;
		  g.totalbytes = (uint)GetUnmanagedSize(typeof(LG));
		  g.gcpause = LUAI_GCPAUSE;
          g.gcmajorinc = LUAI_GCMAJOR;
		  g.gcstepmul = LUAI_GCMUL;
		  for (i=0; i < LUA_NUMTAGS; i++) g.mt[i] = null;
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
		  luai_userstateclose(L);
		  close_state(L);
		}

	}
}

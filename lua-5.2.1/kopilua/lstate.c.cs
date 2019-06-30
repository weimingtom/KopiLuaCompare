/*
** $Id: lstate.c,v 2.98 2012/05/30 12:33:44 roberto Exp $
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
	using l_mem = System.Int32; 

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
		** a macro to help the creation of a unique random seed when a state is
		** created; the seed is used to randomize hashes.
		*/
		//#if !defined(luai_makeseed)
		//#include <time.h>
		private static uint luai_makeseed() { return (uint)(DateTime.Now.Ticks); } //cast(size_t, time(NULL))
		//#endif




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
		** Compute an initial seed as random as possible. In ANSI, rely on
		** Address Space Layout Randomization (if present) to increase
		** randomness..
		*/
		private static void addbuff(CharPtr b, int p, object e)
			{ uint t = (uint)(e);
			memcpy(b + p, CharPtr.FromNumber(t), (uint)GetUnmanagedSize(typeof(uint))); p += GetUnmanagedSize(typeof(uint)); }

		delegate lua_State lua_newstate_delegate (lua_Alloc f, object ud);
		private static uint makeseed (lua_State L) {
		  throw new Exception("not implemented"); //FIXME:???
		  CharPtr buff = new CharPtr(new char[4 * GetUnmanagedSize(typeof(uint))]);
		  uint h = luai_makeseed();
		  int p = 0;
		  addbuff(buff, p, L);  /* heap variable */
		  addbuff(buff, p, h);  /* local variable */
		  addbuff(buff, p, luaO_nilobject);  /* global variable */
		  lua_newstate_delegate _d = lua_newstate;
		  addbuff(buff, p, Marshal.GetFunctionPointerForDelegate(_d));  /* public function */
		  lua_assert(p == buff.chars.Length);
		  return luaS_hash(buff, (uint)p, h);
		}


		/*
		** set GCdebt to a new value keeping the value (totalbytes + GCdebt)
		** invariant
		*/
		private static void luaE_setdebt (global_State g, l_mem debt) {
		  g.totalbytes -= (uint)(debt - g.GCdebt); //FIXME:(uint)
		  g.GCdebt = debt;
		}


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
		  luaH_setint(L, registry, LUA_RIDX_MAINTHREAD, mt);
		  /* registry[LUA_RIDX_GLOBALS] = table of globals */
  		  sethvalue(L, mt, luaH_new(L));
		  luaH_setint(L, registry, LUA_RIDX_GLOBALS, mt);
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
		  g.gcrunning = 1;  /* allow gc */
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
          L.nCcalls = 0;
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
		  lua_assert(gettotalbytes(g) == GetUnmanagedSize(typeof(LG))); //FIXME:changed, sizeof(LG)
		  //g.frealloc(g.ud, fromstate(L), (uint)GetUnmanagedSize(typeof(LG)), 0);  /* free main block */ //FIXME:???deleted
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
		  lu_byte marked = L.marked;	// can't pass properties in as ref ???//FIXME:??? //FIXME:added
		  L.marked = marked; //remove this //FIXME:??? //FIXME:added
		  preinit_state(L, g);
		  g.frealloc = f;
		  g.ud = ud;
		  g.mainthread = L;
		  g.seed = makeseed(L);
		  g.uvhead.u.l.prev = g.uvhead;
		  g.uvhead.u.l.next = g.uvhead;
		  g.gcrunning = 0;  /* no GC while building state */
  		  g.GCestimate = 0;
		  g.strt.size = 0;
		  g.strt.nuse = 0;
		  g.strt.hash = null;
		  setnilvalue(g.l_registry);
		  luaZ_initbuffer(L, g.buff);
		  g.panic = null;
          g.version = lua_version(null);
		  g.gcstate = GCSpause;
		  g.allgc = null;
  		  g.finobj = null;
		  g.tobefnz = null;
		  g.sweepgc = g.sweepfin = null;
		  g.gray = g.grayagain = null;
		  g.weak = g.ephemeron = g.allweak = null;
		  g.totalbytes = (uint)GetUnmanagedSize(typeof(LG));
          g.GCdebt = 0;
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
		  luai_userstateclose(L);
		  close_state(L);
		}

	}
}

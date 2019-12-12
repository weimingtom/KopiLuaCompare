/*
** $Id: lstate.c,v 2.133 2015/11/13 12:16:51 roberto Exp $
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


		//#if !defined(LUAI_GCMUL)
		private const int LUAI_GCMUL = 200; /* GC runs 'twice the speed' of memory allocation */
		//#endif


		/*
		** a macro to help the creation of a unique random seed when a state is
		** created; the seed is used to randomize hashes.
		*/
		//#if !defined(luai_makeseed)
		//#include <time.h>
		private static uint luai_makeseed() { return (uint)(time(null)); } //cast(unsigned int, time(NULL))
		//#endif




		/*
		** thread state + extra space
		*/
		public class LX {
		  public lu_byte[] extra_ = new lu_byte[LUA_EXTRASPACE];
		  public lua_State l = new lua_State();
		  
		  public LX() { l._parent = this; }
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
		 //throw new Exception("not implemented"); //FIXME:???
		 //return /*((LX)((lu_byte[])(L) - offsetof(LX, l)))*/ null; 
		 if (L._parent == null)
		 {
		 	throw new Exception();
		 }
		 return L._parent;
        } 


		/*
		** Compute an initial seed as random as possible. Rely on Address Space
		** Layout Randomization (if present) to increase randomness..
		*/
		private static void addbuff(CharPtr b, int p, object e)
			{ 
			//https://blog.csdn.net/yingwang9/article/details/82215619
			GCHandle handle1 = GCHandle.Alloc(e);IntPtr ptr = GCHandle.ToIntPtr(handle1);	
			uint t = (uint)(ptr);
			memcpy(b + p, CharPtr.FromNumber(t), (uint)GetUnmanagedSize(typeof(uint))); p += GetUnmanagedSize(typeof(uint)); }

		delegate lua_State lua_newstate_delegate (lua_Alloc f, object ud);
		private static uint makeseed (lua_State L) {
		  //throw new Exception("not implemented"); //FIXME:???
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
		** invariant (and avoiding underflows in 'totalbytes')
		*/
		private static void luaE_setdebt (global_State g, l_mem debt) {
		  l_mem tb = (l_mem)gettotalbytes(g);
		  lua_assert(tb > 0);
		  if (debt < tb - MAX_LMEM)
		    debt = tb - MAX_LMEM;  /* will make 'totalbytes == MAX_LMEM' */
		  g.totalbytes = tb - debt;
		  g.GCdebt = debt;
		}


		public static CallInfo luaE_extendCI (lua_State L) {
		  CallInfo ci = luaM_new<CallInfo>(L); //FIXME:???
		  lua_assert(L.ci.next == null);
		  L.ci.next = ci;
		  ci.previous = L.ci;
		  ci.next = null;
		  L.nci++;
		  return ci;
		}


		/*
		** free all CallInfo structures not in use by a thread
		*/
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


		/*
		** free half of the CallInfo structures not in use by a thread
		*/
		public static void luaE_shrinkCI (lua_State L) {
		  CallInfo ci = L.ci;
		  CallInfo next2;  /* next's next */
		  /* while there are two nexts */
		  while (ci.next != null && (next2 = ci.next.next) != null) {
		    luaM_free(L, ci.next);  /* free next */
		    L.nci--;
		    ci.next = next2;  /* remove 'next' from the list */
		    next2.previous = ci;
		    ci = next2;  /* keep next's next */
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
		  lua_assert(L.nci == 0);
		  luaM_freearray(L, L.stack);  /* free stack array */
		}


		/*
		** Create registry table and its predefined values
		*/
		private static void init_registry (lua_State L, global_State g) {
		  TValue temp = new TValue();
		  /* create registry */
		  Table registry = luaH_new(L);
//		  _registry = registry;
		  sethvalue(L, g.l_registry, registry);
		  luaH_resize(L, registry, LUA_RIDX_LAST, 0);
		  /* registry[LUA_RIDX_MAINTHREAD] = L */
		  setthvalue(L, temp, L);  /* temp = L */
		  luaH_setint(L, registry, LUA_RIDX_MAINTHREAD, temp);  //FIXME:这里没有设置成功
//		  lua_xxx();
		  /* registry[LUA_RIDX_GLOBALS] = table of globals */
  		  sethvalue(L, temp, luaH_new(L));  /* temp = new table (global table) */
		  luaH_setint(L, registry, LUA_RIDX_GLOBALS, temp);
//		  lua_xxx();
		}


		/*
		** open parts of the state that may cause memory-allocation errors.
		** ('g->version' != NULL flags that the state was completely build)
		*/
		private static void f_luaopen (lua_State L, object ud) {
		  global_State g = G(L);
		  //UNUSED(ud);
		  stack_init(L, L);  /* init stack */
		  init_registry(L, g);
		  luaS_init(L);
		  luaT_init(L);
		  luaX_init(L);
		  g.gcrunning = 1;  /* allow gc */	  
		  g.version = lua_version(null);
		  luai_userstateopen(L);		  
		}


		/*
		** preinitialize a thread with consistent values without allocating
		** any memory (to avoid errors)
		*/
		private static void preinit_thread (lua_State L, global_State g) {
		  G_set(L, g);
		  L.stack = null;
          L.ci = null;
		  L.nci = 0;
		  L.stacksize = 0;
		  L.twups = L;  /* thread has no upvalues */
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
		  if (g.version!=null)  /* closing a fully built state? */
		    luai_userstateclose(L);		  	  
		  luaM_freearray(L, G(L).strt.hash);
		  freestack(L);
		  lua_assert(gettotalbytes(g) == GetUnmanagedSize(typeof(LG))); //FIXME:changed, sizeof(LG)
		  //g.frealloc(g.ud, fromstate(L), (uint)GetUnmanagedSize(typeof(LG)), 0);  /* free main block */ //FIXME:???deleted
		}


		private static lua_State lua_newthread (lua_State L) {
		  global_State g = G(L);
		  lua_State L1;
		  lua_lock(L);
		  luaC_checkGC(L);
		  /* create new thread */
		  L1 = ((LX)luaM_newobject<LX>(L/*, LUA_TTHREAD)*/)).l; //FIXME:
		  L1.marked = luaC_white(g);
		  L1.tt = LUA_TTHREAD;
		  /* link it on list 'allgc' */
		  L1.next = g.allgc;
		  g.allgc = obj2gco(L1);
		  /* anchor it on L stack */
		  setthvalue(L, L.top, L1);
		  api_incr_top(L);
		  preinit_thread(L1, g);
		  L1.hookmask = L.hookmask;
		  L1.basehookcount = L.basehookcount;
		  L1.hook = L.hook;
		  resethookcount(L1);
		  /* initialize L1 extra space */
		  memcpy(lua_getextraspace(L1), lua_getextraspace(g.mainthread),
		         LUA_EXTRASPACE);		  
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
		  g.currentwhite = (lu_byte)bitmask(WHITE0BIT);
		  L.marked = luaC_white(g);
		  lu_byte marked = L.marked;	// can't pass properties in as ref ???//FIXME:??? //FIXME:added
		  L.marked = marked; //remove this //FIXME:??? //FIXME:added
		  preinit_thread(L, g);
		  g.frealloc = f;
		  g.ud = ud;
		  g.mainthread = L;
		  g.seed = makeseed(L);
		  g.gcrunning = 0;  /* no GC while building state */
  		  g.GCestimate = 0;
		  g.strt.size = g.strt.nuse = 0;
		  g.strt.hash = null;
		  setnilvalue(g.l_registry);
		  g.panic = null;
          g.version = null;
		  g.gcstate = GCSpause;
		  g.gckind = KGC_NORMAL;
		  g.allgc = g.finobj = g.tobefnz = g.fixedgc = null;
		  g.sweepgc = null;
		  g.gray = g.grayagain = null;
		  g.weak = g.ephemeron = g.allweak = null;
		  g.twups = null;
		  g.totalbytes = (int)(uint)GetUnmanagedSize(typeof(LG));
          g.GCdebt = 0;
		  g.gcfinnum = 0;
		  g.gcpause = LUAI_GCPAUSE;
		  g.gcstepmul = LUAI_GCMUL;
		  for (i=0; i < LUA_NUMTAGS; i++) g.mt[i] = null;
		  if (luaD_rawrunprotected(L, f_luaopen, null) != LUA_OK) {
			/* memory allocation error: free partial state */
			close_state(L);
			L = null;
		  }
		  return L;
		}


		public static void lua_close (lua_State L) {
		  L = G(L).mainthread;  /* only the main thread can be closed */
		  lua_lock(L);
		  close_state(L);
		}

	}
}

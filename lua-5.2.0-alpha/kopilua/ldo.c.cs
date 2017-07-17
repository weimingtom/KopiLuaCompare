/*
** $Id: ldo.c,v 2.89 2010/09/30 17:21:31 roberto Exp roberto $
** Stack and Call structure of Lua
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace KopiLua
{
	using lua_Integer = System.Int32;
	using ptrdiff_t = System.Int32;
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using lu_byte = System.Byte;
	using Instruction = System.UInt32;
	
	public partial class Lua
	{


		/*
		** {======================================================
		** Error-recovery functions
		** =======================================================
		*/
		
		/*
		** LUAI_THROW/LUAI_TRY define how Lua does exception handling. By
		** default, Lua handles errors with exceptions when compiling as
		** C++ code, with _longjmp/_setjmp when asked to use them, and with
		** longjmp/setjmp otherwise.
		*/
		//#if !defined(LUAI_THROW)

		//#if defined(__cplusplus) && !defined(LUA_USE_LONGJMP)
		/* C++ exceptions */
		//#define LUAI_THROW(L,c)		throw(c)
		//FIXME:added:
		public class LuaException : Exception
		{
			public lua_State L;
			public lua_longjmp c;

			public LuaException(lua_State L, lua_longjmp c) { this.L = L; this.c = c; }
		}
		public static void LUAI_THROW(lua_State L, lua_longjmp c)	{throw new LuaException(L, c);}
		//#define LUAI_TRY(L,c,a) \
		//	try { a } catch(...) { if ((c)->status == 0) (c)->status = -1; }
		//FIXME:added:
		public static void LUAI_TRY(lua_State L, lua_longjmp c, object a) {
			if (c.status == 0) c.status = -1;
		}
		//#define luai_jmpbuf		int  /* dummy variable */

		//#elif defined(LUA_USE_ULONGJMP)
		/* in Unix, try _longjmp/_setjmp (more efficient) */
		//#define LUAI_THROW(L,c)		_longjmp((c)->b, 1)
		//#define LUAI_TRY(L,c,a)		if (_setjmp((c)->b) == 0) { a }
		//#define luai_jmpbuf		jmp_buf

		//#else
		/* default handling with long jumps */
		//#define LUAI_THROW(L,c)		longjmp((c)->b, 1)
		//#define LUAI_TRY(L,c,a)		if (setjmp((c)->b) == 0) { a }
		//#define luai_jmpbuf		jmp_buf

		//#endif

		//#endif
		
		//FIXME:???
		public delegate void luai_jmpbuf(lua_Integer b);

		/* chain list of long jump buffers */
		public class lua_longjmp {
		  public lua_longjmp previous;
		  public luai_jmpbuf b;
		  public volatile int status;  /* error code */
		};


		private static void seterrorobj (lua_State L, int errcode, StkId oldtop) {
		  switch (errcode) {
			case LUA_ERRMEM: {  /* memory error? */
			  setsvalue2s(L, oldtop, G(L).memerrmsg); /* reuse preregistered msg. */
			  break;
			}
			case LUA_ERRERR: {
			  setsvalue2s(L, oldtop, luaS_newliteral(L, "error in error handling"));
			  break;
			}
			default: {
			  setobjs2s(L, oldtop, L.top-1);  /* error message on current top */
			  break;
			}
		  }
		  L.top = oldtop + 1;
		}


		public static void luaD_throw (lua_State L, int errcode) {
		  if (L.errorJmp != null) {  /* thread has an error handler? */
		    L.errorJmp.status = errcode;  /* set status */
		    LUAI_THROW(L, L.errorJmp);  /* jump to it */
		  }
		  else {  /* thread has no error handler */
		    L.status = cast_byte(errcode);  /* mark it as dead */
		    if (G(L).mainthread.errorJmp != null) {  /* main thread has a handler? */
		      setobjs2s(L, G(L).mainthread.top, L.top - 1); lua_TValue.inc(ref G(L).mainthread.top);  /* copy error obj. */ //FXIME:++
		      luaD_throw(G(L).mainthread, errcode);  /* re-throw in main thread */
		    }
		    else {  /* no handler at all; abort */
		      if (G(L).panic != null) {  /* panic function? */
		        lua_unlock(L);
		        G(L).panic(L);  /* call it (last chance to jump out) */
		      }
		      abort();
		    }
		  }
        }


		public static int luaD_rawrunprotected (lua_State L, Pfunc f, object ud) {
          ushort oldnCcalls = G(L).nCcalls;
		  lua_longjmp lj = new lua_longjmp();
		  lj.status = LUA_OK;
		  lj.previous = L.errorJmp;  /* chain new error handler */
		  L.errorJmp = lj;
			/*
		  LUAI_TRY(L, lj,
			f(L, ud)
		  );
			 * */
#if CATCH_EXCEPTIONS
		  try
#endif
		  {
			  f(L, ud);
		  }
#if CATCH_EXCEPTIONS
          catch (Exception e)
		  {
          	  Debug.WriteLine(e); //FIXME:added for debug
		      if (lj.status == 0)
		          lj.status = -1;
		  }
#endif
		  L.errorJmp = lj.previous;  /* restore old error handler */
          G(L).nCcalls = oldnCcalls;
		  return lj.status;
		}

		/* }====================================================== */


		private static void correctstack (lua_State L, TValue[] oldstack) {
		   //FIXME:???
			/* don't need to do this
		  CallInfo ci;
		  GCObject up;
		  L.top = L.stack[L.top - oldstack];
		  for (up = L.openupval; up != null; up = up.gch.next)
			gco2uv(up).v = L.stack[gco2uv(up).v - oldstack];
		  for (ci = L.base_ci[0]; ci != null; ci = ci.previous) {
			  ci.top = L.stack[ci.top - oldstack];
			ci.func = L.stack[ci.func - oldstack];
		    if (isLua(ci))
		      ci.u.l.base = (ci.u.l.base - oldstack) + L.stack;
		  }
			 * */
		}


		/* some space for error handling */
		private const int ERRORSTACKSIZE = (LUAI_MAXSTACK + 200);


		public static void luaD_reallocstack (lua_State L, int newsize) {
		  TValue[] oldstack = L.stack;
          int lim = L.stacksize;
		  lua_assert(newsize <= LUAI_MAXSTACK || newsize == ERRORSTACKSIZE);
		  lua_assert(L.stack_last == L.stacksize - EXTRA_STACK - 1);
		  luaM_reallocvector(L, ref L.stack, L.stacksize, newsize/*, TValue*/);
		  for (; lim < newsize; lim++)
		  	setnilvalue(L.stack[lim]); /* erase new segment */
		  L.stacksize = newsize;
		  L.stack_last = L.stack[newsize - EXTRA_STACK]; //FIXME:???
		  correctstack(L, oldstack);
		}


		public static void luaD_growstack (lua_State L, int n) {
		  int size = L.stacksize;
		  if (size > LUAI_MAXSTACK)  /* error after extra size? */
		    luaD_throw(L, LUA_ERRERR);
		  else {
		    int needed = (int)(L.top - L.stack) + n + EXTRA_STACK; //FIXME:changed, cast_int()
		    int newsize = 2 * size;
		    if (newsize > LUAI_MAXSTACK) newsize = LUAI_MAXSTACK;
		    if (newsize < needed) newsize = needed;
		    if (newsize > LUAI_MAXSTACK) {  /* stack overflow? */
		      luaD_reallocstack(L, ERRORSTACKSIZE);
		      luaG_runerror(L, "stack overflow");
		    }
		    else
		      luaD_reallocstack(L, newsize);
		  }
		}


		private static int stackinuse (lua_State L) {
		  CallInfo ci;
		  StkId lim = L.top;
		  for (ci = L.ci; ci != null; ci = ci.previous) {
		    lua_assert(ci.top <= L.stack_last);
		    if (lim < ci.top) lim = ci.top;
		  }
		  return cast_int(lim - L.stack) + 1;  /* part of stack in use */
		}


		public static void luaD_shrinkstack (lua_State L) {
		  int inuse = stackinuse(L);
		  int goodsize = inuse + (inuse / 8) + 2*EXTRA_STACK;
		  if (goodsize > LUAI_MAXSTACK) goodsize = LUAI_MAXSTACK;
		  if (inuse > LUAI_MAXSTACK ||  /* handling stack overflow? */
		      goodsize >= L.stacksize) {  /* would grow instead of shrink? */
		    ;//FIXME:???//condmovestack(L);  /* don't change stack (change only for debugging) */
		  } else
		    luaD_reallocstack(L, goodsize);  /* shrink it */
		}


		public static void luaD_hook (lua_State L, int event_, int line) {
		  lua_Hook hook = L.hook;
		  if ((hook!=null) && (L.allowhook!=0)) {
            CallInfo ci = L.ci;
			ptrdiff_t top = savestack(L, L.top);
			ptrdiff_t ci_top = savestack(L, ci.top);
			lua_Debug ar = new lua_Debug();
			ar.event_ = event_;
			ar.currentline = line;
			ar.i_ci = ci;
			luaD_checkstack(L, LUA_MINSTACK);  /* ensure minimum stack size */
			ci.top = L.top + LUA_MINSTACK;
			lua_assert(ci.top <= L.stack_last);
			L.allowhook = 0;  /* cannot call hooks inside a hook */
            ci.callstatus |= CIST_HOOKED;
			lua_unlock(L);
			hook(L, ar);
			lua_lock(L);
			lua_assert(L.allowhook==0);
			L.allowhook = 1;
			ci.top = restorestack(L, ci_top);
			L.top = restorestack(L, top);
			ci.callstatus &= (byte)((~CIST_HOOKED) & 0xff);
		  }
		}


		private static void callhook (lua_State L, CallInfo ci) {
		  int hook = LUA_HOOKCALL;
		  InstructionPtr.inc(ref ci.u.l.savedpc);  /* hooks assume 'pc' is already incremented */
		  if (isLua(ci.previous) != 0 &&
		      GET_OPCODE(ci.previous.u.l.savedpc[- 1]) == OpCode.OP_TAILCALL) {
		    ci.callstatus |= CIST_TAIL;
		    hook = LUA_HOOKTAILCALL;
		  }
		  luaD_hook(L, hook, -1);
		  InstructionPtr.dec(ref ci.u.l.savedpc);  /* correct 'pc' */
		}



		private static StkId adjust_varargs (lua_State L, Proto p, int actual) {
		  int i;
		  int nfixargs = p.numparams;
		  StkId base_, fixed_;
		  lua_assert(actual >= nfixargs);
		  /* move fixed parameters to final position */
		  fixed_ = L.top - actual;  /* first fixed argument */
		  base_ = L.top;  /* final position of first argument */
		  for (i=0; i<nfixargs; i++) {
		  	setobjs2s(L, lua_TValue.inc(ref L.top), fixed_ + i);
		    setnilvalue(fixed_ + i);
		  }
		  return base_;
		}


		static StkId tryfuncTM (lua_State L, StkId func) {
		  /*const*/ TValue tm = luaT_gettmbyobj(L, func, TMS.TM_CALL);
		  StkId p;
		  ptrdiff_t funcr = savestack(L, func);
		  if (!ttisfunction(tm))
			luaG_typeerror(L, func, "call");
		  /* Open a hole inside the stack at `func' */
		  for (p = L.top; p > func; StkId.dec(ref p)) setobjs2s(L, p, p - 1);
		  incr_top(L);
		  func = restorestack(L, funcr);  /* previous call may change stack */
		  setobj2s(L, func, tm);  /* tag method is the new function to be called */
		  return func;
		}



		public static CallInfo next_ci(lua_State L)
		{
			L.ci = (L.ci.next != null ? L.ci.next : luaE_extendCI(L));
			return L.ci;
		}


		/*
		** returns true if function has been executed (C function)
		*/
		public static int luaD_precall (lua_State L, StkId func, int nresults) {
		  Closure cl;
          lua_CFunction f;
		  ptrdiff_t funcr;
		  if (!ttisfunction(func)) /* `func' is not a function? */
			func = tryfuncTM(L, func);  /* check the `function' tag method */
		  funcr = savestack(L, func);
		  if (ttislcf(func)) {  /* light C function? */
		      f = fvalue(func);  /* get it */
		      goto isCfunc;  /* go to call it */
		  }
		  cl = clvalue(func);
		  if (cl.c.isC!=0) {  /* C closure? */
		    CallInfo ci;
		    int n;
		    f = cl.c.f;
		  isCfunc:  /* call C function 'f' */
		    luaD_checkstack(L, LUA_MINSTACK);  /* ensure minimum stack size */
		    ci = next_ci(L);  /* now 'enter' new function */
		    ci.nresults = (short)nresults; //FIXME:added, (short)
		    ci.func = restorestack(L, funcr);
		    ci.top = L.top + LUA_MINSTACK;
		    lua_assert(ci.top <= L.stack_last);
		    ci.callstatus = 0;
		    if ((L.hookmask & LUA_MASKCALL)!=0)
		      luaD_hook(L, LUA_HOOKCALL, -1);
		    lua_unlock(L);
		    n = f(L);  /* do the actual call */
		    lua_lock(L);
		    api_checknelems(L, n);
		    luaD_poscall(L, L.top - n);
		    return 1;
		  }
		  else {  /* Lua function: prepare its call */
			CallInfo ci;
            int nparams, nargs;
			StkId base_;
			Proto p = cl.l.p;
			luaD_checkstack(L, p.maxstacksize);
			func = restorestack(L, funcr);
		    nargs = cast_int(L.top - func) - 1;  /* number of real arguments */
		    nparams = p.numparams;  /* number of expected parameters */
		    for (; nargs < nparams; nargs++)
		      setnilvalue(lua_TValue.inc(ref L.top));  /* complete missing arguments */
			if (p.is_vararg == 0)  /* no varargs? */
			  base_ = L.stack[func + 1];
			else  /* vararg function */
				base_ = adjust_varargs(L, p, nargs);
			ci = next_ci(L);  /* now `enter' new function */
			ci.nresults = (short)nresults; //FIXME:added, (short)
			ci.func = func;
			ci.u.l.base_ = base_;
			ci.top = base_ + p.maxstacksize;
			lua_assert(ci.top <= L.stack_last);
			ci.u.l.savedpc = new InstructionPtr(p.code, 0);  /* starting point */ //FIXME:??? //FIXME:???
            ci.callstatus = CIST_LUA;
			L.top = ci.top;
			if ((L.hookmask & LUA_MASKCALL) != 0)
			  callhook(L, ci);
			return 0;
		  }
		}


		public static int luaD_poscall (lua_State L, StkId firstResult) {
		  StkId res;
		  int wanted, i;
		  CallInfo ci = L.ci;
		  if ((L.hookmask & (LUA_MASKRET | LUA_MASKLINE)) != 0) {
			  if ((L.hookmask & LUA_MASKRET) != 0) {
		        ptrdiff_t fr = savestack(L, firstResult);  /* hook may change stack */
			    luaD_hook(L, LUA_HOOKRET, -1);
				firstResult = restorestack(L, fr);
			  }
			  L.oldpc = ci.previous.u.l.savedpc;  /* 'oldpc' for caller function */
          }
		  res = ci.func;  /* res == final position of 1st result */
		  wanted = ci.nresults;
          L.ci = ci = ci.previous;  /* back to caller */
		  /* move results to correct place */
		  for (i = wanted; i != 0 && firstResult < L.top; i--)
		  {
			  setobjs2s(L, res, firstResult);
			  res = res + 1; //FIXME:moved here, can change to origin
			  firstResult = firstResult + 1; //FIXME:moved here, can change to origin
		  }
		  while (i-- > 0)
			  setnilvalue(StkId.inc(ref res));
		  L.top = res;
		  return (wanted - LUA_MULTRET);  /* 0 iff wanted == LUA_MULTRET */
		}


		/*
		** Call a function (C or Lua). The function to be called is at *func.
		** The arguments are on the stack, right after the function.
		** When returns, all the results are on the stack, starting at the original
		** function position.
		*/ 
		private static void luaD_call (lua_State L, StkId func, int nResults, int allowyield) {
		  global_State g = G(L);
		  if (++g.nCcalls >= LUAI_MAXCCALLS) {
			if (g.nCcalls == LUAI_MAXCCALLS)
			  luaG_runerror(L, "C stack overflow");
			else if (g.nCcalls >= (LUAI_MAXCCALLS + (LUAI_MAXCCALLS>>3)))
			  luaD_throw(L, LUA_ERRERR);  /* error while handing stack error */
		  }
          if (allowyield==0) L.nny++;
		  if (luaD_precall(L, func, nResults) == 0)  /* is a Lua function? */
			luaV_execute(L);  /* call it */
          if (allowyield==0) L.nny--;
		  g.nCcalls--;
		  luaC_checkGC(L);
		}


		private static void finishCcall (lua_State L) {
		  CallInfo ci = L.ci;
		  int n;
		  lua_assert(ci.u.c.k != null);  /* must have a continuation */
		  lua_assert(L.nny == 0);
		  /* finish 'luaD_call' */
		  G(L).nCcalls--;
		  /* finish 'lua_callk' */
		  adjustresults(L, ci.nresults);
		  /* call continuation function */
		  if ((ci.callstatus & CIST_STAT) == 0)  /* no call status? */
		    ci.u.c.status = LUA_YIELD;  /* 'default' status */
		  lua_assert(ci.u.c.status != LUA_OK);
		  ci.callstatus = (byte)((((byte)(ci.callstatus & (byte)((~(CIST_YPCALL | CIST_STAT)) & 0xff) & 0xff)) | CIST_YIELDED) & 0xff); //FIXME:???
		  lua_unlock(L);
		  n = ci.u.c.k(L);
		  lua_lock(L);
          api_checknelems(L, n);
		  /* finish 'luaD_precall' */
		  luaD_poscall(L, L.top - n);
		}


		private static void unroll (lua_State L, object ud) {
          //UNUSED(ud);
		  for (;;) {
		    if (L.ci == L.base_ci[0])  /* stack is empty? */
		      return;  /* coroutine finished normally */
		    if (isLua(L.ci)==0)  /* C function? */
		      finishCcall(L);
		    else {  /* Lua function */
		      luaV_finishOp(L);  /* finish interrupted instruction */
		      luaV_execute(L);  /* execute down to higher C 'boundary' */
		    }
		  }
		}


		/*
		** check whether thread has a suspended protected call
		*/
		private static CallInfo findpcall (lua_State L) {
		  CallInfo ci;
		  for (ci = L.ci; ci != null; ci = ci.previous) {  /* search for a pcall */
		  	if ((ci.callstatus & CIST_YPCALL)!=0)
		      return ci;
		  }
		  return null;  /* no pending pcall */
		}


		private static int recover (lua_State L, int status) {
		  StkId oldtop;
		  CallInfo ci = findpcall(L);
		  if (ci == null) return 0;  /* no recovery point */
		  /* "finish" luaD_pcall */
		  oldtop = restorestack(L, ci.u.c.extra);
		  luaF_close(L, oldtop);
		  seterrorobj(L, status, oldtop);
		  L.ci = ci;
		  L.allowhook = ci.u.c.old_allowhook;
		  L.nny = 0;  /* should be zero to be yieldable */
		  luaD_shrinkstack(L);
		  L.errfunc = ci.u.c.old_errfunc;
		  ci.callstatus |= CIST_STAT;  /* call has error status */
		  ci.u.c.status = (byte)(status & 0xff);  /* (here it is) */ //FIXME:
		  return 1;  /* continue running the coroutine */
		}


		/*
		** signal an error in the call to 'resume', not in the execution of the
		** coroutine itself. (Such errors should not be handled by any coroutine
		** error handler and should not kill the coroutine.)
		*/
		private static void resume_error (lua_State L, CharPtr msg, StkId firstArg) {
		  L.top = firstArg;  /* remove args from the stack */
		  setsvalue2s(L, L.top, luaS_new(L, msg));  /* push error message */
		  incr_top(L);
		  luaD_throw(L, -1);  /* jump back to 'lua_resume' */
		}


		/*
		** do the work for 'lua_resume' in protected mode
		*/
		private static void resume (lua_State L, object ud) {
		  StkId firstArg = (StkId)(ud); //FIXME:???
		  CallInfo ci = L.ci;
		  if (G(L).nCcalls >= LUAI_MAXCCALLS)
		    resume_error(L, "C stack overflow", firstArg);
		  if (L.status == LUA_OK) {  /* may be starting a coroutine */
		    if (ci != L.base_ci)  /* not in base level? */
		      resume_error(L, "cannot resume non-suspended coroutine", firstArg);
		    /* coroutine is in base level; start running it */
		    if (luaD_precall(L, firstArg - 1, LUA_MULTRET)==0)  /* Lua function? */
		      luaV_execute(L);  /* call it */
		  }
		  else if (L.status != LUA_YIELD)
		    resume_error(L, "cannot resume dead coroutine", firstArg);
		  else {  /* resuming from previous yield */
		    L.status = LUA_OK;
		    if (isLua(ci) != 0)  /* yielded inside a hook? */
		      luaV_execute(L);  /* just continue running Lua code */
		    else {  /* 'common' yield */
              ci.func = restorestack(L, ci.u.c.extra);
		      if (ci.u.c.k != null) {  /* does it have a continuation? */
		        int n;
		        ci.u.c.status = LUA_YIELD;  /* 'default' status */
		        ci.callstatus |= CIST_YIELDED;
		        lua_unlock(L);
		        n = ci.u.c.k(L);  /* call continuation */
		        lua_lock(L);
		        api_checknelems(L, n);
		        firstArg = L.top - n;  /* yield results come from continuation */
		      }
		      G(L).nCcalls--;  /* finish 'luaD_call' */
		      luaD_poscall(L, firstArg);  /* finish 'luaD_precall' */
		    }
		    unroll(L, null);
		  }
		}


		public static int lua_resume (lua_State L, int nargs) {
		  int status;
		  lua_lock(L);
		  luai_userstateresume(L, nargs);
          ++G(L).nCcalls;  /* count resume */
		  L.nny = 0;  /* allow yields */
          api_checknelems(L, (L.status == LUA_OK) ? nargs + 1 : nargs);
		  status = luaD_rawrunprotected(L, resume, L.top - nargs);
		  if (status == -1)  /* error calling 'lua_resume'? */
		    status = LUA_ERRRUN;
		  else {  /* yield or regular error */
			  while (status != LUA_OK && status != LUA_YIELD) {  /* error? */
			    if (recover(L, status) != 0)  /* recover point? */
			      status = luaD_rawrunprotected(L, unroll, null);  /* run continuation */
			    else {  /* unrecoverable error */
			      L.status = cast_byte(status);  /* mark thread as `dead' */
			      seterrorobj(L, status, L.top);
			      L.ci.top = L.top;
			      break;
			    }
			  }
			  lua_assert(status == L.status);
		  }
          L.nny = 1;  /* do not allow yields */
		  --G(L).nCcalls;
		  lua_unlock(L);
		  return status;
		}


		public static int lua_yieldk (lua_State L, int nresults, int ctx, lua_CFunction k) {
		  CallInfo ci = L.ci;
		  luai_userstateyield(L, nresults);
		  lua_lock(L);
		  api_checknelems(L, nresults);
		  if (L.nny > 0) {
		    if (L != G(L).mainthread)
		      luaG_runerror(L, "attempt to yield across metamethod/C-call boundary");
		    else
		      luaG_runerror(L, "attempt to yield from outside a coroutine");
		  }
		  L.status = LUA_YIELD;
		  if (isLua(ci) != 0) {  /* inside a hook? */
		    api_check(L, k == null, "hooks cannot continue after yielding");
		  }
		  else {
		    if ((ci.u.c.k = k) != null)  /* is there a continuation? */
		      ci.u.c.ctx = ctx;  /* save context */
		    ci.u.c.extra = savestack(L, ci.func);  /* save current 'func' */
		    ci.func = L.top - nresults - 1;  /* protect stack below results */
		    luaD_throw(L, LUA_YIELD);
		  }
		  lua_assert(ci.callstatus & CIST_HOOKED);  /* must be inside a hook */
		  lua_unlock(L);
		  return 0;  /* return to 'luaD_hook' */
		}


		public static int luaD_pcall (lua_State L, Pfunc func, object u,
						ptrdiff_t old_top, ptrdiff_t ef) {
		  int status;
		  CallInfo old_ci = L.ci;
		  lu_byte old_allowhooks = L.allowhook;
          ushort old_nny = L.nny;
		  ptrdiff_t old_errfunc = L.errfunc;
		  L.errfunc = ef;
		  status = luaD_rawrunprotected(L, func, u);
		  if (status != LUA_OK) {  /* an error occurred? */
			StkId oldtop = restorestack(L, old_top);
			luaF_close(L, oldtop);  /* close possible pending closures */
			seterrorobj(L, status, oldtop);
			L.ci = old_ci;
			L.allowhook = old_allowhooks;
            L.nny = old_nny;
			luaD_shrinkstack(L);
		  }
		  L.errfunc = old_errfunc;
		  return status;
		}



		/*
		** Execute a protected parser.
		*/
		public class SParser {  /* data to `f_parser' */
		  public ZIO z;
		  public Mbuffer buff = new Mbuffer();  /* buffer to be used by the scanner */
          public Varlist varl = new Varlist();  /* list of local variables (to be used by the parser) */
		  public CharPtr name;
		};

		private static void f_parser (lua_State L, object ud) {
		  int i;
		  Proto tf;
		  Closure cl;
		  SParser p = (SParser)ud;
		  int c = luaZ_lookahead(p.z);
          tf = (c == LUA_SIGNATURE[0])
		        ? luaU_undump(L, p.z, p.buff, p.name) 
		        : luaY_parser(L, p.z, p.buff, p.varl, p.name);
		  setptvalue2s(L, L.top, tf);
		  incr_top(L);
		  cl = luaF_newLclosure(L, tf);
		  setclvalue(L, L.top - 1, cl);
		  for (i = 0; i < tf.sizeupvalues; i++)  /* initialize upvalues */
			cl.l.upvals[i] = luaF_newupval(L);
		}


		public static int luaD_protectedparser (lua_State L, ZIO z, CharPtr name) {
		  SParser p = new SParser();
		  int status;
          L.nny++;  /* cannot yield during parsing */
		  p.z = z; p.name = new CharPtr(name); //FIXME:
          p.varl.actvar = null; p.varl.nactvar = p.varl.actvarsize = 0;
		  luaZ_initbuffer(L, p.buff);
		  status = luaD_pcall(L, f_parser, p, savestack(L, L.top), L.errfunc);
		  luaZ_freebuffer(L, p.buff);
		  luaM_freearray<vardesc>(L, p.varl.actvar/*, p.varl.actvarsize*/);
		  L.nny--;
		  return status;
		}
	}
}

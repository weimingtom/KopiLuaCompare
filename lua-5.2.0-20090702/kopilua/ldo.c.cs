/*
** $Id: ldo.c,v 2.50 2008/10/30 15:39:30 roberto Exp roberto $
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
		
		public delegate void luai_jmpbuf(lua_Integer b);

		/* chain list of long jump buffers */
		public class lua_longjmp {
		  public lua_longjmp previous;
		  public luai_jmpbuf b;
		  public volatile int status;  /* error code */
		};


		public static void luaD_seterrorobj (lua_State L, int errcode, StkId oldtop) {
		  switch (errcode) {
			case LUA_ERRMEM: {
			  setsvalue2s(L, oldtop, luaS_newliteral(L, MEMERRMSG));
			  break;
			}
			case LUA_ERRERR: {
			  setsvalue2s(L, oldtop, luaS_newliteral(L, "error in error handling"));
			  break;
			}
			case LUA_ERRSYNTAX:
            case LUA_ERRGCMM:
			case LUA_ERRRUN: {
			  setobjs2s(L, oldtop, L.top-1);  /* error message on current top */
			  break;
			}
		  }
		  L.top = oldtop + 1;
		}


		private static void restore_stack_limit (lua_State L) {
		  if (L.nci >= LUAI_MAXCALLS)  /* stack overflow? */
    		luaE_freeCI(L);  /* erase all extras CIs */
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

		public static void luaD_reallocstack (lua_State L, int newsize) {
		  TValue[] oldstack = L.stack;
          int lim = L.stacksize;
		  int realsize = newsize + 1 + EXTRA_STACK;
		  lua_assert(L.stack_last == L.stacksize - EXTRA_STACK - 1);
		  luaM_reallocvector(L, ref L.stack, L.stacksize, realsize/*, TValue*/);
		  for (; lim < realsize; lim++)
		  	setnilvalue(L.stack[lim]); /* erase new segment */
		  L.stacksize = realsize;
		  L.stack_last = L.stack[newsize];
		  correctstack(L, oldstack);
		}


		public static void luaD_growstack (lua_State L, int n) {
		  if (n <= L.stacksize)  /* double size is enough? */
			luaD_reallocstack(L, 2*L.stacksize);
		  else
			luaD_reallocstack(L, L.stacksize + n);
		}


		public static void luaD_callhook (lua_State L, int event_, int line) {
		  lua_Hook hook = L.hook;
		  if ((hook!=null) && (L.allowhook!=0)) {
            CallInfo ci = L.ci;
			ptrdiff_t top = savestack(L, L.top);
			ptrdiff_t ci_top = savestack(L, ci.top);
			lua_Debug ar = new lua_Debug();
			ar.event_ = event_;
			ar.currentline = line;
			if (event_ == LUA_HOOKTAILRET)
			  ar.i_ci = null;  /* tail call; no debug information about it */
			else
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
		  LClosure cl;
		  ptrdiff_t funcr;
		  if (!ttisfunction(func)) /* `func' is not a function? */
			func = tryfuncTM(L, func);  /* check the `function' tag method */
		  funcr = savestack(L, func);
		  cl = clvalue(func).l;
		  L.ci.nresults = (short)nresults; //FIXME:???
		  if (cl.isC==0) {  /* Lua function? prepare its call */
			CallInfo ci;
            int nparams, nargs;
			StkId base_;
			Proto p = cl.p;
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
			ci.func = func;
			ci.u.l.base_ = base_;
			ci.top = base_ + p.maxstacksize;
			lua_assert(ci.top <= L.stack_last);
			ci.u.l.savedpc = new InstructionPtr(p.code, 0);  /* starting point */ //FIXME:??? //FIXME:???
		    ci.u.l.tailcalls = 0;
            ci.callstatus = CIST_LUA;
			L.top = ci.top;
			if ((L.hookmask & LUA_MASKCALL) != 0) {
			  InstructionPtr.inc(ref ci.u.l.savedpc);  /* hooks assume 'pc' is already incremented */
			  luaD_callhook(L, LUA_HOOKCALL, -1);
			  InstructionPtr.dec(ref ci.u.l.savedpc);  /* correct 'pc' */
			}
			return 0;
		  }
		  else {  /* if is a C function, call it */
			CallInfo ci;
			int n;
			luaD_checkstack(L, LUA_MINSTACK);  /* ensure minimum stack size */
			ci = next_ci(L);  /* now `enter' new function */
			ci.func = restorestack(L, funcr);
			ci.top = L.top + LUA_MINSTACK;
			lua_assert(ci.top <= L.stack_last);
            ci.callstatus = 0;
			if ((L.hookmask & LUA_MASKCALL) != 0)
			  luaD_callhook(L, LUA_HOOKCALL, -1);
			lua_unlock(L);
			n = curr_func(L).c.f(L);  /* do the actual call */
			lua_lock(L);
			luaD_poscall(L, L.top - n);
			return 1;
		  }
		}


		private static StkId callrethooks (lua_State L, StkId firstResult) {
		  ptrdiff_t fr = savestack(L, firstResult);  /* next call may change stack */
		  luaD_callhook(L, LUA_HOOKRET, -1);
		  if (isLua(L.ci) != 0) {  /* Lua function? */
			while ((L.hookmask & LUA_MASKRET) != 0 && L.ci.u.l.tailcalls-- != 0)
			  luaD_callhook(L, LUA_HOOKTAILRET, -1);  /* ret. hooks for tail calls */
		  }
		  return restorestack(L, fr);
		}


		public static int luaD_poscall (lua_State L, StkId firstResult) {
		  StkId res;
		  int wanted, i;
		  CallInfo ci = L.ci;
		  if ((L.hookmask & (LUA_MASKRET | LUA_MASKLINE)) != 0) {
			  if ((L.hookmask & LUA_MASKRET) != 0)
				firstResult = callrethooks(L, firstResult);
			  L.oldpc = ci.previous.u.l.savedpc;  /* 'oldpc' for returning function */
          }
		  res = ci.func;  /* res == final position of 1st result */
          L.ci = ci = ci.previous;  /* back to caller */
		  wanted = ci.nresults;
		  /* move results to correct place */
		  for (i = wanted; i != 0 && firstResult < L.top; i--)
		  {
			  setobjs2s(L, res, firstResult);
			  res = res + 1;
			  firstResult = firstResult + 1;
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

		private static void resume (lua_State L, object ud) {
		  StkId firstArg = (StkId)(ud); //FIXME:???
		  CallInfo ci = L.ci;
		  if (L.status == LUA_OK) {  /* start coroutine? */
		    lua_assert(ci == L.base_ci[0]);
		    if (luaD_precall(L, firstArg - 1, LUA_MULTRET)==0)  /* Lua function? */
		      luaV_execute(L);  /* call it */
		  }
		  else {  /* resuming from previous yield */
		    lua_assert(L.status == LUA_YIELD);
		    L.status = LUA_OK;
		    if (isLua(ci)!=0)  /* yielded inside a hook? */
		      luaV_execute(L);
		    else {  /* 'common' yield */
		      G(L).nCcalls--;  /* finish 'luaD_call' */
		      luaD_poscall(L, firstArg);  /* finish 'luaD_precall' */
		    }
		    unroll(L, null);
		  }
		}


		private static int resume_error (lua_State L, CharPtr msg) {
		  L.top = L.ci.func + 1;
		  setsvalue2s(L, L.top, luaS_new(L, msg));
		  incr_top(L);
		  lua_unlock(L);
		  return LUA_ERRRUN;
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
		  oldtop = restorestack(L, ci.u.c.oldtop);
		  luaF_close(L, oldtop);
		  luaD_seterrorobj(L, status, oldtop);
		  L.ci = ci;
		  L.allowhook = ci.u.c.old_allowhook;
		  L.nny = 0;  /* should be zero to be yieldable */
		  restore_stack_limit(L);
		  L.errfunc = ci.u.c.old_errfunc;
		  ci.callstatus |= CIST_STAT;  /* call has error status */
		  ci.u.c.status = (byte)(status & 0xff);  /* (here it is) */ //FIXME:
		  return 1;  /* continue running the coroutine */
		}


		public static int lua_resume (lua_State L, int nargs) {
		  int status;
		  lua_lock(L);
		  if (L.status != LUA_YIELD) {
		    if (L.status != LUA_OK)
		      return resume_error(L, "cannot resume dead coroutine");
		    else if (L.ci != L.base_ci)
		      return resume_error(L, "cannot resume non-suspended coroutine");
		  }
		  luai_userstateresume(L, nargs);
		  if (G(L).nCcalls >= LUAI_MAXCCALLS)
			return resume_error(L, "C stack overflow");
          ++G(L).nCcalls;  /* count resume */
		  L.nny = 0;  /* allow yields */
		  status = luaD_rawrunprotected(L, resume, L.top - nargs);
		  while (status != LUA_OK && status != LUA_YIELD) {  /* error? */
		    if (recover(L, status) != 0)  /* recover point? */
		      status = luaD_rawrunprotected(L, unroll, null);  /* run continuation */
		    else {  /* unrecoverable error */
		      L.status = cast_byte(status);  /* mark thread as `dead' */
		      luaD_seterrorobj(L, status, L.top);
		      L.ci.top = L.top;
		      break;
		    }
		  }
		  lua_assert(status == L.status);
          L.nny = 1;  /* do not allow yields */
		  --G(L).nCcalls;
		  lua_unlock(L);
		  return status;
		}


		public static int lua_yield (lua_State L, int nresults) {
		  luai_userstateyield(L, nresults);
		  lua_lock(L);
		  if (L.nny > 0)
		    luaG_runerror(L, "attempt to yield across metamethod/C-call boundary");
		  L.status = LUA_YIELD;
		  if (isLua(L.ci)==0) {  /* not inside a hook? */
		    L.ci.func = L.top - nresults - 1;  /* protect stack slots below ??? */
		    luaD_throw(L, LUA_YIELD);
		  }
		  lua_assert(L.ci.callstatus & CIST_HOOKED);  /* must be inside a hook */
		  lua_unlock(L);
		  return 0;  /* otherwise, return to 'luaD_callhook' */
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
			luaD_seterrorobj(L, status, oldtop);
			L.ci = old_ci;
			L.allowhook = old_allowhooks;
            L.nny = old_nny;
			restore_stack_limit(L);
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
		  public CharPtr name;
		};

		private static void f_parser (lua_State L, object ud) {
		  int i;
		  Proto tf;
		  Closure cl;
		  SParser p = (SParser)ud;
		  int c = luaZ_lookahead(p.z);
		  luaC_checkGC(L);
		  tf = (c == LUA_SIGNATURE[0]) ? luaU_undump(L, p.z, p.buff, p.name) 
		                               : luaY_parser(L, p.z, p.buff, p.name);
		  setptvalue2s(L, L.top, tf);
		  incr_top(L);
		  cl = luaF_newLclosure(L, tf.nups, hvalue(gt(L)));
		  cl.l.p = tf;
		  setclvalue(L, L.top - 1, cl);
		  for (i = 0; i < tf.nups; i++)  /* initialize eventual upvalues */
			cl.l.upvals[i] = luaF_newupval(L);
		}


		public static int luaD_protectedparser (lua_State L, ZIO z, CharPtr name) {
		  SParser p = new SParser();
		  int status;
		  p.z = z; p.name = new CharPtr(name);
		  luaZ_initbuffer(L, p.buff);
		  status = luaD_pcall(L, f_parser, p, savestack(L, L.top), L.errfunc);
		  luaZ_freebuffer(L, p.buff);
		  return status;
		}
	}
}

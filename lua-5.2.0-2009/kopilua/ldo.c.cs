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
			case LUA_ERRRUN: {
			  setobjs2s(L, oldtop, L.top-1);  /* error message on current top */
			  break;
			}
		  }
		  L.top = oldtop + 1;
		}


		private static void restore_stack_limit (lua_State L) {
			lua_assert(L.stack_last == L.stacksize - EXTRA_STACK - 1);
		  if (L.size_ci > LUAI_MAXCALLS) {  /* there was an overflow? */
			int inuse = L.ci - L.base_ci;
			if (inuse + 1 < LUAI_MAXCALLS)  /* can `undo' overflow? */
			  luaD_reallocCI(L, LUAI_MAXCALLS);
		  }
		}


		private static void resetstack (lua_State L, int status) {
		  L.ci = L.base_ci[0];
		  L.base_ = L.ci.base_;
		  luaF_close(L, L.base_);  /* close possible pending closures */
		  luaD_seterrorobj(L, status, L.base_);
		  L.allowhook = 1;
		  restore_stack_limit(L);
		  L.errfunc = 0;
		  L.errorJmp = null;
		}


		public static void luaD_throw (lua_State L, int errcode) {
		  if (L.errorJmp != null) {
			L.errorJmp.status = errcode;
			LUAI_THROW(L, L.errorJmp);
		  }
		  else {
			L.status = cast_byte(errcode);
			if (G(L).panic != null) {
			  resetstack(L, errcode);
			  lua_unlock(L);
			  G(L).panic(L);
			}
			Environment.Exit(EXIT_FAILURE);
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
		  catch
		  {
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
			/* don't need to do this
		  CallInfo ci;
		  GCObject up;
		  L.top = L.stack[L.top - oldstack];
		  for (up = L.openupval; up != null; up = up.gch.next)
			gco2uv(up).v = L.stack[gco2uv(up).v - oldstack];
		  for (ci = L.base_ci[0]; ci <= L.ci; CallInfo.inc(ref ci)) {
			  ci.top = L.stack[ci.top - oldstack];
			ci.base_ = L.stack[ci.base_ - oldstack];
			ci.func = L.stack[ci.func - oldstack];
		  }
		  L.base_ = L.stack[L.base_ - oldstack];
			 * */
		}

		public static void luaD_reallocstack (lua_State L, int newsize) {
		  TValue[] oldstack = L.stack;
		  int realsize = newsize + 1 + EXTRA_STACK;
		  lua_assert(L.stack_last == L.stacksize - EXTRA_STACK - 1);
		  luaM_reallocvector(L, ref L.stack, L.stacksize, realsize/*, TValue*/);
		  L.stacksize = realsize;
		  L.stack_last = L.stack[newsize];
		  correctstack(L, oldstack);
		}

		public static void luaD_reallocCI (lua_State L, int newsize) {
		  CallInfo oldci = L.base_ci[0];
		  luaM_reallocvector(L, ref L.base_ci, L.size_ci, newsize/*, CallInfo*/);
		  L.size_ci = newsize;
		  L.ci = L.base_ci[L.ci - oldci];
		  L.end_ci = L.base_ci[L.size_ci - 1];
		}

		public static void luaD_growstack (lua_State L, int n) {
		  if (n <= L.stacksize)  /* double size is enough? */
			luaD_reallocstack(L, 2*L.stacksize);
		  else
			luaD_reallocstack(L, L.stacksize + n);
		}

		private static CallInfo growCI (lua_State L) {
		  if (L.size_ci > LUAI_MAXCALLS)  /* overflow while handling overflow? */
			luaD_throw(L, LUA_ERRERR);
		  else {
			luaD_reallocCI(L, 2*L.size_ci);
			if (L.size_ci > LUAI_MAXCALLS)
			  luaG_runerror(L, "stack overflow");
		  }
		  CallInfo.inc(ref L.ci);
		  return L.ci;
		}


		public static void luaD_callhook (lua_State L, int event_, int line) {
		  lua_Hook hook = L.hook;
		  if ((hook!=null) && (L.allowhook!=0)) {
			ptrdiff_t top = savestack(L, L.top);
			ptrdiff_t ci_top = savestack(L, L.ci.top);
			lua_Debug ar = new lua_Debug();
			ar.event_ = event_;
			ar.currentline = line;
			if (event_ == LUA_HOOKTAILRET)
			  ar.i_ci = 0;  /* tail call; no debug information about it */
			else
			  ar.i_ci = L.ci - L.base_ci;
			luaD_checkstack(L, LUA_MINSTACK);  /* ensure minimum stack size */
			L.ci.top = L.top + LUA_MINSTACK;
			lua_assert(L.ci.top <= L.stack_last);
			L.allowhook = 0;  /* cannot call hooks inside a hook */
            L.ci.callstatus |= CIST_HOOKED;
			lua_unlock(L);
			hook(L, ar);
			lua_lock(L);
			lua_assert(L.allowhook==0);
			L.allowhook = 1;
			L.ci.top = restorestack(L, ci_top);
			L.top = restorestack(L, top);
			L.ci.callstatus &= (byte)((~CIST_HOOKED) & 0xff);
		  }
		}


		private static StkId adjust_varargs (lua_State L, Proto p, int actual) {
		  int i;
		  int nfixargs = p.numparams;
		  Table htab = null;
		  StkId base_, fixed_;
		  for (; actual < nfixargs; ++actual)
			  setnilvalue(StkId.inc(ref L.top));
		#if LUA_COMPAT_VARARG
		  if ((p.is_vararg & VARARG_NEEDSARG) != 0) { /* compat. with old-style vararg? */
			int nvar = actual - nfixargs;  /* number of extra arguments */
			lua_assert(p.is_vararg & VARARG_HASARG);
			luaC_checkGC(L);
			htab = luaH_new(L);  /* create `arg' table */
			sethvalue(L, StkId.inc(ref L.top), htab);
			for (i=0; i<nvar; i++)  /* put extra arguments into `arg' table */
			  setobj2n(L, luaH_setnum(L, htab, i+1), L.top - nvar + i);
			/* store counter in field `n' */
			setnvalue(luaH_setstr(L, htab, luaS_newliteral(L, "n")), cast_num(nvar));
			StkId.dec(ref L.top);
		  }
		#endif
		  /* move fixed parameters to final position */
		  fixed_ = L.top - actual;  /* first fixed argument */
		  base_ = L.top;  /* final position of first argument */
		  for (i=0; i<nfixargs; i++) {
			setobjs2s(L, StkId.inc(ref L.top), fixed_ + i);
			setnilvalue(fixed_ + i);
		  }
		  /* add `arg' parameter */
		  if (htab!=null) {
			StkId top = L.top;
			StkId.inc(ref L.top);
			sethvalue(L, top, htab);
			lua_assert(iswhite(obj2gco(htab)));
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



		public static CallInfo inc_ci(lua_State L)
		{
			if (L.ci == L.end_ci) return growCI(L);
			//   (condhardstacktests(luaD_reallocCI(L, L.size_ci)), ++L.ci))
			CallInfo.inc(ref L.ci);
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
		  L.ci.savedpc = InstructionPtr.Assign(L.savedpc);
		  if (cl.isC==0) {  /* Lua function? prepare its call */
			CallInfo ci;
			StkId st, base_;
			Proto p = cl.p;
			luaD_checkstack(L, p.maxstacksize);
			func = restorestack(L, funcr);
			if (p.is_vararg == 0)  /* no varargs? */
			  base_ = L.stack[func + 1];
			else {  /* vararg function */
				int nargs = L.top - func - 1;
				base_ = adjust_varargs(L, p, nargs);
				func = restorestack(L, funcr);  /* previous call may change the stack */
			}
			ci = inc_ci(L);  /* now `enter' new function */
			ci.func = func;
			L.base_ = ci.base_ = base_;
			ci.top = L.base_ + p.maxstacksize;
			lua_assert(ci.top <= L.stack_last);
			L.savedpc = new InstructionPtr(p.code, 0);  /* starting point */
			ci.tailcalls = 0;
            ci.callstatus = CIST_LUA;
            ci.nresults = (short)nresults; //FIXME:???(short)
			for (st = L.top; st < ci.top; StkId.inc(ref st))
				setnilvalue(st);
			L.top = ci.top;
			if ((L.hookmask & LUA_MASKCALL) != 0) {
			  InstructionPtr.inc(ref L.savedpc);  /* hooks assume 'pc' is already incremented */
			  luaD_callhook(L, LUA_HOOKCALL, -1);
			  InstructionPtr.dec(ref L.savedpc);  /* correct 'pc' */
			}
			return 0;
		  }
		  else {  /* if is a C function, call it */
			CallInfo ci;
			int n;
			luaD_checkstack(L, LUA_MINSTACK);  /* ensure minimum stack size */
			ci = inc_ci(L);  /* now `enter' new function */
			ci.func = restorestack(L, funcr);
			L.base_ = ci.base_ = ci.func + 1;
			ci.top = L.top + LUA_MINSTACK;
			lua_assert(ci.top <= L.stack_last);
			ci.nresults = (short)nresults; //FIXME:???(short)
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
			while ((L.hookmask & LUA_MASKRET) != 0 && L.ci.tailcalls-- != 0) /* tail calls */
			  luaD_callhook(L, LUA_HOOKTAILRET, -1);
		  }
		  return restorestack(L, fr);
		}


		public static int luaD_poscall (lua_State L, StkId firstResult) {
		  StkId res;
		  int wanted, i;
		  CallInfo ci;
		  if ((L.hookmask & (LUA_MASKRET | LUA_MASKLINE)) != 0) {
			  if ((L.hookmask & LUA_MASKRET) != 0)
				firstResult = callrethooks(L, firstResult);
			  ci = CallInfo.dec(ref L.ci);
          }
		  ci = CallInfo.dec(ref L.ci);
		  res = ci.func;  /* res == final position of 1st result */
		  wanted = ci.nresults;
		  L.base_ = (ci - 1).base_;  /* restore base */
		  L.savedpc = InstructionPtr.Assign((ci - 1).savedpc);  /* restore savedpc */
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
		private static void luaD_call (lua_State L, StkId func, int nResults) {
		  global_State g = G(L);
		  if (++g.nCcalls >= LUAI_MAXCCALLS) {
			if (g.nCcalls == LUAI_MAXCCALLS)
			  luaG_runerror(L, "C stack overflow");
			else if (g.nCcalls >= (LUAI_MAXCCALLS + (LUAI_MAXCCALLS>>3)))
			  luaD_throw(L, LUA_ERRERR);  /* error while handing stack error */
		  }
		  if (luaD_precall(L, func, nResults) == 0)  /* is a Lua function? */
			luaV_execute(L);  /* call it */
		  g.nCcalls--;
		  luaC_checkGC(L);
		}

		private static void unroll (lua_State L) {
		  for (;;) {
		    Instruction inst;
		    luaV_execute(L);  /* execute down to higher C 'boundary' */
		    if (L.ci == L.base_ci[0]) {  /* stack is empty? */ //FIXME:???==
		      lua_assert(L.baseCcalls == G(L).nCcalls);
		      return;  /* coroutine finished normally */
		    }
		    L.baseCcalls--;  /* undo increment that allows yields */
		    inst = L.savedpc[-1];  /* interrupted instruction */
		    switch (GET_OPCODE(inst)) {  /* finish its execution */
		      case OpCode.OP_ADD: case OpCode.OP_SUB: case OpCode.OP_MUL: case OpCode.OP_DIV:
		      case OpCode.OP_MOD: case OpCode.OP_POW: case OpCode.OP_UNM: case OpCode.OP_LEN:
		      case OpCode.OP_GETGLOBAL: case OpCode.OP_GETTABLE: case OpCode.OP_SELF: {
		        setobjs2s(L, L.base_ + GETARG_A(inst), StkId.dec(ref L.top)); //FIXME:--
		        break;
		      }
		      case OpCode.OP_LE: case OpCode.OP_LT: case OpCode.OP_EQ: {
		    	int res = l_isfalse(L.top - 1) == 0 ? 1 : 0;
		    	StkId.dec(ref L.top); //FIXME:--
		        /* metamethod should not be called when operand is K */
		        lua_assert(ISK(GETARG_B(inst))==0);
		        if (GET_OPCODE(inst) == OpCode.OP_LE &&  /* "<=" using "<" instead? */
		            ttisnil(luaT_gettmbyobj(L, L.base_ + GETARG_B(inst), TMS.TM_LE)))
		          res = (res == 0 ? 1 : 0);  /* invert result */
		        lua_assert(GET_OPCODE(L.savedpc[0]) == OpCode.OP_JMP);
		        if (res != GETARG_A(inst))  /* condition failed? */
		          InstructionPtr.inc(ref L.savedpc);  /* skip jump instruction */ //FIXME:++
		        break;
		      }
		      case OpCode.OP_CONCAT: {
		        StkId top = L.top - 1;  /* top when __concat was called */
		        int last = cast_int(top - L.base_) - 2;  /* last element and ... */
		        int b = GETARG_B(inst);      /* ... first element to concatenate */
		        int total = last - b + 1;  /* number of elements to concatenate */
		        setobj2s(L, top - 2, top);  /* put TM result in proper position */
		        L.top = L.ci.top;  /* correct top */
		        if (total > 1)  /* are there elements to concat? */
		          luaV_concat(L, total, last);  /* concat them (may yield again) */
		        /* move final result to final position */
		        setobj2s(L, L.base_ + GETARG_A(inst), L.base_ + b);
		        continue;
		      }
		      case OpCode.OP_TFORCALL: {
		        lua_assert(GET_OPCODE(L.savedpc[0]) == OpCode.OP_TFORLOOP);
		        L.top = L.ci.top;  /* correct top */
		        break;
		      }
		      case OpCode.OP_SETGLOBAL: case OpCode.OP_SETTABLE:
		        break;  /* nothing to be done */
		      default: lua_assert(0);
		        break;//FIXME:
		    }
		  }
		}

		private static void resume (lua_State L, object ud) {
		  StkId firstArg = (StkId)ud;
		  CallInfo ci = L.ci;
		  if (L.status == LUA_OK) {  /* start coroutine? */
			lua_assert(ci == L.base_ci[0] && firstArg > L.base_);
			if (luaD_precall(L, firstArg - 1, LUA_MULTRET) != 0)  /* C function? */
			  return;  /* done */
		  }
		  else {  /* resuming from previous yield */
			lua_assert(L.status == LUA_YIELD);
			L.status = LUA_OK;
			if (isLua(ci) == 1)  /* yielded inside a hook? */
		      L.base_ = L.ci.base_;  /* just continue its execution */
		    else {  /* 'common' yield */
			  /* finish interrupted execution of `OP_CALL' */
			  lua_assert(GET_OPCODE((ci-1).savedpc[-1]) == OpCode.OP_CALL ||
						 GET_OPCODE((ci-1).savedpc[-1]) == OpCode.OP_TAILCALL);
				if (luaD_poscall(L, firstArg) != 0)  /* complete it... */
				L.top = L.ci.top;  /* and correct top if not multiple results */
			}
		  }
		  unroll(L);
		}


		private static int resume_error (lua_State L, CharPtr msg) {
		  L.top = L.ci.base_;
		  setsvalue2s(L, L.top, luaS_new(L, msg));
		  incr_top(L);
		  lua_unlock(L);
		  return LUA_ERRRUN;
		}


		public static int lua_resume (lua_State L, int nargs) {
		  int status;
		  lua_lock(L);
		  if (L.status != LUA_YIELD) {
		    if (L.status != LUA_OK)
		      return resume_error(L, "cannot resume dead coroutine");
		    else if (L.ci != L.base_ci[0])
		      return resume_error(L, "cannot resume non-suspended coroutine");
		  }
		  luai_userstateresume(L, nargs);
		  lua_assert(L.errfunc == 0);
		  if (G(L).nCcalls >= LUAI_MAXCCALLS)
			return resume_error(L, "C stack overflow");
          ++G(L).nCcalls;  /* count resume */
		  L.baseCcalls += G(L).nCcalls;
		  status = luaD_rawrunprotected(L, resume, L.top - nargs);
		  if (status != LUA_OK && status != LUA_YIELD) {  /* error? */
			L.status = cast_byte(status);  /* mark thread as `dead' */
			luaD_seterrorobj(L, status, L.top);
			L.ci.top = L.top;
		  }
		  else {
			lua_assert(status == L.status);
		  }
		  L.baseCcalls -= G(L).nCcalls;
		  --G(L).nCcalls;
		  lua_unlock(L);
		  return status;
		}


		public static int lua_yield (lua_State L, int nresults) {
		  luai_userstateyield(L, nresults);
		  lua_lock(L);
		  if (G(L).nCcalls > L.baseCcalls)
			luaG_runerror(L, "attempt to yield across metamethod/C-call boundary");
		  L.base_ = L.top - nresults;  /* protect stack slots below */
		  L.status = LUA_YIELD;
		  if (isLua(L.ci) == 0)  /* not inside a hook? */
		    luaD_throw(L, LUA_YIELD);
		  lua_assert(L.ci.callstatus & CIST_HOOKED);  /* must be inside a hook */
		  lua_unlock(L);
		  return 0;  /* otherwise, return to 'luaD_callhook' */
		}


		public static int luaD_pcall (lua_State L, Pfunc func, object u,
						ptrdiff_t old_top, ptrdiff_t ef) {
		  int status;
		  ptrdiff_t old_ci = saveci(L, L.ci);
		  lu_byte old_allowhooks = L.allowhook;
		  ptrdiff_t old_errfunc = L.errfunc;
		  L.errfunc = ef;
		  status = luaD_rawrunprotected(L, func, u);
		  if (status != LUA_OK) {  /* an error occurred? */
			StkId oldtop = restorestack(L, old_top);
			luaF_close(L, oldtop);  /* close possible pending closures */
			luaD_seterrorobj(L, status, oldtop);
			L.ci = restoreci(L, old_ci);
			L.base_ = L.ci.base_;
			L.savedpc = InstructionPtr.Assign(L.ci.savedpc);
			L.allowhook = old_allowhooks;
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

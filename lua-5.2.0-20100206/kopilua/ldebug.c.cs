/*
** $Id: ldebug.c,v 2.62 2010/01/11 17:37:59 roberto Exp roberto $
** Debug Interface
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using Instruction = System.UInt32;

	public partial class Lua
	{



		private static int currentpc (CallInfo ci) {
		  lua_assert(isLua(ci))
		  return pcRel(ci.u.l.savedpc, ci_func(ci).l.p);
		}


		private static int currentline (CallInfo ci) {
		  return getfuncline(ci_func(ci).l.p, currentpc(ci));
		}


		/*
		** this function can be called asynchronous (e.g. during a signal)
		*/
		public static int lua_sethook (lua_State L, lua_Hook func, int mask, int count) {
		  if (func == null || mask == 0) {  /* turn off hooks? */
			mask = 0;
			func = null;
		  }
		  if (isLua(L.ci))
		    L.oldpc = L.ci.u.l.savedpc;
		  L.hook = func;
		  L.basehookcount = count;
		  resethookcount(L);
		  L.hookmask = cast_byte(mask);
		  return 1;
		}


		public static lua_Hook lua_gethook (lua_State L) {
		  return L.hook;
		}


		public static int lua_gethookmask (lua_State L) {
		  return L.hookmask;
		}


		public static int lua_gethookcount (lua_State L) {
		  return L.basehookcount;
		}


		public static int lua_getstack (lua_State L, int level, lua_Debug ar) {
		  int status;
		  CallInfo ci;
          if (level < 0) return 0;  /* invalid (negative) level */
		  lua_lock(L);
		  for (ci = L.ci; level > 0 && ci != L.base_ci[0]; ci = ci.previous)
			level--;
		  if (level == 0 && ci != L.base_ci[0]) {  /* level found? */
			status = 1;
			ar.i_ci = ci;
		  }
		  else status = 0;  /* no such level */
		  lua_unlock(L);
		  return status;
		}


		private static CharPtr findlocal (lua_State L, CallInfo ci, int n,
		                              ref StkId pos) {
		  CharPtr name = null;
		  StkId base_;
		  if (isLua(ci) != 0) {
		    base_ = ci.u.l.base_;
		    name = luaF_getlocalname(ci_func(ci).l.p, n, currentpc(ci));
		  }
		  else
		    base_ = ci.func + 1;
		  if (name == null) {  /* no 'standard' name? */
		    StkId limit = (ci == L.ci) ? L.top : ci.next.func;
		    if (limit - base_ >= n && n > 0)  /* is 'n' inside 'ci' stack? */
		      name = "(*temporary)";  /* generic name for any valid slot */
		    else {
		      pos = base_;  /* to avoid warnings */
		      return null;  /* no name */
		    }
		  }
		  pos = base_ + (n - 1);
		  return name;
		}


		public static CharPtr lua_getlocal (lua_State L, lua_Debug ar, int n) {
		  StkId pos = new StkId();
		  CharPtr name = findlocal(L, ar.i_ci, n, ref pos);
		  lua_lock(L);
		  if (name != null) {
			  setobj2s(L, L.top, pos);
		      api_incr_top(L);
          }
		  lua_unlock(L);
		  return name;
		}


		public static CharPtr lua_setlocal (lua_State L, lua_Debug ar, int n) {
		  StkId pos = new StkId();
		  CharPtr name = findlocal(L, ar.i_ci, n, ref pos);
		  lua_lock(L);
		  if (name != null)
			  setobjs2s(L, pos, L.top-1);
		  StkId.dec(ref L.top);  /* pop value */
		  lua_unlock(L);
		  return name;
		}


		private static void funcinfo (lua_Debug ar, Closure cl) {
		  if (cl.c.isC != 0) {
			ar.source = "=[C]";
			ar.linedefined = -1;
			ar.lastlinedefined = -1;
			ar.what = "C";
		  }
		  else {
			ar.source = getstr(cl.l.p.source);
			ar.linedefined = cl.l.p.linedefined;
			ar.lastlinedefined = cl.l.p.lastlinedefined;
			ar.what = (ar.linedefined == 0) ? "main" : "Lua";
		  }
		  luaO_chunkid(ar.short_src, ar.source, LUA_IDSIZE);
		}


		private static void collectvalidlines (lua_State L, Closure f) {
		  if (f == null || (f.c.isC!=0)) {
			setnilvalue(L.top);
            incr_top(L);
		  }
		  else {
			int i;
			int[] lineinfo = f.l.p.lineinfo;
		    Table t = luaH_new(L);
		    sethvalue(L, L.top, t);
		    incr_top(L);
			for (i=0; i<f.l.p.sizelineinfo; i++)
			  setbvalue(luaH_setint(L, t, lineinfo[i]), 1);
		  }
		}


		private static int auxgetinfo (lua_State L, CharPtr what, lua_Debug ar,
							Closure f, CallInfo ci) {
		  int status = 1;
		  for (; what[0] != 0; what = what.next()) {
			switch (what[0]) {
			  case 'S': {
				funcinfo(ar, f);
				break;
			  }
			  case 'l': {
				ar.currentline = (ci != null && isLua(ci)) ? currentline(ci) : -1;
				break;
			  }
			  case 'u': {
				ar.nups = f.c.nupvalues;
		        if (f.c.isC) {
		          ar.isvararg = 1;
		          ar.nparams = 0;
		        }
		        else {
		          ar.isvararg = f.l.p.is_vararg;
		          ar.nparams = f.l.p->numparams;
		        }
		        break;
		      }
		      case 't': {
		        ar.istailcall = (ci) ? ci.callstatus & CIST_TAIL : 0;
				break;
			  }
			  case 'n': {
				ar.namewhat = (ci!=null) ? getfuncname(L, ci, ref ar.name) : null;
				if (ar.namewhat == null) {
				  ar.namewhat = "";  /* not found */
				  ar.name = null;
				}
				break;
			  }
			  case 'L':
			  case 'f':  /* handled by lua_getinfo */
				break;
			  default: status = 0;  break;/* invalid option */
			}
		  }
		  return status;
		}


		public static int lua_getinfo (lua_State L, CharPtr what, lua_Debug ar) {
		  int status;
		  Closure f = null;
		  CallInfo ci = null;
		  lua_lock(L);
		  if (what == '>') {
			StkId func = L.top - 1;
			luai_apicheck(L, ttisfunction(func));
			what = what.next();  /* skip the '>' */
			f = clvalue(func);
			StkId.dec(ref L.top);  /* pop function */
		  }
		  else {
			ci = ar.i_ci;
			lua_assert(ttisfunction(ci.func));
			f = clvalue(ci.func);
		  }
		  status = auxgetinfo(L, what, ar, f, ci);
		  if (strchr(what, 'f') != null) {
			setclvalue(L, L.top, f);
			incr_top(L);
		  }
		  if (strchr(what, 'L') != null)
			collectvalidlines(L, f);
		  lua_unlock(L);
		  return status;
		}


		/*
		** {======================================================
		** Symbolic Execution
		** =======================================================
		*/

		private static CharPtr kname (Proto p, int c) {
		  if (ISK(c) != 0 && ttisstring(p.k[INDEXK(c)]))
		    return svalue(p.k[INDEXK(c)]);
		  else
		    return "?";
		}


		private static CharPtr getobjname (lua_State L, CallInfo ci, int reg,
		                               ref CharPtr name) {
		  Proto p;
		  int lastpc, pc;
		  CharPtr what = null;
		  lua_assert(isLua(ci));
		  p = ci_func(ci).l.p;
		  lastpc = currentpc(ci);
		  name = luaF_getlocalname(p, reg + 1, lastpc);
		  if (name != null)  /* is a local? */
		    return "local";
		  /* else try symbolic execution */
		  for (pc = 0; pc < lastpc; pc++) {
		    Instruction i = p.code[pc];
		    OpCode op = GET_OPCODE(i);
		    int a = GETARG_A(i);
		    switch (op) {
		      case OpCode.OP_GETGLOBAL: {
		        if (reg == a) {
		          int g = GETARG_Bx(i);
		          if (g != 0) g--;
		          else g = GETARG_Ax(p->code[++pc]);
		          lua_assert(ttisstring(p.k[g]));
		          name = svalue(p.k[g]);
		          what = "global";
		        }
		        break;
		      }
		      case OpCode.OP_MOVE: {
		        if (reg == a) {
		          int b = GETARG_B(i);  /* move from 'b' to 'a' */
		          if (b < a)
		            what = getobjname(L, ci, b, ref name);  /* get name for 'b' */
		          else what = null;
		        }
		        break;
		      }
		      case OpCode.OP_GETTABLE: {
		        if (reg == a) {
		          int k = GETARG_C(i);  /* key index */
		          name = kname(p, k);
		          what = "field";
		        }
		        break;
		      }
		      case OpCode.OP_GETUPVAL: {
		        if (reg == a) {
		          int u = GETARG_B(i);  /* upvalue index */
		          TString tn = p.upvalues[u].name;
		          name = tn ? getstr(tn) : "?";
		          what = "upvalue";
		        }
		        break;
		      }
		      case OpCode.OP_LOADNIL: {
		        int b = GETARG_B(i);  /* move from 'b' to 'a' */
		        if (a <= reg && reg <= b)  /* set registers from 'a' to 'b' */
		          what = null;
		        break;
		      }
		      case OpCode.OP_SELF: {
		        if (reg == a) {
		          int k = GETARG_C(i);  /* key index */
		          name = kname(p, k);
		          what = "method";
		        }
		        break;
		      }
		      case OpCode.OP_TFORCALL: {
		        if (reg >= a + 2) what = null;  /* affect all regs above its base */
		        break;
		      }
		      case OpCode.OP_CALL:
		      case OpCode.OP_TAILCALL: {
		        if (reg >= a) what = null;  /* affect all registers above base */
		        break;
		      }
		      case OpCode.OP_JMP: {
		        int b = GETARG_sBx(i);
		        int dest = pc + 1 + b;
		        /* jump is forward and do not skip `lastpc'? */
		        if (pc < dest && dest <= lastpc)
		          pc += b;  /* do the jump */
		        break;
		      }
		      default:
		        if (testAMode(op) != 0 && reg == a) what = null;
		        break;
		    }
		  }
		  return what;
		}


		private static CharPtr getfuncname (lua_State L, CallInfo ci, ref CharPtr name) {
          TMS tm = 0;
		  Instruction i;
		  if ((ci.callstatus & CIST_TAIL)!=0 || isLua(ci.previous)==0)
			return null;  /* calling function is not Lua (or is unknown) */
		  ci = ci.previous;  /* calling function */
		  i = ci_func(ci).l.p.code[currentpc(ci)];
		  if (GET_OPCODE(i) == OP_EXTRAARG)  /* extra argument? */
		    i = ci_func(ci).l.p.code[currentpc(ci) - 1];  /* get 'real' instruction */
		  switch (GET_OPCODE(i)) {
		    case OpCode.OP_CALL:
		    case OpCode.OP_TAILCALL:
		    case OpCode.OP_TFORLOOP:
		      return getobjname(L, ci, GETARG_A(i), ref name);
		    case OpCode.OP_GETGLOBAL:
		    case OpCode.OP_SELF:
		    case OpCode.OP_GETTABLE: tm = TMS.TM_INDEX; break;
		    case OpCode.OP_SETGLOBAL:
		    case OpCode.OP_SETTABLE: tm = TMS.TM_NEWINDEX; break;
		    case OpCode.OP_EQ: tm = TMS.TM_EQ; break;
		    case OpCode.OP_ADD: tm = TMS.TM_ADD; break;
		    case OpCode.OP_SUB: tm = TMS.TM_SUB; break;
		    case OpCode.OP_MUL: tm = TMS.TM_MUL; break;
		    case OpCode.OP_DIV: tm = TMS.TM_DIV; break;
		    case OpCode.OP_MOD: tm = TMS.TM_MOD; break;
		    case OpCode.OP_POW: tm = TMS.TM_POW; break;
		    case OpCode.OP_UNM: tm = TMS.TM_UNM; break;
		    case OpCode.OP_LEN: tm = TMS.TM_LEN; break;
		    case OpCode.OP_LT: tm = TMS.TM_LT; break;
		    case OpCode.OP_LE: tm = TMS.TM_LE; break;
		    case OpCode.OP_CONCAT: tm = TMS.TM_CONCAT; break;
		    default:
		      return null;  /* else no useful name can be found */
		  }
		  name = getstr(G(L).tmname[(int)tm]); //FIXME:(int)
		  return "metamethod";
		}

		/* }====================================================== */



		/* only ANSI way to check whether a pointer points to an array */
		private static int isinstack (CallInfo ci, TValue o) {
		  StkId p;
		  for (p = ci.u.l.base_; p < ci.top; StkId.inc(ref p))
			if (o == p) return 1;
		  return 0;
		}


		public static void luaG_typeerror (lua_State L, TValue o, CharPtr op) {
          CallInfo ci = L.ci;
		  CharPtr name = null;
		  CharPtr t = typename(ttype(o));
		  CharPtr kind = (isLua(ci) != 0 && isinstack(ci, o) != 0) ?
								 getobjname(L, ci, cast_int(o - ci.u.l.base_), ref name) :
								 null;
		  if (kind != null)
			luaG_runerror(L, "attempt to %s %s " + LUA_QS + " (a %s value)",
						op, kind, name, t);
		  else
			luaG_runerror(L, "attempt to %s a %s value", op, t);
		}


		public static void luaG_concaterror (lua_State L, StkId p1, StkId p2) {
		  if (ttisstring(p1) || ttisnumber(p1)) p1 = p2;
		  lua_assert(!ttisstring(p1) && !ttisnumber(p2));
		  luaG_typeerror(L, p1, "concatenate");
		}


		public static void luaG_aritherror (lua_State L, TValue p1, TValue p2) {
		  TValue temp = new TValue();
		  if (luaV_tonumber(p1, temp) == null)
			p2 = p1;  /* first operand is wrong */
		  luaG_typeerror(L, p2, "perform arithmetic on");
		}


		public static int luaG_ordererror (lua_State L, TValue p1, TValue p2) {
		  CharPtr t1 = typename(ttype(p1));
		  CharPtr t2 = typename(ttype(p2));
		  if (t1 == t2) //FIXME:???
			luaG_runerror(L, "attempt to compare two %s values", t1);
		  else
			luaG_runerror(L, "attempt to compare %s with %s", t1, t2);
		  return 0;
		}


		private static void addinfo (lua_State L, CharPtr msg) {
		  CallInfo ci = L.ci;
		  if (isLua(ci) != 0) {  /* is Lua code? */
			CharPtr buff = new CharPtr(new char[LUA_IDSIZE]);  /* add file:line information */
			int line = currentline(ci);
			luaO_chunkid(buff, getstr(ci_func(ci).l.p.source), LUA_IDSIZE);
			luaO_pushfstring(L, "%s:%d: %s", buff, line, msg);
		  }
		}


		public static void luaG_errormsg (lua_State L) {
		  if (L.errfunc != 0) {  /* is there an error handling function? */
			StkId errfunc = restorestack(L, L.errfunc);
			if (!ttisfunction(errfunc)) luaD_throw(L, LUA_ERRERR);
			setobjs2s(L, L.top, L.top - 1);  /* move argument */
			setobjs2s(L, L.top - 1, errfunc);  /* push function */
			incr_top(L);
			luaD_call(L, L.top - 2, 1, 0);  /* call it */
		  }
		  luaD_throw(L, LUA_ERRRUN);
		}

		public static void luaG_runerror(lua_State L, CharPtr fmt, params object[] argp)
		{
			addinfo(L, luaO_pushvfstring(L, fmt, argp));
			luaG_errormsg(L);
		}

	}
}

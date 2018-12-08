/*
** $Id: lvm.c,v 2.93 2009/06/17 17:50:09 roberto Exp roberto $
** Lua virtual machine
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
	using lua_Number = System.Double;
	using lu_byte = System.Byte;
	using ptrdiff_t = System.Int32;
	using Instruction = System.UInt32;

	public partial class Lua
	{



		/* limit for table tag-method chains (to avoid loops) */
		public const int MAXTAGLOOP	= 100;


		public static TValue luaV_tonumber (TValue obj, TValue n) {
		  lua_Number num;
		  if (ttisnumber(obj)) return obj;
		  if (ttisstring(obj) && (luaO_str2d(svalue(obj), out num)!=0)) {
			setnvalue(n, num);
			return n;
		  }
		  else
			return null;
		}


		public static int luaV_tostring (lua_State L, StkId obj) {
		  if (!ttisnumber(obj))
			return 0;
		  else {
			lua_Number n = nvalue(obj);
			CharPtr s = lua_number2str(n);
			setsvalue2s(L, obj, luaS_new(L, s));
			return 1;
		  }
		}


		private static void traceexec (lua_State L) {
          CallInfo ci = L.ci;
		  lu_byte mask = L.hookmask;
		  if (((mask & LUA_MASKCOUNT) != 0) && (L.hookcount == 0)) {
			resethookcount(L);
			luaD_callhook(L, LUA_HOOKCOUNT, -1);
		  }
		  if ((mask & LUA_MASKLINE) != 0) {
			Proto p = ci_func(ci).l.p;
			int npc = pcRel(ci.u.l.savedpc, p);
			int newline = getfuncline(p, npc);
		    if (npc == 0 ||  /* call linehook when enter a new function, */
		        ci.u.l.savedpc <= L.oldpc ||  /* when jump back (loop), or when */
		        newline != getfuncline(p, pcRel(L.oldpc, p)))  /* enter a new line */
			  luaD_callhook(L, LUA_HOOKLINE, newline);
		  }
          L.oldpc = ci.u.l.savedpc;
		}





		private static void callTM (lua_State L, TValue f, TValue p1,
							TValue p2, TValue p3, int hasres) {
		  ptrdiff_t result = savestack(L, p3);
		  setobj2s(L, L.top, f); StkId.inc(ref L.top);  /* push function */ //FIXME:++
		  setobj2s(L, L.top, p1); StkId.inc(ref L.top);  /* 1st argument */ //FIXME:++
		  setobj2s(L, L.top, p2); StkId.inc(ref L.top);  /* 2nd argument */ //FIXME:++
		  if (hasres == 0)  /* no result? 'p3' is third argument */ //FIXME:++
		    setobj2s(L, L.top, p3); StkId.inc(ref L.top); /* 3th argument */ //FIXME:++
		  luaD_checkstack(L, 0);
		  /* metamethod may yield only when called from Lua code */
  		  luaD_call(L, L.top - (4 - hasres), hasres, isLua(L.ci));
		  if (hasres != 0) {  /* if has result, move it to its place */
		    p3 = restorestack(L, result);
		    setobjs2s(L, p3, StkId.dec(ref L.top)); //FIXME:--
		  }
		}


		public static void luaV_gettable (lua_State L, TValue t, TValue key, StkId val) {
		  int loop;
		  for (loop = 0; loop < MAXTAGLOOP; loop++) {
			TValue tm;
			if (ttistable(t)) {  /* `t' is a table? */
			  Table h = hvalue(t);
			  TValue res = luaH_get(h, key); /* do a primitive get */
			  if (!ttisnil(res) ||  /* result is no nil? */
				  (tm = fasttm(L, h.metatable, TMS.TM_INDEX)) == null) { /* or no TM? */
				setobj2s(L, val, res);
				return;
			  }
			  /* else will try the tag method */
			}
			else if (ttisnil(tm = luaT_gettmbyobj(L, t, TMS.TM_INDEX)))
			  luaG_typeerror(L, t, "index");
			if (ttisfunction(tm)) {
			  callTM(L, tm, t, key, val, 1);
			  return;
			}
			t = tm;  /* else repeat with 'tm' */ 
		  }
		  luaG_runerror(L, "loop in gettable");
		}

		public static void luaV_settable (lua_State L, TValue t, TValue key, StkId val) {
		  int loop;
          TValue temp = new TValue();
          for (loop = 0; loop < MAXTAGLOOP; loop++) {
			TValue tm;
			if (ttistable(t)) {  /* `t' is a table? */
			  Table h = hvalue(t);
			  TValue oldval = luaH_set(L, h, key); /* do a primitive set */
			  if (!ttisnil(oldval) ||  /* result is no nil? */
				  (tm = fasttm(L, h.metatable, TMS.TM_NEWINDEX)) == null) { /* or no TM? */
				setobj2t(L, oldval, val);
                luaC_barriert(L, h, val);
				return;
			  }
			  /* else will try the tag method */
			}
			else if (ttisnil(tm = luaT_gettmbyobj(L, t, TMS.TM_NEWINDEX)))
			  luaG_typeerror(L, t, "index");
			if (ttisfunction(tm)) {
			  callTM(L, tm, t, key, val, 0);
			  return;
			}
		    /* else repeat with 'tm' */
		    setobj(L, temp, tm);  /* avoid pointing inside table (may rehash) */
		    t = temp;
		  }
		  luaG_runerror(L, "loop in settable");
		}


		private static int call_binTM (lua_State L, TValue p1, TValue p2,
							   StkId res, TMS event_) {
		  TValue tm = luaT_gettmbyobj(L, p1, event_);  /* try first operand */
		  if (ttisnil(tm))
			tm = luaT_gettmbyobj(L, p2, event_);  /* try second operand */
		  if (ttisnil(tm)) return 0;
		  callTM(L, tm, p1, p2, res, 1);
		  return 1;
		}


		private static TValue get_compTM (lua_State L, Table mt1, Table mt2,
										  TMS event_) {
		  TValue tm1 = fasttm(L, mt1, event_);
		  TValue tm2;
		  if (tm1 == null) return null;  /* no metamethod */
		  if (mt1 == mt2) return tm1;  /* same metatables => same metamethods */
		  tm2 = fasttm(L, mt2, event_);
		  if (tm2 == null) return null;  /* no metamethod */
		  if (luaO_rawequalObj(tm1, tm2) != 0)  /* same metamethods? */
			return tm1;
		return null;
		}


		private static int call_orderTM (lua_State L, TValue p1, TValue p2,
								 TMS event_) {
		  TValue tm1 = luaT_gettmbyobj(L, p1, event_);
		  TValue tm2;
		  if (ttisnil(tm1)) return -1;  /* no metamethod? */
		  tm2 = luaT_gettmbyobj(L, p2, event_);
		  if (luaO_rawequalObj(tm1, tm2)==0)  /* different metamethods? */
			return -1;
		  callTM(L, tm1, p1, p2, L.top, 1);
		  return l_isfalse(L.top) == 0 ? 1 : 0;
		}


		private static int l_strcmp (TString ls, TString rs) {
		  CharPtr l = getstr(ls);
		  uint ll = ls.tsv.len;
		  CharPtr r = getstr(rs);
		  uint lr = rs.tsv.len;
		  for (;;) {
		    //int temp = strcoll(l, r);
		      int temp = String.Compare(l.ToString(), r.ToString());
		    if (temp != 0) return temp;
		    else {  /* strings are equal up to a `\0' */
		      uint len = (uint)l.ToString().Length;  /* index of first `\0' in both strings */
		      if (len == lr)  /* r is finished? */
		        return (len == ll) ? 0 : 1;
		      else if (len == ll)  /* l is finished? */
		        return -1;  /* l is smaller than r (because r is not finished) */
		      /* both strings longer than `len'; go on comparing (after the `\0') */
		      len++;
		      l += len; ll -= len; r += len; lr -= len;
		    }
		  }
		}


		public static int luaV_lessthan (lua_State L, TValue l, TValue r) {
		  int res;
		  if (ttype(l) != ttype(r))
			return luaG_ordererror(L, l, r);
		  else if (ttisnumber(l))
			return luai_numlt(L, nvalue(l), nvalue(r)) ? 1 : 0;
		  else if (ttisstring(l))
			  return (l_strcmp(rawtsvalue(l), rawtsvalue(r)) < 0) ? 1 : 0;
		  else if ((res = call_orderTM(L, l, r, TMS.TM_LT)) != -1)
			return res;
		  return luaG_ordererror(L, l, r);
		}


		public static int luaV_lessequal (lua_State L, TValue l, TValue r) {
		  int res;
		  if (ttype(l) != ttype(r))
			return luaG_ordererror(L, l, r);
		  else if (ttisnumber(l))
			return luai_numle(L, nvalue(l), nvalue(r)) ? 1 : 0;
		  else if (ttisstring(l))
			  return (l_strcmp(rawtsvalue(l), rawtsvalue(r)) <= 0) ? 1 : 0;
		  else if ((res = call_orderTM(L, l, r, TMS.TM_LE)) != -1)  /* first try `le' */
			return res;
		  else if ((res = call_orderTM(L, r, l, TMS.TM_LT)) != -1)  /* else try `lt' */
			return (res == 0) ? 1 : 0;
		  return luaG_ordererror(L, l, r);
		}

		static CharPtr mybuff = null;

		public static int luaV_equalval_ (lua_State L, TValue t1, TValue t2) {
		  TValue tm = null;
		  lua_assert(ttype(t1) == ttype(t2));
		  switch (ttype(t1)) {
			case LUA_TNIL: return 1;
			case LUA_TNUMBER: return luai_numeq(nvalue(t1), nvalue(t2)) ? 1 : 0;
			case LUA_TBOOLEAN: return (bvalue(t1) == bvalue(t2)) ? 1 : 0;  /* true must be 1 !! */
			case LUA_TLIGHTUSERDATA: return (pvalue(t1) == pvalue(t2)) ? 1 : 0;
			case LUA_TUSERDATA: {
			  if (uvalue(t1) == uvalue(t2)) return 1;
			  tm = get_compTM(L, uvalue(t1).metatable, uvalue(t2).metatable, TMS.TM_EQ);
			  break;  /* will try TM */
			}
			case LUA_TTABLE: {
			  if (hvalue(t1) == hvalue(t2)) return 1;
			  tm = get_compTM(L, hvalue(t1).metatable, hvalue(t2).metatable, TMS.TM_EQ);
			  break;  /* will try TM */
			}
			default: return (gcvalue(t1) == gcvalue(t2)) ? 1 : 0;
		  }
		  if (tm == null) return 0;  /* no TM? */
		  callTM(L, tm, t1, t2, L.top, 1);  /* call TM */
		  return l_isfalse(L.top) == 0 ? 1 : 0;
		}


		public static void luaV_concat (lua_State L, int total) {
          lua_assert(total >= 2);
		  do {
			StkId top = L.top;
			int n = 2;  /* number of elements handled in this pass (at least 2) */
			if (!(ttisstring(top-2) || ttisnumber(top-2)) || tostring(L, top-1) == 0) {
			  if (call_binTM(L, top-2, top-1, top-2, TMS.TM_CONCAT)==0)
				luaG_concaterror(L, top-2, top-1);
		    } 
			else if (tsvalue(top-1).len == 0)  /* second operand is empty? */
		      tostring(L, top - 2);  /* result is first operand */
		    else if (ttisstring(top-2) && tsvalue(top-2).len == 0) {
		      setsvalue2s(L, top-2, rawtsvalue(top-1));  /* result is second op. */
		    } 
			else {
			  /* at least two non-empty string values; get as many as possible */
			  uint tl = tsvalue(top-1).len;
			  CharPtr buffer;
			  int i;
			  /* collect total length */
			  for (n = 1; n < total && (tostring(L, top-n-1)!=0); n++) {
				uint l = tsvalue(top-n-1).len;
				if (l >= MAX_SIZET - tl) luaG_runerror(L, "string length overflow");
				tl += l;
			  }
			  buffer = luaZ_openspace(L, G(L).buff, tl);
			  if (mybuff == null)
				  mybuff = buffer;
			  tl = 0;
			  for (i=n; i>0; i--) {  /* concat all strings */
				uint l = tsvalue(top-i).len;
				memcpy(buffer.chars, (int)tl, svalue(top-i).chars, (int)l);
				tl += l;
			  }
			  setsvalue2s(L, top-n, luaS_newlstr(L, buffer, tl));
			}
			total -= n-1;  /* got 'n' strings to create 1 new */
			L.top -= n-1;  /* poped 'n' strings and pushed one */
		  } while (total > 1);  /* repeat until only 1 result left */
		}



		private static void objlen (lua_State L, StkId ra, /*const*/ TValue rb) {
		  TValue tm;
		  switch (ttype(rb)) {
		    case LUA_TTABLE: {
		      Table h = hvalue(rb);
		      tm = fasttm(L, h.metatable, TMS.TM_LEN);
		      if (tm != null) break;  /* metamethod? break switch to call it */
		      setnvalue(ra, cast_num(luaH_getn(h)));  /* else primitive len */
		      return;
		    }
		    case LUA_TSTRING: {
		      setnvalue(ra, cast_num(tsvalue(rb).len));
		      return;
		    }
		    default: {  /* try metamethod */
		      tm = luaT_gettmbyobj(L, rb, TMS.TM_LEN);
		      if (ttisnil(tm))  /* no metamethod? */
		        luaG_typeerror(L, rb, "get length of");
		      break;
		    }
		  }
		  callTM(L, tm, rb, luaO_nilobject, ra, 1);
		}


		public static void luaV_arith (lua_State L, StkId ra, TValue rb,
						   TValue rc, TMS op) {
		  TValue tempb = new TValue(), tempc = new TValue();
		  TValue b, c;
		  if ((b = luaV_tonumber(rb, tempb)) != null &&
			  (c = luaV_tonumber(rc, tempc)) != null) {
			lua_Number res = luaO_arith(op - TMS.TM_ADD + LUA_OPADD, nvalue(b), nvalue(c));
    		setnvalue(ra, res);
		  }
		  else if (call_binTM(L, rb, rc, ra, op) == 0)
			luaG_aritherror(L, rb, rc);
		}


		/*
		** finish execution of an opcode interrupted by an yield
		*/
		public static void luaV_finishOp (lua_State L) {
		  CallInfo ci = L.ci;
		  StkId base_ = ci.u.l.base_;
		  Instruction inst = ci.u.l.savedpc[-1];  /* interrupted instruction */
		  switch (GET_OPCODE(inst)) {  /* finish its execution */
		    case OpCode.OP_ADD: case OpCode.OP_SUB: case OpCode.OP_MUL: case OpCode.OP_DIV:
		    case OpCode.OP_MOD: case OpCode.OP_POW: case OpCode.OP_UNM: case OpCode.OP_LEN:
		    case OpCode.OP_GETGLOBAL: case OpCode.OP_GETTABLE: case OpCode.OP_SELF: {
		  	  lua_TValue.dec(ref L.top);//--L.top
		      setobjs2s(L, base_ + GETARG_A(inst), L.top);
		      break;
		    }
		    case OpCode.OP_LE: case OpCode.OP_LT: case OpCode.OP_EQ: {
		  	  int res = l_isfalse(L.top - 1) != 0 ? 0 : 1;
		  	  lua_TValue.dec(ref L.top);
		      /* metamethod should not be called when operand is K */
		      lua_assert(ISK(GETARG_B(inst))==0);
		      if (GET_OPCODE(inst) == OpCode.OP_LE &&  /* "<=" using "<" instead? */
		          ttisnil(luaT_gettmbyobj(L, base_ + GETARG_B(inst), TMS.TM_LE)))
		      	res = (res != 0 ? 0 : 1);  /* invert result */
		      lua_assert(GET_OPCODE(ci.u.l.savedpc[0]) == OpCode.OP_JMP);
		      if (res != GETARG_A(inst))  /* condition failed? */
		        InstructionPtr.inc(ref ci.u.l.savedpc);  /* skip jump instruction */
		      break;
		    }
		    case OpCode.OP_CONCAT: {
		      StkId top = L.top - 1;  /* top when 'call_binTM' was called */
		      int b = GETARG_B(inst);      /* first element to concatenate */
		      int total = top - 1 - (base_ + b);  /* elements yet to concatenate */
		      setobj2s(L, top - 2, top);  /* put TM result in proper position */
		      if (total > 1) {  /* are there elements to concat? */
		        L.top = top - 1;  /* top is one after last element (at top-2) */
		        luaV_concat(L, total);  /* concat them (may yield again) */
		      }
		      /* move final result to final position */
		      setobj2s(L, ci.u.l.base_ + GETARG_A(inst), L.top - 1);
		      L.top = ci.top;  /* restore top */
		      break;
		    }
		    case OpCode.OP_TFORCALL: {
		  	  lua_assert(GET_OPCODE(ci.u.l.savedpc[0]) == OpCode.OP_TFORLOOP);
		      L.top = ci.top;  /* correct top */
		      break;
		    }
		    case OpCode.OP_CALL: {
		      if (GETARG_C(inst) - 1 >= 0)  /* nresults >= 0? */
		        L.top = ci.top;  /* adjust results */
		      break;
		    }
		    case OpCode.OP_TAILCALL: case OpCode.OP_SETGLOBAL: case OpCode.OP_SETTABLE:
		      break;
		    default: lua_assert(0);
		      break;//FIXME:added
		  }
		}



		/*
		** some macros for common tasks in `luaV_execute'
		*/

		//#define RA(i)	(base+GETARG_A(i))
		/* to be used after possible stack reallocation */
		//#define RB(i)	check_exp(getBMode(GET_OPCODE(i)) == OpArgMask.OpArgR, base+GETARG_B(i))
		//#define RC(i)	check_exp(getCMode(GET_OPCODE(i)) == OpArgMask.OpArgR, base+GETARG_C(i))
		//#define RKB(i)	check_exp(getBMode(GET_OPCODE(i)) == OpArgMask.OpArgK, \
			//ISK(GETARG_B(i)) ? k+INDEXK(GETARG_B(i)) : base+GETARG_B(i))
		//#define RKC(i)	check_exp(getCMode(GET_OPCODE(i)) == OpArgMask.OpArgK, \
		//	ISK(GETARG_C(i)) ? k+INDEXK(GETARG_C(i)) : base+GETARG_C(i))
		//#define KBx(i)	check_exp(getBMode(GET_OPCODE(i)) == OpArgMask.OpArgK, k+GETARG_Bx(i))

		// todo: implement proper checks, as above
		public static TValue RA(lua_State L, StkId base_, Instruction i) { return base_ + GETARG_A(i); }
		public static TValue RB(lua_State L, StkId base_, Instruction i) { return base_ + GETARG_B(i); }
		public static TValue RC(lua_State L, StkId base_, Instruction i) { return base_ + GETARG_C(i); }
		public static TValue RKB(lua_State L, StkId base_, Instruction i, TValue[] k) { return ISK(GETARG_B(i)) != 0 ? k[INDEXK(GETARG_B(i))] : base_ + GETARG_B(i); }
		public static TValue RKC(lua_State L, StkId base_, Instruction i, TValue[] k) { return ISK(GETARG_C(i)) != 0 ? k[INDEXK(GETARG_C(i))] : base_ + GETARG_C(i); }
		public static TValue KBx(lua_State L, Instruction i, TValue[] k) { return k[GETARG_Bx(i)]; }


		public static void dojump(int i, CallInfo ci, lua_State L) { InstructionPtr.inc(ref ci.u.l.savedpc, i); luai_threadyield(L); } //FIXME:


		//#define Protect(x)	{ {x;}; base = ci->u.l.base_; } //FIXME:


		public static void arith_op(lua_State L, op_delegate op, TMS tm, StkId base_, Instruction i, TValue[] k, StkId ra/*, InstructionPtr pc*/, CallInfo ci) {
		        TValue rb = RKB(L, base_, i, k);
		        TValue rc = RKC(L, base_, i, k);
				if (ttisnumber(rb) && ttisnumber(rc))
				{
					lua_Number nb = nvalue(rb), nc = nvalue(rc);
					setnvalue(ra, op(L, nb, nc));
				}
				else
				{
					//Protect(
					//L.savedpc = InstructionPtr.Assign(pc); //FIXME:
					luaV_arith(L, ra, rb, rc, tm);
					base_ = ci.u.l.base_;
					//);
				}
		      }

		//FIXME:added for debug //FIXME: not sync
		internal static void Dump(int pc, Instruction i)
		{
			int A = GETARG_A(i);
			int B = GETARG_B(i);
			int C = GETARG_C(i);
			int Bx = GETARG_Bx(i);
			int sBx = GETARG_sBx(i);
			if ((sBx & 0x100) != 0)
				sBx = - (sBx & 0xff);

			Console.Write("{0,5} ({1,10}): ", pc, i);
			Console.Write("{0,-10}\t", luaP_opnames[(int)GET_OPCODE(i)]);
			switch (GET_OPCODE(i))
			{
				case OpCode.OP_CLOSE:
					Console.Write("{0}", A);
					break;

				case OpCode.OP_MOVE:
				case OpCode.OP_LOADNIL:
				case OpCode.OP_GETUPVAL:
				case OpCode.OP_SETUPVAL:
				case OpCode.OP_UNM:
				case OpCode.OP_NOT:
				case OpCode.OP_RETURN:
					Console.Write("{0}, {1}", A, B);
					break;

				case OpCode.OP_LOADBOOL:
				case OpCode.OP_GETTABLE:
				case OpCode.OP_SETTABLE:
				case OpCode.OP_NEWTABLE:
				case OpCode.OP_SELF:
				case OpCode.OP_ADD:
				case OpCode.OP_SUB:
				case OpCode.OP_MUL:
				case OpCode.OP_DIV:
				case OpCode.OP_POW:
				case OpCode.OP_CONCAT:
				case OpCode.OP_EQ:
				case OpCode.OP_LT:
				case OpCode.OP_LE:
				case OpCode.OP_TEST:
				case OpCode.OP_CALL:
				case OpCode.OP_TAILCALL:
					Console.Write("{0}, {1}, {2}", A, B, C);
					break;

				case OpCode.OP_LOADK:					
					Console.Write("{0}, {1}", A, Bx);
					break;

				case OpCode.OP_GETGLOBAL:
				case OpCode.OP_SETGLOBAL:
				case OpCode.OP_SETLIST:
				case OpCode.OP_CLOSURE:
					Console.Write("{0}, {1}", A, Bx);
					break;

				case OpCode.OP_TFORLOOP:
					Console.Write("{0}, {1}", A, C);
					break;

				case OpCode.OP_JMP:
				case OpCode.OP_FORLOOP:
				case OpCode.OP_FORPREP:
					Console.Write("{0}, {1}", A, sBx);
					break;
			}
			Console.WriteLine();

		}

		public static void luaV_execute (lua_State L) {
		  CallInfo ci = L.ci;
		  LClosure cl = clvalue(ci.func).l;
		  TValue[] k = cl.p.k;
		  StkId base_ = ci.u.l.base_;
		  lua_assert(isLua(ci));
		  /* main loop of interpreter */
		  for (;;) {
			Instruction i = ci.u.l.savedpc[0]; InstructionPtr.inc(ref ci.u.l.savedpc); //FIXME:++
			StkId ra;
			if ( ((L.hookmask & (LUA_MASKLINE | LUA_MASKCOUNT)) != 0) &&
				(((--L.hookcount) == 0) || ((L.hookmask & LUA_MASKLINE) != 0))) {
			  traceexec(L);
			  if (L.status == LUA_YIELD) {  /* did hook yield? */
			  	InstructionPtr.dec(ref ci.u.l.savedpc);  /* undo increment */
				luaD_throw(L, LUA_YIELD);
			  }
			  base_ = ci.u.l.base_;
			}
			/* warning!! several calls may realloc the stack and invalidate `ra' */
			ra = RA(L, base_, i);
			lua_assert(base_ == ci.u.l.base_);
			lua_assert(base_ <= L.top && ((L.top - L.stack) <= L.stacksize));
			//Dump(L.ci.u.l.savedpc.pc, i);	//FIXME:added, only for debugging	
			switch (GET_OPCODE(i)) {
			  case OpCode.OP_MOVE: {
				setobjs2s(L, ra, RB(L, base_, i));
				continue;
			  }
			  case OpCode.OP_LOADK: {
				setobj2s(L, ra, KBx(L, i, k));
				continue;
			  }
			  case OpCode.OP_LOADBOOL: {
				setbvalue(ra, GETARG_B(i));
				if (GETARG_C(i) != 0) InstructionPtr.inc(ref ci.u.l.savedpc);  /* skip next instruction (if C) */
				continue;
			  }
			  case OpCode.OP_LOADNIL: {
				TValue rb = RB(L, base_, i);
				do {
					setnilvalue(StkId.dec(ref rb));
				} while (rb >= ra);
				continue;
			  }
			  case OpCode.OP_GETUPVAL: {
				int b = GETARG_B(i);
				setobj2s(L, ra, cl.upvals[b].v);
				continue;
			  }
			  case OpCode.OP_GETGLOBAL: {
				TValue g = new TValue();
				TValue rb = KBx(L, i, k);
				sethvalue(L, g, cl.env);
				lua_assert(ttisstring(rb));
				//Protect(
				  //L.savedpc = InstructionPtr.Assign(pc); //FIXME:
				  luaV_gettable(L, g, rb, ra);
				  base_ = ci.u.l.base_;
				  //);
				  //L.savedpc = InstructionPtr.Assign(pc); //FIXME:???
				continue;
			  }
			  case OpCode.OP_GETTABLE: {
				//Protect(
				  //L.savedpc = InstructionPtr.Assign(pc); //FIXME: 
				  luaV_gettable(L, RB(L, base_, i), RKC(L, base_, i, k), ra);
				  base_ = ci.u.l.base_;
				  //);
				//L.savedpc = InstructionPtr.Assign(pc);//FIXME:???
				continue;
			  }
			  case OpCode.OP_SETGLOBAL: {
				TValue g = new TValue();
				sethvalue(L, g, cl.env);
				lua_assert(ttisstring(KBx(L, i, k)));
				//Protect(
				  //L.savedpc = InstructionPtr.Assign(pc); //FIXME:
				  luaV_settable(L, g, KBx(L, i, k), ra);
				  base_ = ci.u.l.base_;
				  //);
				//L.savedpc = InstructionPtr.Assign(pc); //FIXME:???
				continue;
			  }
			  case OpCode.OP_SETUPVAL: {
				UpVal uv = cl.upvals[GETARG_B(i)];
				setobj(L, uv.v, ra);
				luaC_barrier(L, uv, ra);
				continue;
			  }
			  case OpCode.OP_SETTABLE: {
				//Protect(
				  //L.savedpc = InstructionPtr.Assign(pc); //FIXME:
				  luaV_settable(L, ra, RKB(L, base_, i, k), RKC(L, base_, i, k));
				  base_ = ci.u.l.base_;
				  //);
				//L.savedpc = InstructionPtr.Assign(pc); //FIXME:???
				continue;
			  }
			  case OpCode.OP_NEWTABLE: {
				int b = GETARG_B(i);
				int c = GETARG_C(i);
		        Table t = luaH_new(L);
		        sethvalue(L, ra, t);
		        if (b != 0 || c != 0)
		          luaH_resize(L, t, luaO_fb2int(b), luaO_fb2int(c));
				//Protect(
				  //L.savedpc = InstructionPtr.Assign(pc); //FIXME:
				  luaC_checkGC(L);
				  base_ = ci.u.l.base_;
				  //);
				//L.savedpc = InstructionPtr.Assign(pc); //FIXME:???
				continue;
			  }
			  case OpCode.OP_SELF: {
				StkId rb = RB(L, base_, i);
				setobjs2s(L, ra + 1, rb);
				//Protect(
				  //L.savedpc = InstructionPtr.Assign(pc); //FIXME:
				  luaV_gettable(L, rb, RKC(L, base_, i, k), ra);
				  base_ = ci.u.l.base_;
				  //);
				//L.savedpc = InstructionPtr.Assign(pc); //FIXME:???
				continue;
			  }
			  case OpCode.OP_ADD: {
				arith_op(L, luai_numadd, TMS.TM_ADD, base_, i, k, ra, ci);
				continue;
			  }
			  case OpCode.OP_SUB: {
				arith_op(L, luai_numsub, TMS.TM_SUB, base_, i, k, ra, ci);
				continue;
			  }
			  case OpCode.OP_MUL: {
				arith_op(L, luai_nummul, TMS.TM_MUL, base_, i, k, ra, ci);
				continue;
			  }
			  case OpCode.OP_DIV: {
				arith_op(L, luai_numdiv, TMS.TM_DIV, base_, i, k, ra, ci);
				continue;
			  }
			  case OpCode.OP_MOD: {
				arith_op(L, luai_nummod, TMS.TM_MOD, base_, i, k, ra, ci);
				continue;
			  }
			  case OpCode.OP_POW: {
				arith_op(L, luai_numpow, TMS.TM_POW, base_, i, k, ra, ci);
				continue;
			  }
			  case OpCode.OP_UNM: {
				TValue rb = RB(L, base_, i);
				if (ttisnumber(rb)) {
				  lua_Number nb = nvalue(rb);
				  setnvalue(ra, luai_numunm(L, nb));
				}
				else {
				  //Protect(
					//L.savedpc = InstructionPtr.Assign(pc); //FIXME:
					luaV_arith(L, ra, rb, rb, TMS.TM_UNM);
					base_ = ci.u.l.base_;
					//);
				  //L.savedpc = InstructionPtr.Assign(pc);//FIXME:???
				}
				continue;
			  }
			  case OpCode.OP_NOT: {
				int res = l_isfalse(RB(L, base_, i)) == 0 ? 0 : 1;  /* next assignment may change this value */
				setbvalue(ra, res);
				continue;
			  }
			  case OpCode.OP_LEN: {
				//Protect(
				  //L.savedpc = InstructionPtr.Assign(pc); //FIXME:
				  objlen(L, ra, RB(L, base_, i));
				  base_ = ci.u.l.base_; //FIXME:???
				//)
				continue;
			  }
			  case OpCode.OP_CONCAT: {
				int b = GETARG_B(i);
				int c = GETARG_C(i);
				L.top = base_ + c + 1;  /* mark the end of concat operands */
				//Protect(
				  //L.savedpc = InstructionPtr.Assign(pc); //FIXME:
				  luaV_concat(L, c-b+1); luaC_checkGC(L);
				  base_ = ci.u.l.base_;
				  //);
				L.top = ci.top;  /* restore top */
				setobjs2s(L, RA(L, base_, i), base_ + b);
				continue;
			  }
			  case OpCode.OP_JMP: {
				dojump(GETARG_sBx(i), ci, L);
				continue;
			  }
			  case OpCode.OP_EQ: {
				TValue rb = RKB(L, base_, i, k);
				TValue rc = RKC(L, base_, i, k);
				//Protect(
				  //L.savedpc = InstructionPtr.Assign(pc); //FIXME:
				  if (equalobj(L, rb, rc) == GETARG_A(i))
					dojump(GETARG_sBx(ci.u.l.savedpc[0]), ci, L);
				  base_ = ci.u.l.base_;
				//);
				InstructionPtr.inc(ref ci.u.l.savedpc);
				continue;
			  }
			  case OpCode.OP_LT: {
				//Protect(
				  //L.savedpc = InstructionPtr.Assign(pc); //FIXME:
				  if (luaV_lessthan(L, RKB(L, base_, i, k), RKC(L, base_, i, k)) == GETARG_A(i))
					dojump(GETARG_sBx(ci.u.l.savedpc[0]), ci, L);
				  base_ = ci.u.l.base_;
				//);
				InstructionPtr.inc(ref ci.u.l.savedpc);
				continue;
			  }
			  case OpCode.OP_LE: {
				//Protect(
				  //L.savedpc = InstructionPtr.Assign(pc); //FIXME:
				  if (luaV_lessequal(L, RKB(L, base_, i, k), RKC(L, base_, i, k)) == GETARG_A(i))
					dojump(GETARG_sBx(ci.u.l.savedpc[0]), ci, L);
				  base_ = ci.u.l.base_;
				//);
				InstructionPtr.inc(ref ci.u.l.savedpc);
				continue;
			  }
			  case OpCode.OP_TEST: {
				if (GETARG_C(i) != 0 ? l_isfalse(ra) == 0 : l_isfalse(ra) != 0)
				  dojump(GETARG_sBx(ci.u.l.savedpc[0]), ci, L);
				InstructionPtr.inc(ref ci.u.l.savedpc);
				continue;
			  }
			  case OpCode.OP_TESTSET: {
				TValue rb = RB(L, base_, i);
				if (GETARG_C(i) != 0 ? l_isfalse(rb) == 0 : l_isfalse(rb) != 0) {
				  setobjs2s(L, ra, rb);
				  dojump(GETARG_sBx(ci.u.l.savedpc[0]), ci, L);
				}
				InstructionPtr.inc(ref ci.u.l.savedpc);
				continue;
			  }
			  case OpCode.OP_CALL: {
				int b = GETARG_B(i);
		        int nresults = GETARG_C(i) - 1;
		        if (b != 0) L.top = ra+b;  /* else previous instruction set top */
		        if (luaD_precall(L, ra, nresults) != 0) {  /* C function? */
		          if (nresults >= 0) L.top = ci.top;  /* adjust results */
		          base_ = ci.u.l.base_;
		          continue;
		        }
		        else {  /* Lua function */
                  ci = L.ci;
		          ci.callstatus |= CIST_REENTRY;
		          break;  /* restart luaV_execute over new Lua function */
		        }
			  }
			  case OpCode.OP_TAILCALL: {
				int b = GETARG_B(i);
				if (b != 0) L.top = ra + b;  /* else previous instruction set top */
				lua_assert(GETARG_C(i) - 1 == LUA_MULTRET);
		        if (luaD_precall(L, ra, LUA_MULTRET) != 0) {  /* C function? */
		          base_ = ci.u.l.base_;
		          continue;
		        }
		        else {
			        /* tail call: put called frame (n) in place of caller one (o) */
			        CallInfo nci = L.ci;  /* called frame */
			        CallInfo oci = nci.previous;  /* caller frame */
			        StkId nfunc = nci.func;  /* called function index */
			        StkId ofunc = oci.func;
					int aux;
		            if (cl.p.sizep > 0) luaF_close(L, oci.u.l.base_);
		            oci.u.l.base_ = ofunc + (nci.u.l.base_ - nfunc);
		            for (aux = 0; nfunc+aux < L.top; aux++)  /* move frame down */
		              setobjs2s(L, ofunc + aux, nfunc + aux);
		            oci.top = L.top = ofunc + aux;  /* correct top */
		            lua_assert(L.top == oci.u.l.base_ + clvalue(ofunc).l.p.maxstacksize);
		            oci.u.l.savedpc = nci.u.l.savedpc;
		            oci.u.l.tailcalls++;  /* one more call lost */
		            ci = L.ci = oci;  /* remove new frame */
		            break;  /* restart luaV_execute over new Lua function */
				}
			  }
			  case OpCode.OP_RETURN: {
				int b = GETARG_B(i);
				if (b != 0) L.top = ra+b-1;
				if (cl.p.sizep > 0) luaF_close(L, base_);
				b = luaD_poscall(L, ra);
				if ((ci.callstatus & CIST_REENTRY)==0)  /* 'ci' still the called one */
				  return;  /* external invocation: return */
				else {  /* invocation via reentry: continue execution */
		          ci = L.ci;
		          if (b != 0) L.top = ci.top;
		          lua_assert(isLua(ci));
		          lua_assert(GET_OPCODE(ci.u.l.savedpc[-1]) == OpCode.OP_CALL);
		          break;  /* restart luaV_execute over new Lua function */
				}
			  }
			  case OpCode.OP_FORLOOP: {
				lua_Number step = nvalue(ra+2);
				lua_Number idx = luai_numadd(L, nvalue(ra), step); /* increment index */
				lua_Number limit = nvalue(ra+1);
				if (luai_numlt(L, 0, step) ? luai_numle(L, idx, limit)
										: luai_numle(L, limit, idx)) {
				  dojump(GETARG_sBx(i), ci, L);  /* jump back */
				  setnvalue(ra, idx);  /* update internal index... */
				  setnvalue(ra+3, idx);  /* ...and external index */
				}
				continue;
			  }
			  case OpCode.OP_FORPREP: {
				TValue init = ra;
				TValue plimit = ra+1;
				TValue pstep = ra+2;
				if (tonumber(ref init, ra) == 0)
				  luaG_runerror(L, LUA_QL("for") + " initial value must be a number");
				else if (tonumber(ref plimit, ra+1)  == 0)
				  luaG_runerror(L, LUA_QL("for") + " limit must be a number");
				else if (tonumber(ref pstep, ra+2)  == 0)
				  luaG_runerror(L, LUA_QL("for") + " step must be a number");
				setnvalue(ra, luai_numsub(L, nvalue(ra), nvalue(pstep)));
				dojump(GETARG_sBx(i), ci, L);
				continue;
			  }
			  case OpCode.OP_TFORCALL: {
				StkId cb = ra + 3;  /* call base */
				setobjs2s(L, cb+2, ra+2);
				setobjs2s(L, cb+1, ra+1);
				setobjs2s(L, cb, ra);
				L.top = cb+3;  /* func. + 2 args (state and index) */
				//Protect(
					//L.savedpc = InstructionPtr.Assign(pc);//FIXME:
					luaD_call(L, cb, GETARG_C(i), 1);
					base_ = ci.u.l.base_;
				  //);
				L.top = ci.top;
		        i = ci.u.l.savedpc[0]; InstructionPtr.inc(ref ci.u.l.savedpc);  /* go to next instruction */ //FIXME:++
		        ra = RA(L, base_, i);
		        lua_assert(GET_OPCODE(i) == OpCode.OP_TFORLOOP);
		        /* go through */
				goto case OpCode.OP_TFORLOOP; //FIXME:added
              }
      		  case OpCode.OP_TFORLOOP: {
	  		    if (!ttisnil(ra + 1)) {  /* continue loop? */
				  setobjs2s(L, ra, ra + 1);  /* save control variable */
				  dojump(GETARG_sBx(i), ci, L);  /* jump back */
				}
				continue;
			  }
			  case OpCode.OP_SETLIST: {
				int n = GETARG_B(i);
				int c = GETARG_C(i);
				int last;
				Table h;
				if (n == 0) n = cast_int(L.top - ra) - 1;
				if (c == 0) {
                  lua_assert(GET_OPCODE(ci.u.l.savedpc[0]) == OpCode.OP_EXTRAARG);
                  c = GETARG_Ax(ci.u.l.savedpc[0]); InstructionPtr.inc(ref ci.u.l.savedpc); //FIXME:++
				}
				h = hvalue(ra);
				last = ((c-1)*LFIELDS_PER_FLUSH) + n;
				if (last > h.sizearray)  /* needs more space? */
				  luaH_resizearray(L, h, last);  /* pre-alloc it at once */
				for (; n > 0; n--) {
				  TValue val = ra+n;
				  setobj2t(L, luaH_setnum(L, h, last--), val);
				  luaC_barriert(L, h, val);
				}
                L.top = ci.top;  /* correct top (in case of previous open call) */
				continue;
			  }
			  case OpCode.OP_CLOSE: {
				luaF_close(L, ra);
				continue;
			  }
			  case OpCode.OP_CLOSURE: {
				Proto p;
				Closure ncl;
				int nup, j;
				p = cl.p.p[GETARG_Bx(i)];
				nup = p.nups;
				ncl = luaF_newLclosure(L, nup, cl.env);
				ncl.l.p = p;
                setclvalue(L, ra, ncl);
				for (j=0; j<nup; j++) {
                  Instruction u = ci.u.l.savedpc[0]; InstructionPtr.inc(ref ci.u.l.savedpc);
				  if (GET_OPCODE(u) == OpCode.OP_GETUPVAL)
					ncl.l.upvals[j] = cl.upvals[GETARG_B(u)];
				  else {
					lua_assert(GET_OPCODE(u) == OpCode.OP_MOVE);
					ncl.l.upvals[j] = luaF_findupval(L, base_ + GETARG_B(u));
				  }
				}
				//Protect(
				  //L.savedpc = InstructionPtr.Assign(pc);//FIXME:
					luaC_checkGC(L);
				  base_ = ci.u.l.base_;
				  //);
				continue;
			  }
			  case OpCode.OP_VARARG: {
				int b = GETARG_B(i) - 1;
				int j;
				int n = cast_int(base_ - ci.func) - cl.p.numparams - 1;
				if (b == LUA_MULTRET) {
				  //Protect(
					//L.savedpc = InstructionPtr.Assign(pc);//FIXME:
					  luaD_checkstack(L, n);
					base_ = ci.u.l.base_;
					//);
				  ra = RA(L, base_, i);  /* previous call may change the stack */
				  b = n;
				  L.top = ra + n;
				}
				for (j = 0; j < b; j++) {
				  if (j < n) {
					setobjs2s(L, ra + j, base_ - n + j);
				  }
				  else {
					setnilvalue(ra + j);
				  }
				}
				continue;
			  }
		      case OpCode.OP_EXTRAARG: {
		        luaG_runerror(L, "bad opcode");
		        return;
		      }						
			}	
		    /* function changed (call/return): update pointers */
		    lua_assert(ci == L.ci);
		    cl = clvalue(ci.func).l;
		    k = cl.p.k;
		    base_ = ci.u.l.base_;	
		  }
		}
	}
}

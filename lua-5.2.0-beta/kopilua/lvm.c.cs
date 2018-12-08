/*
** $Id: lvm.c,v 2.140 2011/06/02 19:31:40 roberto Exp roberto $
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
		  if (ttisstring(obj) && (luaO_str2d(svalue(obj), tsvalue(obj).len, out num)!=0)) {
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
            CharPtr s = new char[LUAI_MAXNUMBER2STR]; //FIXME:???
			lua_Number n = nvalue(obj);
			uint l = (uint)lua_number2str(ref s, n); //FIXME:??? //FIXME: int->uint
			setsvalue2s(L, obj, luaS_newlstr(L, s, l));
			return 1;
		  }
		}


		private static void traceexec (lua_State L) {
          CallInfo ci = L.ci;
		  lu_byte mask = L.hookmask;
		  if (((mask & LUA_MASKCOUNT) != 0) && (L.hookcount == 0)) {
			resethookcount(L);
			luaD_hook(L, LUA_HOOKCOUNT, -1);
		  }
		  if ((mask & LUA_MASKLINE) != 0) {
			Proto p = ci_func(ci).p;
			int npc = pcRel(ci.u.l.savedpc, p);
			int newline = getfuncline(p, npc);
		    if (npc == 0 ||  /* call linehook when enter a new function, */
		        ci.u.l.savedpc <= L.oldpc ||  /* when jump back (loop), or when */
		        newline != getfuncline(p, pcRel(L.oldpc, p)))  /* enter a new line */
			  luaD_hook(L, LUA_HOOKLINE, newline);
		  }
          L.oldpc = ci.u.l.savedpc;
		  if (L.status == LUA_YIELD) {  /* did hook yield? */
          	InstructionPtr.dec(ref ci.u.l.savedpc);  /* undo increment (resume will increment it again) */
		    luaD_throw(L, LUA_YIELD);
		  }
		}





		private static void callTM (lua_State L, TValue f, TValue p1,
							TValue p2, TValue p3, int hasres) {
		  ptrdiff_t result = savestack(L, p3);
		  setobj2s(L, L.top, f); StkId.inc(ref L.top);  /* push function */ //FIXME:++
		  setobj2s(L, L.top, p1); StkId.inc(ref L.top);  /* 1st argument */ //FIXME:++
		  setobj2s(L, L.top, p2); StkId.inc(ref L.top);  /* 2nd argument */ //FIXME:++
		  if (hasres == 0)  /* no result? 'p3' is third argument */ //FIXME:++
		    setobj2s(L, L.top, p3); StkId.inc(ref L.top); /* 3rd argument */ //FIXME:++
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
			  if (!ttisnil(res) ||  /* result is not nil? */
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
			  if (!ttisnil(oldval) ||  /* result is not nil? */
				  (tm = fasttm(L, h.metatable, TMS.TM_NEWINDEX)) == null) { /* or no TM? */
				setobj2t(L, oldval, val);
                luaC_barrierback(L, obj2gco(h), val);
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


		private static TValue get_equalTM (lua_State L, Table mt1, Table mt2,
										  TMS event_) {
		  TValue tm1 = fasttm(L, mt1, event_);
		  TValue tm2;
		  if (tm1 == null) return null;  /* no metamethod */
		  if (mt1 == mt2) return tm1;  /* same metatables => same metamethods */
		  tm2 = fasttm(L, mt2, event_);
		  if (tm2 == null) return null;  /* no metamethod */
		  if (luaV_rawequalobj(tm1, tm2) != 0)  /* same metamethods? */
			return tm1;
		  return null;
		}


		private static int call_orderTM (lua_State L, TValue p1, TValue p2,
								 TMS event_) {
		  if (call_binTM(L, p1, p2, L.top, event_)==0)
		    return -1;  /* no metamethod */
		  else
		  	return l_isfalse(L.top)!=0?0:1;
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
		  if (ttisnumber(l) && ttisnumber(r))
			return luai_numlt(L, nvalue(l), nvalue(r)) ? 1 : 0;
		  else if (ttisstring(l) && ttisstring(r))
			  return (l_strcmp(rawtsvalue(l), rawtsvalue(r)) < 0) ? 1 : 0;
		  else if ((res = call_orderTM(L, l, r, TMS.TM_LT)) != -1)
			return res;
		  return luaG_ordererror(L, l, r);
		}


		public static int luaV_lessequal (lua_State L, TValue l, TValue r) {
		  int res;
		  if (ttisnumber(l) && ttisnumber(r))
			return luai_numle(L, nvalue(l), nvalue(r)) ? 1 : 0;
		  else if (ttisstring(l) && ttisstring(r))
			  return (l_strcmp(rawtsvalue(l), rawtsvalue(r)) <= 0) ? 1 : 0;
		  else if ((res = call_orderTM(L, l, r, TMS.TM_LE)) != -1)  /* first try `le' */
			return res;
		  else if ((res = call_orderTM(L, r, l, TMS.TM_LT)) != -1)  /* else try `lt' */
			return (res == 0) ? 1 : 0;
		  return luaG_ordererror(L, l, r);
		}

		private static CharPtr mybuff = null; //FIXME:added

		/*
		** equality of Lua values. L == NULL means raw equality (no metamethods)
		*/
		public static int luaV_equalobj_ (lua_State L, TValue t1, TValue t2) {
		  TValue tm = null;
		  lua_assert(ttisequal(t1, t2));
		  switch (ttype(t1)) {
			case LUA_TNIL: return 1;
			case LUA_TNUMBER: return luai_numeq(nvalue(t1), nvalue(t2)) ? 1 : 0;
			case LUA_TBOOLEAN: return (bvalue(t1) == bvalue(t2)) ? 1 : 0;  /* true must be 1 !! */
			case LUA_TLIGHTUSERDATA: return (pvalue(t1) == pvalue(t2)) ? 1 : 0;
		    case LUA_TLCF: return (fvalue(t1) == fvalue(t2)) ? 1 : 0;
		    case LUA_TSTRING: return (eqstr(rawtsvalue(t1), rawtsvalue(t2))) ? 1 : 0;
			case LUA_TUSERDATA: {
			  if (uvalue(t1) == uvalue(t2)) return 1;
              else if (L == null) return 0;
			  tm = get_equalTM(L, uvalue(t1).metatable, uvalue(t2).metatable, TMS.TM_EQ);
			  break;  /* will try TM */
			}
			case LUA_TTABLE: {
			  if (hvalue(t1) == hvalue(t2)) return 1;
              else if (L == null) return 0;
			  tm = get_equalTM(L, hvalue(t1).metatable, hvalue(t2).metatable, TMS.TM_EQ);
			  break;  /* will try TM */
			}
		    default:
		      lua_assert(iscollectable(t1));
			  return (gcvalue(t1) == gcvalue(t2)) ? 1 : 0;
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
			  for (i = 1; i < total && (tostring(L, top-i-1)!=0); i++) {
				uint l = tsvalue(top-i-1).len;
                if (l >= (MAX_SIZET/1) - tl) //FIXME:changed, sizeof(char)
				  luaG_runerror(L, "string length overflow");
				tl += l;
			  }
			  buffer = luaZ_openspace(L, G(L).buff, tl);
			  if (mybuff == null) //FIXME:added
				  mybuff = buffer; //FIXME:added
			  tl = 0;
		      n = i;
		      do {  /* concat all strings */
				uint l = tsvalue(top-i).len;
				memcpy(buffer.chars, (int)tl, svalue(top-i).chars, (int)l * 1); //FIXME:changed, * sizeof(char), (int), +t1=>.chars
				tl += l;
			  } while (--i > 0);
			  setsvalue2s(L, top-n, luaS_newlstr(L, buffer, tl));
			}
			total -= n-1;  /* got 'n' strings to create 1 new */
			L.top -= n-1;  /* popped 'n' strings and pushed one */
		  } while (total > 1);  /* repeat until only 1 result left */
		}


		private static void luaV_objlen (lua_State L, StkId ra, /*const*/ TValue rb) {
		  TValue tm;
		  switch (ttypenv(rb)) {
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
		  callTM(L, tm, rb, rb, ra, 1);
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
		** check whether cached closure in prototype 'p' may be reused, that is,
		** whether there is a cached closure with the same upvalues needed by
		** new closure to be created.
		*/
		private static Closure getcached (Proto p, UpVal[] encup, StkId base_) {
		  Closure c = p.cache;
		  if (c != null) {  /* is there a cached closure? */
		    int nup = p.sizeupvalues;
		    Upvaldesc[] uv = p.upvalues;
		    int i;
		    for (i = 0; i < nup; i++) {  /* check whether it has right upvalues */
		      TValue v = uv[i].instack!=0 ? base_ + uv[i].idx : encup[uv[i].idx].v;
		      if (c.l.upvals[i].v != v)
		        return null;  /* wrong upvalue; cannot reuse closure */
		    }
		  }
		  return c;  /* return cached closure (or NULL if no cached closure) */
		}


		/*
		** create a new Lua closure, push it in the stack, and initialize
		** its upvalues. Note that the call to 'luaC_barrierproto' must come
		** before the assignment to 'p->cache', as the function needs the
		** original value of that field.
		*/
		private static void pushclosure (lua_State L, Proto p, UpVal[] encup, StkId base_,
		                         StkId ra) {
		  int nup = p.sizeupvalues;
		  Upvaldesc[] uv = p.upvalues;
		  int i;
		  Closure ncl = luaF_newLclosure(L, p);
		  setclLvalue(L, ra, ncl);  /* anchor new closure in stack */
		  for (i = 0; i < nup; i++) {  /* fill in its upvalues */
		    if (uv[i].instack!=0)  /* upvalue refers to local variable? */
		      ncl.l.upvals[i] = luaF_findupval(L, base_ + uv[i].idx);
		    else  /* get upvalue from enclosing function */
		      ncl.l.upvals[i] = encup[uv[i].idx];
		  }
		  luaC_barrierproto(L, p, ncl);
		  p.cache = ncl;  /* save it on cache for reuse */
		}


		/*
		** finish execution of an opcode interrupted by an yield
		*/
		public static void luaV_finishOp (lua_State L) {
		  CallInfo ci = L.ci;
		  StkId base_ = ci.u.l.base_;
		  Instruction inst = ci.u.l.savedpc[-1];  /* interrupted instruction */
		  OpCode op = GET_OPCODE(inst);
		  switch (op) {  /* finish its execution */
		    case OpCode.OP_ADD: case OpCode.OP_SUB: case OpCode.OP_MUL: case OpCode.OP_DIV:
		    case OpCode.OP_MOD: case OpCode.OP_POW: case OpCode.OP_UNM: case OpCode.OP_LEN:
		    case OpCode.OP_GETTABUP: case OpCode.OP_GETTABLE: case OpCode.OP_SELF: {
		  	  lua_TValue.dec(ref L.top);//--L.top
		      setobjs2s(L, base_ + GETARG_A(inst), L.top);
		      break;
		    }
		    case OpCode.OP_LE: case OpCode.OP_LT: case OpCode.OP_EQ: {
		  	  int res = l_isfalse(L.top - 1) != 0 ? 0 : 1;
		  	  lua_TValue.dec(ref L.top);
		      /* metamethod should not be called when operand is K */
		      lua_assert(ISK(GETARG_B(inst))==0);
		      if (op == OpCode.OP_LE &&  /* "<=" using "<" instead? */
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
		      int total = (int)(top - 1 - (base_ + b));  /* yet to concatenate */
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
		    case OpCode.OP_TAILCALL: case OpCode.OP_SETTABUP: case OpCode.OP_SETTABLE:
		      break;
		    default: lua_assert(0);
		      break;//FIXME:added
		  }
		}



		/*
		** some macros for common tasks in `luaV_execute'
		*/

		//#if !defined luai_runtimecheck
		public static void luai_runtimecheck(lua_State L, bool c) { /* void */ }
		//#endif


		//#define RA(i)	(base+GETARG_A(i))
		/* to be used after possible stack reallocation */
		//#define RB(i)	check_exp(getBMode(GET_OPCODE(i)) == OpArgMask.OpArgR, base+GETARG_B(i))
		//#define RC(i)	check_exp(getCMode(GET_OPCODE(i)) == OpArgMask.OpArgR, base+GETARG_C(i))
		//#define RKB(i)	check_exp(getBMode(GET_OPCODE(i)) == OpArgMask.OpArgK, \
			//ISK(GETARG_B(i)) ? k+INDEXK(GETARG_B(i)) : base+GETARG_B(i))
		//#define RKC(i)	check_exp(getCMode(GET_OPCODE(i)) == OpArgMask.OpArgK, \
		//	ISK(GETARG_C(i)) ? k+INDEXK(GETARG_C(i)) : base+GETARG_C(i))
		//#define KBx(i)  \
		//	(k + (GETARG_Bx(i) != 0 ? GETARG_Bx(i) - 1 : GETARG_Ax(*ci->u.l.savedpc++)))
		
		// todo: implement proper checks, as above
		public static TValue RA(lua_State L, StkId base_, Instruction i) { return base_ + GETARG_A(i); }
		public static TValue RB(lua_State L, StkId base_, Instruction i) { return base_ + GETARG_B(i); }
		public static TValue RC(lua_State L, StkId base_, Instruction i) { return base_ + GETARG_C(i); }
		public static TValue RKB(lua_State L, StkId base_, Instruction i, TValue[] k) { return ISK(GETARG_B(i)) != 0 ? k[INDEXK(GETARG_B(i))] : base_ + GETARG_B(i); }
		public static TValue RKC(lua_State L, StkId base_, Instruction i, TValue[] k) { return ISK(GETARG_C(i)) != 0 ? k[INDEXK(GETARG_C(i))] : base_ + GETARG_C(i); }
		public static TValue KBx(lua_State L, Instruction i, TValue[] k, CallInfo ci) { 
			return k[(GETARG_Bx(i) != 0 ? GETARG_Bx(i) - 1 : GETARG_Ax(InstructionPtr.inc(ref ci.u.l.savedpc)[0]))]; }

		/* execute a jump instruction */
		public static void dojump(CallInfo ci, Instruction i, int e, lua_State L) //FIXME:???Instruction???InstructionPtr???
		  { int a = GETARG_A(i);
		    if (a > 0) luaF_close(L, ci.u.l.base_ + a - 1);
		    InstructionPtr.inc(ref ci.u.l.savedpc, GETARG_sBx(i) + e); }

		/* for test instructions, execute the jump instruction that follows it */
		public static void donextjump(CallInfo ci, ref Instruction i, lua_State L)	{ i = ci.u.l.savedpc[0]; dojump(ci, i, 1, L); }


		//#define Protect(x)	{ {x;}; base = ci->u.l.base_; } //FIXME:

		//#define checkGC(L,c)	Protect(luaC_condGC(L, c); luai_threadyield(L);) //FIXME:
		public delegate lua_Number op_delegate(lua_State L, lua_Number a, lua_Number b); //FIXME:added
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


		//#define vmdispatch(o)	switch(o)
		//#define vmcase(l,b)	case l: {b}  break;

        //FIXME:added for debug //FIXME: not sync 
        //FIXME:GETARG_xxx may be different from luac result, see INDEXK
		internal static void Dump(int pc, Instruction i)
		{
			int A = GETARG_A(i);
			int B = GETARG_B(i);
			int C = GETARG_C(i);
			int Bx = GETARG_Bx(i);
			int sBx = GETARG_sBx(i);
			int Ax = GETARG_Ax(i);
			if ((sBx & 0x100) != 0)
				sBx = - (sBx & 0xff);

			fprintf(stdout, "%d (%d): ", pc, i);
			fprintf(stdout, "%s\t", luaP_opnames[(int)GET_OPCODE(i)].ToString());
			switch (GET_OPCODE(i))
			{
				case OpCode.OP_MOVE:
				case OpCode.OP_LOADNIL:
				case OpCode.OP_GETUPVAL:
				case OpCode.OP_SETUPVAL:
				case OpCode.OP_UNM:
				case OpCode.OP_NOT:
				case OpCode.OP_RETURN:
				case OpCode.OP_LEN:
				case OpCode.OP_VARARG:
					fprintf(stdout, "%d, %d", A, B);
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
				case OpCode.OP_TESTSET:
				case OpCode.OP_CALL:
				case OpCode.OP_TAILCALL:
				case OpCode.OP_GETTABUP:
				case OpCode.OP_SETTABUP:
				case OpCode.OP_SETLIST:
					fprintf(stdout, "%d, %d, %d", A, B, C);
					break;

				case OpCode.OP_LOADK:					
					fprintf(stdout, "%d, %d", A, Bx);
					break;

				case OpCode.OP_CLOSURE:
					fprintf(stdout, "%d, %d", A, Bx);
					break;

				case OpCode.OP_TEST:
				case OpCode.OP_TFORCALL:
					fprintf(stdout, "%d, %d", A, C);
					break;

				case OpCode.OP_JMP:
				case OpCode.OP_FORLOOP:
				case OpCode.OP_FORPREP:
				case OpCode.OP_TFORLOOP:
					fprintf(stdout, "%d, %d", A, sBx);
					break;
					
				case OpCode.OP_LOADKX:
					fprintf(stdout, "%d", A);
					break;
					
				case OpCode.OP_EXTRAARG:
					fprintf(stdout, "%d", Ax);
					break;
			}
			fprintf(stdout, "\n");

		}

		public static void luaV_execute (lua_State L) {
		  CallInfo ci = L.ci;
		  LClosure cl;
		  TValue[] k;
		  StkId base_;
         newframe:  /* reentry point when frame changes (call/return) */
		  lua_assert(ci == L.ci);
		  cl = clLvalue(ci.func);
		  k = cl.p.k;
		  base_ = ci.u.l.base_;
		  /* main loop of interpreter */
		  for (;;) {
			Instruction i = ci.u.l.savedpc[0]; InstructionPtr.inc(ref ci.u.l.savedpc); //FIXME:++
			StkId ra;
			if ( ((L.hookmask & (LUA_MASKLINE | LUA_MASKCOUNT)) != 0) &&
				(((--L.hookcount) == 0) || ((L.hookmask & LUA_MASKLINE) != 0))) {
			  //Protect(
				traceexec(L);
			  base_ = ci.u.l.base_;
			  //);
			}
			/* warning!! several calls may realloc the stack and invalidate `ra' */
			ra = RA(L, base_, i);
			lua_assert(base_ == ci.u.l.base_);
			lua_assert(base_ <= L.top && L.top <= L.stack[L.stacksize-1]); //FIXME:L.top < L.stack[L.stacksize]??? L.stacksize >= L.stack.Length, overflow, so changed to <=
			//Dump(L.ci.u.l.savedpc.pc, i);	//FIXME:added, only for debugging
			switch (GET_OPCODE(i)) {
			  case OpCode.OP_MOVE: {
				setobjs2s(L, ra, RB(L, base_, i));
				break;
			  }
			  case OpCode.OP_LOADK: {
				TValue rb = k[GETARG_Bx(i)];
		        setobj2s(L, ra, rb);
				break;
			  }
		      case OpCode.OP_LOADKX: {
		        TValue rb;
		        lua_assert(GET_OPCODE(ci.u.l.savedpc[0]) == OpCode.OP_EXTRAARG);
		        rb = k[GETARG_Ax(ci.u.l.savedpc[0])]; InstructionPtr.inc(ref ci.u.l.savedpc); //FIXME:changed, ++
		        setobj2s(L, ra, rb);
				break;
		      }
			  case OpCode.OP_LOADBOOL: {
				setbvalue(ra, GETARG_B(i));
				if (GETARG_C(i) != 0) InstructionPtr.inc(ref ci.u.l.savedpc);  /* skip next instruction (if C) */
				break;
			  }
			  case OpCode.OP_LOADNIL: {
				int b = GETARG_B(i);
				do {
					setnilvalue(ra); lua_TValue.inc(ref ra); //FIXME:changed, ra++
				if (b==0) {break;}b--;} while(true); //FIXME:changed,} while (b--);
				break;
			  }
			  case OpCode.OP_GETUPVAL: {
				int b = GETARG_B(i);
				setobj2s(L, ra, cl.upvals[b].v);
				break;
			  }
			  case OpCode.OP_GETTABUP: {
				int b = GETARG_B(i);
				//Protect(
				  luaV_gettable(L, cl.upvals[b].v, RKC(L, base_, i, k), ra);
				base_ = ci.u.l.base_;
				//);
				break;
			  }
			  case OpCode.OP_GETTABLE: {
				//Protect(
				  luaV_gettable(L, RB(L, base_, i), RKC(L, base_, i, k), ra);
				base_ = ci.u.l.base_;
				//);
				break;
			  }
			  case OpCode.OP_SETTABUP: {
				int a = GETARG_A(i);
				//Protect(
        	      luaV_settable(L, cl.upvals[a].v, RKB(L, base_, i, k), RKC(L, base_, i, k));
				base_ = ci.u.l.base_;
				//);
				break;
			  }
			  case OpCode.OP_SETUPVAL: {
				UpVal uv = cl.upvals[GETARG_B(i)];
				setobj(L, uv.v, ra);
				luaC_barrier(L, uv, ra);
				break;
			  }
			  case OpCode.OP_SETTABLE: {
				//Protect(
				  luaV_settable(L, ra, RKB(L, base_, i, k), RKC(L, base_, i, k));
				base_ = ci.u.l.base_;
				//);
				break;
			  }
			  case OpCode.OP_NEWTABLE: {
				int b = GETARG_B(i);
				int c = GETARG_C(i);
		        Table t = luaH_new(L);
		        sethvalue(L, ra, t);
		        if (b != 0 || c != 0)
		          luaH_resize(L, t, luaO_fb2int(b), luaO_fb2int(c));
		        //Protect(
		        	luaC_condGC(L, delegate() {//checkGC()
                    	L.top = ra + 1;  /* limit of live values */
			          	luaC_step(L);
			          	L.top = ci.top;  /* restore top */
                    }); luai_threadyield(L);
		        base_ = ci.u.l.base_;
				//);
				break;
			  }
			  case OpCode.OP_SELF: {
				StkId rb = RB(L, base_, i);
				setobjs2s(L, ra + 1, rb);
				//Protect(
				  luaV_gettable(L, rb, RKC(L, base_, i, k), ra);
				base_ = ci.u.l.base_;
				//);
				break;
			  }
			  case OpCode.OP_ADD: {
				arith_op(L, luai_numadd, TMS.TM_ADD, base_, i, k, ra, ci);
				break;
			  }
			  case OpCode.OP_SUB: {
				arith_op(L, luai_numsub, TMS.TM_SUB, base_, i, k, ra, ci);
				break;
			  }
			  case OpCode.OP_MUL: {
				arith_op(L, luai_nummul, TMS.TM_MUL, base_, i, k, ra, ci);
				break;
			  }
			  case OpCode.OP_DIV: {
				arith_op(L, luai_numdiv, TMS.TM_DIV, base_, i, k, ra, ci);
				break;
			  }
			  case OpCode.OP_MOD: {
				arith_op(L, luai_nummod, TMS.TM_MOD, base_, i, k, ra, ci);
				break;
			  }
			  case OpCode.OP_POW: {
				arith_op(L, luai_numpow, TMS.TM_POW, base_, i, k, ra, ci);
				break;
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
				}
				break;
			  }
			  case OpCode.OP_NOT: {
                TValue rb = RB(L, base_, i);
				int res = l_isfalse(rb) == 0 ? 0 : 1;  /* next assignment may change this value */
				setbvalue(ra, res);
				break;
			  }
			  case OpCode.OP_LEN: {
				//Protect(
				  luaV_objlen(L, ra, RB(L, base_, i));
				base_ = ci.u.l.base_;
				//);
				break;
			  }
			  case OpCode.OP_CONCAT: {
				int b = GETARG_B(i);
				int c = GETARG_C(i);
                StkId rb;
				L.top = base_ + c + 1;  /* mark the end of concat operands */
				//Protect(
				  luaV_concat(L, c-b+1); 
				base_ = ci.u.l.base_;
				//);
		        ra = RA(L, base_, i);  /* 'luav_concat' may invoke TMs and move the stack */
		        rb = b + base_;
		        setobjs2s(L, ra, rb);
		        //Protect(
		        	luaC_condGC(L, delegate() {//checkGC()
                    	L.top = (ra >= rb ? ra + 1 : rb);  /* limit of live values */
		          		luaC_step(L);
                    }); luai_threadyield(L);
		        base_ = ci.u.l.base_;
				//};
				L.top = ci.top;  /* restore top */
				break;
			  }
			  case OpCode.OP_JMP: {
				dojump(ci, i, 0, L);
				break;
			  }
			  case OpCode.OP_EQ: {
				TValue rb = RKB(L, base_, i, k);
				TValue rc = RKC(L, base_, i, k);
				//Protect(
				  if ((equalobj(L, rb, rc)?1:0) != GETARG_A(i))
				  	InstructionPtr.inc(ref ci.u.l.savedpc); //FIXME:changed, ++
				  else
				  	donextjump(ci, ref i, L);
				base_ = ci.u.l.base_;
				//);
				break;
			  }
			  case OpCode.OP_LT: {
				//Protect(
				  if (luaV_lessthan(L, RKB(L, base_, i, k), RKC(L, base_, i, k)) != GETARG_A(i))
				  	InstructionPtr.inc(ref ci.u.l.savedpc); //FIXME:changed, ++
				  else
					donextjump(ci, ref i, L);
				base_ = ci.u.l.base_;
				//);
				break;
			  }
			  case OpCode.OP_LE: {
				//Protect(
				  if (luaV_lessequal(L, RKB(L, base_, i, k), RKC(L, base_, i, k)) != GETARG_A(i))
				  	InstructionPtr.inc(ref ci.u.l.savedpc); //FIXME:changed, ++
				  else
				    donextjump(ci, ref i, L);
				base_ = ci.u.l.base_;
				//);
				break;
			  }
			  case OpCode.OP_TEST: {
				if (GETARG_C(i) != 0 ? l_isfalse(ra) != 0 : l_isfalse(ra) == 0)
				  InstructionPtr.inc(ref ci.u.l.savedpc); //FIXME:changed, ++;
				else
				  donextjump(ci, ref i, L);
				break;
			  }
			  case OpCode.OP_TESTSET: {
				TValue rb = RB(L, base_, i);
				if (GETARG_C(i) != 0 ? l_isfalse(rb) != 0 : l_isfalse(rb) == 0)
				  InstructionPtr.inc(ref ci.u.l.savedpc); //FIXME:changed, ++;
                else {
				  setobjs2s(L, ra, rb);
				  donextjump(ci, ref i, L);
				}
				break;
			  }
			  case OpCode.OP_CALL: {
				int b = GETARG_B(i);
		        int nresults = GETARG_C(i) - 1;
		        if (b != 0) L.top = ra+b;  /* else previous instruction set top */
		        if (luaD_precall(L, ra, nresults) != 0) {  /* C function? */
		          if (nresults >= 0) L.top = ci.top;  /* adjust results */
		          base_ = ci.u.l.base_;
		        }
		        else {  /* Lua function */
                  ci = L.ci;
		          ci.callstatus |= CIST_REENTRY;
		          goto newframe;  /* restart luaV_execute over new Lua function */
		        }
				break;
			  }
			  case OpCode.OP_TAILCALL: {
				int b = GETARG_B(i);
				if (b != 0) L.top = ra + b;  /* else previous instruction set top */
				lua_assert(GETARG_C(i) - 1 == LUA_MULTRET);
		        if (luaD_precall(L, ra, LUA_MULTRET) != 0)  /* C function? */
		          base_ = ci.u.l.base_;
		        else {
			        /* tail call: put called frame (n) in place of caller one (o) */
			        CallInfo nci = L.ci;  /* called frame */
			        CallInfo oci = nci.previous;  /* caller frame */
			        StkId nfunc = nci.func;  /* called function */
		            StkId ofunc = oci.func;  /* caller function */
		            /* last stack slot filled by 'precall' */
		            StkId lim = nci.u.l.base_ + getproto(nfunc).numparams;
					int aux;
                    /* close all upvalues from previous call */
		            if (cl.p.sizep > 0) luaF_close(L, oci.u.l.base_);
		            /* move new frame into old one */
					for (aux = 0; nfunc+aux < lim; aux++)
		              setobjs2s(L, ofunc + aux, nfunc + aux);
                    oci.u.l.base_ = ofunc + (nci.u.l.base_ - nfunc);  /* correct base */
                    oci.top = L.top = ofunc + (L.top - nfunc);  /* correct top */
		            oci.u.l.savedpc = nci.u.l.savedpc;
		            oci.callstatus |= CIST_TAIL;  /* function was tail called */
		            ci = L.ci = oci;  /* remove new frame */
                    lua_assert(L.top == oci.u.l.base_ + getproto(ofunc).maxstacksize);
		            goto newframe;  /* restart luaV_execute over new Lua function */
				}
				break;
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
		          goto newframe;  /* restart luaV_execute over new Lua function */
				}
				break;
			  }
			  case OpCode.OP_FORLOOP: {
				lua_Number step = nvalue(ra+2);
				lua_Number idx = luai_numadd(L, nvalue(ra), step); /* increment index */
				lua_Number limit = nvalue(ra+1);
				if (luai_numlt(L, 0, step) ? luai_numle(L, idx, limit)
										: luai_numle(L, limit, idx)) {
				  InstructionPtr.inc(ref ci.u.l.savedpc, GETARG_sBx(i));  /* jump back */ //FIXME:changed, +=
				  setnvalue(ra, idx);  /* update internal index... */
				  setnvalue(ra+3, idx);  /* ...and external index */
				}
				break;
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
				InstructionPtr.inc(ref ci.u.l.savedpc, GETARG_sBx(i)); //FIXME:changed, +=
				break;
			  }
			  case OpCode.OP_TFORCALL: {
				StkId cb = ra + 3;  /* call base */
				setobjs2s(L, cb+2, ra+2);
				setobjs2s(L, cb+1, ra+1);
				setobjs2s(L, cb, ra);
				L.top = cb+3;  /* func. + 2 args (state and index) */
				//Protect(
				  luaD_call(L, cb, GETARG_C(i), 1);
				base_ = ci.u.l.base_;
				//);
				L.top = ci.top;
		        i = ci.u.l.savedpc[0]; InstructionPtr.inc(ref ci.u.l.savedpc);  /* go to next instruction */ //FIXME:++
		        ra = RA(L, base_, i);
		        lua_assert(GET_OPCODE(i) == OpCode.OP_TFORLOOP);
		        goto case OpCode.OP_TFORLOOP;//goto l_tforloop; //FIXME:changed
				//break; //FIXME: removed
              }
      		  case OpCode.OP_TFORLOOP: {
                //l_tforloop://FIXME:removed
	  		    if (!ttisnil(ra + 1)) {  /* continue loop? */
				  setobjs2s(L, ra, ra + 1);  /* save control variable */
				  InstructionPtr.inc(ref ci.u.l.savedpc, GETARG_sBx(i));  /* jump back */ //FIXME:changed, +=
				}
				break;
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
                luai_runtimecheck(L, ttistable(ra));
				h = hvalue(ra);
				last = ((c-1)*LFIELDS_PER_FLUSH) + n;
				if (last > h.sizearray)  /* needs more space? */
				  luaH_resizearray(L, h, last);  /* pre-allocate it at once */
				for (; n > 0; n--) {
				  TValue val = ra+n;
				  setobj2t(L, luaH_setint(L, h, last--), val);
				  luaC_barrierback(L, obj2gco(h), val);
				}
                L.top = ci.top;  /* correct top (in case of previous open call) */
				break;
			  }
			  case OpCode.OP_CLOSURE: {
				Proto p = cl.p.p[GETARG_Bx(i)];
			  	Closure ncl = getcached(p, cl.upvals, base_);  /* cached closure */
		        if (ncl == null)  /* no match? */
		          pushclosure(L, p, cl.upvals, base_, ra);  /* create a new one */
		        else
		          setclLvalue(L, ra, ncl);  /* push cashed closure */
		        //Protect(
		        	luaC_condGC(L, delegate() {//CheckGC()
			          	L.top = ra + 1;  /* limit of live values */
			          	luaC_step(L);
			          	L.top = ci.top;  /* restore top */
                    }); luai_threadyield(L);
		        base_ = ci.u.l.base_;
				//};
				break;
			  }
			  case OpCode.OP_VARARG: {
				int b = GETARG_B(i) - 1;
				int j;
				int n = cast_int(base_ - ci.func) - cl.p.numparams - 1;
		        if (b < 0) {  /* B == 0? */
		          b = n;  /* get all var. arguments */
				  //Protect(
					 luaD_checkstack(L, n);
				  base_ = ci.u.l.base_;
				  //);
				  ra = RA(L, base_, i);  /* previous call may change the stack */
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
				break;
			  }
		      case OpCode.OP_EXTRAARG: {
		        lua_assert(0);
				break;
		      }
			}
		  }
	  }
   }
}

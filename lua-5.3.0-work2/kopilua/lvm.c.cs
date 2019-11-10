/*
** $Id: lvm.c,v 2.190 2014/03/15 12:29:48 roberto Exp $
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
	using lua_Integer = System.Int32;

	public partial class Lua
	{



		/* limit for table tag-method chains (to avoid loops) */
		public const int MAXTAGLOOP	= 2000;


		/* maximum length of the conversion of a number to a string */
		public const int MAXNUMBER2STR = 50;


		public static int luaV_tonumber_ (TValue obj, ref lua_Number n) {
		  lua_assert(!ttisfloat(obj));
		  if (ttisinteger(obj)) {
		    n = cast_num(ivalue(obj));
		    return 1;
		  }
		  else
		  	return (ttisstring(obj) && luaO_str2d(svalue(obj), tsvalue(obj).len, out n)!=0) ? 1 : 0;
		}


		public static int luaV_tostring (lua_State L, StkId obj) {
		  if (!ttisnumber(obj))
		    return 0;
		  else {
		  	CharPtr buff = new CharPtr(new char[MAXNUMBER2STR]);
	        uint len = (ttisinteger(obj))
	         ? (uint)lua_integer2str(buff, ivalue(obj))
             : (uint)lua_number2str(buff, fltvalue(obj));
		    setsvalue2s(L, obj, luaS_newlstr(L, buff, len));
		    return 1;
		  }
		}


		/*
		** Check whether a float number is within the range of a lua_Integer.
		** (The comparisons are tricky because of rounding, which can or
		** not occur depending on the relative sizes of floats and integers.)
		** This function should be called only when 'n' has an integral value.
		*/
		public static int luaV_numtointeger (lua_Number n, ref lua_Integer p) {
		  if (cast_num(MIN_INTEGER) <= n && n < (MAX_INTEGER + cast_num(1))) {
		    p = cast_integer(n);
		    lua_assert(cast_num(p) == n);
		    return 1;
		  }
		  return 0;  /* number is outside integer limits */
		}


		/*
		** try to convert a non-integer value to an integer
		*/
		public static int luaV_tointeger_ (TValue obj, ref lua_Integer p) {
		  lua_Number n = 0;
		  lua_assert(!ttisinteger(obj));
		  if (tonumber(ref obj, ref n)!=0) {
		    n = floor(n);
		    return luaV_numtointeger(n, ref p);
		  }
		  else return 0;
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
			  luaT_callTM(L, tm, t, key, val, 1);
			  return;
			}
			t = tm;  /* else repeat with 'tm' */ 
		  }
		  luaG_runerror(L, "gettable chain too long; possible loop");
		}

		public static bool luaV_settable_sub(lua_State L, Table h, TValue key, ref TValue oldval) //FIXME:added
		{
			oldval = luaH_newkey(L, h, key);
			return true;
		}
		public static void luaV_settable (lua_State L, TValue t, TValue key, StkId val) {
		  int loop;
		  for (loop = 0; loop < MAXTAGLOOP; loop++) {
		    /*const */TValue tm;
		    if (ttistable(t)) {  /* `t' is a table? */
		      Table h = hvalue(t);
		      TValue oldval = (TValue)(luaH_get(h, key));
		      /* if previous value is not nil, there must be a previous entry
		         in the table; moreover, a metamethod has no relevance */
		      if (!ttisnil(oldval) ||
		         /* previous value is nil; must check the metamethod */
		         ((tm = fasttm(L, h.metatable, TMS.TM_NEWINDEX)) == null &&
		         /* no metamethod; is there a previous entry in the table? */
		         (oldval != luaO_nilobject ||
		         /* no previous entry; must create one. (The next test is
		            always true; we only need the assignment.) */
		         (luaV_settable_sub(L, h, key, ref oldval))))) { //FIXME:changed, (oldval = luaH_newkey(L, h, key), 1)))) {
		        /* no metamethod and (now) there is an entry with given key */
		        setobj2t(L, oldval, val);  /* assign new value to that entry */
		        invalidateTMcache(h);
		        luaC_barrierback(L, h, val);
		        return;
		      }
		      /* else will try the metamethod */
		    }
		    else  /* not a table; check metamethod */
		      if (ttisnil(tm = luaT_gettmbyobj(L, t, TMS.TM_NEWINDEX)))
		        luaG_typeerror(L, t, "index");
		    /* there is a metamethod */
		    if (ttisfunction(tm)) {
		      luaT_callTM(L, tm, t, key, val, 0);
		      return;
		    }
		    t = tm;  /* else repeat with 'tm' */
		  }
		  luaG_runerror(L, "settable chain too long; possible loop");
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
		  lua_Number nl = 0, nr = 0;
		  if (ttisinteger(l) && ttisinteger(r))
		  	return (ivalue(l) < ivalue(r)) ? 1 : 0;
		  else if (tonumber(ref l, ref nl)!=0 && tonumber(ref r, ref nr)!=0)
		  	return luai_numlt(nl, nr) ? 1 : 0;
		  else if (ttisstring(l) && ttisstring(r))
		  	return (l_strcmp(rawtsvalue(l), rawtsvalue(r)) < 0) ? 1 : 0;
		  else if ((res = luaT_callorderTM(L, l, r, TMS.TM_LT)) < 0)
		    luaG_ordererror(L, l, r);
		  return res;
		}


		public static int luaV_lessequal (lua_State L, TValue l, TValue r) {
		  int res;
		  lua_Number nl = 0, nr = 0;
		  if (ttisinteger(l) && ttisinteger(r))
		    return (ivalue(l) <= ivalue(r)) ? 1 : 0;
		  else if (tonumber(ref l, ref nl)!=0 && tonumber(ref r, ref nr)!=0)
		    return luai_numle(nl, nr) ? 1 : 0;
		  else if (ttisstring(l) && ttisstring(r))
		  	return (l_strcmp(rawtsvalue(l), rawtsvalue(r)) <= 0) ? 1 : 0;
		  else if ((res = luaT_callorderTM(L, l, r, TMS.TM_LE)) >= 0)  /* first try `le' */
		    return res;
		  else if ((res = luaT_callorderTM(L, r, l, TMS.TM_LT)) < 0)  /* else try `lt' */
		    luaG_ordererror(L, l, r);
		  return res == 0 ? 1 : 0;
		}

		/*
		** equality of Lua values. L == NULL means raw equality (no metamethods)
		*/
		public static int luaV_equalobj (lua_State L, TValue t1, TValue t2) {
		  TValue tm;
		  if (ttype(t1) != ttype(t2)) {
		    if (ttnov(t1) != ttnov(t2) || ttnov(t1) != LUA_TNUMBER)
		      return 0;  /* only numbers can be equal with different variants */
		    else {  /* two numbers with different variants */
		      lua_Number n1 = 0, n2 = 0;
		      lua_assert(ttisnumber(t1) && ttisnumber(t2));
		      tonumber(ref t1, ref n1); tonumber(ref t2, ref n2);
		      return (luai_numeq(n1, n2))?1:0;
		    }
		  }
		  /* values have same type and same variant */
		  switch (ttype(t1)) {
		    case LUA_TNIL: return 1;
		    case LUA_TNUMINT: return (ivalue(t1) == ivalue(t2))?1:0;
		    case LUA_TNUMFLT: return (luai_numeq(fltvalue(t1), fltvalue(t2)))?1:0;
		    case LUA_TBOOLEAN: return (bvalue(t1) == bvalue(t2))?1:0;  /* true must be 1 !! */
		    case LUA_TLIGHTUSERDATA: return (pvalue(t1) == pvalue(t2))?1:0;
		    case LUA_TLCF: return (fvalue(t1) == fvalue(t2))?1:0;
		    case LUA_TSHRSTR: return (eqshrstr(rawtsvalue(t1), rawtsvalue(t2)))?1:0;
		    case LUA_TLNGSTR: return luaS_eqlngstr(rawtsvalue(t1), rawtsvalue(t2));
		    case LUA_TUSERDATA: {
		      if (uvalue(t1) == uvalue(t2)) return 1;
		      else if (L == null) return 0;
		      tm = luaT_getequalTM(L, uvalue(t1).metatable, uvalue(t2).metatable);
		      break;  /* will try TM */
		    }
		    case LUA_TTABLE: {
		      if (hvalue(t1) == hvalue(t2)) return 1;
		      else if (L == null) return 0;
		      tm = luaT_getequalTM(L, hvalue(t1).metatable, hvalue(t2).metatable);
		      break;  /* will try TM */
		    }
		    default:
		      return (gcvalue(t1) == gcvalue(t2)) ? 1 : 0;
		  }
		  if (tm == null) return 0;  /* no TM? */
		  luaT_callTM(L, tm, t1, t2, L.top, 1);  /* call TM */
		  return l_isfalse(L.top)==0 ? 1 : 0;
		}


		public static void luaV_concat (lua_State L, int total) {
		   lua_assert(total >= 2);
		  do {
		    StkId top = L.top;
		    int n = 2;  /* number of elements handled in this pass (at least 2) */
		    if (!(ttisstring(top-2) || ttisnumber(top-2)) || 0==tostring(L, top-1))
		      luaT_trybinTM(L, top-2, top-1, top-2, TMS.TM_CONCAT);
		    else if (tsvalue(top-1).len == 0)  /* second operand is empty? */
		      tostring(L, top - 2);  /* result is first operand */
		    else if (ttisstring(top-2) && tsvalue(top-2).len == 0) {
		      setobjs2s(L, top - 2, top - 1);  /* result is second op. */
		    }
		    else {
		      /* at least two non-empty string values; get as many as possible */
		      uint tl = tsvalue(top-1).len;
		      CharPtr buffer;
		      int i;
		      /* collect total length */
		      for (i = 1; i < total && tostring(L, top-i-1)!=0; i++) {
		        uint l = tsvalue(top-i-1).len;
		        if (l >= (MAX_SIZE/1/*sizeof(char)*/) - tl)
		          luaG_runerror(L, "string length overflow");
		        tl += l;
		      }
		      buffer = luaZ_openspace(L, G(L).buff, tl);
		      tl = 0;
		      n = i;
		      do {  /* concat all strings */
		        uint l = tsvalue(top-i).len;
		        memcpy(buffer+tl, svalue(top-i), l * 1/*sizeof(char)*/);//FIXME: sizeof(char)==1
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
		  switch (ttnov(rb)) {
		    case LUA_TTABLE: {
		      Table h = hvalue(rb);
		      tm = fasttm(L, h.metatable, TMS.TM_LEN);
		      if (tm!=null) break;  /* metamethod? break switch to call it */
		      setivalue(ra, luaH_getn(h));  /* else primitive len */
		      return;
		    }
		    case LUA_TSTRING: {
		  	  setivalue(ra, (int)(tsvalue(rb).len));
		      return;
		    }
		    default: {  /* try metamethod */
		      tm = luaT_gettmbyobj(L, rb, TMS.TM_LEN);
		      if (ttisnil(tm))  /* no metamethod? */
		        luaG_typeerror(L, rb, "get length of");
		      break;
		    }
		  }
		  luaT_callTM(L, tm, rb, rb, ra, 1);
		}


		public static lua_Integer luaV_div (lua_State L, lua_Integer x, lua_Integer y) {
		  if (cast_unsigned(y) + 1 <= 1U) {  /* special cases: -1 or 0 */
		    if (y == 0)
		      luaG_runerror(L, "attempt to divide by zero");
		    return intop_minus(0, x);   /* y==-1; avoid overflow with 0x80000...//-1 */
		  }
		  else {
		    lua_Integer d = x / y;  /* perform division */
		    if ((x ^ y) >= 0 || x % y == 0)   /* same signal or no rest? */
		      return d;
		    else
		      return d - 1;  /* correct 'div' for negative case */
		  }
		  return 0; //FIXME:
		}


		public static lua_Integer luaV_mod (lua_State L, lua_Integer x, lua_Integer y) {
		  if (cast_unsigned(y) + 1 <= 1U) {  /* special cases: -1 or 0 */
		    if (y == 0)
		      luaG_runerror(L, "attempt to perform 'n%%0'");
		    return 0;   /* y==-1; avoid overflow with 0x80000...%-1 */
		  }
		  else {
		    lua_Integer r = x % y;
		    if (r == 0 || (x ^ y) >= 0)
		      return r;
		    else
		      return r + y;  /* correct 'mod' for negative case */
		  }
		  return 0; //FIXME:
		}


		public static lua_Integer luaV_pow (lua_State L, lua_Integer x, lua_Integer y) {
		  if (y <= 0) {  /* special cases: 0 or negative exponent */
		    if (y < 0)
		      luaG_runerror(L, "integer exponentiation with negative exponent");
		    return 1;  /* x^0 == 1 */
		  }
		  else {
		    lua_Integer r = 1;
		    for (; y > 1; y >>= 1) {
		      if ((y & 1)!=0) r = intop_mul(r, x);
		      x = intop_mul(x, x);
		    }
		    r = intop_mul(r, x);
		    return r;
		  }
		}


		/* number of bits in an integer */
		public static int NBITS = cast_int(sizeof(lua_Integer) * CHAR_BIT);

		public static lua_Integer luaV_shiftl (lua_Integer x, lua_Integer y) {
		  if (y < 0) {  /* shift right? */
		    if (y <= -NBITS) return 0;
		    else return cast_integer(cast_unsigned(x) >> (-y));
		  }
		  else {  /* shift left */
		    if (y >= NBITS) return 0;
		    else return x << y;
		  }
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
		** its upvalues. Note that the closure is not cached if prototype is
		** already black (which means that 'cache' was already cleared by the
		** GC).
		*/
		private static void pushclosure (lua_State L, Proto p, UpVal[] encup, StkId base_,
		                         StkId ra) {
		  int nup = p.sizeupvalues;
		  Upvaldesc[] uv = p.upvalues;
		  int i;
		  Closure ncl = luaF_newLclosure(L, nup);
  		  ncl.l.p = p;
		  setclLvalue(L, ra, ncl);  /* anchor new closure in stack */
		  for (i = 0; i < nup; i++) {  /* fill in its upvalues */
		    if (uv[i].instack!=0)  /* upvalue refers to local variable? */
		      ncl.l.upvals[i] = luaF_findupval(L, base_ + uv[i].idx);
		    else  /* get upvalue from enclosing function */
		      ncl.l.upvals[i] = encup[uv[i].idx];
		    ncl.l.upvals[i].refcount++;
		    /* new closure is white, so we do not need a barrier here */			  
		  }
		  if (!isblack(obj2gco(p)))  /* cache will not break GC invariant? */
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
		    case OpCode.OP_ADD: case OpCode.OP_SUB: case OpCode.OP_MUL: case OpCode.OP_DIV: case OpCode.OP_IDIV:
		    case OpCode.OP_BAND: case OpCode.OP_BOR: case OpCode.OP_BXOR: case OpCode.OP_SHL: case OpCode.OP_SHR:
		    case OpCode.OP_MOD: case OpCode.OP_POW:
		    case OpCode.OP_UNM: case OpCode.OP_BNOT: case OpCode.OP_LEN:
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
		      StkId top = L.top - 1;  /* top when 'luaT_trybinTM' was called */
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

		//#define checkGC(L,c)	\
		//	Protect( luaC_condGC(L,{L->top = (c);  /* limit of live values */ \
        //                  luaC_step(L); \
        //                  L->top = ci->top;})  /* restore top */ \
        //   	luai_threadyield(L); )
		
		


		//#define vmdispatch(o)	switch(o)
		//#define vmcase(l,b)	case l: {b}  break;
		//#define vmcasenb(l,b)	case l: {b}		/* nb = no break */

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
				luaG_traceexec(L);
			  base_ = ci.u.l.base_;
			  //);
			}
			/* WARNING: several calls may realloc the stack and invalidate `ra' */
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
				luaC_upvalbarrier(L, uv);
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
                    }); 
					luai_threadyield(L);
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
		        TValue rb = RKB(L, base_, i, k);
		        TValue rc = RKC(L, base_, i, k);
		        lua_Number nb = 0; lua_Number nc = 0;
		        if (ttisinteger(rb) && ttisinteger(rc)) {
		          lua_Integer ib = ivalue(rb); lua_Integer ic = ivalue(rc);
		          setivalue(ra, intop_plus(ib, ic));
		        }
		        else if (tonumber(ref rb, ref nb)!=0 && tonumber(ref rc, ref nc)!=0) {
		          setnvalue(ra, luai_numadd(L, nb, nc));
		        }
		        else { 
		        	//Protect(
		        		luaT_trybinTM(L, rb, rc, ra, TMS.TM_ADD);
		        	base_ = ci.u.l.base_;
		        	//);
		        }
				break;
			  }
			  case OpCode.OP_SUB: {
		        TValue rb = RKB(L, base_, i, k);
		        TValue rc = RKC(L, base_, i, k);
		        lua_Number nb = 0; lua_Number nc = 0;
		        if (ttisinteger(rb) && ttisinteger(rc)) {
		          lua_Integer ib = ivalue(rb); lua_Integer ic = ivalue(rc);
		          setivalue(ra, intop_minus(ib, ic));
		        }
		        else if (tonumber(ref rb, ref nb)!=0 && tonumber(ref rc, ref nc)!=0) {
		          setnvalue(ra, luai_numsub(L, nb, nc));
		        }
		        else { 
		        	//Protect(
		        		luaT_trybinTM(L, rb, rc, ra, TMS.TM_SUB);
		        	base_ = ci.u.l.base_;
		        	//); 
		       	}
				break;
			  }
			  case OpCode.OP_MUL: {
		        TValue rb = RKB(L, base_, i, k);
		        TValue rc = RKC(L, base_, i, k);
		        lua_Number nb = 0; lua_Number nc = 0;
		        if (ttisinteger(rb) && ttisinteger(rc)) {
		          lua_Integer ib = ivalue(rb); lua_Integer ic = ivalue(rc);
		          setivalue(ra, intop_mul(ib, ic));
		        }
		        else if (tonumber(ref rb, ref nb)!=0 && tonumber(ref rc, ref nc)!=0) {
		          setnvalue(ra, luai_nummul(L, nb, nc));
		        }
		        else { 
		        	//Protect(
		        		luaT_trybinTM(L, rb, rc, ra, TMS.TM_MUL);
		        	base_ = ci.u.l.base_;
		        	//); 
		       	}
				break;
			  }
			  case OpCode.OP_DIV: {  /* float division (always with floats) */
		        TValue rb = RKB(L, base_, i, k);
		        TValue rc = RKC(L, base_, i, k);
		        lua_Number nb = 0; lua_Number nc = 0;
		        if (tonumber(ref rb, ref nb)!=0 && tonumber(ref rc, ref nc)!=0) {
		          setnvalue(ra, luai_numdiv(L, nb, nc));
		        }
		        else { 
		        	//Protect(
		        		luaT_trybinTM(L, rb, rc, ra, TMS.TM_DIV);
		        	base_ = ci.u.l.base_;
		        	//);
		        }
				break;
			  }
			  case OpCode.OP_IDIV: {  /* integer division */
		        TValue rb = RKB(L, base_, i, k);
		        TValue rc = RKC(L, base_, i, k);
		        lua_Integer ib = 0; lua_Integer ic = 0;
		        if (tointeger(ref rb, ref ib)!=0 && tointeger(ref rc, ref ic)!=0) {
		          setivalue(ra, luaV_div(L, ib, ic));
		        }
		        else { 
		        	//Protect(
		        		luaT_trybinTM(L, rb, rc, ra, TMS.TM_IDIV);
		        	base_ = ci.u.l.base_;
		        	//);
		        }
			  	break;
			  }
			  case OpCode.OP_BAND: {
		        TValue rb = RKB(L, base_, i, k);
		        TValue rc = RKC(L, base_, i, k);
		        lua_Integer ib = 0; lua_Integer ic = 0;
		        if (0!=tointeger(ref rb, ref ib) && 0!=tointeger(ref rc, ref ic)) {
		          setivalue(ra, intop_and(ib, ic));
		        }
		        else { 
		        	//Protect(
		        		luaT_trybinTM(L, rb, rc, ra, TMS.TM_BAND); 
		        	base_ = ci.u.l.base_;
		        	//);
		        }
			  	break;
			  }
			  case OpCode.OP_BOR: {
		        TValue rb = RKB(L, base_, i, k);
		        TValue rc = RKC(L, base_, i, k);
		        lua_Integer ib = 0; lua_Integer ic = 0;
		        if (0!=tointeger(ref rb, ref ib) && 0!=tointeger(ref rc, ref ic)) {
		          setivalue(ra, intop_or(ib, ic));
		        }
		        else { 
		        	//Protect(
		        		luaT_trybinTM(L, rb, rc, ra, TMS.TM_BOR); 
		        	base_ = ci.u.l.base_;
		        	//);
		        }
		      	break;
			  }
			  case OpCode.OP_BXOR: {
		        TValue rb = RKB(L, base_, i, k);
		        TValue rc = RKC(L, base_, i, k);
		        lua_Integer ib = 0; lua_Integer ic = 0;
		        if (0!=tointeger(ref rb, ref ib) && 0!=tointeger(ref rc, ref ic)) {
		          setivalue(ra, intop_xor(ib, ic));
		        }
		        else { 
		        	//Protect(
		        		luaT_trybinTM(L, rb, rc, ra, TMS.TM_BXOR); 
		        	base_ = ci.u.l.base_;
		        	//);
		        }
			  	break;
			  }
			  case OpCode.OP_SHL: {
		        TValue rb = RKB(L, base_, i, k);
		        TValue rc = RKC(L, base_, i, k);
		        lua_Integer ib = 0; lua_Integer ic = 0;
		        if (0!=tointeger(ref rb, ref ib) && 0!=tointeger(ref rc, ref ic)) {
		          setivalue(ra, luaV_shiftl(ib, ic));
		        }
		        else { 
		        	//Protect(
		        		luaT_trybinTM(L, rb, rc, ra, TMS.TM_SHL); 
		        	base_ = ci.u.l.base_;
		        	//);
		        }
			  	break;
			  }
			  case OpCode.OP_SHR: {
		        TValue rb = RKB(L, base_, i, k);
		        TValue rc = RKC(L, base_, i, k);
		        lua_Integer ib = 0; lua_Integer ic = 0;
		        if (0!=tointeger(ref rb, ref ib) && 0!=tointeger(ref rc, ref ic)) {
		          setivalue(ra, luaV_shiftl(ib, -ic));
		        }
		        else { 
		        	//Protect(
		        		luaT_trybinTM(L, rb, rc, ra, TMS.TM_SHR); 
		        	base_ = ci.u.l.base_;
		        	//);
		        }
			  	break;
			  }
			  case OpCode.OP_MOD: {
		        TValue rb = RKB(L, base_, i, k);
		        TValue rc = RKC(L, base_, i, k);
		        lua_Number nb = 0; lua_Number nc = 0;
		        if (ttisinteger(rb) && ttisinteger(rc)) {
		          lua_Integer ib = ivalue(rb); lua_Integer ic = ivalue(rc);
		          setivalue(ra, luaV_mod(L, ib, ic));
		        }
		        else if (tonumber(ref rb, ref nb)!=0 && tonumber(ref rc, ref nc)!=0) {
		          setnvalue(ra, luai_nummod(L, nb, nc));
		        }
		        else { 
		        	//Protect(
		        		luaT_trybinTM(L, rb, rc, ra, TMS.TM_MOD);
		        	base_ = ci.u.l.base_;
		        	//);
		        }
				break;
			  }
			  case OpCode.OP_POW: {
		        TValue rb = RKB(L, base_, i, k);
		        TValue rc = RKC(L, base_, i, k);
		        lua_Number nb = 0; lua_Number nc = 0;
		        if (ttisinteger(rb) && ttisinteger(rc)) {
		          lua_Integer ib = ivalue(rb); lua_Integer ic = ivalue(rc);
		          setivalue(ra, luaV_pow(L, ib, ic));
		        }
		        else if (tonumber(ref rb, ref nb)!=0 && tonumber(ref rc, ref nc)!=0) {
		          setnvalue(ra, luai_numpow(L, nb, nc));
		        }
		        else { 
		        	//Protect(
		        		luaT_trybinTM(L, rb, rc, ra, TMS.TM_POW);
		        	base_ = ci.u.l.base_;
		        	//); 
		        }
				break;
			  }
			  case OpCode.OP_UNM: {
		        TValue rb = RB(L, base_, i);
		        lua_Number nb = 0;
		        if (ttisinteger(rb)) {
		          lua_Integer ib = ivalue(rb);
		          setivalue(ra, intop_minus(0, ib));
		        }
		        else if (tonumber(ref rb, ref nb)!=0) {
		          setnvalue(ra, luai_numunm(L, nb));
		        }
		        else {
		          	//Protect(
		          		luaT_trybinTM(L, rb, rb, ra, TMS.TM_UNM);
		          	base_ = ci.u.l.base_;
		        	//);
		        }
				break;
			  }
			  case OpCode.OP_BNOT: {
		        TValue rb = RB(L, base_, i);
		        lua_Integer ib = 0;
		        if (0!=tointeger(ref rb, ref ib)) {
		          setivalue(ra, intop_xor(cast_integer(-1), ib));
		        }
		        else {
		          	//Protect(
		        		luaT_trybinTM(L, rb, rb, ra, TMS.TM_BNOT);
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
						L.top = ci.top;  /* restore top */
                    }); 
					luai_threadyield(L);
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
				 if (luaV_equalobj(L, rb, rc) != GETARG_A(i))
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
				//FIXME: no break;
			  }
			  case OpCode.OP_FORLOOP: {
				if (ttisinteger(ra)) {  /* integer loop? */
		          lua_Integer step = ivalue(ra + 2);
		          lua_Integer idx = ivalue(ra) + step; /* increment index */
		          lua_Integer limit = ivalue(ra + 1);
		          if ((0 < step) ? (idx <= limit) : (limit <= idx)) {
		          	InstructionPtr.inc(ref ci.u.l.savedpc, GETARG_sBx(i));  /* jump back */
		            setivalue(ra, idx);  /* update internal index... */
		            setivalue(ra + 3, idx);  /* ...and external index */
		          }
		        }
		        else {  /* floating loop */
		          lua_Number step = fltvalue(ra + 2);
		          lua_Number idx = luai_numadd(L, fltvalue(ra), step); /* inc. index */
		          lua_Number limit = fltvalue(ra + 1);
		          if (luai_numlt(0, step) ? luai_numle(idx, limit)
		                                  : luai_numle(limit, idx)) {
		          	InstructionPtr.inc(ref ci.u.l.savedpc, GETARG_sBx(i));  /* jump back */
		            setnvalue(ra, idx);  /* update internal index... */
		            setnvalue(ra + 3, idx);  /* ...and external index */
		          }
		        }
				break;
			  }
			  case OpCode.OP_FORPREP: {
				TValue init = ra;
		        TValue plimit = ra + 1;
		        TValue pstep = ra + 2;
		        if (ttisinteger(init) && ttisinteger(plimit) && ttisinteger(pstep)) {
		          /* all values are integer */
		          setivalue(init, ivalue(init) - ivalue(pstep));
		        }
		        else {  /* try making all values floats */
		          lua_Number ninit = 0; lua_Number nlimit = 0; lua_Number nstep = 0;
		          if (0==tonumber(ref plimit, ref nlimit))
		            luaG_runerror(L, LUA_QL("for") + " limit must be a number");
		          setnvalue(plimit, nlimit);
		          if (0==tonumber(ref pstep, ref nstep))
		            luaG_runerror(L, LUA_QL("for") + " step must be a number");
		          setnvalue(pstep, nstep);
		          if (0==tonumber(ref init, ref ninit))
		            luaG_runerror(L, LUA_QL("for") + " initial value must be a number");
		          setnvalue(init, luai_numsub(L, ninit, nstep));
		        }
		        InstructionPtr.inc(ref ci.u.l.savedpc, GETARG_sBx(i));
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
				//break; //FIXME: no break;
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
				  luaH_setint(L, h, last--, val);
				  luaC_barrierback(L, h, val);
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
                    }); 
					luai_threadyield(L);
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

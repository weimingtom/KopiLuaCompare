/*
** $Id: lvm.c,v 2.268 2016/02/05 19:59:14 roberto Exp $
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



		/*
		** 'l_intfitsf' checks whether a given integer can be converted to a
		** float without rounding. Used in comparisons. Left undefined if
		** all integers fit in a float precisely.
		*/
		//#if !defined(l_intfitsf)

		/* number of bits in the mantissa of a float */
		private const int NBM = 53;//		(l_mathlim(MANT_DIG))

		/*
		** Check whether some integers may not fit in a float, that is, whether
		** (maxinteger >> NBM) > 0 (that implies (1 << NBM) <= maxinteger).
		** (The shifts are done in parts to avoid shifting by more than the size
		** of an integer. In a worst case, NBM == 113 for long double and
		** sizeof(integer) == 32.)
		*/
		//#if ((((LUA_MAXINTEGER >> (NBM / 4)) >> (NBM / 4)) >> (NBM / 4)) \
		//	>> (NBM - (3 * (NBM / 4))))  >  0

		private static bool l_intfitsf(lua_Integer i)  {
			return (-((lua_Integer)1 << NBM) <= (i) && (i) <= ((lua_Integer)1 << NBM)); } 

		//#endif

		//#endif


		/*
		** Try to convert a value to a float. The float case is already handled
		** by the macro 'tonumber'.
		*/
		public static int luaV_tonumber_ (TValue obj, ref lua_Number n) {
		  TValue v = new TValue();
		  if (ttisinteger(obj)) {
		    n = cast_num(ivalue(obj));
		    return 1;
		  }
		  else if (0!=cvt2num(obj) &&  /* string convertible to number? */
		            luaO_str2num(svalue(obj), v) == vslen(obj) + 1) {
		    n = nvalue(v);  /* convert result of 'luaO_str2num' to a float */
		    return 1;
		  }
		  else
		    return 0;  /* conversion failed */
		}


		/*
		** try to convert a value to an integer, rounding according to 'mode':
		** mode == 0: accepts only integral values
		** mode == 1: takes the floor of the number
		** mode == 2: takes the ceil of the number
		*/
		public static int luaV_tointeger (TValue obj, ref lua_Integer p, int mode) {
		  TValue v = new TValue();
		 again:
		  if (ttisfloat(obj)) {
		    lua_Number n = fltvalue(obj);
		    lua_Number f = l_floor(n);
		    if (n != f) {  /* not an integral value? */
		      if (mode == 0) return 0;  /* fails if mode demands integral value */
		      else if (mode > 1)  /* needs ceil? */
		        f += 1;  /* convert floor to ceil (remember: n != f) */
		    }
		    return lua_numbertointeger(f, ref p);
		  }
		  else if (ttisinteger(obj)) {
		    p = ivalue(obj);
		    return 1;
		  }
		  else if (0!=cvt2num(obj) &&
		            luaO_str2num(svalue(obj), v) == vslen(obj) + 1) {
		    obj = v;
		    goto again;  /* convert result from 'luaO_str2num' to an integer */
		  }
		  return 0;  /* conversion failed */
		}


		/*
		** Try to convert a 'for' limit to an integer, preserving the
		** semantics of the loop.
		** (The following explanation assumes a non-negative step; it is valid
		** for negative steps mutatis mutandis.)
		** If the limit can be converted to an integer, rounding down, that is
		** it.
		** Otherwise, check whether the limit can be converted to a number.  If
		** the number is too large, it is OK to set the limit as LUA_MAXINTEGER,
		** which means no limit.  If the number is too negative, the loop
		** should not run, because any initial integer value is larger than the
		** limit. So, it sets the limit to LUA_MININTEGER. 'stopnow' corrects
		** the extreme case when the initial value is LUA_MININTEGER, in which
		** case the LUA_MININTEGER limit would still run the loop once.
		*/
		private static int forlimit (TValue obj, ref lua_Integer p, lua_Integer step,
		                     ref int stopnow) {
		  stopnow = 0;  /* usually, let loops run */
		  if (0==luaV_tointeger(obj, ref p, (step < 0 ? 2 : 1))) {  /* not fit in integer? */
		    lua_Number n = 0;  /* try to convert to float */
		    if (0==tonumber(ref obj, ref n)) /* cannot convert to float? */
		      return 0;  /* not a number */
		    if (luai_numlt(0, n)) {  /* if true, float is larger than max integer */
		      p = LUA_MAXINTEGER;
		      if (step < 0) stopnow = 1;
		    }
		    else {  /* float is smaller than min integer */
		      p = LUA_MININTEGER;
		      if (step >= 0) stopnow = 1;
		    }
		  }
		  return 1;
		}


		/*
		** Finish the table access 'val = t[key]'.
		** if 'slot' is NULL, 't' is not a table; otherwise, 'slot' points to
		** t[k] entry (which must be nil).
		*/
		public static void luaV_finishget (lua_State L, TValue t, TValue key, StkId val,
		                      TValue slot) {
		  int loop;  /* counter to avoid infinite loops */
		  TValue tm;  /* metamethod */
		  for (loop = 0; loop < MAXTAGLOOP; loop++) {
		    if (slot == null) {  /* 't' is not a table? */
		      lua_assert(!ttistable(t));
		      tm = luaT_gettmbyobj(L, t, TMS.TM_INDEX);
		      if (ttisnil(tm))
		        luaG_typeerror(L, t, "index");  /* no metamethod */
		      /* else will try the metamethod */
		    }
		    else {  /* 't' is a table */
		      lua_assert(ttisnil(slot));
		      tm = fasttm(L, hvalue(t).metatable, TMS.TM_INDEX);  /* table's metamethod */
		      if (tm == null) {  /* no metamethod? */
		        setnilvalue(val);  /* result is nil */
		        return;
		      }
		      /* else will try the metamethod */
		    }
		    if (ttisfunction(tm)) {  /* is metamethod a function? */
		      luaT_callTM(L, tm, t, key, val, 1);  /* call it */
		      return;
		    }
		    t = tm;  /* else try to access 'tm[key]' */
		    if (0!=luaV_fastget_luaH_get(L,t,key,ref slot)) {  /* fast track? */
		      setobj2s(L, val, slot);  /* done */
		      return;
		    }
		    /* else repeat (tail call 'luaV_finishget') */
		  }
		  luaG_runerror(L, "'__index' chain too long; possible loop");
		}


		public static bool luaV_settable_sub(lua_State L, TValue t, TValue key, ref TValue oldval) //FIXME:added
		{
			oldval = luaH_newkey(L, hvalue(t), key);
			return true;
		}
		/*
		** Finish a table assignment 't[key] = val'.
		** If 'slot' is NULL, 't' is not a table.  Otherwise, 'slot' points
		** to the entry 't[key]', or to 'luaO_nilobject' if there is no such
		** entry.  (The value at 'slot' must be nil, otherwise 'luaV_fastset'
		** would have done the job.)
		*/
		public static void luaV_finishset (lua_State L, TValue t, TValue key,
		                     StkId val, TValue slot) {
		  int loop;  /* counter to avoid infinite loops */
		  for (loop = 0; loop < MAXTAGLOOP; loop++) {
		    TValue tm;  /* '__newindex' metamethod */
		   if (slot != null) {  /* is 't' a table? */
		      Table h = hvalue(t);  /* save 't' table */
		      lua_assert(ttisnil(slot));  /* old value must be nil */
		      tm = fasttm(L, h.metatable, TMS.TM_NEWINDEX);  /* get metamethod */
		      if (tm == null) {  /* no metamethod? */
		        if (slot == luaO_nilobject)  /* no previous entry? */
		          slot = luaH_newkey(L, h, key);  /* create one */
		        /* no metamethod and (now) there is an entry with given key */
		        setobj2t(L, (TValue)(slot), val);  /* set its new value */
		        invalidateTMcache(h);
		        luaC_barrierback(L, h, val);
		        return;
		      }
		      /* else will try the metamethod */
		    }
		    else {  /* not a table; check metamethod */
		      if (ttisnil(tm = luaT_gettmbyobj(L, t, TMS.TM_NEWINDEX)))
		        luaG_typeerror(L, t, "index");
		    }
		    /* try the metamethod */
		    if (ttisfunction(tm)) {
		      luaT_callTM(L, tm, t, key, val, 0);
		      return;
		    }
		    t = tm;  /* else repeat assignment over 'tm' */
		    if (0!=luaV_fastset_luaH_get(L, t, key, ref slot, val))
		      return;  /* done */
		    /* else loop */
		  }
		  luaG_runerror(L, "'__newindex' chain too long; possible loop");
		}


		/*
		** Compare two strings 'ls' x 'rs', returning an integer smaller-equal-
		** -larger than zero if 'ls' is smaller-equal-larger than 'rs'.
		** The code is a little tricky because it allows '\0' in the strings
		** and it uses 'strcoll' (to respect locales) for each segments
		** of the strings.
		*/
		private static int l_strcmp (TString ls, TString rs) {
		  CharPtr l = getstr(ls);
		  uint ll = tsslen(ls);
		  CharPtr r = getstr(rs);
		  uint lr = tsslen(rs);
		  for (;;) {  /* for each segment */
		    //int temp = strcoll(l, r);
		      int temp = String.Compare(l.ToString(), r.ToString());
		    if (temp != 0)  /* not equal? */
			  return temp;  /* done */
		    else {  /* strings are equal up to a '\0' */
		      uint len = (uint)l.ToString().Length;  /* index of first '\0' in both strings */
		      if (len == lr)  /* 'rs' is finished? */
		        return (len == ll) ? 0 : 1;  /* check 'ls' */
		      else if (len == ll)  /* 'ls' is finished? */
		        return -1;  /* 'ls' is smaller than 'rs' ('rs' is not finished) */
		      /* both strings longer than 'len'; go on comparing after the '\0' */
		      len++;
		      l += len; ll -= len; r += len; lr -= len;
		    }
		  }
		}


		/*
		** Check whether integer 'i' is less than float 'f'. If 'i' has an
		** exact representation as a float ('l_intfitsf'), compare numbers as
		** floats. Otherwise, if 'f' is outside the range for integers, result
		** is trivial. Otherwise, compare them as integers. (When 'i' has no
		** float representation, either 'f' is "far away" from 'i' or 'f' has
		** no precision left for a fractional part; either way, how 'f' is
		** truncated is irrelevant.) When 'f' is NaN, comparisons must result
		** in false.
		*/
		private static int LTintfloat (lua_Integer i, lua_Number f) {
		//#if defined(l_intfitsf)
		  if (!l_intfitsf(i)) {
		    if (f >= -cast_num(LUA_MININTEGER))  /* -minint == maxint + 1 */
		      return 1;  /* f >= maxint + 1 > i */
		    else if (f > cast_num(LUA_MININTEGER))  /* minint < f <= maxint ? */
		    return (i < (lua_Integer)(f))?1:0;  /* compare them as integers */
		    else  /* f <= minint <= i (or 'f' is NaN)  -->  not(i < f) */
		      return 0;
		  }
		//#endif
		  return luai_numlt(cast_num(i), f)?1:0;  /* compare them as floats */
		}


		/*
		** Check whether integer 'i' is less than or equal to float 'f'.
		** See comments on previous function.
		*/
		private static int LEintfloat (lua_Integer i, lua_Number f) {
		//#if defined(l_intfitsf)
		  if (!l_intfitsf(i)) {
		    if (f >= -cast_num(LUA_MININTEGER))  /* -minint == maxint + 1 */
		      return 1;  /* f >= maxint + 1 > i */
		    else if (f >= cast_num(LUA_MININTEGER))  /* minint <= f <= maxint ? */
		      return (i <= (lua_Integer)(f))?1:0;  /* compare them as integers */
		    else  /* f < minint <= i (or 'f' is NaN)  -->  not(i <= f) */
		      return 0;
		  }
		//#endif
		  return luai_numle(cast_num(i), f)?1:0;  /* compare them as floats */
		}


		/*
		** Return 'l < r', for numbers.
		*/
		private static int LTnum (TValue l, TValue r) {
		  if (ttisinteger(l)) {
		    lua_Integer li = ivalue(l);
		    if (ttisinteger(r))
		      return (li < ivalue(r))?1:0;  /* both are integers */
		    else  /* 'l' is int and 'r' is float */
		      return LTintfloat(li, fltvalue(r));  /* l < r ? */
		  }
		  else {
		    lua_Number lf = fltvalue(l);  /* 'l' must be float */
		    if (ttisfloat(r))
		      return luai_numlt(lf, fltvalue(r))?1:0;  /* both are float */
		    else if (luai_numisnan(lf))  /* 'r' is int and 'l' is float */
		      return 0;  /* NaN < i is always false */
		    else  /* without NaN, (l < r)  <-->  not(r <= l) */
		      return (0==LEintfloat(ivalue(r), lf))?1:0;  /* not (r <= l) ? */
		  }
		}


		/*
		** Return 'l <= r', for numbers.
		*/
		private static int LEnum (TValue l, TValue r) {
		  if (ttisinteger(l)) {
		    lua_Integer li = ivalue(l);
		    if (ttisinteger(r))
		      return (li <= ivalue(r))?1:0;  /* both are integers */
		    else  /* 'l' is int and 'r' is float */
		      return LEintfloat(li, fltvalue(r));  /* l <= r ? */
		  }
		  else {
		    lua_Number lf = fltvalue(l);  /* 'l' must be float */
		    if (ttisfloat(r))
		      return luai_numle(lf, fltvalue(r))?1:0;  /* both are float */
		    else if (luai_numisnan(lf))  /* 'r' is int and 'l' is float */
		      return 0;  /*  NaN <= i is always false */
		    else  /* without NaN, (l <= r)  <-->  not(r < l) */
		      return (0==LTintfloat(ivalue(r), lf))?1:0;  /* not (r < l) ? */
		  }
		}


		/*
		** Main operation less than; return 'l < r'.
		*/
		public static int luaV_lessthan (lua_State L, TValue l, TValue r) {
		  int res;
		  if (ttisnumber(l) && ttisnumber(r))  /* both operands are numbers? */
    		return LTnum(l, r);
		  else if (ttisstring(l) && ttisstring(r))  /* both are strings? */
		  	return (l_strcmp(tsvalue(l), tsvalue(r)) < 0) ? 1 : 0;
		  else if ((res = luaT_callorderTM(L, l, r, TMS.TM_LT)) < 0)  /* no metamethod? */
		    luaG_ordererror(L, l, r);  /* error */
		  return res;
		}


		/*
		** Main operation less than or equal to; return 'l <= r'. If it needs
		** a metamethod and there is no '__le', try '__lt', based on
		** l <= r iff !(r < l) (assuming a total order). If the metamethod
		** yields during this substitution, the continuation has to know
		** about it (to negate the result of r<l); bit CIST_LEQ in the call
		** status keeps that information.
		*/
		public static int luaV_lessequal (lua_State L, TValue l, TValue r) {
		  int res;
		  if (ttisnumber(l) && ttisnumber(r))  /* both operands are numbers? */
		    return LEnum(l, r);
		  else if (ttisstring(l) && ttisstring(r))  /* both are strings? */
		    return (l_strcmp(tsvalue(l), tsvalue(r)) <= 0)?1:0;
		  else if ((res = luaT_callorderTM(L, l, r, TMS.TM_LE)) >= 0)  /* try 'le' */
		    return res;
		  else {  /* try 'lt': */
		    L.ci.callstatus |= CIST_LEQ;  /* mark it is doing 'lt' for 'le' */
		    res = luaT_callorderTM(L, r, l, TMS.TM_LT);
		    L.ci.callstatus ^= CIST_LEQ;  /* clear mark */
		    if (res < 0)
		      luaG_ordererror(L, l, r);
		    return (0==res)?1:0;  /* result is negated */
		  }
		}

		/*
		** Main operation for equality of Lua values; return 't1 == t2'.
		** L == NULL means raw equality (no metamethods)
		*/
		public static int luaV_equalobj (lua_State L, TValue t1, TValue t2) {
		  TValue tm;
		  if (ttype(t1) != ttype(t2)) {  /* not the same variant? */
		    if (ttnov(t1) != ttnov(t2) || ttnov(t1) != LUA_TNUMBER)
		      return 0;  /* only numbers can be equal with different variants */
		    else {  /* two numbers with different variants */
		      lua_Integer i1 = 0, i2 = 0;  /* compare them as integers */
		      return (0!=tointeger(ref t1, ref i1) && 0!=tointeger(ref t2, ref i2) && i1 == i2)?1:0;
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
		    case LUA_TSHRSTR: return (eqshrstr(tsvalue(t1), tsvalue(t2)))?1:0;
		    case LUA_TLNGSTR: return luaS_eqlngstr(tsvalue(t1), tsvalue(t2));
		    case LUA_TUSERDATA: {
		      if (uvalue(t1) == uvalue(t2)) return 1;
		      else if (L == null) return 0;
		      tm = fasttm(L, uvalue(t1).metatable, TMS.TM_EQ);
		      if (tm == null)
		        tm = fasttm(L, uvalue(t2).metatable, TMS.TM_EQ);
		      break;  /* will try TM */
		    }
		    case LUA_TTABLE: {
		      if (hvalue(t1) == hvalue(t2)) return 1;
		      else if (L == null) return 0;
		      tm = fasttm(L, hvalue(t1).metatable, TMS.TM_EQ);
		      if (tm == null)
		        tm = fasttm(L, hvalue(t2).metatable, TMS.TM_EQ);
		      break;  /* will try TM */
		    }
		    default:
		      return (gcvalue(t1) == gcvalue(t2)) ? 1 : 0;
		  }
		  if (tm == null)  /* no TM? */
		    return 0;  /* objects are different */
		  luaT_callTM(L, tm, t1, t2, L.top, 1);  /* call TM */
		  return l_isfalse(L.top)==0 ? 1 : 0;
		}


		/* macro used by 'luaV_concat' to ensure that element at 'o' is a string */
		private static int tostring(lua_State L, TValue o) {
			if (!ttisstring(o)) { if (0!=cvt2str(o)) { luaO_tostring(L, o); return 1; } return 0; } return 1; } //FIXME:???

		private static bool isemptystr(TValue o) { return (ttisshrstring(o) && tsvalue(o).shrlen == 0); }

		/* copy strings in stack from top - n up to top - 1 to buffer */
		private static void copy2buff (StkId top, int n, CharPtr buff) {
		  uint tl = 0;  /* size already copied */
		  do {
		    uint l = vslen(top - n);  /* length of string being copied */
		    memcpy(buff + tl, svalue(top - n), l * 1/*sizeof(char)*/);
		    tl += l;
		  } while (--n > 0);
		}


		/*
		** Main operation for concatenation: concat 'total' values in the stack,
		** from 'L->top - total' up to 'L->top - 1'.
		*/
		public static void luaV_concat (lua_State L, int total) {
		   lua_assert(total >= 2);
		  do {
		    StkId top = L.top;
		    int n = 2;  /* number of elements handled in this pass (at least 2) */
		    if (!(ttisstring(top-2) || 0!=cvt2str(top-2)) || 0==tostring(L, top-1))
		      luaT_trybinTM(L, top-2, top-1, top-2, TMS.TM_CONCAT);
		    else if (isemptystr(top - 1))  /* second operand is empty? */
		      tostring(L, top - 2);  /* result is first operand */
		    else if (isemptystr(top - 2)) {  /* first operand is an empty string? */
		      setobjs2s(L, top - 2, top - 1);  /* result is second op. */
		    }
		    else {
		      /* at least two non-empty string values; get as many as possible */
		      uint tl = vslen(top - 1);
		      TString ts;
		      /* collect total length and number of strings */
		      for (n = 1; n < total && 0!=tostring(L, top - n - 1); n++) {
		        uint l = vslen(top - n - 1);
		        if (l >= (MAX_SIZE/1/*sizeof(char)*/) - tl)
		          luaG_runerror(L, "string length overflow");
		        tl += l;
		      }
		      if (tl <= LUAI_MAXSHORTLEN) {  /* is result a short string? */
		      	CharPtr buff = new CharPtr(new char[LUAI_MAXSHORTLEN]);
		        copy2buff(top, n, buff);  /* copy strings to buffer */
		        ts = luaS_newlstr(L, buff, tl);
		      }
		      else {  /* long string; copy strings directly to final result */
		        ts = luaS_createlngstrobj(L, tl);
		        copy2buff(top, n, getstr(ts));
		      }
		      setsvalue2s(L, top - n, ts);  /* create result */
		    }
		    total -= n-1;  /* got 'n' strings to create 1 new */
		    L.top -= n-1;  /* popped 'n' strings and pushed one */
		  } while (total > 1);  /* repeat until only 1 result left */
		}


		/*
		** Main operation 'ra' = #rb'.
		*/
		private static void luaV_objlen (lua_State L, StkId ra, /*const*/ TValue rb) {
		  TValue tm;
		  switch (ttype(rb)) {
		    case LUA_TTABLE: {
		      Table h = hvalue(rb);
		      tm = fasttm(L, h.metatable, TMS.TM_LEN);
		      if (tm!=null) break;  /* metamethod? break switch to call it */
		      setivalue(ra, luaH_getn(h));  /* else primitive len */
		      return;
		    }
		    case LUA_TSHRSTR: {
		      setivalue(ra, tsvalue(rb).shrlen);
		      return;
		    }
		    case LUA_TLNGSTR: {
		  	  setivalue(ra, (int)tsvalue(rb).u.lnglen);
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


		/*
		** Integer division; return 'm // n', that is, floor(m/n).
		** C division truncates its result (rounds towards zero).
		** 'floor(q) == trunc(q)' when 'q >= 0' or when 'q' is integer,
		** otherwise 'floor(q) == trunc(q) - 1'.
		*/
		public static lua_Integer luaV_div (lua_State L, lua_Integer m, lua_Integer n) {
		  if (l_castS2U(n) + 1u <= 1u) {  /* special cases: -1 or 0 */
		    if (n == 0)
		      luaG_runerror(L, "attempt to divide by zero");
		    return intop_minus(0, m);   /* n==-1; avoid overflow with 0x80000...//-1 */
		  }
		  else {
		    lua_Integer q = m / n;  /* perform C division */
		    if ((m ^ n) < 0 && m % n != 0)  /* 'm/n' would be negative non-integer? */
		      q -= 1;  /* correct result for different rounding */
		    return q;
		  }
		}


		/*
		** Integer modulus; return 'm % n'. (Assume that C '%' with
		** negative operands follows C99 behavior. See previous comment
		** about luaV_div.)
		*/
		public static lua_Integer luaV_mod (lua_State L, lua_Integer m, lua_Integer n) {
		  if (l_castS2U(n) + 1u <= 1u) {  /* special cases: -1 or 0 */
		    if (n == 0)
		      luaG_runerror(L, "attempt to perform 'n%%0'");
		    return 0;   /* m % -1 == 0; avoid overflow with 0x80000...%-1 */
		  }
		  else {
		    lua_Integer r = m % n;
		    if (r != 0 && (m ^ n) < 0)  /* 'm/n' would be non-integer negative? */
		      r += n;  /* correct result for different rounding */
		    return r;
		  }
		}


		/* number of bits in an integer */
		public static int NBITS = cast_int(sizeof(lua_Integer) * CHAR_BIT);

		/*
		** Shift left operation. (Shift right just negates 'y'.)
		*/
		public static lua_Integer luaV_shiftl (lua_Integer x, lua_Integer y) {
		  if (y < 0) {  /* shift right? */
		    if (y <= -NBITS) return 0;
		    else return intop_shiftright(x, -y);
		  }
		  else {  /* shift left */
		    if (y >= NBITS) return 0;
		    else return intop_shiftleft(x, y);
		  }
		}


		/*
		** check whether cached closure in prototype 'p' may be reused, that is,
		** whether there is a cached closure with the same upvalues needed by
		** new closure to be created.
		*/
		private static LClosure getcached (Proto p, UpVal[] encup, StkId base_) {
		  LClosure c = p.cache;
		  if (c != null) {  /* is there a cached closure? */
		    int nup = p.sizeupvalues;
		    Upvaldesc[] uv = p.upvalues;
		    int i;
		    for (i = 0; i < nup; i++) {  /* check whether it has right upvalues */
		      TValue v = uv[i].instack!=0 ? base_ + uv[i].idx : encup[uv[i].idx].v;
		      if (c.upvals[i].v != v)
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
		  LClosure ncl = luaF_newLclosure(L, nup);
  		  ncl.p = p;
		  setclLvalue(L, ra, ncl);  /* anchor new closure in stack */
		  for (i = 0; i < nup; i++) {  /* fill in its upvalues */
		    if (uv[i].instack!=0)  /* upvalue refers to local variable? */
		      ncl.upvals[i] = luaF_findupval(L, base_ + uv[i].idx);
		    else  /* get upvalue from enclosing function */
		      ncl.upvals[i] = encup[uv[i].idx];
		    ncl.upvals[i].refcount++;
		    /* new closure is white, so we do not need a barrier here */			  
		  }
		  if (!isblack(p))  /* cache will not break GC invariant? */
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
		  	  if (0!=(ci.callstatus & CIST_LEQ)) {  /* "<=" using "<" instead? */
		        lua_assert(op == OpCode.OP_LE);
		        ci.callstatus ^= CIST_LEQ;  /* clear mark */
		        res = (0==res)?1:0;  /* negate result */
		      }
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
		** {==================================================================
		** Function 'luaV_execute': main interpreter loop
		** ===================================================================
		*/
		

		/*
		** some macros for common tasks in 'luaV_execute'
		*/


		//#define RA(i)	(base+GETARG_A(i))
		//#define RB(i)	check_exp(getBMode(GET_OPCODE(i)) == OpArgMask.OpArgR, base+GETARG_B(i))
		//#define RC(i)	check_exp(getCMode(GET_OPCODE(i)) == OpArgMask.OpArgR, base+GETARG_C(i))
		//#define RKB(i)	check_exp(getBMode(GET_OPCODE(i)) == OpArgMask.OpArgK, \
			//ISK(GETARG_B(i)) ? k+INDEXK(GETARG_B(i)) : base+GETARG_B(i))
		//#define RKC(i)	check_exp(getCMode(GET_OPCODE(i)) == OpArgMask.OpArgK, \
		//	ISK(GETARG_C(i)) ? k+INDEXK(GETARG_C(i)) : base+GETARG_C(i))
		
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
		    if (a != 0) luaF_close(L, ci.u.l.base_ + a - 1);
		    InstructionPtr.inc(ref ci.u.l.savedpc, GETARG_sBx(i) + e); }

		/* for test instructions, execute the jump instruction that follows it */
		public static void donextjump(CallInfo ci, ref Instruction i, lua_State L)	{ i = ci.u.l.savedpc[0]; dojump(ci, i, 1, L); }


		//#define Protect(x)	{ {x;}; base = ci->u.l.base_; } //FIXME:

		//#define checkGC(L,c)	\
		//	{ luaC_condGC(L, L->top = (c),  /* limit of live values */ \
		//                         Protect(L->top = ci->top));  /* restore top */ \
		//           luai_threadyield(L); }
		
#if false
		{     
		  if (G(L).GCdebt > 0) {
		    L->top = (c); /* limit of live values */
		    luaC_step(L); 
		    //Protect(
				L->top = ci->top;  /* restore top */
			base_ = ci.u.l.base_;
			//);
		  }
		  //condchangemem(L,pre,pos); //empty
		  luai_threadyield(L); 
		}
#endif


		/* fetch an instruction and prepare its execution */
		//#define vmfetch()	{ \
		//  i = *(ci->u.l.savedpc++); \
		//  if (L->hookmask & (LUA_MASKLINE | LUA_MASKCOUNT)) \
		//    Protect(luaG_traceexec(L)); \
		//  ra = RA(i); /* WARNING: any stack reallocation invalidates 'ra' */ \
		//  lua_assert(base == ci->u.l.base); \
		//  lua_assert(base <= L->top && L->top < L->stack + L->stacksize); \
		//}
		private static void vmfetch(ref Instruction i, ref lua_State L, ref StkId ra, ref CallInfo ci, ref StkId base_) {
		  i = ci.u.l.savedpc[0]; InstructionPtr.inc(ref ci.u.l.savedpc); //FIXME:++
		  if (0!=(L.hookmask & (LUA_MASKLINE | LUA_MASKCOUNT))) {
		    //Protect(
			  luaG_traceexec(L);
			base_ = ci.u.l.base_;
			//);
          }
		  ra = RA(L, base_, i); /* WARNING: any stack reallocation invalidates 'ra' */
		  lua_assert(base_ == ci.u.l.base_);
		  lua_assert(base_ <= L.top && L.top <= L.stack[L.stacksize-1]); //FIXME:L.top < L.stack[L.stacksize]??? L.stacksize >= L.stack.Length, overflow, so changed to <=
		}

		//#define vmdispatch(o)	switch(o)
		//#define vmcase(l)	case l:
		//#define vmbreak		break


		/*
		** copy of 'luaV_gettable', but protecting the call to potential
		** metamethod (which can reallocate the stack)
		*/
		//#define gettableProtected(L,t,k,v)  { const TValue *slot; \
		//  if (luaV_fastget(L,t,k,slot,luaH_get)) { setobj2s(L, v, slot); } \
		//  else Protect(luaV_finishget(L,t,k,v,slot)); }
#if false
{ 
  TValue slot = null;
  if (0!=luaV_fastget_luaH_get(L,t,k,ref slot)) { 
    setobj2s(L, v, slot); 
  }
  else {
    //Protect(
		luaV_finishget(L,t,k,v,slot);
	base_ = ci.u.l.base_;
	//);    
  }
}
#endif

		/* same for 'luaV_settable' */
		//#define settableProtected(L,t,k,v) { const TValue *slot; \
		//  if (!luaV_fastset(L,t,k,slot,luaH_get,v)) \
		//    Protect(luaV_finishset(L,t,k,v,slot)); }
#if false
{ 
  TValue slot = null;
  if (0==luaV_fastset_luaH_get(L,t,k,ref slot,v)) { 
    //Protect(
		luaV_finishset(L,t,k,v,slot);
	base_ = ci.u.l.base_;
	//);    
  }
}
#endif


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
		  ci.callstatus |= CIST_FRESH;  /* fresh invocation of 'luaV_execute" */
         newframe:  /* reentry point when frame changes (call/return) */
		  lua_assert(ci == L.ci);
		  cl = clLvalue(ci.func);  /* local reference to function's closure */
		  k = cl.p.k;  /* local reference to function's constant table */
		  base_ = ci.u.l.base_;  /* local copy of function's base */
		  /* main loop of interpreter */
		  for (;;) {
		  	Instruction i = 0;
			StkId ra = null;
			vmfetch(ref i, ref L, ref ra, ref ci, ref base_);
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
			    TValue upval = cl.upvals[GETARG_B(i)].v;
		        TValue rc = RKC(L, base_, i, k);
		        {//gettableProtected(L, upval, rc, ra); 
				  TValue aux = null;
				  if (0!=luaV_fastget_luaH_get(L,upval,rc,ref aux)) { 
				    setobj2s(L, ra, aux); 
				  }
				  else {
				    //Protect(
						luaV_finishget(L,upval,rc,ra,aux);
					base_ = ci.u.l.base_;
					//);    
				  }
				}
				break;
			  }
			  case OpCode.OP_GETTABLE: {
				StkId rb = RB(L, base_, i);
		        TValue rc = RKC(L, base_, i, k);
		        {//gettableProtected(L, rb, rc, ra); 
				  TValue aux = null;
				  if (0!=luaV_fastget_luaH_get(L,rb,rc,ref aux)) { 
				    setobj2s(L, ra, aux); 
				  }
				  else {
				    //Protect(
						luaV_finishget(L,rb,rc,ra,aux);
					base_ = ci.u.l.base_;
					//);    
				  }
				}
				break;
			  }
			  case OpCode.OP_SETTABUP: {
				TValue upval = cl.upvals[GETARG_A(i)].v;
		        TValue rb = RKB(L, base_, i, k);
		        TValue rc = RKC(L, base_, i, k);
		        {//settableProtected(L, upval, rb, rc); 
				  TValue slot = null;
				  if (0==luaV_fastset_luaH_get(L,upval,rb,ref slot,rc)) { 
				    //Protect(
						luaV_finishset(L,upval,rb,rc,slot);
					base_ = ci.u.l.base_;
					//);    
				  }
				}
				break;
			  }
			  case OpCode.OP_SETUPVAL: {
				UpVal uv = cl.upvals[GETARG_B(i)];
				setobj(L, uv.v, ra);
				luaC_upvalbarrier(L, uv);
				break;
			  }
			  case OpCode.OP_SETTABLE: {
				TValue rb = RKB(L, base_, i, k);
		        TValue rc = RKC(L, base_, i, k);
				{//settableProtected(L, ra, rb, rc); 
				  TValue slot = null;
				  if (0==luaV_fastset_luaH_get(L,ra,rb,ref slot,rc)) { 
				    //Protect(
						luaV_finishset(L,ra,rb,rc,slot);
					base_ = ci.u.l.base_;
					//);    
				  }
				}
		        break;
			  }
			  case OpCode.OP_NEWTABLE: {
				int b = GETARG_B(i);
				int c = GETARG_C(i);
		        Table t = luaH_new(L);
		        sethvalue(L, ra, t);
		        if (b != 0 || c != 0)
		          luaH_resize(L, t, (uint)luaO_fb2int(b), (uint)luaO_fb2int(c));
		        {//checkGC(L, ra + 1);
		          if (G(L).GCdebt > 0) {
				    L.top = (ra + 1); /* limit of live values */
				    luaC_step(L); 
				    //Protect(
						L.top = ci.top;  /* restore top */
					base_ = ci.u.l.base_;
					//);
				  }
				  //condchangemem(L,pre,pos); //empty
				  luai_threadyield(L); 
				}
				break;
			  }
			  case OpCode.OP_SELF: {
			    TValue aux = null;
		        StkId rb = RB(L, base_, i);
		        TValue rc = RKC(L, base_, i, k);
		        TString key = tsvalue(rc);  /* key must be a string */
		        setobjs2s(L, ra + 1, rb);
		        if (0!=luaV_fastget_luaH_getstr(L, rb, key, ref aux)) {
		          setobj2s(L, ra, aux);
		        }
		        else {
		        	//Protect(
			        	luaV_finishget(L, rb, rc, ra, aux);
			        base_ = ci.u.l.base_;
					//);
		        }
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
		          setfltvalue(ra, luai_numadd(L, nb, nc));
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
		          setfltvalue(ra, luai_numsub(L, nb, nc));
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
		          setfltvalue(ra, luai_nummul(L, nb, nc));
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
		          setfltvalue(ra, luai_numdiv(L, nb, nc));
		        }
		        else { 
		        	//Protect(
		        		luaT_trybinTM(L, rb, rc, ra, TMS.TM_DIV);
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
		          lua_Number m = 0;
		          luai_nummod(L, nb, ref nc, m);
		          setfltvalue(ra, m);
		        }
		        else { 
		        	//Protect(
		        		luaT_trybinTM(L, rb, rc, ra, TMS.TM_MOD);
		        	base_ = ci.u.l.base_;
		        	//);
		        }
				break;
			  }
		      case OpCode.OP_IDIV: {  /* floor division */
		        TValue rb = RKB(L, base_, i, k);
		        TValue rc = RKC(L, base_, i, k);
		        lua_Number nb = 0; lua_Number nc = 0;
		        if (ttisinteger(rb) && ttisinteger(rc)) {
		          lua_Integer ib = ivalue(rb); lua_Integer ic = ivalue(rc);
		          setivalue(ra, luaV_div(L, ib, ic));
		        }
		        else if (0!=tonumber(ref rb, ref nb) && 0!=tonumber(ref rc, ref nc)) {
		          setfltvalue(ra, luai_numidiv(L, nb, nc));
		        }
		        else { 
		        	//Protect(
		        		luaT_trybinTM(L, rb, rc, ra, TMS.TM_IDIV);
		        	base_ = ci.u.l.base_;
		        	//);
		        }
			  	break;
			  }
			  case OpCode.OP_POW: {
		        TValue rb = RKB(L, base_, i, k);
		        TValue rc = RKC(L, base_, i, k);
		        lua_Number nb = 0; lua_Number nc = 0;
		        if (tonumber(ref rb, ref nb)!=0 && tonumber(ref rc, ref nc)!=0) {
		          setfltvalue(ra, luai_numpow(L, nb, nc));
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
		          setfltvalue(ra, luai_numunm(L, nb));
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
		          setivalue(ra, intop_xor((int)~l_castS2U(0), ib));
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
		        ra = RA(L, base_, i);  /* 'luaV_concat' may invoke TMs and move the stack */
		        rb = base_ + b;
		        setobjs2s(L, ra, rb);
		        {//checkGC(L, (ra >= rb ? ra + 1 : rb));
		          if (G(L).GCdebt > 0) {
				    L.top = (ra >= rb ? ra + 1 : rb); /* limit of live values */
				    luaC_step(L); 
				    //Protect(
						L.top = ci.top;  /* restore top */
					base_ = ci.u.l.base_;
					//);
				  }
				  //condchangemem(L,pre,pos); //empty
				  luai_threadyield(L); 
				}
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
		      	  if (nresults >= 0)
		            L.top = ci.top;  /* adjust results */
		          //Protect(
		          	/*(void)0*/   /* update 'base' */
		          base_ = ci.u.l.base_;
		          //);
		        }
		        else {  /* Lua function */
                  ci = L.ci;
		          goto newframe;  /* restart luaV_execute over new Lua function */
		        }
				break;
			  }
			  case OpCode.OP_TAILCALL: {
				int b = GETARG_B(i);
				if (b != 0) L.top = ra + b;  /* else previous instruction set top */
				lua_assert(GETARG_C(i) - 1 == LUA_MULTRET);
		        if (0!=luaD_precall(L, ra, LUA_MULTRET)) {  /* C function? */
		          //Protect(
		          	/*(void)0*/   /* update 'base' */
		          base_ = ci.u.l.base_;
		          //);
		        }
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
				if (cl.p.sizep > 0) luaF_close(L, base_);
				b = luaD_poscall(L, ci, ra, (b != 0 ? b - 1 : cast_int(L.top - ra)));
        		if ((ci.callstatus & CIST_FRESH)!=0)  /* local 'ci' still from callee */
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
		          lua_Integer idx = intop_plus(ivalue(ra), step); /* increment index */
		          lua_Integer limit = ivalue(ra + 1);
		          if ((0 < step) ? (idx <= limit) : (limit <= idx)) {
		          	InstructionPtr.inc(ref ci.u.l.savedpc, GETARG_sBx(i));  /* jump back */
		            chgivalue(ra, idx);  /* update internal index... */
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
		            chgfltvalue(ra, idx);  /* update internal index... */
		            setfltvalue(ra + 3, idx);  /* ...and external index */
		          }
		        }
				break;
			  }
			  case OpCode.OP_FORPREP: {
				TValue init = ra;
		        TValue plimit = ra + 1;
		        TValue pstep = ra + 2;
		        lua_Integer ilimit = 0;
		        int stopnow = 0;
		        if (ttisinteger(init) && ttisinteger(pstep) &&
		            0!=forlimit(plimit, ref ilimit, ivalue(pstep), ref stopnow)) {
		          /* all values are integer */
		          lua_Integer initv = (stopnow!=0 ? 0 : ivalue(init));
		          setivalue(plimit, ilimit);
		          setivalue(init, intop_minus(initv, ivalue(pstep)));
		        }
		        else {  /* try making all values floats */
		          lua_Number ninit = 0; lua_Number nlimit = 0; lua_Number nstep = 0;
		          if (0==tonumber(ref plimit, ref nlimit))
		            luaG_runerror(L, "'for' limit must be a number");
		          setfltvalue(plimit, nlimit);
		          if (0==tonumber(ref pstep, ref nstep))
		            luaG_runerror(L, "'for' step must be a number");
		          setfltvalue(pstep, nstep);
		          if (0==tonumber(ref init, ref ninit))
		            luaG_runerror(L, "'for' initial value must be a number");
		          setfltvalue(init, luai_numsub(L, ninit, nstep));
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
				  luaD_call(L, cb, GETARG_C(i));
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
				uint last;
				Table h;
				if (n == 0) n = cast_int(L.top - ra) - 1;
				if (c == 0) {
                  lua_assert(GET_OPCODE(ci.u.l.savedpc[0]) == OpCode.OP_EXTRAARG);
                  c = GETARG_Ax(ci.u.l.savedpc[0]); InstructionPtr.inc(ref ci.u.l.savedpc); //FIXME:++
				}
				h = hvalue(ra);
				last = (uint)(((c-1)*LFIELDS_PER_FLUSH) + n);
				if (last > h.sizearray)  /* needs more space? */
				  luaH_resizearray(L, h, last);  /* preallocate it at once */
				for (; n > 0; n--) {
				  TValue val = ra+n;
				  luaH_setint(L, h, (int)last, val); last--;
				  luaC_barrierback(L, h, val);
				}
                L.top = ci.top;  /* correct top (in case of previous open call) */
				break;
			  }
			  case OpCode.OP_CLOSURE: {
				Proto p = cl.p.p[GETARG_Bx(i)];
			  	LClosure ncl = getcached(p, cl.upvals, base_);  /* cached closure */
		        if (ncl == null)  /* no match? */
		          pushclosure(L, p, cl.upvals, base_, ra);  /* create a new one */
		        else
		          setclLvalue(L, ra, ncl);  /* push cashed closure */
		        {//checkGC(L, ra + 1);
		          if (G(L).GCdebt > 0) {
				    L.top = (ra + 1); /* limit of live values */
				    luaC_step(L); 
				    //Protect(
						L.top = ci.top;  /* restore top */
					base_ = ci.u.l.base_;
					//);
				  }
				  //condchangemem(L,pre,pos); //empty
				  luai_threadyield(L); 
				}
				break;
			  }
			  case OpCode.OP_VARARG: {
				int b = GETARG_B(i) - 1;  /* required results */
				int j;
				int n = cast_int(base_ - ci.func) - cl.p.numparams - 1;
		        if (n < 0)  /* less arguments than parameters? */
		          n = 0;  /* no vararg arguments */				
		        if (b < 0) {  /* B == 0? */
		          b = n;  /* get all var. arguments */
				  //Protect(
					 luaD_checkstack(L, n);
				  base_ = ci.u.l.base_;
				  //);
				  ra = RA(L, base_, i);  /* previous call may change the stack */
				  L.top = ra + n;
				}
				for (j = 0; j < b && j < n; j++)
		          setobjs2s(L, ra + j, base_ - n + j);
		        for (; j < b; j++)  /* complete required results with nil */
		          setnilvalue(ra + j);
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

/* }================================================================== */

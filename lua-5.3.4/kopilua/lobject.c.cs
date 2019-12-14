/*
** $Id: lobject.c,v 2.113 2016/12/22 13:08:50 roberto Exp $
** Some generic functions over Lua objects
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using lu_byte = System.Byte;
	using lua_Number = System.Double;
	using l_uacNumber = System.Double;
	using Instruction = System.UInt32;
	using lu_int32 = System.UInt32;
	using lua_Integer = System.Int32;
	using lua_Unsigned = System.UInt32;
	using l_uacInt = System.Int32;
	
	public partial class Lua
	{

	
	    //FIXME:moved to lobject.h
        //LUAI_DDEF const TValue luaO_nilobject_ = {NILCONSTANT};

		
		/*
		** converts an integer to a "floating point byte", represented as
		** (eeeeexxx), where the real value is (1xxx) * 2^(eeeee - 1) if
		** eeeee != 0 and (xxx) otherwise.
		*/
		public static int luaO_int2fb (uint x) {
		  int e = 0;  /* expoent */
          if (x < 8) return (int)x;
		  while (x >= (8 << 4)) {  /* coarse steps */
		    x = (x + 0xf) >> 4;  /* x = ceil(x / 16) */
		    e += 4;
		  }		  
		  while (x >= (8 << 1)) {  /* fine steps */
			x = (x + 1) >> 1;  /* x = ceil(x / 2) */
			e++;
		  }
		  return ((e+1) << 3) | (cast_int(x) - 8);
		}


		/* converts back */
		public static int luaO_fb2int (int x) {
		  return (x < 8) ? x : ((x & 7) + 8) << ((x >> 3) - 1);
		}

		/*
		** Computes ceil(log2(x))
		*/
		private static readonly lu_byte[] log_2 = {  /* log_2[i] = ceil(log2(i - 1)) */
		    0,1,2,2,3,3,3,3,4,4,4,4,4,4,4,4,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
		    6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
		    7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
		    7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
		    8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
		    8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
		    8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
		    8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8
		};
		
		public static int luaO_ceillog2 (uint x) {
		  int l = 0;
		  x--;
		  while (x >= 256) { l += 8; x >>= 8; }
		  return l + log_2[x];
		}

		private static lua_Integer intarith (lua_State L, int op, lua_Integer v1,
		                                                   lua_Integer v2) {
		  switch (op) {
		    case LUA_OPADD: return intop_plus(v1, v2);
		    case LUA_OPSUB:return intop_minus(v1, v2);
		    case LUA_OPMUL:return intop_mul(v1, v2);
		    case LUA_OPMOD: return luaV_mod(L, v1, v2);
		    case LUA_OPIDIV: return luaV_div(L, v1, v2);
		    case LUA_OPBAND: return intop_and(v1, v2);
		    case LUA_OPBOR: return intop_or(v1, v2);
		    case LUA_OPBXOR: return intop_xor(v1, v2);
		    case LUA_OPSHL: return luaV_shiftl(v1, v2);
		    case LUA_OPSHR: return luaV_shiftl(v1, -v2);
		    case LUA_OPUNM: return intop_minus(0, v1);
		    case LUA_OPBNOT: return intop_xor((int)(~l_castS2U(0)), v1);
		    default: lua_assert(0); return 0;
		  }
		}
		
		private static lua_Number numarith (lua_State L, int op, lua_Number v1, 
		                                                         lua_Number v2) {
		  switch (op) {
		    case LUA_OPADD: return luai_numadd(L, v1, v2);
		    case LUA_OPSUB: return luai_numsub(L, v1, v2);
		    case LUA_OPMUL: return luai_nummul(L, v1, v2);
		    case LUA_OPDIV: return luai_numdiv(L, v1, v2);
		    case LUA_OPPOW: return luai_numpow(L, v1, v2);
			case LUA_OPIDIV: return luai_numidiv(L, v1, v2);
		    case LUA_OPUNM: return luai_numunm(L, v1);
			case LUA_OPMOD: {
		      lua_Number m = 0;
		      luai_nummod(L, v1, ref v2, m);
		      return m;
		    }
		    default: lua_assert(0); return 0;
		  }
		}

		public static void luaO_arith (lua_State L, int op, TValue p1, TValue p2,
		                 TValue res) {
		  switch (op) {
		    case LUA_OPBAND: case LUA_OPBOR: case LUA_OPBXOR:
		    case LUA_OPSHL: case LUA_OPSHR:
		    case LUA_OPBNOT: {  /* operate only on integers */
		      lua_Integer i1 = 0; lua_Integer i2 = 0;
		      if (0!=tointeger(ref p1, ref i1) && 0!=tointeger(ref p2, ref i2)) {
		        setivalue(res, intarith(L, op, i1, i2));
		        return;
		      }
		      else break;  /* go to the end */
		    }
		    case LUA_OPDIV: case LUA_OPPOW:  {  /* operates only on floats */
		      lua_Number n1 = 0; lua_Number n2 = 0;
		      if (0!=tonumber(ref p1, ref n1) && 0!=tonumber(ref p2, ref n2)) {
		        setfltvalue(res, numarith(L, op, n1, n2));
		        return;
		      }
		      else break;  /* go to the end */
		    }
		    default: {  /* other operations */
		      lua_Number n1 = 0; lua_Number n2 = 0;
		      if (ttisinteger(p1) && ttisinteger(p2)) {
		        setivalue(res, intarith(L, op, ivalue(p1), ivalue(p2)));
		        return;
		      }
		      else if (0!=tonumber(ref p1, ref n1) && 0!=tonumber(ref p2, ref n2)) {
		        setfltvalue(res, numarith(L, op, n1, n2));
		        return;
		      }
		      else break;  /* go to the end */
		    }
		  }
		  /* could not perform raw operation; try metamethod */
		  lua_assert(L != null);  /* should not fail when folding (compile time) */
		  luaT_trybinTM(L, p1, p2, res, (TMS)((op - LUA_OPADD) + TMS.TM_ADD));
		}


		private static int luaO_hexavalue (int c) {
		  if (lisdigit(c)!=0) return c - '0';
		  else return (ltolower(c) - 'a') + 10;
		}




		private static int isneg (ref CharPtr s) {
		  if (s[0] == '-') { s.inc(); return 1; }
		  else if (s[0] == '+') s.inc();
		  return 0;
		}


		/*
		** {==================================================================
		** Lua's implementation for 'lua_strx2number'
		** ===================================================================
		*/
		//#if !defined(lua_strx2number)

		/* maximum number of significant digits to read (to avoid overflows
		   even with single floats) */
		public const int MAXSIGDIG = 30;



		/*
		** convert an hexadecimal numeric string to a number, following
		** C99 specification for 'strtod'
		*/
		private static lua_Number lua_strx2number (CharPtr s, out CharPtr endptr) {
		  s = new CharPtr(s); //FIXME: added
		  int dot = lua_getlocaledecpoint();
		  lua_Number r = 0.0;  /* result (accumulator) */
		  int sigdig = 0;  /* number of significant digits */
		  int nosigdig = 0;  /* number of non-significant digits */
		  int e = 0;  /* exponent correction */
		  int neg;  /* 1 if number is negative */
		  int hasdot = 0;  /* true after seen a dot */
		  endptr = (CharPtr)(s);  /* nothing is valid yet */
		  while (lisspace((byte)(s[0]))!=0) s.inc();  /* skip initial spaces */
		  neg = isneg(ref s);  /* check signal */
		  if (!(s[0] == '0' && (s[1] == 'x' || s[1] == 'X')))  /* check '0x' */
		    return 0.0;  /* invalid format (no '0x') */
		  for (s += 2; ; s.inc()) {  /* skip '0x' and read numeral */
		    if (s[0] == dot) {
		      if (hasdot!=0) break;  /* second dot? stop loop */
		      else hasdot = 1;
		    }
		    else if (lisxdigit((byte)(s[0]))!=0) {
		      if (sigdig == 0 && s[0] == '0')  /* non-significant digit (zero)? */
		        nosigdig++;
		      else if (++sigdig <= MAXSIGDIG)  /* can read it without overflow? */
		        r = (r * cast_num(16.0)) + luaO_hexavalue(s[0]);
		      else e++; /* too many digits; ignore, but still count for exponent */
		      if (0!=hasdot) e--;  /* decimal digit? correct exponent */
		    }
		    else break;  /* neither a dot nor a digit */
		  }
		  if (nosigdig + sigdig == 0)  /* no digits? */
		    return 0.0;  /* invalid format */
		  endptr = (CharPtr)(s);  /* valid up to here */
		  e *= 4;  /* each digit multiplies/divides value by 2^4 */
		  if (s[0] == 'p' || s[0] == 'P') {  /* exponent part? */
		    int exp1 = 0;  /* exponent value */
		    int neg1;  /* exponent signal */
		    s.inc();  /* skip 'p' */
		    neg1 = isneg(ref s);  /* signal */
		    if (0==lisdigit((byte)(s[0])))
		      return 0.0;  /* invalid; must have at least one digit */
		    while (lisdigit((byte)(s[0]))!=0) {  /* read exponent */
		    	exp1 = exp1 * 10 + s[0] - '0'; s.inc();
		    }
		    if (neg1!=0) exp1 = -exp1;
		    e += exp1;
		    endptr = (CharPtr)(s);  /* valid up to here */
		  }
		  if (neg!=0) r = -r;
		  return ldexp(r, e);
		}

		//#endif

		/* }====================================================== */


		/* maximum length of a numeral */
		//#if !defined (L_MAXLENNUM)
		private const int L_MAXLENNUM = 200;
		//#endif

		private static CharPtr l_str2dloc (CharPtr s, out lua_Number result, int mode) {
		  CharPtr endptr;
		  result = (mode == 'x') ? lua_strx2number(s, out endptr)  /* try to convert */
		                          : lua_str2number(s, out endptr);
		  if (endptr == s) return null;  /* nothing recognized? */
		  while (0!=lisspace(cast_uchar(endptr[0]))) endptr.inc();  /* skip trailing spaces */
		  return (endptr[0] == '\0') ? endptr : null;  /* OK if no trailing characters */
		}


		/*
		** Convert string 's' to a Lua number (put in 'result'). Return NULL
		** on fail or the address of the ending '\0' on success.
		** 'pmode' points to (and 'mode' contains) special things in the string:
		** - 'x'/'X' means an hexadecimal numeral
		** - 'n'/'N' means 'inf' or 'nan' (which should be rejected)
		** - '.' just optimizes the search for the common case (nothing special)
		** This function accepts both the current locale or a dot as the radix
		** mark. If the convertion fails, it may mean number has a dot but
		** locale accepts something else. In that case, the code copies 's'
		** to a buffer (because 's' is read-only), changes the dot to the
		** current locale radix mark, and tries to convert again.
		*/
		private static CharPtr l_str2d (CharPtr s, ref lua_Number result) {
		  CharPtr endptr;
		  CharPtr pmode = strpbrk(s, ".xXnN");
		  int mode = pmode != null ? ltolower(cast_uchar(pmode[0])) : 0;
		  if (mode == 'n')  /* reject 'inf' and 'nan' */
		    return null;
		  endptr = l_str2dloc(s, out result, mode);  /* try to convert */
		  if (endptr == null) {  /* failed? may be a different locale */
		  	CharPtr buff = new CharPtr(new char[L_MAXLENNUM + 1]);
		    CharPtr pdot = strchr(s, '.');
		    if (strlen(s) > L_MAXLENNUM || pdot == null)
		      return null;  /* string too long or no dot; fail */
		    strcpy(buff, s);  /* copy string to buffer */
		    buff[pdot - s] = lua_getlocaledecpoint();  /* correct decimal point */
		    endptr = l_str2dloc(buff, out result, mode);  /* try again */
		    if (endptr != null)
		      endptr = s + (endptr - buff);  /* make relative to 's' */
		  }
		  return endptr;
		}


		private static lua_Unsigned MAXBY10	= (lua_Unsigned)(LUA_MAXINTEGER / 10);
		private static int MAXLASTD	= cast_int(LUA_MAXINTEGER % 10);


		private static CharPtr l_str2int (CharPtr s, ref lua_Integer result) {
		  s = new CharPtr(s); //FIXME:added			
		  lua_Unsigned a = 0;
		  int empty = 1;
		  int neg;
		  while (lisspace((byte)(s[0]))!=0) s.inc();  /* skip initial spaces */
		  neg = isneg(ref s);
		  if (s[0] == '0' &&
		      (s[1] == 'x' || s[1] == 'X')) {  /* hex? */
		    s += 2;  /* skip '0x' */
		    for (; lisxdigit((byte)(s[0]))!=0; s.inc()) {
		      a = (uint)(a * 16 + luaO_hexavalue(s[0]));
		      empty = 0;
		    }
		  }
		  else {  /* decimal */
		  	for (; lisdigit(cast_uchar(s[0]))!=0; s.inc()) {
		  	  int d = s[0] - '0';
		      if (a >= MAXBY10 && (a > MAXBY10 || d > MAXLASTD + neg))  /* overflow? */
		        return null;  /* do not accept it (as integer) */
		      a = (uint)(a * 10 + d);
		      empty = 0;
		    }
		  }
		  while (lisspace((byte)(s[0]))!=0) s.inc();  /* skip trailing spaces */
		  if (empty!=0 || s[0] != '\0') return null;  /* something wrong in the numeral */
		  else {
		    result = l_castU2S((neg!=0) ? 0u - a : a);
		    return s;
		  }
		}


		public static uint luaO_str2num (CharPtr s, TValue o) {
		  lua_Integer i = 0; lua_Number n = 0;
		  CharPtr e;
		  if ((e = l_str2int(s, ref i)) != null) {  /* try as an integer */
		    setivalue(o, i);
		  }
		  else if ((e = l_str2d(s, ref n)) != null) {  /* else try as a float */
		    setfltvalue(o, n);
		  }
		  else
		    return 0;  /* conversion failed */
		  return (uint)((e - s) + 1);  /* success; return string size */
		}


		public static int luaO_utf8esc (CharPtr buff, ulong x) {
		  int n = 1;  /* number of bytes put in buffer (backwards) */
		  lua_assert(x <= 0x10FFFF);
		  if (x < 0x80)  /* ascii? */
		    buff[UTF8BUFFSZ - 1] = (char)x;
		  else {  /* need continuation bytes */
		    uint mfb = 0x3f;  /* maximum that fits in first byte */
		    do {  /* add continuation bytes */
		      buff[UTF8BUFFSZ - (n++)] = (char)(0x80 | (x & 0x3f));
		      x >>= 6;  /* remove added bits */
		      mfb >>= 1;  /* now there is one less bit available in first byte */
		    } while (x > mfb);  /* still needs continuation byte? */
		    buff[UTF8BUFFSZ - n] = (char)((~mfb << 1) | x);  /* add first byte */
		  }
		  return n;
		}


		/* maximum length of the conversion of a number to a string */
		private const int MAXNUMBER2STR	= 50;


		/*
		** Convert a number object to a string
		*/
		private static void luaO_tostring (lua_State L, StkId obj) {
		  CharPtr buff = new CharPtr(new char[MAXNUMBER2STR]);
		  uint len;
		  lua_assert(ttisnumber(obj));
		  if (ttisinteger(obj))
		  	len = (uint)lua_integer2str(buff, MAXNUMBER2STR/*sizeof(buff)*/, ivalue(obj));
		  else {
		    len = (uint)lua_number2str(buff, MAXNUMBER2STR/*sizeof(buff)*/, fltvalue(obj));
		//#if !defined(LUA_COMPAT_FLOATSTRING)
		    if (buff[strspn(buff, "-0123456789")] == '\0') {  /* looks like an int? */
		      buff[len++] = lua_getlocaledecpoint();
		      buff[len++] = '0';  /* adds '.0' to result */
		    }
		//#endif
		  }
		  setsvalue2s(L, obj, luaS_newlstr(L, buff, len));
		}



		private static void pushstr (lua_State L, CharPtr str, uint l) {
		  setsvalue2s(L, L.top, luaS_newlstr(L, str, l));
		  luaD_inctop(L);
		}


		/*
		** this function handles only '%d', '%c', '%f', '%p', and '%s'
		   conventional formats, plus Lua-specific '%I' and '%U'
		*/
		public static CharPtr luaO_pushvfstring (lua_State L, CharPtr fmt, params object[] argp) {
		  int parm_index = 0; //FIXME: added, for emulating va_arg(argp, xxx)
		  int n = 0;
		  for (;;) {
		    CharPtr e = strchr(fmt, '%');
		    if (e == null) break;
		  	pushstr(L, fmt, (uint)(e - fmt));
		    switch (e[1]) {
		      case 's': {  /* zero-terminated string */
				  object o = argp[parm_index++]; //FIXME: changed
				  CharPtr s = o as CharPtr; //FIXME: changed
				  if (s == null) //FIXME: changed
					  s = (string)o; //FIXME: changed
				  if (s == null) s = "(null)";
				  pushstr(L, s, (uint)strlen(s)); //FIXME:changed, (uint)
		          break;
		      }
		      case 'c': {  /* an 'int' as a character */
		        CharPtr buff = new char[1]; buff[0] = (char)(int)argp[parm_index++]; //FIXME:???char->CharPtr
				if (0!=lisprint(cast_uchar(buff)))
		          pushstr(L, buff, 1); //FIXME:???&buff
			    else  /* non-printable character; print its code */
          		  luaO_pushfstring(L, "<\\%d>", cast_uchar(buff));
		        break;
		      }
		      case 'd': {  /* an 'int' */
		        setivalue(L.top, (int)argp[parm_index++]);
		        //goto top2str;
		        luaD_inctop(L);
				luaO_tostring(L, L.top - 1);
		        break;
		      }
		      case 'I': {  /* a 'lua_Integer' */
		  		setivalue(L.top, (lua_Integer)(l_uacInt)argp[parm_index++]);
		        //goto top2str;
		        luaD_inctop(L);
				luaO_tostring(L, L.top - 1);
		        break;
		      }
		      case 'f': {  /* a 'lua_Number' */
		        setfltvalue(L.top, cast_num((l_uacNumber)argp[parm_index++]));
		      //top2str: //FIXME: see upper, 2 places
		        luaD_inctop(L);
				luaO_tostring(L, L.top - 1);
		        break;
		      }
		      case 'p': {  /* a pointer */
		        CharPtr buff = new char[32]; /* should be enough space for a '%p' */ //FIXME: changed, char buff[4*sizeof(void *) + 8];
		        int l = l_sprintf(buff, 32/*sizeof(buff)*/, "0x%08x", argp[parm_index++].GetHashCode()); //FIXME: changed, %p->%08x  //FIXME:changed, (uint)
		        pushstr(L, buff, (uint)l);
		        break;
		      }
		      case 'U': {  /* an 'int' as a UTF-8 sequence */
		  	    CharPtr buff = new CharPtr(new char[UTF8BUFFSZ]);
		  	    int l = luaO_utf8esc(buff, (ulong)(long)argp[parm_index++]);
		        pushstr(L, buff + UTF8BUFFSZ - l, (uint)l);
		        break;
		      }			  
		      case '%': {
		        pushstr(L, "%", 1);
		        break;
		      }
		      default: {
		        luaG_runerror(L, "invalid option '%%%c' to 'lua_pushfstring'",
		                         (e + 1).ToString()); //FIXME: changed, *(e+1)
		        break; //FIXME:added
		      }
		    }
		    n += 2;
		    fmt = e+2;
		  }
		  luaD_checkstack(L, 1);
		  pushstr(L, fmt, (uint)strlen(fmt)); //FIXME:changed, (uint)
		  if (n > 0) luaV_concat(L, n+1);
		  return svalue(L.top - 1);
		}


		public static CharPtr luaO_pushfstring(lua_State L, CharPtr fmt, params object[] argp) {
		  CharPtr msg;
		  //va_list argp;
		  //va_start(argp, fmt);
		  msg = luaO_pushvfstring(L, fmt, argp); //FIXME: (argp->args), sync
          //va_end(argp);
          return msg;
		}


		/* number of chars of a literal string without the ending \0 */
		private static uint LL(string x) { return (uint)x.Length; } //FIXME:sizeof(x)/sizeof(char) - 1


		private const string RETS = "...";
		private const string PRE = "[string \"";
		private const string POS = "\"]";

		public static void addstr(CharPtr a, CharPtr b, uint l) { memcpy(a,b,l * 1); a += l; } //FIXME: * sizeof(char)

		public static void luaO_chunkid (CharPtr out_, CharPtr source, uint bufflen) {
		  uint l = (uint)strlen(source);
		  if (source[0] == '=') {  /* 'literal' source */
		    if (l <= bufflen)  /* small enough? */
		      memcpy(out_, source + 1, l * 1); //FIXME: * sizeof(char)
		    else {  /* truncate it */
		      addstr(out_, source + 1, bufflen - 1);
		      out_ = "";//FIXME:???//*out_ = '\0';
		    }
		  }
		  else if (source[0] == '@') {  /* file name */
		    if (l <= bufflen)  /* small enough? */
		      memcpy(out_, source + 1, l * 1); //FIXME:* sizeof(char)
		    else {  /* add '...' before rest of name */
		      addstr(out_, RETS, LL(RETS));
		      bufflen -= LL(RETS);
		      memcpy(out_, source + 1 + l - bufflen, bufflen * 1); //FIXME:* sizeof(char)
		    }
		  }
		  else {  /* string; format as [string "source"] */
		    CharPtr nl = strchr(source, '\n');  /* find first new line (if any) */
		    addstr(out_, PRE, LL(PRE));  /* add prefix */
		    bufflen -= LL(PRE + RETS + POS) + 1;  /* save space for prefix+suffix+'\0' */
		    if (l < bufflen && nl == null) {  /* small one-line source? */
		      addstr(out_, source, l);  /* keep it */
		    }
		    else {
		      if (nl != null) l = (uint)(nl - source);  /* stop at first newline */ //FIXME:(uint)
		      if (l > bufflen) l = bufflen;
		      addstr(out_, source, l);
		      addstr(out_, RETS, LL(RETS));
		    }
		    memcpy(out_, POS, (LL(POS) + 1) * 1); //FIXME:* sizeof(char)
		  }
		}
	}
}

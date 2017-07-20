/*
** $Id: lobject.c,v 2.42 2010/10/29 11:13:14 roberto Exp roberto $
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
	
	public partial class Lua
	{

	
	    //FIXME:moved to lobject.h
        //LUAI_DDEF const TValue luaO_nilobject_ = {NILCONSTANT};

		
		/*
		** converts an integer to a "floating point byte", represented as
		** (eeeeexxx), where the real value is (1xxx) * 2^(eeeee - 1) if
		** eeeee != 0 and (xxx) otherwise.
		*/
		public static int luaO_int2fb (lu_int32 x) {
		  int e = 0;  /* expoent */
          if (x < 8) return (int)x;
		  while (x >= 0x10) {
			x = (x+1) >> 1;
			e++;
		  }
		  return ((e+1) << 3) | (cast_int(x) - 8);
		}


		/* converts back */
		public static int luaO_fb2int (int x) {
		  int e = (x >> 3) & 0x1f;
		  if (e == 0) return x;
		  else return ((x & 7) + 8) << (e - 1);
		}

		private static readonly lu_byte[] log_2 = {
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

        
		public static int luaO_rawequalObj (TValue t1, TValue t2) {
		  if (ttype(t1) != ttype(t2)) return 0;
		  else switch (ttype(t1)) {
			case LUA_TNIL:
			  return 1;
			case LUA_TNUMBER:
			  return luai_numeq(nvalue(t1), nvalue(t2)) ? 1 : 0;
			case LUA_TBOOLEAN:
			  return bvalue(t1) == bvalue(t2) ? 1 : 0;  /* boolean true must be 1 !! */
			case LUA_TLIGHTUSERDATA:
				return pvalue(t1) == pvalue(t2) ? 1 : 0;
		    case LUA_TSTRING:
		      return rawtsvalue(t1) == rawtsvalue(t2) ? 1 : 0;
		    case LUA_TLCF:
		      return fvalue(t1) == fvalue(t2) ? 1 : 0;
			default:
			  lua_assert(iscollectable(t1));
			  return gcvalue(t1) == gcvalue(t2) ? 1 : 0;
		  }
		}


		public static lua_Number luaO_arith (int op, lua_Number v1, lua_Number v2) {
		  switch (op) {
		    case LUA_OPADD: return luai_numadd(null, v1, v2);
		    case LUA_OPSUB: return luai_numsub(null, v1, v2);
		    case LUA_OPMUL: return luai_nummul(null, v1, v2);
		    case LUA_OPDIV: return luai_numdiv(null, v1, v2);
		    case LUA_OPMOD: return luai_nummod(null, v1, v2);
		    case LUA_OPPOW: return luai_numpow(null, v1, v2);
		    case LUA_OPUNM: return luai_numunm(null, v1);
		    default: lua_assert(0); return 0;
		  }
		}


		private static int checkend (CharPtr s, CharPtr endptr) {
		  if (endptr == s) return 0;  /* no characters converted */
		  while (lisspace((byte)(endptr[0]))!=0) /*endptr++*/endptr = endptr + 1; //FIXME:changed, ++
		  return (endptr[0] == '\0')?1:0;  /* OK if no trailing characters */
		}


		public static int luaO_str2d (CharPtr s, out lua_Number result) {
		  CharPtr endptr;
		  result = lua_str2number(s, out endptr);
		  if (checkend(s, endptr)!=0) return 1;  /* conversion OK? */
		  result = cast_num(strtoul(s, out endptr, 0)); /* try hexadecimal */
		  return checkend(s, endptr);
		}



		private static void pushstr (lua_State L, CharPtr str, uint l) {
		  setsvalue2s(L, L.top, luaS_newlstr(L, str, l));
		  incr_top(L);
		}


		/* this function handles only `%d', `%c', %f, %p, and `%s' formats */
		public static CharPtr luaO_pushvfstring (lua_State L, CharPtr fmt, params object[] argp) {
		  int parm_index = 0; //FIXME: added, for emulating va_arg(argp, xxx)
		  int n = 0;
		  for (;;) {
		    CharPtr e = strchr(fmt, '%');
		    if (e == null) break;
		    setsvalue2s(L, L.top, luaS_newlstr(L, fmt, (uint)(e-fmt)));
		    incr_top(L);
		    switch (e[1]) {
		      case 's': {
				  object o = argp[parm_index++]; //FIXME: changed
				  CharPtr s = o as CharPtr; //FIXME: changed
				  if (s == null) //FIXME: changed
					  s = (string)o; //FIXME: changed
				  if (s == null) s = "(null)";
				  pushstr(L, s, (uint)strlen(s)); //FIXME:changed, (uint)
		          break;
		      }
		      case 'c': {
		        CharPtr buff = new char[1]; //FIXME:???char->CharPtr
		        buff[0] = (char)(int)argp[parm_index++];
		        pushstr(L, buff, 1); //FIXME:???&buff
		        break;
		      }
		      case 'd': {
		        setnvalue(L.top, (int)argp[parm_index++]);
		        incr_top(L);
		        break;
		      }
		      case 'f': {
		        setnvalue(L.top, (l_uacNumber)argp[parm_index++]);
		        incr_top(L);
		        break;
		      }
		      case 'p': {
		        CharPtr buff = new char[32]; /* should be enough space for a `%p' */ //FIXME: changed, char buff[4*sizeof(void *) + 8];
		        uint l = (uint)sprintf(buff, "0x%08x", argp[parm_index++].GetHashCode()); //FIXME: changed, %p->%08x  //FIXME:changed, (uint)
		        pushstr(L, buff, l);
		        break;
		      }
		      case '%': {
		        pushstr(L, "%", 1);
		        break;
		      }
		      default: {
		        luaG_runerror(L,
		            "invalid option " + LUA_QL("%%%c") + " to " + LUA_QL("lua_pushfstring"),
		            (e + 1).ToString()); //FIXME: changed, *(e+1)
		        break;
		      }
		    }
		    n += 2;
		    fmt = e+2;
		  }
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


		private static uint LL(string x) { return (uint)x.Length; }
		private const string RETS = "...";
		private const string PRE = "[string \"";
		private const string POS = "\"]";

		public static void addstr(CharPtr a, CharPtr b, uint l) { memcpy(a,b,l); a += (l); }

		public static void luaO_chunkid (CharPtr out_, CharPtr source, uint bufflen) {
		  uint l = (uint)strlen(source);
		  if (source[0] == '=') {  /* 'literal' source */
		    if (l <= bufflen)  /* small enough? */
		      memcpy(out_, source + 1, l);
		    else {  /* truncate it */
		      addstr(out_, source + 1, bufflen - 1);
		      out_ = "";//FIXME:???//*out_ = '\0';
		    }
		  }
		  else if (source[0] == '@') {  /* file name */
		    if (l <= bufflen)  /* small enough? */
		      memcpy(out_, source + 1, l);
		    else {  /* add '...' before rest of name */
		      addstr(out_, RETS, LL(RETS));
		      bufflen -= LL(RETS);
		      memcpy(out_, source + 1 + l - bufflen, bufflen);
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
		    memcpy(out_, POS, LL(POS) + 1);
		  }
		}
	}
}

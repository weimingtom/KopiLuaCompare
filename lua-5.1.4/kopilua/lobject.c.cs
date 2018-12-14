/*
** $Id: lobject.c,v 2.22.1.1 2007/12/27 13:02:25 roberto Exp $
** Some generic functions over Lua objects
** See Copyright Notice in lua.h
*/


namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using lu_byte = System.Byte;
	using lua_Number = System.Double;
	using l_uacNumber = System.Double;
	using Instruction = System.UInt32;

	public partial class Lua
	{

	


		/*
		** converts an integer to a "floating point byte", represented as
		** (eeeeexxx), where the real value is (1xxx) * 2^(eeeee - 1) if
		** eeeee != 0 and (xxx) otherwise.
		*/
		public static int luaO_int2fb (uint x) {
		  int e = 0;  /* expoent */
		  while (x >= 16) {
			x = (x+1) >> 1;
			e++;
		  }
		  if (x < 8) return (int)x;
		  else return ((e+1) << 3) | (cast_int(x) - 8);
		}


		/* converts back */
		public static int luaO_fb2int (int x) {
		  int e = (x >> 3) & 31;
		  if (e == 0) return x;
		  else return ((x & 7)+8) << (e - 1);
		}


		private readonly static lu_byte[] log_2 = {
			0,1,2,2,3,3,3,3,4,4,4,4,4,4,4,4,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
			6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
			7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
			7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
			8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
			8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
			8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
			8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8
		  };

		public static int luaO_log2 (uint x) {
		  int l = -1;
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
			  return bvalue(t1) == bvalue(t2) ? 1 : 0;  /* boolean true must be 1....but not in C# !! */
			case LUA_TLIGHTUSERDATA:
				return pvalue(t1) == pvalue(t2) ? 1 : 0;
			default:
			  lua_assert(iscollectable(t1));
			  return gcvalue(t1) == gcvalue(t2) ? 1 : 0;
		  }
		}

		public static int luaO_str2d (CharPtr s, out lua_Number result) {
		  CharPtr endptr;
		  result = lua_str2number(s, out endptr);
		  if (endptr == s) return 0;  /* conversion failed */
		  if (endptr[0] == 'x' || endptr[0] == 'X')  /* maybe an hexadecimal constant? */
			result = cast_num(strtoul(s, out endptr, 16));
		  if (endptr[0] == '\0') return 1;  /* most common case */
		  while (isspace(endptr[0])) endptr = endptr.next();
		  if (endptr[0] != '\0') return 0;  /* invalid trailing characters? */
		  return 1;
		}



		private static void pushstr (lua_State L, CharPtr str) {
		  setsvalue2s(L, L.top, luaS_new(L, str));
		  incr_top(L);
		}


		/* this function handles only `%d', `%c', %f, %p, and `%s' formats */
		public static CharPtr luaO_pushvfstring (lua_State L, CharPtr fmt, params object[] argp) {
		  int parm_index = 0;
		  int n = 1;
		  pushstr(L, "");
		  for (;;) {
		    CharPtr e = strchr(fmt, '%');
		    if (e == null) break;
		    setsvalue2s(L, L.top, luaS_newlstr(L, fmt, (uint)(e-fmt)));
		    incr_top(L);
		    switch (e[1]) {
		      case 's': {
				  object o = argp[parm_index++];
				  CharPtr s = o as CharPtr;
				  if (s == null)
					  s = (string)o;
				  if (s == null) s = "(null)";
		          pushstr(L, s);
		          break;
		      }
		      case 'c': {
		        CharPtr buff = new char[2];
		        buff[0] = (char)(int)argp[parm_index++];
		        buff[1] = '\0';
		        pushstr(L, buff);
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
		        //CharPtr buff = new char[4*sizeof(void *) + 8]; /* should be enough space for a `%p' */
				CharPtr buff = new char[32];
				sprintf(buff, "0x%08x", argp[parm_index++].GetHashCode());
		        pushstr(L, buff);
		        break;
		      }
		      case '%': {
		        pushstr(L, "%");
		        break;
		      }
		      default: {
		        CharPtr buff = new char[3];
		        buff[0] = '%';
		        buff[1] = e[1];
		        buff[2] = '\0';
		        pushstr(L, buff);
		        break;
		      }
		    }
		    n += 2;
		    fmt = e+2;
		  }
		  pushstr(L, fmt);
		  luaV_concat(L, n+1, cast_int(L.top - L.base_) - 1);
		  L.top -= n;
		  return svalue(L.top - 1);
		}

		public static CharPtr luaO_pushfstring(lua_State L, CharPtr fmt, params object[] args)
		{
			return luaO_pushvfstring(L, fmt, args);
		}


		public static void luaO_chunkid (CharPtr out_, CharPtr source, uint bufflen) {
			//out_ = "";
		  if (source[0] == '=') {
		    strncpy(out_, source+1, (int)bufflen);  /* remove first char */
		    out_[bufflen-1] = '\0';  /* ensures null termination */
		  }
		  else {  /* out = "source", or "...source" */
		    if (source[0] == '@') {
		      uint l;
		      source = source.next();  /* skip the `@' */
		      bufflen -= (uint)(" '...' ".Length + 1);
		      l = (uint)strlen(source);
		      strcpy(out_, "");
		      if (l > bufflen) {
		        source += (l-bufflen);  /* get last part of file name */
		        strcat(out_, "...");
		      }
		      strcat(out_, source);
		    }
		    else {  /* out = [string "string"] */
		      uint len = strcspn(source, "\n\r");  /* stop at first newline */
		      bufflen -= (uint)(" [string \"...\"] ".Length + 1);
		      if (len > bufflen) len = bufflen;
		      strcpy(out_, "[string \"");
		      if (source[len] != '\0') {  /* must truncate? */
		        strncat(out_, source, (int)len);
		        strcat(out_, "...");
		      }
		      else
		        strcat(out_, source);
		      strcat(out_, "\"]");
		    }
		  }
		}

	}
}

/*
** $Id: loslib.c,v 1.57 2015/04/10 17:41:04 roberto Exp $
** Standard Operating System library
** See Copyright Notice in lua.h
*/

using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using lua_Integer = System.Int32;
	using lua_Number = System.Double;

	public partial class Lua
	{

		/*
		** {==================================================================
		** list of valid conversion specifiers for the 'strftime' function
		** ===================================================================
		*/
		//#if !defined(LUA_STRFTIMEOPTIONS)	/* { */

		//#if !defined(LUA_USE_POSIX)
		private static CharPtr[] LUA_STRFTIMEOPTIONS = new CharPtr[] { "aAbBcdHIjmMpSUwWxXyYz%", "" };
		//#else
		//#define LUA_STRFTIMEOPTIONS \
		//{ "aAbBcCdDeFgGhHIjmMnprRStTuUVwWxXyYzZ%", "", \
		//  "E", "cCxXyY",  \
		//	  "O", "deHImMSuUVwWy" }
		//#endif

		//#endif					/* } */
		/* }================================================================== */


		/*
		** {==================================================================
		** Configuration for time-related stuff
		** ===================================================================
		*/


		//#if !defined(l_time_t)		/* { */
		/*
		** type to represent time_t in Lua
		*/
		//#define l_timet			lua_Integer
		//#define l_pushtime(L,t)		lua_pushinteger(L,(lua_Integer)(t))
		//#define l_checktime(L,a)	((time_t)luaL_checkinteger(L,a))

		//#endif				/* } */


		//#if !defined(l_gmtime)		/* { */
		/*
		** By default, Lua uses gmtime/localtime, except when POSIX is available,
		** where it uses gmtime_r/localtime_r
		*/

		//#if defined(LUA_USE_POSIX)	/* { */

		//#define l_gmtime(t,r)		gmtime_r(t,r)
		//#define l_localtime(t,r)	localtime_r(t,r)

		//#else				/* }{ */

		/* ISO C definitions */
		//#define l_gmtime(t,r)		((void)(r)->tm_sec, gmtime(t))
		//#define l_localtime(t,r)  	((void)(r)->tm_sec, localtime(t))

		//#endif				/* } */

		//#endif				/* } */

		/* }================================================================== */


		/*
		** {==================================================================
		** Configuration for 'tmpnam':
		** By default, Lua uses tmpnam except when POSIX is available, where
		** it uses mkstemp.
		** ===================================================================
		*/

		//#if !defined(lua_tmpnam)	/* { */
		/*
		** By default, Lua uses tmpnam except when POSIX is available, where it
		** uses mkstemp.
		*/
		
		//#if defined(LUA_USE_POSIX)	/* { */
		
		//#include <unistd.h>
		
		//#define LUA_TMPNAMBUFSIZE       32
		
		//#if !defined(LUA_TMPNAMTEMPLATE)
		//#define LUA_TMPNAMTEMPLATE	"/tmp/lua_XXXXXX"
		//#endif

		//#define lua_tmpnam(b,e) { \
		//        strcpy(b, LUA_TMPNAMTEMPLATE); \
		//        e = mkstemp(b); \
		//        if (e != -1) close(e); \
		//        e = (e == -1); }

		//#else				/* }{ */

		/* ANSI definitions */
		public const int LUA_TMPNAMBUFSIZE = L_tmpnam;
		public static void lua_tmpnam(CharPtr b, out int e)		{ e = (tmpnam(b) == null)?1:0; }

		//#endif				/* } */
		/* }================================================================== */



		private static int os_execute (lua_State L) {
		  CharPtr cmd = luaL_optstring(L, 1, null);
		  int stat = system(cmd);
		  if (cmd != null)
		    return luaL_execresult(L, stat);
		  else {
		    lua_pushboolean(L, stat);  /* true if there is a shell */
		    return 1;
		  }
		}


		private static int os_remove (lua_State L) {
		  CharPtr filename = luaL_checkstring(L, 1);
		  return luaL_fileresult(L, (remove(filename) == 0)?1:0, filename);
		}


		private static int os_rename (lua_State L) {
		  CharPtr fromname = luaL_checkstring(L, 1);
		  CharPtr toname = luaL_checkstring(L, 2);
		  return luaL_fileresult(L, (rename(fromname, toname) == 0)?1:0, null);
		}


		private static int os_tmpname (lua_State L) {
		  CharPtr buff = new char[LUA_TMPNAMBUFSIZE];
		  int err;
		  lua_tmpnam(buff, out err);
		  if (err != 0)
		    return luaL_error(L, "unable to generate a unique filename");
		  lua_pushstring(L, buff);
		  return 1;
		}


		private static int os_getenv (lua_State L) {
		  lua_pushstring(L, getenv(luaL_checkstring(L, 1)));  /* if NULL push nil */
		  return 1;
		}


		private static int os_clock (lua_State L) {
		  lua_pushnumber(L, ((lua_Number)clock())/(lua_Number)CLOCKS_PER_SEC);
		  return 1;
		}


		/*
		** {======================================================
		** Time/Date operations
		** { year=%Y, month=%m, day=%d, hour=%H, min=%M, sec=%S,
		**   wday=%w+1, yday=%j, isdst=? }
		** =======================================================
		*/

		private static void setfield (lua_State L, CharPtr key, int value) {
		  lua_pushinteger(L, value);
		  lua_setfield(L, -2, key);
		}

		private static void setboolfield (lua_State L, CharPtr key, int value) {
		  if (value < 0)  /* undefined? */
			return;  /* does not set field */
		  lua_pushboolean(L, value);
		  lua_setfield(L, -2, key);
		}

		private static int getboolfield (lua_State L, CharPtr key) {
		  int res;
		  res = (lua_getfield(L, -1, key) == LUA_TNIL) ? -1 : lua_toboolean(L, -1);
  		  lua_pop(L, 1);
		  return res;
		}


		private static int getfield (lua_State L, CharPtr key, int d) {
		  int res, isnum=0; //FIXME:added, =0;
		  lua_getfield(L, -1, key);
		  res = (int)lua_tointegerx(L, -1, ref isnum);
		  if (isnum==0) {
			if (d < 0)
			  return luaL_error(L, "field '%s' missing in date table", key);
			res = d;
		  }
		  lua_pop(L, 1);
		  return res;
		}


		private static CharPtr[] options = LUA_STRFTIMEOPTIONS;
		private static CharPtr checkoption (lua_State L, CharPtr conv, CharPtr buff) {
		  //static const char *const options[] = LUA_STRFTIMEOPTIONS;
		  uint i;
		  for (i = 0; i < options.Length; i += 2) {
		  	if (conv[0] != '\0' && strchr(options[i], conv[0]) != null) {
		  	  buff[1] = conv[0];
		  	  if (options[i + 1][0] == '\0') {  /* one-char conversion specifier? */
		        buff[2] = '\0';  /* end buffer */
		        return conv + 1;
		      }
		  	  else if (conv[1] != '\0' &&
		  	           strchr(options[i + 1], conv[1]) != null) {
		  	  	buff[2] = conv[1];  /* valid two-char conversion specifier */
		        buff[3] = '\0';  /* end buffer */
		        return conv + 2;
		      }
		    }
		  }
		  luaL_argerror(L, 1,
		    lua_pushfstring(L, "invalid conversion specifier '%%%s'", conv));
		  return conv;  /* to avoid warnings */
		}


		private static int os_date (lua_State L) { //FIXME:changed, implemented by self
		  CharPtr s = luaL_optstring(L, 1, "%c");
		  DateTime stm;
		  if (s[0] == '!') {  /* UTC? */
			stm = DateTime.UtcNow;
			s.inc();  /* skip `!' */
		  }
		  else
			  stm = DateTime.Now;
		  if (strcmp(s, "*t") == 0) {
			lua_createtable(L, 0, 9);  /* 9 = number of fields */
			setfield(L, "sec", stm.Second);
			setfield(L, "min", stm.Minute);
			setfield(L, "hour", stm.Hour);
			setfield(L, "day", stm.Day);
			setfield(L, "month", stm.Month);
			setfield(L, "year", stm.Year);
			setfield(L, "wday", (int)stm.DayOfWeek);
			setfield(L, "yday", stm.DayOfYear);
			setboolfield(L, "isdst", stm.IsDaylightSavingTime() ? 1 : 0);
		  }
		  else {
			  luaL_error(L, "strftime not implemented yet"); // todo: implement this - mjf
#if false
			  //FIXME:not implemented ------------------>
		    char cc[4];
		    luaL_Buffer b;
		    cc[0] = '%';
		    luaL_buffinit(L, &b);
		    while (*s) {
		      if (*s != '%')  /* no conversion specifier? */
		        luaL_addchar(&b, *s++);
		      else {
		        size_t reslen;
		        char buff[200];  /* should be big enough for any conversion result */
		        s = checkoption(L, s + 1, cc);
		        reslen = strftime(buff, sizeof(buff), cc, stm);
		        luaL_addlstring(&b, buff, reslen);
		      }
		    }
		    luaL_pushresult(&b);
#endif // #if 0
		  }
			return 1;
		}


		private static int os_time (lua_State L) {
		  DateTime t;
		  if (lua_isnoneornil(L, 1))  /* called without args? */
			t = DateTime.Now;  /* get current time */
		  else {
			luaL_checktype(L, 1, LUA_TTABLE);
			lua_settop(L, 1);  /* make sure table is at the top */
			int sec = getfield(L, "sec", 0);
			int min = getfield(L, "min", 0);
			int hour = getfield(L, "hour", 12);
			int day = getfield(L, "day", -1);
			int month = getfield(L, "month", -1) - 1;
			int year = getfield(L, "year", -1) - 1900;
			int isdst = getboolfield(L, "isdst");	// todo: implement this - mjf
			t = new DateTime(year, month, day, hour, min, sec);
		  }
		  lua_pushinteger(L, (int)(t.Ticks));
		  return 1;
		}


		private static int os_difftime (lua_State L) {
		  long ticks = (long)luaL_checknumber(L, 1) - (long)luaL_optnumber(L, 2, 0);
		  double res = ticks/TimeSpan.TicksPerSecond;
		  lua_pushnumber(L, (lua_Number)res);
		  return 1;
		}

		/* }====================================================== */

		// locale not supported yet
		private static int os_setlocale (lua_State L) {		  
		  /*
		  static string[] cat = {LC_ALL, LC_COLLATE, LC_CTYPE, LC_MONETARY,
							  LC_NUMERIC, LC_TIME};
		  static string[] catnames[] = {"all", "collate", "ctype", "monetary",
			 "numeric", "time", null};
		  CharPtr l = luaL_optstring(L, 1, null);
		  int op = luaL_checkoption(L, 2, "all", catnames);
		  lua_pushstring(L, setlocale(cat[op], l));
		  */
		  CharPtr l = luaL_optstring(L, 1, null);
		  lua_pushstring(L, "C");
		  return (l.ToString() == "C") ? 1 : 0;
		}


		private static int os_exit (lua_State L) {
		  int status;
		  if (lua_isboolean(L, 1))
		    status = (lua_toboolean(L, 1)!=0 ? EXIT_SUCCESS : EXIT_FAILURE);
		  else
		    status = (int)luaL_optinteger(L, 1, EXIT_SUCCESS);
		  if (lua_toboolean(L, 2) != 0)
		    lua_close(L);
		  if (L!=null) exit(status);  /* 'if' to avoid warnings for unreachable 'return' */
		  return 0;
		}
		

		private readonly static luaL_Reg[] syslib = {
		  new luaL_Reg("clock",     os_clock),
		  new luaL_Reg("date",      os_date),
		  new luaL_Reg("difftime",  os_difftime),
		  new luaL_Reg("execute",   os_execute),
		  new luaL_Reg("exit",      os_exit),
		  new luaL_Reg("getenv",    os_getenv),
		  new luaL_Reg("remove",    os_remove),
		  new luaL_Reg("rename",    os_rename),
		  new luaL_Reg("setlocale", os_setlocale),
		  new luaL_Reg("time",      os_time),
		  new luaL_Reg("tmpname",   os_tmpname),
		  new luaL_Reg(null, null)
		};

		/* }====================================================== */



		public static int luaopen_os (lua_State L) {
		  luaL_newlib(L, syslib);
		  return 1;
		}

	}
}

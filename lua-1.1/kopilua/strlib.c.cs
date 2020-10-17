/*
** strlib.c
** String library to LUA
*/
using System;

namespace KopiLua
{
	using lua_Object = KopiLua.Lua.Object_;
	
	public partial class Lua
	{
		//char *rcs_strlib="$Id: strlib.c,v 1.2 1994/03/28 15:14:02 celes Exp $";

		//#include <stdlib.h>
		//#include <string.h>
		//#include <ctype.h>

		//#include "mm.h"


		//#include "lua.h"

		/*
		** Return the position of the first caracter of a substring into a string
		** LUA interface:
		**			n = strfind (string, substring)
		*/
		private static void str_find ()
		{
		 	CharPtr s1, s2, f;
			lua_Object o1 = lua_getparam (1);
			lua_Object o2 = lua_getparam (2);
			if (lua_isstring(o1) == 0 || lua_isstring(o2) == 0)
			{ lua_error ("incorrect arguments to function `strfind'"); return; }
			s1 = lua_getstring(o1);
			s2 = lua_getstring(o2);
			f = strstr(s1,s2);
			if (f != null)
			 lua_pushnumber (f-s1+1);
			else
			 lua_pushnil();
		}
	
		/*
		** Return the string length
		** LUA interface:
		**			n = strlen (string)
		*/
		private static void str_len ()
		{
		 	lua_Object o = lua_getparam (1);
		 	if (lua_isstring(o) == 0)
		 	{ lua_error("incorrect arguments to function `strlen'"); return; }
		 	lua_pushnumber(strlen(lua_getstring(o)));
		}
	
	
		/*
		** Return the substring of a string, from start to end
		** LUA interface:
		**			substring = strsub (string, start, end)
		*/
		private static void str_sub ()
		{
		 	int start, end;
		 	CharPtr s;
		 	lua_Object o1 = lua_getparam(1);
		 	lua_Object o2 = lua_getparam(2);
		 	lua_Object o3 = lua_getparam(3);
			if (lua_isstring(o1) == 0 || lua_isnumber(o2) == 0)
			{ lua_error ("incorrect arguments to function `strsub'"); return; }
			if (o3 != null && lua_isnumber(o3) == 0)
			{ lua_error ("incorrect third argument to function `strsub'"); return; }
			s = lua_copystring(o1);
			start = (int)lua_getnumber (o2);
			end = o3 == null ? (int)strlen(s) : (int)lua_getnumber (o3);
			if (end < start || start < 1 || end > strlen(s))
			 lua_pushstring("");
			else
			{
			 s[end] = '\0';
			 lua_pushstring (s.add(start-1));
			}
			free (s);
		}

		/*
		** Convert a string to lower case.
		** LUA interface:
		**			lowercase = strlower (string)
		*/
		private static void str_lower ()
		{
		 	CharPtr s, c;
		 	lua_Object o = lua_getparam(1);
		 	if (lua_isstring(o) == 0)
		 	{ lua_error("incorrect arguments to function `strlower'"); return; }
		 	s = lua_getstring(o); c = new CharPtr(s);
		 	while (c[0] != '\0')
		 	{
		 		c[0] = tolower(c[0]);
		 		c.inc();
		 	}
		 	lua_pushstring(s);
		 	free(s);
		}
	
	
		/*
		** Convert a string to upper case.
		** LUA interface:
		**			uppercase = strupper (string)
		*/
		private static void str_upper ()
		{
		 	CharPtr s, c;
		 	lua_Object o = lua_getparam(1);
		 	if (lua_isstring(o) == 0)
		 	{ lua_error("incorrect arguments to function `strlower'"); return; }
		 	s = lua_getstring(o); c = new CharPtr(s);
		 	while (c[0] != '\0')
		 	{
		 		c[0] = toupper(c[0]);
		 		c.inc();
		 	}
		 	lua_pushstring(s);
		 	free(s);
		}
	
	
		/*
		** Open string library
		*/
		public static void strlib_open ()
		{
			lua_register ("strfind", str_find);
			lua_register ("strlen", str_len);
			lua_register ("strsub", str_sub);
			lua_register ("strlower", str_lower);
			lua_register ("strupper", str_upper);
		}
	}
}


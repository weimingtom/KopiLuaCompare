/*
** strlib.c
** String library to LUA
*/
using System;

namespace KopiLua
{
	using lua_Object = System.UInt32;
	
	public partial class Lua
	{
		//char *rcs_strlib="$Id: strlib.c,v 1.12 1995/02/06 19:37:51 roberto Exp $";

		//#include <string.h>
		//#include <stdlib.h>
		//#include <ctype.h>

		//#include "lua.h"
		//#include "lualib.h"


		private static CharPtr newstring (lua_Object o)
		{
			CharPtr s = lua_getstring(o);
			CharPtr ns = malloc(strlen(s) + 1);
		  	if (ns == null)
		    	lua_error("not enough memory for new string");
		  	strcpy(ns, s);
		  	return ns;
		}


		/*
		** Return the position of the first caracter of a substring into a string
		** LUA interface:
		**			n = strfind (string, substring, init, end)
		*/
		private static void str_find ()
		{
			CharPtr s1, s2, f;
			int init; 
			lua_Object o1 = lua_getparam (1);
			lua_Object o2 = lua_getparam (2);
			lua_Object o3 = lua_getparam (3);
			lua_Object o4 = lua_getparam (4);
			if (0==lua_isstring(o1) || 0==lua_isstring(o2))
		   		lua_error ("incorrect arguments to function `strfind'");
		 	if (o3 == LUA_NOOBJECT)
		  		init = 0;
		 	else if (0!=lua_isnumber(o3))
		 		init = (int)(lua_getnumber(o3)-1);
		 	else
		 	{
		   		lua_error ("incorrect arguments to function `strfind'");
		   		return;  /* to avoid warnings */
		 	}
		 	s1 = lua_getstring(o1);
		 	s2 = lua_getstring(o2);
		 	f = strstr(s1 + init,s2);
		 	if (f != null)
		 	{
		  		int pos = f-s1+1;
		  		if (o4 == LUA_NOOBJECT)
		   			lua_pushnumber (pos);
		  		else if (0==lua_isnumber(o4))
		   			lua_error ("incorrect arguments to function `strfind'");
		  		else if ((int)lua_getnumber(o4) >= pos+strlen(s2)-1)
		   			lua_pushnumber (pos);
		  		else
		   			lua_pushnil();
		 	}
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
		 	if (0==lua_isstring(o))
		 	  lua_error("incorrect arguments to function `strlen'");
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
			if (0==lua_isstring(o1) || 0==lua_isnumber(o2))
			  	lua_error ("incorrect arguments to function `strsub'");
			if (o3 != LUA_NOOBJECT && 0==lua_isnumber(o3))
			  	lua_error ("incorrect third argument to function `strsub'");
			s = newstring(o1);
			start = (int)lua_getnumber (o2);
			end = o3 == LUA_NOOBJECT ? (int)strlen(s) : (int)lua_getnumber (o3);
			if (end < start || start < 1 || end > strlen(s))
				lua_pushliteral("");
			else
			{
			 	s[end] = '\0';
			 	lua_pushstring (s.add(start-1));
			}
			free(s);
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
		 		lua_error("incorrect arguments to function `strlower'");
		 	s = newstring(o); c = new CharPtr(s);
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
		 		lua_error("incorrect arguments to function `strlower'");
		 	s = newstring(o); c = new CharPtr(s);
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
			lua_register ("strfind", str_find, "str_find");
			lua_register ("strlen", str_len, "str_len");
			lua_register ("strsub", str_sub, "str_sub");
			lua_register ("strlower", str_lower, "str_lower");
			lua_register ("strupper", str_upper, "str_upper");
		}
	}
}

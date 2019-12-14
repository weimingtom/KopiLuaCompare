/*
** $Id: lualib.h,v 1.45.1.1 2017/04/19 17:20:42 roberto Exp $
** Lua standard libraries
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace KopiLua
{
	public partial class Lua
	{
		//#include "lua.h"


		/* version suffix for environment variable names */
		public const string LUA_VERSUFFIX = "_" + LUA_VERSION_MAJOR + "_" + LUA_VERSION_MINOR;


		//LUAMOD_API int (luaopen_base) (lua_State *L);

		public const string LUA_COLIBNAME = "coroutine";
		//LUAMOD_API int (luaopen_coroutine) (lua_State *L);
		
		public const string LUA_TABLIBNAME = "table";
		//LUAMOD_API int (luaopen_table) (lua_State *L);
		
		public const string LUA_IOLIBNAME = "io";
		//LUAMOD_API int (luaopen_io) (lua_State *L);
		
		public const string LUA_OSLIBNAME = "os";
		//LUAMOD_API int (luaopen_os) (lua_State *L);
		
		public const string LUA_STRLIBNAME = "string";
		//LUAMOD_API int (luaopen_string) (lua_State *L);
		
		public const string LUA_UTF8LIBNAME	= "utf8";
		//LUAMOD_API int (luaopen_utf8) (lua_State *L);

        public const string LUA_BITLIBNAME = "bit32";
		//LUAMOD_API int (luaopen_bit32) (lua_State *L);
		
		public const string LUA_MATHLIBNAME = "math";
		//LUAMOD_API int (luaopen_math) (lua_State *L);
		
		public const string LUA_DBLIBNAME = "debug";
		//LUAMOD_API int (luaopen_debug) (lua_State *L);
		
		public const string LUA_LOADLIBNAME = "package";
		//LUAMOD_API int (luaopen_package) (lua_State *L);


		/* open all previous libraries */
		//LUALIB_API void (luaL_openlibs) (lua_State *L);



		//#if !defined(lua_assert)
		//#define lua_assert(x)	((void)0)
		//#endif

	}
}
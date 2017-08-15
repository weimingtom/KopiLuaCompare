/*
** $Id: lualib.h,v 1.43 2011/12/08 12:11:37 roberto Exp $
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


		public const string LUA_COLIBNAME = "coroutine";
		public const string LUA_TABLIBNAME = "table";
		public const string LUA_IOLIBNAME = "io";
		public const string LUA_OSLIBNAME = "os";
		public const string LUA_STRLIBNAME = "string";
        public const string LUA_BITLIBNAME = "bit32";
		public const string LUA_MATHLIBNAME = "math";
		public const string LUA_DBLIBNAME = "debug";
		public const string LUA_LOADLIBNAME = "package";


		/* open all previous libraries */
		//LUALIB_API void (luaL_openlibs) (lua_State *L);



		//#if !defined(lua_assert)
		//#define lua_assert(x)	((void)0)
		//#endif

	}
}

/*
** $Id: linit.c,v 1.15 2007/06/22 16:59:11 roberto Exp roberto $
** Initialization of libraries for lua.c
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace KopiLua
{
	public partial class Lua
	{
		/*
		** these libs are preloaded in Lua and are readily available to any program
		*/
		private readonly static luaL_Reg[] lualibs = {
		  new luaL_Reg("_G", luaopen_base),
		  new luaL_Reg(LUA_LOADLIBNAME, luaopen_package),
		  new luaL_Reg(LUA_TABLIBNAME, luaopen_table),
		  new luaL_Reg(LUA_IOLIBNAME, luaopen_io),
		  new luaL_Reg(LUA_OSLIBNAME, luaopen_os),
		  new luaL_Reg(LUA_STRLIBNAME, luaopen_string),
		  new luaL_Reg(LUA_MATHLIBNAME, luaopen_math),
		  new luaL_Reg(null, null)
		};
		/*
		** these libs must be required before used
		*/
		private readonly static luaL_Reg[] luareqlibs = {
		  new luaL_Reg(LUA_DBLIBNAME, luaopen_debug),
		  new luaL_Reg(null, null)
		};


		public static void luaL_openlibs (lua_State L) {
		  for (int i=0; i<lualibs.Length-1; i++)
		  {
			luaL_Reg lib = lualibs[i];
			lua_pushcfunction(L, lib.func);
			lua_pushstring(L, lib.name);
			lua_call(L, 1, 0);
		  }
		  //lib = luareqlibs; //FIXME:
		  luaL_findtable(L, LUA_GLOBALSINDEX, "package.preload", 0);
		  for (int i=0; i<luareqlibs.Length-1; i++) {
		    luaL_Reg lib = luareqlibs[i];
		    lua_pushcfunction(L, lib.func);
		    lua_setfield(L, -2, lib.name);
		  }
		  lua_pop(L, 1);  /* remove package.preload table */
#if LUA_COMPAT_DEBUGLIB
		  lua_getglobal(L, "require");
		  lua_pushliteral(L, LUA_DBLIBNAME);
		  lua_call(L, 1, 0);  /* call 'require"debug"' */
#endif
		}

	}
}

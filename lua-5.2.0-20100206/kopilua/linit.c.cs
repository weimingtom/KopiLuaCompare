/*
** $Id: linit.c,v 1.18 2009/05/01 13:46:35 roberto Exp roberto $
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
		** these libs are loaded by lua.c and are readily available to any program
		*/
		private readonly static luaL_Reg[] loadedlibs = {
		  new luaL_Reg("_G", luaopen_base),
		  new luaL_Reg(LUA_LOADLIBNAME, luaopen_package),
		  new luaL_Reg(LUA_TABLIBNAME, luaopen_table),
		  new luaL_Reg(LUA_IOLIBNAME, luaopen_io),
		  new luaL_Reg(LUA_OSLIBNAME, luaopen_os),
		  new luaL_Reg(LUA_STRLIBNAME, luaopen_string),
		  new luaL_Reg(LUA_BITLIBNAME, luaopen_bit),
		  new luaL_Reg(LUA_MATHLIBNAME, luaopen_math),
		  new luaL_Reg(null, null)
		};


		/*
		** these libs are preloaded and must be required before used
		*/
		private readonly static luaL_Reg[] preloadedlibs = {
		  new luaL_Reg(LUA_DBLIBNAME, luaopen_debug),
		  new luaL_Reg(null, null)
		};


		public static void luaL_openlibs (lua_State L) {
		  /* call open functions from 'loadedlibs' */
		  for (int i=0; i<loadedlibs.Length-1; i++)
		  {
			luaL_Reg lib = loadedlibs[i];
			lua_pushcfunction(L, lib.func);
			lua_pushstring(L, lib.name);
			lua_call(L, 1, 0);
		  }
		  /* add open functions from 'preloadedlibs' into 'package.preload' table */
          //lib = preloadedlibs;
		  luaL_findtable(L, LUA_GLOBALSINDEX, "package.preload", 0);
		  for (int i=0; i<preloadedlibs.Length-1; i++) {
		    luaL_Reg lib = preloadedlibs[i];
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

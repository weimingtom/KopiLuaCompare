/*
** $Id: linit.c,v 1.31 2011/01/26 16:30:02 roberto Exp roberto $
** Initialization of libraries for lua.c and other clients
** See Copyright Notice in lua.h
*/


/*
** If you embed Lua in your program and need to open the standard
** libraries, call luaL_openlibs in your program. If you need a
** different set of libraries, copy this file to your project and edit
** it to suit your needs.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace KopiLua
{
	public partial class Lua
	{

		/*
		** these libs are loaded by lua.c and are readily available to any Lua
		** program
		*/
		private readonly static luaL_Reg[] loadedlibs = {
		  new luaL_Reg("_G", luaopen_base),
		  new luaL_Reg(LUA_LOADLIBNAME, luaopen_package),
          new luaL_Reg(LUA_COLIBNAME, luaopen_coroutine),
		  new luaL_Reg(LUA_TABLIBNAME, luaopen_table),
		  new luaL_Reg(LUA_IOLIBNAME, luaopen_io),
		  new luaL_Reg(LUA_OSLIBNAME, luaopen_os),
		  new luaL_Reg(LUA_STRLIBNAME, luaopen_string),
		  new luaL_Reg(LUA_BITLIBNAME, luaopen_bit32),
		  new luaL_Reg(LUA_MATHLIBNAME, luaopen_math),
		  new luaL_Reg(LUA_DBLIBNAME, luaopen_debug),
		  new luaL_Reg(null, null)
		};


		/*
		** these libs are preloaded and must be required before used
		*/
		private readonly static luaL_Reg[] preloadedlibs = {
		  new luaL_Reg(null, null)
		};


		public static void luaL_openlibs (lua_State L) {
          //const luaL_Reg *lib;
		  /* call open functions from 'loadedlibs' and set results to global table */
		  for (int i=0; i<loadedlibs.Length-1; i++) { //FIXME: changed
			luaL_Reg lib = loadedlibs[i]; //FIXME: added
			luaL_requiref(L, lib.name, lib.func, 1);
			lua_pop(L, 1);  /* remove lib */
		  }
		  /* add open functions from 'preloadedlibs' into 'package.preload' table */
          luaL_getsubtable(L, LUA_REGISTRYINDEX, "_PRELOAD");
		  for (int i=0; i<preloadedlibs.Length-1; i++) { //FIXME: changed
		    luaL_Reg lib = preloadedlibs[i]; //FIXME: added
		    lua_pushcfunction(L, lib.func);
		    lua_setfield(L, -2, lib.name);
		  }
		  lua_pop(L, 1);  /* remove _PRELOAD table */
		}

	}
}

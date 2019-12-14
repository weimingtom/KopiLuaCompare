/*
** $Id: linit.c,v 1.39 2016/12/04 20:17:24 roberto Exp $
** Initialization of libraries for lua.c and other clients
** See Copyright Notice in lua.h
*/


/*
** If you embed Lua in your program and need to open the standard
** libraries, call luaL_openlibs in your program. If you need a
** different set of libraries, copy this file to your project and edit
** it to suit your needs.
**
** You can also *preload* libraries, so that a later 'require' can
** open the library, which is already linked to the application.
** For that, do the following code:
**
**  luaL_getsubtable(L, LUA_REGISTRYINDEX, LUA_PRELOAD_TABLE);
**  lua_pushcfunction(L, luaopen_modname);
**  lua_setfield(L, -2, modname);
**  lua_pop(L, 1);  // remove PRELOAD table
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
		  new luaL_Reg(LUA_MATHLIBNAME, luaopen_math),
		  new luaL_Reg(LUA_UTF8LIBNAME, luaopen_utf8),
		  new luaL_Reg(LUA_DBLIBNAME, luaopen_debug),
		//#if defined(LUA_COMPAT_BITLIB)
		  new luaL_Reg(LUA_BITLIBNAME, luaopen_bit32),	
		//#endif	  
		  new luaL_Reg(null, null)
		};


		public static void luaL_openlibs (lua_State L) {
          //const luaL_Reg *lib;
		  /* "require" functions from 'loadedlibs' and set results to global table */
		  for (int i=0; i<loadedlibs.Length-1; i++) { //FIXME: changed
			luaL_Reg lib = loadedlibs[i]; //FIXME: added
			luaL_requiref(L, lib.name, lib.func, 1);
			lua_pop(L, 1);  /* remove lib */
		  }
		}

	}
}

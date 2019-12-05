/*
** $Id: loadlib.c,v 1.124 2015/01/05 13:51:39 roberto Exp $
** Dynamic library loader for Lua
** See Copyright Notice in lua.h
**
** This module contains an implementation of loadlib for Unix systems
** that have dlfcn, an implementation for Windows, and a stub for other
** systems.
*/
#define _WIN32
#define LUA_COMPAT_MOD

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KopiLua
{
	using lua_Integer = System.Int32;
	
	public partial class Lua
	{
		//#define loadlib_c
		//#define LUA_LIB

		//#include "lprefix.h"


		//#include <stdlib.h>
		//#include <string.h>

		//#include "lua.h"

		//#include "lauxlib.h"
		//#include "lualib.h"


		/*
		** LUA_PATH_VAR and LUA_CPATH_VAR are the names of the environment
		** variables that Lua check to set its paths.
		*/
		//#if !defined(LUA_PATH_VAR)
		private const string LUA_PATH_VAR = "LUA_PATH";
		//#endif

		//#if !defined(LUA_CPATH_VAR)
		private const string LUA_CPATH_VAR = "LUA_CPATH";
		//#endif

		private const string LUA_PATHSUFFIX	 = "_" + LUA_VERSION_MAJOR + "_" + LUA_VERSION_MINOR;

		private const string LUA_PATHVARVERSION = LUA_PATH_VAR + LUA_PATHSUFFIX;
		private const string LUA_CPATHVARVERSION = LUA_CPATH_VAR + LUA_PATHSUFFIX;

		/*
		** LUA_PATH_SEP is the character that separates templates in a path.
		** LUA_PATH_MARK is the string that marks the substitution points in a
		** template.
		** LUA_EXEC_DIR in a Windows path is replaced by the executable's
		** directory.
		** LUA_IGMARK is a mark to ignore all before it when building the
		** luaopen_ function name.
		*/
		//#if !defined (LUA_PATH_SEP)
		private const string LUA_PATH_SEP = ";";
		//#endif
		//#if !defined (LUA_PATH_MARK)
		private const string LUA_PATH_MARK = "?";
		//#endif
		//#if !defined (LUA_EXEC_DIR)
		private const string LUA_EXEC_DIR = "!";
		//#endif
		//#if !defined (LUA_IGMARK)
		private const string LUA_IGMARK = "-";
		//#endif


		/*
		** LUA_CSUBSEP is the character that replaces dots in submodule names
		** when searching for a C loader.
		** LUA_LSUBSEP is the character that replaces dots in submodule names
		** when searching for a Lua loader.
		*/
		//#if !defined(LUA_CSUBSEP)
		private const string LUA_CSUBSEP = LUA_DIRSEP;
		//#endif

		//#if !defined(LUA_LSUBSEP)
		private const string LUA_LSUBSEP	= LUA_DIRSEP;
		//#endif


		/* prefix for open functions in C libraries */
		public const string LUA_POF = "luaopen_";

		/* separator for open functions in C libraries */
		public const string LUA_OFSEP = "_";


		/*
		** unique key for table in the registry that keeps handles
		** for all loaded C libraries
		*/
		private static int CLIBS = 0;

		public const string LIB_FAIL = "open";

		//public static void setprogdir(lua_State L) { }


		/*
		** system-dependent functions
		*/

		public static void setprogdir(lua_State L)
		{
			CharPtr buff = Directory.GetCurrentDirectory();
			luaL_gsub(L, lua_tostring(L, -1), LUA_EXEC_DIR, buff);
			lua_remove(L, -2);  /* remove original string */
		}


		#if LUA_USE_DLOPEN	///* { */
		/*
		** {========================================================================
		** This is an implementation of loadlib based on the dlfcn interface.
		** The dlfcn interface is available in Linux, SunOS, Solaris, IRIX, FreeBSD,
		** NetBSD, AIX 4.2, HPUX 11, and  probably most other Unix flavors, at least
		** as an emulation layer on top of native functions.
		** =========================================================================
		*/

		//#include <dlfcn.h>

		/*
		** Macro to covert pointer to void* to pointer to function. This cast
		** is undefined according to ISO C, but POSIX assumes that it must work.
		** (The '__extension__' in gnu compilers is only to avoid warnings.)
		*/
		//#if defined(__GNUC__)
		//#define cast_func(p) (__extension__ (lua_CFunction)(p))
		//#else
		//#define cast_func(p) ((lua_CFunction)(p))
		//#endif


		static void lsys_unloadlib (void *lib) {
		  dlclose(lib);
		}


		static void *lsys_load (lua_State L, readonly CharPtr path, int seeglb) {
		  void *lib = dlopen(path, RTLD_NOW | (seeglb ? RTLD_GLOBAL : RTLD_LOCAL));
		  if (lib == null) lua_pushstring(L, dlerror());
		  return lib;
		}


		static lua_CFunction lsys_sym (lua_State L, void *lib, readonly CharPtr sym) {
		  lua_CFunction f = cast_func(dlsym(lib, sym));
		  if (f == null) lua_pushstring(L, dlerror());
		  return f;
		}

		/* }====================================================== */



		//#elif defined(LUA_DL_DLL)	/* }{ */
		/*
		** {======================================================================
		** This is an implementation of loadlib for Windows using native functions.
		** =======================================================================
		*/
		
		//#include <windows.h>

		//#undef setprogdir

		/*
		** optional flags for LoadLibraryEx
		*/
		//#if !defined(LUA_LLE_FLAGS)
		private const int LUA_LLE_FLAGS	= 0;
		//#endif


		static void setprogdir (lua_State L) {
		  char buff[MAX_PATH + 1];
		  char *lb;
		  DWORD nsize = sizeof(buff)/GetUnmanagedSize(typeof(char));
		  DWORD n = GetModuleFileNameA(null, buff, nsize);
		  if (n == 0 || n == nsize || (lb = strrchr(buff, '\\')) == null)
			luaL_error(L, "unable to get ModuleFileName");
		  else {
			*lb = '\0';
			luaL_gsub(L, lua_tostring(L, -1), LUA_EXEC_DIR, buff);
			lua_remove(L, -2);  /* remove original string */
		  }
		}


		static void pusherror (lua_State L) {
		  int error = GetLastError();
		  char buffer[128];
		  if (FormatMessageA(FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_FROM_SYSTEM,
			  null, error, 0, buffer, sizeof(buffer)/1, null)) //FIXME:changed, sizeof(char)
			lua_pushstring(L, buffer);
		  else
			lua_pushfstring(L, "system error %d\n", error);
		}

		static void lsys_unloadlib (void *lib) {
		  FreeLibrary((HMODULE)lib);
		}


		static void *lsys_load (lua_State L, readonly CharPtr path, int seeglb) {
		  HMODULE lib = LoadLibraryExA(path, null, LUA_LLE_FLAGS);
          //(void)(seeglb);  /* not used: symbols are 'global' by default */
		  if (lib == null) pusherror(L);
		  return lib;
		}


		static lua_CFunction lsys_sym (lua_State L, void *lib, readonly CharPtr sym) {
		  lua_CFunction f = (lua_CFunction)GetProcAddress((HMODULE)lib, sym);
		  if (f == null) pusherror(L);
		  return f;
		}

		/* }====================================================== */


		#else				///* }{ */
		/*
		** {======================================================
		** Fallback for other systems
		** =======================================================
		*/

		//#undef LIB_FAIL
		//#define LIB_FAIL	"absent"


		public const string DLMSG = "dynamic libraries not enabled; check your Lua installation";


		public static void lsys_unloadlib (object lib) {
		  //(void)(lib);   /* not used */
		}


		public static object lsys_load (lua_State L, CharPtr path, int seeglb) { //FIXME:(added seeglb) already sync
		  //(void)(path); (void)(seeglb);  /* not used */
		  lua_pushliteral(L, DLMSG);
		  return null;
		}


		public static lua_CFunction lsys_sym (lua_State L, object lib, CharPtr sym) {
		  //(void)(lib); (void)(sym);  /* not used */
		  lua_pushliteral(L, DLMSG);
		  return null;
		}

		/* }====================================================== */
		#endif				///* } */



		/*
		** return registry.CLIBS[path]
		*/
		private static object checkclib (lua_State L, CharPtr path) {
		  object plib;
		  lua_rawgetp(L, LUA_REGISTRYINDEX, CLIBS);
		  lua_getfield(L, -1, path);
		  plib = lua_touserdata(L, -1);  /* plib = CLIBS[path] */
		  lua_pop(L, 2);  /* pop CLIBS table and 'plib' */
		  return plib;
		}

		/*
		** registry.CLIBS[path] = plib        -- for queries
		** registry.CLIBS[#CLIBS + 1] = plib  -- also keep a list of all libraries
		*/
		private static void addtoclib (lua_State L, CharPtr path, object plib) {
		  lua_rawgetp(L, LUA_REGISTRYINDEX, CLIBS);
		  lua_pushlightuserdata(L, plib);
		  lua_pushvalue(L, -1);
		  lua_setfield(L, -3, path);  /* CLIBS[path] = plib */
		  lua_rawseti(L, -2, luaL_len(L, -2) + 1);  /* CLIBS[#CLIBS + 1] = plib */
		  lua_pop(L, 1);  /* pop CLIBS table */
		}


		/*
		** __gc tag method for CLIBS table: calls 'lsys_unloadlib' for all lib
		** handles in list CLIBS
		*/
		private static int gctm (lua_State L) {
		  lua_Integer n = luaL_len(L, 1);
		  for (; n >= 1; n--) {  /* for each handle, in reverse order */
		    lua_rawgeti(L, 1, n);  /* get handle CLIBS[n] */
		    lsys_unloadlib(lua_touserdata(L, -1));
		    lua_pop(L, 1);  /* pop handle */
		  }
		  return 0;
		}



		/* error codes for 'lookforfunc' */
		private const int ERRLIB		= 1;
		private const int ERRFUNC		= 2;

		/*
		** Look for a C function named 'sym' in a dynamically loaded library
		** 'path'.
		** First, check whether the library is already loaded; if not, try
		** to load it.
		** Then, if 'sym' is '*', return true (as library has been loaded).
		** Otherwise, look for symbol 'sym' in the library and push a
		** C function with that symbol.
		** Return 0 and 'true' or a function in the stack; in case of
		** errors, return an error code and an error message in the stack.
		*/
		private static int lookforfunc (lua_State L, CharPtr path, CharPtr sym) {
		  object reg = checkclib(L, path);  /* check loaded C libraries */
		  if (reg == null) {  /* must load library? */
		  	reg = lsys_load(L, path, sym[0] == '*'?1:0);  /* global symbols if 'sym'=='*' */
		    if (reg == null) return ERRLIB;  /* unable to load library */
		    addtoclib(L, path, reg);
		  }
		  if (sym[0] == '*') {  /* loading only library (no function)? */
		    lua_pushboolean(L, 1);  /* return 'true' */
		    return 0;  /* no errors */
		  }
		  else {
		    lua_CFunction f = lsys_sym(L, reg, sym);
		    if (f == null)
		      return ERRFUNC;  /* unable to find function */
		    lua_pushcfunction(L, f);  /* else create new function */
		    return 0;  /* no errors */
		  }
		}


		private static int ll_loadlib (lua_State L) {
		  CharPtr path = luaL_checkstring(L, 1);
		  CharPtr init = luaL_checkstring(L, 2);
		  int stat = lookforfunc(L, path, init);
		  if (stat == 0)  /* no errors? */
			return 1;  /* return the loaded function */
		  else {  /* error; error message is on stack top */
			lua_pushnil(L);
			lua_insert(L, -2);
			lua_pushstring(L, (stat == ERRLIB) ?  LIB_FAIL : "init");
			return 3;  /* return nil, error message, and where */
		  }
		}



		/*
		** {======================================================
		** 'require' function
		** =======================================================
		*/


		private static int readable (CharPtr filename) {
		  StreamProxy f = fopen(filename, "r");  /* try to open file */
		  if (f == null) return 0;  /* open failed */
		  fclose(f);
		  return 1;
		}


		private static CharPtr pushnexttemplate (lua_State L, CharPtr path) {
		  CharPtr l;
		  while (path[0] == LUA_PATH_SEP[0]) path = path.next();  /* skip separators */
		  if (path[0] == '\0') return null;  /* no more templates */
		  l = strchr(path, LUA_PATH_SEP[0]);  /* find next separator */
		  if (l == null) l = path + strlen(path);
		  lua_pushlstring(L, path, (uint)(l - path));  /* template */
		  return l;
		}


		private static CharPtr searchpath (lua_State L, CharPtr name,
		                                                CharPtr path,
													    CharPtr sep,
													    CharPtr dirsep) {
		  luaL_Buffer msg = new luaL_Buffer();  /* to build error message */
		  luaL_buffinit(L, msg);
		  if (sep[0] != '\0')  /* non-empty separator? */
            name = luaL_gsub(L, name, sep, dirsep);  /* replace it by 'dirsep' */
		  while ((path = pushnexttemplate(L, path)) != null) {
		    CharPtr filename = luaL_gsub(L, lua_tostring(L, -1),
		                                     LUA_PATH_MARK, name);
		    lua_remove(L, -2);  /* remove path template */
		    if (readable(filename) != 0)  /* does file exist and is readable? */
		      return filename;  /* return that file name */
		    lua_pushfstring(L, "\n\tno file '%s'", filename);
		    lua_remove(L, -2);  /* remove file name */
		    luaL_addvalue(msg);  /* concatenate error msg. entry */
		  }
		  luaL_pushresult(msg);  /* create error message */
		  return null;  /* not found */
		}


		private static int ll_searchpath (lua_State L) {
		  CharPtr f = searchpath(L, luaL_checkstring(L, 1), 
		                            luaL_checkstring(L, 2),
                                    luaL_optstring(L, 3, "."),
									luaL_optstring(L, 4, LUA_DIRSEP));
		  if (f != null) return 1;
		  else {  /* error message is on top of the stack */
		    lua_pushnil(L);
		    lua_insert(L, -2);
		    return 2;  /* return nil + error message */
		  }
		}


		private static CharPtr findfile (lua_State L, CharPtr name,
												      CharPtr pname,
												      CharPtr dirsep) {
		  CharPtr path;
		  lua_getfield(L, lua_upvalueindex(1), pname);
		  path = lua_tostring(L, -1);
		  if (path == null)
			luaL_error(L, "'package.%s' must be a string", pname);
		  return searchpath(L, name, path, ".", dirsep);
		}


		private static int checkload (lua_State L, int stat, CharPtr filename) {
		  if (stat!=0) {  /* module loaded successfully? */
		    lua_pushstring(L, filename);  /* will be 2nd argument to module */
			return 2;  /* return open function and file name */
		  }
		  else
		    return luaL_error(L, "error loading module '%s' from file '%s':\n\t%s",
						          lua_tostring(L, 1), filename, lua_tostring(L, -1));
		}


		private static int searcher_Lua (lua_State L) {
		  CharPtr filename;
		  CharPtr name = luaL_checkstring(L, 1);
		  filename = findfile(L, name, "path", LUA_LSUBSEP);
		  if (filename == null) return 1;  /* module not found in this path */
		  return checkload(L, (luaL_loadfile(L, filename) == LUA_OK)?1:0, filename);
		}


		/*
		** Try to find a load function for module 'modname' at file 'filename'.
		** First, change '.' to '_' in 'modname'; then, if 'modname' has
		** the form X-Y (that is, it has an "ignore mark"), build a function
		** name "luaopen_X" and look for it. (For compatibility, if that
		** fails, it also tries "luaopen_Y".) If there is no ignore mark,
		** look for a function named "luaopen_modname".
		*/
		private static int loadfunc (lua_State L, CharPtr filename, CharPtr modname) {
		  CharPtr openfunc;
		  CharPtr mark;
		  modname = luaL_gsub(L, modname, ".", LUA_OFSEP);
		  mark = strchr(modname, LUA_IGMARK[0]);
		  if (mark != null) {
		    int stat;
		    openfunc = lua_pushlstring(L, modname, (uint)(mark - modname)); //FIXME:(uint)
		    openfunc = lua_pushfstring(L, LUA_POF + "%s", openfunc);
		    stat = lookforfunc(L, filename, openfunc);
		    if (stat != ERRFUNC) return stat;
		    modname = mark + 1;  /* else go ahead and try old-style name */
		  }
		  openfunc = lua_pushfstring(L, LUA_POF + "%s", modname);
		  return lookforfunc(L, filename, openfunc);
		}


		private static int searcher_C (lua_State L) {
		  CharPtr name = luaL_checkstring(L, 1);
		  CharPtr filename = findfile(L, name, "cpath", LUA_CSUBSEP);
		  if (filename == null) return 1;  /* module not found in this path */
		  return checkload(L, (loadfunc(L, filename, name) == 0)?1:0, filename);
		}


		private static int searcher_Croot (lua_State L) {
		  CharPtr filename;
		  CharPtr name = luaL_checkstring(L, 1);
		  CharPtr p = strchr(name, '.');
		  int stat;
		  if (p == null) return 0;  /* is root */
		  lua_pushlstring(L, name, (uint)(p - name));
		  filename = findfile(L, lua_tostring(L, -1), "cpath", LUA_CSUBSEP);
		  if (filename == null) return 1;  /* root not found */
		  if ((stat = loadfunc(L, filename, name)) != 0) {
			if (stat != ERRFUNC) 
			  return checkload(L, 0, filename);  /* real error */
            else {  /* open function not found */
			  lua_pushfstring(L, "\n\tno module '%s' in file '%s'", name, filename);
			  return 1;
			}
		  }
          lua_pushstring(L, filename);  /* will be 2nd argument to module */
		  return 2;
		}


		private static int searcher_preload (lua_State L) {
		  CharPtr name = luaL_checkstring(L, 1);
		  lua_getfield(L, LUA_REGISTRYINDEX, "_PRELOAD");
		  if (lua_getfield(L, -1, name) == LUA_TNIL)  /* not found? */
			lua_pushfstring(L, "\n\tno field package.preload['%s']", name);
		  return 1;
		}


		private static void findloader (lua_State L, CharPtr name) {
		  int i;
		  luaL_Buffer msg = new luaL_Buffer();  /* to build error message */
		  luaL_buffinit(L, msg);
		    /* push 'package.searchers' to index 3 in the stack */
  		  if (lua_getfield(L, lua_upvalueindex(1), "searchers") != LUA_TTABLE)
		    luaL_error(L, "'package.searchers' must be a table");
		  /*  iterate over available searchers to find a loader */
		  for (i = 1; ; i++) {
		    if (lua_rawgeti(L, 3, i) == LUA_TNIL) {  /* no more searchers? */
		      lua_pop(L, 1);  /* remove nil */
		      luaL_pushresult(msg);  /* create error message */
		      luaL_error(L, "module '%s' not found:%s", name, lua_tostring(L, -1));
		    }
		    lua_pushstring(L, name);
		    lua_call(L, 1, 2);  /* call it */
		    if (lua_isfunction(L, -2))  /* did it find a loader? */
		      return;  /* module loader found */
		    else if (lua_isstring(L, -2)!=0) {  /* searcher returned error message? */
		      lua_pop(L, 1);  /* remove extra return */
		      luaL_addvalue(msg);  /* concatenate error message */
		    }
		    else
		      lua_pop(L, 2);  /* remove both returns */
		  }
		}


		public static int ll_require (lua_State L) {
		  CharPtr name = luaL_checkstring(L, 1);
		  lua_settop(L, 1);  /* _LOADED table will be at index 2 */
		  lua_getfield(L, LUA_REGISTRYINDEX, "_LOADED");
		  lua_getfield(L, 2, name);  /* _LOADED[name] */
		  if (lua_toboolean(L, -1)!=0)  /* is it there? */
		    return 1;  /* package is already loaded */
		  /* else must load package */
		  lua_pop(L, 1);  /* remove 'getfield' result */
		  findloader(L, name);
		  lua_pushstring(L, name);  /* pass name as argument to module loader */
		  lua_insert(L, -2);  /* name is 1st argument (before search data) */
		  lua_call(L, 2, 1);  /* run loader to load module */
		  if (!lua_isnil(L, -1))  /* non-nil return? */
		    lua_setfield(L, 2, name);  /* _LOADED[name] = returned value */
		  if (lua_getfield(L, 2, name) == LUA_TNIL) {   /* module set no value? */
		    lua_pushboolean(L, 1);  /* use true as result */
		    lua_pushvalue(L, -1);  /* extra copy to be returned */
		    lua_setfield(L, 2, name);  /* _LOADED[name] = true */
		  }
		  return 1;
		}

		/* }====================================================== */



		/*
		** {======================================================
		** 'module' function
		** =======================================================
		*/
//#if LUA_COMPAT_MODULE //FIXME:???
		
		/*
		** changes the environment variable of calling function
		*/
		private static void set_env (lua_State L) {
		  lua_Debug ar = new lua_Debug();
		  if (lua_getstack(L, 1, ar) == 0 ||
		      lua_getinfo(L, "f", ar) == 0 ||  /* get calling function */
		      lua_iscfunction(L, -1))
		    luaL_error(L, "'module' not called from a Lua function");
		  lua_pushvalue(L, -2);  /* copy new environment table to top */
		  lua_setupvalue(L, -2, 1);
		  lua_pop(L, 1);  /* remove function */
		}


		private static void dooptions (lua_State L, int n) {
		  int i;
		  for (i = 2; i <= n; i++) {
            if (lua_isfunction(L, i)) {  /* avoid 'calling' extra info. */
			  lua_pushvalue(L, i);  /* get option (a function) */
			  lua_pushvalue(L, -2);  /* module */
			  lua_call(L, 1, 0);
			}
		  }
		}


		private static void modinit (lua_State L, CharPtr modname) {
		  CharPtr dot;
		  lua_pushvalue(L, -1);
		  lua_setfield(L, -2, "_M");  /* module._M = module */
		  lua_pushstring(L, modname);
		  lua_setfield(L, -2, "_NAME");
		  dot = strrchr(modname, '.');  /* look for last dot in module name */
		  if (dot == null) dot = modname;
		  else dot = dot.next();
		  /* set _PACKAGE as package name (full module name minus last part) */
		  lua_pushlstring(L, modname, (uint)(dot - modname));
		  lua_setfield(L, -2, "_PACKAGE");
		}


		private static int ll_module (lua_State L) {
		  CharPtr modname = luaL_checkstring(L, 1);
		  int lastarg = lua_gettop(L);  /* last parameter */
  		  luaL_pushmodule(L, modname, 1);  /* get/create module table */
		  /* check whether table already has a _NAME field */
		  if (lua_getfield(L, -1, "_NAME") != LUA_TNIL)
    		lua_pop(L, 1);  /* table is an initialized module */
		  else {  /* no; initialize it */
			lua_pop(L, 1);
			modinit(L, modname);
		  }
		  lua_pushvalue(L, -1);
		  set_env(L);
		  dooptions(L, lastarg);
		  return 1;
		}


		private static int ll_seeall (lua_State L) {
		  luaL_checktype(L, 1, LUA_TTABLE);
		  if (lua_getmetatable(L, 1)==0) {
			lua_createtable(L, 0, 1); /* create new metatable */
			lua_pushvalue(L, -1);
			lua_setmetatable(L, 1);
		  }
		  lua_pushglobaltable(L);
		  lua_setfield(L, -2, "__index");  /* mt.__index = _G */
		  return 0;
		}

//#endif
		/* }====================================================== */



		/* auxiliary mark (for internal use) */
		public readonly static string AUXMARK		= String.Format("{0}", (char)1);


		/*
		** return registry.LUA_NOENV as a boolean
		*/
		private static int noenv (lua_State L) {
		  int b;
		  lua_getfield(L, LUA_REGISTRYINDEX, "LUA_NOENV");
		  b = lua_toboolean(L, -1);
		  lua_pop(L, 1);  /* remove value */
		  return b;
		}


		private static void setpath (lua_State L, CharPtr fieldname, CharPtr envname1,
                                                  CharPtr envname2, CharPtr def) {
		  CharPtr path = getenv(envname1);
		  if (path == null)  /* no environment variable? */
		    path = getenv(envname2);  /* try alternative name */		  
		  if (path == null || noenv(L)!=0)  /* no environment variable? */
			lua_pushstring(L, def);  /* use default */
		  else {
			/* replace ";;" by ";AUXMARK;" and then AUXMARK by default path */
			path = luaL_gsub(L, path, LUA_PATH_SEP + LUA_PATH_SEP,
									  LUA_PATH_SEP + AUXMARK + LUA_PATH_SEP);
			luaL_gsub(L, path, AUXMARK, def);
			lua_remove(L, -2);
		  }
		  setprogdir(L);
		  lua_setfield(L, -2, fieldname);
		}


		private readonly static luaL_Reg[] pk_funcs = {
		  new luaL_Reg("loadlib", ll_loadlib),
          new luaL_Reg("searchpath", ll_searchpath),
//#if defined(LUA_COMPAT_MODULE)
		  new luaL_Reg("seeall", ll_seeall),
//#endif
		  /* placeholders */
		  new luaL_Reg("preload", null),
		  new luaL_Reg("cpath", null),
		  new luaL_Reg("path", null),
		  new luaL_Reg("searchers", null),
		  new luaL_Reg("loaded", null),
		  new luaL_Reg(null, null)
		};


		private readonly static luaL_Reg[] ll_funcs = {
//#if defined(LUA_COMPAT_MODULE)
		  new luaL_Reg("module", ll_module),
//#endif
		  new luaL_Reg("require", ll_require),
		  new luaL_Reg(null, null)
		};


		public readonly static lua_CFunction[] searchers =
		  {searcher_preload, searcher_Lua, searcher_C, searcher_Croot, null};
		private static void createsearcherstable (lua_State L) {
		  int i;
		  /* create 'searchers' table */
		  lua_createtable(L, searchers.Length - 1, 0); //FIXME:changed, sizeof(searchers)/sizeof(searchers[0]) -1
		  /* fill it with pre-defined searchers */
		  for (i=0; searchers[i] != null; i++) {
		    lua_pushvalue(L, -2);  /* set 'package' as upvalue for all searchers */
		    lua_pushcclosure(L, searchers[i], 1);
			lua_rawseti(L, -2, i+1);
		  }
		//#if defined(LUA_COMPAT_LOADERS)
		  lua_pushvalue(L, -1);  /* make a copy of 'searchers' table */
		  lua_setfield(L, -3, "loaders");  /* put it in field 'loaders' */
		//#endif
		  lua_setfield(L, -2, "searchers");  /* put it in field 'searchers' */		  
		}


		/*
		** create table CLIBS to keep track of loaded C libraries,
		** setting a finalizer to close all libraries when closing state.
		*/
		private static void createclibstable (lua_State L) {
		  lua_newtable(L);  /* create CLIBS table */
		  lua_createtable(L, 0, 1);  /* create metatable for CLIBS */
		  lua_pushcfunction(L, gctm);
		  lua_setfield(L, -2, "__gc");  /* set finalizer for CLIBS table */
		  lua_setmetatable(L, -2);
		  lua_rawsetp(L, LUA_REGISTRYINDEX, CLIBS);  /* set CLIBS table in registry */
		}
		
		public static int luaopen_package (lua_State L) {
		  createclibstable(L);
		  luaL_newlib(L, pk_funcs);  /* create 'package' table */
		  createsearcherstable(L);
		  /* set field 'path' */
		  setpath(L, "path", LUA_PATHVARVERSION, LUA_PATH_VAR, LUA_PATH_DEFAULT);
		  /* set field 'cpath' */
		  setpath(L, "cpath", LUA_CPATHVARVERSION, LUA_CPATH_VAR, LUA_CPATH_DEFAULT);
		  /* store config information */
		  lua_pushliteral(L, LUA_DIRSEP + "\n" + LUA_PATH_SEP + "\n" + LUA_PATH_MARK + "\n" +
							 LUA_EXEC_DIR + "\n" + LUA_IGMARK + "\n");
		  lua_setfield(L, -2, "config");
		  /* set field 'loaded' */
		  luaL_getsubtable(L, LUA_REGISTRYINDEX, "_LOADED");
		  lua_setfield(L, -2, "loaded");
		  /* set field 'preload' */
		  luaL_getsubtable(L, LUA_REGISTRYINDEX, "_PRELOAD");
		  lua_setfield(L, -2, "preload");
		  lua_pushglobaltable(L);
		  lua_pushvalue(L, -2);  /* set 'package' as upvalue for next lib */
		  luaL_setfuncs(L, ll_funcs, 1);  /* open lib into global table */
		  lua_pop(L, 1);  /* pop global table */
		  return 1;  /* return 'package' table */
		}

	}
}

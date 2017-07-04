/*
** $Id: loadlib.c,v 1.79 2010/01/13 16:09:05 roberto Exp roberto $
** Dynamic library loader for Lua
** See Copyright Notice in lua.h
**
** This module contains an implementation of loadlib for Unix systems
** that have dlfcn, an implementation for Darwin (Mac OS X), an
** implementation for Windows, and a stub for other systems.
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KopiLua
{
	public partial class Lua
	{

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



		/* prefix for open functions in C libraries */
		public const string LUA_POF = "luaopen_";

		/* separator for open functions in C libraries */
		public const string LUA_OFSEP = "_";


		public const string LIBPREFIX = "LOADLIB: ";

		public const string POF = LUA_POF;
		public const string LIB_FAIL = "open";


		/* error codes for ll_loadfunc */
		public const int ERRLIB			= 1;
		public const int ERRFUNC		= 2;

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


		#if LUA_USE_DLOPEN
		/*
		** {========================================================================
		** This is an implementation of loadlib based on the dlfcn interface.
		** The dlfcn interface is available in Linux, SunOS, Solaris, IRIX, FreeBSD,
		** NetBSD, AIX 4.2, HPUX 11, and  probably most other Unix flavors, at least
		** as an emulation layer on top of native functions.
		** =========================================================================
		*/

		//#include <dlfcn.h>

		static void ll_unloadlib (void *lib) {
		  dlclose(lib);
		}


		static void *ll_load (lua_State L, readonly CharPtr path, int seeglb) {
		  void *lib = dlopen(path, RTLD_NOW | (seeglb ? RTLD_GLOBAL : 0));
		  if (lib == null) lua_pushstring(L, dlerror());
		  return lib;
		}


		static lua_CFunction ll_sym (lua_State L, void *lib, readonly CharPtr sym) {
		  lua_CFunction f = (lua_CFunction)dlsym(lib, sym);
		  if (f == null) lua_pushstring(L, dlerror());
		  return f;
		}

		/* }====================================================== */



		//#elif defined(LUA_DL_DLL)
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
		  DWORD n = GetModuleFileName(null, buff, nsize);
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
		  if (FormatMessage(FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_FROM_SYSTEM,
			  null, error, 0, buffer, sizeof(buffer), null))
			lua_pushstring(L, buffer);
		  else
			lua_pushfstring(L, "system error %d\n", error);
		}

		static void ll_unloadlib (void *lib) {
		  FreeLibrary((HMODULE)lib);
		}


		static void *ll_load (lua_State L, readonly CharPtr path, int seeglb) {
		  HMODULE lib = LoadLibraryEx(path, null, LUA_LLE_FLAGS);
          UNUSED(seeglb);  /* symbols are 'global' by default? */
		  if (lib == null) pusherror(L);
		  return lib;
		}


		static lua_CFunction ll_sym (lua_State L, void *lib, readonly CharPtr sym) {
		  lua_CFunction f = (lua_CFunction)GetProcAddress((HMODULE)lib, sym);
		  if (f == null) pusherror(L);
		  return f;
		}

		/* }====================================================== */



#elif LUA_DL_DYLD
		/*
		** {======================================================================
		** Old native Mac OS X - only for old versions of Mac OS (< 10.3)
		** =======================================================================
		*/

		//#include <mach-o/dyld.h>


		/* Mac appends a `_' before C function names */
		//#undef POF
		//#define POF	"_" LUA_POF


		static void pusherror (lua_State L) {
		  CharPtr err_str;
		  CharPtr err_file;
		  NSLinkEditErrors err;
		  int err_num;
		  NSLinkEditError(err, err_num, err_file, err_str);
		  lua_pushstring(L, err_str);
		}


		static CharPtr errorfromcode (NSObjectFileImageReturnCode ret) {
		  switch (ret) {
			case NSObjectFileImageInappropriateFile:
			  return "file is not a bundle";
			case NSObjectFileImageArch:
			  return "library is for wrong CPU type";
			case NSObjectFileImageFormat:
			  return "bad format";
			case NSObjectFileImageAccess:
			  return "cannot access file";
			case NSObjectFileImageFailure:
			default:
			  return "unable to load library";
		  }
		}


		static void ll_unloadlib (void *lib) {
		  NSUnLinkModule((NSModule)lib, NSUNLINKMODULE_OPTION_RESET_LAZY_REFERENCES);
		}


		static void *ll_load (lua_State L, readonly CharPtr path, int seeglb) {
		  NSObjectFileImage img;
		  NSObjectFileImageReturnCode ret;
		  /* this would be a rare case, but prevents crashing if it happens */
		  if(!_dyld_present()) {
			lua_pushliteral(L, "dyld not present");
			return null;
		  }
		  ret = NSCreateObjectFileImageFromFile(path, img);
		  if (ret == NSObjectFileImageSuccess) {
			NSModule mod = NSLinkModule(img, 
			                            path, 
										NSLINKMODULE_OPTION_RETURN_ON_ERROR |
                                  (seeglb ? 0 : NSLINKMODULE_OPTION_PRIVATE));
			NSDestroyObjectFileImage(img);
			if (mod == null) pusherror(L);
			return mod;
		  }
		  lua_pushstring(L, errorfromcode(ret));
		  return null;
		}


		static lua_CFunction ll_sym (lua_State L, void *lib, readonly CharPtr sym) {
		  NSSymbol nss = NSLookupSymbolInModule((NSModule)lib, sym);
		  if (nss == null) {
			lua_pushfstring(L, "symbol " + LUA_QS + " not found", sym);
			return null;
		  }
		  return (lua_CFunction)NSAddressOfSymbol(nss);
		}

		/* }====================================================== */



#else
		/*
		** {======================================================
		** Fallback for other systems
		** =======================================================
		*/

		//#undef LIB_FAIL
		//#define LIB_FAIL	"absent"


		public const string DLMSG = "dynamic libraries not enabled; check your Lua installation";


		public static void ll_unloadlib (object lib) {
		  //(void)(lib);  /* to avoid warnings */
		}


		public static object ll_load (lua_State L, CharPtr path, int seeglb) { //FIXME:added seeglb
		  //(void)(path);  /* to avoid warnings */
		  lua_pushliteral(L, DLMSG);
		  return null;
		}


		public static lua_CFunction ll_sym (lua_State L, object lib, CharPtr sym) {
		  //(void)(lib); (void)(sym);  /* to avoid warnings */
		  lua_pushliteral(L, DLMSG);
		  return null;
		}

		/* }====================================================== */
		#endif



		private static object ll_register (lua_State L, CharPtr path) {
			// todo: the whole usage of plib here is wrong, fix it - mjf
		  //void **plib;
		  object plib = null;
		  lua_pushfstring(L, "%s%s", LIBPREFIX, path);
		  lua_gettable(L, LUA_REGISTRYINDEX);  /* check library in registry? */
		  if (!lua_isnil(L, -1))  /* is there an entry? */
			plib = lua_touserdata(L, -1);
		  else {  /* no entry yet; create one */
			lua_pop(L, 1);  /* remove result from gettable */
			//plib = lua_newuserdata(L, (uint)Marshal.SizeOf(plib)); //FIXME:deleted ???
			//plib[0] = null;
			luaL_getmetatable(L, "_LOADLIB");
			lua_setmetatable(L, -2);
			lua_pushfstring(L, "%s%s", LIBPREFIX, path);
			lua_pushvalue(L, -2);
			lua_settable(L, LUA_REGISTRYINDEX);
		  }
		  return plib;
		}


		/*
		** __gc tag method: calls library's `ll_unloadlib' function with the lib
		** handle
		*/
		private static int gctm (lua_State L) {
		  object lib = luaL_checkudata(L, 1, "_LOADLIB");
		  if (lib != null) ll_unloadlib(lib);
		  lib = null;  /* mark library as closed */
		  return 0;
		}


		private static int ll_loadfunc (lua_State L, CharPtr path, CharPtr sym) {
		  object reg = ll_register(L, path);
		  if (reg == null) reg = ll_load(L, path, (sym[0] == '*') ? 1 : 0);
		  if (reg == null) return ERRLIB;  /* unable to load library */
		  if (sym[0] == '*') {  /* loading only library (no function)? */
		    lua_pushboolean(L, 1);  /* return 'true' */
		    return 0;  /* no errors */
		  }
		  else {
			lua_CFunction f = ll_sym(L, reg, sym);
			if (f == null)
			  return ERRFUNC;  /* unable to find function */
		    lua_pushcfunction(L, f);  /* else create new function... */
		    lua_pushglobaltable(L);   /* ... and set the standard global table... */
		    lua_setfenv(L, -2);       /* ... as its environment */
		    return 0;  /* no errors */
		  }
		}


		private static int ll_loadlib (lua_State L) {
		  CharPtr path = luaL_checkstring(L, 1);
		  CharPtr init = luaL_checkstring(L, 2);
		  int stat = ll_loadfunc(L, path, init);
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
		  Stream f = fopen(filename, "r");  /* try to open file */
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
		                                             CharPtr path) {
          name = luaL_gsub(L, name, ".", LUA_DIRSEP);
		  lua_pushliteral(L, "");  /* error accumulator */
		  while ((path = pushnexttemplate(L, path)) != null) {
		    CharPtr filename = luaL_gsub(L, lua_tostring(L, -1),
		                                     LUA_PATH_MARK, name);
		    lua_remove(L, -2);  /* remove path template */
		    if (readable(filename) != 0)  /* does file exist and is readable? */
		      return filename;  /* return that file name */
		    lua_pushfstring(L, "\n\tno file " + LUA_QS, filename);
		    lua_remove(L, -2);  /* remove file name */
		    lua_concat(L, 2);  /* add entry to possible error message */
		  }
		  return null;  /* not found */
		}


		private static int ll_searchpath (lua_State L) {
		  CharPtr f = searchpath(L, luaL_checkstring(L, 1), luaL_checkstring(L, 2));
		  if (f != null) return 1;
		  else {  /* error message is on top of the stack */
		    lua_pushnil(L);
		    lua_insert(L, -2);
		    return 2;  /* return nil + error message */
		  }
		}


		private static CharPtr findfile (lua_State L, CharPtr name,
												   CharPtr pname) {
		  CharPtr path;
		  lua_getfield(L, LUA_ENVIRONINDEX, pname);
		  path = lua_tostring(L, -1);
		  if (path == null)
			luaL_error(L, LUA_QL("package.%s") + " must be a string", pname);
		  return searchpath(L, name, path);
		}


		private static void loaderror (lua_State L, CharPtr filename) {
		  luaL_error(L, "error loading module " + LUA_QS + " from file " + LUA_QS + ":\n\t%s",
						lua_tostring(L, 1), filename, lua_tostring(L, -1));
		}


		private static int loader_Lua (lua_State L) {
		  CharPtr filename;
		  CharPtr name = luaL_checkstring(L, 1);
		  filename = findfile(L, name, "path");
		  if (filename == null) return 1;  /* library not found in this path */
		  if (luaL_loadfile(L, filename) != LUA_OK)
			loaderror(L, filename);
		  return 1;  /* library loaded successfully */
		}


		private static int loadfunc (lua_State L, CharPtr filename, CharPtr modname) {
		  CharPtr funcname;
		  CharPtr mark;
		  modname = luaL_gsub(L, modname, ".", LUA_OFSEP);
		  CharPtr mark = strchr(modname, LUA_IGMARK[0]);
		  if (mark != null) {
		    int stat;
		    funcname = lua_pushlstring(L, modname, mark - modname);
		    funcname = lua_pushfstring(L, POF"%s", funcname);
		    stat = ll_loadfunc(L, filename, funcname);
		    if (stat != ERRFUNC) return stat;
		    modname = mark + 1;  /* else go ahead and try old-style name */
		  }
		  funcname = lua_pushfstring(L, POF"%s", modname);
		  return ll_loadfunc(L, filename, funcname);
		}


		private static int loader_C (lua_State L) {
		  CharPtr name = luaL_checkstring(L, 1);
		  CharPtr filename = findfile(L, name, "cpath");
		  if (filename == null) return 1;  /* library not found in this path */
		  if (loadfunc(L, filename, name) != 0)
			loaderror(L, filename);
		  return 1;  /* library loaded successfully */
		}


		private static int loader_Croot (lua_State L) {
		  CharPtr filename;
		  CharPtr name = luaL_checkstring(L, 1);
		  CharPtr p = strchr(name, '.');
		  int stat;
		  if (p == null) return 0;  /* is root */
		  lua_pushlstring(L, name, (uint)(p - name));
		  filename = findfile(L, lua_tostring(L, -1), "cpath");
		  if (filename == null) return 1;  /* root not found */
		  if ((stat = loadfunc(L, filename, name)) != 0) {
			if (stat != ERRFUNC) loaderror(L, filename);  /* real error */
			lua_pushfstring(L, "\n\tno module " + LUA_QS + " in file " + LUA_QS,
							   name, filename);
			return 1;  /* function not found */
		  }
		  return 1;
		}


		private static int loader_preload (lua_State L) {
		  CharPtr name = luaL_checkstring(L, 1);
		  lua_getfield(L, LUA_ENVIRONINDEX, "preload");
		  if (!lua_istable(L, -1))
			luaL_error(L, LUA_QL("package.preload") + " must be a table");
		  lua_getfield(L, -1, name);
		  if (lua_isnil(L, -1))  /* not found? */
			lua_pushfstring(L, "\n\tno field package.preload['%s']", name);
		  return 1;
		}


		public static object sentinel = new object();


		public static int ll_require (lua_State L) {
		  CharPtr name = luaL_checkstring(L, 1);
		  int i;
		  lua_settop(L, 1);  /* _LOADED table will be at index 2 */
		  lua_getfield(L, LUA_REGISTRYINDEX, "_LOADED");
		  lua_getfield(L, 2, name);
		  if (lua_toboolean(L, -1) != 0) {  /* is it there? */
			if (lua_touserdata(L, -1) == sentinel)  /* check loops */
			  luaL_error(L, "loop or previous error loading module " + LUA_QS, name);
			return 1;  /* package is already loaded */
		  }
		  /* else must load it; iterate over available loaders */
		  lua_getfield(L, LUA_ENVIRONINDEX, "loaders");
		  if (!lua_istable(L, -1))
			luaL_error(L, LUA_QL("package.loaders") + " must be a table");
		  lua_pushliteral(L, "");  /* error message accumulator */
		  for (i=1; ; i++) {
			lua_rawgeti(L, -2, i);  /* get a loader */
			if (lua_isnil(L, -1))
			  luaL_error(L, "module " + LUA_QS + " not found:%s",
							name, lua_tostring(L, -2));
			lua_pushstring(L, name);
			lua_call(L, 1, 1);  /* call it */
			if (lua_isfunction(L, -1))  /* did it find module? */
			  break;  /* module loaded successfully */
			else if (lua_isstring(L, -1) != 0)  /* loader returned error message? */
			  lua_concat(L, 2);  /* accumulate it */
			else
			  lua_pop(L, 1);
		  }
		  lua_pushlightuserdata(L, sentinel);
		  lua_setfield(L, 2, name);  /* _LOADED[name] = sentinel */
		  lua_pushstring(L, name);  /* pass name as argument to module */
		  lua_call(L, 1, 1);  /* run loaded module */
		  if (!lua_isnil(L, -1))  /* non-nil return? */
			lua_setfield(L, 2, name);  /* _LOADED[name] = returned value */
		  lua_getfield(L, 2, name);
		  if (lua_touserdata(L, -1) == sentinel) {   /* module did not set a value? */
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
		

		private static void setfenv (lua_State L) {
		  lua_Debug ar = new lua_Debug();
		  if (lua_getstack(L, 1, ar) == 0 ||
		      lua_getinfo(L, "f", ar) == 0 ||  /* get calling function */
		      lua_iscfunction(L, -1))
		    luaL_error(L, LUA_QL("module") + " not called from a Lua function");
		  lua_pushvalue(L, -2);  /* copy new environment table to top */
		  lua_setfenv(L, -2);
		  lua_pop(L, 1);  /* remove function */
		}


		private static void dooptions (lua_State L, int n) {
		  int i;
		  for (i = 2; i <= n; i++) {
			lua_pushvalue(L, i);  /* get option (a function) */
			lua_pushvalue(L, -2);  /* module */
			lua_call(L, 1, 0);
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
		  int loaded = lua_gettop(L) + 1;  /* index of _LOADED table */
		  lua_getfield(L, LUA_REGISTRYINDEX, "_LOADED");
		  lua_getfield(L, loaded, modname);  /* get _LOADED[modname] */
		  if (!lua_istable(L, -1)) {  /* not found? */
			lua_pop(L, 1);  /* remove previous result */
			/* try global variable (and create one if it does not exist) */
		    lua_pushglobaltable(L);
		    if (luaL_findtable(L, 0, modname, 1) != null)
			  return luaL_error(L, "name conflict for module " + LUA_QS, modname);
			lua_pushvalue(L, -1);
			lua_setfield(L, loaded, modname);  /* _LOADED[modname] = new table */
		  }
		  /* check whether table already has a _NAME field */
		  lua_getfield(L, -1, "_NAME");
		  if (!lua_isnil(L, -1))  /* is table an initialized module? */
			lua_pop(L, 1);
		  else {  /* no; initialize it */
			lua_pop(L, 1);
			modinit(L, modname);
		  }
		  lua_pushvalue(L, -1);
		  setfenv(L);
		  dooptions(L, loaded - 1);
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


		/* }====================================================== */



		/* auxiliary mark (for internal use) */
		public readonly static string AUXMARK		= String.Format("{0}", (char)1);

		private static void setpath (lua_State L, CharPtr fieldname, CharPtr envname,
										   CharPtr def) {
		  CharPtr path = getenv(envname);
		  if (path == null)  /* no environment variable? */
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
		  new luaL_Reg("seeall", ll_seeall),
		  new luaL_Reg(null, null)
		};


		private readonly static luaL_Reg[] ll_funcs = {
		  new luaL_Reg("module", ll_module),
		  new luaL_Reg("require", ll_require),
		  new luaL_Reg(null, null)
		};


		public readonly static lua_CFunction[] loaders =
		  {loader_preload, loader_Lua, loader_C, loader_Croot, null};


		public static int luaopen_package (lua_State L) {
		  int i;
		  /* create new type _LOADLIB */
		  luaL_newmetatable(L, "_LOADLIB");
		  lua_pushcfunction(L, gctm);
		  lua_setfield(L, -2, "__gc");
		  /* create `package' table */
		  luaL_register(L, LUA_LOADLIBNAME, pk_funcs);
		  lua_copy(L, -1, LUA_ENVIRONINDEX);
		  /* create `loaders' table */
		  lua_createtable(L, loaders.Length - 1, 0);
		  /* fill it with pre-defined loaders */
		  for (i=0; loaders[i] != null; i++) {
			lua_pushcfunction(L, loaders[i]);
			lua_rawseti(L, -2, i+1);
		  }
		  lua_setfield(L, -2, "loaders");  /* put it in field `loaders' */
		  setpath(L, "path", LUA_PATH_VAR, LUA_PATH_DEFAULT);  /* set field `path' */
		  setpath(L, "cpath", LUA_CPATH_VAR, LUA_CPATH_DEFAULT); /* set field `cpath' */
		  /* store config information */
		  lua_pushliteral(L, LUA_DIRSEP + "\n" + LUA_PATH_SEP + "\n" + LUA_PATH_MARK + "\n" +
							 LUA_EXEC_DIR + "\n" + LUA_IGMARK + "\n");
		  lua_setfield(L, -2, "config");
		  /* set field `loaded' */
		  luaL_findtable(L, LUA_REGISTRYINDEX, "_LOADED", 2);
		  lua_setfield(L, -2, "loaded");
		  /* set field `preload' */
		  lua_newtable(L);
		  lua_setfield(L, -2, "preload");
		  lua_pushglobaltable(L);
		  luaL_register(L, null, ll_funcs);  /* open lib into global table */
		  lua_pop(L, 1);
		  return 1;  /* return 'package' table */
		}

	}
}

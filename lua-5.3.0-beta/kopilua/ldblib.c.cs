/*
** $Id: ldblib.c,v 1.139 2014/05/15 19:27:33 roberto Exp $
** Interface from Lua to its debug API
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace KopiLua
{
	using lua_Integer = System.Int32;
	using lua_Number = System.Double;
	
	public partial class Lua
	{

		private const string HOOKKEY = "_HKEY";



		private static int db_getregistry (lua_State L) {
		  lua_pushvalue(L, LUA_REGISTRYINDEX);
		  return 1;
		}


		private static int db_getmetatable (lua_State L) {
		  luaL_checkany(L, 1);
		  if (lua_getmetatable(L, 1) == 0) {
			lua_pushnil(L);  /* no metatable */
		  }
		  return 1;
		}


		private static int db_setmetatable (lua_State L) {
		  int t = lua_type(L, 2);
		  luaL_argcheck(L, t == LUA_TNIL || t == LUA_TTABLE, 2,
							"nil or table expected");
		  lua_settop(L, 2);
		  lua_setmetatable(L, 1);
		  return 1;  /* return 1st argument */
		}


		private static int db_getuservalue (lua_State L) {
		  if (lua_type(L, 1) != LUA_TUSERDATA)
		    lua_pushnil(L);
		  else
		    lua_getuservalue(L, 1);
		  return 1;
		}


		private static int db_setuservalue (lua_State L) {
		  luaL_checktype(L, 1, LUA_TUSERDATA);
		  luaL_checkany(L, 2);
		  lua_settop(L, 2);
		  lua_setuservalue(L, 1);
		  return 1;
		}


		/*
		** Auxiliary function used by several library functions: check for
		** an optional thread as function's first argument and set 'arg' with
		** 1 if this argument is present (so that functions can skip it to
		** access their other arguments)
		*/
		private static lua_State getthread (lua_State L, out int arg) {
		  if (lua_isthread(L, 1)) {
			arg = 1;
			return lua_tothread(L, 1);
		  }
		  else {
			arg = 0;
			return L;  /* function will operate over current thread */
		  }
		}


		/*
		** Variations of 'lua_settable', used by 'db_getinfo' to put results
		** from 'lua_getinfo' into result table. Key is always a string;
		** value can be a string, an int, or a boolean.
		*/
		private static void settabss (lua_State L, CharPtr k, CharPtr v) {
		  lua_pushstring(L, v);
		  lua_setfield(L, -2, k);
		}

		private static void settabsi (lua_State L, CharPtr k, int v) {
		  lua_pushinteger(L, v);
		  lua_setfield(L, -2, k);
		}

		private static void settabsb (lua_State L, CharPtr k, int v) {
		  lua_pushboolean(L, v);
		  lua_setfield(L, -2, k);
		}


		/*
		** In function 'db_getinfo', the call to 'lua_getinfo' may push
		** results on the stack; later it creates the result table to put
		** these objects. Function 'treatstackoption' puts the result from
		** 'lua_getinfo' on top of the result table so that it can call
		** 'lua_setfield'.
		*/
		private static void treatstackoption (lua_State L, lua_State L1, CharPtr fname) {
		  if (L == L1)
		    lua_rotate(L, -2, 1);  /* exchange object and table */
		  else
		    lua_xmove(L1, L, 1);  /* move object to the "main" stack */
		  lua_setfield(L, -2, fname);  /* put object into table */
		}


		/*
		** Calls 'lua_getinfo' and collects all results in a new table.
		*/
		private static int db_getinfo (lua_State L) {
		  lua_Debug ar = new lua_Debug();
		  int arg;
		  lua_State L1 = getthread(L, out arg);
		  CharPtr options = luaL_optstring(L, arg+2, "flnStu");
		  if (lua_isfunction(L, arg + 1)) {  /* info about a function? */
		    options = lua_pushfstring(L, ">%s", options);  /* add '>' to 'options' */
		    lua_pushvalue(L, arg + 1);  /* move function to 'L1' stack */
		    lua_xmove(L, L1, 1);
		  }
		  else {  /* stack level */
		    if (0==lua_getstack(L1, (int)luaL_checkinteger(L, arg + 1), ar)) {
		      lua_pushnil(L);  /* level out of range */
		      return 1;
		    }
		  }
		  if (lua_getinfo(L1, options, ar)==0)
			return luaL_argerror(L, arg+2, "invalid option");
		  lua_newtable(L);  /* table to collect results */
		  if (strchr(options, 'S') != null) {
			settabss(L, "source", ar.source);
			settabss(L, "short_src", ar.short_src);
			settabsi(L, "linedefined", ar.linedefined);
			settabsi(L, "lastlinedefined", ar.lastlinedefined);
			settabss(L, "what", ar.what);
		  }
		  if (strchr(options, 'l') != null)
			settabsi(L, "currentline", ar.currentline);
		  if (strchr(options, 'u') != null) {
			settabsi(L, "nups", ar.nups);
		    settabsi(L, "nparams", ar.nparams);
		    settabsb(L, "isvararg", ar.isvararg);
          }
		  if (strchr(options, 'n') != null) {
			settabss(L, "name", ar.name);
			settabss(L, "namewhat", ar.namewhat);
		  }
		  if (strchr(options, 't') != null)
		    settabsb(L, "istailcall", ar.istailcall);
		  if (strchr(options, 'L') != null)
			treatstackoption(L, L1, "activelines");
		  if (strchr(options, 'f') != null)
			treatstackoption(L, L1, "func");
		  return 1;  /* return table */
		}
		    

		private static int db_getlocal (lua_State L) {
		  int arg;
		  lua_State L1 = getthread(L, out arg);
		  lua_Debug ar = new lua_Debug();
		  CharPtr name;
		  int nvar = (int)luaL_checkinteger(L, arg + 2);  /* local-variable index */
		  if (lua_isfunction(L, arg + 1)) {  /* function argument? */
		    lua_pushvalue(L, arg + 1);  /* push function */
		    lua_pushstring(L, lua_getlocal(L, null, nvar));  /* push local name */
		    return 1;  /* return only name (there is no value) */
		  }
		  else {  /* stack-level argument */
		    int level = (int)luaL_checkinteger(L, arg + 1);
			if (lua_getstack(L1, level, ar)==0)  /* out of range? */
			  return luaL_argerror(L, arg+1, "level out of range");
			name = lua_getlocal(L1, ar, nvar);
			if (name != null) {
			  lua_xmove(L1, L, 1);  /* move local value */
			  lua_pushstring(L, name);  /* push name */
			  lua_rotate(L, -2, 1);  /* re-order */
			  return 2;
			}
		    else {
			  lua_pushnil(L);  /* no name (nor value) */
			  return 1;
			}
		  }
		}


		private static int db_setlocal (lua_State L) {
		  int arg;
		  lua_State L1 = getthread(L, out arg);
		  lua_Debug ar = new lua_Debug();
		  int level = (int)luaL_checkinteger(L, arg + 1);
		  if (lua_getstack(L1, level, ar)==0)  /* out of range? */
			return luaL_argerror(L, arg+1, "level out of range");
		  luaL_checkany(L, arg+3);
		  lua_settop(L, arg+3);
		  lua_xmove(L, L1, 1);
		  lua_pushstring(L, lua_setlocal(L1, ar, (int)luaL_checkinteger(L, arg+2)));
		  return 1;
		}


		/*
		** get (if 'get' is true) or set an upvalue from a closure
		*/
		private static int auxupvalue (lua_State L, int get) {
		  CharPtr name;
		  int n = (int)luaL_checkinteger(L, 2);  /* upvalue index */
		  luaL_checktype(L, 1, LUA_TFUNCTION);  /* closure */
		  name = (get!=0) ? lua_getupvalue(L, 1, n) : lua_setupvalue(L, 1, n);
		  if (name == null) return 0;
		  lua_pushstring(L, name);
		  lua_insert(L, -(get+1));  /* no-op if get is false */
		  return get + 1;
		}


		private static int db_getupvalue (lua_State L) {
		  return auxupvalue(L, 1);
		}


		private static int db_setupvalue (lua_State L) {
		  luaL_checkany(L, 3);
		  return auxupvalue(L, 0);
		}



		/*
		** Check whether a given upvalue from a given closure exists and
		** returns its index
		*/
		private static int checkupval (lua_State L, int argf, int argnup) {
		  int nup = (int)luaL_checkinteger(L, argnup);
		  luaL_checktype(L, argf, LUA_TFUNCTION);
		  luaL_argcheck(L, (lua_getupvalue(L, argf, nup) != null), argnup,
                   "invalid upvalue index");
		  return nup;
		}


		private static int db_upvalueid (lua_State L) {
		  int n = checkupval(L, 1, 2);
		  lua_pushlightuserdata(L, lua_upvalueid(L, 1, n));
		  return 1;
		}


		private static int db_upvaluejoin (lua_State L) {
		  int n1 = checkupval(L, 1, 2);
		  int n2 = checkupval(L, 3, 4);
		  luaL_argcheck(L, !lua_iscfunction(L, 1), 1, "Lua function expected");
		  luaL_argcheck(L, !lua_iscfunction(L, 3), 3, "Lua function expected");
		  lua_upvaluejoin(L, 1, n1, 3, n2);
		  return 0;
		}


		/*
		** The hook table (at registry[HOOKKEY]) maps threads to their current
		** hook function
		*/
		private static int gethooktable(lua_State L) { return luaL_getsubtable(L, LUA_REGISTRYINDEX, HOOKKEY); }



		/*
		** Call hook function registered at hook table for the current
		** thread (if there is one)
		*/
		private static readonly string[] hooknames = //FIXME:changed
			{"call", "return", "line", "count", "tail call"};
		private static void hookf (lua_State L, lua_Debug ar) {
		  gethooktable(L);
		  lua_pushthread(L);
		  if (lua_rawget(L, -2) == LUA_TFUNCTION) {  /* is there a hook function? */
			lua_pushstring(L, hooknames[(int)ar.event_]);  /* push event name */
			if (ar.currentline >= 0)
			  lua_pushinteger(L, ar.currentline);  /* push current line */
			else lua_pushnil(L);
			lua_assert(lua_getinfo(L, "lS", ar));
			lua_call(L, 2, 0);  /* call hook function */
		  }
		}


		/*
		** Convert a string mask (for 'sethook') into a bit mask
		*/
		private static int makemask (CharPtr smask, int count) {
		  int mask = 0;
		  if (strchr(smask, 'c') != null) mask |= LUA_MASKCALL;
		  if (strchr(smask, 'r') != null) mask |= LUA_MASKRET;
		  if (strchr(smask, 'l') != null) mask |= LUA_MASKLINE;
		  if (count > 0) mask |= LUA_MASKCOUNT;
		  return mask;
		}


		/*
		** Convert a bit mask (for 'gethook') into a string mask
		*/
		private static CharPtr unmakemask (int mask, CharPtr smask) {
			int i = 0;
			if ((mask & LUA_MASKCALL) != 0) smask[i++] = 'c';
			if ((mask & LUA_MASKRET) != 0) smask[i++] = 'r';
			if ((mask & LUA_MASKLINE) != 0) smask[i++] = 'l';
			smask[i] = '\0';
			return smask;
		}


		private static int db_sethook (lua_State L) {
		  int arg, mask, count;
		  lua_Hook func;
		  lua_State L1 = getthread(L, out arg);
		  if (lua_isnoneornil(L, arg+1)) {  /* no hook? */
		    lua_settop(L, arg+1);
		    func = null; mask = 0; count = 0;  /* turn off hooks */
		  }
		  else {
		    CharPtr smask = luaL_checkstring(L, arg+2);
		    luaL_checktype(L, arg+1, LUA_TFUNCTION);
		    count = (int)luaL_optinteger(L, arg + 3, 0);
		    func = hookf; mask = makemask(smask, count);
		  }
		  if (gethooktable(L) == 0) {  /* creating hook table? */
		    lua_pushstring(L, "k");
		    lua_setfield(L, -2, "__mode");  /** hooktable.__mode = "k" */
		    lua_pushvalue(L, -1);
		    lua_setmetatable(L, -2);  /* setmetatable(hooktable) = hooktable */
		  }
		  lua_pushthread(L1); lua_xmove(L1, L, 1);  /* key */
		  lua_pushvalue(L, arg+1);  /* value */
		  lua_rawset(L, -3);  /* hooktable[L1] = new Lua hook */
		  lua_sethook(L1, func, mask, count);  /* set hooks */
		  return 0;
		}


		private static int db_gethook (lua_State L) {
		  int arg;
		  lua_State L1 = getthread(L, out arg);
		  CharPtr buff = new char[5];
		  int mask = lua_gethookmask(L1);
		  lua_Hook hook = lua_gethook(L1);
		  if (hook != null && hook != hookf)  /* external hook? */
			lua_pushliteral(L, "external hook");
		  else {
		    gethooktable(L);
		    lua_pushthread(L1); lua_xmove(L1, L, 1);
		    lua_rawget(L, -2);  /* 1st result = hooktable[L1] */
		    lua_remove(L, -2);  /* remove hook table */
		  }
		  lua_pushstring(L, unmakemask(mask, buff));  /* 2nd result = mask */
		  lua_pushinteger(L, lua_gethookcount(L1));  /* 3rd result = count */
		  return 3;
		}


		private static int db_debug (lua_State L) {
		  for (;;) {
			CharPtr buffer = new char[250];
			luai_writestringerror("%s", "lua_debug> ");
			if (fgets(buffer, stdin) == null ||
				strcmp(buffer, "cont\n") == 0)
			  return 0;
			if (luaL_loadbuffer(L, buffer, (uint)strlen(buffer), "=(debug command)")!=0 ||
				lua_pcall(L, 0, 0, 0)!=0)
			  luai_writestringerror("%s\n", lua_tostring(L, -1));
			lua_settop(L, 0);  /* remove eventual returns */
		  }
		}


		private static int db_traceback (lua_State L) {
		  int arg;
		  lua_State L1 = getthread(L, out arg);
		  CharPtr msg = lua_tostring(L, arg + 1);
		  if (msg == null && !lua_isnoneornil(L, arg + 1))  /* non-string 'msg'? */
		    lua_pushvalue(L, arg + 1);  /* return it untouched */
		  else {
		    int level = (int)luaL_optinteger(L, arg + 2, (L == L1) ? 1 : 0);
		    luaL_traceback(L, L1, msg, level);
		  }
		  return 1;
		}


		private readonly static luaL_Reg[] dblib = {
		  new luaL_Reg("debug", db_debug),
		  new luaL_Reg("getuservalue", db_getuservalue),
		  new luaL_Reg("gethook", db_gethook),
		  new luaL_Reg("getinfo", db_getinfo),
		  new luaL_Reg("getlocal", db_getlocal),
		  new luaL_Reg("getregistry", db_getregistry),
		  new luaL_Reg("getmetatable", db_getmetatable),
		  new luaL_Reg("getupvalue", db_getupvalue),
		  new luaL_Reg("upvaluejoin", db_upvaluejoin),
		  new luaL_Reg("upvalueid", db_upvalueid),
		  new luaL_Reg("setuservalue", db_setuservalue),
		  new luaL_Reg("sethook", db_sethook),
		  new luaL_Reg("setlocal", db_setlocal),
		  new luaL_Reg("setmetatable", db_setmetatable),
		  new luaL_Reg("setupvalue", db_setupvalue),
		  new luaL_Reg("traceback", db_traceback),
		  new luaL_Reg(null, null)
		};


		public static int luaopen_debug (lua_State L) {
		  luaL_newlib(L, dblib);
		  return 1;
		}

	}
}

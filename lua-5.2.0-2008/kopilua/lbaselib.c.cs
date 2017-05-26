/*
** $Id: lbaselib.c,v 1.199 2007/10/17 17:26:39 roberto Exp roberto $
** Basic library
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace KopiLua
{
	using lua_Number = System.Double;

	public partial class Lua
	{
		/*
		** If your system does not support `stdout', you can just remove this function.
		** If you need, you can define your own `print' function, following this
		** model but changing `fputs' to put the strings at a proper place
		** (a console window or a log file, for instance).
		*/
		private static int luaB_print (lua_State L) {
		  int n = lua_gettop(L);  /* number of arguments */
		  int i;
		  lua_getglobal(L, "tostring");
		  for (i=1; i<=n; i++) {
			CharPtr s;
			lua_pushvalue(L, -1);  /* function to be called */
			lua_pushvalue(L, i);   /* value to print */
			lua_call(L, 1, 1);
			s = lua_tostring(L, -1);  /* get result */
			if (s == null)
			  return luaL_error(L, LUA_QL("tostring") + " must return a string to " +
								   LUA_QL("print"));
			if (i > 1) fputs("\t", stdout);
			fputs(s, stdout);
			lua_pop(L, 1);  /* pop result */
		  }
		  Console.Write("\n", stdout);
		  return 0;
		}


		private static int luaB_tonumber (lua_State L) {
		  int base_ = luaL_optint(L, 2, 10);
		  if (base_ == 10) {  /* standard conversion */
			luaL_checkany(L, 1);
			if (lua_isnumber(L, 1) != 0) {
			  lua_pushnumber(L, lua_tonumber(L, 1));
			  return 1;
			}
		  }
		  else {
			CharPtr s1 = luaL_checkstring(L, 1);
			CharPtr s2;
			ulong n;
			luaL_argcheck(L, 2 <= base_ && base_ <= 36, 2, "base out of range");
			n = strtoul(s1, out s2, base_);
			if (s1 != s2) {  /* at least one valid digit? */
			  while (isspace((byte)(s2[0]))) s2 = s2.next();  /* skip trailing spaces */
			  if (s2[0] == '\0') {  /* no invalid trailing characters? */
				lua_pushnumber(L, (lua_Number)n);
				return 1;
			  }
			}
		  }
		  lua_pushnil(L);  /* else not a number */
		  return 1;
		}


		private static int luaB_error (lua_State L) {
		  int level = luaL_optint(L, 2, 1);
		  lua_settop(L, 1);
		  if ((lua_isstring(L, 1)!=0) && (level > 0)) {  /* add extra information? */
			luaL_where(L, level);
			lua_pushvalue(L, 1);
			lua_concat(L, 2);
		  }
		  return lua_error(L);
		}


		private static int luaB_getmetatable (lua_State L) {
		  luaL_checkany(L, 1);
		  if (lua_getmetatable(L, 1)==0) {
			lua_pushnil(L);
			return 1;  /* no metatable */
		  }
		  luaL_getmetafield(L, 1, "__metatable");
		  return 1;  /* returns either __metatable field (if present) or metatable */
		}


		private static int luaB_setmetatable (lua_State L) {
		  int t = lua_type(L, 2);
		  luaL_checktype(L, 1, LUA_TTABLE);
		  luaL_argcheck(L, t == LUA_TNIL || t == LUA_TTABLE, 2,
							"nil or table expected");
		  if (luaL_getmetafield(L, 1, "__metatable") != 0)
			luaL_error(L, "cannot change a protected metatable");
		  lua_settop(L, 2);
		  lua_setmetatable(L, 1);
		  return 1;
		}


		private static void getfunc (lua_State L, int opt) {
		  if (lua_isfunction(L, 1)) lua_pushvalue(L, 1);
		  else {
			lua_Debug ar = new lua_Debug();
			int level = opt != 0 ? luaL_optint(L, 1, 1) : luaL_checkint(L, 1);
			luaL_argcheck(L, level >= 0, 1, "level must be non-negative");
			if (lua_getstack(L, level, ar) == 0)
			  luaL_argerror(L, 1, "invalid level");
			lua_getinfo(L, "f", ar);
			if (lua_isnil(L, -1))
			  luaL_error(L, "no function environment for tail call at level %d",
							level);
		  }
		}


		private static int luaB_getfenv (lua_State L) {
		  getfunc(L, 1);
		  if (lua_iscfunction(L, -1))  /* is a C function? */
			lua_pushvalue(L, LUA_GLOBALSINDEX);  /* return the thread's global env. */
		  else
			lua_getfenv(L, -1);
		  return 1;
		}


		private static int luaB_setfenv (lua_State L) {
		  luaL_checktype(L, 2, LUA_TTABLE);
		  getfunc(L, 0);
		  lua_pushvalue(L, 2);
		  if ((lua_isnumber(L, 1)!=0) && (lua_tonumber(L, 1) == 0)) {
			/* change environment of current thread */
			lua_pushthread(L);
			lua_insert(L, -2);
			lua_setfenv(L, -2);
			return 0;
		  }
		  else if (lua_iscfunction(L, -2) || lua_setfenv(L, -2) == 0)
			luaL_error(L,
				  LUA_QL("setfenv") + " cannot change environment of given object");
		  return 1;
		}


		private static int luaB_rawequal (lua_State L) {
		  luaL_checkany(L, 1);
		  luaL_checkany(L, 2);
		  lua_pushboolean(L, lua_rawequal(L, 1, 2));
		  return 1;
		}


		private static int luaB_rawget (lua_State L) {
		  luaL_checktype(L, 1, LUA_TTABLE);
		  luaL_checkany(L, 2);
		  lua_settop(L, 2);
		  lua_rawget(L, 1);
		  return 1;
		}

		private static int luaB_rawset (lua_State L) {
		  luaL_checktype(L, 1, LUA_TTABLE);
		  luaL_checkany(L, 2);
		  luaL_checkany(L, 3);
		  lua_settop(L, 3);
		  lua_rawset(L, 1);
		  return 1;
		}


		private static int luaB_gcinfo (lua_State L) {
		  lua_pushinteger(L, lua_getgccount(L));
		  return 1;
		}

		public static readonly CharPtr[] opts = {"stop", "restart", "collect",
			"count", "step", "setpause", "setstepmul", null};
		public readonly static int[] optsnum = {LUA_GCSTOP, LUA_GCRESTART, LUA_GCCOLLECT,
			LUA_GCCOUNT, LUA_GCSTEP, LUA_GCSETPAUSE, LUA_GCSETSTEPMUL};

		private static int luaB_collectgarbage (lua_State L) {		  
		  int o = luaL_checkoption(L, 1, "collect", opts);
		  int ex = luaL_optint(L, 2, 0);
		  int res = lua_gc(L, optsnum[o], ex);
		  switch (optsnum[o]) {
			case LUA_GCCOUNT: {
			  int b = lua_gc(L, LUA_GCCOUNTB, 0);
			  lua_pushnumber(L, res + ((lua_Number)b/1024));
			  return 1;
			}
			case LUA_GCSTEP: {
			  lua_pushboolean(L, res);
			  return 1;
			}
			default: {
			  lua_pushinteger(L, res);
			  return 1;
			}
		  }
		}


		private static int luaB_type (lua_State L) {
		  luaL_checkany(L, 1);
		  lua_pushstring(L, luaL_typename(L, 1));
		  return 1;
		}
		private static int pairsmeta (lua_State L, CharPtr method, int iszero) {
		  if (luaL_getmetafield(L, 1, method) == 0) {  /* no metamethod? */
		    luaL_checktype(L, 1, LUA_TTABLE);  /* argument must be a table */
		    lua_pushvalue(L, lua_upvalueindex(1));  /* will return generator, */
		    lua_pushvalue(L, 1);  /* state, */
		    if (iszero != 0) lua_pushinteger(L, 0);  /* and initial value */
		    else lua_pushnil(L);
		  }
		  else {
		    lua_pushvalue(L, 1);  /* argument 'self' to metamethod */
		    lua_call(L, 1, 3);  /* get 3 values from metamethod */
		  }
		  return 3;
		}


		private static int luaB_next (lua_State L) {
		  luaL_checktype(L, 1, LUA_TTABLE);
		  lua_settop(L, 2);  /* create a 2nd argument if there isn't one */
		  if (lua_next(L, 1) != 0)
			return 2;
		  else {
			lua_pushnil(L);
			return 1;
		  }
		}


		private static int luaB_pairs (lua_State L) {
		  return pairsmeta(L, "__pairs", 0);
		}


		private static int ipairsaux (lua_State L) {
		  int i = luaL_checkint(L, 2);
		  luaL_checktype(L, 1, LUA_TTABLE);
		  i++;  /* next value */
		  lua_pushinteger(L, i);
		  lua_rawgeti(L, 1, i);
		  return (lua_isnil(L, -1)) ? 0 : 2;
		}


		private static int luaB_ipairs (lua_State L) {
		  return pairsmeta(L, "__ipairs", 1);
		}


		private static int load_aux (lua_State L, int status) {
		  if (status == LUA_OK)  /* OK? */
			return 1;
		  else {
			lua_pushnil(L);
			lua_insert(L, -2);  /* put before error message */
			return 2;  /* return nil plus error message */
		  }
		}


		private static int luaB_loadstring (lua_State L) {
		  uint l;
		  CharPtr s = luaL_checklstring(L, 1, out l);
		  CharPtr chunkname = luaL_optstring(L, 2, s);
		  return load_aux(L, luaL_loadbuffer(L, s, l, chunkname));
		}


		private static int luaB_loadfile (lua_State L) {
		  CharPtr fname = luaL_optstring(L, 1, null);
		  return load_aux(L, luaL_loadfile(L, fname));
		}


		/*
		** Reader for generic `load' function: `lua_load' uses the
		** stack for internal stuff, so the reader cannot change the
		** stack top. Instead, it keeps its resulting string in a
		** reserved slot inside the stack.
		*/
		private static CharPtr generic_reader (lua_State L, object ud, out uint size) {
		  //(void)ud;  /* to avoid warnings */
		  luaL_checkstack(L, 2, "too many nested functions");
		  lua_pushvalue(L, 1);  /* get function */
		  lua_call(L, 0, 1);  /* call it */
		  if (lua_isnil(L, -1)) {
			size = 0;
			return null;
		  }
		  else if (lua_isstring(L, -1) != 0)
		  {
			  lua_replace(L, 3);  /* save string in a reserved stack slot */
			  return lua_tolstring(L, 3, out size);
		  }
		  else
		  {
			  size = 0;
			  luaL_error(L, "reader function must return a string");
		  }
		  return null;  /* to avoid warnings */
		}


		private static int luaB_load (lua_State L) {
		  int status;
		  CharPtr cname = luaL_optstring(L, 2, "=(load)");
		  luaL_checktype(L, 1, LUA_TFUNCTION);
		  lua_settop(L, 3);  /* function, eventual name, plus one reserved slot */
		  status = lua_load(L, generic_reader, null, cname);
		  return load_aux(L, status);
		}


		private static int luaB_dofile (lua_State L) {
		  CharPtr fname = luaL_optstring(L, 1, null);
		  lua_settop(L, 1);
		  if (luaL_loadfile(L, fname) != LUA_OK) lua_error(L);
		  lua_call(L, 0, LUA_MULTRET);
		  return lua_gettop(L) - 1;
		}


		private static int luaB_assert (lua_State L) {
		  luaL_checkany(L, 1);
		  if (lua_toboolean(L, 1)==0)
			return luaL_error(L, "%s", luaL_optstring(L, 2, "assertion failed!"));
		  return lua_gettop(L);
		}


		private static int luaB_unpack (lua_State L) {
		  int i, e, n;
		  luaL_checktype(L, 1, LUA_TTABLE);
		  i = luaL_optint(L, 2, 1);
		  e = luaL_opt_integer(L, luaL_checkint, 3, (int)lua_objlen(L, 1));
		  n = e - i + 1;  /* number of elements */
		  if (n <= 0) return 0;  /* empty range */
		  luaL_checkstack(L, n, "table too big to unpack");
		  for (; i<=e; i++)  /* push arg[i...e] */
		    lua_rawgeti(L, 1, i);
		  return n;
		}


		private static int luaB_select (lua_State L) {
		  int n = lua_gettop(L);
		  if (lua_type(L, 1) == LUA_TSTRING && lua_tostring(L, 1)[0] == '#') {
			lua_pushinteger(L, n-1);
			return 1;
		  }
		  else {
			int i = luaL_checkint(L, 1);
			if (i < 0) i = n + i;
			else if (i > n) i = n;
			luaL_argcheck(L, 1 <= i, 1, "index out of range");
			return n - i;
		  }
		}


		private static int luaB_pcall (lua_State L) {
		  int status;
		  luaL_checkany(L, 1);
		  status = lua_pcall(L, lua_gettop(L) - 1, LUA_MULTRET, 0);
		  lua_pushboolean(L, (status == LUA_OK) ? 1 : 0);
		  lua_insert(L, 1);
		  return lua_gettop(L);  /* return status + all results */
		}


		private static int luaB_xpcall (lua_State L) {
		  int status;
		  int n = lua_gettop(L);
		  luaL_argcheck(L, n >= 2, 2, "value expected");
		  lua_pushvalue(L, 1);  /* exchange function... */
		  lua_pushvalue(L, 2);  /* ...and error handler */
		  lua_replace(L, 1);
		  lua_replace(L, 2);
		  status = lua_pcall(L, n - 2, LUA_MULTRET, 1);
		  lua_pushboolean(L, (status == LUA_OK) ? 1 : 0);
		  lua_replace(L, 1);
		  return lua_gettop(L);  /* return status + all results */
		}


		private static int luaB_tostring (lua_State L) {
		  luaL_checkany(L, 1);
		  luaL_tostring(L, 1);
		  return 1;
		}


		private static int luaB_newproxy (lua_State L) {
		  lua_settop(L, 1);
		  lua_newuserdata(L, 0);  /* create proxy */
		  if (lua_toboolean(L, 1) == 0)
			return 1;  /* no metatable */
		  else if (lua_isboolean(L, 1)) {
			lua_newtable(L);  /* create a new metatable `m' ... */
			lua_pushvalue(L, -1);  /* ... and mark `m' as a valid metatable */
			lua_pushboolean(L, 1);
			lua_rawset(L, lua_upvalueindex(1));  /* weaktable[m] = true */
		  }
		  else {
			int validproxy = 0;  /* to check if weaktable[metatable(u)] == true */
			if (lua_getmetatable(L, 1) != 0) {
			  lua_rawget(L, lua_upvalueindex(1));
			  validproxy = lua_toboolean(L, -1);
			  lua_pop(L, 1);  /* remove value */
			}
			luaL_argcheck(L, validproxy!=0, 1, "boolean or proxy expected");
			lua_getmetatable(L, 1);  /* metatable is valid; get it */
		  }
		  lua_setmetatable(L, 2);
		  return 1;
		}


		private readonly static luaL_Reg[] base_funcs = {
		  new luaL_Reg("assert", luaB_assert),
		  new luaL_Reg("collectgarbage", luaB_collectgarbage),
		  new luaL_Reg("dofile", luaB_dofile),
		  new luaL_Reg("error", luaB_error),
		  new luaL_Reg("gcinfo", luaB_gcinfo),
		  new luaL_Reg("getfenv", luaB_getfenv),
		  new luaL_Reg("getmetatable", luaB_getmetatable),
		  new luaL_Reg("loadfile", luaB_loadfile),
		  new luaL_Reg("load", luaB_load),
		  new luaL_Reg("loadstring", luaB_loadstring),
		  new luaL_Reg("next", luaB_next),
		  new luaL_Reg("pcall", luaB_pcall),
		  new luaL_Reg("print", luaB_print),
		  new luaL_Reg("rawequal", luaB_rawequal),
		  new luaL_Reg("rawget", luaB_rawget),
		  new luaL_Reg("rawset", luaB_rawset),
		  new luaL_Reg("select", luaB_select),
		  new luaL_Reg("setfenv", luaB_setfenv),
		  new luaL_Reg("setmetatable", luaB_setmetatable),
		  new luaL_Reg("tonumber", luaB_tonumber),
		  new luaL_Reg("tostring", luaB_tostring),
		  new luaL_Reg("type", luaB_type),
		  new luaL_Reg("unpack", luaB_unpack),
		  new luaL_Reg("xpcall", luaB_xpcall),
		  new luaL_Reg(null, null)
		};


		/*
		** {======================================================
		** Coroutine library
		** =======================================================
		*/


		private static int auxresume (lua_State L, lua_State co, int narg) {
		  int status;
		  if (lua_checkstack(co, narg)==0)
			luaL_error(L, "too many arguments to resume");
	      if (lua_status(co) == LUA_OK && lua_gettop(co) == 0) {
		    lua_pushliteral(L, "cannot resume dead coroutine");
		    return -1;  /* error flag */
		  }
		  lua_xmove(L, co, narg);
		  status = lua_resume(co, narg);
		  if (status == LUA_OK || status == LUA_YIELD) {
			int nres = lua_gettop(co);
			if (lua_checkstack(L, nres)==0)
			  luaL_error(L, "too many results to resume");
			lua_xmove(co, L, nres);  /* move yielded values */
			return nres;
		  }
		  else {
			lua_xmove(co, L, 1);  /* move error message */
			return -1;  /* error flag */
		  }
		}


		private static int luaB_coresume (lua_State L) {
		  lua_State co = lua_tothread(L, 1);
		  int r;
		  luaL_argcheck(L, co!=null, 1, "coroutine expected");
		  r = auxresume(L, co, lua_gettop(L) - 1);
		  if (r < 0) {
			lua_pushboolean(L, 0);
			lua_insert(L, -2);
			return 2;  /* return false + error message */
		  }
		  else {
			lua_pushboolean(L, 1);
			lua_insert(L, -(r + 1));
			return r + 1;  /* return true + `resume' returns */
		  }
		}


		private static int luaB_auxwrap (lua_State L) {
		  lua_State co = lua_tothread(L, lua_upvalueindex(1));
		  int r = auxresume(L, co, lua_gettop(L));
		  if (r < 0) {
			if (lua_isstring(L, -1) != 0) {  /* error object is a string? */
			  luaL_where(L, 1);  /* add extra info */
			  lua_insert(L, -2);
			  lua_concat(L, 2);
			}
			lua_error(L);  /* propagate error */
		  }
		  return r;
		}


		private static int luaB_cocreate (lua_State L) {
		  lua_State NL = lua_newthread(L);
		  luaL_argcheck(L, lua_isfunction(L, 1) && !lua_iscfunction(L, 1), 1,
			"Lua function expected");
		  lua_pushvalue(L, 1);  /* move function to top */
		  lua_xmove(L, NL, 1);  /* move function from L to NL */
		  return 1;
		}


		private static int luaB_cowrap (lua_State L) {
		  luaB_cocreate(L);
		  lua_pushcclosure(L, luaB_auxwrap, 1);
		  return 1;
		}


		private static int luaB_yield (lua_State L) {
		  return lua_yield(L, lua_gettop(L));
        }
		private static int luaB_costatus (lua_State L) {
		  lua_State co = lua_tothread(L, 1);
		  luaL_argcheck(L, co != null, 1, "coroutine expected");
		  if (L == co) lua_pushliteral(L, "running");
		  else {
		    switch (lua_status(co)) {
		      case LUA_YIELD:
		        lua_pushliteral(L, "suspended");
		        break;
		      case LUA_OK: {
		        lua_Debug ar = new lua_Debug();
		        if (lua_getstack(co, 0, ar) > 0)  /* does it have frames? */
		          lua_pushliteral(L, "normal");  /* it is running */
		        else if (lua_gettop(co) == 0)
		            lua_pushliteral(L, "dead");
		        else
		          lua_pushliteral(L, "suspended");  /* initial state */
		        break;
		      }
		      default:  /* some error occured */
		        lua_pushliteral(L, "dead");
		        break;
		    }
		  }
		  return 1;
		}


		private static int luaB_corunning (lua_State L) {
		  if (lua_pushthread(L) != 0)
			lua_pushnil(L);  /* main thread is not a coroutine */
		  return 1;
		}


		private readonly static luaL_Reg[] co_funcs = {
		  new luaL_Reg("create", luaB_cocreate),
		  new luaL_Reg("resume", luaB_coresume),
		  new luaL_Reg("running", luaB_corunning),
		  new luaL_Reg("status", luaB_costatus),
		  new luaL_Reg("wrap", luaB_cowrap),
		  new luaL_Reg("yield", luaB_yield),
		  new luaL_Reg(null, null)
		};

		/* }====================================================== */


		private static void auxopen (lua_State L, CharPtr name,
							 lua_CFunction f, lua_CFunction u) {
		  lua_pushcfunction(L, u);
		  lua_pushcclosure(L, f, 1);
		  lua_setfield(L, -2, name);
		}


		private static void base_open (lua_State L) {
		  /* set global _G */
		  lua_pushvalue(L, LUA_GLOBALSINDEX);
		  lua_setglobal(L, "_G");
		  /* open lib into global table */
		  luaL_register(L, "_G", base_funcs);
		  lua_pushliteral(L, LUA_VERSION);
		  lua_setglobal(L, "_VERSION");  /* set global _VERSION */
		  /* `ipairs' and `pairs' need auxiliary functions as upvalues */
		  auxopen(L, "ipairs", luaB_ipairs, ipairsaux);
		  auxopen(L, "pairs", luaB_pairs, luaB_next);
		  /* `newproxy' needs a weaktable as upvalue */
		  lua_createtable(L, 0, 1);  /* new table `w' */
		  lua_pushvalue(L, -1);  /* `w' will be its own metatable */
		  lua_setmetatable(L, -2);
		  lua_pushliteral(L, "kv");
		  lua_setfield(L, -2, "__mode");  /* metatable(w).__mode = "kv" */
		  lua_pushcclosure(L, luaB_newproxy, 1);
		  lua_setglobal(L, "newproxy");  /* set global `newproxy' */
		}


		public static int luaopen_base (lua_State L) {
		  base_open(L);
		  luaL_register(L, LUA_COLIBNAME, co_funcs);
		  return 2;
		}

	}
}

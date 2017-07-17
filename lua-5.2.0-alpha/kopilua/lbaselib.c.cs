/*
** $Id: lbaselib.c,v 1.250 2010/09/07 19:38:36 roberto Exp roberto $
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

		private static int luaB_print (lua_State L) {
		  int n = lua_gettop(L);  /* number of arguments */
		  int i;
		  lua_getglobal(L, "tostring");
		  for (i=1; i<=n; i++) {
			CharPtr s;
            uint l;
			lua_pushvalue(L, -1);  /* function to be called */
			lua_pushvalue(L, i);   /* value to print */
			lua_call(L, 1, 1);
			s = lua_tolstring(L, -1, out l);  /* get result */
			if (s == null)
			  return luaL_error(L, LUA_QL("tostring") + " must return a string to " +
								   LUA_QL("print"));
			if (i > 1) luai_writestring("\t", 1);
			luai_writestring(s, l);
			lua_pop(L, 1);  /* pop result */
		  }
		  luai_writestring("\n", 1);
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
            int neg = 0;
			luaL_argcheck(L, 2 <= base_ && base_ <= 36, 2, "base out of range");
			while (isspace((int)(byte)(s1[0]))) /*s1++*/s1=s1+1;  /* skip initial spaces */ //FIXME:added, (int)
			if (s1[0] == '-') { /*s1++*/s1 = s1 + 1; neg = 1; } //FIXME:changed, ++
			n = strtoul(s1, out s2, base_);
			if (s1 != s2) {  /* at least one valid digit? */
			  while (isspace((byte)(s2[0]))) s2 = s2.next();  /* skip trailing spaces */
			  if (s2[0] == '\0') {  /* no invalid trailing characters? */
				lua_pushnumber(L, (neg!=0) ? -(lua_Number)n : (lua_Number)n);
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
			return luaL_error(L, "cannot change a protected metatable");
		  lua_settop(L, 2);
		  lua_setmetatable(L, 1);
		  return 1;
		}


		private static int luaB_getfenv (lua_State L) {
		  return luaL_error(L, "getfenv/setfenv deprecated");
		}

		private static int luaB_setfenv (lua_State L) {return luaB_getfenv(L); }


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




		public static readonly CharPtr[] opts = {"stop", "restart", "collect",
			"count", "step", "setpause", "setstepmul",
            "setmajorinc", "isrunning", "gen", "inc", null};
		public readonly static int[] optsnum = {LUA_GCSTOP, LUA_GCRESTART, LUA_GCCOLLECT,
			LUA_GCCOUNT, LUA_GCSTEP, LUA_GCSETPAUSE, LUA_GCSETSTEPMUL,
		    LUA_GCSETMAJORINC, LUA_GCISRUNNING, LUA_GCGEN, LUA_GCINC};
		private static int luaB_collectgarbage (lua_State L) {		  
		  int o = optsnum[luaL_checkoption(L, 1, "collect", opts)];
		  int ex = luaL_optint(L, 2, 0);
		  int res = lua_gc(L, o, ex);
		  switch (o) {
			case LUA_GCCOUNT: {
			  int b = lua_gc(L, LUA_GCCOUNTB, 0);
			  lua_pushnumber(L, res + ((lua_Number)b/1024));
              lua_pushinteger(L, b);
			  return 2;
			}
			case LUA_GCSTEP: case LUA_GCISRUNNING: {
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


		private static int pairsmeta (lua_State L, CharPtr method, int iszero,
		                              lua_CFunction iter) {
		  if (luaL_getmetafield(L, 1, method) == 0) {  /* no metamethod? */
		    luaL_checktype(L, 1, LUA_TTABLE);  /* argument must be a table */
		    lua_pushcfunction(L, iter);  /* will return generator, */
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
		  return pairsmeta(L, "__pairs", 0, luaB_next);
		}


		private static int ipairsaux (lua_State L) {
		  int i = luaL_checkint(L, 2);
		  luaL_checktype(L, 1, LUA_TTABLE);
		  i++;  /* next value */
		  lua_pushinteger(L, i);
		  lua_rawgeti(L, 1, i);
		  return (lua_isnil(L, -1)) ? 1 : 2;
		}


		private static int luaB_ipairs (lua_State L) {
		  return pairsmeta(L, "__ipairs", 1, ipairsaux);
		}


		private static int load_aux (lua_State L, int status) {
		  if (status == LUA_OK)
			return 1;
		  else {
			lua_pushnil(L);
			lua_insert(L, -2);  /* put before error message */
			return 2;  /* return nil plus error message */
		  }
		}


		private static int luaB_loadfile (lua_State L) {
		  CharPtr fname = luaL_optstring(L, 1, null);
		  return load_aux(L, luaL_loadfile(L, fname));
		}


		/*
		** {======================================================
		** Generic Read function
		** =======================================================
		*/

		private static CharPtr checkrights (lua_State L, CharPtr mode, CharPtr s) {
		  if (strchr(mode, 'b') == null && s[0] == LUA_SIGNATURE[0])
		    return lua_pushstring(L, "attempt to load a binary chunk");
		  if (strchr(mode, 't') == null && s[0] != LUA_SIGNATURE[0])
		    return lua_pushstring(L, "attempt to load a text chunk");
		  return null;  /* chunk in allowed format */
		}


		/*
		** reserves a slot, above all arguments, to hold a copy of the returned
		** string to avoid it being collected while parsed
		*/
		private const int RESERVEDSLOT = 4;


		/*
		** Reader for generic `load' function: `lua_load' uses the
		** stack for internal stuff, so the reader cannot change the
		** stack top. Instead, it keeps its resulting string in a
		** reserved slot inside the stack.
		*/
		private class Readstat {  /* reader state */
		  public int f;  /* position of reader function on stack */
		  public CharPtr mode;  /* allowed modes (binary/text) */
		};

		private static CharPtr generic_reader (lua_State L, object ud, out uint size) {
		  CharPtr s;
		  Readstat stat = (Readstat)ud;
		  luaL_checkstack(L, 2, "too many nested functions");
		  lua_pushvalue(L, stat.f);  /* get function */
		  lua_call(L, 0, 1);  /* call it */
		  if (lua_isnil(L, -1)) {
			size = 0;
			return null;
		  }
		  else if ((s = lua_tostring(L, -1)) != null) {
		  	if (stat.mode != null) {  /* first time? */
		  	  s = checkrights(L, stat.mode, s);  /* check mode */
		  	  stat.mode = null;  /* to avoid further checks */
		      if (s != null) luaL_error(L, s);
		  	}
			lua_replace(L, RESERVEDSLOT);  /* save string in reserved slot */
			return lua_tolstring(L, RESERVEDSLOT, out size);
		  }
		  else {
			  luaL_error(L, "reader function must return a string");
			  size = 0; //FIXME:added
		      return null;  /* to avoid warnings */
		  }
		}


		private static int luaB_load_aux (lua_State L, int farg) {
		  int status;
		  Readstat stat = new Readstat();
		  uint l;
		  CharPtr s = lua_tolstring(L, farg, out l);
		  CharPtr mode = luaL_optstring(L, farg + 2, "bt");
		  if (s != null) {  /* loading a string? */
		    CharPtr chunkname = luaL_optstring(L, farg + 1, s);
		    status = ((checkrights(L, mode, s) != null)
		              || luaL_loadbuffer(L, s, l, chunkname) != 0) ? 1 : 0;
		  }
		  else {  /* loading from a reader function */
			  CharPtr chunkname = luaL_optstring(L, farg + 1, "=(load)");
			  luaL_checktype(L, farg, LUA_TFUNCTION);
              stat.f = farg;
			  lua_settop(L, RESERVEDSLOT);  /* create reserved slot */
			  status = lua_load(L, generic_reader, stat, chunkname); //FIXME:???
		  }
		  return load_aux(L, status);
		}


		private static int luaB_load (lua_State L) {
		  return luaB_load_aux(L, 1);
		}


		private static int luaB_loadin (lua_State L) {
		  int n;
		  luaL_checkany(L, 1);
		  n = luaB_load_aux(L, 2);
		  if (n == 1) {  /* success? */
		    lua_pushvalue(L, 1);  /* environment for loaded function */
		    if (lua_setupvalue(L, -2, 1) == null)
      		  luaL_error(L, "loaded chunk does not have an upvalue");
		  }
		  return n;
		}



		private static int luaB_loadstring (lua_State L) {
		  lua_settop(L, 2);
		  lua_pushliteral(L, "tb");
		  return luaB_load(L);  /* dostring(s, n) == load(s, n, "tb") */

		}
		/* }====================================================== */


		private static int dofilecont (lua_State L) {
		  return lua_gettop(L) - 1;
		}


		private static int luaB_dofile (lua_State L) {
		  CharPtr fname = luaL_optstring(L, 1, null);
		  lua_settop(L, 1);
		  if (luaL_loadfile(L, fname) != LUA_OK) lua_error(L);
		  lua_callk(L, 0, LUA_MULTRET, 0, dofilecont);
		  return dofilecont(L);
		}


		private static int luaB_assert (lua_State L) {
		  if (lua_toboolean(L, 1)==0)
			return luaL_error(L, "%s", luaL_optstring(L, 2, "assertion failed!"));
		  return lua_gettop(L);
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


		private static int pcallcont (lua_State L) {
		  int errfunc = 0;  /* call has an error function in bottom of the stack */ //FIXME:???not init with 0
		  int status = lua_getctx(L, ref errfunc);
		  lua_assert(status != LUA_OK);
		  lua_pushboolean(L, (status == LUA_YIELD) ? 1 : 0);  /* first result (status) */
		  if (errfunc != 0)  /* came from xpcall? */
		    lua_replace(L, 1);  /* put first result in place of error function */
		  else  /* came from pcall */
		    lua_insert(L, 1);  /* open space for first result */
		  return lua_gettop(L);
		}


		private static int luaB_pcall (lua_State L) {
		  int status;
		  luaL_checkany(L, 1);
		  status = lua_pcallk(L, lua_gettop(L) - 1, LUA_MULTRET, 0, 0, pcallcont);
          luaL_checkstack(L, 1, null);
		  lua_pushboolean(L, (status == LUA_OK) ? 1 : 0);
		  lua_insert(L, 1);
		  return lua_gettop(L);  /* return status + all results */
		}


		private static int luaB_xpcall (lua_State L) {
		  int status;
		  int n = lua_gettop(L);
		  luaL_argcheck(L, n >= 2, 2, "value expected");
		  lua_pushvalue(L, 1);  /* exchange function... */
		  lua_copy(L, 2, 1);  /* ...and error handler */
		  lua_replace(L, 2);
		  status = lua_pcallk(L, n - 2, LUA_MULTRET, 1, 1, pcallcont);
          luaL_checkstack(L, 1, null);
		  lua_pushboolean(L, (status == LUA_OK) ? 1 : 0);
		  lua_replace(L, 1);
		  return lua_gettop(L);  /* return status + all results */
		}


		private static int luaB_tostring (lua_State L) {
		  luaL_checkany(L, 1);
		  uint nullVal = 0; //FIXME: added
		  luaL_tolstring(L, 1, out nullVal); //FIXME: ..., null)
		  return 1;
		}


		private static int luaB_newproxy (lua_State L) {
		  lua_settop(L, 1);
		  lua_newuserdata(L, 0);  /* create proxy */
		  if (lua_toboolean(L, 1) == 0)
			return 1;  /* no metatable */
		  else if (lua_isboolean(L, 1)) {
		    lua_createtable(L, 0, 1);  /* create a new metatable `m' ... */
		    lua_pushboolean(L, 1);
		    lua_setfield(L, -2, "__gc");  /* ... m.__gc = false (HACK!!)... */
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
		  new luaL_Reg("getfenv", luaB_getfenv),
		  new luaL_Reg("getmetatable", luaB_getmetatable),
          new luaL_Reg("ipairs", luaB_ipairs),
		  new luaL_Reg("loadfile", luaB_loadfile),
		  new luaL_Reg("load", luaB_load),
          new luaL_Reg("loadin", luaB_loadin),
		  new luaL_Reg("loadstring", luaB_loadstring),
		  new luaL_Reg("next", luaB_next),
          new luaL_Reg("pairs", luaB_pairs),
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
		  new luaL_Reg("xpcall", luaB_xpcall),
		  new luaL_Reg(null, null)
		};


		public static int luaopen_base (lua_State L) {
		  /* set global _G */
		  lua_pushglobaltable(L);
          lua_pushglobaltable(L);
          lua_setfield(L, -2, "_G");
		  /* open lib into global table */
		  luaL_setfuncs(L, base_funcs, 0);
		  lua_pushliteral(L, LUA_VERSION);
		  lua_setfield(L, -2, "_VERSION");  /* set global _VERSION */
		  /* `newproxy' needs a weaktable as upvalue */
		  lua_createtable(L, 0, 1);  /* new table `w' */
		  lua_pushvalue(L, -1);  /* `w' will be its own metatable */
		  lua_setmetatable(L, -2);
		  lua_pushliteral(L, "kv");
		  lua_setfield(L, -2, "__mode");  /* metatable(w).__mode = "kv" */
		  lua_pushcclosure(L, luaB_newproxy, 1);
		  lua_setfield(L, -2, "newproxy");  /* set global `newproxy' */
		  return 1;
		}

	}
}

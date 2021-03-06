/*
** $Id: lbaselib.c,v 1.274 2012/04/27 14:13:19 roberto Exp $
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
			  return luaL_error(L, 
			    LUA_QL("tostring") + " must return a string to " + LUA_QL("print"));
			if (i > 1) luai_writestring("\t", 1);
			luai_writestring(s, l);
			lua_pop(L, 1);  /* pop result */
		  }
		  luai_writeline();
		  return 0;
		}


		private const string SPACECHARS = " \f\n\r\t\v";

		private static int luaB_tonumber (lua_State L) {
		  if (lua_isnoneornil(L, 2)) {  /* standard conversion */
		    int isnum=0; //FIXME:changed, =0
		    lua_Number n = lua_tonumberx(L, 1, ref isnum);
		    if (isnum!=0) {
		      lua_pushnumber(L, n);
		      return 1;
		    }  /* else not a number; must be something */
		    luaL_checkany(L, 1);
		  }
		  else {
		    uint l;
		    CharPtr s = luaL_checklstring(L, 1, out l);
		    CharPtr e = s + l;  /* end point for 's' */
            int base_ = luaL_checkint(L, 2);
		    int neg = 0;
            luaL_argcheck(L, 2 <= base_ && base_ <= 36, 2, "base out of range");
		    s += strspn(s, SPACECHARS);  /* skip initial spaces */
		    if (s[0] == '-') { s.inc(); neg = 1; }  /* handle signal */ //FIXME:changed, s++;
		    else if (s[0] == '+') s.inc();//FIXME:changed, s++;
		    if (isalnum((byte)s[0])) {
		      lua_Number n = 0;
		      do {
		      	int digit = (isdigit((byte)s[0])) ? s[0] - '0'
		      				: toupper((byte)s[0]) - 'A' + 10;
		        if (digit >= base_) break;  /* invalid numeral; force a fail */
		        n = n * (lua_Number)base_ + (lua_Number)digit;
		        s.inc(); //FIXME:changed, s++;
		      } while (isalnum((byte)s[0]));
		      s += strspn(s, SPACECHARS);  /* skip trailing spaces */
		      if (s == e) {  /* no invalid trailing characters? */
		        lua_pushnumber(L, (neg!=0) ? -n : n);
		        return 1;
		      }  /* else not a number */
		    }  /* else not a number */
		  }
		  lua_pushnil(L);  /* not a number */
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


		private static int luaB_rawequal (lua_State L) {
		  luaL_checkany(L, 1);
		  luaL_checkany(L, 2);
		  lua_pushboolean(L, lua_rawequal(L, 1, 2));
		  return 1;
		}


		private static int luaB_rawlen (lua_State L) {
		  int t = lua_type(L, 1);
		  luaL_argcheck(L, t == LUA_TTABLE || t == LUA_TSTRING, 1,
		                   "table or string expected");
		  lua_pushinteger(L, (int)lua_rawlen(L, (int)1)); //FIXME:added, (int), (int)
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
            "setmajorinc", "isrunning", "generational", "incremental", null};
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
		  CharPtr mode = luaL_optstring(L, 2, null);
		  int env = lua_isnone(L, 3)?0:1;  /* 'env' parameter? */
		  int status = luaL_loadfilex(L, fname, mode);
		  if (status == LUA_OK && env!=0) {  /* 'env' parameter? */
		    lua_pushvalue(L, 3);
		    lua_setupvalue(L, -2, 1);  /* set it as 1st upvalue of loaded chunk */
		  }
		  return load_aux(L, status);
		}


		/*
		** {======================================================
		** Generic Read function
		** =======================================================
		*/


		/*
		** reserved slot, above all arguments, to hold a copy of the returned
		** string to avoid it being collected while parsed. 'load' has four
		** optional arguments (chunk, source name, mode, and environment).
		*/
		private const int RESERVEDSLOT = 5;


		/*
		** Reader for generic `load' function: `lua_load' uses the
		** stack for internal stuff, so the reader cannot change the
		** stack top. Instead, it keeps its resulting string in a
		** reserved slot inside the stack.
		*/
		private static CharPtr generic_reader (lua_State L, object ud, out uint size) {
		  //(void)(ud);  /* not used */
		  luaL_checkstack(L, 2, "too many nested functions");
		  lua_pushvalue(L, 1);  /* get function */
		  lua_call(L, 0, 1);  /* call it */
		  if (lua_isnil(L, -1)) {
			lua_pop(L, 1);  /* pop result */		  
			size = 0;
			return null;
		  }
		  else if (lua_isstring(L, -1)==0)
    		luaL_error(L, "reader function must return a string");
		  lua_replace(L, RESERVEDSLOT);  /* save string in reserved slot */
		  return lua_tolstring(L, RESERVEDSLOT, out size);
		}


		private static int luaB_load (lua_State L) {
		  int status;
		  uint l;
		  int top = lua_gettop(L);
		  CharPtr s = lua_tolstring(L, 1, out l);
		  CharPtr mode = luaL_optstring(L, 3, "bt");
		  if (s != null) {  /* loading a string? */
		    CharPtr chunkname = luaL_optstring(L, 2, s);
			status = luaL_loadbufferx(L, s, l, chunkname, mode);
		  }
		  else {  /* loading from a reader function */
		    CharPtr chunkname = luaL_optstring(L, 2, "=(load)");
			luaL_checktype(L, 1, LUA_TFUNCTION);
			lua_settop(L, RESERVEDSLOT);  /* create reserved slot */
			status = lua_load(L, generic_reader, null, chunkname, mode);
		  }
		  if (status == LUA_OK && top >= 4) {  /* is there an 'env' argument */
		    lua_pushvalue(L, 4);  /* environment for loaded function */
			lua_setupvalue(L, -2, 1);  /* set it as 1st upvalue */
	      }
		  return load_aux(L, status);
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


		private static int finishpcall (lua_State L, int status) {
		  if (lua_checkstack(L, 1)==0) {  /* no space for extra boolean? */
		    lua_settop(L, 0);  /* create space for return values */
		    lua_pushboolean(L, 0);
		    lua_pushstring(L, "stack overflow");
		    return 2;  /* return false, msg */
		  }
		  lua_pushboolean(L, status);  /* first result (status) */
		  lua_replace(L, 1);  /* put first result in first slot */
		  return lua_gettop(L);
		}


		private static int pcallcont (lua_State L) {
		  int null_ = 0; //FIXME:added
		  int status = lua_getctx(L, ref null_);
		  return finishpcall(L, (status == LUA_YIELD)?1:0);
		}


		private static int luaB_pcall (lua_State L) {
		  int status;
		  luaL_checkany(L, 1);
		  lua_pushnil(L);
		  lua_insert(L, 1);  /* create space for status result */
		  status = lua_pcallk(L, lua_gettop(L) - 2, LUA_MULTRET, 0, 0, pcallcont);
		  return finishpcall(L, (status == LUA_OK)?1:0);
		}


		private static int luaB_xpcall (lua_State L) {
		  int status;
		  int n = lua_gettop(L);
		  luaL_argcheck(L, n >= 2, 2, "value expected");
		  lua_pushvalue(L, 1);  /* exchange function... */
		  lua_copy(L, 2, 1);  /* ...and error handler */
		  lua_replace(L, 2);
		  status = lua_pcallk(L, n - 2, LUA_MULTRET, 1, 0, pcallcont);
		  return finishpcall(L, (status == LUA_OK)?1:0);
		}


		private static int luaB_tostring (lua_State L) {
		  luaL_checkany(L, 1);
		  uint nullVal = 0; //FIXME: added
		  luaL_tolstring(L, 1, out nullVal); //FIXME: ..., null)
		  return 1;
		}


		private readonly static luaL_Reg[] base_funcs = {
		  new luaL_Reg("assert", luaB_assert),
		  new luaL_Reg("collectgarbage", luaB_collectgarbage),
		  new luaL_Reg("dofile", luaB_dofile),
		  new luaL_Reg("error", luaB_error),
		  new luaL_Reg("getmetatable", luaB_getmetatable),
          new luaL_Reg("ipairs", luaB_ipairs),
		  new luaL_Reg("loadfile", luaB_loadfile),
		  new luaL_Reg("load", luaB_load),
//#if defined(LUA_COMPAT_LOADSTRING)
		  new luaL_Reg("loadstring", luaB_load),
//#endif
		  new luaL_Reg("next", luaB_next),
          new luaL_Reg("pairs", luaB_pairs),
		  new luaL_Reg("pcall", luaB_pcall),
		  new luaL_Reg("print", luaB_print),
		  new luaL_Reg("rawequal", luaB_rawequal),
          new luaL_Reg("rawlen", luaB_rawlen),
		  new luaL_Reg("rawget", luaB_rawget),
		  new luaL_Reg("rawset", luaB_rawset),
		  new luaL_Reg("select", luaB_select),
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
		  return 1;
		}

	}
}

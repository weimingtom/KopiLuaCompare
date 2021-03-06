/*
** $Id: lcorolib.c,v 1.7 2014/09/01 18:00:04 roberto Exp $
** Coroutine Library
** See Copyright Notice in lua.h
*/


namespace KopiLua
{
	public partial class Lua
	{
		private static lua_State getco (lua_State L) {
		  lua_State co = lua_tothread(L, 1);
		  luaL_argcheck(L, co!=null, 1, "thread expected");
		  return co;
		}
		
		
		
		private static int auxresume (lua_State L, lua_State co, int narg) {
		  int status;
		  if (lua_checkstack(co, narg)==0) {
		    lua_pushliteral(L, "too many arguments to resume");
		    return -1;  /* error flag */
		  }
		  if (lua_status(co) == LUA_OK && lua_gettop(co) == 0) {
		    lua_pushliteral(L, "cannot resume dead coroutine");
		    return -1;  /* error flag */
		  }
		  lua_xmove(L, co, narg);
		  status = lua_resume(co, L, narg);
		  if (status == LUA_OK || status == LUA_YIELD) {
		    int nres = lua_gettop(co);
		    if (lua_checkstack(L, nres + 1)==0) {
		      lua_pop(co, nres);  /* remove results anyway */
		      lua_pushliteral(L, "too many results to resume");
		      return -1;  /* error flag */
		    }
		    lua_xmove(co, L, nres);  /* move yielded values */
		    return nres;
		  }
		  else {
		    lua_xmove(co, L, 1);  /* move error message */
		    return -1;  /* error flag */
		  }
		}
		
		
		private static int luaB_coresume (lua_State L) {
		  lua_State co = getco(L);
		  int r;
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
		    if (lua_isstring(L, -1)!=0) {  /* error object is a string? */
		      luaL_where(L, 1);  /* add extra info */
		      lua_insert(L, -2);
		      lua_concat(L, 2);
		    }
		    return lua_error(L);  /* propagate error */
		  }
		  return r;
		}
		
		
		private static int luaB_cocreate (lua_State L) {
		  lua_State NL;
		  luaL_checktype(L, 1, LUA_TFUNCTION);
		  NL = lua_newthread(L);
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
		  lua_State co = getco(L);
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
		      default:  /* some error occurred */
		        lua_pushliteral(L, "dead");
		        break;
		    }
		  }
		  return 1;
		}
		
		
		private static int luaB_yieldable (lua_State L) {
		  lua_pushboolean(L, lua_isyieldable(L));
		  return 1;
		}		
		
		
		private static int luaB_corunning (lua_State L) {
		  int ismain = lua_pushthread(L);
		  lua_pushboolean(L, ismain);
		  return 2;
		}
		
		
		private static readonly luaL_Reg[] co_funcs = {
		  new luaL_Reg("create", luaB_cocreate),
		  new luaL_Reg("resume", luaB_coresume),
		  new luaL_Reg("running", luaB_corunning),
		  new luaL_Reg("status", luaB_costatus),
		  new luaL_Reg("wrap", luaB_cowrap),
		  new luaL_Reg("yield", luaB_yield),
		  new luaL_Reg("isyieldable", luaB_yieldable),
		  new luaL_Reg(null, null)
		};
		
		
		
		public static int luaopen_coroutine (lua_State L) {
		  luaL_newlib(L, co_funcs);
		  return 1;
		}
	}
}


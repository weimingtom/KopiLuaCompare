/*
** $Id: lapi.h,v 2.9 2015/03/06 19:49:50 roberto Exp $
** Auxiliary functions from Lua API
** See Copyright Notice in lua.h
*/
namespace KopiLua
{

	public partial class Lua
	{

		public static void api_incr_top(lua_State L)   {L.top++; api_check(L, L.top <= L.ci.top, "stack overflow");}

		public static void adjustresults(lua_State L, int nres)
    	{ if ((nres) == LUA_MULTRET && L.ci.top < L.top) L.ci.top = L.top; }

		public static void api_checknelems(lua_State L, int n)	{ api_check(L, (n) < (L.top - L.ci.func),
			"not enough elements in the stack"); }

	}
}

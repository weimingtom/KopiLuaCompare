/*
** $Id: lapi.h,v 2.8 2014/07/15 21:26:50 roberto Exp $
** Auxiliary functions from Lua API
** See Copyright Notice in lua.h
*/

namespace KopiLua
{
	using StkId = Lua.lua_TValue;
	
	public partial class Lua
	{
		private static void api_incr_top(lua_State L)  { lua_TValue.inc(ref L.top, 1); api_check(L.top <= L.ci.top, 
		      "stack overflow");}

		private static void adjustresults(lua_State L, int nres) 
			{ if (nres == LUA_MULTRET && L.ci.top < L.top) L.ci.top = L.top; }

		private static void api_checknelems(lua_State L, int n) { api_check((n) < (L.top - L.ci.func),
						  "not enough elements in the stack"); }
	
	}
}
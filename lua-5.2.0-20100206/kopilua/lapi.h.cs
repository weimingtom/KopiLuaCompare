namespace KopiLua
{
	using StkId = Lua.lua_TValue;
	
	public partial class Lua
	{
		private static void api_incr_top(lua_State L)   
		{
			StkId.inc(ref L.top);
			api_check(L, L.top <= L.ci.top);
		}

		private static void adjustresults(lua_State L, int nres)
    	{ 
			if (nres == LUA_MULTRET && L.ci.top < L.top) 
				L.ci.top = L.top; 
		}
	
	}
}
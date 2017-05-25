namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	
	public partial class Lua
	{
		public static int tostring(lua_State L, StkId o) {
			return ((ttype(o) == LUA_TSTRING) || (luaV_tostring(L, o) != 0)) ? 1 : 0;
		}

		public static int tonumber(ref StkId o, TValue n) {
			return ((ttype(o) == LUA_TNUMBER || (((o) = luaV_tonumber(o, n)) != null))) ? 1 : 0;
		}

		public static int equalobj(lua_State L, TValue o1, TValue o2) {
			return ((ttype(o1) == ttype(o2)) && (luaV_equalval_(L, o1, o2) != 0)) ? 1 : 0;
		}
	}
}

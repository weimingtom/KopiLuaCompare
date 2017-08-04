namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	
	public partial class Lua
	{
		public static int tostring(lua_State L, StkId o) { return (ttisstring(o) || (luaV_tostring(L, o) != 0)) ? 1 : 0; }

		public static int tonumber(ref StkId o, TValue n) { return (ttisnumber(o) || ((o = luaV_tonumber(o, n)) != null)) ? 1 : 0; }

		public static bool equalobj(L,o1,o2)  { return (ttisequal(o1, o2) && luaV_equalobj_(L, o1, o2)); }

		public static int luaV_rawequalobj(t1, t2)
			     { return (ttisequal(t1,t2) && luaV_equalobj_(null,t1,t2)) ? 1 : 0; }
	}
}

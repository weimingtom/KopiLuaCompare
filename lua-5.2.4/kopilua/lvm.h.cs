/*
** $Id: lvm.h,v 2.18.1.1 2013/04/12 18:48:47 roberto Exp $
** Lua virtual machine
** See Copyright Notice in lua.h
*/

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	
	public partial class Lua
	{
		public static int tostring(lua_State L, StkId o) { return (ttisstring(o) || (luaV_tostring(L, o) != 0)) ? 1 : 0; }

		public static int tonumber(ref StkId o, TValue n) { return (ttisnumber(o) || ((o = luaV_tonumber(o, n)) != null)) ? 1 : 0; }

		public static bool equalobj(lua_State L, TValue o1, TValue o2)  { return (ttisequal(o1, o2) && luaV_equalobj_(L, o1, o2) != 0); }

		public static int luaV_rawequalobj(TValue t1, TValue t2)
			     { return (ttisequal(t1,t2) && luaV_equalobj_(null,t1,t2) != 0) ? 1 : 0; }
	}
}

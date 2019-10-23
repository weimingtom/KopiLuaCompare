/*
** $Id: lvm.h,v 2.23 2013/05/02 12:31:26 roberto Exp $
** Lua virtual machine
** See Copyright Notice in lua.h
*/

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using lua_Integer = System.Int32;
	using lua_Number = System.Double;
	
	public partial class Lua
	{
		public static int tostring(lua_State L, StkId o) { return (ttisstring(o) || (luaV_tostring(L, o) != 0)) ? 1 : 0; }

		public static int tonumber(ref StkId o, ref lua_Number n) 
			{ if (ttisfloat(o)) { n = fltvalue(o); return 1; } else { return luaV_tonumber_(o, ref n);} }

		public static int tointeger(ref StkId o, ref lua_Integer i)
			{ if (ttisinteger(o)) { i = ivalue(o); return 1; } else { return luaV_tointeger_(o, ref i); } }

		//FIXME:changed, see intop
		//FIXME:???Lua_Number
		public static int intop_plus(lua_Integer v1, lua_Integer v2) 
			{ return (int)((uint)(v1) + (uint)(v2));}
		public static int intop_minus(lua_Integer v1, lua_Integer v2) 
			{ return (int)((uint)(v1) - (uint)(v2));}
		public static int intop_mul(lua_Integer v1, lua_Integer v2) 
			{ return (int)((uint)(v1) * (uint)(v2));}

		public static int luaV_rawequalobj(TValue t1,TValue t2) { return luaV_equalobj(null,t1,t2); }
	}
}

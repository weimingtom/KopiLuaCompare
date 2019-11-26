/*
** $Id: lvm.h,v 2.31 2014/05/26 17:10:22 roberto Exp $
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
		public static int tonumber(ref StkId o, ref lua_Number n) 
			{ if (ttisfloat(o)) { n = fltvalue(o); return 1; } else { return luaV_tonumber_(o, ref n);} }

		public static int tointeger(ref StkId o, ref lua_Integer i)
			{ if (ttisinteger(o)) { i = ivalue(o); return 1; } else { return luaV_tointeger_(o, ref i); } }

		//FIXME:changed, see intop
		//FIXME:???Lua_Number
		public static int intop_plus(lua_Integer v1, lua_Integer v2) { return l_castU2S(l_castS2U(v1) + l_castS2U(v2));}
		public static int intop_minus(lua_Integer v1, lua_Integer v2) { return l_castU2S(l_castS2U(v1) - l_castS2U(v2));}
		public static int intop_mul(lua_Integer v1, lua_Integer v2) { return l_castU2S(l_castS2U(v1) * l_castS2U(v2));}
		public static int intop_xor(lua_Integer v1, lua_Integer v2) { return l_castU2S(l_castS2U(v1) ^ l_castS2U(v2));}
		public static int intop_or(lua_Integer v1, lua_Integer v2) { return l_castU2S(l_castS2U(v1) | l_castS2U(v2));}
		public static int intop_and(lua_Integer v1, lua_Integer v2) { return l_castU2S(l_castS2U(v1) & l_castS2U(v2));}
		public static int intop_shiftleft(lua_Integer v1, lua_Integer v2) { return l_castU2S((int)l_castS2U(v1) << (int)l_castS2U(v2));} //FIXME:???(int)
		public static int intop_shiftright(lua_Integer v1, lua_Integer v2) { return l_castU2S((int)l_castS2U(v1) >> (int)l_castS2U(v2));} //FIXME:???(int)
		
		public static int luaV_rawequalobj(TValue t1,TValue t2) { return luaV_equalobj(null,t1,t2); }
	}
}

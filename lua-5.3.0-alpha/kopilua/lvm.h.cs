/*
** $Id: lvm.h,v 2.33 2014/07/30 14:42:44 roberto Exp $
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
		//#if !defined(LUA_NOCVTN2S)
		public static int cvt2str(TValue o)	{ return ttisnumber(o)?1:0; }
		//#else
		//#define cvt2str(o)	0	/* no convertion from numbers to strings */
		//#endif


		//#if !defined(LUA_NOCVTS2N)
		public static int cvt2num(TValue o)	{ return ttisstring(o)?1:0; }
		//#else
		//#define cvt2num(o)	0	/* no convertion from strings to numbers */
		//#endif

	
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

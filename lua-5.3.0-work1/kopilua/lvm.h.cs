/*
** $Id: lvm.h,v 2.23 2013/05/02 12:31:26 roberto Exp $
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

		public static int tonumber(ref StkId o, TValue n) 
			{ return ttisfloat(o) ? (*(n) = fltvalue(o), 1) : luaV_tonumber_(o,n); }

		#define tointeger(o,i) \
			(ttisinteger(o) ? (*(i) = ivalue(o), 1) : luaV_tointeger_(o,i))

		#define intop(op,v1,v2) \
			cast_integer(cast_unsigned(v1) op cast_unsigned(v2))

		#define luaV_rawequalobj(t1,t2)		luaV_equalobj(NULL,t1,t2)
	}
}

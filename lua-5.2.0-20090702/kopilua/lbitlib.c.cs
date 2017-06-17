/*
** $Id: $
** Standard library for bitwise operations
** See Copyright Notice in lua.h
*/

namespace KopiLua
{
	using lua_Number = System.Double;
	using b_int = System.Int32;
	using b_uint = System.UInt32;
	using lua_Integer = System.Int32;
	
	public partial class Lua
	{
		
		
		/* number of bits considered when shifting/rotating (must be a power of 2) */
		private const int NBITS	= 32;
		
		
		//typedef LUA_INT32 b_int;
		//typedef unsigned LUA_INT32 b_uint;
		
		
		private static b_uint getuintarg (lua_State L, int arg) {
		  b_uint r;
		  lua_Number x = lua_tonumber(L, arg);
		  if (x == 0) luaL_checktype(L, arg, LUA_TNUMBER);
		  r = (uint)x;//lua_number2uint(r, x); //FIXME:
		  return r;
		}
		
		
		private static b_uint andaux (lua_State L) {
		  int i, n = lua_gettop(L);
		  b_uint r = ~(b_uint)0;
		  for (i = 1; i <= n; i++)
		    r &= getuintarg(L, i);
		  return r;
		}
		
		
		private static int b_and (lua_State L) {
		  b_uint r = andaux(L);
		  lua_pushnumber(L, lua_uint2number(r));
		  return 1;
		}
		
		
		private static int b_test (lua_State L) {
		  b_uint r = andaux(L);
		  lua_pushboolean(L, (r != 0) ? 1 : 0);
		  return 1;
		}
		
		
		private static int b_or (lua_State L) {
		  int i, n = lua_gettop(L);
		  b_uint r = 0;
		  for (i = 1; i <= n; i++)
		    r |= getuintarg(L, i);
		  lua_pushnumber(L, lua_uint2number(r));
		  return 1;
		}
		
		
		private static int b_xor (lua_State L) {
		  int i, n = lua_gettop(L);
		  b_uint r = 0;
		  for (i = 1; i <= n; i++)
		    r ^= getuintarg(L, i);
		  lua_pushnumber(L, lua_uint2number(r));
		  return 1;
		}
		
		
		private static int b_not (lua_State L) {
		  b_uint r = ~getuintarg(L, 1);
		  lua_pushnumber(L, lua_uint2number(r));
		  return 1;
		}
		
		
		private static int b_shift (lua_State L) {
		  b_uint r = getuintarg(L, 1);
		  lua_Integer i = luaL_checkinteger(L, 2);
		  if (i < 0) {  /* shift right? */
		    i = -i;
		    if (i >= NBITS) r = 0;
		    else r >>= i;
		  }
		  else {  /* shift left */
		    if (i >= NBITS) r = 0;
		    else r <<= i;
		  }
		  lua_pushnumber(L, lua_uint2number(r));
		  return 1;
		}
		
		
		private static int b_rotate (lua_State L) {
		  b_uint r = getuintarg(L, 1);
		  lua_Integer i = luaL_checkinteger(L, 2);
		  i &= (NBITS - 1);  /* i = i % NBITS */
		  r = (r << i) | (r >> (NBITS - i));
		  lua_pushnumber(L, lua_uint2number(r));
		  return 1;
		}
		
		
		private readonly static luaL_Reg[] bitlib = new luaL_Reg[] {
		  new luaL_Reg("band", b_and),
		  new luaL_Reg("btest", b_test),
		  new luaL_Reg("bor", b_or),
		  new luaL_Reg("bxor", b_xor),
		  new luaL_Reg("bnot", b_not),
		  new luaL_Reg("bshift", b_shift),
		  new luaL_Reg("brotate", b_rotate),
		  new luaL_Reg(null, null)
		};
		
		
		
		public static int luaopen_bit (lua_State L) {
		  luaL_register(L, LUA_BITLIBNAME, bitlib);
		  return 1;
		}
	}
}

/*
** $Id: lbitlib.c,v 1.12 2010/11/22 16:39:20 roberto Exp roberto $
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
		
		
		/* number of bits to consider in a number */
		private const int NBITS	= 32;
		
		private const int ALLONES = (~(((~(lua_Unsigned)0) << (NBITS - 1)) << 1));

		/* mask to trim extra bits */
		private static void trim(x)	 { return ((x) & ALLONES); }


		//typedef unsigned LUA_INT32 b_uint;
		
		
		private static b_uint getuintarg (lua_State L, int arg) { return luaL_checkunsigned(L,arg); }
		
		
		private static b_uint andaux (lua_State L) {
		  int i, n = lua_gettop(L);
		  b_uint r = ~(b_uint)0;
		  for (i = 1; i <= n; i++)
		    r &= getuintarg(L, i);
		  return trim(r);
		}
		
		
		private static int b_and (lua_State L) {
		  b_uint r = andaux(L);
		  lua_pushunsigned(L, r);
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
		  lua_pushunsigned(L, trim(r));
		  return 1;
		}
		
		
		private static int b_xor (lua_State L) {
		  int i, n = lua_gettop(L);
		  b_uint r = 0;
		  for (i = 1; i <= n; i++)
		    r ^= getuintarg(L, i);
		  lua_pushunsigned(L, trim(r));
		  return 1;
		}
		
		
		private static int b_not (lua_State L) {
		  b_uint r = ~getuintarg(L, 1);
		  lua_pushunsigned(L, trim(r));
		  return 1;
		}
		
		
		private static int b_shift (lua_State L) {
		  if (i < 0) {  /* shift right? */
		    i = -i;
            r = trim(r);
		    if (i >= NBITS) r = 0;
		    else r >>= i;
		  }
		  else {  /* shift left */
		    if (i >= NBITS) r = 0;
		    else r <<= i;
            r = trim(r);
		  }
		  lua_pushunsigned(L, r);
		  return 1;
		}
		
		
		private static int b_lshift (lua_State L) {
		  return b_shift(L, getuintarg(L, 1), luaL_checkint(L, 2));
		}


		private static int b_rshift (lua_State L) {
		  return b_shift(L, getuintarg(L, 1), -luaL_checkint(L, 2));
		}


		private static int b_arshift (lua_State L) {
		  b_uint r = getuintarg(L, 1);
		  int i = luaL_checkint(L, 2);
		  if (i < 0 || !(r & ((b_uint)1 << (NBITS - 1))))
		    return b_shift(L, r, -i);
		  else {  /* arithmetic shift for 'negative' number */
		    if (i >= NBITS) r = ALLONES;
		    else
		      r = trim((r >> i) | ~(~(b_uint)0 >> i));  /* add signal bit */
		    lua_pushunsigned(L, r);
		    return 1;
		  }
		}


		private static int b_rot (lua_State L, int i) {
		  b_uint r = getuintarg(L, 1);
		  i &= (NBITS - 1);  /* i = i % NBITS */
		  r = trim(r);
		  r = (r << i) | (r >> (NBITS - i));
		  lua_pushunsigned(L, trim(r));
		  return 1;
		}


		private static int b_lrot (lua_State L) {
		  return b_rot(L, luaL_checkint(L, 2));
		}


		private static int b_rrot (lua_State L) {
		  return b_rot(L, -luaL_checkint(L, 2));
		}
		
		
		private readonly static luaL_Reg[] bitlib = new luaL_Reg[] {
          new luaL_Reg("arshift", b_arshift),
		  new luaL_Reg("band", b_and),
		  new luaL_Reg("bnot", b_not),
		  new luaL_Reg("bor", b_or),
		  new luaL_Reg("bxor", b_xor),
		  new luaL_Reg("lrotate", b_lrot),
		  new luaL_Reg("lshift", b_lshift),
		  new luaL_Reg("rrotate", b_rrot),
		  new luaL_Reg("rshift", b_rshift),
		  new luaL_Reg("btest", b_test),
		  new luaL_Reg(null, null)
		};
		
		
		
		public static int luaopen_bit32 (lua_State L) {
		  luaL_newlib(L, bitlib);
		  return 1;
		}
	}
}

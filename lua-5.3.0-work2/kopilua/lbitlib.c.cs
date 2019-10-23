/*
** $Id: lbitlib.c,v 1.20 2013/06/21 17:27:24 roberto Exp $
** Standard library for bitwise operations
** See Copyright Notice in lua.h
*/

namespace KopiLua
{
	using lua_Number = System.Double;
	using b_int = System.Int32;
	using b_uint = System.UInt32;
	using lua_Integer = System.Int32;
	using lua_Unsigned = System.UInt32;
	
	public partial class Lua
	{
				
		/* number of bits to consider in a number */
		//#if !defined(LUA_NBITS)
		private const int LUA_NBITS	= 32;
		//#endif
		

		/* type with (at least) LUA_NBITS bits */
		//typedef unsigned long b_uint;


		private const lua_Unsigned ALLONES = (~(((~(b_uint)0) << (LUA_NBITS - 1)) << 1));

		/* macro to trim extra bits */
		private static lua_Unsigned trim(b_uint x)	 { return ((x) & ALLONES); }


		/* builds a number with 'n' ones (1 <= n <= LUA_NBITS) */
		private static int mask(int n) { return (int)(~((ALLONES << 1) << ((n) - 1))); } //FIXME:???//FIXME:(int)
		
		
				
		private static b_uint andaux (lua_State L) {
		  int i, n = lua_gettop(L);
		  b_uint r = ~(b_uint)0;
		  for (i = 1; i <= n; i++)
		    r &= luaL_checkunsigned(L, i);
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
		    r |= luaL_checkunsigned(L, i);
		  lua_pushunsigned(L, trim(r));
		  return 1;
		}
		
		
		private static int b_xor (lua_State L) {
		  int i, n = lua_gettop(L);
		  b_uint r = 0;
		  for (i = 1; i <= n; i++)
		    r ^= luaL_checkunsigned(L, i);
		  lua_pushunsigned(L, trim(r));
		  return 1;
		}
		
		
		private static int b_not (lua_State L) {
		  b_uint r = ~luaL_checkunsigned(L, 1);
		  lua_pushunsigned(L, trim(r));
		  return 1;
		}
		
		
		private static int b_shift (lua_State L, b_uint r, int i) {
		  if (i < 0) {  /* shift right? */
		    i = -i;
            r = trim(r);
		    if (i >= LUA_NBITS) r = 0;
		    else r >>= i;
		  }
		  else {  /* shift left */
		    if (i >= LUA_NBITS) r = 0;
		    else r <<= i;
            r = trim(r);
		  }
		  lua_pushunsigned(L, r);
		  return 1;
		}
		
		
		private static int b_lshift (lua_State L) {
		  return b_shift(L, luaL_checkunsigned(L, 1), luaL_checkint(L, 2));
		}


		private static int b_rshift (lua_State L) {
		  return b_shift(L, luaL_checkunsigned(L, 1), -luaL_checkint(L, 2));
		}


		private static int b_arshift (lua_State L) {
		  b_uint r = luaL_checkunsigned(L, 1);
		  int i = luaL_checkint(L, 2);
		  if (i < 0 || (r & ((b_uint)1 << (LUA_NBITS - 1)))==0)
		    return b_shift(L, r, -i);
		  else {  /* arithmetic shift for 'negative' number */
		    if (i >= LUA_NBITS) r = ALLONES;
		    else
		      r = trim((r >> i) | ~(trim(~(b_uint)0) >> i));  /* add signal bit */
		    lua_pushunsigned(L, r);
		    return 1;
		  }
		}


		private static int b_rot (lua_State L, int i) {
		  b_uint r = luaL_checkunsigned(L, 1);
		  i &= (LUA_NBITS - 1);  /* i = i % NBITS */
		  r = trim(r);
		  r = (r << i) | (r >> (LUA_NBITS - i));
		  lua_pushunsigned(L, trim(r));
		  return 1;
		}


		private static int b_lrot (lua_State L) {
		  return b_rot(L, luaL_checkint(L, 2));
		}


		private static int b_rrot (lua_State L) {
		  return b_rot(L, -luaL_checkint(L, 2));
		}
		
		
		/*
		** get field and width arguments for field-manipulation functions,
		** checking whether they are valid.
		** ('luaL_error' called without 'return' to avoid later warnings about
		** 'width' being used uninitialized.)
		*/
		private static int fieldargs (lua_State L, int farg, out int width) {
		  int f = luaL_checkint(L, farg);
		  int w = luaL_optint(L, farg + 1, 1);
		  luaL_argcheck(L, 0 <= f, farg, "field cannot be negative");
		  luaL_argcheck(L, 0 < w, farg + 1, "width must be positive");
		  if (f + w > LUA_NBITS)
		    luaL_error(L, "trying to access non-existent bits");
		  width = w;
		  return f;
		}


		private static int b_extract (lua_State L) {
		  int w;
		  b_uint r = trim(luaL_checkunsigned(L, 1));
		  int f = fieldargs(L, 2, out w);
		  r = (uint)((r >> f) & mask(w)); //FIXME:changed, (uint)
		  lua_pushunsigned(L, r);
		  return 1;
		}


		private static int b_replace (lua_State L) {
		  int w;
		  b_uint r = trim(luaL_checkunsigned(L, 1));
		  b_uint v = luaL_checkunsigned(L, 2);
		  int f = fieldargs(L, 3, out w);
		  int m = mask(w);
		  v &= (uint)m;  /* erase bits outside given width */ //FIXME:changed, (uint)
		  r = (uint)((r & ~(m << f)) | (v << f)); //FIXME:changed, (uint)
		  lua_pushunsigned(L, r);
		  return 1;
		}


		private readonly static luaL_Reg[] bitlib = new luaL_Reg[] {
          new luaL_Reg("arshift", b_arshift),
		  new luaL_Reg("band", b_and),
		  new luaL_Reg("bnot", b_not),
		  new luaL_Reg("bor", b_or),
		  new luaL_Reg("bxor", b_xor),
		  new luaL_Reg("btest", b_test),
		  new luaL_Reg("extract", b_extract),
		  new luaL_Reg("lrotate", b_lrot),
		  new luaL_Reg("lshift", b_lshift),
		  new luaL_Reg("replace", b_replace),
		  new luaL_Reg("rrotate", b_rrot),
		  new luaL_Reg("rshift", b_rshift),
		  new luaL_Reg(null, null)
		};
		
		
		
		public static int luaopen_bit32 (lua_State L) {
		  luaL_newlib(L, bitlib);
		  return 1;
		}
	}
}

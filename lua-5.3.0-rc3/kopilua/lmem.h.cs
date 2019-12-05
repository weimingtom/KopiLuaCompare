/*
** $Id: lmem.h,v 1.43 2014/12/19 17:26:14 roberto Exp $
** Interface to Memory Manager
** See Copyright Notice in lua.h
*/

using System.Diagnostics;

namespace KopiLua
{
	public partial class Lua
	{

		/*
		** This macro reallocs a vector 'b' from 'on' to 'n' elements, where
		** each element has size 'e'. In case of arithmetic overflow of the
		** product 'n'*'e', it raises an error (calling 'luaM_toobig'). Because
		** 'e' is always constant, it avoids the runtime division MAX_SIZET/(e).
		**
		** (The macro is somewhat complex to avoid warnings:  The 'sizeof'
		** comparison avoids a runtime comparison when overflow cannot occur.
		** The compiler should be able to optimize the real test by itself, but
		** when it does it, it may give a warning about "comparison is always
		** false due to limited range of data type"; the +1 tricks the compiler,
		** avoiding this warning but also this optimization.)
		*/
		public static T[] luaM_reallocv<T>(lua_State L, T[] block, int new_size)
		{
			return (T[])luaM_realloc_(L, block, new_size);
		}
		
		/*
		** Arrays of chars do not need any test
		*/
		public static CharPtr luaM_reallocvchar(lua_State L, CharPtr b, int on,int n) {
			return (CharPtr)(char[])luaM_realloc_(L, (b.chars), /*(on)*1*//*sizeof(char)*//*,*/ (n)*1/*sizeof(char)*/); } //FIXME:b->b.chars, remove 'on', char[] to CharPtr
			
		//#define luaM_freemem(L, b, s)	luaM_realloc_(L, (b), (s), 0)
		//#define luaM_free(L, b)		luaM_realloc_(L, (b), sizeof(*(b)), 0)
		//public static void luaM_freearray(lua_State L, object b, int n, Type t) { luaM_reallocv(L, b, n, 0, Marshal.SizeOf(b)); }

		// C# has it's own gc, so nothing to do here...in theory...
		public static void luaM_freemem<T>(lua_State L, T b, uint s) { luaM_realloc_<T>(L, new T[] {b}, 0); SubtractTotalBytes(L, s); } //FIXME: added
		public static void luaM_free<T>(lua_State L, T b) { luaM_realloc_<T>(L, new T[] {b}, 0); }
		public static void luaM_freearray<T>(lua_State L, T[] b) { luaM_realloc_(L, b, 0); } //FIXME:???

		public static T luaM_malloc<T>(lua_State L) { return (T)luaM_realloc_<T>(L); }
		public static T luaM_new<T>(lua_State L) { return (T)luaM_realloc_<T>(L); }
		public static T[] luaM_newvector<T>(lua_State L, int n)
		{
			return luaM_reallocv<T>(L, null, n);
		}

		//FIXME:
		public static T luaM_newobject<T>(lua_State L)	{ return (T)luaM_realloc_<T>(L); }


		public static void luaM_growvector<T>(lua_State L, ref T[] v, int nelems, ref int size, int limit, CharPtr e)
		{
			if (nelems + 1 > size)
				v = (T[])luaM_growaux_(L, ref v, ref size, limit, e);
		}

		public static T[] luaM_reallocvector<T>(lua_State L, ref T[] v, int oldn, int n)
		{
			Debug.Assert((v == null && oldn == 0) || (v.Length == oldn));
			v = luaM_reallocv<T>(L, v, n);
			return v;
		}
	}
}

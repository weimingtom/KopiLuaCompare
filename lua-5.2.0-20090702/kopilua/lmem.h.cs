using System.Diagnostics;

namespace KopiLua
{
	public partial class Lua
	{
		public const string MEMERRMSG	= "not enough memory";

		public static T[] luaM_reallocv<T>(lua_State L, T[] block, int new_size)
		{
			return (T[])luaM_realloc_(L, block, new_size);
		}
			
		//#define luaM_freemem(L, b, s)	luaM_realloc_(L, (b), (s), 0)
		//#define luaM_free(L, b)		luaM_realloc_(L, (b), sizeof(*(b)), 0)
		//public static void luaM_freearray(lua_State L, object b, int n, Type t) { luaM_reallocv(L, b, n, 0, Marshal.SizeOf(b)); }

		// C# has it's own gc, so nothing to do here...in theory...
		public static void luaM_freemem<T>(lua_State L, T b) { luaM_realloc_<T>(L, new T[] {b}, 0); }
		public static void luaM_free<T>(lua_State L, T b) { luaM_realloc_<T>(L, new T[] {b}, 0); }
		public static void luaM_freearray<T>(lua_State L, T[] b) { luaM_reallocv(L, b, 0); } //FIXME:???

		public static T luaM_malloc<T>(lua_State L) { return (T)luaM_realloc_<T>(L); }
		public static T luaM_new<T>(lua_State L) { return (T)luaM_realloc_<T>(L); }
		public static T[] luaM_newvector<T>(lua_State L, int n)
		{
			return luaM_reallocv<T>(L, null, n);
		}

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

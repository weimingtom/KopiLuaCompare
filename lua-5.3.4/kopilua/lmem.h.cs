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
		public static T[] luaM_reallocv<T>(lua_State L, T[] b, /*on,*/ int n/*, e*/) {
//FIXME:
//		  	if (sizeof(n) >= sizeof(size_t) && cast(size_t, (n)) + 1 > MAX_SIZET/(e))
//		      ? luaM_toobig(L) : cast_void(0);
		  	return (T[]) luaM_realloc_(L, b, n/*(on)*(e), (n)*(e)*/); } //FIXME:

		/*
		** Arrays of chars do not need any test
		*/
		public static char[] luaM_reallocvchar(lua_State L, char[] b, /*on,*/ int n)  {
			return (char[])(luaM_realloc_(L, (b), /*(on)*sizeof(char), (n)*sizeof(char)*/n)); }
		//FIXME: (s)
		public static void luaM_freemem<T>(lua_State L, T b) { luaM_realloc_(L, new T[] {(b)}, 0);}
		public static void luaM_free<T>(lua_State L, T b) { luaM_realloc_<T>(L, new T[] {(b)}, 0);}
		public static void luaM_freearray<T>(lua_State L, T[] b) { luaM_realloc_<T>(L, (b), 0); }

		public static T luaM_malloc<T>(lua_State L) {return (T)luaM_realloc_<T>(L);}
		public static T luaM_new<T>(lua_State L) {return (T)(luaM_malloc<T>(L));}
		public static T[] luaM_newvector<T>(lua_State L, int n) {
			return (T[])(luaM_reallocv<T>(L, null, n)); }
	
		//FIXME:
		public static T luaM_newobject<T>(lua_State L/*, tag, s*/)	{ return (T)luaM_realloc_<T>(L); }// NULL, tag, (s))}
	
		public static void luaM_growvector<T>(lua_State L, ref T[] v, int nelems, ref int size, int limit, CharPtr e) {
			if (nelems + 1 > size) {
			 	v = (T[])luaM_growaux_(L, ref v, ref size, limit, e); } } //FIXME: sizeof(t)
	
		public static T[] luaM_reallocvector<T>(lua_State L, ref T[] v, int oldn, int n) {
			Debug.Assert((v == null && oldn == 0) || (v.Length == oldn)); //FIXME:
			v = luaM_reallocv<T>(L, v, n); return v; }//FIXME: sizeof(t)

//LUAI_FUNC l_noret luaM_toobig (lua_State *L);

/* not to be called directly */
//LUAI_FUNC void *luaM_realloc_ (lua_State *L, void *block, size_t oldsize,
//                                                          size_t size);
//LUAI_FUNC void *luaM_growaux_ (lua_State *L, void *block, int *size,
//                               size_t size_elem, int limit,
//                               const char *what);

	}
}


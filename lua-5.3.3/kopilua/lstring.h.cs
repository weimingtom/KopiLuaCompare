/*
** $Id: lstring.h,v 1.61 2015/11/03 15:36:01 roberto Exp $
** String table (keep all strings handled by Lua)
** See Copyright Notice in lua.h
*/
namespace KopiLua
{
	using lu_byte = System.Byte;
	
	public partial class Lua
	{

		public static uint sizelstring(uint l) {return (uint)(GetUnmanagedSize(typeof(UTString)) + ((l) + 1) * 1/*sizeof(char)*/); }

		public static uint sizeludata(uint l) { return (uint)(GetUnmanagedSize(typeof(UUdata)) + (l)); }
		public static uint sizeudata(Udata u) {	return sizeludata((u).len); }

		public static TString luaS_newliteral(lua_State L, CharPtr s) { return luaS_newlstr(L, s, 
		                                                                       (uint)strlen(s)); } //FIXME:changed 


		/*
		** test whether a string is a reserved word
		*/
		public static bool isreserved(TString s) { return (s.tt == LUA_TSHRSTR && s.extra > 0);}


		/*
		** equality for short strings, which are always internalized
		*/
		public static bool eqshrstr(TString a, TString b) { return (bool)check_exp(a.tt == LUA_TSHRSTR, (a) == (b)); }
		

		//LUAI_FUNC unsigned int luaS_hash (const char *str, size_t l, unsigned int seed);
		//LUAI_FUNC unsigned int luaS_hashlongstr (TString *ts);
		//LUAI_FUNC int luaS_eqlngstr (TString *a, TString *b);
		//LUAI_FUNC void luaS_resize (lua_State *L, int newsize);
		//LUAI_FUNC void luaS_clearcache (global_State *g);
		//LUAI_FUNC void luaS_init (lua_State *L);
		//LUAI_FUNC void luaS_remove (lua_State *L, TString *ts);
		//LUAI_FUNC Udata *luaS_newudata (lua_State *L, size_t s);
		//LUAI_FUNC TString *luaS_newlstr (lua_State *L, const char *str, size_t l);
		//LUAI_FUNC TString *luaS_new (lua_State *L, const char *str);
		//LUAI_FUNC TString *luaS_createlngstrobj (lua_State *L, size_t l);
	}
}

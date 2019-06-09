/*
** $Id: lstring.h,v 1.46 2010/04/05 16:26:37 roberto Exp $
** String table (keep all strings handled by Lua)
** See Copyright Notice in lua.h
*/
namespace KopiLua
{
	using lu_byte = System.Byte;
	
	public partial class Lua
	{

		public static int sizestring(TString s) {return ((int)s.len + 1) * GetUnmanagedSize(typeof(char)); }

		public static uint sizeudata(Udata u) { return u.len; }

		public static TString luaS_newliteral(lua_State L, CharPtr s) { return luaS_newlstr(L, s, 
		                                                                       (uint)strlen(s)); } //FIXME:changed 

		public static void luaS_fix(TString s)
		{
			lu_byte marked = s.tsv.marked;	// can't pass properties in as ref
			l_setbit(ref marked, FIXEDBIT);
			s.tsv.marked = marked;
		}

		/*
		** as all string are internalized, string equality becomes
		** pointer equality
		*/
		public static bool eqstr(TString a, TString b) { return ((a) == (b)); }
		
		//LUAI_FUNC void luaS_resize (lua_State *L, int newsize);
		//LUAI_FUNC Udata *luaS_newudata (lua_State *L, size_t s, Table *e);
		//LUAI_FUNC TString *luaS_newlstr (lua_State *L, const char *str, size_t l);
		//LUAI_FUNC TString *luaS_new (lua_State *L, const char *str);
	}
}

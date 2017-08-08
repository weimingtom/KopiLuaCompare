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
		
		
	}
}

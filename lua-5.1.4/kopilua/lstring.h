		public static int sizestring(TString s) {return ((int)s.len + 1) * GetUnmanagedSize(typeof(char)); }

		public static int sizeudata(Udata u) { return (int)u.len; }

		public static TString luaS_new(lua_State L, CharPtr s) { return luaS_newlstr(L, s, (uint)strlen(s)); }
		public static TString luaS_newliteral(lua_State L, CharPtr s) { return luaS_newlstr(L, s, (uint)strlen(s)); }

		public static void luaS_fix(TString s)
		{
			lu_byte marked = s.tsv.marked;	// can't pass properties in as ref
			l_setbit(ref marked, FIXEDBIT);
			s.tsv.marked = marked;
		}

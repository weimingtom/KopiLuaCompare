namespace KopiLua
{
	using StkId = Lua.lua_TValue;
	
	public partial class Lua
	{
		public static void luaD_checkstack(lua_State L, int n) {
			if ((L.stack_last - L.top) <= n)
				luaD_growstack(L, n);
			else
			{
				#if HARDSTACKTESTS
				luaD_reallocstack(L, L.stacksize - EXTRA_STACK - 1);
				#endif
			}
		}

		public static void incr_top(lua_State L)
		{
			StkId.inc(ref L.top);
			luaD_checkstack(L, 0);

		}

		// in the original C code these values save and restore the stack by number of bytes. marshalling sizeof
		// isn't that straightforward in managed languages, so i implement these by index instead.
		public static int savestack(lua_State L, StkId p)		{return p;}
		public static StkId restorestack(lua_State L, int n)	{return L.stack[n];}
		public static int saveci(lua_State L, CallInfo p)		{return p - L.base_ci;}
		public static CallInfo restoreci(lua_State L, int n)	{ return L.base_ci[n]; }





		/* type of protected functions, to be ran by `runprotected' */
		public delegate void Pfunc(lua_State L, object ud);
	}
}

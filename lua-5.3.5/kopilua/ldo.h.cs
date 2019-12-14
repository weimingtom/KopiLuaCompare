/*
** $Id: ldo.h,v 2.29.1.1 2017/04/19 17:20:42 roberto Exp $
** Stack and Call structure of Lua
** See Copyright Notice in lua.h
*/

namespace KopiLua
{
	using StkId = Lua.lua_TValue;
	
	public partial class Lua
	{
		/*
		** Macro to check stack size and grow stack if needed.  Parameters
		** 'pre'/'pos' allow the macro to preserve a pointer into the
		** stack across reallocations, doing the work only when needed.
		** 'condmovestack' is used in heavy tests to force a stack reallocation
		** at every check.
		*/
		public static void luaD_checkstackaux(lua_State L, int n, luaC_condGC_pre pre, luaC_condGC_pos pos)  {
			if (L.stack_last - L.top <= (n))
			{ pre(); luaD_growstack(L, n); pos(); } else { condmovestack(L,pre,pos); }
		}

		/* In general, 'pre'/'pos' are empty (nothing to save) */
		public static void luaD_checkstack(lua_State L, int n)	{ luaD_checkstackaux(L,n,delegate(){},delegate(){}); }



		// in the original C code these values save and restore the stack by number of bytes. marshalling sizeof
		// isn't that straightforward in managed languages, so i implement these by index instead.
		public static int savestack(lua_State L, StkId p)		{return p;}
		public static StkId restorestack(lua_State L, int n)	{return L.stack[n];}






		/* type of protected functions, to be ran by 'runprotected' */
		public delegate void Pfunc(lua_State L, object ud);
	}
}

/*
** $Id: ltm.h,v 2.16 2013/04/29 16:56:50 roberto Exp $
** Tag methods
** See Copyright Notice in lua.h
*/

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	
	public partial class Lua
	{
		/*
		* WARNING: if you change the order of this enumeration,
		* grep "ORDER TM"
		*/
		public enum TMS {
		  TM_INDEX,
		  TM_NEWINDEX,
		  TM_GC,
		  TM_MODE,
		  TM_LEN,
		  TM_EQ,  /* last tag method with `fast' access */
		  TM_ADD,
		  TM_SUB,
		  TM_MUL,
		  TM_DIV,
		  TM_IDIV,
		  TM_MOD,
		  TM_POW,
		  TM_UNM,
		  TM_LT,
		  TM_LE,
		  TM_CONCAT,
		  TM_CALL,
		  TM_N		/* number of elements in the enum */
		};


		public static TValue gfasttm(global_State g, Table et, TMS e)
		{
			return (et == null) ? null : 
			((et.flags & (1 << (int)e)) != 0) ? null :
			luaT_gettm(et, e, g.tmname[(int)e]);
		}

		public static TValue fasttm(lua_State l, Table et, TMS e)	{return gfasttm(G(l), et, e);}


		public static CharPtr ttypename(int x) { return luaT_typenames_[(x) + 1]; }
		public static CharPtr objtypename(TValue x) { return ttypename(ttnov(x)); }

	}
}

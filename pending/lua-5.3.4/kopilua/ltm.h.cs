/*
** $Id: ltm.h,v 2.22 2016/02/26 19:20:15 roberto Exp $
** Tag methods
** See Copyright Notice in lua.h
*/

namespace KopiLua
{
	public partial class Lua
	{
		


		/*
		* WARNING: if you change the order of this enumeration,
		* grep "ORDER TM" and "ORDER OP"
		*/
		public enum TMS {
		  TM_INDEX,
		  TM_NEWINDEX,
		  TM_GC,
		  TM_MODE,
		  TM_LEN,
		  TM_EQ,  /* last tag method with fast access */
		  TM_ADD,
		  TM_SUB,
		  TM_MUL,
		  TM_MOD,
		  TM_POW,
		  TM_DIV,
		  TM_IDIV,
		  TM_BAND,
		  TM_BOR,
		  TM_BXOR,
		  TM_SHL,
		  TM_SHR,
		  TM_UNM,
		  TM_BNOT,
		  TM_LT,
		  TM_LE,
		  TM_CONCAT,
		  TM_CALL,
		  TM_N		/* number of elements in the enum */
		}
		
		
		
		public static TValue gfasttm(global_State g, Table et, TMS e) { if (et == null) { return null; } else {
				if ((et.flags & (1 << (int)(e))) != 0) { return null;} else {return luaT_gettm(et, e, g.tmname[(int)e]);} }}
		
		public static TValue fasttm(lua_State l, Table et, TMS e) { return gfasttm(G(l), et, e);}
		
		public static string ttypename(int x)	{ return luaT_typenames_[x + 1]; }
		
		//LUAI_DDEC const char *const luaT_typenames_[LUA_TOTALTAGS];
		
		
		//LUAI_FUNC const char *luaT_objtypename (lua_State *L, const TValue *o);
		
		//LUAI_FUNC const TValue *luaT_gettm (Table *events, TMS event, TString *ename);
		//LUAI_FUNC const TValue *luaT_gettmbyobj (lua_State *L, const TValue *o,
		//                                                       TMS event);
		//LUAI_FUNC void luaT_init (lua_State *L);
		
		//LUAI_FUNC void luaT_callTM (lua_State *L, const TValue *f, const TValue *p1,
		//                            const TValue *p2, TValue *p3, int hasres);
		//LUAI_FUNC int luaT_callbinTM (lua_State *L, const TValue *p1, const TValue *p2,
		//                              StkId res, TMS event);
		//LUAI_FUNC void luaT_trybinTM (lua_State *L, const TValue *p1, const TValue *p2,
		//                              StkId res, TMS event);
		//LUAI_FUNC int luaT_callorderTM (lua_State *L, const TValue *p1,
		//                                const TValue *p2, TMS event);



	}
}

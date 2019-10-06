/*
** $Id: ltm.c,v 2.20 2013/05/06 17:19:11 roberto Exp $
** Tag methods
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using ptrdiff_t = System.Int32;
	
	public partial class Lua
	{
		private static CharPtr udatatypename = "userdata";

		public readonly static CharPtr[] luaT_typenames_ = { //FIXME:changed, LUA_TOTALTAGS
          "no value",
		  "nil", "boolean", udatatypename, "number",
		  "string", "table", "function", udatatypename, "thread",
		  "proto", "upval"  /* these last two cases are used for tests only */
		};

		private readonly static CharPtr[] luaT_eventname = {  /* ORDER TM */
			"__index", "__newindex",
		    "__gc", "__mode", "__len", "__eq",
		    "__add", "__sub", "__mul", "__div", "__idiv", "__mod",
		    "__pow", "__unm", "__lt", "__le",
			"__concat", "__call"
		  };

		public static void luaT_init (lua_State L) {
		  int i;
		  for (i=0; i<(int)TMS.TM_N; i++) {
			G(L).tmname[i] = luaS_new(L, luaT_eventname[i]);
			luaS_fix(G(L).tmname[i]);  /* never collect these names */
		  }
		}


		/*
		** function to be used with macro "fasttm": optimized for absence of
		** tag methods
		*/
		public static TValue luaT_gettm (Table events, TMS event_, TString ename) {
		  /*const*/ TValue tm = luaH_getstr(events, ename);
		  lua_assert(event_ <= TMS.TM_EQ);
		  if (ttisnil(tm)) {  /* no tag method? */
			events.flags |= (byte)(1<<(int)event_);  /* cache this fact */
			return null;
		  }
		  else return tm;
		}


		public static TValue luaT_gettmbyobj (lua_State L, TValue o, TMS event_) {
		  Table mt;
		  switch (ttnov(o)) {
			case LUA_TTABLE:
			  mt = hvalue(o).metatable;
			  break;
			case LUA_TUSERDATA:
			  mt = uvalue(o).metatable;
			  break;
			default:
			  mt = G(L).mt[ttnov(o)];
			  break;//FIXME: added
		  }
		  return ((mt!=null) ? luaH_getstr(mt, G(L).tmname[(int)event_]) : luaO_nilobject);
		}


		public static void luaT_callTM (lua_State L, TValue f, TValue p1,
		                  TValue p2, TValue p3, int hasres) {
		  ptrdiff_t result = savestack(L, p3);
		  setobj2s(L, L.top, f); StkId.inc(ref L.top);  /* push function */
		  setobj2s(L, L.top, p1); StkId.inc(ref L.top);  /* 1st argument */
		  setobj2s(L, L.top, p2); StkId.inc(ref L.top);  /* 2nd argument */
		  if (0==hasres) {  /* no result? 'p3' is third argument */
		    setobj2s(L, L.top, p3); StkId.inc(ref L.top);  /* 3rd argument */
		  }
		  /* metamethod may yield only when called from Lua code */
		  luaD_call(L, L.top - (4 - hasres), hasres, isLua(L.ci));
		  if (hasres!=0) {  /* if has result, move it to its place */
		    p3 = restorestack(L, result);
		    StkId.dec(ref L.top); setobjs2s(L, p3, L.top);
		  }
		}


		public static int luaT_callbinTM (lua_State L, TValue p1, TValue p2,
		                    StkId res, TMS event_) {
		  TValue tm = luaT_gettmbyobj(L, p1, event_);  /* try first operand */
		  if (ttisnil(tm))
		    tm = luaT_gettmbyobj(L, p2, event_);  /* try second operand */
		  if (ttisnil(tm)) return 0;
		  luaT_callTM(L, tm, p1, p2, res, 1);
		  return 1;
		}


		public static void luaT_trybinTM (lua_State L, TValue p1, TValue p2,
		                    StkId res, TMS event_) {
		  if (0==luaT_callbinTM(L, p1, p2, res, event_)) {
		    if (event_ == TMS.TM_CONCAT)
		      luaG_concaterror(L, p1, p2);
		    else if (event_ == TMS.TM_IDIV && ttisnumber(p1) && ttisnumber(p2))
		      luaG_tointerror(L, p1, p2);
		    else
		      luaG_aritherror(L, p1, p2);
		  }
		}


		public static TValue luaT_getequalTM (lua_State L, Table mt1, Table mt2) {
		  TValue tm1 = fasttm(L, mt1, TMS.TM_EQ);
		  TValue tm2;
		  if (tm1 == null) return null;  /* no metamethod */
		  if (mt1 == mt2) return tm1;  /* same metatables => same metamethods */
		  tm2 = fasttm(L, mt2, TMS.TM_EQ);
		  if (tm2 == null) return null;  /* no metamethod */
		  if (luaV_rawequalobj(tm1, tm2)!=0)  /* same metamethods? */
		    return tm1;
		  return null;
		}


		public static int luaT_callorderTM (lua_State L, TValue p1, TValue p2,
		                      TMS event_) {
		  if (0==luaT_callbinTM(L, p1, p2, L.top, event_))
		    return -1;  /* no metamethod */
		  else
		  	return (0==l_isfalse(L.top)) ? 1 : 0;
		}
	}
}

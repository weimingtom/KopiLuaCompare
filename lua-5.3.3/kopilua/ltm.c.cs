/*
** $Id: ltm.c,v 2.37 2016/02/26 19:20:15 roberto Exp $
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
	using lua_Number = System.Double;
	
	public partial class Lua
	{
		private static CharPtr udatatypename = "userdata";

		public readonly static CharPtr[] luaT_typenames_ = { //FIXME:changed, LUA_TOTALTAGS
          "no value",
		  "nil", "boolean", udatatypename, "number",
		  "string", "table", "function", udatatypename, "thread",
		  "proto", /* this last case is used for tests only */
		};

		private readonly static CharPtr[] luaT_eventname = {  /* ORDER TM */
			"__index", "__newindex",
		    "__gc", "__mode", "__len", "__eq",
		    "__add", "__sub", "__mul", "__mod", "__pow",
		    "__div", "__idiv",
		    "__band", "__bor", "__bxor", "__shl", "__shr",
		    "__unm", "__bnot", "__lt", "__le",
			"__concat", "__call"
		  };

		public static void luaT_init (lua_State L) {
		  int i;
		  for (i=0; i<(int)TMS.TM_N; i++) {
			G(L).tmname[i] = luaS_new(L, luaT_eventname[i]);
			luaC_fix(L, obj2gco(G(L).tmname[i]));  /* never collect these names */
		  }
		}


		/*
		** function to be used with macro "fasttm": optimized for absence of
		** tag methods
		*/
		public static TValue luaT_gettm (Table events, TMS event_, TString ename) {
		  /*const*/ TValue tm = luaH_getshortstr(events, ename);
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
		  return ((mt!=null) ? luaH_getshortstr(mt, G(L).tmname[(int)event_]) : luaO_nilobject);
		}


		/*
		** Return the name of the type of an object. For tables and userdata
		** with metatable, use their '__name' metafield, if present.
		*/
		private static CharPtr luaT_objtypename (lua_State L, TValue o) {
		  Table mt;
		  if ((ttistable(o) && (mt = hvalue(o).metatable) != null) ||
		      (ttisfulluserdata(o) && (mt = uvalue(o).metatable) != null)) {
		    TValue name = luaH_getshortstr(mt, luaS_new(L, "__name"));
		    if (ttisstring(name))  /* is '__name' a string? */
		      return getstr(tsvalue(name));  /* use it as type name */
		  }
		  return ttypename(ttnov(o));  /* else use standard type name */
		}


		public static void luaT_callTM (lua_State L, TValue f, TValue p1,
		                  TValue p2, TValue p3, int hasres) {
		  ptrdiff_t result = savestack(L, p3);
		  StkId func = L.top;
		  setobj2s(L, func, f);  /* push function (assume EXTRA_STACK) */
		  setobj2s(L, func + 1, p1);  /* 1st argument */
		  setobj2s(L, func + 2, p2);  /* 2nd argument */
		  L.top += 3;
		  if (0==hasres) {  /* no result? 'p3' is third argument */
		    setobj2s(L, L.top, p3); StkId.inc(ref L.top);  /* 3rd argument */
		  }
		  /* metamethod may yield only when called from Lua code */
		  if (0!=isLua(L.ci))
		    luaD_call(L, func, hasres);
		  else
		    luaD_callnoyield(L, func, hasres);
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
		    switch (event_) {
		      case TMS.TM_CONCAT:
		        luaG_concaterror(L, p1, p2);
		        goto case TMS.TM_BAND;//FIXME:added
			  /* call never returns, but to avoid warnings: *//* FALLTHROUGH */
		      case TMS.TM_BAND: case TMS.TM_BOR: case TMS.TM_BXOR:
		      case TMS.TM_SHL: case TMS.TM_SHR: case TMS.TM_BNOT: {
		        lua_Number dummy = 0;
		        if (0!=tonumber(ref p1, ref dummy) && 0!=tonumber(ref p2, ref dummy))
		          luaG_tointerror(L, p1, p2);
		        else
		          luaG_opinterror(L, p1, p2, "perform bitwise operation on");
				goto default; //FIXME:added
		      }
			  /* calls never return, but to avoid warnings: *//* FALLTHROUGH */
		      default:
		        luaG_opinterror(L, p1, p2, "perform arithmetic on");
		        break; //FIXME:added
		    }
		  }
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

/*
** $Id: ltm.c,v 2.10 2009/11/19 19:06:52 roberto Exp roberto $
** Tag methods
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	
	public partial class Lua
	{
		private static CharPtr udatatypename = "userdata";

		public readonly static CharPtr[] luaT_typenames_ = {
          "no value",
		  "nil", "boolean", udatatypename, "number",
		  "string", "table", "function", udatatypename, "thread",
		  "proto", "upval"
		};

		private readonly static CharPtr[] luaT_eventname = {  /* ORDER TM */
			"__index", "__newindex",
		    "__gc", "__mode", "__len", "__eq",
		    "__add", "__sub", "__mul", "__div", "__mod",
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
		  switch (ttype(o)) {
			case LUA_TTABLE:
			  mt = hvalue(o).metatable;
			  break;
			case LUA_TUSERDATA:
			  mt = uvalue(o).metatable;
			  break;
			default:
			  mt = G(L).mt[ttype(o)];
			  break;
		  }
		  return ((mt!=null) ? luaH_getstr(mt, G(L).tmname[(int)event_]) : luaO_nilobject);
		}

	}
}

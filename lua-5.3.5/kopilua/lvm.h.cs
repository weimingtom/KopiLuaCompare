/*
** $Id: lvm.h,v 2.41.1.1 2017/04/19 17:20:42 roberto Exp $
** Lua virtual machine
** See Copyright Notice in lua.h
*/

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using lua_Integer = System.Int32;
	using lua_Number = System.Double;
	
	public partial class Lua
	{
		//#if !defined(LUA_NOCVTN2S)
		public static int cvt2str(TValue o)	{ return ttisnumber(o)?1:0; }
		//#else
		//#define cvt2str(o)	0	/* no conversion from numbers to strings */
		//#endif


		//#if !defined(LUA_NOCVTS2N)
		public static int cvt2num(TValue o)	{ return ttisstring(o)?1:0; }
		//#else
		//#define cvt2num(o)	0	/* no conversion from strings to numbers */
		//#endif


		/*
		** You can define LUA_FLOORN2I if you want to convert floats to integers
		** by flooring them (instead of raising an error if they are not
		** integral values)
		*/
		//#if !defined(LUA_FLOORN2I)
		public const int LUA_FLOORN2I = 0;
		//#endif

	
		public static int tonumber(ref StkId o, ref lua_Number n) 
			{ if (ttisfloat(o)) { n = fltvalue(o); return 1; } else { return luaV_tonumber_(o, ref n);} }

		public static int tointeger(ref StkId o, ref lua_Integer i)
			{ if (ttisinteger(o)) { i = ivalue(o); return 1; } else { return luaV_tointeger(o, ref i, LUA_FLOORN2I); } }

		//FIXME:changed, see intop
		//FIXME:???Lua_Number
		public static int intop_plus(lua_Integer v1, lua_Integer v2) { return l_castU2S(l_castS2U(v1) + l_castS2U(v2));}
		public static int intop_minus(lua_Integer v1, lua_Integer v2) { return l_castU2S(l_castS2U(v1) - l_castS2U(v2));}
		public static int intop_mul(lua_Integer v1, lua_Integer v2) { return l_castU2S(l_castS2U(v1) * l_castS2U(v2));}
		public static int intop_xor(lua_Integer v1, lua_Integer v2) { return l_castU2S(l_castS2U(v1) ^ l_castS2U(v2));}
		public static int intop_or(lua_Integer v1, lua_Integer v2) { return l_castU2S(l_castS2U(v1) | l_castS2U(v2));}
		public static int intop_and(lua_Integer v1, lua_Integer v2) { return l_castU2S(l_castS2U(v1) & l_castS2U(v2));}
		public static int intop_shiftleft(lua_Integer v1, lua_Integer v2) { return l_castU2S((int)l_castS2U(v1) << (int)l_castS2U(v2));} //FIXME:???(int)
		public static int intop_shiftright(lua_Integer v1, lua_Integer v2) { return l_castU2S((int)l_castS2U(v1) >> (int)l_castS2U(v2));} //FIXME:???(int)
		
		public static int luaV_rawequalobj(TValue t1,TValue t2) { return luaV_equalobj(null,t1,t2); }


		/*
		** fast track for 'gettable': if 't' is a table and 't[k]' is not nil,
		** return 1 with 'slot' pointing to 't[k]' (final result).  Otherwise,
		** return 0 (meaning it will have to check metamethod) with 'slot'
		** pointing to a nil 't[k]' (if 't' is a table) or NULL (otherwise).
		** 'f' is the raw get function to use.
		*/
		//#define luaV_fastget(L,t,k,slot,f) \
		//  (!ttistable(t)  \
		//   ? (slot = NULL, 0)  /* not a table; 'slot' is NULL and result is 0 */  \
		//   : (slot = f(hvalue(t), k),  /* else, do raw access */  \
		//      !ttisnil(slot)))  /* result not nil? */
		public static int luaV_fastget_luaH_getstr(lua_State L, TValue t, TString k, ref TValue slot) {
		  if (!ttistable(t)) {
		    slot = null;
		    return 0;  /* not a table; 'slot' is NULL and result is 0 */
		  } else {
			slot = luaH_getstr(hvalue(t), k);  /* else, do raw access */
			return (!ttisnil(slot))?1:0;  /* result not nil? */
		  }
		}
		public static int luaV_fastget_luaH_getint(lua_State L, TValue t, int k, ref TValue slot) {
		  if (!ttistable(t)) {
		    slot = null;
		    return 0;  /* not a table; 'slot' is NULL and result is 0 */
		  } else {
			slot = luaH_getint(hvalue(t), k);  /* else, do raw access */
			return (!ttisnil(slot))?1:0;  /* result not nil? */
		  }
		}
		public static int luaV_fastget_luaH_get(lua_State L, TValue t, TValue k, ref TValue slot) {
		  if (!ttistable(t)) {
		    slot = null;
		    return 0;  /* not a table; 'slot' is NULL and result is 0 */
		  } else {
			slot = luaH_get(hvalue(t), k);  /* else, do raw access */
			return (!ttisnil(slot))?1:0;  /* result not nil? */
		  }
		}
		
		/*
		** standard implementation for 'gettable'
		*/
		public static void luaV_gettable(lua_State L, TValue t, TValue k, StkId v) { TValue slot = null;
		  if (0!=luaV_fastget_luaH_get(L,t,k,ref slot)) { setobj2s(L, v, slot); }
		  else luaV_finishget(L,t,k,v,slot); }


		/*
		** Fast track for set table. If 't' is a table and 't[k]' is not nil,
		** call GC barrier, do a raw 't[k]=v', and return true; otherwise,
		** return false with 'slot' equal to NULL (if 't' is not a table) or
		** 'nil'. (This is needed by 'luaV_finishget'.) Note that, if the macro
		** returns true, there is no need to 'invalidateTMcache', because the
		** call is not creating a new entry.
		*/
		//#define luaV_fastset(L,t,k,slot,f,v) \
		//  (!ttistable(t) \
		//   ? (slot = NULL, 0) \
		//   : (slot = f(hvalue(t), k), \
		//     ttisnil(slot) ? 0 \
		//     : (luaC_barrierback(L, hvalue(t), v), \
		//        setobj2t(L, cast(TValue *,slot), v), \
		//        1)))
		public static int luaV_fastset_luaH_getstr(lua_State L, TValue t, TString k, ref TValue slot, StkId v) {
		  if (!ttistable(t)) {
	 	    slot = null;
	 	    return 0;
		  } else {
			slot = luaH_getstr(hvalue(t), k);
			if (ttisnil(slot)) {
			  return 0;
			} else {
		      luaC_barrierback(L, hvalue(t), v);
		      setobj2t(L, (TValue)(slot), v);
			  return 1;
			}
		  }
		}
		public static int luaV_fastset_luaH_get(lua_State L, TValue t, TValue k, ref TValue slot, StkId v) {
		  if (!ttistable(t)) {
	 	    slot = null;
	 	    return 0;
		  } else {
			slot = luaH_get(hvalue(t), k);
			if (ttisnil(slot)) {
			  return 0;
			} else {
		      luaC_barrierback(L, hvalue(t), v);
		      setobj2t(L, (TValue)(slot), v);
			  return 1;
			}
		  }
		}
		public static int luaV_fastset_luaH_getint(lua_State L, TValue t, int k, ref TValue slot, StkId v) {
		  if (!ttistable(t)) {
	 	    slot = null;
	 	    return 0;
		  } else {
			slot = luaH_getint(hvalue(t), k);
			if (ttisnil(slot)) {
			  return 0;
			} else {
		      luaC_barrierback(L, hvalue(t), v);
		      setobj2t(L, (TValue)(slot), v);
			  return 1;
			}
		  }
		}
			

		public static void luaV_settable(lua_State L, StkId t, StkId k, StkId v) { TValue slot = null;
		  if (0==luaV_fastset_luaH_get(L,t,k,ref slot,v))
		    luaV_finishset(L,t,k,v,slot); }


		
	}
}

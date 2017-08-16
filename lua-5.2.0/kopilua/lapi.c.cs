/*
** $Id: lapi.c,v 2.159 2011/11/30 12:32:05 roberto Exp $
** Lua API
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace KopiLua
{
	using lu_mem = System.UInt32;
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using lua_Integer = System.Int32;
	using lua_Number = System.Double;
	using ptrdiff_t = System.Int32;
	using lua_Unsigned = System.UInt32;

	public partial class Lua
	{
		public const string lua_ident =
		  "$LuaVersion: " + LUA_COPYRIGHT + " $" + 
		  "$LuaAuthors: " + LUA_AUTHORS + " $";


		/* value at a non-valid index */
		//#define NONVALIDVALUE		cast(TValue *, luaO_nilobject) //private static TValue NONVALIDVALUE = (TValue)(luaO_nilobject); //FIXME:WARNING, don't save it as static var, world be null, not safe

		/* corresponding test */
		private static bool isvalid(int o) { return (o != luaO_nilobject); }

		public static void api_checkvalidindex(lua_State L, int i) { api_check(L, isvalid(i), "invalid index"); }


		static TValue index2addr (lua_State L, int idx) {
          CallInfo ci = L.ci;
		  if (idx > 0) {
			TValue o = ci.func + idx;
			api_check(L, idx <= ci.top - (ci.func + 1), "unacceptable index");
			if (o >= L.top) return (TValue)(luaO_nilobject); //FIXME:changed, return NONVALIDVALUE;
			else return o;
		  }
		  else if (idx > LUA_REGISTRYINDEX) {
			api_check(L, idx != 0 && -idx <= L.top - (ci.func + 1), "invalid index");
			return L.top + idx;
		  }
		  else if (idx == LUA_REGISTRYINDEX)
		    return G(L).l_registry;
		  else {  /* upvalues */
		    idx = LUA_REGISTRYINDEX - idx;
		    api_check(L, idx <= MAXUPVAL + 1, "upvalue index too large");
		    if (ttislcf(ci.func))  /* light C function? */
		    return (TValue)(luaO_nilobject);  /* it has no upvalues */ //FIXME:changed, return NONVALIDVALUE;
		    else {
		      CClosure func = clCvalue(ci.func);
		      return (idx <= func.nupvalues) ? func.upvalue[idx-1] : (TValue)(luaO_nilobject); //FIXME:changed, return NONVALIDVALUE;
		    }
		  }
		}


		/*
		** to be called by 'lua_checkstack' in protected mode, to grow stack
		** capturing memory errors
		*/
		private static void growstack (lua_State L, object ud) {
		  int size = ((int[])ud)[0];
		  luaD_growstack(L, size);
		}


		public static int lua_checkstack (lua_State L, int size) {
		  int res;
          CallInfo ci = L.ci;
		  lua_lock(L);
		  if (L.stack_last - L.top > size)  /* stack large enough? */
		    res = 1;  /* yes; check is OK */
		  else {  /* no; need to grow stack */
		    int inuse = (int)(L.top - L.stack) + EXTRA_STACK; //FIXME:cast_int()
		    if (inuse > LUAI_MAXSTACK - size)  /* can grow without overflow? */
		      res = 0;  /* no */
		    else  {/* try to grow stack */
		      int[] sizep = new int[]{size}; //FIXME:added
		      res = (luaD_rawrunprotected(L, growstack, sizep) == LUA_OK ? 1 : 0);
		      size = sizep[0]; //FIXME:added
		    }
		  }
		  if (res != 0 && ci.top < L.top + size)
		    ci.top = L.top + size;  /* adjust frame top */
		  lua_unlock(L);
		  return res;
		}


		public static void lua_xmove (lua_State from, lua_State to, int n) {
		  int i;
		  if (from == to) return;
		  lua_lock(to);
		  api_checknelems(from, n);
		  api_check(from, G(from) == G(to), "moving among independent states");
		  api_check(from, to.ci.top - to.top >= n, "not enough elements to move");
		  from.top -= n;
		  for (i = 0; i < n; i++) {
			setobj2s(to, StkId.inc(ref to.top), from.top + i);
		  }
		  lua_unlock(to);
		}



		public static lua_CFunction lua_atpanic (lua_State L, lua_CFunction panicf) {
		  lua_CFunction old;
		  lua_lock(L);
		  old = G(L).panic;
		  G(L).panic = panicf;
		  lua_unlock(L);
		  return old;
		}

        private static double[] version = new double[]{LUA_VERSION_NUM};
		public static double[] lua_version (lua_State L) {
		  if (L == null) return version;
		  else return G(L).version;
		}



		/*
		** basic stack manipulation
		*/


		/*
		** convert an acceptable stack index into an absolute index
		*/
		public static int lua_absindex (lua_State L, int idx) {
		  return (idx > 0 || idx <= LUA_REGISTRYINDEX)
		         ? idx
		         : cast_int(L.top - L.ci.func + idx);
		}


		public static int lua_gettop (lua_State L) {
		  return cast_int(L.top - (L.ci.func + 1));
		}


		public static void lua_settop (lua_State L, int idx) {
          StkId func = L.ci.func;
		  lua_lock(L);
		  if (idx >= 0) {
			api_check(L, idx <= L.stack_last - (func + 1), "new top too large");
			while (L.top < (func + 1) + idx)
			  setnilvalue(StkId.inc(ref L.top)); //FIXME:???
			L.top = (func + 1) + idx;
		  }
		  else {
			api_check(L, -(idx+1) <= (L.top - (func + 1)), "invalid new top");
			L.top += idx+1;  /* `subtract' index (index is negative) */
		  }
		  lua_unlock(L);
		}


		public static void lua_remove (lua_State L, int idx) {
		  StkId p;
		  lua_lock(L);
		  p = index2addr(L, idx);
		  api_checkvalidindex(L, p);
		  while ((p=p[1]) < L.top) setobjs2s(L, p-1, p);
		  StkId.dec(ref L.top);
		  lua_unlock(L);
		}


		public static void lua_insert (lua_State L, int idx) {
		  StkId p;
		  StkId q;
		  lua_lock(L);
		  p = index2addr(L, idx);
		  api_checkvalidindex(L, p);
		  for (q = L.top; q>p; StkId.dec(ref q)) setobjs2s(L, q, q-1);
		  setobjs2s(L, p, L.top);
		  lua_unlock(L);
		}


		private static void moveto (lua_State L, TValue fr, int idx) {
		  TValue to = index2addr(L, idx);
		  api_checkvalidindex(L, to);
		  setobj(L, to, fr);
		  if (idx < LUA_REGISTRYINDEX)  /* function upvalue? */
		    luaC_barrier(L, clvalue(L.ci.func), fr);
		  /* LUA_REGISTRYINDEX does not need gc barrier
		     (collector revisits it before finishing collection) */
		}


		public static void lua_replace (lua_State L, int idx) {
		  lua_lock(L);
		  api_checknelems(L, 1);
		  moveto(L, L.top - 1, idx);
		  StkId.dec(ref L.top);
		  lua_unlock(L);
		}


		public static void lua_copy (lua_State L, int fromidx, int toidx) {
		  TValue fr;
		  lua_lock(L);
		  fr = index2addr(L, fromidx);
		  api_checkvalidindex(L, fr);
		  moveto(L, fr, toidx);
		  lua_unlock(L);
		}


		public static void lua_pushvalue (lua_State L, int idx) {
		  lua_lock(L);
		  setobj2s(L, L.top, index2addr(L, idx));
		  api_incr_top(L);
		  lua_unlock(L);
		}



		/*
		** access functions (stack . C)
		*/


		public static int lua_type (lua_State L, int idx) {
		  StkId o = index2addr(L, idx);
		  return (isvalid(o) ? ttypenv(o) : LUA_TNONE);
		}


		public static CharPtr lua_typename (lua_State L, int t) {
		  //UNUSED(L);
		  return ttypename(t);
		}


		public static bool lua_iscfunction (lua_State L, int idx) {
		  StkId o = index2addr(L, idx);
		  return (ttislcf(o) || (ttisCclosure(o)));
		}


		public static int lua_isnumber (lua_State L, int idx) {
		  TValue n = new TValue();
		  TValue o = index2addr(L, idx);
		  return tonumber(ref o, n);
		}


		public static int lua_isstring (lua_State L, int idx) {
		  int t = lua_type(L, idx);
		  return (t == LUA_TSTRING || t == LUA_TNUMBER) ? 1 : 0;
		}


		public static int lua_isuserdata (lua_State L, int idx) {
		  TValue o = index2addr(L, idx);
		  return (ttisuserdata(o) || ttislightuserdata(o)) ? 1 : 0;
		}


		public static int lua_rawequal (lua_State L, int index1, int index2) {
		  StkId o1 = index2addr(L, index1);
		  StkId o2 = index2addr(L, index2);
		  return (isvalid(o1) && isvalid(o2)) ? luaV_rawequalobj(o1, o2) : 0;
		}


		public static void lua_arith (lua_State L, int op) {
		  StkId o1;  /* 1st operand */
		  StkId o2;  /* 2nd operand */
		  lua_lock(L);
          if (op != LUA_OPUNM) /* all other operations expect two operands */
		    api_checknelems(L, 2);
		  else {  /* for unary minus, add fake 2nd operand */
		    api_checknelems(L, 1);
		    setobjs2s(L, L.top, L.top - 1);
		    lua_TValue.inc(ref L.top); //FIXME:++
		  }
		  o1 = L.top - 2;
		  o2 = L.top - 1;
		  if (ttisnumber(o1) && ttisnumber(o2)) {
		    changenvalue(o1, luaO_arith(op, nvalue(o1), nvalue(o2)));
          }
		  else
		    luaV_arith(L, o1, o1, o2, (TMS)(op - LUA_OPADD + TMS.TM_ADD));
		  lua_TValue.dec(ref L.top);
		  lua_unlock(L);
		}


		public static int lua_compare (lua_State L, int index1, int index2, int op) {
		  StkId o1, o2;
		  int i = 0;
		  lua_lock(L);  /* may call tag method */
		  o1 = index2addr(L, index1);
		  o2 = index2addr(L, index2);
		  if (isvalid(o1) && isvalid(o2)) {
		    switch (op) {
		  	  case LUA_OPEQ: i = equalobj(L, o1, o2) ? 1 : 0; break;
		      case LUA_OPLT: i = luaV_lessthan(L, o1, o2); break;
		      case LUA_OPLE: i = luaV_lessequal(L, o1, o2); break;
		      default: api_check(L, 0, "invalid option"); break; //FIXME:break added
			}
		  }
		  lua_unlock(L);
		  return i;
		}


		public static lua_Number lua_tonumberx (lua_State L, int idx, ref int isnum) {
		  TValue n = new TValue();
		  TValue o = index2addr(L, idx);
		  if (tonumber(ref o, n) != 0) {
            /*if (isnum)*/ isnum = 1; //FIXME:changed
			return nvalue(o);
		  }
		  else {
            /*if (isnum)*/ isnum = 0; //FIXME:changed
			return 0;
		  }
		}


		public static lua_Integer lua_tointegerx (lua_State L, int idx, ref int isnum) {
		  TValue n = new TValue();
		  TValue o = index2addr(L, idx);
		  if (tonumber(ref o, n) != 0) {
			lua_Integer res;
			lua_Number num = nvalue(o);
			lua_number2integer(out res, num);
            /*if (isnum)*/ isnum = 1; //FIXME:changed
			return res;
		  }
		  else {
            /*if (isnum)*/ isnum = 0; //FIXME:changed
			return 0;
		  }
		}


		public static lua_Unsigned lua_tounsignedx (lua_State L, int idx, ref int isnum) {
		  TValue n = new TValue();
		  /*const*/ TValue o = index2addr(L, idx);
		  if (tonumber(ref o, n)!=0) {
		    lua_Unsigned res;
		    lua_Number num = nvalue(o);
		    lua_number2unsigned(out res, num);
		    /*if (isnum)*/ isnum = 1; //FIXME:changed
		    return res;
		  }
		  else {
		    /*if (isnum)*/ isnum = 0; //FIXME:changed
		    return 0;
		  }
		}


		public static int lua_toboolean (lua_State L, int idx) {
		  TValue o = index2addr(L, idx);
		  return (l_isfalse(o) == 0) ? 1 : 0;
		}


		public static CharPtr lua_tolstring (lua_State L, int idx, out uint len) {
		  StkId o = index2addr(L, idx);
		  if (!ttisstring(o)) {
			lua_lock(L);  /* `luaV_tostring' may create a new string */
			if (luaV_tostring(L, o)==0) {  /* conversion failed? */
			  len = 0;
			  lua_unlock(L);
			  return null;
			}
			luaC_checkGC(L);
			o = index2addr(L, idx);  /* previous call may reallocate the stack */
			lua_unlock(L);
		  }
		  len = tsvalue(o).len; //FIXME: no if (len != null)
		  return svalue(o);
		}


		public static uint lua_rawlen (lua_State L, int idx) {
		  StkId o = index2addr(L, idx);
		  switch (ttypenv(o)) {
			case LUA_TSTRING: return tsvalue(o).len;
			case LUA_TUSERDATA: return uvalue(o).len;
			case LUA_TTABLE: return (uint)luaH_getn(hvalue(o));
			default: return 0;
		  }
		}


		public static lua_CFunction lua_tocfunction (lua_State L, int idx) {
		  StkId o = index2addr(L, idx);
		  if (ttislcf(o)) return fvalue(o);
		  else if (ttisCclosure(o))
		    return clCvalue(o).f;
		  else return null;  /* not a C function */
		}


		public static object lua_touserdata (lua_State L, int idx) {
		  StkId o = index2addr(L, idx);
		  switch (ttypenv(o)) {
			case LUA_TUSERDATA: return (rawuvalue(o).user_data);
			case LUA_TLIGHTUSERDATA: return pvalue(o);
			default: return null;
		  }
		}


		public static lua_State lua_tothread (lua_State L, int idx) {
		  StkId o = index2addr(L, idx);
		  return (!ttisthread(o)) ? null : thvalue(o);
		}


		public static object lua_topointer (lua_State L, int idx) {
		  StkId o = index2addr(L, idx);
		  switch (ttype(o)) {
			case LUA_TTABLE: return hvalue(o);
			case LUA_TLCL: return clLvalue(o);
            case LUA_TCCL: return clCvalue(o);
			case LUA_TLCF: return (object)(lua_CFunction)fvalue(o); //FIXME:???size_t to void*
			case LUA_TTHREAD: return thvalue(o);
			case LUA_TUSERDATA:
			case LUA_TLIGHTUSERDATA:
			  return lua_touserdata(L, idx);
			default: return null;
		  }
		}



		/*
		** push functions (C . stack)
		*/


		public static void lua_pushnil (lua_State L) {
		  lua_lock(L);
		  setnilvalue(L.top);
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static void lua_pushnumber (lua_State L, lua_Number n) {
		  lua_lock(L);
		  setnvalue(L.top, n);
		  luai_checknum(L, L.top, delegate() {
		                	luaG_runerror(L, "C API - attempt to push a signaling NaN");});
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static void lua_pushinteger (lua_State L, lua_Integer n) {
		  lua_lock(L);
		  setnvalue(L.top, cast_num(n));
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static void lua_pushunsigned (lua_State L, lua_Unsigned u) {
		  lua_Number n;
		  lua_lock(L);
		  n = lua_unsigned2number(u);
		  setnvalue(L.top, n);
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static CharPtr lua_pushlstring (lua_State L, CharPtr s, uint len) {
          TString ts;
		  lua_lock(L);
		  luaC_checkGC(L);
		  ts = luaS_newlstr(L, s, len);
		  setsvalue2s(L, L.top, ts);
		  api_incr_top(L);
		  lua_unlock(L);
          return getstr(ts);
		}


		public static CharPtr lua_pushstring (lua_State L, CharPtr s) {
		  if (s == null) {
			lua_pushnil(L);
            return null;
          }
		  else {
		    TString ts;
		    lua_lock(L);
		    luaC_checkGC(L);
		    ts = luaS_new(L, s);
		    setsvalue2s(L, L.top, ts);
		    api_incr_top(L);
		    lua_unlock(L);
		    return getstr(ts);
		  }
		}


		public static CharPtr lua_pushvfstring (lua_State L, CharPtr fmt,
											  object[] argp) {
		  CharPtr ret;
		  lua_lock(L);
		  luaC_checkGC(L);
		  ret = luaO_pushvfstring(L, fmt, argp);
		  lua_unlock(L);
		  return ret;
		}

        //FIXME: addded, override, see below
		public static CharPtr lua_pushfstring (lua_State L, CharPtr fmt) {
			CharPtr ret;
			lua_lock(L);
			luaC_checkGC(L);
			ret = luaO_pushvfstring(L, fmt, null);
			lua_unlock(L);
			return ret;
		}

		public static CharPtr lua_pushfstring(lua_State L, CharPtr fmt, params object[] p)
		{
			  CharPtr ret;
			  lua_lock(L);
			  luaC_checkGC(L);
			  ret = luaO_pushvfstring(L, fmt, p);
			  lua_unlock(L);
			  return ret;
		}


		public static void lua_pushcclosure (lua_State L, lua_CFunction fn, int n) {
		  lua_lock(L);
		  if (n == 0) {
		    setfvalue(L.top, fn);
		  }
		  else {
		    Closure cl;
		    api_checknelems(L, n);
		    api_check(L, n <= MAXUPVAL, "upvalue index too large");
		    luaC_checkGC(L);
		    cl = luaF_newCclosure(L, n);
		    cl.c.f = fn;
		    L.top -= n;
		    while (n-- != 0)
		      setobj2n(L, cl.c.upvalue[n], L.top + n);
		    setclCvalue(L, L.top, cl);
		  }
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static void lua_pushboolean (lua_State L, int b) {
		  lua_lock(L);
		  setbvalue(L.top, (b != 0) ? 1 : 0);  /* ensure that true is 1 */
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static void lua_pushlightuserdata (lua_State L, object p) {
		  lua_lock(L);
		  setpvalue(L.top, p);
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static int lua_pushthread (lua_State L) {
		  lua_lock(L);
		  setthvalue(L, L.top, L);
		  api_incr_top(L);
		  lua_unlock(L);
		  return (G(L).mainthread == L) ? 1 : 0;
		}



		/*
		** get functions (Lua . stack)
		*/


		public static void lua_getglobal (lua_State L, CharPtr var) {
		  Table reg = hvalue(G(L).l_registry);
		  /*const */TValue gt;  /* global table */
		  lua_lock(L);
		  gt = luaH_getint(reg, LUA_RIDX_GLOBALS);
		  setsvalue2s(L, lua_TValue.inc(ref L.top), luaS_new(L, var));
		  luaV_gettable(L, gt, L.top - 1, L.top - 1);
		  lua_unlock(L);
		}


		public static void lua_gettable (lua_State L, int idx) {
		  StkId t;
		  lua_lock(L);
		  t = index2addr(L, idx);
		  api_checkvalidindex(L, t);
		  luaV_gettable(L, t, L.top - 1, L.top - 1);
		  lua_unlock(L);
		}


		public static void lua_getfield (lua_State L, int idx, CharPtr k) {
		  StkId t;
		  lua_lock(L);
		  t = index2addr(L, idx);
		  api_checkvalidindex(L, t);
		  setsvalue2s(L, L.top, luaS_new(L, k));
		  api_incr_top(L);
          luaV_gettable(L, t, L.top - 1, L.top - 1);
		  lua_unlock(L);
		}


		public static void lua_rawget (lua_State L, int idx) {
		  StkId t;
		  lua_lock(L);
		  t = index2addr(L, idx);
		  api_check(L, ttistable(t), "table expected");
		  setobj2s(L, L.top - 1, luaH_get(hvalue(t), L.top - 1));
		  lua_unlock(L);
		}


		public static void lua_rawgeti (lua_State L, int idx, int n) {
		  StkId t;
		  lua_lock(L);
		  t = index2addr(L, idx);
		  api_check(L, ttistable(t), "table expected");
		  setobj2s(L, L.top, luaH_getint(hvalue(t), n));
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static void lua_rawgetp (lua_State L, int idx, object p) {
		  StkId t;
		  TValue k = new TValue();
		  lua_lock(L);
		  t = index2addr(L, idx);
		  api_check(L, ttistable(t), "table expected");
		  setpvalue(k, (object)(p));
		  setobj2s(L, L.top, luaH_get(hvalue(t), k));
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static void lua_createtable (lua_State L, int narray, int nrec) {
          Table t;
		  lua_lock(L);
		  luaC_checkGC(L);
          t = luaH_new(L);
		  sethvalue(L, L.top, t);
		  api_incr_top(L);
		  if (narray > 0 || nrec > 0)
		    luaH_resize(L, t, narray, nrec);
		  lua_unlock(L);
		}


		public static int lua_getmetatable (lua_State L, int objindex) {
		  TValue obj;
		  Table mt = null;
		  int res;
		  lua_lock(L);
		  obj = index2addr(L, objindex);
		  switch (ttypenv(obj)) {
			case LUA_TTABLE:
			  mt = hvalue(obj).metatable;
			  break;
			case LUA_TUSERDATA:
			  mt = uvalue(obj).metatable;
			  break;
			default:
			  mt = G(L).mt[ttypenv(obj)];
			  break;
		  }
		  if (mt == null)
			res = 0;
		  else {
			sethvalue(L, L.top, mt);
			api_incr_top(L);
			res = 1;
		  }
		  lua_unlock(L);
		  return res;
		}


		public static void lua_getuservalue (lua_State L, int idx) {
		  StkId o;
		  lua_lock(L);
		  o = index2addr(L, idx);
		  api_checkvalidindex(L, o);
		  api_check(L, ttisuserdata(o), "userdata expected");
		  if (uvalue(o).env != null) {
		    sethvalue(L, L.top, uvalue(o).env);
		  } else
		    setnilvalue(L.top);
		  api_incr_top(L);
		  lua_unlock(L);
		}


		/*
		** set functions (stack . Lua)
		*/


		public static void lua_setglobal (lua_State L, CharPtr var) {
		  Table reg = hvalue(G(L).l_registry);
		  /*const */TValue gt;  /* global table */
		  lua_lock(L);
		  api_checknelems(L, 1);
		  gt = luaH_getint(reg, LUA_RIDX_GLOBALS);
		  setsvalue2s(L, lua_TValue.inc(ref L.top), luaS_new(L, var));
		  luaV_settable(L, gt, L.top - 1, L.top - 2);
		  L.top -= 2;  /* pop value and key */
		  lua_unlock(L);
		}


		public static void lua_settable (lua_State L, int idx) {
		  StkId t;
		  lua_lock(L);
		  api_checknelems(L, 2);
		  t = index2addr(L, idx);
		  api_checkvalidindex(L, t);
		  luaV_settable(L, t, L.top - 2, L.top - 1);
		  L.top -= 2;  /* pop index and value */
		  lua_unlock(L);
		}


		public static void lua_setfield (lua_State L, int idx, CharPtr k) {
		  StkId t;
		  lua_lock(L);
		  api_checknelems(L, 1);
		  t = index2addr(L, idx);
		  api_checkvalidindex(L, t);
		  setsvalue2s(L, StkId.inc(ref L.top), luaS_new(L, k));
		  luaV_settable(L, t, L.top - 1, L.top - 2);
		  L.top -= 2;  /* pop value and key */
		  lua_unlock(L);
		}


		public static void lua_rawset (lua_State L, int idx) {
		  StkId t;
		  lua_lock(L);
		  api_checknelems(L, 2);
		  t = index2addr(L, idx);
		  api_check(L, ttistable(t), "table expected");
		  setobj2t(L, luaH_set(L, hvalue(t), L.top-2), L.top-1);
          invalidateTMcache(hvalue(t));
		  luaC_barrierback(L, gcvalue(t), L.top-1);
		  L.top -= 2;
		  lua_unlock(L);
		}


		public static void lua_rawseti (lua_State L, int idx, int n) {
		  StkId t;
		  lua_lock(L);
		  api_checknelems(L, 1);
		  t = index2addr(L, idx);
		  api_check(L, ttistable(t), "table expected");
		  luaH_setint(L, hvalue(t), n, L.top - 1);
		  luaC_barrierback(L, gcvalue(t), L.top-1);
		  StkId.dec(ref L.top);
		  lua_unlock(L);
		}


		public static void lua_rawsetp (lua_State L, int idx, object p) {
		  StkId t;
		  TValue k = new TValue();
		  lua_lock(L);
		  api_checknelems(L, 1);
		  t = index2addr(L, idx);
		  api_check(L, ttistable(t), "table expected");
		  setpvalue(k, (object)(p));
		  setobj2t(L, luaH_set(L, hvalue(t), k), L.top - 1);
		  luaC_barrierback(L, gcvalue(t), L.top - 1);
		  lua_TValue.dec(ref L.top); //FIXME:--
		  lua_unlock(L);
		}


		public static int lua_setmetatable (lua_State L, int objindex) {
		  TValue obj;
		  Table mt;
		  lua_lock(L);
		  api_checknelems(L, 1);
		  obj = index2addr(L, objindex);
		  api_checkvalidindex(L, obj);
		  if (ttisnil(L.top - 1))
			  mt = null;
		  else {
			api_check(L, ttistable(L.top - 1), "table expected");
			mt = hvalue(L.top - 1);
		  }
		  switch (ttypenv(obj)) {
			case LUA_TTABLE: {
			  hvalue(obj).metatable = mt;
			  if (mt != null)
				luaC_objbarrierback(L, gcvalue(obj), mt);
              luaC_checkfinalizer(L, gcvalue(obj), mt); //FIXME: changed, delete space
			  break;
			}
			case LUA_TUSERDATA: {
			  uvalue(obj).metatable = mt;
			  if (mt != null) {
				luaC_objbarrier(L, rawuvalue(obj), mt);
                luaC_checkfinalizer(L, gcvalue(obj), mt);
              }
			  break;
			}
			default: {
			  G(L).mt[ttypenv(obj)] = mt;
			  break;
			}
		  }
		  StkId.dec(ref L.top);
		  lua_unlock(L);
		  return 1;
		}


		public static void lua_setuservalue (lua_State L, int idx) {
		  StkId o;
		  lua_lock(L);
		  api_checknelems(L, 1);
		  o = index2addr(L, idx);
		  api_checkvalidindex(L, o);
		  api_check(L, ttisuserdata(o), "userdata expected");
		  if (ttisnil(L.top - 1))
		    uvalue(o).env = null;
		  else {
		    api_check(L, ttistable(L.top - 1), "table expected");
		    uvalue(o).env = hvalue(L.top - 1);
		    luaC_objbarrier(L, gcvalue(o), hvalue(L.top - 1));
		  }
		  lua_TValue.dec(ref L.top);
		  lua_unlock(L);
		}


		/*
		** `load' and `call' functions (run Lua code)
		*/


		public static void checkresults(lua_State L, int na, int nr) {
			api_check(L, (nr) == LUA_MULTRET || (L.ci.top - L.top >= (nr) - (na)), 
		      "results from function overflow current stack size");
		}
			

		public static int lua_getctx (lua_State L, ref int ctx) {
		  if ((L.ci.callstatus & CIST_YIELDED) != 0) {
			ctx = L.ci.u.c.ctx; //FIXME: no if (ctx != null)
			return L.ci.u.c.status;
		  }
		  else return LUA_OK;
		}


		public static void lua_callk (lua_State L, int nargs, int nresults, int ctx,
                        lua_CFunction k) {
		  StkId func;
		  lua_lock(L);

		  api_check(L, k == null || isLua(L.ci)==0, 
		    "cannot use continuations inside hooks");
		  api_checknelems(L, nargs+1);
          api_check(L, L.status == LUA_OK, "cannot do calls on non-normal thread");
		  checkresults(L, nargs, nresults);
		  func = L.top - (nargs+1);
		  if (k != null && L.nny == 0) {  /* need to prepare continuation? */
		    L.ci.u.c.k = k;  /* save continuation */
		    L.ci.u.c.ctx = ctx;  /* save context */
		    luaD_call(L, func, nresults, 1);  /* do the call */
		  }
		  else  /* no continuation or no yieldable */
		    luaD_call(L, func, nresults, 0);  /* just do the call */

		  adjustresults(L, nresults);
		  lua_unlock(L);
		}



		/*
		** Execute a protected call.
		*/
		public class CallS {  /* data to `f_call' */
		  public StkId func;
			public int nresults;
		};


		static void f_call (lua_State L, object ud) {
		  CallS c = ud as CallS;
		  luaD_call(L, c.func, c.nresults, 0);
		}



		public static int lua_pcallk (lua_State L, int nargs, int nresults, int errfunc,
                        int ctx, lua_CFunction k) {
		  CallS c = new CallS();
		  int status;
		  ptrdiff_t func;
		  lua_lock(L);
		  api_check(L, k == null || isLua(L.ci)==0,
		    "cannot use continuations inside hooks");
		  api_checknelems(L, nargs+1);
          api_check(L, L.status == LUA_OK, "cannot do calls on non-normal thread");
		  checkresults(L, nargs, nresults);
		  if (errfunc == 0)
			func = 0;
		  else {
			StkId o = index2addr(L, errfunc);
			api_checkvalidindex(L, o);
			func = savestack(L, o);
		  }
		  c.func = L.top - (nargs+1);  /* function to be called */
		  if (k == null || L.nny > 0) {  /* no continuation or no yieldable? */
		    c.nresults = nresults;  /* do a 'conventional' protected call */
		    status = luaD_pcall(L, f_call, c, savestack(L, c.func), func);
		  }
		  else {  /* prepare continuation (call is already protected by 'resume') */
		    CallInfo ci = L.ci;
		    ci.u.c.k = k;  /* save continuation */
		    ci.u.c.ctx = ctx;  /* save context */
		    /* save information for error recovery */
		    ci.u.c.extra = savestack(L, c.func);
		    ci.u.c.old_allowhook = L.allowhook;
		    ci.u.c.old_errfunc = L.errfunc;
		    L.errfunc = func;
		    /* mark that function may do error recovery */
		    ci.callstatus |= CIST_YPCALL;
		    luaD_call(L, c.func, nresults, 1);  /* do the call */
		    ci.callstatus &= (byte)((~CIST_YPCALL) & 0xff);
		    L.errfunc = ci.u.c.old_errfunc;
		    status = LUA_OK;  /* if it is here, there were no errors */
		  }

		  adjustresults(L, nresults);
		  lua_unlock(L);
		  return status;
		}


		public static int lua_load (lua_State L, lua_Reader reader, object data,
							  CharPtr chunkname, CharPtr mode) {
		  ZIO z = new ZIO();
		  int status;
		  lua_lock(L);
		  if (chunkname == null) chunkname = "?";
		  luaZ_init(L, z, reader, data);
		  status = luaD_protectedparser(L, z, chunkname, mode);
		  if (status == LUA_OK) {  /* no errors? */
		    LClosure f = clLvalue(L.top - 1);  /* get newly created function */
		    if (f.nupvalues == 1) {  /* does it have one upvalue? */
		      /* get global table from registry */
		      Table reg = hvalue(G(L).l_registry);
		      /*const*/ TValue gt = luaH_getint(reg, LUA_RIDX_GLOBALS);
		      /* set global table as 1st upvalue of 'f' (may be LUA_ENV) */
		      setobj(L, f.upvals[0].v, gt);
		      luaC_barrier(L, f.upvals[0], gt);
		    }
		  }
		  lua_unlock(L);
		  return status;
		}


		public static int lua_dump (lua_State L, lua_Writer writer, object data) {
		  int status;
		  TValue o;
		  lua_lock(L);
		  api_checknelems(L, 1);
		  o = L.top - 1;
		  if (isLfunction(o))
			status = luaU_dump(L, getproto(o), writer, data, 0);
		  else
			status = 1;
		  lua_unlock(L);
		  return status;
		}


		public static int  lua_status (lua_State L) {
		  return L.status;
		}


		/*
		** Garbage-collection function
		*/

		public static int lua_gc (lua_State L, int what, int data) {
		  int res = 0;
		  global_State g;
		  lua_lock(L);
		  g = G(L);
		  switch (what) {
			case LUA_GCSTOP: {
			  g.gcrunning = 0;
			  break;
			}
			case LUA_GCRESTART: {
			  luaE_setdebt(g, 0);
      		  g.gcrunning = 1;
			  break;
			}
			case LUA_GCCOLLECT: {
			  luaC_fullgc(L, 0);
			  break;
			}
			case LUA_GCCOUNT: {
			  /* GC values are expressed in Kbytes: #bytes/2^10 */
			  res = cast_int(gettotalbytes(g) >> 10);
			  break;
			}
			case LUA_GCCOUNTB: {
			  res = cast_int(gettotalbytes(g) & 0x3ff);
			  break;
			}
			case LUA_GCSTEP: {
		      if (g.gckind == KGC_GEN) {  /* generational mode? */
		  	  	res = (g.lastmajormem == 0)?1:0;  /* 1 if will do major collection */
		        luaC_forcestep(L);  /* do a single step */
		      }
		      else {
		        while (data-- >= 0) {
		          luaC_forcestep(L);
		          if (g.gcstate == GCSpause) {  /* end of cycle? */
		            res = 1;  /* signal it */
		            break;
		          }
		        }
		      }
		      break;
			}
			case LUA_GCSETPAUSE: {
			  res = g.gcpause;
			  g.gcpause = data;
			  break;
			}
		    case LUA_GCSETMAJORINC: {
		      res = g.gcmajorinc;
		      g.gcmajorinc = data;
		      break;
		    }
			case LUA_GCSETSTEPMUL: {
			  res = g.gcstepmul;
			  g.gcstepmul = data;
			  break;
			}
		    case LUA_GCISRUNNING: {
		  	  res = g.gcrunning;
		      break;
		    }
		    case LUA_GCGEN: {  /* change collector to generational mode */
		      luaC_changemode(L, KGC_GEN);
		      break;
		    }
		    case LUA_GCINC: {  /* change collector to incremental mode */
		      luaC_changemode(L, KGC_NORMAL);
		      break;
		    }
			default: res = -1;  /* invalid option */
				break; //FIXME: added
		  }
		  lua_unlock(L);
		  return res;
		}



		/*
		** miscellaneous functions
		*/


		public static int lua_error (lua_State L) {
		  lua_lock(L);
		  api_checknelems(L, 1);
		  luaG_errormsg(L);
		  lua_unlock(L);
		  return 0;  /* to avoid warnings */
		}


		public static int lua_next (lua_State L, int idx) {
		  StkId t;
		  int more;
		  lua_lock(L);
		  t = index2addr(L, idx);
		  api_check(L, ttistable(t), "table expected");
		  more = luaH_next(L, hvalue(t), L.top - 1);
		  if (more != 0) {
			api_incr_top(L);
		  }
		  else  /* no more elements */
			StkId.dec(ref L.top);  /* remove key */
		  lua_unlock(L);
		  return more;
		}


		public static void lua_concat (lua_State L, int n) {
		  lua_lock(L);
		  api_checknelems(L, n);
		  if (n >= 2) {
			luaC_checkGC(L);
			luaV_concat(L, n);
		  }
		  else if (n == 0) {  /* push empty string */
			setsvalue2s(L, L.top, luaS_newlstr(L, "", 0));
			api_incr_top(L);
		  }
		  /* else n == 1; nothing to do */
		  lua_unlock(L);
		}


		public static void lua_len (lua_State L, int idx) {
		  StkId t;
		  lua_lock(L);
		  t = index2addr(L, idx);
		  luaV_objlen(L, L.top, t);
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static lua_Alloc lua_getallocf (lua_State L, ref object ud) {
		  lua_Alloc f;
		  lua_lock(L);
		  if (ud != null) ud = G(L).ud;
		  f = G(L).frealloc;
		  lua_unlock(L);
		  return f;
		}


		public static void lua_setallocf (lua_State L, lua_Alloc f, object ud) {
		  lua_lock(L);
		  G(L).ud = ud;
		  G(L).frealloc = f;
		  lua_unlock(L);
		}


		public static object lua_newuserdata(lua_State L, uint size)
		{
			Udata u;
			lua_lock(L);
			luaC_checkGC(L);
			u = luaS_newudata(L, size, null);
			setuvalue(L, L.top, u);
			api_incr_top(L);
			lua_unlock(L);
			return u.user_data;
		}

        //FIXME:added
		public static object lua_newuserdata(lua_State L, Type t)
		{
			Udata u;
			lua_lock(L);
			luaC_checkGC(L);
			u = luaS_newudata(L, t, null); //FIXME:???removed, getcurrenv(L)->null
			setuvalue(L, L.top, u);
			api_incr_top(L);
			lua_unlock(L);
			return u.user_data;
		}

		static CharPtr aux_upvalue (StkId fi, int n, ref TValue val,
                                    ref GCObject owner) {
		  switch (ttype(fi)) {
		    case LUA_TCCL: {  /* C closure */
		      CClosure f = clCvalue(fi);
		  	  if (!(1 <= n && n <= f.nupvalues)) return null;
			  val = f.upvalue[n-1];
              /*if (owner)*/ owner = obj2gco(f); //FIXME: changed
			  return "";
		    }
		    case LUA_TLCL: {  /* Lua closure */
      		  LClosure f = clLvalue(fi);
		      TString name;
		      Proto p = f.p;
		      if (!(1 <= n && n <= p.sizeupvalues)) return null;
		      val = f.upvals[n-1].v;
		      /*if (owner)*/ owner = obj2gco(f.upvals[n - 1]); //FIXME: changed
		      name = p.upvalues[n-1].name;
		      return (name == null) ? "" : getstr(name);  /* no debug information? */
		    }
            default: return null;  /* not a closure */
		  }
		}


		public static CharPtr lua_getupvalue (lua_State L, int funcindex, int n) {
		  CharPtr name;
		  TValue val = null;  /* to avoid warnings */
		  lua_lock(L);
		  GCObject null_ = new GCObject(); //FIXME:added
		  name = aux_upvalue(index2addr(L, funcindex), n, ref val, ref null_); //FIXME:changed
		  if (name != null) {
			setobj2s(L, L.top, val);
			api_incr_top(L);
		  }
		  lua_unlock(L);
		  return name;
		}


		public static CharPtr lua_setupvalue (lua_State L, int funcindex, int n) {
		  CharPtr name;
		  TValue val = null;  /* to avoid warnings */
		  GCObject owner = null;  /* to avoid warnings */
		  StkId fi;
		  lua_lock(L);
		  fi = index2addr(L, funcindex);
		  api_checknelems(L, 1);
		  name = aux_upvalue(fi, n, ref val, ref owner);
		  if (name != null) {
			StkId.dec(ref L.top);
			setobj(L, val, L.top);
			luaC_barrier(L, owner, L.top);
		  }
		  lua_unlock(L);
		  return name;
		}


		private static UpValRef getupvalref (lua_State L, int fidx, int n, ref LClosure pf) { //FIXME:???ref ? array ?
		  LClosure f;
		  StkId fi = index2addr(L, fidx);
		  api_check(L, ttisLclosure(fi), "Lua function expected");
		  f = clLvalue(fi);
		  api_check(L, (1 <= n && n <= f.p.sizeupvalues), "invalid upvalue index");
		  pf = f; //FIXME:changed, no if (pf != null)
		  return new UpValRef(f.upvals, n - 1);  /* get its upvalue pointer */
		}


		private static object lua_upvalueid (lua_State L, int fidx, int n) {
		  StkId fi = index2addr(L, fidx);
		  switch (ttype(fi)) {
		    case LUA_TLCL: {  /* lua closure */
		  	  LClosure null_ = null; //FIXME:added
		  	  return getupvalref(L, fidx, n, ref null_).get();
		    }
		    case LUA_TCCL: {  /* C closure */
		      CClosure f = clCvalue(fi);
		      api_check(L, 1 <= n && n <= f.nupvalues, "invalid upvalue index");
		      return f.upvalue[n - 1];
		    }
		    default: {
		      api_check(L, 0, "closure expected");
		      return null;
		    }
		  }
		}


		public static void lua_upvaluejoin (lua_State L, int fidx1, int n1,
		                                            int fidx2, int n2) {
		  LClosure f1 = null;
		  LClosure null_=null;//FIXME:added
		  UpValRef up1 = getupvalref(L, fidx1, n1, ref f1);
		  UpValRef up2 = getupvalref(L, fidx2, n2, ref null_);
		  up1.set(up2.get());
		  luaC_objbarrier(L, f1, up2.get());
		}

	}
}

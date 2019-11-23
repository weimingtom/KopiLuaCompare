9:27 2019/11/22
lapi.c
9:32 2019/11/22
lauxlib.c
9:33 2019/11/22
lauxlib.h
9:44 2019/11/22
lbaselib.c
9:17 2019/11/23
lbitlib.c
9:21 2019/11/23
lcode.c
9:25 2019/11/23
lcorolib.c
10:07 2019/11/23
ldblib.c
10:15 2019/11/23
ldo.c
10:21 2019/11/23
ldump.c
10:28 2019/11/23
lfunc.c
10:30 2019/11/23
lfunc.h
11:00 2019/11/23
lgc.c
11:03 2019/11/23
linit.c


















------------------------------

1. 
		private static CharPtr b_str2int (CharPtr s, int base_, ref lua_Integer pn) {
----->		  s = new CharPtr(s); //FIXME:???

------------------------------

2. 
string ==, ???may be bug


		private static int luaB_tonumber (lua_State L) {
		  if (lua_isnoneornil(L, 2)) {  /* standard conversion? */
		    luaL_checkany(L, 1);
		    if (lua_type(L, 1) == LUA_TNUMBER) {  /* already a number? */
		      lua_settop(L, 1);  /* yes; return it */
		      return 1;
		    }
		    else {
		      uint l;
		      CharPtr s = lua_tolstring(L, 1, out l);
--------->		      if (s != null && lua_strtonum(L, s) == l + 1)
		        return 1;  /* successful conversion to number */
		      /* else not a number */
		    }
		  }
		  else {
		    uint l;
		    CharPtr s;
		    lua_Integer n = 0;  /* to avoid warnings */
		    int base_ = luaL_checkint(L, 2);
		    luaL_checktype(L, 1, LUA_TSTRING);  /* before 'luaL_checklstring'! */
		    s = luaL_checklstring(L, 1, out l);
		    luaL_argcheck(L, 2 <= base_ && base_ <= 36, 2, "base out of range");
--------->		    if (b_str2int(s, base, ref n) == s + l) {
		      lua_pushinteger(L, n);
		      return 1;
		    }  /* else not a number */
		  }  /* else not a number */
		  lua_pushnil(L);  /* not a number */
		  return 1;
		}

------------------------------


		public static CClosure luaF_newCclosure (lua_State L, int n) {
		  GCObject o = luaC_newobj<Closure>(L, LUA_TCCL, sizeCclosure(n));
		  CClosure c = gco2ccl(o);
		  c.nupvalues = cast_byte(n);
--->		  c.c.upvalue = new TValue[n]; //FIXME:added???
--->		  for (int i = 0; i < n; i++)  //FIXME:added???
--->			  c.c.upvalue[i] = new lua_TValue(); //FIXME:??? //FIXME:added???
		  return c;
		}


		public static Closure luaF_newLclosure (lua_State L, int n) {
		  Closure c = luaC_newobj<Closure>(L, LUA_TLCL, sizeLclosure(n)).cl;
		  c.l.p = null;
		  c.l.nupvalues = cast_byte(n);
--->		  c.l.upvals = new UpVal[n]; //FIXME:added???
--->		  /*
--->		  for (int i = 0; i < n; i++) //FIXME:added???
--->			  c.l.upvals[i] = new UpVal(); //FIXME:??? //FIXME:added???
--->		  while (n > 0) c.l.upvals[n] = null; //FIXME:added??? while (n--) c->l.upvals[n] = NULL;
--->		  */
		  while (n-- != 0) c.l.upvals[n] = null;
		  return c;
		}
		
		
-------------------------------

public->private

--->		private static void luaC_upvalbarrier_ (lua_State L, UpVal uv) {
		
		
		
		
-------------------------------

		  new luaL_Reg(LUA_MATHLIBNAME, luaopen_math),
		  new luaL_Reg(LUA_DBLIBNAME, luaopen_debug),
		  new luaL_Reg(LUA_UTF8LIBNAME, luaopen_utf8),
--->		//#if defined(LUA_COMPAT_BITLIB)
--->		  new luaL_Reg(LUA_BITLIBNAME, luaopen_bit32),	
--->		//#endif	
		
-------------------------------
	


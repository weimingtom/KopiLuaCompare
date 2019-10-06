FIXME: loslib.c, os_date and os_time not sync

-------------------


		public static lua_Integer luaV_mod (lua_State L, lua_Integer x, lua_Integer y) {
		  if (cast_unsigned(y) + 1 <= 1U) {  /* special cases: -1 or 0 */
		    if (y == 0)
		      luaG_runerror(L, "attempt to perform 'n%%0'");
		    else  /* -1 */
		      return 0;   /* avoid overflow with 0x80000... */
		  }
		  else {
		    lua_Integer r = x % y;
		    if (r == 0 || (x ^ y) >= 0)
		      return r;
		    else
		      return r + y;  /* correct 'mod' for negative case */
		  }
---------->		  return 0; //FIXME:
		}

-------------------

public static lua_Integer luaV_div (lua_State L, lua_Integer x, lua_Integer y) {
		  if (cast_unsigned(y) + 1 <= 1U) {  /* special cases: -1 or 0 */
		    if (y == 0)
		      luaG_runerror(L, "attempt to divide by zero");
		    else  /* -1 */
		      return -x;   /* avoid overflow with 0x80000... */
		  }
		  else {
		    lua_Integer d = x / y;  /* perform division */
		    if ((x ^ y) >= 0 || x % y == 0)   /* same signal or no rest? */
		      return d;
		    else
		      return d - 1;  /* correct 'div' for negative case */
		  }
-------->		  return 0; //FIXME:
		}


-------------------

		public const int CHAR_BIT = 8;
		public const int DBL_MAX_EXP = 1024;
		public const int INT_MAX = 0x7fffffff;
		
-------------------

		//FIXME:changed, see intop
		//FIXME:???Lua_Number
		public static int intop_plus(lua_Integer v1, lua_Integer v2) 
			{ return (int)((uint)(v1) + (uint)(v2));}
		public static int intop_minus(lua_Integer v1, lua_Integer v2) 
			{ return (int)((uint)(v1) - (uint)(v2));}
		public static int intop_mul(lua_Integer v1, lua_Integer v2) 
			{ return (int)((uint)(v1) * (uint)(v2));}

-------------------

		  s = new CharPtr(s); //FIXME:added	

-------------------

		public const int CHAR_BIT = 8;

-------------------

		  lua_Integer i = 0; lua_Number n;
ref -> out, no i=0 ------->		  if (luaO_str2int(s, len, ref i) != 0) {  /* try as an integer */
		    setivalue(L.top, i);
		  }
		  

---------------------
FIXME:???Int32, Int64
using lua_Integer = System.Int32;




---------------------
/* reasonable limit to avoid arithmetic overflow and strings too big */
//#if INT_MAX / 2 <= 0x10000000
---------->public const uint MAXSIZE = (uint)(int.MaxValue / 2); //FIXME: //((size_t)(INT_MAX / 2));
//#else
//#define MAXSIZE		((size_t)0x10000000)
//#endif





---------------------

10:46 2019/10/1
lapi.c
10:54 2019/10/1
lauxlib.c
11:00 2019/10/1
lauxlib.h
11:01 2019/10/1
lbaselib.c

14:19
lbitlib.c
14:23
lcode.c
14:37
lcode.h
14:38
ldblib.c
14:42
ldebug.c
14:55
ldebug.h
14:56
ldo.c
14:58
ldump.c
15:01
lgc.c
15:02
liolib.c
15:09
llex.c
15:18
llex.h
15:20
llimits.h
15:25
lmathlib.c
15:40
lobject.c
15:46
lobject.h
15:56
lopcodes.c
15:58
lopcodes.h
15:59
loslib.c
16:05
lparser.c
16:15
lparser.h
16:17
lstate.c
16:20
lstring.c
16:21
lstrlib.c
16:30
ltable.c
16:50
ltable.h
16:51
ltablib.c
16:53
ltm.c
16:56
ltm.h
16:57
lua.h
17:01
luac.c
17:03
lundump.c
17:05
lvm.c
17:25
lvm.h
17:29
luaconf.h



--------------------------------
loslib.c:, not sync and same

static int os_date (lua_State *L) {
  const char *s = luaL_optstring(L, 1, "%c");
--------->  time_t t = luaL_opt(L, (time_t)luaL_checkinteger, 2, time(NULL));
  struct tm tmr, *stm;
  if (*s == '!') {  /* UTC? */
    stm = l_gmtime(&t, &tmr);
    s++;  /* skip `!' */
  }
  else
    stm = l_localtime(&t, &tmr);
  if (stm == NULL)  /* invalid date? */
    lua_pushnil(L);
  else if (strcmp(s, "*t") == 0) {
    lua_createtable(L, 0, 9);  /* 9 = number of fields */
    setfield(L, "sec", stm->tm_sec);
    setfield(L, "min", stm->tm_min);
    setfield(L, "hour", stm->tm_hour);
    setfield(L, "day", stm->tm_mday);
    setfield(L, "month", stm->tm_mon+1);
    setfield(L, "year", stm->tm_year+1900);
    setfield(L, "wday", stm->tm_wday+1);
    setfield(L, "yday", stm->tm_yday+1);
    setboolfield(L, "isdst", stm->tm_isdst);
  }
  else {
    char cc[4];
    luaL_Buffer b;
    cc[0] = '%';
    luaL_buffinit(L, &b);
    while (*s) {
      if (*s != '%')  /* no conversion specifier? */
        luaL_addchar(&b, *s++);
      else {
        size_t reslen;
        char buff[200];  /* should be big enough for any conversion result */
        s = checkoption(L, s + 1, cc);
        reslen = strftime(buff, sizeof(buff), cc, stm);
        luaL_addlstring(&b, buff, reslen);
      }
    }
    luaL_pushresult(&b);
  }
  return 1;
}


		private static int os_time (lua_State L) {
		  DateTime t;
		  if (lua_isnoneornil(L, 1))  /* called without args? */
			t = DateTime.Now;  /* get current time */
		  else {
			luaL_checktype(L, 1, LUA_TTABLE);
			lua_settop(L, 1);  /* make sure table is at the top */
			int sec = getfield(L, "sec", 0);
			int min = getfield(L, "min", 0);
			int hour = getfield(L, "hour", 12);
			int day = getfield(L, "day", -1);
			int month = getfield(L, "month", -1) - 1;
			int year = getfield(L, "year", -1) - 1900;
			int isdst = getboolfield(L, "isdst");	// todo: implement this - mjf
			t = new DateTime(year, month, day, hour, min, sec);
		  }
		  lua_pushinteger(L, t.Ticks);
		  return 1;
		}
		
		
-----------------------------

lvm.c

      vmcase(OP_EQ,
        TValue *rb = RKB(i);
        TValue *rc = RKC(i);
        Protect(
cast_int not do ???? -------->          if (cast_int(luaV_equalobj(L, rb, rc)) != GETARG_A(i))

		  
		  
























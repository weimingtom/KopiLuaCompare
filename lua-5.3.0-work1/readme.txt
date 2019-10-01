FIXME: loslib.c, os_date and os_time not sync


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

		  
		  
























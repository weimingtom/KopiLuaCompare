???luaC_checkGC, luaC_condGC not used???
lcode.c, ////FIXME: no star

setallfields not sync
os_time not implemented

----------------
14:24 2019/12/13
ldo.c
14:39 2019/12/13
lapi.c
14:50 2019/12/13
lauxlib.c
14:53 2019/12/13
lbaselib.c

(TODO) lcode.c

14:54 2019/12/13
lcode.h
14:55 2019/12/13
lcorolib.c
15:05 2019/12/13
ldebug.c
15:06 2019/12/13
ldo.h
15:32 2019/12/13
lgc.c
15:33 2019/12/13
lgc.h
15:47 2019/12/13
liolib.c
15:49 2019/12/13
llex.c
15:50 2019/12/13
llex.h
16:23 2019/12/13
lobject.c
16:30 2019/12/13
loslib.c
16:31 2019/12/13
lparser.c
16:34 2019/12/13
lparser.h
16:37 2019/12/13
lstate.h
16:51 2019/12/13
lstrlib.c
16:57 2019/12/13
ltablib.c
16:58 2019/12/13
ltm.c
17:05 2019/12/13
ltm.h
17:06 2019/12/13
lua.h
17:07 2019/12/13
luaconf.h
17:16 2019/12/13
lvm.c
17:23 2019/12/13
lvm.h
17:54 2019/12/13
lcode.c

---------------------
???

using l_signalT = System.Byte;

---------------------
convert (Instruction)

i[0] = (Instruction)CREATE_ABC(OpCode.OP_TEST, GETARG_B(i), 0, GETARG_C(i));

-------------------

/*
		** Set all fields from structure 'tm' in the table on top of the stack
		*/
		private static void setallfields (lua_State L, DateTime stm) {
			throw new Exception();
			/*
		  setfield(L, "sec", stm.tm_sec);
		  setfield(L, "min", stm.tm_min);
		  setfield(L, "hour", stm.tm_hour);
		  setfield(L, "day", stm.tm_mday);
		  setfield(L, "month", stm.tm_mon + 1);
		  setfield(L, "year", stm.tm_year + 1900);
		  setfield(L, "wday", stm.tm_wday + 1);
		  setfield(L, "yday", stm.tm_yday + 1);
		  setboolfield(L, "isdst", stm.tm_isdst);*/
		}
		
-----------------------

		private static int os_time (lua_State L) {
		  DateTime t;
		  if (lua_isnoneornil(L, 1))  /* called without args? */
			t = DateTime.Now;  /* get current time */
		  else {
			luaL_checktype(L, 1, LUA_TTABLE);
			lua_settop(L, 1);  /* make sure table is at the top */
			int sec = getfield(L, "sec", 0, 0);
			int min = getfield(L, "min", 0, 0);
			int hour = getfield(L, "hour", 12, 0);
			int day = getfield(L, "day", -1, 0);
			int month = getfield(L, "month", -1, 1);
			int year = getfield(L, "year", -1, 1900);
			int isdst = getboolfield(L, "isdst");	// todo: implement this - mjf
			t = new DateTime(year, month, day, hour, min, sec);
			throw new Exception();
--->			//FIXME:not implemented//setallfields(L, &ts);  /* update fields with normalized values */
		  }
		  lua_pushinteger(L, (int)(t.Ticks));
		  return 1;
		}
		
----------------

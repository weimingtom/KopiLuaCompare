todo:lgc.c


lmem.c???

-------------

9:20 2019-6-7
lapi.c
9:25 2019-6-7
lauxlib.c

9:27 2019-6-7
lopcodes.c
lzio.c

9:30 2019-6-7
lbaselib.c
9:32 2019-6-7
lcorolib.c
9:36 2019-6-7
ldblib.c
9:41 2019-6-7
ldebug.c
9:48 2019-6-7
ldo.c
9:51 2019-6-7
ldump.c
9:54 2019-6-7
lfunc.c
9:58 2019-6-7
lfunc.h
10:00 2019-6-7
lgc.h
10:04 2019-6-7
llex.c
10:07 2019-6-7
llimits.h
10:10 2019-6-7
lmathlib.c
10:12 2019-6-7
lmem.c???
10:22 2019-6-7
loadlib.c


//------------------------------

11:31 2019/6/9
lobject.h
11:34 2019/6/9
loslib.c
12:09 2019/6/9
lparser.c
12:12 2019/6/9
lparser.h
12:15 2019/6/9
lstate.c
12:21 2019/6/9
lstate.h
12:28 2019/6/9
lstring.c  //luaS_newudata(lua_State L, Type t, Table e)????
12:30 2019/6/9
lstring.h
12:35 2019/6/9
lstrlib.c
12:39 2019/6/9
ltable.c
12:41 2019/6/9
lua.c
13:49 2019/6/9
lua.h
13:55 2019/6/9
luaconf.h
14:01 2019/6/9
lundump.c
14:03 2019/6/9
lundump.h
14:15 2019/6/9
lvm.c














---------------------------


lobject.h

/* check whether a number is valid (useful only for NaN trick) */
//public static void luai_checknum(lua_State L, TValue o, luai_checknum_func c)	{ /* empty */ } //FIXME:???
		
		

----------------------------


//FIXME:???not implemented
private static LX fromstate(lua_State L) { 
 throw new Exception("not implemented"); //FIXME:???
 return /*((LX)((lu_byte[])(L) - offsetof(LX, l)))*/ null; 
} 


------------------------------
����ֵ�޷�ת��Ϊbyte

ci.callstatus &= ~CIST_HOOKYIELD  /* erase mark */
->
ci.callstatus &= (byte)((~CIST_HOOKYIELD) & 0xff);  /* erase mark */

------------------------------
����תָ��

		delegate lua_State lua_newstate_delegate (lua_Alloc f, object ud);
		private static uint makeseed (lua_State L) {
		  CharPtr buff = new CharPtr(new char[4 * GetUnmanagedSize(typeof(uint))]);
		  uint h = luai_makeseed();
		  int p = 0;
		  addbuff(buff, p, L);  /* heap variable */
		  addbuff(buff, p, h);  /* local variable */
		  addbuff(buff, p, luaO_nilobject);  /* global variable */
		  lua_newstate_delegate _d = lua_newstate;
		  addbuff(buff, p, Marshal.GetFunctionPointerForDelegate(_d));  /* public function */
		  lua_assert(p == buff.chars.Length);
		  return luaS_hash(buff, (uint)p, h);
		}
		
-------------------------------

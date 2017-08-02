04:58 2017-07-28
lapi.c
	private static UpValRef getupvalref (lua_State L, int fidx, int n, LClosure[] pf) { //FIXME:???ref ? array ?
lapi.h
05:20 2017-07-28
lauxlib.c
05:21 2017-07-28
lauxlib.h
	#if defined(LUA_COMPAT_MODULE)

21:55 2017-07-28
lbaselib.c

21:18 2017-07-29
lbitlib.c

03:51 2017-08-01
lcode.c
03:52 2017-08-01
lcode.h
lcorolib.c
03:58 2017-08-01
lctype.c
	#if !LUA_USE_CTYPE	///* { */
04:09 2017-08-01
lctype.h
	#define LUA_USE_CTYPE	0
	#if !defined(LUA_USE_CTYPE)
04:13 2017-08-01
ldblib.c
04:31 2017-08-01
ldebug.c
	StkId pos = new StkId()= 0;  /* to avoid warnings */
	StkId pos = new StkId() = 0;  /* to avoid warnings */
04:32 2017-08-01
ldebug.h
04:45 2017-08-01
ldo.c
	Cfunc:
ldo.h

15:03 2017/8/2
ldump.c
lfunc.c
lfunc.h
15:25 2017/8/2
lgc.c
15:31 2017/8/2
lgc.h
	public static void luaC_condGC(L,c) {
			{if (G(L).GCdebt > 0) {c;}; condchangemem(L);} } //FIXME:???macro
		public static void luaC_checkGC(lua_State L) {luaC_condGC(L, ()=>{luaC_step(L);}} 		//FIXME: macro in {}
15:32 2017/8/2
linit.c































	
	
	
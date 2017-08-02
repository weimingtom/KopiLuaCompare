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

20:36 2017-08-02
liolib.c
	LStream p = (LStream)lua_newuserdata(L, typeof(LStream));->typeof(LStream)
	private static int read_number (lua_State L, Stream f) {
	  //lua_Number d; //FIXME:???
	  object[] parms = { (object)(double)0.0 }; //FIXME:???
	private const int MAX_SIZE_T = (~(size_t)0);
	//
	int[] mode = { SEEK_SET, SEEK_CUR, SEEK_END }; //FIXME: ???static const???
	CharPtr[] modenames = { "set", "cur", "end", null }; //FIXME: ???static const???
	//Flush->fflush()
	private static int io_flush (lua_State L) {
		int result = 1;//FIXME: added
		try {getiofile(L, IO_OUTPUT).Flush();} catch {result = 0;}//FIXME: added
	  return luaL_fileresult(L, result, null); //FIXME: changed
	}
	private static int f_flush (lua_State L) {
		int result = 1;//FIXME: added
		try {tofile(L).Flush();} catch {result = 0;} //FIXME: added
		return luaL_fileresult(L, result, null); //FIXME: changed
	}	



02:07 2017-08-03
llex.c
	ls.decpoint = '.'; // (cv ? cv.decimal_point[0] : '.'); //getlocaledecpoint() //FIXME:changed
	UCHAR_MAX
02:09 2017-08-03
llex.h
	public static lua_Number cast_num(i) { return (lua_Number)i; } //FIXME:???remove?
	public static int cast_int(i) { return (int)i; } //FIXME:???remove?
	public static byte cast_uchar(i) { return (byte)(i)); } //FIXME:???remove?
02:24 2017-08-03
llimits.h
	//------------------>FIXME: below ignore???, TODO
	//#define condchangemem(L)  \
	//	((void)(!(G(L)->gcrunning) || (luaC_fullgc(L, 0), 1)))
02:28 2017-08-03
lmathlib.c
	//#if defined(LUA_COMPAT_LOG10)
			private static int math_log10 (lua_State L) {
	#if defined(LUA_COMPAT_LOG10)
			  new luaL_Reg("log10", math_log10),
	#endif
02:29 2017-08-03
lmem.c
	//FIXME: not sync, no gc below
02:30 2017-08-03
lmem.h
02:48 2017-08-03
loadlib.c
	#if defined(LUA_COMPAT_MODULE)
			  new luaL_Reg("seeall", ll_seeall),
	#endif
	#if defined(LUA_COMPAT_MODULE)
			  new luaL_Reg("module", ll_module),
	#endif
	#if defined(LUA_COMPAT_LOADERS)
			  lua_pushvalue(L, -1);  /* make a copy of 'searchers' table */
			  lua_setfield(L, -3, "loaders");  /* put it in field `loaders' */
	#endif





















	
	
	
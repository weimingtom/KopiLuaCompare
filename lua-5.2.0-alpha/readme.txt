21:08 2017-07-12
lapi.c
lapi.h

18:49 2017-07-13
lauxlib.c
18:59 2017-07-13
lauxlib.h

21:50 2017-07-14
lbaselib.c
21:59 2017-07-14
lbitlib.c

04:47 2017-07-15
lcode.c
04:48 2017-07-15
lcode.h

8:55 2017/7/15
(TODO)lcorolib.c 
lctype.c
lctype.h
9:08 2017/7/15
ldblib.c
9:29 2017/7/15
ldebug.c
ldebug.h

10:02 2017/7/15
ldo.c
ldo.h
10:07 2017/7/15
ldump.c

10:29 2017/7/15
lfunc.c
lfunc.h

16:47 2017-07-15
lgc.c //many changes
17:01 2017-07-15
lgc.h //many changes

19:40 2017-07-15
linit.c
19:58 2017-07-15
liolib.c
20:16 2017-07-15
llex.c
20:17 2017-07-15
llex.h

00:16 2017-07-16
llimits.h
00:27 2017-07-16
lmathlib.c
00:30 2017-07-16
lmem.c
	luaM_realloc_ not sync, no gc
00:31 2017-07-16
lmem.h
00:49 2017-07-16
loadlib.c
	LUA_COMPAT_MODULE


09:52 2017-07-16
lobject.c
10:18 2017-07-16
lobject.h
10:21 2017-07-16
lopcodes.c
10:25 2017-07-16
lopcodes.h

13:08 2017-07-16
loslib.c
13:29 2017-07-16
lparser.c
13:43 2017-07-16
lparser.h


15:49 2017-07-16
lstate.c
	LG l = (LG)f(typeof(LG)); //FIXME:(LG)(f(ud, null, LUA_TTHREAD, (uint)(GetUnmanagedSize(typeof(LG))))); //FIXME:???LUA_TTHREAD
16:03 2017-07-16
lstate.h
	public TString[] tmname = new TString[TM_N];  /* array with tag-method names */ //FIXME:not init with new TString()
	public Table[] mt = new Table[LUA_NUMTAGS];  /* metatables for basic types */
		changed to LUA_NUMTAGS, !!!NOTE!!!,where init with new Table() (maybe for loop)
16:06 2017-07-16
lstring.c
16:08 2017-07-16
lstring.h

20:12 2017-07-16
lstrlib.c
20:21 2017-07-16
ltable.c
	luaH_get(): //FIXME:added ???this is not beautiful, use goto default
	luaH_get(): //FIXME: n->node
20:21 2017-07-16
ltable.h
20:27 2017-07-16
ltablib.c

21:14 2017-07-16
ltm.c
21:15 2017-07-16
ltm.h
21:32 2017-07-16
lua.c
	luai_writestringerror( //FIXME:???%s
	collectargs (TODO) go through not sync //FIXME:go through->goto xxx
21:44 2017-07-16
lua.h
	public static CharPtr lua_pushliteral(lua_State L, CharPtr s) {
            //TODO: Implement use using lua_pushlstring instead of lua_pushstring
			//lua_pushlstring(L, "" s, (sizeof(s)/GetUnmanagedSize(typeof(char)))-1)
            return lua_pushstring(L, s); } //FIXME: changed 


22:17 2017-07-16
luaconf.h
	//FIXME:TODO:LUA_COMPAT_ALL is defined, but all defines removed here 
		lua_cpcall <-#define
	public const int LUAL_BUFFERSIZE		= 1024; // BUFSIZ; todo: check this - mjf
		//FIXME: changed here, = BUFSIZ;
22:19 2017-07-16
lualib.h
22:29 2017-07-16
lundump.c	
	return (char)LoadVar(S, typeof(char)); //FIXME: changed // return -> void no return
lundump.h


23:56 2017-07-16
lvm.c
	//#define checkGC(L)	Protect(luaC_checkGC(L); luai_threadyield(L);) //FIXME:
	------
	//Protect(
	  //L.savedpc = InstructionPtr.Assign(pc); //FIXME:
	  luaV_objlen(L, ra, RB(L, base_, i));
	  base_ = ci.u.l.base_; //FIXME:???
	//)
lvm.h
lzio.c
lzio.h
				



-------------------------------------------
(below not certain)

using lua_Unsigned = System.UInt32; ???

private const int sweepphases = ??? uint???

public static bool isdeadm(int ow, int m) { return (!(((m) ^ WHITEBITS) & (ow))); } ??? int? uint?

llimit.h////FIXME:<----------------------------------removed

public static void changenvalue(TValue o, int x) { //??int??object???

using lua_Unsigned = System.UInt32; ???

using lua_Unsigned = System.UInt32; ???

private static int sweepphases {get{ //FIXME:->getter

private const int MAX_SIZE_T = uint.MaxValue;//(int)(~(uint)0); //FIXME: ???

public const uint MIN_LMEM = (/*(l_mem)*/~((~(lu_mem)0)>>1)); //FIXME:??? = 0x80000000 //FIXME:(l_mem) removed

-------------------------------------------

public static int lua_number2str(ref CharPtr s, double n) { s = String.Format("{0}", n); return strlen(s); } //FIXME:changed, sprintf->String.Format //FIXME: not assign, fill



        //FIXME:added
		public static object lua_newuserdata(lua_State L, Type t)
		{
			Udata u;
			lua_lock(L);
			luaC_checkGC(L);
			u = luaS_newudata(L, t, null); //FIXME:???removed, getcurrenv(L)->null
			
			

//#if LUA_COMPAT_MODULE //remove it



rng = new Random((int)luaL_checkunsigned(L, 1)); //FIXME:changed - srand(luaL_checkunsigned(L, 1)); //FIXME:added, (int)























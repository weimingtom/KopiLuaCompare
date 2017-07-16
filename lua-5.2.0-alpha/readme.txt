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



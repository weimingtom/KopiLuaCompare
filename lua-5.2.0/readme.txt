05:36 2017-08-13
lapi.c
lapi.h
05:47 2017-08-13
lauxlib.c
05:51 2017-08-13
lauxlib.h
06:03 2017-08-13
lbaselib.c



20:47 2017-08-14
lbitlib.c
20:52 2017-08-14
lcode.c
20:53 2017-08-14
lcode.h
20:54 2017-08-14
lcorolib.c
20:55 2017-08-14
lctype.c
20:55 2017-08-14
lctype.h
20:58 2017-08-14
ldblib.c
21:14 2017-08-14
ldebug.c
21:14 2017-08-14
ldebug.h


03:00 2017-08-15
ldo.c
03:01 2017-08-15
ldo.h
03:02 2017-08-15
lfunc.c
lfunc.h
03:31 2017-08-15
lgc.c
	traverseweakvalue
	traverseephemeron
	traversestrongtable
	clearkeys
	clearvalues
		sizenode(h) ????===gnodelast(h) (original is: gnode(h, sizenode(h));)
03:32 2017-08-15
lgc.h
03:32 2017-08-15
linit.c

	
13:24 2017/8/15
liolib.c
13:43 2017/8/15
llex.c
	escerror(ls, new int[]{ls.current}, 1, "invalid escape sequence"); //FIXME:changed, new int[]{}
13:44 2017/8/15
llex.h

20:48 2017-08-15
llimits.h
	removed, //#define lua_assert
20:49 2017-08-15
lmathlib.c
20:51 2017-08-15
lmem.c
	gettotalbytes(g), g->GCdebt, g->gcstate * 10000);
20:52 2017-08-15
lmem.h
21:04 2017-08-15
loadlib.c


01:39 2017-08-16
lobject.c
	lua_strx2number, removed???
01:47 2017-08-16
lobject.h
01:48 2017-08-16
lopcodes.h
01:52 2017-08-16
loslib.c
02:09 2017-08-16
lparser.c
02:11 2017-08-16
lparser.h

02:29 2017-08-16
lstate.c
		//g.frealloc(g.ud, fromstate(L), (uint)GetUnmanagedSize(typeof(LG)), 0);  /* free main block */ //FIXME:???deleted
02:31 2017-08-16
lstate.h
02:31 2017-08-16
lstring.c
lstring.h
02:35 2017-08-16
lstrlib.c
02:41 2017-08-16
ltable.c
02:43 2017-08-16
ltable.h
02:44 2017-08-16
ltablib.c
02:45 2017-08-16
ltm.c
ltm.h
02:54 2017-08-16
lua.c
02:55 2017-08-16
lua.h
03:00 2017-08-16
luaconf.h
03:01 2017-08-16
lualib.h
03:03 2017-08-16
lundump.c
lundump.h
03:11 2017-08-16
lvm.c
	//not sync
	//#define vmcasenb(l,b)	case l: {b}		/* nb = no break */ 
03:14 2017-08-16
lzio.c
03:15 2017-08-16
lzio.h
	//not sync
	//#define S(x)	(int)(x),SS(x)


---------------------------


		//#define MAX_UINTFRM	((lua_Number)(~(unsigned LUA_INTFRM_T)0))
		//#define MAX_INTFRM	((lua_Number)((~(unsigned LUA_INTFRM_T)0)/2))
		//#define MIN_INTFRM	(-(lua_Number)((~(unsigned LUA_INTFRM_T)0)/2) - 1)
		//FIXME:???here not sure
		private const uint MAX_UINTFRM = uint.MaxValue / 2;
		private const int MAX_INTFRM = int.MaxValue / 2;
		private const int MIN_INTFRM = int.MinValue / 2 - 1;
		
		
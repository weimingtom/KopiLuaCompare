22:19 2017-06-06
lapi.c
	(todo:) {setnilvalue(L.top);StkId.inc(ref L.top);} //FIXME:??? check old version, this inc function is inc post

03:42 2017-06-11
lapi.h
lauxlib.c

03:46 2017-06-11
lauxlib.h
11:14 2017-06-11
lbaselib.c
11:39 2017-06-11
lcode.c
11:43 2017-06-11
lcode.h
11:58 2017-06-11
ldblib.c
14:10 2017-06-11
ldebug.c
14:17 2017-06-11
ldebug.h
15:13 2017-06-11
ldo.c
15:15 2017-06-11
ldo.h

20:48 2017-06-14
lfunc.c
20:59 2017-06-14
lgc.c
21:02 2017-06-14
lgc.h
21:06 2017-06-14
linit.c
21:08 2017-06-14
liolib.c
21:19 2017-06-14
llex.c
21:22 2017-06-14
llimits.h
21:26 2017-06-14
lmathlib.c
21:28 2017-06-14
lmem.h
21:34 2017-06-14
loadlib.c

04:13 2017-06-16
lobject.c
04:16 2017-06-16
lobject.h
04:17 2017-06-16
lopcodes.h
04:19 2017-06-16
loslib.c
04:26 2017-06-16
lparser.c
04:36 2017-06-16
lstate.c
04:42 2017-06-16
lstate.h
04:47 2017-06-16
lstring.c
04:49 2017-06-16
lstrlib.c
04:51 2017-06-16
ltable.c
04:55 2017-06-16
ltablib.c
04:58 2017-06-16
lua.c
05:08 2017-06-16
lua.h
05:22 2017-06-16
luaconf.h
	#define LUA_COMPAT_LOG10 //FIXME:???
05:23 2017-06-16
lualib.h
05:24 2017-06-16
lundump.c

22:08 2017-06-16
lvm.c
		public static void dojump(int i) { InstructionPtr.inc(ref ci->u.l.savedpc, i); luai_threadyield(L); } //FIXME:
		//#define Protect(x)	{ {x;}; base = ci->u.l.base_; } //FIXME:
		->
		#define dojump(i)	{ ci->u.l.savedpc += (i); luai_threadyield(L);}
		#define Protect(x)	{ {x;}; base = ci->u.l.base; }
22:11 2017-06-16
lzio.h



UCHAR_MAX
->
System.Byte.MaxValue



!!!NOTICE!!!

public static object luaM_realloc_<T>(lua_State L, T[] old_block, int new_size)
	mod logic

!!!NOTICE!!!


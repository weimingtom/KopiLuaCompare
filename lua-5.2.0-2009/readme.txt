00:38 2017-05-29
lapi.c
00:55 2017-05-29
lauxlib.c
lauxlib.h
01:02 2017-05-29
lbaselib.c
01:10 2017-05-29
lcode.c
01:13 2017-05-29
lcode.h
01:16 2017-05-29
ldblib.c
01:37 2017-05-29
ldebug.c

04:37 2017-05-30
ldo.c
ldo.h
04:40 2017-05-30
ldump.c
04:45 2017-05-30
liolib.c
04:47 2017-05-30
llex.c
04:52 2017-05-30
loadlib.c
04:56 2017-05-30
lobject.c
04:58 2017-05-30
lobject.h
05:01 2017-05-30
lopcodes.c
05:05 2017-05-30
loslib.c
05:10 2017-05-30
lparser.c
05:13 2017-05-30
lparser.h

07:36 2017-05-30
lstate.c
07:46 2017-05-30
lstate.h
07:58 2017-05-30
lstring.c
08:01 2017-05-30
lstrlib.c
08:04 2017-05-30
ltable.c
08:09 2017-05-30
ltablib.c
08:19 2017-05-30
lua.c
08:24 2017-05-30
lua.h

08:31 2017-05-30
luaconf.h
	NOTICE---------------->defined ouside!!! #define LUA_COMPAT_API
    public const int LUAI_MCS_AUX = SHRT_MAX < (INT_MAX / 16) ? SHRT_MAX : (INT_MAX / 16);
    
    
08:34 2017-05-30
lundump.c
lvm.h






16:35 2017-05-30
lopcodes.h
	//FIXME:???
	//#if SIZE_Ax < LUAI_BITSINT-1
	public const int MAXARG_Ax	= ((1<<SIZE_Ax)-1);
	//#else
	//public const int MAXARG_Ax	= MAX_INT;
	//#endif

16:40 2017-05-30
lgc.h

17:02 2017-05-30
lvm.c
	OP_TFORLOOP->OP_TFORCALL+OP_TFORLOOP
	c = GETARG_Ax((L.savedpc++)[0]); //FIXME:???

17:46 2017-05-30
lgc.c



------------------------

INT_MAX
->
Int32.MaxValue

UCHAR_MAX
->
System.Byte.MaxValue

-----------------------

Dump(L.savedpc.pc, i);	//FIXME:added, only for debugging

-----------------------


		public class PtrRef : GCObjectRef
		{
			public PtrRef(GCObject obj) { this.obj = obj; }
			public void set(GCObject value) { this.obj = value; }
			public GCObject get() { return this.obj; }
			GCObject obj;
		}
		
->

		  GCObjectRef lastnext = new PtrRef(g.tobefnz); //FIXME:??????
		  /* find last 'next' field in 'tobefnz' list (to insert elements in its end) */
		  while (lastnext.get() != null) lastnext = new PtrRef(gch(lastnext.get()).next);
		  
		  		
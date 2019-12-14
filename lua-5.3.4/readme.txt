os_date not sync



-------------------------
4:00 2019/12/14
linit.c
4:01 2019/12/14
lobject.h
4:02 2019/12/14
ltm.c
4:03 2019/12/14
lutf8lib.c
4:04 2019/12/14
lvm.h

4:10 2019/12/14
lauxlib.c
4:12 2019/12/14
lauxlib.h
4:13 2019/12/14
lbaselib.c
4:18 2019/12/14
lcode.c
4:23 2019/12/14
ldebug.c
4:42 2019/12/14
ldo.c
5:27 2019/12/14
lgc.c
5:32 2019/12/14
liolib.c
5:35 2019/12/14
lmathlib.c
5:44 2019/12/14
loadlib.c
5:45 2019/12/14
lobject.c
5:46 2019/12/14
lopcodes.h
5:51 2019/12/14
loslib.c
5:54 2019/12/14
lparser.c
5:57 2019/12/14
lstate.h
6:01 2019/12/14
lstrlib.c
6:11 2019/12/14
ltable.c
6:12 2019/12/14
ltable.h
6:15 2019/12/14
lua.c
6:16 2019/12/14
lua.h
6:18 2019/12/14
lualib.h
6:24 2019/12/14
luaconf.h

----------------------
"\1"

		private const string AUXMARK = "\x001";	/* auxiliary mark */ //FIXME:???
		
------------------------
byte->short???

		public static void setoah(byte st, byte v)	{ st = (byte)(((st & ~CIST_OAH) | v) & 0xff); }
		public static byte getoah(byte st)	{ return (byte)((st & CIST_OAH) & 0xff); }
		
------------------------
&0xffff, ushort

L.ci.callstatus &= ((~CIST_FIN) & 0xffff);  /* not running a finalizer anymore */

-------------------------
NULL->int.MinValue

		    t.lastfree = int.MinValue/*NULL*/;  /* signal that it is using dummy node */
		    
------------------------
NULL->int.MinValue

public static bool isdummy(Table t)		{ return (t.lastfree == int.MinValue/*NULL*/); }

-------------------------

if (oldhsize > 0)  /* not the dummy node? */
---->		    luaM_freearray(L, nold);//luaM_freearray(L, nold, (uint)(oldhsize)); /* free old hash */
		    
-------------------------


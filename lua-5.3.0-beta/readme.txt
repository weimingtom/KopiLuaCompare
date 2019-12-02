math_random not implemented same
luaM_reallocv, luaM_freearray not implemented same
os_difftime not implemented same
nativeendian not implemented correctly
size_t->uint

l_mathop not used (removed), see l_mathop(op)		(lua_Number)op

		public class Table : GCUnion {
		  public lu_byte flags;  /* 1<<p means tagmethod(p) is not present */
		  public lu_byte lsizenode;  /* log2 of size of `node' array */
resume not getter--->		  private uint _sizearray;  /* size of `array' array */
		  public uint sizearray
		  {
		  	get
		  	{
		  		return _sizearray;
		  	}
		  	set
		  	{
		  		this._sizearray = value;
		  	}
		  }	
		  
		  

----------------------
10:18 2019/12/2
lopcodes.h
lvm.h

10:23 2019/12/2
lapi.c
10:36 2019/12/2
lauxlib.c
10:38 2019/12/2
lauxlib.h
10:45 2019/12/2
lbaselib.c
11:11 2019/12/2
lbitlib.c
11:12 2019/12/2
lcorolib.c
11:32 2019/12/2
ldblib.c
14:04 2019/12/2
ldebug.c
14:07 2019/12/2
ldo.c
14:40 2019/12/2
lgc.c
14:46 2019/12/2
liolib.c
14:49 2019/12/2
llex.c
14:53 2019/12/2
lmathlib.c
14:59 2019/12/2
lmem.h
15:03 2019/12/2
loadlib.c
15:09 2019/12/2
lobject.c
15:11 2019/12/2
lobject.h
15:14 2019/12/2
loslib.c
15:17 2019/12/2
lparser.c
15:19 2019/12/2
lstate.h
15:25 2019/12/2
lstrlib.c
---
(TODO)ltable.c
(TODO)luaconf.h
(TODO)ltablib.c
---
16:28 2019/12/2
lvm.c
17:11 2019/12/2
lutf8lib.c
17:13 2019/12/2
lua.h
17:17 2019/12/2
lua.c
17:20 2019/12/2
ltable.h
17:24 2019/12/2
ltablib.c
17:34 2019/12/2
luaconf.h
17:49 2019/12/2
ltable.c



----------------------------------

		private const int MAXALIGN = 1; //#define MAXALIGN	(offsetof(struct cD, u))



----------------------------

		private static void copywithendian (/*volatile*/ CharPtr dest, /*volatile*/ CharPtr src,
		                            int size, int islittle) {
		  dest = new CharPtr(dest); //FIXME:???
		  src = new CharPtr(src); //FIXME:???
		  
--------------------------------



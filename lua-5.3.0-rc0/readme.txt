* some struct embed commonheader,but in csharp it extends GCUnion

lapi.c include lprefix.h
#define->const
lbitlib.c include lprefix.h

lua.c print cammand to file invalid

luaL_getmetatable return int ???(see old version)

lua_rawgetp third param void *, use int directly

not implement --->		public static CharPtr lua_getextraspace(lua_State L) { throw new Exception(); return null; }	//((void *)((char *)(L) - LUA_EXTRASPACE)) //FIXME:???

-------------------

9:46 2019/12/3
ldo.h
9:50 2019/12/3
lgc.h
9:53 2019/12/3
lobject.h
9:56 2019/12/3
lopcodes.h
9:58 2019/12/3
lparser.h
10:02 2019/12/3
lstate.h
10:02 2019/12/3
ltm.h




8:34 2019/12/4
lapi.c
8:39 2019/12/4
lauxlib.c
8:40 2019/12/4
lauxlib.h
8:45 2019/12/4
lbaselib.c
8:54 2019/12/4
lbitlib.c
9:15 2019/12/4
lcode.c
9:16 2019/12/4
lcorolib.c
9:17 2019/12/4
lctype.c
10:20 2019/12/4
ldblib.c
10:28 2019/12/4
ldebug.c
10:57 2019/12/4
ldebug.h
11:01 2019/12/4
ldo.c
11:22 2019/12/4
ldump.c
11:24 2019/12/4
lfunc.c
11:27 2019/12/4
lgc.c
11:30 2019/12/4
linit.c
11:35 2019/12/4
liolib.c
11:42 2019/12/4 
llex.c

14:20 2019/12/4
llex.h
14:26 2019/12/4
llimits.h
15:20 2019/12/4
lmathlib.c
15:23 2019/12/4
lmem.c
15:30 2019/12/4
loadlib.c
16:06 2019/12/4
lobject.c
16:07 2019/12/4
lopcodes.c
16:21 2019Äê12ÔÂ4ÈÕ
lparser.c
16:22 2019/12/4
lstate.c
16:22 2019/12/4
lstring.c
16:50 2019/12/4
lstrlib.c
16:55 2019/12/4
ltable.c
16:57 2019/12/4
ltablib.c
16:59 2019/12/4
ltm.c
17:04 2019/12/4
lua.c
17:07 2019/12/4
lua.h
17:08 2019/12/4
luac.c

(TODO) luaconf.h


17:09 2019/12/4
lundump.c
17:09 2019/12/4
lutf8lib.c
17:20 2019/12/4
lvm.c
17:21 2019/12/4
lzio.c



















----------------
use ref not array

		      int[] np = new int[]{n}; //FIXME:added
		      res = (luaD_rawrunprotected(L, growstack, np) == LUA_OK ? 1 : 0);
		      n = np[0]; //FIXME:added
		      
----------------------
/* recfield . (NAME | `['exp1`]') = exp1 */
->
/* recfield -> (NAME | `['exp1`]') = exp1 */

/* param . `...' */
->
/* param -> `...' */


** subexpr . (simpleexp | unop subexpr) { binop subexpr }
->
** subexpr -> (simpleexp | unop subexpr) { binop subexpr }



------------------------

public const string LUA_SIGNATURE = "\x1bLua"; ???
->
public const string LUA_SIGNATURE = "\x01bLua"; ???



-----------------------

		#if _WIN32
		public const string LUA_DIRSEP = "\\";
		#else
		public const string LUA_DIRSEP = "/";
FIXME:here not align--->		#endif
		
--------------------------


		public static bool CanIndex(Type t)
		{
			if (t == typeof(char))
				return false;
			if (t == typeof(byte))
				return false;
			if (t == typeof(int))
				return false;
			if (t == typeof(uint))
				return false;
			if (t == typeof(LocVar))
				return false;
			if (t == typeof(LClosure)) //FIXME:
				return false;
Ctrl+Z, throw exception, write here to avoid  ---->			if (t == typeof(UpVal)) //FIXME:
				return false;
same problem--->			if (t == typeof(GCObject)) //FIXME:
				return false;
			return true;
		}
		
		

Need to derive type KopiLua.Lua+GCObject from ArrayElement

    at Lua.luaM_realloc_(lua_State L, T[] old_block, Int32 new_size)  e:\kopiluacompare\20191203\KopiLuaCompare\lua-5.3.0-rc0\kopilua\lmem.c.cs(119)
    at Lua.luaM_freemem(lua_State L, T b, UInt32 s)  e:\kopiluacompare\20191203\KopiLuaCompare\lua-5.3.0-rc0\kopilua\lmem.h.cs(37)
    at Lua.freeobj(lua_State L, GCObject o)  e:\kopiluacompare\20191203\KopiLuaCompare\lua-5.3.0-rc0\kopilua\lgc.c.cs(738)
    at Lua.sweeplist(lua_State L, GCObjectRef p, UInt32 count)  e:\kopiluacompare\20191203\KopiLuaCompare\lua-5.3.0-rc0\kopilua\lgc.c.cs(782)
    at Lua.sweepwholelist(lua_State L, GCObjectRef p)  e:\kopiluacompare\20191203\KopiLuaCompare\lua-5.3.0-rc0\kopilua\lgc.c.cs(761)
    at Lua.luaC_freeallobjects(lua_State L)  e:\kopiluacompare\20191203\KopiLuaCompare\lua-5.3.0-rc0\kopilua\lgc.c.cs(1017)
    at Lua.close_state(lua_State L)  e:\kopiluacompare\20191203\KopiLuaCompare\lua-5.3.0-rc0\kopilua\lstate.c.cs(257)
    at Lua.lua_close(lua_State L)  e:\kopiluacompare\20191203\KopiLuaCompare\lua-5.3.0-rc0\kopilua\lstate.c.cs(360)
    at Program.Main(String[] args)  e:\kopiluacompare\20191203\KopiLuaCompare\lua-5.3.0-rc0\kopilua\lua.c.cs(632)


!!!this is not a very good method to avoid this exception




---------------------------










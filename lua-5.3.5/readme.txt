os_date not sync
os_time not sync


-----------------
9:22 2019/12/14
lapi.h
9:23 2019/12/14
lauxlib.c
9:24 2019/12/14
lauxlib.h
9:24 2019/12/14
lbaselib.c
9:25 2019/12/14
lbitlib.c
9:25 2019/12/14
lcode.c
9:26 2019/12/14
lcode.h
9:26 2019/12/14
lcorolib.c
9:27 2019/12/14
lctype.c
9:28 2019/12/14
lctype.h
9:28 2019/12/14
ldblib.c
9:29 2019/12/14
ldebug.h
9:30 2019/12/14
ldo.c
9:31 2019/12/14
ldo.h
9:33 2019/12/14
ldump.c
9:33 2019/12/14
lfunc.c
9:33 2019/12/14
lfunc.h
9:34 2019/12/14
lgc.h
9:35 2019/12/14
linit.c
9:36 2019/12/14
llex.c
9:38 2019/12/14
llex.h
9:39 2019/12/14
llimits.h
9:40 2019/12/14
lmathlib.c
9:41 2019/12/14
lmem.c
9:42 2019/12/14
lmem.h
9:43 2019/12/14
loadlib.c 
9:44 2019/12/14
lobject.h
10:07 2019/12/14
lopcodes.c
10:08 2019/12/14
lopcodes.h
10:08 2019/12/14
lparser.h
10:09 2019/12/14
lprefix.h
10:09 2019/12/14
lstate.c
10:10 2019/12/14
lstate.h
10:10 2019/12/14
lstring.c
10:11 2019/12/14
lstring.h
10:11 2019/12/14
ltablib.c
10:11 2019/12/14
ltm.c
10:12 2019/12/14
ltm.h
10:12 2019/12/14
lualib.h
10:13 2019/12/14
lundump.c
10:13 2019/12/14
lundump.h
10:19 2019/12/14
lvm.c
10:19 2019/12/14
lvm.h
10:20 2019/12/14
lzio.c
10:20 2019/12/14
lzio.h

10:21 2019/12/14
lapi.c
10:23 2019/12/14
ldebug.c
10:24 2019/12/14
lgc.c
10:26 2019/12/14
liolib.c
10:28 2019/12/14
lobject.c
10:31 2019/12/14
loslib.c
10:32 2019/12/14
lparser.c
10:33 2019/12/14
lstrlib.c
10:37 2019/12/14
ltable.c
10:38 2019/12/14
ltable.h
10:39 2019/12/14
lua.c
10:40 2019/12/14
lua.h
10:42 2019/12/14
luac.c
10:43 2019/12/14
luaconf.h


-------------------------------
for 5.3.4
		      case 'p': {  /* a pointer */
		        CharPtr buff = new char[32]; /* should be enough space for a '%p' */ //FIXME: changed, char buff[4*sizeof(void *) + 8];
int->uint  ---->		        int l = l_sprintf(buff, 32/*sizeof(buff)*/, "0x%08x", argp[parm_index++].GetHashCode()); //FIXME: changed, %p->%08x  //FIXME:changed, (uint)
		        pushstr(L, buff, l);
		        break;
		      }
		      
		      
--------------------------------


		/*
		@@ lua_pointer2str converts a pointer to a readable string in a
		** non-specified way.
		*/
---->		public static int lua_pointer2str(CharPtr buff, int sz, object p) { return l_sprintf(buff,sz,"0x%08x",p.GetHashCode()); } //FIXME: l_sprintf(buff,sz,"%p",p);
		
		

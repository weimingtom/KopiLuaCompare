luaG_runerror in ldebug.c: indent not good
luaM_realloc_ not same
os_time not same
os_difftime not same

public const int MAX_ITEM	= 512; //(120 + l_mathlim(MAX_10_EXP)); //FIXME:???not calculate with compiler

----------------
TStringPtrRef not sync

  while (*p != ts)  /* find previous element */
 -->   p = &(*p)->u.hnext;
    
p = new TStringPtrRef(p.get());
    

-------------------------

8:28 2019/12/6
ltm.c
8:49 2019/12/6
lapi.c
8:50 2019/12/6
lapi.h
8:57 2019/12/6
lauxlib.c
8:58 2019/12/6
lbaselib.c
9:02 2019/12/6
lcode.c
9:06 2019/12/6
ldblib.c
9:19 2019/12/6
ldebug.c
9:22 2019/12/6
ldebug.h
9:30 2019/12/6
ldo.c
10:27 2019/12/6
ldo.h
10:29 2019/12/6
ldump.c
10:30 2019/12/6
lfunc.h
10:41 2019/12/6
lgc.c
10:43 2019/12/6
liolib.c
10:47 2019/12/6
llex.c
12:05 2019/12/6
llimits.h
12:07 2019/12/6
lmathlib.c
12:08 2019/12/6
lmem.c
14:09 2019/12/6
loadlib.c
14:16 2019/12/6
lobject.c
14:23 2019/12/6
lobject.h
14:29 2019/12/6
loslib.c
14:55 2019/12/6
lstate.c
14:57 2019/12/6
lstate.h
15:25 2019/12/6
lstring.c
15:25 2019/12/6
lstring.h
15:36 2019/12/6
lstrlib.c

16:33 2019/12/6
(TODO) ltable.c

15:38 2019/12/6
ltablib.c
15:45 2019/12/6
lua.c
15:49 2019/12/6
lua.h
15:50 2019/12/6
luac.c

16:47 2019/12/6
(TODO) luaconf.h

15:58 2019/12/6
lutf8lib.c

16:59 2019/12/6
(TODO) lvm.c

16:01 2019/12/6
lvm.h

-------------------

ldblib.c:
lua_pushthread(L1); lua_xmove(L1, L, 1);  /* key */
->
lua_pushthread(L1); lua_xmove(L1, L, 1);  /* key (thread) */




-----------------

		private static void freeobj (lua_State L, GCObject o) {
		  switch (o.tt) {
			case LUA_TPROTO: luaF_freeproto(L, gco2p(o)); break;
			case LUA_TLCL: {
			  freeLclosure(L, gco2lcl(o));
		      break;
		    }
		    case LUA_TCCL: {
			  luaM_freemem(L, o, sizeCclosure(gco2ccl(o).nupvalues));
		      break;
		    }
			case LUA_TTABLE: luaH_free(L, gco2t(o)); break;
			case LUA_TTHREAD: luaE_freethread(L, gco2th(o)); break;
    		case LUA_TUSERDATA: 
				//luaM_freemem(L, o, sizeudata(gco2u(o)));
				luaM_freemem(L, gco2u(o), sizeudata(gco2u(o))); //FIXME:???
				break;
			case LUA_TSHRSTR:
			  luaS_remove(L, gco2ts(o));  /* remove it from hash table */
		      luaM_freemem(L, o, sizelstring(gco2ts(o).shrlen));
		      break;
    		case LUA_TLNGSTR: {
--->			  //luaM_freemem(L, o, sizelstring(gco2ts(o)->u.lnglen));
--->			  luaM_freemem(L, gco2ts(o), sizelstring(gco2ts(o).u.lnglen)); //FIXME:???
			  break;
			}
			default: lua_assert(0); break;
		  }
		}
		
		
-----------------

		//#if defined(LUA_FLOAT_LONGDOUBLE)
Use which??? --->		//#define LUAL_BUFFERSIZE		8192
		//#else
Use which??? --->		private const int LUAL_BUFFERSIZE = ((int)0x80 * 4 * 4); //((int)(0x80 * sizeof(void*) * sizeof(lua_Integer)))
		//#endif
		
		


--------------------

public static void lua_seti (lua_State L, int idx, lua_Integer n) {
		  StkId t;
		  lua_lock(L);
		  api_checknelems(L, 1);
		  t = index2addr(L, idx);
		  setivalue(ref L.top, n);
->
		  setivalue(L.top, n);
		  
		  

---------------------------

no init to assign TString[1] for this array ---->		  public TString[][] strcache = new TString[STRCACHE_SIZE][/*1*/];  /* cache for strings in API */
		  
--------------------------


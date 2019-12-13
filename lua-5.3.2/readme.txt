savestack, restorestack not same
condmovestack, condchangemem not same
math_randomseed not same
os_date not sync
os_time not sync
addbuff not sync ??? (in 5.3.1, buffer->b)


---
changed:

#define checkGC(L,c)  \
	{ luaC_condGC(L, L->top = (c),  /* limit of live values */ \
                         Protect(L->top = ci->top));  /* restore top */ \
           luai_threadyield(L); }
           
           

-----------------------------

14:30 2019/12/9
lauxlib.h
14:31 2019/12/9
ldblib.c
14:31 2019/12/9
loadlib.c

15:33 2019/12/9
lapi.c
15:39 2019/12/9
lauxlib.c
15:44 2019/12/9
lbaselib.c
15:49 2019/12/9
lbitlib.c
15:51 2019/12/9
lcode.c
15:53 2019/12/9
ldebug.c

8:45 2019/12/10
ldo.c
8:49 2019/12/10
ldo.h
8:57 2019/12/10
ldump.c
9:04 2019/12/10
lgc.c
9:14 2019/12/10
lgc.h
9:24 2019/12/10
liolib.c
9:27 2019/12/10
llex.c
9:33 2019/12/10
llimits.h
9:36 2019/12/10
lmathlib.c

8:24 2019/12/11
lobject.c
8:48 2019/12/11
lobject.h
9:44 2019/12/11
loslib.c
9:46 2019/12/11
lparser.c
12:06 2019/12/11
lstate.c
12:10 2019/12/11
lstate.h


8:41 2019/12/12
lstring.c
8:42 2019/12/12
lstring.h
8:58 2019/12/12
lstrlib.c
11:02 2019/12/12
ltable.c
11:08 2019/12/12
ltable.h

(TODO) ltablib.c

11:11 2019/12/12
ltm.c
11:13 2019/12/12
lua.c
11:14 2019/12/12
lua.h
11:21 2019/12/12
luaconf.h
11:25 2019/12/12
lundump.c
11:26 2019/12/12
lundump.h

(TODO) lvm.c

11:28 2019/12/12
lvm.h
11:29 2019/12/12
lzio.c
11:30 2019/12/12
lzio.h

11:55 2019/12/12
ltablib.c
14:13 2019/12/12
lvm.c

----------------------------------------


		/*
		** {==================================================================
		** Stack reallocation
		** ===================================================================
		*/
		private static void correctstack (lua_State L, TValue[] oldstack) {
		   //FIXME:???
			/* don't need to do this
		  CallInfo ci;
		  UpVal up;
		  L.top = L.stack[L.top - oldstack];
		  for (up = L.openupval; up != null; up = up.u.open.next)
			up.v = L.stack[up.v - oldstack];
		  for (ci = L.base_ci; ci != null; ci = ci.previous) {
			  ci.top = L.stack[ci.top - oldstack];
			ci.func = L.stack[ci.func - oldstack];
		    if (isLua(ci))
		      ci.u.l.base = (ci.u.l.base - oldstack) + L.stack;
		  }
			 * */
		}
		
---------------------------------------------

luaD_poscall, removed 

		  /* move results to correct place */
		  for (i = wanted; i != 0 && nres-- > 0; i--)
		  {
			  setobjs2s(L, res, firstResult);
			  res = res + 1; //FIXME:moved here, can change to origin
			  firstResult = firstResult + 1; //FIXME:moved here, can change to origin
		  }
		  while (i-- > 0)
			  setnilvalue(StkId.inc(ref res));
		  L.top = res;
		  return (wanted - LUA_MULTRET);  /* 0 iff wanted == LUA_MULTRET */
		  
---------------------


--->		// in the original C code these values save and restore the stack by number of bytes. marshalling sizeof
		// isn't that straightforward in managed languages, so i implement these by index instead.
		public static int savestack(lua_State L, StkId p)		{return p;}
		public static StkId restorestack(lua_State L, int n)	{return L.stack[n];}
		
		
------------------------

TString *luaS_newlstr (lua_State *L, const char *str, size_t l) {
  if (l <= LUAI_MAXSHORTLEN)  /* short string? */
    return internshrstr(L, str, l);
  else {
    TString *ts;
not calculate for compiler ---------->    if (l >= (MAX_SIZE - sizeof(TString))/sizeof(char))
      luaM_toobig(L);
    ts = luaS_createlngstrobj(L, l);
    memcpy(getstr(ts), str, l * sizeof(char));
    return ts;
  }
}


-------------------------
expand


		/* macro to check stack size, preserving 'p' */
		private static void checkstackp(lua_State L, int n, ref StkId p)  {
//		  luaD_checkstackaux(L, n, delegate() {
//		    ptrdiff_t t__ = savestack(L, p);  /* save 'p' */
//		    luaC_checkGC(L); },  /* stack grow uses memory */
//		    delegate() { p = restorestack(L, t__); } );  /* 'pos' part: restore 'p' */
//		}
			if (L.stack_last - L.top <= (n)) { 
			  ptrdiff_t t__ = savestack(L, p);  /* save 'p' */
		      luaC_checkGC(L);  /* stack grow uses memory */
			  luaD_growstack(L, n); 
			  p = restorestack(L, t__); 
			} else {
			  //FIXME: empty				
			  //condmovestack(L,pre,pos);
			}
		}
		
-------------------------------


		private static int gmatch_aux (lua_State L) {
		  GMatchState gm = (GMatchState)lua_touserdata(L, lua_upvalueindex(3));
		  CharPtr src;
--->		  for (src = new CharPtr(gm.src); src <= gm.ms.src_end; src.inc()) { //FIXME:new CharPtr()
		    CharPtr e;
		    reprepstate(gm.ms);
		    if ((e = match(gm.ms, src, gm.p)) != null) {
		      if (e == src)  /* empty match? */
		        gm.src =src + 1;  /* go at least one position */
		      else
		        gm.src = e;
		      return push_captures(gm.ms, src, e);
		    }
		  }
		  return 0;  /* not found */
		}
		
-------------------------------


		private static uint l_randomizePivot () {
			/*
		  clock_t c = clock();
		  time_t t = time(NULL);
		  uint buff[sof(c) + sof(t)];
		  uint i, rnd = 0;
		  memcpy(buff, &c, sof(c) * sizeof(unsigned int));
		  memcpy(buff + sof(c), &t, sof(t) * sizeof(unsigned int));
		  for (i = 0; i < sof(buff); i++)
		    rnd += buff[i];
		  return rnd;
		  */
		 throw new Exception();
		 return 0;
		}
		
--------------------------------
old (int), new convert to (uint)

		/* builds a number with 'n' ones (1 <= n <= LUA_NBITS) */
		private static uint mask(int n) { return (uint)(~((ALLONES << 1) << ((n) - 1))); } //FIXME:???//FIXME:(uint)
		
--------------------------------

		public static LClosure luaU_undump (lua_State L, ZIO Z, CharPtr name) {
		  LoadState S = new LoadState();
		  LClosure cl;
		  if (name[0] == '@' || name[0] == '=')
		    S.name = name + 1;
		  else if (name[0] == LUA_SIGNATURE[0])
		    S.name = "binary string";
		  else
		    S.name = name;
		  S.L = L;
		  S.Z = Z;
		  checkHeader(S);
		  cl = luaF_newLclosure(L, LoadByte(S));
		  setclLvalue(L, L.top, cl);
		  luaD_inctop(L);
		  cl.p = luaF_newproto(L);
		  LoadFunction(S, cl.p, null);
		  lua_assert(cl.nupvalues == cl.p.sizeupvalues);
--->		  Mbuffer buff__ = null; luai_verifycode(L, buff__, cl.p); //FIXME:
		  return cl;
		}
		
--------------------------------------

private static object resizebox (lua_State L, int idx, uint newsize) {
		  object ud;
		  lua_Alloc allocf = lua_getallocf(L, ref ud);
		  UBox box = (UBox)lua_touserdata(L, idx);
		  throw new Exception();
allocf---->		  object temp = allocf(ud, box.box, box.bsize, newsize);
		  if (temp == null && newsize > 0) {  /* allocation error? */
		    resizebox(L, idx, 0);  /* free buffer */
		    luaL_error(L, "not enough memory for buffer allocation");
		  }
		  box.box = temp;
		  box.bsize = newsize;
		  return temp;
		}


		private static object resizebox (lua_State L, int idx, uint newsize) {
		  object ud = null;
		  lua_Alloc allocf = lua_getallocf(L, ref ud);
		  UBox box = (UBox)lua_touserdata(L, idx);
		  throw new Exception();
		  //object temp = allocf(ud, box.box, box.bsize, newsize);
------>		  object temp = allocf(typeof(UBox));
		  if (temp == null && newsize > 0) {  /* allocation error? */
		    resizebox(L, idx, 0);  /* free buffer */
		    luaL_error(L, "not enough memory for buffer allocation");
		  }
		  box.box = temp;
		  box.bsize = newsize;
		  return temp;
		}
		
		
		
-------------------------

print got nil bug: (fixed)
ttisnil(aux) should be !ttisnil(aux)
see luaV_fastget_luaH_getstr (mod from luaV_fastget)


@@ -74,11 +74,11 @@ namespace KopiLua
 		  if (!ttistable(t)) {
 		    aux = null;
 		    return 0;  /* not a table; 'aux' is NULL and result is 0 */
 		  } else {
 			aux = luaH_getstr(hvalue(t), k);  /* else, do raw access */
-			if (ttisnil(aux)) {
+			if (!ttisnil(aux)) {
 			  return 1;  /* result not nil? 'aux' has it */
 			} else {
 		      aux = fasttm(L, hvalue(t).metatable, TMS.TM_INDEX);  /* get metamethod */
 		      if (aux != null) {
 		      	return 0;  /* has metamethod? must call it */
@@ -93,11 +93,11 @@ namespace KopiLua
 		  if (!ttistable(t)) {
 		    aux = null;
 		    return 0;  /* not a table; 'aux' is NULL and result is 0 */
 		  } else {
 			aux = luaH_getint(hvalue(t), k);  /* else, do raw access */
-			if (ttisnil(aux)) {
+			if (!ttisnil(aux)) {
 			  return 1;  /* result not nil? 'aux' has it */
 			} else {
 		      aux = fasttm(L, hvalue(t).metatable, TMS.TM_INDEX);  /* get metamethod */
 		      if (aux != null) {
 		      	return 0;  /* has metamethod? must call it */
@@ -112,11 +112,11 @@ namespace KopiLua
 		  if (!ttistable(t)) {
 		    aux = null;
 		    return 0;  /* not a table; 'aux' is NULL and result is 0 */
 		  } else {
 			aux = luaH_get(hvalue(t), k);  /* else, do raw access */
-			if (ttisnil(aux)) {
+			if (!ttisnil(aux)) {
 			  return 1;  /* result not nil? 'aux' has it */
 			} else {
 		      aux = fasttm(L, hvalue(t).metatable, TMS.TM_INDEX);  /* get metamethod */
 		      if (aux != null) {
 		      	return 0;  /* has metamethod? must call it */




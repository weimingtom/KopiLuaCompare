TODO: class embeded, see class CallInfo

			else if (t == typeof(UTString))
				return 64; //FIXME:???UTString
			else if (t == typeof(UUdata))
				return 64；//FIXME:？？

----------------------

9:22 2019/11/28
lparser.c
9:23 2019/11/28
ltm.c

9:57 2019/11/28
lapi.c
10:00 2019/11/28
lapi.h
10:05 2019/11/28
lauxlib.c
10:07 2019/11/28
lauxlib.h
10:12 2019/11/28
lbaselib.c
10:45 2019/11/28
ldebug.c
10:47 2019/11/28
ldo.c
10:48 2019/11/28
ldump.c
11:15 2019/11/28
lgc.c
11:20 2019/11/28
lgc.h
11:23 2019/11/28
liolib.c
11:25 2019/11/28
llex.c
11:38 2019/11/28
llimits.h
11:46 2019/11/28
lmathlib.c
11:59 2019/11/28
lmem.c
14:54 2019/11/28
loadlib.c
15:20 2019/11/28
lobject.c
15:39 2019/11/28
lobject.h
15:44 2019/11/28
lstate.c
16:04 2019/11/28
lstate.h
16:10 2019/11/28
lstring.c
16:13 2019/11/28
lstring.h
16:17 2019/11/28
lstrlib.c
16:23 2019/11/28
ltable.c
16:25 2019/11/28
ltable.h


11:57 2019/11/29
ltablib.c
12:00 2019/11/29
lua.c
12:07 2019/11/29
lua.h
12:09 2019/11/29
luac.c
12:12 2019/11/29
luaconf.h
12:13 2019/11/29
lutf8lib.c
13:56 2019/11/29
lvm.c
13:57 2019/11/29
lvm.h






















--------------------
1. lapi.h, work3 version

here two places not modify indentation

		private static void api_incr_top(lua_State L)   
--->		{
			StkId.inc(ref L.top);
			api_check(L, L.top <= L.ci.top, 
		      "stack overflow");
		}

		private static void adjustresults(lua_State L, int nres)
---->    	{ 
			if (nres == LUA_MULTRET && L.ci.top < L.top) 
				L.ci.top = L.top; 
		}
		
		
	
--------------------
2. not sync, no port


/*
** generic allocation routine.
*/
void *luaM_realloc_ (lua_State *L, void *block, size_t osize, size_t nsize) {
  void *newblock;
  global_State *g = G(L);
  size_t realosize = (block) ? osize : 0;
  lua_assert((realosize == 0) == (block == NULL));
#if defined(HARDMEMTESTS)
  if (nsize > realosize && g->gcrunning)
    luaC_fullgc(L, 1);  /* force a GC whenever possible */
#endif
  newblock = (*g->frealloc)(g->ud, block, osize, nsize);
  if (newblock == NULL && nsize > 0) {
    api_check( nsize > realosize,
                 "realloc cannot fail when shrinking a block");
    luaC_fullgc(L, 1);  /* try to free some memory... */
    newblock = (*g->frealloc)(g->ud, block, osize, nsize);  /* try again */
    if (newblock == NULL)
      luaD_throw(L, LUA_ERRMEM);
  }
  lua_assert((nsize == 0) == (newblock == NULL));
  g->GCdebt = (g->GCdebt + nsize) - realosize;
  return newblock;
}

-----------------------------

3. GetHashCode()???

		public static Node hashpointer(Table t, object p) { return hashmod(t, p.GetHashCode()); }
->
		public static Node hashpointer(Table t, object p) { return hashmod(t, point2int(p)); }
		

->

#define hashpointer(t,p)	hashmod(t, point2int(p))


---------------------------

4. ???this is right???

		/* macro used by 'luaV_concat' to ensure that element at 'o' is a string */
		private static int tostring(lua_State L, TValue o) {
			if (!ttisstring(o)) { if (cvt2str(o)) { luaO_tostring(L, o); return 1; } return 0; } return 1; } //FIXME:???
			
------------------------------


		/*
		**  Get the address of memory block inside 'Udata'.
		** (Access to 'ttuv_' ensures that value is really a 'Udata'.)
		*/
		public static object getudatamem(Udata u)  {
			throw new Exception(); return null; } //return check_exp(sizeof((u).ttuv_), (cast(char*, (u)) + sizeof(UUdata))); } //FIXME:???
			
			
------------------------------

		/*
		** Get the actual string (array of bytes) from a 'TString'.
		** (Access to 'extra' ensures that value is really a 'TString'.)
		*/
		//public static object getaddrstr(ts)	(cast(char *, (ts)) + sizeof(UTString))
		public static object getstr(TString ts)  {
			throw new Exception(); return null; } //  check_exp(sizeof((ts)->extra), cast(const char*, getaddrstr(ts))) //FIXME:
			
------------------------------


		/*
		** conversion of pointer to integer:
		** this is for hashing only; there is no problem if the integer
		** cannot hold the whole pointer value
		*/
		public static uint point2int(object p) { return (uint)p.GetHashCode(); } //((uint)((lu_mem)(p) & UINT_MAX)) //FIXME:???
		
----------------------------------

		//FIXME:added
		public static Udata luaS_newudata(lua_State L, Type t)
		{
		    Udata u;
		    GCObject o;
			uint s = (uint)GetUnmanagedSize(t);
		    if (s > MAX_SIZE - GetUnmanagedSize(typeof(Udata)))
			  luaM_toobig(L);
		    o = luaC_newobj<Udata>(L, LUA_TUSERDATA, sizeludata(s)); //FIXME:(uint)
			u = gco2u(o);
			u.len = 0;//FIXME:s;
			u.metatable = null;
			setuservalue(L, u, luaO_nilobject);
----->			throw new Exception();
??? not port ----->			//u.user_data = luaM_realloc_(L, t);  //FIXME:???
			AddTotalBytes(L, GetUnmanagedSize(typeof(Udata)));  //FIXME:???
			return u;
		}
		
---------------------------------------

		/*
		** Get the actual string (array of bytes) from a 'TString'.
		** (Access to 'extra' ensures that value is really a 'TString'.)
		*/
--->		public static CharPtr getaddrstr(ts)	{ throw new Exception(); return null; } //(cast(char *, (ts)) + sizeof(UTString)) //FIXME:
		
-------------------------------------

		/*
		@@ LUA_EXTRASPACE defines the size of a raw memory area associated with
		** a Lua state with very fast access.
		** CHANGE it if you need a different size.
		*/
		public static int LUA_EXTRASPACE	= 4; //(sizeof(void *)) //FIXME:
		
---------------------------------------

		public static GCObject luaC_newobj<T> (lua_State L, int tt, uint sz) {
		  global_State g = G(L);
		  //FIXME:???
		  //throw new Exception();
		  GCObject o = (GCObject)(object)luaM_newobject<T>(L/*, novariant(tt), sz*/);
		  if (o is TString) //FIXME:added
		  {
		  	int len_plus_1 = (int)sz - GetUnmanagedSize(typeof(TString));
----->		  	throw new Exception();
----->		  	//((TString) o).str = new CharPtr(new char[len_plus_1]);
		  }
		  o.marked = luaC_white(g);
		  o.tt = (byte)tt; //FIXME:(byte)
		  o.next = g.allgc;
		  g.allgc = o;
		  return o;
		}
		
------------------------------------

		public static void luaC_barrierback(lua_State L, GCUnion p, TValue v) { 
			if (iscollectable(v) && isblack(p) && iswhite(gcvalue(v)))
(Table)p ---->				luaC_barrierback_(L,(Table)p); } //FIXME:???(Table)p
				
-------------------------------------

--->				public static CharPtr lua_getextraspace(lua_State L) { throw new Exception(); return null; }	//((void *)((char *)(L) - LUA_EXTRASPACE)) //FIXME:???

-----------------------------------------
-----------------------------------------
runtime
-----------------------------------------
-----------------------------------------
need get result with exe


--->			else if (t == typeof(UTString))
				return 64; //FIXME:???UTString
				
				
--------------------------------------

//if (o is TString) //FIXME:added
		  //{
		  //	int len_plus_1 = (int)sz - GetUnmanagedSize(typeof(TString));
		  //	throw new Exception();
		  //	//((TString) o).str = new CharPtr(new char[len_plus_1]);
		  //}
		  
-------------------------------

		public class Table : GCObject {
		  public lu_byte flags;  /* 1<<p means tagmethod(p) is not present */
		  public lu_byte lsizenode;  /* log2 of size of `node' array */
		  private int _sizearray;  /* size of `array' array */
for debugging------>		  public int sizearray
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
		  
-------------------------------
registry array [0] and [1] not thread and table (this bug fixed, see below setobj())


		//FIXME:added
static Table _registry;
static void lua_xxx()
{
	Table t = _registry; //_tt;
  for (int i = 1; i < 3; ++i)
  {
    TValue temp = t.array[i-1];
	if (ttisthread(temp)) //tt_ == 0x48 = 72; low 4bit: 8 is thread
	{
		Debug.WriteLine("lua_xxx 003: thread at array[" + (i-1) + "]");
	}
	if (ttistable(temp)) //tt_ == 0x45 = 69; low 4bit: 5 is table
	{
		Debug.WriteLine("lua_xxx 004: table at array[" + (i-1) + "]");
	}
  }
}	

---

found bug is here

		public static void setobj(lua_State L, TValue obj1, TValue obj2) 
io1 = obj2, value copy, not ref copy ----->		    { TValue io1=(obj1); io1.copy(obj2);
			  /*(void)L;*/ checkliveness(G(L), io1);}
			  
			  
---
method see this, not safe???

			public static void copy(lua_TValue v1, lua_TValue v2)
			{
				v1.value_ = v2.value_; 
				v1.tt_ = v2.tt_;
				//FIXME:???see setobj()
//				v1.index = v2.index;
//				v1.value_ = v2.value_;
//				v1._parent = v2._parent
			}
			
---


----------------------

		public class Udata : GCUnion { //FIXME:added
    		public lu_byte ttuv_;  /* user value's tag */
			public Table metatable;
			public uint len;  /* number of bytes */
			public Value user_;  /* user value */
			
------->			public object user_data;
		};

---
		
			u.user_data = luaM_realloc_(L, t);  //FIXME:???
			

---

		public static object getudatamem(Udata u)  {
			return u.user_data; }
			//throw new Exception(); return null; } //return check_exp(sizeof((u).ttuv_), (cast(char*, (u)) + sizeof(UUdata))); } //FIXME:???
			

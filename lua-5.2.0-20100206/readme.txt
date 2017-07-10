21:14 2017-06-24
lapi.c
21:17 2017-06-24
lapi.h
21:38 2017-06-24
lauxlib.c
21:42 2017-06-24
lauxlib.h
22:14 2017-06-24
lbaselib.c
	---->??? #if defined(LUA_COMPAT_FENV)

22:28 2017-06-24
lbitlib.c
22:41 2017-06-24
lcode.c

02:55 2017-06-25
lcode.h
02:56 2017-06-25
lctype.c
02:58 2017-06-25
lctype.h
03:04 2017-06-25
ldblib.c
03:36 2017-06-25
ldebug.c

19:11 2017-06-25
ldo.c
19:15 2017-06-25
ldo.h
19:18 2017-06-25
ldump.c
19:30 2017-06-25
lfunc.c
lfunc.h

05:44 2017-06-30
lgc.c
05:46 2017-06-30
lgc.h
05:50 2017-06-30
linit.c
05:56 2017-06-30
liolib.c
	LUALIB_API->LUAMOD_API ???
	#if !defined(lua_popen) ?????


10:01 2017/7/1
llex.c
10:09 2017/7/1
llex.h
10:19 2017/7/1
llimits.h
10:20 2017/7/1
lmathlib.c
10:24 2017/7/1
lmem.c
	luaM_realloc_ not modify
10:27 2017/7/1
lmem.h
	luaM_newobject ???
10:50 2017/7/1
loadlib.c
	LUA_DL_DLOPEN -> LUA_USE_DLOPEN
10:52 2017/7/1
lobject.c
	luaO_nilobject_ ??? deleted
11:11 2017/7/1
lobject.h
	luaO_nilobject_ move to lobject.c
	NILCONSTANT

11:18 2017/7/1
lopcodes.c
11:22 2017/7/1
lopcodes.h
11:35 2017/7/1
loslib.c
	os_date not implemented
12:07 2017/7/1
lparser.c

20:30 2017-07-01
lparser.h
20:44 2017-07-01
lstate.c
	fromstate
20:51 2017-07-01
lstate.h

09:26 2017-07-02
lstring.c
lstring.h
09:34 2017-07-02
lstrlib.c
09:43 2017-07-02
ltable.c
	isdummy
	dummynode_ ???
09:45 2017-07-02
ltable.h
09:49 2017-07-02
ltablib.c
09:51 2017-07-02
ltm.c
09:52 2017-07-02
ltm.h

21:47 2017-07-02
lua.c
22:06 2017-07-02
lua.h

03:17 2017-07-04
luaconf.h
        //FIXME:??? not defined
		#if defined(LUA_COMPAT_ALL)
		//FIXME:???not defined
		#if defined(lobject_c) || defined(lvm_c) || defined(luaall_c)
		//???
		public delegate lua_Number op_delegate(lua_State L, lua_Number a, lua_Number b); //FIXME:added
		//???
		#define luai_hashnum(i,d) { int e;  \
03:19 2017-07-04
lualib.h
03:21 2017-07-04
lundump.c
lundump.h

	//!!!!!!!!!changed!!!!!!!!!!!
	check_exp(getBMode(GET_OPCODE(i)) == OpArgK, k+GETARG_Bx(i))
	->
	#define KBx(i) 
		(k + (GETARG_Bx(i) != 0 ? GETARG_Bx(i) - 1 : GETARG_Ax(*ci->u.l.savedpc++)))

06:19 2017-07-04
lvm.c
06:21 2017-07-04
lvm.h
lzio.c
lzio.h









bug/todo:
//-----------------------------------------------
//sizeof is not good for csharp managed code, use GetUnmanagedSize(typeof()) instead
sizeof(r) => GetUnmanagedSize(typeof(r))

//-----------------------------------------------
??? fromstate() is empty, offset is ignored

        //FIXME:???not implemented
        private static LX fromstate(lua_State L) { return /*((LX)((lu_byte[])(L) - offsetof(LX, l)))*/ null; } //FIXME:???

		  GCObject o = obj2gco(luaM_newobject<T>(L/*, tt, sz*/))/* + offset*/); //FIXME:???no offset

//-------------------------------------------------
??? only account for TString

lgc.c:
		  if (o is TString) //FIXME:added
		  {
		  	int len_plus_1 = (int)sz - GetUnmanagedSize(typeof(TString));
		  	((TString) o).str = new CharPtr(new char[len_plus_1]);
		  }

//------------------------------------------------
???DBL_MAX_EXP not implemented

luaconf.h:
		public static void luai_hashnum(out int i, lua_Number d) { int e;
		  d = frexp(d, out e) * (lua_Number)(Int32.MaxValue - /*DBL_MAX_EXP*/Double.MaxValue); //FIXME:DBL_MAX_EXP==Double.MaxValue???
		  lua_number2int(out i, d); i += e; }
		  

//-------------------------------------------------
??? <= is changed to < (not only here), see below ('lgc.c: stop overflow')  

lvm.c
lua_assert(base_ <= L.top && L.top <= L.stack[L.stacksize-1]); //FIXME:L.top < L.stack[L.stacksize]??? L.stacksize >= L.stack.Length, overflow, so changed to <=
			

//------------------------------------------------
??? AddTotalBytes/SubtractTotalBytes is ignored at somewhere (maybe at luaM_newobject and lgc.c ('if (o is TString)', see upper))  

		static void AddTotalBytes(lua_State L, int num_bytes) { G(L).totalbytes += (uint)num_bytes; }
		static void SubtractTotalBytes(lua_State L, int num_bytes) { G(L).totalbytes -= (uint)num_bytes; }

		static void AddTotalBytes(lua_State L, uint num_bytes) {G(L).totalbytes += num_bytes;}
		static void SubtractTotalBytes(lua_State L, uint num_bytes) {G(L).totalbytes -= num_bytes;}
		
//------------------------------------------------







-----------------------------------------
TString 
lstring.c
		  ts = luaC_newobj<TString>(L, LUA_TSTRING, totalsize, list, 0).ts;
		  ts.tsv.len = l;
		  ts.tsv.hash = h;
		  ts.tsv.reserved = 0;
		  //memcpy(ts+1, str, l*GetUnmanagedSize(typeof(char))); //<---------------------here is changed, 'ts+1' to 'ts.str.chars' 
		  memcpy(ts.str.chars, str.chars, str.index, (int)l);

ts+1 => ts.str.chars, see 

 (1): lgc.c
		 public static GCObject luaC_newobj<T> (lua_State L, int tt, uint sz, GCObjectRef list,
		                       int offset) {
		  global_State g = G(L);
		  GCObject o = obj2gco(luaM_newobject<T>(L/*, tt, sz*/)/* + offset*/); //FIXME:???no offset
		  if (o is TString) //FIXME:added
		  {
		  	int len_plus_1 = (int)sz - GetUnmanagedSize(typeof(TString));
		  	((TString) o).str = new CharPtr(new char[len_plus_1]);
		  }
(2): lobject.h
		public class TString : TString_tsv {
			//public L_Umaxalign dummy;  /* ensures maximum alignment for strings */			
			public TString_tsv tsv { get { return this; } }

			public TString()
			{
				
			}
			//public TString(CharPtr str) { this.str = str; } //FIXME:removed

here --------->			public CharPtr str; //FIXME:added = new CharPtr()???;

			public override string ToString() { return str.ToString(); } // for debugging
		};
-----------------------------------------

lgc.c: add if and break statement, for stoping overflow


		private static void traversestack (global_State g, lua_State L) {
		  StkId o;
		  if (L.stack == null)
		    return;  /* stack not completely built yet */
		  for (o = new lua_TValue(L.stack); o < L.top; /*StkId.inc(ref o)*/o = o + 1) {//FIXME:L.stack->new StkId(L.stack[0]) //FIXME:don't use StackId.inc(), overflow ([-1])
		    markvalue(g, o);
		    
-------->		    //------------------------
		    if (o >= L.top - 1) 
		    {
		    	break;//FIXME:added, o will overflow
		    }
		    //------------------------
		  }
		  if (g.gcstate == GCSatomic) {  /* final traversal? */
		  	StkId limMinus1 = L.stack[L.stacksize-1];  /* real end of stack */ //FIXME:L.stack[L.stacksize] will overvlow, changed it
		  	for (; o <= limMinus1; /*StkId.inc(ref o)*/o = o + 1) { /* clear not-marked stack slice */ //FIXME:overflow, changed 'o < lim' to 'o <= limMinus1'
		      setnilvalue(o);
			  
-------->		      //------------------------
		      if (o >= L.top - 1)
			  {
			  	break;//FIXME:added, o will overflow
			  }
		      //------------------------
		  	}
		  }
		}
		
		
		
		
		
		
------------------------------

lapi.c	76
lapi.h	4
lauxlib.c	19
lauxlib.h	6
lbaselib.c	46
lbitlib.c	3
lcode.c	17
lcode.h	3
lctype	2
ldblib.c	13
ldebug.c	28
ldo.c	42
ldo.h	6
ldump.c	6
lfunc.c	19
lgc.c	60
lgc.h	8
linit.c	8
liolib.c	8
llex.c	3
llex.h	2
llimmits.h	19
lmathlib.c	2
lmem.c	7
lmem.h	3
loadlib.c	38
lobject.c	4
lobject.h	33
lopcodes.c	3
lopcodes.h	11
loslib.c	10
lparser.c	64
lparser.h	6
lstate.c	26
lstate.h	10
lstring.c	7
lstrlib.c	10
ltable.c	23
ltable.h	3
ltablib.c	13
ltm.c	3
ltm.h	2
ltype.h	2
lua.c	31
lua.h	24
luaconf.h	66
lualib.h	10
lundump	6
lvm.c	24
lvm.h	4


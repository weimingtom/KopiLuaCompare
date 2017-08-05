04:58 2017-07-28
lapi.c
	private static UpValRef getupvalref (lua_State L, int fidx, int n, LClosure[] pf) { //FIXME:???ref ? array ?
lapi.h
05:20 2017-07-28
lauxlib.c
05:21 2017-07-28
lauxlib.h
	#if defined(LUA_COMPAT_MODULE)

21:55 2017-07-28
lbaselib.c

21:18 2017-07-29
lbitlib.c

03:51 2017-08-01
lcode.c
03:52 2017-08-01
lcode.h
lcorolib.c
03:58 2017-08-01
lctype.c
	#if !LUA_USE_CTYPE	///* { */
04:09 2017-08-01
lctype.h
	#define LUA_USE_CTYPE	0
	#if !defined(LUA_USE_CTYPE)
04:13 2017-08-01
ldblib.c
04:31 2017-08-01
ldebug.c
	StkId pos = new StkId()= 0;  /* to avoid warnings */
	StkId pos = new StkId() = 0;  /* to avoid warnings */
04:32 2017-08-01
ldebug.h
04:45 2017-08-01
ldo.c
	Cfunc:
ldo.h

15:03 2017/8/2
ldump.c
lfunc.c
lfunc.h
15:25 2017/8/2
lgc.c
15:31 2017/8/2
lgc.h
	public static void luaC_condGC(L,c) {
			{if (G(L).GCdebt > 0) {c;}; condchangemem(L);} } //FIXME:???macro
		public static void luaC_checkGC(lua_State L) {luaC_condGC(L, ()=>{luaC_step(L);}} 		//FIXME: macro in {}
15:32 2017/8/2
linit.c

20:36 2017-08-02
liolib.c
	LStream p = (LStream)lua_newuserdata(L, typeof(LStream));->typeof(LStream)
	private static int read_number (lua_State L, Stream f) {
	  //lua_Number d; //FIXME:???
	  object[] parms = { (object)(double)0.0 }; //FIXME:???
	private const int MAX_SIZE_T = (~(size_t)0);
	//
	int[] mode = { SEEK_SET, SEEK_CUR, SEEK_END }; //FIXME: ???static const???
	CharPtr[] modenames = { "set", "cur", "end", null }; //FIXME: ???static const???
	//Flush->fflush()
	private static int io_flush (lua_State L) {
		int result = 1;//FIXME: added
		try {getiofile(L, IO_OUTPUT).Flush();} catch {result = 0;}//FIXME: added
	  return luaL_fileresult(L, result, null); //FIXME: changed
	}
	private static int f_flush (lua_State L) {
		int result = 1;//FIXME: added
		try {tofile(L).Flush();} catch {result = 0;} //FIXME: added
		return luaL_fileresult(L, result, null); //FIXME: changed
	}	



02:07 2017-08-03
llex.c
	ls.decpoint = '.'; // (cv ? cv.decimal_point[0] : '.'); //getlocaledecpoint() //FIXME:changed
	UCHAR_MAX
02:09 2017-08-03
llex.h
	public static lua_Number cast_num(i) { return (lua_Number)i; } //FIXME:???remove?
	public static int cast_int(i) { return (int)i; } //FIXME:???remove?
	public static byte cast_uchar(i) { return (byte)(i)); } //FIXME:???remove?
02:24 2017-08-03
llimits.h
	//------------------>FIXME: below ignore???, TODO
	//#define condchangemem(L)  \
	//	((void)(!(G(L)->gcrunning) || (luaC_fullgc(L, 0), 1)))
02:28 2017-08-03
lmathlib.c
	//#if defined(LUA_COMPAT_LOG10)
			private static int math_log10 (lua_State L) {
	#if defined(LUA_COMPAT_LOG10)
			  new luaL_Reg("log10", math_log10),
	#endif
02:29 2017-08-03
lmem.c
	//FIXME: not sync, no gc below
02:30 2017-08-03
lmem.h
02:48 2017-08-03
loadlib.c
	#if defined(LUA_COMPAT_MODULE)
			  new luaL_Reg("seeall", ll_seeall),
	#endif
	#if defined(LUA_COMPAT_MODULE)
			  new luaL_Reg("module", ll_module),
	#endif
	#if defined(LUA_COMPAT_LOADERS)
			  lua_pushvalue(L, -1);  /* make a copy of 'searchers' table */
			  lua_setfield(L, -3, "loaders");  /* put it in field `loaders' */
	#endif



8:45 2017/8/3
lobject.c
10:45 2017/8/3
lobject.h
	/*
	** Union of all Lua values
	*/
	typedef union Value Value;
	#define numfield	lua_Number n;    /* numbers */
	/*
	** Tagged Values. This is the basic representation of values in Lua,
	** an actual value plus a tag with its type.
	*/
	#define TValuefields	Value value_; int tt_
	typedef struct lua_TValue TValue;
	//-----------------------
	//struct lua_TValue {
	//  TValuefields;
	//};
	//typedef TValue *StkId;  /* index to stack elements */	
	//------------------------
	public CharPtr str; //FIXME:added = new CharPtr()???;
	public override string ToString() { return str.ToString(); } // for debugging
	//------------------------
	// in the original C code this was allocated alongside the structure memory. it would probably
	// be possible to still do that by allocating memory and pinning it down, but we can do the
	// same thing just as easily by allocating a seperate byte array for it instead.
	public object user_data;
	//--------------------------
10:47 2017/8/3
lopcodes.c
10:50 2017/8/3
lopcodes.h

13:15 2017/8/3
loslib.c	
		private static int os_execute (lua_State L) {
		  CharPtr cmd = luaL_optstring(L, 1, NULL);
		  int stat = system(cmd);
		private static int os_remove (lua_State L) {
		  CharPtr filename = luaL_checkstring(L, 1);
		  return luaL_fileresult(L, remove(filename) == 0, filename);		  	
		private static int os_rename (lua_State L) {
		  CharPtr fromname = luaL_checkstring(L, 1);
		  CharPtr toname = luaL_checkstring(L, 2);
		  return luaL_fileresult(L, rename(fromname, toname) == 0, fromname);
		private static int os_clock (lua_State L) {
		  lua_pushnumber(L, ((lua_Number)clock())/(lua_Number)CLOCKS_PER_SEC);
		private static int os_date (lua_State L) {
				  CharPtr s = luaL_optstring(L, 1, "%c");
17:05 2017/8/3
lparser.c 
	//------------------------------
			MAX_INT->Int32.MaxValue
	//------------------------------
	lastfunc = f; //FIXME:added, ???
	//------------------------------
		  for (int i = 0; i < f.p.Length; i++) //FIXME:added
		  {
			  f.p[i].protos = f.p;
			  f.p[i].index = i;
		  }
	//------------------------------









20:53 2017-08-03
lparser.h
21:00 2017-08-03
lstate.c
		//FIXME:???not implemented
        private static LX fromstate(lua_State L) { 
		 throw new Exception("not implemented"); //FIXME:???
		 return /*((LX)((lu_byte[])(L) - offsetof(LX, l)))*/ null; 
21:04 2017-08-03
lstate.h
21:09 2017-08-03
lstring.c
lstring.h
21:21 2017-08-03
lstrlib.c
	if ((uint)(p - strfrmt) >= (FLAGS.Length+1)) //FIXME:???sizeof(FLAGS)/sizeof(char)
	if ((uint)(p - strfrmt) >= (FLAGS.Length)) //FIXME:???sizeof(FLAGS)/sizeof(char), ?+1
21:27 2017-08-03
ltable.c
	/* else go through */
	//FIXME:added ???this is not beautiful, use goto default
ltable.h
21:33 2017-08-03
ltablib.c
21:35 2017-08-03
ltm.c
21:36 2017-08-03
ltm.h
21:40 2017-08-03
lua.c
21:42 2017-08-03
lua.h


02:10 2017-08-04
luaconf.h
		//--------------
		public static int lua_number2str(ref CharPtr s, double n) { s = String.Format("{0}", n); return strlen(s); } //FIXME:changed, sprintf->String.Format //FIXME: not assign, fill
		->
		public static int lua_number2str(ref CharPtr s, double n) { return sprintf(s, LUA_NUMBER_FMT, n); } //FIXME:changed, sprintf->String.Format //FIXME: not assign, fill
		//---------------
		public static double lua_str2number(CharPtr s, out CharPtr end)
		{			
			end = new CharPtr(s.chars, s.index);
			string str = "";
			while (end[0] == ' ')
				end = end.next();
			while (number_chars.IndexOf(end[0]) >= 0)
			{
				str += end[0];
				end = end.next();
			}

			try
			{
				return Convert.ToDouble(str.ToString());
			}
			catch (System.OverflowException)
			{
				// this is a hack, fix it - mjf
				if (str[0] == '-')
					return System.Double.NegativeInfinity;
				else
					return System.Double.PositiveInfinity;
			}
			catch
			{
				end = new CharPtr(s.chars, s.index);
				return 0;
			}
		}
		->
		public static double lua_str2number(CharPtr s, out CharPtr p) { return strtod(s, p); }
		#if defined(LUA_USE_STRTODHEX)
				public static double lua_strx2number(CharPtr s, out CharPtr p) { return strtod(s, p); }
		#endif
		//-----------------
		#define LUA_NANTRICKLE
		//-----------------
		public delegate lua_Number op_delegate(lua_State L, lua_Number a, lua_Number b); //FIXME:added ???
		//-----------------
02:14 2017-08-04
lualib.h		
		#if !defined(lua_assert)
		#define lua_assert(x)	((void)0)
		#endif
02:31 2017-08-04
lundump.c
		LoadVar(S,x);
		->
		x = (int)LoadVar(S, typeof(int)); //FIXME: changed
		//-------------
		public static void luaU_header(lu_byte[] h) //FIXME:changed, lu_byte*
		{
		 int x=1;
		 memcpy(h, LUA_SIGNATURE, LUA_SIGNATURE.Length); //FIXME:changed, sizeof(LUA_SIGNATURE)-sizeof(char) 
		 h = h.add(LUA_SIGNATURE.Length); //FIXME:changed, sizeof(LUA_SIGNATURE)-sizeof(char);
		 h[0] = (byte)LUAC_VERSION; h.inc();
		 h[0] = (byte)LUAC_FORMAT; h.inc();
		 h[0] = (byte)x; h.inc();				/* endianness */ //FIXME:changed, *h++=cast_byte(*(char*)&x);
		 h[0] = (byte)GetUnmanagedSize(typeof(int)); h.inc();
		 h[0] = (byte)GetUnmanagedSize(typeof(uint)); h.inc();
		 h[0] = (byte)GetUnmanagedSize(typeof(Instruction)); h.inc();
		 h[0] = (byte)GetUnmanagedSize(typeof(lua_Number)); h.inc();
         h[0] = (byte)(((lua_Number)0.5)==0 ? 1 : 0); h.inc();		/* is lua_Number integral? */ //FIXME:???always 0 on this build
		 memcpy(h,LUAC_TAIL,sizeof(LUAC_TAIL)-sizeof(char));
		}
02:34 2017-08-04
lundump.h

15:57 2017/8/4
lvm.c
	//----------------------
	if (mybuff == null) //FIXME:added
		mybuff = buffer; //FIXME:added
	//----------------------
	//???????macro
	/* execute a jump instruction */
		#define dojump(ci,i,e) \
		  { int a = GETARG_A(i); \
		    if (a > 0) luaF_close(L, ci->u.l.base + a - 1); \
		    ci->u.l.savedpc += GETARG_sBx(i) + e; }
	//---------------------
	//????????macro
	//#define checkGC(L,c)	Protect(luaC_checkGC(L); luai_threadyield(L);) //FIXME:
	//------------------------
		OP_NEWTABLE
		//
	        checkGC(L,
          L->top = ra + 1;  /* limit of live values */
          luaC_step(L);
          L->top = ci->top;  /* restore top */
    //-------------------------
		OP_CONCAT
		//
        ra = RA(i);  /* 'luav_concat' may invoke TMs and move the stack */
        rb = b + base;
        setobjs2s(L, ra, rb);
        checkGC(L,
          L->top = (ra >= rb ? ra + 1 : rb);  /* limit of live values */
          luaC_step(L);
        )
    //-----------------------
	vmcase->add this line:InstructionPtr.inc(ref ci.u.l.savedpc);
	???
	//----------------------
	OP_CLOSURE
				checkGC(L,
		          L->top = ra + 1;  /* limit of live values */
		          luaC_step(L);
		          L->top = ci->top;  /* restore top */
		        )
	//----------------------
16:00 2017/8/4
lvm.h
16:02 2017/8/4
lzio.c
16:07 2017/8/4
lzio.h
			public static void luaZ_resizebuffer(lua_State L, Mbuffer buff, int size) {
			if (buff.buffer == null) //FIXME:added
				buff.buffer = new CharPtr(); //FIXME:added







-------------------------------------------------------------


public static int L_tmpnam = 256; //FIXME:???


-----------------------
lparser,lvm
-----------------------

public static void dojump(CallInfo ci, Instruction i, int e, lua_State L) //FIXME:???Instruction???InstructionPtr???


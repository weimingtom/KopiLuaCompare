TODO: lua.c: some methods without private or public
TODO: lua.c: dolua_, not ehecked, maybe not work
TODO: lauxlib.c: luaL_opt may be wrong, should not be ?1:0


----------------------

9:27 2019/11/22
lapi.c
9:32 2019/11/22
lauxlib.c
9:33 2019/11/22
lauxlib.h
9:44 2019/11/22
lbaselib.c
9:17 2019/11/23
lbitlib.c
9:21 2019/11/23
lcode.c
9:25 2019/11/23
lcorolib.c
10:07 2019/11/23
ldblib.c
10:15 2019/11/23
ldo.c
10:21 2019/11/23
ldump.c
10:28 2019/11/23
lfunc.c
10:30 2019/11/23
lfunc.h
11:00 2019/11/23
lgc.c
11:03 2019/11/23
linit.c

7:25 2019/11/24
liolib.c
7:38 2019/11/24
llex.c
7:42 2019/11/24
llimits.h
8:10 2019/11/24
lmathlib.c
8:14 2019/11/24
lobject.c
9:03 2019/11/24
lobject.h
9:05 2019/11/24
loslib.c
10:34 2019/11/24
lparser.c
10:35 2019/11/24
lparser.h
10:39 2019/11/24
lstate.h
10:42 2019/11/24
lstring.c
10:51 2019/11/24
lstrlib.c
15:49 2019/11/24
ltable.c
15:53 2019/11/24
ltablib.c
15:55 2019/11/24
ltm.c
15:56 2019/11/24
ltm.h
16:23 2019/11/24
lua.c
lua.h
16:27 2019/11/24
luac.c


14:31 2019/11/25
luaconf.h
15:16 2019/11/25
lundump.c
15:18 2019/11/25
lundump.h
15:21 2019/11/25
lutf8lib.c
15:24 2019/11/25
lvm.h
16:19 2019/11/25
lvm.c














------------------------------

1. 
		private static CharPtr b_str2int (CharPtr s, int base_, ref lua_Integer pn) {
----->		  s = new CharPtr(s); //FIXME:???

------------------------------

2. 
string ==, ???may be bug


		private static int luaB_tonumber (lua_State L) {
		  if (lua_isnoneornil(L, 2)) {  /* standard conversion? */
		    luaL_checkany(L, 1);
		    if (lua_type(L, 1) == LUA_TNUMBER) {  /* already a number? */
		      lua_settop(L, 1);  /* yes; return it */
		      return 1;
		    }
		    else {
		      uint l;
		      CharPtr s = lua_tolstring(L, 1, out l);
--------->		      if (s != null && lua_strtonum(L, s) == l + 1)
		        return 1;  /* successful conversion to number */
		      /* else not a number */
		    }
		  }
		  else {
		    uint l;
		    CharPtr s;
		    lua_Integer n = 0;  /* to avoid warnings */
		    int base_ = luaL_checkint(L, 2);
		    luaL_checktype(L, 1, LUA_TSTRING);  /* before 'luaL_checklstring'! */
		    s = luaL_checklstring(L, 1, out l);
		    luaL_argcheck(L, 2 <= base_ && base_ <= 36, 2, "base out of range");
--------->		    if (b_str2int(s, base, ref n) == s + l) {
		      lua_pushinteger(L, n);
		      return 1;
		    }  /* else not a number */
		  }  /* else not a number */
		  lua_pushnil(L);  /* not a number */
		  return 1;
		}

------------------------------


		public static CClosure luaF_newCclosure (lua_State L, int n) {
		  GCObject o = luaC_newobj<Closure>(L, LUA_TCCL, sizeCclosure(n));
		  CClosure c = gco2ccl(o);
		  c.nupvalues = cast_byte(n);
--->		  c.c.upvalue = new TValue[n]; //FIXME:added???
--->		  for (int i = 0; i < n; i++)  //FIXME:added???
--->			  c.c.upvalue[i] = new lua_TValue(); //FIXME:??? //FIXME:added???
		  return c;
		}


		public static Closure luaF_newLclosure (lua_State L, int n) {
		  Closure c = luaC_newobj<Closure>(L, LUA_TLCL, sizeLclosure(n)).cl;
		  c.l.p = null;
		  c.l.nupvalues = cast_byte(n);
--->		  c.l.upvals = new UpVal[n]; //FIXME:added???
--->		  /*
--->		  for (int i = 0; i < n; i++) //FIXME:added???
--->			  c.l.upvals[i] = new UpVal(); //FIXME:??? //FIXME:added???
--->		  while (n > 0) c.l.upvals[n] = null; //FIXME:added??? while (n--) c->l.upvals[n] = NULL;
--->		  */
		  while (n-- != 0) c.l.upvals[n] = null;
		  return c;
		}
		
		
-------------------------------

public->private

--->		private static void luaC_upvalbarrier_ (lua_State L, UpVal uv) {
		
		
		
		
-------------------------------

		  new luaL_Reg(LUA_MATHLIBNAME, luaopen_math),
		  new luaL_Reg(LUA_DBLIBNAME, luaopen_debug),
		  new luaL_Reg(LUA_UTF8LIBNAME, luaopen_utf8),
--->		//#if defined(LUA_COMPAT_BITLIB)
--->		  new luaL_Reg(LUA_BITLIBNAME, luaopen_bit32),	
--->		//#endif	
		
-------------------------------
	

private static Random rng = new Random(); //FIXME:added
		/*
		** This function uses 'double' (instead of 'lua_Number') to ensure that
		** all bits from 'l_rand' can be represented, and that 'RAND_MAX + 1.0'
		** will keep full precision (ensuring that 'r' is always less than 1.0.)
		*/
		private static int math_random (lua_State L) {
		  lua_Integer low, up;
		  double r = (double)l_rand() * (1.0 / ((double)RAND_MAX + 1.0));
		  switch (lua_gettop(L)) {  /* check number of arguments */
			case 0: {  /* no arguments */
			  lua_pushnumber(L, r);  /* Number between 0 and 1 */
			  return 1;
			}
			case 1: {  /* only upper limit */
		  	  low = 1;
      		  up = luaL_checkinteger(L, 1);
			  break;
			}
			case 2: {  /* lower and upper limits */
		      low = luaL_checkinteger(L, 1);
      		  up = luaL_checkinteger(L, 2);
			  break;
			}
			default: return luaL_error(L, "wrong number of arguments");
		  }
		  /* random integer in the interval [low, up] */
		  luaL_argcheck(L, low <= up, 1, "interval is empty"); 
		  luaL_argcheck(L, (lua_Unsigned)up - low <= (lua_Unsigned)LUA_MAXINTEGER,
		                   1, "interval too large");
		  r *= (double)(up - low) + 1.0;
		  lua_pushinteger(L, (lua_Integer)r + low);
		  return 1;
		}
		
------------------------------------

		private static int math_randomseed (lua_State L) {
		  rng = new Random((uint)luaL_checkunsigned(L, 1)); //FIXME:changed - l_srand((unsigned int)luaL_checkunsigned(L, 1)); //FIXME:added, (int)
		  rng.Next(); /* discard first value to avoid undesirable correlations */ //FIXME:changed - (void)rand();
		  return 0;
		}
		

------------------------------------

		/* reasonable limit to avoid arithmetic overflow and strings too big */
------>		//#if LUA_MAXINTEGER / 2 <= 0x10000000
------>		public const uint MAXSIZE = (uint)(int.MaxValue / 2); //FIXME: //((size_t)(LUA_MAXINTEGER / 2));
		//#else
		//#define MAXSIZE		((size_t)0x10000000)
		//#endif
		
------------------------------------

		/*
		** Interface to 'lua_pcall', which sets appropriate message function
		** and C-signal handler. Used to run all chunks.
		*/
		static int docall(Lua.lua_State L, int narg, int nres) {
			int status;
			int base_ = Lua.lua_gettop(L) - narg;  /* function index */
			Lua.lua_pushcfunction(L, msghandler);  /* push message handler */
			Lua.lua_insert(L, base_);  /* put it under function and args */
            globalL = L;  /* to be available to 'laction' */
--->			//signal(SIGINT, laction);  /* set C-signal handler */ //FIXME:removed
			status = Lua.lua_pcall(L, narg, nres, base_);
--->			//signal(SIGINT, SIG_DFL); /* reset C-signal handler */ //FIXME:removed
			Lua.lua_remove(L, base_);  /* remove message handler from the stack */
			return status;
		}
		
--------------------------------------

		//#if !defined(l_rand)		/* { */
		//#if defined(LUA_USE_POSIX)
---->		//#define l_rand()	random()
---->		//#define l_srand(x)	srandom(x)
		//#else
		//#define l_rand()	rand()
		//#define l_srand(x)	srand(x)
		//#endif
		//#endif				/* } */
		

		private static int math_random (lua_State L) {
		  lua_Integer low, up;
---->		  //double r = (double)l_rand() * (1.0 / ((double)RAND_MAX + 1.0));
		  double r = (lua_Number)rng.NextDouble();
		  

		private static int math_randomseed (lua_State L) {
---->		  rng = new Random((uint)luaL_checkunsigned(L, 1)); //FIXME:changed - l_srand((unsigned int)luaL_checkunsigned(L, 1)); //FIXME:added, (int)
		  
		  
------------------------------------
redefine, two getlocaledecpoint


//#if !defined(getlocaledecpoint)
--->		//private static char getlocaledecpoint() { return localeconv().decimal_point[0];}
		//#endif
		
---------------------

		//#define LUA_MAXUNSIGNED		UINT_MAX
		public const int LUA_MAXINTEGER = int.MaxValue; //#define LUA_MAXINTEGER		INT_MAX
		public const int LUA_MININTEGER = int.MinValue //#define LUA_MININTEGER		INT_MIN
		
---------------------

		public static int intop_shiftleft(lua_Integer v1, lua_Integer v2) { return l_castU2S((int)l_castS2U(v1) << (int)l_castS2U(v2));} //FIXME:???(int)
		public static int intop_shiftright(lua_Integer v1, lua_Integer v2) { return l_castU2S((int)l_castS2U(v1) >> (int)l_castS2U(v2));} //FIXME:???(int)
		
-----------------
		    g.GCestimate = (uint)(g.GCestimate + (g.GCdebt - olddebt)); //FIXME: g.GCestimate += g.GCdebt - olddebt;  /* update estimate */

-----------------

g.GCestimate = (uint)(g.GCestimate + g.GCdebt - olddebt);//g.GCestimate += g.GCdebt - olddebt;  /* update estimate */
		    
		    
--------------------

(int)????

		    case LUA_OPBNOT: return intop_xor((int)(~l_castS2U(0)), v1);
		    
------------------

case OpCode.OP_BNOT: {
		        TValue rb = RB(L, base_, i);
		        lua_Integer ib = 0;
		        if (0!=tointeger(ref rb, ref ib)) {
--->		          setivalue(ra, intop_xor((int)~l_castS2U(0), ib));
		        }
		        
-----------------
How to convert LClosure and CClosure to GCObject:

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!!!Please use (ClosureHeader)x_, 
LClosure->ClosureHeader(overide operator, see LClosure definition)->GCObject
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!




		public static void setclLvalue(lua_State L, TValue obj, LClosure x)
	      { TValue io = obj; LClosure x_ = (LClosure)x;
---->		     io.value_.gc = obj2gco((ClosureHeader)x_); settt_(io, ctb(LUA_TLCL)); //FIXME:chagned, val_(io)
			 checkliveness(G(L),io); }

		public static void setclCvalue(lua_State L, TValue obj, CClosure x)
		  { TValue io = obj; CClosure x_ = (CClosure)x;
---->			 io.value_.gc = obj2gco((ClosureHeader)x_); settt_(io, ctb(LUA_TCCL)); //FIXME:chagned, val_(io)
			 checkliveness(G(L),io); }
			 
	
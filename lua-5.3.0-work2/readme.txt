10:44 2019/10/20
ltablib.c
10:45 2019/10/20
lapi.c
11:10 2019/10/20
lauxlib.c
11:36 2019/10/20
lauxlib.h
11:41 2019/10/20
lbaselib.c

20:40 2019/10/26
lbitlib.c
20:52 2019/10/26
lcode.c
20:54 2019/10/26
lcode.h
20:56 2019/10/26
ldblib.c
10:07 2019/10/27
ldebug.c
10:08 2019/10/27
ldebug.h
10:17 2019/10/27
ldo.c
10:49 2019/10/27
ldump.c

9:28 2019/10/29
lfunc.c
9:30 2019/10/29
lfunc.h
10:29 2019/10/29
lgc.c

4:30 2019/10/31
lgc.h
6:08 2019/10/31
linit.c
4:38 2019/11/1
liolib.c
4:50 2019/11/1
llex.c
4:57 2019/11/1
llex.h
5:02 2019/11/1
llimits.h
5:10 2019/11/1
lmathlib.c

15:08 2019/11/1
loadlib.c
16:53 2019/11/1
lobject.c
17:02 2019/11/1
lobject.h
17:05 2019/11/1
lopcodes.c
17:07 2019/11/1
lopcodes.h

4:53 2019/11/2
loslib.c
5:11 2019/11/2
lparser.c
5:12 2019/11/2
lparser.h



14:55 2019/11/2
lstate.c
15:10 2019/11/2
lstate.h
15:23 2019/11/2
lstring.c
15:24 2019/11/2
lstring.h
15:30 2019/11/2
lstrlib.c

5:35 2019/11/3
ltable.c
6:16 2019/11/3
ltable.h
6:19 2019/11/3
ltm.c
6:21 2019/11/3
ltm.h


---------------------

16:30 2019/11/3
lua.c
16:35 2019/11/3
lua.h
16:40 2019/11/3
luac.c
16:56 2019/11/3
luaconf.h
16:57 2019/11/3
lualib.h



16:58 2019/11/3
lzio.h
16:59 2019/11/3
lvm.h
17:11 2019/11/3
lvm.c

20:21 2019/11/3
lundump.h
20:43 2019/11/3
lundump.c







---------------------------

1.
(can remove)
public static CharPtr luaL_checklstring(lua_State L, int arg) {uint len; return luaL_checklstring(L, arg, out len);}

...and so on

---------------------------
2. 
(???)

		private static void correctstack (lua_State L, TValue[] oldstack) {
		   //FIXME:???
-------->			/* don't need to do this
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

...

		public static void luaD_shrinkstack (lua_State L) {
		  int inuse = stackinuse(L);
		  int goodsize = inuse + (inuse / 8) + 2*EXTRA_STACK;
		  if (goodsize > LUAI_MAXSTACK) goodsize = LUAI_MAXSTACK;
		  if (L->stacksize > LUAI_MAXSTACK)  /* was handling stack overflow? */
		    luaE_freeCI(L);  /* free all CIs (list grew because of an error) */
		  else
		    luaE_shrinkCI(L);  /* shrink list */	  
		  if (inuse > LUAI_MAXSTACK ||  /* still handling stack overflow? */
		      goodsize >= L.stacksize) {  /* would grow instead of shrink? */
-------->		    ;//FIXME:???//condmovestack(L);  /* don't change stack (change only for debugging) */
		  } else
		    luaD_reallocstack(L, goodsize);  /* shrink it */
		}
		


-----------------------------
4. DumpBlock



		/*
		** All high-level dumps go through DumpVector; you can change it to
		** change the endianess of the result
		*/
#define DumpVector(v,n,D)	DumpBlock(v,(n)*sizeof((v)[0]),D)

#define DumpLiteral(s,D)	DumpBlock(s, sizeof(s) - sizeof(char), D)
/*		
		public static void DumpMem(object b, DumpState D)
		{
			int size = Marshal.SizeOf(b);
			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(b, ptr, false);
			byte[] bytes = new byte[size];
			Marshal.Copy(ptr, bytes, 0, size);
			char[] ch = new char[bytes.Length];
			for (int i = 0; i < bytes.Length; i++)
				ch[i] = (char)bytes[i];
			CharPtr str = ch;
			DumpBlock(str, (uint)str.chars.Length, D);
		}

		public static void DumpMem(object b, int n, DumpState D)
		{
			Array array = b as Array;
			Debug.Assert(array.Length == n);
			for (int i = 0; i < n; i++)
				DumpMem(array.GetValue(i), D);
		}

		public static void DumpVar(object x, DumpState D)
		{
			DumpMem(x, D);
		}
*/
----------------------------------
5. 

		/*
		** create a new collectable object (with given type and size) and link
		** it to 'allgc' list.
		*/
		public static GCObject luaC_newobj<T> (lua_State L, int tt, uint sz) {
		  global_State g = G(L);
		  GCObject o = (GCObject)luaM_newobject<T>(L/*, novariant(tt), sz*/);
-------->		  if (o is TString) //FIXME:added
		  {
		  	int len_plus_1 = (int)sz - GetUnmanagedSize(typeof(TString));
		  	((TString) o).str = new CharPtr(new char[len_plus_1]);
		  }
		  gch(o).marked = luaC_white(g);
		  gch(o).tt = (byte)tt; //FIXME:(byte)
		  gch(o).next = g.allgc;
		  g.allgc = o;
		  return o;
		}
		


--------------------------------------


6.

    		case LUA_TUSERDATA: 
-------->				//luaM_freemem(L, o, sizeudata(gco2u(o)));
				luaM_freemem(L, gco2u(o), sizeudata(gco2u(o))); //FIXME:???
				break;
				

--------------------------------------

7.


???---->		public const lua_Unsigned MAX_UINTEGER = (~(lua_Unsigned)0);


----------------------------------------
8.

		//#if !defined(LUA_USE_POSIX)
----->		private static CharPtr[] LUA_STRFTIMEOPTIONS = new CharPtr[] { "aAbBcdHIjmMpSUwWxXyYz%", "" };
		//#else
		//#define LUA_STRFTIMEOPTIONS \
----->		//{ "aAbBcCdDeFgGhHIjmMnprRStTuUVwWxXyYzZ%", "", \
		//  "E", "cCxXyY",  \
		//	  "O", "deHImMSuUVwWy" }
		//#endif
	
-----------------------------------------

9.

os_date
os_time
os_difftime

not ported

------------------------------------------
10.

add (lu_byte) --->		  g.currentwhite = (lu_byte)bitmask(WHITE0BIT);
		  
------------------------------------------

11.

FIXME:??? remain, not removed --->		  lu_byte marked = L.marked;	// can't pass properties in as ref ???//FIXME:??? //FIXME:added
--->		  L.marked = marked; //remove this //FIXME:??? //FIXME:added
	
	  
--------------------------------------------

12. why???

			public GCheader gch {get{return (GCheader)this;}}   /* common header */
			public TString ts {get{return (TString)this;}}
			public Udata u {get{return (Udata)this;}}
			public Closure cl {get{return (Closure)this;}}
			public Table h {get{return (Table)this;}}
---->			public Proto p {get{return (Proto)this;}}
removed--> public UpVal uv {get{return (UpVal)this;}}	


----------------------------------------------

13. double define

		//FIXME:here changed
		public static Udata luaS_newudata(lua_State L, uint s)
		{
		    Udata u;
		    if (s > MAX_SIZE - GetUnmanagedSize(typeof(Udata)))
			  luaM_toobig(L);
		    u = luaC_newobj<Udata>(L, LUA_TUSERDATA, (uint)(GetUnmanagedSize(typeof(Udata)) + s)).u; //FIXME:(uint)
			u.uv.len = s;
			u.uv.metatable = null;
			setuservalue(L, u, luaO_nilobject);
			return u;
		}
		
		//FIXME:added
		public static Udata luaS_newudata(lua_State L, Type t)
		{
		    Udata u;
		    uint s = (uint)GetUnmanagedSize(t);
		    if (s > MAX_SIZE - GetUnmanagedSize(typeof(Udata)))
			  luaM_toobig(L);
		    u = luaC_newobj<Udata>(L, LUA_TUSERDATA, (uint)(GetUnmanagedSize(typeof(Udata)) + s)).u; //FIXME:(uint)
			u.uv.len = 0;//FIXME:s;
			u.uv.metatable = null;
			setuservalue(L, u, luaO_nilobject);
			u.user_data = luaM_realloc_(L, t);  //FIXME:???
			AddTotalBytes(L, GetUnmanagedSize(typeof(Udata)));  //FIXME:???
			return u;
		}
		
-----------------------------------------------


14.

//static const Node dummynode_ = {
		  //{NILCONSTANT},  /* value */
		  //{{NILCONSTANT, 0}}  /* key */
		//};
		public static Node dum
		
-----------------------------------------------

15.

sizeof(Table)
->
(uint)GetUnmanagedSize(typeof(Table))

-----------------------------------------------
16. 

n->node, not need ---->			  Node node = mainposition(t, key); //FIXME: n->node
			  do {  /* check whether `key' is somewhere in the chain */
				if (luaV_rawequalobj(gkey(node), key) != 0)//FIXME: n->node
				  return gval(node);  /* that's it *///FIXME: n->node
---->				else node = gnext(node);//FIXME: n->node
---->			  } while (node != null);//FIXME: n->node
			  return luaO_nilobject;
			}
		  }
		  
		  
--------------------------------------------------

17. lundump.c

  int x;
  LoadVar(S, x);
  return x;
->
 
  int x;
  x = (int)LoadVar(S, typeof(int)); //FIXME: changed
  return x;
  
   
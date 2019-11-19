TODO: sizeof->GetUnmangedSize(typeof(xxx))
TODO:一些存疑的地方，搜索throw new Exception();
TODO:offsetof，已经全部改成_parent成员实现，搜索FIXME:added, see offsetof
TODO:lundump.c和ldump.c的实现都没有实际测试过，可能有bug

--------------------------

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
  
----------------------------------------------------

18. 

/*int*/byte oldrunning = g.gcrunning; //FIXME: int->byte

--------------------------------------------------------


19. 

		/*
		** All high-level dumps go through DumpVector; you can change it to
		** change the endianess of the result
		*/
not implemented --->		public static void DumpVector(object[] v, int n, DumpState D)	{ throw new Exception(); /*DumpBlock(v,(n)*sizeof(v[0]),D);*/ }

not implemented --->		public static void DumpLiteral(string s, DumpState D)	{ throw new Exception(); DumpBlock(new CharPtr(s), (uint)((s.Length + 1) - 1/*sizeof(char)*/), D); }
		
		
----------------------------------------------------------

20.

not implemented----->		public static void DumpVar(double x, DumpState D)	{ throw new Exception(); /*DumpVector(&x,1,D);*/ }


-----------------------------------------------------------

21. 

		//FIXME:
		private class nativeendian_union {
		  public int dummy;
		  public char little;  /* true iff machine is little endian */
		  
		  public nativeendian_union(int dummy)
		  {
		  	this.dummy = dummy;
		  }
		};
used to get endian, not implemented---------->		private static nativeendian_union nativeendian = new nativeendian_union(1);


------------------------------------

22. 

		/*
		** All high-level loads go through LoadVector; you can change it to
		** adapt to the endianess of the input
		*/
		private static void LoadVector(LoadState S, object[] b, int n)	{ throw new Exception(); /*LoadBlock(S,b,(n)*sizeof((b)[0]));*/ }

		
------------------------------------
23.

		private static void LoadVar(LoadState S, object x)		{ throw new Exception(); /*LoadVector(S,&x,1);*/ }
		->
		private static object LoadVar(LoadState S, object x)		{ throw new Exception(); return 0;/*LoadVector(S,&x,1);*/ }
		
--------------------------------------

24. 

		/* returns the key, given the value of a table entry */
		public static TValue keyfromval(object v) {
------>			throw new Exception(); return null; } //(gkey((Node)(object)(v)) - offsetof(Node, i_val)))); }
				

--------------------------------------

25.

		private static int unpackfloat_l (lua_State L) {
		  lua_Number res = 0;
		  uint len;
		  CharPtr s = luaL_checklstring(L, 1, out len);
		  lua_Integer pos = posrelat(luaL_optinteger(L, 2, 1), len);
		  int size = getfloatsize(L, 3);
		  luaL_argcheck(L, 1 <= pos && (uint)pos + size - 1 <= len, 1,
		                   "string too short");
		  if (size == sizeof(lua_Number)) {
-->		  	throw new Exception();
		  	memcpy(CharPtr.FromNumber(res), s + pos - 1, size);
		  	correctendianess(L, CharPtr.FromNumber(res), size, 4);
		  }
		  else if (size == sizeof(float)) {
		    float f = 0;
-->		    throw new Exception();
		    memcpy(CharPtr.FromNumber(f), s + pos - 1, size);
		    correctendianess(L, CharPtr.FromNumber(f), size, 4);
		    res = (lua_Number)f;
		  }  
		  else {  /* native lua_Number may be neither float nor double */
		    double d = 0;
		    lua_assert(size == sizeof(double));
-->		    throw new Exception();
		    memcpy(CharPtr.FromNumber(d), s + pos - 1, size);
		    correctendianess(L, CharPtr.FromNumber(d), size, 4);
		    res = (lua_Number)d;
		  }
		  lua_pushnumber(L, res);
		  return 1;
		}
		
-------------------------------------


private static int packfloat_l (lua_State L) {
		  float f;  double d;
		  CharPtr pn;  /* pointer to number */
		  lua_Number n = luaL_checknumber(L, 1);
		  int size = getfloatsize(L, 2);
		  if (size == sizeof(lua_Number))
--->		  	pn = CharPtr.FromNumber(n);
		  else if (size == sizeof(float)) {
		    f = (float)n;
--->		    pn = CharPtr.FromNumber(f);
		  }  
		  else {  /* native lua_Number may be neither float nor double */
		    lua_assert(size == sizeof(double));
		    d = (double)n;
--->		    pn = CharPtr.FromNumber(d);
		  }
--->		  throw new Exception();
		  correctendianess(L, pn, size, 3);
		  lua_pushlstring(L, pn, (uint)size);
		  return 1;
		}
		
		
-------------------------------------

		  L1 = ((LX)luaM_newobject<LX>(L/*, LUA_TTHREAD)*/)).l; //FIXME:
		  
		 
-------------------------------------

???not copy value???
 
public void Assign(Node copy)
			{
				//FIXME:
				this.values = copy.values;
				this.index = copy.index;
				this.i_val = new TValue(copy.i_val);
				this.i_key = new TKey(copy.i_key);				
			}
			
--------------------------------------

public static GCObject luaC_newobj<T> (lua_State L, int tt, uint sz) {
		  global_State g = G(L);
		  //FIXME:???
		  throw new Exception();
------>		  GCObject o = (GCObject)luaM_newobject<GCObject>(L/*, novariant(tt), sz*/);
		  if (o is TString) //FIXME:added
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
		
----------------------------------------


			public static void inc(ref Node node, int n)
			{
				node = node[n];
--->				//return node[-n];
				//FIXME:???array overflow???
			}

			public static void dec(ref Node node, int n)
			{
				node = node[-n];
--->				//return node[n];
				//FIXME:???array overflow???
			}	
			
----------------------------------------
FIXME:这个bug在work1中也存在，需要改回去

public static void luaC_barrier(lua_State L, object p, TValue v) { 
->
public static void luaC_barrier(lua_State L, GCObject p, TValue v) { 


---------------------------

FIXME: while死循环

		private static TValue luaH_newkey (lua_State L, Table t, TValue key) {
		  Node mp;
		  TValue aux = new TValue();
		  if (ttisnil(key)) luaG_runerror(L, "table index is nil");
		  else if (ttisfloat(key)) {
		    lua_Number n = fltvalue(key);
		    lua_Integer k = 0;
		    if (luai_numisnan(n))
		      luaG_runerror(L, "table index is NaN");
		    if (numisinteger(n, ref k)) {  /* index is int? */
		      setivalue(aux, k); 
		      key = aux;  /* insert it as an integer */
		    }
		  }
		  mp = mainposition(t, key);
		  if (!ttisnil(gval(mp)) || isdummy(mp)) {  /* main position is taken? */
			Node othern;
			Node f = getfreepos(t);  /* get a free place */
			if (f == null) {  /* cannot find a free place? */
			  rehash(L, t, key);  /* grow table */
		      /* whatever called 'newkey' take care of TM cache and GC barrier */
		      return luaH_set(L, t, key);  /* insert key into grown table */
			}
			lua_assert(!isdummy(f));
			othern = mainposition(t, gkey(mp));
			if (othern != mp) {  /* is colliding node out of its main position? */
			  /* yes; move colliding node into free position */
---->			  while (Node.plus(othern, gnext(othern)) != mp)  /* find previous */
		        Node.inc(ref othern, gnext(othern));
		        


			public static bool operator ==(Node n1, Node n2)
			{
				object o1 = n1 as Node;
				object o2 = n2 as Node;
				if ((o1 == null) && (o2 == null)) return true;
				if (o1 == null) return false;
				if (o2 == null) return false;
				if (n1.values != n2.values) return false;
				return n1.index == n2.index;
			}
---->			public static bool operator !=(Node n1, Node n2) { return !(n1==n2); }
			
			

			public static Node plus(Node node, int n)
			{
				if (n == 0)
				{
死循环时触发这里，n==0----->					Debug.WriteLine("zero");
				}
				return node[n];
			}
		

xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
	
这个bug已经改好了，因为gnext是+=不是=：because of 
gnext_inc(f, mp - f);
not 
gnext_set(f, mp - f);


以下注释代码是Debug.WriteLine是调试这个bug时加的，已经全部注释掉


xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

		public class TKey_nk : TValue
		{
			public TKey_nk() { }
			public TKey_nk(Value value, int tt, int next) : base(value, tt)
			{
				this.next = next;
			}
			private int _next;  /* for chaining (offset for next node) */
			public int next {
				set
				{
					//if (this._next != 0 && value == 0)
					//{
					//	Debug.WriteLine("???");
					//}
					this._next = value;
--->					//_changed_time++;
--->					//_changed_v[_changed_time] = value;
				}
				get
				{
					return this._next;
				}
			}
			
			public static Node plus(Node node, int n)
			{
				//if (n == 0)
				//{
--->				//	Debug.WriteLine("zero");
				//}
				return node[n];
			}
			

		private static Node mainposition (Table t, TValue key) {
--->			//Debug.WriteLine("ttype == " + ttype(key));
		  switch (ttype(key)) {
		    case LUA_TNUMINT:
		      return hashint(t, ivalue(key));
		      
		      

			if (othern != mp) {  /* is colliding node out of its main position? */
--->				//Debug.WriteLine("othern != mp, " + gnext(othern));
			  /* yes; move colliding node into free position */
			  while (Node.plus(othern, gnext(othern)) != mp)  /* find previous */
		        Node.inc(ref othern, gnext(othern));
		      gnext_set(othern, f - othern);  /* re-chain with 'f' in place of 'mp' */
		      f.Assign(mp);  /* copy colliding node into free pos. (mp->next also goes) */
		      if (gnext(mp) != 0) {
		      	//if (mp - f == 0)
		      	//{
--->		      	//	Debug.WriteLine("???");
		      	//}
		      	
		      	
-------------------------------

		private static void freeLclosure (lua_State L, LClosure cl) {
		  int i;
		  for (i = 0; i < cl.nupvalues; i++) {
		    UpVal uv = cl.upvals[i];
		    if (uv!=null)
		      luaC_upvdeccount(L, uv);
		  }
----->		  luaM_freemem(L, cl, sizeLclosure(cl.nupvalues));
		}
		

public static object luaM_realloc_<T>(lua_State L, T[] old_block, int new_size)
		{
			int unmanaged_size = (int)GetUnmanagedSize(typeof(T));
			int old_size = (old_block == null) ? 0 : old_block.Length;
			int osize = old_size * unmanaged_size;
			int nsize = new_size * unmanaged_size;
			T[] new_block = new T[new_size];
			for (int i = 0; i < Math.Min(old_size, new_size); i++)
				new_block[i] = old_block[i];
			for (int i = old_size; i < new_size; i++)
				new_block[i] = (T)System.Activator.CreateInstance(typeof(T));
------>			if (CanIndex(typeof(T))) 
			{
				//FIXME:added
				T test = (T)System.Activator.CreateInstance(typeof(T));
			    Debug.Assert(test is ArrayElement, String.Format("Need to derive type {0} from ArrayElement", typeof(T).ToString()));
				
			    for (int i = 0; i < new_size; i++)
				{
					ArrayElement elem = new_block[i] as ArrayElement;
					//FIXME:???
					//Debug.Assert(elem != null, String.Format("Need to derive type {0} from ArrayElement", typeof(T).ToString()));
					if (elem != null)
					{
						elem.set_index(i);
						elem.set_array(new_block);
					}
				}
			}
			SubtractTotalBytes(L, osize);
			AddTotalBytes(L, nsize);
			return new_block;
		}

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
add this code ---->			if (t == typeof(LClosure)) //FIXME:
---->				return false;
			return true;
		}
		
		
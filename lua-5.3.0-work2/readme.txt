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




	
(x, 17:16 2019-7-21) todo:lgc.c
(x) todo:lgc.h
		private static lu_mem singlestep (lua_State L) {
		  global_State g = G(L);
		  switch (g.gcstate) {
			case GCSpause: {
		      /* start to count memory traversed */
		      g.GCmemtrav = (uint)(g.strt.size * 4); //FIXME:??? sizeof(GCObject*);

todo:math_abs, l_tg,...., replace Math.xxx
（FIXME: 之前的版本没有修改这里！！！）
（FIXME: 同步luaconf_ex.h！！！）


(x, 16:11 2019-7-21) todo:lstrlib.c

-----------------
0:08 2019-7-14
lbitlib.c
llex.c
lstate.h

0:24 2019-7-14
lapi.c

0:36 2019-7-14
lauxlib.c

9:28 2019/7/14
lbaselib.c

9:33 2019/7/14
lcode.c


		    /* use raw representation as key to avoid numeric problems */
		    setsvalue(L, L.top, luaS_newlstr(L, CharPtr.FromNumber(r), (uint)GetUnmanagedSize(typeof(lua_Number)))); StkId.inc(ref L.top); //FIXME:???
		     n = addk(fs, L.top - 1, o);
		     StkId.dec(ref L.top);
		     

9:36 2019/7/14
lcorolib.c

9:37 2019/7/14
ldebug.c


9:39 2019/7/14
ldo.c

		    case LUA_TCCL: {  /* C closure */
		      f = clCvalue(func).f;
----->		     //Cfunc: //FIXME:removed, see upper
		     

9:44 2019/7/14
lfunc.c


9:47 2019/7/14
liolib.c

9:51 2019/7/14
llimits.h

9:54 2019/7/14
lmathlib.c


		private static int math_abs (lua_State L) {
------------>		  lua_pushnumber(L, Math.Abs(luaL_checknumber(L, 1))); //FIXME:l_tg(fabs), same below
		  return 1;
		}

		private static int math_sin (lua_State L) {
		  lua_pushnumber(L, Math.Sin(luaL_checknumber(L, 1)));
		  return 1;
		}
		
    case 1: {  /* only upper limit */
      lua_Number u = luaL_checknumber(L, 1);
      luaL_argcheck(L, 1.0 <= u, 1, "interval is empty");
/* int in [1, u] */--------->      lua_pushnumber(L, l_tg(floor)(r*u) + 1.0);  /* int in [1, u] */
      

10:08 2019/7/14
lmem.h


		/*
		** This macro avoids the runtime division MAX_SIZET/(e), as 'e' is
		** always constant.
		** The macro is somewhat complex to avoid warnings:
		** +1 avoids warnings of "comparison has constant result";
		** cast to 'void' avoids warnings of "value unused".
		*/
		public static T[] luaM_reallocv<T>(lua_State L, T[] block, int new_size)
		{
not sync to c--------->			return (T[])luaM_realloc_(L, block, new_size);
		}
		

10:11 2019/7/14
lobject.c

-------> return ldexp(r, e); //FIXME:l_mathop(ldexp)
		}
		
		
		
10:14 2019/7/14
lobject.h

10:32 2019/7/14
loslib.c

10:33 2019/7/14
lparser.c

10:35 2019/7/14
lstate.c

10:52 2019/7/14
lstring.c

10:58 2019/7/14
ltable.c

11:01 2019/7/14
ltablib.c

11:05 2019/7/14
lua.c

11:07 2019/7/14
lua.h

11:09 2019/7/14
luaconf.h

11:13 2019/7/14
lvm.c

--------------------------

        //FIXME:???not implemented
        private static LX fromstate(lua_State L) { 
		 throw new Exception("not implemented"); //FIXME:???
		 return /*((LX)((lu_byte[])(L) - offsetof(LX, l)))*/ null; 
        } 


----------------------------


		public static long time(object p) 
		{
			if (p == null)
			{
				return DateTime.Now.Ticks;
			}
			else
			{
				throw new Exception("time(NULL);");
				return 0;
			}
		}



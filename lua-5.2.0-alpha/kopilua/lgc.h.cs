namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using lu_byte = System.Byte;
	
	public partial class Lua
	{
	
		/*
		** Collectable objects may have one of three colors: white, which
		** means the object is not marked; gray, which means the
		** object is marked, but its references may be not marked; and
		** black, which means that the object and all its references are marked.
		** The main invariant of the garbage collector, while marking objects,
		** is that a black object can never point to a white one. Moreover,
		** any gray object must be in a "gray list" (gray, grayagain, weak,
		** allweak, ephemeron) so that it can be visited again before finishing
		** the collection cycle. These lists have no meaning when the invariant
		** is not being enforced (e.g., sweep phase).
		*/
	

		/*
		** Possible states of the Garbage Collector
		*/
		public const int GCSpropagate = 0;
		public const int GCSatomic = 1;
		public const int GCSsweepstring = 2;
		public const int GCSsweepudata = 3;
		public const int GCSsweep = 4;
		public const int GCSpause = 5;


		public static bool issweepphase(global_State g)  { return
			(GCSsweepstring <= g.gcstate && g.gcstate <= GCSsweep); }

		public static bool isgenerational(global_State g) { return (g.gckind == KGC_GEN); }

		/*
		** macro to tell when main invariant (white objects cannot point to black
		** ones) must be kept. During a non-generational collection, the sweep
		** phase may break the invariant, as objects turned white may point to
		** still-black objects. The invariant is restored when sweep ends and
		** all objects are white again. During a generational collection, the
		** invariant must be kept all times.
		*/
		public static bool keepinvariant(global_State g) { return (isgenerational(g) || g.gcstate <= GCSatomic); }


		public static bool gcstopped(global_State g) { return (g.GCdebt == MIN_LMEM); }
		public static bool stopgc(global_State g) { return (g.GCdebt = MIN_LMEM); }


		/*
		** some useful bit tricks
		*/
		public static int resetbits(ref lu_byte x, int m) { x &= (lu_byte)~m; return x; }
		public static int setbits(ref lu_byte x, int m) { x |= (lu_byte)m; return x; }
		public static bool testbits(lu_byte x, int m) { return (x & (lu_byte)m) != 0; }
		public static int bitmask(int b)	{return 1<<b;}
		public static int bit2mask(int b1, int b2)	{return (bitmask(b1) | bitmask(b2));}
		public static int l_setbit(ref lu_byte x, int b) { return setbits(ref x, bitmask(b)); }
		public static int resetbit(ref lu_byte x, int b) { return resetbits(ref x, bitmask(b)); }
		public static bool testbit(lu_byte x, int b) { return testbits(x, bitmask(b)); }
		public static int set2bits(ref lu_byte x, int b1, int b2) { return setbits(ref x, (bit2mask(b1, b2))); }
		public static int reset2bits(ref lu_byte x, int b1, int b2) { return resetbits(ref x, (bit2mask(b1, b2))); }



		/* Layout for bit use in `marked' field: */
		public const int WHITE0BIT		= 0;  /* object is white (type 0) */
		public const int WHITE1BIT		= 1;  /* object is white (type 1) */
		public const int BLACKBIT		= 2;  /* object is black */
		public const int FINALIZEDBIT	= 3;  /* for userdata: has been finalized */
		public const int SEPARATED		= 4;  /*  "    ": it's in 'udgc' list or in 'tobefnz' */
		public const int FIXEDBIT		= 5;  /* object is fixed (should not be collected) */
		public const int OLDBIT		= 6;  /* object is old (only in generational mode) */
		/* bit 7 is currently used by tests (luaL_checkmemory) */

		public readonly static int WHITEBITS		= bit2mask(WHITE0BIT, WHITE1BIT);


		public static bool iswhite(GCObject x) { return testbits(x.gch.marked, WHITEBITS); }
		public static bool isblack(GCObject x) { return testbit(x.gch.marked, BLACKBIT); }
		public static bool isgray(GCObject x)   /* neither white nor black */
			{ return (!testbits(x.gch.marked, WHITEBITS | bitmask(BLACKBIT))); }

		public static bool isold(GCObject x) { return testbit(x.gch.marked, OLDBIT); }

		/* MOVE OLD rule: whenever an object is moved to the beginning of
		   a GC list, its old bit must be cleared */
		public static void resetoldbit(GCObject o) { return resetbit(o.gch.marked, OLDBIT); }

		public static int otherwhite(global_State g) { return g.currentwhite ^ WHITEBITS; }
		public static bool isdeadm(int ow, int m) { return (!(((m) ^ WHITEBITS) & (ow))); }
		public static bool isdead(global_State g, GCObject v) { return (isdeadm(otherwhite(g), (v)->gch.marked)); }

		public static void changewhite(GCObject x) { x.gch.marked ^= (byte)WHITEBITS; }
		public static void gray2black(GCObject x) { l_setbit(ref x.gch.marked, BLACKBIT); }

		public static bool valiswhite(TValue x) { return (iscollectable(x) && iswhite(gcvalue(x))); }

		public static byte luaC_white(global_State g) { return (byte)(g.currentwhite & WHITEBITS); }


        //FIXME:empty-> //condchangemem(L);
		public static void luaC_checkGC(lua_State L) {/*condchangemem(L);*/ if (G(L).GCdebt > 0) luaC_step(L);} //FIXME: macro in {}


		public static void luaC_barrier(lua_State L, object p, TValue v) { if (valiswhite(v) && isblack(obj2gco(p)))
			luaC_barrier_(L,obj2gco(p),gcvalue(v)); }

		public static void luaC_barrierback(lua_State L, object p, TValue v) { if (valiswhite(v) && isblack(obj2gco(p)))
		    luaC_barrierback_(L,p); }

		public static void luaC_objbarrier(lua_State L, object p, object o)
			{ if (iswhite(obj2gco(o)) && isblack(obj2gco(p)))
				luaC_barrier_(L,obj2gco(p),obj2gco(o)); }

		public static void luaC_objbarrierback(lua_State L, object p, object o)
			{ if (iswhite(obj2gco(o)) && isblack(obj2gco(p))) luaC_barrierback_(L,p); }
			
		public static void luaC_barrierproto(lua_State L, object p, Closure c)
   			{ if (isblack(obj2gco(p))) luaC_barrierproto_(L,p,c); }
	
	}
}

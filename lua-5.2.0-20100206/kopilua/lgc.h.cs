namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using lu_byte = System.Byte;
	
	public partial class Lua
	{
		/*
		** Possible states of the Garbage Collector
		*/
		public const int GCSpause		= 0;
		public const int GCSpropagate	= 1;
		public const int GCSatomic	    = 2;
		public const int GCSsweepstring	= 3;
		public const int GCSsweep		= 4;
		public const int GCSfinalize	= 5;



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



		/*
		** Layout for bit use in `marked' field:
		** bit 0 - object is white (type 0)
		** bit 1 - object is white (type 1)
		** bit 2 - object is black
		** bit 3 - for userdata: has been finalized
		** bit 4 - for userdata: it's in 2nd part of rootgc list or in tobefnz
		** bit 5 - object is fixed (should not be collected)
		** bit 6 - object is "super" fixed (only the main thread)
		*/


		public const int WHITE0BIT		= 0;
		public const int WHITE1BIT		= 1;
		public const int BLACKBIT		= 2;
		public const int FINALIZEDBIT	= 3;
		public const int SEPARATED		= 4;
		public const int FIXEDBIT		= 5;
		public const int SFIXEDBIT		= 6;
		public readonly static int WHITEBITS		= bit2mask(WHITE0BIT, WHITE1BIT);


		public static bool iswhite(GCObject x) { return testbits(x.gch.marked, WHITEBITS); }
		public static bool isblack(GCObject x) { return testbit(x.gch.marked, BLACKBIT); }
		public static bool isgray(GCObject x) { return (!isblack(x) && !iswhite(x)); }

		public static int otherwhite(global_State g) { return g.currentwhite ^ WHITEBITS; }
		public static bool isdead(global_State g, GCObject v) { return (v.gch.marked & otherwhite(g) & WHITEBITS) != 0; }

		public static void changewhite(GCObject x) { x.gch.marked ^= (byte)WHITEBITS; }
		public static void gray2black(GCObject x) { l_setbit(ref x.gch.marked, BLACKBIT); }

		public static bool valiswhite(TValue x) { return (iscollectable(x) && iswhite(gcvalue(x))); }

		public static byte luaC_white(global_State g) { return (byte)(g.currentwhite & WHITEBITS); }


		public static void luaC_checkGC(lua_State L)
		{
			condchangemem(L); //FIXME:???
			if (G(L).totalbytes >= G(L).GCthreshold) luaC_step(L);
		}


		public static void luaC_barrier(lua_State L, object p, TValue v) { if (valiswhite(v) && isblack(obj2gco(p)))
			luaC_barrierf(L,obj2gco(p),gcvalue(v)); }

		public static void luaC_barriert(lua_State L, Table t, TValue v) { if (valiswhite(v) && isblack(obj2gco(t)))
		    luaC_barrierback(L,t); }

		public static void luaC_objbarrier(lua_State L, object p, object o)
			{ if (iswhite(obj2gco(o)) && isblack(obj2gco(p)))
				luaC_barrierf(L,obj2gco(p),obj2gco(o)); }

		public static void luaC_objbarriert(lua_State L, Table t, object o)
			{ if (iswhite(obj2gco(o)) && isblack(obj2gco(t))) luaC_barrierback(L,t); }
	}
}

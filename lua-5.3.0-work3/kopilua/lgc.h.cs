/*
** $Id: lgc.h,v 2.82 2014/03/19 18:51:16 roberto Exp $
** Garbage Collector
** See Copyright Notice in lua.h
*/
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
	

		/* how much to allocate before next GC step */
		//#if !defined(GCSTEPSIZE)
		/* ~100 small strings */
		public const int GCSTEPSIZE =	((int)(100 * 16)); //FIXME:sizeof(TString) == 16
		//#endif


		/*
		** Possible states of the Garbage Collector
		*/
		public const int GCSpropagate = 0;
		public const int GCSatomic = 1;
		public const int GCSswpallgc = 2;
		public const int GCSswpfinobj = 3;
		public const int GCSswptobefnz = 4;
		public const int GCSswpend = 5;
		public const int GCScallfin = 6;
		public const int GCSpause = 7;


		public static bool issweepphase(global_State g)  { return
			(GCSswpallgc <= g.gcstate && g.gcstate <= GCSswpend); }


		/*
		** macro to tell when main invariant (white objects cannot point to black
		** ones) must be kept. During a collection, the sweep
		** phase may break the invariant, as objects turned white may point to
		** still-black objects. The invariant is restored when sweep ends and
		** all objects are white again.
		*/
		
		public static bool keepinvariant(global_State g)	{ return (g.gcstate <= GCSatomic); }
			
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


		/* Layout for bit use in `marked' field: */
		public const int WHITE0BIT		= 0;  /* object is white (type 0) */
		public const int WHITE1BIT		= 1;  /* object is white (type 1) */
		public const int BLACKBIT		= 2;  /* object is black */
		public const int FINALIZEDBIT	= 3;  /* object has been marked for finalization */
		/* bit 7 is currently used by tests (luaL_checkmemory) */

		public readonly static int WHITEBITS		= bit2mask(WHITE0BIT, WHITE1BIT);


		public static bool iswhite(GCObject x) { return testbits(x.gch.marked, WHITEBITS); }
		public static bool isblack(GCObject x) { return testbit(x.gch.marked, BLACKBIT); }
		public static bool isgray(GCObject x)   /* neither white nor black */
			{ return (!testbits(x.gch.marked, WHITEBITS | bitmask(BLACKBIT))); }

		public static bool tofinalize(GCObject x) { return testbit(x.gch.marked, FINALIZEDBIT); }

		public static int otherwhite(global_State g) { return g.currentwhite ^ WHITEBITS; }
		public static bool isdeadm(int ow, int m) { return (((m ^ WHITEBITS) & ow)==0); }
		public static bool isdead(global_State g, GCObject v) { return (isdeadm(otherwhite(g), v.gch.marked)); }

		public static void changewhite(GCObject x) { x.gch.marked ^= (byte)WHITEBITS; }
		public static void gray2black(GCObject x) { l_setbit(ref x.gch.marked, BLACKBIT); }

		public static byte luaC_white(global_State g) { return (byte)(g.currentwhite & WHITEBITS); }

		public delegate void luaC_condGC_func(); //FIXME: added
		public static void luaC_condGC(lua_State L, luaC_condGC_func c) {
			{if (G(L).GCdebt > 0) {c();}; condchangemem(L);} } //FIXME:???macro
		public static void luaC_checkGC(lua_State L) {luaC_condGC(L, delegate() {luaC_step(L);}); } //FIXME: macro in {}


		public static void luaC_barrier(lua_State L, GCObject p, TValue v) { 
			if (iscollectable(v) && isblack(obj2gco(p)) && iswhite(gcvalue(v)))
			luaC_barrier_(L,obj2gco(p),gcvalue(v)); }

		public static void luaC_barrierback(lua_State L, GCObject p, TValue v) { 
			if (iscollectable(v) && isblack(obj2gco(p)) && iswhite(gcvalue(v)))
		    luaC_barrierback_(L,obj2gco(p)); }

		public static void luaC_objbarrier(lua_State L, object p, object o) { 
			if (isblack(obj2gco(p)) && iswhite(obj2gco(o)))
				luaC_barrier_(L,obj2gco(p),obj2gco(o)); }

		public static void luaC_upvalbarrier(lua_State L, UpVal uv) {
			if (iscollectable(uv.v) && !upisopen(uv)) 
				luaC_upvalbarrier_(L,uv); }
			
	
	}
}

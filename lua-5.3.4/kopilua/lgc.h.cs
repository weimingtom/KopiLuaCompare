/*
** $Id: lgc.h,v 2.91 2015/12/21 13:02:14 roberto Exp $
** Garbage Collector
** See Copyright Notice in lua.h
*/

namespace KopiLua
{
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
		public static int GCSTEPSIZE = 100 * GetUnmanagedSize(typeof(TString));
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


		public static bool issweepphase(global_State g) {
			return (GCSswpallgc <= g.gcstate && g.gcstate <= GCSswpend); }


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
		public static void resetbits(int x, int m) { (x) &= (lu_byte)(~(m)); }
		public static void setbits(int x, int m) { (x) |= (m); }
		public static int testbits(int x, int m) { return ((x) & (m)); }
		public static int bitmask(int b) { return (1<<(b)); }
		public static int bit2mask(int b1, int b2) { return (bitmask(b1) | bitmask(b2)); }
		public static void l_setbit(int x, int b) { setbits(x, bitmask(b)); }
		public static void resetbit(int x, int b) { resetbits(x, bitmask(b)); }
		public static int testbit(int x, int b)	{ return testbits(x, bitmask(b)); }


		/* Layout for bit use in 'marked' field: */
		public const int WHITE0BIT = 0;  /* object is white (type 0) */
		public const int WHITE1BIT = 1;  /* object is white (type 1) */
		public const int BLACKBIT = 2;  /* object is black */
		public const int FINALIZEDBIT = 3;  /* object has been marked for finalization */
		/* bit 7 is currently used by tests (luaL_checkmemory) */

		public static int WHITEBITS = bit2mask(WHITE0BIT, WHITE1BIT);


		public static bool iswhite(GCObject x) { return testbits(x.marked, WHITEBITS) != 0;}
		public static bool isblack(GCObject x) { return testbit(x.marked, BLACKBIT) != 0;}
		public static bool isgray(GCObject x)  /* neither white nor black */
		{ return (!(testbits(x.marked, WHITEBITS | bitmask(BLACKBIT)) != 0)); }

		public static int tofinalize(GCObject x) { return testbit(x.marked, FINALIZEDBIT); } 
		
		public static int otherwhite(global_State g) { return (g.currentwhite ^ WHITEBITS); }
		public static bool isdeadm(int ow, int m) { return (!((((m) ^ WHITEBITS) & (ow)) != 0)); }
		public static bool isdead(global_State g, TValue v) { return isdeadm(otherwhite(g), v.marked); }
		
		public static void changewhite(GCObject x) { x.marked ^= WHITEBITS; }
		public static void gray2black(GCObject x) { l_setbit(x.marked, BLACKBIT); }
		
		public static byte luaC_white(global_State g)	{return (lu_byte)(g.currentwhite & WHITEBITS); }


		/*
		** Does one step of collection when debt becomes positive. 'pre'/'pos'
		** allows some adjustments to be done only when needed. macro
		** 'condchangemem' is used only for heavy tests (forcing a full
		** GC cycle on every opportunity)
		*/
		public void luaC_condGC(lua_State L/*, object pre, object pos*/)
		{ if (G(L).GCdebt > 0) { /*pre;*/ luaC_step(L); /*pos;*/};
			condchangemem(L/*,pre,pos*/); } //FIXME:

		/* more often than not, 'pre'/'pos' are empty */
		public void luaC_checkGC(lua_State L) {luaC_condGC(L/*,(void)0,(void)0*/);}
		//FIXME:

		public static void luaC_barrier(lua_State L, GCObject p, TValue v) {
			if (iscollectable(v) && isblack(p) && iswhite(gcvalue(v))) {
				luaC_barrier_(L, obj2gco(p), gcvalue(v)); } }
		
		public static void luaC_barrierback(lua_State L, GCObject p, TValue v) {
			if (iscollectable(v) && isblack(p) && iswhite(gcvalue(v))) {
				luaC_barrierback_(L, p); } }
		
		public static void luaC_objbarrier(lua_State L, GCObject p, GCObject o) {
			if (isblack(p) && iswhite(o)) {
				luaC_barrier_(L,obj2gco(p), obj2gco(o)); } }
		
		public static void luaC_upvalbarrier(lua_State L, UpVal uv) {
			if (iscollectable(uv.v) && !upisopen(uv)) {
				luaC_upvalbarrier_(L, uv); } }

//LUAI_FUNC void luaC_fix (lua_State *L, GCObject *o);
//LUAI_FUNC void luaC_freeallobjects (lua_State *L);
//LUAI_FUNC void luaC_step (lua_State *L);
//LUAI_FUNC void luaC_runtilstate (lua_State *L, int statesmask);
//LUAI_FUNC void luaC_fullgc (lua_State *L, int isemergency);
//LUAI_FUNC GCObject *luaC_newobj (lua_State *L, int tt, size_t sz);
//LUAI_FUNC void luaC_barrier_ (lua_State *L, GCObject *o, GCObject *v);
//LUAI_FUNC void luaC_barrierback_ (lua_State *L, Table *o);
//LUAI_FUNC void luaC_upvalbarrier_ (lua_State *L, UpVal *uv);
//LUAI_FUNC void luaC_checkfinalizer (lua_State *L, GCObject *o, Table *mt);
//LUAI_FUNC void luaC_upvdeccount (lua_State *L, UpVal *uv);


	}
}
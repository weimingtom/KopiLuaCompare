/*
** $Id: lstate.h,v 2.117 2014/10/07 18:29:13 roberto Exp $
** Global State
** See Copyright Notice in lua.h
*/
using System.Diagnostics;

namespace KopiLua
{
	using lu_byte = System.Byte;
	using lu_int32 = System.Int32;
	using lu_mem = System.UInt32;
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using ptrdiff_t = System.Int32;
	using lua_Number = System.Double;
	using l_mem = System.Int32;
	using lua_KContext = System.Int32;
	
	/*

	** Some notes about garbage-collected objects: All objects in Lua must
	** be kept somehow accessible until being freed, so all objects always
	** belong to one (and only one) of these lists, using field 'next' of
	** the 'CommonHeader' for the link:
	**
	** allgc: all objects not marked for finalization;
	** finobj: all objects marked for finalization;
	** tobefnz: all objects ready to be finalized; 
	** fixedgc: all objects that are not to be collected (currently
	** only small strings, such as reserved words).

	*/
	public partial class Lua
	{


		/* extra stack space to handle TM calls and some other extras */
		public const int EXTRA_STACK   = 5;


		public const int BASIC_STACK_SIZE        = (2*LUA_MINSTACK);


		/* kinds of Garbage Collection */
		public const int KGC_NORMAL	= 0;
		public const int KGC_EMERGENCY	= 1;	/* gc was forced by an allocation failure */


		public class stringtable {
			public TString[] hash;
			public int nuse;  /* number of elements */
			public int size;
		};


		/*
		** Information about a call.
		** When a thread yields, 'func' is adjusted to pretend that the
		** top function has only the yielded values in its stack; in that
		** case, the actual 'func' value is saved in field 'extra'. 
		** When a function calls another with a continuation, 'extra' keeps
		** the function index so that, in case of errors, the continuation
		** function can be called with the correct top.
		*/
		public class CallInfo : ArrayElement
		{
			private CallInfo[] values = null;
			private int index = -1;

			public void set_index(int index)
			{
				this.index = index;
			}

			public void set_array(object array)
			{
				this.values = (CallInfo[])array;
				Debug.Assert(this.values != null);
			}

			public CallInfo this[int offset]
			{
				get { return values[index+offset]; }
			}

			public static CallInfo operator +(CallInfo value, int offset)
			{
				return value.values[value.index + offset];
			}

			public static CallInfo operator -(CallInfo value, int offset)
			{
				return value.values[value.index - offset];
			}

			public static int operator -(CallInfo ci, CallInfo[] values)
			{
				Debug.Assert(ci.values == values);
				return ci.index;
			}

			public static int operator -(CallInfo ci1, CallInfo ci2)
			{
				Debug.Assert(ci1.values == ci2.values);
				return ci1.index - ci2.index;
			}

			public static bool operator <(CallInfo ci1, CallInfo ci2)
			{
				Debug.Assert(ci1.values == ci2.values);
				return ci1.index < ci2.index;
			}

			public static bool operator <=(CallInfo ci1, CallInfo ci2)
			{
				Debug.Assert(ci1.values == ci2.values);
				return ci1.index <= ci2.index;
			}

			public static bool operator >(CallInfo ci1, CallInfo ci2)
			{
				Debug.Assert(ci1.values == ci2.values);
				return ci1.index > ci2.index;
			}

			public static bool operator >=(CallInfo ci1, CallInfo ci2)
			{
				Debug.Assert(ci1.values == ci2.values);
				return ci1.index >= ci2.index;
			}

			public static CallInfo inc(ref CallInfo value)
			{
				value = value[1];
				return value[-1];
			}

			public static CallInfo dec(ref CallInfo value)
			{
				value = value[-1];
				return value[1];
			}


			public StkId func;  /* function index in the stack */
			public StkId top;  /* top for this function */
			public CallInfo previous, next;  /* dynamic call link */
			public class _u {
			    public class _l {  /* only for Lua functions */
			      public StkId base_;  /* base for this function */
			      public InstructionPtr savedpc;
			    };
				public _l l = new _l();
			    public class _c {  /* only for C functions */
			      public lua_KFunction k;  /* continuation in case of yields */
			      public ptrdiff_t old_errfunc;
			      public lua_KContext ctx;  /* context info. in case of yields */
			    };
				public _c c = new _c();
			};
			public _u u = new _u();
			public ptrdiff_t extra;
			public short nresults;  /* expected number of results from this function */
			public lu_byte callstatus;			
		};

		/*
		** Bits in CallInfo status
		*/
		public const int CIST_OAH = (1<<0);	/* original value of 'allowhook' */
		public const int CIST_LUA = (1<<1);	/* call is running a Lua function */
		public const int CIST_HOOKED = (1<<2);	/* call is running a debug hook */
		public const int CIST_REENTRY = (1<<3);	/* call is running on same invocation of
		                                   luaV_execute of previous call */
		public const int CIST_YPCALL = 	(1<<4);	/* call is a yieldable protected call */
		public const int CIST_TAIL = (1<<5); /* call was tail called */
		public const int CIST_HOOKYIELD	= (1<<6);	/* last hook called yielded */

		public static int isLua(CallInfo ci)	{return ((ci.callstatus & CIST_LUA) != 0) ? 1 : 0;}

		/* assume that CIST_OAH has offset 0 and that 'v' is strictly 0/1 */
		public static void setoah(byte st, byte v)	{ st = (byte)(((st & ~CIST_OAH) | v) & 0xff); }
		public static byte getoah(byte st)	{ return (byte)((st & CIST_OAH) & 0xff); }

		/*
		** `global state', shared by all threads of this state
		*/
		public class global_State {
		  public lua_Alloc frealloc;  /* function to reallocate memory */
		  public object ud;         /* auxiliary data to `frealloc' */
		  public lu_mem totalbytes;  /* number of bytes currently allocated - GCdebt */
		  public l_mem GCdebt;  /* bytes allocated not yet compensated by the collector */
		  public lu_mem GCmemtrav;  /* memory traversed by the GC */
		  public lu_mem GCestimate;  /* an estimate of the non-garbage memory in use */
		  public stringtable strt = new stringtable();  /* hash table for strings */
		  public TValue l_registry = new TValue();
		  public uint seed;  /* randomized seed for hashes */
		  public lu_byte currentwhite;
		  public lu_byte gcstate;  /* state of garbage collector */
          public lu_byte gckind;  /* kind of GC running */
          public lu_byte gcrunning;  /* true if GC is running */
		  public GCObject allgc;  /* list of all collectable objects */
		  public GCObjectRef sweepgc;  /* current position of sweep in list */
  		  public GCObject finobj;  /* list of collectable objects with finalizers */
		  public GCObject gray;  /* list of gray objects */
		  public GCObject grayagain;  /* list of objects to be traversed atomically */
		  public GCObject weak;  /* list of tables with weak values */
		  public GCObject ephemeron;  /* list of ephemeron tables (weak keys) */
		  public GCObject allweak;  /* list of all-weak tables */
		  public GCObject tobefnz;  /* list of userdata to be GC */
		  public GCObject fixedgc;  /* list of objects not to be collected */
		  public lua_State twups;  /* list of threads with open upvalues */
		  public Mbuffer buff = new Mbuffer();  /* temporary buffer for string concatenation */
		  public uint gcfinnum;  /* number of finalizers to call in each GC step */
		  public int gcpause;  /* size of pause between successive GCs */
		  public int gcstepmul;  /* GC `granularity' */
		  public lua_CFunction panic;  /* to be called in unprotected errors */
		  public lua_State mainthread;
          public /*const*/ lua_Number[] version;  /* pointer to version number */
          public TString memerrmsg;  /* memory-error message */
          public TString[] tmname = new TString[(int)TMS.TM_N];  /* array with tag-method names */ //FIXME:???not init with new TString()
		  public Table[] mt = new Table[LUA_NUMTAGS];  /* metatables for basic types */
		  
		  //------------------------
		  public GCObject mt_ = null; //FIXME: added, only for MtRef
		  public GCObject nullp = null; //FIXME: added, only for NullpRef
		};


		/*
		** `per thread' state
		*/
		public class lua_State : GCUnion {

		  public lu_byte status;
		  public StkId top;  /* first free slot in the stack */
		  public global_State l_G;
		  public CallInfo ci;  /* call info for current function */
          public /*const*/ InstructionPtr oldpc;  /* last pc traced */
		  public StkId stack_last;  /* last free slot in the stack */
		  public StkId[] stack;  /* stack base */
		  public UpVal openupval;  /* list of open upvalues in this stack */
		  public GCObject gclist;
		  public lua_State twups;  /* list of threads with open upvalues */
		  public lua_longjmp errorJmp;  /* current error recover point */
		  public CallInfo base_ci = new CallInfo();  /* CallInfo for first level (C calling Lua) */
		  public lua_Hook hook;		  
		  public ptrdiff_t errfunc;  /* current error handling function (stack index) */
		  public int stacksize;
		  public int basehookcount;
		  public int hookcount;
		  public ushort nny;  /* number of non-yieldable calls in stack */
		  public ushort nCcalls;  /* number of nested C calls */
		  public lu_byte hookmask;
		  public lu_byte allowhook;
  		  
		  public LX _parent; //FIXME:added, see offsetof	  
		};
		public interface lua_StateRef {
			lua_State get();
			void set(lua_State s);			
		}
		public class TwupsRef : lua_StateRef {
			private global_State g;
			public TwupsRef(global_State g)
			{
				this.g = g;
			}
			public lua_State get() {return this.g.twups; }
			public void set(lua_State s) {this.g.twups = s;}
		}
		public class TwupsStateRef : lua_StateRef {
			private lua_State s;
			public TwupsStateRef(lua_State s)
			{
				this.s = s;
			}
			public lua_State get() {return this.s.twups; }
			public void set(lua_State s) {this.s.twups = s;}
		}


		public static global_State G(lua_State L)	{return L.l_G;}
		public static void G_set(lua_State L, global_State s) { L.l_G = s; } //FIXME:added, not used???


		/*
		** Union of all collectable objects (not a union anymore in the C# port)
		*/
		public class GCUnion : GCObject, ArrayElement
		{
			// todo: remove this?
			//private GCObject[] values = null;
			//private int index = -1;

			public void set_index(int index)
			{
				//this.index = index;
			}

			public void set_array(object array)
			{
				//this.values = (GCObject[])array;
				//Debug.Assert(this.values != null);
			}

			public GCObject gc {get{return (GCObject)this;}}   /* common header */
			public TString ts {get{return (TString)this;}}
			public Udata u {get{return (Udata)this;}}
			public Closure cl {get{return (Closure)this;}}
			public Table h {get{return (Table)this;}}
			public Proto p {get{return (Proto)this;}}
			public lua_State th {get{return (lua_State)this;}}  /* thread */
		};

		public static GCUnion cast_u(GCObject o)	{ return (GCUnion)o; }

		/*	this interface and is used for implementing GCObject references,
		    it's used to emulate the behaviour of a C-style GCObject **
		 */
		public interface GCObjectRef
		{
			void set(GCObject value);
			GCObject get();
		}
		
		/*
		public class ArrayRef : GCObjectRef 
		{
			public ArrayRef(GCObject[] vals, int index)
			{
				this.vals = vals;
				this.index = index;
			}
			public void set(GCObject value) { vals[index] = value; }
			public GCObject get() { return vals[index]; }
			GCObject[] vals;
			int index;
		}
		 * */
		public class ArrayRef : GCObjectRef, ArrayElement
		{
			public ArrayRef()
			{
				this.array_elements = null;
				this.array_index = 0;
				this.vals = null;
				this.index = 0;
			}
			public ArrayRef(GCObject[] array_elements, int array_index)
			{
				this.array_elements = array_elements;
				this.array_index = array_index;
				this.vals = null;
				this.index = 0;
			}
			public void set(GCObject value) { array_elements[array_index] = value; }
			public GCObject get() { return array_elements[array_index]; }

			public void set_index(int index)
			{
				this.index = index;
			}
			public void set_array(object vals)
			{
				// don't actually need this
				this.vals = (ArrayRef[])vals;
				Debug.Assert(this.vals != null);
			}

			// ArrayRef is used to reference GCObject objects in an array, the next two members
			// point to that array and the index of the GCObject element we are referencing
			GCObject[] array_elements;
			int array_index;

			// ArrayRef is itself stored in an array and derived from ArrayElement, the next
			// two members refer to itself i.e. the array and index of it's own instance.
			ArrayRef[] vals;
			int index;
		}

		//removed
		//public class OpenValRef : GCObjectRef
		//{
		//	public OpenValRef(lua_State L) { this.L = L; }
		//	public void set(UpVal value) { this.L.openupval = value; }
		//	public UpVal get() { return this.L.openupval; }
		//	lua_State L;
		//}
		//FIXME:removed, no rootgc
	
		public class AllGCRef : GCObjectRef
		{
			public AllGCRef(global_State g) { this.g = g; }
			public void set(GCObject value) { this.g.allgc = value; }
			public GCObject get() { return this.g.allgc; }
			global_State g;
		}

		public class NextRef : GCObjectRef
		{
			public NextRef(GCObject header) { 
				this.header = header; 
				//FIXME:added, for debug only
				//if (this.header == null)
				//{
				//	Debug.Assert(this.header == null);
				//}
			}
			public void set(GCObject value) { this.header.next = value; }
			public GCObject get() { return this.header.next; }
			GCObject header;
		}
		/*
		public class PtrRef : GCObjectRef
		{
			public PtrRef(GCObject obj) { this.obj = obj; }
			public void set(GCObject value) { this.obj = value; }
			public GCObject get() { return this.obj; }
			GCObject obj;
		}*/
		
		public class FinobjRef : GCObjectRef
		{
			public FinobjRef(global_State g) { this.g = g; }
			public void set(GCObject value) { this.g.finobj = value; }
			public GCObject get() { return this.g.finobj; }
			global_State g;
		}
		
		public class TobefnzRef : GCObjectRef
		{
			public TobefnzRef(global_State g) { this.g = g; }
			public void set(GCObject value) { this.g.tobefnz = value; }
			public GCObject get() { return this.g.tobefnz; }
			global_State g;
		}
		
		public class FixedgcRef : GCObjectRef
		{
			public FixedgcRef(global_State g) { this.g = g; }
			public void set(GCObject value) { this.g.fixedgc = value; }
			public GCObject get() { return this.g.fixedgc; }
			global_State g;
		}
		
		public class MtRef : GCObjectRef
		{
			public MtRef(global_State g) { this.g = g; }
			public void set(GCObject value) { this.g.mt_ = value; }
			public GCObject get() { return this.g.mt_; }
			global_State g;
		}

		public class NullpRef : GCObjectRef
		{
			public NullpRef(global_State g) { this.g = g; }
			public void set(GCObject value) { this.g.nullp = value; }
			public GCObject get() { return this.g.nullp; }
			global_State g;
		}
		
		/* macros to convert a GCObject into a specific value */
		public static TString gco2ts(GCObject o) 
			{ return (TString)check_exp(novariant(o.tt) == LUA_TSTRING, (cast_u(o)).ts); }
		public static Udata gco2u(GCObject o) { return (Udata)check_exp(o.tt == LUA_TUSERDATA, (cast_u(o)).u); }
		public static LClosure gco2lcl(GCObject o)	{ return (LClosure)check_exp(o.tt == LUA_TLCL, (cast_u(o)).cl.l); }
		public static CClosure gco2ccl(GCObject o)	{ return (CClosure)check_exp(o.tt == LUA_TCCL, (cast_u(o)).cl.c); }
		public static Closure gco2cl(GCObject o)  {
			return (Closure)check_exp(novariant(o.tt) == LUA_TFUNCTION, (cast_u(o)).cl); }
		public static Table gco2t(GCObject o) { return (Table)check_exp(o.tt == LUA_TTABLE, (cast_u(o)).h); }
		public static Proto gco2p(GCObject o) { return (Proto)check_exp(o.tt == LUA_TPROTO, (cast_u(o)).p); }
		public static lua_State gco2th(GCObject o) { return (lua_State)check_exp(o.tt == LUA_TTHREAD, (cast_u(o)).th); }

		
		/* macro to convert a Lua object into a GCObject */
		public static GCObject obj2gco(GCObject v)	
			{return (GCObject)check_exp(novariant((v).tt) < LUA_TDEADKEY, ((cast_u(v).gc)));}


		/* actual number of total bytes allocated */
		public static uint gettotalbytes(global_State g) { return (uint)(g.totalbytes + g.GCdebt); } //FIXME:(uint)

	}
}

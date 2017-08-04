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
	
	/*

	** Some notes about garbage-collected objects:  All objects in Lua must
	** be kept somehow accessible until being freed.
	**
	** Lua keeps most objects linked in list g->allgc. The link uses field
	** 'next' of the CommonHeader.
	**
	** Strings are kept in several lists headed by the array g->strt.hash.
	**
	** Open upvalues are not subject to independent garbage collection. They
	** are collected together with their respective threads. Lua keeps a
	** double-linked list with all open upvalues (g->uvhead) so that it can
	** mark objects referred by them. (They are always gray, so they must
	** be remarked in the atomic step. Usually their contents would be marked
	** when traversing the respective threads, but the thread may already be
	** dead, while the upvalue is still accessible through closures.)
	**
	** Objects with finalizers are kept in the list g->finobj.
	**
	** The list g->tobefnz links all objects being finalized.

	*/
	public partial class Lua
	{


		/* extra stack space to handle TM calls and some other extras */
		public const int EXTRA_STACK   = 5;


		public const int BASIC_CI_SIZE           = 8;

		public const int BASIC_STACK_SIZE        = (2*LUA_MINSTACK);


		/* kinds of Garbage Collection */
		public const int KGC_NORMAL	= 0;
		public const int KGC_EMERGENCY	= 1;	/* gc was forced by an allocation failure */
		public const int KGC_GEN = 2;	/* generational collection */


		public class stringtable {
			public GCObject[] hash;
			public lu_int32 nuse;  /* number of elements */
			public int size;
		};


		/*
		** information about a call
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
			public short nresults;  /* expected number of results from this function */
			public lu_byte callstatus;
			public class _u {
			    public class _l {  /* only for Lua functions */
			      public StkId base_;  /* base for this function */
			      public InstructionPtr savedpc;
			    };
				public _l l = new _l();
			    public class _c {  /* only for C functions */
			      public int ctx;  /* context info. in case of yields */
			      public lua_CFunction k;  /* continuation in case of yields */
			      public ptrdiff_t old_errfunc;
			      public ptrdiff_t extra;
			      public lu_byte old_allowhook;
			      public lu_byte status;
			    };
				public _c c = new _c();
			};
			public _u u = new _u();
		};

		/*
		** Bits in CallInfo status
		*/
		public const int CIST_LUA = (1<<0);	/* call is running a Lua function */
		public const int CIST_HOOKED = (1<<1);	/* call is running a debug hook */
		public const int CIST_REENTRY = (1<<2);	/* call is running on same invocation of
		                                   luaV_execute of previous call */
		public const int CIST_YIELDED =	(1<<3);	/* call reentered after suspension */
		public const int CIST_YPCALL = 	(1<<4);	/* call is a yieldable protected call */
		public const int CIST_STAT = 	(1<<5);	/* call has an error status (pcall) */
		public const int CIST_TAIL = (1<<6); /* call was tail called */


		public static int isLua(CallInfo ci)	{return ((ci.callstatus & CIST_LUA) != 0) ? 1 : 0;}


		/*
		** `global state', shared by all threads of this state
		*/
		public class global_State {
		  public lua_Alloc frealloc;  /* function to reallocate memory */
		  public object ud;         /* auxiliary data to `frealloc' */
		  public lu_mem totalbytes;  /* number of bytes currently allocated - GCdebt */
		  public l_mem GCdebt;  /* bytes allocated not yet compensated by the collector */
		  public lu_mem lastmajormem;  /* memory in use after last major collection */
		  public stringtable strt = new stringtable();  /* hash table for strings */
		  public TValue l_registry = new TValue();
          public ushort nCcalls;  /* number of nested C calls */
		  public lu_byte currentwhite;
		  public lu_byte gcstate;  /* state of garbage collector */
          public lu_byte gckind;  /* kind of GC running */
          public lu_byte gcrunning;  /* true if GC is running */
		  public int sweepstrgc;  /* position of sweep in `strt' */
		  public GCObject allgc;  /* list of all collectable objects */
		  public GCObject finobj;  /* list of collectable objects with finalizers */
		  public GCObjectRef sweepgc;  /* current position of sweep */
		  public GCObject gray;  /* list of gray objects */
		  public GCObject grayagain;  /* list of objects to be traversed atomically */
		  public GCObject weak;  /* list of tables with weak values */
		  public GCObject ephemeron;  /* list of ephemeron tables (weak keys) */
		  public GCObject allweak;  /* list of all-weak tables */
		  public GCObject tobefnz;  /* list of userdata to be GC */
          public UpVal uvhead = new UpVal();  /* head of double-linked list of all open upvalues */
		  public Mbuffer buff = new Mbuffer();  /* temporary buffer for string concatenation */
		  public int gcpause;  /* size of pause between successive GCs */
          public int gcmajorinc;  /* how much to wait for a major GC (only in gen. mode) */
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
		public class lua_State : GCObject {

		  public lu_byte status;
		  public StkId top;  /* first free slot in the stack */
		  public global_State l_G;
		  public CallInfo ci;  /* call info for current function */
          public /*const*/ InstructionPtr oldpc;  /* last pc traced */
		  public StkId stack_last;  /* last free slot in the stack */
		  public StkId[] stack;  /* stack base */
		  public int stacksize;
		  public ushort nny;  /* number of non-yieldable calls in stack */
		  public lu_byte hookmask;
		  public lu_byte allowhook;
		  public int basehookcount;
		  public int hookcount;
		  public lua_Hook hook;
		  public GCObject openupval;  /* list of open upvalues in this stack */
		  public GCObject gclist;
		  public lua_longjmp errorJmp;  /* current error recover point */
		  public ptrdiff_t errfunc;  /* current error handling function (stack index) */
		  public CallInfo base_ci = new CallInfo();  /* CallInfo for first level (C calling Lua) */
		};


		public static global_State G(lua_State L)	{return L.l_G;}
		public static void G_set(lua_State L, global_State s) { L.l_G = s; } //FIXME:added, not used???


		/*
		** Union of all collectable objects (not a union anymore in the C# port)
		*/
		public class GCObject : GCheader, ArrayElement
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

			public GCheader gch {get{return (GCheader)this;}}   /* common header */
			public TString ts {get{return (TString)this;}}
			public Udata u {get{return (Udata)this;}}
			public Closure cl {get{return (Closure)this;}}
			public Table h {get{return (Table)this;}}
			public Proto p {get{return (Proto)this;}}
			public UpVal uv {get{return (UpVal)this;}}
			public lua_State th {get{return (lua_State)this;}}  /* thread */
		};

		public static GCheader gch(GCObject o)	{ return o.gch; }

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

		public class OpenValRef : GCObjectRef
		{
			public OpenValRef(lua_State L) { this.L = L; }
			public void set(GCObject value) { this.L.openupval = value; }
			public GCObject get() { return this.L.openupval; }
			lua_State L;
		}
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
			public NextRef(GCheader header) { 
				this.header = header; 
				//FIXME:added, for debug only
				//if (this.header == null)
				//{
				//	Debug.Assert(this.header == null);
				//}
			}
			public void set(GCObject value) { this.header.next = value; }
			public GCObject get() { return this.header.next; }
			GCheader header;
		}
		/*
		public class PtrRef : GCObjectRef
		{
			public PtrRef(GCObject obj) { this.obj = obj; }
			public void set(GCObject value) { this.obj = value; }
			public GCObject get() { return this.obj; }
			GCObject obj;
		}*/
		
		public class UDGCRef : GCObjectRef
		{
			public UDGCRef(global_State g) { this.g = g; }
			public void set(GCObject value) { this.g.udgc = value; }
			public GCObject get() { return this.g.udgc; }
			global_State g;
		}
		
		public class TobefnzRef : GCObjectRef
		{
			public TobefnzRef(global_State g) { this.g = g; }
			public void set(GCObject value) { this.g.tobefnz = value; }
			public GCObject get() { return this.g.tobefnz; }
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
		public static TString rawgco2ts(GCObject o) { return (TString)check_exp(o.gch.tt == LUA_TSTRING, o.ts); }
		public static TString gco2ts(GCObject o) { return (TString)(rawgco2ts(o).tsv); }
		public static Udata rawgco2u(GCObject o) { return (Udata)check_exp(o.gch.tt == LUA_TUSERDATA, o.u); }
		public static Udata gco2u(GCObject o) { return (Udata)(rawgco2u(o).uv); }
		public static Closure gco2cl(GCObject o) { return (Closure)check_exp(o.gch.tt == LUA_TFUNCTION, o.cl); }
		public static Table gco2t(GCObject o) { return (Table)check_exp(o.gch.tt == LUA_TTABLE, o.h); }
		public static Proto gco2p(GCObject o) { return (Proto)check_exp(o.gch.tt == LUA_TPROTO, o.p); }
		public static UpVal gco2uv(GCObject o) { return (UpVal)check_exp(o.gch.tt == LUA_TUPVAL, o.uv); }
		public static lua_State gco2th(GCObject o) { return (lua_State)check_exp(o.gch.tt == LUA_TTHREAD, o.th); }

		/* macro to convert any Lua object into a GCObject */
		public static GCObject obj2gco(object v)	{return (GCObject)v;}


		/* actual number of total bytes allocated */
		public static int gettotalbytes(g) { return (g.totalbytes + g.GCdebt); }

	}
}

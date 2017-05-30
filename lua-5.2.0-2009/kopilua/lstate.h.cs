using System.Diagnostics;

namespace KopiLua
{
	using lu_byte = System.Byte;
	using lu_int32 = System.Int32;
	using lu_mem = System.UInt32;
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using ptrdiff_t = System.Int32;
	
	/*

	** Some notes about garbage-collected objects:  All objects in Lua must
	** be kept somehow accessible until being freed.
	**
	** Lua keeps most objects linked in list g->rootgc. The link uses field
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
	** Userdata with finalizers are kept in the list g->rootgc, but after
	** the mainthread, which should be otherwise the last element in the
	** list, as it was the first one inserted there.
	**
	** The list g->tobefnz links all userdata being finalized.

	*/
	public partial class Lua
	{
		/* table of globals */
		public static TValue gt(lua_State L)	{return L.l_gt;}

		/* registry */
		public static TValue registry(lua_State L)	{return G(L).l_registry;}


		/* extra stack space to handle TM calls and some other extras */
		public const int EXTRA_STACK   = 5;


		public const int BASIC_CI_SIZE           = 8;

		public const int BASIC_STACK_SIZE        = (2*LUA_MINSTACK);


		/* kinds of Garbage Collection */
		public const int KGC_NORMAL	= 0;
		public const int KGC_FORCED	= 1;	/* gc was forced by the program */
		public const int KGC_EMERGENCY = 2;	/* gc was forced by an allocation failure */


		public class stringtable {
			public GCObject[] hash;
			public lu_int32 nuse;  /* number of elements */
			public int size;
		};


		/*
		** informations about a call
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

			public StkId base_;  /* base for this function */
			public StkId func;  /* function index in the stack */
			public StkId top;  /* top for this function */
			public InstructionPtr savedpc;
			public short nresults;  /* expected number of results from this function */
			public lu_byte callstatus;
			public int tailcalls;  /* number of tail calls lost under this entry */
		};

		/*
		** Bits in CallInfo status
		*/
		public const int CIST_LUA = 1;	/* call is running a Lua function */
		public const int CIST_HOOKED = 2;	/* call is running a debug hook */
		public const int CIST_REENTRY = 4;	/* call is running on same invocation of
		                                   luaV_execute of previous call */


		public static Closure curr_func(lua_State L) { return (clvalue(L.ci.func)); }
		public static Closure ci_func(CallInfo ci) { return (clvalue(ci.func)); }
		public static int isLua(CallInfo ci)	{return ((ci.callstatus & CIST_LUA) != 0) ? 1 : 0;}


		/*
		** `global state', shared by all threads of this state
		*/
		public class global_State {
		  public stringtable strt = new stringtable(); /* hash table for strings */
		  public lua_Alloc frealloc;  /* function to reallocate memory */
		  public object ud;         /* auxiliary data to `frealloc' */
          public ushort nCcalls;  /* number of nested C calls */
		  public lu_byte currentwhite;
		  public lu_byte gcstate;  /* state of garbage collector */
          public lu_byte gckind;  /* kind of GC running */
		  public int sweepstrgc;  /* position of sweep in `strt' */
		  public GCObject rootgc;  /* list of all collectable objects */
		  public GCObjectRef sweepgc;  /* current position of sweep */
		  public GCObject gray;  /* list of gray objects */
		  public GCObject grayagain;  /* list of objects to be traversed atomically */
		  public GCObject weak;  /* list of tables with weak values */
		  public GCObject ephemeron;  /* list of ephemeron tables (weak keys) */
		  public GCObject allweak;  /* list of all-weak tables */
		  public GCObject tobefnz;  /* list of userdata to be GC */
		  public Mbuffer buff = new Mbuffer();  /* temporary buffer for string concatentation */
		  public lu_mem GCthreshold;
		  public lu_mem totalbytes;  /* number of bytes currently allocated */
		  public lu_mem estimate;  /* an estimate of number of bytes actually in use */
		  public lu_mem gcdept;  /* how much GC is `behind schedule' */
		  public int gcpause;  /* size of pause between successive GCs */
		  public int gcstepmul;  /* GC `granularity' */
		  public lua_CFunction panic;  /* to be called in unprotected errors */
		  public TValue l_registry = new TValue();
		  public lua_State mainthread;
		  public UpVal uvhead = new UpVal();  /* head of double-linked list of all open upvalues */
		  public Table[] mt = new Table[NUM_TAGS];  /* metatables for basic types */
		  public TString[] tmname = new TString[(int)TMS.TM_N];  /* array with tag-method names */
		};


		/*
		** `per thread' state
		*/
		public class lua_State : GCObject {

		  public lu_byte status;
		  public StkId top;  /* first free slot in the stack */
		  public StkId base_;  /* base of current function */
		  public global_State l_G;
		  public CallInfo ci;  /* call info for current function */
		  public InstructionPtr savedpc = new InstructionPtr();  /* `savedpc' of current function */
          public /*const*/ InstructionPtr oldpc;  /* last pc traced */
		  public StkId stack_last;  /* last free slot in the stack */
		  public StkId[] stack;  /* stack base */
		  public CallInfo end_ci;  /* points after end of ci array*/
		  public CallInfo[] base_ci;  /* array of CallInfo's */
		  public int stacksize;
		  public int size_ci;  /* size of array `base_ci' */
		  public ushort baseCcalls;  /* number of nested C calls when resuming */
		  public lu_byte hookmask;
		  public lu_byte allowhook;
		  public int basehookcount;
		  public int hookcount;
		  public lua_Hook hook;
		  public TValue l_gt = new TValue();  /* table of globals */
		  public TValue env = new TValue();  /* temporary place for environments */
		  public GCObject openupval;  /* list of open upvalues in this stack */
		  public GCObject gclist;
		  public lua_longjmp errorJmp;  /* current error recover point */
		  public ptrdiff_t errfunc;  /* current error handling function (stack index) */
		};


		public static global_State G(lua_State L)	{return L.l_G;}
		public static void G_set(lua_State L, global_State s) { L.l_G = s; }


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

		public class RootGCRef : GCObjectRef
		{
			public RootGCRef(global_State g) { this.g = g; }
			public void set(GCObject value) { this.g.rootgc = value; }
			public GCObject get() { return this.g.rootgc; }
			global_State g;
		}

		public class NextRef : GCObjectRef
		{
			public NextRef(GCheader header) { this.header = header; }
			public void set(GCObject value) { this.header.next = value; }
			public GCObject get() { return this.header.next; }
			GCheader header;
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
		public static UpVal ngcotouv(GCObject o) {return (UpVal)check_exp((o.gch.tt == LUA_TUPVAL), o.uv); }
		public static lua_State gco2th(GCObject o) { return (lua_State)check_exp(o.gch.tt == LUA_TTHREAD, o.th); }

		/* macro to convert any Lua object into a GCObject */
		public static GCObject obj2gco(object v)	{return (GCObject)v;}

	}
}

using System;
using System.Diagnostics;

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using lu_byte = System.Byte;
	using lua_Number = System.Double;
	using Instruction = System.UInt32;
	
	public partial class Lua
	{


		/*
		** Extra tags for non-values
		*/
		public const int LUA_TPROTO	= LUA_NUMTAGS;
		public const int LUA_TUPVAL	= (LUA_NUMTAGS+1);
		public const int LUA_TDEADKEY	= (LUA_NUMTAGS+2);

		/*
		** number of all possible tags (including LUA_TNONE but excluding DEADKEY)
		*/
		public const int LUA_TOTALTAGS = (LUA_TUPVAL+2);


		/*
		** tags for Tagged Values have the following use of bits:
		** bits 0-3: actual tag (a LUA_T* value)
		** bits 4-5: variant bits
		** bit 6: whether value is collectable
		*/

		/*
		** LUA_TFUNCTION variants:
		** 0 - Lua function
		** 1 - light C function
		** 2 - regular C function (closure)
		*/

		/* Variant tags for functions */
		public const int LUA_TLCL = (LUA_TFUNCTION | (0 << 4));  /* Lua closure */
		public const int LUA_TLCF = (LUA_TFUNCTION | (1 << 4));  /* light C function */
		public const int LUA_TCCL = (LUA_TFUNCTION | (2 << 4));  /* C closure */


		/* Bit mark for collectable types */
		public const int BIT_ISCOLLECTABLE = (1 << 6);

		/* mark a tag as collectable */
		public static int ctb(int t) { return (t | BIT_ISCOLLECTABLE); }


		public interface ArrayElement //FIXME:added
		{
			void set_index(int index);
			void set_array(object array);
		}

		/*
		** Union of all collectable objects
		*/
		//typedef union GCObject GCObject;


		/*
		** Common Header for all collectable objects (in macro form, to be
		** included in other objects)
		*/
		public class CommonHeader { public GCObject next; public lu_byte tt; public lu_byte marked;}


		/*
		** Common header in struct form
		*/
		public class GCheader : CommonHeader {
		};



		/*
		** Union of all Lua values
		*/
		//typedef union Value Value;


		//#define numfield	lua_Number n;    /* numbers */		//FIXME:remove, see below



		/*
		** Tagged Values. This is the basic representation of values in Lua,
		** an actual value plus a tag with its type.
		*/

		//#define TValuefields	Value value; int tt

        //typedef struct lua_TValue TValue;

		public class lua_TValue : ArrayElement //FIXME:changed
		{
			private lua_TValue[] values = null;
			private int index = -1;

			public void set_index(int index)
			{
				this.index = index;
			}

			public void set_array(object array)
			{
				this.values = (lua_TValue[])array;
				Debug.Assert(this.values != null);
			}

			public lua_TValue this[int offset]
			{
				get { return this.values[this.index + offset]; }
			}

			public lua_TValue this[uint offset]
			{
				get { return this.values[this.index + (int)offset]; }
			}

			public static lua_TValue operator +(lua_TValue value, int offset)
			{
				return value.values[value.index + offset];
			}

			public static lua_TValue operator +(int offset, lua_TValue value)
			{
				return value.values[value.index + offset];
			}

			public static lua_TValue operator -(lua_TValue value, int offset)
			{
				return value.values[value.index - offset];
			}

			public static int operator -(lua_TValue value, lua_TValue[] array)
			{
				Debug.Assert(value.values == array);
				return value.index;
			}

			public static int operator -(lua_TValue a, lua_TValue b)
			{
				Debug.Assert(a.values == b.values);
				return a.index - b.index;
			}
			
			public static bool operator <(lua_TValue a, lua_TValue b)
			{
				Debug.Assert(a.values == b.values);
				return a.index < b.index;
			}

			public static bool operator <=(lua_TValue a, lua_TValue b)
			{
				Debug.Assert(a.values == b.values);
				return a.index <= b.index;
			}

			public static bool operator >(lua_TValue a, lua_TValue b)
			{
				Debug.Assert(a.values == b.values);
				return a.index > b.index;
			}

			public static bool operator >=(lua_TValue a, lua_TValue b)
			{
				Debug.Assert(a.values == b.values);
				return a.index >= b.index;
			}
			
			public static lua_TValue inc(ref lua_TValue value)
			{
				value = value[1];
				return value[-1];
			}

			public static lua_TValue dec(ref lua_TValue value)
			{
				value = value[-1];
				return value[1];
			}

			public static implicit operator int(lua_TValue value)
			{
				return value.index;
			}

			public lua_TValue()
			{
				this.values = null;
				this.index = 0;
				this.value_ = new Value();
				this.tt_ = 0;
			}

			public lua_TValue(lua_TValue value)
			{
				this.values = value.values;
				this.index = value.index;
				this.value_ = value.value_; // todo: do a shallow copy here
				this.tt_ = value.tt_;
			}

			public lua_TValue(lua_TValue[] values)
			{
				this.values = values;
				this.index = Array.IndexOf(values, this);
				this.value_ = new Value();
				this.tt_ = 0;
			}

			public lua_TValue(Value value, int tt)
			{
				this.values = null;
				this.index = 0;
				this.value_ = value;
				this.tt_ = tt;
			}

			public lua_TValue(lua_TValue[] values, Value value, int tt)
			{
				this.values = values;
				this.index = Array.IndexOf(values, this);
				this.value_ = value;
				this.tt_ = tt;
			}

		  public Value value_ = new Value(); //FIXME:see TValuefields
		  public int tt_;
		};


		/* macro defining a nil value */
		//#define NILCONSTANT {NULL}, LUA_TNIL //FIXME:removed


		public static Value val_(TValue o) { return o.value_; }
		public static lua_Number num_(TValue o) { return val_(o).n; }
		public static lua_Number setNum_(TValue o, lua_Number x) { return (val_(o).n = x); } //FIXME:added

		/* raw type tag of a TValue */
		public static int rttype(TValue o) { return o.tt_; }

		/* type tag of a TValue (bits 0-3 for tags + variant bits 4-5) */
		public static int ttype(TValue o) { return (rttype(o) & 0x3F); }


		/* type tag of a TValue with no variants (bits 0-3) */
		public static int ttypenv(TValue o) { return (rttype(o) & 0x0F); }


		/* Macros to test type */
		public static bool checktag(TValue o, int t)	    {return (rttype(o) == t);}
		public static bool ttisnumber(TValue o)		{return checktag(o, LUA_TNUMBER);}		
		public static bool ttisnil(TValue o)	{return checktag(o, LUA_TNIL);}
		public static bool ttisboolean(TValue o)	{return checktag(o, LUA_TBOOLEAN);}
		public static bool ttislightuserdata(TValue o)	{return checktag(o, LUA_TLIGHTUSERDATA);}
		public static bool ttisstring(TValue o)	{return checktag(o, ctb(LUA_TSTRING));}
		public static bool ttistable(TValue o)	{return checktag(o, ctb(LUA_TTABLE));}
		public static bool ttisfunction(TValue o)	{return (ttypenv(o) == LUA_TFUNCTION);}
		public static bool ttisclosure(TValue o) {return ((rttype(o) & 0x1F) == LUA_TFUNCTION);}
		public static bool ttisCclosure(TValue o) {return checktag(o, ctb(LUA_TCCL));}
		public static bool ttisLclosure(TValue o) {return checktag((o), ctb(LUA_TLCL));}
		public static bool ttislcf(TValue o) {return checktag((o), LUA_TLCF);}
		public static bool ttisuserdata(TValue o)	{return checktag(o, ctb(LUA_TUSERDATA));}
		public static bool ttisthread(TValue o)	{return checktag(o, ctb(LUA_TTHREAD));}
        public static bool ttisdeadkey(TValue o) {return checktag(o, ctb(LUA_TDEADKEY));}
		
        public static bool ttisequal(TValue o1, TValue o2)	{return (rttype(o1) == rttype(o2));}

		/* Macros to access values */
        public static lua_Number nvalue(TValue o) { return (lua_Number)check_exp(ttisnumber(o), num_(o)); }
		public static GCObject gcvalue(TValue o) { return (GCObject)check_exp(iscollectable(o), val_(o).gc); }
		public static object pvalue(TValue o) { return (object)check_exp(ttislightuserdata(o), val_(o).p); }
		public static TString rawtsvalue(TValue o) { return (TString)check_exp(ttisstring(o), val_(o).gc.ts); }
		public static TString_tsv tsvalue(TValue o) { return rawtsvalue(o).tsv; }
		public static Udata rawuvalue(TValue o) { return (Udata)check_exp(ttisuserdata(o), val_(o).gc.u); }
		public static Udata_uv uvalue(TValue o) { return rawuvalue(o).uv; }
		public static Closure clvalue(TValue o)	{return (Closure)check_exp(ttisclosure(o), val_(o).gc.cl);}
		public static LClosure clLvalue(TValue o) {return (LClosure)check_exp(ttisLclosure(o), val_(o).gc.cl.l);}
		public static CClosure clCvalue(TValue o) {return (CClosure)check_exp(ttisCclosure(o), val_(o).gc.cl.c);}
		public static lua_CFunction fvalue(TValue o)	{ return (lua_CFunction)check_exp(ttislcf(o), val_(o).f); }
		public static Table hvalue(TValue o)	{return (Table)check_exp(ttistable(o), val_(o).gc.h);}
		public static int bvalue(TValue o)	{return (int)check_exp(ttisboolean(o), val_(o).b);}
		public static lua_State thvalue(TValue o)	{return (lua_State)check_exp(ttisthread(o), val_(o).gc.th);}

		public static int l_isfalse(TValue o) { return ((ttisnil(o) || (ttisboolean(o) && bvalue(o) == 0))) ? 1 : 0; }

		
		public static bool iscollectable(TValue o)	{ return (rttype(o) & BIT_ISCOLLECTABLE) != 0; }


		/* Macros for internal tests */
		public static bool righttt(TValue obj) { return (ttypenv(obj) == gcvalue(obj).gch.tt); }

		public static void checkliveness(global_State g, TValue obj) {
			lua_longassert(!iscollectable(obj) || 
					(righttt(obj) && !isdead(g, gcvalue(obj))));}


		/* Macros to set values */
		public static void settt_(TValue o, int t) {o.tt_=t;}

		public static void setnvalue(TValue obj, lua_Number x)
		  { TValue io=obj; setNum_(io, x); settt_(io, LUA_TNUMBER);} //FIXME:num_=>setNum_

		public static void changenvalue(TValue o, lua_Number x) { check_exp(ttisnumber(o), setNum_(o, x));} //FIXME:num_=>setNum_

		public static void setnilvalue(TValue obj) { settt_(obj, LUA_TNIL);}

		public static void setfvalue(TValue obj, lua_CFunction x) 
		  { TValue io=obj; val_(io).f=x; settt_(io, LUA_TLCF); }
		
		public static void setpvalue( TValue obj, object x) 
		  { TValue io=obj; val_(io).p=x; settt_(io, LUA_TLIGHTUSERDATA);}

		public static void setbvalue(TValue obj, int x) 
		  { TValue io=obj; val_(io).b=x; settt_(io, LUA_TBOOLEAN); }

		public static void setgcovalue(lua_State L, TValue obj, GCObject x)
		  { TValue io=obj; GCObject i_g=x;
		    val_(io).gc=i_g; settt_(io, ctb(gch(i_g).tt)); }
	
		public static void setsvalue(lua_State L, TValue obj, GCObject x) 
		  {  TValue io=obj; 
		     val_(io).gc=(GCObject)x; settt_(io, ctb(LUA_TSTRING));
			 checkliveness(G(L),io); }

		public static void setuvalue(lua_State L, TValue obj, GCObject x) 
		  { TValue io=obj; 
		     val_(io).gc=(GCObject)x; settt_(io, ctb(LUA_TUSERDATA));
			 checkliveness(G(L),io); }

		public static void setthvalue(lua_State L, TValue obj, GCObject x) 
		  { TValue io=obj; 
		     val_(io).gc=(GCObject)x; settt_(io, ctb(LUA_TTHREAD));
			 checkliveness(G(L),io); }

		public static void setclLvalue(lua_State L, TValue obj, GCObject x)
	      { TValue io=obj; 
		     val_(io).gc=(GCObject)x; settt_(io, ctb(LUA_TLCL));
			 checkliveness(G(L),io); }

		public static void setclCvalue(lua_State L, TValue obj, GCObject x)
		  { TValue io=obj; 
		     val_(io).gc=(GCObject)x; settt_(io, ctb(LUA_TCCL));
			 checkliveness(G(L),io); }

		public static void sethvalue(lua_State L, TValue obj, Table x)
		  { TValue io=obj; 
		     val_(io).gc=(GCObject)x; settt_(io, ctb(LUA_TTABLE));
			 checkliveness(G(L),io); }

		public static void setptvalue(lua_State L, TValue obj, Proto x)
		  { TValue io=obj; 
		     val_(io).gc=(GCObject)x; settt_(io, ctb(LUA_TPROTO));
			 checkliveness(G(L),io); }
		
		public static void setdeadvalue(TValue obj) { settt_(obj, ctb(LUA_TDEADKEY)); }



		public static void setobj(lua_State L, TValue obj1, TValue obj2) 
		    { TValue io2=(obj2); TValue io1=(obj1);
			  io1.value_ = io2.value_; io1.tt_ = io2.tt_;
			  checkliveness(G(L), io1);}


		/*
		** different types of assignments, according to destination
		*/

		/* from stack to (same) stack */
		//#define setobjs2s	setobj
		public static void setobjs2s(lua_State L, TValue obj, TValue x) { setobj(L, obj, x); }
		///* to stack (not from same stack) */
		
		//#define setobj2s	setobj
		public static void setobj2s(lua_State L, TValue obj, TValue x) { setobj(L, obj, x); }

		//#define setsvalue2s	setsvalue
		public static void setsvalue2s(lua_State L, TValue obj, TString x) { setsvalue(L, obj, x); }

		//#define sethvalue2s	sethvalue
		public static void sethvalue2s(lua_State L, TValue obj, Table x) { sethvalue(L, obj, x); }

		//#define setptvalue2s	setptvalue
		public static void setptvalue2s(lua_State L, TValue obj, Proto x) { setptvalue(L, obj, x); }

		///* from table to same table */
		//#define setobjt2t	setobj
		public static void setobjt2t(lua_State L, TValue obj, TValue x) { setobj(L, obj, x); }

		///* to table */
		//#define setobj2t	setobj
		public static void setobj2t(lua_State L, TValue obj, TValue x) { setobj(L, obj, x); }

		///* to new object */
		//#define setobj2n	setobj
		public static void setobj2n(lua_State L, TValue obj, TValue x) { setobj(L, obj, x); }

		//#define setsvalue2n	setsvalue
		public static void setsvalue2n(lua_State L, TValue obj, TString x) { setsvalue(L, obj, x); }



		//----------------------------------
		//FIXME:removed, see below
		/*
		** {======================================================
		** NaN Trick
		** =======================================================
		*/

		//#if defined(LUA_NANTRICKLE) || defined(LUA_NANTRICKBE)

		/*
		** numbers are represented in the 'd_' field. All other values have the
		** value (NNMARK | tag) in 'tt_'. A number with such pattern would be
		** a "signaled NaN", which is never generated by regular operations by
		** the CPU (nor by 'strtod')
		*/
		//#if !defined(NNMARK)
		//#define NNMARK		0x7FF7A500
		//#endif

		//#undef TValuefields
		//#undef NILCONSTANT
		//#if defined(LUA_NANTRICKLE)
		/* little endian */
		//#define TValuefields  \
		//	union { struct { Value v_; int tt_; } i; double d_; } u
		//#define NILCONSTANT	{{{NULL}, tag2tt(LUA_TNIL)}}
		//#else
		/* big endian */
		//#define TValuefields  \
		//	union { struct { int tt_; Value v_; } i; double d_; } u
		//#define NILCONSTANT	{{tag2tt(LUA_TNIL), {NULL}}}
		//#endif

		//#undef numfield
		//#define numfield	/* no such field; numbers are the entire struct */

		/* basic check to distinguish numbers from non-numbers */
		//#undef ttisnumber
		//#define ttisnumber(o)	(((o)->u.i.tt_ & 0x7fffff00) != NNMARK)

		//#define tag2tt(t)	(NNMARK | (t))

		//#undef val_
		//#define val_(o)		((o)->u.i.v_)
		//#undef num_
		//#define num_(o)		((o)->u.d_)

		//#undef rttype
		//#define rttype(o)	(ttisnumber(o) ? LUA_TNUMBER : (o)->u.i.tt_ & 0xff)

		//#undef settt_
		//#define settt_(o,t)	((o)->u.i.tt_=tag2tt(t))

		//#undef setnvalue
		//#define setnvalue(obj,x) \
		//	{ TValue *io_=(obj); num_(io_)=(x); lua_assert(ttisnumber(io_)); }

		//#undef setobj
		//#define setobj(L,obj1,obj2) \
		//	{ const TValue *o2_=(obj2); TValue *o1_=(obj1); \
		//	  o1_->u = o2_->u; \
		//	  checkliveness(G(L),o1_); }


		/*
		** these redefinitions are not mandatory, but these forms are more efficient
		*/

		//#undef checktag
		//#define checktag(o,t)	((o)->u.i.tt_ == tag2tt(t))

		//#undef ttisequal
		//#define ttisequal(o1,o2)  \
		//	(ttisnumber(o1) ? ttisnumber(o2) : ((o1)->u.i.tt_ == (o2)->u.i.tt_))



		//#define luai_checknum(L,o,c)	{ if (!ttisnumber(o)) c; }


		//#else

		//#define luai_checknum(L,o,c)	{ /* empty */ }

		//#endif
		/* }====================================================== */
		//FIXME:removed, see upper
		//----------------------------------
		public delegate void luai_checknum_func();
		public static void luai_checknum(lua_State L, StkId o, luai_checknum_func c)  { /* empty */ }


		/*
		** {======================================================
		** types and prototypes
		** =======================================================
		*/


		public class Value {
		  public GCObject gc;     /* collectable objects */
		  public object p;        /* light userdata */
		  public int b;           /* booleans */
		  public lua_CFunction f; /* light C functions */
		  public lua_Number n;    /* numbers */ //FIXME:changed, numfield
		};


		//struct lua_TValue {
		//  TValuefields;
		//};


		//typedef TValue *StkId;  /* index to stack elements */




		/*
		** Header for string value; string bytes follow the end of this structure
		*/
		public class TString_tsv : GCObject { //FIXME:added
            //CommonHeader;
			public lu_byte reserved;
			public uint hash;
			public uint len;  /* number of characters in string */
		};	
		public class TString : TString_tsv {
			//public L_Umaxalign dummy;  /* ensures maximum alignment for strings */
			//struct {
			//    CommonHeader;
			//    lu_byte reserved;
			//    unsigned int hash;
			//    size_t len;  /* number of characters in string */
			//} tsv;
			public TString_tsv tsv { get { return this; } }
			public TString() { }
			public CharPtr str; //FIXME:added = new CharPtr()???;
			public override string ToString() { return str.ToString(); } // for debugging
		};

        /* get the actual string (array of bytes) from a TString */
		public static CharPtr getstr(TString ts) { return ts.str; }
		public static CharPtr getstr(TString_tsv ts) { return ((TString)ts).str; } //FIXME:added
		
		/* get the actual string (array of bytes) from a Lua value */
		public static CharPtr svalue(StkId o) { return getstr(rawtsvalue(o)); }


		/*
		** Header for userdata; memory area follows the end of this structure
		*/
		public class Udata_uv : GCObject { //FIXME:added
			public Table metatable;
			public Table env;
			public uint len;  /* number of bytes */
		};
		public class Udata : Udata_uv {
			//public L_Umaxalign dummy;  /* ensures maximum alignment for `local' udata */
			public Udata() { this.uv = this; }
			//struct {
			//    CommonHeader;
			//    struct Table *metatable;
			//    struct Table *env;
			//    size_t len;  /* number of bytes */
			//} uv;
			public new Udata_uv uv;
			// in the original C code this was allocated alongside the structure memory. it would probably
			// be possible to still do that by allocating memory and pinning it down, but we can do the
			// same thing just as easily by allocating a seperate byte array for it instead.
			public object user_data;
		};

		/*
		** Description of an upvalue for function prototypes
		*/
		public class Upvaldesc : ArrayElement {
			//-----------------------------------
			//FIXME:ArrayElement added
			private Upvaldesc[] values = null; 
			private int index = -1;

			public void set_index(int index)
			{
				this.index = index;
			}

			public void set_array(object array)
			{
				this.values = (Upvaldesc[])array;
				Debug.Assert(this.values != null);
			}
			//------------------------------------------
			
		  public TString name;  /* upvalue name (for debug information) */
		  public lu_byte instack;  /* whether it is in stack */
		  public lu_byte idx;  /* index of upvalue (in stack or in outer function's list) */
		};


		/*
		** Description of a local variable for function prototypes
		** (used for debug information)
		*/
		public class LocVar {
		  public TString varname;
		  public int startpc;  /* first point where variable is active */
		  public int endpc;    /* first point where variable is dead */
		};



		/*
		** Function Prototypes
		*/
		public class Proto : GCObject {

		  public Proto[] protos = null; //FIXME:added, CommonHeader
		  public int index = 0; //FIXME:added, CommonHeader
		  public Proto this[int offset] {get { return this.protos[this.index + offset]; }} //FIXME:added, CommonHeader

		  public TValue[] k;  /* constants used by the function */
		  public Instruction[] code;
		  public new Proto[] p;  /* functions defined inside the function */ //FIXME:added, new
		  public int[] lineinfo;  /* map from opcodes to source lines */
		  public LocVar[] locvars;  /* information about local variables */
		  public Upvaldesc[] upvalues;  /* upvalue information */
          public Closure cache;  /* last created closure with this prototype */
		  public TString  source;
		  public int sizeupvalues;  /* size of 'upvalues' */
		  public int sizek;  /* size of `k' */
		  public int sizecode;
		  public int sizelineinfo;
		  public int sizep;  /* size of `p' */
		  public int sizelocvars;
		  public int linedefined;
		  public int lastlinedefined;
		  public GCObject gclist;
		  public lu_byte numparams;  /* number of fixed parameters */
		  public lu_byte is_vararg;
		  public lu_byte maxstacksize; /* maximum stack used by this function */
		};



		/*
		** Lua Upvalues
		*/
		public class UpVal : GCObject {
		  public TValue v;  /* points to stack or to its own value */

			public class _u {
				public TValue value_ = new TValue();  /* the value (when closed) */

				public class _l {  /* double linked list (when open) */
				  public UpVal prev;
				  public UpVal next;
				};

				public _l l = new _l();
		  }
			public new _u u = new _u();
		};
		//added
		public class UpValRef {
			private UpVal[] _upVals;
			private int _index;
			public UpValRef(UpVal[] upVals, int index)
			{
				this._upVals = upVals;
				this._index = index;
			}
			
			public UpVal get()
			{
				return this._upVals[this._index];
			}
			
			public void set(UpVal val)
			{
				this._upVals[this._index] = val;
			}
		}


		/*
		** Closures
		*/

		public class ClosureHeader : GCObject {
			/*CommonHeader; */public lu_byte isC; public lu_byte nupvalues; public GCObject gclist;};

		public class ClosureType {

			ClosureHeader header;

			public static implicit operator ClosureHeader(ClosureType ctype) {return ctype.header;}
			public ClosureType(ClosureHeader header) {this.header = header;}

			public lu_byte isC { get { return header.isC; } set { header.isC = value; } }
			public lu_byte nupvalues { get { return header.nupvalues; } set { header.nupvalues = value; } }
			public GCObject gclist { get { return header.gclist; } set { header.gclist = value; } }
			//public Table env { get { return header.env; } set { header.env = value; } }//FIXME:removed
		}

		public class CClosure : ClosureType {
			public CClosure(ClosureHeader header) : base(header) { }
			public lua_CFunction f;
			public TValue[] upvalue;  /* list of upvalues */ //FIXME:TValue[1]
		};


		public class LClosure : ClosureType {
			public LClosure(ClosureHeader header) : base(header) { }
			public Proto p;
			public UpVal[] upvals;  /* list of upvalues */ //FIXME:UpVal*[1]
		};

		public class Closure : ClosureHeader
		{
		  public Closure()
		  {
			  c = new CClosure(this);
			  l = new LClosure(this);
		  }

		  public CClosure c;
		  public LClosure l;
		};


		public static bool isLfunction(TValue o) { return ttisLclosure(o); }

        public static Proto getproto(TValue o) { return (clLvalue(o).p); }


		/*
		** Tables
		*/

		public class TKey_nk : TValue
		{
			public TKey_nk() { }
			public TKey_nk(Value value, int tt, Node next) : base(value, tt)
			{
				this.next = next;
			}
			public Node next;  /* for chaining */
		};

		public class TKey {
			public TKey()
			{
				this.nk = new TKey_nk();
			}
			public TKey(TKey copy)
			{
				this.nk = new TKey_nk(copy.nk.value_, copy.nk.tt_, copy.nk.next);
			}
			public TKey(Value value, int tt, Node next)
			{
				this.nk = new TKey_nk(value, tt, next);
			}

			public TKey_nk nk = new TKey_nk();
			public TValue tvk { get { return this.nk; } }
		};


		public class Node : ArrayElement
		{
			private Node[] values = null;
			private int index = -1;

			public void set_index(int index)
			{
				this.index = index;
			}

			public void set_array(object array)
			{
				this.values = (Node[])array;
				Debug.Assert(this.values != null);
			}

			public static int ids = 0;
			public int id = ids++;

			public Node()
			{
				this.i_val = new TValue();
				this.i_key = new TKey();
			}

			public Node(Node copy)
			{
				this.values = copy.values;
				this.index = copy.index;
				this.i_val = new TValue(copy.i_val);
				this.i_key = new TKey(copy.i_key);
			}

			public Node(TValue i_val, TKey i_key)
			{
				this.values = new Node[] { this };
				this.index = 0;
				this.i_val = i_val;
				this.i_key = i_key;
			}

			public TValue i_val;
			public TKey i_key;

			public Node this[uint offset]
			{
				get { return this.values[this.index + (int)offset]; }
			}

			public Node this[int offset]
			{
				get { return this.values[this.index + offset]; }
			}

			public static int operator -(Node n1, Node n2)
			{
				Debug.Assert(n1.values == n2.values);
				return n1.index - n2.index;
			}

			public static Node inc(ref Node node)
			{
				node = node[1];
				return node[-1];
			}

			public static Node dec(ref Node node)
			{
				node = node[-1];
				return node[1];
			}

			public static bool operator >(Node n1, Node n2) { Debug.Assert(n1.values == n2.values); return n1.index > n2.index; }
			public static bool operator >=(Node n1, Node n2) { Debug.Assert(n1.values == n2.values); return n1.index >= n2.index; }
			public static bool operator <(Node n1, Node n2) { Debug.Assert(n1.values == n2.values); return n1.index < n2.index; }
			public static bool operator <=(Node n1, Node n2) { Debug.Assert(n1.values == n2.values); return n1.index <= n2.index; }
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
			public static bool operator !=(Node n1, Node n2) { return !(n1==n2); }

			public override bool Equals(object o) {return this == (Node)o;}
			public override int GetHashCode() {return 0;}
		};


		public class Table : GCObject {
		  public lu_byte flags;  /* 1<<p means tagmethod(p) is not present */
		  public lu_byte lsizenode;  /* log2 of size of `node' array */
		  public Table metatable;
		  public TValue[] array;  /* array part */
		  public Node[] node;
		  public int lastfree;  /* any free position is before this position */ //FIXME: this is differente from original code, use t.node[t.lastfree] to get Node value
		  public GCObject gclist;
		  public int sizearray;  /* size of `array' array */
		};



		/*
		** `module' operation for hashing (size is always a power of 2)
		*/
		//#define lmod(s,size) \
		//    (check_exp((size&(size-1))==0, (cast(int, (s) & ((size)-1)))))


		public static int twoto(int x) { return 1 << x; }
		public static int sizenode(Table t)	{return twoto(t.lsizenode);}


        //FIXME:??? move to lobject.c
		/*
		** (address of) a fixed nil value
		*/
		public static TValue luaO_nilobject_ = new TValue(new Value(), LUA_TNIL); //FIXME:??? new Tvalue(null, LUA_TNIL);
		public static TValue luaO_nilobject = luaO_nilobject_;
	}
}

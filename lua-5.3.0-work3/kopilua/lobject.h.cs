/*
** $Id: lobject.h,v 2.86 2014/02/19 13:52:42 roberto Exp $
** Type definitions for Lua objects
** See Copyright Notice in lua.h
*/
using System;
using System.Diagnostics;

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using lu_byte = System.Byte;
	using lua_Number = System.Double;
	using Instruction = System.UInt32;
	using lua_Integer = System.Int32;
	
	public partial class Lua
	{


		/*
		** Extra tags for non-values
		*/
		public const int LUA_TPROTO	= LUA_NUMTAGS;
		public const int LUA_TDEADKEY	= (LUA_NUMTAGS+1);

		/*
		** number of all possible tags (including LUA_TNONE but excluding DEADKEY)
		*/
		public const int LUA_TOTALTAGS = (LUA_TPROTO+2);


		/*
		** tags for Tagged Values have the following use of bits:
		** bits 0-3: actual tag (a LUA_T* value)
		** bits 4-5: variant bits
		** bit 6: whether value is collectable
		*/
		
		public const int VARBITS = (3 << 4);


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


		/* Variant tags for strings */
		public const int LUA_TSHRSTR = (LUA_TSTRING | (0 << 4));  /* short strings */
		public const int LUA_TLNGSTR = (LUA_TSTRING | (1 << 4));  /* long strings */


		/* Variant tags for numbers */
		public const int LUA_TNUMFLT = (LUA_TNUMBER | (0 << 4));  /* float numbers */
		public const int LUA_TNUMINT = (LUA_TNUMBER | (1 << 4));  /* integer numbers */


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
		  
		  public Node _parent; //FIXME:added, see offsetof
		};


		/* macro defining a nil value */
		//#define NILCONSTANT {NULL}, LUA_TNIL //FIXME:removed


		public static Value val_(TValue o) { return o.value_; }

		/* raw type tag of a TValue */
		public static int rttype(TValue o) { return o.tt_; }

		/* tag with no variants (bits 0-3) */
		public static int novariant(int x)	{ return ((x) & 0x0F); }

		/* type tag of a TValue (bits 0-3 for tags + variant bits 4-5) */
		public static int ttype(TValue o) { return (rttype(o) & 0x3F); }


		/* type tag of a TValue with no variants (bits 0-3) */
		public static int ttnov(TValue o) { return novariant(rttype(o)); }


		/* Macros to test type */
		public static bool checktag(TValue o, int t)	    {return (rttype(o) == t);}
		public static bool checktype(TValue o, int t)		{return (ttnov(o) == (t));}
		public static bool ttisnumber(TValue o)		{return checktype(o, LUA_TNUMBER);}	
		public static bool ttisfloat(TValue o)		{return checktag(o, LUA_TNUMFLT); }
		public static bool ttisinteger(TValue o)	{ return checktag(o, LUA_TNUMINT); }
		public static bool ttisnil(TValue o)	{return checktag(o, LUA_TNIL);}
		public static bool ttisboolean(TValue o)	{return checktag(o, LUA_TBOOLEAN);}
		public static bool ttislightuserdata(TValue o)	{return checktag(o, LUA_TLIGHTUSERDATA);}
		public static bool ttisstring(TValue o)		{return checktype((o), LUA_TSTRING);}
		public static bool ttisshrstring(TValue o)	{return checktag((o), ctb(LUA_TSHRSTR));}
		public static bool ttislngstring(TValue o)	{return checktag((o), ctb(LUA_TLNGSTR));}
		public static bool ttistable(TValue o)	{return checktag(o, ctb(LUA_TTABLE));}
		public static bool ttisfunction(TValue o)		{return checktype(o, LUA_TFUNCTION);}
		public static bool ttisclosure(TValue o) {return ((rttype(o) & 0x1F) == LUA_TFUNCTION);}
		public static bool ttisCclosure(TValue o) {return checktag(o, ctb(LUA_TCCL));}
		public static bool ttisLclosure(TValue o) {return checktag((o), ctb(LUA_TLCL));}
		public static bool ttislcf(TValue o) {return checktag((o), LUA_TLCF);}
		public static bool ttisfulluserdata(TValue o)	{return checktag(o, ctb(LUA_TUSERDATA));}
		public static bool ttisthread(TValue o)	{return checktag(o, ctb(LUA_TTHREAD));}
        public static bool ttisdeadkey(TValue o) {return checktag(o, LUA_TDEADKEY);}
		

		/* Macros to access values */
		public static lua_Integer ivalue(TValue o)	{ return (lua_Integer)check_exp(ttisinteger(o), val_(o).i); }
		public static lua_Number fltvalue(TValue o) { return (lua_Number)check_exp(ttisfloat(o), val_(o).n); }
		public static GCObject gcvalue(TValue o) { return (GCObject)check_exp(iscollectable(o), val_(o).gc); }
		public static object pvalue(TValue o) { return (object)check_exp(ttislightuserdata(o), val_(o).p); }
		public static TString rawtsvalue(TValue o) { return (TString)check_exp(ttisstring(o), val_(o).gc.ts); }
		public static TString_tsv tsvalue(TValue o) { return rawtsvalue(o).tsv; }
		public static Udata rawuvalue(TValue o) { return (Udata)check_exp(ttisfulluserdata(o), val_(o).gc.u); }
		public static Udata_uv uvalue(TValue o) { return rawuvalue(o).uv; }
		public static Closure clvalue(TValue o)	{return (Closure)check_exp(ttisclosure(o), val_(o).gc.cl);}
		public static LClosure clLvalue(TValue o) {return (LClosure)check_exp(ttisLclosure(o), val_(o).gc.cl.l);}
		public static CClosure clCvalue(TValue o) {return (CClosure)check_exp(ttisCclosure(o), val_(o).gc.cl.c);}
		public static lua_CFunction fvalue(TValue o)	{ return (lua_CFunction)check_exp(ttislcf(o), val_(o).f); }
		public static Table hvalue(TValue o)	{return (Table)check_exp(ttistable(o), val_(o).gc.h);}
		public static int bvalue(TValue o)	{return (int)check_exp(ttisboolean(o), val_(o).b);}
		public static lua_State thvalue(TValue o)	{return (lua_State)check_exp(ttisthread(o), val_(o).gc.th);}
		/* a dead value may get the 'gc' field, but cannot access its contents */
		public static object deadvalue(TValue o) { return (object)check_exp(ttisdeadkey(o), (object)(val_(o).gc)); }

		public static int l_isfalse(TValue o) { return ((ttisnil(o) || (ttisboolean(o) && bvalue(o) == 0))) ? 1 : 0; }

		
		public static bool iscollectable(TValue o)	{ return (rttype(o) & BIT_ISCOLLECTABLE) != 0; }


		/* Macros for internal tests */
		public static bool righttt(TValue obj) { return (ttype(obj) == gcvalue(obj).gch.tt); }

		public static void checkliveness(global_State g, TValue obj) {
			lua_longassert(!iscollectable(obj) || 
					(righttt(obj) && !isdead(g, gcvalue(obj))));}


		/* Macros to set values */
		public static void settt_(TValue o, int t) {o.tt_=t;}

		public static void setnvalue(TValue obj, lua_Number x)
		  { TValue io=obj; io.value_.n=x; settt_(io, LUA_TNUMFLT);}

		public static void setivalue(TValue obj, lua_Integer x)
		  { TValue io=obj; io.value_.i=x; settt_(io, LUA_TNUMINT);}

		public static void setnilvalue(TValue obj) { settt_(obj, LUA_TNIL);}

		public static void setfvalue(TValue obj, lua_CFunction x) 
		  { TValue io=obj; io.value_.f=x; settt_(io, LUA_TLCF); } //FIXME:chagned, val_(io)
		
		public static void setpvalue(TValue obj, object x) 
		  { TValue io=obj; io.value_.p=x; settt_(io, LUA_TLIGHTUSERDATA);} //FIXME:chagned, val_(io)

		public static void setbvalue(TValue obj, int x) 
		  { TValue io=obj; io.value_.b=x; settt_(io, LUA_TBOOLEAN); } //FIXME:chagned, val_(io)

		public static void setgcovalue(lua_State L, TValue obj, GCObject x)
		  { TValue io=obj; GCObject i_g=x;
		    io.value_.gc=i_g; settt_(io, ctb(gch(i_g).tt)); } //FIXME:chagned, val_(io)
	
		public static void setsvalue(lua_State L, TValue obj, GCObject x) 
		  {  TValue io=obj; 
			 TString x_ = (TString)x;
		     io.value_.gc=(GCObject)x_; settt_(io, ctb(x_.tsv.tt)); //FIXME:chagned, val_(io)
			 checkliveness(G(L),io); }

		public static void setuvalue(lua_State L, TValue obj, GCObject x) 
		  { TValue io=obj; 
		     io.value_.gc=(GCObject)x; settt_(io, ctb(LUA_TUSERDATA)); //FIXME:chagned, val_(io)
			 checkliveness(G(L),io); }

		public static void setthvalue(lua_State L, TValue obj, GCObject x) 
		  { TValue io=obj; 
		     io.value_.gc=(GCObject)x; settt_(io, ctb(LUA_TTHREAD)); //FIXME:chagned, val_(io)
			 checkliveness(G(L),io); }

		public static void setclLvalue(lua_State L, TValue obj, GCObject x)
	      { TValue io=obj; 
		     io.value_.gc=(GCObject)x; settt_(io, ctb(LUA_TLCL)); //FIXME:chagned, val_(io)
			 checkliveness(G(L),io); }

		public static void setclCvalue(lua_State L, TValue obj, GCObject x)
		  { TValue io=obj; 
		     io.value_.gc=(GCObject)x; settt_(io, ctb(LUA_TCCL)); //FIXME:chagned, val_(io)
			 checkliveness(G(L),io); }

		public static void sethvalue(lua_State L, TValue obj, Table x)
		  { TValue io=obj; 
		     io.value_.gc=(GCObject)x; settt_(io, ctb(LUA_TTABLE)); //FIXME:chagned, val_(io)
			 checkliveness(G(L),io); }
		
		public static void setdeadvalue(TValue obj) { settt_(obj, LUA_TDEADKEY); }



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
		//public static void setptvalue2s(lua_State L, TValue obj, Proto x) { setptvalue(L, obj, x); }

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


		/* check whether a number is valid (useful only for NaN trick) */
		public static void luai_checknum(lua_State L, TValue o, luai_checknum_func c)	{ /* empty */ }
		public delegate void luai_checknum_func();


		/*
		** {======================================================
		** types and prototypes
		** =======================================================
		*/


		public struct Value { //FIXME:!!!WARNING!!! Don't make struct to class 
		  public GCObject gc;     /* collectable objects */
		  public object p;        /* light userdata */
		  public int b;           /* booleans */
		  public lua_CFunction f; /* light C functions */
		  public lua_Integer i;   /* integer numbers */
		  public lua_Number n;    /* float numbers */
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
			public lu_byte extra;  /* reserved words for short strings; "has hash" for longs */

			public uint len;  /* number of characters in string */
			public TString hnext;  /* linked list for hash table */
			public uint hash;
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
		public interface TStringRef {
			TString get();
			void set(TString v);		
		}
		public class TStringArrayRef : TStringRef {
			private TString[] arr;
			private int index;
			
			public TStringArrayRef(TString[] arr, int index)
			{
				this.arr = arr;
				this.index = index;
			}
			public TString get()
			{
				return this.arr[this.index];
			}
			public void set(TString v)
			{
				this.arr[this.index] = v;
			}
		}
		public class TStringTsvRef : TStringRef {
			private TString_tsv tsv;
			
			public TStringTsvRef(TString_tsv tsv)
			{
				this.tsv = tsv;
			}
			public TString get()
			{
				return this.tsv.hnext;
			}
			public void set(TString v)
			{
				this.tsv.hnext = v;
			}			
		}

        /* get the actual string (array of bytes) from a TString */
		public static CharPtr getstr(TString ts) { return ts.str; }
		public static CharPtr getstr(TString_tsv ts) { return ((TString)ts).str; } //FIXME:added
		
		/* get the actual string (array of bytes) from a Lua value */
		public static CharPtr svalue(StkId o) { return getstr(rawtsvalue(o)); }


		/*
		** Header for userdata; memory area follows the end of this structure
		*/
		public class Udata_uv : GCObject { //FIXME:added
    		public lu_byte ttuv_;  /* user value's tag */
			public Table metatable;
			public uint len;  /* number of bytes */
			public Value user_;  /* user value */
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
			public Udata_uv uv = new Udata_uv();
			// in the original C code this was allocated alongside the structure memory. it would probably
			// be possible to still do that by allocating memory and pinning it down, but we can do the
			// same thing just as easily by allocating a seperate byte array for it instead.
			public object user_data;
		};

		public static void setuservalue(lua_State L, Udata u, TValue o)
			{ TValue io=(o); Udata iu = (u);
			  iu.uv.user_ = io.value_; iu.uv.ttuv_ = (byte)io.tt_;
			  checkliveness(G(L),io); }


		public static void getuservalue(lua_State L, Udata u, TValue o)
			{ TValue io=(o); Udata iu = (u);
			  io.value_ = iu.uv.user_; io.tt_ = iu.uv.ttuv_;
			  checkliveness(G(L),io); }

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
		  public int[] lineinfo;  /* map from opcodes to source lines (debug information) */
		  public LocVar[] locvars;  /* information about local variables (debug information) */
		  public Upvaldesc[] upvalues;  /* upvalue information */
          public Closure cache;  /* last created closure with this prototype */
		  public TString  source;  /* used for debug information */
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
		//typedef struct UpVal UpVal;
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

		public interface UpValPtrRef {
			UpVal get();
			void set(UpVal val);
		}
		public class OpenupvalRef : UpValPtrRef {
			private lua_State L;
			public OpenupvalRef(lua_State L)
			{
				this.L = L;
			}
			
			public UpVal get()
			{
				return this.L.openupval;
			}
			
			public void set(UpVal val)
			{
				this.L.openupval = val;
			}
		}
		public class UpValUOpenRef : UpValPtrRef {
			private UpVal_u_open u;
			public UpValUOpenRef(UpVal_u_open u)
			{
				this.u = u;
			}
			
			public UpVal get()
			{
				return this.u.next;
			}
			
			public void set(UpVal val)
			{
				this.u.next = val;
			}
		}
				
		
		
		/*
		** Closures
		*/

		public class ClosureHeader : GCObject {
			/*CommonHeader; */public lu_byte nupvalues; public GCObject gclist;} //FIXME:change here, please check ClosureType's getters and setters

		public class ClosureType {

			ClosureHeader header;

			public static implicit operator ClosureHeader(ClosureType ctype) {return ctype.header;}
			public ClosureType(ClosureHeader header) {this.header = header;}

			public lu_byte nupvalues { get { return header.nupvalues; } set { header.nupvalues = value; } }
			public GCObject gclist { get { return header.gclist; } set { header.gclist = value; } }
			//public Table env { get { return header.env; } set { header.env = value; } }//FIXME:removed
			public byte tt { get { return header.tt; } set { header.tt = value; } }
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
			public TKey_nk(Value value, int tt, int next) : base(value, tt)
			{
				this.next = next;
			}
			private int _next;  /* for chaining (offset for next node) */
			public int next {
				set
				{
					//if (this._next != 0 && value == 0)
					//{
					//	Debug.WriteLine("???");
					//}
					this._next = value;
					//_changed_time++;
					//_changed_v[_changed_time] = value;
				}
				get
				{
					return this._next;
				}
			}
			//private int _changed_time = 0;
			//private int[] _changed_v = new int[20];
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
			public TKey(Value value, int tt, int next)
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
				this.i_val = new TValue();this.i_val._parent = this;
				this.i_key = new TKey();
			}

			public Node(Node copy)
			{
				this.values = copy.values;
				this.index = copy.index;
				this.i_val = new TValue(copy.i_val);this.i_val._parent = this;
				this.i_key = new TKey(copy.i_key);
			}
			
			public void Assign(Node copy)
			{
				//FIXME:
				this.values = copy.values;
				this.index = copy.index;
				this.i_val = new TValue(copy.i_val);this.i_val._parent = this;
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
			
			public static Node plus(Node node, int n)
			{
				//if (n == 0)
				//{
				//	Debug.WriteLine("zero");
				//}
				return node[n];
			}
			
			public static void inc(ref Node node, int n)
			{
				node = node[n];
				//return node[-n];
				//FIXME:???array overflow???
			}

			public static void dec(ref Node node, int n)
			{
				node = node[-n];
				//return node[n];
				//FIXME:???array overflow???
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
		
		/* size of buffer for 'luaO_utf8esc' function */
		public const int UTF8BUFFSZ	 = 8;		
	}
}

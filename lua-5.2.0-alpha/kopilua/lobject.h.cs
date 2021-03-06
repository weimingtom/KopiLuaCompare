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
		** Variant tag for light C functions (negative to be considered
		** non collectable by 'iscollectable')
		*/
		public const int LUA_TLCF = (~0x0F | LUA_TFUNCTION); //FIXME:???

		public interface ArrayElement //FIXME:added
		{
			void set_index(int index);
			void set_array(object array);
		}



		/*
		** Common Header for all collectable objects (in macro form, to be
		** included in other objects)
		*/
		public class CommonHeader
		{
			public GCObject next;
			public lu_byte tt;
			public lu_byte marked;
		}


		/*
		** Common header in struct form
		*/
		public class GCheader : CommonHeader {
		};



		/*
		** Union of all Lua values
		*/
		public struct Value {
		  public GCObject gc;     /* collectable objects */
		  public object p;        /* light userdata */
		  public lua_Number n;    /* numbers */
		  public int b;           /* booleans */
		  public lua_CFunction f; /* light C functions */
		};


		/*
		** Tagged Values. This is the basic representation of values in Lua,
		** an actual value plus a tag with its type.
		*/

		//#define TValuefields	Value value; int tt

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
		//#define NILCONSTANT    {NULL}, LUA_TNIL //FIXME:removed


		/*
		** type tag of a TValue
		*/
		public static int ttype(TValue o) { return o.tt_; }


		/*
		** type tag of a TValue with no variants
		*/
		public static int ttypenv(TValue o) { return (ttype(o) & 0x0F); }



		/* Macros to test type */
		public static bool ttisnil(TValue o)	{return (ttype(o) == LUA_TNIL);}
		public static bool ttisnumber(TValue o)	{return (ttype(o) == LUA_TNUMBER);}
		public static bool ttisstring(TValue o)	{return (ttype(o) == LUA_TSTRING);}
		public static bool ttistable(TValue o)	{return (ttype(o) == LUA_TTABLE);}
		public static bool ttisfunction(TValue o)	{return (ttypenv(o) == LUA_TFUNCTION);}
		public static bool ttisclosure(TValue o) { return (ttype(o) == LUA_TFUNCTION);}
		public static bool ttislcf(TValue o) { return (ttype(o) == LUA_TLCF);}
		public static bool ttisboolean(TValue o)	{return (ttype(o) == LUA_TBOOLEAN);}
		public static bool ttisuserdata(TValue o)	{return (ttype(o) == LUA_TUSERDATA);}
		public static bool ttisthread(TValue o)	{return (ttype(o) == LUA_TTHREAD);}
		public static bool ttislightuserdata(TValue o)	{return (ttype(o) == LUA_TLIGHTUSERDATA);}
        public static bool ttisdeadkey(TValue o) { return (ttype(o) == LUA_TDEADKEY);}
		
		/* Macros to access values */
		public static GCObject gcvalue(TValue o) { return (GCObject)check_exp(iscollectable(o), o.value_.gc); }
		public static object pvalue(TValue o) { return (object)check_exp(ttislightuserdata(o), o.value_.p); }
		public static lua_Number nvalue(TValue o) { return (lua_Number)check_exp(ttisnumber(o), o.value_.n); }
		public static TString rawtsvalue(TValue o) { return (TString)check_exp(ttisstring(o), o.value_.gc.ts); }
		public static TString_tsv tsvalue(TValue o) { return rawtsvalue(o).tsv; }
		public static Udata rawuvalue(TValue o) { return (Udata)check_exp(ttisuserdata(o), o.value_.gc.u); }
		public static Udata_uv uvalue(TValue o) { return rawuvalue(o).uv; }
		public static Closure clvalue(TValue o)	{return (Closure)check_exp(ttisclosure(o), o.value_.gc.cl);}
		public static lua_CFunction fvalue(TValue o)	{ return (lua_CFunction)check_exp(ttislcf(o), o.value_.f); }
		public static Table hvalue(TValue o)	{return (Table)check_exp(ttistable(o), o.value_.gc.h);}
		public static int bvalue(TValue o)	{return (int)check_exp(ttisboolean(o), o.value_.b);}
		public static lua_State thvalue(TValue o)	{return (lua_State)check_exp(ttisthread(o), o.value_.gc.th);}

		public static int l_isfalse(TValue o) { return ((ttisnil(o) || (ttisboolean(o) && bvalue(o) == 0))) ? 1 : 0; }

		
		public static bool iscollectable(TValue o)	{ return (ttype(o) >= LUA_TSTRING); }


		/* Macros for internal tests */
		public static bool righttt(TValue obj) { return (ttype(obj) == gcvalue(obj).gch.tt); }
		
		public static void checkconsistency(TValue obj)
		{
			lua_assert(!iscollectable(obj) || righttt(obj));
		}

		public static void checkliveness(global_State g, TValue obj)
		{
			lua_assert(!iscollectable(obj) || (righttt(obj) && !isdead(g, gcvalue(obj))));
		}


		/* Macros to set values */
		public static void setnilvalue(TValue obj) {
			obj.tt_=LUA_TNIL;
		}

		public static void setnvalue(TValue obj, lua_Number x) {
			obj.value_.n = x;
			obj.tt_ = LUA_TNUMBER;
		}

		public static void setfvalue(TValue obj, lua_CFunction x) {
			TValue i_o=obj; i_o.value_.f=x; i_o.tt_=LUA_TLCF; }
		
		public static void changenvalue(TValue o, lua_Number x) { check_exp(o.tt_==LUA_TNUMBER, o.value_.n=x); }
  

		public static void setpvalue( TValue obj, object x) {
			obj.value_.p = x;
			obj.tt_ = LUA_TLIGHTUSERDATA;
		}

		public static void setbvalue(TValue obj, int x) {
			obj.value_.b = x;
			obj.tt_ = LUA_TBOOLEAN;
		}

		public static void setsvalue(lua_State L, TValue obj, GCObject x) {
			obj.value_.gc = x;
			obj.tt_ = LUA_TSTRING;
			checkliveness(G(L), obj);
		}

		public static void setuvalue(lua_State L, TValue obj, GCObject x) {
			obj.value_.gc = x;
			obj.tt_ = LUA_TUSERDATA;
			checkliveness(G(L), obj);
		}

		public static void setthvalue(lua_State L, TValue obj, GCObject x) {
			obj.value_.gc = x;
			obj.tt_ = LUA_TTHREAD;
			checkliveness(G(L), obj);
		}

		public static void setclvalue(lua_State L, TValue obj, Closure x) {
			obj.value_.gc = x;
			obj.tt_ = LUA_TFUNCTION;
			checkliveness(G(L), obj);
		}

		public static void sethvalue(lua_State L, TValue obj, Table x) {
			obj.value_.gc = x;
			obj.tt_ = LUA_TTABLE;
			checkliveness(G(L), obj);
		}

		public static void setptvalue(lua_State L, TValue obj, Proto x) {
			obj.value_.gc = x;
			obj.tt_ = LUA_TPROTO;
			checkliveness(G(L), obj);
		}
		
		public static void setdeadvalue(TValue obj) { obj.tt_=LUA_TDEADKEY; }


		public static void setobj(lua_State L, TValue obj1, TValue obj2) {
			obj1.value_ = obj2.value_;
			obj1.tt_ = obj2.tt_;
			checkliveness(G(L), obj1);
		}


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



		//typedef TValue *StkId;  /* index to stack elements */
		
		/*
		** Header for string value; string bytes follow the end of this structure
		*/
		public class TString_tsv : GCObject //FIXME:added
		{
			public lu_byte reserved;
			public uint hash;
			public uint len;
		};
		public class TString : TString_tsv {
			//public L_Umaxalign dummy;  /* ensures maximum alignment for strings */			
			public TString_tsv tsv { get { return this; } }

			public TString()
			{
				
			}
			//public TString(CharPtr str) { this.str = str; } //FIXME:removed

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
		public class Udata_uv : GCObject //FIXME:added
		{
			public Table metatable;
			public Table env;
			public uint len;
		};

		public class Udata : Udata_uv
		{
			public Udata() { this.uv = this; }

			public new Udata_uv uv;

			//public L_Umaxalign dummy;  /* ensures maximum alignment for `local' udata */

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
			public lu_byte isC;
			public lu_byte nupvalues;
			public GCObject gclist;
		};

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


		public static bool isLfunction(TValue o) { return (ttisclosure(o) && (clvalue(o).c.isC==0)); }

        public static Proto getproto(TValue o) { return (clvalue(o).l.p); }


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

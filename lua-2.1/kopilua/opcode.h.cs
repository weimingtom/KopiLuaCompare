/*
** TeCGraf - PUC-Rio
** $Id: opcode.h,v 3.10 1994/12/20 21:20:36 roberto Exp $
*/
using System;
using System.Diagnostics;

namespace KopiLua
{
	using Cfunction = KopiLua.Lua.lua_CFunction;
	using Word = System.UInt16;
	
	public partial class Lua
	{
		//#ifndef opcode_h
		//#define opcode_h

		//#include "lua.h"
		//#include "types.h"
		//#include "tree.h"

//		#ifndef STACKGAP
//		#define STACKGAP	128
//		#endif 
		public const int STACKGAP = 128;

//		#ifndef real
//		#define real float
//		#endif

		public const int FIELDS_PER_FLUSH = 40;

		public const int MAX_TEMPS = 20;


		public enum OpCode
		{
			PUSHNIL,
			PUSH0, PUSH1, PUSH2,
			PUSHBYTE,
			PUSHWORD,
			PUSHFLOAT,
			PUSHSTRING,
			PUSHFUNCTION,
			PUSHLOCAL0, PUSHLOCAL1, PUSHLOCAL2, PUSHLOCAL3, PUSHLOCAL4,
			PUSHLOCAL5, PUSHLOCAL6, PUSHLOCAL7, PUSHLOCAL8, PUSHLOCAL9,
			PUSHLOCAL,
			PUSHGLOBAL,
			PUSHINDEXED,
			PUSHSELF,
			STORELOCAL0, STORELOCAL1, STORELOCAL2, STORELOCAL3, STORELOCAL4,
			STORELOCAL5, STORELOCAL6, STORELOCAL7, STORELOCAL8, STORELOCAL9,
			STORELOCAL,
			STOREGLOBAL,
			STOREINDEXED0,
			STOREINDEXED,
			STORELIST0,
			STORELIST,
			STORERECORD,
			ADJUST0,
			ADJUST,
			CREATEARRAY,
			EQOP,
			LTOP,
			LEOP,
			GTOP,
			GEOP,
			ADDOP,
			SUBOP,
			MULTOP,
			DIVOP,
			POWOP,
			CONCOP,
			MINUSOP,
			NOTOP,
			ONTJMP,
			ONFJMP,
			JMP,
			UPJMP,
			IFFJMP,
			IFFUPJMP,
			POP,
			CALLFUNC,
			RETCODE0,
			RETCODE,
			SETFUNCTION,
			SETLINE,
			RESET
		}

		public const int MULT_RET = 255;


		//public delegate void Cfunction (); //see using Cfunction = KopiLua.Lua.lua_CFunction;
		public delegate int Input ();

		public class Value //union
		{
			public string __name__ = "";
			
		 	public Cfunction f;
			public float n;
		 	public TaggedString ts;
		 	public BytePtr b;
		 	public Hash a;
		 	public Object u;
			
			public void set(Value v)
		 	{
				this.f = v.f;
				this.n = v.n;
				this.ts = v.ts;
				this.b = v.b;
				this.a = v.a;
				this.u = v.u;
				this.__name__ = v.__name__;
			}
			
		 	public bool isEquals(Value v)
		 	{
		 		if (this.f != null)
		 		{
		 			return this.f == v.f;
		 		}
		 		if (this.n != 0)
		 		{
		 			return this.n == v.n;
		 		}
		 		if (this.ts != null)
		 		{
		 			return this.ts == v.ts; //FIXME:???
		 		}
		 		if (this.b != null)
		 		{
		 			return this.b == v.b;
		 		}
		 		if (this.a != null)
		 		{
		 			return this.a == v.a;
		 		}
		 		if (this.u != null)
		 		{
		 			return this.u == v.u;
		 		}
		 		if (this == v)
		 		{
		 			return true;
		 		}
		 		return false;
		 	}				
		}

		public class Object_
		{
		 	private lua_Type _tag;
		 	public Value value = new Value();
		 	public lua_Type tag
		 	{
		 		get
		 		{
		 			return _tag;
		 		}
		 		set
		 		{
//		 			if (_tag == Type.T_NUMBER && value == Type.T_MARK)
//		 			{
//		 				Console.WriteLine("================");
//		 			}
		 			_tag = value;
		 		}
		 	}
		 	
		 	public void set(Object_ obj)
		 	{
		 		this.tag = obj.tag;
		 		this.value.set(obj.value);
		 	}
		 	
		 	public bool isEquals(Object_ obj)
		 	{
		 		return this.tag == obj.tag && this.value.isEquals(obj.value);
		 	}
		 	
		 	public Object_(lua_Type tag, Cfunction f)
		 	{
		 		this._tag = tag;
		 		this.value.f = f;
		 	}
		 	public Object_()
		 	{
		 		
		 	}
		}
		public class ObjectRef 
		{
			public int index
			{
				get
				{
					return _index;
				}
				set
				{
//					if (value == 11 && this.obj[8].value.__name__.Equals("writeto"))
//					{
//						Console.WriteLine("====================");
//					}
					_index = value;
				}
			}
			
			public Object_[] obj;
			private int _index;
			public ObjectRef(ObjectRef _obj)
			{
				this.obj = _obj.obj;
				this.index = _obj.index;
			}
			public ObjectRef(ObjectRef _obj, int _index)
			{
				this.obj = _obj.obj;
				this.index = _obj.index + _index;
			}
			public ObjectRef(Object_[] _obj, int _index)
			{
				this.obj = _obj;
				this.index = _index;
			}
			
			public void inc() 
			{
				index++;
			}

			public void dec() 
			{
				index--;
			}
			
			public Object_ get()
			{
				return obj[index];
			}

			public Object_ get(int offset)
			{
				return obj[index + offset];
			}

			public ObjectRef getRef(int offset)
			{
				return new ObjectRef(obj, index + offset);
			}
			
			public void set(int offset, Object_ o)
			{
				obj[index + offset].set(o);
			}
			
			public void add(int offset) 
			{
				index += offset;
			}
			
			public bool notEqualsTo(Object_ o)
			{
				return obj[index] != o;
			}
			
			public bool isLessThan(Object_ o)
			{
				int idx = -1;
				bool found = false;
				for (int i = 0; i < obj.Length; ++i)
				{
					if (o == obj[i])
					{
						idx = i;
						found = true;
						break;
					}
				}
				if (found == false)
				{
					throw new Exception("objs not same");
				}
				return this.index < idx;				
			}
			
			public bool isLargerEquals(Object_[] objs)
			{
				if (this.obj != objs)
				{
					throw new Exception("objs not same");
				}
				return this.index >= 0;				
			}
			
			public bool isLessEquals(ObjectRef oref)
			{
				if (this.obj != oref.obj)
				{
					throw new Exception("objs not same");
				}
				return this.index <= oref.index;					
			}
			public bool isLessThan(ObjectRef oref)
			{
				if (this.obj != oref.obj)
				{
					throw new Exception("objs not same");
				}
				return this.index < oref.index;					
			}
			
			public void setRef(Object_ o)
			{
				bool found = false;
				for (int i = 0; i < obj.Length; ++i)
				{
					if (o == obj[i])
					{
						this.index = i;
						found = true;
						break;
					}
				}
				if (found == false)
				{
					throw new Exception("objs not same");
				}
			}
			
			public static int minus(ObjectRef this_, ObjectRef oref)
			{
				if (this_ == null && oref == null) 
				{
					return 0;
				}
				if (this_.obj == oref.obj)
				{
					return this_.index - oref.index;
				}
				throw new Exception("objs not same");
			}

			public static int minus(ObjectRef this_, Object_[] oarr)
			{
				if (this_ == null && oarr == null) 
				{
					return 0;
				}
				if (this_.obj == oarr)
				{
					return this_.index - 0;
				}
				throw new Exception("objs not same");
			}
		}
		
		public class Symbol
		{
		 	public Object_ object_ = new Object_();

		 	public Symbol() 
		 	{
		 		
		 	}
		 	
		 	public Symbol(lua_Type tag, Cfunction f) 
		 	{
		 		this.object_.tag = tag;
		 		this.object_.value.f = f;
		 	}
		}
		public class SymbolPtr
		{
			public Symbol[] chars;
			public int index;
			
			public Symbol this[int offset]
			{
				get { return chars[index + offset]; }
				set { chars[index + offset] = value; }
			}
			public Symbol this[uint offset]
			{
				get { return chars[index + offset]; }
				set { chars[index + offset] = value; }
			}
			public Symbol this[long offset]
			{
				get { return chars[index + (int)offset]; }
				set { chars[index + (int)offset] = value; }
			}

//			public static implicit operator CharPtr(string str) { return new CharPtr(str); }
			public static implicit operator SymbolPtr(Symbol[] chars) { return new SymbolPtr(chars); }

			public SymbolPtr()
			{
				this.chars = null;
				this.index = 0;
			}

//			public CharPtr(string str)
//			{
//				this.chars = (str + '\0').ToCharArray();
//				this.index = 0;
//			}

			public SymbolPtr(SymbolPtr ptr)
			{
				this.chars = ptr.chars;
				this.index = ptr.index;
			}

			public SymbolPtr(SymbolPtr ptr, int index)
			{
				this.chars = ptr.chars;
				this.index = index;
			}

			public SymbolPtr(Symbol[] chars)
			{
				this.chars = chars;
				this.index = 0;
			}

			public SymbolPtr(Symbol[] chars, int index)
			{
				this.chars = chars;
				this.index = index;
			}

			public static SymbolPtr operator +(SymbolPtr ptr, int offset) {return new SymbolPtr(ptr.chars, ptr.index+offset);}
			public static SymbolPtr operator -(SymbolPtr ptr, int offset) {return new SymbolPtr(ptr.chars, ptr.index-offset);}
			public static SymbolPtr operator +(SymbolPtr ptr, uint offset) { return new SymbolPtr(ptr.chars, ptr.index + (int)offset); }
			public static SymbolPtr operator -(SymbolPtr ptr, uint offset) { return new SymbolPtr(ptr.chars, ptr.index - (int)offset); }

			public void inc() { this.index++; }
			public void dec() { this.index--; }
			public SymbolPtr next() { return new SymbolPtr(this.chars, this.index + 1); }
			public SymbolPtr prev() { return new SymbolPtr(this.chars, this.index - 1); }
			public SymbolPtr add(int ofs) { return new SymbolPtr(this.chars, this.index + ofs); }
			public SymbolPtr sub(int ofs) { return new SymbolPtr(this.chars, this.index - ofs); }
			
//			public static bool operator ==(SymbolPtr ptr, int ch) { return ptr[0] == ch; }
//			public static bool operator ==(int ch, SymbolPtr ptr) { return ptr[0] == ch; }
//			public static bool operator !=(SymbolPtr ptr, int ch) { return ptr[0] != ch; }
//			public static bool operator !=(int ch, SymbolPtr ptr) { return ptr[0] != ch; }

//			public static CharPtr operator +(BytePtr ptr1, BytePtr ptr2)
//			{
//				string result = "";
//				for (int i = 0; ptr1[i] != '\0'; i++)
//					result += ptr1[i];
//				for (int i = 0; ptr2[i] != '\0'; i++)
//					result += ptr2[i];
//				return new CharPtr(result);
//			}
			public static int operator -(SymbolPtr ptr1, SymbolPtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index - ptr2.index; }
			public static bool operator <(SymbolPtr ptr1, SymbolPtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index < ptr2.index; }
			public static bool operator <=(SymbolPtr ptr1, SymbolPtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index <= ptr2.index; }
			public static bool operator >(SymbolPtr ptr1, SymbolPtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index > ptr2.index; }
			public static bool operator >=(SymbolPtr ptr1, SymbolPtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index >= ptr2.index; }
			public static bool operator ==(SymbolPtr ptr1, SymbolPtr ptr2) {
				object o1 = ptr1 as SymbolPtr;
				object o2 = ptr2 as SymbolPtr;
				if ((o1 == null) && (o2 == null)) return true;
				if (o1 == null) return false;
				if (o2 == null) return false;
				return (ptr1.chars == ptr2.chars) && (ptr1.index == ptr2.index); }
			public static bool operator !=(SymbolPtr ptr1, SymbolPtr ptr2) {return !(ptr1 == ptr2); }

			public override bool Equals(object o)
			{
				return this == (o as SymbolPtr);
			}

			public override int GetHashCode()
			{
				return 0;
			}
//			public override string ToString()
//			{
//				string result = "";
//				for (int i = index; (i<chars.Length) && (chars[i] != '\0'); i++)
//					result += chars[i];
//				return result;
//			}
		}
		
		
		/* Macros to access structure members */
		//#define tag(o) ((o)->tag)
		public static lua_Type tag(Object_ o) { return o.tag; }
		public static void tag(Object_ o, lua_Type t) { 
//			if (t == Type.T_NUMBER)
//			{
//				Console.WriteLine("==============");
//			}
			o.tag = t;
		}
		//#define nvalue(o) ((o)->value.n)
		public static float nvalue(Object_ o) { return o.value.n; }		
		public static void nvalue(Object_ o, float n) { o.value.n = n; }	
		//#define svalue(o)	((o)->value.ts->str)
		public static CharPtr svalue(Object_ o) { return o.value.ts.str != null ? new CharPtr(o.value.ts.str) : null; }
		public static void svalue(Object_ o, CharPtr ptr) { o.value.ts.str = (ptr != null ? new CharPtr(ptr) : null); }
		//#define tsvalue(o)	((o)->value.ts)
		public static TaggedString tsvalue(Object_ o) { return o.value.ts != null ? o.value.ts : null; }
		public static void tsvalue(Object_ o, TaggedString ptr) { o.value.ts = (ptr != null ? ptr : null); }				
		//#define bvalue(o) ((o)->value.b)
		public static BytePtr bvalue(Object_ o) { return o.value.b != null ? new BytePtr(o.value.b) : null; }
		public static void bvalue(Object_ o, BytePtr b) { o.value.b = (b != null ? new BytePtr(b) : null); }
		//#define avalue(o) ((o)->value.a)
		public static Hash avalue(Object_ o) { return o.value.a; }		
		public static void avalue(Object_ o, Hash a) { o.value.a = a; }	
		//#define fvalue(o) ((o)->value.f)
		public static Cfunction fvalue(Object_ o) { return o.value.f; }		
		public static void fvalue(Object_ o, Cfunction f, string name) { o.value.f = f; o.value.__name__ = name;}	
		//#define uvalue(o) ((o)->value.u)
		public static object uvalue(Object_ o) { return o.value.u; }		
		public static void uvalue(Object_ o, object u) { o.value.u = u; }
		
		/* Macros to access symbol table */
		//#define s_object(i) (lua_table[i].object)
		public static Object_ s_object(int i) { return lua_table[i].object_; }
		public static void s_object(int i, Object_ o) { lua_table[i].object_.set(o); }
		//#define s_tag(i) (tag(&s_object(i)))
		public static lua_Type s_tag(int i) { return tag(s_object(i)); }
		public static void s_tag(int i, lua_Type t) { tag(s_object(i), t); }
		//#define s_nvalue(i) (nvalue(&s_object(i)))
		public static float s_nvalue(int i) {return nvalue(s_object(i));}
		//#define s_svalue(i) (svalue(&s_object(i)))
		public static CharPtr s_svalue(int i) {return svalue(s_object(i));}
		//#define s_bvalue(i) (bvalue(&s_object(i)))
		public static BytePtr s_bvalue(int i) {return bvalue(s_object(i));}
		public static void s_bvalue(int i, BytePtr ptr) {bvalue(s_object(i), ptr);}
		//#define s_avalue(i) (avalue(&s_object(i)))
		public static Hash s_avalue(int i) {return avalue(s_object(i));}
		//#define s_fvalue(i) (fvalue(&s_object(i)))
		public static lua_CFunction s_fvalue(int i) {return fvalue(s_object(i));}
		public static void s_fvalue(int i, lua_CFunction f, string name) {fvalue(s_object(i), f, name);}
		//#define s_uvalue(i) (uvalue(&s_object(i)))
		public static object s_uvalue(int i) {return uvalue(s_object(i));}

		public class CodeWord
		{
			public class CodeWord_m_struct 
			{
				public Byte c1
				{
					get { return c1_; }
					set
					{
						c1_ = value;
						this.parent_.update();
					}
				}
				public Byte c2
				{
					get { return c2_; }
					set
					{
						c2_ = value;
						this.parent_.update();						
					}
				}	
				
				private Byte c1_;
				private Byte c2_;
				private CodeWord parent_;
				public CodeWord_m_struct(CodeWord parent)
				{
					this.parent_ = parent;
				}
				public void update()
				{
					byte[] bytes = BitConverter.GetBytes(parent_.w);
					if (bytes.Length != 2)
					{
						throw new Exception("CodeWord_m_struct convert fail");
					}
					this.c1_ = bytes[0];
		 			this.c2_ = bytes[1];
				}
			}
		 	public CodeWord_m_struct m;
		 	public Word w
		 	{
		 		get { return w_;}
		 		set 
		 		{ 
		 			this.w_ = value; 
		 			this.m.update();
		 		}
		 	}
		 	
		 	private Word w_;
		 	public CodeWord() 
		 	{
		 		m = new CodeWord_m_struct(this);
		 	}
		 	public void update()
			{
		 		byte[] bytes = new byte[] { this.m.c1, this.m.c2 };
				this.w_ = BitConverter.ToUInt16(bytes, 0);
			}
		}
		public static void get_word(CodeWord code, BytePtr pc)    {code.m.c1 = (Byte)pc[0]; pc.inc(); code.m.c2 = (Byte)pc[0]; pc.inc();}
		
		public class CodeFloat
		{
		 	public class CodeFloat_m_struct 
		 	{
		 		public Byte c1
				{
					get { return c1_; }
					set
					{
						c1_ = value;
						this.parent_.update();
					}
				}		 			
		 		public Byte c2
				{
					get { return c2_; }
					set
					{
						c2_ = value;
						this.parent_.update();
					}
				}		 		
		 		public Byte c3
		 		{
					get { return c3_; }
					set
					{
						c3_ = value;
						this.parent_.update();
					}
				}
		 		public Byte c4
		 		{
					get { return c4_; }
					set
					{
						c4_ = value;
						this.parent_.update();
					}
				}
		 		
				private Byte c1_;
				private Byte c2_;
				private Byte c3_;
				private Byte c4_;
				private CodeFloat parent_;
				public CodeFloat_m_struct(CodeFloat parent)
				{
					this.parent_ = parent;
				}
				public void update()
				{
					byte[] bytes = BitConverter.GetBytes(parent_.f);
					if (bytes.Length != 4)
					{
						throw new Exception("CodeFloat_m_struct convert fail");
					}
					this.c1_ = bytes[0];
		 			this.c2_ = bytes[1];
		 			this.c3_ = bytes[2];
		 			this.c4_ = bytes[3];
				}
		 	}
		 	
		 	
		 	public CodeFloat_m_struct m;
		 	public float f
		 	{
		 		get { return f_;}
		 		set 
		 		{ 
		 			this.f_ = value; 
		 			this.m.update();
		 		}
		 	}
		
			private float f_;
		 	public CodeFloat() 
		 	{
		 		m = new CodeFloat_m_struct(this);
		 	}
		 	public void update()
			{
		 		byte[] bytes = new byte[] { this.m.c1, this.m.c2, this.m.c3, this.m.c4 };
		 		this.f_ = BitConverter.ToSingle(bytes, 0);
			}
		}
		public static void get_float(CodeFloat code, BytePtr pc)   {code.m.c1 = (Byte)pc[0]; pc.inc(); code.m.c2 = (Byte)pc[0]; pc.inc();
			code.m.c3 = (Byte)pc[0]; pc.inc(); code.m.c4 = (Byte)pc[0]; pc.inc();}

		public class CodeCode
		{
		 	public class CodeCode_m_struct 
		 	{
		 		public Byte c1
				{
					get { return c1_; }
					set
					{
						c1_ = value;
						this.parent_.update();
					}
				}		 			
		 		public Byte c2
				{
					get { return c2_; }
					set
					{
						c2_ = value;
						this.parent_.update();
					}
				}		 		
		 		public Byte c3
		 		{
					get { return c3_; }
					set
					{
						c3_ = value;
						this.parent_.update();
					}
				}
		 		public Byte c4
		 		{
					get { return c4_; }
					set
					{
						c4_ = value;
						this.parent_.update();
					}
				}
		 		
				private Byte c1_;
				private Byte c2_;
				private Byte c3_;
				private Byte c4_;
				private CodeCode parent_;
				public CodeCode_m_struct(CodeCode parent)
				{
					this.parent_ = parent;
				}
				public void update()
				{
					byte[] bytes = parent_.b;
					if (bytes.Length != 4)
					{
						throw new Exception("CodeFloat_m_struct convert fail");
					}
					this.c1_ = bytes[0];
		 			this.c2_ = bytes[1];
		 			this.c3_ = bytes[2];
		 			this.c4_ = bytes[3];
				}
		 	}
		 	
		 	
		 	public CodeCode_m_struct m;
		 	public byte[] b
		 	{
		 		get { return b_;}
		 		set 
		 		{ 
		 			for (int i = 0; i < b_.Length; ++i)
		 			{
		 				this.b_[i] = value[i];
		 			}
		 			this.m.update();
		 		}
		 	}
		
		 	private byte[] b_ = new byte[4];
		 	public CodeCode() 
		 	{
		 		m = new CodeCode_m_struct(this);
		 	}
		 	public void update()
			{
		 		byte[] bytes = new byte[] { this.m.c1, this.m.c2, this.m.c3, this.m.c4 };
		 		this.b_[0] = bytes[0];
		 		this.b_[1] = bytes[1];
		 		this.b_[2] = bytes[2];
		 		this.b_[3] = bytes[3];		 		
			}
		}
		public static void get_code(CodeCode code, BytePtr pc)   {code.m.c1 = (Byte)pc[0]; pc.inc(); code.m.c2 = (Byte)pc[0]; pc.inc();
			code.m.c3 = (Byte)pc[0]; pc.inc(); code.m.c4 = (Byte)pc[0]; pc.inc();}


		/* Exported functions */
//		char   *lua_strdup (char *l);

//		void    lua_setinput   (Input fn);	/* from "lex.c" module */
//		char   *lua_lasttext   (void);		/* from "lex.c" module */
//		int     yylex (void);		        /* from "lex.c" module */
//		void    lua_parse      (Byte **code);	/* from "lua.stx" module */
//		void    lua_travstack (void (*fn)(Object *));
//		Object *luaI_Address (lua_Object o);
//		void	luaI_pushobject (Object *o);
		//void    luaI_gcFB       (Object *o);

		//#endif
	}
}

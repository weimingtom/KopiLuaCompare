/*
** TeCGraf - PUC-Rio
** $Id: opcode.h,v 2.1 1994/04/20 22:07:57 celes Exp $
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

//		#ifndef STACKGAP
//		#define STACKGAP	128
//		#endif 
		public const int STACKGAP = 128;

//		#ifndef real
//		#define real float
//		#endif

		public const int FIELDS_PER_FLUSH = 40;

//		typedef unsigned char  Byte;
//
//		typedef unsigned short Word;

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

		public enum OpCode
		{
			PUSHNIL,
			PUSH0, PUSH1, PUSH2,
			PUSHBYTE,
			PUSHWORD,
			PUSHFLOAT,
			PUSHSTRING,
			PUSHLOCAL0, PUSHLOCAL1, PUSHLOCAL2, PUSHLOCAL3, PUSHLOCAL4,
			PUSHLOCAL5, PUSHLOCAL6, PUSHLOCAL7, PUSHLOCAL8, PUSHLOCAL9,
			PUSHLOCAL,
			PUSHGLOBAL,
			PUSHINDEXED,
			PUSHMARK,
			PUSHOBJECT,
			STORELOCAL0, STORELOCAL1, STORELOCAL2, STORELOCAL3, STORELOCAL4,
			STORELOCAL5, STORELOCAL6, STORELOCAL7, STORELOCAL8, STORELOCAL9,
			STORELOCAL,
			STOREGLOBAL,
			STOREINDEXED0,
			STOREINDEXED,
		 	STORELIST0,
		 	STORELIST,
		 	STORERECORD,
			ADJUST,
			CREATEARRAY,
			EQOP,
			LTOP,
			LEOP,
			ADDOP,
			SUBOP,
			MULTOP,
			DIVOP,
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
			RETCODE,
			HALT,
			SETFUNCTION,
			SETLINE,
			RESET
		}

		public enum Type
		{
			T_MARK,
		 	T_NIL,
		 	T_NUMBER,
		 	T_STRING,
		 	T_ARRAY,
		 	T_FUNCTION,
		 	T_CFUNCTION,
		 	T_USERDATA
		}

		//public delegate void Cfunction (); //see using Cfunction = KopiLua.Lua.lua_CFunction;
		public delegate int Input ();

		public class Value //union
		{
			public string __name__ = "";
			
		 	public Cfunction f;
			public float n;
		 	public CharPtr s;
		 	public BytePtr b;
		 	public Hash a;
		 	public Object u;
			
			public void set(Value v)
		 	{
				this.f = v.f;
				this.n = v.n;
				this.s = v.s;
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
		 		if (this.s != null)
		 		{
		 			return this.s == v.s;
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
		 	private Type _tag;
		 	public Value value = new Value();
		 	public Type tag
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
		}

		public class Symbol
		{
		 	private CharPtr _name = null;
		 	public Object_ object_ = new Object_();
		 	
		 	public CharPtr name 
		 	{
		 		get
		 		{
		 			return _name;
		 		}
		 		set
		 		{
//		 			if (value.ToString().Equals("a"))
//	 			    {
//	 			    	Console.WriteLine("=================");
//	 			    }
//		 			if (value.ToString().Equals("i"))
//	 			    {
//	 			    	Console.WriteLine("=================");
//	 			    }
//		 			if (value.ToString().Equals("="))
//	 			    {
//	 			    	Console.WriteLine("=================");
//	 			    }		 			
//		 			if (_name != null && _name.ToString().Equals("a"))
//		 			{
//		 				Console.WriteLine("=================");
//		 				if (value != null && !value.ToString().Equals("a"))
//		 				{
//		 					Console.WriteLine("=================");
//		 				}
//		 			}
		 			_name = value;
		 		}
		 	}
		 	
		 	public Symbol() 
		 	{
		 		
		 	}
		 	
		 	public Symbol(CharPtr name, Type tag, Cfunction f) 
		 	{
		 		this.name = name;
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
		public static Type tag(Object_ o) { return o.tag; }
		public static void tag(Object_ o, Type t) { 
//			if (t == Type.T_NUMBER)
//			{
//				Console.WriteLine("==============");
//			}
			o.tag = t;
		}
		//#define nvalue(o) ((o)->value.n)
		public static float nvalue(Object_ o) { return o.value.n; }		
		public static void nvalue(Object_ o, float n) { o.value.n = n; }	
		//#define svalue(o) ((o)->value.s)
		public static CharPtr svalue(Object_ o) { return o.value.s != null ? new CharPtr(o.value.s) : null; }
		public static void svalue(Object_ o, CharPtr ptr) { o.value.s = (ptr != null ? new CharPtr(ptr) : null); }
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
		//#define s_name(i) (lua_table[i].name)
		public static CharPtr s_name(int i) {return lua_table[i].name; }
		public static void s_name(int i, CharPtr ptr) {lua_table[i].name = ptr; }
		//#define s_object(i) (lua_table[i].object)
		public static Object_ s_object(int i) { return lua_table[i].object_; }
		public static void s_object(int i, Object_ o) { lua_table[i].object_.set(o); }
		//#define s_tag(i) (tag(&s_object(i)))
		public static Type s_tag(int i) { return tag(s_object(i)); }
		public static void s_tag(int i, Type t) { tag(s_object(i), t); }
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
		//#define s_uvalue(i) (uvalue(&s_object(i)))
		public static object s_uvalue(int i) {return uvalue(s_object(i));}

		public static void get_word(CodeWord code, BytePtr pc)    {code.m.c1 = (Byte)pc[0]; pc.inc(); code.m.c2 = (Byte)pc[0]; pc.inc();}
		public static void get_float(CodeFloat code, BytePtr pc)   {code.m.c1 = (Byte)pc[0]; pc.inc(); code.m.c2 = (Byte)pc[0]; pc.inc();
			code.m.c3 = (Byte)pc[0]; pc.inc(); code.m.c4 = (Byte)pc[0]; pc.inc();}
 


		/* Exported functions */
//		int     lua_execute   (Byte *pc);
//		void    lua_markstack (void);
//		char   *lua_strdup (char *l);
//
//		void    lua_setinput   (Input fn);	/* from "lex.c" module */
//		char   *lua_lasttext   (void);		/* from "lex.c" module */
//		int     lua_parse      (void); 		/* from "lua.stx" module */
//		void    lua_type       (void);
//		void 	lua_obj2number (void);
//		void 	lua_print      (void);
//		void 	lua_internaldofile (void);
//		void 	lua_internaldostring (void);
//		void    lua_travstack (void (*fn)(Object *));

		//#endif
	}
}

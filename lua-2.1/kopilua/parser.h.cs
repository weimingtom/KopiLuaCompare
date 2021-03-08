using System;
using System.Diagnostics;

namespace KopiLua
{
	using lua_Object = System.UInt32;
	using Word = System.UInt16;
	using real = System.Single;	
	using Bool = System.Int32;
	using Long = System.Int32;
	
	public partial class Lua
	{
		public class YYSTYPE //FIXME:union
		{
			 private int   vInt_;
			 private float vFloat_;
			 private CharPtr pChar_;
			 private Word  vWord_;
			 private long  vLong_;
			 private BytePtr pByte_;
			 public TreeNode pNode_;

		 	public int vInt {get{return vInt_;}set{vInt_ = value;}}
		 	public float vFloat {get{return vFloat_;}set{vFloat_ = value;}}
		 	public CharPtr pChar {get{return pChar_;}set{pChar_ = value;}}
			public Word vWord {
		 		get
		 		{
		 			return vWord_;
		 		}
		 		set
		 		{
//		 			if (value == 33)
//		 			{
//		 				Console.WriteLine("==================");
//		 			}
		 			vWord_ = value;
		 		}
		 	}
		 	public long vLong {get{return vLong_;}set{vLong_ = value;}}
		 	public BytePtr pByte {get{return pByte_;}set{pByte_ = value;}}
		 	public TreeNode pNode {get{return pNode_;}set{pNode_ = value;}}
		 	
		 	public void set(YYSTYPE s) 
		 	{
		 		this.vInt = s.vInt;
		 		this.vFloat = s.vFloat;
		 		this.vWord = s.vWord;
		 		this.vLong = s.vLong;
		 		if (s.pByte != null)
		 		{
		 			this.pByte = new BytePtr(s.pByte);
		 		}
		 		else
		 		{
		 			this.pByte = null;
		 		}
		 		if (s.pChar != null)
		 		{
		 			this.pChar = new CharPtr(s.pChar);
		 		}
		 		else
		 		{
		 			this.pChar = null;
		 		}
		 		if (s.pNode != null)
		 		{
		 			this.pNode = s.pNode; //???
		 		}
		 		else
		 		{
		 			this.pNode = null;
		 		}
		 	}		 	
		}
		public class YYSTYPEPtr
		{
			public YYSTYPE[] chars;
			public int index;
			
			public YYSTYPE this[int offset]
			{
				get { return chars[index + offset]; }
				set { chars[index + offset] = value; }
			}
			public YYSTYPE this[uint offset]
			{
				get { return chars[index + offset]; }
				set { chars[index + offset] = value; }
			}
			public YYSTYPE this[long offset]
			{
				get { return chars[index + (int)offset]; }
				set { chars[index + (int)offset] = value; }
			}

//			public static implicit operator CharPtr(string str) { return new CharPtr(str); }
			public static implicit operator YYSTYPEPtr(YYSTYPE[] chars) { return new YYSTYPEPtr(chars); }

			public YYSTYPEPtr()
			{
				this.chars = null;
				this.index = 0;
			}

//			public CharPtr(string str)
//			{
//				this.chars = (str + '\0').ToCharArray();
//				this.index = 0;
//			}

			public YYSTYPEPtr(YYSTYPEPtr ptr)
			{
				this.chars = ptr.chars;
				this.index = ptr.index;
			}

			public YYSTYPEPtr(YYSTYPEPtr ptr, int index)
			{
				this.chars = ptr.chars;
				this.index = index;
			}

			public YYSTYPEPtr(YYSTYPE[] chars)
			{
				this.chars = chars;
				this.index = 0;
			}

			public YYSTYPEPtr(YYSTYPE[] chars, int index)
			{
				this.chars = chars;
				this.index = index;
			}

			public static YYSTYPEPtr operator +(YYSTYPEPtr ptr, int offset) {return new YYSTYPEPtr(ptr.chars, ptr.index+offset);}
			public static YYSTYPEPtr operator -(YYSTYPEPtr ptr, int offset) {return new YYSTYPEPtr(ptr.chars, ptr.index-offset);}
			public static YYSTYPEPtr operator +(YYSTYPEPtr ptr, uint offset) { return new YYSTYPEPtr(ptr.chars, ptr.index + (int)offset); }
			public static YYSTYPEPtr operator -(YYSTYPEPtr ptr, uint offset) { return new YYSTYPEPtr(ptr.chars, ptr.index - (int)offset); }

			public void inc() { this.index++; }
			public void dec() { this.index--; }
			public YYSTYPEPtr next() { return new YYSTYPEPtr(this.chars, this.index + 1); }
			public YYSTYPEPtr prev() { return new YYSTYPEPtr(this.chars, this.index - 1); }
			public YYSTYPEPtr add(int ofs) { return new YYSTYPEPtr(this.chars, this.index + ofs); }
			public YYSTYPEPtr sub(int ofs) { return new YYSTYPEPtr(this.chars, this.index - ofs); }
			
			public static bool operator ==(YYSTYPEPtr ptr, YYSTYPE ch) { return ptr[0] == ch; }
			public static bool operator ==(YYSTYPE ch, YYSTYPEPtr ptr) { return ptr[0] == ch; }
			public static bool operator !=(YYSTYPEPtr ptr, YYSTYPE ch) { return ptr[0] != ch; }
			public static bool operator !=(YYSTYPE ch, YYSTYPEPtr ptr) { return ptr[0] != ch; }

//			public static CharPtr operator +(BytePtr ptr1, BytePtr ptr2)
//			{
//				string result = "";
//				for (int i = 0; ptr1[i] != '\0'; i++)
//					result += ptr1[i];
//				for (int i = 0; ptr2[i] != '\0'; i++)
//					result += ptr2[i];
//				return new CharPtr(result);
//			}
			public static int operator -(YYSTYPEPtr ptr1, YYSTYPEPtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index - ptr2.index; }
			public static bool operator <(YYSTYPEPtr ptr1, YYSTYPEPtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index < ptr2.index; }
			public static bool operator <=(YYSTYPEPtr ptr1, YYSTYPEPtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index <= ptr2.index; }
			public static bool operator >(YYSTYPEPtr ptr1, YYSTYPEPtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index > ptr2.index; }
			public static bool operator >=(YYSTYPEPtr ptr1, YYSTYPEPtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index >= ptr2.index; }
			public static bool operator ==(YYSTYPEPtr ptr1, YYSTYPEPtr ptr2) {
				object o1 = ptr1 as YYSTYPEPtr;
				object o2 = ptr2 as YYSTYPEPtr;
				if ((o1 == null) && (o2 == null)) return true;
				if (o1 == null) return false;
				if (o2 == null) return false;
				return (ptr1.chars == ptr2.chars) && (ptr1.index == ptr2.index); }
			public static bool operator !=(YYSTYPEPtr ptr1, YYSTYPEPtr ptr2) {return !(ptr1 == ptr2); }

			public override bool Equals(object o)
			{
				return this == (o as YYSTYPEPtr);
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
		
		//extern YYSTYPE yylval;
		public const int WRONGTOKEN = 257;
		public const int NIL = 258;
		public const int IF = 259;
		public const int THEN = 260;
		public const int ELSE = 261;
		public const int ELSEIF = 262;
		public const int WHILE = 263;
		public const int DO = 264;
		public const int REPEAT = 265;
		public const int UNTIL = 266;
		public const int END = 267;
		public const int RETURN = 268;
		public const int LOCAL = 269;
		public const int FUNCTION = 270;
		public const int NUMBER = 271;
		public const int STRING = 272;
		public const int NAME = 273;
		public const int DEBUG = 274;
		public const int AND = 275;
		public const int OR = 276;
		public const int EQ = 277;
		public const int NE = 278;
		public const int LE = 279;
		public const int GE = 280;
		public const int CONC = 281;
		public const int UNARY = 282;
		public const int NOT = 283;
	}
}
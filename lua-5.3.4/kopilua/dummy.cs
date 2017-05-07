using System;
using System.Diagnostics;

namespace KopiLua
{
	using StkId = Lua.lua_TValue;
	using lua_Number = System.Double;
		
	public partial class Lua
	{
		public Lua()
		{
			
		}
		
		public static void api_check(lua_State L, bool b, string str)
		{
			
		}
		
		public static int luaK_codeABx(FuncState fs, OpCode o, int A, int sBx)
		{
			return 0;
		}
		
		public static void luaK_setreturns(FuncState fs, expdesc e, int arg3)
		{
			
		}
		
		public static int luaK_jump(FuncState fs)
		{
			return 0;
		}
		
		public static void luaK_patchlist(FuncState fs, int arg2, int t)
		{
			
		}
		
		public static void condmovestack(lua_State L, int pre, int pos)
		{
			
		}
		
		public static void luaD_growstack(lua_State L, int n)
		{
			
		}
		
		public static int GetUnmanagedSize(Type type)
		{
			return 0;
		}
		
		public static global_State G(lua_State L)
		{
			return null;
		}
		
		public static void luaC_step(lua_State L)
		{
			
		}
		
		public static void condchangemem(lua_State L)
		{
			
		}
		
		public static bool iscollectable(TValue t)
		{
			return false;
		}
		
		public static GCObject gcvalue(TValue t)
		{
			return null;
		}
		
		public static GCObject obj2gco(GCObject obj)
		{
			return null;
		}
		
		public static void luaC_barrier_(lua_State L, GCObject p, GCObject v)
		{
			
		}
		
		public static void luaC_barrierback_(lua_State L, GCObject p)
		{
			
		}
		
		public static void luaC_upvalbarrier_(lua_State L, UpVal uv)
		{
			
		}
		
		public static object luaM_realloc_(lua_State L, Type t)
		{
			return null;
		}

		public static object luaM_realloc_<T>(lua_State L)
		{
			return null;
		}

		public static object luaM_realloc_<T>(lua_State L, T obj)
		{
			return null;
		}
		
		public static object luaM_realloc_<T>(lua_State L, T[] old_block, int new_size)
		{
			return null;
		}
		
		public static T[] luaM_growaux_<T>(lua_State L, ref T[] block, ref int size,
		                                   int limit, CharPtr errormsg) 
		{
			return null;
		}
		
		public static int sizenode(Table t)
		{
			return 0;
		}
		
		public static int LUA_MULTRET = 0;
		public static int MAXARG_sBx = 0;
		
		public class lua_TValue
		{
			public static implicit operator int(lua_TValue value)
			{
				return 0;
			}
		}
		
		public static int cast_uchar(char c) { return (int)c; } //FIXME: return char???
		
		public static int luaZ_fill(ZIO z) 
		{
			return 0;
		}
		
		public static lua_Number cast_num(double i)
		{
			return (lua_Number)Convert.ToSingle(i);
		}
				
		public const string LUA_VERSION_MAJOR = "5";
		public const string LUA_VERSION_MINOR = "3";
		public const int LUA_VERSION_NUM = 503;
		public const string LUA_VERSION_RELEASE = "4";
		
		public static string[] luaT_typenames_ = new string[] {};
		
		public static TValue luaT_gettm (Table events, TMS event_, TString ename) 
		{
			return null;
		}
	}
	
	public class lua_State 
	{
		public int top;
		public lua_State twups;

		public class ci_cls 
		{
			public int top;
			public int func;
		}
		
		public ci_cls ci;
		public int basehookcount = 0;
		public int hookcount = 0;
		public int stack_last = 0;
		public StkId[] stack;
	}
	
	public class Test
	{
		static void Main(string[] args)
		{
			Console.WriteLine("hello, world!");
			Console.ReadLine();
		}
	}
	
	public class InstructionPtr
	{
		public InstructionPtr(int arg1, int arg2){}
		public int pc;
		public int codes;
	}
	
	public class FuncState
	{
		public class f_cls {public int code;};
		public f_cls f;
	}
	
	public class expdesc
	{
		public class u_cls {public int info;};
		public u_cls u;
	}
	
	public class OpCode
	{
		
	}
	
	public class Proto
	{
		public int code;
		public int[] lineinfo;
	}
	
	public class TValue
	{
		public int marked;
	}
	
	public class lu_mem
	{
		
	}
	
	public class CClosure
	{
		
	}
	
	public class LClosure
	{
		
	}
	
	public class TString
	{
		
	}
	
	public class global_State
	{
		public int gcstate;
		public int currentwhite;
		public int GCdebt;
		
		public TString[] tmname = new TString[0];
	}
	
	public class GCObject
	{
		public int marked;
	}
	
	public class ZIO
	{
		
	}
	
	public class Mbuffer
	{
		
	}
	
	public class Table
	{
		public Node[] node;
		public int flags;
		public object lastfree;
	}
	
	public class Dyndata
	{
		
	}
	
	public class CharPtr
	{
		public char[] chars;
		public int index;
		
		public char this[int offset]
		{
			get { return chars[index + offset]; }
			set { chars[index + offset] = value; }
		}
		public char this[uint offset]
		{
			get { return chars[index + offset]; }
			set { chars[index + offset] = value; }
		}
		public char this[long offset]
		{
			get { return chars[index + (int)offset]; }
			set { chars[index + (int)offset] = value; }
		}

		public static implicit operator CharPtr(string str) { return new CharPtr(str); }
		public static implicit operator CharPtr(char[] chars) { return new CharPtr(chars); }

		public CharPtr()
		{
			this.chars = null;
			this.index = 0;
		}

		public CharPtr(string str)
		{
			this.chars = (str + '\0').ToCharArray();
			this.index = 0;
		}

		public CharPtr(CharPtr ptr)
		{
			this.chars = ptr.chars;
			this.index = ptr.index;
		}

		public CharPtr(CharPtr ptr, int index)
		{
			this.chars = ptr.chars;
			this.index = index;
		}

		public CharPtr(char[] chars)
		{
			this.chars = chars;
			this.index = 0;
		}

		public CharPtr(char[] chars, int index)
		{
			this.chars = chars;
			this.index = index;
		}

		public CharPtr(IntPtr ptr)
		{
			this.chars = new char[0];
			this.index = 0;
		}

		public static CharPtr operator +(CharPtr ptr, int offset) {return new CharPtr(ptr.chars, ptr.index+offset);}
		public static CharPtr operator -(CharPtr ptr, int offset) {return new CharPtr(ptr.chars, ptr.index-offset);}
		public static CharPtr operator +(CharPtr ptr, uint offset) { return new CharPtr(ptr.chars, ptr.index + (int)offset); }
		public static CharPtr operator -(CharPtr ptr, uint offset) { return new CharPtr(ptr.chars, ptr.index - (int)offset); }

		public void inc() { this.index++; }
		public void dec() { this.index--; }
		public CharPtr next() { return new CharPtr(this.chars, this.index + 1); }
		public CharPtr prev() { return new CharPtr(this.chars, this.index - 1); }
		public CharPtr add(int ofs) { return new CharPtr(this.chars, this.index + ofs); }
		public CharPtr sub(int ofs) { return new CharPtr(this.chars, this.index - ofs); }
		
		public static bool operator ==(CharPtr ptr, char ch) { return ptr[0] == ch; }
		public static bool operator ==(char ch, CharPtr ptr) { return ptr[0] == ch; }
		public static bool operator !=(CharPtr ptr, char ch) { return ptr[0] != ch; }
		public static bool operator !=(char ch, CharPtr ptr) { return ptr[0] != ch; }

		public static CharPtr operator +(CharPtr ptr1, CharPtr ptr2)
		{
			string result = "";
			for (int i = 0; ptr1[i] != '\0'; i++)
				result += ptr1[i];
			for (int i = 0; ptr2[i] != '\0'; i++)
				result += ptr2[i];
			return new CharPtr(result);
		}
		public static int operator -(CharPtr ptr1, CharPtr ptr2) {
			Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index - ptr2.index; }
		public static bool operator <(CharPtr ptr1, CharPtr ptr2) {
			Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index < ptr2.index; }
		public static bool operator <=(CharPtr ptr1, CharPtr ptr2) {
			Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index <= ptr2.index; }
		public static bool operator >(CharPtr ptr1, CharPtr ptr2) {
			Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index > ptr2.index; }
		public static bool operator >=(CharPtr ptr1, CharPtr ptr2) {
			Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index >= ptr2.index; }
		public static bool operator ==(CharPtr ptr1, CharPtr ptr2) {
			object o1 = ptr1 as CharPtr;
			object o2 = ptr2 as CharPtr;
			if ((o1 == null) && (o2 == null)) return true;
			if (o1 == null) return false;
			if (o2 == null) return false;
			return (ptr1.chars == ptr2.chars) && (ptr1.index == ptr2.index); }
		public static bool operator !=(CharPtr ptr1, CharPtr ptr2) {return !(ptr1 == ptr2); }

		public override bool Equals(object o)
		{
			return this == (o as CharPtr);
		}

		public override int GetHashCode()
		{
			return 0;
		}
		public override string ToString()
		{
			string result = "";
			for (int i = index; (i<chars.Length) && (chars[i] != '\0'); i++)
				result += chars[i];
			return result;
		}
	}
	
	public class TKey_nk
	{
		public Node next;
	}
	
	public class TKey
	{
		public TKey_nk nk;
		public TValue tvk;
	}
	
	public class Node
	{
		public TKey i_key;
		public TValue i_val;	
	}
	
	public class lua_Reader
	{
		
	}
}

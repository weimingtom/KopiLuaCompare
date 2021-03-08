/*
 * Created by SharpDevelop.
 * User: 
 * Date: 2017/12/21
 * Time: 15:21
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Diagnostics;
using System.Text;

using AT.MIN;
 
namespace KopiLua
{
	public partial class Lua
	{
		public class BytePtr
		{
			public byte[] chars;
			private int _index;
			public int index
			{
				get
				{
					return _index;
				}
				set
				{
//					if (value == 4374)
//					{
//						Console.WriteLine("=================");
//					}
					_index = value;
				}
			}
			
			public byte this[int offset]
			{
				get { return chars[index + offset]; }
				set { chars[index + offset] = value; }
			}
			public byte this[uint offset]
			{
				get { return chars[index + offset]; }
				set { chars[index + offset] = value; }
			}
			public byte this[long offset]
			{
				get { return chars[index + (int)offset]; }
				set { chars[index + (int)offset] = value; }
			}

//			public static implicit operator CharPtr(string str) { return new CharPtr(str); }
//			public static implicit operator BytePtr(byte[] chars) { return new BytePtr(chars); }

			public BytePtr()
			{
				this.chars = null;
				this.index = 0;
			}

//			public CharPtr(string str)
//			{
//				this.chars = (str + '\0').ToCharArray();
//				this.index = 0;
//			}

			public BytePtr(BytePtr ptr)
			{
				this.chars = ptr.chars;
				this.index = ptr.index;
			}
			
			public BytePtr(CharPtr arr)
			{
				this.chars = new byte[arr.chars.Length];
				for (int i = 0; i < arr.chars.Length; ++i)
				{
					this.chars[i] = (byte)arr.chars[i];
				}
				this.index = 0;
			}

			public BytePtr(BytePtr ptr, int index)
			{
				this.chars = ptr.chars;
				this.index = index;
			}

			public BytePtr(byte[] chars, int index)
			{
				this.chars = chars;
				this.index = index;
			}			
			
			private BytePtr(int index, byte[] chars)
			{
				this.chars = chars;
				this.index = index;
			}

//			public BytePtr(IntPtr ptr)
//			{
//				this.chars = new byte[0];
//				this.index = 0;
//			}

			public static BytePtr operator +(BytePtr ptr, int offset) {return new BytePtr(ptr.index+offset, ptr.chars);}
			public static BytePtr operator -(BytePtr ptr, int offset) {return new BytePtr(ptr.index-offset, ptr.chars);}
			public static BytePtr operator +(BytePtr ptr, uint offset) { return new BytePtr(ptr.index + (int)offset, ptr.chars); }
			public static BytePtr operator -(BytePtr ptr, uint offset) { return new BytePtr(ptr.index - (int)offset, ptr.chars); }

			public void inc() { this.index++; }
			public void dec() { this.index--; }
			public BytePtr next() { return new BytePtr(this.index + 1, this.chars); }
			public BytePtr prev() { return new BytePtr(this.index - 1, this.chars); }
			public BytePtr add(int ofs) { return new BytePtr(this.index + ofs, this.chars); }
			public BytePtr sub(int ofs) { return new BytePtr(this.index - ofs, this.chars); }
			
			public static bool operator ==(BytePtr ptr, byte ch) { return ptr[0] == ch; }
			public static bool operator ==(byte ch, BytePtr ptr) { return ptr[0] == ch; }
			public static bool operator !=(BytePtr ptr, byte ch) { return ptr[0] != ch; }
			public static bool operator !=(byte ch, BytePtr ptr) { return ptr[0] != ch; }

//			public static CharPtr operator +(BytePtr ptr1, BytePtr ptr2)
//			{
//				string result = "";
//				for (int i = 0; ptr1[i] != '\0'; i++)
//					result += ptr1[i];
//				for (int i = 0; ptr2[i] != '\0'; i++)
//					result += ptr2[i];
//				return new CharPtr(result);
//			}
			public static int operator -(BytePtr ptr1, BytePtr ptr2) {
#if false
				//maincode-code == 4356
				if (ptr1.chars == mainbuffer_ && ptr2.chars == buffer_)
				{
					int result = ptr1.index - ptr2.index + (1024 * 4 + 256 + 4);
					return result;
				}
#endif
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index - ptr2.index;
			}
			public static bool operator <(BytePtr ptr1, BytePtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index < ptr2.index; }
			public static bool operator <=(BytePtr ptr1, BytePtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index <= ptr2.index; }
			public static bool operator >(BytePtr ptr1, BytePtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index > ptr2.index; }
			public static bool operator >=(BytePtr ptr1, BytePtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index >= ptr2.index; }
			public static bool operator ==(BytePtr ptr1, BytePtr ptr2) {
				object o1 = ptr1 as BytePtr;
				object o2 = ptr2 as BytePtr;
				if ((o1 == null) && (o2 == null)) return true;
				if (o1 == null) return false;
				if (o2 == null) return false;
				return (ptr1.chars == ptr2.chars) && (ptr1.index == ptr2.index); }
			public static bool operator !=(BytePtr ptr1, BytePtr ptr2) {return !(ptr1 == ptr2); }

			public override bool Equals(object o)
			{
				return this == (o as BytePtr);
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
			
			public byte[] ToByteArray()
			{
				byte[] arr = new byte[this.chars.Length - this.index];
				for (int i = 0; i < arr.Length; ++i)
				{
					arr[i] = this.chars[this.index + i];
				}
				return arr;
			}
		}
		
		public class IntegerPtr
		{
			public int[] chars;
			public int index;
			
			public int this[int offset]
			{
				get { return chars[index + offset]; }
				set { chars[index + offset] = value; }
			}
			public int this[uint offset]
			{
				get { return chars[index + offset]; }
				set { chars[index + offset] = value; }
			}
			public int this[long offset]
			{
				get { return chars[index + (int)offset]; }
				set { chars[index + (int)offset] = value; }
			}

//			public static implicit operator CharPtr(string str) { return new CharPtr(str); }
			public static implicit operator IntegerPtr(int[] chars) { return new IntegerPtr(chars); }

			public IntegerPtr()
			{
				this.chars = null;
				this.index = 0;
			}

//			public CharPtr(string str)
//			{
//				this.chars = (str + '\0').ToCharArray();
//				this.index = 0;
//			}

			public IntegerPtr(IntegerPtr ptr)
			{
				this.chars = ptr.chars;
				this.index = ptr.index;
			}

			public IntegerPtr(IntegerPtr ptr, int index)
			{
				this.chars = ptr.chars;
				this.index = index;
			}

			public IntegerPtr(int[] chars)
			{
				this.chars = chars;
				this.index = 0;
			}

			public IntegerPtr(int[] chars, int index)
			{
				this.chars = chars;
				this.index = index;
			}

			public static IntegerPtr operator +(IntegerPtr ptr, int offset) {return new IntegerPtr(ptr.chars, ptr.index+offset);}
			public static IntegerPtr operator -(IntegerPtr ptr, int offset) {return new IntegerPtr(ptr.chars, ptr.index-offset);}
			public static IntegerPtr operator +(IntegerPtr ptr, uint offset) { return new IntegerPtr(ptr.chars, ptr.index + (int)offset); }
			public static IntegerPtr operator -(IntegerPtr ptr, uint offset) { return new IntegerPtr(ptr.chars, ptr.index - (int)offset); }

			public void inc() { this.index++; }
			public void dec() { this.index--; }
			public IntegerPtr next() { return new IntegerPtr(this.chars, this.index + 1); }
			public IntegerPtr prev() { return new IntegerPtr(this.chars, this.index - 1); }
			public IntegerPtr add(int ofs) { return new IntegerPtr(this.chars, this.index + ofs); }
			public IntegerPtr sub(int ofs) { return new IntegerPtr(this.chars, this.index - ofs); }
			
			public static bool operator ==(IntegerPtr ptr, int ch) { return ptr[0] == ch; }
			public static bool operator ==(int ch, IntegerPtr ptr) { return ptr[0] == ch; }
			public static bool operator !=(IntegerPtr ptr, int ch) { return ptr[0] != ch; }
			public static bool operator !=(int ch, IntegerPtr ptr) { return ptr[0] != ch; }

//			public static CharPtr operator +(BytePtr ptr1, BytePtr ptr2)
//			{
//				string result = "";
//				for (int i = 0; ptr1[i] != '\0'; i++)
//					result += ptr1[i];
//				for (int i = 0; ptr2[i] != '\0'; i++)
//					result += ptr2[i];
//				return new CharPtr(result);
//			}
			public static int operator -(IntegerPtr ptr1, IntegerPtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index - ptr2.index; }
			public static bool operator <(IntegerPtr ptr1, IntegerPtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index < ptr2.index; }
			public static bool operator <=(IntegerPtr ptr1, IntegerPtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index <= ptr2.index; }
			public static bool operator >(IntegerPtr ptr1, IntegerPtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index > ptr2.index; }
			public static bool operator >=(IntegerPtr ptr1, IntegerPtr ptr2) {
				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index >= ptr2.index; }
			public static bool operator ==(IntegerPtr ptr1, IntegerPtr ptr2) {
				object o1 = ptr1 as IntegerPtr;
				object o2 = ptr2 as IntegerPtr;
				if ((o1 == null) && (o2 == null)) return true;
				if (o1 == null) return false;
				if (o2 == null) return false;
				return (ptr1.chars == ptr2.chars) && (ptr1.index == ptr2.index); }
			public static bool operator !=(IntegerPtr ptr1, IntegerPtr ptr2) {return !(ptr1 == ptr2); }

			public override bool Equals(object o)
			{
				return this == (o as IntegerPtr);
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
		
		
		public class CharPtr
		{
			public bool checkChange = false;
			public char[] chars;
			private int _index;
			public int index
			{
				get
				{
					return _index;
				}
				set
				{
					if (checkChange)
					{
						Debug.Assert(false, "index changed");
					}
					_index = value;
				}
			}
			
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
			public CharPtr(byte[] arr)
			{
				this.chars = new char[arr.Length];
				for (int i = 0; i < arr.Length; ++i)
				{
					this.chars[i] = (char)arr[i];
				}
				this.index = 0;
			}

			public CharPtr(CharPtr ptr)
			{
				this.chars = ptr.chars;
				this.index = ptr.index;
			}

			public CharPtr(CharPtr ptr, bool checkChange)
			{
				this.chars = ptr.chars;
				this.index = ptr.index;
				this.checkChange = checkChange;
			}
			
			public CharPtr(CharPtr ptr, int index)
			{
				this.chars = ptr.chars;
				this.index = ptr.index + index;
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
		
		public static YYSTYPE[] realloc_YYSTYPE(YYSTYPE[] obj, uint size)
		{
			YYSTYPE[] ret = new YYSTYPE[size];
			for (int i = 0; i < obj.Length; ++i)
			{
				if (i < obj.Length)
				{
					ret[i] = obj[i];
				}
				else
				{
					ret[i] = new YYSTYPE();
				}
			}
			return ret;
		}

		public static int[] realloc_int(int[] obj, uint size)
		{
			int[] ret = new int[size];
			for (int i = 0; i < size; ++i)
			{
				if (i < obj.Length)
				{
					ret[i] = obj[i];
				}
				else
				{
					ret[i] = (int)0;
				}
			}
			return ret;
		}
		
		public static BytePtr realloc_BytePtr(BytePtr obj, uint size)
		{
			byte[] ret = new byte[size];
			for (int i = 0; i < size; ++i)
			{
				if (i < obj.chars.Length)
				{
					ret[i] = obj[i];
				}
				else
				{
					ret[i] = (Byte)0;
				}
			}
			if (obj.index != 0)
			{
				throw new Exception("realloc_BytePtr not zero index");
			}
			return new BytePtr(ret, obj.index);
		}
		
		public static CharPtr calloc_char(uint n) 
		{
			return new CharPtr(new char[n]);
		}
		
		public static BytePtr calloc_Byte(uint n) 
		{
			return new BytePtr(new Byte[n], 0);
		}
		
		//private static List malloc_List()
		//{
		//	return new List();
		//}
		public static CharPtr malloc (ulong size)
		{
			return new CharPtr(new char[size]);
		}
		public static YYSTYPE[] malloc_YYSTYPE(int yynewmax)
		{
			YYSTYPE[] temp1 = new YYSTYPE[yynewmax];
			for (int kk = 0; kk < temp1.Length; ++kk)
			{
				temp1[kk] = new YYSTYPE();
			}
			return temp1;
		}
		public static int[] malloc_int(int yynewmax) {
			return new int[yynewmax];
		}
		
		public static void free(object obj)
		{
			
		}
		
		public static BytePtr memcpy(BytePtr ptr1, BytePtr ptr2, uint size)
		{
			for (int i = 0; i < size; i++)
				ptr1[i] = ptr2[i];
			return new BytePtr(ptr1);
		}
		
		public static YYSTYPE[] memcpy_YYSTYPE(YYSTYPE[] newyyv, YYSTYPE[] yyv, int yynewmax) {
			for (int kk = 0; kk < yynewmax; ++kk)
			{
				newyyv[kk].set(yyv[kk]);
			}
			return newyyv;
		}
		
		public static int[] memcpy_IntegerPtr(int[] newyys, int[] yys, int yynewmax) {
			//yys = (IntegerPtr) memcpy(newyys, (CharPtr) yys, yynewmax * sizeof(int));
			for (int kk = 0; kk < yynewmax; ++kk)
			{
				newyys[kk] = yys[kk];
			}
			return newyys;
		}
			
		
		
		public static int strcmp(CharPtr s1, CharPtr s2)
		{
			if (s1 == s2)
				return 0;
			if (s1 == null)
				return -1;
			if (s2 == null)
				return 1;

			for (int i = 0; ; i++)
			{
				if (s1[i] != s2[i])
				{
					if (s1[i] < s2[i])
						return -1;
					else
						return 1;
				}
				if (s1[i] == '\0')
					return 0;
			}
		}
		
		public class FILE
		{
			public Stream stream;
			
			public FILE()
			{
				
			}
			
			public FILE(Stream stream)
			{
				this.stream = stream;
			}
		}
		
		public static int fgetc(FILE fp)
		{
			int result = fp.stream.ReadByte();
			if (result == (int)'\r') //FIXME: only tested under Windows
			{
				result = fp.stream.ReadByte();
			}
			return result;
		}
		public static void ungetc(int c, FILE fp)
		{
			if (fp.stream.Position > 0)
				fp.stream.Seek(-1, SeekOrigin.Current);
		}
		
		public const int EOF = -1;
		
		public static FILE fopen(CharPtr filename, CharPtr mode)
		{
			FileStream stream = null;
			string str = filename.ToString();			
			FileMode filemode = FileMode.Open;
			FileAccess fileaccess = (FileAccess)0;			
			for (int i=0; mode[i] != '\0'; i++)
				switch (mode[i])
				{
					case 'r': 
						fileaccess = fileaccess | FileAccess.Read;
						if (!File.Exists(str))
							return null;
						break;

					case 'w':
						filemode = FileMode.Create;
						fileaccess = fileaccess | FileAccess.Write;
						break;
				}
			try
			{
				stream = new FileStream(str, filemode, fileaccess);
			}
			catch
			{
				stream = null;
			}			
			
			FILE ret = new FILE();
			ret.stream = stream;
			return ret;
		}
		
		public static void fclose(FILE fp)
		{
			try
			{
				fp.stream.Flush();
				fp.stream.Close();
			}
			catch { }
		}
		
		public static FILE stdin = new FILE(Console.OpenStandardInput());
		public static FILE stdout = new FILE(Console.OpenStandardOutput());
		public static FILE stderr = new FILE(Console.OpenStandardError());
		
		public static int isspace(int c)
		{
			return ((c==' ') || (c>=(char)0x09 && c<=(char)0x0D)) ? 1 : 0;
		}
		public static int isdigit(int c)
		{
			return Char.IsDigit((char)c) ? 1 : 0;
		}
		
		public static char tolower(char c)
		{
			return Char.ToLower(c);
		}
		
		public static char toupper(char c)
		{
			return Char.ToUpper(c);
		}

		public static int fprintf(FILE fp, string str, params object[] argv)
		{
			string result = Tools.sprintf(str.ToString(), argv);
			char[] chars = result.ToCharArray();
			byte[] bytes = new byte[chars.Length];
			for (int i=0; i<chars.Length; i++)
				bytes[i] = (byte)chars[i];
			fp.stream.Write(bytes, 0, bytes.Length);
			return 1; //Returns the number of characters printed
		}
		public static int printf(string str, params object[] argv)
		{
			Tools.printf(str.ToString(), argv);
			return 1; //Returns the number of characters printed
		}
		public static void sprintf(CharPtr buffer, CharPtr str, params object[] argv)
		{
			string temp = Tools.sprintf(str.ToString(), argv);
			strcpy(buffer, new CharPtr(temp));
		}		
		
		public static int system(CharPtr str)
		{
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.FileName = "cmd";
            processStartInfo.Arguments = "/c ";
            processStartInfo.Arguments += str.ToString();

            Process process = Process.Start(processStartInfo);
            process.WaitForExit();
            return process.ExitCode;
		}
		
		public static int remove(CharPtr filename)
		{
			try 
			{
				if (File.Exists(filename.ToString()))
	            {  
					File.Delete(filename.ToString());
					return 0;
				}
				else
				{
					return -1;
				}
			} 
			catch
			{
				return -1;
			}
		}
		
		public const int BUFSIZ = 8192; //FIXME:???
		
		public static double sin(double x)
		{
			return Math.Sin(x);
		}
		
		public static double cos(double x)
		{
			return Math.Cos(x);
		}
		
		public static double tan(double x)
		{
			return Math.Tan(x);
		}
		
		public static double asin(double x)
		{
			return Math.Asin(x);
		}
		
		public static double acos(double x)
		{
			return Math.Acos(x);
		}
		
		public static double atan(double x)
		{
			return Math.Atan(x);
		}
		
		public static double ceil(double x) 
		{
			return Math.Ceiling(x);
		}
		
		public static double floor(double x)
		{
			return Math.Floor(x);
		}
		
		public static double sqrt(double x) 
		{
			return Math.Sqrt(x);
		}
		
		public static double pow(double x, double y)
		{
			return Math.Pow(x, y);
		}
		
		public static CharPtr strstr(CharPtr str, CharPtr substr)
		{
			int index = str.ToString().IndexOf(substr.ToString());
			if (index < 0)
				return null;
			return new CharPtr(str + index);
		}
		
		public static uint strlen(CharPtr str)
		{
			uint index = 0;
			while (str[index] != '\0')
				index++;
			return index;
		}
		
		public static CharPtr strdup(CharPtr s)
		{
			int index = s.index;
			char[] chars = new char[s.chars.Length];
			for (int i = 0; i < chars.Length; ++i) 
				chars[i] = s.chars[i];
			return new CharPtr(chars, index);
		}
		
		//https://github.com/weimingtom/KopiLuaCompare/blob/1450ff7c6b1885b6e9aa9af9e78dc7fd19678aec/lua-5.1.5/kopilua/luaconf_ex.h.cs
		//from strtoul
		public static double strtod(CharPtr s, ref CharPtr end) 
		{
			int base_ = 10;
			try
			{
				end = new CharPtr(s.chars, s.index);

				// skip over any leading whitespace
				while (end[0] == ' ')
					end = end.next();

				// ignore any leading 0x
				if ((end[0] == '0') && (end[1] == 'x'))
					end = end.next().next();
				else if ((end[0] == '0') && (end[1] == 'X'))
					end = end.next().next();

				// do we have a leading + or - sign?
				bool negate = false;
				if (end[0] == '+')
					end = end.next();
				else if (end[0] == '-')
				{
					negate = true;
					end = end.next();
				}

				// loop through all chars
				bool invalid = false;
				bool had_digits = false;
				ulong result = 0;
				while (true)
				{
					// get this char
					char ch = end[0];					

					// which digit is this?
					int this_digit = 0;
					if (Char.IsDigit(ch)) //(isdigit(ch))
						this_digit = ch - '0';
					else if (Char.IsLetter(ch)) //(isalpha(ch))
						this_digit = tolower(ch) - 'a' + 10;
					else
						break;

					// is this digit valid?
					if (this_digit >= base_)
						invalid = true;
					else
					{
						had_digits = true;
						result = result * (ulong)base_ + (ulong)this_digit;
					}

					end = end.next();
				}

				// were any of the digits invalid?
				if (invalid || (!had_digits))
				{
					end = s;
					return System.UInt64.MaxValue;
				}

				// if the value was a negative then negate it here
				if (negate)
					result = (ulong)-(long)result;

				// ok, we're done
				return (ulong)result;
			}
			catch
			{
				end = s;
				return 0;
			}
		}
		
		public static int fscanf(FILE fp, CharPtr format, params object[] argp)
		{
			string str = Console.ReadLine();
			return sscanf(str, format, argp);
		}
		private static int sscanf(CharPtr str, CharPtr fmt, params object[] argp)
		{
			//throw new Exception("sscanf not implemented");
			int parm_index = 0;
			int index = 0;
			while (fmt[index] != 0)
			{
				if (fmt[index++]=='%')
					switch (fmt[index++])
					{
						case 's':
							{
								argp[parm_index++] = str;
								break;
							}
						case 'c':
							{
								argp[parm_index++] = Convert.ToChar(str);
								break;
							}
						case 'd':
							{
								argp[parm_index++] = Convert.ToInt32(str);
								break;
							}
						case 'l':
							{
								argp[parm_index++] = Convert.ToDouble(str);
								break;
							}
						case 'f':
							{
								argp[parm_index++] = Convert.ToDouble(str);
								break;
							}
						//case 'p': //FIXME:
						//    {
						//        result += "(pointer)";
						//        break;
						//    }
					}
			}
			return parm_index;
		}
		
		
		public static CharPtr strchr(CharPtr str, char c)
		{
			if (c != '\0')
			{
				for (int index = str.index; str.chars[index] != 0; index++)
					if (str.chars[index] == c)
						return new CharPtr(str.chars, index);
			}
			else
			{
				for (int index = str.index; index < str.chars.Length; index++)
					if (str.chars[index] == c)
						return new CharPtr(str.chars, index);
			}
			return null;
		}
		
		public static CharPtr strcpy(CharPtr dst, CharPtr src)
		{
			int i;
			for (i = 0; src[i] != '\0'; i++)
				dst[i] = src[i];
			dst[i] = '\0';
			return dst;
		}
		
		public static int puts(CharPtr str)
		{
			Console.WriteLine(str.ToString());
			return 0;
		}
		public static int putc(int ch, FILE fp)
		{
			fp.stream.WriteByte((byte)ch);
			return ch;
		}
		
		//https://github.com/weimingtom/KopiLuaCompare/blob/1450ff7c6b1885b6e9aa9af9e78dc7fd19678aec/lua-5.1.5/kopilua/luaconf_ex.h.cs
		public static CharPtr strcat(CharPtr dst, CharPtr src)
		{
			int dst_index = 0;
			while (dst[dst_index] != '\0')
				dst_index++;
			int src_index = 0;
			while (src[src_index] != '\0')
				dst[dst_index++] = src[src_index++];
			dst[dst_index++] = '\0';
			return dst;
		}
		
		//Java
		//float f = 123.456f;
		//int i = Float.floatToIntBits(f);
		//System.out.println(i);
		//public static float intBitsToFloat(int bits)
		public static float bytesToFloat(byte byte0, byte byte1, byte byte2, byte byte3)
		{
			return BitConverter.ToSingle(new byte[]{byte0, byte1, byte2, byte3}, 0);
		}
		
		public static double atof(CharPtr nptr)
		{
			return Convert.ToDouble(nptr.ToString());
		}
		
		public static int putchar(int ch)
		{
			printf("%c", (char)ch);
		 	return ch;
		}
		
		public class stat_struct
		{
			
		}
		public static int stat(CharPtr filename, stat_struct st)
		{
			if (File.Exists(filename.ToString()))
			{
				return 0;
			}
			else
			{
				return -1;
			}
		}
		public static CharPtr fgets(CharPtr s, int n, FILE file)
		{
			bool isEnd = false;
			StringBuilder sb = new StringBuilder();
			while (true)
			{
				if (sb.Length >= n - 1)
				{
					break;
				}
				int result = fp.stream.ReadByte();
				if (result == (int)'\r') //FIXME: only tested under Windows
				{
					result = fp.stream.ReadByte();
					if (result == -1)
					{
						isEnd = true;
						break;
					}
					break;
				} 
				else if (result == -1)
				{
					isEnd = true;
					break;
				}
				sb.Append((char)(byte)result);
			}
			if (isEnd && sb.Length == 0)
			{
				//FIXME:if get eof not \n after the string, make eof like \n
				return null;
			}
			CharPtr src = sb.ToString() + "\n"; //FIXME:\n
			strcpy(s, src);
			return s;
		}
		public static int isalnum(int c) 
		{
			return char.IsLetterOrDigit((char)c) ? 1 : 0;
		}
		public static CharPtr gets(CharPtr s)
		{
			string str = Console.ReadLine();
			if (str != null)
			{
				strcpy (s, new CharPtr(str));
				return s;
			}
			else
			{
				return null;
			}
		}
		
		public class jmp_buf
		{
			
		}
		public class LongjmpException : Exception {
			
		}
		public static int setjmp(jmp_buf env)
		{
			throw new Exception("not implemented, use try instead");
		}
		public static void longjmp(jmp_buf env, int val)
		{
			//throw new Exception("not implemented");
			throw new LongjmpException();
		}
		
		public static double log(double d)
		{
			return Math.Log(d);
		}

		public static double log10(double d)
		{
			return Math.Log10(d);
		}		
		
		public static double exp(double d)
		{
			return Math.Exp(d);
		}
		
		public static void exit(int exitCode)
		{
			Environment.Exit(exitCode);
		}
		
		public static CharPtr getenv(CharPtr envname)
		{
			// todo: fix this - mjf
			//if (envname == "LUA_PATH)
				//return "MyPath";
			return Environment.GetEnvironmentVariable(envname.ToString());
		}		
		
		public class time_t
		{
			
		}
		
		public class tm
		{
			public int tm_mday = 0;
			public int tm_mon = 0; //+1
			public int tm_year = 0; //+1900
			public int tm_hour = 0;
			public int tm_min = 0;
			public int tm_sec = 0;
		}
		
		public static void time(time_t t)
		{
			
		}
		
		public static tm localtime(time_t t)
		{
			tm result = new tm();
			DateTime currentTime = DateTime.Now;
			result.tm_mday = currentTime.Day;
			result.tm_mon = currentTime.Month - 1; //+1
			result.tm_year = currentTime.Year - 1900; //+1900
			result.tm_hour = currentTime.Hour;
			result.tm_min = currentTime.Minute;
			result.tm_sec = currentTime.Second;
			return result;
		}
		
//		public static void lua_parse (ref BytePtr code)
//		{
//			
//		}
		
		//#define Word lua_Word	/* some systems have Word as a predefined type */
		//typedef unsigned short Word;  /* unsigned 16 bits */
		public static int sizeof_Word() {
			return 2;
		}
	}
}

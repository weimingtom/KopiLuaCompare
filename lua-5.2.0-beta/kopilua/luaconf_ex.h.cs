using System;
using System.IO;
using System.Diagnostics;
using AT.MIN;

namespace KopiLua
{
	using lua_Number = System.Double;
	using clock_t = System.Int64;
	
	public partial class Lua
	{
		public const int TYPE_STDIN = 0;
		public const int TYPE_STDOUT = 1;
		public const int TYPE_STDERR = 2;
		
		public class StreamProxy 
		{	
			private Stream stream;
			private int type = -1;
			
			public StreamProxy(int type)
			{
				this.type = type;
			}
			
			public StreamProxy(Stream stream)
			{
				this.stream = stream;
			}
			
			public void Write(byte[] buffer, int offset, int count)
			{
				if (type >= 0)
				{
					if (libImpl != null)
					{
						libImpl.Write(type, buffer, offset, count);
					}
				}
				else
				{
					if (this.stream != null)
					{
						this.stream.Write(buffer, offset, count);
					}
				}
			}
			
			public int Read(byte[] buffer, int offset, int count)
			{
				if (type >= 0)
				{
					if (libImpl != null)
					{
						return libImpl.Read(type, buffer, offset, count);
					}
					return 0;
				}
				else
				{
					if (stream != null)
					{
						return stream.Read(buffer, offset, count);
					}
					return 0;
				}
			}
			
			public int ReadByte()
			{
				if (type >= 0)
				{
					if (libImpl != null)
					{
						return libImpl.ReadByte(type);
					}
					return 0;
				}
				else
				{
					if (this.stream != null)
					{
						return this.stream.ReadByte();
					}
					return 0;
				}
			}
			
			public void ungetc()
			{
				if (this.stream != null)
				{
					if (this.stream.Position > 0)
					{
						 this.stream.Seek(-1, SeekOrigin.Current);
					}
				}
			}
			
			public bool isEof()
			{
				if (type >= 0)
				{
					if (libImpl != null)
					{
						return libImpl.isEof(type);
					}
					return false;
				}
				else
				{
					if (this.stream != null)
					{
						return stream.Position >= stream.Length;
					}
					return true;
				}
			}
			
			public void close()
			{
				if (stream != null)
				{
					try
					{
						stream.Flush();
						stream.Close();
					}
					catch { }
				}
			}
			
			public int flush()
			{
				if (type >= 0)
				{
					if (libImpl != null)
					{
						libImpl.flush(type);
					}
					return 0;
				}
				else
				{
					int result = 0;
					if (stream != null)
					{
						try
						{
							stream.Flush();
						} 
						catch 
						{
							result = 1;
						}
					}
					else
					{
						result = 1;
					}
					return result;
				}
			}
			
			public int seek(long offset, int origin)
			{
				if (stream != null)
				{
					try
					{
						stream.Seek(offset, (SeekOrigin)origin);
						return 0;
					}
					catch
					{
						return 1;
					}
				}
				else
				{
					return 1;
				}
			}
			
			public long getPosition()
			{
				if (stream != null)
				{
					return stream.Position;
				}
				return 0;
			}
			
			public void puts(string str)
			{
				if (type >= 0)
				{
					if (libImpl != null)
					{
						libImpl.writeString(str.ToString());
					}
				}
				else
				{
					if (this.stream != null)
					{
						byte[] byteArray = System.Text.Encoding.Default.GetBytes(str);
						stream.Write(byteArray, 0, byteArray.Length);
					}
				}
			}
			
			public string readline()
			{
				if (type >= 0)
				{
					if (libImpl != null)
					{
						return libImpl.readLine(type);
					}
					return null;
				}
				else
				{
					throw new System.NotImplementedException();
				}
			}
		}
		
		public interface LibImpl
		{
			int ReadByte(int type);
			int Read(int type, byte[] buffer, int offset, int count);
			void Write(int type, byte[] buffer, int offset, int count);
			bool isEof(int type);
			void flush(int type);
			void putchar(char ch);
			void writeString(string str);
			string readLine(int type);
		}
		private static LibImpl libImpl = new ConsoleLibImpl();
		public static void setLibImpl(LibImpl _libImpl)
		{
			libImpl = _libImpl;
		}
		
		public class ConsoleLibImpl : LibImpl
		{
#if UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5

#else
			public static Stream stdout_ = Console.OpenStandardOutput();
			public static Stream stdin_ = Console.OpenStandardInput();
			public static Stream stderr_ = Console.OpenStandardError();
#endif

			public int ReadByte(int type)
			{
				if (type == TYPE_STDIN)
				{
#if UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5

#else
					return stdin_.ReadByte();
#endif
				}
				return 0;
			}
			public int Read(int type, byte[] buffer, int offset, int count)
			{
				if (type == TYPE_STDIN)
				{
#if UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5

#else
					return stdin_.Read(buffer, offset, count);
#endif
				}
				return 0;
			}
			public void Write(int type, byte[] buffer, int offset, int count)
			{
				if (type == TYPE_STDOUT)
				{
#if UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5
					string str = "";
					for (int i = 0; i < count; i++)
					{
						char ch = (char)buffer[offset + i];
						str += ch;
					}
					int n = -1;
					for (int i = str.Length - 1; i >= 0; i--)
					{
						char ch = str[i];
						if (ch != '\r' && ch != '\n')
						{
							n = i;
							break;
						}
					}
					if (n >= 0)
					{
						str = str.Substring(0, n+1);
					}
					else
					{
						str = "";
					}
					if (str.Length > 0)
					{
						UnityEngine.Debug.Log(str);
					}
#else
					stdout_.Write(buffer, offset, count);
#endif
				}
			}
			public bool isEof(int type)
			{
				return false;
			}
			public void flush(int type)
			{
#if UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5

#else
				if (type == TYPE_STDIN)
				{
					stdin_.Flush();
				} 
				else if (type == TYPE_STDOUT)
				{
					stdout_.Flush();
				}
				else if (type == TYPE_STDERR)
				{
					stderr_.Flush();
				}
#endif
			}
			public void putchar(char ch)
			{
#if UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5
				UnityEngine.Debug.Log(ch);
#else
				Console.Write(ch);
#endif
			}
			public void writeString(string str)
			{
#if UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5
				UnityEngine.Debug.Log(str);
#else
				Console.Write(str);
#endif
			}
			public string readLine(int type)
			{
				if (type == TYPE_STDIN)
				{
#if UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5

#else
					return Console.ReadLine();
#endif
				}
				return null;
			}
		}
		public static StreamProxy stdout = new StreamProxy(TYPE_STDOUT);
		public static StreamProxy stdin = new StreamProxy(TYPE_STDIN);
		public static StreamProxy stderr = new StreamProxy(TYPE_STDERR);
		
		// misc stuff needed for the compile

		public static bool isalpha(char c) { return Char.IsLetter(c); }
		public static bool iscntrl(char c) { return Char.IsControl(c); }
		public static bool isdigit(char c) { return Char.IsDigit(c); }
		public static bool islower(char c) { return Char.IsLower(c); }
		public static bool ispunct(char c) { return Char.IsPunctuation(c); }
		public static bool isspace(char c) { return (c==' ') || (c>=(char)0x09 && c<=(char)0x0D); }
		public static bool isupper(char c) { return Char.IsUpper(c); }
		public static bool isalnum(char c) { return Char.IsLetterOrDigit(c); }
		public static bool isxdigit(char c) { return "0123456789ABCDEFabcdef".IndexOf(c) >= 0; }

		public static bool isalpha(int c) { return Char.IsLetter((char)c); }
		public static bool iscntrl(int c) { return Char.IsControl((char)c); }
		public static bool isdigit(int c) { return Char.IsDigit((char)c); }
		public static bool islower(int c) { return Char.IsLower((char)c); }
		public static bool ispunct(int c) { return ((char)c != ' ') && !isalnum((char)c); } // *not* the same as Char.IsPunctuation
		public static bool isspace(int c) { return ((char)c == ' ') || ((char)c >= (char)0x09 && (char)c <= (char)0x0D); }
		public static bool isupper(int c) { return Char.IsUpper((char)c); }
		public static bool isalnum(int c) { return Char.IsLetterOrDigit((char)c); }
		public static bool isxdigit(int c) { return "0123456789ABCDEFabcdef".IndexOf((char)c) >= 0; }
		
		public static char tolower(char c) { return Char.ToLower(c); }
		public static char toupper(char c) { return Char.ToUpper(c); }
		public static char tolower(int c) { return Char.ToLower((char)c); }
		public static char toupper(int c) { return Char.ToUpper((char)c); }

		public static ulong strtoul(CharPtr s, out CharPtr end, int base_)
		{
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
					if (isdigit(ch))
						this_digit = ch - '0';
					else if (isalpha(ch))
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

		public static void putchar(char ch)
		{
			if (libImpl != null)
			{
				libImpl.putchar(ch);
			}
		}

		public static void putchar(int ch)
		{
			if (libImpl != null)
			{
				libImpl.putchar((char)ch);
			}
		}

		public static bool isprint(byte c)
		{
			return (c >= (byte)' ') && (c <= (byte)127);
		}

		public static int parse_scanf(string str, CharPtr fmt, params object[] argp)
		{
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
						//case 'p':
						//    {
						//        result += "(pointer)";
						//        break;
						//    }
					}
			}
			return parm_index;
		}

		public static void printf(CharPtr str, params object[] argv)
		{
			Tools.printf(str.ToString(), argv);
		}

		public static int sprintf(CharPtr buffer, CharPtr str, params object[] argv)
		{
			string temp = Tools.sprintf(str.ToString(), argv);
			strcpy(buffer, temp);
			return strlen(buffer); //FIXME:added
		}

		public static int fprintf(StreamProxy stream, CharPtr str, params object[] argv)
		{
			string result = Tools.sprintf(str.ToString(), argv);
			char[] chars = result.ToCharArray();
			byte[] bytes = new byte[chars.Length];
			for (int i=0; i<chars.Length; i++)
				bytes[i] = (byte)chars[i];
			stream.Write(bytes, 0, bytes.Length);
			return 1;
		}

		public const int EXIT_SUCCESS = 0;
		public const int EXIT_FAILURE = 1;

		public static int errno = -1;
		//FIXME:changed, see upper
//		public static int errno()
//		{
//			return -1;	// todo: fix this - mjf
//		}

		public static CharPtr strerror(int error)
		{
			return String.Format("error #{0}", error); // todo: check how this works - mjf
		}

		public static CharPtr getenv(CharPtr envname)
		{
			// todo: fix this - mjf
			//if (envname == "LUA_PATH)
				//return "MyPath";
			return null;
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
			//FIXME:added
			public static CharPtr FromNumber(lua_Number n)
			{
				byte[] bytes = BitConverter.GetBytes(n);
				char[] chars = new Char[bytes.Length];
				for (int i = 0; i < bytes.Length; ++i)
				{
					chars[i] = (char)bytes[i];
				}
				return new CharPtr(chars);
			}
		}

		public static int memcmp(CharPtr ptr1, CharPtr ptr2, uint size) { return memcmp(ptr1, ptr2, (int)size); }
		public static int memcmp(CharPtr ptr1, CharPtr ptr2, int size)
		{
			for (int i=0; i<size; i++)
				if (ptr1[i]!=ptr2[i])
				{
					if (ptr1[i]<ptr2[i])
						return -1;
					else
						return 1;
				}
			return 0;
		}

		public static CharPtr memchr(CharPtr ptr, char c, uint count)
		{
			for (uint i = 0; i < count; i++)
				if (ptr[i] == c)
					return new CharPtr(ptr.chars, (int)(ptr.index + i));
			return null;
		}

		public static CharPtr strpbrk(CharPtr str, CharPtr charset)
		{
			for (int i=0; str[i] != '\0'; i++)
				for (int j = 0; charset[j] != '\0'; j++)
					if (str[i] == charset[j])
						return new CharPtr(str.chars, str.index + i);
			return null;
		}

		// find c in str
		public static CharPtr strchr(CharPtr str, char c)
		{
			for (int index = str.index; str.chars[index] != 0; index++)
				if (str.chars[index] == c)
					return new CharPtr(str.chars, index);
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

		public static CharPtr strncat(CharPtr dst, CharPtr src, int count)
		{
			int dst_index = 0;
			while (dst[dst_index] != '\0')
				dst_index++;
			int src_index = 0;
			while ((src[src_index] != '\0') && (count-- > 0))
				dst[dst_index++] = src[src_index++];
			return dst;
		}

		public static uint strcspn(CharPtr str, CharPtr charset)
		{
			int index = str.ToString().IndexOfAny(charset.ToString().ToCharArray());
			if (index < 0)
				index = str.ToString().Length;
			return (uint)index;
		}

		public static CharPtr strncpy(CharPtr dst, CharPtr src, int length)
		{
			int index = 0;
			while ((src[index] != '\0') && (index<length))
			{
				dst[index] = src[index];
				index++;
			}
			while (index < length)
				dst[index++] = '\0';
			return dst;
		}

		public static int strlen(CharPtr str)
		{
			int index = 0;
			while (str[index] != '\0')
				index++;
			return index;
		}

		public static lua_Number fmod(lua_Number a, lua_Number b)
		{
			float quotient = (int)Math.Floor(a / b);
			return a - quotient * b;
		}

		public static lua_Number modf(lua_Number a, out lua_Number b)
		{
			b = Math.Floor(a);
			return a - Math.Floor(a);
		}

		public static long lmod(lua_Number a, lua_Number b)
		{
			return (long)a % (long)b;
		}

		public static int getc(StreamProxy f)
		{
			return f.ReadByte();
		}

		public static void ungetc(int c, StreamProxy f)
		{
			f.ungetc();
		}

		public static int EOF = -1;

		public static void fputs(CharPtr str, StreamProxy stream)
		{
			stream.puts(str.ToString());
		}

		public static int feof(StreamProxy s)
		{
			return (s.isEof()? 1 : 0);
		}

		public static int fread(CharPtr ptr, int size, int num, StreamProxy stream)
		{
			int num_bytes = num * size;
			byte[] bytes = new byte[num_bytes];
			try
			{
				int result = stream.Read(bytes, 0, num_bytes);
				for (int i = 0; i < result; i++)
					ptr[i] = (char)bytes[i];
				return result/size;
			}
			catch
			{
				return 0;
			}
		}

		public static int fwrite(CharPtr ptr, int size, int num, StreamProxy stream)
		{
			int num_bytes = num * size;
			byte[] bytes = new byte[num_bytes];
			for (int i = 0; i < num_bytes; i++)
				bytes[i] = (byte)ptr[i];
			try
			{
				stream.Write(bytes, 0, num_bytes);
			}
			catch
			{
				return 0;
			}
			return num;
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

		public static CharPtr fgets(CharPtr str, StreamProxy stream)
		{
			int index = 0;
			try
			{
				while (true)
				{
					str[index] = (char)stream.ReadByte();
					if (str[index] == '\n')
						break;
					if (index >= str.chars.Length)
						break;
					index++;
				}
			}
			catch
			{
			}
			return str;
		}

		public static double frexp(double x, out int expptr)
		{
			expptr = (int)Math.Log(x, 2) + 1;
			double s = x / Math.Pow(2, expptr);
			return s;
		}

		public static double ldexp(double x, int expptr)
		{
			return x * Math.Pow(2, expptr);
		}

		public static CharPtr strstr(CharPtr str, CharPtr substr)
		{
			int index = str.ToString().IndexOf(substr.ToString());
			if (index < 0)
				return null;
			return new CharPtr(str + index);
		}

		public static CharPtr strrchr(CharPtr str, char ch)
		{
			int index = str.ToString().LastIndexOf(ch);
			if (index < 0)
				return null;
			return str + index;
		}

		public static StreamProxy fopen(CharPtr filename, CharPtr mode)
		{
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
				return new StreamProxy(new FileStream(str, filemode, fileaccess));
			}
			catch
			{
				return null;
			}
		}

		public static StreamProxy freopen(CharPtr filename, CharPtr mode, StreamProxy stream)
		{
			stream.close();
			return fopen(filename, mode);
		}
		
		//see below
//		public static void fflush(Stream stream)
//		{
//			stream.Flush();
//		}

		public static int ferror(StreamProxy stream)
		{
			return 0;	// todo: fix this - mjf
		}

		public static int fclose(StreamProxy stream)
		{
			stream.close();
			return 0;
		}

		public static StreamProxy tmpfile()
		{
			return new StreamProxy(new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite));
		}

		public static int fscanf(StreamProxy f, CharPtr format, params object[] argp)
		{
			string str = f.readline();
			return parse_scanf(str, format, argp);
		}
		
		public static int fseek(StreamProxy f, long offset, int origin)
		{
			return f.seek(offset, origin);
		}


		public static int ftell(StreamProxy f)
		{
			return (int)f.getPosition();
		}

		public static int clearerr(StreamProxy f)
		{
			//Debug.Assert(false, "clearerr not implemented yet - mjf");
			return 0;
		}

		public static int setvbuf(StreamProxy stream, CharPtr buffer, int mode, uint size)
		{
			Debug.Assert(false, "setvbuf not implemented yet - mjf");
			return 0;
		}

		public static void memcpy<T>(T[] dst, T[] src, int length)
		{
			for (int i = 0; i < length; i++)
				dst[i] = src[i];
		}

		public static void memcpy<T>(T[] dst, int offset, T[] src, int length)
		{
			for (int i=0; i<length; i++)
				dst[offset+i] = src[i];
		}

		public static void memcpy<T>(T[] dst, T[] src, int srcofs, int length)
		{
			for (int i = 0; i < length; i++)
				dst[i] = src[srcofs+i];
		}

		public static void memcpy(CharPtr ptr1, CharPtr ptr2, uint size) { memcpy(ptr1, ptr2, (int)size); }
		public static void memcpy(CharPtr ptr1, CharPtr ptr2, int size)
		{
			for (int i = 0; i < size; i++)
				ptr1[i] = ptr2[i];
		}

		public static object VOID(object f) { return f; }

		public const double HUGE_VAL = System.Double.MaxValue;
		public const uint SHRT_MAX = System.UInt16.MaxValue;

		public const int _IONBF = 0;
		public const int _IOFBF = 1;
		public const int _IOLBF = 2;

		public const int SEEK_SET = 0;
		public const int SEEK_CUR = 1;
		public const int SEEK_END = 2;

		// one of the primary objectives of this port is to match the C version of Lua as closely as
		// possible. a key part of this is also matching the behaviour of the garbage collector, as
		// that affects the operation of things such as weak tables. in order for this to occur the
		// size of structures that are allocated must be reported as identical to their C++ equivelents.
		// that this means that variables such as global_State.totalbytes no longer indicate the true
		// amount of memory allocated.
		public static int GetUnmanagedSize(Type t)
		{
			if (t == typeof(global_State))
				return 228;
			else if (t == typeof(LG))
				return 376;
			else if (t == typeof(CallInfo))
				return 24;
			else if (t == typeof(lua_TValue))
				return 16;
			else if (t == typeof(Table))
				return 32;
			else if (t == typeof(Node))
				return 32;
			else if (t == typeof(GCObject))
				return 120;
			else if (t == typeof(GCObjectRef))
				return 4;
			else if (t == typeof(ArrayRef))
				return 4;
			else if (t == typeof(Closure))
				return 0;	// handle this one manually in the code
			else if (t == typeof(Proto))
				return 76;
			else if (t == typeof(luaL_Reg))
				return 8;
			else if (t == typeof(luaL_Buffer))
				return 524;
			else if (t == typeof(lua_State))
				return 120;
			else if (t == typeof(lua_Debug))
				return 100;
			else if (t == typeof(CallS))
				return 8;
			else if (t == typeof(LoadF))
				return 520;
			else if (t == typeof(LoadS))
				return 8;
			else if (t == typeof(lua_longjmp))
				return 72;
			else if (t == typeof(SParser))
				return 20;
			else if (t == typeof(Token))
				return 16;
			else if (t == typeof(LexState))
				return 52;
			else if (t == typeof(FuncState))
				return 572;
			else if (t == typeof(GCheader))
				return 8;
			else if (t == typeof(lua_TValue))
				return 16;
			else if (t == typeof(TString))
				return 16;
			else if (t == typeof(LocVar))
				return 12;
			else if (t == typeof(UpVal))
				return 32;
			else if (t == typeof(CClosure))
				return 40;
			else if (t == typeof(LClosure))
				return 24;
			else if (t == typeof(TKey))
				return 16;
			else if (t == typeof(ConsControl))
				return 40;
			else if (t == typeof(LHS_assign))
				return 32;
			else if (t == typeof(expdesc))
				return 24;
			else if (t == typeof(Upvaldesc)) //replace upvaldesc
				return 6; //replace 2
			else if (t == typeof(BlockCnt))
				return 12;
			else if (t == typeof(Zio))
				return 20;
			else if (t == typeof(Mbuffer))
				return 12;
			else if (t == typeof(LoadState))
				return 16;
			else if (t == typeof(MatchState))
				return 272;
			else if (t == typeof(stringtable))
				return 12;
			else if (t == typeof(FilePtr))
				return 4;
			else if (t == typeof(Udata))
				return 24;
			else if (t == typeof(Char))
				return 1;
			else if (t == typeof(UInt16))
				return 2;
			else if (t == typeof(Int16))
				return 2;
			else if (t == typeof(UInt32))
				return 4;
			else if (t == typeof(Int32))
				return 4;
			else if (t == typeof(Single))
				return 4;
			else if (t == typeof(Vardesc))
				return 2;
			else if (t == typeof(LStream))
				return 8;
			else if (t == typeof(Labeldesc))
				return 8;
			Debug.Assert(false, "Trying to get unknown sized of unmanaged type " + t.ToString());
			return 0;
		}
		
		public static void exit(int exitCode)
		{
			Environment.Exit(exitCode);
		}
		
		public static void abort()
		{
			Environment.Exit(-1); //FIXME:???
		}
		
		public static lua_Number floor(lua_Number a)
		{
			return Math.Floor(a);
		}
		
		public static double log(double d)
		{
			return Math.Log(d);
		}

		public static double log10(double d)
		{
			return Math.Log10(d);
		}	

		public static StreamProxy _popen(CharPtr command, CharPtr type)
		{
			//FIXME:not implemented
			return null;
		}
		
		public static int _pclose(StreamProxy stream)
		{
			//FIXME:not implemented
			return 0;
		}
		
		public const byte UCHAR_MAX = System.Byte.MaxValue;
		
		//from https://github.com/xanathar/moonsharp/blob/master/src/MoonSharp.Interpreter/Interop/LuaStateInterop/LuaBase_CLib.cs
		public static bool isgraph(char c) { return !Char.IsControl(c) && !Char.IsWhiteSpace(c); }
		public static bool isgraph(int c) { return !Char.IsControl((char)c) && !Char.IsWhiteSpace((char)c); }
		
		public static int system(CharPtr cmd) 
		{
			CharPtr strCmdLine = "/C regenresx " + cmd;
			System.Diagnostics.Process proc = new System.Diagnostics.Process();
			proc.EnableRaisingEvents=false;
			proc.StartInfo.FileName = "CMD.exe";
			proc.StartInfo.Arguments = strCmdLine.ToString();
			proc.Start();
			proc.WaitForExit();
			return proc.ExitCode;
		}
		
		public static int remove(CharPtr filename)
		{
		  	int result = 1;
		  	try 
		  	{
		  		File.Delete(filename.ToString());
		  	} 
		  	catch 
		  	{
		  		result = 0;
		  	}
		  	return result;
		}
		
		public static int rename(CharPtr fromname, CharPtr toname)
		{
			int result;
			try
			{
				File.Move(fromname.ToString(), toname.ToString());
				result = 0;
			}
			catch
			{
				result = 1; // todo: this should be a proper error code
			}
		  	return result;
		}
		
		public const int L_tmpnam = 16;
		public static CharPtr tmpnam(CharPtr name) 
		{
			return strcpy(name, Path.GetTempFileName());
		}
		
		private const string number_chars = "0123456789+-eE.";
		public static double strtod(CharPtr s, out CharPtr end)
		{
			end = new CharPtr(s.chars, s.index);
			string str = "";
			while (end[0] == ' ')
				end = end.next();
			while (number_chars.IndexOf(end[0]) >= 0)
			{
				str += end[0];
				end = end.next();
			}

			try
			{
				return Convert.ToDouble(str.ToString());
			}
			catch (System.OverflowException)
			{
				// this is a hack, fix it - mjf
				if (str[0] == '-')
					return System.Double.NegativeInfinity;
				else
					return System.Double.PositiveInfinity;
			}
			catch
			{
				end = new CharPtr(s.chars, s.index);
				return 0;
			}
		}
		
		public static uint strspn (CharPtr s, CharPtr accept)
		{
			int p;//s
		    int a;//accept
		    uint count = 0;
		    for (p = 0; s[p] != '\0'; ++p)
		    {
		    	for (a = 0; accept[a] != '\0'; ++a)
		        {
		    		if (s[p] == accept[a])
		            {
		                ++count;
		                break;
		            }
		        }
		    	if (accept[a] == '\0')
		        {
		            return count;
		        }
		    }
		    return count;
		}
		
		public const clock_t CLOCKS_PER_SEC = (clock_t)1000;
		public static clock_t clock()
		{
			long ticks = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
			return ticks;
		}
		
		public static int fflush(StreamProxy stream)
		{
			return stream.flush();
		}
		
		public class lconv 
		{
			public CharPtr decimal_point;
			
			public lconv()
			{
				decimal_point = ".";
			}
		}
		public static lconv _lconv = new lconv();
		public static lconv localeconv()
		{
			return _lconv;
		}
	}
}

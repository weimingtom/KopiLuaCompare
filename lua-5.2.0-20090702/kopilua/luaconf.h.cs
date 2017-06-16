/*
** $Id: luaconf.h,v 1.105 2009/06/18 18:19:36 roberto Exp roberto $
** Configuration file for Lua
** See Copyright Notice in lua.h
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace KopiLua
{
	using LUA_INTEGER	= System.Int32;
	using LUA_NUMBER	= System.Double;
	using LUAI_UACNUMBER	= System.Double;
	using LUA_INTFRM_T		= System.Int64;
	using TValue = Lua.lua_TValue;
	using lua_Number = System.Double;

	public partial class Lua
	{
		/*
		** ==================================================================
		** Search for "@@" to find all configurable definitions.
		** ===================================================================
		*/


		/*
		@@ LUA_ANSI controls the use of non-ansi features.
		** CHANGE it (define it) if you want Lua to avoid the use of any
		** non-ansi feature or library.
		*/
		//#if !defined(LUA_ANSI) && defined(__STRICT_ANSI__)
		//#define LUA_ANSI
		//#endif


		//#if !defined(LUA_ANSI) && _WIN32
		//#define LUA_WIN
		//#endif

		//#if defined(LUA_WIN)
		//#include <windows.h>
		//#endif



		//#if defined(LUA_USE_LINUX)
		//#define LUA_USE_POSIX
		//#define LUA_USE_DLOPEN		/* needs an extra library: -ldl */
		//#define LUA_USE_READLINE	/* needs some extra libraries */
		//#endif

		//#if defined(LUA_USE_MACOSX)
		//#define LUA_USE_POSIX
		//#define LUA_DL_DYLD		/* does not need extra library */
		//#endif



		/*
		@@ LUA_USE_POSIX includes all functionallity listed as X/Open System
		@* Interfaces Extension (XSI).
		** CHANGE it (define it) if your system is XSI compatible.
		*/
		//#if defined(LUA_USE_POSIX)
		//#define LUA_USE_MKSTEMP
		//#define LUA_USE_ISATTY
		//#define LUA_USE_POPEN
		//#define LUA_USE_ULONGJMP
		//#endif


		/*
		@@ LUA_PATH and LUA_CPATH are the names of the environment variables that
		@* Lua check to set its paths.
		@@ LUA_INIT is the name of the environment variable that Lua
		@* checks for initialization code.
		** CHANGE them if you want different names.
		*/
		public const string LUA_PATH = "LUA_PATH";
		public const string LUA_CPATH = "LUA_CPATH";
		public const string LUA_INIT = "LUA_INIT";


		/*
		@@ LUA_PATH_DEFAULT is the default path that Lua uses to look for
		@* Lua libraries.
		@@ LUA_CPATH_DEFAULT is the default path that Lua uses to look for
		@* C libraries.
		** CHANGE them if your machine has a non-conventional directory
		** hierarchy or if you want to install your libraries in
		** non-conventional directories.
		*/
		#if _WIN32
		/*
		** In Windows, any exclamation mark ('!') in the path is replaced by the
		** path of the directory of the executable file of the current process.
		*/
		public const string LUA_LDIR = "!\\lua\\";
		public const string LUA_CDIR = "!\\";
		public const string LUA_PATH_DEFAULT =
							LUA_LDIR + "?.lua;"  + LUA_LDIR + "?\\init.lua;"
							 + LUA_CDIR + "?.lua;"  + LUA_CDIR + "?\\init.lua;" + ".\\?.lua";
		public const string LUA_CPATH_DEFAULT =
							LUA_CDIR + "?.dll;" + LUA_CDIR + "loadall.dll;" + ".\\?.dll";

		#else
		public const string LUA_ROOT	= "/usr/local/";
		public const string LUA_LDIR	= LUA_ROOT + "share/lua/5.1/";
		public const string LUA_CDIR	= LUA_ROOT + "lib/lua/5.1/";
		public const string LUA_PATH_DEFAULT  =
							LUA_LDIR + "?.lua;"  + LUA_LDIR + "?/init.lua;" +
							LUA_CDIR + "?.lua;"  + LUA_CDIR + "?/init.lua;" + "./?.lua";
		public const string LUA_CPATH_DEFAULT =
							LUA_CDIR + "?.so;" + LUA_CDIR + "loadall.so;" + "./?.so";
#endif


		/*
		@@ LUA_DIRSEP is the directory separator (for submodules).
		** CHANGE it if your machine does not use "/" as the directory separator
		** and is not Windows. (On Windows Lua automatically uses "\".)
		*/
		#if _WIN32
		public const string LUA_DIRSEP = "\\";
		#else
		public const string LUA_DIRSEP = "/";
#endif


		/*
		@@ LUA_PATHSEP is the character that separates templates in a path.
		@@ LUA_PATH_MARK is the string that marks the substitution points in a
		@* template.
		@@ LUA_EXECDIR in a Windows path is replaced by the executable's
		@* directory.
		@@ LUA_IGMARK is a mark to ignore all before it when bulding the
		@* luaopen_ function name.
		** CHANGE them if for some reason your system cannot use those
		** characters. (E.g., if one of those characters is a common character
		** in file/directory names.) Probably you do not need to change them.
		*/
		public const string LUA_PATHSEP = ";";
		public const string LUA_PATH_MARK = "?";
		public const string LUA_EXECDIR = "!";
		public const string LUA_IGMARK = "-";


		/*
		@@ LUA_INTEGER is the integral type used by lua_pushinteger/lua_tointeger.
		** CHANGE that if ptrdiff_t is not adequate on your machine. (On most
		** machines, ptrdiff_t gives a good choice between int or long.)
		*/
		//#define LUA_INTEGER	ptrdiff_t


		/*
		@@ LUA_API is a mark for all core API functions.
		@@ LUALIB_API is a mark for all standard library functions.
		** CHANGE them if you need to define those functions in some special way.
		** For instance, if you want to create one Windows DLL with the core and
		** the libraries, you may want to use the following definition (define
		** LUA_BUILD_AS_DLL to get it).
		*/
		//#if LUA_BUILD_AS_DLL

		//#if defined(LUA_CORE) || defined(LUA_LIB)
		//#define LUA_API __declspec(dllexport)
		//#else
		//#define LUA_API __declspec(dllimport)
		//#endif

		//#else

		//#define LUA_API		extern

		//#endif

		/* more often than not the libs go together with the core */
		//#define LUALIB_API	LUA_API


		/*
		@@ LUAI_FUNC is a mark for all extern functions that are not to be
		@* exported to outside modules.
		@@ LUAI_DATA is a mark for all extern (const) variables that are not to
		@* be exported to outside modules.
		** CHANGE them if you need to mark them in some special way. Elf/gcc
		** (versions 3.2 and later) mark them as "hidden" to optimize access
		** when Lua is compiled as a shared library. Not all elf targets support
		** this attribute. Unfortunately, gcc does not offer a way to check
		** whether the target offers that support, and those without support
		** give a warning about it. To avoid these warnings, change to the
		** default definition.
		*/
		//#if defined(luaall_c)
		//#define LUAI_FUNC	static
		//#define LUAI_DATA	/* empty */

		//#elif defined(__GNUC__) && ((__GNUC__*100 + __GNUC_MINOR__) >= 302) && \
		//      defined(__ELF__)
		//#define LUAI_FUNC	__attribute__((visibility("hidden"))) extern
		//#define LUAI_DATA	LUAI_FUNC

		//#else
		//#define LUAI_FUNC	extern
		//#define LUAI_DATA	extern
		//#endif



		/*
		@@ LUA_QL describes how error messages quote program elements.
		** CHANGE it if you want a different appearance.
		*/
		public static CharPtr LUA_QL(string x)	{return "'" + x + "'";}
		public static CharPtr LUA_QS {get {return LUA_QL("%s"); }}


		/*
		@@ LUA_IDSIZE gives the maximum size for the description of the source
		@* of a function in debug information.
		** CHANGE it if you want a different size.
		*/
		public const int LUA_IDSIZE	= 60;


		/*
		@@ luai_writestring defines how 'print' prints its results.
		** CHANGE it if your system does not have a useful stdout.
		*/
		public static void luai_writestring(CharPtr s, int l) { fwrite(s, 1/*sizeof(char)*/, l, stdout); }


		/*
		** {==================================================================
		** Stand-alone configuration
		** ===================================================================
		*/

		//#if lua_c || luaall_c

		/*
		@@ lua_stdin_is_tty detects whether the standard input is a 'tty' (that
		@* is, whether we're running lua interactively).
		** CHANGE it if you have a better definition for non-POSIX/non-Windows
		** systems.
		*/
		#if LUA_USE_ISATTY
		//#include <unistd.h>
		//#define lua_stdin_is_tty()	isatty(0)
		#elif LUA_WIN
		//#include <io.h>
		//#include <stdio.h>
		//#define lua_stdin_is_tty()	_isatty(_fileno(stdin))
		#else
		public static int lua_stdin_is_tty() { return 1; }  /* assume stdin is a tty */
		#endif


		/*
		@@ LUA_PROMPT is the default prompt used by stand-alone Lua.
		@@ LUA_PROMPT2 is the default continuation prompt used by stand-alone Lua.
		** CHANGE them if you want different prompts. (You can also change the
		** prompts dynamically, assigning to globals _PROMPT/_PROMPT2.)
		*/
		public const string LUA_PROMPT		= "> ";
		public const string LUA_PROMPT2		= ">> ";


		/*
		@@ LUA_PROGNAME is the default name for the stand-alone Lua program.
		** CHANGE it if your stand-alone interpreter has a different name and
		** your system is not able to detect that name automatically.
		*/
		public const string LUA_PROGNAME		= "lua";


		/*
		@@ LUA_MAXINPUT is the maximum length for an input line in the
		@* stand-alone interpreter.
		** CHANGE it if you need longer lines.
		*/
		public const int LUA_MAXINPUT	= 512;


		/*
		@@ lua_readline defines how to show a prompt and then read a line from
		@* the standard input.
		@@ lua_saveline defines how to "save" a read line in a "history".
		@@ lua_freeline defines how to free a line read by lua_readline.
		** CHANGE them if you want to improve/adapt this functionality.
		*/
#if LUA_USE_READLINE
		//#include <stdio.h>
		//#include <readline/readline.h>
		//#include <readline/history.h>
		//#define lua_readline(L,b,p)	((void)L, ((b)=readline(p)) != null)
		//#define lua_saveline(L,idx) \
		//	if (lua_objlen(L,idx) > 0)  /* non-empty line? */ \
		//	  add_history(lua_tostring(L, idx));  /* add it to history */
		//#define lua_freeline(L,b)	((void)L, free(b))
#else
		public static bool lua_readline(lua_State L, CharPtr b, CharPtr p)
		{
			fputs(p, stdout);
			fflush(stdout);		/* show prompt */
			return (fgets(b, stdin) != null);  /* get line */
		}
		public static void lua_saveline(lua_State L, int idx)	{}
		public static void lua_freeline(lua_State L, CharPtr b)	{}
#endif

//#endif

		/* }================================================================== */


		/*
		@@ LUAI_GCPAUSE defines the default pause between garbage-collector cycles
		@* as a percentage.
		** CHANGE it if you want the GC to run faster or slower (higher values
		** mean larger pauses which mean slower collection.) You can also change
		** this value dynamically.
		*/
		public const int LUAI_GCPAUSE	= 162;  /* 162% (wait memory to double before next GC) */


		/*
		@@ LUAI_GCMUL defines the default speed of garbage collection relative to
		@* memory allocation as a percentage.
		** CHANGE it if you want to change the granularity of the garbage
		** collection. (Higher values mean coarser collections. 0 represents
		** infinity, where each step performs a full collection.) You can also
		** change this value dynamically.
		*/
		public const int LUAI_GCMUL	= 200; /* GC runs 'twice the speed' of memory allocation */



		/*
		** {==================================================================
		** Compatibility with previous versions
		** ===================================================================
		*/

		/*
		@@ LUA_COMPAT_LOG10 defines the function 'log10' in the math library.
		** CHANGE it (undefine it) if as soon as you rewrite all calls 'log10(x)'
		** as 'log(x, 10)'
		*/
		//#define LUA_COMPAT_LOG10 //FIXME:???

		/*
		@@ LUA_COMPAT_API includes some macros and functions that supply some
		@* compatibility with previous versions.
		** CHANGE it (undefine it) if you do not need these compatibility facilities.
		*/
		//#define LUA_COMPAT_API		


		/*
		@@ LUA_COMPAT_GFIND controls compatibility with old 'string.gfind' name.
		** CHANGE it to undefined as soon as you rename 'string.gfind' to
		** 'string.gmatch'.
		*/
		//#define LUA_COMPAT_GFIND /* defined higher up */

		/*
		@@ LUA_COMPAT_DEBUGLIB controls compatibility with preloading
		@* the debug library.
		** CHANGE it to undefined as soon as you add 'require"debug"' everywhere
		** you need the debug library.
		*/
		//#define LUA_COMPAT_DEBUGLIB /* defined higher up */

		/* }================================================================== */




		/*
		@@ luai_apicheck is the assert macro used by the Lua-C API.
		** CHANGE luai_apicheck if you want Lua to perform some checks in the
		** parameters it gets from API calls. This may slow down the interpreter
		** a bit, but may be quite useful when debugging C code that interfaces
		** with Lua. A useful redefinition is to use assert.h.
		*/
		#if LUA_USE_APICHECK
			public static void luai_apicheck(lua_State L, bool o)	{Debug.Assert(o);}
			public static void luai_apicheck(lua_State L, int o) {Debug.Assert(o != 0);}
		#else
			public static void luai_apicheck(lua_State L, bool o)	{}
			public static void luai_apicheck(lua_State L, int o) { }
		#endif


		/*
		@@ LUAI_BITSINT defines the number of bits in an int.
		** CHANGE here if Lua cannot automatically detect the number of bits of
		** your machine. Probably you do not need to change this.
		*/
		/* avoid overflows in comparison */
		//#if INT_MAX-20 < 32760
		//public const int LUAI_BITSINT	= 16
		//#elif INT_MAX > 2147483640L
		/* int has at least 32 bits */
		public const int LUAI_BITSINT	= 32;
		//#else
		//#error "you must define LUA_BITSINT with number of bits in an integer"
		//#endif


		/*
		@@ LUA_INT32 is an signed integer with exactly 32 bits.
		@@ LUAI_UMEM is an unsigned integer big enough to count the total
		@* memory used by Lua.
		@@ LUAI_MEM is a signed integer big enough to count the total memory
		@* used by Lua.
		** CHANGE here if for some weird reason the default definitions are not
		** good enough for your machine.  Probably you do not need to change
		** this.
		*/
		//#if LUAI_BITSINT >= 32
		//#define LUA_INT32	int
		//#define LUAI_UMEM	uint
		//#define LUAI_MEM	ptrdiff_t
		//#else
		///* 16-bit ints */
		//#define LUA_INT32	long
		//#define LUAI_UMEM	unsigned long
		//#define LUAI_MEM	long
		//#endif


		/*
		@@ LUAI_MAXCALLS limits the number of nested calls.
		** CHANGE it if you need really deep recursive calls. This limit is
		** arbitrary; its only purpose is to stop infinite recursion before
		** exhausting memory.
		*/
		public const int LUAI_MAXCALLS	= 20000;


		/*
		@@ LUAI_MAXCSTACK limits the number of Lua stack slots that a C function
		@* can use.
		** CHANGE it if you need a different limit. This limit is arbitrary;
		** its only purpose is to stop C functions to consume unlimited stack
		** space.
		*/
		/* life is simpler if stack size fits in an int (16 is an estimate
		   for the size of a Lua value) */
		//#if SHRT_MAX < (INT_MAX / 16)
		//#define LUAI_MCS_AUX	SHRT_MAX
		//#else
		//#define LUAI_MCS_AUX	(INT_MAX / 16)
		//#endif
        //FIXME:here changed
        public const int LUAI_MCS_AUX = SHRT_MAX < (Int32.MaxValue / 16) ? (int)SHRT_MAX : (Int32.MaxValue / 16);

		/* reserve some space for pseudo-indices */
		public const int LUAI_MAXCSTACK = (LUAI_MCS_AUX - 1000);



		/*
		** {==================================================================
		** CHANGE (to smaller values) the following definitions if your system
		** has a small C stack. (Or you may want to change them to larger
		** values if your system has a large C stack and these limits are
		** too rigid for you.) Some of these constants control the size of
		** stack-allocated arrays used by the compiler or the interpreter, while
		** others limit the maximum number of recursive calls that the compiler
		** or the interpreter can perform. Values too large may cause a C stack
		** overflow for some forms of deep constructs.
		** ===================================================================
		*/


		/*
		@@ LUAI_MAXCCALLS is the maximum depth for nested C calls (short) and
		@* syntactical nested non-terminals in a program.
		*/
		public const int LUAI_MAXCCALLS		= 200;


		/*
		@@ LUAI_MAXVARS is the maximum number of local variables per function
		@* (must be smaller than 250).
		*/
		public const int LUAI_MAXVARS		= 200;


		/*
		@@ LUAI_MAXUPVALUES is the maximum number of upvalues per function
		@* (must be smaller than 250).
		*/
		public const int LUAI_MAXUPVALUES	= 60;


		/*
		@@ LUAL_BUFFERSIZE is the buffer size used by the lauxlib buffer system.
		*/
		public const int LUAL_BUFFERSIZE		= 1024; // BUFSIZ; todo: check this - mjf

		/* }================================================================== */




		/*
		** {==================================================================
		@@ LUA_NUMBER is the type of numbers in Lua.
		** CHANGE the following definitions only if you want to build Lua
		** with a number type different from double. You may also need to
		** change lua_number2int & lua_number2integer.
		** ===================================================================
		*/

		//#define LUA_NUMBER_DOUBLE
		//#define LUA_NUMBER	double	/* declared in dotnet build with using statement */

		/*
		@@ LUAI_UACNUMBER is the result of an 'usual argument conversion'
		@* over a number.
		*/
		//#define LUAI_UACNUMBER	double /* declared in dotnet build with using statement */


		/*
		@@ LUA_NUMBER_SCAN is the format for reading numbers.
		@@ LUA_NUMBER_FMT is the format for writing numbers.
		@@ lua_number2str converts a number to a string.
		@@ LUAI_MAXNUMBER2STR is maximum size of previous conversion.
		@@ lua_str2number converts a string to a number.
		*/
		public const string LUA_NUMBER_SCAN = "%lf";
		public const string LUA_NUMBER_FMT = "%.14g";
		public static CharPtr lua_number2str(double n) { return String.Format("{0}", n); }
		public const int LUAI_MAXNUMBER2STR = 32; /* 16 digits, sign, point, and \0 */

		private const string number_chars = "0123456789+-eE.";
		public static double lua_str2number(CharPtr s, out CharPtr end)
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

		/*
		@@ The luai_num* macros define the primitive operations over numbers.
		*/
		#if LUA_CORE
		//#include <math.h>
		public delegate lua_Number op_delegate(lua_State L, lua_Number a, lua_Number b);
		public static lua_Number luai_numadd(lua_State L, lua_Number a, lua_Number b) { return ((a) + (b)); }
		public static lua_Number luai_numsub(lua_State L, lua_Number a, lua_Number b) { return ((a) - (b)); }
		public static lua_Number luai_nummul(lua_State L, lua_Number a, lua_Number b) { return ((a) * (b)); }
		public static lua_Number luai_numdiv(lua_State L, lua_Number a, lua_Number b) { return ((a) / (b)); }
		public static lua_Number luai_nummod(lua_State L, lua_Number a, lua_Number b) { return ((a) - Math.Floor((a) / (b)) * (b)); }
		public static lua_Number luai_numpow(lua_State L, lua_Number a, lua_Number b) { return (Math.Pow(a, b)); }
		public static lua_Number luai_numunm(lua_State L, lua_Number a) { return (-(a)); }
		public static bool luai_numeq(lua_Number a, lua_Number b) { return ((a) == (b)); }
		public static bool luai_numlt(lua_State L, lua_Number a, lua_Number b) { return ((a) < (b)); }
		public static bool luai_numle(lua_State L, lua_Number a, lua_Number b) { return ((a) <= (b)); }
		public static bool luai_numisnan(lua_State L, lua_Number a) { return lua_Number.IsNaN(a); }
		#endif

		/*
		@@ LUA_INTEGER is the integral type used by lua_pushinteger/lua_tointeger.
		** CHANGE that if ptrdiff_t is not adequate on your machine. (On most
		** machines, ptrdiff_t gives a good choice between int or long.)
		*/
		//#define LUA_INTEGER	ptrdiff_t


		/*
		@@ lua_number2int is a macro to convert lua_Number to int.
		@@ lua_number2integer is a macro to convert lua_Number to lUA_INTEGER.
		@@ lua_number2uint is a macro to convert a lua_Number to an unsigned
		@* LUA_INT32.
		@@ lua_uint2number is a macro to convert an unsigned LUA_INT32
		@* to a lua_Number.
		** CHANGE them if you know a faster way to convert a lua_Number to
		** int (with any rounding method and without throwing errors) in your
		** system. In Pentium machines, a naive typecast from double to int
		** in C is extremely slow, so any alternative is worth trying.
		*/

		/* On a Pentium, resort to a trick */
		//#if defined(LUA_NUMBER_DOUBLE) && !defined(LUA_ANSI) && !defined(__SSE2__) && \
		//	(defined(__i386) || defined (_M_IX86) || defined(__i386__))

		/* On a Microsoft compiler, use assembler */
		//#if defined(_MSC_VER)

		//#define lua_number2int(i,d)   __asm fld d   __asm fistp i
		//#define lua_number2integer(i,n)		lua_number2int(i, n)

        //#else
		/* the next trick should work on any Pentium, but sometimes clashes
		   with a DirectX idiosyncrasy */

		//union luai_Cast { double l_d; long l_l; };
		//#define lua_number2int(i,d) \
		//  { volatile union luai_Cast u; u.l_d = (d) + 6755399441055744.0; (i) = u.l_l; }
		//#define lua_number2integer(i,n)		lua_number2int(i, n)
        //#define lua_number2uint(i,n)		lua_number2int(i, n)

		//#endif


		//#else
		/* this option always works, but may be slow */
		//#define lua_number2int(i,d)	((i)=(int)(d))
		//#define lua_number2integer(i,d)	((i)=(LUA_INTEGER)(d))
        //#define lua_number2uint(i,d)	((i)=(unsigned LUA_INT32)(d))

		//#endif


		/* on several machines, coercion from unsigned to double is too slow,
		   so avoid that if possible */
		public static lua_Number lua_uint2number(uint u) {
			return ((LUA_INT32)(u) < 0 ? (lua_Number)(u) : (lua_Number)(LUA_INT32)(u)); }

		private static void lua_number2int(out int i,lua_Number d)   {i = (int)d;}
		private static void lua_number2integer(out int i, lua_Number n) { i = (int)n; }

		/* }================================================================== */


		/*
		@@ LUAI_USER_ALIGNMENT_T is a type that requires maximum alignment.
		** CHANGE it if your system requires alignments larger than double. (For
		** instance, if your system supports long doubles and they must be
		** aligned in 16-byte boundaries, then you should add long double in the
		** union.) Probably you do not need to change this.
		*/
		//#define LUAI_USER_ALIGNMENT_T	union { double u; void *s; long l; }

		public class LuaException : Exception
		{
			public lua_State L;
			public lua_longjmp c;

			public LuaException(lua_State L, lua_longjmp c) { this.L = L; this.c = c; }
		}

		/*
		@@ LUAI_THROW/LUAI_TRY define how Lua does exception handling.
		** CHANGE them if you prefer to use longjmp/setjmp even with C++
		** or if want/don't to use _longjmp/_setjmp instead of regular
		** longjmp/setjmp. By default, Lua handles errors with exceptions when
		** compiling as C++ code, with _longjmp/_setjmp when asked to use them,
		** and with longjmp/setjmp otherwise.
		*/
		//#if defined(__cplusplus)
		///* C++ exceptions */
		public static void LUAI_THROW(lua_State L, lua_longjmp c)	{throw new LuaException(L, c);}
		//#define LUAI_TRY(L,c,a)	try { a } catch(...) \
		//    { if ((c).status == 0) (c).status = -1; }
		public static void LUAI_TRY(lua_State L, lua_longjmp c, object a) {
			if (c.status == 0) c.status = -1;
		}
		//#define luai_jmpbuf	int  /* dummy variable */

		//#elif defined(LUA_USE_ULONGJMP)
		///* in Unix, try _longjmp/_setjmp (more efficient) */
		//#define LUAI_THROW(L,c)	_longjmp((c).b, 1)
		//#define LUAI_TRY(L,c,a)	if (_setjmp((c).b) == 0) { a }
		//#define luai_jmpbuf	jmp_buf

		//#else
		///* default handling with long jumps */
		//public static void LUAI_THROW(lua_State L, lua_longjmp c) { c.b(1); }
		//#define LUAI_TRY(L,c,a)	if (setjmp((c).b) == 0) { a }
		//#define luai_jmpbuf	jmp_buf

		//#endif


		/*
		@@ LUA_MAXCAPTURES is the maximum number of captures that a pattern
		@* can do during pattern-matching.
		** CHANGE it if you need more captures. This limit is arbitrary.
		*/
		public const int LUA_MAXCAPTURES		= 32;


		/*
		@@ lua_tmpnam is the function that the OS library uses to create a
		@* temporary name.
		@@ LUA_TMPNAMBUFSIZE is the maximum size of a name created by lua_tmpnam.
		** CHANGE them if you have an alternative to tmpnam (which is considered
		** insecure) or if you want the original tmpnam anyway.  By default, Lua
		** uses tmpnam except when POSIX is available, where it uses mkstemp.
		*/
		#if loslib_c || luaall_c

		#if LUA_USE_MKSTEMP
		//#include <unistd.h>
		public const int LUA_TMPNAMBUFSIZE	= 32;
		//#define lua_tmpnam(b,e)	{ \
		//    strcpy(b, "/tmp/lua_XXXXXX"); \
		//    e = mkstemp(b); \
		//    if (e != -1) close(e); \
		//    e = (e == -1); }

		#else
			public const int LUA_TMPNAMBUFSIZE	= L_tmpnam;
			public static void lua_tmpnam(CharPtr b, int e)		{ e = (tmpnam(b) == null) ? 1 : 0; }
		#endif

		#endif



		/*
		@@ LUA_STRFTIMEOPTIONS is the list of valid conversion specifier
		@* characters for the 'strftime' function;
		@@ LUA_STRFTIMEPREFIX is the list of valid modifiers for
		@* that function.
		** CHANGE them if you want to use non-ansi options specific to your system.
		*/
		public const string LUA_STRFTIMEOPTIONS	= "aAbBcdHIjmMpSUwWxXyYz%";
		public const string LUA_STRFTIMEPREFIX = "";

		/*
		@@ lua_popen spawns a new process connected to the current one through
		@* the file streams.
		** CHANGE it if you have a way to implement it in your system.
		*/
		//#if LUA_USE_POPEN

		//#define lua_popen(L,c,m)	((void)L, fflush(NULL), popen(c,m))
		//#define lua_pclose(L,file)	((void)L, pclose(file))

		//#elif LUA_WIN

		//#define lua_popen(L,c,m)	((void)L, _popen(c,m))
		//#define lua_pclose(L,file)	((void)L, _pclose(file))

		//#else

		public static Stream lua_popen(lua_State L, CharPtr c, CharPtr m) { luaL_error(L, LUA_QL("popen") + " not supported"); return null; }
		public static int lua_pclose(lua_State L, Stream file) { return -1; }
	
		//#endif

		/*
		@@ LUA_DL_* define which dynamic-library system Lua should use.
		** CHANGE here if Lua has problems choosing the appropriate
		** dynamic-library system for your platform (either Windows' DLL, Mac's
		** dyld, or Unix's dlopen). If your system is some kind of Unix, there
		** is a good chance that it has dlopen, so LUA_DL_DLOPEN will work for
		** it.  To use dlopen you also need to adapt the src/Makefile (probably
		** adding -ldl to the linker options), so Lua does not select it
		** automatically.  (When you change the makefile to add -ldl, you must
		** also add -DLUA_USE_DLOPEN.)
		** If you do not want any kind of dynamic library, undefine all these
		** options.
		** By default, _WIN32 gets LUA_DL_DLL and MAC OS X gets LUA_DL_DYLD.
		*/
		//#if LUA_USE_DLOPEN
		//#define LUA_DL_DLOPEN
		//#endif

		//#if LUA_WIN
		//#define LUA_DL_DLL
		//#endif


		/*
		@@ LUAI_EXTRASPACE allows you to add user-specific data in a lua_State
		@* (the data goes just *before* the lua_State pointer).
		** CHANGE (define) this if you really need that. This value must be
		** a multiple of the maximum alignment required for your machine.
		*/
		public const int LUAI_EXTRASPACE		= 0;


		/*
		@@ luai_userstate* allow user-specific actions on threads.
		** CHANGE them if you defined LUAI_EXTRASPACE and need to do something
		** extra when a thread is created/deleted/resumed/yielded.
		*/
		public static void luai_userstateopen(lua_State L)					{}
		public static void luai_userstateclose(lua_State L)					{}
		public static void luai_userstatethread(lua_State L, lua_State L1)	{}
		public static void luai_userstatefree(lua_State L)					{}
		public static void luai_userstateresume(lua_State L,int n)			{}
		public static void luai_userstateyield(lua_State L,int n)			{}


		/*
		@@ LUA_INTFRMLEN is the length modifier for integer conversions
		@* in 'string.format'.
		@@ LUA_INTFRM_T is the integer type correspoding to the previous length
		@* modifier.
		** CHANGE them if your system supports long long or does not support long.
		*/

		#if LUA_USELONGLONG

		public const string LUA_INTFRMLEN		= "ll";
		//#define LUA_INTFRM_T		long long

		#else

		public const string LUA_INTFRMLEN = "l";
		//#define LUA_INTFRM_T		long			/* declared in dotnet build with using statement */

		#endif



		/* =================================================================== */

		/*
		** Local configuration. You can use this space to add your redefinitions
		** without modifying the main part of the file.
		*/


	}
}

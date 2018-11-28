/*
** $Id: luaconf.h,v 1.131 2010/01/21 16:31:24 roberto Exp roberto $
** Configuration file for Lua
** See Copyright Notice in lua.h
*/

using System;
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
	using LUA_INT32 = System.Int32;

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
		//#define LUA_USE_DLOPEN
		//#define LUA_USE_READLINE	/* needs some extra libraries */
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
		public const string LUA_LDIR	= LUA_ROOT + "share/lua/5.2/";
		public const string LUA_CDIR	= LUA_ROOT + "lib/lua/5.2/";
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
		@@ LUA_API is a mark for all core API functions.
		@@ LUALIB_API is a mark for all auxiliary library functions.
		@@ LUAMOD_API is a mark for all standard library opening functions.
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
		//#define LUAMOD_API	LUALIB_API


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
		//#define LUAI_DDEC	static
		//#define LUAI_DDEF	static

		//#elif defined(__GNUC__) && ((__GNUC__*100 + __GNUC_MINOR__) >= 302) && \
		//      defined(__ELF__)
		//#define LUAI_FUNC	__attribute__((visibility("hidden"))) extern
		//#define LUAI_DDEC	LUAI_FUNC
		//#define LUAI_DDEF	/* empty */

		//#else
		//#define LUAI_FUNC	extern
		//#define LUAI_DDEC	extern
		//#define LUAI_DDEF	/* empty */
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
		public static void luai_writestring(CharPtr s, uint l) { fwrite(s, 1/*sizeof(char)*/, (int)l, stdout); }





		/*
		** {==================================================================
		** Compatibility with previous versions
		** ===================================================================
		*/
        //FIXME:TODO:LUA_COMPAT_ALL is defined, but all defines removed here
		/*
		@@ LUA_COMPAT_ALL controls all compatibility options.
		** You can define it to get all options, or change specific options
		** to fit your specific needs.
		*/
		#if LUA_COMPAT_ALL

		/*
		@@ LUA_COMPAT_UNPACK controls the presence of global 'unpack'.
		** You can replace it with 'table.unpack'.
		*/
		//#define LUA_COMPAT_UNPACK

		/*
		@@ LUA_COMPAT_CPCALL controls the presence of macro 'lua_cpcall'.
		** You can replace it with the preregistered function 'cpcall'.
		*/
		//#define lua_cpcall(L,f,u)  \
		//	(lua_pushlightuserdata(L,(u)), luaL_cpcall(L,(f),1,0))

		/*
		@@ LUA_COMPAT_FENV controls the presence of functions 'setfenv/getfenv'.
		** You can replace them with lexical environments, 'loadin', or the
		** debug library.
		*/
		//#define LUA_COMPAT_FENV


		/*
		@@ LUA_COMPAT_LOG10 defines the function 'log10' in the math library.
		** You can rewrite 'log10(x)' as 'log(x, 10)'.
		*/
		//#define LUA_COMPAT_LOG10

		/*
		@@ LUA_COMPAT_MAXN defines the function 'maxn' in the table library.
		*/
		//#define LUA_COMPAT_MAXN

		/*
		@@ LUA_COMPAT_DEBUGLIB controls compatibility with preloading
		** the debug library.
		** You should add 'require"debug"' everywhere you need the debug
		** library.
		*/
		//#define LUA_COMPAT_DEBUGLIB

		/*
		@@ The following macros supply trivial compatibility for some
		** changes in the API. The macros themselves document how to
		** change your code to avoid using them.
		*/
		//#define lua_strlen(L,i)		lua_rawlen(L, (i))

		//#define lua_objlen(L,i)		lua_rawlen(L, (i))

		//#define lua_equal(L,idx1,idx2)	lua_compare(L,(idx1),(idx2),LUA_OPEQ)
		//#define lua_lessthan(L,idx1,idx2)	lua_compare(L,(idx1),(idx2),LUA_OPLT)

		/* compatibility with previous wrong spelling */
		//#define luaL_typerror		luaL_typeerror

		#endif

		/* }================================================================== */



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
		** good enough for your machine. Probably you do not need to change
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
		@@ LUAI_MAXSTACK limits the size of the Lua stack.
		** CHANGE it if you need a different limit. This limit is arbitrary;
		** its only purpose is to stop Lua to consume unlimited stack
		** space (and to reserve some numbers for pseudo-indices).
		*/
		//#if LUAI_BITSINT >= 32
		//#define LUAI_MAXSTACK		1000000
		//#else
		//#define LUAI_MAXSTACK		15000
		//#endif
        //FIXME:here changed
        public const int LUAI_MAXSTACK = LUAI_BITSINT >= 32 ? 1000000 : 15000;

		/* reserve some space for error handling */
		public const int LUAI_FIRSTPSEUDOIDX = (-LUAI_MAXSTACK - 1000);



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
		@@ LUAL_BUFFERSIZE is the buffer size used by the lauxlib buffer system.
		*/
		public const int LUAL_BUFFERSIZE		= 1024; // BUFSIZ; todo: check this - mjf
		//FIXME: changed here, = BUFSIZ;

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
		public static double lua_str2number(CharPtr s, out CharPtr p) { return strtod(s, out p); }


		/*
		@@ The luai_num* macros define the primitive operations over numbers.
		*/

		/* the following operations need the math library */
		//FIXME:???not defined
		//#if defined(lobject_c) || defined(lvm_c) || defined(luaall_c)
		//#include <math.h>
		public static lua_Number luai_nummod(lua_State L, lua_Number a, lua_Number b) { return ((a) - Math.Floor((a) / (b)) * (b)); }
		public static lua_Number luai_numpow(lua_State L, lua_Number a, lua_Number b) { return (Math.Pow(a, b)); }		
		//#endif

		/* these are quite standard operations */
		#if LUA_CORE
		public delegate lua_Number op_delegate(lua_State L, lua_Number a, lua_Number b); //FIXME:added ???
		public static lua_Number luai_numadd(lua_State L, lua_Number a, lua_Number b) { return ((a) + (b)); }
		public static lua_Number luai_numsub(lua_State L, lua_Number a, lua_Number b) { return ((a) - (b)); }
		public static lua_Number luai_nummul(lua_State L, lua_Number a, lua_Number b) { return ((a) * (b)); }
		public static lua_Number luai_numdiv(lua_State L, lua_Number a, lua_Number b) { return ((a) / (b)); }
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
		@@ lua_number2integer is a macro to convert lua_Number to LUA_INTEGER.
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

		//#define lua_number2int(i,d)   {__asm fld d   __asm fistp i}
		//#define lua_number2integer(i,n)		lua_number2int(i, n)
        //#define lua_number2uint(i,n)		lua_number2int(i, n)

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

        //FXIME:added ???
		private static void lua_number2int(out int i,lua_Number d)   {i = (int)d;}
		private static void lua_number2integer(out int i, lua_Number n) { i = (int)n; }


		/*
		@@ luai_hashnum is a macro do hash a lua_Number value into an integer.
		@* The hash must be deterministic and give reasonable values for
		@* both small and large values (outside the range of integers). 
		@* It is used only in ltable.c.
		*/

		//#if defined(ltable_c) || defined(luaall_c)

		//#include <float.h>
		//#include <math.h>

		public static void luai_hashnum(out int i, lua_Number d) { int e;
		  d = frexp(d, out e) * (lua_Number)(Int32.MaxValue - /*DBL_MAX_EXP*/Double.MaxValue); //FIXME:DBL_MAX_EXP==Double.MaxValue???
		  lua_number2int(out i, d); i += e; }

		//#endif

		/* }================================================================== */




		/* =================================================================== */

		/*
		** Local configuration. You can use this space to add your redefinitions
		** without modifying the main part of the file.
		*/


	}
}

/*
** $Id: luaconf.h,v 1.185 2013/06/25 19:04:40 roberto Exp $
** Configuration file for Lua
** See Copyright Notice in lua.h
*/
#define LUA_CORE
#define _WIN32
#define LUA_COMPAT_MOD

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
	using LUA_INT32 = System.Int32;
	using lua_Integer = System.Int32;

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


		//#if !defined(LUA_ANSI) && _WIN32 && !defined(_WIN32_WCE)
		//#define LUA_WIN		/* enable goodies for regular Windows platforms */
		//#endif

		//#if defined(LUA_WIN)
		//#define LUA_DL_DLL
		//#define LUA_USE_AFORMAT		/* assume 'printf' handles 'aA' specifiers */
		//#endif



		//#if defined(LUA_USE_LINUX)
		//#define LUA_USE_POSIX
		//#define LUA_USE_DLOPEN		/* needs an extra library: -ldl */
		//#define LUA_USE_READLINE	/* needs some extra libraries */
		//#define LUA_USE_STRTODHEX	/* assume 'strtod' handles hex formats */
		//#define LUA_USE_AFORMAT		/* assume 'printf' handles 'aA' specifiers */
		//#define LUA_USE_LONGLONG	/* assume support for long long */
		//#endif

		//#if defined(LUA_USE_MACOSX)
		//#define LUA_USE_POSIX
		//#define LUA_USE_DLOPEN    /* does not need -ldl */
		//#define LUA_USE_READLINE	/* needs an extra library: -lreadline */
		//#define LUA_USE_STRTODHEX	/* assume 'strtod' handles hex formats */
		//#define LUA_USE_AFORMAT		/* assume 'printf' handles 'aA' specifiers */
		//#define LUA_USE_LONGLONG	/* assume support for long long */
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
        //#define LUA_USE_GMTIME_R
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
		#if _WIN32 ///* { */
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

		#else			///* }{ */

		public const string LUA_VDIR    = LUA_VERSION_MAJOR + "." + LUA_VERSION_MINOR + "/";
		public const string LUA_ROOT	= "/usr/local/";
		public const string LUA_LDIR	= LUA_ROOT + "share/lua/" + LUA_VDIR;
		public const string LUA_CDIR	= LUA_ROOT + "lib/lua/" + LUA_VDIR;
		public const string LUA_PATH_DEFAULT  =
							LUA_LDIR + "?.lua;"  + LUA_LDIR + "?/init.lua;" +
							LUA_CDIR + "?.lua;"  + LUA_CDIR + "?/init.lua;" + "./?.lua";
		public const string LUA_CPATH_DEFAULT =
							LUA_CDIR + "?.so;" + LUA_CDIR + "loadall.so;" + "./?.so";
		#endif			///* } */


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
		@@ LUA_ENV is the name of the variable that holds the current
		@@ environment, used to access global names.
		** CHANGE it if you do not like this name.
		*/
		public const string LUA_ENV	= "_ENV";


		/*
		@@ LUA_API is a mark for all core API functions.
		@@ LUALIB_API is a mark for all auxiliary library functions.
		@@ LUAMOD_API is a mark for all standard library opening functions.
		** CHANGE them if you need to define those functions in some special way.
		** For instance, if you want to create one Windows DLL with the core and
		** the libraries, you may want to use the following definition (define
		** LUA_BUILD_AS_DLL to get it).
		*/
		//#if LUA_BUILD_AS_DLL	/* { */

		//#if defined(LUA_CORE) || defined(LUA_LIB)	/* { */
		//#define LUA_API __declspec(dllexport)
		//#else						/* }{ */
		//#define LUA_API __declspec(dllimport)
		//#endif						/* } */

		//#else				/* }{ */

		//#define LUA_API		extern

		//#endif				/* } */


		/* more often than not the libs go together with the core */
		//#define LUALIB_API	LUA_API
		//#define LUAMOD_API	LUALIB_API


		/*
		@@ LUAI_FUNC is a mark for all extern functions that are not to be
		@* exported to outside modules.
		@@ LUAI_DDEF and LUAI_DDEC are marks for all extern (const) variables
		@* that are not to be exported to outside modules (LUAI_DDEF for
		@* definitions and LUAI_DDEC for declarations).
		** CHANGE them if you need to mark them in some special way. Elf/gcc
		** (versions 3.2 and later) mark them as "hidden" to optimize access
		** when Lua is compiled as a shared library. Not all elf targets support
		** this attribute. Unfortunately, gcc does not offer a way to check
		** whether the target offers that support, and those without support
		** give a warning about it. To avoid these warnings, change to the
		** default definition.
		*/
		//#if defined(__GNUC__) && ((__GNUC__*100 + __GNUC_MINOR__) >= 302) && \
		//      defined(__ELF__)
		//#define LUAI_FUNC	__attribute__((visibility("hidden"))) extern
		//#define LUAI_DDEC	LUAI_FUNC
		//#define LUAI_DDEF	/* empty */

		//#else				/* }{ */
		//#define LUAI_FUNC	extern
		//#define LUAI_DDEC	extern
		//#define LUAI_DDEF	/* empty */
		//#endif				/* } */



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
		@@ luai_writestring/luai_writeline define how 'print' prints its results.
		** They are only used in libraries and the stand-alone program. (The #if
		** avoids including 'stdio.h' everywhere.)
		*/
        //#if defined(LUA_LIB) || defined(lua_c)
        //#include <stdio.h>
		public static void luai_writestring(CharPtr s, uint l) { fwrite(s, 1/*sizeof(char)*/, (int)l, stdout); }
		public static void luai_writeline() { luai_writestring("\n", 1); fflush(stdout); }
        //#endif

		/*
		@@ luai_writestringerror defines how to print error messages.
		** (A format string with one argument is enough for Lua...)
		*/
		public static void luai_writestringerror(CharPtr s, object p) {
			fprintf(stderr, s, p); fflush(stderr); }


		/*
		@@ LUAI_MAXSHORTLEN is the maximum length for short strings, that is,
		** strings that are internalized. (Cannot be smaller than reserved words
		** or tags for metamethods, as these strings must be internalized;
		** #("function") = 8, #("__newindex") = 10.)
		*/
		public const int LUAI_MAXSHORTLEN = 40;


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
		#if LUA_COMPAT_ALL	///* { */

		/*
		@@ LUA_COMPAT_UNPACK controls the presence of global 'unpack'.
		** You can replace it with 'table.unpack'.
		*/
		//#define LUA_COMPAT_UNPACK

		/*
		@@ LUA_COMPAT_LOADERS controls the presence of table 'package.loaders'.
		** You can replace it with 'package.searchers'.
		*/
		//#define LUA_COMPAT_LOADERS

		/*
		@@ macro 'lua_cpcall' emulates deprecated function lua_cpcall.
		** You can call your C function directly (with light C functions).
		*/
		public static void lua_cpcall(L,f,u)
			{lua_pushcfunction(L, (f)); 
			 lua_pushlightuserdata(L,(u));
			 return lua_pcall(L,1,0,0); }


		/*
		@@ LUA_COMPAT_LOG10 defines the function 'log10' in the math library.
		** You can rewrite 'log10(x)' as 'log(x, 10)'.
		*/
		//#define LUA_COMPAT_LOG10

		/*
		@@ LUA_COMPAT_LOADSTRING defines the function 'loadstring' in the base
		** library. You can rewrite 'loadstring(s)' as 'load(s)'.
		*/
		//#define LUA_COMPAT_LOADSTRING

		/*
		@@ LUA_COMPAT_MAXN defines the function 'maxn' in the table library.
		*/
		//#define LUA_COMPAT_MAXN

		/*
		@@ The following macros supply trivial compatibility for some
		** changes in the API. The macros themselves document how to
		** change your code to avoid using them.
		*/
		//#define lua_strlen(L,i)		lua_rawlen(L, (i))

		//#define lua_objlen(L,i)		lua_rawlen(L, (i))

		//#define lua_equal(L,idx1,idx2)	lua_compare(L,(idx1),(idx2),LUA_OPEQ)
		//#define lua_lessthan(L,idx1,idx2)	lua_compare(L,(idx1),(idx2),LUA_OPLT)

		/*
		@@ LUA_COMPAT_MODULE controls compatibility with previous
		** module functions 'module' (Lua) and 'luaL_register' (C).
		*/
		//#define LUA_COMPAT_MODULE

		#endif

		/* }================================================================== */



		/*
		@@ LUAI_BITSINT defines the number of bits in an int.
		** CHANGE here if Lua cannot automatically detect the number of bits of
		** your machine. Probably you do not need to change this.
		*/
		/* avoid overflows in comparison */
		//#if INT_MAX-20 < 32760		/* { */
		//public const int LUAI_BITSINT	= 16
		//#elif INT_MAX > 2147483640L	/* }{ */
		/* int has at least 32 bits */
		public const int LUAI_BITSINT	= 32;
		//#else				/* }{ */
		//#error "you must define LUA_BITSINT with number of bits in an integer"
		//#endif				/* } */


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
		//#if LUAI_BITSINT >= 32		/* { */
		//#define LUA_INT32	int
		//#define LUAI_UMEM	uint
		//#define LUAI_MEM	ptrdiff_t
		//#else				/* }{ */
		///* 16-bit ints */
		//#define LUA_INT32	long
		//#define LUAI_UMEM	unsigned long
		//#define LUAI_MEM	long
		//#endif				/* } */


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
		@@ LUAL_BUFFERSIZE is the buffer size used by the lauxlib buffer system.
        ** CHANGE it if it uses too much C-stack space.
		*/
		public const int LUAL_BUFFERSIZE		= 1024; // BUFSIZ; todo: check this - mjf
		//FIXME: changed here, = BUFSIZ;

		
		
        
		//FIXME: 20191006:here changed begin
		/*
		** {==================================================================
		** The following definitions set the numeric types for Lua.
		** Lua should work fine with 32-bit or 64-bit integers mixed with
		** 32-bit or 64-bit floats. The usual configurations are 64-bit
		** integers and floats (the default) and 32-bit integers and floats.
		** ===================================================================
		*/
		
		/*
		@@ LUA_INTSIZE defines size for Lua integer: 1=int, 2=long, 3=long long
		@@ LUA_FLOATSIZE defines size for Lua float: 1=float, 2=double, 3=long double
		** Default is long long + double
		*/
		public const int LUA_INTSIZE = 3;
		public const int LUA_FLOATSIZE = 2;
		
		
		/*
		@@ LUA_NUMBER is the floating-point type used by Lua.
		**
		@@ LUAI_UACNUMBER is the result of an 'usual argument conversion'
		@* over a floating number.
		**
		@@ LUA_NUMBER_FRMLEN is the length modifier for writing floats.
		@@ LUA_NUMBER_SCAN is the format for reading floats.
		@@ LUA_NUMBER_FMT is the format for writing floats.
		@@ lua_number2str converts a floats to a string.
		**
		@@ l_mathop allows the addition of an 'l' or 'f' to all math operations
		**
		@@ lua_str2number converts a decimal numeric string to a number.
		*/
		
		//#if LUA_FLOATSIZE == 1	/* { single float */
		
		//#define LUA_NUMBER	float
		
		//#define LUAI_UACNUMBER	double
		
		//#define LUA_NUMBER_FRMLEN	""
		//#define LUA_NUMBER_SCAN		"%f"
		//#define LUA_NUMBER_FMT		"%.7g"
		
		//#define l_mathop(op)		op##f
		
		//#define lua_str2number(s,p)	strtof((s), (p))
		
		
		//#elif LUA_FLOATSIZE == 3	/* }{ long double */
		
		//#define LUA_NUMBER	long double
		
		//#define LUAI_UACNUMBER	long double
		
		//#define LUA_NUMBER_FRMLEN	"L"
		//#define LUA_NUMBER_SCAN		"%Lf"
		//#define LUA_NUMBER_FMT		"%.19Lg"
		
		//#define l_mathop(op)		op##l
		
		//#define lua_str2number(s,p)	strtold((s), (p))
		
		//#else	/* }{ default: double */
		
		//#define LUA_NUMBER	double
		
		//#define LUAI_UACNUMBER	double
		
		public const string LUA_NUMBER_FRMLEN = "";
		public const string LUA_NUMBER_SCAN = "%lf";
		public const string LUA_NUMBER_FMT = "%.14g";
		
		//#define l_mathop(op)		op
		
		public static double lua_str2number(CharPtr s, out CharPtr p) { return strtod(s, out p); }
		
		//#endif	/* } */
		
		
		//#if defined(LUA_ANSI)
		/* C89 does not support 'opf' variants for math functions */
		//#undef l_mathop
		//#define l_mathop(op)		(lua_Number)op
		//#endif
		
		
		//#if defined(LUA_ANSI) || defined(_WIN32)
		/* C89 and Windows do not support 'strtof'... */
		//#undef lua_str2number
		//#define lua_str2number(s,p)	((lua_Number)strtod((s), (p)))
		//#endif
		
		
		//#define l_floor(x)		(l_mathop(floor)(x))
		
		public static int lua_number2str(CharPtr s, lua_Number n) { return sprintf(s, LUA_NUMBER_FMT, n); }
		
		
		/*
		@@ The luai_num* macros define the primitive operations over numbers.
		@* They should work for any size of floating numbers.
		*/
		
		/* the following operations need the math library */
		//#if defined(lobject_c) || defined(lvm_c)
		//#include <math.h>
		public static lua_Number luai_nummod(lua_State L, lua_Number a, lua_Number b)	{ return ((a) - floor((a)/(b))*(b));}
		public static lua_Number luai_numpow(lua_State L, lua_Number a, lua_Number b)	{ return (pow(a,b));}
		//#endif
		
		/* these are quite standard operations */
		//#if defined(LUA_CORE)
		public static lua_Number luai_numadd(lua_State L, lua_Number a, lua_Number b) { return ((a)+(b));}
		public static lua_Number luai_numsub(lua_State L, lua_Number a, lua_Number b) { return ((a)-(b));}
		public static lua_Number luai_nummul(lua_State L, lua_Number a, lua_Number b) { return ((a)*(b));}
		public static lua_Number luai_numdiv(lua_State L, lua_Number a, lua_Number b) { return ((a)/(b));}
		public static lua_Number luai_numunm(lua_State L, lua_Number a) { return (-(a));}
		public static bool luai_numeq(lua_Number a, lua_Number b) { return ((a)==(b));}
		public static bool luai_numlt(lua_State L, lua_Number a, lua_Number b) { return ((a)<(b));}
		public static bool luai_numle(lua_State L, lua_Number a, lua_Number b) { return ((a)<=(b));}
		public static bool luai_numisnan(lua_State L, lua_Number a)	{ return (!luai_numeq((a), (a)));}
		//#endif
		
		
		
		/*
		@@ LUA_INTEGER is the integer type used by Lua.
		**
		@@ LUA_UNSIGNED is the unsigned version of LUA_INTEGER.
		**
		@@ LUA_INTEGER_FRMLEN is the length modifier for reading/writing integers.
		@@ LUA_INTEGER_SCAN is the format for reading integers.
		@@ LUA_INTEGER_FMT is the format for writing integers.
		@@ lua_integer2str converts an integer to a string.
		*/
		
		//#if LUA_INTSIZE == 1	/* { int */
		
		//#define LUA_INTEGER		int
		public const string LUA_INTEGER_FRMLEN = "";
		
		//#elif LUA_INTSIZE == 2	/* }{ long */
		
		//#define LUA_INTEGER		long
		//#define LUA_INTEGER_FRMLEN	"l"
		
		//#else	/* }{ default: long long */
		
		//#define LUA_INTEGER		long long
		//public const string LUA_INTEGER_FRMLEN	= "ll";
		
		//#endif	/* } */
		
		
		public const string LUA_INTEGER_SCAN = "%" + LUA_INTEGER_FRMLEN + "d";
		public const string LUA_INTEGER_FMT = "%" + LUA_INTEGER_FRMLEN + "d";
		public static int lua_integer2str(CharPtr s, lua_Integer n)	{ return sprintf(s, LUA_INTEGER_FMT, n); }
		
		//#define LUA_UNSIGNED		unsigned LUA_INTEGER
		
		/* }================================================================== */
		
		
		
		
		/* =================================================================== */
		
		/*
		** Local configuration. You can use this space to add your redefinitions
		** without modifying the main part of the file.
		*/
		//FIXME: 20191006:here changed end



	}
}

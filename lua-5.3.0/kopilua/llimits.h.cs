/*
** $Id: llimits.h,v 1.125 2014/12/19 13:30:23 roberto Exp $
** Limits, basic types, and some other 'installation-dependent' definitions
** See Copyright Notice in lua.h
*/

//#define DEBUG

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace KopiLua
{
	using lu_int32 = System.UInt32;
	using lu_mem = System.UInt32;
	using l_mem = System.Int32;
	using lu_byte = System.Byte;
	using l_uacNumber = System.Double;
	using lua_Number = System.Double;
	using Instruction = System.UInt32;
	using lua_Integer = System.Int32;
	using lua_Unsigned = System.UInt32;

	public partial class Lua
	{
		/*
		** 'lu_mem' and 'l_mem' are unsigned/signed integers big enough to count
		** the total memory used by Lua (in bytes). Usually, 'size_t' and
		** 'ptrdiff_t' should work, but we use 'long' for 16-bit machines.
		*/
		//#if defined(LUAI_MEM)		/* { external definitions? */
		//typedef LUAI_UMEM lu_mem;
		//typedef LUAI_MEM l_mem;
		//#elif LUAI_BITSINT >= 32	/* }{ */
		//typedef size_t lu_mem;
		//typedef ptrdiff_t l_mem;
		//#else  /* 16-bit ints */	/* }{ */
		//typedef unsigned long lu_mem;
		//typedef long l_mem;
		//#endif				/* } */


		/* chars used as small naturals (so that `char' is reserved for characters) */
		//typedef unsigned char lu_byte;


		/* maximum value for size_t */
		public const uint MAX_SIZET	= uint.MaxValue; //FIXME:changed

		/* maximum size visible for Lua (must be representable in a lua_Integer */
		public const uint MAX_SIZE = (sizeof(uint) < sizeof(lua_Integer) ? MAX_SIZET 
                          : (uint)(LUA_MAXINTEGER));


		public const lu_mem MAX_LUMEM	= lu_mem.MaxValue; //FIXME:changed

		public const l_mem MAX_LMEM =	((l_mem)(MAX_LUMEM >> 1));


		public const int MAX_INT = Int32.MaxValue;  /* maximum value of an int */


		/*
		** conversion of pointer to integer:
		** this is for hashing only; there is no problem if the integer
		** cannot hold the whole pointer value
		*/
		public static uint point2int(object p) { return (uint)p.GetHashCode(); } //((uint)((size_t)(p) & UINT_MAX)) //FIXME:???



		/* type to ensure maximum alignment */
		//#if defined(LUAI_USER_ALIGNMENT_T)
		//typedef LUAI_USER_ALIGNMENT_T L_Umaxalign;
		//#else
		public class L_Umaxalign { public double u; public object s; public lua_Integer i; public long l; };
		//#endif



		/* types of 'usual argument conversions' for lua_Number and lua_Integer */
		//typedef LUAI_UACNUMBER l_uacNumber;
		//typedef LUAI_UACINT l_uacInt;


		/* internal assertions for in-house debugging */

#if lua_assert
		[Conditional("DEBUG")]
		public static void lua_assert(bool c) {Debug.Assert(c);}
		[Conditional("DEBUG")]
		public static void lua_assert(bool c, string msg) {Debug.Assert(c, msg);}

		[Conditional("DEBUG")]
		public static void lua_assert(int c) { Debug.Assert(c != 0); }
		[Conditional("DEBUG")]
		public static void lua_assert(int c, string msg) { Debug.Assert(c != 0, msg); }

		public static object check_exp(bool c, object e)		{lua_assert(c); return e;}
		public static object check_exp(int c, object e) { lua_assert(c != 0); return e; }

		/* to avoid problems with conditions too long */
		public static void lua_longassert(bool c)	{ if (!c) lua_assert(0); }
		public static void lua_longassert(int c)	{ if (c==0) lua_assert(0); }
#else

		[Conditional("DEBUG")]
		public static void lua_assert(bool c) {/* empty */}
		[Conditional("DEBUG")]
		public static void lua_assert(bool c, string msg) {/* empty */}

		[Conditional("DEBUG")]
		public static void lua_assert(int c) {/* empty */}
		[Conditional("DEBUG")]
		public static void lua_assert(int c, string msg) {/* empty */}

		public static object check_exp(bool c, object e) { return e; }
		public static object check_exp(int c, object e) { return e; }

		/* to avoid problems with conditions too long */
		public static void lua_longassert(bool c)	{ /* empty */ }
		public static void lua_longassert(int c)	{ /* empty */ }
#endif

		/*
		** assertion for checking API calls
		*/
		//#if defined(LUA_USE_APICHECK)
		//#include <assert.h>
		//#define luai_apicheck(e)	assert(e)
		//#else
		//#define luai_apicheck(e)	lua_assert(e)
		//#endif

		//FIXME:???
		[Conditional("DEBUG")]
		public static void luai_apicheck(bool e) {lua_assert(e);}
		[Conditional("DEBUG")]
		public static void luai_apicheck(bool e, string msg) {lua_assert(e, msg);}
		
		[Conditional("DEBUG")]
		public static void api_check(bool e, string msg)		{luai_apicheck(e,msg);}
		[Conditional("DEBUG")]
		public static void api_check(int e, string msg) { luai_apicheck(e!=0,msg); }

        //#if !defined(UNUSED)
		//#define UNUSED(x)	((void)(x))	/* to avoid warnings */
        //#endif


		//#define cast(t, exp)	((t)(exp))

		public static void cast_void(object i)	{ /*cast(void, (i));*/ }
		public static lu_byte cast_byte(int i) { return (lu_byte)i; }
		public static lu_byte cast_byte(long i) { return (lu_byte)(int)i; }
		public static lu_byte cast_byte(bool i) { return i ? (lu_byte)1 : (lu_byte)0; }
		public static lu_byte cast_byte(lua_Number i) { return (lu_byte)i; }
		public static lu_byte cast_byte(object i) { return (lu_byte)(int)(i); }
		//FIXME: see below
		//public static lua_Number cast_num(object i) { return (lua_Number)i; } //FIXME:???remove?
		//public static int cast_int(object i) { return (int)i; } //FIXME:???remove?
		public static byte cast_uchar(object i) { 
			if (i is char) 
			{
				return (byte)(((char)i) & 0xff);
			}
			else
			{
				return (byte)((int)i & 0xff);
			}
		} //FIXME:???remove?




		/* cast a signed lua_Integer to lua_Unsigned */
		//#if !defined(l_castS2U)
		public static lua_Unsigned l_castS2U(int i)	{ return ((lua_Unsigned)(i)); }
		public static lua_Unsigned l_castS2U(uint i)	{ return ((lua_Unsigned)(i)); }
		//#endif

		/*
		** cast a lua_Unsigned to a signed lua_Integer; this cast is
		** not strict ISO C, but two-complement architectures should
		** work fine.
		*/
		//#if !defined(l_castU2S)
		public static lua_Integer l_castU2S(int i)	{ return ((lua_Integer)(i)); }
		public static lua_Integer l_castU2S(uint i)	{ return ((lua_Integer)(i)); }
		//#endif



		/*
		** non-return type
		*/
		//#if defined(__GNUC__)
		//#define l_noret		void __attribute__((noreturn))
		//#elif defined(_MSC_VER) && _MSC_VER >= 1200
		//#define l_noret		void __declspec(noreturn)
		//#else
		//#define l_noret		void
		//#endif



		/*
		** maximum depth for nested C calls and syntactical nested non-terminals
		** in a program. (Value must fit in an unsigned short int.)
		*/
		//#if !defined(LUAI_MAXCCALLS)
		public const int LUAI_MAXCCALLS = 200;
		//#endif

		/*
		** maximum number of upvalues in a closure (both C and Lua). (Value
		** must fit in an unsigned char.)
		*/
		public const int MAXUPVAL = UCHAR_MAX; //FIXME:UCHAR_MAX???

		//--------------------------------
		//FIXME: added
		public static int cast_int(int i) { return (int)i; }
		public static int cast_int(long i) { return (int)(int)i; }
		public static int cast_int(bool i) { return i ? (int)1 : (int)0; }
		public static int cast_int(lua_Number i) { return (int)i; }
		public static int cast_int(Instruction i) { return Convert.ToInt32(i); }
		public static int cast_int(object i) { Debug.Assert(false, "Can't convert int."); return Convert.ToInt32(i); }

		public static lua_Number cast_num(int i) { return (lua_Number)i; }
		public static lua_Number cast_num(long i) { return (lua_Number)i; }
		public static lua_Number cast_num(bool i) { return i ? (lua_Number)1 : (lua_Number)0; }
		public static lua_Number cast_num(ulong i) { return (lua_Number)i; }
		public static lua_Number cast_num(double i) { return (lua_Number)i; }
		public static lua_Number cast_num(object i) { Debug.Assert(false, "Can't convert number."); return Convert.ToSingle(i); }

		//
		//--------------------------------
		
		/*
		** type for virtual-machine instructions;
		** must be an unsigned with (at least) 4 bytes (see details in lopcodes.h)
		*/
		//#if LUAI_BITSINT >= 32
		//typedef unsigned int Instruction;
		//#else
		//typedef unsigned long Instruction;
		//#endif






		/* minimum size for the string table (must be power of 2) */
		public const int MINSTRTABSIZE	= 64;	/* minimum size for "predefined" strings */



		/* minimum size for string buffer */
		public const int LUA_MINBUFFER	= 32;



		#if !lua_lock
		public static void lua_lock(lua_State L) { }
		public static void lua_unlock(lua_State L) { }
		#endif

		#if !luai_threadyield
		public static void luai_threadyield(lua_State L)     {lua_unlock(L); lua_lock(L);}
		#endif


		/*
		** these macros allow user-specific actions on threads when you defined
		** LUAI_EXTRASPACE and need to do something extra when a thread is
		** created/deleted/resumed/yielded.
		*/
		#if !luai_userstateopen
		public static void luai_userstateopen(lua_State L)           { /*((void)L)*/ }
		#endif

		#if !luai_userstateclose
		public static void luai_userstateclose(lua_State L)          { /*((void)L)*/ }
		#endif

		#if !luai_userstatethread
		public static void luai_userstatethread(lua_State L, lua_State L1)      { /*((void)L)*/ }
		#endif

		#if !luai_userstatefree
		public static void luai_userstatefree(lua_State L, lua_State L1)           { /*((void)L)*/ }
		#endif

		#if !luai_userstateresume
		public static void luai_userstateresume(lua_State L, int n)       { /*((void)L)*/ }
		#endif

		#if !luai_userstateyield
		public static void luai_userstateyield(lua_State L, int n)        { /*((void)L)*/ }
		#endif


		/*
		** macro to control inclusion of some hard tests on stack reallocation
		*/
		//------------------>FIXME: below ignore???, TODO
		//#ifndef HARDSTACKTESTS
		//#define condmovestack(x)	((void)0)
		//#else
		//#define condmovestack(L) /* realloc stack keeping its size */ \
		//	luaD_reallocstack((L), (L)->stacksize - EXTRA_STACK - 1)
		//#endif
        //------------------>FIXME:below ignore???, TODO		
		//#if !defined(HARDMEMTESTS)
		//#define condchangemem(L)	condmovestack(L)
		//#else
		//#define condchangemem(L)  \
		//	((void)(!(G(L)->gcrunning) || (luaC_fullgc(L, 0), 1)))
		//#endif
		//FIXME: added, see upper
		public static void condchangemem(lua_State L) {}
	}
}

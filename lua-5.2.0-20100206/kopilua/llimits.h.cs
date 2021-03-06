//#define lua_assert

/*
** $Id: llimits.h,v 1.76 2009/12/17 12:26:09 roberto Exp roberto $
** Limits, basic types, and some other `installation-dependent' definitions
** See Copyright Notice in lua.h
*/

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

	public partial class Lua
	{

		//typedef unsigned LUA_INT32 lu_int32;

		//typedef LUAI_UMEM lu_mem;

		//typedef LUAI_MEM l_mem;



		/* chars used as small naturals (so that `char' is reserved for characters) */
		//typedef unsigned char lu_byte;


		public const uint MAX_SIZET	= uint.MaxValue - 2;

		public const lu_mem MAX_LUMEM	= lu_mem.MaxValue - 2;


		public const int MAX_INT = (Int32.MaxValue - 2);  /* maximum value of an int (-2 for safety) */

		/*
		** conversion of pointer to integer
		** this is for hashing only; there is no problem if the integer
		** cannot hold the whole pointer value
		*/
		//#define IntPoint(p)  ((uint)(lu_mem)(p))



		/* type to ensure maximum alignment */
		//#if !defined(LUAI_USER_ALIGNMENT_T)
		//#define LUAI_USER_ALIGNMENT_T	union { double u; void *s; long l; }
		//#endif

		//typedef LUAI_USER_ALIGNMENT_T L_Umaxalign;


		/* result of a `usual argument conversion' over lua_Number */
		//typedef LUAI_UACNUMBER l_uacNumber;


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

#else

		[Conditional("DEBUG")]
		public static void lua_assert(bool c) {}
		[Conditional("DEBUG")]
		public static void lua_assert(bool c, string msg) {}

		[Conditional("DEBUG")]
		public static void lua_assert(int c) {}
		[Conditional("DEBUG")]
		public static void lua_assert(int c, string msg) {}

		public static object check_exp(bool c, object e) { return e; }
		public static object check_exp(int c, object e) { return e; }

#endif

		/*
		** assertion for checking API calls
		*/
		//#if defined(LUA_USE_APICHECK)
		//#include <assert.h>
		//#define luai_apicheck(L,e)	{ (void)L; assert(e); }
		//#elif !defined(luai_apicheck)
		//#define luai_apicheck(L,e)	lua_assert(e)
		//#endif
		//FIXME:???
		[Conditional("DEBUG")]
		public static void luai_apicheck(object L, bool e) {lua_assert(e);}
		[Conditional("DEBUG")]
		public static void luai_apicheck(object L, bool e, string msg) {lua_assert(e, msg);}
		
		[Conditional("DEBUG")]
		public static void api_check(object l, bool e, string msg)		{luai_apicheck(l,e,msg);}
		[Conditional("DEBUG")]
		public static void api_check(object l, int e, string msg) { luai_apicheck(l,e!=0,msg); }

		//#define UNUSED(x)	((void)(x))	/* to avoid warnings */


		public static lu_byte cast_byte(int i) { return (lu_byte)i; }
		public static lu_byte cast_byte(long i) { return (lu_byte)(int)i; }
		public static lu_byte cast_byte(bool i) { return i ? (lu_byte)1 : (lu_byte)0; }
		public static lu_byte cast_byte(lua_Number i) { return (lu_byte)i; }
		public static lu_byte cast_byte(object i) { return (lu_byte)(int)(i); }

		/*
		** maximum depth for nested C calls and syntactical nested non-terminals
		** in a program. (Value must fit in an unsigned short int.)
		*/
		//#if !defined(LUAI_MAXCCALLS)
		public const int LUAI_MAXCCALLS = 200;
		//#endif

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
		public static lua_Number cast_num(object i) { Debug.Assert(false, "Can't convert number."); return Convert.ToSingle(i); }

		/*
		** type for virtual-machine instructions
		** must be an unsigned with (at least) 4 bytes (see details in lopcodes.h)
		*/
		//typedef lu_int32 Instruction;



		/* maximum stack for a Lua function */
		public const int MAXSTACK	= 250;



		/* minimum size for the string table (must be power of 2) */
		public const int MINSTRTABSIZE	= 32;


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
		public static void luai_userstatefree(lua_State L)           { /*((void)L)*/ }
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
		//#ifndef HARDSTACKTESTS
		//#define condmovestack(x)	((void)0)
		//#else
		//#define condmovestack(L) /* realloc stack keeping its size */ \
		//	luaD_reallocstack((L), (L)->stacksize - EXTRA_STACK - 1)
		//#endif
        //------------------>FIXME:???
	}
}

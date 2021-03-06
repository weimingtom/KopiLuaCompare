/*
** $Id: lua.h,v 1.283 2012/04/20 13:18:26 roberto Exp $
** Lua - A Scripting Language
** Lua.org, PUC-Rio, Brazil (http://www.lua.org)
** See Copyright Notice at the end of this file
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace KopiLua
{
	using lua_Number = Double;
	using lua_Integer = System.Int32;
	using lua_Unsigned = System.UInt32;
	
	public partial class Lua
	{

		public const string LUA_VERSION_MAJOR = "5";
		public const string LUA_VERSION_MINOR = "2";
        public const int LUA_VERSION_NUM = 502;
		public const string LUA_VERSION_RELEASE = "1";

		public const string LUA_VERSION = "Lua " + LUA_VERSION_MAJOR + "." + LUA_VERSION_MINOR;
		public const string LUA_RELEASE	= LUA_VERSION + "." + LUA_VERSION_RELEASE;
		public const string LUA_COPYRIGHT = LUA_RELEASE + "  Copyright (C) 1994-2012 Lua.org, PUC-Rio";
		public const string LUA_AUTHORS = "R. Ierusalimschy, L. H. de Figueiredo, W. Celes";


		/* mark for precompiled code ('<esc>Lua') */
		public const string LUA_SIGNATURE = "\x01bLua";

		/* option for multiple returns in `lua_pcall' and `lua_call' */
		public const int LUA_MULTRET	= (-1);


		/*
		** pseudo-indices
		*/
		public const int LUA_REGISTRYINDEX	= LUAI_FIRSTPSEUDOIDX;
		public static int lua_upvalueindex(int i)	{return LUA_REGISTRYINDEX-i;}


		/* thread status */
        public const int LUA_OK = 0;
		public const int LUA_YIELD	= 1;
		public const int LUA_ERRRUN = 2;
		public const int LUA_ERRSYNTAX	= 3;
		public const int LUA_ERRMEM	= 4;
		public const int LUA_ERRGCMM = 5;
		public const int LUA_ERRERR = 6;


		public delegate int lua_CFunction(lua_State L);


		/*
		** functions that read/write blocks when loading/dumping Lua chunks
		*/
        public delegate CharPtr lua_Reader(lua_State L, object ud, out uint sz);

		public delegate int lua_Writer(lua_State L, CharPtr p, uint sz, object ud);


		/*
		** prototype for memory-allocation functions
		*/
        //public delegate object lua_Alloc(object ud, object ptr, uint osize, uint nsize);
		public delegate object lua_Alloc(Type t); //FIXME:!!!changed here!!!


		/*
		** basic types
		*/
		public const int LUA_TNONE = -1;

        public const int LUA_TNIL = 0;
        public const int LUA_TBOOLEAN = 1;
        public const int LUA_TLIGHTUSERDATA = 2;
        public const int LUA_TNUMBER = 3;
        public const int LUA_TSTRING = 4;
        public const int LUA_TTABLE = 5;
        public const int LUA_TFUNCTION = 6;
        public const int LUA_TUSERDATA = 7;
        public const int LUA_TTHREAD = 8;

		public const int LUA_NUMTAGS = 9;



		/* minimum Lua stack available to a C function */
		public const int LUA_MINSTACK = 20;


		/* predefined values in the registry */
		public const int LUA_RIDX_MAINTHREAD = 1;
		public const int LUA_RIDX_GLOBALS = 2;
		public const int LUA_RIDX_LAST = LUA_RIDX_GLOBALS;


		/* type of numbers in Lua */
		//typedef LUA_NUMBER lua_Number;


		/* type for integer functions */
		//typedef LUA_INTEGER lua_Integer;


		/* unsigned integer type */
		//typedef LUA_UNSIGNED lua_Unsigned;



		/*
		** generic extra include file
		*/
		//#if defined(LUA_USER_H)
		//#include LUA_USER_H
		//#endif

        //<-----------------------------ignore


/*
** state manipulation
*/












/*
** basic stack manipulation
*/



/*
** access functions (stack -> C)
*/




        //----------------------------->ignore
		/*
		** Comparison and arithmetic functions
		*/

		public const int LUA_OPADD = 0;	/* ORDER TM */
		public const int LUA_OPSUB = 1;
		public const int LUA_OPMUL = 2;
		public const int LUA_OPDIV = 3;
		public const int LUA_OPMOD = 4;
		public const int LUA_OPPOW = 5;
		public const int LUA_OPUNM = 6;


		public const int LUA_OPEQ = 0;
		public const int LUA_OPLT = 1;
		public const int LUA_OPLE = 2;

		//<-----------------------------ignore
		public static void lua_call(lua_State L, int n, int r) { lua_callk(L, n, r, 0, null); }

        public static int lua_pcall(lua_State L, int n, int r, int f) { return lua_pcallk(L, n, r, f, 0, null); }
		public static int lua_yield(lua_State L, int n) { return lua_yieldk(L, n, 0, null); }
		
        

























		//----------------------------->ignore
		/*
		** garbage-collection function and options
		*/

		public const int LUA_GCSTOP			= 0;
		public const int LUA_GCRESTART		= 1;
		public const int LUA_GCCOLLECT		= 2;
		public const int LUA_GCCOUNT		= 3;
		public const int LUA_GCCOUNTB		= 4;
		public const int LUA_GCSTEP			= 5;
		public const int LUA_GCSETPAUSE		= 6;
		public const int LUA_GCSETSTEPMUL	= 7;
		public const int LUA_GCSETMAJORINC	= 8;
		public const int LUA_GCISRUNNING	= 9;
		public const int LUA_GCGEN		    = 10;
		public const int LUA_GCINC          = 11;




		/*
		** miscellaneous functions
		*/













		/* 
		** ===============================================================
		** some useful macros
		** ===============================================================
		*/

		public static lua_Number lua_tonumber(lua_State L, int i) { int null_=0; return lua_tonumberx(L,i,ref null_);} //FIXME: changed
		public static lua_Integer lua_tointeger(lua_State L, int i) { int null_=0; return lua_tointegerx(L,i,ref null_);} //FIXME: changed
		public static lua_Unsigned lua_tounsigned(lua_State L, int i) { int null_=0; return lua_tounsignedx(L,i,ref null_);} //FIXME: changed

        public static void lua_pop(lua_State L, int n) { lua_settop(L, -(n)-1); }

        public static void lua_newtable(lua_State L) { lua_createtable(L, 0, 0); }

        public static void lua_register(lua_State L, CharPtr n, lua_CFunction f) { lua_pushcfunction(L, f); lua_setglobal(L, n); }

        public static void lua_pushcfunction(lua_State L, lua_CFunction f) { lua_pushcclosure(L, f, 0); }

        public static bool lua_isfunction(lua_State L, int n) { return lua_type(L, n) == LUA_TFUNCTION; }
        public static bool lua_istable(lua_State L, int n) { return lua_type(L, n) == LUA_TTABLE; }
        public static bool lua_islightuserdata(lua_State L, int n) { return lua_type(L, n) == LUA_TLIGHTUSERDATA; }
        public static bool lua_isnil(lua_State L, int n) { return lua_type(L, n) == LUA_TNIL; }
        public static bool lua_isboolean(lua_State L, int n) { return lua_type(L, n) == LUA_TBOOLEAN; }
        public static bool lua_isthread(lua_State L, int n) { return lua_type(L, n) == LUA_TTHREAD; }
        public static bool lua_isnone(lua_State L, int n) { return lua_type(L, n) == LUA_TNONE; }
        public static bool lua_isnoneornil(lua_State L, lua_Number n) { return lua_type(L, (int)n) <= 0; } //FIXME: ???(int)

        public static CharPtr lua_pushliteral(lua_State L, CharPtr s) {
        	return lua_pushlstring(L, "" + s, (uint)strlen(s)); } //FIXME: changed???

        public static void lua_pushglobaltable(lua_State L) {
            lua_rawgeti(L, LUA_REGISTRYINDEX, LUA_RIDX_GLOBALS); }

        public static CharPtr lua_tostring(lua_State L, int i) { uint blah; return lua_tolstring(L, i, out blah); } //FIXME: changed, null



		/*
		** {======================================================================
		** Debug API
		** =======================================================================
		*/


		/*
		** Event codes
		*/
		public const int LUA_HOOKCALL = 0;
        public const int LUA_HOOKRET = 1;
        public const int LUA_HOOKLINE = 2;
        public const int LUA_HOOKCOUNT = 3;
        public const int LUA_HOOKTAILCALL = 4;


		/*
		** Event masks
		*/
		public const int LUA_MASKCALL = (1 << LUA_HOOKCALL);
        public const int LUA_MASKRET = (1 << LUA_HOOKRET);
        public const int LUA_MASKLINE = (1 << LUA_HOOKLINE);
        public const int LUA_MASKCOUNT = (1 << LUA_HOOKCOUNT);

		/* Functions to be called by the debuger in specific events */
		public delegate void lua_Hook(lua_State L, lua_Debug ar);


		public class lua_Debug {
		  public int event_;
		  public CharPtr name;	/* (n) */
		  public CharPtr namewhat;	/* (n) 'global', 'local', 'field', 'method' */
		  public CharPtr what;	/* (S) 'Lua', 'C', 'main', 'tail' */
		  public CharPtr source;	/* (S) */
		  public int currentline;	/* (l) */
		  public int linedefined;	/* (S) */
		  public int lastlinedefined;	/* (S) */
		  public byte nups;	/* (u) number of upvalues */
		  public byte nparams;/* (u) number of parameters */
		  public char isvararg;        /* (u) */
		  public char istailcall;	/* (t) */
		  public CharPtr short_src = new char[LUA_IDSIZE]; /* (S) */
		  /* private part */
		  public CallInfo i_ci = new CallInfo();  /* active function */
		};

		/* }====================================================================== */


		/******************************************************************************
        * Copyright (C) 1994-2012 Lua.org, PUC-Rio.
		*
		* Permission is hereby granted, free of charge, to any person obtaining
		* a copy of this software and associated documentation files (the
		* "Software"), to deal in the Software without restriction, including
		* without limitation the rights to use, copy, modify, merge, publish,
		* distribute, sublicense, and/or sell copies of the Software, and to
		* permit persons to whom the Software is furnished to do so, subject to
		* the following conditions:
		*
		* The above copyright notice and this permission notice shall be
		* included in all copies or substantial portions of the Software.
		*
		* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
		* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
		* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
		* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
		* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
		* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
		* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
		******************************************************************************/

	}
}

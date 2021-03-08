/*
** mathlib.c
** Mathematics library to LUA
*/
using System;

namespace KopiLua
{
	using lua_Object = System.UInt32;
	using real = System.Single;	
	
	public partial class Lua
	{
		//char *rcs_mathlib="$Id: mathlib.c,v 1.9 1995/02/06 19:36:43 roberto Exp $";

		//#include <stdio.h>		/* NULL */
		//#include <math.h>

		//#include "lualib.h"
		//#include "lua.h"

		private const double PI = 3.14159265358979323846;
		private static double TODEGREE(double a) { return ((a)*180.0/PI); }
		private static double TORAD(double a) { return ((a)*PI/180.0); }

		private static void math_abs ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == LUA_NOOBJECT)
		 	  lua_error ("too few arguments to function `abs'");
		 	if (0==lua_isnumber(o))
		 	  lua_error ("incorrect arguments to function `abs'");
		 	d = lua_getnumber(o);
		 	if (d < 0) d = -d;
		 	lua_pushnumber ((real)d);
		}


		private static void math_sin ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == LUA_NOOBJECT)
		 	  lua_error ("too few arguments to function `sin'");
		 	if (0==lua_isnumber(o))
		 	  lua_error ("incorrect arguments to function `sin'");
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)sin(TORAD(d)));
		}
	
	
	
		private static void math_cos ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
			if (o == LUA_NOOBJECT)
			  lua_error ("too few arguments to function `cos'");
		 	if (0==lua_isnumber(o))
		 	  lua_error ("incorrect arguments to function `cos'");
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)cos(TORAD(d)));
		}
	
		
		
		private static void math_tan ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == LUA_NOOBJECT)
		 	  lua_error ("too few arguments to function `tan'");
		 	if (0==lua_isnumber(o))
		 	  lua_error ("incorrect arguments to function `tan'");
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)tan(TORAD(d)));
		}


		private static void math_asin ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == LUA_NOOBJECT)
		 	  lua_error ("too few arguments to function `asin'");
		 	if (0==lua_isnumber(o))
		 	  lua_error ("incorrect arguments to function `asin'");
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)TODEGREE(asin(d)));
		}
	
	
		private static void math_acos ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == LUA_NOOBJECT)
		 	  lua_error ("too few arguments to function `acos'");
		 	if (0==lua_isnumber(o))
		 	  lua_error ("incorrect arguments to function `acos'");
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)TODEGREE(acos(d)));
		}



		private static void math_atan ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == LUA_NOOBJECT)
		 	  lua_error ("too few arguments to function `atan'");
			if (0==lua_isnumber(o))
			  lua_error ("incorrect arguments to function `atan'");
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)TODEGREE(atan(d)));
		}


		private static void math_ceil ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == LUA_NOOBJECT)
		 	  lua_error ("too few arguments to function `ceil'");
		 	if (0==lua_isnumber(o))
		 	  lua_error ("incorrect arguments to function `ceil'");
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)ceil(d));
		}
	
	
		private static void math_floor ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == LUA_NOOBJECT)
		 	  lua_error ("too few arguments to function `floor'");
		 	if (0==lua_isnumber(o))
			  lua_error ("incorrect arguments to function `floor'");
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)floor(d));
		}
	
		private static void math_mod()
		{
		 	int d1, d2;
		 	lua_Object o1 = lua_getparam(1);
		 	lua_Object o2 = lua_getparam(2);
		 	if (0==lua_isnumber(o1) || 0==lua_isnumber(o2))
		 	  lua_error ("incorrect arguments to function `mod'");
		 	d1 = (int) lua_getnumber(o1);
		 	d2 = (int) lua_getnumber(o2);
		 	lua_pushnumber(d1%d2);
		}


		private static void math_sqrt()
		{
		 	double d;
		 	lua_Object o = lua_getparam(1);
			if (o == LUA_NOOBJECT)
		 	  lua_error ("too few arguments to function `sqrt'");
		 	if (0==lua_isnumber(o))
		 	  lua_error ("incorrect arguments to function `sqrt'");
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)sqrt(d));
		}

		private static int old_pow;

		private static void math_pow()
		{
			lua_Object o1 = lua_getparam (1);
			lua_Object o2 = lua_getparam (2);
			lua_Object op = lua_getparam(3);
			if (0==lua_isnumber(o1) || 0==lua_isnumber(o2) || lua_getstring(op)[0] != 'p')
		 	{
		   		lua_Object old = lua_getlocked(old_pow);
		   		lua_pushobject(o1);
		   		lua_pushobject(o2);
		   		lua_pushobject(op);
		   		if (lua_callfunction(old) != 0)
		     		lua_error(null);
		 	}
		 	else
		 	{
		   		double d1 = lua_getnumber(o1);
		   		double d2 = lua_getnumber(o2);
		   		lua_pushnumber ((real)pow(d1,d2));
		 	}
		}

		private static void math_min()
		{
		 	int i=1;
		 	double d, dmin;
		 	lua_Object o;
		 	if ((o = lua_getparam(i++)) == LUA_NOOBJECT)
		 	  lua_error ("too few arguments to function `min'");
		 	if (0==lua_isnumber(o))
		 	  lua_error ("incorrect arguments to function `min'");
		 	dmin = lua_getnumber(o);
			while ((o = lua_getparam(i++)) != LUA_NOOBJECT)
		 	{
		  		if (0==lua_isnumber(o))
		  		  lua_error ("incorrect arguments to function `min'");
		  		d = lua_getnumber(o);
		  		if (d < dmin) dmin = d;
		 	}
		 	lua_pushnumber ((real)dmin);
		}


		private static void math_max()
		{
		 	int i=1;
		 	double d, dmax;
		 	lua_Object o;
		 	if ((o = lua_getparam(i++)) == LUA_NOOBJECT)
		 	  lua_error ("too few arguments to function `max'");
		 	if (0==lua_isnumber(o))
		 	  lua_error ("incorrect arguments to function `max'");
		 	dmax = lua_getnumber (o);
		 	while ((o = lua_getparam(i++)) != LUA_NOOBJECT)
		 	{
		  		if (0==lua_isnumber(o))
		  		  lua_error ("incorrect arguments to function `max'");
		  		d = lua_getnumber (o);
		  		if (d > dmax) dmax = d;
		 	}
		 	lua_pushnumber ((real)dmax);
		}


		private static void math_log ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == LUA_NOOBJECT)
		   		lua_error ("too few arguments to function `log'");
		 	if (0==lua_isnumber(o))
		   		lua_error ("incorrect arguments to function `log'");
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)log(d));
		}


		private static void math_log10 ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == LUA_NOOBJECT)
		   		lua_error ("too few arguments to function `log10'");
		 	if (0==lua_isnumber(o))
		   		lua_error ("incorrect arguments to function `log10'");
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)log10(d));
		}


		private static void math_exp ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == LUA_NOOBJECT)
		   		lua_error ("too few arguments to function `exp'");
		 	if (0==lua_isnumber(o))
		   		lua_error ("incorrect arguments to function `exp'");
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)exp(d));
		}

		private static void math_deg ()
		{
		 	float d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == LUA_NOOBJECT)
		   		lua_error ("too few arguments to function `deg'");
		 	if (0==lua_isnumber(o))
		   		lua_error ("incorrect arguments to function `deg'");
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)(d*180.0/PI));
		}

		private static void math_rad ()
		{
		 	float d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == LUA_NOOBJECT)
		   		lua_error ("too few arguments to function `rad'");
		 	if (0==lua_isnumber(o))
		   		lua_error ("incorrect arguments to function `rad'");
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)(d/180.0*PI));
		}

		/*
		** Open math library
		*/
		public static void mathlib_open ()
		{
			lua_register ("abs",   math_abs, "math_abs");
			lua_register ("sin",   math_sin, "math_sin");
			lua_register ("cos",   math_cos, "math_cos");
			lua_register ("tan",   math_tan, "math_tan");
			lua_register ("asin",  math_asin, "math_asin");
			lua_register ("acos",  math_acos, "math_acos");
			lua_register ("atan",  math_atan, "math_atan");
			lua_register ("ceil",  math_ceil, "math_ceil");
			lua_register ("floor", math_floor, "math_floor");
			lua_register ("mod",   math_mod, "math_mod");
			lua_register ("sqrt",  math_sqrt, "math_sqrt");
			lua_register ("min",   math_min, "math_min");
			lua_register ("max",   math_max, "math_max");
			lua_register ("log",   math_log, "math_log");
			lua_register ("log10", math_log10, "math_log10");
			lua_register ("exp",   math_exp, "math_exp");
			lua_register ("deg",   math_deg, "math_deg");
			lua_register ("rad",   math_rad, "math_rad");
			old_pow = lua_lockobject(lua_setfallback("arith", math_pow, "math_pow"));
		}
	} 
}

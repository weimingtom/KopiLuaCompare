/*
** mathlib.c
** Mathematics library to LUA
*/
using System;

namespace KopiLua
{
	using lua_Object = KopiLua.Lua.Object_;	
	using real = System.Single;	
	
	public partial class Lua
	{
		//char *rcs_mathlib="$Id: mathlib.c,v 1.1 1993/12/17 18:41:19 celes Exp $";

		//#include <stdio.h>		/* NULL */
		//#include <math.h>

		//#include "lua.h"

		private static double TODEGREE(double a) { return ((a)*180.0/3.14159); }
		private static double TORAD(double a) { return ((a)*3.14159/180.0); }

		private static void math_abs ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)
		 	{ lua_error ("too few arguments to function `abs'"); return; }
		 	if (0==lua_isnumber(o))
		 	{ lua_error ("incorrect arguments to function `abs'"); return; }
		 	d = lua_getnumber(o);
		 	if (d < 0) d = -d;
		 	lua_pushnumber ((real)d);
		}


		private static void math_sin ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)
		 	{ lua_error ("too few arguments to function `sin'"); return; }
		 	if (0==lua_isnumber(o))
		 	{ lua_error ("incorrect arguments to function `sin'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)sin(TORAD(d)));
		}
	
	
	
		private static void math_cos ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
			if (o == null)
			{ lua_error ("too few arguments to function `cos'"); return; }
		 	if (0==lua_isnumber(o))
		 	{ lua_error ("incorrect arguments to function `cos'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)cos(TORAD(d)));
		}
	
		
		
		internal static void math_tan ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)
		 	{ lua_error ("too few arguments to function `tan'"); return; }
		 	if (0==lua_isnumber(o))
		 	{ lua_error ("incorrect arguments to function `tan'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)tan(TORAD(d)));
		}


		internal static void math_asin ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)
		 	{ lua_error ("too few arguments to function `asin'"); return; }
		 	if (0==lua_isnumber(o))
		 	{ lua_error ("incorrect arguments to function `asin'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)TODEGREE(asin(d)));
		}
	
	
		internal static void math_acos ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)
		 	{ lua_error ("too few arguments to function `acos'"); return; }
		 	if (0==lua_isnumber(o))
		 	{ lua_error ("incorrect arguments to function `acos'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)TODEGREE(acos(d)));
		}



		internal static void math_atan ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)
		 	{ lua_error ("too few arguments to function `atan'"); return; }
			if (0==lua_isnumber(o))
			{ lua_error ("incorrect arguments to function `atan'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)TODEGREE(atan(d)));
		}
	
	
		internal static void math_ceil ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)
		 	{ lua_error ("too few arguments to function `ceil'"); return; }
		 	if (0==lua_isnumber(o))
		 	{ lua_error ("incorrect arguments to function `ceil'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)ceil(d));
		}
	
	
		internal static void math_floor ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)
		 	{ lua_error ("too few arguments to function `floor'"); return; }
		 	if (0==lua_isnumber(o))
			{ lua_error ("incorrect arguments to function `floor'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)floor(d));
		}
	
		internal static void math_mod()
		{
		 	int d1, d2;
		 	lua_Object o1 = lua_getparam(1);
		 	lua_Object o2 = lua_getparam(2);
		 	if (0==lua_isnumber(o1) || 0==lua_isnumber(o2))
		 	{ lua_error ("incorrect arguments to function `mod'"); return; }
		 	d1 = (int) lua_getnumber(o1);
		 	d2 = (int) lua_getnumber(o2);
		 	lua_pushnumber(d1%d2);
		}
	
	
		internal static void math_sqrt()
		{
		 	double d;
		 	lua_Object o = lua_getparam(1);
			if (o == null)
		 	{ lua_error ("too few arguments to function `sqrt'"); return; }
		 	if (0==lua_isnumber(o))
		 	{ lua_error ("incorrect arguments to function `sqrt'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber ((real)sqrt(d));
		}
	
		internal static void math_pow()
		{
		 	double d1, d2;
		 	lua_Object o1 = lua_getparam(1);
		 	lua_Object o2 = lua_getparam(2);
		 	if (lua_isnumber(o1) == 0 || lua_isnumber(o2) == 0)
		 	{ lua_error ("incorrect arguments to function `pow'"); return; }
		 	d1 = lua_getnumber(o1);
		 	d2 = lua_getnumber(o2);
		 	lua_pushnumber ((real)pow(d1,d2));
		}
	
		internal static void math_min()
		{
		 	int i=1;
		 	double d, dmin;
		 	lua_Object o;
		 	if ((o = lua_getparam(i++)) == null)
		 	{ lua_error ("too few arguments to function `min'"); return; }
		 	if (0==lua_isnumber(o))
		 	{ lua_error ("incorrect arguments to function `min'"); return; }
		 	dmin = lua_getnumber(o);
			while ((o = lua_getparam(i++)) != null)
		 	{
		  		if (0==lua_isnumber(o))
		  		{ lua_error ("incorrect arguments to function `min'"); return; }
		  		d = lua_getnumber(o);
		  		if (d < dmin) dmin = d;
		 	}
		 	lua_pushnumber ((real)dmin);
		}
	
	
		internal static void math_max()
		{
		 	int i=1;
		 	double d, dmax;
		 	lua_Object o;
		 	if ((o = lua_getparam(i++)) == null)
		 	{ lua_error ("too few arguments to function `max'"); return; }
		 	if (0==lua_isnumber(o))
		 	{ lua_error ("incorrect arguments to function `max'"); return; }
		 	dmax = lua_getnumber (o);
		 	while ((o = lua_getparam(i++)) != null)
		 	{
		  		if (0==lua_isnumber(o))
		  		{ lua_error ("incorrect arguments to function `max'"); return; }
		  		d = lua_getnumber (o);
		  		if (d > dmax) dmax = d;
		 	}
		 	lua_pushnumber ((real)dmax);
		}
	
	
	
		/*
		** Open math library
		*/
		public static void mathlib_open ()
		{
			lua_register ("abs",   math_abs);
			lua_register ("sin",   math_sin);
			lua_register ("cos",   math_cos);
			lua_register ("tan",   math_tan);
			lua_register ("asin",  math_asin);
			lua_register ("acos",  math_acos);
			lua_register ("atan",  math_atan);
			lua_register ("ceil",  math_ceil);
			lua_register ("floor", math_floor);
			lua_register ("mod",   math_mod);
			lua_register ("sqrt",  math_sqrt);
			lua_register ("pow",   math_pow);
			lua_register ("min",   math_min);
			lua_register ("max",   math_max);
		}
	}
}
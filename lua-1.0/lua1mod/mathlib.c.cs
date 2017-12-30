/*
** mathlib.c
** Mathematica library to LUA
**
** Waldemar Celes Filho
** TeCGraf - PUC-Rio
** 19 May 93
*/
using System;

namespace KopiLua
{
	using lua_Object = KopiLua.Lua.Object_;		
	
	public partial class Lua
	{
		private static void math_abs ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)
		 	{ lua_error ("too few arguments to function `abs'"); return; }
		 	if (lua_isnumber(o) == 0)
		 	{ lua_error ("incorrect arguments to function `abs'"); return; }
		 	d = lua_getnumber(o);
		 	if (d < 0) d = -d;
		 	lua_pushnumber (d);
		}
	
		private static void math_sin ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)
		 	{ lua_error ("too few arguments to function `sin'"); return; }
		 	if (lua_isnumber(o) == 0)
		 	{ lua_error ("incorrect arguments to function `sin'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber (sin(d));
		}
	
	
	
		private static void math_cos ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
			if (o == null)
			{ lua_error ("too few arguments to function `cos'"); return; }
		 	if (lua_isnumber(o) == 0)
		 	{ lua_error ("incorrect arguments to function `cos'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber (cos(d));
		}
	
		
		
		internal static void math_tan ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)
		 	{ lua_error ("too few arguments to function `tan'"); return; }
		 	if (lua_isnumber(o) == 0)
		 	{ lua_error ("incorrect arguments to function `tan'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber (tan(d));
		}
			
		internal static void math_asin ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)
		 	{ lua_error ("too few arguments to function `asin'"); return; }
		 	if (lua_isnumber(o) == 0)
		 	{ lua_error ("incorrect arguments to function `asin'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber (asin(d));
		}
	
	
		internal static void math_acos ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)
		 	{ lua_error ("too few arguments to function `acos'"); return; }
		 	if (lua_isnumber(o) == 0)
		 	{ lua_error ("incorrect arguments to function `acos'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber (acos(d));
		}
	
	
	
		internal static void math_atan ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)
		 	{ lua_error ("too few arguments to function `atan'"); return; }
			if (lua_isnumber(o) == 0)
			{ lua_error ("incorrect arguments to function `atan'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber (atan(d));
		}
	
	
		internal static void math_ceil ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)
		 	{ lua_error ("too few arguments to function `ceil'"); return; }
		 	if (lua_isnumber(o) == 0)
		 	{ lua_error ("incorrect arguments to function `ceil'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber (ceil(d));
		}
	
	
		internal static void math_floor ()
		{
		 	double d;
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)
		 	{ lua_error ("too few arguments to function `floor'"); return; }
		 	if (lua_isnumber(o) == 0)
			{ lua_error ("incorrect arguments to function `floor'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber (floor(d));
		}
	
		internal static void math_mod()
		{
		 	int d1;
		 	int d2;
		 	lua_Object o1 = lua_getparam(1);
		 	lua_Object o2 = lua_getparam(2);
		 	if (lua_isnumber(o1) == 0 || lua_isnumber(o2) == 0)
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
		 	if (lua_isnumber(o) == 0)
		 	{ lua_error ("incorrect arguments to function `sqrt'"); return; }
		 	d = lua_getnumber(o);
		 	lua_pushnumber (sqrt(d));
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
		 	lua_pushnumber (pow(d1,d2));
		}
	
		internal static void math_min()
		{
		 	int i=1;
		 	double d, dmin;
		 	lua_Object o;
		 	if ((o = lua_getparam(i++)) == null)
		 	{ lua_error ("too few arguments to function `min'"); return; }
		 	if (lua_isnumber(o) == 0)
		 	{ lua_error ("incorrect arguments to function `min'"); return; }
		 	dmin = lua_getnumber(o);
			while ((o = lua_getparam(i++)) != null)
		 	{
		  		if (lua_isnumber(o) == 0)
		  		{ lua_error ("incorrect arguments to function `min'"); return; }
		  		d = lua_getnumber(o);
		  		if (d < dmin) dmin = d;
		 	}
		 	lua_pushnumber (dmin);
		}
	
	
		internal static void math_max()
		{
		 	int i=1;
		 	double d, dmax;
		 	lua_Object o;
		 	if ((o = lua_getparam(i++)) == null)
		 	{ lua_error ("too few arguments to function `max'"); return; }
		 	if (lua_isnumber(o) == 0)
		 	{ lua_error ("incorrect arguments to function `max'"); return; }
		 	dmax = lua_getnumber (o);
		 	while ((o = lua_getparam(i++)) != null)
		 	{
		  		if (lua_isnumber(o) == 0)
		  		{ lua_error ("incorrect arguments to function `max'"); return; }
		  		d = lua_getnumber (o);
		  		if (d > dmax) dmax = d;
		 	}
		 	lua_pushnumber (dmax);
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


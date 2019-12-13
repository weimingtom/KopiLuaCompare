/*
** $Id: lmathlib.c,v 1.117 2015/10/02 15:39:23 roberto Exp $
** Standard mathematical library
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace KopiLua
{
	using lua_Number = System.Double;
	using lua_Integer = System.Int32;
	using lua_Unsigned = System.UInt32;

	public partial class Lua
	{

        //#undef PI
		public const double PI = (3.141592653589793238462643383279502884);


		//#if !defined(l_rand)		/* { */
		//#if defined(LUA_USE_POSIX)
		//#define l_rand()	random()
		//#define l_srand(x)	srandom(x)
		//#define L_RANDMAX	2147483647	/* (2^31 - 1), following POSIX */
		//#else
		//#define l_rand()	rand()
		//#define l_srand(x)	srand(x)
		//#define L_RANDMAX	RAND_MAX
		//#endif
		//#endif				/* } */

		private static int math_abs (lua_State L) {
		  if (0!=lua_isinteger(L, 1)) {
		    lua_Integer n = lua_tointeger(L, 1);
		    if (n < 0) n = (lua_Integer)(0u - (lua_Unsigned)n);
		    lua_pushinteger(L, n);
		  }
		  else
		    lua_pushnumber(L, fabs(luaL_checknumber(L, 1))); //FIXME:l_tg(fabs), same below
		  return 1;
		}

		private static int math_sin (lua_State L) {
		  lua_pushnumber(L, sin(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_cos (lua_State L) {
		  lua_pushnumber(L, cos(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_tan (lua_State L) {
		  lua_pushnumber(L, tan(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_asin (lua_State L) {
		  lua_pushnumber(L, asin(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_acos (lua_State L) {
		  lua_pushnumber(L, acos(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_atan (lua_State L) {
		  lua_Number y = luaL_checknumber(L, 1);
		  lua_Number x = luaL_optnumber(L, 2, 1);
		  lua_pushnumber(L, atan2(y, x));
		  return 1;
		}


		private static int math_toint (lua_State L) {
		  int valid = 0;
		  lua_Integer n = lua_tointegerx(L, 1, ref valid);
		  if (0!=valid)
		    lua_pushinteger(L, n);
		  else {
		    luaL_checkany(L, 1);
		    lua_pushnil(L);  /* value is not convertible to integer */
		  }
		  return 1;
		}


		private static void pushnumint (lua_State L, lua_Number d) {
		  lua_Integer n = 0;
		  if (0!=lua_numbertointeger(d, ref n))  /* does 'd' fit in an integer? */
		    lua_pushinteger(L, n);  /* result is integer */
		  else
		    lua_pushnumber(L, d);  /* result is float */
		}


		private static int math_floor (lua_State L) {
		  if (0!=lua_isinteger(L, 1))
		    lua_settop(L, 1);  /* integer is its own floor */
		  else {
		    lua_Number d = floor(luaL_checknumber(L, 1));
		    pushnumint(L, d);
		  }
		  return 1;
		}


		private static int math_ceil (lua_State L) {
		  if (0!=lua_isinteger(L, 1))
		    lua_settop(L, 1);  /* integer is its own ceil */
		  else {
		    lua_Number d = ceil(luaL_checknumber(L, 1));
		    pushnumint(L, d);
		  }
		  return 1;
		}


		private static int math_fmod (lua_State L) {
		  if (0!=lua_isinteger(L, 1) && 0!=lua_isinteger(L, 2)) {
		    lua_Integer d = lua_tointeger(L, 2);
		    if ((lua_Unsigned)d + 1u <= 1u) {  /* special cases: -1 or 0 */
		      luaL_argcheck(L, d != 0, 2, "zero");
		      lua_pushinteger(L, 0);  /* avoid overflow with 0x80000... / -1 */
		    }
		    else
		      lua_pushinteger(L, lua_tointeger(L, 1) % d);
		  }
		  else		
		    lua_pushnumber(L, fmod(luaL_checknumber(L, 1), 
		                           luaL_checknumber(L, 2)));
		  return 1;
		}


		/*
		** next function does not use 'modf', avoiding problems with 'double*'
		** (which is not compatible with 'float*') when lua_Number is not
		** 'double'.
		*/
		private static int math_modf (lua_State L) {
		  if (0!=lua_isinteger(L ,1)) {
		    lua_settop(L, 1);  /* number is its own integer part */
		    lua_pushnumber(L, 0);  /* no fractional part */
		  }
		  else {		
		    lua_Number n = luaL_checknumber(L, 1);
		    /* integer part (rounds toward zero) */
		    lua_Number ip = (n < 0) ? -ceil(-n) : floor(n);
		    pushnumint(L, ip);
		    /* fractional part (test needed for inf/-inf) */
		    lua_pushnumber(L, (n == ip) ? 0.0 : (n - ip)); //FIXME:l_mathop(0.0)
		  }
		  return 2;
		}


		private static int math_sqrt (lua_State L) {
		  lua_pushnumber(L, sqrt(luaL_checknumber(L, 1)));
		  return 1;
		}


		private static int math_ult (lua_State L) {
		  lua_Integer a = luaL_checkinteger(L, 1);
		  lua_Integer b = luaL_checkinteger(L, 2);
		  lua_pushboolean(L, ((lua_Unsigned)a < (lua_Unsigned)b)?1:0);
		  return 1;
		}

		private static int math_log (lua_State L) {
		  lua_Number x = luaL_checknumber(L, 1);
		  lua_Number res;
		  if (lua_isnoneornil(L, 2))
		    res = log(x);
		  else {
		    lua_Number base_ = luaL_checknumber(L, 2);
		//#if !defined(LUA_USE_C89)
		    if (base_ == 2.0) res = log2(x); else
		//#endif			
		    if (base_ == 10.0) res = log10(x);
		    else res = log(x)/log(base_);
		  }
		  lua_pushnumber(L, res);
		  return 1;
		}

		private static int math_exp (lua_State L) {
		  lua_pushnumber(L, exp(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_deg (lua_State L) {
		  lua_pushnumber(L, luaL_checknumber(L, 1) * (180.0 / PI)); //FIXME:l_mathop(180.0)
		  return 1;
		}

		private static int math_rad (lua_State L) {
		  lua_pushnumber(L, luaL_checknumber(L, 1) * (PI / 180.0)); //FIXME:l_mathop(180.0)
		  return 1;
		}


		private static int math_min (lua_State L) {
		  int n = lua_gettop(L);  /* number of arguments */
		  int imin = 1;  /* index of current minimum value */
		  int i;
		  luaL_argcheck(L, n >= 1, 1, "value expected");
		  for (i = 2; i <= n; i++) {
		    if (0!=lua_compare(L, i, imin, LUA_OPLT))
		      imin = i;
		  }
		  lua_pushvalue(L, imin);
		  return 1;
		}


		private static int math_max (lua_State L) {
		  int n = lua_gettop(L);  /* number of arguments */
		  int imax = 1;  /* index of current maximum value */
		  int i;
		  luaL_argcheck(L, n >= 1, 1, "value expected");
		  for (i = 2; i <= n; i++) {
		    if (0!=lua_compare(L, imax, i, LUA_OPLT))
		      imax = i;
		  }
		  lua_pushvalue(L, imax);
		  return 1;
		}

		private static Random rng = new Random(); //FIXME:added
		/*
		** This function uses 'double' (instead of 'lua_Number') to ensure that
		** all bits from 'l_rand' can be represented, and that 'RANDMAX + 1.0'
		** will keep full precision (ensuring that 'r' is always less than 1.0.)
		*/
		private static int math_random (lua_State L) {
		  lua_Integer low, up;
		  //double r = (double)l_rand() * (1.0 / ((double)L_RANDMAX + 1.0));
		  double r = (lua_Number)rng.NextDouble();
		  switch (lua_gettop(L)) {  /* check number of arguments */
			case 0: {  /* no arguments */
			  lua_pushnumber(L, (lua_Number)r);  /* Number between 0 and 1 */
			  return 1;
			}
			case 1: {  /* only upper limit */
		  	  low = 1;
      		  up = luaL_checkinteger(L, 1);
			  break;
			}
			case 2: {  /* lower and upper limits */
		      low = luaL_checkinteger(L, 1);
      		  up = luaL_checkinteger(L, 2);
			  break;
			}
			default: return luaL_error(L, "wrong number of arguments");
		  }
		  /* random integer in the interval [low, up] */
		  luaL_argcheck(L, low <= up, 1, "interval is empty"); 
		  luaL_argcheck(L, low >= 0 || up <= LUA_MAXINTEGER + low, 1,
                           "interval too large");
		  r *= (double)(up - low) + 1.0;
		  lua_pushinteger(L, (lua_Integer)r + low);
		  return 1;
		}


		private static int math_randomseed (lua_State L) {
		  rng = new Random(/*(unsigned int)*/(lua_Integer)luaL_checknumber(L, 1)); //FIXME:changed - l_srand((unsigned int)luaL_checkunsigned(L, 1)); //FIXME:added, (int)
		  rng.Next(); /* discard first value to avoid undesirable correlations */ //FIXME:changed - (void)l_rand();
		  return 0;
		}


		private static int math_type (lua_State L) {
		  if (lua_type(L, 1) == LUA_TNUMBER) {
		      if (0!=lua_isinteger(L, 1))
		        lua_pushliteral(L, "integer"); 
		      else
		        lua_pushliteral(L, "float"); 
		  }
		  else {
		    luaL_checkany(L, 1);
		    lua_pushnil(L);
		  }
		  return 1;
		}


		/*
		** {==================================================================
		** Deprecated functions (for compatibility only)
		** ===================================================================
		*/
		//#if defined(LUA_COMPAT_MATHLIB)

		private static int math_cosh (lua_State L) {
		  lua_pushnumber(L, cosh(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_sinh (lua_State L) {
		  lua_pushnumber(L, sinh(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_tanh (lua_State L) {
		  lua_pushnumber(L, tanh(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_pow (lua_State L) {
		  lua_Number x = luaL_checknumber(L, 1);
		  lua_Number y = luaL_checknumber(L, 2);
		  lua_pushnumber(L, pow(x, y));
		  return 1;
		}

		private static int math_frexp (lua_State L) {
		  int e;
		  lua_pushnumber(L, frexp(luaL_checknumber(L, 1), out e));
		  lua_pushinteger(L, e);
		  return 2;
		}

		private static int math_ldexp (lua_State L) {
		  lua_Number x = luaL_checknumber(L, 1);
		  int ep = (int)luaL_checkinteger(L, 2);
		  lua_pushnumber(L, ldexp(x, ep));
		  return 1;
		}

		private static int math_log10 (lua_State L) {
		  lua_pushnumber(L, log10(luaL_checknumber(L, 1)));
		  return 1;
		}

		//#endif
		/* }================================================================== */



		private readonly static luaL_Reg[] mathlib = {
		  new luaL_Reg("abs",   math_abs),
		  new luaL_Reg("acos",  math_acos),
		  new luaL_Reg("asin",  math_asin),
		  new luaL_Reg("atan",  math_atan),
		  new luaL_Reg("ceil",  math_ceil),
		  new luaL_Reg("cos",   math_cos),
		  new luaL_Reg("deg",   math_deg),
		  new luaL_Reg("exp",   math_exp),
		  new luaL_Reg("tointeger", math_toint),
		  new luaL_Reg("floor", math_floor),
		  new luaL_Reg("fmod",   math_fmod),
		  new luaL_Reg("ult",   math_ult),
		  new luaL_Reg("log",   math_log),
		  new luaL_Reg("max",   math_max),
		  new luaL_Reg("min",   math_min),
		  new luaL_Reg("modf",   math_modf),
		  new luaL_Reg("rad",   math_rad),
		  new luaL_Reg("random",     math_random),
		  new luaL_Reg("randomseed", math_randomseed),
		  new luaL_Reg("sin",   math_sin),
		  new luaL_Reg("sqrt",  math_sqrt),
		  new luaL_Reg("tan",   math_tan),
		  new luaL_Reg("type", math_type),
		//#if defined(LUA_COMPAT_MATHLIB)
		  new luaL_Reg("atan2", math_atan),
		  new luaL_Reg("cosh",   math_cosh),
		  new luaL_Reg("sinh",   math_sinh),
		  new luaL_Reg("tanh",   math_tanh),
		  new luaL_Reg("pow",   math_pow),
		  new luaL_Reg("frexp", math_frexp),
		  new luaL_Reg("ldexp", math_ldexp),
		  new luaL_Reg("log10", math_log10),
		//#endif
		  /* placeholders */
		  new luaL_Reg("pi", null),
		  new luaL_Reg("huge", null),
		  new luaL_Reg("maxinteger", null),
		  new luaL_Reg("mininteger", null),		
		  new luaL_Reg(null, null)
		};


		/*
		** Open math library
		*/
		public static int luaopen_math (lua_State L) {
		  luaL_newlib(L, mathlib);
		  lua_pushnumber(L, PI);
		  lua_setfield(L, -2, "pi");
		  lua_pushnumber(L, (lua_Number)HUGE_VAL);
		  lua_setfield(L, -2, "huge");
		  lua_pushinteger(L, LUA_MAXINTEGER);
		  lua_setfield(L, -2, "maxinteger");
		  lua_pushinteger(L, LUA_MININTEGER);
		  lua_setfield(L, -2, "mininteger");
		  return 1;
		}

	}
}

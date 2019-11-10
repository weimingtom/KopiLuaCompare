/*
** $Id: lmathlib.c,v 1.92 2013/07/22 16:05:53 roberto Exp $
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

	public partial class Lua
	{

        //#undef PI
		public const double PI = (lua_Number)(3.1415926535897932384626433832795);
		public const double RADIANS_PER_DEGREE = (lua_Number)(PI / 180.0);


		private static int math_abs (lua_State L) {
		  lua_pushnumber(L, fabs(luaL_checknumber(L, 1))); //FIXME:l_tg(fabs), same below
		  return 1;
		}

		private static int math_sin (lua_State L) {
		  lua_pushnumber(L, sin(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_sinh (lua_State L) {
		  lua_pushnumber(L, sinh(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_cos (lua_State L) {
		  lua_pushnumber(L, cos(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_cosh (lua_State L) {
		  lua_pushnumber(L, cosh(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_tan (lua_State L) {
		  lua_pushnumber(L, tan(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_tanh (lua_State L) {
		  lua_pushnumber(L, tanh(luaL_checknumber(L, 1)));
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
		  lua_pushnumber(L, atan(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_atan2 (lua_State L) {
		  lua_pushnumber(L, atan2(luaL_checknumber(L, 1), 
		                          luaL_checknumber(L, 2)));
		  return 1;
		}

		private static int math_ceil (lua_State L) {
		  lua_pushnumber(L, ceil(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_floor (lua_State L) {
		  lua_pushnumber(L, floor(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_ifloor (lua_State L) {
		  int valid = 0;
		  lua_Integer n = lua_tointegerx(L, 1, ref valid);
		  if (valid!=0)
		    lua_pushinteger(L, n);
		  else {
		    luaL_checktype(L, 1, LUA_TNUMBER);  /* error if not a number */
		    lua_pushnil(L);  /* number with invalid integer value */
		  }
		  return 1;
		}

		private static int math_fmod (lua_State L) {
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
		  lua_Number n = luaL_checknumber(L, 1);
		  /* integer part (rounds toward zero) */
		  lua_Number ip = (n < 0) ? -floor(-n) : floor(n);
		  lua_pushnumber(L, ip);
		  /* fractionary part (test handles inf/-inf) */
		  lua_pushnumber(L, (n == ip) ? 0.0 : (n - ip));
		  return 2;
		}


		private static int math_sqrt (lua_State L) {
		  lua_pushnumber(L, sqrt(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_pow (lua_State L) {
		  lua_Number x = luaL_checknumber(L, 1);
		  lua_Number y = luaL_checknumber(L, 2);		
		  lua_pushnumber(L, pow(x, y));
		  return 1;
		}

		private static int math_log (lua_State L) {
		  lua_Number x = luaL_checknumber(L, 1);
		  lua_Number res;
		  if (lua_isnoneornil(L, 2))
		    res = log(x);
		  else {
		    lua_Number base_ = luaL_checknumber(L, 2);
		    if (base_ == (lua_Number)10.0) res = log10(x);
		    else res = log(x)/log(base_);
		  }
		  lua_pushnumber(L, res);
		  return 1;
		}

//#if defined(LUA_COMPAT_LOG10)
		private static int math_log10 (lua_State L) {
		  lua_pushnumber(L, log10(luaL_checknumber(L, 1)));
		  return 1;
		}
//#endif

		private static int math_exp (lua_State L) {
		  lua_pushnumber(L, exp(luaL_checknumber(L, 1)));
		  return 1;
		}

		private static int math_deg (lua_State L) {
		  lua_pushnumber(L, luaL_checknumber(L, 1)/RADIANS_PER_DEGREE);
		  return 1;
		}

		private static int math_rad (lua_State L) {
		  lua_pushnumber(L, luaL_checknumber(L, 1)*RADIANS_PER_DEGREE);
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
  		  int ep = luaL_checkint(L, 2);
		  lua_pushnumber(L, ldexp(x, ep));
		  return 1;
		}



		private static int math_min (lua_State L) {
		  int n = lua_gettop(L);  /* number of arguments */
		  lua_Number dmin = luaL_checknumber(L, 1);
		  int i;
		  for (i=2; i<=n; i++) {
			lua_Number d = luaL_checknumber(L, i);
			if (d < dmin)
			  dmin = d;
		  }
		  lua_pushnumber(L, dmin);
		  return 1;
		}


		private static int math_max (lua_State L) {
		  int n = lua_gettop(L);  /* number of arguments */
		  lua_Number dmax = luaL_checknumber(L, 1);
		  int i;
		  for (i=2; i<=n; i++) {
			lua_Number d = luaL_checknumber(L, i);
			if (d > dmax)
			  dmax = d;
		  }
		  lua_pushnumber(L, dmax);
		  return 1;
		}

		private static Random rng = new Random(); //FIXME:added

		private static int math_random (lua_State L) {
		  /* the `%' avoids the (rare) case of r==1, and is needed also because on
			 some systems (SunOS!) `rand()' may return a value larger than RAND_MAX */
		  //lua_Number r = (lua_Number)(rng.Next()%RAND_MAX) / (lua_Number)RAND_MAX;
			lua_Number r = (lua_Number)rng.NextDouble();
		  lua_Integer low, up;
		  switch (lua_gettop(L)) {  /* check number of arguments */
			case 0: {  /* no arguments */
			  lua_pushnumber(L, r);  /* Number between 0 and 1 */
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
		  up++;  /* change interval to [low, up) */
		  luaL_argcheck(L, up - low > 0, 1, "interval is empty");
		  lua_pushinteger(L, (lua_Integer)(r * (lua_Number)(up - low)) + low);
		  return 1;
		}


		private static int math_randomseed (lua_State L) {
		  rng = new Random((int)luaL_checkunsigned(L, 1)); //FIXME:changed - srand(luaL_checkunsigned(L, 1)); //FIXME:added, (int)
		  rng.Next(); /* discard first value to avoid undesirable correlations */ //FIXME:changed - (void)rand();
		  return 0;
		}


		private static int math_type (lua_State L) {
		  luaL_checkany(L, 1);
		  if (lua_type(L, 1) == LUA_TNUMBER) {
		      if (0!=lua_isinteger(L, 1))
		        lua_pushliteral(L, "integer"); 
		      else
		        lua_pushliteral(L, "float"); 
		  }
		  else
		    lua_pushnil(L);
		  return 1;
		}

		private readonly static luaL_Reg[] mathlib = {
		  new luaL_Reg("abs",   math_abs),
		  new luaL_Reg("acos",  math_acos),
		  new luaL_Reg("asin",  math_asin),
		  new luaL_Reg("atan2", math_atan2),
		  new luaL_Reg("atan",  math_atan),
		  new luaL_Reg("ceil",  math_ceil),
		  new luaL_Reg("cosh",   math_cosh),
		  new luaL_Reg("cos",   math_cos),
		  new luaL_Reg("deg",   math_deg),
		  new luaL_Reg("exp",   math_exp),
		  new luaL_Reg("floor", math_floor),
		  new luaL_Reg("ifloor", math_ifloor),
		  new luaL_Reg("fmod",   math_fmod),
		  new luaL_Reg("frexp", math_frexp),
		  new luaL_Reg("ldexp", math_ldexp),
//#if defined(LUA_COMPAT_LOG10)
		  new luaL_Reg("log10", math_log10),
//#endif
		  new luaL_Reg("log",   math_log),
		  new luaL_Reg("max",   math_max),
		  new luaL_Reg("min",   math_min),
		  new luaL_Reg("modf",   math_modf),
		  new luaL_Reg("pow",   math_pow),
		  new luaL_Reg("rad",   math_rad),
		  new luaL_Reg("random",     math_random),
		  new luaL_Reg("randomseed", math_randomseed),
		  new luaL_Reg("sinh",   math_sinh),
		  new luaL_Reg("sin",   math_sin),
		  new luaL_Reg("sqrt",  math_sqrt),
		  new luaL_Reg("tanh",   math_tanh),
		  new luaL_Reg("tan",   math_tan),
		  new luaL_Reg("type", math_type),
		  new luaL_Reg(null, null)
		};


		/*
		** Open math library
		*/
		public static int luaopen_math (lua_State L) {
		  luaL_newlib(L, mathlib);
		  lua_pushnumber(L, PI);
		  lua_setfield(L, -2, "pi");
		  lua_pushnumber(L, HUGE_VAL);
		  lua_setfield(L, -2, "huge");
		  return 1;
		}

	}
}

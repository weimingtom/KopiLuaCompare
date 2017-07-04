/*
** $Id: ltablib.c,v 1.53 2009/12/28 16:30:31 roberto Exp roberto $
** Library for Table Manipulation
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace KopiLua
{
	using lua_Number = System.Double;

	public partial class Lua
	{
		private static int aux_getn(lua_State L, int n)	{luaL_checktype(L, n, LUA_TTABLE); return (int)lua_rawlen(L, n);}

		private static int foreachi (lua_State L) {
		  int n = aux_getn(L, 1);
		  int i = 0; //FIXME:???not init with 0
          //if (lua_getctx(L, ref i) == LUA_YIELD) goto poscall;
		  //FIXME:start========================================
		  if (lua_getctx(L, ref i) == LUA_YIELD)
		  {
		  	 bool firsttime = true;
			  for (; i <= n; i++) {
		  	 	if (firsttime)
		  	 	{
		  	 		firsttime = false;
		  	 	}
		  	 	else
		  	 	{
					lua_pushvalue(L, 2);  /* function */
					lua_pushinteger(L, i);  /* 1st argument */
					lua_rawgeti(L, 1, i);  /* 2nd argument */
					lua_callk(L, 2, 1, i, foreachi);
		  	 	}
		  	 	//poscall:
				if (!lua_isnil(L, -1))
				  return 1;
				lua_pop(L, 1);  /* remove nil result */
			  }
			  return 0;		  	
		  }
		  //FIXME:end========================================
          luaL_checktype(L, 2, LUA_TFUNCTION);
		  for (i = 1; i <= n; i++) {
			lua_pushvalue(L, 2);  /* function */
			lua_pushinteger(L, i);  /* 1st argument */
			lua_rawgeti(L, 1, i);  /* 2nd argument */
			lua_callk(L, 2, 1, i, foreachi);
            //poscall:
			if (!lua_isnil(L, -1))
			  return 1;
			lua_pop(L, 1);  /* remove nil result */
		  }
		  return 0;
		}


		private static int foreachcont (lua_State L) {
		  for (;;) {
		    if (!lua_isnil(L, -1))
		      return 1;
		    lua_pop(L, 2);  /* remove value and result */
		    if (lua_next(L, 1) == 0)  /* no more elements? */
		      return 0;
		    lua_pushvalue(L, 2);  /* function */
		    lua_pushvalue(L, -3);  /* key */
		    lua_pushvalue(L, -3);  /* value */
		    lua_callk(L, 2, 1, 0, foreachcont);
		  }
		}


		private static int _foreach (lua_State L) {
		  luaL_checktype(L, 1, LUA_TTABLE);
		  luaL_checktype(L, 2, LUA_TFUNCTION);
		  lua_pushnil(L);  /* first key */
		  lua_pushnil(L);  /* first value */
		  lua_pushnil(L);  /* first "return" */
		  return foreachcont(L);
		}


#if LUA_COMPAT_MAXN
		private static int maxn (lua_State L) {
		  lua_Number max = 0;
		  luaL_checktype(L, 1, LUA_TTABLE);
		  lua_pushnil(L);  /* first key */
		  while (lua_next(L, 1) != 0) {
			lua_pop(L, 1);  /* remove value */
			if (lua_type(L, -1) == LUA_TNUMBER) {
			  lua_Number v = lua_tonumber(L, -1);
			  if (v > max) max = v;
			}
		  }
		  lua_pushnumber(L, max);
		  return 1;
		}
#else
		private static int maxn (lua_State L) {
		  return luaL_error(L, "function 'maxn' is deprecated");
		}
#endif


		private static int getn (lua_State L) {
		  lua_pushinteger(L, aux_getn(L, 1));
		  return 1;
		}


		private static int tinsert (lua_State L) {
		  int e = aux_getn(L, 1) + 1;  /* first empty element */
		  int pos;  /* where to insert new element */
		  switch (lua_gettop(L)) {
			case 2: {  /* called with only 2 arguments */
			  pos = e;  /* insert new element at the end */
			  break;
			}
			case 3: {
			  int i;
			  pos = luaL_checkint(L, 2);  /* 2nd argument is the position */
			  if (pos > e) e = pos;  /* `grow' array if necessary */
			  for (i = e; i > pos; i--) {  /* move up elements */
				lua_rawgeti(L, 1, i-1);
				lua_rawseti(L, 1, i);  /* t[i] = t[i-1] */
			  }
			  break;
			}
			default: {
			  return luaL_error(L, "wrong number of arguments to " + LUA_QL("insert"));
			}
		  }
		  lua_rawseti(L, 1, pos);  /* t[pos] = v */
		  return 0;
		}


		private static int tremove (lua_State L) {
		  int e = aux_getn(L, 1);
		  int pos = luaL_optint(L, 2, e);
		  if (!(1 <= pos && pos <= e))  /* position is outside bounds? */
		    return 0;  /* nothing to remove */
		  lua_rawgeti(L, 1, pos);  /* result = t[pos] */
		  for ( ;pos<e; pos++) {
			lua_rawgeti(L, 1, pos+1);
			lua_rawseti(L, 1, pos);  /* t[pos] = t[pos+1] */
		  }
		  lua_pushnil(L);
		  lua_rawseti(L, 1, e);  /* t[e] = nil */
		  return 1;
		}


		private static void addfield (lua_State L, luaL_Buffer b, int i) {
		  lua_rawgeti(L, 1, i);
		  if (lua_isstring(L, -1) == 0)
		    luaL_error(L, "invalid value (%s) at index %d in table for " + 
		                  LUA_QL("concat"), luaL_typename(L, -1), i);
		    luaL_addvalue(b);
		}


		private static int tconcat (lua_State L) {
		  luaL_Buffer b = new luaL_Buffer();
		  uint lsep;
		  int i, last;
		  CharPtr sep = luaL_optlstring(L, 2, "", out lsep);
		  luaL_checktype(L, 1, LUA_TTABLE);
		  i = luaL_optint(L, 3, 1);
		  last = luaL_opt_integer(L, luaL_checkint, 4, (int)lua_rawlen(L, 1));
		  luaL_buffinit(L, b);
		  for (; i < last; i++) {
		    addfield(L, b, i);
		    luaL_addlstring(b, sep, lsep);
		  }
		  if (i == last)  /* add last value (if interval was not empty) */
		    addfield(L, b, i);
		  luaL_pushresult(b);
		  return 1;
		}


		/*
		** {======================================================
		** Pack/unpack
		** =======================================================
		*/

		private static int pack (lua_State L) {
		  int top = lua_gettop(L);
		  lua_createtable(L, top, 1);  /* create result table */
		  /* use function environment as a temporary place to keep new table */
		  lua_replace(L, LUA_ENVIRONINDEX);
		  lua_pushinteger(L, top);  /* number of elements */
		  lua_setfield(L, LUA_ENVIRONINDEX, "n");  /* t.n = number of elements */
		  for (; top >= 1; top--)  /* assign elements */
		    lua_rawseti(L, LUA_ENVIRONINDEX, top);
		  lua_pushvalue(L, LUA_ENVIRONINDEX);  /* return new table */
		  /* remove new table from environment to allow its later collection */
		  lua_copy(L, LUA_REGISTRYINDEX, LUA_ENVIRONINDEX);
		  return 1;
		}


		private static int unpack (lua_State L) {
		  int i, e, n;
		  luaL_checktype(L, 1, LUA_TTABLE);
		  i = luaL_optint(L, 2, 1);
		  e = luaL_opt(L, luaL_checkint, 3, (int)lua_rawlen(L, 1));
		  if (i > e) return 0;  /* empty range */
		  n = e - i + 1;  /* number of elements */
		  if (n <= 0 || !lua_checkstack(L, n))  /* n <= 0 means arith. overflow */
		    return luaL_error(L, "too many results to unpack");
		  lua_rawgeti(L, 1, i);  /* push arg[i] (avoiding overflow problems) */
		  while (i++ < e)  /* push arg[i + 1...e] */
		    lua_rawgeti(L, 1, i);
		  return n;
		}

		/* }====================================================== */



		/*
		** {======================================================
		** Quicksort
		** (based on `Algorithms in MODULA-3', Robert Sedgewick;
		**  Addison-Wesley, 1993.)
		** =======================================================
		*/


		private static void set2 (lua_State L, int i, int j) {
		  lua_rawseti(L, 1, i);
		  lua_rawseti(L, 1, j);
		}

		private static int sort_comp (lua_State L, int a, int b) {
		  if (!lua_isnil(L, 2)) {  /* function? */
			int res;
			lua_pushvalue(L, 2);
			lua_pushvalue(L, a-1);  /* -1 to compensate function */
			lua_pushvalue(L, b-2);  /* -2 to compensate function and `a' */
			lua_call(L, 2, 1);
			res = lua_toboolean(L, -1);
			lua_pop(L, 1);
			return res;
		  }
		  else  /* a < b? */
			return lua_compare(L, a, b, LUA_OPLT);
		}

		private static int auxsort_loop1(lua_State L, ref int i)
		{
			lua_rawgeti(L, 1, ++i);
			return sort_comp(L, -1, -2);
		}

		private static int auxsort_loop2(lua_State L, ref int j)
		{
			lua_rawgeti(L, 1, --j);
			return sort_comp(L, -3, -1);
		}

		private static void auxsort (lua_State L, int l, int u) {
		  while (l < u) {  /* for tail recursion */
			int i, j;
			/* sort elements a[l], a[(l+u)/2] and a[u] */
			lua_rawgeti(L, 1, l);
			lua_rawgeti(L, 1, u);
			if (sort_comp(L, -1, -2) != 0)  /* a[u] < a[l]? */
			  set2(L, l, u);  /* swap a[l] - a[u] */
			else
			  lua_pop(L, 2);
			if (u-l == 1) break;  /* only 2 elements */
			i = (l+u)/2;
			lua_rawgeti(L, 1, i);
			lua_rawgeti(L, 1, l);
			if (sort_comp(L, -2, -1) != 0)  /* a[i]<a[l]? */
			  set2(L, i, l);
			else {
			  lua_pop(L, 1);  /* remove a[l] */
			  lua_rawgeti(L, 1, u);
			  if (sort_comp(L, -1, -2) != 0)  /* a[u]<a[i]? */
				set2(L, i, u);
			  else
				lua_pop(L, 2);
			}
			if (u-l == 2) break;  /* only 3 elements */
			lua_rawgeti(L, 1, i);  /* Pivot */
			lua_pushvalue(L, -1);
			lua_rawgeti(L, 1, u-1);
			set2(L, i, u-1);
			/* a[l] <= P == a[u-1] <= a[u], only need to sort from l+1 to u-2 */
			i = l; j = u-1;
			for (;;) {  /* invariant: a[l..i] <= P <= a[j..u] */
			  /* repeat ++i until a[i] >= P */
			  while (auxsort_loop1(L, ref i) != 0) { //FIXME:here changed
				if (i>=u) luaL_error(L, "invalid order function for sorting");
				lua_pop(L, 1);  /* remove a[i] */
			  }
			  /* repeat --j until a[j] <= P */
			  while (auxsort_loop2(L, ref j) != 0) { //FIXME:here changed
				if (j<=l) luaL_error(L, "invalid order function for sorting");
				lua_pop(L, 1);  /* remove a[j] */
			  }
			  if (j<i) {
				lua_pop(L, 3);  /* pop pivot, a[i], a[j] */
				break;
			  }
			  set2(L, i, j);
			}
			lua_rawgeti(L, 1, u-1);
			lua_rawgeti(L, 1, i);
			set2(L, u-1, i);  /* swap pivot (a[u-1]) with a[i] */
			/* a[l..i-1] <= a[i] == P <= a[i+1..u] */
			/* adjust so that smaller half is in [j..i] and larger one in [l..u] */
			if (i-l < u-i) {
			  j=l; i=i-1; l=i+2;
			}
			else {
			  j=i+1; i=u; u=j-2;
			}
			auxsort(L, j, i);  /* call recursively the smaller one */
		  }  /* repeat the routine for the larger one */
		}

		private static int sort (lua_State L) {
		  int n = aux_getn(L, 1);
		  luaL_checkstack(L, 40, "");  /* assume array is smaller than 2^40 */
		  if (!lua_isnoneornil(L, 2))  /* is there a 2nd argument? */
			luaL_checktype(L, 2, LUA_TFUNCTION);
		  lua_settop(L, 2);  /* make sure there is two arguments */
		  auxsort(L, 1, n);
		  return 0;
		}

		/* }====================================================== */


		private readonly static luaL_Reg[] tab_funcs = {
		  new luaL_Reg("concat", tconcat),
		  new luaL_Reg("foreach", _foreach),
		  new luaL_Reg("foreachi", foreachi),
		  new luaL_Reg("getn", getn),
		  new luaL_Reg("maxn", maxn),
		  new luaL_Reg("insert", tinsert),
		  new luaL_Reg("pack", pack),
		  new luaL_Reg("unpack", unpack),
		  new luaL_Reg("remove", tremove),
		  new luaL_Reg("sort", sort),
		  new luaL_Reg(null, null)
		};


		public static int luaopen_table (lua_State L) {
		  luaL_register(L, LUA_TABLIBNAME, tab_funcs);
#if LUA_COMPAT_UNPACK
		  /* _G.unpack = table.unpack */
		  lua_getfield(L, -1, "unpack");
		  lua_setfield(L, LUA_ENVIRONINDEX, "unpack");
#endif
		  return 1;
		}

	}
}

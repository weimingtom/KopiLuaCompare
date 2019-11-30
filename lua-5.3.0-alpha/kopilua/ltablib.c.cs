/*
** $Id: ltablib.c,v 1.73 2014/07/29 16:01:00 roberto Exp $
** Library for Table Manipulation
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

		/*
		** Structure with table-access functions
		*/
		public delegate int geti_delegate(lua_State L, int idx, lua_Integer n); 
		public delegate void seti_delegate(lua_State L, int idx, lua_Integer n); 
		public class TabA {
		  public geti_delegate geti;	
		  public seti_delegate seti;
		};


		/*
		** equivalent to 'lua_rawgeti', but not raw
		*/
		private static int geti (lua_State L, int idx, lua_Integer n) {
		  lua_pushinteger(L, n);
		  return lua_gettable(L, idx);  /* assume 'idx' is not negative */
		}


		/*
		** equivalent to 'lua_rawseti', but not raw
		*/
		private static void seti (lua_State L, int idx, lua_Integer n) {
		  lua_pushinteger(L, n);
		  lua_rotate(L, -2, 1);  /* exchange key and value */
		  lua_settable(L, idx);  /* assume 'idx' is not negative */
		}


		/*
		** Check that 'arg' has a table and set access functions in 'ta' to raw
		** or non-raw according to the presence of corresponding metamethods.
		*/
		private static void checktab (lua_State L, int arg, TabA ta) {
		  luaL_checktype(L, arg, LUA_TTABLE);
		  if (0==lua_getmetatable(L, arg)) {  /* fast track */
		    ta.geti = lua_rawgeti;  /* with no metatable, all is raw */
		    ta.seti = lua_rawseti;
		  }
		  else {
		    lua_pushliteral(L, "__index");  /* 'index' metamethod */
		    ta.geti = (lua_rawget(L, -2) == LUA_TNIL) ? (geti_delegate)lua_rawgeti : geti;
		    lua_pushliteral(L, "__newindex");  /* 'newindex' metamethod */
		    ta.seti = (lua_rawget(L, -3) == LUA_TNIL) ? (seti_delegate)lua_rawseti : seti;
		    lua_pop(L, 3);  /* pop metatable plus both metamethods */
		  }
		}
	
	
		private static int aux_getn(lua_State L, int n, TabA ta)	{checktab(L, n, ta); return luaL_len(L, n);}


//#if LUA_COMPAT_MAXN
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
//#endif


		private static int tinsert (lua_State L) {
		  TabA ta = new TabA();
		  lua_Integer e = aux_getn(L, 1, ta) + 1;  /* first empty element */
		  lua_Integer pos;  /* where to insert new element */
		  switch (lua_gettop(L)) {
			case 2: {  /* called with only 2 arguments */
			  pos = e;  /* insert new element at the end */
			  break;
			}
			case 3: {
			  lua_Integer i;
			  pos = luaL_checkinteger(L, 2);  /* 2nd argument is the position */
			  luaL_argcheck(L, 1 <= pos && pos <= e, 2, "position out of bounds");
			  for (i = e; i > pos; i--) {  /* move up elements */
				ta.geti(L, 1, i-1);
				ta.seti(L, 1, i);  /* t[i] = t[i-1] */
			  }
			  break;
			}
			default: {
			  return luaL_error(L, "wrong number of arguments to " + LUA_QL("insert"));
			}
		  }
		  ta.seti(L, 1, pos);  /* t[pos] = v */
		  return 0;
		}


		private static int tremove (lua_State L) {
		  TabA ta = new TabA();
		  lua_Integer size = aux_getn(L, 1, ta);
		  lua_Integer pos = luaL_optinteger(L, 2, size);
		  if (pos != size)  /* validate 'pos' if given */
    		luaL_argcheck(L, 1 <= pos && pos <= size + 1, 1, "position out of bounds");
		  ta.geti(L, 1, pos);  /* result = t[pos] */
		  for ( ; pos < size; pos++) {
			ta.geti(L, 1, pos+1);
			ta.seti(L, 1, pos);  /* t[pos] = t[pos+1] */
		  }
		  lua_pushnil(L);
		  ta.seti(L, 1, pos);  /* t[pos] = nil */
		  return 1;
		}


		private static int tcopy (lua_State L) {
		  TabA ta = new TabA();
		  lua_Integer f = luaL_checkinteger(L, 2);
		  lua_Integer e = luaL_checkinteger(L, 3);
		  lua_Integer t;
		  int tt = 4;  /* destination table */
		  /* the following restriction avoids several problems with overflows */
		  luaL_argcheck(L, f > 0, 2, "initial position must be positive");
		  if (lua_istable(L, tt))
		    t = luaL_checkinteger(L, 5);
		  else {
		    tt = 1;  /* destination table is equal to source */
		    t = luaL_checkinteger(L, 4);
		  }
		  if (e >= f) {  /* otherwise, nothing to move */
		    lua_Integer n, i;
		    ta.geti = (0==luaL_getmetafield(L, 1, "__index")) ? (geti_delegate)lua_rawgeti : geti;
		    ta.seti = (0==luaL_getmetafield(L, tt, "__newindex")) ? (seti_delegate)lua_rawseti : seti;
		    n = e - f + 1;  /* number of elements to move */
		    if (t > f) {
		      for (i = n - 1; i >= 0; i--) {
		        ta.geti(L, 1, f + i);
		        ta.seti(L, tt, t + i);
		      }
		    }
		    else {
		      for (i = 0; i < n; i++) {
		        ta.geti(L, 1, f + i);
		        ta.seti(L, tt, t + i);
		      }
		    }
		  }
		  lua_pushvalue(L, tt);  /* return "to table" */
		  return 1;
		}


		private static void addfield (lua_State L, luaL_Buffer b, TabA ta, lua_Integer i) {
		  ta.geti(L, 1, i);
		  if (lua_isstring(L, -1) == 0)
		    luaL_error(L, "invalid value (%s) at index %d in table for " + 
		                  LUA_QL("concat"), luaL_typename(L, -1), i);
		    luaL_addvalue(b);
		}


		private static int tconcat (lua_State L) {
		  TabA ta = new TabA();
		  luaL_Buffer b = new luaL_Buffer();
		  uint lsep;
		  lua_Integer i, last;
		  CharPtr sep = luaL_optlstring(L, 2, "", out lsep);
		  checktab(L, 1, ta);
		  i = luaL_optinteger(L, 3, 1);
		  last = luaL_opt_integer(L, luaL_checkinteger, 4, luaL_len(L, 1));
		  luaL_buffinit(L, b);
		  for (; i < last; i++) {
		    addfield(L, b, ta, i);
		    luaL_addlstring(b, sep, lsep);
		  }
		  if (i == last)  /* add last value (if interval was not empty) */
		    addfield(L, b, ta, i);
		  luaL_pushresult(b);
		  return 1;
		}


		/*
		** {======================================================
		** Pack/unpack
		** =======================================================
		*/

		private static int pack (lua_State L) {
		  int i;
		  int n = lua_gettop(L);  /* number of elements to pack */
		  lua_createtable(L, n, 1);  /* create result table */
		  lua_insert(L, 1);  /* put it at index 1 */
		  for (i = n; i >= 1; i--)  /* assign elements */
		    lua_rawseti(L, 1, i);
		  lua_pushinteger(L, n);
		  lua_setfield(L, 1, "n");  /* t.n = number of elements */
		  return 1;  /* return table */
		}


		private static int unpack (lua_State L) {
		  TabA ta = new TabA();
		  lua_Integer i, e;
		  lua_Unsigned n = 0;
		  checktab(L, 1, ta);
		  i = luaL_optinteger(L, 2, 1);
		  e = luaL_opt_integer(L, luaL_checkinteger, 3, luaL_len(L, 1));
		  if (i > e) return 0;  /* empty range */
		  n = (uint)((lua_Unsigned)e - i);  /* number of elements minus 1 (avoid overflows) */
		  if (n >= (uint)INT_MAX  || 0==lua_checkstack(L, (int)(++n)))
		    return luaL_error(L, "too many results to unpack");
		  do {  /* must have at least one element */
		    ta.geti(L, 1, i);  /* push arg[i..e] */
		  } while (i++ < e); 

		  return (int)n;
		}

		/* }====================================================== */



		/*
		** {======================================================
		** Quicksort
		** (based on `Algorithms in MODULA-3', Robert Sedgewick;
		**  Addison-Wesley, 1993.)
		** =======================================================
		*/


		private static void set2 (lua_State L, TabA ta, int i, int j) {
		  ta.seti(L, 1, i);
		  ta.seti(L, 1, j);
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

		private static int auxsort_loop1(lua_State L, ref int i, TabA ta)
		{
			ta.geti(L, 1, ++i);
			return sort_comp(L, -1, -2);
		}
		private static int auxsort_loop2(lua_State L, ref int j, TabA ta)
		{
			ta.geti(L, 1, --j);
			return sort_comp(L, -3, -1);
		}
		private static void auxsort (lua_State L, TabA ta, int l, int u) {
		  while (l < u) {  /* for tail recursion */
			int i, j;
			/* sort elements a[l], a[(l+u)/2] and a[u] */
			ta.geti(L, 1, l);
			ta.geti(L, 1, u);
			if (sort_comp(L, -1, -2) != 0)  /* a[u] < a[l]? */
			  set2(L, ta, l, u);  /* swap a[l] - a[u] */
			else
			  lua_pop(L, 2);
			if (u-l == 1) break;  /* only 2 elements */
			i = (l+u)/2;
			ta.geti(L, 1, i);
			ta.geti(L, 1, l);
			if (sort_comp(L, -2, -1) != 0)  /* a[i]<a[l]? */
			  set2(L, ta, i, l);
			else {
			  lua_pop(L, 1);  /* remove a[l] */
			  ta.geti(L, 1, u);
			  if (sort_comp(L, -1, -2) != 0)  /* a[u]<a[i]? */
				set2(L, ta, i, u);
			  else
				lua_pop(L, 2);
			}
			if (u-l == 2) break;  /* only 3 elements */
			ta.geti(L, 1, i);  /* Pivot */
			lua_pushvalue(L, -1);
			ta.geti(L, 1, u-1);
			set2(L, ta, i, u-1);
			/* a[l] <= P == a[u-1] <= a[u], only need to sort from l+1 to u-2 */
			i = l; j = u-1;
			for (;;) {  /* invariant: a[l..i] <= P <= a[j..u] */
			  /* repeat ++i until a[i] >= P */
			  while (auxsort_loop1(L, ref i, ta) != 0) { //FIXME:here changed
				if (i>=u) luaL_error(L, "invalid order function for sorting");
				lua_pop(L, 1);  /* remove a[i] */
			  }
			  /* repeat --j until a[j] <= P */
			  while (auxsort_loop2(L, ref j, ta) != 0) { //FIXME:here changed
				if (j<=l) luaL_error(L, "invalid order function for sorting");
				lua_pop(L, 1);  /* remove a[j] */
			  }
			  if (j<i) {
				lua_pop(L, 3);  /* pop pivot, a[i], a[j] */
				break;
			  }
			  set2(L, ta, i, j);
			}
			lua_rawgeti(L, 1, u-1);
			lua_rawgeti(L, 1, i);
			set2(L, ta, u-1, i);  /* swap pivot (a[u-1]) with a[i] */
			/* a[l..i-1] <= a[i] == P <= a[i+1..u] */
			/* adjust so that smaller half is in [j..i] and larger one in [l..u] */
			if (i-l < u-i) {
			  j=l; i=i-1; l=i+2;
			}
			else {
			  j=i+1; i=u; u=j-2;
			}
			auxsort(L, ta, j, i);  /* call recursively the smaller one */
		  }  /* repeat the routine for the larger one */
		}

		private static int sort (lua_State L) {
		  TabA ta = new TabA();
		  int n = (int)aux_getn(L, 1, ta);
		  luaL_checkstack(L, 50, "");  /* assume array is smaller than 2^50 */
		  if (!lua_isnoneornil(L, 2))  /* is there a 2nd argument? */
			luaL_checktype(L, 2, LUA_TFUNCTION);
		  lua_settop(L, 2);  /* make sure there are two arguments */
		  auxsort(L, ta, 1, n);
		  return 0;
		}

		/* }====================================================== */


		private readonly static luaL_Reg[] tab_funcs = {
		  new luaL_Reg("concat", tconcat),
//#if defined(LUA_COMPAT_MAXN)
		  new luaL_Reg("maxn", maxn),
//#endif
		  new luaL_Reg("insert", tinsert),
		  new luaL_Reg("pack", pack),
		  new luaL_Reg("unpack", unpack),
		  new luaL_Reg("remove", tremove),
		  new luaL_Reg("copy", tcopy),
		  new luaL_Reg("sort", sort),
		  new luaL_Reg(null, null)
		};


		public static int luaopen_table (lua_State L) {
		  luaL_newlib(L, tab_funcs);
//#if LUA_COMPAT_UNPACK
		  /* _G.unpack = table.unpack */
		  lua_getfield(L, -1, "unpack");
		  lua_setglobal(L, "unpack");
//#endif
		  return 1;
		}

	}
}

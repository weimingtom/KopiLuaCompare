/*
** $Id: lstring.c,v 2.38 2014/03/19 18:51:42 roberto Exp $
** String table (keeps all strings handled by Lua)
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KopiLua
{
	using lu_byte = System.Byte;
	using lu_int32 = System.UInt32;

	public partial class Lua
	{


		/*
		** Lua will use at most ~(2^LUAI_HASHLIMIT) bytes from a string to
		** compute its hash
		*/
		//#if !defined(LUAI_HASHLIMIT)
		public const int LUAI_HASHLIMIT	= 5;
		//#endif


		/*
		** equality for long strings
		*/
		public static int luaS_eqlngstr (TString a, TString b) {
		  uint len = a.tsv.len;
		  lua_assert(a.tsv.tt == LUA_TLNGSTR && b.tsv.tt == LUA_TLNGSTR);
		  return ((a == b) ||  /* same instance or... */
		    ((len == b.tsv.len) &&  /* equal length and ... */
		    (memcmp(getstr(a), getstr(b), len) == 0))) ? 1 : 0;  /* equal contents */
		}




		private static uint luaS_hash (CharPtr str, uint l, uint seed) {
		  uint h = seed ^ (uint)(l);
		  uint l1;
		  uint step = (l >> LUAI_HASHLIMIT) + 1;
		  for (l1 = l; l1 >= step; l1 -= step)
		    h = h ^ ((h<<5) + (h>>2) + cast_byte(str[l1 - 1]));
		  return h;
		}


		/*
		** resizes the string table
		*/
		public static void luaS_resize (lua_State L, int newsize) {
		  int i;
		  stringtable tb = G(L).strt;
		  if (newsize > tb.size) {  /* grow table if needed */
		    luaM_reallocvector(L, ref tb.hash, tb.size, newsize/*, TString * */);
		    for (i = tb.size; i < newsize; i++) 
			  tb.hash[i] = null;
		  }
		  for (i=0; i<tb.size; i++) {  /* rehash */
		    TString p = tb.hash[i];
		    tb.hash[i] = null;
		    while (p != null) {  /* for each node in the list */
		      TString hnext = p.tsv.hnext;  /* save next */
		      unsigned int h = lmod(p.tsv.hash, newsize);  /* new position */
		      p.tsv.hnext = tb.hash[h];  /* chain it */
		      tb.hash[h] = p;
		      p = hnext;
		    }
		  }
		  if (newsize < tb.size) {  /* shrink table if needed */
		    /* vanishing slice should be empty */
		    lua_assert(tb.hash[newsize] == null && tb.hash[tb.size - 1] == null);
		    luaM_reallocvector(L, ref tb.hash, tb.size, newsize/*, TString * */);
		  }
		  tb.size = newsize;
		}

		/*
		** creates a new string object
		*/
		private static TString createstrobj (lua_State L, CharPtr str, uint l,
		                              int tag, uint h) {
		  TString ts;
		  uint totalsize;  /* total size of TString object */
		  totalsize = (uint)(GetUnmanagedSize(typeof(TString)) + ((l + 1) * GetUnmanagedSize(typeof(char))));
		  ts = luaC_newobj<TString>(L, tag, totalsize).ts;
		  ts.tsv.len = l;
		  ts.tsv.hash = h;
		  ts.tsv.extra = 0;
		  memcpy(ts.str, str, l*1); //FIXME:sizeof(char) == 1
		  ts.str[l] = '\0';  /* ending 0 */
		  return ts;
		}


		LUAI_FUNC void luaS_remove (lua_State *L, TString *ts) {
		  stringtable *tb = &G(L)->strt;
		  TString **p = &tb->hash[lmod(ts->tsv.hash, tb->size)];
		  while (*p != ts)  /* find previous element */
		    p = &(*p)->tsv.hnext;
		  *p = (*p)->tsv.hnext;  /* remove element from its list */
		  tb->nuse--;
		}


		/*
		** checks whether short string exists and reuses it or creates a new one
		*/
		static TString internshrstr (lua_State L, CharPtr str, uint l) {
		  TString ts;
		  global_State g = G(L);
		  uint h = luaS_hash(str, l, g.seed);
		  TString **list = &g->strt.hash[lmod(h, g->strt.size)];
		  for (ts = *list; ts != NULL; ts = ts->tsv.hnext) {
		    if (l == ts->tsv.len &&
		        (memcmp(str, getstr(ts), l * sizeof(char)) == 0)) {
		      /* found! */
		      if (isdead(g, obj2gco(ts)))  /* dead (but not collected yet)? */
		        changewhite(obj2gco(ts));  /* resurrect it */
		      return ts;
		    }
		  }
		  if (g->strt.nuse >= g->strt.size && g->strt.size <= MAX_INT/2) {
		    luaS_resize(L, g->strt.size * 2);
		    list = &g->strt.hash[lmod(h, g->strt.size)];  /* recompute with new size */
		  }
		  ts = createstrobj(L, str, l, LUA_TSHRSTR, h);
		  ts->tsv.hnext = *list;
		  *list = ts;
		  g->strt.nuse++;
		  return ts;
		}


		/*
		** new string (with explicit length)
		*/
		public static TString luaS_newlstr (lua_State L, CharPtr str, uint l) {
		  if (l <= LUAI_MAXSHORTLEN)  /* short string? */
		    return internshrstr(L, str, l);
		  else {
		    if (l + 1 > (MAX_SIZE - GetUnmanagedSize(typeof(TString)))/GetUnmanagedSize(typeof(char)))
		      luaM_toobig(L);
		    return createstrobj(L, str, l, LUA_TLNGSTR, G(L).seed);
		  }
		}


		/*
		** new zero-terminated string
		*/
		public static TString luaS_new (lua_State L, CharPtr str) {
		  return luaS_newlstr(L, str, (uint)strlen(str)); //FIXME:added, (uint)
		}

		//FIXME:here changed
		public static Udata luaS_newudata(lua_State L, uint s)
		{
		    Udata u;
		    if (s > MAX_SIZE - GetUnmanagedSize(typeof(Udata)))
			  luaM_toobig(L);
		    u = luaC_newobj<Udata>(L, LUA_TUSERDATA, (uint)(GetUnmanagedSize(typeof(Udata)) + s)).u; //FIXME:(uint)
			u.uv.len = s;
			u.uv.metatable = null;
			setuservalue(L, u, luaO_nilobject);
			return u;
		}
		
		//FIXME:added
		public static Udata luaS_newudata(lua_State L, Type t)
		{
		    Udata u;
		    uint s = (uint)GetUnmanagedSize(t);
		    if (s > MAX_SIZE - GetUnmanagedSize(typeof(Udata)))
			  luaM_toobig(L);
		    u = luaC_newobj<Udata>(L, LUA_TUSERDATA, (uint)(GetUnmanagedSize(typeof(Udata)) + s)).u; //FIXME:(uint)
			u.uv.len = 0;//FIXME:s;
			u.uv.metatable = null;
			setuservalue(L, u, luaO_nilobject);
			u.user_data = luaM_realloc_(L, t);  //FIXME:???
			AddTotalBytes(L, GetUnmanagedSize(typeof(Udata)));  //FIXME:???
			return u;
		}
	}
}

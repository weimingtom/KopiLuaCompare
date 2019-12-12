/*
** $Id: lstring.c,v 2.56 2015/11/23 11:32:51 roberto Exp $
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
		private const string MEMERRMSG = "not enough memory";


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
		  uint len = a.u.lnglen;
		  lua_assert(a.tt == LUA_TLNGSTR && b.tt == LUA_TLNGSTR);
		  return ((a == b) ||  /* same instance or... */
		    ((len == b.u.lnglen) &&  /* equal length and ... */
		    (memcmp(getstr(a), getstr(b), len) == 0))) ? 1 : 0;  /* equal contents */
		}




		private static uint luaS_hash (CharPtr str, uint l, uint seed) {
		  uint h = seed ^ (uint)(l);
		  uint step = (l >> LUAI_HASHLIMIT) + 1;
		  for (; l >= step; l -= step)
		    h ^= ((h<<5) + (h>>2) + cast_byte(str[l - 1]));
		  return h;
		}


		public static uint luaS_hashlongstr (TString ts) {
		  lua_assert(ts.tt == LUA_TLNGSTR);
		  if (ts.extra == 0) {  /* no hash? */
		    ts.hash = luaS_hash(getstr(ts), ts.u.lnglen, ts.hash);
		    ts.extra = 1;  /* now it has its hash */
		  }
		  return ts.hash;
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
		      TString hnext = p.u.hnext;  /* save next */
		      uint h = (uint)lmod(p.hash, newsize);  /* new position */
		      p.u.hnext = tb.hash[h];  /* chain it */
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
		** Clear API string cache. (Entries cannot be empty, so fill them with
		** a non-collectable string.)
		*/
		public static void luaS_clearcache (global_State g) {
		  int i, j;
		  for (i = 0; i < STRCACHE_N; i++)
		    for (j = 0; j < STRCACHE_M; j++) {
		    if (iswhite(g.strcache[i][j]))  /* will entry be collected? */
		      g.strcache[i][j] = g.memerrmsg;  /* replace it with something fixed */
		    }
		}


		/*
		** Initialize the string table and the string cache
		*/
		public static void luaS_init (lua_State L) {
		  global_State g = G(L);
		  int i, j;
		  luaS_resize(L, MINSTRTABSIZE);  /* initial size of string table */
		  /* pre-create memory-error message */
		  g.memerrmsg = luaS_newliteral(L, MEMERRMSG);
		  luaC_fix(L, obj2gco(g.memerrmsg));  /* it should never be collected */
		  for (i = 0; i < STRCACHE_N; i++)  /* fill cache with valid strings */
		    for (j = 0; j < STRCACHE_M; j++)
		      g.strcache[i][j] = g.memerrmsg;
		}



		/*
		** creates a new string object
		*/
		private static TString createstrobj (lua_State L, uint l, int tag, uint h) {
		  TString ts;
		  GCObject o;
		  uint totalsize;  /* total size of TString object */
		  totalsize = sizelstring(l);
		  o = luaC_newobj<TString>(L, tag, totalsize);
		  ts = gco2ts(o);
		  ts.hash = h;
		  ts.extra = 0;
		  getstr(ts)[l] = '\0';  /* ending 0 */
		  return ts;
		}


		public static TString luaS_createlngstrobj (lua_State L, uint l) {
		  TString ts = createstrobj(L, l, LUA_TLNGSTR, G(L).seed);
		  ts.u.lnglen = l;
		  return ts;
		}


		public static void luaS_remove (lua_State L, TString ts) {
		  stringtable tb = G(L).strt;
		  TStringRef p = new TStringArrayRef(tb.hash, (int)lmod(ts.hash, tb.size));
		  while (p.get() != ts)  /* find previous element */
		  	p = new TStringPtrRef(p.get());
		  p.set(p.get().u.hnext);  /* remove element from its list */
		  tb.nuse--;
		}


		/*
		** checks whether short string exists and reuses it or creates a new one
		*/
		static TString internshrstr (lua_State L, CharPtr str, uint l) {
		  TString ts;
		  global_State g = G(L);
		  uint h = luaS_hash(str, l, g.seed);
		  TStringRef list = new TStringArrayRef(g.strt.hash, (int)lmod(h, g.strt.size));
		  lua_assert(str != null);  /* otherwise 'memcmp'/'memcpy' are undefined */
		  for (ts = list.get(); ts != null; ts = ts.u.hnext) {
		    if (l == ts.shrlen &&
		        (memcmp(str, getstr(ts), l * 1/*sizeof(char)*/) == 0)) { //FIXME:sizeof(char)
		      /* found! */
		      if (isdead(g, ts))  /* dead (but not collected yet)? */
		        changewhite(ts);  /* resurrect it */
		      return ts;
		    }
		  }
		  if (g.strt.nuse >= g.strt.size && g.strt.size <= MAX_INT/2) {
		    luaS_resize(L, g.strt.size * 2);
		    list = new TStringArrayRef(g.strt.hash, (int)lmod(h, g.strt.size));  /* recompute with new size */
		  }
		  ts = createstrobj(L, l, LUA_TSHRSTR, h);
  		  memcpy(getstr(ts), str, l * 1/*sizeof(char)*/);
		  ts.shrlen = cast_byte(l);
		  ts.u.hnext = list.get();
		  list.set(ts);
		  g.strt.nuse++;
		  return ts;
		}


		/*
		** new string (with explicit length)
		*/
		public static TString luaS_newlstr (lua_State L, CharPtr str, uint l) {
		  if (l <= LUAI_MAXSHORTLEN)  /* short string? */
		    return internshrstr(L, str, l);
		  else {
		    TString ts;
		    if (l >= (MAX_SIZE - GetUnmanagedSize(typeof(TString)))/GetUnmanagedSize(typeof(char)))
		      luaM_toobig(L);
		    ts = luaS_createlngstrobj(L, l);
    		memcpy(getstr(ts), str, l * 1/*sizeof(char)*/);
		    return ts;			
		  }
		}


		/*
		** Create or reuse a zero-terminated string, first checking in the
		** cache (using the string address as a key). The cache can contain
		** only zero-terminated strings, so it is safe to use 'strcmp' to
		** check hits.
		*/
		public static TString luaS_new (lua_State L, CharPtr str) {
		  uint i = point2uint(str) % STRCACHE_N;  /* hash */
		  int j;
		  TString[] p = G(L).strcache[i];
		  for (j = 0; j < STRCACHE_M; j++) {
		    if (strcmp(str, getstr(p[j])) == 0)  /* hit? */
		      return p[j];  /* that is it */
		  }
		  /* normal route */
		  for (j = STRCACHE_M - 1; j > 0; j--)
		    p[j] = p[j - 1];  /* move out last element */
		  /* new element is first in the list */
		  p[0] = luaS_newlstr(L, str, (uint)strlen(str));
		  return p[0];
		}

		//FIXME:here changed
		public static Udata luaS_newudata(lua_State L, uint s)
		{
		    Udata u;
			GCObject o;
		    if (s > MAX_SIZE - GetUnmanagedSize(typeof(Udata)))
			  luaM_toobig(L);
		    o = luaC_newobj<Udata>(L, LUA_TUSERDATA, sizeludata(s)); //FIXME:(uint)
			u = gco2u(o);
			u.len = s;
			u.metatable = null;
			setuservalue(L, u, luaO_nilobject);
			return u;
		}
		
		//FIXME:added
		public static Udata luaS_newudata(lua_State L, Type t)
		{
		    Udata u;
		    GCObject o;
			uint s = (uint)GetUnmanagedSize(t);
		    if (s > MAX_SIZE - GetUnmanagedSize(typeof(Udata)))
			  luaM_toobig(L);
		    o = luaC_newobj<Udata>(L, LUA_TUSERDATA, sizeludata(s)); //FIXME:(uint)
			u = gco2u(o);
			u.len = 0;//FIXME:s;
			u.metatable = null;
			setuservalue(L, u, luaO_nilobject);
//			throw new Exception();
			u.user_data = luaM_realloc_(L, t);  //FIXME:???
			AddTotalBytes(L, GetUnmanagedSize(typeof(Udata)));  //FIXME:???
			return u;
		}
	}
}

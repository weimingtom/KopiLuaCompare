/*
** $Id: lstring.c,v 2.24 2012/05/11 14:14:42 roberto Exp $
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


		/*
		** equality for strings
		*/
		private static int luaS_eqstr (TString a, TString b) {
		  return ((a.tsv.tt == b.tsv.tt) &&
			(a.tsv.tt == LUA_TSHRSTR ? eqshrstr(a, b) : luaS_eqlngstr(a, b)!=0)) ? 1 : 0;
		}


		private static uint luaS_hash (CharPtr str, uint l, uint seed) {
		  uint h = seed ^ l;
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
		  /* cannot resize while GC is traversing strings */
		  luaC_runtilstate(L, ~bitmask(GCSsweepstring));
		  if (newsize > tb.size) {
		    luaM_reallocvector(L, ref tb.hash, tb.size, newsize/*, GCObject * */);
		    for (i = tb.size; i < newsize; i++) tb.hash[i] = null;
		  }
		  /* rehash */
		  for (i=0; i<tb.size; i++) {
		    GCObject p = tb.hash[i];
		    tb.hash[i] = null;
		    while (p != null) {  /* for each node in the list */
		      GCObject next = gch(p).next;  /* save next */
		      uint h = (uint)lmod(gco2ts(p).hash, newsize);  /* new position */ //FIXME:(uint)lmod()
		      gch(p).next = tb.hash[h];  /* chain it */
		      tb.hash[h] = p;
              resetoldbit(p);  /* see MOVE OLD rule */
		      p = next;
		    }
		  }
		  if (newsize < tb.size) {
		    /* shrinking slice must be empty */
		    lua_assert(tb.hash[newsize] == null && tb.hash[tb.size - 1] == null);
		    luaM_reallocvector(L, ref tb.hash, tb.size, newsize/*, GCObject * */);
		  }
		  tb.size = newsize;
		}

		/*
		** creates a new string object
		*/
		private static TString createstrobj (lua_State L, CharPtr str, uint l,
		                              int tag, uint h, GCObjectRef list) {
		  TString ts;
		  uint totalsize;  /* total size of TString object */
		  totalsize = (uint)(GetUnmanagedSize(typeof(TString)) + ((l + 1) * GetUnmanagedSize(typeof(char))));
		  ts = luaC_newobj<TString>(L, tag, totalsize, list, 0).ts;
		  ts.tsv.len = l;
		  ts.tsv.hash = h;
		  ts.tsv.extra = 0;
		  memcpy(ts.str, str, l*1); //FIXME:sizeof(char) == 1
		  ts.str[l] = '\0';  /* ending 0 */
		  return ts;
		}


		/*
		** creates a new short string, inserting it into string table
		*/
		static TString newshrstr (lua_State L, CharPtr str, uint l,
		                                       uint h) {
		  GCObjectRef list;  /* (pointer to) list where it will be inserted */
		  stringtable tb = G(L).strt;
		  TString s;
		  if (tb.nuse >= (lu_int32)(tb.size) && tb.size <= MAX_INT/2)
		    luaS_resize(L, tb.size*2);  /* too crowded */
		  list = new ArrayRef(tb.hash, (int)lmod(h, tb.size));
		  s = createstrobj(L, str, l, LUA_TSHRSTR, h, list);
		  tb.nuse++;
		  return s;
		}


		/*
		** checks whether short string exists and reuses it or creates a new one
		*/
		static TString internshrstr (lua_State L, CharPtr str, uint l) {
		  GCObject o;
		  global_State g = G(L);
		  uint h = luaS_hash(str, l, g.seed);
		  for (o = g.strt.hash[lmod(h, g.strt.size)];
		       o != null;
		       o = gch(o).next) {
		    TString ts = rawgco2ts(o);
		    if (h == ts.tsv.hash &&
		        ts.tsv.len == l &&
		        (memcmp(str, getstr(ts), l * 1) == 0)) { //FIXME:sizeof(char) == 1
		      if (isdead(G(L), o))  /* string is dead (but was not collected yet)? */
		        changewhite(o);  /* resurrect it */
		      return ts;
		    }
		  }
		  return newshrstr(L, str, l, h);  /* not found; create a new string */
		}


		/*
		** new string (with explicit length)
		*/
		public static TString luaS_newlstr (lua_State L, CharPtr str, uint l) {
		  if (l <= LUAI_MAXSHORTLEN)  /* short string? */
		    return internshrstr(L, str, l);
		  else {
		    if (l + 1 > (MAX_SIZET - GetUnmanagedSize(typeof(TString)))/GetUnmanagedSize(typeof(char)))
		      luaM_toobig(L);
		    return createstrobj(L, str, l, LUA_TLNGSTR, G(L).seed, null);
		  }
		}


		/*
		** new zero-terminated string
		*/
		public static TString luaS_new (lua_State L, CharPtr str) {
		  return luaS_newlstr(L, str, (uint)strlen(str)); //FIXME:added, (uint)
		}

		//FIXME:here changed
		public static Udata luaS_newudata(lua_State L, uint s, Table e)
		{
		    Udata u;
		    if (s > MAX_SIZET - GetUnmanagedSize(typeof(Udata)))
			  luaM_toobig(L);
		    u = luaC_newobj<Udata>(L, LUA_TUSERDATA, (uint)(GetUnmanagedSize(typeof(Udata)) + s), null, 0).u; //FIXME:(uint)
			u.uv.len = s;
			u.uv.metatable = null;
			u.uv.env = e;
			return u;
		}
		
		//FIXME:added
		public static Udata luaS_newudata(lua_State L, Type t, Table e)
		{
		    Udata u;
		    uint s = (uint)GetUnmanagedSize(t);
		    if (s > MAX_SIZET - GetUnmanagedSize(typeof(Udata)))
			  luaM_toobig(L);
		    u = luaC_newobj<Udata>(L, LUA_TUSERDATA, (uint)(GetUnmanagedSize(typeof(Udata)) + s), null, 0).u; //FIXME:(uint)
			u.uv.len = 0;//FIXME:s;
			u.uv.metatable = null;
			u.uv.env = e;
			u.user_data = luaM_realloc_(L, t);  //FIXME:???
			AddTotalBytes(L, GetUnmanagedSize(typeof(Udata)));  //FIXME:???
			return u;
		}
	}
}

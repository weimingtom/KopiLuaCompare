/*
** $Id: lstring.c,v 2.18 2010/05/10 18:23:45 roberto Exp roberto $
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

	public partial class Lua
	{


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


		public static TString newlstr (lua_State L, CharPtr str, uint l,
											   uint h) {
		  uint totalsize;  /* total size of TString object */
		  GCObjectRef list;  /* (pointer to) list where it will be inserted */
		  TString ts;
		  stringtable tb = G(L).strt;
		  if (l+1 > MAX_SIZET /GetUnmanagedSize(typeof(char)))
		    luaM_toobig(L);
		  if ((tb.nuse > (int)tb.size) && (tb.size <= MAX_INT/2))
		    luaS_resize(L, tb.size*2);  /* too crowded */
		  totalsize = (uint)(GetUnmanagedSize(typeof(TString)) + ((l + 1) * GetUnmanagedSize(typeof(char))));//FIXME:(uint)
		  list = new ArrayRef(tb.hash, (int)lmod(h, tb.size)); //FIXME:(int)
		  ts = luaC_newobj<TString>(L, LUA_TSTRING, totalsize, list, 0).ts;
		  ts.tsv.len = l;
		  ts.tsv.hash = h;
		  ts.tsv.reserved = 0;
		  //memcpy(ts+1, str, l*GetUnmanagedSize(typeof(char)));
		  memcpy(ts.str.chars, str.chars, str.index, (int)l); //FIXME:changed 
		  ts.str[l] = '\0';  /* ending 0 */
		  tb.nuse++;
		  return ts;
		}


		public static TString luaS_newlstr (lua_State L, CharPtr str, uint l) {
		  GCObject o;
		  uint h = (uint)l;  /* seed */
		  uint step = (l>>5)+1;  /* if string is too long, don't hash all its chars */
		  uint l1;
		  for (l1=l; l1>=step; l1-=step)  /* compute hash */
			h = h ^ ((h<<5)+(h>>2)+(byte)str[l1-1]);
		  for (o = G(L).strt.hash[lmod(h, G(L).strt.size)];
			   o != null;
			   o = gch(o).next) {
			TString ts = rawgco2ts(o);			
			if (h == ts.tsv.hash && 
			    ts.tsv.len == l &&
				(memcmp(str, getstr(ts), l * 1) == 0)) { //FIXME: changed, sizeof(char)
			  if (isdead(G(L), o)) /* string is dead (but was not collected yet)? */
                changewhite(o);  /* resurrect it */
			  return ts;
			}
		  }
		  return newlstr(L, str, l, h);  /* not found; create a new string */
        }


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

/*
** $Id: lstring.c,v 2.12 2009/04/17 14:40:13 roberto Exp roberto $
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
		  stringtable tb = &G(L).strt;
		  if (G(L).gcstate == GCSsweepstring)
		    return;  /* cannot resize during GC traverse */
		  if (newsize > tb.size) {
		    luaM_reallocvector(L, tb.hash, tb.size, newsize, GCObject *);
		    for (i = tb.size; i < newsize; i++) tb->hash[i] = NULL;
		  }
		  /* rehash */
		  for (i=0; i<tb.size; i++) {
		    GCObject p = tb.hash[i];
		    tb->hash[i] = null;
		    while (p) {  /* for each node in the list */
		      GCObject next = gch(p).next;  /* save next */
		      unsigned int h = lmod(gco2ts(p).hash, newsize);  /* new position */
		      gch(p).next = tb.hash[h];  /* chain it */
		      tb.hash[h] = p;
		      p = next;
		    }
		  }
		  if (newsize < tb.size) {
		    /* shrinking slice must be empty */
		    lua_assert(tb.hash[newsize] == null && tb.hash[tb.size - 1] == null);
		    luaM_reallocvector(L, tb.hash, tb.size, newsize, GCObject *);
		  }
		  tb.size = newsize;
		}


		public static TString newlstr (lua_State L, CharPtr str, uint l,
											   uint h) {
		  TString ts;
		  stringtable tb = G(L).strt;
		  if (l+1 > MAX_SIZET /GetUnmanagedSize(typeof(char)))
		    luaM_toobig(L);
		  if ((tb.nuse > (int)tb.size) && (tb.size <= MAX_INT/2))
		    luaS_resize(L, tb.size*2);  /* too crowded */
		  ts = new TString(new char[l+1]);
		  AddTotalBytes(L, (int)(l + 1) * GetUnmanagedSize(typeof(char)) + GetUnmanagedSize(typeof(TString)));
		  ts.tsv.len = l;
		  ts.tsv.hash = h;
		  ts.tsv.marked = luaC_white(G(L));
		  ts.tsv.tt = LUA_TSTRING;
		  ts.tsv.reserved = 0;
		  //memcpy(ts+1, str, l*GetUnmanagedSize(typeof(char)));
		  memcpy(ts.str.chars, str.chars, str.index, (int)l);
		  ts.str[l] = '\0';  /* ending 0 */
		  h = (uint)lmod(h, tb.size);
		  ts.tsv.next = tb.hash[h];  /* chain new entry */
		  tb.hash[h] = obj2gco(ts);
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
			if (h == ts.tsv.hash && ts.tsv.len == l &&
									(memcmp(str, getstr(ts), l) == 0)) {
			  if (isdead(G(L), o)) /* string is dead (but was not collected yet)? */
                changewhite(o);  /* resurrect it */
			  return ts;
			}
		  }
		  return newlstr(L, str, l, h);  /* not found; create a new string */
        }

		//FIXME:here changed
		public static Udata luaS_newudata(lua_State L, uint s, Table e)
		{
		    Udata u;
			//FIXME:not added
		    //if (s > MAX_SIZET - sizeof(Udata))
			//  luaM_toobig(L);
			//FXIME:here changed
			u = new Udata();
			luaC_link(L, obj2gco(u), LUA_TUSERDATA);
			u.uv.len = s;
			u.uv.metatable = null;
			u.uv.env = e;
			u.user_data = new byte[s]; //FIXME:???
			return u;
		}

        //FIXME:here changed
		public static Udata luaS_newudata(lua_State L, Type t, Table e)
		{
		    Udata u;
		    //FIXME:not added
		    //if (s > MAX_SIZET - sizeof(Udata))
			//  luaM_toobig(L);
			//FXIME:here changed
			u = new Udata();
			luaC_link(L, obj2gco(u), LUA_TUSERDATA);
			u.uv.len = 0;
			u.uv.metatable = null;
			u.uv.env = e;
			u.user_data = luaM_realloc_(L, t);  //FIXME:???
			AddTotalBytes(L, GetUnmanagedSize(typeof(Udata)));  //FIXME:???
			return u;
		}

	}
}

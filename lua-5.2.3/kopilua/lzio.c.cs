/*
** $Id: lzio.c,v 1.35.1.1 2013/04/12 18:48:47 roberto Exp $
** Buffered streams
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace KopiLua
{
	public partial class Lua
	{


		public static int luaZ_fill (ZIO z) {
		  uint size;
		  lua_State L = z.L;
		  CharPtr buff;
		  lua_unlock(L);
		  buff = z.reader(L, z.data, out size);
		  lua_lock(L);
		  if (buff == null || size == 0) 
		    return EOZ;
		  z.n = size - 1;  /* discount char being returned */
		  z.p = new CharPtr(buff);
		  int result = (int)(z.p[0]); z.p.inc(); return result; //FIXME:changed, (byte)->(int)
		}


		public static void luaZ_init(lua_State L, ZIO z, lua_Reader reader, object data)
		{
		  z.L = L;
		  z.reader = reader;
		  z.data = data;
		  z.n = 0;
		  z.p = null;
		}


		/* --------------------------------------------------------------- read --- */
		public static uint luaZ_read (ZIO z, CharPtr b, uint n) {
		  b = new CharPtr(b);
		  while (n != 0) {
			uint m;
		    if (z.n == 0) {  /* no bytes in buffer? */
		      if (luaZ_fill(z) == EOZ)  /* try to read more */
		        return n;  /* no more input; return number of missing bytes */
		      else {
		        z.n++;  /* luaZ_fill consumed first byte; put it back */
		        z.p.dec(); //FIXME:--
		      }
		    }
			m = (n <= z.n) ? n : z.n;  // min. between n and z.n
			memcpy(b, z.p, m);
			z.n -= m;
			z.p += m;
			b = b + m;
			n -= m;
		  }
		  return 0;
		}

		/* ------------------------------------------------------------------------ */
		public static CharPtr luaZ_openspace (lua_State L, Mbuffer buff, uint n) {
		  if (n > buff.buffsize) {
			if (n < LUA_MINBUFFER) n = LUA_MINBUFFER;
			luaZ_resizebuffer(L, buff, (int)n);
		  }
		  return buff.buffer;
		}


	}
}

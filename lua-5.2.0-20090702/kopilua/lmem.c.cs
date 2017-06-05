/*
** $Id: lmem.c,v 1.72 2006/09/14 12:59:06 roberto Exp roberto $
** Interface to Memory Manager
** See Copyright Notice in lua.h
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KopiLua
{
	public partial class Lua
	{


		/*
		** About the realloc function:
		** void * frealloc (void *ud, void *ptr, uint osize, uint nsize);
		** (`osize' is the old size, `nsize' is the new size)
		**
		** Lua ensures that (ptr == null) iff (osize == 0).
		**
		** * frealloc(ud, null, 0, x) creates a new block of size `x'
		**
		** * frealloc(ud, p, x, 0) frees the block `p'
		** (in this specific case, frealloc must return null).
		** particularly, frealloc(ud, null, 0, 0) does nothing
		** (which is equivalent to free(null) in ANSI C)
		**
		** frealloc returns null if it cannot create or reallocate the area
		** (any reallocation to an equal or smaller size cannot fail!)
		*/



		public const int MINSIZEARRAY	= 4;


		public static T[] luaM_growaux_<T>(lua_State L, ref T[] block, ref int size,
							 int limit, CharPtr what)
		{
			T[] newblock;
			int newsize;
			if (size >= limit / 2)
			{  /* cannot double it? */
				if (size >= limit)  /* cannot grow even a little? */
					luaG_runerror(L, "too many %s (limit is %d)", what, limit);
				newsize = limit;  /* still have at least one free place */
			}
			else
			{
				newsize = size * 2;
				if (newsize < MINSIZEARRAY)
					newsize = MINSIZEARRAY;  /* minimum size */
			}
			newblock = luaM_reallocv<T>(L, block, newsize);
			size = newsize;  /* update only when everything else is OK */
			return newblock;
		}


		public static object luaM_toobig (lua_State L) {
		  luaG_runerror(L, "memory allocation error: block too big");
		  return null;  /* to avoid warnings */
		}



		/*
		** generic allocation routine.
		*/

		public static object luaM_realloc_(lua_State L, Type t)
		{
			int unmanaged_size = (int)GetUnmanagedSize(t);
			int nsize = unmanaged_size;
			object new_obj = System.Activator.CreateInstance(t);
			AddTotalBytes(L, nsize);
			return new_obj;
		}

		public static object luaM_realloc_<T>(lua_State L)
		{
			int unmanaged_size = (int)GetUnmanagedSize(typeof(T));
			int nsize = unmanaged_size;
			T new_obj = (T)System.Activator.CreateInstance(typeof(T));
			AddTotalBytes(L, nsize);
			return new_obj;
		}

		public static object luaM_realloc_<T>(lua_State L, T obj)
		{
			int unmanaged_size = (int)GetUnmanagedSize(typeof(T));
			int old_size = (obj == null) ? 0 : unmanaged_size;
			int osize = old_size * unmanaged_size;
			int nsize = unmanaged_size;
			T new_obj = (T)System.Activator.CreateInstance(typeof(T));
			SubtractTotalBytes(L, osize);
			AddTotalBytes(L, nsize);
			return new_obj;
		}

		public static object luaM_realloc_<T>(lua_State L, T[] old_block, int new_size)
		{
			int unmanaged_size = (int)GetUnmanagedSize(typeof(T));
			int old_size = (old_block == null) ? 0 : old_block.Length;
			int osize = old_size * unmanaged_size;
			int nsize = new_size * unmanaged_size;
			T[] new_block = new T[new_size];
			for (int i = 0; i < Math.Min(old_size, new_size); i++)
				new_block[i] = old_block[i];
			for (int i = old_size; i < new_size; i++)
				new_block[i] = (T)System.Activator.CreateInstance(typeof(T));
			if (CanIndex(typeof(T)))
				for (int i = 0; i < new_size; i++)
				{
					ArrayElement elem = new_block[i] as ArrayElement;
					Debug.Assert(elem != null, String.Format("Need to derive type {0} from ArrayElement", typeof(T).ToString()));
					elem.set_index(i);
					elem.set_array(new_block);
				}
			SubtractTotalBytes(L, osize);
			AddTotalBytes(L, nsize);
			return new_block;
		}

		public static bool CanIndex(Type t)
		{
			if (t == typeof(char))
				return false;
			if (t == typeof(byte))
				return false;
			if (t == typeof(int))
				return false;
			if (t == typeof(uint))
				return false;
			if (t == typeof(LocVar))
				return false;
			return true;
		}

		static void AddTotalBytes(lua_State L, int num_bytes) { G(L).totalbytes += (uint)num_bytes; }
		static void SubtractTotalBytes(lua_State L, int num_bytes) { G(L).totalbytes -= (uint)num_bytes; }

		static void AddTotalBytes(lua_State L, uint num_bytes) {G(L).totalbytes += num_bytes;}
		static void SubtractTotalBytes(lua_State L, uint num_bytes) {G(L).totalbytes -= num_bytes;}
	}
}

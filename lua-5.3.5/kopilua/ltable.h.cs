/*
** $Id: ltable.h,v 2.23.1.2 2018/05/24 19:39:05 roberto Exp $
** Lua tables (hash)
** See Copyright Notice in lua.h
*/


using System;

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	
	public partial class Lua
	{
		public static Node gnode(Table t, int i)	{return t.node[i];}
		public static TValue gval(Node n)			{return n.i_val;}
		public static int gnext(Node n)			{return n.i_key.nk.next;}
		
		public static void gnext_set(Node n, int v) { 
			n.i_key.nk.next = v; 
		}
		public static void gnext_inc(Node n, int v) { 
			n.i_key.nk.next += v; 
		}


		/* 'const' to avoid wrong writings that can mess up field 'next' */ 
		public static TValue gkey(Node n)		{ return (TValue)(n.i_key.tvk); }

		/*
		** writable version of 'gkey'; allows updates to individual fields,
		** but not to the whole (which has incompatible type)
		*/
		public static TKey_nk wgkey(Node n)		{ return n.i_key.nk; }
		
		public static void invalidateTMcache(Table t)	{ t.flags = 0; }


		/* true when 't' is using 'dummynode' as its hash part */
		public static bool isdummy(Table t)		{ return (t.lastfree == int.MinValue/*NULL*/); }


		/* allocated size for hash nodes */
		public static int allocsizenode(Table t)	{ return (isdummy(t) ? 0 : sizenode(t)); }


		/* returns the key, given the value of a table entry */
		public static TValue keyfromval(TValue v) {
			//throw new Exception(); return null; } //(gkey((Node)(object)(v)) - offsetof(Node, i_val)))); }
			if (v._parent == null) 
			{
				throw new Exception();
			}
			return gkey(v._parent);
		}
  
		//LUAI_FUNC const TValue *luaH_getint (Table *t, lua_Integer key);
		//LUAI_FUNC void luaH_setint (lua_State *L, Table *t, lua_Integer key,
		//                                                    TValue *value);
		//LUAI_FUNC const TValue *luaH_getshortstr (Table *t, TString *key);
		//LUAI_FUNC const TValue *luaH_getstr (Table *t, TString *key);
		//LUAI_FUNC const TValue *luaH_get (Table *t, const TValue *key);
		//LUAI_FUNC TValue *luaH_newkey (lua_State *L, Table *t, const TValue *key);
		//LUAI_FUNC TValue *luaH_set (lua_State *L, Table *t, const TValue *key);
		//LUAI_FUNC Table *luaH_new (lua_State *L);
		//LUAI_FUNC void luaH_resize (lua_State *L, Table *t, unsigned int nasize,
		//                                                    unsigned int nhsize);
		//LUAI_FUNC void luaH_resizearray (lua_State *L, Table *t, unsigned int nasize);
		//LUAI_FUNC void luaH_free (lua_State *L, Table *t);
		//LUAI_FUNC int luaH_next (lua_State *L, Table *t, StkId key);
		//LUAI_FUNC lua_Unsigned luaH_getn (Table *t);


		//#if defined(LUA_DEBUG)
		//LUAI_FUNC Node *luaH_mainposition (const Table *t, const TValue *key);
		//LUAI_FUNC int luaH_isdummy (const Table *t);
		//#endif

	}
}

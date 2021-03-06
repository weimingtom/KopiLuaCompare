/*
** $Id: ltable.h,v 2.16.1.2 2013/08/30 15:49:41 roberto Exp $
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
		public static TValue gkey(Node n)			{ return n.i_key.tvk; }
		public static TValue gval(Node n)			{return n.i_val;}
		public static Node gnext(Node n)			{return n.i_key.nk.next;}
		
		public static void gnext_set(Node n, Node v) { n.i_key.nk.next = v; }

		public static void invalidateTMcache(Table t)	{ t.flags = 0; }

		/* returns the key, given the value of a table entry */
		public static TValue keyfromval(object v) { throw new Exception("not implemented"); //FIXME:
			return ((gkey((Node)((object)(v)) /*- offsetof(Node, i_val)*/))); } //FIXME:(char *), - offsetof(Node, i_val)


		//LUAI_FUNC const TValue *luaH_getint (Table *t, int key);
		//LUAI_FUNC void luaH_setint (lua_State *L, Table *t, int key, TValue *value);
		//LUAI_FUNC const TValue *luaH_getstr (Table *t, TString *key);
		//LUAI_FUNC const TValue *luaH_get (Table *t, const TValue *key);
		//LUAI_FUNC TValue *luaH_newkey (lua_State *L, Table *t, const TValue *key);
		//LUAI_FUNC TValue *luaH_set (lua_State *L, Table *t, const TValue *key);
		//LUAI_FUNC Table *luaH_new (lua_State *L);
		//LUAI_FUNC void luaH_resize (lua_State *L, Table *t, int nasize, int nhsize);
		//LUAI_FUNC void luaH_resizearray (lua_State *L, Table *t, int nasize);
		//LUAI_FUNC void luaH_free (lua_State *L, Table *t);
		//LUAI_FUNC int luaH_next (lua_State *L, Table *t, StkId key);
		//LUAI_FUNC int luaH_getn (Table *t);


		//#if defined(LUA_DEBUG)
		//LUAI_FUNC Node *luaH_mainposition (const Table *t, const TValue *key);
		//LUAI_FUNC int luaH_isdummy (Node *n);
		//#endif

	}
}

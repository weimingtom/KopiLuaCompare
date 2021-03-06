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

	}
}

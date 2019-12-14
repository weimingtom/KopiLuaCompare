/*
** $Id: lzio.h,v 1.31.1.1 2017/04/19 17:20:42 roberto Exp $
** Buffered streams
** See Copyright Notice in lua.h
*/


namespace KopiLua
{
	public partial class Lua
	{
		public const int EOZ = 0xffff; //-1;			/* end of stream */ //FIXME:changed here

		public class ZIO : Zio { };

		public static int zgetc(ZIO z) { if (z.n-- > 0) {int ch = (int)(z.p[0]);z.p.inc();return ch;} else { return luaZ_fill(z); }} //FIXME:(byte)->(int)		

		
		public class Mbuffer {
		  public CharPtr buffer = new CharPtr();
		  public uint n;
		  public uint buffsize;
		};

		public static void luaZ_initbuffer(lua_State L, Mbuffer buff) { buff.buffer = null; buff.buffsize = 0; }

		public static CharPtr luaZ_buffer(Mbuffer buff)	{return buff.buffer;}
		public static uint luaZ_sizebuffer(Mbuffer buff) { return buff.buffsize; }
		public static uint luaZ_bufflen(Mbuffer buff)	{return buff.n;}

		public static void luaZ_buffremove(Mbuffer buff, int i)	{ /*buff.n -= i;*/buff.n = (uint)(buff.n - i); }
		public static void luaZ_resetbuffer(Mbuffer buff) {buff.n = 0;}


		public static void luaZ_resizebuffer(lua_State L, Mbuffer buff, int size) {
			if (buff.buffer == null) //FIXME:added
				buff.buffer = new CharPtr(); //FIXME:added
			buff.buffer = luaM_reallocvchar(L, buff.buffer,
				(int)buff.buffsize, size);
			buff.buffsize = (uint)buff.buffer.chars.Length; }

		public static void luaZ_freebuffer(lua_State L, Mbuffer buff) {luaZ_resizebuffer(L, buff, 0);}





		/* --------- Private Part ------------------ */

		public class Zio {
			public uint n;			/* bytes still unread */
			public CharPtr p;			/* current position in buffer */
			public lua_Reader reader;		/* reader function */
			public object data;			/* additional data */
			public lua_State L;			/* Lua state (for reader) */
		};
	}
}

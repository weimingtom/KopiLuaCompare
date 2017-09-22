/*
** $Id: lzio.h,v 1.31 2015/09/08 15:41:05 roberto Exp $
** Buffered streams
** See Copyright Notice in lua.h
*/


namespace KopiLua
{
	public partial class Lua
	{

		
		public const int EOZ = -1;			/* end of stream */
		
		public class ZIO : Zio {}
		
		public static int zgetc(ZIO z) { if (z.n-- > 0) {  int ch = cast_uchar(z.p[0]); z.p.inc(); return ch; } else {return luaZ_fill(z);} }
		
		
		public class Mbuffer {
			public CharPtr buffer = new CharPtr();
		  	public uint n;
		  	public uint buffsize;
		}
		
		public static void luaZ_initbuffer(lua_State L, Mbuffer buff) { buff.buffer = null; buff.buffsize = 0; }
		
		public static CharPtr luaZ_buffer(Mbuffer buff)	{ return buff.buffer; }
		public static uint luaZ_sizebuffer(Mbuffer buff) { return buff.buffsize; }
		public static uint luaZ_bufflen(Mbuffer buff) { return buff.n; }
		
		public static void luaZ_buffremove(Mbuffer buff, int i)	{ buff.n -= (uint)i; } //FIXME:
		public static void luaZ_resetbuffer(Mbuffer buff) { buff.n = 0; }
		
		//FIXME:???
		public static void luaZ_resizebuffer(lua_State L, Mbuffer buff, int size) {
			//FIXME:original
			//buff.buffer = luaM_reallocvchar(L, buff.buffer,
			//buff.buffsize, size);
			
			//-------------------
			//FIXME:after
			if (buff.buffer == null)
				buff.buffer = new CharPtr();
			luaM_reallocvector(L, ref buff.buffer.chars, (int)buff.buffsize, size);
			//-------------------
			
			buff.buffsize = (uint)size; }
		
		public static void luaZ_freebuffer(lua_State L, Mbuffer buff) { luaZ_resizebuffer(L, buff, 0); }
		
		
		//LUAI_FUNC void luaZ_init (lua_State *L, ZIO *z, lua_Reader reader,
		//                                        void *data);
		//LUAI_FUNC size_t luaZ_read (ZIO* z, void *b, size_t n);	/* read next n bytes */
		
		
		
		/* --------- Private Part ------------------ */
		
		public class Zio {
		  public uint n;			/* bytes still unread */
		  public CharPtr p;		/* current position in buffer */
		  public lua_Reader reader;		/* reader function */
		  public object data;			/* additional data */
		  public lua_State L;			/* Lua state (for reader) */
		}
		
		
		//LUAI_FUNC int luaZ_fill (ZIO *z);

	}
}

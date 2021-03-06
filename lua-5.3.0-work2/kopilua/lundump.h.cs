/*
** $Id: lundump.h,v 1.42 2014/03/11 14:22:54 roberto Exp $
** load precompiled Lua chunks
** See Copyright Notice in lua.h
*/
namespace KopiLua
{
	public partial class Lua
	{

		/* data to catch conversion errors */
		public const string LUAC_DATA = "\x19\x93\r\n\x1a\n";

		public static int LUAC_INT = cast_integer(0xABCD);
		public static double LUAC_NUM = cast_num(370.5);

		public static int MYINT(CharPtr s)	{ return (s[0]-'0');}
		public static int LUAC_VERSION = (MYINT(LUA_VERSION_MAJOR)*16+MYINT(LUA_VERSION_MINOR));
		public const int LUAC_FORMAT = 0;	/* this is the official format */	
	
		/* load one chunk; from lundump.c */
		//LUAI_FUNC Closure* luaU_undump (lua_State* L, ZIO* Z, Mbuffer* buff,
        //                        const char* name);


		/* dump one chunk; from ldump.c */
		//LUAI_FUNC int luaU_dump (lua_State* L, const Proto* f, lua_Writer w,
        //                 void* data, int strip);


	}
}

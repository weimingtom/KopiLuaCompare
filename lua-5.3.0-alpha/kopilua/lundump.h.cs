/*
** $Id: lundump.h,v 1.44 2014/06/19 18:27:20 roberto Exp $
** load precompiled Lua chunks
** See Copyright Notice in lua.h
*/
namespace KopiLua
{
	public partial class Lua
	{

		/* data to catch conversion errors */
		public const string LUAC_DATA = "\x19\x93\r\n\x1a\n";

		public static int LUAC_INT = 0x5678;
		public static double LUAC_NUM = cast_num(370.5);

		public static int MYINT(CharPtr s)	{ return (s[0]-'0');}
		public static int LUAC_VERSION = (MYINT(LUA_VERSION_MAJOR)*16+MYINT(LUA_VERSION_MINOR));
		public const int LUAC_FORMAT = 0;	/* this is the official format */	
	
		/* load one chunk; from lundump.c */
		//LUAI_FUNC LClosure* luaU_undump (lua_State* L, ZIO* Z, Mbuffer* buff,
        //                        const char* name);


		/* dump one chunk; from ldump.c */
		//LUAI_FUNC int luaU_dump (lua_State* L, const Proto* f, lua_Writer w,
        //                 void* data, int strip);


	}
}

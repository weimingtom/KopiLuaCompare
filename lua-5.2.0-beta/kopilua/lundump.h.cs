namespace KopiLua
{
	public partial class Lua
	{

		/* data to catch conversion errors */
		public const string LUAC_TAIL = "\x19\x93\r\n\x1a\n";

		/* size in bytes of header of binary files */
		public const int LUAC_HEADERSIZE		= (sizeof(LUA_SIGNATURE)-sizeof(char)+2+6+sizeof(LUAC_TAIL)-sizeof(char));
	}
}

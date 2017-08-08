namespace KopiLua
{
	public partial class Lua
	{

		/* data to catch conversion errors */
		public const string LUAC_TAIL = "\x19\x93\r\n\x1a\n";

		/* size in bytes of header of binary files */
		public readonly static int LUAC_HEADERSIZE		= (LUA_SIGNATURE.Length+2+6+LUAC_TAIL.Length); //FIXME:changed, sizeof(LUA_SIGNATURE)-sizeof(char), sizeof(LUAC_TAIL)-sizeof(char)
	}
}

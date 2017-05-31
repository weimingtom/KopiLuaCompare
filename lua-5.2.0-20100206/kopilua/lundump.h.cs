namespace KopiLua
{
	public partial class Lua
	{
		/* for header of binary files -- this is Lua 5.1 */
		public const int LUAC_VERSION		= 0x51;

		/* for header of binary files -- this is the official format */
		public const int LUAC_FORMAT		= 0;

		/* size of header of binary files */
		public const int LUAC_HEADERSIZE		= 12;
	}
}

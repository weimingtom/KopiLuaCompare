/*
** $Id: lualib.h,v 1.38 2009/07/01 16:16:40 roberto Exp roberto $
** Lua standard libraries
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace KopiLua
{
	public partial class Lua
	{
		/* Key to file-handle type */
		public const string LUA_FILEHANDLE = "FILE*";

		public const string LUA_COLIBNAME = "coroutine";
		public const string LUA_TABLIBNAME = "table";
		public const string LUA_IOLIBNAME = "io";
		public const string LUA_OSLIBNAME = "os";
		public const string LUA_STRLIBNAME = "string";
        public const string LUA_BITLIBNAME = "bit";
		public const string LUA_MATHLIBNAME = "math";
		public const string LUA_DBLIBNAME = "debug";
		public const string LUA_LOADLIBNAME = "package";

	}
}

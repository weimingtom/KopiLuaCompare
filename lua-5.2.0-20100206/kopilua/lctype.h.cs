/*
** $Id: lctype.h,v 1.7 2009/05/27 16:51:15 roberto Exp roberto $
** 'ctype' functions for Lua
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KopiLua
{   	
	public partial class Lua
	{
		
		
		public const int ALPHABIT = 0;
		public const int DIGITBIT = 1;
		public const int PRINTBIT = 2;
		public const int SPACEBIT = 3;
		public const int XDIGITBIT = 4;
		public const int UPPERBIT = 5;
		
		
		public static int MASK(int B)	 { return 1 << B; }
		
		
		/*
		** add 1 to char to allow index -1 (EOZ)
		*/
		public static int testprop(int c, int p) { return luai_ctype_[c+1] & p; }
		
		/*
		** 'lalpha' (Lua alphabetic) and 'lalnum' (Lua alphanumeric) both include '_'
		*/
		public static int lislalpha(int c) { return testprop(c, MASK(ALPHABIT)); }
		public static int lislalnum(int c) { return testprop(c, (MASK(ALPHABIT) | MASK(DIGITBIT))); }
		public static int lisupper(int c) { return testprop(c, MASK(UPPERBIT)); }
		public static int lisdigit(int c) { return testprop(c, MASK(DIGITBIT)); }
		public static int lisspace(int c) { return testprop(c, MASK(SPACEBIT)); }
		public static int lisprint(int c) { return testprop(c, MASK(PRINTBIT)); }
		public static int lisxdigit(int c) { return testprop(c, MASK(XDIGITBIT)); }


		/* one more entry for 0 and one more for -1 (EOZ) */
		//LUAI_DDEC const lu_byte luai_ctype_[UCHAR_MAX + 2];

	}
}


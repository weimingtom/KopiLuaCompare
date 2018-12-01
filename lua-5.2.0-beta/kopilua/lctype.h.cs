/*
** $Id: lctype.h,v 1.10 2011/06/24 12:25:33 roberto Exp roberto $
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


	/*
	** WARNING: the functions defined here do not necessarily correspond 
	** to the similar functions in the standard C ctype.h. They are
	** optimized for the specific needs of Lua
	*/

//#if !defined(LUA_USE_CTYPE)

//#if 'A' == 65 && '0' == 48
/* ASCII case: can use its own tables; faster and fixed */
//#define LUA_USE_CTYPE	0
//#else
/* must use standard C ctype */
//#define LUA_USE_CTYPE	1
//#endif

//#endif
		
		
#if !LUA_USE_CTYPE	///* { */

//#include <limits.h>

//#include "llimits.h"


		public const int ALPHABIT = 0;
		public const int DIGITBIT = 1;
		public const int PRINTBIT = 2;
		public const int SPACEBIT = 3;
		public const int XDIGITBIT = 4;
		
		
		public static int MASK(int B)	 { return 1 << B; }
		
		
		/*
		** add 1 to char to allow index -1 (EOZ)
		*/
		public static int testprop(int c, int p) { if (c == EOZ) {c = -1;} return luai_ctype_[c+1] & p; } //FIXME:added, if (c == EOZ) {c = -1;}
		
		/*
		** 'lalpha' (Lua alphabetic) and 'lalnum' (Lua alphanumeric) both include '_'
		*/
		public static int lislalpha(int c) { return testprop(c, MASK(ALPHABIT)); }
		public static int lislalnum(int c) { return testprop(c, (MASK(ALPHABIT) | MASK(DIGITBIT))); }
		public static int lisdigit(int c) { return testprop(c, MASK(DIGITBIT)); }
		public static int lisspace(int c) { return testprop(c, MASK(SPACEBIT)); }
		public static int lisprint(int c) { return testprop(c, MASK(PRINTBIT)); }
		public static int lisxdigit(int c) { return testprop(c, MASK(XDIGITBIT)); }

		/*
		** this 'ltolower' only works for alphabetic characters
		*/
		public static int ltolower(int c) { return ((c) | ('A' ^ 'a')); }


		/* two more entries for 0 and -1 (EOZ) */
		//LUAI_DDEC const lu_byte luai_ctype_[UCHAR_MAX + 2];


#else			///* }{ */

		/*
		** use standard C ctypes
		*/

		//#include <ctype.h>


		public static int lislalpha(c) { return (isalpha(c) != 0 || (c) == '_') ? 1 : 0; }
		public static int lislalnum(c) { return (isalnum(c) != 0 || (c) == '_') ? 1 : 0; }
		public static int lisdigit(c) { return (isdigit(c)); }
		public static int lisspace(c) { return (isspace(c)); }
		public static int lisprint(c) { return (isprint(c)); }
		public static int lisxdigit(c) { return (isxdigit(c)); }

		public static int ltolower(c) { return (tolower(c)); }

#endif			///* } */



	}
}


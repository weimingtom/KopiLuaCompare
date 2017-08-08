/*
** $Id: lctype.c,v 1.9 2011/06/23 16:00:43 roberto Exp roberto $
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
	using lu_byte = System.Byte;
    
	public partial class Lua
	{

#if !LUA_USE_CTYPE	///* { */


		
		public static lu_byte[] luai_ctype_ = new lu_byte[] {
		  0x00,  /* EOZ */
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,	/* 0. */
		  0x00,  0x08,  0x08,  0x08,  0x08,  0x08,  0x00,  0x00,
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,	/* 1. */
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,
		  0x0c,  0x04,  0x04,  0x04,  0x04,  0x04,  0x04,  0x04,	/* 2. */
		  0x04,  0x04,  0x04,  0x04,  0x04,  0x04,  0x04,  0x04,
		  0x16,  0x16,  0x16,  0x16,  0x16,  0x16,  0x16,  0x16,	/* 3. */
		  0x16,  0x16,  0x04,  0x04,  0x04,  0x04,  0x04,  0x04,
		  0x04,  0x15,  0x15,  0x15,  0x15,  0x15,  0x15,  0x05,	/* 4. */
		  0x05,  0x05,  0x05,  0x05,  0x05,  0x05,  0x05,  0x05,
		  0x05,  0x05,  0x05,  0x05,  0x05,  0x05,  0x05,  0x05,	/* 5. */
		  0x05,  0x05,  0x05,  0x04,  0x04,  0x04,  0x04,  0x05,
		  0x04,  0x15,  0x15,  0x15,  0x15,  0x15,  0x15,  0x05,	/* 6. */
		  0x05,  0x05,  0x05,  0x05,  0x05,  0x05,  0x05,  0x05,
		  0x05,  0x05,  0x05,  0x05,  0x05,  0x05,  0x05,  0x05,	/* 7. */
		  0x05,  0x05,  0x05,  0x04,  0x04,  0x04,  0x04,  0x00,
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,	/* 8. */
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,	/* 9. */
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,	/* a. */
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,	/* b. */
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,	/* c. */
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,	/* d. */
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,	/* e. */
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,	/* f. */
		  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,  0x00,
		};


#endif			///* } */
	}
}
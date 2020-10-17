/*
** lua.c
** Linguagem para Usuarios de Aplicacao
*/
using System;

namespace KopiLua
{
	using lua_Object = KopiLua.Lua.Object_;		
	
	public partial class Lua
	{
		//char *rcs_lua="$Id: lua.c,v 1.1 1993/12/17 18:41:19 celes Exp $";

		//#include <stdio.h>

		//#include "lua.h"
		//#include "lualib.h"

		public static int main (int argc, CharPtr[] argv)
		{
		 	int i;
		 	if (false) fprintf(stdout, "=================>iolib_open\n");
		 	iolib_open ();
		 	if (false) fprintf(stdout, "=================>strlib_open\n");
		 	strlib_open ();
		 	if (false) fprintf(stdout, "=================>mathlib_open\n");
		 	mathlib_open ();
		 	if (argc < 2)
		 	{
		 		CharPtr buffer = new CharPtr(new char[2048]);
		  		if (false)  fprintf(stdout, "=================>lua_dostring\n");
		   		while (gets(buffer) != null)
		     		lua_dostring(buffer);
		 	}
		 	else {
		   		if (false) fprintf(stdout, "=================>lua_dofile\n");
		   		for (i=1; i<argc; i++)
		    		lua_dofile (argv[i]);
    
		  	}
		  	return 0;
		}
	}
}

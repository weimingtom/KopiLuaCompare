/*
** lua.c
** Linguagem para Usuarios de Aplicacao
*/

namespace KopiLua
{
	using lua_Object = System.UInt32;
	using Word = System.UInt16;
	using real = System.Single;	
	using Bool = System.Int32;
	using Long = System.Int32;	
		
	public partial class Lua
	{
		//char *rcs_lua="$Id: lua.c,v 1.4 1995/02/07 16:04:15 lhf Exp $";
		
		//#include <stdio.h>
		//#include <string.h>
		
		//#include "lua.h"
		//#include "lualib.h"
		
		private static int lua_argc;
		private static CharPtr[] lua_argv;
		
		/*
		%F Allow Lua code to access argv strings.
		%i Receive from Lua the argument number (starting with 1).
		%o Return to Lua the argument, or nil if it does not exist.
		*/
		private static void lua_getargv ()
		{
			lua_Object lo = lua_getparam(1);
		 	if (0==lua_isnumber(lo))
		 		lua_pushnil();
		 	else
		 	{
		  		int n = (int)lua_getnumber(lo);
		  		if (n < 1 || n > lua_argc) lua_pushnil();
		  		else                       lua_pushstring(lua_argv[n]);
		 	}
		}
		
		
		public static int main (int argc, CharPtr[] argv)
		{
			int i;
		 	int result = 0;
		 	iolib_open ();
		 	strlib_open ();
		 	mathlib_open ();
		
		 	lua_register("argv", lua_getargv, "lua_getargv");
		
		 	if (argc < 2)
		 	{
		 		CharPtr buffer = new CharPtr(new char[250]);
		   		while (gets(buffer) != null)
		     		result = lua_dostring(buffer);
		 	}
		 	else
		 	{
		  		for (i=1; i<argc; i++)
		  		{
		   			if (strcmp(argv[i], "--") == 0)
		   			{
		    			lua_argc = argc-i-1;
		    			CharPtr[] temp = new CharPtr[argv.Length - i];
		    			for (int k = i; k < argv.Length; ++k)
		    			{
		    				temp[k - i] = argv[k];
		    			}
		    			lua_argv = temp;
		    			break;
		   			}
		  		}
		  		for (i=1; i<argc; i++)
		  		{
		   			if (strcmp(argv[i], "--") == 0)
		    			break;
		   			else
		    			result = lua_dofile (argv[i]);
		  		}
		 	}
		 	return result;
		}
	}
}


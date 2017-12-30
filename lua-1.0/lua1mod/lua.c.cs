/*
** lua.c
** Linguagem para Usuarios de Aplicacao
** TeCGraf - PUC-Rio
** 28 Apr 93
*/
using System;

namespace KopiLua
{
	using lua_Object = KopiLua.Lua.Object_;		
	
	public partial class Lua
	{
		public static void test ()
		{
		  	lua_pushobject(lua_getparam(1));
		  	lua_call ("c", 1);
		}

		private static void callfunc ()
		{
		 	lua_Object obj = lua_getparam (1);
		 	if (lua_isstring(obj) != 0) lua_call(lua_getstring(obj), 0);
		}
	
		private static void execstr ()
		{
		 	lua_Object obj = lua_getparam (1);
		 	if (lua_isstring(obj) != 0) lua_dostring(lua_getstring(obj));
		}
	
		public static int main (int argc, CharPtr[] argv)
		{
		 	int i;
		 	if (argc < 2)
		 	{
		  		puts ("usage: lua filename [functionnames]");
		  		return 0;
		 	}
			lua_register ("callfunc", callfunc);
			lua_register ("execstr", execstr);
			lua_register ("test", test);
		 	iolib_open ();
		 	strlib_open ();
		 	mathlib_open ();
		 	lua_dofile (argv[1]);
		 	for (i=2; i<argc; i++)
		 	{
		  		lua_call(argv[i], 0);
		 	}
		 	return 0;
		}
	}
}

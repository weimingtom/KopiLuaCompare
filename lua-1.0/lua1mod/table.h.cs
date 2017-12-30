/*
** table.c
** Module to control static tables
** TeCGraf - PUC-Rio
** 11 May 93
*/

namespace KopiLua
{
	public partial class Lua
	{
		//extern Symbol *lua_table;
		//extern Word lua_ntable;
	
		//extern sbyte **lua_constant;
		//extern Word lua_nconstant;
	
		//extern sbyte **lua_string;
		//extern Word lua_nstring;
	
		//extern Hash **lua_array;
		//extern Word lua_narray;
	
		//extern sbyte *lua_file[];
		//extern int lua_nfile;
	
		//#define lua_markstring(s) (*((s)-1))
		public static char lua_markstring(CharPtr s) { return s[-1]; }
		public static void lua_markstring(CharPtr s, char ch) { s[-1] = ch; }
		
		//int   lua_findsymbol           (char *s);
		//int   lua_findenclosedconstant (char *s);
		//int   lua_findconstant         (char *s);
		//void  lua_markobject           (Object *o);
		//char *lua_createstring         (char *s);
		//void *lua_createarray          (void *a);
		//int   lua_addfile              (char *fn);
		//char *lua_filename             (void);
		//void  lua_nextvar              (void);
	}
}

/*
** table.c
** Module to control static tables
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
		//char *rcs_table="$Id: table.c,v 2.28 1995/01/18 20:15:54 celes Exp $";
		
		//#include <string.h>
		
		//#include "mem.h"
		//#include "opcode.h"
		//#include "tree.h"
		//#include "hash.h"
		//#include "inout.h"
		//#include "table.h"
		//#include "lua.h"
		//#include "fallback.h"
		
		
		private const int BUFFER_BLOCK = 256;
		
		public static Symbol[] lua_table;
		private static Word lua_ntable = 0;
		private static Long lua_maxsymbol = 0;
		
		public static TaggedString[] lua_constant;
		private static Word lua_nconstant = 0;
		private static Long lua_maxconstant = 0;
		
		
		
		private const int MAXFILE = 20;
		public static CharPtr[] lua_file = new CharPtr[MAXFILE];
		public static int lua_nfile;
		
		private const int GARBAGE_BLOCK = 256;
		private const int MIN_GARBAGE_BLOCK = 10;
		
		//static void lua_nextvar (void);
		//static void setglobal (void);
		//static void getglobal (void);
		
		/*
		** Initialise symbol table with internal functions
		*/
		private static void lua_initsymbol ()
		{
			Word n;
		 	lua_maxsymbol = BUFFER_BLOCK;
		 	lua_table = newvector_Symbol(lua_maxsymbol);
		 	n = luaI_findsymbolbyname("next");
		 	s_tag(n, lua_Type.LUA_T_CFUNCTION); s_fvalue(n, lua_next, "lua_next");
		 	n = luaI_findsymbolbyname("dofile");
		 	s_tag(n, lua_Type.LUA_T_CFUNCTION); s_fvalue(n, lua_internaldofile, "lua_internaldofile");
		 	n = luaI_findsymbolbyname("setglobal");
		 	s_tag(n, lua_Type.LUA_T_CFUNCTION); s_fvalue(n, setglobal, "setglobal");
		 	n = luaI_findsymbolbyname("getglobal");
		 	s_tag(n, lua_Type.LUA_T_CFUNCTION); s_fvalue(n, getglobal, "getglobal");
		 	n = luaI_findsymbolbyname("nextvar");
		 	s_tag(n, lua_Type.LUA_T_CFUNCTION); s_fvalue(n, lua_nextvar, "lua_nextvar");
		 	n = luaI_findsymbolbyname("type"); 
		 	s_tag(n, lua_Type.LUA_T_CFUNCTION); s_fvalue(n, luaI_type, "luaI_type");
		 	n = luaI_findsymbolbyname("tonumber");
		 	s_tag(n, lua_Type.LUA_T_CFUNCTION); s_fvalue(n, lua_obj2number, "lua_obj2number");
		 	n = luaI_findsymbolbyname("print");
		 	s_tag(n, lua_Type.LUA_T_CFUNCTION); s_fvalue(n, lua_print, "lua_print");
			n = luaI_findsymbolbyname("dostring");
			s_tag(n, lua_Type.LUA_T_CFUNCTION); s_fvalue(n, lua_internaldostring, "lua_internaldostring");
		 	n = luaI_findsymbolbyname("setfallback");
		 	s_tag(n, lua_Type.LUA_T_CFUNCTION); s_fvalue(n, luaI_setfallback, "luaI_setfallback");
		 	n = luaI_findsymbolbyname("error");
		 	s_tag(n, lua_Type.LUA_T_CFUNCTION); s_fvalue(n, luaI_error, "luaI_error");
		}
		
		
		/*
		** Initialise constant table with pre-defined constants
		*/
		public static void lua_initconstant ()
		{
			lua_maxconstant = BUFFER_BLOCK;
			lua_constant = newvector_TaggedString(lua_maxconstant);
		}
		
		
		/*
		** Given a name, search it at symbol table and return its index. If not
		** found, allocate it.
		*/
		public static Word luaI_findsymbol (TreeNode t)
		{
			if (lua_table == null)
		  		lua_initsymbol(); 
		 	if (t.varindex == NOT_USED)
		 	{
		  		if (lua_ntable == lua_maxsymbol)
		  		{
		   			if (lua_maxsymbol >= MAX_WORD)
		     			lua_error("symbol table overflow");
		   			lua_maxsymbol *= 2;
		   			if (lua_maxsymbol >= MAX_WORD)
		     			lua_maxsymbol = MAX_WORD; 
		   			lua_table = growvector_Symbol(lua_table, lua_maxsymbol);
		  		}
		  		t.varindex = lua_ntable;
		  		s_tag(lua_ntable, lua_Type.LUA_T_NIL);
		  		lua_ntable++;
		 	}
		 	return t.varindex;
		}
		
		
		public static Word luaI_findsymbolbyname (CharPtr name)
		{
			return luaI_findsymbol(lua_constcreate(name));
		}
		
		
		/*
		** Given a name, search it at constant table and return its index. If not
		** found, allocate it.
		** On error, return -1.
		*/
		public static Word luaI_findconstant (TreeNode t)
		{
			if (lua_constant == null)
				lua_initconstant();
		 	if (t.constindex == NOT_USED)
		 	{
		  		if (lua_nconstant == lua_maxconstant)
		  		{
		   			if (lua_maxconstant >= MAX_WORD)
		     			lua_error("constant table overflow");
		   			lua_maxconstant *= 2;
		   			if (lua_maxconstant >= MAX_WORD)
		     			lua_maxconstant = MAX_WORD;
		   			lua_constant = growvector_TaggedString(lua_constant, lua_maxconstant);
		  		}
		  		t.constindex = lua_nconstant;
		  		lua_constant[lua_nconstant] = t.ts;
		  		lua_nconstant++;
		 	}
		 	return t.constindex;
		}
		
		
		/*
		** Traverse symbol table objects
		*/
		//void (*fn)(Object *)
		public delegate void lua_travsymbol_fn(Object_ obj);
		public static void lua_travsymbol (lua_travsymbol_fn fn)
		{
			Word i;
		 	for (i=0; i<lua_ntable; i++)
		  		fn(s_object(i));
		}
		
		
		/*
		** Mark an object if it is a string or a unmarked array.
		*/
		public static void lua_markobject (Object_ o)
		{
			if (tag(o) == lua_Type.LUA_T_STRING && (char)0==tsvalue(o).marked)
				tsvalue(o).marked = (char)1;
		 	else if (tag(o) == lua_Type.LUA_T_ARRAY)
		   		lua_hashmark (avalue(o));
		}
		
		
		/*
		** Garbage collection. 
		** Delete all unused strings and arrays.
		*/
		private static Long lua_pack_block = GARBAGE_BLOCK; /* when garbage collector will be called */
		private static Long lua_pack_nentity = 0;  /* counter of new entities (strings and arrays) */
		public static void lua_pack ()
		{
		  	Long recovered = 0;
		  	if (lua_pack_nentity++ < lua_pack_block) return;
		  	lua_travstack(lua_markobject); /* mark stack objects */
		  	lua_travsymbol(lua_markobject); /* mark symbol table objects */
		  	luaI_travlock(lua_markobject); /* mark locked objects */
		  	recovered += lua_strcollector();
		  	recovered += lua_hashcollector();
		  	lua_pack_nentity = 0;				/* reset counter */
		  	lua_pack_block=(16*lua_pack_block-7*recovered)/12;	/* adapt block size */
		  	if (lua_pack_block < MIN_GARBAGE_BLOCK) lua_pack_block = MIN_GARBAGE_BLOCK;
		} 
		
		
		/*
		** Add a file name at file table, checking overflow. This function also set
		** the external variable "lua_filename" with the function filename set.
		** Return 0 on success or error message on error.
		*/
		public static CharPtr lua_addfile (CharPtr fn)
		{
			if (lua_nfile >= MAXFILE)
		   		return "too many files";
		 	if ((lua_file[lua_nfile++] = luaI_strdup (fn)) == null)
		   		return "not enough memory";
		 	return null;
		}
		
		/*
		** Delete a file from file stack
		*/
		public static int lua_delfile ()
		{
			luaI_free_CharPtr(ref lua_file[--lua_nfile]); 
		 	return 1;
		}
		
		/*
		** Return the last file name set.
		*/
		public static CharPtr lua_filename ()
		{
			return lua_file[lua_nfile-1];
		}
		
		/*
		** Internal function: return next global variable
		*/
		private static void lua_nextvar ()
		{
			CharPtr varname;
		 	TreeNode next;
		 	lua_Object o = lua_getparam(1);
		 	if (o == LUA_NOOBJECT)
		   		lua_reportbug("too few arguments to function `nextvar'");
		 	if (lua_getparam(2) != LUA_NOOBJECT)
		   		lua_reportbug("too many arguments to function `nextvar'");
		 	if (0!=lua_isnil(o))
		   		varname = null;
		 	else if (0==lua_isstring(o))
		 	{
		   		lua_reportbug("incorrect argument to function `nextvar'"); 
		   		return;  /* to avoid warnings */
		 	}
		 	else
		   		varname = lua_getstring(o);
		 	next = lua_varnext(varname);
		 	if (next == null)
		 	{
		  		lua_pushnil();
		  		lua_pushnil();
		 	}
		 	else
		 	{
		 		Object_ name = new Object_();
		 		tag(name, lua_Type.LUA_T_STRING);
		  		tsvalue(name, next.ts);
		  		luaI_pushobject(name);
		  		luaI_pushobject(s_object(next.varindex));
		 	}
		}
		
		
		private static void setglobal ()
		{
			lua_Object name = lua_getparam(1);
		  	lua_Object value = lua_getparam(2);
		  	if (0==lua_isstring(name))
		    	lua_reportbug("incorrect argument to function `setglobal'");
		 	lua_pushobject(value);
		  	lua_storeglobal(lua_getstring(name));
		}
		
		
		private static void getglobal ()
		{
			lua_Object name = lua_getparam(1);
		  	if (0==lua_isstring(name))
		    	lua_reportbug("incorrect argument to function `getglobal'");
		  	lua_pushobject(lua_getglobal(lua_getstring(name)));
		}
	}
}


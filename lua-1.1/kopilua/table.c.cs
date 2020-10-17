/*
** table.c
** Module to control static tables
*/
namespace KopiLua
{
	using lua_Object = KopiLua.Lua.Object_;	
	using Word = System.UInt16;
	
	public partial class Lua
	{
	
		public static string rcs_table ="$Id: table.c,v 2.1 1994/04/20 22:07:57 celes Exp $";
	
		//#include <stdlib.h>
		//#include <string.h>
	
		//#include "mm.h"
	
		//#include "opcode.h"
		//#include "hash.h"
		//#include "inout.h"
		//#include "table.h"
		//#include "lua.h"
	
		//#define streq(s1,s2)	(s1[0]==s2[0]&&strcmp(s1+1,s2+1)==0)
		private static bool streq(CharPtr s1, CharPtr s2) { return (s1[0]==s2[0]&&strcmp(s1+1,s2+1)==0); }
		
		//#ifndef MAXSYMBOL
		//#define MAXSYMBOL	512
		private const int MAXSYMBOL = 512;
		//#endif
		private static Symbol[] tablebuffer = tablebuffer_init();
		private static Symbol[] tablebuffer_init() {
			Symbol[] tablebuffer_ = new Symbol[MAXSYMBOL];
			int i = 0;
			for (i = 0; i < tablebuffer_.Length; ++i)
			{
				tablebuffer_[i] = new Symbol();
			}
			i = 0;
			tablebuffer_[i++] = new Symbol("type", Type.T_CFUNCTION, lua_type);
			tablebuffer_[i++] = new Symbol("tonumber", Type.T_CFUNCTION, lua_obj2number);
			tablebuffer_[i++] = new Symbol("next", Type.T_CFUNCTION, lua_next);
		    tablebuffer_[i++] = new Symbol("nextvar", Type.T_CFUNCTION, lua_nextvar);
		    tablebuffer_[i++] = new Symbol("print", Type.T_CFUNCTION, lua_print);
		    tablebuffer_[i++] = new Symbol("dofile", Type.T_CFUNCTION, lua_internaldofile);
		    tablebuffer_[i++] = new Symbol("dostring", Type.T_CFUNCTION, lua_internaldostring);
			return tablebuffer_;
		}
		private static Symbol[] lua_table=tablebuffer;
		private static Word lua_ntable=7;
	
		private class List
		{
			public SymbolPtr s;
		 	public List next;
		 	
		 	public List()
		 	{
		 		
		 	}
		 	
		 	public List(SymbolPtr s, List next)
		 	{
		 		this.s = s;
		 		this.next = next;
		 	}
		};
	
		private static List o6 = new List(new SymbolPtr(tablebuffer, 6), null);
		private static List o5 = new List(new SymbolPtr(tablebuffer, 5), o6);
		private static List o4 = new List(new SymbolPtr(tablebuffer, 4), o5);
		private static List o3 = new List(new SymbolPtr(tablebuffer, 3), o4);
		private static List o2 = new List(new SymbolPtr(tablebuffer, 2), o3);
		private static List o1 = new List(new SymbolPtr(tablebuffer, 1), o2);
		private static List o0 = new List(new SymbolPtr(tablebuffer, 0), o1);
		private static List searchlist = o0;
	
		//#ifndef MAXCONSTANT
		//#define MAXCONSTANT	256
		private const int MAXCONSTANT = 256;
		//#endif
		/* pre-defined constants need garbage collection extra byte */ 
		private static CharPtr tm = new CharPtr(" mark");
		private static CharPtr ti = new CharPtr(" nil");
		private static CharPtr tn = new CharPtr(" number");
		private static CharPtr ts = new CharPtr(" string");
		private static CharPtr tt = new CharPtr(" table");
		private static CharPtr tf = new CharPtr(" function");
		private static CharPtr tc = new CharPtr(" cfunction");
		private static CharPtr tu = new CharPtr(" userdata");
		private static CharPtr[] constantbuffer = constantbuffer_init();
		private static CharPtr[] constantbuffer_init() {
			CharPtr[] constantbuffer_ = new CharPtr[MAXCONSTANT];
			int i = 0;
			constantbuffer_[i++] = new CharPtr(tm, 1);
			constantbuffer_[i++] = new CharPtr(ti, 1);
			constantbuffer_[i++] = new CharPtr(tn, 1);
			constantbuffer_[i++] = new CharPtr(ts, 1);
			constantbuffer_[i++] = new CharPtr(tt, 1);
			constantbuffer_[i++] = new CharPtr(tf, 1);
			constantbuffer_[i++] = new CharPtr(tc, 1);
			constantbuffer_[i++] = new CharPtr(tu, 1);
			return constantbuffer_;
		}
		public static CharPtr[] lua_constant = constantbuffer;
		public static Word lua_nconstant=(Word)((int)Type.T_USERDATA+1);
	
		//#ifndef MAXSTRING
		//#define MAXSTRING	512
		private const int MAXSTRING = 512;
		//#endif
		private static CharPtr[] stringbuffer = new CharPtr[MAXSTRING];
		public static CharPtr[] lua_string = stringbuffer;
		public static Word lua_nstring=0;
	
		//#define MAXFILE 	20
		private const int MAXFILE = 20;
		public static CharPtr[] lua_file = new CharPtr[MAXFILE];
		public static int lua_nfile;
	
	
		//#define markstring(s)   (*((s)-1))
		private static void markstring(CharPtr s, char c) { s[-1] = c; }
		private static char markstring(CharPtr s) { return s[-1]; }
	
		/* Variables to controll garbage collection */
		public static Word lua_block=10; /* to check when garbage collector will be called */
		public static Word lua_nentity;   /* counter of new entities (strings and arrays) */
	
	
		/*
		** Given a name, search it at symbol table and return its index. If not
		** found, allocate at end of table, checking oveflow and return its index.
		** On error, return -1.
		*/
		public static int lua_findsymbol(CharPtr s)
		{
		 	List l, p;
		 	for (p = null, l = searchlist; l != null; p = l, l = l.next) 
		 	{
		 		if (streq(s, l.s[0].name))
		  		{
		   			if (p != null)
		   			{
		    			p.next = l.next;
		    			l.next = searchlist;
		    			searchlist = l;
		   			}
		   			return (l.s - lua_table);
		  		}
		 	}

		 	if (lua_ntable >= MAXSYMBOL-1)
		 	{
		  		lua_error ("symbol table overflow");
		  		return -1;
		 	}
		 	s_name(lua_ntable, strdup(s));
		 	if (s_name(lua_ntable) == null)
		 	{
		  		lua_error ("not enough memory");
		  		return -1;
		 	}
		 	s_tag(lua_ntable, Type.T_NIL);
		 	p = malloc_List();
		 	p.s = new SymbolPtr(lua_table, lua_ntable);
		 	p.next = searchlist;
		 	searchlist = p;
	
		 	return lua_ntable++;
		}
	
		/*
		** Given a constant string, search it at constant table and return its index.
		** If not found, allocate at end of the table, checking oveflow and return 
		** its index.
		**
		** For each allocation, the function allocate a extra char to be used to
		** mark used string (it's necessary to deal with constant and string 
		** uniformily). The function store at the table the second position allocated,
		** that represents the beginning of the real string. On error, return -1.
		** 
		*/
		public static int lua_findconstant(CharPtr s)
		{
		 	int i;
		 	for (i =0; i<lua_nconstant; i++)
  		  		if (streq(s,lua_constant[i]))
		   			return i;
		 	if (lua_nconstant >= MAXCONSTANT-1)
		 	{
		  		lua_error ("lua: constant string table overflow");
		  		return -1;
		 	}
		 	{
		 		CharPtr c = calloc_char(strlen(s) + 2);
		 		c.inc(); // create mark space
		  		lua_constant[lua_nconstant++] = strcpy(c,s);
		 	}
		 	return (lua_nconstant-1);
		}
	
	
		/*
		** Traverse symbol table objects
		*/
		public delegate void fnDelegate(Object_ NamelessParameter);
		public static void lua_travsymbol (fnDelegate fn)
		{
		 	int i;
		 	for (i = 0; i < lua_ntable; i++)
		  		fn(s_object(i));
		}
	
	
		/*
		** Mark an object if it is a string or a unmarked array.
		*/
		public static void lua_markobject(Object_ o)
		{
		 	if (tag(o) == Type.T_STRING)
		 		markstring (svalue(o), (char)1);
		 	else if (tag(o) == Type.T_ARRAY)
		   		lua_hashmark (avalue(o));
		}
	
	
		/*
		** Garbage collection. 
		** Delete all unused strings and arrays.
		*/
		public static void lua_pack ()
		{
		 // mark stack strings
		 lua_travstack(lua_markobject);
	
		 // mark symbol table strings
		 lua_travsymbol(lua_markobject);
	
		 lua_stringcollector();
		 lua_hashcollector();
	
		 lua_nentity = 0; // reset counter
		}
	
		/*
		** Garbage collection to atrings.
		** Delete all unmarked strings
		*/
		public static void lua_stringcollector ()
		{
		 	int i, j;
		 	for (i = j = 0; i < lua_nstring; i++)
		 	{
	  		 	if (markstring(lua_string[i]) == 1)
			  	{
	  		 		lua_string[j++] = new CharPtr(lua_string[i]);
			   		markstring(lua_string[i], (char)0);
			  	}
			  	else
			  	{
			   		free (lua_string[i] - 1); //FIXME: not implemented
			  	}
		 	}
		 	lua_nstring = (Word)j;
		}
	
		/*
		** Allocate a new string at string table. The given string is already 
		** allocated with mark space and the function puts it at the end of the
		** table, checking overflow, and returns its own pointer, or NULL on error.
		*/
		public static CharPtr lua_createstring (CharPtr s)
		{
		 	int i;
		 	if (s == null) return null;
	
		 	for (i = 0; i < lua_nstring; i++)
		 	{
  		  		if (streq(s,lua_string[i]))
		  		{
		   			free(s - 1);
		   			return new CharPtr(lua_string[i]);
		  		}
		 	}
		 	if (lua_nentity == lua_block || lua_nstring >= MAXSTRING-1)
		 	{
		  		lua_pack ();
		  		if (lua_nstring >= MAXSTRING-1)
		  		{
		   			lua_error ("string table overflow");
		   			return null;
		  		}
		 	}
		 	lua_string[lua_nstring++] = new CharPtr(s);
		 	lua_nentity++;
		 	return new CharPtr(s);
		}
	
		/*
		** Add a file name at file table, checking overflow. This function also set
		** the external variable "lua_filename" with the function filename set.
		** Return 0 on success or 1 on error.
		*/
		public static int lua_addfile (CharPtr fn)
		{
		 	if (lua_nfile >= MAXFILE-1)
		 	{
		  		lua_error ("too many files");
		  		return 1;
		 	}
 		 	if ((lua_file[lua_nfile++] = strdup (fn)) == null)
		 	{
		  		lua_error ("not enough memory");
		  		return 1;
		 	}
		 	return 0;
		}
	
		/*
		** Delete a file from file stack
		*/
		public static int lua_delfile ()
		{
		 	lua_nfile--;
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
		public static void lua_nextvar ()
		{
		 	int index;
		 	Object_ o = lua_getparam (1);
		 	if (o == null)
		 	{ 
		 		lua_error ("too few arguments to function `nextvar'"); 
		 		return; 
		 	}
		 	if (lua_getparam (2) != null)
		 	{ 
		 		lua_error ("too many arguments to function `nextvar'"); 
		 		return; 
		 	}
		 	if (tag(o) == Type.T_NIL)
		 	{
		  		index = 0;
		 	}
 		 	else if (tag(o) != Type.T_STRING) 
		 	{
		  		lua_error ("incorrect argument to function `nextvar'");
		  		return;
		 	}
		 	else
		 	{
		  		for (index = 0; index < lua_ntable; index++)
		  		{
   		    		if (streq(s_name(index),svalue(o))) break;
		  			if (index == lua_ntable)
		  			{
		   				lua_error ("name not found in function `nextvar'");
		   				return;
		  			}
		  		}
		  		index++;
  				while (index < lua_ntable && tag(s_object(index)) == Type.T_NIL) 
  					index++;
	
		  		if (index == lua_ntable)
		  		{
					lua_pushnil();
				   	lua_pushnil();
				   	return;
		  		}
		 	}
		 	{
		 		Object_ name = new Object_();
		 		tag(name, Type.T_STRING);
		 		svalue(name, lua_createstring(lua_strdup(s_name(index))));
		  		if (0!=lua_pushobject (name)) return;
		  		if (0!=lua_pushobject (s_object(index))) return;
		 	}
		}


		public static int lua_findenclosedconstant (CharPtr s)
		{
			int i, j, l = (int)strlen(s);
			CharPtr c = calloc_char ((uint)l); // make a copy
	
		 	c.inc(); // create mark space
	
		 	// introduce scape characters
		 	for (i = 1,j = 0; i < l - 1; i++)
		 	{
		  		if (s[i] == '\\')
		  		{
					switch (s[++i])
				   	{
					case 'n': c[j++] = '\n'; break;
					case 't': c[j++] = '\t'; break;
					case 'r': c[j++] = '\r'; break;
					default : c[j++] = '\\'; c[j++] = c[i]; break;
				   	}
		  		}
		  		else
		  		{
		   			c[j++] = s[i];
		  		}
		 	}
		 	c[j++] = (char)0;
	
		 	for (i = 0; i < lua_nconstant; i++)
		 	{
  		  		if (streq(c,lua_constant[i]))
		  		{
		   			free (c-1);
		   			return i;
		  		}
		 	}
         	if (lua_nconstant >= MAXCONSTANT-1)
		 	{
		  		lua_error ("lua: constant string table overflow");
		  		return -1;
		 	}
		 	lua_constant[lua_nconstant++] = c;
		 	return (lua_nconstant-1);
		}
	}
}

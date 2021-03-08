/*
** inout.c
** Provide function to realise the input/output function and debugger 
** facilities.
** Also provides some predefined lua functions.
*/
using System;

namespace KopiLua
{
	using lua_Object = System.UInt32;
	using Word = System.UInt16;
	using real = System.Single;	
	using Bool = System.Int32;
	using Long = System.Int32;
	
	public partial class Lua
	{
		//char *rcs_inout="$Id: inout.c,v 2.16 1994/12/20 21:20:36 roberto Exp $";
		
		//#include <stdio.h>
		//#include <stdlib.h>
		//#include <string.h>
		
		//#include "mem.h"
		//#include "opcode.h"
		//#include "hash.h"
		//#include "inout.h"
		//#include "table.h"
		//#include "tree.h"
		//#include "lua.h"
		
		/* Exported variables */
		public static Word lua_linenumber;
		public static Bool lua_debug;
		public static Word lua_debugline = 0;
		
		
		/* Internal variables */
		 
		//#ifndef MAXFUNCSTACK
		private const int MAXFUNCSTACK = 100;
		//#endif
		 
		public class FuncStackNode {
		  public FuncStackNode next;
		  public CharPtr file;
		  public Word function;
		  public Word line;
		}
		 
		private static FuncStackNode funcStack = null;
		private static Word nfuncstack = 0;
		
		private static FILE fp;
		private static CharPtr st;
		
		/*
		** Function to get the next character from the input file
		*/
		private static int fileinput ()
		{
			return fgetc (fp);
		}
		
		/*
		** Function to get the next character from the input string
		*/
		private static int stringinput ()
		{
			int result = (int)st[0]; st.inc(); return result;
		}
		
		/*
		** Function to open a file to be input unit. 
		** Return 0 on success or error message on error.
		*/
		private static CharPtr lua_openfile_buff = new CharPtr(new char[32]);
		public static CharPtr lua_openfile (CharPtr fn)
		{
			lua_linenumber = 1;
		 	lua_setinput (fileinput);
		 	fp = fopen (fn, "r");
		 	if (fp == null)
		 	{
		 		sprintf(lua_openfile_buff, "unable to open file %.10s", fn);
		   		return lua_openfile_buff;
		 	}
		 	return lua_addfile (fn);
		}
		
		/*
		** Function to close an opened file
		*/
		public static void lua_closefile ()
		{
			if (fp != null)
		 	{
		  		lua_delfile();
		  		fclose (fp);
		  		fp = null;
		 	}
		}
		
		/*
		** Function to open a string to be input unit
		*/
		public static CharPtr lua_openstring (CharPtr s)
		{
			lua_linenumber = 1;
		 	lua_setinput (stringinput);
		 	st = new CharPtr(s);
		 	{
		 		CharPtr sn = new CharPtr(new char[64]);
		  		sprintf (sn, "String: %10.10s...", s);
		  		return lua_addfile (sn);
		 	}
		}
		
		/*
		** Function to close an opened string
		*/
		public static void lua_closestring ()
		{
			lua_delfile();
		}
		
		
		/*
		** Called to execute  SETFUNCTION opcode, this function pushs a function into
		** function stack.
		*/
		public static void lua_pushfunction (CharPtr file, Word function)
		{
		 	FuncStackNode newNode;
		 	if (nfuncstack++ >= MAXFUNCSTACK)
		 	{
		  		lua_reportbug("function stack overflow");
		 	}
		 	newNode = new_FuncStackNode();
		 	newNode.function = function;
		 	newNode.file = new CharPtr(file);
		 	newNode.line= lua_debugline;
		 	newNode.next = funcStack;
		 	funcStack = newNode;
		}
		
		/*
		** Called to execute RESET opcode, this function pops a function from 
		** function stack.
		*/
		public static void lua_popfunction ()
		{
			FuncStackNode temp = funcStack;
		 	if (temp == null) return;
		 	--nfuncstack;
		 	lua_debugline = temp.line;
		 	funcStack = temp.next;
		 	luaI_free_FuncStackNode(ref temp);
		}
		
		/*
		** Report bug building a message and sending it to lua_error function.
		*/
		public static void lua_reportbug (CharPtr s)
		{
			CharPtr msg = new CharPtr(new char[MAXFUNCSTACK*80]);
		 	strcpy (msg, s);
		 	if (lua_debugline != 0)
		 	{
		  		if (funcStack != null)
		  		{
		   			FuncStackNode func = funcStack;
		   			int line = lua_debugline;
		   			sprintf (strchr(msg,(char)0), "\n\tactive stack:\n");
				   	do
				   	{
				    	sprintf (strchr(msg,(char)0),
				       		"\t-> function \"%s\" at file \"%s\":%u\n", 
				              lua_constant[func.function].str, func.file, line);
				     	line = func.line;
				     	func = func.next;
				     	lua_popfunction();
				   	} while (func!=null);
				}
				else
				{
					sprintf (strchr(msg,(char)0),
				   		"\n\tin statement begining at line %u of file \"%s\"", 
				         lua_debugline, lua_filename());
				}
			}
		 	lua_error (msg);
		}
		
		 
		/*
		** Internal function: do a string
		*/
		public static void lua_internaldostring ()
		{
			lua_Object obj = lua_getparam (1);
		 	if (0!=lua_isstring(obj) && 0==lua_dostring(lua_getstring(obj)))
		  		lua_pushnumber(1);
		 	else
		  		lua_pushnil();
		}
		
		/*
		** Internal function: do a file
		*/
		public static void lua_internaldofile ()
		{
			lua_Object obj = lua_getparam (1);
		 	if (0!=lua_isstring(obj) && 0==lua_dofile(lua_getstring(obj)))
		  		lua_pushnumber(1);
		 	else
		  		lua_pushnil();
		}
		 
		/*
		** Internal function: print object values
		*/
		public static void lua_print ()
		{
		 	int i=1;
		 	lua_Object obj;
		 	while ((obj=lua_getparam (i++)) != LUA_NOOBJECT)
		 	{
		  		if      (0!=lua_isnumber(obj))    printf("%g\n",lua_getnumber(obj));
		  		else if (0!=lua_isstring(obj))    printf("%s\n",lua_getstring(obj));
		  		else if (0!=lua_isfunction(obj))  printf("function: %p\n",bvalue(luaI_Address(obj)));
		  		else if (0!=lua_iscfunction(obj)) printf("cfunction: %p\n",lua_getcfunction(obj));
		  		else if (0!=lua_isuserdata(obj))  printf("userdata: %p\n",lua_getuserdata(obj));
		  		else if (0!=lua_istable(obj))     printf("table: %p\n",avalue(luaI_Address(obj)));
		  		else if (0!=lua_isnil(obj))       printf("nil\n");
		  		else                           printf("invalid value to print\n");
		 	}
		}
		
		
		/*
		** Internal function: return an object type.
		*/
		public static void luaI_type ()
		{
			lua_Object o = lua_getparam(1);
		  	if (o == LUA_NOOBJECT)
		    	lua_error("no parameter to function 'type'");
		  	switch (lua_type(o))
		  	{
		  		case (int)lua_Type.LUA_T_NIL :
		      		lua_pushliteral("nil");
		      		break;
		    	case (int)lua_Type.LUA_T_NUMBER :
		      		lua_pushliteral("number");
		      		break;
		    	case (int)lua_Type.LUA_T_STRING :
		      		lua_pushliteral("string");
		      		break;
		    	case (int)lua_Type.LUA_T_ARRAY :
		      		lua_pushliteral("table");
		      		break;
		    	case (int)lua_Type.LUA_T_FUNCTION :
		      		lua_pushliteral("function");
		      		break;
		    	case (int)lua_Type.LUA_T_CFUNCTION :
		      		lua_pushliteral("cfunction");
		      		break;
		    	default :
		      		lua_pushliteral("userdata");
		      		break;
		  	}
		}
		 
		/*
		** Internal function: convert an object to a number
		*/
		public static void lua_obj2number ()
		{
			lua_Object o = lua_getparam(1);
		  	if (0!=lua_isnumber(o))
		    	lua_pushobject(o);
		  	else if (0!=lua_isstring(o))
		  	{
		  		char c = (char)0;
		    	float f = 0;
		    	if (sscanf(lua_getstring(o),"%f %c",f,c) == 1)
		      		lua_pushnumber(f);
		    	else
		      		lua_pushnil();
		  	}
		  	else
		    	lua_pushnil();
		}
		
		
		public static void luaI_error ()
		{
			CharPtr s = lua_getstring(lua_getparam(1));
		  	if (s == null) s = "(no message)";
		  	lua_reportbug(s);
		}
	}
}

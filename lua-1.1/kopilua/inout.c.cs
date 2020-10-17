/*
** inout.c
** Provide function to realise the input/output function and debugger 
** facilities.
*/
using System;

namespace KopiLua
{
	public partial class Lua
	{

		//char *rcs_inout="$Id: inout.c,v 1.2 1993/12/22 21:15:16 roberto Exp $";

		//#include <stdio.h>
		//#include <string.h>

		//#include "opcode.h"
		//#include "hash.h"
		//#include "inout.h"
		//#include "table.h"

		/* Exported variables */
		public static int lua_linenumber;
		public static int lua_debug;
		public static int lua_debugline;
		
		/* Internal variables */
		//#ifndef MAXFUNCSTACK
		//#define MAXFUNCSTACK 32
		private const int MAXFUNCSTACK = 32;
		//#endif		
		private class FuncstackCls { public int file; public int function; }
		private static FuncstackCls[] funcstack = _init_funcstack();
		private static FuncstackCls[] _init_funcstack()
		{
			FuncstackCls[] result = new FuncstackCls[MAXFUNCSTACK];
			for (int i = 0; i < result.Length; ++i)
			{
				result[i] = new FuncstackCls();
			}
			return result;
		}
		private static int nfuncstack=0;
		
		private static FILE fp;
		private static CharPtr st;
		public delegate void usererrorDelegate(CharPtr s);
		private static usererrorDelegate usererror;
	
		/*
		** Function to set user function to handle errors.
		*/
		public static void lua_errorfunction(usererrorDelegate fn)
		{
		 	usererror = fn;
		}
		
		/*
		** Function to get the next character from the input file
		*/
		private static int fileinput ()
		{
		 	int c = fgetc (fp);
			if (false) fprintf(stdout, "=============>fileinput %c\n", c);
		 	return (c == EOF ? 0 : c);
		}

		/*
		** Function to get the next character from the input string
		*/
		private static int stringinput ()
		{
			st.inc();
		 	int ret = (int)st[-1];
//		 	if (ret == 34)
//			{
//				int a = 0;
//			}
//		 	printf("stringinput >>> %d\n", ret);
		 	return ret;
		}

		/*
		** Function to open a file to be input unit. 
		** Return 0 on success or 1 on error.
		*/
		public static int lua_openfile(CharPtr fn)
		{
		   	if (false) fprintf(stdout, "=============>lua_openfile %s\n", fn);
		 	lua_linenumber = 1;
		 	lua_setinput (fileinput);
		 	if (false) fprintf(stdout, "=============>lua_openfile 2 %s\n", fn);
		 	fp = fopen (fn, "r");
		 	if (false) fprintf(stdout, "=============>lua_openfile 3 %s\n", fn);
		 	if (fp == null) return 1;
		 	if (false) fprintf(stdout, "=============>lua_openfile 4 %s\n", fn);
		 	if (lua_addfile (fn) != 0) return 1;
		 	if (false) fprintf(stdout, "=============>lua_openfile 5 %s\n", fn);
		 	return 0;
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
		public static int lua_openstring (CharPtr s)
		{
		 	lua_linenumber = 1;
		 	lua_setinput (stringinput);
		 	st = new CharPtr(s);
		 	{
		  		CharPtr sn = new CharPtr(new char[64]);
				sprintf (sn, "String: %10.10s...", s);
		  		if (lua_addfile(sn) != 0) return 1;
		  	}
		 	return 0;
		}

		/*
		** Function to close an opened string
		*/
		public static void lua_closestring ()
		{
			lua_delfile();
		}

		/*
		** Call user function to handle error messages, if registred. Or report error
		** using standard function (fprintf).
		*/
		public static void lua_error (CharPtr s)
		{
		 	if (usererror != null) usererror(s);
		 	else fprintf (stderr, "lua: %s\n", s);
		}
		
		/*
		** Called to execute  SETFUNCTION opcode, this function pushs a function into
		** function stack. Return 0 on success or 1 on error.
		*/
		public static int lua_pushfunction(int file, int function)
		{
		 	if (nfuncstack >= MAXFUNCSTACK - 1)
		 	{
		  		lua_error ("function stack overflow");
		  		return 1;
		 	}
		 	funcstack[nfuncstack].file = file;
		 	funcstack[nfuncstack].function = function;
		 	nfuncstack++;
		 	return 0;
		}
	
		/*
		** Called to execute  RESET opcode, this function pops a function from 
		** function stack.
		*/
		public static void lua_popfunction ()
		{
		 	nfuncstack--;
		}
	
		/*
		** Report bug building a message and sending it to lua_error function.
		*/
		public static void lua_reportbug (CharPtr s)
		{
		 	CharPtr msg = new CharPtr(new char[1024]);
		 	strcpy (msg, s);
		 	if (lua_debugline != 0)
		 	{
		  		int i;
		  		if (nfuncstack > 0)
		  		{
					sprintf (strchr(msg,'\0'), 
					        "\n\tin statement begining at line %d in function \"%s\" of file \"%s\"",
							lua_debugline, s_name(funcstack[nfuncstack-1].function),
					  	 	lua_file[funcstack[nfuncstack-1].file]);
					sprintf (strchr(msg,'\0'), "\n\tactive stack\n");
		   			for (i=nfuncstack-1; i>=0; i--)
		   				sprintf (strchr(msg,'\0'), "\t-> function \"%s\" of file \"%s\"\n", 
                            s_name(funcstack[i].function),
			    			lua_file[funcstack[i].file]);
		  		}
		  		else
		  		{
				   	sprintf (strchr(msg,'\0'),
				         "\n\tin statement begining at line %d of file \"%s\"", 
				         lua_debugline, lua_filename());
		  		}
		 	}
		 	lua_error(msg);
		}
	}
}

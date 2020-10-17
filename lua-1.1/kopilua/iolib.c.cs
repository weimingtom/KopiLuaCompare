/*
** iolib.c
** Input/output library to LUA
*/

using System;

namespace KopiLua
{
	using lua_Object = KopiLua.Lua.Object_;
	using Word = System.UInt16;
	using real = System.Single;
	
	public partial class Lua
	{
		//char *rcs_iolib="$Id: iolib.c,v 1.4 1994/04/25 20:11:23 celes Exp $";

		//#include <stdlib.h>
		//#include <string.h>
		//#include <stdio.h>
		//#include <ctype.h>
		//#include <sys/stat.h>
		//#if defined(__GNUC__) && !defined(__MINGW32__)
		//#include <floatingpoint.h>
		//#endif

		//#include "mm.h"

		//#include "lua.h"

		private static FILE @in=null, @out=null;

		//#if 0
		/*http://stackoverflow.com/questions/7623735/error-initializer-element-is-not-constant/7623769*/
		//static void init_streams(void) __attribute__((constructor)); 
		//static void init_streams(void)
		//{
		//  in = stdin;
		//  out  = stdout;
		//}
		//#endif

		/*
		** Open a file to read.
		** LUA interface:
		**			status = readfrom (filename)
		** where:
		**			status = 1 -> success
		**			status = 0 -> error
		*/
		private static void io_readfrom ()
		{
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)			/* restore standart input */
		 	{
		  		if (@in != stdin)
		  		{
		   			fclose (@in);
		   			@in = stdin;
		  		}
		  		lua_pushnumber (1);
		 	}
		 	else
		 	{
		  		if (lua_isstring(o) == 0)
		  		{
		   			lua_error ("incorrect argument to function 'readfrom`");
		   			lua_pushnumber (0);
		  		}
		  		else
		  		{
		   			FILE fp = fopen (lua_getstring(o),"r");
		   			if (fp == null)
		   			{
						lua_pushnumber (0);
		   			}
		   			else
		   			{
						if (@in != stdin) fclose(@in);
						@in = fp;
						lua_pushnumber (1);
		   			}
		  		}
		 	}
		}
	
	
		/*
		** Open a file to write.
		** LUA interface:
		**			status = writeto (filename)
		** where:
		**			status = 1 -> success
		**			status = 0 -> error
		*/
		private static void io_writeto ()
		{
		 	lua_Object o = lua_getparam (1);
		 	if (o == null)			/* restore standart output */
		 	{
		  		if (@out != stdout)
		  		{
		   			fclose(@out);
		   			@out = stdout;
		  		}
		  		lua_pushnumber (1);
		 	}
		 	else
		 	{
		  		if (lua_isstring(o) == 0)
		  		{
		   			lua_error ("incorrect argument to function 'writeto`");
		   			lua_pushnumber (0);
		  		}
		  		else
		  		{
		   			FILE fp = fopen (lua_getstring(o),"w");
		   			if (fp == null)
		   			{
						lua_pushnumber (0);
		   			}
		   			else
		   			{
						if (@out != stdout) fclose(@out);
						@out = fp;
						lua_pushnumber (1);
		   			}
		  		}
		 	}
		}		


		/*
		** Open a file to write appended.
		** LUA interface:
		**			status = appendto (filename)
		** where:
		**			status = 2 -> success (already exist)
		**			status = 1 -> success (new file)
		**			status = 0 -> error
		*/
		private static void io_appendto ()
		{
		 lua_Object o = lua_getparam (1);
		 if (o == null)			/* restore standart output */
		 {
		  if (@out != stdout)
		  {
		   fclose (@out);
		   @out = stdout;
		  }
		  lua_pushnumber (1);
		 }
		 else
		 {
		  if (0==lua_isstring (o))
		  {
		   lua_error ("incorrect argument to function 'appendto`");
		   lua_pushnumber (0);
		  }
		  else
		  {
		   int r;
		   FILE fp;
		   stat_struct st = new stat_struct();
		   if (stat(lua_getstring(o), st) == -1) r = 1;
		   else                                  r = 2;
		   fp = fopen (lua_getstring(o),"a");
		   if (fp == null)
		   {
		    lua_pushnumber (0);
		   }
		   else
		   {
		    if (@out != stdout) fclose (@out);
		    @out = fp;
		    lua_pushnumber (r);
		   }
		  }
		 }
		}



		/*
		** Read a variable. On error put nil on stack.
		** LUA interface:
		**			variable = read ([format])
		**
		** O formato pode ter um dos seguintes especificadores:
		**
		**	s ou S -> para string
		**	f ou F, g ou G, e ou E -> para reais
		**	i ou I -> para inteiros
		**
		**	Estes especificadores podem vir seguidos de numero que representa
		**	o numero de campos a serem lidos.
		*/
		private static void io_read ()
		{
		 	lua_Object o = lua_getparam (1);
			if (o == null || 0==lua_isstring(o))	/* free format */
			{
				int c_;
				char[] s = new char[256];
				while (0!=isspace(c_=fgetc(@in)))
				{
					;
				}
				if (c_ == '\"')
				{
					int c, n=0;
					while((c = fgetc(@in)) != '\"')
					{
						if (c == EOF)
						{
						 	lua_pushnil ();
						 	return;
						}
						s[n++] = (char)c;
					}
					s[n] = (char)0;
				}
				else if (c_ == '\'')
				{
					int c, n=0;
					while((c = fgetc(@in)) != '\'')
					{
						if (c == EOF)
						{
						 	lua_pushnil ();
						 	return;
						}
						s[n++] = (char)c;
					}
					s[n] = (char)0;
				}
				else
				{
					CharPtr ptr = null;
					double d;
					ungetc (c_, @in);
					if (fscanf (@in, "%s", s) != 1)
					{
						lua_pushnil ();
						return;
					}
					d = strtod (s, ref ptr);
					if (0==(ptr[0]))
					{
						lua_pushnumber ((real)d);
						return;
					}
				}
				lua_pushstring (s);
				return;
			}
			else				/* formatted */
			{
				CharPtr e = lua_getstring(o);
				char t;
				int  m=0;
				while (0!=isspace(e[0])) e.inc();
				t = e[0]; e.inc();
				while (0!=isdigit(e[0])) {
					m = m*10 + (e[0] - '0'); e.inc();
				}
				
				if (m > 0)
				{
					char[] f = new char[80];
					char[] s = new char[256];
					sprintf (f, "%%%ds", m);
					if (fgets (s, m, @in) == null)
					{
						lua_pushnil();
						return;
					}
					else
					{
						if (s[strlen(s)-1] == '\n')
							s[strlen(s)-1] = (char)0;
					}
					switch (tolower(t))
					{
					case 'i':
						{
						 	long l = 0;
						 	sscanf (s, "%ld", l);
						 	lua_pushnumber(l);
						}
						break;
					
					case 'f': case 'g': case 'e':
						{
							float f_ = 0;
						 	sscanf (s, "%f", f_);
						 	lua_pushnumber(f_);
						}
						break;
						
					default:
						lua_pushstring(s); 
						break;
					}
				}
				else
				{
					switch (tolower(t))
					{
					case 'i':
						{
						 	long l = 0;
						 	if (fscanf (@in, "%ld", l) == EOF)
						   		lua_pushnil();
						   	else lua_pushnumber(l);
						}
						break;
						
					case 'f': case 'g': case 'e':
						{
						 	float f = 0;
						 	if (fscanf (@in, "%f", f) == EOF)
						   		lua_pushnil();
						   	else lua_pushnumber(f);
						}
						break;
						
					default:
						{
							char[] s = new char[256];
						 	if (fscanf (@in, "%s", s) == EOF)
						   		lua_pushnil();
						   	else lua_pushstring(s);
						}
						break;
			   		}
			  	}
		 	}
		}
		
		private static CharPtr buildformat_buffer = new CharPtr(new char[512]);
		private static CharPtr buildformat_f = new CharPtr(new char[80]);
		/*
		** Write a variable. On error put 0 on stack, otherwise put 1.
		** LUA interface:
		**			status = write (variable [,format])
		**
		** O formato pode ter um dos seguintes especificadores:
		**
		**	s ou S -> para string
		**	f ou F, g ou G, e ou E -> para reais
		**	i ou I -> para inteiros
		**
		**	Estes especificadores podem vir seguidos de:
		**
		**		[?][m][.n]
		**
		**	onde:
		**		? -> indica justificacao
		**			< = esquerda
		**			| = centro
		**			> = direita (default)
		**		m -> numero maximo de campos (se exceder estoura)
		**		n -> indica precisao para
		**			reais -> numero de casas decimais
		**			inteiros -> numero minimo de digitos
		**			string -> nao se aplica
		*/
		private static CharPtr buildformat (CharPtr e, lua_Object o)
		{
			//static char buffer[512];
 			//static char f[80];
		 	CharPtr @string = buildformat_buffer;
		 	char t, j = 'r';
		 	int m=0, n=0, l;
		 	while (isspace(e[0])!=0) e.inc();
		 	t = e[0]; e.inc();
		 	if (e[0] == '<' || e[0] == '|' || e[0] == '>')  {j = e[0]; e.inc();}
		 	while (isdigit(e[0])!=0) {m = m*10 + (e[0] - '0'); e.inc(); }
		 	e.inc();	/* skip point */
		 	while (isdigit(e[0])!=0) {n = n*10 + (e[0] - '0'); e.inc(); }
	
		 	sprintf(buildformat_f,"%%");
		 	if (j == '<' || j == '|') sprintf(strchr(buildformat_f,'\0'),"-");
		 	if (m != 0)   sprintf(strchr(buildformat_f,'\0'),"%d", m);
		 	if (n != 0)   sprintf(strchr(buildformat_f,'\0'),".%d", n);
		 	sprintf(strchr(buildformat_f,'\0'), "%c", t);
		 	switch (tolower(t))
		 	{
	  		case 'i':
		  		t = 'i';
	   			sprintf(@string, buildformat_f, (long)lua_getnumber(o));
		  		break;
		  
		  	case 'f': case 'g': case 'e':
				t = 'f';
		   		sprintf(@string, buildformat_f, (float)lua_getnumber(o));
		  		break;
		  	
		  	case 's':
				t = 's';
		   		sprintf(@string, buildformat_f, lua_getstring(o));
		  		break;
		  
		  	default:
			  	return "";
		 	}
		 	l = (int)strlen(@string);
			if (m != 0 && l > m)
			{
			  	int i;
			  	for (i = 0; i < m; i++)
			  		@string[i] = '*';
			  	@string[i] = '\0';
			}
			else if (m!=0 && j=='|')
			{
			  	int i=l-1;
			  	while (isspace(@string[i])!=0) i--;
			  	@string -= (m-i) / 2;
			  	i=0;
			  	while (@string[i] == '\0') @string[i++] = ' ';
			  	@string[l] = '\0';
		 	}
		 	return @string;
		}
		
		private static void io_write ()
		{
		 	lua_Object o1 = lua_getparam (1);
		 	lua_Object o2 = lua_getparam (2);
		 	if (o1 == null)			/* new line */
		 	{
		  		fprintf (@out, "\n");
		  		lua_pushnumber(1);
		 	}
		 	else if (o2 == null)   		/* free format */
		 	{
		  		int status=0;
		  		if (lua_isnumber(o1) != 0)
		  			status = fprintf (@out, "%g", lua_getnumber(o1));
		  		else if (lua_isstring(o1) != 0)
		  			status = fprintf (@out, "%s", lua_getstring(o1));
		  		lua_pushnumber(status);
		 	}
		 	else					/* formated */
		 	{
		  		if (lua_isstring(o2) == 0)
		  		{
		   			lua_error ("incorrect format to function `write'");
		   			lua_pushnumber(0);
		   			return;
		  		}
		  		lua_pushnumber(fprintf (@out, "%s", buildformat(lua_getstring(o2), o1)));
		 	}
		}

		/*
		** Execute a executable program using "system".
		** Return the result of execution.
		*/
		public static void io_execute ()
		{
		 	lua_Object o = lua_getparam (1);
		 	if (o == null || lua_isstring(o) == 0)
		 	{
		  		lua_error ("incorrect argument to function 'execute`");
		  		lua_pushnumber (0);
		 	}
		 	else
		 	{
		  		int res = system(lua_getstring(o));
		  		lua_pushnumber (res);
		 	}
		 	return;
		}
	
		/*
		** Remove a file.
		** On error put 0 on stack, otherwise put 1.
		*/
		public static void io_remove ()
		{
		 	lua_Object o = lua_getparam (1);
		 	if (o == null || lua_isstring(o) == 0)
		 	{
		  		lua_error ("incorrect argument to function 'execute`");
		  		lua_pushnumber (0);
		 	}
		 	else
		 	{
		  		if (remove(lua_getstring(o)) == 0)
		  			lua_pushnumber (1);
		  		else
		  			lua_pushnumber (0);
		  	}
		 	return;
		}
		
		/*
		** Open io library
		*/
		public static void iolib_open ()
		{
		  	@in = stdin; 
			@out = stdout;
			lua_register ("readfrom", io_readfrom);
			lua_register ("writeto",  io_writeto);
		 	lua_register ("appendto", io_appendto);
			lua_register ("read",     io_read);
			lua_register ("write",    io_write);
			lua_register ("execute",  io_execute);
			lua_register ("remove",   io_remove);
		}
	}
}

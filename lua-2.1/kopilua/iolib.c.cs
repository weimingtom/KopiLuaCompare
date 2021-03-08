/*
** iolib.c
** Input/output library to LUA
*/

namespace KopiLua
{
	using lua_Object = System.UInt32;
	using Word = System.UInt16;
	using real = System.Single;	
	
	public partial class Lua
	{
		//char *rcs_iolib="$Id: iolib.c,v 1.21 1995/02/06 19:36:13 roberto Exp $";
		
		//#include <stdio.h>
		//#include <ctype.h>
		//#include <sys/types.h>
		//#include <sys/stat.h>
		//#include <string.h>
		//#include <time.h>
		//#include <stdlib.h>
		
		//#include "lua.h"
		//#include "lualib.h"
		
		private static FILE @in=stdin, @out=stdout;
		
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
		 	if (o == LUA_NOOBJECT)			/* restore standart input */
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
		  		if (0==lua_isstring (o))
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
				    	if (@in != stdin) fclose (@in);
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
		 	if (o == LUA_NOOBJECT)			/* restore standart output */
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
				    	if (@out != stdout) fclose (@out);
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
		 	if (o == LUA_NOOBJECT)			/* restore standart output */
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
				   	else                                   r = 2;
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
		 	if (o == LUA_NOOBJECT || 0==lua_isstring(o))	/* free format */
		 	{
		  		int c;
		  		CharPtr s = new CharPtr(new char[256]);
		  		while (0!=isspace(c=fgetc(@in)))
		   			;
		  		if (c == '\"')
		  		{
					int n=0;
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
				else if (c == '\'')
				{
		   			int n=0;
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
					double d = 0;
				   	ungetc (c, @in);
				   	if (fscanf (@in, "%s", s) != 1)
				   	{
				    	lua_pushnil ();
				    	return;
				   	}
				   	if (sscanf(s, "%lf %*c", d) == 1)
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
		  			m = m*10 + (e[0] - '0');
		  			e.inc();
		  		}
		  		if (m > 0)
		  		{
		  			CharPtr f = new CharPtr(new char[80]);
		  			CharPtr s = new CharPtr(new char[256]);
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
					     		float fl = 0;
					     		sscanf (s, "%f", fl);
					     		lua_pushnumber(fl);
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
						    CharPtr s = new CharPtr(new char[256]);
						    if (fscanf (@in, "%s", s) == EOF)
						       lua_pushnil();
						    else lua_pushstring(s);
						}
						break;
					}
		  		}
		 	}
		}
		
		
		/*
		** Read characters until a given one. The delimiter is not read.
		*/
		private static void io_readuntil ()
		{
			int n=255,m=0;
		 	int c,d;
		 	CharPtr s;
		 	lua_Object lo = lua_getparam(1);
		 	if (0==lua_isstring(lo))
		  		d = EOF; 
		 	else
		 		d = (int)lua_getstring(lo)[0];
		 
		 	s = new CharPtr(new char[n+1]);
		 	while((c = fgetc(@in)) != EOF && c != d)
			{
		  		if (m==n)
		 	 	{
		   			n *= 2;
		   			CharPtr temp = new CharPtr(new char[n+1]); //(char *)realloc(s, n+1);
		   			for (int i = 0; i < s.chars.Length; ++i)
		   			{
		   				if (i < temp.chars.Length)
		   				{
		   					temp.chars[i] = s.chars[i];
		   				}
		   			}
		   			s = temp;
		  		}
		  		s[m++] = (char)c;
		 	}
		 	if (c != EOF) ungetc(c,@in);
		 	s[m] = (char)0;
		 	lua_pushstring(s);
		 	free(s);
		}
		
		
		
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
		private static CharPtr buffer = new CharPtr(new char[2048]);
		private static CharPtr f = new CharPtr(new char[80]);
		private static CharPtr buildformat (CharPtr e, lua_Object o)
		{
			CharPtr @string = new CharPtr(buffer, 255);
		 	CharPtr fstart = e, fspace, send;
		 	char t, j='r';
		 	int  m=0, n=-1, l;
		 	while (0!=isspace(e[0])) e.inc();
		 	fspace = e;
		 	t = e[0]; e.inc();
		 	if (e[0] == '<' || e[0] == '|' || e[0] == '>') {j = e[0]; e.inc(); }
		 	while (0!=isdigit(e[0])) {
		 		m = m*10 + (e[0] - '0'); e.inc();
		 	}
		 	if (e[0] == '.') e.inc();	/* skip point */
		 	while (0!=isdigit(e[0]))
		 		if (n < 0) { n = (e[0] - '0'); e.inc();}
		 	else       { n = n*10 + (e[0] - '0'); e.inc(); }
		
		 	sprintf(f,"%%");
		 	if (j == '<' || j == '|') sprintf(strchr(f,(char)0),"-");
		 	if (m >  0)   sprintf(strchr(f,(char)0),"%d", m);
		 	if (n >= 0)   sprintf(strchr(f,(char)0),".%d", n);
		 	switch (t)
		 	{
		  		case 'i': case 'I': t = 'd';
		   			sprintf(strchr(f,(char)0), "%c", t);
		   			sprintf (@string, f, (long)lua_getnumber(o));
		  			break;
		  		case 'f': case 'g': case 'e': case 'G': case 'E': 
		   			sprintf(strchr(f,(char)0), "%c", t);
		   			sprintf (@string, f, (float)lua_getnumber(o));
		  			break;
		  		case 'F': t = 'f';
		  			sprintf(strchr(f,(char)0), "%c", t);
		   			sprintf (@string, f, (float)lua_getnumber(o));
		  			break;
		  		case 's': case 'S': t = 's';
		   			sprintf(strchr(f,(char)0), "%c", t);
		   			sprintf (@string, f, lua_getstring(o));
		  			break;
		  		default: return "";
		 	}
		 	l = (int)strlen(@string);
		 	send = @string+l;
		 	if (m!=0 && l>m)
		 	{
		  		int i;
		 	 	for (i=0; i<m; i++)
		   			@string[i] = '*';
		 	 	@string[i] = (char)0;
		 	}
		 	else if (m!=0 && j=='|')
		 	{
		  		int k;
		  		int i=l-1;
		  		while (0!=isspace(@string[i]) || @string[i]==0) i--;
		  		@string -= (m-i)/2;
		  		for(k=0; k<(m-i)/2; k++)
		   			@string[k] = ' ';
		 	}
		 	/* add space characteres */
		 	while (fspace != fstart)
		 	{
		 		@string.dec();
		  		fspace.dec();
		  		@string[0] = fspace[0];
		 	}
		 	while (0!=isspace(e[0])) { send[0] = e[0]; e.inc(); send.inc(); }
		 	send[0] = (char)0;
		 	return @string;
		}
		static void io_write ()
		{
		 	lua_Object o1 = lua_getparam (1);
		 	lua_Object o2 = lua_getparam (2);
		 	if (o1 == LUA_NOOBJECT)			/* new line */
		 	{
		  		fprintf (@out, "\n");
		  		lua_pushnumber(1);
		 	}
		 	else if (o2 == LUA_NOOBJECT)   		/* free format */
		 	{
		  		int status=0;
		  		if (0!=lua_isnumber(o1))
		   			status = fprintf (@out, "%g", lua_getnumber(o1));
		  		else if (0!=lua_isstring(o1))
		   			status = fprintf (@out, "%s", lua_getstring(o1));
		  		lua_pushnumber(status);
		 	}
		 	else /* formated */
		 	{
		  		if (0==lua_isstring(o2))
		  		{ 
		   			lua_error ("incorrect format to function `write'"); 
		   			lua_pushnumber(0);
		   			return;
		  		}
		  		lua_pushnumber(fprintf (@out, "%s", buildformat(lua_getstring(o2),o1)));
		 	}
		}
		
		/*
		** Execute a executable program using "system".
		** Return the result of execution.
		*/
		private static void io_execute ()
		{
		 	lua_Object o = lua_getparam (1);
		 	if (o == LUA_NOOBJECT || 0==lua_isstring (o))
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
		private static void io_remove  ()
		{
		 	lua_Object o = lua_getparam (1);
		 	if (o == LUA_NOOBJECT || 0==lua_isstring (o))
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
		** To get a environment variables
		*/
		private static void io_getenv ()
		{
		 	lua_Object s = lua_getparam(1);
			if (0==lua_isstring(s))
		 		lua_pushnil();
		 	else
		 	{
		  		CharPtr env = getenv(lua_getstring(s));
		  		if (env == null) lua_pushnil();
		  		else             lua_pushstring(env); 
		 	}
		}
		
		/*
		** Return time: hour, min, sec
		*/
		private static void io_time ()
		{
			time_t t = new time_t();
			tm s = new tm();
		 
		 	time(t);
		 	s = localtime(t);
		 	lua_pushnumber(s.tm_hour);
		 	lua_pushnumber(s.tm_min);
			lua_pushnumber(s.tm_sec);
		}
		 
		/*
		** Return date: dd, mm, yyyy
		*/
		private static void io_date ()
		{
			time_t t = new time_t();
			tm s = new tm();
		 
		 	time(t);
		 	s = localtime(t);
		 	lua_pushnumber(s.tm_mday);
		 	lua_pushnumber(s.tm_mon+1);
		 	lua_pushnumber(s.tm_year+1900);
		}
		 
		/*
		** Beep
		*/
		private static void io_beep ()
		{
			printf("\a");
		}
		 
		/*
		** To exit
		*/
		private static void io_exit ()
		{
		 	lua_Object o = lua_getparam(1);
		 	if (0!=lua_isstring(o))
		  		printf("%s\n", lua_getstring(o));
		 	exit(1);
		}
		
		/*
		** To debug a lua program. Start a dialog with the user, interpreting
		   lua commands until an 'cont'.
		*/
		private static void io_debug ()
		{
			while (true)
		  	{
				CharPtr buffer = new CharPtr(new char[250]);
		    	fprintf(stderr, "lua_debug> ");
		    	if (gets(buffer) == null) return;
		    	if (strcmp(buffer, "cont") == 0) return;
		    	lua_dostring(buffer);
		  	}
		}
		
		/*
		** Open io library
		*/
		public static void iolib_open ()
		{
			lua_register ("readfrom", io_readfrom, "io_readfrom");
		 	lua_register ("writeto",  io_writeto, "io_writeto");
		 	lua_register ("appendto", io_appendto, "io_appendto");
		 	lua_register ("read",     io_read, "io_read");
		 	lua_register ("readuntil",io_readuntil, "io_readuntil");
		 	lua_register ("write",    io_write, "io_write");
		 	lua_register ("execute",  io_execute, "io_execute");
		 	lua_register ("remove",   io_remove, "io_remove");
		 	lua_register ("getenv",   io_getenv, "io_getenv");
		 	lua_register ("time",     io_time, "io_time");
			lua_register ("date",     io_date, "io_date");
		 	lua_register ("beep",     io_beep, "io_beep");
		 	lua_register ("exit",     io_exit, "io_exit");
		 	lua_register ("debug",    io_debug, "io_debug");
		}
	}
}

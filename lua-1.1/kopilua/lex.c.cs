using System;

namespace KopiLua
{
	using Word = System.UInt16; //unsigned short
	using YYTYPE = System.SByte;
	
	public partial class Lua
	{	
		public static string rcs_lex = "$Id: lex.c,v 2.1 1994/04/15 19:00:28 celes Exp $";
		/*$Log: lex.c,v $
		 * Revision 2.1  1994/04/15  19:00:28  celes
		 * Retirar chamada da funcao lua_findsymbol associada a cada
		 * token NAME. A decisao de chamar lua_findsymbol ou lua_findconstant
		 * fica a cargo do modulo "lua.stx".
		 *
		 * Revision 1.3  1993/12/28  16:42:29  roberto
		 * "include"s de string.h e stdlib.h para evitar warnings
		 *
		 * Revision 1.2  1993/12/22  21:39:15  celes
		 * Tratamento do token $debug e $nodebug
		 *
		 * Revision 1.1  1993/12/22  21:15:16  roberto
		 * Initial revision
		 **/

		//#include <ctype.h>
		//#include <math.h>
		//#include <stdlib.h>
		//#include <string.h>

		//#include "opcode.h"
		//#include "hash.h"
		//#include "inout.h"
		//#include "table.h"
		//#include "y.tab.h"

		private static void next() { current = input(); }
		private static void save(int x) { yytextLast[0] = (char)(x); yytextLast.inc(); }
		private static void save_and_next() { { yytextLast[0] = (char)(current); yytextLast.inc(); }; { current = input(); }; }

		private static int current;
		private static CharPtr yytext = new CharPtr(new char[256]);
		private static CharPtr yytextLast;

		private static Input input = null;

		public static void lua_setinput(Input fn)
		{
		  current = ' ';
		  input = fn;
		}

		public static CharPtr lua_lasttext()
		{
			yytextLast[0] = (char)0;
			return new CharPtr(yytext);
		}
		public class reserved_struct
		{
		    public CharPtr name;
		    public int token;
		    
		    public reserved_struct(CharPtr name, int token)
		    {
		    	this.name = new CharPtr(name);
		    	this.token = token;
		    }
		}
		public static reserved_struct[] reserved = {
			new reserved_struct("and", AND),
			new reserved_struct("do", DO), 
			new reserved_struct("else", ELSE), 
			new reserved_struct("elseif", ELSEIF), 
			new reserved_struct("end", END), 
			new reserved_struct("function", FUNCTION), 
			new reserved_struct("if", IF), 
			new reserved_struct("local", LOCAL), 
			new reserved_struct("nil", NIL), 
			new reserved_struct("not", NOT), 
			new reserved_struct("or", OR), 
			new reserved_struct("repeat", REPEAT), 
			new reserved_struct("return", RETURN), 
			new reserved_struct("then", THEN), 
			new reserved_struct("until", UNTIL), 
			new reserved_struct("while", WHILE) };

		//#define RESERVEDSIZE (sizeof(reserved)/sizeof(reserved[0]))
		private static int RESERVEDSIZE = reserved.Length;


		public static int findReserved (CharPtr name)
		{
			int l = 0;
		  	int h = RESERVEDSIZE - 1;
		  	while (l <= h)
		  	{
				int m = (l+h)/2;
				int comp = strcmp(name, reserved[m].name);
				if (comp < 0)
			  		h = m-1;
				else if (comp == 0)
			  		return reserved[m].token;
				else
			  		l = m+1;
		  	}
		  	return 0;
		}


		public static int yylex ()
		{
		  	while (true)
		  	{
		  		yytextLast = new CharPtr(yytext);
				switch (current)
				{
			  	case '\n':
				  	lua_linenumber++;
				  	goto case ' ';
				  
			  	case ' ':
			  	case '\t':
				  	next();
					continue;

			  	case '$':
					next();
					while (0!=isalnum(current) || current == '_')
				  		save_and_next();
					yytextLast[0] = (char)0;
					if (strcmp(yytext, "debug") == 0)
					{
			  			yylval.vInt = 1;
			  			return DEBUG;
					}
					else if (strcmp(yytext, "nodebug") == 0)
					{
			  			yylval.vInt = 0;
			  			return DEBUG;
					}
					return WRONGTOKEN;

			  	case '-':
					save_and_next();
					if (current != '-')
						return '-';
					do
					{
						next();
					} while (current != '\n' && current != 0);
					continue;

			  	case '<':
					save_and_next();
					if (current != '=')
						return '<';
					else
					{
						save_and_next();
						return LE;
					}

			  	case '>':
					save_and_next();
					if (current != '=')
						return '>';
					else
					{
						save_and_next();
						return GE;
					}

			  	case '~':
					save_and_next();
					if (current != '=')
						return '~';
					else
					{
						save_and_next();
						return NE;
					}

			  	case '"':
			  	case '\'':
					{
						int del = current;
						next();  /* skip the delimiter */
						while (current != del)
						{
						  	switch (current)
						  	{
							case 0:
							case '\n':
							  	return WRONGTOKEN;
							  	
							case '\\':
								next();  /* do not save the '\' */
							  	switch (current)
							  	{
								case 'n':
									save('\n'); 
									next();
									break;
								
								case 't':
									save('\t'); 
									next();
									break;
								
								case 'r':
									save('\r'); 
									next();
									break;
								
								default:
									save('\\');
									break;
							  	}
							  	break;
							  	
							default:
							  	save_and_next();
						  		break;
						  	}
						}
						next();  /* skip the delimiter */
						yytextLast[0] = (char)0;
						yylval.vWord = (Word)lua_findconstant (yytext);
						return STRING;
					}

			  	case 'a': case 'b': case 'c': case 'd': case 'e':
			  	case 'f': case 'g': case 'h': case 'i': case 'j':
			  	case 'k': case 'l': case 'm': case 'n': case 'o':
				case 'p': case 'q': case 'r': case 's': case 't':
			  	case 'u': case 'v': case 'w': case 'x': case 'y':
			  	case 'z':
			  	case 'A': case 'B': case 'C': case 'D': case 'E':
			  	case 'F': case 'G': case 'H': case 'I': case 'J':
			  	case 'K': case 'L': case 'M': case 'N': case 'O':
			  	case 'P': case 'Q': case 'R': case 'S': case 'T':
				case 'U': case 'V': case 'W': case 'X': case 'Y':
			  	case 'Z':
			  	case '_':
			  		{
						int res;
						do
						{
							save_and_next();
						} while (0 != isalnum(current) || current == '_');
						yytextLast[0] = (char)0;
						res = findReserved(yytext);
						if (0!=res)
							return res;
						yylval.pChar = new CharPtr(yytext);
						return NAME;
					 }

			  	case '.':
					save_and_next();
					if (current == '.')
					{
						save_and_next();
					  	return CONC;
					}
					else if (0==isdigit(current))
						return '.';
					// current is a digit: goes through to number 
					goto fraction;

			  	case '0': case '1': case '2': case '3': case '4':
			  	case '5': case '6': case '7': case '8': case '9':

			  		do
					{
						save_and_next();
					} while (0!=isdigit(current));
					if (current == '.')
						save_and_next();
fraction:
					while (0 != isdigit(current))
						save_and_next();
					if (current == 'e' || current == 'E')
					{
					  	save_and_next();
					  	if (current == '+' || current == '-')
					  		save_and_next();
					  	if (0==isdigit(current))
						  	return WRONGTOKEN;
					  	do
					  	{
						  	save_and_next();
					  	} while (0!=isdigit(current));
				  	}
					yytextLast[0] = (char)0;
					yylval.vFloat = (float)atof(yytext);
					return NUMBER;

			  	default: // also end of file 
				  	{
						save_and_next();
						return yytext[0];
				  	}
				}
		  	}
		}
	}
}


//#define MY_DEBUG

namespace KopiLua
{
	using lua_Object = System.UInt32;
	using Word = System.UInt16;
	using real = System.Single;	
	using Bool = System.Int32;
	using Long = System.Int32;	
	
	public partial class Lua
	{
		//char *rcs_lex = "$Id: lex.c,v 2.14 1994/12/27 20:50:38 celes Exp $";
		 
		
		//#include <ctype.h>
		//#include <math.h>
		//#include <stdio.h>
		//#include <stdlib.h>
		//#include <string.h>
		
		//#include "tree.h"
		//#include "table.h"
		//#include "opcode.h"
		//#include "inout.h"
		//#include "parser.h"
		//#include "ugly.h"
		
		private static int lua_strcmp(CharPtr a, CharPtr b)	{ return (a[0]<b[0]?(-1):(a[0]>b[0]?(1):strcmp(a,b))); }
		
		private static void next() { current = input(); }
		private static void save(char x) { yytextLast[0] = (x); yytextLast.inc(); }
		private static void save_and_next()  { save((char)current); next(); }
		
		private static int current;
		private static CharPtr yytext = new CharPtr(new char[256]);
		private static CharPtr yytextLast;
		
		private static Input input;
		
		public static void lua_setinput (Input fn)
		{
			current = ' ';
		  	input = fn;
		}
		
		public static CharPtr lua_lasttext ()
		{
			yytextLast[0] = (char)0;
		  	return yytext;
		}
		
		
		/* The reserved words must be listed in lexicographic order */
		class reserved_struct
		{
		    public CharPtr name;
		    public int token;
		    
		    public reserved_struct(CharPtr name, int token)
		    {
		    	this.name = name;
		    	this.token = token;
		    }
		}
		private static reserved_struct[] reserved = {
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
			new reserved_struct("while", WHILE)
		};
		
		private static int RESERVEDSIZE = reserved.Length;
		
		
		private static int findReserved (CharPtr name)
		{
			int l = 0;
		  	int h = RESERVEDSIZE - 1;
		  	while (l <= h)
		  	{
		    	int m = (l+h)/2;
		    	int comp = lua_strcmp(name, reserved[m].name);
		    	if (comp < 0)
		      		h = m-1;
		    	else if (comp == 0)
		      		return reserved[m].token;
		    	else
		      		l = m+1;
		  	}
		  	return 0;
		}
		
		/*
		public static int yylex() {
			int result = yylex_();
			if (result == 0)
			{
				printf("hello\n");
			}
			return result;
		}*/
		public static int yylex ()
		{
		  	float a;
		  	while (true)
		  	{
		    	yytextLast = new CharPtr(yytext);
#if MY_DEBUG
		    	//fprintf(stderr,"'%c' %d\n",current,current);
		    	if (current == 40)
		    	{
		    		//printf("hello\n");
		    	}
#endif
		    	switch (current)
				{
	      		case EOF:
	      		case 0:
	       			return 0;
	       			
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
					{
			        	save_and_next();
					}
					yytextLast[0] = (char)0;
					if (lua_strcmp(yytext, "debug") == 0)
					{
				  		yylval.vInt = 1;
				  		return DEBUG;
			        }
					else if (lua_strcmp(yytext, "nodebug") == 0)
					{
				  		yylval.vInt = 0;
				  		return DEBUG;
			        }
					return WRONGTOKEN;
			
			    case '-':
			        save_and_next();
			        if (current != '-') 
			        {
			        	return '-';
			        }
			        do 
			        { 
			        	next(); 
			        } while (current != '\n' && current != 0);
			        continue;
			
			    case '=':
			        save_and_next();
			        if (current != '=') 
			        {
			        	return '=';
			        }
			        else 
			        { 
			        	save_and_next(); 
			        	return EQ; 
			        }
			
			    case '<':
			        save_and_next();
			        if (current != '=') 
			        {
			        	return '<';
			        }
			        else 
			        { 
			        	save_and_next(); 
			        	return LE; 
			        }
			
			    case '>':
			        save_and_next();
			        if (current != '=') 
			        {
			        	return '>';
			        }
			        else 
			        { 
			        	save_and_next(); 
			        	return GE; 
			        }
			
			    case '~':
			        save_and_next();
			    	if (current != '=') 
			    	{
			    		return '~';
			    	}
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
				            case EOF:
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
				                
				              	default : 
				              		save((char)current); 
				              		next(); 
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
				        yylval.vWord = luaI_findconstant(lua_constcreate(yytext));
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
				        Word res;
				        do 
				        { 
				        	save_and_next(); 
				        } while (0!=isalnum(current) || current == '_');
				        yytextLast[0] = (char)0;
				        res = (Word)findReserved(yytext);
				        if (res!=0) 
				        {
				        	return res;
				        }
				        yylval.pNode = lua_constcreate(yytext);
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
			        {
			        	return '.';
			        }
			        /* current is a digit: goes through to number */
			        a = (float)0.0;
			        goto fraction;
			
			    case '0': case '1': case '2': case '3': case '4':
			    case '5': case '6': case '7': case '8': case '9':
			        a = (float)0.0;
			        do 
			        { 
			        	a = 10 * a + current - '0'; 
			        	save_and_next(); 
			        } while (0!=isdigit(current));
			        if (current == '.') 
			        {
			        	save_and_next();
			        }
fraction:
					{ 
			        	float da = (float)0.1;
						while (0!=isdigit(current))
						{
							a += (current-'0')*da; 
							da /= (float)10.0;
							save_and_next();
						};
					    if (current == 'e' || current == 'E')
					    {
							int e = 0;
						    int neg;
						    float ea;
					        save_and_next();
					        neg = (current == '-') ? 1 : 0;
					        if (current == '+' || current == '-') 
					        {
					        	save_and_next();
					        }
					        if (0==isdigit(current)) 
					        {
					        	return WRONGTOKEN;
					        }
					        do 
					        { 
					        	e = 10 * e + current - '0'; 
					        	save_and_next(); 
					        } while (0!=isdigit(current));
					        for (ea = neg != 0 ? (float)0.1 : (float)10.0; e > 0; e >>= 1)
						    {
						    	if ((e & 1) != 0) 
						    	{
						    		a *= ea;
						    	}
						      	ea*=ea;
						    }
					    }
					    yylval.vFloat = a;
					    return NUMBER;
			        }
			
			    case U_and: case U_do: case U_else: case U_elseif: case U_end:
			    case U_function: case U_if: case U_local: case U_nil: case U_not:
			    case U_or: case U_repeat: case U_return: case U_then:
			    case U_until: case U_while:
			    	{
			        	int old = current;
			        	next();
			        	return reserved[old - U_and].token;
			      	}
			
			    case U_eq:	
			        next(); 
			        return EQ;
			        
			    case U_le:	
			        next(); 
			        return LE;
			    
			    case U_ge:	
			        next(); 
			        return GE;
			    
			    case U_ne:	
			        next(); 
			        return NE;
			    
			    case U_sc:	
			        next(); 
			        return CONC;
			
			    default: 		/* also end of file */
			    	{
			        	save_and_next();
			        	return yytext[0];
			      	}
				}
		  	}
		}
	}
}


//#define YYDEBUG

using System;

//FIXME:TODO:ptr class var value assign
namespace KopiLua
{
	using Word = System.UInt16; //unsigned short
	
	public partial class Lua
	{
		//#line 2 "lua.stx"
		
//		#include <stdio.h>
//		#include <stdlib.h>
//		#include <string.h>
//		
//		#include "opcode.h"
//		#include "hash.h"
//		#include "inout.h"
//		#include "table.h"
//		#include "lua.h"
		
//		#ifndef ALIGNMENT
//		#define ALIGNMENT	(sizeof(void *))
//		#endif
		public const int ALIGNMENT = 4;
		
//		#ifndef MAXCODE
//		#define MAXCODE 1024
//		#endif	
		public const int MAXCODE = 1024;
		
#if false
		//private static long[] buffer = new long[MAXCODE];
		private static byte[] buffer_ = new byte[MAXCODE * 4];
		private static BytePtr code = new BytePtr(buffer_);
		//private static long[] mainbuffer = new long[MAXCODE];
		private static byte[] mainbuffer_ = new byte[MAXCODE * 4];
		private static BytePtr maincode = new BytePtr(mainbuffer_);
#else
		private const int BUFFER_SPACE_INNER = 256 + 4;
		private static byte[] buffer_mainbuffer_ = new byte[MAXCODE * 4 + BUFFER_SPACE_INNER + MAXCODE * 4];
		private static BytePtr code = new BytePtr(buffer_mainbuffer_, 0);
		private static BytePtr maincode = new BytePtr(buffer_mainbuffer_, 0 + MAXCODE * 4 + BUFFER_SPACE_INNER);
#endif
		private static BytePtr basepc = null;
		private static BytePtr pc = null;
	
		//#define MAXVAR 32
		public const int MAXVAR = 32;
		internal static long[] varbuffer = new long[MAXVAR];
		internal static byte nvarbuffer=0; // number of variables at a list
	
		internal static Word[] localvar = new Word[STACKGAP];
		internal static byte nlocalvar=0; // number of local variables
		internal static int ntemp=0; // number of temporary var into stack
		internal static int err=0; // flag to indicate error
	
		/* Internal functions */
		//#define align(n)  align_n(sizeof(n))
		private static void align (uint n) { align_n(n); }
		
		private static void code_byte(Byte c)
		{
		 	if (pc-basepc>MAXCODE-1)
		 	{
		  		lua_error ("code buffer overflow");
		  		err = 1;
			}
		 	pc[0] = c; pc.inc();
		}
	
		private static void code_word (Word n)
		{
		 	if (pc-basepc>MAXCODE-2)
		 	{
		  		lua_error("code buffer overflow");
		  		err = 1;
		 	}
		 	pc[0] = (byte)(n & 0xff); pc[1] = (byte)((n >> 8) & 0xff);
		 	pc += 2;
		}
	
		private static void code_float (float n)
		{
		 	if (pc-basepc>MAXCODE-4)
		 	{
		  		lua_error("code buffer overflow");
		  		err = 1;
		 	}
		 	byte[] bytes = BitConverter.GetBytes((Single)n); pc[0] = bytes[0]; pc[1] = bytes[1]; pc[2] = bytes[2]; pc[3] = bytes[3];
		 	pc += 4;
		}
	
		private static void incr_ntemp ()
		{
		 	if (ntemp+nlocalvar+MAXVAR+1 < STACKGAP)
		  		ntemp++;
		 	else
		 	{
		  		lua_error ("stack overflow");
		  		err = 1;
		 	}
		}
	
		private static void incr_nlocalvar ()
		{
		 	if (ntemp+nlocalvar+MAXVAR+1 < STACKGAP)
		 		nlocalvar++;
		 	else
		 	{
		  		lua_error ("too many local variables or expression too complicate");
		  		err = 1;
		 	}
		}
	
		private static void incr_nvarbuffer ()
		{
		 	if (nvarbuffer < MAXVAR-1)
		  		nvarbuffer++;
		 	else
		 	{
		  		lua_error ("variable buffer overflow");
		  		err = 1;
		 	}
		}
	
		private static void align_n (uint size)
		{
		 	if (size > ALIGNMENT) size = ALIGNMENT;
		 	while (((pc+1-code)%size) != 0) // +1 to include BYTECODE
		 		code_byte ((byte)OpCode.NOP);
		}
	
		private static void code_number (float f)
		{
			int i = (int)f; //BitConverter.ToInt32(BitConverter.GetBytes(f), 0);
		  	if (f == i)  /* f has an integer value */
		  	{
		  		if (i <= 2) code_byte((byte)((int)OpCode.PUSH0 + i));
		   		else if (i <= 255)
		   		{
		   			code_byte((byte)OpCode.PUSHBYTE);
					code_byte((byte)i);
		   		}
		   		else
		   		{
					align_n(2);
					code_byte((byte)OpCode.PUSHWORD);
					code_word((byte)i);
		   		}
		  	}
		  	else
		  	{
		   		align_n(4); //FIXME:float
		   		code_byte((byte)OpCode.PUSHFLOAT);
		   		code_float(f);
		  	}
		  	incr_ntemp();
		}
		

		
//		#line 140 "lua.stx"
//		public struct YYSTYPE //union
//		{
//		 	public int vInt;
//		 	public int vLong;
//		 	public float vFloat;
//		 	public Word vWord;
//			public BytePtr pByte;
//		}
//		# define NIL 257
//		# define IF 258
//		# define THEN 259
//		# define ELSE 260
//		# define ELSEIF 261
//		# define WHILE 262
//		# define DO 263
//		# define REPEAT 264
//		# define UNTIL 265
//		# define END 266
//		# define RETURN 267
//		# define LOCAL 268
//		# define NUMBER 269
//		# define FUNCTION 270
//		# define NAME 271
//		# define STRING 272
//		# define DEBUG 273
//		# define NOT 274
//		# define AND 275
//		# define OR 276
//		# define NE 277
//		# define LE 278
//		# define GE 279
//		# define CONC 280
//		# define UNARY 281
		//#define yyclearin yychar = -1
		//#define yyerrok yyerrflag = 0
		//extern int yychar;
		//extern int yyerrflag;
//		#ifndef YYMAXDEPTH
//		#define YYMAXDEPTH 150
//		#endif
		private const int YYMAXDEPTH = 150; 
		public static YYSTYPE yylval = new YYSTYPE(), yyval = new YYSTYPE();
//		# define YYERRCODE 256
		private const int YYERRCODE = 256;
		
		//#line 530 "lua.stx"
	
	
		/*
		** Search a local name and if find return its index. If do not find return -1
		*/
		private static int lua_localname (Word n)
		{
		 	int i;
		 	for (i=nlocalvar-1; i >= 0; i--)
		  		if (n == localvar[i]) return i;	/* local var */
		 	return -1;		        /* global var */
		}
	
		/*
		** Push a variable given a number. If number is positive, push global variable
		** indexed by (number -1). If negative, push local indexed by ABS(number)-1.
		** Otherwise, if zero, push indexed variable (record).
		*/
		private static void lua_pushvar (int number) //FIXME:???long???
		{
		 	if (number > 0)	/* global var */
		 	{
		  		align_n(2);
		  		code_byte((byte)OpCode.PUSHGLOBAL);
		  		code_word((Word)(number-1));
		  		incr_ntemp();
		 	}
		 	else if (number < 0)	/* local var */
		 	{
		  		number = (-number) - 1;
		  		if (number < 10) code_byte((byte)(OpCode.PUSHLOCAL0 + number));
		  		else
		  		{
		  			code_byte((byte)OpCode.PUSHLOCAL);
		  			code_byte((byte)(Word)number);
		  		}
		  		incr_ntemp();
		 	}
		 	else
		 	{
		 		code_byte((byte)OpCode.PUSHINDEXED);
		  		ntemp--;
		 	}
		}
	
		internal static void lua_codeadjust(int n)
		{
			code_byte((byte)OpCode.ADJUST);
			code_byte((byte)(n + nlocalvar));
		}
	
		internal static void lua_codestore(int i)
		{
			if (varbuffer[i] > 0)		/* global var */
			{
				align_n(sizeof(Word));
				code_byte((byte)OpCode.STOREGLOBAL);
				code_word((Word)(varbuffer[i]-1));
			}
			else if (varbuffer[i] < 0)      /* local var */
			{
				int number = (int)((-varbuffer[i]) - 1);
				if (number < 10)
				{
					code_byte((byte)(OpCode.STORELOCAL0 + number));
				}
				else
				{
					code_byte((byte)OpCode.STORELOCAL);
					code_byte((byte)number);
				}
			}
			else				  /* indexed var */
			{
				int j;
				int upper = 0;      	/* number of indexed variables upper */
				int param;		/* number of itens until indexed expression */
				for (j = i + 1; j < nvarbuffer; j++)
					if (varbuffer[j] == 0) upper++;
				param = upper * 2 + i;
				if (param == 0)
					code_byte((byte)OpCode.STOREINDEXED0);
				else
				{
					code_byte((byte)OpCode.STOREINDEXED);
					code_byte((byte)param);
				}
			}
		}
	
		private static CharPtr yyerror_msg = new CharPtr(new char[256]);
		public static void yyerror(CharPtr s)
		{
			//static char msg[256];
			string lasttext = lua_lasttext ().ToString();
			lasttext = lasttext.Replace("\r", "\\r");
			sprintf (yyerror_msg,"%s near \"%s\" at line %d in file \"%s\"",
			         s.ToString(), lasttext, lua_linenumber, lua_filename());
//			Console.WriteLine("===" + yyerror_msg.ToString());
			lua_error (yyerror_msg);
			err = 1;
		}
	
		public static int yywrap()
		{
		 	return 1;
		}
	
	
		/*
		** Parse LUA code and execute global statement.
		** Return 0 on success or 1 on error.
		*/
		public static int lua_parse()
		{
			BytePtr initcode = new BytePtr(maincode);
		 	err = 0;
		 	if (yyparse() != 0 || (err == 1)) return 1;
		 	maincode[0] = (byte)OpCode.HALT; maincode.inc();
		 	if (false) 
		 	{
		 		PrintCode();
		 	}
		 	if (lua_execute(initcode) != 0) return 1;
		 	maincode = new BytePtr(initcode);
		 	return 0;
		}
	
	
#if true
		
		public static void PrintCode ()
		{
			BytePtr p = new BytePtr(code);
		 	printf ("\n\nCODE\n");
		 	 //FIXME:should be p < pc, so here will overflow
		 	while (p.index < pc.index)
		 	//while (p != pc)
		 	{
		 		switch ((OpCode)p[0])
		  		{
				case OpCode.NOP:		
		 			printf ("%d    NOP\n", p-code);
		 			p.inc();
		 			break;
				
		 		case OpCode.PUSHNIL:	
		 			printf ("%d    PUSHNIL\n", p-code); 
		 			p.inc();
		 			break;
				
		 		case OpCode.PUSH0: case OpCode.PUSH1: case OpCode.PUSH2:
		 			printf ("%d    PUSH%c\n", p-code, (char)(p[0]-(int)OpCode.PUSH0)+'0');
		 			p.inc();
					break;
				
				case OpCode.PUSHBYTE:
					printf ("%d    PUSHBYTE   %d\n", p-code, (int)p[1]);
					p.inc();
					p.inc();
					break;
				
				case OpCode.PUSHWORD:
					Word word_PUSHWORD = (Word)((byte)p[1] | ((byte)p[2] << 8));
					printf ("%d    PUSHWORD   %d\n", p-code, word_PUSHWORD);
					p += 1 + 2;
					break;
				
				case OpCode.PUSHFLOAT:
					printf ("%d    PUSHFLOAT  %f\n", p-code, bytesToFloat(p[1], p[2], p[3], p[4]));
					p += 1 + 4;
					break;
				
				case OpCode.PUSHSTRING:
					Word word_PUSHSTRING = (Word)((byte)p[1] | ((byte)p[2] << 8));
					printf ("%d    PUSHSTRING   %d\n", p-code, word_PUSHSTRING);
					p += 1 + 2;
					break;
				
				case OpCode.PUSHLOCAL0: case OpCode.PUSHLOCAL1: case OpCode.PUSHLOCAL2: case OpCode.PUSHLOCAL3:
				case OpCode.PUSHLOCAL4: case OpCode.PUSHLOCAL5: case OpCode.PUSHLOCAL6: case OpCode.PUSHLOCAL7:
				case OpCode.PUSHLOCAL8: case OpCode.PUSHLOCAL9:
					printf ("%d    PUSHLOCAL%c\n", p-code, (char)(p[0]-OpCode.PUSHLOCAL0+'0'));
					p.inc();
					break;
				
				case OpCode.PUSHLOCAL:	
					printf ("%d    PUSHLOCAL   %d\n", p-code, (int)p[1]);
					p.inc();
					p.inc();
					break;
				
				case OpCode.PUSHGLOBAL:
					Word word_PUSHGLOBAL = (Word)((byte)p[1] | ((byte)p[2] << 8));
					printf ("%d    PUSHGLOBAL   %d\n", p-code, word_PUSHGLOBAL);
					p += 1 + 2;
					break;
				
				case OpCode.PUSHINDEXED:    
					printf ("%d    PUSHINDEXED\n", p-code);
					p.inc();
					break;
				
				case OpCode.PUSHMARK:
					printf ("%d    PUSHMARK\n", p-code); 
					p.inc();
					break;
				
				case OpCode.PUSHOBJECT:
					printf ("%d    PUSHOBJECT\n", p-code);
					p.inc();
					break;
				
				case OpCode.STORELOCAL0: case OpCode.STORELOCAL1: case OpCode.STORELOCAL2: case OpCode.STORELOCAL3:
				case OpCode.STORELOCAL4: case OpCode.STORELOCAL5: case OpCode.STORELOCAL6: case OpCode.STORELOCAL7:
				case OpCode.STORELOCAL8: case OpCode.STORELOCAL9:
					printf ("%d    STORELOCAL%c\n", p-code, (char)(p[0]-OpCode.STORELOCAL0+'0'));
					p.inc();
					break;
						
				case OpCode.STORELOCAL:
					printf ("%d    STORELOCAK   %d\n", p-code, p[+1]);
					p.inc();
					p.inc();
					break;
				
				case OpCode.STOREGLOBAL:
					Word word_STOREGLOBAL = (Word)((byte)p[1] | ((byte)p[2] << 8));
					printf ("%d    STOREGLOBAL   %d\n", p-code, word_STOREGLOBAL);
					p += 1 + sizeof(Word);
					break;
				
				case OpCode.STOREINDEXED0:
					printf ("%d    STOREINDEXED0\n", p-code);
					p.inc();
					break;
				
				case OpCode.STOREINDEXED:
					printf ("%d    STOREINDEXED   %d\n", p-code, (int)p[1]);
					p.inc();
					p.inc();
					break;
					
				case OpCode.STOREFIELD:     
					printf ("%d    STOREFIELD\n", p-code);
					p.inc();
					break;
				
				case OpCode.ADJUST:
					printf ("%d    ADJUST   %d\n", p-code, p[+1]);
					p.inc();
					p.inc();
					break;
					
				case OpCode.CREATEARRAY:	
					printf ("%d    CREATEARRAY\n", p-code);
					p.inc();
					break;
				
				case OpCode.EQOP:       	
					printf ("%d    EQOP\n", p-code);
					p.inc();
					break;
				
				case OpCode.LTOP:       	
					printf ("%d    LTOP\n", p-code);
					p.inc();
					break;
				
				case OpCode.LEOP:       	
					printf ("%d    LEOP\n", p-code);
					p.inc();
					break;
				
				case OpCode.ADDOP:       	
					printf ("%d    ADDOP\n", p-code);
					p.inc();
					break;
				
				case OpCode.SUBOP:       	
					printf ("%d    SUBOP\n", p-code);
					p.inc();
					break;
				
				case OpCode.MULTOP:      	
					printf ("%d    MULTOP\n", p-code);
					p.inc();
					break;
				
				case OpCode.DIVOP:       	
					printf ("%d    DIVOP\n", p-code);
					p.inc();
					break;
				
				case OpCode.CONCOP:       	
					printf ("%d    CONCOP\n", p-code);
					p.inc();
					break;
				
				case OpCode.MINUSOP:
					printf ("%d    MINUSOP\n", p-code);
					p.inc();
					break;
				
				case OpCode.NOTOP:
					printf ("%d    NOTOP\n", p-code);
					p.inc();
					break;
				
				case OpCode.ONTJMP:
					Word word_ONTJMP = (Word)((byte)p[1] | ((byte)p[2] << 8));
					printf ("%d    ONTJMP  %d\n", p-code, word_ONTJMP);
					p += sizeof(Word) + 1;
					break;
		
				case OpCode.ONFJMP:
					Word word_ONFJMP = (Word)((byte)p[1] | ((byte)p[2] << 8));
					printf ("%d    ONFJMP  %d\n", p-code, word_ONFJMP);
					p += 2 + 1;
					break;
				
				case OpCode.JMP:
					Word word_JMP = (Word)((byte)p[1] | ((byte)p[2] << 8));
					printf ("%d    JMP  %d\n", p-code, word_JMP);
					p += 2 + 1;
					break;
				
				case OpCode.UPJMP:
					Word word_UPJMP = (Word)((byte)p[1] | ((byte)p[2] << 8));
					printf ("%d    UPJMP  %d\n", p-code, word_UPJMP);
					p += 2 + 1;
					break;
		
				case OpCode.IFFJMP:
					Word word_IFFJMP = (Word)((byte)p[1] | ((byte)p[2] << 8));
					printf ("%d    IFFJMP  %d\n", p-code, word_IFFJMP);
					p += 2 + 1;
					break;
					
				case OpCode.IFFUPJMP:
					Word word_IFFUPJMP = (Word)((byte)p[1] | ((byte)p[2] << 8));
					printf ("%d    IFFUPJMP  %d\n", p-code, word_IFFUPJMP);
					p += 2 + 1;
					break;
					
				case OpCode.POP:       	
					printf ("%d    POP\n", p-code);
					p.inc();
					break;
				
				case OpCode.CALLFUNC:
					printf ("%d    CALLFUNC\n", p-code);
					p.inc();
					break;
				
				case OpCode.RETCODE:
					printf ("%d    RETCODE   %d\n", p-code, p[+1]); 
					p.inc();
					p.inc();
					break;
				
				case OpCode.HALT: 
					printf ("%d    HALT\n", p-code);p.inc();
					break;
				
				case OpCode.SETFUNCTION:
					Word word_SETFUNCTION1 = (Word)((byte)p[1] | ((byte)p[2] << 8));
					Word word_SETFUNCTION2 = (Word)((byte)p[3] | ((byte)p[4] << 8));
					printf ("%d    SETFUNCTION  %d, %d\n",  p-code, word_SETFUNCTION1, word_SETFUNCTION2);
				    p += 2 * 2 + 1;
				   	break;
				   			
				case OpCode.SETLINE:
				   	Word word_SETLINE = (Word)((byte)p[1] | ((byte)p[2] << 8));
					printf ("%d    SETLINE  %d\n", p-code, word_SETLINE);
				    p += sizeof(Word) + 1;
				   	break;
				
				case OpCode.RESET: 
				   	printf ("%d    RESET\n", p-code); 
				   	p.inc();
				   	break;
					
				default:
					printf ("%d    Cannot happen\n", p-code); 
					p.inc(); 
					break;
		  		}
		 	}
		}
#endif
	
		public static int[] yyexca = {
			-1, 1, 
			0, -1, 
			-2, 2, 
			
			-1, 19, 
			40, 65, 
			91, 95, 
			46, 97, 
			-2, 92, 
			
			-1, 29, 
			40, 65, 
			91, 95, 
			46, 97, 
			-2, 51, 
			
			-1, 70, 
			275, 33, 
			276, 33, 
			61, 33, 
			277, 33, 
			62, 33, 
			60, 33, 
			278, 33, 
			279, 33, 
			280, 33, 
			43, 33, 
			45, 33, 
			42, 33, 
			47, 33, 
			-2, 68, 
			
			-1, 71, 
			91, 95, 
			46, 97, 
			-2, 93, 
			
			-1, 102, 
			260, 27, 
			261, 27, 
			265, 27, 
			266, 27, 
			267, 27, 
			-2, 11, 
			
			-1, 117,
			93, 85, 
			-2, 87, 
			
			-1, 122, 
			267, 30, 
			-2, 29, 
			
			-1, 145, 
			275, 33, 
			276, 33, 
			61, 33, 
			277, 33, 
			62, 33, 
			60, 33, 
			278, 33, 
			279, 33, 
			280, 33, 
			43, 33, 
			45, 33, 
			42, 33, 
			47, 33, 
			-2, 70
		};
//		# define YYNPROD 105
//		# define YYLAST 318
		private const int YYNPROD = 105;
		private const int YYLAST = 318;
		public static int[] yyact = {
			54, 52, 136, 53, 13, 55, 54, 52, 14, 53, 
			15, 55, 5, 166, 18, 6, 129, 21, 47, 46, 
			48, 107, 104, 97, 47, 46, 48, 54, 52, 80, 
			53, 21, 55, 54, 52, 40, 53, 9, 55, 54, 
			52, 158, 53, 160, 55, 47, 46, 48, 159, 101, 
			81, 47, 46, 48, 10, 54, 52, 126, 53, 67, 
			55, 54, 52, 60, 53, 155, 55, 148, 149, 135, 
			147, 108, 150, 47, 46, 48, 73, 23, 75, 47, 
			46, 48, 7, 25, 38, 153, 26, 164, 27, 117, 
			61, 62, 74, 11, 76, 54, 24, 127, 65, 66, 
			55, 37, 154, 151, 103, 111, 72, 28, 93, 94, 
			82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 
			92, 116, 59, 77, 54, 52, 118, 53, 99, 55, 
			110, 95, 64, 44, 70, 109, 29, 33, 105, 106, 
			42, 112, 41, 165, 139, 19, 17, 152, 79, 123, 
			43, 119, 20, 114, 113, 98, 63, 144, 143, 122, 
			68, 39, 36, 130, 35, 120, 12, 8, 102, 125, 
			128, 141, 78, 69, 70, 71, 142, 131, 132, 140, 
			22, 124, 4, 3, 2, 121, 96, 138, 146, 137, 
			134, 157, 133, 115, 16, 1, 0, 0, 0, 0, 
			0, 0, 0, 156, 0, 0, 0, 0, 161, 0, 
			0, 0, 0, 162, 0, 0, 0, 168, 0, 172, 
			145, 163, 171, 0, 174, 0, 0, 0, 169, 156, 
			167, 170, 173, 57, 58, 49, 50, 51, 56, 57, 
			58, 49, 50, 51, 56, 175, 0, 0, 100, 0, 
			45, 0, 0, 0, 0, 70, 0, 0, 0, 0, 
			57, 58, 49, 50, 51, 56, 57, 58, 49, 50, 
			51, 56, 0, 0, 0, 0, 0, 56, 0, 0, 
			0, 0, 0, 0, 0, 0, 0, 0, 57, 58, 
			49, 50, 51, 56, 0, 0, 49, 50, 51, 56, 
			32, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
			0, 0, 30, 0, 21, 31, 0, 34
		};
		public static int[] yypact = {
			-1000, -258, -1000, -1000, -1000, -234, -1000, 34, -254, -1000,
			-1000, -1000, -1000, 43, -1000, -1000, 40, -1000, -236, -1000, 
			-1000, -1000, 93, -9, -1000, 43, 43, 43, 92, -1000, 
			-1000, -1000, -1000, -1000, 43, 43, -1000, 43, -240, 62, 
			31, -13, 48, 83, -242, -1000, 43, 43, 43, 43, 
			43, 43, 43, 43, 43, 43, 43, -1000, -1000, 90, 
			13, -1000, -1000, -248, 43, 19, -15, -216, -1000, 60, 
			-1000, -1000, -249, -1000, -1000, 43, -250, 43, 89, 61, 
			-1000, -1000, -3, -3, -3, -3, -3, -3, 53, 53, 
			-1000, -1000, 82, -1000, -1000, -1000, -2, -1000, 85, 13, 
			-1000, 43, -1000, -1000, 31, 43, -36, -1000, 56, 60, 
			-1000, -255, -1000, 43, 43, -1000, -269, -1000, -1000, -1000, 
			13, 34, -1000, 43, -1000, 13, -1000, -1000, -1000, -1000, 
			-193, 19, 19, -53, 59, -1000, -1000, -8, 58, 43, 
			-1000, -1000, -1000, -1000, -226, -1000, -218, -223, -1000, 43, 
			-1000, -269, 26, -1000, -1000, -1000, 13, -253, 43, -1000, 
			-1000, -1000, -42, -1000, 43, 43, -1000, 34, -1000, 13, 
			-1000, -1000, -1000, -1000, -193, -1000
		};
		public static int[] yypgo = {
			0, 195, 50, 96, 71, 135, 194, 193, 192, 190, 
			189, 187, 136, 186, 184, 82, 54, 183, 182, 180, 
			172, 170, 59, 168, 167, 166, 63, 70, 164, 162, 
			137, 161, 160, 159, 158, 157, 156, 155, 154, 153, 
			152, 150, 149, 148, 69, 147, 144, 65, 143, 142, 
			140, 76, 138
		};
		public static int[] yyr1 = {
			0, 1, 14, 1, 1, 1, 19, 21, 17, 23, 
			23, 24, 15, 16, 16, 25, 28, 25, 29, 25, 
			25, 25, 25, 27, 27, 27, 32, 33, 22, 34, 
			35, 34, 2, 26, 3, 3, 3, 3, 3, 3, 
			3, 3, 3, 3, 3, 3, 3, 3, 36, 3, 
			3, 3, 3, 3, 3, 3, 3, 38, 3, 39, 
			3, 37, 37, 41, 30, 40, 4, 4, 5, 42, 
			5, 20, 20, 43, 43, 13, 13, 7, 7, 8, 
			8, 9, 9, 45, 44, 10, 10, 46, 11, 48, 
			11, 47, 6, 6, 12, 49, 12, 50, 12, 31, 
			31, 51, 52, 51, 18
		};
		public static int[] yyr2 = {
			0, 0, 1, 9, 4, 4, 1, 1, 19, 0, 
			6, 1, 4, 0, 2, 17, 1, 17, 1, 13, 
			7, 3, 4, 0, 4, 15, 1, 1, 9, 0, 
			1, 9, 1, 3, 7, 7, 7, 7, 7, 7, 
			7, 7, 7, 7, 7, 7, 5, 5, 1, 9, 
			9, 3, 3, 3, 3, 3, 5, 1, 11, 1, 
			11, 1, 2, 1, 11, 3, 1, 3, 3, 1, 
			9, 0, 2, 3, 7, 1, 3, 7, 7, 1, 
			3, 3, 7, 1, 9, 1, 3, 1, 5, 1, 
			9, 3, 3, 7, 3, 1, 11, 1, 9, 5, 
			9, 1, 1, 6, 3
		};
		public static int[] yychk = {
			-1000, -1, -14, -17, -18, 270, 273, -15, -24, 271, 
			-16, 59, -25, 258, 262, 264, -6, -30, 268, -12, 
			-40, 271, -19, -26, -3, 40, 43, 45, 64, -12, 
			269, 272, 257, -30, 274, -28, -29, 61, 44, -31, 
			271, -49, -50, -41, 40, 259, 61, 60, 62, 277, 
			278, 279, 43, 45, 42, 47, 280, 275, 276, -3, 
			-26, -26, -26, -36, 40, -26, -26, -22, -32, -5, 
			-3, -12, 44, -51, 61, 91, 46, 40, -20, -43, 
			271, -2, -26, -26, -26, -26, -26, -26, -26, -26, 
			-26, -26, -26, -2, -2, 41, -13, 271, -37, -26, 
			263, 265, -23, 44, 271, -52, -26, 271, -4, -5, 
			41, 44, -22, -38, -39, -7, 123, 91, 41, -2, 
			-26, -15, -33, -42, -51, -26, 93, 41, -21, 271, 
			-2, -26, -26, -8, -9, -44, 271, -10, -11, -46, 
			-22, -2, -16, -34, -35, -3, -22, -27, 260, 261, 
			125, 44, -45, 93, 44, -47, -26, -2, 267, 266, 
			266, -22, -26, -44, 61, -48, 266, -4, 259, -26, 
			-47, -16, -2, -22, -2, -27
		};
		public static int[] yydef = {
			1, -2, 11, 4, 5, 0, 104, 13, 0, 6, 
			3, 14, 12, 0, 16, 18, 0, 21, 0, -2, 
			63, 94, 0, 0, 33, 0, 0, 0, 48, -2, 
			52, 53, 54, 55, 0, 0, 26, 0, 0, 22, 
			101, 0, 0, 0, 71, 32, 0, 0, 0, 0, 
			0, 0, 0, 0, 0, 0, 0, 32, 32, 33, 
			0, 46, 47, 75, 61, 56, 0, 0, 9, 20, 
			-2, -2, 0, 99, 102, 0, 0, 66, 0, 72, 
			73, 26, 35, 36, 37, 38, 39, 40, 41, 42, 
			43, 44, 45, 57, 59, 34, 0, 76, 0, 62, 
			32, 0, -2, 69, 101, 0, 0, 98, 0, 67, 
			7, 0, 32, 0, 0, 49, 79, -2, 50, 26, 
			32, 13, -2, 0, 100, 103, 96, 64, 26, 74, 
			23, 58, 60, 0, 80, 81, 83, 0, 86, 0, 
			32, 19, 10, 28, 0, -2, 0, 0, 26, 0, 
			77, 0, 0, 78, 89, 88, 91, 0, 66, 8, 
			15, 24, 0, 82, 0, 0, 17, 13, 32, 84, 
			90, 31, 26, 32, 23, 25
		};
		public class yytoktype {public CharPtr t_name; public int t_val; 
			public yytoktype(CharPtr t_name, int t_val) 
			{ 
				this.t_name = t_name; 
				this.t_val = t_val;
			} 
		}
//		#ifndef YYDEBUG
//		#	define YYDEBUG	0	/* don't allow debugging */
//		#endif

#if YYDEBUG
		
		public static yytoktype[] yytoks =
		{
			new yytoktype("NIL", 257),
			new yytoktype("IF", 258),
			new yytoktype("THEN", 259),
			new yytoktype("ELSE", 260),
			new yytoktype("ELSEIF", 261),
			new yytoktype("WHILE", 262),
			new yytoktype("DO", 263),
			new yytoktype("REPEAT", 264),
			new yytoktype("UNTIL", 265),
			new yytoktype("END", 266),
			new yytoktype("RETURN", 267),
			new yytoktype("LOCAL", 268),
			new yytoktype("NUMBER", 269),
			new yytoktype("FUNCTION", 270),
			new yytoktype("NAME", 271),
			new yytoktype("STRING", 272),
			new yytoktype("DEBUG", 273),
			new yytoktype("NOT", 274),
			new yytoktype("AND", 275),
			new yytoktype("OR", 276),
			new yytoktype("=", 61),
			new yytoktype("NE", 277),
			new yytoktype(">", 62),
			new yytoktype("<", 60),
			new yytoktype("LE", 278),
			new yytoktype("GE", 279),
			new yytoktype("CONC", 280),
			new yytoktype("+", 43),
			new yytoktype("-", 45),
			new yytoktype("*", 42),
			new yytoktype("/", 47),
			new yytoktype("%", 37),
			new yytoktype("UNARY", 281),
			new yytoktype("-unknown-", -1)	/* ends search */
		};
	
		public static string[] yyreds = {
			"-no such reduction-", 
			"functionlist : /* empty */", 
			"functionlist : functionlist", 
			"functionlist : functionlist stat sc", 
			"functionlist : functionlist function", 
			"functionlist : functionlist setdebug", 
			"function : FUNCTION NAME", 
			"function : FUNCTION NAME '(' parlist ')'", 
			"function : FUNCTION NAME '(' parlist ')' block END", 
			"statlist : /* empty */", 
			"statlist : statlist stat sc", 
			"stat : /* empty */", 
			"stat : stat1", 
			"sc : /* empty */", 
			"sc : ';'", 
			"stat1 : IF expr1 THEN PrepJump block PrepJump elsepart END", 
			"stat1 : WHILE", 
			"stat1 : WHILE expr1 DO PrepJump block PrepJump END", 
			"stat1 : REPEAT", 
			"stat1 : REPEAT block UNTIL expr1 PrepJump", 
			"stat1 : varlist1 '=' exprlist1", 
			"stat1 : functioncall", 
			"stat1 : LOCAL declist", 
			"elsepart : /* empty */", 
			"elsepart : ELSE block", 
			"elsepart : ELSEIF expr1 THEN PrepJump block PrepJump elsepart", 
			"block : /* empty */", 
			"block : statlist", 
			"block : statlist ret", 
			"ret : /* empty */", 
			"ret : /* empty */", 
			"ret : RETURN exprlist sc", 
			"PrepJump : /* empty */", 
			"expr1 : expr", 
			"expr : '(' expr ')'", 
			"expr : expr1 '=' expr1", 
			"expr : expr1 '<' expr1", 
			"expr : expr1 '>' expr1", 
			"expr : expr1 NE expr1", 
			"expr : expr1 LE expr1", 
			"expr : expr1 GE expr1", 
			"expr : expr1 '+' expr1", 
			"expr : expr1 '-' expr1", 
			"expr : expr1 '*' expr1", 
			"expr : expr1 '/' expr1", 
			"expr : expr1 CONC expr1", 
			"expr : '+' expr1", 
			"expr : '-' expr1", 
			"expr : '@'", "expr : '@' objectname fieldlist", 
			"expr : '@' '(' dimension ')'", 
			"expr : var", 
			"expr : NUMBER", 
			"expr : STRING", 
			"expr : NIL", 
			"expr : functioncall", 
			"expr : NOT expr1", 
			"expr : expr1 AND PrepJump", 
			"expr : expr1 AND PrepJump expr1", 
			"expr : expr1 OR PrepJump", 
			"expr : expr1 OR PrepJump expr1", 
			"dimension : /* empty */", 
			"dimension : expr1", 
			"functioncall : functionvalue", 
			"functioncall : functionvalue '(' exprlist ')'", 
			"functionvalue : var", 
			"exprlist : /* empty */", 
			"exprlist : exprlist1", 
			"exprlist1 : expr", 
			"exprlist1 : exprlist1 ','", 
			"exprlist1 : exprlist1 ',' expr", 
			"parlist : /* empty */", 
			"parlist : parlist1", 
			"parlist1 : NAME", 
			"parlist1 : parlist1 ',' NAME", 
			"objectname : /* empty */", 
			"objectname : NAME", 
			"fieldlist : '{' ffieldlist '}'", 
			"fieldlist : '[' lfieldlist ']'", 
			"ffieldlist : /* empty */", 
			"ffieldlist : ffieldlist1", 
			"ffieldlist1 : ffield", 
			"ffieldlist1 : ffieldlist1 ',' ffield", 
			"ffield : NAME", 
			"ffield : NAME '=' expr1", 
			"lfieldlist : /* empty */", 
			"lfieldlist : lfieldlist1", 
			"lfieldlist1 : /* empty */", 
			"lfieldlist1 : lfield", 
			"lfieldlist1 : lfieldlist1 ','", 
			"lfieldlist1 : lfieldlist1 ',' lfield", 
			"lfield : expr1", 
			"varlist1 : var", 
			"varlist1 : varlist1 ',' var", 
			"var : NAME", 
			"var : var", 
			"var : var '[' expr1 ']'", 
			"var : var", 
			"var : var '.' NAME", 
			"declist : NAME init", 
			"declist : declist ',' NAME init", 
			"init : /* empty */", 
			"init : '='", 
			"init : '=' expr1", 
			"setdebug : DEBUG"
		};
#endif
		//#line 1 "/usr/lib/yaccpar"
		/*	@(#)yaccpar 1.10 89/04/04 SMI; from S5R3 1.10	*/
	
		/*
		** Skeleton parser driver for yacc output
		*/
	
		/*
		** yacc user known macros and defines
		*/
//		#define YYERROR		goto yyerrlab
		public static int YYACCEPT()	{ free(yys); free(yyv); return(0); }
		public static int YYABORT()		{ free(yys); free(yyv); return(1); }
//		#define YYBACKUP( newtoken, newvalue )\
//		{\
//			if ( yychar >= 0 || ( yyr2[ yytmp ] >> 1 ) != 1 )\
//			{\
//				yyerror( "syntax error - cannot backup" );\
//				goto yyerrlab;\
//			}\
//			yychar = newtoken;\
//			yystate = *yyps;\
//			yylval = newvalue;\
//			goto yynewstate;\
//		}
//		#define YYRECOVERING()	(!!yyerrflag)
//		#ifndef YYDEBUG
//		#	define YYDEBUG	1	/* make debugging available */
//		#endif
	
		/*
		** user known globals
		*/
		public static int yydebug; // set to 1 to get debugging
	
		/*
		** driver internal defines
		*/
//		#define YYFLAG		(-1000)
		private const int YYFLAG = -1000;
		
		/*
		** static variables used by the parser
		*/
		private static YYSTYPE[] yyv;			/* value stack */
		private static int[] yys;			/* state stack */
	
		private static YYSTYPEPtr yypv;			/* top of value stack */
		private static IntegerPtr yyps;			/* top of state stack */
	
		private static int yystate;			/* current state */
		private static int yytmp;			/* extra var (lasts between blocks) */
	
		public static int yynerrs;			/* number of errors */
	
		public static int yyerrflag;			/* error recovery flag */
		public static int yychar;			/* current input token number */
		

		/*
		** yyparse - return 0 if worked, 1 if syntax error not recovered from
		*/
		public static int yyparse()
		{
			YYSTYPEPtr yypvt = null;	/* top of value stack for $vars */
			uint yymaxdepth = YYMAXDEPTH;
			/*
			** Initialize externals - yyparse may be called more than once
			*/
			yyv = new YYSTYPE[yymaxdepth]; for (int i = 0; i < yymaxdepth; ++i) yyv[i] = new YYSTYPE();
			yys = new int[yymaxdepth];
			if (yyv==null || yys==null) 
			{
				yyerror( "out of memory" );
				return (1);
			}
			yypv = new YYSTYPEPtr(yyv, -1);
			yyps = new IntegerPtr(yys, -1);
			yystate = 0;
			yytmp = 0;
			yynerrs = 0;
			yyerrflag = 0;
			yychar = -1;
	
			//goto yystack; //FIXME:move to below
//			{
			
			YYSTYPEPtr yy_pv; // top of value stack
			IntegerPtr yy_ps; // top of state stack
			int yy_state; // current state
			int yy_n; // internal state number info
	
//			/*
//			** get globals into registers.
//			** branch to here only if YYBACKUP was called.
//			*/
////yynewstate:
//			yy_pv = yypv;
//			yy_ps = yyps;
//			yy_state = yystate;
//			goto yy_newstate;

			/*
			** get globals into registers.
			** either we just started, or we just finished a reduction
			*/
yystack:
			yy_pv = new YYSTYPEPtr(yypv);
			yy_ps = new IntegerPtr(yyps);
			yy_state = yystate;
	
			/*
			** top of for (;;) loop while no reductions done
			*/
yy_stack:
			/*
			** put a state and value onto the stacks
			*/
#if YYDEBUG
			/*
			** if debugging, look up token value in list of value vs.
			** name pairs.  0 and negative (-1) are special values.
			** Note: linear search is used since time is not a real
			** consideration while debugging.
			*/
			if (yydebug != 0)
			{
				int yy_i;
	
				printf( "State %d, token ", yy_state );
				if ( yychar == 0 )
					printf( "end-of-file\n" );
				else if ( yychar < 0 )
					printf( "-none-\n" );
				else
				{
					for ( yy_i = 0; yytoks[yy_i].t_val >= 0;
						yy_i++ )
					{
						if ( yytoks[yy_i].t_val == yychar )
							break;
					}
					printf( "%s\n", yytoks[yy_i].t_name );
				}
			}
#endif
			yy_ps.inc();
			if ( yy_ps >= new IntegerPtr(yys, (int)yymaxdepth) )	/* room on stack? */
			{
				/*
				** reallocate and recover.  Note that pointers
				** have to be reset, or bad things will happen
				*/
				int yyps_index = (yy_ps - yys);
				int yypv_index = (yy_pv - yyv);
				int yypvt_index = (yypvt - yyv);
				yymaxdepth += YYMAXDEPTH;
				yyv = realloc_YYSTYPE(yyv,
					yymaxdepth);
				yys = realloc_int(yys,
					yymaxdepth);
				if (yyv==null || yys==null)
				{
					yyerror( "yacc stack overflow" );
					return(1);
				}
				yy_ps = new IntegerPtr(yys, yyps_index);
				yy_pv = new YYSTYPEPtr(yyv, yypv_index);
				yypvt = new YYSTYPEPtr(yyv, yypvt_index);
			}
			yy_ps[0] = yy_state;
			yy_pv.inc(); yy_pv[0].set(yyval);
			
			/*
			** we have a new state - find out what to do
			*/
yy_newstate:
			if ( ( yy_n = yypact[ yy_state ] ) <= YYFLAG )
				goto yydefault;		/* simple state */
#if YYDEBUG
			/*
			** if debugging, need to mark whether new token grabbed
			*/
			yytmp = yychar < 0 ? 1 : 0;
#endif
			if ((yychar < 0) && ((yychar = yylex()) < 0))
			{
				yychar = 0; // reached EOF
			}
#if YYDEBUG
			if ( yydebug!=0 && yytmp!=0 )
			{
				int yy_i;

				printf( "Received token " );
				if ( yychar == 0 )
					printf( "end-of-file\n" );
				else if ( yychar < 0 )
					printf( "-none-\n" );
				else
				{
					for ( yy_i = 0;
						yytoks[yy_i].t_val >= 0;
						yy_i++ )
					{
						if ( yytoks[yy_i].t_val
							== yychar )
						{
							break;
						}
					}
					printf( "%s\n", yytoks[yy_i].t_name );
				}
			}
#endif
			if ( ( ( yy_n += yychar ) < 0 ) || ( yy_n >= YYLAST ) )
				goto yydefault;
			if ( yychk[ yy_n = yyact[ yy_n ] ] == yychar )	/*valid shift*/
			{
				yychar = -1;
				yyval = yylval;
				yy_state = yy_n;
				if ( yyerrflag > 0 )
					yyerrflag--;
				goto yy_stack;
			}
	
yydefault:
			if ( ( yy_n = yydef[ yy_state ] ) == -2 )
			{
#if YYDEBUG
				yytmp = (yychar < 0 ? 1 : 0);
#endif
				if ( ( yychar < 0 ) && ( ( yychar = yylex() ) < 0 ) )
					yychar = 0;		/* reached EOF */
#if YYDEBUG
				if ( yydebug != 0 && yytmp != 0 )
				{
					int yy_i;
	
					printf( "Received token " );
					if ( yychar == 0 )
						printf( "end-of-file\n" );
					else if ( yychar < 0 )
						printf( "-none-\n" );
					else
					{
						for ( yy_i = 0;
							yytoks[yy_i].t_val >= 0;
							yy_i++ )
						{
							if ( yytoks[yy_i].t_val
								== yychar )
							{
								break;
							}
						}
						printf( "%s\n", yytoks[yy_i].t_name );
					}
				}
#endif
				/*
				** look through exception table
				*/
				{
					IntegerPtr yyxi = new IntegerPtr(yyexca);
	
					while ( ( yyxi[0] != -1 ) ||
						( yyxi[1] != yy_state ) )
					{
						yyxi += 2;
					}
					while ( ( (yyxi += 2)[0] >= 0 ) &&
					    ( yyxi[0] != yychar ) )
						;
					if ( ( yy_n = yyxi[1] ) < 0 )
						return YYACCEPT();
				}
			}


				
			/*
			** check for syntax error
			*/
			if (yy_n == 0) // have an error
			{
				/* no worry about speed here! */
				switch (yyerrflag)
				{
				case 0: // new error
					yyerror("syntax error");
					goto case 1; //goto skip_init;
//yyerrlab:
//					/*
//					** get globals into registers.
//					** we have a user generated syntax type error
//					*/
//					yy_pv = yypv;
//					yy_ps = yyps;
//					yy_state = yystate;
//					yynerrs++;
//					goto case 1;
//skip_init:
				case 1:
				case 2: 	/* incompletely recovered error */
						/* try again... */
					yyerrflag = 3;
					/*
					** find state where "error" is a legal
					** shift action
					*/
					while (yy_ps >= yys)
					{
						yy_n = yypact[yy_ps[0]] + YYERRCODE;
						if (yy_n >= 0 && yy_n < YYLAST && yychk[yyact[yy_n]] == YYERRCODE)
						{
							/*
							** simulate shift of "error"
							*/
							yy_state = yyact[yy_n];
							goto yy_stack;
						}
						/*
						** current state has no shift on
						** "error", pop stack
						*/
#if YYDEBUG
						string _POP_ = "Error recovery pops state %d, uncovers state %d\n";
						if (yydebug != 0)
						{
							printf(_POP_, yy_ps, yy_ps[-1]);
						}
						//# undef _POP_
#endif
						yy_ps.dec();
						yy_pv.dec();
					}
					/*
					** there is no state on stack with "error" as
					** a valid shift.  give up.
					*/
					return YYABORT();
					
				case 3: // no shift yet; eat a token
#if YYDEBUG
					/*
					** if debugging, look up token in list of
					** pairs.  0 and negative shouldn't occur,
					** but since timing doesn't matter when
					** debugging, it doesn't hurt to leave the
					** tests here.
					*/
					if ( yydebug != 0 )
					{
						int yy_i;
	
						printf( "Error recovery discards " );
						if ( yychar == 0 )
							printf( "token end-of-file\n" );
						else if ( yychar < 0 )
							printf( "token -none-\n" );
						else
						{
							for ( yy_i = 0;
								yytoks[yy_i].t_val >= 0;
								yy_i++ )
							{
								if ( yytoks[yy_i].t_val
									== yychar )
								{
									break;
								}
							}
							printf( "token %s\n",
								yytoks[yy_i].t_name );
						}
					}
#endif
					if (yychar == 0) // reached EOF. quit
					{
						return YYABORT();
					}
					yychar = -1;
					goto yy_newstate;
				}
			} // end if ( yy_n == 0 )
			
			/*
			** reduction by production yy_n
			** put stack tops, etc. so things right after switch
			*/
#if YYDEBUG
			/*
			** if debugging, print the string that is the user's
			** specification of the reduction which is just about
			** to be done.
			*/
			if ( yydebug != 0 )
				printf( "Reduce by (%d) \"%s\"\n",
					yy_n, yyreds[ yy_n ] );
#endif
			yytmp = yy_n;			/* value to switch over */
			yypvt = yy_pv;			/* $vars top of value stack */
			/*
			** Look in goto table for next state
			** Sorry about using yy_state here as temporary
			** register variable, but why not, if it works...
			** If yyr2[ yy_n ] doesn't have the low order bit
			** set, then there is no action to be done for
			** this reduction.  So, no saving & unsaving of
			** registers done.  The only difference between the
			** code just after the if and the body of the if is
			** the goto yy_stack in the body.  This way the test
			** can be made before the choice of what to do is needed.
			*/
			{
				/* length of production doubled with extra bit */
				int yy_len = yyr2[ yy_n ];
	
				if ( ( yy_len & 01 )==0 )
				{
					yy_len >>= 1;
					yyval = ( yy_pv -= yy_len )[1];	/* $$ = $1 */
					yy_state = yypgo[ yy_n = yyr1[ yy_n ] ] +
						( yy_ps -= yy_len )[0] + 1;
					if ( yy_state >= YYLAST ||
						yychk[ yy_state =
						yyact[ yy_state ] ] != -yy_n )
					{
						yy_state = yyact[ yypgo[ yy_n ] ];
					}
					goto yy_stack;
				}
				yy_len >>= 1;
				yyval = ( yy_pv -= yy_len )[1];	/* $$ = $1 */
				yy_state = yypgo[ yy_n = yyr1[ yy_n ] ] +
					( yy_ps -= yy_len )[0] + 1;
				if ( yy_state >= YYLAST ||
					yychk[ yy_state = yyact[ yy_state ] ] != -yy_n )
				{
					yy_state = yyact[ yypgo[ yy_n ] ];
				}
			}
					/* save until reenter driver code */
			yystate = yy_state;
			yyps = new IntegerPtr(yy_ps);
			yypv = new YYSTYPEPtr(yy_pv);
				


//			} //FIXME:end: goto yystack;				
			/*
			** code supplied by user is placed in this switch
			*/
			switch (yytmp)
			{		
			case 2:
				//#line 179 "lua.stx"
				{
					basepc = new BytePtr(maincode); pc = new BytePtr(basepc);
					nlocalvar = 0;
				}
				break;
				
			case 3:
				//#line 179 "lua.stx"
				{
					maincode = new BytePtr(pc);
				}
				break;
				
			case 6:
				//#line 184 "lua.stx"
				{
					basepc = new BytePtr(code); pc = new BytePtr(basepc);
					nlocalvar = 0;
				}
				break;
				
			case 7:
				//#line 185 "lua.stx"
				{
					if (lua_debug != 0)
					{
						align(2);
						code_byte((byte)OpCode.SETFUNCTION);
						code_word(yypvt[-5].vWord);
						code_word(yypvt[-4].vWord);
					}
					lua_codeadjust(0);
				}
				break;
				
			case 8:
				//#line 197 "lua.stx"
				{
					if (lua_debug != 0)
					{
						code_byte((byte)OpCode.RESET);
					}
					code_byte((byte)OpCode.RETCODE);
					code_byte(nlocalvar);
					s_tag(yypvt[-7].vWord, Type.T_FUNCTION);
					BytePtr ptr = new BytePtr(new byte[pc - code], 0);
					s_bvalue(yypvt[-7].vWord, ptr);
					memcpy(s_bvalue(yypvt[-7].vWord), code, (uint)((pc - code) * 1));
//					if (false)
//					{
//						for (int i = 0; i < pc-code; ++i)
//						{
//							printf("%d: %x\n", i, ptr[i]);
//						}
//					}
//					else 
					if (false)
					{
						BytePtr ptr2 = new BytePtr(ptr);
						CharPtr str = new CharPtr(new char[200]);
						printf("func begin\n");
						while (ptr2.index < pc - code)
						{
							int index = ptr2.index;
							ptr2 = PrintCodeName(str, ptr2);
							printf("[%d] %s\n", index, str.ToString());
						}
						printf("func end\n");
					}
				}
				break;
				
			case 11:
				//#line 210 "lua.stx"
				{
					ntemp = 0;
					if (lua_debug != 0)
					{
				 		align(2);
				 		code_byte((byte)OpCode.SETLINE);
				 		code_word((Word)lua_linenumber);
					}
				}
				break;
				
			case 15:
				//#line 223 "lua.stx"
				{
					{
						BytePtr elseinit = yypvt[-2].pByte + 2+1;
						if (pc - elseinit == 0) // no else
						{
						  	pc -= 2 + 1;
						 	/* if (*(pc-1) == NOP) --pc; */
						  	elseinit = pc;
						}
						else
						{
							yypvt[-2].pByte[0] = (byte)OpCode.JMP;
							Word tempWord = (Word)(pc - elseinit); yypvt[-2].pByte[+1] = (byte)(tempWord & 0xff); yypvt[-2].pByte[+1+1] = (byte)((tempWord >> 8) & 0xff);
						}
						yypvt[-4].pByte[0] = (byte)OpCode.IFFJMP;
						Word tempWord2 = (Word)(elseinit - (yypvt[-4].pByte + 2+1)); yypvt[-4].pByte[+1] = (byte)(tempWord2 & 0xff); yypvt[-4].pByte[+1+1] = (byte)((tempWord2 >> 8) & 0xff);
					}
				}
				break;
				   
			case 16:
				//#line 242 "lua.stx"
				{
					yyval.pByte = new BytePtr(pc);
				}
				break;
	
			case 17:
				//#line 244 "lua.stx"
				{
					yypvt[-3].pByte[0] = (byte)OpCode.IFFJMP;
					Word tempWord = (Word)(pc - (yypvt[-3].pByte + 2 + 1)); yypvt[-3].pByte[+1] = (byte)(tempWord & 0xff); yypvt[-3].pByte[+1+1] = (byte)((tempWord >> 8) & 0xff);
			
					yypvt[-1].pByte[0] = (byte)OpCode.UPJMP;
					Word tempWord2 = (Word)(pc - yypvt[-6].pByte); yypvt[-1].pByte[+1] = (byte)(tempWord2 & 0xff); yypvt[-1].pByte[+1+1] = (byte)((tempWord2 >> 8) & 0xff);
				}
				break;
				
			case 18:
				//#line 252 "lua.stx"
				{
					yyval.pByte = new BytePtr(pc);
				}
				break;
				
			case 19:
				//#line 254 "lua.stx"
				{
					yypvt[-0].pByte[0] = (byte)OpCode.IFFUPJMP;
					Word tempWord = (Word)(pc - yypvt[-4].pByte); yypvt[-0].pByte[+1] = (byte)(tempWord & 0xff); yypvt[-0].pByte[+1+1] = (byte)((tempWord >> 8) & 0xff);
				}
				break;
				
			case 20:
				//#line 261 "lua.stx"
				{
					{
						int i;
						if (yypvt[-0].vInt == 0 || nvarbuffer != ntemp - yypvt[-2].vInt * 2)
							lua_codeadjust(yypvt[-2].vInt * 2 + nvarbuffer);
						for (i=nvarbuffer-1; i>=0; i--)
							lua_codestore(i);
						if (yypvt[-2].vInt > 1 || (yypvt[-2].vInt == 1 && varbuffer[0] != 0))
							lua_codeadjust(0);
					}
				}
				break;
				
			case 21:
				//#line 272 "lua.stx"
				{
					lua_codeadjust (0);
				}
				break;
	
			case 25:
				//#line 279 "lua.stx"
				{
					{
						BytePtr elseinit = yypvt[-1].pByte + 2 + 1;
						if (pc - elseinit == 0)		/* no else */
						{
							pc -= 2 + 1;
							/* if (*(pc-1) == NOP) --pc; */
							elseinit = pc;
						}
						else
						{
							yypvt[-1].pByte[0] = (byte)OpCode.JMP;
							Word tempWord = (Word)(pc - elseinit); yypvt[-1].pByte[+1] = (byte)(tempWord & 0xff); yypvt[-1].pByte[+1+1] = (byte)((tempWord >> 8) & 0xff);
						}
						yypvt[-3].pByte[0] = (byte)OpCode.IFFJMP;
						Word tempWord2 = (Word)(elseinit - (yypvt[-3].pByte + 2 + 1)); yypvt[-3].pByte[+1] = (byte)(tempWord2 & 0xff); yypvt[-3].pByte[+1+1] = (byte)((tempWord2>>8) & 0xff);
					 }
				}
				break;
	
			case 26:
				//#line 299 "lua.stx"
				{
					yyval.vInt = nlocalvar;
				}
				break;
	
			case 27:
				//#line 299 "lua.stx"
				{
					ntemp = 0;
				}
				break;
	
			case 28:
				//#line 300 "lua.stx"
				{
					if (nlocalvar != yypvt[-3].vInt)
					{
						nlocalvar = (byte)yypvt[-3].vInt;
					   	lua_codeadjust (0);
					}
				}
				break;
	
			case 30:
				//#line 310 "lua.stx"
				{
					if (lua_debug != 0)
					{
						align(2);
						code_byte((byte)OpCode.SETLINE);
						code_word((Word)lua_linenumber);
					}
				}
				break;
	
			case 31:
				//#line 312 "lua.stx"
				{
					if (lua_debug != 0) code_byte((byte)OpCode.RESET);
					code_byte((byte)OpCode.RETCODE); code_byte(nlocalvar);
				}
				break;
				
			case 32:
				//#line 319 "lua.stx"
				{
					align(2);
					yyval.pByte = new BytePtr(pc);
					code_byte(0);		/* open space */
					code_word (0);
				}
				break;
	
			case 33:
				//#line 326 "lua.stx"
				{
					if (yypvt[-0].vInt == 0) {lua_codeadjust(ntemp + 1); incr_ntemp();}
				}
				break;
				
			case 34:
				//#line 329 "lua.stx"
				{
					yyval.vInt = yypvt[-1].vInt;
				}
				break;
	
			case 35:
				//#line 330 "lua.stx"
				{
					code_byte((byte)OpCode.EQOP); yyval.vInt = 1; ntemp--;
				}
				break;
	
			case 36:
				//#line 331 "lua.stx"
				{
					code_byte((byte)OpCode.LTOP); yyval.vInt = 1; ntemp--;
				}
				break;
	
			case 37:
				//#line 332 "lua.stx"
				{
					code_byte((byte)OpCode.LEOP); code_byte((byte)OpCode.NOTOP); yyval.vInt = 1; ntemp--;
				}
				break;
	
			case 38:
				//#line 333 "lua.stx"
				{
					code_byte((byte)OpCode.EQOP); code_byte((byte)OpCode.NOTOP); yyval.vInt = 1; ntemp--;
				}
				break;
	
			case 39:
				//#line 334 "lua.stx"
				{
					code_byte((byte)OpCode.LEOP); yyval.vInt = 1; ntemp--;
				}
				break;
	
			case 40:
				//#line 335 "lua.stx"
				{
					code_byte((byte)OpCode.LTOP); code_byte((byte)OpCode.NOTOP); yyval.vInt = 1; ntemp--;
				}
				break;
	
			case 41:
				//#line 336 "lua.stx"
				{
					code_byte((byte)OpCode.ADDOP); yyval.vInt = 1; ntemp--;
				}
				break;
	
			case 42:
				//#line 337 "lua.stx"
				{
					code_byte((byte)OpCode.SUBOP); yyval.vInt = 1; ntemp--;
				}
				break;
	
			case 43:
				//#line 338 "lua.stx"
				{
					code_byte((byte)OpCode.MULTOP); yyval.vInt = 1; ntemp--;
				}
				break;
			
			case 44:
				//#line 339 "lua.stx"
				{
					code_byte((byte)OpCode.DIVOP); yyval.vInt = 1; ntemp--;
				}
				break;
	
			case 45:
				//#line 340 "lua.stx"
				{
					code_byte((byte)OpCode.CONCOP); yyval.vInt = 1; ntemp--;
				}
				break;
			
			case 46:
				//#line 341 "lua.stx"
				{
					yyval.vInt = 1;
				}
				break;
	
			case 47:
				//#line 342 "lua.stx"
				{
					code_byte((byte)OpCode.MINUSOP); yyval.vInt = 1;
				}
				break;
	
			case 48:
				//#line 344 "lua.stx"
				{
					code_byte((byte)OpCode.PUSHBYTE);
					yyval.pByte = new BytePtr(pc); code_byte(0);
					incr_ntemp();
					code_byte((byte)OpCode.CREATEARRAY);
				}
				break;
	
			case 49:
				//#line 351 "lua.stx"
				{
					yypvt[-2].pByte[0] = (byte)yypvt[-0].vInt;
					if (yypvt[-1].vLong < 0) // there is no function to be called
					{
						yyval.vInt = 1;
					}
					else
					{
						lua_pushvar((int)(yypvt[-1].vLong + 1));
						code_byte((byte)OpCode.PUSHMARK);
					   	incr_ntemp();
					   	code_byte((byte)OpCode.PUSHOBJECT);
					   	incr_ntemp();
					   	code_byte((byte)OpCode.CALLFUNC);
					   	ntemp -= 4;
					   	yyval.vInt = 0;
					   	if (lua_debug!=0)
					   	{
							align(2);
							code_byte((byte)OpCode.SETLINE);
							code_word((Word)lua_linenumber);
					   	}
					}
				}
				break;
			
			case 50:
				//#line 374 "lua.stx"
				{
					code_byte((byte)OpCode.CREATEARRAY);
					yyval.vInt = 1;
				}
				break;
			
			case 51:
				//#line 378 "lua.stx"
				{
					lua_pushvar((int)yypvt[-0].vLong);
					yyval.vInt = 1;
				}
				break;
			
			case 52:
				//#line 379 "lua.stx"
				{
					code_number(yypvt[-0].vFloat);
					yyval.vInt = 1;
				}
				break;
	
			case 53:
				//#line 381 "lua.stx"
				{
					align(2);
					code_byte((byte)OpCode.PUSHSTRING);
					code_word(yypvt[-0].vWord);
					yyval.vInt = 1;
					incr_ntemp();
				}
				break;
	
			case 54:
				//#line 388 "lua.stx"
				{
					code_byte((byte)OpCode.PUSHNIL); yyval.vInt = 1; incr_ntemp();
				}
				break;
			
			case 55:
				//#line 390 "lua.stx"
				{
					yyval.vInt = 0;
					if (lua_debug!=0)
					{
						align(2);
						code_byte((byte)OpCode.SETLINE);
						code_word((Word)lua_linenumber);
					}
				}
				break;
			
			case 56:
				//#line 397 "lua.stx"
				{
					code_byte((byte)OpCode.NOTOP);
					yyval.vInt = 1;
				}
				break;
			
			case 57:
				//#line 398 "lua.stx"
				{
					code_byte((byte)OpCode.POP);
					ntemp--;
				}
				break;
				
			case 58:
				//#line 399 "lua.stx"
				{
					yypvt[-2].pByte[0] = (byte)OpCode.ONFJMP;
					Word tempWord = (Word)(pc - (yypvt[-2].pByte + 2 + 1)); yypvt[-2].pByte[+1] = (byte)(tempWord & 0xff); yypvt[-2].pByte[+1+1] = (byte)((tempWord >> 8) & 0xff);
					yyval.vInt = 1;
				}
				break;
			
			case 59:
				//#line 404 "lua.stx"
				{
					code_byte((byte)OpCode.POP);
					ntemp--;
				}
				break;
			
			case 60:
				//#line 405 "lua.stx"
				{
					yypvt[-2].pByte[0] = (byte)OpCode.ONTJMP;
					Word tempWord = (Word)(pc - (yypvt[-2].pByte + 2 + 1)); yypvt[-2].pByte[+1] = (byte)(tempWord & 0xff); yypvt[-2].pByte[+1+1] = (byte)((tempWord >> 8) & 0xff);
					yyval.vInt = 1;
				}
				break;
			
			case 61:
				//#line 412 "lua.stx"
				{
					code_byte((byte)OpCode.PUSHNIL); incr_ntemp();
				}
				break;
				
			case 63:
				//#line 416 "lua.stx"
				{
					code_byte((byte)OpCode.PUSHMARK); yyval.vInt = ntemp; incr_ntemp();
				}
				break;
	
			case 64:
				//#line 417 "lua.stx"
				{
					code_byte((byte)OpCode.CALLFUNC); ntemp = yypvt[-3].vInt - 1;
				}
				break;
			
			case 65:
				//#line 419 "lua.stx"
				{
					lua_pushvar((int)yypvt[-0].vLong);
				}
				break;
			
			case 66:
				//#line 422 "lua.stx"
				{
					yyval.vInt = 1;
				}
				break;
	
			case 67:
				//#line 423 "lua.stx"
				{
					yyval.vInt = yypvt[-0].vInt;
				}
				break;
	
			case 68:
				//#line 426 "lua.stx"
				{
					yyval.vInt = yypvt[-0].vInt;
				}
				break;
			
			case 69:
				//#line 427 "lua.stx"
				{
					if (yypvt[-1].vInt==0) {lua_codeadjust(ntemp + 1); incr_ntemp();}
				}
				break;
			
			case 70:
				//#line 428 "lua.stx"
				{
					yyval.vInt = yypvt[-0].vInt;
				}
				break;
			
			case 73:
				//#line 435 "lua.stx"
				{
					localvar[nlocalvar] = yypvt[-0].vWord;
					incr_nlocalvar();
				}
				break;
	
			case 74:
				//#line 436 "lua.stx"
				{
					localvar[nlocalvar] = yypvt[-0].vWord; incr_nlocalvar();
				}
				break;
			
			case 75:
				//#line 439 "lua.stx"
				{
					yyval.vLong = -1;
				}
				break;
	
			case 76:
				//#line 440 "lua.stx"
				{
					yyval.vLong = yypvt[-0].vWord;
				}
				break;
	
			case 77:
				//#line 443 "lua.stx"
				{
					yyval.vInt = yypvt[-1].vInt;
				}
				break;
	
			case 78:
				//#line 444 "lua.stx"
				{
					yyval.vInt = yypvt[-1].vInt;
				}
				break;
	
			case 79:
				//#line 447 "lua.stx"
				{
					yyval.vInt = 0;
				}
				break;
			
			case 80:
				//#line 448 "lua.stx"
				{
					yyval.vInt = yypvt[-0].vInt;
				}
				break;
	
			case 81:
				//#line 451 "lua.stx"
				{
					yyval.vInt = 1;
				}
				break;
	
			case 82:
				//#line 452 "lua.stx"
				{
					yyval.vInt = yypvt[-2].vInt + 1;
				}
				break;
	
			case 83:
				//#line 456 "lua.stx"
				{
					align(2);
					code_byte((byte)OpCode.PUSHSTRING);
					code_word((Word)lua_findconstant(s_name(yypvt[-0].vWord)));
					incr_ntemp();
				}
				break;
				
			case 84:
				//#line 463 "lua.stx"
				{
					code_byte((byte)OpCode.STOREFIELD); ntemp -= 2;
				}
				break;
	
			case 85:
				//#line 469 "lua.stx"
				{
					yyval.vInt = 0;
				}
				break;
	
			case 86:
				//#line 470 "lua.stx"
				{
					yyval.vInt = yypvt[-0].vInt;
				}
				break;
	
			case 87:
				//#line 473 "lua.stx"
				{
					code_number(1);
				}
				break;
	
			case 88:
				//#line 473 "lua.stx"
				{
					yyval.vInt = 1;
				}
				break;
	
			case 89:
				//#line 474 "lua.stx"
				{
					code_number(yypvt[-1].vInt + 1);
				}
				break;
				
			case 90:
				//#line 475 "lua.stx"
				{
					yyval.vInt = yypvt[-3].vInt + 1;
				}
				break;
				
			case 91:
				//#line 479 "lua.stx"
				{
					code_byte((byte)OpCode.STOREFIELD); ntemp -= 2;
				}
				break;
	
			case 92:
				//#line 486 "lua.stx"
				{
					nvarbuffer = 0;
					varbuffer[nvarbuffer] = yypvt[-0].vLong; incr_nvarbuffer();
					yyval.vInt = (yypvt[-0].vLong == 0) ? 1 : 0;
				}
				break;
	
			case 93:
				//#line 492 "lua.stx"
				{
					varbuffer[nvarbuffer] = yypvt[-0].vLong; incr_nvarbuffer();
					yyval.vInt = (yypvt[-0].vLong == 0) ? yypvt[-2].vInt + 1 : yypvt[-2].vInt;
				}
				break;
			
			case 94:
				//#line 499 "lua.stx"
				{
					int local = lua_localname(yypvt[-0].vWord);
					if (local == -1)	/* global var */
						yyval.vLong = yypvt[-0].vWord + 1;		/* return positive value */
					else
						yyval.vLong = -(local + 1);		/* return negative value */
				}
				break;
	
			case 95:
				//#line 507 "lua.stx"
				{
					lua_pushvar ((int)yypvt[-0].vLong);
				}
				break;
	
			case 96:
				//#line 508 "lua.stx"
				{
					yyval.vLong = 0;		/* indexed variable */
				}
				break;
			
			case 97:
				//#line 511 "lua.stx"
				{
					lua_pushvar ((int)yypvt[-0].vLong);
				}
				break;
				
			case 98:
				//#line 512 "lua.stx"
				{
					align(2);
					code_byte((byte)OpCode.PUSHSTRING);
					code_word((Word)lua_findconstant(s_name(yypvt[-0].vWord))); incr_ntemp();
					yyval.vLong = 0;		/* indexed variable */
				}
				break;
	
			case 99:
				//#line 520 "lua.stx"
				{
					localvar[nlocalvar] = yypvt[-1].vWord; incr_nlocalvar();
				}
				break;
				
			case 100:
				//#line 521 "lua.stx"
				{
					localvar[nlocalvar] = yypvt[-1].vWord; incr_nlocalvar();
				}
				break;
				
			case 101:
				//#line 524 "lua.stx"
				{
					code_byte((byte)OpCode.PUSHNIL);
				}
				break;
				
			case 102:
				//#line 525 "lua.stx"
				{
					ntemp = 0;
				}
				break;
				
			case 104:
				//#line 528 "lua.stx"
				{
					lua_debug = yypvt[-0].vInt;
				}
				break;
			}
			goto yystack; // reset registers in driver code
		}
	}
}



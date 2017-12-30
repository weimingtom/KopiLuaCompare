#define YYOPTIM
//#define LEXDEBUG

using System;

namespace KopiLua
{
	using Word = System.UInt16; //unsigned short
	using YYTYPE = System.SByte;
	
	public partial class Lua
	{		
//FIXME:added
#if LEXDEBUG
		//search allprint yacc
		//https://github.com/juddy/stali/blob/master/bin/hbase/lex/allprint.c
		//https://github.com/codesrxxx/unix_image/blob/master/v7source/source/usr/src/cmd/lex/lib/allprint.c
		public static void allprint(int i)
		{
//			if (i == 40)
//			{
//				Console.WriteLine("======================");
//			}
			
//			if ((char)i == '\n')
//			{
//				printf("[%d \'\\n\']", i);
//			}
//			else
//			{
//				printf("[%d \'%c\']", i, (char)i);
//			}
			
//			if (i == '(')
//			{
//				Console.WriteLine("=====================");
//			}
			int c = i;
			switch(c){
				case '\n':
					fprintf(yyout,"\\n");
					break;
				case '\t':
					fprintf(yyout,"\\t");
					break;
				case '\b':
					fprintf(yyout,"\\b");
					break;
				case ' ':
					fprintf(yyout,"\\_");
					break;
				default:
					if(!(32 < c && c < 127))
						fprintf(yyout,"\\%-2x",c);
					else 
						putc(c,yyout);
					break;
				}
			return;
		}
		
		public static void sprint(CharPtr ch)
		{
//			printf("\"%s\"", ch.ToString());
			
			CharPtr s = new CharPtr(ch);
			while (s[0] != 0)
			{
				allprint((int)s[0]);s.inc();
			}
		}
#endif		
		
		//# define U(x) x
		//# define NLSTATE yyprevious=YYNEWLINE
		//# define BEGIN yybgin = yysvec + 1 +
		//# define INITIAL 0
		//# define YYLERR yysvec
		public static yysvf[] YYLERR() {return yysvec;}
		//# define YYSTATE (yyestate-yysvec-1)
		//# define YYOPTIM 1
		public const int YYLMAX = BUFSIZ;
		//# define output(c) putc(c,yyout)
		public static void output(char c) { putc(c, yyout); }
		//# define input() (((yytchar=yysptr>yysbuf?U(*--yysptr):getc(yyin))==10?(yylineno++,yytchar):yytchar)==EOF?0:yytchar)
		//# define unput(c) {yytchar= (c);if(yytchar=='\n')yylineno--;*yysptr++=yytchar;}
		//# define yymore() (yymorfg=1)
		//# define ECHO fprintf(yyout, "%s",yytext)
		//# define REJECT { nstr = yyreject(); goto yyfussy;}
		public static int yyleng;
		//extern sbyte yytext[];
		public static int yymorfg;
		//extern sbyte *yysptr, yysbuf[];
		public static int yytchar;
		public static FILE yyin = null;//new FILE(null); //FIXME:???
		public static FILE yyout = null;//new FILE(null);
		//extern int yylineno;

		
		public class yysvfRef 
		{
			public yysvf[] arr;
			private int index;
			
//			public yysvfRef()
//			{
//				
//			}
			public yysvfRef(int index, yysvf[] arr)
			{
//				if (arr == null)
//				{
//					Console.WriteLine("=============");
//				}
				this.arr = arr;
				this.index = index;
			}			
			public yysvfRef(yysvf[] arr, int index)
			{
//				if (arr == null)
//				{
//					Console.WriteLine("=============");
//				}
				this.arr = arr;
				this.index = index;
			}

			public yysvfRef(yysvfRef yref)
			{
//				if (yref.arr == null)
//				{
//					Console.WriteLine("=============");
//				}
				this.arr = yref.arr;
				this.index = yref.index;
			}
			
			public void inc()
			{
				this.index++;
			}
			public yysvfRef dec() //suffix--
			{
				this.index--;
				return new yysvfRef(this.index + 1, this.arr);
			}
			public bool isLargerThan(yysvf[] arr)
			{
				return this.arr == arr && this.index > 0;
			}
			public int minus(yysvf[] arr)
			{
				if (this.arr == arr)
				{
					return this.index - 0;
				}
				throw new Exception("minus");
			}
			
			public yysvf get()
			{
				return arr[index];
			}
			
			public yysvf get(int offset)
			{
				return arr[index + offset];
			}
			public yysvfRef getRef(int offset)
			{
				return new yysvfRef(arr, index + offset);
			}
			public void set(yysvfRef v)
			{
				if (arr[index + 0] == null)
				{
					arr[index + 0] = new yysvf(null, null, null);
					arr[index + 0].set(v.get());
				}
				else
				{
					arr[index + 0].set(v.get());
				}
			}
			public void set(int offset, yysvfRef v)
			{
				arr[index + offset].set(v.get());
			}
			
			public bool isEquals(yysvfRef yyref)
			{
				return this.arr == yyref.arr && this.index == yyref.index;
			}
			public bool isEquals(yysvf[] arr)
			{
				return this.arr == arr && this.index == 0;
			}
		}
		public class yysvfRefRef 
		{
			public yysvfRef[] arr;
			private int index;
			
//			public yysvfRef()
//			{
//				
//			}
			public yysvfRefRef(int index, yysvfRef[] arr)
			{
//				if (arr == null)
//				{
//					Console.WriteLine("=============");
//				}
				this.arr = arr;
				this.index = index;
			}			
			public yysvfRefRef(yysvfRef[] arr, int index)
			{
//				if (arr == null)
//				{
//					Console.WriteLine("=============");
//				}
				this.arr = arr;
				this.index = index;
			}

			public yysvfRefRef(yysvfRefRef yref)
			{
//				if (yref.arr == null)
//				{
//					Console.WriteLine("=============");
//				}
				this.arr = yref.arr;
				this.index = yref.index;
			}
			
			public void inc()
			{
				this.index++;
			}
			public yysvfRefRef dec() //suffix--
			{
				this.index--;
				return new yysvfRefRef(this.index + 1, this.arr);
			}
			public bool isLargerThan(yysvfRef[] arr)
			{
				return this.arr == arr && this.index > 0;
			}
//			public int minus(yysvfRef[] arr)
//			{
//				if (this.arr == arr)
//				{
//					return this.index - 0;
//				}
//				throw new Exception("minus");
//			}
			
			public yysvfRef get()
			{
				return arr[index];
			}
			
			public yysvfRef get(int offset)
			{
//				if (index + offset < 0)
//				{
//					Console.WriteLine("=======================");
//				}
				if (index + offset < 0)
				{
					return null;
				}
				return arr[index + offset];
			}
//			public yysvfRefRef getRef(int offset)
//			{
//				return new yysvfRefRef(arr, index + offset);
//			}
//			public void set(yysvfRefRef v)
//			{
//				if (arr[index + 0] == null)
//				{
//					arr[index + 0] = new yysvfRef();
//					arr[index + 0].set(v.get());
//				}
//				else
//				{
//					arr[index + 0].set(v.get());
//				}
//			}
//			public void set(int offset, yysvfRefRef v)
//			{
//				arr[index + offset].set(v.get());
//			}
			public void set(yysvfRef v)
			{
				arr[index] = new yysvfRef(v);
				//arr[index] = v;
			}
			
//			public bool isEquals(yysvfRefRef yyref)
//			{
//				return this.arr == yyref.arr && this.index == yyref.index;
//			}
//			public bool isEquals(yysvfRef[] arr)
//			{
//				return this.arr == arr && this.index == 0;
//			}
		}		
		
		public class yysvf
		{
			public yyworkRef yystoff;
			public yysvfRef yyother;
			public IntegerPtr yystops;
			
			public yysvf(yyworkRef yystoff, yysvfRef yyother, IntegerPtr yystops)
			{
				this.yystoff = yystoff;
				this.yyother = yyother;
				this.yystops = yystops;
			}
			
			public void set(yysvf yyref)
			{
				this.yystoff = new yyworkRef(yyref.yystoff);
				if (yyref.yyother != null)
				{
					this.yyother = new yysvfRef(yyref.yyother);
				}
				else
				{
					this.yyother = null;
				}
				if (yyref.yystops != null)
				{
					this.yystops = new IntegerPtr(yyref.yystops);
				}
				else
				{
					this.yystops = null;
				}
			}
		}		
		public static yysvfRef yyestate;
		//extern struct yysvf yysvec[], *yybgin;
		
		//#undef input
		//#undef unput

		private static Input input = null;
		private static Unput unput = null;
	
		public static void lua_setinput(Input fn)
		{
		 	input = fn;
		}
	
		public static void lua_setunput(Unput fn)
		{
		 	unput = fn;
		}
	
		public static CharPtr lua_lasttext ()
		{
		 	return yytext;
		}
	
		//# define YYNEWLINE 10
		private const int YYNEWLINE = 10;
		public static int yylex()
		{
			int nstr;
			//extern int yyprevious;
			while ((nstr = yylook()) >= 0)
			{
//yyfussy:
				switch (nstr)
				{
				case 0:
					if (yywrap() != 0)
					{
						return (0);
					}
					break;
		
				case 1:
					;
					break;
		
				case 2:
					{
						yylval.vInt = 1;
						return DEBUG;
					}
					//break;
		
				case 3:
					{
						yylval.vInt = 0;
						return DEBUG;
					}
					//break;
		
				case 4:
					lua_linenumber++;
					break;
		
				case 5:
					;
					break;
		
				case 6:
					return LOCAL;
					//break;
		
				case 7:
					return IF;
					//break;
		
				case 8:
					return THEN;
					//break;
		
				case 9:
					return ELSE;
					//break;
		
				case 10:
					return ELSEIF;
					//break;
		
				case 11:
					return WHILE;
					//break;
		
				case 12:
					return DO;
					//break;
		
				case 13:
					return REPEAT;
					//break;
		
				case 14:
					return UNTIL;
					//break;
		
				case 15:
					{
						yylval.vWord = (Word)(lua_nfile-1);
						return FUNCTION;
					}
					//break;
		
				case 16:
					return END;
					//break;
		
				case 17:
					return RETURN;
					//break;
		
				case 18:
					return LOCAL;
					//break;
		
				case 19:
					return NIL;
					//break;
		
				case 20:
					return AND;
					//break;
		
				case 21:
					return OR;
					//break;
		
				case 22:
					return NOT;
					//break;
		
				case 23:
					return NE;
					//break;
		
				case 24:
					return LE;
					//break;
		
				case 25:
					return GE;
					//break;
		
				case 26:
					return CONC;
					//break;
		
				case 27:
				case 28:
					{
						yylval.vWord = (Word)lua_findenclosedconstant(yytext);
						return STRING;
					}
					//break;
		
				case 29:
				case 30:
				case 31:
				case 32:
					{
						yylval.vFloat = (float)atof(yytext);
						return NUMBER;
					}
					//break;
		
				case 33:
					{
						yylval.vWord = (Word)lua_findsymbol(yytext);
						return NAME;
					}
					//break;
					
				case 34:
					return (int)yytext[0];
					//break;
				
				case -1:
					break;
	
				default:
					fprintf(yyout,"bad switch yylook %d",nstr);
					break;
				}
			}
			return (0);
		}
		/* end of yylex */
		
		
		
		public static int[] yyvstop = {
			0, 
			1, 0, 
			1, 0, 
			34, 0, 
			1, 34, 0, 
			4, 0, 
			34, 0, 
			34, 0, 
			34, 0, 
			34, 0, 
			29, 34, 0, 
			34, 0, 
			34, 0, 
			33, 34, 0, 
			33, 34, 0, 
			33, 34, 0, 
			33, 34, 0, 
			33, 34, 0, 
			33, 34, 0, 
			33, 34, 0, 
			33, 34, 0, 
			33, 34, 0, 
			33, 34, 0, 
			33, 34, 0, 
			33, 34, 0, 
			33, 34, 0, 
			34, 0, 
			34, 0, 
			1, 0, 
			27, 0, 
			28, 0, 
			5, 0, 
			26, 0, 
			30, 0,
			29, 0,
			29, 0,
			24, 0,
			25, 0,
			33, 0,
			33, 0,
			12, 33, 0,
			33, 0,
			33, 0,
			33, 0,
			7, 33, 0,
			33, 0,
			33, 0,
			33, 0,
			21, 33, 0,
			33, 0,
			33, 0,
			33, 0,
			33, 0,
			23, 0,
			29, 30, 0,
			31, 0,
			20, 33, 0,
			33, 0,
			16, 33, 0,
			33, 0,
			33, 0,
			19, 33, 0,
			22, 33, 0,
			33, 0,
			33, 0,
			33, 0,
			33, 0,
			33, 0,
			32, 0,
			9, 33, 0,
			33, 0,
			33, 0,
			33, 0,
			33, 0,
			8, 33, 0,
			33, 0,
			33, 0,
			31, 32, 0,
			33, 0,
			33, 0,
			6, 18, 33, 0,
			33, 0,
			33, 0,
			14, 33, 0,
			11, 33, 0,
			10, 33, 0,
			33, 0,
			13, 33, 0,
			17, 33, 0,
			2, 0,
			33, 0,
			15, 33, 0,
			3, 0, 0
		};
		public class yyworkRef
		{
			private yywork[] arr;
			private int index;
			
			public yyworkRef()
			{
				
			}
			
			public yyworkRef(yywork[] arr, int index)
			{
				this.arr = arr;
				this.index = index;
			}
			public yyworkRef(yyworkRef yyref)
			{
				this.arr = yyref.arr;
				this.index = yyref.index;
			}
			
			public bool isEquals(yywork[] arr)
			{
				return this.arr == arr && this.index == 0;
			}
			public bool isNotEquals(yywork[] arr)
			{
				return !isEquals(arr);
			}
			public bool isLargerThan(yywork[] arr)
			{
				return this.arr == arr && this.index > 0;
			}
			public bool isLessThan(yywork[] arr)
			{
				return this.arr == arr && this.index < 0;
			}
			public bool isLessEqualThan(yyworkRef yyref)
			{
				return this.arr == yyref.arr && this.index <= yyref.index;
			}


			public int minus(yywork[] arr)
			{
				if (this.arr == arr)
				{
					return this.index - 0;
				}
				throw new Exception("minus");
			}
			
			public yywork get()
			{
				if (this.index >= 0 && this.index < this.arr.Length)
				{
					return this.arr[this.index];
				}
				else
				{
					return new yywork(0, 0);
				}
			}
			public yywork get(int offset)
			{
				return this.arr[this.index + offset];
			}
			public yyworkRef getRef(int offset)
			{
				return new yyworkRef(this.arr, this.index + offset);
			}
		}
		public class yywork
		{
			public YYTYPE verify, advance;
			
			public yywork(YYTYPE verify, YYTYPE advance) 
			{
				this.verify = verify;
				this.advance = advance;
			}
		}
		public static yywork[] yycrank = {
			new yywork(0, 0), new yywork(0, 0), new yywork(1, 3), new yywork(0, 0),
			new yywork(0, 0), new yywork(0, 0), new yywork(0, 0), new yywork(0, 0),
			new yywork(0, 0), new yywork(0, 0), new yywork(1, 4), new yywork(1, 5),
			new yywork(6, 29), new yywork(4, 28), new yywork(0, 0), new yywork(0, 0),
			new yywork(0, 0), new yywork(0, 0), new yywork(7, 31), new yywork(0, 0),
			new yywork(6, 29), new yywork(6, 29), new yywork(0, 0), new yywork(0, 0),
			new yywork(0, 0), new yywork(0, 0), new yywork(7, 31), new yywork(7, 31),
			new yywork(0, 0), new yywork(0, 0), new yywork(0, 0), new yywork(0, 0),
			new yywork(0, 0), new yywork(0, 0), new yywork(0, 0), new yywork(1, 6),
			new yywork(4, 28), new yywork(0, 0), new yywork(0, 0), new yywork(0, 0),
			new yywork(1, 7), new yywork(0, 0), new yywork(0, 0), new yywork(0, 0),
			new yywork(1, 3), new yywork(6, 30), new yywork(1, 8), new yywork(1, 9),
			new yywork(0, 0), new yywork(1, 10), new yywork(6, 29), new yywork(7, 31),
			new yywork(8, 33), new yywork(0, 0), new yywork(6, 29), new yywork(0, 0),
			new yywork(7, 32), new yywork(0, 0), new yywork(0, 0), new yywork(6, 29),
			new yywork(7, 31), new yywork(1, 11), new yywork(0, 0), new yywork(1, 12),
			new yywork(2, 27), new yywork(7, 31), new yywork(1, 13), new yywork(11, 39),
			new yywork(12, 40), new yywork(1, 13), new yywork(26, 56), new yywork(0, 0),
			new yywork(0, 0), new yywork(2, 8), new yywork(2, 9), new yywork(0, 0),
			new yywork(6, 29), new yywork(0, 0), new yywork(0, 0), new yywork(6, 29),
			new yywork(0, 0), new yywork(0, 0), new yywork(7, 31), new yywork(0, 0),
			new yywork(0, 0), new yywork(7, 31), new yywork(0, 0), new yywork(0, 0),
			new yywork(2, 11), new yywork(0, 0), new yywork(2, 12), new yywork(0, 0),
			new yywork(0, 0), new yywork(0, 0), new yywork(0, 0), new yywork(0, 0),
			new yywork(0, 0), new yywork(0, 0), new yywork(1, 14), new yywork(0, 0),
			new yywork(0, 0), new yywork(1, 15), new yywork(1, 16), new yywork(1, 17),
			new yywork(0, 0), new yywork(22, 52), new yywork(1, 18), new yywork(18, 47),
			new yywork(23, 53), new yywork(1, 19), new yywork(42, 63), new yywork(1, 20),
			new yywork(1, 21), new yywork(25, 55), new yywork(14, 42), new yywork(1, 22),
			new yywork(15, 43), new yywork(1, 23), new yywork(1, 24), new yywork(16, 44),
			new yywork(1, 25), new yywork(16, 45), new yywork(17, 46), new yywork(19, 48),
			new yywork(21, 51), new yywork(2, 14), new yywork(20, 49), new yywork(1, 26),
			new yywork(2, 15), new yywork(2, 16), new yywork(2, 17), new yywork(24, 54),
			new yywork(20, 50), new yywork(2, 18), new yywork(44, 64), new yywork(45, 65),
			new yywork(2, 19), new yywork(46, 66), new yywork(2, 20), new yywork(2, 21),
			new yywork(27, 57), new yywork(48, 67), new yywork(2, 22), new yywork(49, 68),
			new yywork(2, 23), new yywork(2, 24), new yywork(50, 69), new yywork(2, 25),
			new yywork(52, 70), new yywork(53, 72), new yywork(27, 58), new yywork(54, 73),
			new yywork(52, 71), new yywork(9, 34), new yywork(2, 26), new yywork(9, 35),
			new yywork(9, 35), new yywork(9, 35), new yywork(9, 35), new yywork(9, 35),
			new yywork(9, 35), new yywork(9, 35), new yywork(9, 35), new yywork(9, 35),
			new yywork(9, 35), new yywork(10, 36), new yywork(55, 74), new yywork(10, 37),
			new yywork(10, 37), new yywork(10, 37), new yywork(10, 37), new yywork(10, 37),
			new yywork(10, 37), new yywork(10, 37), new yywork(10, 37), new yywork(10, 37),
			new yywork(10, 37), new yywork(57, 75), new yywork(58, 76), new yywork(64, 80),
			new yywork(66, 81), new yywork(67, 82), new yywork(70, 83), new yywork(71, 84),
			new yywork(72, 85), new yywork(73, 86), new yywork(74, 87), new yywork(10, 38),
			new yywork(10, 38), new yywork(38, 61), new yywork(10, 38), new yywork(38, 61),
			new yywork(75, 88), new yywork(76, 89), new yywork(38, 62), new yywork(38, 62),
			new yywork(38, 62), new yywork(38, 62), new yywork(38, 62), new yywork(38, 62),
			new yywork(38, 62), new yywork(38, 62), new yywork(38, 62), new yywork(38, 62),
			new yywork(80, 92), new yywork(81, 93), new yywork(13, 41), new yywork(13, 41),
			new yywork(13, 41), new yywork(13, 41), new yywork(13, 41), new yywork(13, 41),
			new yywork(13, 41), new yywork(13, 41), new yywork(13, 41), new yywork(13, 41),
			new yywork(82, 94), new yywork(83, 95), new yywork(84, 96), new yywork(10, 38),
			new yywork(10, 38), new yywork(86, 97), new yywork(10, 38), new yywork(13, 41),
			new yywork(13, 41), new yywork(13, 41), new yywork(13, 41), new yywork(13, 41),
			new yywork(13, 41), new yywork(13, 41), new yywork(13, 41), new yywork(13, 41),
			new yywork(13, 41), new yywork(13, 41), new yywork(13, 41), new yywork(13, 41),
			new yywork(13, 41), new yywork(13, 41), new yywork(13, 41), new yywork(13, 41),
			new yywork(13, 41), new yywork(13, 41), new yywork(13, 41), new yywork(13, 41),
			new yywork(13, 41), new yywork(13, 41), new yywork(13, 41), new yywork(13, 41),
			new yywork(13, 41), new yywork(87, 98), new yywork(88, 99), new yywork(60, 79),
			new yywork(60, 79), new yywork(13, 41), new yywork(60, 79), new yywork(13, 41),
			new yywork(13, 41), new yywork(13, 41), new yywork(13, 41), new yywork(13, 41),
			new yywork(13, 41), new yywork(13, 41), new yywork(13, 41), new yywork(13, 41),
			new yywork(13, 41), new yywork(13, 41), new yywork(13, 41), new yywork(13, 41),
			new yywork(13, 41), new yywork(13, 41), new yywork(13, 41), new yywork(13, 41),
			new yywork(13, 41), new yywork(13, 41), new yywork(13, 41), new yywork(13, 41),
			new yywork(13, 41), new yywork(13, 41), new yywork(13, 41), new yywork(13, 41),
			new yywork(13, 41), new yywork(33, 33), new yywork(89, 100), new yywork(60, 79),
			new yywork(60, 79), new yywork(92, 101), new yywork(60, 79), new yywork(93, 102),
			new yywork(95, 103), new yywork(33, 33), new yywork(33, 0), new yywork(96, 104),
			new yywork(99, 105), new yywork(100, 106), new yywork(102, 107), new yywork(106, 108),
			new yywork(107, 109), new yywork(35, 35), new yywork(35, 35), new yywork(35, 35),
			new yywork(35, 35), new yywork(35, 35), new yywork(35, 35), new yywork(35, 35),
			new yywork(35, 35), new yywork(35, 35), new yywork(35, 35), new yywork(108, 110),
			new yywork(0, 0), new yywork(0, 0), new yywork(0, 0), new yywork(0, 0),
			new yywork(0, 0), new yywork(0, 0), new yywork(33, 33), new yywork(0, 0),
			new yywork(0, 0), new yywork(35, 59), new yywork(35, 59), new yywork(33, 33),
			new yywork(35, 59), new yywork(0, 0), new yywork(0, 0), new yywork(33, 33),
			new yywork(0, 0), new yywork(0, 0), new yywork(0, 0), new yywork(0, 0),
			new yywork(33, 33), new yywork(0, 0), new yywork(0, 0), new yywork(0, 0),
			new yywork(0, 0), new yywork(36, 60), new yywork(36, 60), new yywork(36, 60),
			new yywork(36, 60), new yywork(36, 60), new yywork(36, 60), new yywork(36, 60),
			new yywork(36, 60), new yywork(36, 60), new yywork(36, 60), new yywork(0, 0),
			new yywork(0, 0), new yywork(33, 33), new yywork(0, 0), new yywork(0, 0),
			new yywork(33, 33), new yywork(35, 59), new yywork(35, 59), new yywork(0, 0),
			new yywork(35, 59), new yywork(36, 38), new yywork(36, 38), new yywork(59, 77),
			new yywork(36, 38), new yywork(59, 77), new yywork(0, 0), new yywork(0, 0),
			new yywork(59, 78), new yywork(59, 78), new yywork(59, 78), new yywork(59, 78),
			new yywork(59, 78), new yywork(59, 78), new yywork(59, 78), new yywork(59, 78),
			new yywork(59, 78), new yywork(59, 78), new yywork(61, 62), new yywork(61, 62),
			new yywork(61, 62), new yywork(61, 62), new yywork(61, 62), new yywork(61, 62),
			new yywork(61, 62), new yywork(61, 62), new yywork(61, 62), new yywork(61, 62),
			new yywork(0, 0), new yywork(0, 0), new yywork(0, 0), new yywork(0, 0),
			new yywork(0, 0), new yywork(36, 38), new yywork(36, 38), new yywork(0, 0),
			new yywork(36, 38), new yywork(77, 78), new yywork(77, 78), new yywork(77, 78),
			new yywork(77, 78), new yywork(77, 78), new yywork(77, 78), new yywork(77, 78),
			new yywork(77, 78), new yywork(77, 78), new yywork(77, 78), new yywork(79, 90),
			new yywork(0, 0), new yywork(79, 90), new yywork(0, 0), new yywork(0, 0),
			new yywork(79, 91), new yywork(79, 91), new yywork(79, 91), new yywork(79, 91),
			new yywork(79, 91), new yywork(79, 91), new yywork(79, 91), new yywork(79, 91),
			new yywork(79, 91), new yywork(79, 91), new yywork(90, 91), new yywork(90, 91),
			new yywork(90, 91), new yywork(90, 91), new yywork(90, 91), new yywork(90, 91),
			new yywork(90, 91), new yywork(90, 91), new yywork(90, 91), new yywork(90, 91),
			new yywork(0, 0)
		};
		
		private static yyworkRef yycrankOffset(int off) 
		{
			return new yyworkRef(yycrank, off);
		}
		private static yysvfRef yysvecOffset(int off)
		{
			return new yysvfRef(yysvec, off);
		}
		private static IntegerPtr yyvstopOffset(int off)
		{
			return new IntegerPtr(yyvstop, off);
		}
		private static yyworkRef yycrankZero() 
		{
			return null;//new yyworkRef();
		}
		private static yysvfRef yysvecZero()
		{
			return null;
		}
		private static IntegerPtr yyvstopZero()
		{
			return null;
		}
		public static yysvf[] yysvec = _yysvec_init();
		public static yysvf[] _yysvec_init()
		{
			yysvf[] result = new yysvf[] {
				new yysvf(yycrankZero(), yysvecZero(), yyvstopZero()),
				new yysvf(yycrankOffset(-1), yysvecZero(), yyvstopOffset(1)),
				new yysvf(yycrankOffset(-28), yysvecOffset(1), yyvstopOffset(3)),
				new yysvf(yycrankOffset(0), yysvecZero(), yyvstopOffset(5)),
				new yysvf(yycrankOffset(4), yysvecZero(), yyvstopOffset(7)),
				new yysvf(yycrankOffset(0), yysvecZero(), yyvstopOffset(10)),
				new yysvf(yycrankOffset(-11), yysvecZero(), yyvstopOffset(12)),
				new yysvf(yycrankOffset(-17), yysvecZero(), yyvstopOffset(14)),
				new yysvf(yycrankOffset(7), yysvecZero(), yyvstopOffset(16)),
				new yysvf(yycrankOffset(107), yysvecZero(), yyvstopOffset(18)),
				new yysvf(yycrankOffset(119), yysvecZero(), yyvstopOffset(20)),
				new yysvf(yycrankOffset(6), yysvecZero(), yyvstopOffset(23)),
				new yysvf(yycrankOffset(7), yysvecZero(), yyvstopOffset(25)),
				new yysvf(yycrankOffset(158), yysvecZero(), yyvstopOffset(27)),
				new yysvf(yycrankOffset(4), yysvecOffset(13), yyvstopOffset(30)),
				new yysvf(yycrankOffset(5), yysvecOffset(13), yyvstopOffset(33)),
				new yysvf(yycrankOffset(11), yysvecOffset(13), yyvstopOffset(36)),
				new yysvf(yycrankOffset(5), yysvecOffset(13), yyvstopOffset(39)),
				new yysvf(yycrankOffset(5), yysvecOffset(13), yyvstopOffset(42)),
				new yysvf(yycrankOffset(12), yysvecOffset(13), yyvstopOffset(45)),
				new yysvf(yycrankOffset(21), yysvecOffset(13), yyvstopOffset(48)),
				new yysvf(yycrankOffset(10), yysvecOffset(13), yyvstopOffset(51)),
				new yysvf(yycrankOffset(4), yysvecOffset(13), yyvstopOffset(54)),
				new yysvf(yycrankOffset(4), yysvecOffset(13), yyvstopOffset(57)),
				new yysvf(yycrankOffset(21), yysvecOffset(13), yyvstopOffset(60)),
				new yysvf(yycrankOffset(9), yysvecOffset(13), yyvstopOffset(63)),
				new yysvf(yycrankOffset(9), yysvecZero(), yyvstopOffset(66)),
				new yysvf(yycrankOffset(40), yysvecZero(), yyvstopOffset(68)),
				new yysvf(yycrankOffset(0), yysvecOffset(4), yyvstopOffset(70)),
				new yysvf(yycrankOffset(0), yysvecOffset(6), yyvstopZero()),
				new yysvf(yycrankOffset(0), yysvecZero(), yyvstopOffset(72)),
				new yysvf(yycrankOffset(0), yysvecOffset(7), yyvstopZero()),
				new yysvf(yycrankOffset(0), yysvecZero(), yyvstopOffset(74)),
				new yysvf(yycrankOffset(-280), yysvecZero(), yyvstopOffset(76)),
				new yysvf(yycrankOffset(0), yysvecZero(), yyvstopOffset(78)),
				new yysvf(yycrankOffset(249), yysvecZero(), yyvstopOffset(80)),
				new yysvf(yycrankOffset(285), yysvecZero(), yyvstopOffset(82)),
				new yysvf(yycrankOffset(0), yysvecOffset(10), yyvstopOffset(84)),
				new yysvf(yycrankOffset(146), yysvecZero(), yyvstopZero()),
				new yysvf(yycrankOffset(0), yysvecZero(), yyvstopOffset(86)),
				new yysvf(yycrankOffset(0), yysvecZero(), yyvstopOffset(88)),
				new yysvf(yycrankOffset(0), yysvecOffset(13), yyvstopOffset(90)),
				new yysvf(yycrankOffset(10), yysvecOffset(13), yyvstopOffset(92)),
				new yysvf(yycrankOffset(0), yysvecOffset(13), yyvstopOffset(94)),
				new yysvf(yycrankOffset(19), yysvecOffset(13), yyvstopOffset(97)),
				new yysvf(yycrankOffset(35), yysvecOffset(13), yyvstopOffset(99)),
				new yysvf(yycrankOffset(27), yysvecOffset(13), yyvstopOffset(101)),
				new yysvf(yycrankOffset(0), yysvecOffset(13), yyvstopOffset(103)),
				new yysvf(yycrankOffset(42), yysvecOffset(13), yyvstopOffset(106)),
				new yysvf(yycrankOffset(35), yysvecOffset(13), yyvstopOffset(108)),
				new yysvf(yycrankOffset(30), yysvecOffset(13), yyvstopOffset(110)),
				new yysvf(yycrankOffset(0), yysvecOffset(13), yyvstopOffset(112)),
				new yysvf(yycrankOffset(36), yysvecOffset(13), yyvstopOffset(115)),
				new yysvf(yycrankOffset(48), yysvecOffset(13), yyvstopOffset(117)),
				new yysvf(yycrankOffset(35), yysvecOffset(13), yyvstopOffset(119)),
				new yysvf(yycrankOffset(61), yysvecOffset(13), yyvstopOffset(121)),
				new yysvf(yycrankOffset(0), yysvecZero(), yyvstopOffset(123)),
				new yysvf(yycrankOffset(76), yysvecZero(), yyvstopZero()),
				new yysvf(yycrankOffset(67), yysvecZero(), yyvstopZero()),
				new yysvf(yycrankOffset(312), yysvecZero(), yyvstopZero()),
				new yysvf(yycrankOffset(183), yysvecOffset(36), yyvstopOffset(125)),
				new yysvf(yycrankOffset(322), yysvecZero(), yyvstopZero()),
				new yysvf(yycrankOffset(0), yysvecOffset(61), yyvstopOffset(128)),
				new yysvf(yycrankOffset(0), yysvecOffset(13), yyvstopOffset(130)),
				new yysvf(yycrankOffset(78), yysvecOffset(13), yyvstopOffset(133)),
				new yysvf(yycrankOffset(0), yysvecOffset(13), yyvstopOffset(135)),
				new yysvf(yycrankOffset(81), yysvecOffset(13), yyvstopOffset(138)),
				new yysvf(yycrankOffset(84), yysvecOffset(13), yyvstopOffset(140)),
				new yysvf(yycrankOffset(0), yysvecOffset(13), yyvstopOffset(142)),
				new yysvf(yycrankOffset(0), yysvecOffset(13), yyvstopOffset(145)),
				new yysvf(yycrankOffset(81), yysvecOffset(13), yyvstopOffset(148)),
				new yysvf(yycrankOffset(66), yysvecOffset(13), yyvstopOffset(150)),
				new yysvf(yycrankOffset(74), yysvecOffset(13), yyvstopOffset(152)),
				new yysvf(yycrankOffset(80), yysvecOffset(13), yyvstopOffset(154)),	
				new yysvf(yycrankOffset(78), yysvecOffset(13), yyvstopOffset(156)),
				new yysvf(yycrankOffset(94), yysvecZero(), yyvstopZero()),
				new yysvf(yycrankOffset(93), yysvecZero(), yyvstopZero()),
				new yysvf(yycrankOffset(341), yysvecZero(), yyvstopZero()),
				new yysvf(yycrankOffset(0), yysvecOffset(77), yyvstopOffset(158)),
				new yysvf(yycrankOffset(356), yysvecZero(), yyvstopZero()),
				new yysvf(yycrankOffset(99), yysvecOffset(13), yyvstopOffset(160)),
				new yysvf(yycrankOffset(89), yysvecOffset(13), yyvstopOffset(163)),
				new yysvf(yycrankOffset(108), yysvecOffset(13), yyvstopOffset(165)),
				new yysvf(yycrankOffset(120), yysvecOffset(13), yyvstopOffset(167)),
				new yysvf(yycrankOffset(104), yysvecOffset(13), yyvstopOffset(169)),
				new yysvf(yycrankOffset(0), yysvecOffset(13), yyvstopOffset(171)),
				new yysvf(yycrankOffset(113), yysvecOffset(13), yyvstopOffset(174)),
				new yysvf(yycrankOffset(148), yysvecOffset(13), yyvstopOffset(176)),
				new yysvf(yycrankOffset(133), yysvecZero(), yyvstopZero()),
				new yysvf(yycrankOffset(181), yysvecZero(), yyvstopZero()),
				new yysvf(yycrankOffset(366), yysvecZero(), yyvstopZero()),
				new yysvf(yycrankOffset(0), yysvecOffset(90), yyvstopOffset(178)),
				new yysvf(yycrankOffset(183), yysvecOffset(13), yyvstopOffset(181)),
				new yysvf(yycrankOffset(182), yysvecOffset(13), yyvstopOffset(183)),
				new yysvf(yycrankOffset(0), yysvecOffset(13), yyvstopOffset(185)),	
				new yysvf(yycrankOffset(172), yysvecOffset(13), yyvstopOffset(189)),
				new yysvf(yycrankOffset(181), yysvecOffset(13), yyvstopOffset(191)),
				new yysvf(yycrankOffset(0), yysvecOffset(13), yyvstopOffset(193)),
				new yysvf(yycrankOffset(0), yysvecOffset(13), yyvstopOffset(196)),
				new yysvf(yycrankOffset(189), yysvecZero(), yyvstopZero()),
				new yysvf(yycrankOffset(195), yysvecZero(), yyvstopZero()),	
				new yysvf(yycrankOffset(0), yysvecOffset(13), yyvstopOffset(199)),
				new yysvf(yycrankOffset(183), yysvecOffset(13), yyvstopOffset(202)),
				new yysvf(yycrankOffset(0), yysvecOffset(13), yyvstopOffset(204)),
				new yysvf(yycrankOffset(0), yysvecOffset(13), yyvstopOffset(207)),
				new yysvf(yycrankOffset(0), yysvecZero(), yyvstopOffset(210)),
				new yysvf(yycrankOffset(178), yysvecZero(), yyvstopZero()),	
				new yysvf(yycrankOffset(186), yysvecOffset(13), yyvstopOffset(212)),
				new yysvf(yycrankOffset(204), yysvecZero(), yyvstopZero()),
				new yysvf(yycrankOffset(0), yysvecOffset(13), yyvstopOffset(214)),
				new yysvf(yycrankOffset(0), yysvecZero(), yyvstopOffset(217)),
				new yysvf(yycrankZero(), yysvecZero(), yyvstopZero()),			
			};
			for (int i = 0; i < result.Length; ++i)
			{
				yysvf svf = result[i];
				if (svf.yyother != null)
				{
					svf.yyother.arr = result;
				}
			}
			return result;
		}
		
		public static yyworkRef yytop = yycrankOffset(423);
		public static yysvfRef yybgin = new yysvfRef(yysvec, 1);
		public static sbyte[] yymatch = { //FIXME:???sbyte, char??? //FIXME: 011 == 0x9
			0x0, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 
			0x1, 0x9, 0xA, 0x1, 0x1, 0x1, 0x1, 0x1, 
			0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 
			0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 
			0x9, 0x1, (sbyte)'"', 0x1, 0x1, 0x1, 0x1, 0x27, 
			0x1, 0x1, 0x1, (sbyte)'+', 0x1, (sbyte)'+', 0x1, 0x1, 
			(sbyte)'0', (sbyte)'0', (sbyte)'0', (sbyte)'0', (sbyte)'0', (sbyte)'0', (sbyte)'0', (sbyte)'0', 
			(sbyte)'0', (sbyte)'0', 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 
			0x1, (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'D', (sbyte)'D', (sbyte)'A', (sbyte)'D', 
			(sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', 
			(sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', 
			(sbyte)'A', (sbyte)'A', (sbyte)'A', 0x1, 0x1, 0x1, 0x1, (sbyte)'A', 
			0x1, (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'D', (sbyte)'D', (sbyte)'A', (sbyte)'D', 
			(sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', 
			(sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', (sbyte)'A', 
			(sbyte)'A', (sbyte)'A', (sbyte)'A', 0x1, 0x1, 0x1, 0x1, 0x1, 
			0
		};
		public static sbyte[] yyextra = { //FIXME:sbyte???char???
			0, 0, 0, 0, 0, 0, 0, 0, 
			0, 0, 0, 0, 0, 0, 0, 0, 
			0, 0, 0, 0, 0, 0, 0, 0, 
			0, 0, 0, 0, 0, 0, 0, 0, 
			0, 0, 0, 0, 0, 0, 0, 0, 
			0
		};
		//#if ! lint
		private static CharPtr ncform_sccsid = "@(#)ncform 1.6 88/02/08 SMI"; /* from S5R2 1.2 */
		//#endif
	
		public static int yylineno = 1;
		//#define YYU(x) x
		public static sbyte YYU(sbyte x) {return x;}
		//#define NLSTATE yyprevious=YYNEWLINE
		public static char[] yytext_buffer = new char[YYLMAX];
		public static CharPtr yytext = new CharPtr(yytext_buffer, true);
		public static yysvfRef[] yylstate = new yysvfRef[YYLMAX];
		public static yysvfRefRef yylsp, yyolsp; //FIXME:????
		public static CharPtr yysbuf = new CharPtr(new char[YYLMAX]);
		public static CharPtr yysptr = new CharPtr(yysbuf);
		public static IntegerPtr yyfnd;
		//extern struct yysvf *yyestate;
		public static int yyprevious = YYNEWLINE;
		public static int yylook()
		{
			yysvfRef yystate; yysvfRefRef lsp;
			yyworkRef yyt = new yyworkRef();
			yysvfRef yyz = null;
			int yych=0, yyfirst=0;
			yyworkRef yyr = new yyworkRef();
#if LEXDEBUG
			int debug;
#endif
			CharPtr yylastch;
			/* start off machines */
#if LEXDEBUG
			debug = 1;
			yyout = stdout;
#endif
#if !YYOPTIM
			yyout = stdout;
#endif
			yyfirst=1;
			if (yymorfg==0)
				yylastch = new CharPtr(yytext.chars, 0);
			else {
				yymorfg=0;
				yylastch = new CharPtr(yytext.chars, yytext.index + yyleng);
			}
			for(;;){
				lsp = new yysvfRefRef(yylstate, 0);
				yyestate = new yysvfRef(yybgin); yystate = new yysvfRef(yybgin);
				if (yyprevious==YYNEWLINE) yystate.inc();
				for (;;){
//					fprintf(stdout,"=========state %d\n",yystate.minus(yysvec)-1);
//					if (yystate.minus(yysvec)-1 == 2)
//					{
//						Console.WriteLine("=============");
//					}
#if LEXDEBUG
				if(debug!=0)fprintf(yyout,"state %d\n",yystate.minus(yysvec)-1);
#endif
					yyt = new yyworkRef(yystate.get().yystoff);
					if(yyt.isEquals(yycrank) && yyfirst==0){  /* may not be any transitions */
						yyz = yystate.get().yyother;
						if(yyz == null)break;
						if(yyz.get().yystoff.isEquals(yycrank))break;
					}
					int i_ = input(); 
					
					//yych = i_; //FIXME:
					int i_2 = i_ & 0xff;
					yych = (sbyte)(i_2 < 0x80 ? i_2 : i_2 - 0x100);
					
					yylastch[0] = (char)i_; yylastch.inc();
					yyfirst=0;
#if YYOPTIM
tryagain:
#endif
#if LEXDEBUG
					if(debug!=0){
						fprintf(yyout,"char ");
						allprint(yych);
						putchar('\n');
					}
# endif
					yyr = new yyworkRef(yyt);
					if ( yyt.isLargerThan(yycrank) ){
						yyt = yyr.getRef(yych);
						if (yyt.isLessEqualThan(yytop) && new yysvfRef(yyt.get().verify, yysvec).isEquals(yystate)){
							if(new yysvfRef(yyt.get().advance, yysvec).isEquals(YYLERR()))	/* error transitions */
							{yylastch.dec();unput(yylastch[0]);break;}
							yystate = new yysvfRef(yyt.get().advance, yysvec); lsp.set(yystate); lsp.inc();
							goto contin;
						}
					}
#if YYOPTIM
					else if(yyt.isLessThan(yycrank)) {		/* r < yycrank */
						yyr = new yyworkRef(yycrank, (-yyt.minus(yycrank))); yyt = new yyworkRef(yyr);
#if LEXDEBUG
						if(debug!=0)fprintf(yyout,"compressed state\n");
#endif
						yyt = yyt.getRef(yych);
						if(yyt.isLessEqualThan(yytop) && new yysvfRef(yyt.get().verify, yysvec).isEquals(yystate)){
							if(new yysvfRef(yyt.get().advance, yysvec).isEquals(YYLERR()))	/* error transitions */
							{yylastch.dec();unput(yylastch[0]);break;}
							yystate = new yysvfRef(yyt.get().advance, yysvec); lsp.set(yystate); lsp.inc();
							goto contin;
						}
						sbyte yymatch_v = (yych >= yymatch.Length) ? (sbyte)0 : yymatch[yych]; yyt = yyr.getRef(YYU(yymatch_v));
#if LEXDEBUG
						if(debug!=0){
							fprintf(yyout,"try fall back character ");
							allprint(YYU(yymatch[yymatch_v])); //allprint(YYU(yymatch[yych])); //FIXME:changed
							putchar('\n');
						}
#endif
						if(yyt.isLessEqualThan(yytop) && new yysvfRef(yyt.get().verify, yysvec).isEquals(yystate)){
							if(new yysvfRef(yyt.get().advance, yysvec).isEquals(YYLERR()))	/* error transition */
							{yylastch.dec();unput(yylastch[0]);break;}
							yystate = new yysvfRef(yyt.get().advance, yysvec); lsp.set(yystate); lsp.inc();
							goto contin;
						}
					}
					if ((yystate = yystate.get().yyother)!=null && (yyt= yystate.get().yystoff).isNotEquals(yycrank)){
#if LEXDEBUG
						if(debug!=0)fprintf(yyout,"fall back to state %d\n",yystate.minus(yysvec)-1);
#endif
						goto tryagain;
					}
#endif
					else
						{yylastch.dec();unput(yylastch[0]);break;}
contin:
#if LEXDEBUG
					if(debug!=0){
						fprintf(yyout,"state %d char ",yystate.minus(yysvec)-1);
						allprint(yych);
						putchar('\n');
					}
#endif
					;
				}
#if LEXDEBUG
				if(debug!=0){
					fprintf(yyout,"stopped at %d with ", lsp.get(-1) != null ? lsp.get(-1).minus(yysvec)-1 : -0x5a56b);
					allprint(yych);
					putchar('\n');
				}
#endif
				while (lsp.dec().isLargerThan(yylstate)){
					yylastch[0] = (char)0; yylastch.dec();
					if (lsp.get().get() != null /*FIXME:*/ && (yyfnd= new IntegerPtr(lsp.get().get().yystops))!=null && yyfnd[0] > 0){
						yyolsp = new yysvfRefRef(lsp);
						if(yyextra[yyfnd[0]]!=0){		/* must backup */
							while(yyback(lsp.get().get().yystops,-yyfnd[0]) != 1 && lsp.isLargerThan(yylstate)){
								lsp.dec();
								unput(yylastch[0]);yylastch.dec();
							}
						}
						yyprevious = YYU((sbyte)yylastch[0]);
						yylsp = new yysvfRefRef(lsp);
						yyleng = yylastch-yytext+1;
						yytext[yyleng] = (char)0;
#if LEXDEBUG
						if(debug!=0){
							fprintf(yyout,"\nmatch ");
							sprint(yytext);
							fprintf(yyout," action %d\n",yyfnd[0]);
						}
#endif
						int yyfnd_=yyfnd[0];yyfnd.inc();return(yyfnd_);
					}
					unput(yylastch[0]);
				}
				if (yytext[0] == 0  /* && feof(yyin) */)
				{
					yysptr=yysbuf;
					return(0);
				}
				int i__ = input(); yytext[0] = (char)i__; 
				int i__2 = i__ & 0xff;
				yyprevious = (sbyte)(i__2 < 0x80 ? i__2 : i__2 - 0x100); //yyprevious = unchecked((sbyte)i__2);
				if (yyprevious>0)
					output((char)yyprevious);
				yylastch=new CharPtr(yytext.chars, 0);
#if LEXDEBUG
				if(debug!=0)putchar('\n');
#endif
			}
		}
		public static int yyback(IntegerPtr p, int m)
		{
			if (p==0) return(0);
			while (p[0] != 0)
			{
				int p_ = p[0]; p.inc();
				if (p_ == m)
					return(1);
			}
			return(0);
		}
		/* the following are only used in the lex library */
		public static int yyinput()
		{
			return(input());
		}
		public static void yyoutput(int c)
		{
			output((char)c);
		}
		public static void yyunput(int c)
		{
			unput(c);
		}
	}
}




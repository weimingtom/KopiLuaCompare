/*
** $Id: llex.h,v 1.74 2013/04/26 13:07:53 roberto Exp $
** Lexical Analyzer
** See Copyright Notice in lua.h
*/

namespace KopiLua
{
	using lua_Number = System.Double;
	using lua_Integer = System.Int32;
	
	public partial class Lua
	{
		public const int FIRST_RESERVED	= 257;



		/*
		* WARNING: if you change the order of this enumeration,
		* grep "ORDER RESERVED"
		*/
		public enum RESERVED {
		  /* terminal symbols denoted by reserved words */
		  TK_AND = FIRST_RESERVED, TK_BREAK,
		  TK_DO, TK_ELSE, TK_ELSEIF, TK_END, TK_FALSE, TK_FOR, TK_FUNCTION,
		  TK_GOTO, TK_IF, TK_IN, TK_LOCAL, TK_NIL, TK_NOT, TK_OR, TK_REPEAT,
		  TK_RETURN, TK_THEN, TK_TRUE, TK_UNTIL, TK_WHILE,
		  /* other terminal symbols */
		  TK_IDIV, TK_CONCAT, TK_DOTS, TK_EQ, TK_GE, TK_LE, TK_NE,
		  TK_DBCOLON, TK_EOS,
		  TK_FLT, TK_INT, TK_NAME, TK_STRING
		};

		/* number of reserved words */
		public const int NUM_RESERVED = (int)RESERVED.TK_WHILE - FIRST_RESERVED + 1;

		public class SemInfo {
			public SemInfo() { } //FIXME:added
			public SemInfo(SemInfo copy) //FIXME:added
			{
				this.r = copy.r;
				this.i = copy.i;
				this.ts = copy.ts;
			}
			public lua_Number r;
			public lua_Integer i;
			public TString ts;
		} ;  /* semantics information */

		public class Token {
			public Token() { } //FIXME:added
			public Token(Token copy) //FIXME:added
			{
				this.token = copy.token;
				this.seminfo = new SemInfo(copy.seminfo);
			}
			public int token;
			public SemInfo seminfo = new SemInfo();
		};


		/* state of the lexer plus state of the parser when shared by all
		   functions */
		public class LexState {
			public int current;  /* current character (charint) */
			public int linenumber;  /* input line counter */
			public int lastline;  /* line of last token `consumed' */
			public Token t = new Token();  /* current token */
			public Token lookahead = new Token();  /* look ahead token */
			public FuncState fs;  /* current function (parser) */
			public lua_State L;
			public ZIO z;  /* input stream */
			public Mbuffer buff;  /* buffer for tokens */
            public Dyndata dyd;  /* dynamic structures used by the parser */
			public TString source;  /* current source name */
            public TString envn;  /* environment variable name */
			public char decpoint;  /* locale decimal point */
		};
	}
}

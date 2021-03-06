namespace KopiLua
{
	using lua_Number = System.Double;
	
	public partial class Lua
	{
		public const int FIRST_RESERVED	= 257;

		/* maximum length of a reserved word */
		public const int TOKEN_LEN	= 9; // "function"


		/*
		* WARNING: if you change the order of this enumeration,
		* grep "ORDER RESERVED"
		*/
		public enum RESERVED {
		  /* terminal symbols denoted by reserved words */
		  TK_AND = FIRST_RESERVED, TK_BREAK,
		  TK_DO, TK_ELSE, TK_ELSEIF, TK_END, TK_FALSE, TK_FOR, TK_FUNCTION,
		  TK_IF, TK_IN, TK_LOCAL, TK_NIL, TK_NOT, TK_OR, TK_REPEAT,
		  TK_RETURN, TK_THEN, TK_TRUE, TK_UNTIL, TK_WHILE,
		  /* other terminal symbols */
		  TK_CONCAT, TK_DOTS, TK_EQ, TK_GE, TK_LE, TK_NE, TK_EOS,
		  TK_NUMBER, TK_NAME, TK_STRING
		};

		/* number of reserved words */
		public const int NUM_RESERVED = (int)RESERVED.TK_WHILE - FIRST_RESERVED + 1;

		public class SemInfo {
			public SemInfo() { }
			public SemInfo(SemInfo copy)
			{
				this.r = copy.r;
				this.ts = copy.ts;
			}
			public lua_Number r;
			public TString ts;
		} ;  /* semantics information */

		public class Token {
			public Token() { }
			public Token(Token copy)
			{
				this.token = copy.token;
				this.seminfo = new SemInfo(copy.seminfo);
			}
			public int token;
			public SemInfo seminfo = new SemInfo();
		};


		public class LexState {
			public int current;  /* current character (charint) */
			public int linenumber;  /* input line counter */
			public int lastline;  /* line of last token `consumed' */
			public Token t = new Token();  /* current token */
			public Token lookahead = new Token();  /* look ahead token */
			public FuncState fs;  /* `FuncState' is private to the parser */
			public lua_State L;
			public ZIO z;  /* input stream */
			public Mbuffer buff;  /* buffer for tokens */
            public Varlist varl;  /* list of all active local variables */
			public TString source;  /* current source name */
            public TString envn;  /* environment variable name */
			public char decpoint;  /* locale decimal point */
		};
	}
}

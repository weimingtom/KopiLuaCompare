/*
** $Id: llex.h,v 1.79 2016/05/02 14:02:12 roberto Exp $
** Lexical Analyzer
** See Copyright Notice in lua.h
*/

namespace KopiLua
{
	using lua_Number = System.Double;
	using lua_Integer = System.Int64; //FIXME:
	
	public partial class Lua
	{


		public const int FIRST_RESERVED = 257;


		//#if !defined(LUA_ENV)
		public const string LUA_ENV	= "_ENV";
		//#endif


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
		  	TK_SHL, TK_SHR,
		  	TK_DBCOLON, TK_EOS,
		  	TK_FLT, TK_INT, TK_NAME, TK_STRING
		};

		/* number of reserved words */
		public const int NUM_RESERVED = ((int)(RESERVED.TK_WHILE - FIRST_RESERVED + 1));


		public class SemInfo {
		  	public lua_Number r;
		  	public lua_Integer i;
		  	public TString ts;
		};  /* semantics information */


		public class Token {
		  	public int token;
		  	public SemInfo seminfo;
		};


		/* state of the lexer plus state of the parser when shared by all
		   functions */
		public class LexState {
		  	public int current;  /* current character (charint) */
		  	public int linenumber;  /* input line counter */
		  	public int lastline;  /* line of last token 'consumed' */
		  	public Token t = new Token();  /* current token */
		  	public Token lookahead = new Token();  /* look ahead token */
		  	public FuncState fs;  /* current function (parser) */
		  	public lua_State L;
		  	public ZIO z;  /* input stream */
		  	public Mbuffer buff;  /* buffer for tokens */
		  	public Table h;  /* to avoid collection/reuse strings */
		  	public Dyndata dyd;  /* dynamic structures used by the parser */
		  	public TString source;  /* current source name */
		  	public TString envn;  /* environment variable name */
		};


//LUAI_FUNC void luaX_init (lua_State *L);
//LUAI_FUNC void luaX_setinput (lua_State *L, LexState *ls, ZIO *z,
//                              TString *source, int firstchar);
//LUAI_FUNC TString *luaX_newstring (LexState *ls, const char *str, size_t l);
//LUAI_FUNC void luaX_next (LexState *ls);
//LUAI_FUNC int luaX_lookahead (LexState *ls);
//LUAI_FUNC l_noret luaX_syntaxerror (LexState *ls, const char *s);
//LUAI_FUNC const char *luaX_token2str (LexState *ls, int token);


	}
}

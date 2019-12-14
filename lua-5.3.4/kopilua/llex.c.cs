/*
** $Id: llex.c,v 2.96 2016/05/02 14:02:12 roberto Exp $
** Lexical Analyzer
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using lua_Number = System.Double;
    using StkId = Lua.lua_TValue;
    	
	public partial class Lua
	{


		public static int next(LexState ls) { ls.current = zgetc(ls.z); return ls.current; }



		public static bool currIsNewline(LexState ls) { return (ls.current == '\n' || ls.current == '\r'); }


		/* ORDER RESERVED */
		private static readonly string[] luaX_tokens = {
			"and", "break", "do", "else", "elseif",
			"end", "false", "for", "function", "goto", "if",
			"in", "local", "nil", "not", "or", "repeat",
			"return", "then", "true", "until", "while",
		    "//", "..", "...", "==", ">=", "<=", "~=",
		    "<<", ">>", "::", "<eof>",
    		"<number>", "<integer>", "<name>", "<string>"
		};


		public static void save_and_next(LexState ls) {save(ls, ls.current); next(ls);}


		//static void/*l_noret*/ lexerror (LexState *ls, const char *msg, int token);


		private static void save (LexState ls, int c) {
		  Mbuffer b = ls.buff;
		  if (luaZ_bufflen(b) + 1 > luaZ_sizebuffer(b)) {
			uint newsize;
			if (luaZ_sizebuffer(b) >= MAX_SIZE/2)
			  lexerror(ls, "lexical element too long", 0);
			newsize = luaZ_sizebuffer(b) * 2;
			luaZ_resizebuffer(ls.L, b, (int)newsize);
		  }
		  b.buffer[luaZ_bufflen(b)] = (char)c; b.n++;//FXIME:???luaZ_bufflen(b)++
		}

		
		public static void luaX_init (lua_State L) {
		  int i;
		  TString e = luaS_newliteral(L, LUA_ENV);  /* create env name */
		  luaC_fix(L, obj2gco(e));  /* never collect this name */		  
		  for (i=0; i<NUM_RESERVED; i++) {
			TString ts = luaS_new(L, luaX_tokens[i]);
    		luaC_fix(L, obj2gco(ts));  /* reserved words are never collected */
			ts.extra = cast_byte(i+1);  /* reserved word */
		  }
		}


		public static CharPtr luaX_token2str (LexState ls, int token) {
		  if (token < FIRST_RESERVED) {  /* single-byte symbols? */
			lua_assert(token == cast_uchar(token));
    	    return luaO_pushfstring(ls.L, "'%c'", token);
		  }
		  else {
		    CharPtr s = luaX_tokens[token - FIRST_RESERVED];
		    if (token < (int)RESERVED.TK_EOS)  /* fixed format (symbols and reserved words)? */
		      return luaO_pushfstring(ls.L, "'%s'", s);
		    else  /* names, strings, and numerals */
		      return s;
		  }
		}


		public static CharPtr txtToken (LexState ls, int token) {
		  switch (token) {
			case (int)RESERVED.TK_NAME: case (int)RESERVED.TK_STRING:
    		case (int)RESERVED.TK_FLT: case (int)RESERVED.TK_INT:
			  save(ls, '\0');
			  return luaO_pushfstring(ls.L, "'%s'", luaZ_buffer(ls.buff));
			default:
			  return luaX_token2str(ls, token);
		  }
		}

        
		private static void/*l_noret*/ lexerror (LexState ls, CharPtr msg, int token) {
		  msg = luaG_addinfo(ls.L, msg, ls.source, ls.linenumber);
		  if (token != 0)
			luaO_pushfstring(ls.L, "%s near %s", msg, txtToken(ls, token));
		  luaD_throw(ls.L, LUA_ERRSYNTAX);
		}

        
		public static void/*l_noret*/ luaX_syntaxerror (LexState ls, CharPtr msg) {
		  lexerror(ls, msg, ls.t.token);
		}

        
		/*
		** creates a new string and anchors it in scanner's table so that
		** it will not be collected until the end of the compilation
		** (by that time it should be anchored somewhere)
		*/
		public static TString luaX_newstring(LexState ls, CharPtr str, uint l) {
		  lua_State L = ls.L;
          TValue o;  /* entry for 'str' */
		  TString ts = luaS_newlstr(L, str, l);  /* create new string */
		  setsvalue2s(L, StkId.inc(ref L.top), ts);  /* temporarily anchor it in stack */
		  o = luaH_set(L, ls.h, L.top - 1);
		  if (ttisnil(o)) {  /* not in use yet? (see 'addK') */
		    /* boolean value does not need GC barrier;
		       table has no metatable, so it does not need to invalidate cache */
		    setbvalue(o, 1);  /* t[string] = true */
		    luaC_checkGC(L);
		  }
		  else {  /* string already present */
		    ts = tsvalue(keyfromval(o));  /* re-use value previously stored */
		  } 
		  StkId.dec(ref L.top);  /* remove string from stack */
		  return ts;
		}


		/*
		** increment line number and skips newline sequence (any of
		** \n, \r, \n\r, or \r\n)
		*/
		private static void inclinenumber (LexState ls) {
		  int old = ls.current;
		  lua_assert(currIsNewline(ls));
		  next(ls);  /* skip '\n' or '\r' */
		  if (currIsNewline(ls) && ls.current != old)
			next(ls);  /* skip '\n\r' or '\r\n' */
		  if (++ls.linenumber >= MAX_INT)
			lexerror(ls, "chunk has too many lines", 0);
		}


		public static void luaX_setinput (lua_State L, LexState ls, ZIO z, TString source,
		                                  int firstchar) {
		  ls.t.token = 0;
		  ls.L = L;
          ls.current = firstchar;
		  ls.lookahead.token = (int)RESERVED.TK_EOS;  /* no look-ahead token */
		  ls.z = z;
		  ls.fs = null;
		  ls.linenumber = 1;
		  ls.lastline = 1;
		  ls.source = source;
		  ls.envn = luaS_newliteral(L, LUA_ENV);  /* get env name */
		  luaZ_resizebuffer(ls.L, ls.buff, LUA_MINBUFFER);  /* initialize buffer */
		}



		/*
		** =======================================================
		** LEXICAL ANALYZER
		** =======================================================
		*/



		private static int check_next1 (LexState ls, int c) {
		  if (ls.current == c) {
		    next(ls);
		    return 1;
		  }
		  else return 0;
		}


		/*
		** Check whether current char is in set 'set' (with two chars) and
		** saves it
		*/
		private static int check_next2 (LexState ls, CharPtr set) {
		  lua_assert(set[2] == '\0');
		  if (ls.current == set[0] || ls.current == set[1]) {
		    save_and_next(ls);
		    return 1;
		  }
		  else return 0;
		}


		/* LUA_NUMBER */
		/*
		** this function is quite liberal in what it accepts, as 'luaO_str2num'
		** will reject ill-formed numerals.
		*/		
		private static int read_numeral (LexState ls, SemInfo seminfo) {
		  TValue obj = new TValue();
		  CharPtr expo = new CharPtr("Ee");
		  int first = ls.current;		
		  lua_assert(lisdigit(ls.current));
		  save_and_next(ls);
		  if (first == '0' && check_next2(ls, "xX") != 0)  /* hexadecimal? */
		      expo = "Pp";
		  for (;;) {
		    if (check_next2(ls, expo) != 0)  /* exponent part? */
		      check_next2(ls, "-+");  /* optional exponent sign */
			if (lisxdigit(ls.current) != 0)
		      save_and_next(ls);
			else if (ls.current == '.')
		      save_and_next(ls);
		    else  break;
		  }
		  save(ls, '\0');
		  if (luaO_str2num(luaZ_buffer(ls.buff), obj) == 0)  /* format error? */
		    lexerror(ls, "malformed number", (int)RESERVED.TK_FLT);
		  if (ttisinteger(obj)) {
		    seminfo.i = ivalue(obj);
		    return (int)RESERVED.TK_INT;
		  }
		  else {
		    lua_assert(ttisfloat(obj));
		    seminfo.r = fltvalue(obj);
		    return (int)RESERVED.TK_FLT;
		  }
		}


		/*
		** skip a sequence '[=*[' or ']=*]'; if sequence is well formed, return
		** its number of '='s; otherwise, return a negative number (-1 iff there
		** are no '='s after initial bracket)
		*/
		private static int skip_sep (LexState ls) {
		  int count = 0;
		  int s = ls.current;
		  lua_assert(s == '[' || s == ']');
		  save_and_next(ls);
		  while (ls.current == '=') {
			save_and_next(ls);
			count++;
		  }
		  return (ls.current == s) ? count : (-count) - 1;
		}


		private static void read_long_string (LexState ls, SemInfo seminfo, int sep) {
		  int line = ls.linenumber;  /* initial line (for error message) */
		  save_and_next(ls);  /* skip 2nd '[' */
		  if (currIsNewline(ls))  /* string starts with a newline? */
			inclinenumber(ls);  /* skip it */
		  for (;;) {
			switch (ls.current) {
		      case EOZ: {  /* error */
		        CharPtr what = (seminfo!=null ? "string" : "comment");
		        CharPtr msg = luaO_pushfstring(ls.L,
		                     "unfinished long %s (starting at line %d)", what, line);
		        lexerror(ls, msg, (int)RESERVED.TK_EOS);
		        break;  /* to avoid warnings */
		      }
			  case ']': {
				if (skip_sep(ls) == sep) {
				  save_and_next(ls);  /* skip 2nd ']' */
				  goto endloop;
				}
			  	break;
              }
			  case '\n': case '\r': {
				save(ls, '\n');
				inclinenumber(ls);
				if (seminfo == null) luaZ_resetbuffer(ls.buff);  /* avoid wasting space */
				break;
              }
			  default: {
				if (seminfo != null) save_and_next(ls);
				else next(ls);
			  }
			  break;
			}
		  } endloop:
		  if (seminfo != null)
		  {
			  seminfo.ts = luaX_newstring(ls, luaZ_buffer(ls.buff) + (2 + sep), 
											(uint)(luaZ_bufflen(ls.buff) - 2*(2 + sep)));
		  }
		}

		private static void esccheck (LexState ls, int c, CharPtr msg) {
		  if (c==0) {
		    if (ls.current != EOZ)
		      save_and_next(ls);  /* add current to buffer for error message */
		    lexerror(ls, msg, (int)RESERVED.TK_STRING);
		  }
		}


		private static int gethexa (LexState ls) {
		  save_and_next(ls);
		  esccheck (ls, lisxdigit(ls.current), "hexadecimal digit expected");
		  return luaO_hexavalue(ls.current);
		}


		private static int readhexaesc (LexState ls) {
		  int r = gethexa(ls);
		  r = (r << 4) + gethexa(ls);
		  luaZ_buffremove(ls.buff, 2);  /* remove saved chars from buffer */
		  return r;
		}
		
		private static ulong readutf8esc (LexState ls) {
		  ulong r;
		  int i = 4;  /* chars to be removed: '\', 'u', '{', and first digit */
		  save_and_next(ls);  /* skip 'u' */
		  esccheck(ls, (ls.current == '{')?1:0, "missing '{'");
		  r = (uint)gethexa(ls);  /* must have at least one digit */
		  while (true) {save_and_next(ls); if (0==lisxdigit(ls.current)) {break;}
		    i++;
		    r = (uint)((r << 4) + (ulong)luaO_hexavalue(ls.current));
		    esccheck(ls, (r <= 0x10FFFF)?1:0, "UTF-8 value too large");
		  }
		  esccheck(ls, (ls.current == '}')?1:0, "missing '}'");
		  next(ls);  /* skip '}' */
		  luaZ_buffremove(ls.buff, i);  /* remove saved chars from buffer */
		  return r;
		}


		private static void utf8esc (LexState ls) {
		  CharPtr buff = new CharPtr(new char[UTF8BUFFSZ]);
		  int n = luaO_utf8esc(buff, readutf8esc(ls));
		  for (; n > 0; n--)  /* add 'buff' to string */
		    save(ls, buff[UTF8BUFFSZ - n]);
		}


		private static int readdecesc (LexState ls) {
		  int i;
		  int r = 0;  /* result accumulator */
		  for (i = 0; i < 3 && lisdigit(ls.current)!=0; i++) {  /* read up to 3 digits */
		    r = 10*r + ls.current - '0';
		    save_and_next(ls);
		  }
		  esccheck(ls, (r <= UCHAR_MAX)?1:0, "decimal escape too large");
		  luaZ_buffremove(ls.buff, i);  /* remove read digits from buffer */
		  return r;
		}


		static void read_string (LexState ls, int del, SemInfo seminfo) {
		  save_and_next(ls);  /* keep delimiter (for error messages) */
		  while (ls.current != del) {
			switch (ls.current) {
			  case EOZ:
				lexerror(ls, "unfinished string", (int)RESERVED.TK_EOS);
				break;  /* to avoid warnings */
			  case '\n':
			  case '\r':
				lexerror(ls, "unfinished string", (int)RESERVED.TK_STRING);
				break;  /* to avoid warnings */
			  case '\\': {  /* escape sequences */
				int c;  /* final character to be saved */
				save_and_next(ls);  /* keep '\\' for error messages */
        		switch (ls.current) {
				  case 'a': c = '\a'; goto read_save;
				  case 'b': c = '\b'; goto read_save;
				  case 'f': c = '\f'; goto read_save;
				  case 'n': c = '\n'; goto read_save;
				  case 'r': c = '\r'; goto read_save;
				  case 't': c = '\t'; goto read_save;
				  case 'v': c = '\v'; goto read_save;
                  case 'x': c = readhexaesc(ls); goto read_save;
				  case 'u': utf8esc(ls);  goto no_save;
				  case '\n':  case '\r': 
				    inclinenumber(ls); c = '\n'; goto only_save;
                  case '\\': case '\"': case '\'': 
				    c = ls.current; goto read_save;
				  case EOZ: goto no_save;  /* will raise an error next loop */
		          case 'z': {  /* zap following span of spaces */
		            luaZ_buffremove(ls.buff, 1);  /* remove '\\' */
            		next(ls);  /* skip the 'z' */
		            while (lisspace(ls.current)!=0) {
		              if (currIsNewline(ls)) inclinenumber(ls);
		              else next(ls);
		            }
		            goto no_save;
		          }
				  default: {
		            esccheck(ls, lisdigit(ls.current), "invalid escape sequence");
		            c = readdecesc(ls);  /* digital escape '\ddd' */
					goto only_save;
				  }
				}
		       read_save: 
			     next(ls);
				 /* go through */
		       only_save:
		         luaZ_buffremove(ls.buff, 1);  /* remove '\\' */
		         save(ls, c);
		         /* go through */
		       no_save: break;
			  }
			  default:
				save_and_next(ls);
				break; //FIXME:added
			}
		  }
		  save_and_next(ls);  /* skip delimiter */
		  seminfo.ts = luaX_newstring(ls, luaZ_buffer(ls.buff) + 1,
		                                  luaZ_bufflen(ls.buff) - 2);
		}


		private static int llex (LexState ls, SemInfo seminfo) {
		  luaZ_resetbuffer(ls.buff);
		  for (;;) {
		  	switch (ls.current) {
			  case '\n': case '\r': {  /* line breaks */
				inclinenumber(ls);
				break;
			  }
			  case ' ': case '\f': case '\t': case '\v': {  /* spaces */
		        next(ls);
		        break;
		      }
			  case '-': {  /* '-' or '--' (comment) */
				next(ls);
				if (ls.current != '-') return '-';
				/* else is a comment */
				next(ls);
				if (ls.current == '[') {  /* long comment? */
				  int sep = skip_sep(ls);
				  luaZ_resetbuffer(ls.buff);  /* 'skip_sep' may dirty the buffer */
				  if (sep >= 0) {
					read_long_string(ls, null, sep);  /* skip long comment */
					luaZ_resetbuffer(ls.buff);  /* previous call may dirty the buff. */
					break;
				  }
				}
				/* else short comment */
				while (!currIsNewline(ls) && ls.current != EOZ)
				  next(ls);  /* skip until end of line (or end of file) */
				break;
			  }
			  case '[': {  /* long string or simply '[' */
				int sep = skip_sep(ls);
				if (sep >= 0) {
				  read_long_string(ls, seminfo, sep);
				  return (int)RESERVED.TK_STRING;
				}
		        else if (sep != -1)  /* '[=...' missing second bracket */
		          lexerror(ls, "invalid long string delimiter", (int)RESERVED.TK_STRING);
		        return '[';
			  }
			  break;
			  case '=': {
		        next(ls);
		        if (0!=check_next1(ls, '=')) return (int)RESERVED.TK_EQ;
		        else return '=';
		      }
		      case '<': {
		        next(ls);
		        if (0!=check_next1(ls, '=')) return (int)RESERVED.TK_LE;
		        else if (0!=check_next1(ls, '<')) return (int)RESERVED.TK_SHL;
		        else return '<';
		      }
		      case '>': {
		        next(ls);
		        if (0!=check_next1(ls, '=')) return (int)RESERVED.TK_GE;
		        else if (0!=check_next1(ls, '>')) return (int)RESERVED.TK_SHR;
		        else return '>';
		      }
		      case '/': {
		        next(ls);
		        if (0!=check_next1(ls, '/')) return (int)RESERVED.TK_IDIV;
		        else return '/';
		      }
		      case '~': {
		        next(ls);
		        if (0!=check_next1(ls, '=')) return (int)RESERVED.TK_NE;
		        else return '~';
		      }
		      case ':': {
		        next(ls);
		        if (0!=check_next1(ls, ':')) return (int)RESERVED.TK_DBCOLON;
		        else return ':';
		      }
		      case '"': case '\'': {  /* short literal strings */
		        read_string(ls, ls.current, seminfo);
		        return (int)RESERVED.TK_STRING;
		      }
		      case '.': {  /* '.', '..', '...', or number */
		        save_and_next(ls);
		        if (0!=check_next1(ls, '.')) {
		          if (0!=check_next1(ls, '.'))
		            return (int)RESERVED.TK_DOTS;   /* '...' */
		          else return (int)RESERVED.TK_CONCAT;   /* '..' */
		        }
		        else if (0==lisdigit(ls.current)) return '.';
		        else return read_numeral(ls, seminfo);
		      }
		      case '0': case '1': case '2': case '3': case '4':
		      case '5': case '6': case '7': case '8': case '9': {
		        return read_numeral(ls, seminfo);
		      }
			  case EOZ: {
				  return (int)RESERVED.TK_EOS;
			  }
			  default: {
				if (lislalpha(ls.current) != 0) {  /* identifier or reserved word? */
				  TString ts;
				  do {
					save_and_next(ls);
				  } while (lislalnum(ls.current) != 0);
				  ts = luaX_newstring(ls, luaZ_buffer(ls.buff),
										  luaZ_bufflen(ls.buff));
                  seminfo.ts = ts;
				  if (isreserved(ts))  /* reserved word? */
					return ts.extra - 1 + FIRST_RESERVED;
				  else {
					return (int)RESERVED.TK_NAME;
				  }
				}
				else {  /* single-char tokens (+ - / ...) */
				  int c = ls.current;
				  next(ls);
				  return c;
				}
			  }
			}
		  }
		}


		public static void luaX_next (LexState ls) {
		  ls.lastline = ls.linenumber;
		  if (ls.lookahead.token != (int)RESERVED.TK_EOS) {  /* is there a look-ahead token? */
			ls.t = new Token(ls.lookahead);  /* use this one */ //FIXME:???new Token()
			ls.lookahead.token = (int)RESERVED.TK_EOS;  /* and discharge it */
		  }
		  else
			ls.t.token = llex(ls, ls.t.seminfo);  /* read next token */
		}


		public static int luaX_lookahead (LexState ls) {
		  lua_assert(ls.lookahead.token == (int)RESERVED.TK_EOS);
		  ls.lookahead.token = llex(ls, ls.lookahead.seminfo);
          return ls.lookahead.token;
		}

	}
}

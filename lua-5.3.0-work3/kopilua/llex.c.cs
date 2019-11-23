/*
** $Id: llex.c,v 2.74 2014/02/14 15:23:51 roberto Exp $
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
    		"<number>", "<number>", "<name>", "<string>"
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
		  TString e = luaS_new(L, LUA_ENV);  /* create env name */
		  luaC_fix(L, obj2gco(e));  /* never collect this name */		  
		  for (i=0; i<NUM_RESERVED; i++) {
			TString ts = luaS_new(L, luaX_tokens[i]);
    		luaC_fix(L, obj2gco(ts));  /* reserved words are never collected */
			ts.tsv.extra = cast_byte(i+1);  /* reserved word */
		  }
		}


		public static CharPtr luaX_token2str (LexState ls, int token) {
		  if (token < FIRST_RESERVED) {  /* single-byte symbols? */
			lua_assert(token == (byte)token);
			return (lisprint((byte)token) != 0) ? luaO_pushfstring(ls.L, LUA_QL("%c"), token) :
									  luaO_pushfstring(ls.L, "char(%d)", token);
		  }
		  else {
		    CharPtr s = luaX_tokens[token - FIRST_RESERVED];
		    if (token < (int)RESERVED.TK_EOS)  /* fixed format (symbols and reserved words)? */
		      return luaO_pushfstring(ls.L, LUA_QS, s);
		    else  /* names, strings, and numerals */
		      return s;
		  }
		}


		public static CharPtr txtToken (LexState ls, int token) {
		  switch (token) {
			case (int)RESERVED.TK_NAME: case (int)RESERVED.TK_STRING:
    		case (int)RESERVED.TK_FLT: case (int)RESERVED.TK_INT:
			  save(ls, '\0');
			  return luaO_pushfstring(ls.L, LUA_QS, luaZ_buffer(ls.buff));
			default:
			  return luaX_token2str(ls, token);
		  }
		}

        
		private static void/*l_noret*/ lexerror (LexState ls, CharPtr msg, int token) {
		  CharPtr buff = new char[LUA_IDSIZE];
		  luaO_chunkid(buff, getstr(ls.source), LUA_IDSIZE);
		  msg = luaO_pushfstring(ls.L, "%s:%d: %s", buff, ls.linenumber, msg);
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
          TValue o;  /* entry for `str' */
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
		    ts = rawtsvalue(keyfromval(o));  /* re-use value previously stored */
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
		  next(ls);  /* skip `\n' or `\r' */
		  if (currIsNewline(ls) && ls.current != old)
			next(ls);  /* skip `\n\r' or `\r\n' */
		  if (++ls.linenumber >= MAX_INT)
			luaX_syntaxerror(ls, "chunk has too many lines");
		}


		public static void luaX_setinput (lua_State L, LexState ls, ZIO z, TString source,
		                                  int firstchar) {
		  ls.decpoint = '.';
		  ls.L = L;
          ls.current = firstchar;
		  ls.lookahead.token = (int)RESERVED.TK_EOS;  /* no look-ahead token */
		  ls.z = z;
		  ls.fs = null;
		  ls.linenumber = 1;
		  ls.lastline = 1;
		  ls.source = source;
		  ls.envn = luaS_new(L, LUA_ENV);  /* get env name */
		  luaZ_resizebuffer(ls.L, ls.buff, LUA_MINBUFFER);  /* initialize buffer */
		}



		/*
		** =======================================================
		** LEXICAL ANALYZER
		** =======================================================
		*/



		private static int check_next (LexState ls, CharPtr set) {
		  if ((char)ls.current == '\0' || strchr(set, (char)ls.current) == null)
			return 0;
		  save_and_next(ls);
		  return 1;
		}


		/*
		** change all characters 'from' in buffer to 'to'
		*/
		private static void buffreplace (LexState ls, char from, char to) {
		  uint n = luaZ_bufflen(ls.buff);
		  CharPtr p = luaZ_buffer(ls.buff);
		  while ((n--) != 0)
			  if (p[n] == from) p[n] = to;
		}


		//#if !defined(getlocaledecpoint)
		private static char getlocaledecpoint() { return localeconv().decimal_point[0];}
		//#endif


		private static int buff2d(Mbuffer b, out lua_Number e) { return luaO_str2d(luaZ_buffer(b), luaZ_bufflen(b) - 1, out e);}

		/*
		** in case of format error, try to change decimal point separator to
		** the one defined in the current locale and check again
		*/
		private static void trydecpoint (LexState ls, SemInfo seminfo) {
			// todo: add proper support for localeconv - mjf
			//lconv cv = localeconv(); //FIXME:???removed???
			char old = ls.decpoint;
			ls.decpoint = getlocaledecpoint();
			buffreplace(ls, old, ls.decpoint);  /* try new decimal separator */
			if (buff2d(ls.buff, out seminfo.r) == 0) {
				/* format error with correct decimal point: no more options */
				buffreplace(ls, ls.decpoint, '.');  /* undo change (for error message) */
				lexerror(ls, "malformed number", (int)RESERVED.TK_FLT);
			}
		}


		/* LUA_NUMBER */
		/*
		** this function is quite liberal in what it accepts, as 'luaO_str2d'
		** will reject ill-formed numerals. 'isf' means the numeral is not
		** an integer (it has a dot or an exponent).
		*/		
		private static int read_numeral (LexState ls, SemInfo seminfo, int isf) {
		  CharPtr expo = new CharPtr("Ee");
		  int first = ls.current;		
		  lua_assert(lisdigit(ls.current));
		  save_and_next(ls);
		  if (first == '0' && check_next(ls, "Xx") != 0)  /* hexadecimal? */
		      expo = "Pp";
		  for (;;) {
		    if (check_next(ls, expo) != 0)  { /* exponent part? */
		      check_next(ls, "+-");  /* optional exponent sign */
		      isf = 1;
    		}
			if (lisxdigit(ls.current) != 0)
		      save_and_next(ls);
			else if (ls.current == '.') {
		      save_and_next(ls);
		      isf = 1;			
			}
		    else  break;
		  }
		  save(ls, '\0');
		  if (0==isf) {
		    if (0==luaO_str2int(luaZ_buffer(ls.buff), luaZ_bufflen(ls.buff) - 1,
		                      ref seminfo.i))
		      lexerror(ls, "malformed number", (int)RESERVED.TK_INT);
		    return (int)RESERVED.TK_INT;
		  }
		  else {		  
			buffreplace(ls, '.', ls.decpoint);  /* follow locale for decimal point */
			if (buff2d(ls.buff, out seminfo.r) == 0)  /* format error? */
			  trydecpoint(ls, seminfo); /* try to update decimal point separator */
			return (int)RESERVED.TK_FLT;
		  }
		}


		/*
		** skip a sequence '[=*[' or ']=*]' and return its number of '='s or
		** -1 if sequence is malformed
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
		  save_and_next(ls);  /* skip 2nd `[' */
		  if (currIsNewline(ls))  /* string starts with a newline? */
			inclinenumber(ls);  /* skip it */
		  for (;;) {
			switch (ls.current) {
			  case EOZ:
				lexerror(ls, (seminfo != null) ? "unfinished long string" :
										   "unfinished long comment", (int)RESERVED.TK_EOS);
				break;  /* to avoid warnings */
			  case ']': {
				if (skip_sep(ls) == sep) {
				  save_and_next(ls);  /* skip 2nd `]' */
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
		
		private static uint readutf8esc (LexState ls) {
		  uint r;
		  int i = 4;  /* chars to be removed: '\', 'u', '{', and first digit */
		  save_and_next(ls);  /* skip 'u' */
		  esccheck(ls, (ls.current == '{')?1:0, "missing '{'");
		  r = (uint)gethexa(ls);  /* must have at least one digit */
		  while (true) {save_and_next(ls); if (0==lisxdigit(ls.current)) {break;}
		    i++;
		    r = (uint)((r << 4) + luaO_hexavalue(ls.current));
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
		            c = readdecesc(ls);  /* digital escape \ddd */
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
				  luaZ_resetbuffer(ls.buff);  /* `skip_sep' may dirty the buffer */
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
				else if (sep == -1) return '[';
				else lexerror(ls, "invalid long string delimiter", (int)RESERVED.TK_STRING);
			  }
			  break;
			  case '=': {
				next(ls);
				if (ls.current != '=') return '=';
				else { next(ls); return (int)RESERVED.TK_EQ; }
			  }
			  case '<': {
				next(ls);
		        if (ls.current == '=') { next(ls); return (int)RESERVED.TK_LE; }
		        if (ls.current == '<') { next(ls); return (int)RESERVED.TK_SHL; }
		        return '<';
			  }
			  case '>': {
				next(ls);
		        if (ls.current == '=') { next(ls); return (int)RESERVED.TK_GE; }
		        if (ls.current == '>') { next(ls); return (int)RESERVED.TK_SHR; }
		        return '>';
			  }
			  case '/': {
		        next(ls);
		        if (ls.current != '/') return '/';
		        else { next(ls); return (int)RESERVED.TK_IDIV; }
		      }
			  case '~': {
				next(ls);
				if (ls.current != '=') return '~';
				else { next(ls); return (int)RESERVED.TK_NE; }
			  }
			  case ':': {
		        next(ls);
		        if (ls.current != ':') return ':';
		        else { next(ls); return (int)RESERVED.TK_DBCOLON; }
		      }
			  case '"': case '\'': {  /* short literal strings */
				read_string(ls, ls.current, seminfo);
				return (int)RESERVED.TK_STRING;
			  }
			  case '.': {  /* '.', '..', '...', or number */
				save_and_next(ls);
				if (check_next(ls, ".") != 0) {
				  if (check_next(ls, ".") != 0)
					  return (int)RESERVED.TK_DOTS;   /* '...' */
				  else return (int)RESERVED.TK_CONCAT;   /* '..' */
				}
				else if (lisdigit(ls.current)==0) return '.';
                else return read_numeral(ls, seminfo, 1);
			  }
		      case '0': case '1': case '2': case '3': case '4':
		      case '5': case '6': case '7': case '8': case '9': {
		        return read_numeral(ls, seminfo, 0);
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
					return ts.tsv.extra - 1 + FIRST_RESERVED;
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

/*
** $Id: llex.c,v 2.32 2009/03/11 13:27:32 roberto Exp roberto $
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


		public static void next(LexState ls) { ls.current = zgetc(ls.z); }



		public static bool currIsNewline(LexState ls) { return (ls.current == '\n' || ls.current == '\r'); }


		/* ORDER RESERVED */
		private static readonly string[] luaX_tokens = {
			"and", "break", "do", "else", "elseif",
			"end", "false", "for", "function", "if",
			"in", "local", "nil", "not", "or", "repeat",
			"return", "then", "true", "until", "while",
			"..", "...", "==", ">=", "<=", "~=", "<eof>",
			"<number>", "<name>", "<string>"
		};


		public static void save_and_next(LexState ls) {save(ls, ls.current); next(ls);}


		//static void lexerror (LexState *ls, const char *msg, int token);


		private static void save (LexState ls, int c) {
		  Mbuffer b = ls.buff;
		  if (luaZ_bufflen(b) + 1 > luaZ_sizebuffer(b)) {
			uint newsize;
			if (luaZ_sizebuffer(b) >= MAX_SIZET/2)
			  lexerror(ls, "lexical element too long", 0);
			newsize = luaZ_sizebuffer(b) * 2;
			luaZ_resizebuffer(ls.L, b, (int)newsize);
		  }
		  b.buffer[luaZ_bufflen(b)++] = (char)c;
		}

		
		public static void luaX_init (lua_State L) {
		  int i;
		  for (i=0; i<NUM_RESERVED; i++) {
			TString ts = luaS_new(L, luaX_tokens[i]);
			luaS_fix(ts);  /* reserved words are never collected */
			lua_assert(luaX_tokens[i].Length+1 <= TOKEN_LEN);
			ts.tsv.reserved = cast_byte(i+1);  /* reserved word */
		  }
		}



		public static CharPtr luaX_token2str (LexState ls, int token) {
		  if (token < FIRST_RESERVED) {
			lua_assert(token == (byte)token);
			return (lisprint((byte)token)) ? luaO_pushfstring(ls.L, LUA_QL("%c"), token) :
									  luaO_pushfstring(ls.L, "char(%d)", token);
		  }
		  else {
		    CharPtr s = luaX_tokens[token - FIRST_RESERVED];
		    if (token < (int)RESERVED.TK_EOS)
		      return luaO_pushfstring(ls.L, LUA_QS, s);
		    else
		      return s;
		  }
		}


		public static CharPtr txtToken (LexState ls, int token) {
		  switch (token) {
			case (int)RESERVED.TK_NAME:
			case (int)RESERVED.TK_STRING:
			case (int)RESERVED.TK_NUMBER:
			  save(ls, '\0');
			  return luaO_pushfstring(ls.L, LUA_QS, luaZ_buffer(ls.buff));
			default:
			  return luaX_token2str(ls, token);
		  }
		}

        
		private static void lexerror (LexState ls, CharPtr msg, int token) {
		  CharPtr buff = new char[LUA_IDSIZE];
		  luaO_chunkid(buff, getstr(ls.source), LUA_IDSIZE);
		  msg = luaO_pushfstring(ls.L, "%s:%d: %s", buff, ls.linenumber, msg);
		  if (token != 0)
			luaO_pushfstring(ls.L, "%s near %s", msg, txtToken(ls, token));
		  luaD_throw(ls.L, LUA_ERRSYNTAX);
		}

        
		public static void luaX_syntaxerror (LexState ls, CharPtr msg) {
		  lexerror(ls, msg, ls.t.token);
		}

        
		public static TString luaX_newstring(LexState ls, CharPtr str, uint l)
		{
		  lua_State L = ls.L;
          TValue o;  /* entry for `str' */
		  TString ts = luaS_newlstr(L, str, l);
		  setsvalue2s(L, StkId.inc(ref L.top), ts);  /* anchor string */
		  o = luaH_setstr(L, ls.fs.h, ts);
		  if (ttisnil(o))
		    setbvalue(o, 1);  /* make sure `str' will not be collected */
		  StkId.dec(ref L.top);
		  return ts;
		}


		private static void inclinenumber (LexState ls) {
		  int old = ls.current;
		  lua_assert(currIsNewline(ls));
		  next(ls);  /* skip `\n' or `\r' */
		  if (currIsNewline(ls) && ls.current != old)
			next(ls);  /* skip `\n\r' or `\r\n' */
		  if (++ls.linenumber >= MAX_INT)
			luaX_syntaxerror(ls, "chunk has too many lines");
		}


		public static void luaX_setinput (lua_State L, LexState ls, ZIO z, TString source) {
		  ls.decpoint = '.';
		  ls.L = L;
		  ls.lookahead.token = (int)RESERVED.TK_EOS;  /* no look-ahead token */
		  ls.z = z;
		  ls.fs = null;
		  ls.linenumber = 1;
		  ls.lastline = 1;
		  ls.source = source;
		  luaZ_resizebuffer(ls.L, ls.buff, LUA_MINBUFFER);  /* initialize buffer */
		  next(ls);  /* read first char */
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


		private static void buffreplace (LexState ls, char from, char to) {
		  uint n = luaZ_bufflen(ls.buff);
		  CharPtr p = luaZ_buffer(ls.buff);
		  while ((n--) != 0)
			  if (p[n] == from) p[n] = to;
		}


        //FIXME:
		//#if !defined(getlocaledecpoint)
		//#define getlocaledecpoint()	(localeconv()->decimal_point[0])
		//#endif
		private static void trydecpoint (LexState ls, SemInfo seminfo) {
		  /* format error: try to update decimal point separator */
			// todo: add proper support for localeconv - mjf
			//lconv cv = localeconv(); //FIXME:
			char old = ls.decpoint;
			ls.decpoint = '.'; // (cv ? cv.decimal_point[0] : '.'); //getlocaledecpoint() //FIXME:
			buffreplace(ls, old, ls.decpoint);  /* try updated decimal separator */
			if (luaO_str2d(luaZ_buffer(ls.buff), out seminfo.r) == 0)
			{
				/* format error with correct decimal point: no more options */
				buffreplace(ls, ls.decpoint, '.');  /* undo change (for error message) */
				lexerror(ls, "malformed number", (int)RESERVED.TK_NUMBER);
			}
		}


		/* LUA_NUMBER */
		private static void read_numeral (LexState ls, SemInfo seminfo) {
		  lua_assert(lisdigit(ls.current));
		  do {
			save_and_next(ls);
		  } while (lisdigit(ls.current) || ls.current == '.');
		  if (check_next(ls, "Ee") != 0)  /* `E'? */
			check_next(ls, "+-");  /* optional exponent sign */
		  while (lislalnum(ls.current))
			save_and_next(ls);
		  save(ls, '\0');
		  buffreplace(ls, '.', ls.decpoint);  /* follow locale for decimal point */
		  if (luaO_str2d(luaZ_buffer(ls.buff), out seminfo.r) == 0)  /* format error? */
			trydecpoint(ls, seminfo); /* try to update decimal point separator */
		}


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
			  case ']':
				if (skip_sep(ls) == sep)
				{
				  save_and_next(ls);  /* skip 2nd `]' */
				  goto endloop;
				}
			  break;
			  case '\n':
			  case '\r':
				save(ls, '\n');
				inclinenumber(ls);
				if (seminfo == null) luaZ_resetbuffer(ls.buff);  /* avoid wasting space */
				break;
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

		private static int hexavalue (int c) {
		  if (lisdigit(c)) return c - '0';
		  else if (lisupper(c)) return c - 'A' + 10;
		  else return c - 'a' + 10;
		}


		private static int readhexaesc (LexState ls) {
		  int c1, c2 = EOZ;
		  if (!lisxdigit(c1 = next(ls)) || !lisxdigit(c2 = next(ls))) {
		    luaZ_resetbuffer(ls->buff);  /* prepare error message */
		    save(ls, '\\'); save(ls, 'x');
		    if (c1 != EOZ) save(ls, c1);
		    if (c2 != EOZ) save(ls, c2);
		    lexerror(ls, "hexadecimal digit expected", TK_STRING);
		  }
		  return (hexavalue(c1) << 4) + hexavalue(c2);
		}


		private static int readdecesc (LexState ls) {
		  int c1 = ls.current, c2, c3;
		  int c = c1 - '0';
		  if (lisdigit(c2 = next(ls))) {
		    c = 10*c + c2 - '0';
		    if (lisdigit(c3 = next(ls))) {
		      c = 10*c + c3 - '0';
		      if (c > UCHAR_MAX) {
		        luaZ_resetbuffer(ls.buff);  /* prepare error message */
		        save(ls, '\\');
		        save(ls, c1); save(ls, c2); save(ls, c3);
		        lexerror(ls, "decimal escape too large", TK_STRING);
		      }
		      return c;
		    }
		  }
		  /* else, has read one character that was not a digit */
		  zungetc(ls.z);  /* return it to input stream */
		  return c;
		}


		static void read_string (LexState ls, int del, SemInfo seminfo) {
		  save_and_next(ls);
		  while (ls.current != del) {
			switch (ls.current) {
			  case EOZ:
				lexerror(ls, "unfinished string", (int)RESERVED.TK_EOS);
				continue;  /* to avoid warnings */
			  case '\n':
			  case '\r':
				lexerror(ls, "unfinished string", (int)RESERVED.TK_STRING);
				continue;  /* to avoid warnings */
			  case '\\': {
				int c;
				next(ls);  /* do not save the `\' */
				switch (ls.current) {
				  case 'a': c = '\a'; break;
				  case 'b': c = '\b'; break;
				  case 'f': c = '\f'; break;
				  case 'n': c = '\n'; break;
				  case 'r': c = '\r'; break;
				  case 't': c = '\t'; break;
				  case 'v': c = '\v'; break;
                  case 'x': c = readhexaesc(ls); break;
				  case '\n':  /* go through */
				  case '\r': save(ls, '\n'); inclinenumber(ls); continue;
				  case EOZ: continue;  /* will raise an error next loop */
				  default: {
			            if (!lisdigit(ls.current))
			              c = ls.current;  /* handles \\, \", \', and \? */
			            else  /* digital escape \ddd */
			              c = readdecesc(ls);
			            break;
				  }
				}
				next(ls);
				save(ls, c);
				continue;
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
			  case '\n':
			  case '\r': {
				inclinenumber(ls);
				continue;
			  }
			  case '-': {
				next(ls);
				if (ls.current != '-') return '-';
				/* else is a comment */
				next(ls);
				if (ls.current == '[') {
				  int sep = skip_sep(ls);
				  luaZ_resetbuffer(ls.buff);  /* `skip_sep' may dirty the buffer */
				  if (sep >= 0) {
					read_long_string(ls, null, sep);  /* long comment */
					luaZ_resetbuffer(ls.buff);
					continue;
				  }
				}
				/* else short comment */
				while (!currIsNewline(ls) && ls.current != EOZ)
				  next(ls);
				continue;
			  }
			  case '[': {
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
				if (ls.current != '=') return '<';
				else { next(ls); return (int)RESERVED.TK_LE; }
			  }
			  case '>': {
				next(ls);
				if (ls.current != '=') return '>';
				else { next(ls); return (int)RESERVED.TK_GE; }
			  }
			  case '~': {
				next(ls);
				if (ls.current != '=') return '~';
				else { next(ls); return (int)RESERVED.TK_NE; }
			  }
			  case '"':
			  case '\'': {
				read_string(ls, ls.current, seminfo);
				return (int)RESERVED.TK_STRING;
			  }
			  case '.': {
				save_and_next(ls);
				if (check_next(ls, ".") != 0) {
				  if (check_next(ls, ".") != 0)
					  return (int)RESERVED.TK_DOTS;   /* ... */
				  else return (int)RESERVED.TK_CONCAT;   /* .. */
				}
				else if (!lisdigit(ls.current)) return '.';
				else {
				  read_numeral(ls, seminfo);
				  return (int)RESERVED.TK_NUMBER;
				}
			  }
			  case EOZ: {
				  return (int)RESERVED.TK_EOS;
			  }
			  default: {
				if (lisspace(ls.current)) {
				  lua_assert(!currIsNewline(ls));
				  next(ls);
				  continue;
				}
				else if (lisdigit(ls.current)) {
				  read_numeral(ls, seminfo);
				  return (int)RESERVED.TK_NUMBER;
				}
				else if (lislalpha(ls.current)) {
				  /* identifier or reserved word */
				  TString ts;
				  do {
					save_and_next(ls);
				  } while (lislalnum(ls.current));
				  ts = luaX_newstring(ls, luaZ_buffer(ls.buff),
										  luaZ_bufflen(ls.buff));
				  if (ts.tsv.reserved > 0)  /* reserved word? */
					return ts.tsv.reserved - 1 + FIRST_RESERVED;
				  else {
					seminfo.ts = ts;
					return (int)RESERVED.TK_NAME;
				  }
				}
				else {
				  int c = ls.current;
				  next(ls);
				  return c;  /* single-char tokens (+ - / ...) */
				}
			  }
			}
		  }
		}


		public static void luaX_next (LexState ls) {
		  ls.lastline = ls.linenumber;
		  if (ls.lookahead.token != (int)RESERVED.TK_EOS)
		  {  /* is there a look-ahead token? */
			ls.t = new Token(ls.lookahead);  /* use this one */
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

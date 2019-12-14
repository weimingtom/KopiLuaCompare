/*
** $Id: lstrlib.c,v 1.254 2016/12/22 13:08:50 roberto Exp $
** Standard library for string operations and pattern-matching
** See Copyright Notice in lua.h
*/

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace KopiLua
{
	using ptrdiff_t = System.Int32;
	using lua_Integer = System.Int32;
	using lua_Unsigned = System.UInt32;
	using LUA_INTFRM_T = System.Int64;
	using UNSIGNED_LUA_INTFRM_T = System.UInt64;
	using lua_Number = System.Double;
    using LUA_FLTFRM_T = System.Double;
	using LUAI_UACINT = System.Int32;
	using LUAI_UACNUMBER = System.Double;    

	public partial class Lua
	{

		/*
		** maximum number of captures that a pattern can do during
		** pattern-matching. This limit is arbitrary, but must fit in
		** an unsigned char.
		*/
		//#if !defined(LUA_MAXCAPTURES)
		private const int LUA_MAXCAPTURES = 32;
		//#endif

	
		/* macro to 'unsign' a character */
		private static char uchar(char c)	{return c;}


		/*
		** Some sizes are better limited to fit in 'int', but must also fit in
		** 'size_t'. (We assume that 'lua_Integer' cannot be smaller than 'int'.)
		*/
		//#define MAX_SIZET	((size_t)(~(size_t)0))

		//#define MAXSIZE  \
		//	(sizeof(size_t) < sizeof(int) ? MAX_SIZET : (size_t)(INT_MAX))
		private const uint MAXSIZE = (uint)int.MaxValue;
		


		
		private static int str_len (lua_State L) {
		  uint l;
		  luaL_checklstring(L, 1, out l);
		  lua_pushinteger(L, (lua_Integer)l);
		  return 1;
		}


        /* translate a relative string position: negative means back from end */
		private static lua_Integer posrelat (lua_Integer pos, uint len) {
		  if (pos >= 0) return pos;
		  else if (0u - (uint)pos > len) return 0;
		  else return (lua_Integer)len + pos + 1;
		}


		private static int str_sub (lua_State L) {
		  uint l; //FIXME:???l ? 1 ?
		  CharPtr s = luaL_checklstring(L, 1, out l);
		  lua_Integer start = posrelat(luaL_checkinteger(L, 2), l);
		  lua_Integer end = posrelat(luaL_optinteger(L, 3, -1), l);
		  if (start < 1) start = 1;
		  if (end > (lua_Integer)l) end = (lua_Integer)l;
		  if (start <= end)
			lua_pushlstring(L, s + start - 1, (uint)(end - start) + 1);
		  else lua_pushliteral(L, "");
		  return 1;
		}


		private static int str_reverse (lua_State L) {
		  uint l, i;
		  luaL_Buffer b = new luaL_Buffer();
		  CharPtr s = luaL_checklstring(L, 1, out l);
		  CharPtr p = luaL_buffinitsize(L, b, l);
		  for (i = 0; i < l; i++)
		    p[i] = s[l - i - 1];
		  luaL_pushresultsize(b, l);
		  return 1;
		}


		private static int str_lower (lua_State L) {
		  uint l;
		  uint i;
		  luaL_Buffer b = new luaL_Buffer();
		  CharPtr s = luaL_checklstring(L, 1, out l);
		  CharPtr p = luaL_buffinitsize(L, b, l);
		  for (i=0; i<l; i++)
			  p[i] = tolower(uchar(s[i]));
		  luaL_pushresultsize(b, l);
		  return 1;
		}


		private static int str_upper (lua_State L) {
		  uint l;
		  uint i;
		  luaL_Buffer b = new luaL_Buffer();
		  CharPtr s = luaL_checklstring(L, 1, out l);
		  CharPtr p = luaL_buffinitsize(L, b, l);
		  for (i=0; i<l; i++)
			  p[i] = toupper(uchar(s[i]));
		  luaL_pushresultsize(b, l);
		  return 1;
		}


		private static int str_rep (lua_State L) {
		  uint l, lsep;
		  CharPtr s = luaL_checklstring(L, 1, out l);
		  lua_Integer n = luaL_checkinteger(L, 2);
		  CharPtr sep = luaL_optlstring(L, 3, "", out lsep);
		  if (n <= 0) lua_pushliteral(L, "");
		  else if (l + lsep < l || l + lsep > MAXSIZE / n)  /* may overflow? */
		    return luaL_error(L, "resulting string too large");
		  else {
		    uint totallen = (uint)(n * l) + (uint)(n - 1) * lsep; //FIXME:changed, (uint)
		    luaL_Buffer b = new luaL_Buffer();
		    CharPtr p = luaL_buffinitsize(L, b, totallen);
		    while (n-- > 1) {  /* first n-1 copies (followed by separator) */
		      memcpy(p, s, l * 1); p += l; //FIXME:changed, sizeof(char)
		      if (lsep > 0) {  /* empty 'memcpy' is not that cheap */
		        memcpy(p, sep, lsep * 1); //FIXME:changed, sizeof(char) 
				p += lsep;
		      }
		    }
		    memcpy(p, s, l * 1/*sizeof(char)*/);  /* last copy (not followed by separator) */
		    luaL_pushresultsize(b, totallen);
		  }
		  return 1;
		}


		private static int str_byte (lua_State L) {
		  uint l;
		  CharPtr s = luaL_checklstring(L, 1, out l);
		  lua_Integer posi = posrelat(luaL_optinteger(L, 2, 1), l);
		  lua_Integer pose = posrelat(luaL_optinteger(L, 3, (int)posi), l);
		  int n, i;
		  if (posi < 1) posi = 1;
		  if (pose > (lua_Integer)l) pose = (lua_Integer)l;
		  if (posi > pose) return 0;  /* empty interval; return no values */
		  if (pose - posi >= INT_MAX)  /* arithmetic overflow? */
			return luaL_error(L, "string slice too long");
		  n = (int)(pose -  posi) + 1;
		  luaL_checkstack(L, n, "string slice too long");
		  for (i=0; i<n; i++)
			  lua_pushinteger(L, (byte)(s[posi + i - 1]));
		  return n;
		}


		private static int str_char (lua_State L) {
		  int n = lua_gettop(L);  /* number of arguments */
		  int i;
		  luaL_Buffer b = new luaL_Buffer();
		  CharPtr p = luaL_buffinitsize(L, b, (uint)n); //FIXME:added, (uint)
		  for (i=1; i<=n; i++) {
			lua_Integer c = luaL_checkinteger(L, i);
			luaL_argcheck(L, (byte)(c) == c, i, "value out of range"); //FIXME: uchar()
			p[i - 1] = (char)(c); //FIXME: uchar()->(char)
		  }
		  luaL_pushresultsize(b, (uint)n);//FIXME:added, (uint)
		  return 1;
		}


		private static int writer (lua_State L, object b, uint size, object B) {
			//(void)L;
			if (b.GetType() != typeof(CharPtr)) //FIXME: added below
			{
				using (MemoryStream stream = new MemoryStream())
				{
					BinaryFormatter formatter = new BinaryFormatter();
					formatter.Serialize(stream, b);
					stream.Flush();
					byte[] bytes = stream.GetBuffer();
					char[] chars = new char[bytes.Length];
					for (int i = 0; i < bytes.Length; i++)
						chars[i] = (char)bytes[i];
					b = new CharPtr(chars);
				}
			}
		  luaL_addlstring((luaL_Buffer)B, (CharPtr)b, size); //FIXME: changed
		  return 0;
		}


		private static int str_dump (lua_State L) {
		  luaL_Buffer b = new luaL_Buffer();
		  int strip = lua_toboolean(L, 2);
		  luaL_checktype(L, 1, LUA_TFUNCTION);
		  lua_settop(L, 1);
		  luaL_buffinit(L,b);
		  if (lua_dump(L, writer, b, strip) != 0)
			return luaL_error(L, "unable to dump given function");
		  luaL_pushresult(b);
		  return 1;
		}



		/*
		** {======================================================
		** PATTERN MATCHING
		** =======================================================
		*/


		public const int CAP_UNFINISHED	= (-1);
		public const int CAP_POSITION	= (-2);


		public class MatchState {

		  public MatchState()
		  {
			  for (int i = 0; i < LUA_MAXCAPTURES; i++)
				  capture[i] = new capture_();
		  }

		  public CharPtr src_init;  /* init of source string */
		  public CharPtr src_end;  /* end ('\0') of source string */
		  public CharPtr p_end;  /* end ('\0') of pattern */
		  public lua_State L;
		  public int matchdepth;  /* control for recursive depth (to avoid C stack overflow) */
		  public byte level;  /* total number of captures (finished or unfinished) */

		  public class capture_{
			public CharPtr init;
			public ptrdiff_t len;
		  };
		  public capture_[] capture = new capture_[LUA_MAXCAPTURES];
		};


		/* recursive function */
		//static const char *match (MatchState *ms, const char *s, const char *p);


		/* maximum recursion depth for 'match' */
		//#if !defined(MAXCCALLS)
		public const int MAXCCALLS = 200;
		//#endif



		public const char L_ESC		= '%';
		public const string SPECIALS = "^$*+?.([%-";


		private static int check_capture (MatchState ms, int l) {
		  l -= '1';
		  if (l < 0 || l >= ms.level || ms.capture[l].len == CAP_UNFINISHED)
			return luaL_error(ms.L, "invalid capture index %%%d", l + 1);
		  return l;
		}


		private static int capture_to_close (MatchState ms) {
		  int level = ms.level;
		  for (level--; level>=0; level--)
			if (ms.capture[level].len == CAP_UNFINISHED) return level;
		  return luaL_error(ms.L, "invalid pattern capture");
		}


		private static CharPtr classend (MatchState ms, CharPtr p) {
		  p = new CharPtr(p);
		  char c = p[0];
		  p = p.next();
		  switch (c) {
			case L_ESC: {
			  if (p == ms.p_end)
				luaL_error(ms.L, "malformed pattern (ends with '%%')");
			  return p+1;
			}
			case '[': {
			  if (p[0] == '^') p = p.next();
			  do {  /* look for a ']' */
				if (p == ms.p_end)
				  luaL_error(ms.L, "malformed pattern (missing ']')");
				c = p[0]; //FIXME: added, move to here, see below if
				p = p.next(); //FIXME: added, move to here, see below if
				if (c == L_ESC && p < ms.p_end) //FIXME: changed 
				  p = p.next();  /* skip escapes (e.g. '%]') */ //FIXME: p++
			  } while (p[0] != ']');
			  return p+1;
			}
			default: {
			  return p;
			}
		  }
		}


		private static int match_class (int c, int cl) {
		  bool res;
		  switch (tolower(cl)) {
			case 'a' : res = isalpha(c); break;
			case 'c' : res = iscntrl(c); break;
			case 'd' : res = isdigit(c); break;
            case 'g' : res = isgraph(c); break;
			case 'l' : res = islower(c); break;
			case 'p' : res = ispunct(c); break;
			case 's' : res = isspace(c); break;
			case 'u' : res = isupper(c); break;
			case 'w' : res = isalnum(c); break;
			case 'x' : res = isxdigit(c); break; //FIXME: ???(char)c???->c
			case 'z' : res = (c == 0); break;  /* deprecated option */
			default: return (cl == c) ? 1 : 0;
		  }
		  return (islower(cl) ? (res ? 1 : 0) : ((!res) ? 1 : 0));
		}


		private static int matchbracketclass (int c, CharPtr p, CharPtr ec) {
		  int sig = 1;
		  if (p[1] == '^') {
			sig = 0;
			p = p.next();  /* skip the '^' */
		  }
		  while ((p=p.next()) < ec) {
			if (p == L_ESC) {
			  p = p.next();
			  if (match_class(c, (byte)(p[0])) != 0)
				return sig;
			}
			else if ((p[1] == '-') && (p + 2 < ec)) {
			  p+=2;
			  if ((byte)((p[-2])) <= c && (c <= (byte)p[0]))
				return sig;
			}
			else if ((byte)(p[0]) == c) return sig;
		  }
		  return (sig == 0) ? 1 : 0;
		}


		private static int singlematch (MatchState ms, CharPtr s, CharPtr p,
		                                CharPtr ep) {
		  if (s >= ms.src_end)
		    return 0;
		  else {
		    int c = uchar(s[0]);		
			switch (p[0]) {
			  case '.': return 1;  /* matches any char */
			  case L_ESC: return match_class(c, (byte)(p[1]));
			  case '[': return matchbracketclass(c, p, ep-1);
			  default: return ((byte)(p[0]) == c) ? 1 : 0;
			}
		  }
		}


		private static CharPtr matchbalance (MatchState ms, CharPtr s,
										   CharPtr p) {
		  if (p >= ms.p_end - 1)
		    luaL_error(ms.L, "malformed pattern (missing arguments to '%%b')");
		  if (s[0] != p[0]) return null;
		  else {
			int b = p[0];
			int e = p[1];
			int cont = 1;
			while ((s=s.next()) < ms.src_end) {
			  if (s[0] == e) {
				if (--cont == 0) return s+1;
			  }
			  else if (s[0] == b) cont++;
			}
		  }
		  return null;  /* string ends out of balance */
		}


		private static CharPtr max_expand (MatchState ms, CharPtr s,
										 CharPtr p, CharPtr ep) {
		  ptrdiff_t i = 0;  /* counts maximum expand for item */
		  while (singlematch(ms, s + i, p, ep) != 0)
			i++;
		  /* keeps trying to match with the maximum repetitions */
		  while (i>=0) {
			CharPtr res = match(ms, (s+i), ep+1);
			if (res != null) return res;
			i--;  /* else didn't match; reduce 1 repetition to try again */
		  }
		  return null;
		}


		private static CharPtr min_expand (MatchState ms, CharPtr s,
										 CharPtr p, CharPtr ep) {
		  for (;;) {
			CharPtr res = match(ms, s, ep+1);
			if (res != null)
			  return res;
		  else if (singlematch(ms, s, p, ep) != 0)
			  s = s.next();  /* try with one more repetition */
			else return null;
		  }
		}


		private static CharPtr start_capture (MatchState ms, CharPtr s,
											CharPtr p, int what) {
		  CharPtr res;
		  int level = ms.level;
		  if (level >= LUA_MAXCAPTURES) luaL_error(ms.L, "too many captures");
		  ms.capture[level].init = s;
		  ms.capture[level].len = what;
		  ms.level = (byte)(level+1);
		  if ((res=match(ms, s, p)) == null)  /* match failed? */
			ms.level--;  /* undo capture */
		  return res;
		}


		private static CharPtr end_capture(MatchState ms, CharPtr s,
										  CharPtr p) {
		  int l = capture_to_close(ms);
		  CharPtr res;
		  ms.capture[l].len = s - ms.capture[l].init;  /* close capture */
		  if ((res = match(ms, s, p)) == null)  /* match failed? */
			ms.capture[l].len = CAP_UNFINISHED;  /* undo capture */
		  return res;
		}


		private static CharPtr match_capture(MatchState ms, CharPtr s, int l) {
		  uint len;
		  l = check_capture(ms, l);
		  len = (uint)ms.capture[l].len;
		  if ((uint)(ms.src_end-s) >= len &&
			  memcmp(ms.capture[l].init, s, len) == 0)
			return s+len;
		  else return null;
		}


		private static CharPtr match (MatchState ms, CharPtr s, CharPtr p) {
		  if (ms.matchdepth-- == 0)
		    luaL_error(ms.L, "pattern too complex");
		  init: /* using goto's to optimize tail recursion */		  
		  if (p != ms.p_end) {  /* end of pattern? */
		  	switch (p[0]) {
		      case '(': {  /* start capture */
		  		if (p[1] == ')')  /* position capture? */
		          s = start_capture(ms, s, p + 2, CAP_POSITION);
		        else
		          s = start_capture(ms, s, p + 1, CAP_UNFINISHED);
		        break;
		      }
		      case ')': {  /* end capture */
		        s = end_capture(ms, s, p + 1);
		        break;
		      }
		      case '$': {
		        if ((p + 1) != ms.p_end)  /* is the '$' the last char in pattern? */
		          goto dflt;  /* no; go to default */
		        s = (s == ms.src_end) ? s : null;  /* check end of string */
		        break;
		      }
		      case L_ESC: {  /* escaped sequences not in the format class[*+?-]? */
		  		switch (p[1]) {
		          case 'b': {  /* balanced string? */
		            s = matchbalance(ms, s, p + 2);
		            if (s != null) {
		              p += 4; goto init;  /* return match(ms, s, p + 4); */
		            }  /* else fail (s == NULL) */
		            break;
		          }
		          case 'f': {  /* frontier? */
		            CharPtr ep; char previous;
		            p += 2;
		            if (p[0] != '[')
		              luaL_error(ms.L, "missing '[' after '%%f' in pattern");
		            ep = classend(ms, p);  /* points to what is next */
		            previous = (s == ms.src_init) ? '\0' : s[-1];
		            if (0 == matchbracketclass(uchar(previous), p, ep - 1) &&
		              matchbracketclass(uchar(s[0]), p, ep - 1) != 0) {
		              p = ep; goto init;  /* return match(ms, s, ep); */
		            }
		            s = null;  /* match failed */
		            break;
		          }
		          case '0': case '1': case '2': case '3':
		          case '4': case '5': case '6': case '7':
		          case '8': case '9': {  /* capture results (%0-%9)? */
		  			s = match_capture(ms, s, uchar(p[1]));
		            if (s != null) {
		              p += 2; goto init;  /* return match(ms, s, p + 2) */
		            }
		            break;
		          }
		          default: goto dflt;
		        }
		        break;
		      }
		      default: dflt: {  /* pattern class plus optional suffix */
		        CharPtr ep = classend(ms, p);  /* points to optional suffix */
		        /* does not match at least once? */
		        if (0 == singlematch(ms, s, p, ep)) {
		          if (ep[0] == '*' || ep[0] == '?' || ep[0] == '-') {  /* accept empty? */
		            p = ep + 1; goto init;  /* return match(ms, s, ep + 1); */
		          }
		          else  /* '+' or no suffix */
		            s = null;  /* fail */
		        }
		        else {  /* matched once */			
		          switch (ep[0]) {  /* handle optional suffix */
		            case '?': {  /* optional */
		              CharPtr res;
		              if ((res = match(ms, s + 1, ep + 1)) != null)
		                s = res;
		              else {
		                p = ep + 1; goto init;  /* else return match(ms, s, ep + 1); */
		              }
		              break;
		            }
		            case '+':  /* 1 or more repetitions */
		              s.inc();  /* 1 match already done */
		              /* FALLTHROUGH */
		              goto case '*'; //FIXME:added
		            case '*':  /* 0 or more repetitions */
		              s = max_expand(ms, s, p, ep);
		              break;
		            case '-':  /* 0 or more repetitions (minimum) */
		              s = min_expand(ms, s, p, ep);
		              break;
		            default:  /* no suffix */
		              s.inc(); p = ep; goto init;  /* return match(ms, s + 1, ep); */
		          }
		        }
		        break;
		      }
		    }
		  }
		  ms.matchdepth++;
		  return s;
		}



		private static CharPtr lmemfind (CharPtr s1, uint l1,
									   CharPtr s2, uint l2) {
		  if (l2 == 0) return s1;  /* empty strings are everywhere */
		  else if (l2 > l1) return null;  /* avoids a negative 'l1' */
		  else {
			CharPtr init;  /* to search for a '*s2' inside 's1' */
			l2--;  /* 1st char will be checked by 'memchr' */
			l1 = l1-l2;  /* 's2' cannot be found after that */
			while (l1 > 0 && (init = memchr(s1, s2[0], l1)) != null) {
			  init = init.next();   /* 1st char is already checked */
			  if (memcmp(init, s2+1, l2) == 0)
				return init-1;
			  else {  /* correct 'l1' and 's1' to try again */
				l1 -= (uint)(init-s1);
				s1 = init;
			  }
			}
			return null;  /* not found */
		  }
		}


		private static void push_onecapture (MatchState ms, int i, CharPtr s,
															CharPtr e) {
		  if (i >= ms.level) {
			if (i == 0)  /* ms.level == 0, too */
			  lua_pushlstring(ms.L, s, (uint)(e - s));  /* add whole match */
			else
			  luaL_error(ms.L, "invalid capture index %%%d", i + 1);
		  }
		  else {
			ptrdiff_t l = ms.capture[i].len;
			if (l == CAP_UNFINISHED) luaL_error(ms.L, "unfinished capture");
			if (l == CAP_POSITION)
			  lua_pushinteger(ms.L, (ms.capture[i].init - ms.src_init) + 1);
			else
			  lua_pushlstring(ms.L, ms.capture[i].init, (uint)l);
		  }
		}


		private static int push_captures (MatchState ms, CharPtr s, CharPtr e) {
		  int i;
		  int nlevels = ((ms.level == 0) && (s!=null)) ? 1 : ms.level;
		  luaL_checkstack(ms.L, nlevels, "too many captures");
		  for (i = 0; i < nlevels; i++)
			push_onecapture(ms, i, s, e);
		  return nlevels;  /* number of strings pushed */
		}


		/* check whether pattern has no special characters */
		private static int nospecials (CharPtr p, uint l) {
		  uint upto = 0;
		  do {
		    if (strpbrk(p + upto, SPECIALS)!=null)
		      return 0;  /* pattern has a special character */
		    upto += (uint)strlen(p + upto) + 1;  /* may have more after \0 */ //FIXME:added, (uint)
		  } while (upto <= l);
		  return 1;  /* no special chars found */
		}


		private static void prepstate (MatchState ms, lua_State L,
		                       CharPtr s, uint ls, CharPtr p, uint lp) {
		  ms.L = L;
		  ms.matchdepth = MAXCCALLS;
		  ms.src_init = s;
		  ms.src_end = s + ls;
		  ms.p_end = p + lp;
		}


		private static void reprepstate (MatchState ms) {
		  ms.level = 0;
		  lua_assert(ms.matchdepth == MAXCCALLS);
		}

		


		private static int str_find_aux (lua_State L, int find) {
		  uint ls, lp;
		  CharPtr s = luaL_checklstring(L, 1, out ls);
		  CharPtr p = luaL_checklstring(L, 2, out lp);
		  lua_Integer init = posrelat(luaL_optinteger(L, 3, 1), ls);
		  if (init < 1) init = 1;
		  else if (init > (lua_Integer)ls + 1) {  /* start after string's end? */
		    lua_pushnil(L);  /* cannot find anything */
		    return 1;
		  }
          /* explicit request or no special characters? */
		  if ((find!=0) && ((lua_toboolean(L, 4)!=0) || nospecials(p, lp)!=0)) {
			/* do a plain search */
			CharPtr s2 = lmemfind(s + init - 1, ls - (uint)init + 1, p, lp);
			if (s2 != null) {
			  lua_pushinteger(L, (s2 - s) + 1);
			  lua_pushinteger(L, (int)((s2 - s) + lp));
			  return 2;
			}
		  }
		  else {
			MatchState ms = new MatchState();
			CharPtr s1 = s + init - 1;
			int anchor = (p[0] == '^')?1:0;
		    if (anchor != 0) {
		      /*p++*/p=p+1; lp--;  /* skip anchor character */ //FIXME:changed, ++
		    }
			prepstate(ms, L, s, ls, p, lp);
			do {
			  CharPtr res;
			  reprepstate(ms);
			  if ((res=match(ms, s1, p)) != null) {
				if (find != 0) {
				  lua_pushinteger(L, (s1 - s) + 1);  /* start */
				  lua_pushinteger(L, res - s);   /* end */
				  return push_captures(ms, null, null) + 2;
				}
				else
				  return push_captures(ms, s1, res);
			  }
			} while (((s1=s1.next()) <= ms.src_end) && (anchor==0));
		  }
		  lua_pushnil(L);  /* not found */
		  return 1;
		}


		private static int str_find (lua_State L) {
		  return str_find_aux(L, 1);
		}


		private static int str_match (lua_State L) {
		  return str_find_aux(L, 0);
		}


		/* state for 'gmatch' */
		private class GMatchState {
		  public CharPtr src;  /* current position */
		  public CharPtr p;  /* pattern */
		  public CharPtr lastmatch;  /* end of last match */
		  public MatchState ms = new MatchState();  /* match state */
		};


		private static int gmatch_aux (lua_State L) {
		  GMatchState gm = (GMatchState)lua_touserdata(L, lua_upvalueindex(3));
		  CharPtr src;
		  gm.ms.L = L;
		  for (src = new CharPtr(gm.src); src <= gm.ms.src_end; src.inc()) { //FIXME:new CharPtr()
		    CharPtr e;
		    reprepstate(gm.ms);
		    if ((e = match(gm.ms, src, gm.p)) != null && e != gm.lastmatch) {
		      gm.src = gm.lastmatch = e;
		      return push_captures(gm.ms, src, e);
		    }
		  }
		  return 0;  /* not found */
		}


		private static int gmatch (lua_State L) {
		  uint ls, lp;
		  CharPtr s = luaL_checklstring(L, 1, out ls);
		  CharPtr p = luaL_checklstring(L, 2, out lp);
		  GMatchState gm;
		  lua_settop(L, 2);  /* keep them on closure to avoid being collected */
		  gm = (GMatchState)lua_newuserdata(L, typeof(GMatchState));
		  prepstate(gm.ms, L, s, ls, p, lp);
		  gm.src = s; gm.p = p; gm.lastmatch = null;
		  lua_pushcclosure(L, gmatch_aux, 3);
		  return 1;
		}


		private static void add_s (MatchState ms, luaL_Buffer b, CharPtr s,
														         CharPtr e) {
		  uint l, i;
		  lua_State L = ms.L;
		  CharPtr news = lua_tolstring(L, 3, out l);
		  for (i = 0; i < l; i++) {
			if (news[i] != L_ESC)
			  luaL_addchar(b, news[i]);
			else {
			  i++;  /* skip ESC */
			  if (!isdigit((byte)(news[i]))) {
		        if (news[i] != L_ESC)
		          luaL_error(L, "invalid use of '%c' in replacement string", L_ESC);
		        luaL_addchar(b, news[i]);
		      }
			  else if (news[i] == '0')
				  luaL_addlstring(b, s, (uint)(e - s));
			  else {
			  	uint null_ = 0;
				push_onecapture(ms, news[i] - '1', s, e);
		        luaL_tolstring(L, -1, out null_);  /* if number, convert it to string */
		        lua_remove(L, -2);  /* remove original value */			
				luaL_addvalue(b);  /* add capture to accumulated result */
			  }
			}
		  }
		}


		private static void add_value (MatchState ms, luaL_Buffer b, CharPtr s,
															   CharPtr e, int tr) {
		  lua_State L = ms.L;
		  switch (tr) {
			case LUA_TFUNCTION: {
			  int n;
			  lua_pushvalue(L, 3);
			  n = push_captures(ms, s, e);
			  lua_call(L, n, 1);
			  break;
			}
			case LUA_TTABLE: {
			  push_onecapture(ms, 0, s, e);
			  lua_gettable(L, 3);
			  break;
			}
		    default: {   /* LUA_TNUMBER or LUA_TSTRING */
      		  add_s(ms, b, s, e);
		      return;
		    }
		  }
		  if (lua_toboolean(L, -1)==0) {  /* nil or false? */
			lua_pop(L, 1);
			lua_pushlstring(L, s, (uint)(e - s));  /* keep original text */
		  }
		  else if (lua_isstring(L, -1)==0)
			luaL_error(L, "invalid replacement value (a %s)", luaL_typename(L, -1));
		  luaL_addvalue(b);  /* add result to accumulator */
		}


		private static int str_gsub (lua_State L) {
		  uint srcl, lp;
		  CharPtr src = luaL_checklstring(L, 1, out srcl); /* subject */
		  CharPtr p = luaL_checklstring(L, 2, out lp);  /* pattern */
		  CharPtr lastmatch = null;  /* end of last match */
          int tr = lua_type(L, 3);  /* replacement type */
          lua_Integer max_s = luaL_optinteger(L, 4, (int)(srcl+1));  /* max replacements */
		  int anchor = (p[0] == '^') ? 1 : 0;
		  lua_Integer n = 0;  /* replacement count */
		  MatchState ms = new MatchState();
		  luaL_Buffer b = new luaL_Buffer();
		  luaL_argcheck(L, tr == LUA_TNUMBER || tr == LUA_TSTRING ||
		                   tr == LUA_TFUNCTION || tr == LUA_TTABLE, 3,
		                      "string/function/table expected");
		  luaL_buffinit(L, b);
		  if (anchor!=0) {
		    /*p++*/p=p+1; lp--;  /* skip anchor character */ //FIXME:changed
		  }
		  prepstate(ms, L, src, srcl, p, lp);
		  while (n < max_s) {
			CharPtr e;
			reprepstate(ms);  /* (re)prepare state for new match */
    		if ((e = match(ms, src, p)) != null && e != lastmatch) {  /* match? */
			  n++;
			  add_value(ms, b, src, e, tr);  /* add replacement to buffer */
			  src = lastmatch = e;
			}
			else if (src < ms.src_end)  /* otherwise, skip one character */
			{
				char c = src[0];
				src = src.next();
				luaL_addchar(b, c);
			}
			else break;  /* end of subject */
			if (anchor != 0) break;
		  }
		  luaL_addlstring(b, src, (uint)(ms.src_end-src));
		  luaL_pushresult(b);
		  lua_pushinteger(L, (int)n);  /* number of substitutions */
		  return 2;
		}

		/* }====================================================== */



		/*
		** {======================================================
		** STRING FORMAT
		** =======================================================
		*/

//		#if !defined(lua_number2strx)	/* { */
//
//		/*
//		** Hexadecimal floating-point formatter
//		*/
//		
//		#include <math.h>
//		
//		#define SIZELENMOD	(sizeof(LUA_NUMBER_FRMLEN)/sizeof(char))
//		
//		
//		/*
//		** Number of bits that goes into the first digit. It can be any value
//		** between 1 and 4; the following definition tries to align the number
//		** to nibble boundaries by making what is left after that first digit a
//		** multiple of 4.
//		*/
//		#define L_NBFD		((l_mathlim(MANT_DIG) - 1)%4 + 1)
//		
//		
//		/*
//		** Add integer part of 'x' to buffer and return new 'x'
//		*/
//		static lua_Number adddigit (char *buff, int n, lua_Number x) {
//		  lua_Number dd = l_mathop(floor)(x);  /* get integer part from 'x' */
//		  int d = (int)dd;
//		  buff[n] = (d < 10 ? d + '0' : d - 10 + 'a');  /* add to buffer */
//		  return x - dd;  /* return what is left */
//		}
//		
//		
//		static int num2straux (char *buff, int sz, lua_Number x) {
//		  /* if 'inf' or 'NaN', format it like '%g' */
//		  if (x != x || x == (lua_Number)HUGE_VAL || x == -(lua_Number)HUGE_VAL)
//		    return l_sprintf(buff, sz, LUA_NUMBER_FMT, (LUAI_UACNUMBER)x);
//		  else if (x == 0) {  /* can be -0... */
//		    /* create "0" or "-0" followed by exponent */
//		    return l_sprintf(buff, sz, LUA_NUMBER_FMT "x0p+0", (LUAI_UACNUMBER)x);
//		  }
//		  else {
//		    int e;
//		    lua_Number m = l_mathop(frexp)(x, &e);  /* 'x' fraction and exponent */
//		    int n = 0;  /* character count */
//		    if (m < 0) {  /* is number negative? */
//		      buff[n++] = '-';  /* add signal */
//		      m = -m;  /* make it positive */
//		    }
//		    buff[n++] = '0'; buff[n++] = 'x';  /* add "0x" */
//		    m = adddigit(buff, n++, m * (1 << L_NBFD));  /* add first digit */
//		    e -= L_NBFD;  /* this digit goes before the radix point */
//		    if (m > 0) {  /* more digits? */
//		      buff[n++] = lua_getlocaledecpoint();  /* add radix point */
//		      do {  /* add as many digits as needed */
//		        m = adddigit(buff, n++, m * 16);
//		      } while (m > 0);
//		    }
//		    n += l_sprintf(buff + n, sz - n, "p%+d", e);  /* add exponent */
//		    lua_assert(n < sz);
//		    return n;
//		  }
//		}
//
//
//		static int lua_number2strx (lua_State *L, char *buff, int sz,
//		                            const char *fmt, lua_Number x) {
//		  int n = num2straux(buff, sz, x);
//		  if (fmt[SIZELENMOD] == 'A') {
//		    int i;
//		    for (i = 0; i < n; i++)
//		      buff[i] = toupper(uchar(buff[i]));
//		  }
//		  else if (fmt[SIZELENMOD] != 'a')
//		    luaL_error(L, "modifiers for format '%%a'/'%%A' not implemented");
//		  return n;
//		}
//		
//		#endif				/* } */


		/*
		** Maximum size of each formatted item. This maximum size is produced
		** by format('%.99f', -maxfloat), and is equal to 99 + 3 ('-', '.',
		** and '\0') + number of decimal digits to represent maxfloat (which
		** is maximum exponent + 1). (99+3+1 then rounded to 120 for "extra
		** expenses", such as locale-dependent stuff)
		*/
		public const int MAX_ITEM = 512; //(120 + l_mathlim(MAX_10_EXP)); //FIXME:???not calculate with compiler
		
		
		/* valid flags in a format specification */
		public const string FLAGS = "-+ #0";
		/*
		** maximum size of each format specification (such as "%-099.99d")
		*/
		public static readonly int MAX_FORMAT = 32;


		private static void addquoted (luaL_Buffer b, CharPtr s, uint len) {
		  luaL_addchar(b, '"');
		  while ((len--) != 0) {
		    if (s[0] == '"' || s[0] == '\\' || s[0] == '\n') {
		      luaL_addchar(b, '\\');
		      luaL_addchar(b, s[0]);
		    }
		    else if (iscntrl(uchar(s[0]))) {
		      CharPtr buff = new char[10];
		      if (!isdigit(uchar(s[1])))
		        l_sprintf(buff, 10/*sizeof(buff)*/, "\\%d", (int)uchar(s[0]));
		      else
		        l_sprintf(buff, 10/*sizeof(buff)*/, "\\%03d", (int)uchar(s[0]));
		      luaL_addstring(b, buff);
		    }
		    else
		      luaL_addchar(b, s[0]);
		    s = s.next();
		  }
		  luaL_addchar(b, '"');
		}


		/*
		** Ensures the 'buff' string uses a dot as the radix character.
		*/
		private static void checkdp (CharPtr buff, int nb) {
		  if (memchr(buff, '.', (uint)nb) == null) {  /* no dot? */
		    char point = lua_getlocaledecpoint();  /* try locale point */
		    CharPtr ppoint = (CharPtr)memchr(buff, point, (uint)nb);
		    if (ppoint!=null) ppoint[0] = '.';  /* change it to a dot */
		  }
		}


		private static void addliteral (lua_State L, luaL_Buffer b, int arg) {
		  switch (lua_type(L, arg)) {
		    case LUA_TSTRING: {
		      uint len;
		      CharPtr s = lua_tolstring(L, arg, out len);
		      addquoted(b, s, len);
		      break;
		    }
		    case LUA_TNUMBER: {
		      CharPtr buff = luaL_prepbuffsize(b, MAX_ITEM);
		      int nb;
		      if (0==lua_isinteger(L, arg)) {  /* float? */
		        lua_Number n = lua_tonumber(L, arg);  /* write as hexa ('%a') */
		        nb = lua_number2strx(L, buff, MAX_ITEM, "%" + LUA_NUMBER_FRMLEN + "a", n);
		        checkdp(buff, nb);  /* ensure it uses a dot */
		      }
		      else {  /* integers */
		        lua_Integer n = lua_tointeger(L, arg);
		        CharPtr format = (n == LUA_MININTEGER)  /* corner case? */
		                           ? "0x%" + LUA_INTEGER_FRMLEN + "x"  /* use hexa */
		                           : LUA_INTEGER_FMT;  /* else use default format */
		        nb = l_sprintf(buff, MAX_ITEM, format, (LUAI_UACINT)n);
		      }
		      luaL_addsize(b, (uint)nb);
		      break;
		    }
		    case LUA_TNIL: case LUA_TBOOLEAN: {
			  uint null_ = 0;
		      luaL_tolstring(L, arg, out null_);
		      luaL_addvalue(b);
		      break;
		    }
		    default: {
		      luaL_argerror(L, arg, "value has no literal form");
		      break;
		    }
		  }
		}
		

		private static CharPtr scanformat (lua_State L, CharPtr strfrmt, CharPtr form) {
		  CharPtr p = strfrmt;
		  while (p[0] != '\0' && strchr(FLAGS, p[0]) != null) p = p.next();  /* skip flags */
		  if ((uint)(p - strfrmt) >= (FLAGS.Length)) //FIXME:???sizeof(FLAGS)/sizeof(char), ?+1
			luaL_error(L, "invalid format (repeated flags)");
		  if (isdigit((byte)(p[0]))) p = p.next();  /* skip width */
		  if (isdigit((byte)(p[0]))) p = p.next();  /* (2 digits at most) */
		  if (p[0] == '.') {
			p = p.next();
			if (isdigit((byte)(p[0]))) p = p.next();  /* skip precision */
			if (isdigit((byte)(p[0]))) p = p.next();  /* (2 digits at most) */
		  }
		  if (isdigit((byte)(p[0])))
			luaL_error(L, "invalid format (width or precision too long)");
		  form[0] = '%'; form = form.next();
		  memcpy(form, strfrmt, ((p - strfrmt) + 1) * 1); //FIXME: * sizeof(char)
		  form += (p - strfrmt) + 1;
		  form[0] = '\0';
		  return p;
		}


		/*
		** add length modifier into formats
		*/
		private static void addlenmod (CharPtr form, CharPtr lenmod) {
		  uint l = (uint)strlen(form);
          uint lm = (uint)strlen(lenmod);//FIXME:added, (uint)
		  char spec = form[l - 1];
		  strcpy(form + l - 1, lenmod);
		  form[l + lm - 1] = spec;
		  form[l + lm] = '\0';
		}


		private static int str_format (lua_State L) {
          int top = lua_gettop(L);
		  int arg = 1;
		  uint sfl;
		  CharPtr strfrmt = luaL_checklstring(L, arg, out sfl);
		  CharPtr strfrmt_end = strfrmt+sfl;
		  luaL_Buffer b = new luaL_Buffer();
		  luaL_buffinit(L, b);
		  while (strfrmt < strfrmt_end) {
			  if (strfrmt[0] != L_ESC) {
				  luaL_addchar(b, strfrmt[0]);
				  strfrmt = strfrmt.next();
			  } else if (strfrmt[1] == L_ESC) {
				  luaL_addchar(b, strfrmt[0]);  /* %% */
				  strfrmt = strfrmt + 2;
			  } else { /* format item */
				  strfrmt = strfrmt.next();
				  CharPtr form = new char[MAX_FORMAT];  /* to store the format ('%...') */
				  CharPtr buff = luaL_prepbuffsize(b, MAX_ITEM);  /* to put formatted item */
			      int nb = 0;  /* number of bytes in added item */
			      if (++arg > top)
			        luaL_argerror(L, arg, "no value");
				  strfrmt = scanformat(L, strfrmt, form);
				  char ch = strfrmt[0]; //FIXME:added, move here
				  strfrmt = strfrmt.next(); //FIXME:added, move here
				  switch (ch) {
					  case 'c': {
					    nb = l_sprintf(buff, MAX_ITEM, form, (int)luaL_checkinteger(L, arg));
						break;
					  }
					  case 'd': case 'i':
					  case 'o': case 'u': case 'x': case 'X': {
				        lua_Integer n = luaL_checkinteger(L, arg);
				        addlenmod(form, LUA_INTEGER_FRMLEN);
				        nb = l_sprintf(buff, MAX_ITEM, form, (LUAI_UACINT)n);
					    break;
					  }
                      case 'a': case 'A':
				        addlenmod(form, LUA_NUMBER_FRMLEN);
				        nb = lua_number2strx(L, buff, MAX_ITEM, form, 
						                        luaL_checknumber(L, arg));
				        break;
					  case 'e': case 'E': case 'f':
					  case 'g':  case 'G':  {
          				lua_Number n = luaL_checknumber(L, arg);					  
				        addlenmod(form, LUA_NUMBER_FRMLEN);
				        nb = l_sprintf(buff, MAX_ITEM, form, (LUAI_UACNUMBER)n);
					    break;
					  }
					  case 'q': {
					    addliteral(L, b, arg);
						break;
					  }
					  case 's': {
					    uint l;
						CharPtr s = luaL_tolstring(L, arg, out l);
			            if (form[2] == '\0')  /* no modifiers? */
			              luaL_addvalue(b);  /* keep entire string */
			            else {
			              luaL_argcheck(L, l == strlen(s), arg, "string contains zeros");						
						  if ((strchr(form, '.') == null) && l >= 100) {
						    /* no precision and string is too long to be formatted */
						    luaL_addvalue(b);  /* keep entire string */
					      }
						  else {
						    nb = l_sprintf(buff, MAX_ITEM, form, s);
                            lua_pop(L, 1);  /* remove result from 'luaL_tolstring' */
						  }
						}
						break;
					  }
					  default: {  /* also treat cases 'pnLlh' */
					    return luaL_error(L, "invalid option '%%%c' to 'format'",
						                     strfrmt[-1]);
					  }
				  }
				  lua_assert(nb < MAX_ITEM);
				  luaL_addsize(b, (uint)nb); //FIXME:changed, (uint)
			  }
		  }
		  luaL_pushresult(b);
		  return 1;
		}

		/* }====================================================== */


		/*
		** {======================================================
		** PACK/UNPACK
		** =======================================================
		*/


		/* value used for padding */
		//#if !defined(LUAL_PACKPADBYTE)
		private const char LUAL_PACKPADBYTE = (char)0x00;
		//#endif

		/* maximum size for the binary representation of an integer */
		private const int MAXINTSIZE = 16;

		/* number of bits in a character */
		private const int NB = 8;//#define NB	CHAR_BIT

		/* mask for one character (NB 1's) */
		private const int MC = ((1 << NB) - 1);

		/* size of a lua_Integer */
		private const int SZINT = 4;//#define SZINT	((int)sizeof(lua_Integer))


		//FIXME:x86 always little endian
		/* dummy union to get native endianness */
		private class nativeendian_union {
		  public int dummy;
		  public char little = (char)1;  /* true iff machine is little endian */
		  
		  public nativeendian_union(int dummy)
		  {
		  	this.dummy = dummy;
		  }
		};
		private static nativeendian_union nativeendian = new nativeendian_union(1);		


		/* dummy structure to get native alignment requirements */
		//struct cD {
		//  char c;
		//  union { double d; void *p; lua_Integer i; lua_Number n; } u;
		//};

		private const int MAXALIGN = 1; //#define MAXALIGN	(offsetof(struct cD, u))


		/*
		** Union for serializing floats
		*/
		private class Ftypes {
		  public float f;
		  public double d;
		  public lua_Number n;
		  public char[] buff = new char[5 * 4/*sizeof(lua_Number)*/];  /* enough for any float type */
		};


		/*
		** information to pack/unpack stuff
		*/
		private class Header {
		  public lua_State L;
		  public int islittle;
		  public int maxalign;
		};


		/*
		** options for pack/unpack
		*/
		private enum KOption {
		  Kint,		/* signed integers */
		  Kuint,	/* unsigned integers */
		  Kfloat,	/* floating-point numbers */
		  Kchar,	/* fixed-length strings */
		  Kstring,	/* strings with prefixed length */
		  Kzstr,	/* zero-terminated strings */
		  Kpadding,	/* padding */
		  Kpaddalign,	/* padding for alignment */
		  Knop		/* no-op (configuration or spaces) */
		};


		/*
		** Read an integer numeral from string 'fmt' or return 'df' if
		** there is no numeral
		*/
		private static int digit (int c) { return ('0' <= c && c <= '9')?1:0; }

		private static int getnum (ref CharPtr fmt, int df) {
		  if (0==digit(fmt[0]))  /* no number? */
		    return df;  /* return default value */
		  else {
		    int a = 0;
		    do {
		      a = a*10 + (fmt[0] - '0'); fmt.inc();
		    } while (0!=digit(fmt[0]) && a <= ((int)MAXSIZE - 9)/10);
		    return a;
		  }
		}


		/*
		** Read an integer numeral and raises an error if it is larger
		** than the maximum size for integers.
		*/
		private static int getnumlimit (Header h, ref CharPtr fmt, int df) {
		  int sz = getnum(ref fmt, df);
		  if (sz > MAXINTSIZE || sz <= 0)
		    luaL_error(h.L, "integral size (%d) out of limits [1,%d]", 
			                sz, MAXINTSIZE);
		  return sz;
		}


		/*
		** Initialize Header
		*/
		private static void initheader (lua_State L, Header h) {
		  h.L = L;
		  h.islittle = nativeendian.little;
		  h.maxalign = 1;
		}


		/*
		** Read and classify next option. 'size' is filled with option's size.
		*/
		private static KOption getoption (Header h, ref CharPtr fmt, ref int size) {
		  int opt = fmt[0]; fmt.inc();
		  size = 0;  /* default */
		  switch (opt) {
		    case 'b': size = GetUnmanagedSize(typeof(char)); return KOption.Kint;
		    case 'B': size = GetUnmanagedSize(typeof(char)); return KOption.Kuint;
		    case 'h': size = GetUnmanagedSize(typeof(short)); return KOption.Kint;
		    case 'H': size = GetUnmanagedSize(typeof(short)); return KOption.Kuint;
		    case 'l': size = GetUnmanagedSize(typeof(long)); return KOption.Kint;
		    case 'L': size = GetUnmanagedSize(typeof(long)); return KOption.Kuint;
		    case 'j': size = GetUnmanagedSize(typeof(lua_Integer)); return KOption.Kint;
		    case 'J': size = GetUnmanagedSize(typeof(lua_Integer)); return KOption.Kuint;
		    case 'T': size = GetUnmanagedSize(typeof(uint)); return KOption.Kuint;
		    case 'f': size = GetUnmanagedSize(typeof(float)); return KOption.Kfloat;
		    case 'd': size = GetUnmanagedSize(typeof(double)); return KOption.Kfloat;
		    case 'n': size = GetUnmanagedSize(typeof(lua_Number)); return KOption.Kfloat;
		    case 'i': size = getnumlimit(h, ref fmt, GetUnmanagedSize(typeof(int))); return KOption.Kint;
		    case 'I': size = getnumlimit(h, ref fmt, GetUnmanagedSize(typeof(int))); return KOption.Kuint;
		    case 's': size = getnumlimit(h, ref fmt, GetUnmanagedSize(typeof(uint))); return KOption.Kstring;
		    case 'c':
		      size = getnum(ref fmt, -1);
		      if (size == -1)
		        luaL_error(h.L, "missing size for format option 'c'");
		      return KOption.Kchar;
		    case 'z': return KOption.Kzstr;
		    case 'x': size = 1; return KOption.Kpadding;
		    case 'X': return KOption.Kpaddalign;
		    case ' ': break;
		    case '<': h.islittle = 1; break;
		    case '>': h.islittle = 0; break;
		    case '=': h.islittle = nativeendian.little; break;
		    case '!': h.maxalign = getnumlimit(h, ref fmt, MAXALIGN); break;
		    default: luaL_error(h.L, "invalid format option '%c'", opt); break;
		  }
		  return KOption.Knop;
		}


		/*
		** Read, classify, and fill other details about the next option.
		** 'psize' is filled with option's size, 'notoalign' with its
		** alignment requirements.
		** Local variable 'size' gets the size to be aligned. (Kpadal option
		** always gets its full alignment, other options are limited by
		** the maximum alignment ('maxalign'). Kchar option needs no alignment
		** despite its size.
		*/
		private static KOption getdetails (Header h, uint totalsize,
		                           ref CharPtr fmt, ref int psize, ref int ntoalign) {
		  KOption opt = getoption(h, ref fmt, ref psize);
		  int align = psize;  /* usually, alignment follows size */
		  if (opt == KOption.Kpaddalign) {  /* 'X' gets alignment from following option */
		  	if (fmt[0] == '\0' || getoption(h, ref fmt, ref align) == KOption.Kchar || align == 0)
		      luaL_argerror(h.L, 1, "invalid next option for option 'X'");
		  }
		  if (align <= 1 || opt == KOption.Kchar)  /* need no alignment? */
		    ntoalign = 0;
		  else {
		    if (align > h.maxalign)  /* enforce maximum alignment */
		      align = h.maxalign;
		    if ((align & (align - 1)) != 0)  /* is 'align' not a power of 2? */
		      luaL_argerror(h.L, 1, "format asks for alignment not power of 2");
		    ntoalign = (align - (int)(totalsize & (align - 1))) & (align - 1);
		  }
		  return opt;
		}


		/*
		** Pack integer 'n' with 'size' bytes and 'islittle' endianness.
		** The final 'if' handles the case when 'size' is larger than
		** the size of a Lua integer, correcting the extra sign-extension
		** bytes if necessary (by default they would be zeros).
		*/
		private static void packint (luaL_Buffer b, lua_Unsigned n,
		                     int islittle, int size, int neg) {
		  CharPtr buff = luaL_prepbuffsize(b, (uint)size);
		  int i;
		  buff[0!=islittle ? 0 : size - 1] = (char)(n & MC);  /* first byte */
		  for (i = 1; i < size; i++) {
		    n >>= NB;
		    buff[0!=islittle ? i : size - 1 - i] = (char)(n & MC);
		  }
		  if (0!=neg && size > SZINT) {  /* negative number need sign extension? */
		    for (i = SZINT; i < size; i++)  /* correct extra bytes */
		      buff[0!=islittle ? i : size - 1 - i] = (char)MC;
		  }
		  luaL_addsize(b, (uint)size);  /* add result to buffer */
		}


		/*
		** Copy 'size' bytes from 'src' to 'dest', correcting endianness if
		** given 'islittle' is different from native endianness.
		*/
		private static void copywithendian (/*volatile*/ CharPtr dest, /*volatile*/ CharPtr src,
		                            int size, int islittle) {
		  dest = new CharPtr(dest); //FIXME:???
		  src = new CharPtr(src); //FIXME:???
		  if (islittle == nativeendian.little) {
		  	while (size-- != 0) {
		  	  dest[0] = src[0]; dest.inc(); src.inc();
		  	}
		  }
		  else {
		    dest += size - 1;
		    while (size-- != 0) {
		      dest[0] = src[0]; dest.dec(); src.inc();
		    }
		  }
		}


		private static int str_pack (lua_State L) {
		  luaL_Buffer b = new luaL_Buffer();
		  Header h = new Header();
		  CharPtr fmt = luaL_checkstring(L, 1);  /* format string */
		  int arg = 1;  /* current argument to pack */
		  uint totalsize = 0;  /* accumulate total size of result */
		  initheader(L, h);
		  lua_pushnil(L);  /* mark to separate arguments from string buffer */
		  luaL_buffinit(L, b);
		  while (fmt[0] != '\0') {
		    int size = 0, ntoalign = 0;
		    KOption opt = getdetails(h, totalsize, ref fmt, ref size, ref ntoalign);
		    totalsize = (uint)(totalsize + ntoalign + size);//totalsize += ntoalign + size; //FIXME:???
		    while (ntoalign-- > 0)
     		  luaL_addchar(b, LUAL_PACKPADBYTE);  /* fill alignment */
		    arg++;
		    switch (opt) {
		      case KOption.Kint: {  /* signed integers */
		        lua_Integer n = luaL_checkinteger(L, arg);
		        if (size < SZINT) {  /* need overflow check? */
		          lua_Integer lim = (lua_Integer)1 << ((size * NB) - 1);
		          luaL_argcheck(L, -lim <= n && n < lim, arg, "integer overflow");
		        }
		        packint(b, (lua_Unsigned)n, h.islittle, size, (n < 0)?1:0);
		        break;
		      }
		      case KOption.Kuint: {  /* unsigned integers */
		        lua_Integer n = luaL_checkinteger(L, arg);
		        if (size < SZINT)  /* need overflow check? */
		          luaL_argcheck(L, (lua_Unsigned)n < ((lua_Unsigned)1 << (size * NB)),
		                           arg, "unsigned overflow");
		        packint(b, (lua_Unsigned)n, h.islittle, size, 0);
		        break;
		      }
		      case KOption.Kfloat: {  /* floating-point options */
		    	/*volatile */Ftypes u = new Ftypes();
		    	CharPtr buff = luaL_prepbuffsize(b, (uint)size);
		        lua_Number n = luaL_checknumber(L, arg);  /* get argument */
		        if (size == GetUnmanagedSize(u.f.GetType())) u.f = (float)n;  /* copy it into 'u' */
		        else if (size == GetUnmanagedSize(u.d.GetType())) u.d = (double)n;
		        else u.n = n;
		        /* move 'u' to final result, correcting endianness if needed */
		        copywithendian(buff, u.buff, size, h.islittle);
		        luaL_addsize(b, (uint)size);
		        break;
		      }
		      case KOption.Kchar: {  /* fixed-size string */
		        uint len;
		        CharPtr s = luaL_checklstring(L, arg, out len);
		        luaL_argcheck(L, len <= (uint)size, arg,
                  				"string longer than given size");
		        luaL_addlstring(b, s, len);  /* add string */
		        while (len++ < (uint)size)  /* pad extra space */
		          luaL_addchar(b, LUAL_PACKPADBYTE);
		        break;
		      }
		      case KOption.Kstring: {  /* strings with length count */
		        uint len;
		        CharPtr s = luaL_checklstring(L, arg, out len);
		        luaL_argcheck(L, size >= (int)GetUnmanagedSize(typeof(uint)) ||
		                         len < ((uint)1 << (size * NB)),
		                         arg, "string length does not fit in given size");
		        packint(b, (lua_Unsigned)len, h.islittle, size, 0);  /* pack length */
		        luaL_addlstring(b, s, len);
		        totalsize += len;
		        break;
		      }
		      case KOption.Kzstr: {  /* zero-terminated string */
		        uint len;
		        CharPtr s = luaL_checklstring(L, arg, out len);
		        luaL_argcheck(L, strlen(s) == len, arg, "string contains zeros");
		        luaL_addlstring(b, s, len);
		        luaL_addchar(b, '\0');  /* add zero at the end */
		        totalsize += len + 1;
		        break;
		      }
		      case KOption.Kpadding: luaL_addchar(b, LUAL_PACKPADBYTE);  /* FALLTHROUGH */
		      	goto case KOption.Kpaddalign; //FIXME:added
		      case KOption.Kpaddalign: case KOption.Knop:
		        arg--;  /* undo increment */
		        break;
		    }
		  }
		  luaL_pushresult(b);
		  return 1;
		}


		private static int str_packsize (lua_State L) {
		  Header h = new Header();
		  CharPtr fmt = luaL_checkstring(L, 1);  /* format string */
		  uint totalsize = 0;  /* accumulate total size of result */
		  initheader(L, h);
		  while (fmt[0] != '\0') {
		    int size = 0, ntoalign = 0;
		    KOption opt = getdetails(h, totalsize, ref fmt, ref size, ref ntoalign);
		    size += ntoalign;  /* total space used by option */
		    luaL_argcheck(L, totalsize <= MAXSIZE - size, 1,
		                     "format result too large");
		    totalsize = (uint)(totalsize + size); //totalsize += size;//FIXME:
		    switch (opt) {
		      case KOption.Kstring:  /* strings with length count */
		      case KOption.Kzstr:    /* zero-terminated string */
		        luaL_argerror(L, 1, "variable-length format");
		        /* call never return, but to avoid warnings: *//* FALLTHROUGH */
				goto default;
		      default:  break;
		    }
		  }
		  lua_pushinteger(L, (lua_Integer)totalsize);
		  return 1;
		}


		/*
		** Unpack an integer with 'size' bytes and 'islittle' endianness.
		** If size is smaller than the size of a Lua integer and integer
		** is signed, must do sign extension (propagating the sign to the
		** higher bits); if size is larger than the size of a Lua integer,
		** it must check the unread bytes to see whether they do not cause an
		** overflow.
		*/
		private static lua_Integer unpackint (lua_State L, CharPtr str,
		                              int islittle, int size, int issigned) {
		  lua_Unsigned res = 0;
		  int i;
		  int limit = (size  <= SZINT) ? size : SZINT;
		  for (i = limit - 1; i >= 0; i--) {
		    res <<= NB;
		    res |= (lua_Unsigned)(byte)str[0!=islittle ? i : size - 1 - i];
		  }
		  if (size < SZINT) {  /* real size smaller than lua_Integer? */
		    if (0!=issigned) {  /* needs sign extension? */
		      lua_Unsigned mask = (lua_Unsigned)1 << (size*NB - 1);
		      res = ((res ^ mask) - mask);  /* do sign extension */
		    }
		  }
		  else if (size > SZINT) {  /* must check unread bytes */
		    int mask = (0==issigned || (lua_Integer)res >= 0) ? 0 : MC;
		    for (i = limit; i < size; i++) {
		      if ((byte)str[0!=islittle ? i : size - 1 - i] != mask)
		        luaL_error(L, "%d-byte integer does not fit into Lua Integer", size);
		    }
		  }
		  return (lua_Integer)res;
		}


		private static int str_unpack (lua_State L) {
		  Header h = new Header();
		  CharPtr fmt = luaL_checkstring(L, 1);
		  uint ld;
		  CharPtr data = luaL_checklstring(L, 2, out ld);
		  uint pos = (uint)posrelat(luaL_optinteger(L, 3, 1), ld) - 1;
		  int n = 0;  /* number of results */
		  luaL_argcheck(L, pos <= ld, 3, "initial position out of string");
		  initheader(L, h);
		  while (fmt[0] != '\0') {
		    int size = 0, ntoalign = 0;
		    KOption opt = getdetails(h, pos, ref fmt, ref size, ref ntoalign);
		    if ((uint)ntoalign + size > ~pos || pos + ntoalign + size > ld)
		      luaL_argerror(L, 2, "data string too short");
		    pos = (uint)(pos + ntoalign);  /* skip alignment */ //pos += ntoalign; //FIXME:
		    /* stack space for item + next position */
		    luaL_checkstack(L, 2, "too many results");
		    n++;
		    switch (opt) {
		      case KOption.Kint:
		      case KOption.Kuint: {
		        lua_Integer res = unpackint(L, data + pos, h.islittle, size,
		    			                            (opt == KOption.Kint)?1:0);
		        lua_pushinteger(L, res);
		        break;
		      }
		      case KOption.Kfloat: {
		    	/*volatile*/ Ftypes u = new Ftypes();
		        lua_Number num;
		        copywithendian(u.buff, data + pos, size, h.islittle);
		        if (size == GetUnmanagedSize(u.f.GetType())) num = (lua_Number)u.f;
		        else if (size == GetUnmanagedSize(u.d.GetType())) num = (lua_Number)u.d;
		        else num = u.n;
		        lua_pushnumber(L, num);
		        break;
		      }
		      case KOption.Kchar: {
		    	lua_pushlstring(L, data + pos, (uint)size);
		        break;
		      }
		      case KOption.Kstring: {
		        uint len = (uint)unpackint(L, data + pos, h.islittle, size, 0);
		        luaL_argcheck(L, pos + len + size <= ld, 2, "data string too short");
		        lua_pushlstring(L, data + pos + size, len);
		        pos += len;  /* skip string */
		        break;
		      }
		      case KOption.Kzstr: {
		    	uint len = (uint)(int)strlen(data + pos);
		        lua_pushlstring(L, data + pos, len);
		        pos += len + 1;  /* skip string plus final '\0' */
		        break;
		      }
		      case KOption.Kpaddalign: case KOption.Kpadding: case KOption.Knop:
		        n--;  /* undo increment */
		        break;
		    }
		    pos = (uint)(pos + size); //pos += ntoalign; //FIXME:
		  }
		  lua_pushinteger(L, (int)(pos + 1));  /* next position */
		  return n + 1;
		}

		/* }====================================================== */


		private readonly static luaL_Reg[] strlib = {
		  new luaL_Reg("byte", str_byte),
		  new luaL_Reg("char", str_char),
		  new luaL_Reg("dump", str_dump),
		  new luaL_Reg("find", str_find),
		  new luaL_Reg("format", str_format),
		  new luaL_Reg("gmatch", gmatch),
		  new luaL_Reg("gsub", str_gsub),
		  new luaL_Reg("len", str_len),
		  new luaL_Reg("lower", str_lower),
		  new luaL_Reg("match", str_match),
		  new luaL_Reg("rep", str_rep),
		  new luaL_Reg("reverse", str_reverse),
		  new luaL_Reg("sub", str_sub),
		  new luaL_Reg("upper", str_upper),
		  new luaL_Reg("pack", str_pack),
		  new luaL_Reg("packsize", str_packsize),
  		  new luaL_Reg("unpack", str_unpack),		  
		  new luaL_Reg(null, null)
		};


		private static void createmetatable (lua_State L) {
		  lua_createtable(L, 0, 1);  /* table to be metatable for strings */
		  lua_pushliteral(L, "");  /* dummy string */
		  lua_pushvalue(L, -2);  /* copy table */
		  lua_setmetatable(L, -2);  /* set table as metatable for strings */
		  lua_pop(L, 1);  /* pop dummy string */
		  lua_pushvalue(L, -2);  /* get string library */
		  lua_setfield(L, -2, "__index");  /* metatable.__index = string */
		  lua_pop(L, 1);  /* pop metatable */
		}


		/*
		** Open string library
		*/
		public static int luaopen_string (lua_State L) {
		  luaL_newlib(L, strlib);
		  createmetatable(L);
		  return 1;
		}

	}
}

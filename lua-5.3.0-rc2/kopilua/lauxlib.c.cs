/*
** $Id: lauxlib.c,v 1.279 2014/12/14 18:32:26 roberto Exp $
** Auxiliary functions for building Lua libraries
** See Copyright Notice in lua.h
*/

#define LUA_COMPAT_MOD

#define lauxlib_c
#define LUA_LIB

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KopiLua
{
	using lua_Number = System.Double;
	using lua_Integer = System.Int32;
	using lua_Unsigned = System.UInt32;

	public partial class Lua
	{
		/*
		** {======================================================
		** Traceback
		** =======================================================
		*/


		private const int LEVELS1 = 12;	/* size of the first part of the stack */
		private const int LEVELS2 = 10;	/* size of the second part of the stack */
		/*
		** search for 'objidx' in table at index -1.
		** return 1 + string at top if find a good name.
		*/
		private static int findfield (lua_State L, int objidx, int level) {
		  if (level == 0 || !lua_istable(L, -1))
		    return 0;  /* not found */
		  lua_pushnil(L);  /* start 'next' loop */
		  while (lua_next(L, -2) != 0) {  /* for each pair in table */
		    if (lua_type(L, -2) == LUA_TSTRING) {  /* ignore non-string keys */
		      if (lua_rawequal(L, objidx, -1) != 0) {  /* found object? */
		        lua_pop(L, 1);  /* remove value (but keep name) */
		        return 1;
		      }
		      else if (findfield(L, objidx, level - 1) != 0) {  /* try recursively */
		        lua_remove(L, -2);  /* remove table (but keep name) */
		        lua_pushliteral(L, ".");
		        lua_insert(L, -2);  /* place '.' between the two names */
		        lua_concat(L, 3);
		        return 1;
		      }
		    }
		    lua_pop(L, 1);  /* remove value */
		  }
		  return 0;  /* not found */
		}


		/*
		** Search for a name for a function in all loaded modules
		** (registry._LOADED).
		*/
		private static int pushglobalfuncname (lua_State L, lua_Debug ar) {
		  int top = lua_gettop(L);
		  lua_getinfo(L, "f", ar);  /* push function */
		  lua_getfield(L, LUA_REGISTRYINDEX, "_LOADED");
		  if (findfield(L, top + 1, 2) != 0) {
		    CharPtr name = lua_tostring(L, -1);
		    if (strncmp(name, "_G.", 3) == 0) {  /* name start with '_G.'? */
		      lua_pushstring(L, name + 3);  /* push name without prefix */
		      lua_remove(L, -2);  /* remove original name */
		    }		  
		    lua_copy(L, -1, top + 1);  /* move name to proper place */
		    lua_pop(L, 2);  /* remove pushed values */
		    return 1;
		  }
		  else {
		    lua_settop(L, top);  /* remove function and global table */
		    return 0;
		  }
		}


		private static void pushfuncname (lua_State L, lua_Debug ar) {
		  if (0!=pushglobalfuncname(L, ar)) {  /* try first a global name */
		    lua_pushfstring(L, "function '%s'", lua_tostring(L, -1));
		    lua_remove(L, -2);  /* remove name */
		  }
		  else if (ar.namewhat[0] != '\0')  /* is there a name from code? */
		    lua_pushfstring(L, "%s '%s'", ar.namewhat, ar.name);  /* use it */
		  else if (ar.what[0] == 'm')  /* main? */
		      lua_pushliteral(L, "main chunk");
		  else if (ar.what[0] != 'C')  /* for Lua functions, use <file:line> */
		    lua_pushfstring(L, "function <%s:%d>", ar.short_src, ar.linedefined);
		  else  /* nothing left... */
		    lua_pushliteral(L, "?");
		}


		static int countlevels (lua_State L) {
		  lua_Debug ar = new lua_Debug();
		  int li = 1, le = 1;
		  /* find an upper bound */
		  while (lua_getstack(L, le, ar) != 0) { li = le; le *= 2; }
		  /* do a binary search */
		  while (li < le) {
		    int m = (li + le)/2;
		    if (lua_getstack(L, m, ar) != 0) li = m + 1;
		    else le = m;
		  }
		  return le - 1;
		}


		public static void luaL_traceback (lua_State L, lua_State L1,
		                                CharPtr msg, int level) {
		  lua_Debug ar = new lua_Debug();
		  int top = lua_gettop(L);
		  int numlevels = countlevels(L1);
		  int mark = (numlevels > LEVELS1 + LEVELS2) ? LEVELS1 : 0;
		  if (msg != null) lua_pushfstring(L, "%s\n", msg);
		  lua_pushliteral(L, "stack traceback:");
		  while (lua_getstack(L1, level++, ar) != 0) {
		    if (level == mark) {  /* too many levels? */
		      lua_pushliteral(L, "\n\t...");  /* add a '...' */
		      level = numlevels - LEVELS2;  /* and skip to last ones */
		    }
		    else {
		      lua_getinfo(L1, "Slnt", ar);
		      lua_pushfstring(L, "\n\t%s:", ar.short_src);
		      if (ar.currentline > 0)
		        lua_pushfstring(L, "%d:", ar.currentline);
		      lua_pushliteral(L, " in ");
		      pushfuncname(L, ar);
		      if (ar.istailcall != 0)
		        lua_pushliteral(L, "\n\t(...tail calls...)");
		      lua_concat(L, lua_gettop(L) - top);
		    }
		  }
		  lua_concat(L, lua_gettop(L) - top);
		}

		/* }====================================================== */


		/*
		** {======================================================
		** Error-report functions
		** =======================================================
		*/

		public static int luaL_argerror (lua_State L, int arg, CharPtr extramsg) {
		  lua_Debug ar = new lua_Debug();
		  if (lua_getstack(L, 0, ar)==0)  /* no stack frame? */
		    return luaL_error(L, "bad argument #%d (%s)", arg, extramsg);
		  lua_getinfo(L, "n", ar);
		  if (strcmp(ar.namewhat, "method") == 0) {
		    arg--;  /* do not count 'self' */
		    if (arg == 0)  /* error is in the self argument itself? */
		      return luaL_error(L, "calling '%s' on bad self (%s)",
			  						ar.name, extramsg);
		  }
		  if (ar.name == null)
		    ar.name = (pushglobalfuncname(L, ar) != 0) ? lua_tostring(L, -1) : "?";
		  return luaL_error(L, "bad argument #%d to '%s' (%s)",
		                        arg, ar.name, extramsg);
		}


		private static int typeerror (lua_State L, int arg, CharPtr tname) {
		  CharPtr msg;
		  CharPtr typearg;  /* name for the type of the actual argument */
		  if (luaL_getmetafield(L, arg, "__name") == LUA_TSTRING)
		    typearg = lua_tostring(L, -1);  /* use the given type name */
		  else if (lua_type(L, arg) == LUA_TLIGHTUSERDATA)
		    typearg = "light userdata";  /* special name for messages */
		  else
		    typearg = luaL_typename(L, arg);  /* standard name */
		  msg = lua_pushfstring(L, "%s expected, got %s", tname, typearg);
		  return luaL_argerror(L, arg, msg);
		}


		private static void tag_error (lua_State L, int arg, int tag) {
		  typeerror(L, arg, lua_typename(L, tag));
		}


		public static void luaL_where (lua_State L, int level) {
		  lua_Debug ar = new lua_Debug();
		  if (lua_getstack(L, level, ar) != 0) {  /* check function at level */
		    lua_getinfo(L, "Sl", ar);  /* get info about it */
		    if (ar.currentline > 0) {  /* is there info? */
		      lua_pushfstring(L, "%s:%d: ", ar.short_src, ar.currentline);
		      return;
		    }
		  }
		  lua_pushliteral(L, "");  /* else, no information available... */
		}


		public static int luaL_error (lua_State L, CharPtr fmt, params object[] argp) {
		  //va_list argp;
		  //va_start(argp, fmt);
		  luaL_where(L, 1);
		  lua_pushvfstring(L, fmt, argp);
		  //va_end(argp);
		  lua_concat(L, 2);
		  return lua_error(L);
		}


		public static int luaL_fileresult (lua_State L, int stat, CharPtr fname) {
		  int en = errno;  /* calls to Lua API may change this value */
		  if (stat != 0) {
		    lua_pushboolean(L, 1);
		    return 1;
		  }
		  else {
		    lua_pushnil(L);
		    if (fname != null)
		      lua_pushfstring(L, "%s: %s", fname, strerror(en));
		    else
		      lua_pushstring(L, strerror(en));
		    lua_pushinteger(L, en);
		    return 3;
		  }
		}


		//#if !defined(l_inspectstat)	/* { */

		//#if defined(LUA_USE_POSIX)

		//#include <sys/wait.h>

		/*
		** use appropriate macros to interpret 'pclose' return status
		*/
		//#define l_inspectstat(stat,what)  \
		//   if (WIFEXITED(stat)) { stat = WEXITSTATUS(stat); } \
		//   else if (WIFSIGNALED(stat)) { stat = WTERMSIG(stat); what = "signal"; }

		//#else

		private static void l_inspectstat(int stat, ref CharPtr what) { /* no op */ }

		//#endif

		//#endif				/* } */


		public static int luaL_execresult (lua_State L, int stat) {
		  CharPtr what = "exit";  /* type of termination */
		  if (stat == -1)  /* error? */
		    return luaL_fileresult(L, 0, null);
		  else {
		    l_inspectstat(stat, ref what);  /* interpret result */
		    if (what[0] == 'e' && stat == 0)  /* successful termination? */
		      lua_pushboolean(L, 1);
		    else
		      lua_pushnil(L);
		    lua_pushstring(L, what);
		    lua_pushinteger(L, stat);
		    return 3;  /* return true/nil,what,code */
		  }
		}


		/* }====================================================== */


		/*
		** {======================================================
		** Userdata's metatable manipulation
		** =======================================================
		*/



		public static int luaL_newmetatable (lua_State L, CharPtr tname) {
		  if (0!=luaL_getmetatable(L, tname))  /* name already in use? */
			return 0;  /* leave previous value on top, but return 0 */
		  lua_pop(L, 1);
		  lua_newtable(L);  /* create metatable */
		  lua_pushstring(L, tname);
		  lua_setfield(L, -2, "__name");  /* metatable.__name = tname */		  
		  lua_pushvalue(L, -1);
		  lua_setfield(L, LUA_REGISTRYINDEX, tname);  /* registry.name = metatable */
		  return 1;
		}


		public static void luaL_setmetatable (lua_State L, CharPtr tname) {
		  luaL_getmetatable(L, tname);
		  lua_setmetatable(L, -2);
		}


		public static object luaL_testudata (lua_State L, int ud, CharPtr tname) {
		  object p = lua_touserdata(L, ud);
		  if (p != null) {  /* value is a userdata? */
			if (lua_getmetatable(L, ud) != 0) {  /* does it have a metatable? */
			  luaL_getmetatable(L, tname);  /* get correct metatable */
			  if (lua_rawequal(L, -1, -2) == 0)  /* not the same? */
                p = null;  /* value is a userdata with wrong metatable */
			  lua_pop(L, 2);  /* remove both metatables */
			  return p;
			}
		  }
		  return null;  /* value is not a userdata with a metatable */
		}


		public static object luaL_checkudata (lua_State L, int ud, CharPtr tname) {
		  object p = luaL_testudata(L, ud, tname);
		  if (p == null) typeerror(L, ud, tname);
		  return p;
		}

		/* }====================================================== */


		/*
		** {======================================================
		** Argument check functions
		** =======================================================
		*/

		public static int luaL_checkoption (lua_State L, int arg, CharPtr def,
										 CharPtr [] lst) {
		  CharPtr name = (def != null) ? luaL_optstring(L, arg, def) :
									 luaL_checkstring(L, arg);
		  int i;
		  for (i=0; i<lst.Length; i++)
			if (strcmp(lst[i], name)==0)
			  return i;
		  return luaL_argerror(L, arg,
							   lua_pushfstring(L, "invalid option '%s'", name));
		}


		public static void luaL_checkstack (lua_State L, int space, CharPtr msg) {
		  /* keep some extra space to run error routines, if needed */
		  /*const */int extra = LUA_MINSTACK;
		  if (lua_checkstack(L, space + extra)==0) {
		    if (msg != null)
		      luaL_error(L, "stack overflow (%s)", msg);
		    else
		      luaL_error(L, "stack overflow");
		  }
		}


		public static void luaL_checktype (lua_State L, int arg, int t) {
		  if (lua_type(L, arg) != t)
			tag_error(L, arg, t);
		}


		public static void luaL_checkany (lua_State L, int arg) {
		  if (lua_type(L, arg) == LUA_TNONE)
			luaL_argerror(L, arg, "value expected");
		}


		public static CharPtr luaL_checklstring(lua_State L, int arg) {uint len; return luaL_checklstring(L, arg, out len);}
		public static CharPtr luaL_checklstring (lua_State L, int arg, out uint len) {
		  CharPtr s = lua_tolstring(L, arg, out len);
		  if (s==null) tag_error(L, arg, LUA_TSTRING);
		  return s;
		}


		public static CharPtr luaL_optlstring (lua_State L, int arg, CharPtr def) {
			uint len; return luaL_optlstring (L, arg, def, out len); }
		public static CharPtr luaL_optlstring (lua_State L, int arg, CharPtr def, out uint len) {
		  if (lua_isnoneornil(L, arg)) {
            //if (len) //FIXME: removed
			len = (uint)((def != null) ? strlen(def) : 0);
			return def;
		  }
		  else return luaL_checklstring(L, arg, out len);
		}


		public static lua_Number luaL_checknumber (lua_State L, int arg) {
          int isnum = 0; //FIXME: changed, (empty)-> =0
		  lua_Number d = lua_tonumberx(L, arg, ref isnum);
		  if (isnum==0)
			tag_error(L, arg, LUA_TNUMBER);
		  return d;
		}


		public static lua_Number luaL_optnumber (lua_State L, int arg, lua_Number def) {
		  return luaL_opt(L, luaL_checknumber, arg, def);
		}


		private static void interror (lua_State L, int arg) {
		  if (0!=lua_isnumber(L, arg))
		    luaL_argerror(L, arg, "number has no integer representation");
		  else
		    tag_error(L, arg, LUA_TNUMBER);
		}

		public static lua_Integer luaL_checkinteger (lua_State L, int arg) {
          int isnum = 0; //FIXME: changed, =0
		  lua_Integer d = lua_tointegerx(L, arg, ref isnum);
		  if (isnum==0) {
			interror(L, arg);
		  }
		  return d;
		}


		public static lua_Integer luaL_optinteger (lua_State L, int arg, 
		                                                        lua_Integer def) {
		  return luaL_opt_integer(L, luaL_checkinteger, arg, def);
		}

        /* }====================================================== */


		/*
		** {======================================================
		** Generic Buffer manipulation
		** =======================================================
		*/

		/*
		** check whether buffer is using a userdata on the stack as a temporary
		** buffer
		*/
		private static int buffonstack(luaL_Buffer B)	{return (B.b != B.initb)?1:0;}


		/*
		** returns a pointer to a free area with at least 'sz' bytes
		*/
		public static CharPtr luaL_prepbuffsize (luaL_Buffer B, uint sz) {
		  lua_State L = B.L;
		  if (B.size - B.n < sz) {  /* not enough space? */
		    CharPtr newbuff;
		    uint newsize = B.size * 2;  /* double buffer size */
		    if (newsize - B.n < sz)  /* not big enough? */
		      newsize = B.n + sz;
		    if (newsize < B.n || newsize - B.n < sz)
		      luaL_error(L, "buffer too large");
            /* create larger buffer */
		    newbuff = (CharPtr)lua_newuserdata(L, newsize * 1); //FIXME:changed, sizeof(char)
            /* move content to new buffer */
		    memcpy(newbuff, B.b, B.n * 1); //FIXME:changed, sizeof(char)
		    if (buffonstack(B)!=0)
		      lua_remove(L, -2);  /* remove old buffer */
		    B.b = newbuff;
		    B.size = newsize;
		  }
		  return new CharPtr(B.b, (int)B.n); //FIXME:???B.b[B.n] //FIXME:(int)
		}


		public static void luaL_addlstring (luaL_Buffer B, CharPtr s, uint l) {
		  CharPtr b = luaL_prepbuffsize(B, l);
		  memcpy(b, s, l * 1); //FIXME:changed, sizeof(char)
		  luaL_addsize(B, l);
		}


		public static void luaL_addstring (luaL_Buffer B, CharPtr s) {
		  luaL_addlstring(B, s, (uint)strlen(s));
		}


		public static void luaL_pushresult (luaL_Buffer B) {
		  lua_State L = B.L;
		  lua_pushlstring(L, B.b, B.n);
		  if (buffonstack(B)!=0)
		    lua_remove(L, -2);  /* remove old buffer */
		}


		public static void luaL_pushresultsize (luaL_Buffer B, uint sz) {
		  luaL_addsize(B, sz);
		  luaL_pushresult(B);
		}


		public static void luaL_addvalue (luaL_Buffer B) {
		  lua_State L = B.L;
		  uint l;
		  CharPtr s = lua_tolstring(L, -1, out l);
		  if (buffonstack(B)!=0)
		    lua_insert(L, -2);  /* put value below buffer */
		  luaL_addlstring(B, s, l);
		  lua_remove(L, (buffonstack(B)!=0) ? -2 : -1);  /* remove value */
		}


		public static void luaL_buffinit (lua_State L, luaL_Buffer B) {
		  B.L = L;
		  B.b = B.initb;
		  B.n = 0;
		  B.size = LUAL_BUFFERSIZE;
		}


		public static CharPtr luaL_buffinitsize (lua_State L, luaL_Buffer B, uint sz) {
		  luaL_buffinit(L, B);
		  return luaL_prepbuffsize(B, sz);
		}

		/* }====================================================== */


		/*
		** {======================================================
		** Reference system
		** =======================================================
		*/

		/* index of free-list header */
		private const int freelist = 0;


		public static int luaL_ref (lua_State L, int t) {
		  int ref_;
		  if (lua_isnil(L, -1)) {
			lua_pop(L, 1);  /* remove from stack */
			return LUA_REFNIL;  /* 'nil' has a unique fixed reference */
		  }
		  t = lua_absindex(L, t);
		  lua_rawgeti(L, t, freelist);  /* get first free element */
		  ref_ = (int)lua_tointeger(L, -1);  /* ref = t[freelist] */
		  lua_pop(L, 1);  /* remove it from stack */
		  if (ref_ != 0) {  /* any free element? */
			lua_rawgeti(L, t, ref_);  /* remove it from list */
			lua_rawseti(L, t, freelist);  /* (t[freelist] = t[ref]) */
		  }
		  else  /* no free elements */
		    ref_ = (int)lua_rawlen(L, t) + 1;  /* get a new reference */
		  lua_rawseti(L, t, ref_);
		  return ref_;
		}


		public static void luaL_unref (lua_State L, int t, int ref_) {
		  if (ref_ >= 0) {
			t = lua_absindex(L, t);
			lua_rawgeti(L, t, freelist);
			lua_rawseti(L, t, ref_);  /* t[ref] = t[freelist] */
			lua_pushinteger(L, ref_);
			lua_rawseti(L, t, freelist);  /* t[freelist] = ref */
		  }
		}

        /* }====================================================== */
		

		/*
		** {======================================================
		** Load functions
		** =======================================================
		*/

		public class LoadF {
		  public int n;  /* number of pre-read characters */
		  public StreamProxy f;  /* file being read */
		  public CharPtr buff = new char[BUFSIZ];  /* area for reading file */
		};


		public static CharPtr getF (lua_State L, object ud, out uint size) {
		  size = 0; //FIXME:added
		  LoadF lf = (LoadF)ud;
		  //(void)L;  /* not used */
		  if (lf.n > 0) {  /* are there pre-read characters to be read? */
		  	size = (uint)lf.n;  /* return them (chars already in buffer) */ //FIXME:(uint)
		    lf.n = 0;  /* no more pre-read characters */
		  }
          else {  /* read a block from file */
			/* 'fread' can return > 0 *and* set the EOF flag. If next call to
		       'getF' called 'fread', it might still wait for user input.
		       The next check avoids this problem. */
			if (feof(lf.f) != 0) return null;
			size = (uint)fread(lf.buff, 1, lf.buff.chars.Length, lf.f);  /* read block */
		  }
		  return (size > 0) ? new CharPtr(lf.buff) : null; //FIXME:changed
		}


		private static int errfile (lua_State L, CharPtr what, int fnameindex) {
		  CharPtr serr = strerror(errno);
		  CharPtr filename = lua_tostring(L, fnameindex) + 1;
		  lua_pushfstring(L, "cannot %s %s: %s", what, filename, serr);
		  lua_remove(L, fnameindex);
		  return LUA_ERRFILE;
		}


		private static int skipBOM (LoadF lf) {
		  CharPtr p = "\xEF\xBB\xBF";  /* Utf8 BOM mark */
		  int c;
		  lf.n = 0;
		  do {
		    c = getc(lf.f);
		    //if (c == EOF || c != *(const unsigned char *)p++) return c; //FIXME:changed
		    if (c == EOF)
		    {
		    	return c;
		    }
		    else if (c != (byte)p[0])
		    {
		    	p = p + 1;
		    	return c;
		    }
		    p = p + 1;
		    
		    lf.buff[lf.n++] = (char)c;  /* to be read by the parser */ //FIXME:added, (char)
		  } while (p[0] != '\0');
		  lf.n = 0;  /* prefix matched; discard it */
		  return getc(lf.f);  /* return next character */
		}


		/*
		** reads the first character of file 'f' and skips an optional BOM mark
		** in its beginning plus its first line if it starts with '#'. Returns
		** true if it skipped the first line.  In any case, '*cp' has the
		** first "valid" character of the file (after the optional BOM and
		** a first-line comment).
		*/
		private static int skipcomment (LoadF lf, ref int cp) {
		  int c = (cp = skipBOM(lf));
		  if (c == '#') {  /* first line is a comment (Unix exec. file)? */
		    do {  /* skip first line */
		      c = getc(lf.f);
		    } while (c != EOF && c != '\n') ;
		    cp = getc(lf.f);  /* skip end-of-line, if present */
		    return 1;  /* there was a comment */
		  }
		  else return 0;  /* no comment */
		}


		public static int luaL_loadfilex (lua_State L, CharPtr filename,
		                                               CharPtr mode) {
		  LoadF lf = new LoadF();
		  int status, readstatus;
		  int c = 0; //FIXME: added, =0
		  int fnameindex = lua_gettop(L) + 1;  /* index of filename on the stack */
		  if (filename == null) {
			lua_pushliteral(L, "=stdin");
			lf.f = stdin;
		  }
		  else {
			lua_pushfstring(L, "@%s", filename);
			lf.f = fopen(filename, "r");
			if (lf.f == null) return errfile(L, "open", fnameindex);
		  }
		  if (skipcomment(lf, ref c)!=0)  /* read initial portion */
		    lf.buff[lf.n++] = '\n';  /* add line to correct line numbers */
		  if (c == (int)LUA_SIGNATURE[0] && filename!=null) {  /* binary file? */ //FIXME:added, (int)
		    lf.f = freopen(filename, "rb", lf.f);  /* reopen in binary mode */
		    if (lf.f == null) return errfile(L, "reopen", fnameindex);
		    skipcomment(lf, ref c);  /* re-read initial portion */
		  }
		  if (c != EOF)
		  	lf.buff[lf.n++] = (char)c;  /* 'c' is the first character of the stream */ //FIXME:added, (char)
		  status = lua_load(L, getF, lf, lua_tostring(L, -1), mode);
		  readstatus = ferror(lf.f);
		  if (filename != null) fclose(lf.f);  /* close file (even in case of errors) */
		  if (readstatus != 0) {
			lua_settop(L, fnameindex);  /* ignore results from 'lua_load' */
			return errfile(L, "read", fnameindex);
		  }
		  lua_remove(L, fnameindex);
		  return status;
		}


		public class LoadS {
		  public CharPtr s;
		  public uint size;
		};


		static CharPtr getS (lua_State L, object ud, out uint size) {
		  LoadS ls = (LoadS)ud;
		  //(void)L;  /* not used */
		  //if (ls.size == 0) return null;
		  size = ls.size;
		  ls.size = 0;
		  return ls.s;
		}


		public static int luaL_loadbufferx (lua_State L, CharPtr buff, uint size,
										    CharPtr name, CharPtr mode) {
		  LoadS ls = new LoadS();
		  ls.s = new CharPtr(buff);
		  ls.size = size;
		  return lua_load(L, getS, ls, name, mode);
		}


		public static int luaL_loadstring(lua_State L, CharPtr s) {
		  return luaL_loadbuffer(L, s, (uint)strlen(s), s);
		}

		/* }====================================================== */
        //FIXME:-------->


		public static int luaL_getmetafield (lua_State L, int obj, CharPtr event_) {
		  if (0==lua_getmetatable(L, obj))  /* no metatable? */
		    return LUA_TNIL;
		  else {
		    int tt;
		    lua_pushstring(L, event_);
		    tt = lua_rawget(L, -2);
		    if (tt == LUA_TNIL)  /* is metafield nil? */
		      lua_pop(L, 2);  /* remove metatable and metafield */
		    else
		      lua_remove(L, -2);  /* remove only metatable */
		    return tt;  /* return metafield type */
		  }
		}


		public static int luaL_callmeta (lua_State L, int obj, CharPtr event_) {
		  obj = lua_absindex(L, obj);
		  if (luaL_getmetafield(L, obj, event_) == LUA_TNIL)  /* no metafield? */
			return 0;
		  lua_pushvalue(L, obj);
		  lua_call(L, 1, 1);
		  return 1;
		}


		public static lua_Integer luaL_len (lua_State L, int idx) {
		  lua_Integer l;
          int isnum = 0; //FIXME:changed, =0
		  lua_len(L, idx);
		  l = lua_tointegerx(L, -1, ref isnum);
  		  if (isnum==0)
		    luaL_error(L, "object length is not an integer");
		  lua_pop(L, 1);  /* remove object */
		  return l;
		}


        public static CharPtr luaL_tolstring (lua_State L, int idx, out uint len) { //FIXME: size_t * -> out uint
		  if (luaL_callmeta(L, idx, "__tostring") == 0) {  /* no metafield? */
		    switch (lua_type(L, idx)) {
		      case LUA_TNUMBER: {
		        if (0!=lua_isinteger(L, idx))
		          lua_pushfstring(L, "%I", lua_tointeger(L, idx));
		        else
          		  lua_pushfstring(L, "%f", lua_tonumber(L, idx));
		        break;
			  }
		      case LUA_TSTRING:
		        lua_pushvalue(L, idx);
		        break;
		      case LUA_TBOOLEAN:
		        lua_pushstring(L, (lua_toboolean(L, idx) != 0 ? "true" : "false"));
                break;
		      case LUA_TNIL:
		        lua_pushliteral(L, "nil");
                break;
		      default:
		        lua_pushfstring(L, "%s: %p", luaL_typename(L, idx),
		                                            lua_topointer(L, idx));
                break;
		    }
		  }
		  return lua_tolstring(L, -1, out len);
		}


		/*
		** {======================================================
		** Compatibility with 5.1 module functions
		** =======================================================
		*/
		//#if LUA_COMPAT_MODULE

		private static CharPtr luaL_findtable (lua_State L, int idx,
											   CharPtr fname, int szhint) {
		  CharPtr e;
		  if (idx != 0) lua_pushvalue(L, idx);
		  do {
			e = strchr(fname, '.');
			if (e == null) e = fname + strlen(fname);
			lua_pushlstring(L, fname, (uint)(e - fname));
			if (lua_rawget(L, -2) == LUA_TNIL) {  /* no such field? */
			  lua_pop(L, 1);  /* remove this nil */
			  lua_createtable(L, 0, (e == '.' ? 1 : szhint)); /* new table for field */
			  lua_pushlstring(L, fname, (uint)(e - fname));
			  lua_pushvalue(L, -2);
			  lua_settable(L, -4);  /* set new table into field */
			}
			else if (!lua_istable(L, -1)) {  /* field has a non-table value? */
			  lua_pop(L, 2);  /* remove table and value */
			  return fname;  /* return problematic part of the name */
			}
			lua_remove(L, -2);  /* remove previous table */
			fname = e + 1;
		  } while (e == '.');
		  return null;
		}


		/*
		** Count number of elements in a luaL_Reg list.
		*/
		private static int libsize (/*const*/ luaL_Reg[] l) {
		  int size = 0;
		  for (int i=0; i < l.Length && l[i].name != null; i++) size++; //FIXME:changed, for (; l && l.name; l++) size++;
		  return size;
		}


		/*
		** Find or create a module table with a given name. The function
		** first looks at the _LOADED table and, if that fails, try a
		** global variable with that name. In any case, leaves on the stack
		** the module table.
		*/
		public static void luaL_pushmodule (lua_State L, CharPtr modname,
		                                 int sizehint) {
		  luaL_findtable(L, LUA_REGISTRYINDEX, "_LOADED", 1);  /* get _LOADED table */
		  if (lua_getfield(L, -1, modname) != LUA_TTABLE) {  /* no _LOADED[modname]? */
		    lua_pop(L, 1);  /* remove previous result */
		    /* try global variable (and create one if it does not exist) */
		    lua_pushglobaltable(L);
		    if (luaL_findtable(L, 0, modname, sizehint) != null)
		      luaL_error(L, "name conflict for module '%s'", modname);
		    lua_pushvalue(L, -1);
		    lua_setfield(L, -3, modname);  /* _LOADED[modname] = new table */
		  }
		  lua_remove(L, -2);  /* remove _LOADED table */
		}


		public static void luaL_openlib (lua_State L, CharPtr libname,
		                                 /*const*/ luaL_Reg[] l, int nup) {
		  luaL_checkversion(L);
		  if (libname != null) {
		    luaL_pushmodule(L, libname, libsize(l));  /* get/create library table */
		    lua_insert(L, -(nup + 1));  /* move library table to below upvalues */
		  }
          if (l != null)
		    luaL_setfuncs(L, l, nup);
		  else
		    lua_pop(L, nup);  /* remove upvalues */
		}

		//#endif
		/* }====================================================== */

		/*
		** set functions from list 'l' into table at top - 'nup'; each
		** function gets the 'nup' elements at the top as upvalues.
		** Returns with only the table at the stack.
		*/
		public static void luaL_setfuncs (lua_State L, /*const*/ luaL_Reg[] l, int nup) {
		  luaL_checkstack(L, nup, "too many upvalues");
		  for (int idxL = 0; idxL < l.Length && l[idxL].name != null; idxL++) {  /* fill the table with given functions */ //FIXME:changed://for (; l.name != null; l++) {
		  	luaL_Reg l_ = l[idxL]; //FIXME:added
		  	int i;
		    for (i = 0; i < nup; i++)  /* copy upvalues to the top */
		      lua_pushvalue(L, -nup);
		    lua_pushcclosure(L, l_.func, nup);  /* closure with those upvalues */
		    lua_setfield(L, -(nup + 2), l_.name);
		  }
		  lua_pop(L, nup);  /* remove upvalues */
		}


		/*
		** ensure that stack[idx][fname] has a table and push that table
		** into the stack
		*/
		public static int luaL_getsubtable (lua_State L, int idx, CharPtr fname) {
		  if (lua_getfield(L, idx, fname) == LUA_TTABLE)
    		return 1;  /* table already there */
		  else {
		    lua_pop(L, 1);  /* remove previous result */
		    idx = lua_absindex(L, idx);
		    lua_newtable(L);
		    lua_pushvalue(L, -1);  /* copy to be left at top */
//		    lua_xxx();
		    lua_setfield(L, idx, fname);  /* assign new table to field */
            return 0;  /* false, because did not find table there */
		  }
		}


		/*
		** Stripped-down 'require': After checking "loaded" table, calls 'openf'
		** to open a module, registers the result in 'package.loaded' table and,
		** if 'glb' is true, also registers the result in the global table.
		** Leaves resulting module on the top.
		*/
		public static void luaL_requiref (lua_State L, CharPtr modname,
		                               lua_CFunction openf, int glb) {
		  luaL_getsubtable(L, LUA_REGISTRYINDEX, "_LOADED");
		  lua_getfield(L, -1, modname);  /* _LOADED[modname] */
		  if (0==lua_toboolean(L, -1)) {  /* package not already loaded? */
		    lua_pop(L, 1);  /* remove field */
		    lua_pushcfunction(L, openf);
		    lua_pushstring(L, modname);  /* argument to open function */
		    lua_call(L, 1, 1);  /* call 'openf' to open module */
		    lua_pushvalue(L, -1);  /* make copy of module (call result) */
		    lua_setfield(L, -3, modname);  /* _LOADED[modname] = module */
		  }
		  lua_remove(L, -2);  /* remove _LOADED table */
		  if (0!=glb) {
		    lua_pushvalue(L, -1);  /* copy of module */
//		    Debug.WriteLine("xxx100");
		    lua_setglobal(L, modname);  /* _G[modname] = module */
		  }
		}


		public static CharPtr luaL_gsub (lua_State L, CharPtr s, CharPtr p,
		                                                         CharPtr r) {
		  CharPtr wild;
		  uint l = (uint)strlen(p); //FIXME:added, (uint)
		  luaL_Buffer b = new luaL_Buffer();
		  luaL_buffinit(L, b);
		  while ((wild = strstr(s, p)) != null) {
		  	luaL_addlstring(b, s, (uint)(wild - s));  /* push prefix */ //FIXME:added, (uint)
		    luaL_addstring(b, r);  /* push replacement in place of pattern */
		    s = wild + l;  /* continue after 'p' */
		  }
		  luaL_addstring(b, s);  /* push last suffix */
		  luaL_pushresult(b);
		  return lua_tostring(L, -1);
		}
	
        //FIXME:<--------

        //FIXME: changed here
		private static object l_alloc (Type t) {
			return System.Activator.CreateInstance(t);
		}


		private static int panic (lua_State L) {
		  lua_writestringerror("PANIC: unprotected error in call to Lua API (%s)\n",
						        lua_tostring(L, -1));
          return 0;  /* return to Lua to abort */
		}


		public static lua_State luaL_newstate()
		{
			lua_State L = lua_newstate(l_alloc, null);
		  if (L != null) lua_atpanic(L, panic);
		  return L;
		}


		public static void luaL_checkversion_ (lua_State L, lua_Number ver, uint sz) {
		  lua_Number[] v = lua_version(L);
		  if (sz != LUAL_NUMSIZES)  /* check numeric types */
		    luaL_error(L, "core and library have incompatible numeric types");	  
		  if (v != lua_version(null))
		    luaL_error(L, "multiple Lua VMs detected");
		  else if (v[0] != ver)
		    luaL_error(L, "version mismatch: app. needs %f, Lua core provides %f",
		                  ver, v[0]);
		}

	}
}

/*
** $Id: lauxlib.c,v 1.196 2009/12/22 15:32:50 roberto Exp roberto $
** Auxiliary functions for building Lua libraries
** See Copyright Notice in lua.h
*/

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

	public partial class Lua
	{

		/* convert a stack index to positive */
		public static int abs_index(lua_State L, int i)
		{
			return ((i) > 0 || (i) <= LUA_REGISTRYINDEX ? (i) : lua_gettop(L) + (i) + 1);
		}


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
		  int found = 0;
		  if (level == 0 || !lua_istable(L, -1))
		    return 0;  /* not found */
		  lua_pushnil(L);  /* start 'next' loop */
		  while (found==0 && lua_next(L, -2) != 0) {  /* for each pair in table */
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


		private static int pushglobalfuncname (lua_State L, lua_Debug ar) {
		  int top = lua_gettop(L);
		  lua_getinfo(L, "f", ar);  /* push function */
		  lua_pushglobaltable(L);
		  if (findfield(L, top + 1, 2) != 0) {
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
		  if (ar.namewhat[0] != '\0')  /* is there a name? */
		    lua_pushfstring(L, "function " + LUA_QS, ar.name);
		  else if (ar.what[0] == 'm')  /* main? */
		      lua_pushfstring(L, "main chunk");
		  else if (ar.what[0] == 'C' || ar.what[0] == 't') {
		    if (pushglobalfuncname(L, ar) != 0) {
		      lua_pushfstring(L, "function " + LUA_QS, lua_tostring(L, -1));
		      lua_remove(L, -2);  /* remove name */
		    }
		    else
		    	lua_pushliteral(L, "?");
          }
		  else
		    lua_pushfstring(L, "function <%s:%d>", ar.short_src, ar.linedefined);
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

		public static int luaL_argerror (lua_State L, int narg, CharPtr extramsg) {
		  lua_Debug ar = new lua_Debug();
		  if (lua_getstack(L, 0, ar)==0)  /* no stack frame? */
		    return luaL_error(L, "bad argument #%d (%s)", narg, extramsg);
		  lua_getinfo(L, "n", ar);
		  if (strcmp(ar.namewhat, "method") == 0) {
		    narg--;  /* do not count `self' */
		    if (narg == 0)  /* error is in the self argument itself? */
		      return luaL_error(L, "calling " + LUA_QS + " on bad self", ar.name);
		  }
		  if (ar.name == null)
		    ar.name = (pushglobalfuncname(L, ar) != 0) ? lua_tostring(L, -1) : "?";
		  return luaL_error(L, "bad argument #%d to " + LUA_QS + " (%s)",
		                        narg, ar.name, extramsg);
		}


		public static int luaL_typeerror (lua_State L, int narg, CharPtr tname) {
		  CharPtr msg = lua_pushfstring(L, "%s expected, got %s",
		                                    tname, luaL_typename(L, narg));
		  return luaL_argerror(L, narg, msg);
		}


		private static void tag_error (lua_State L, int narg, int tag) {
		  luaL_typeerror(L, narg, lua_typename(L, tag));
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

		/* }====================================================== */


		/*
		** {======================================================
		** Userdata's metatable manipulation
		** =======================================================
		*/



		public static int luaL_newmetatable (lua_State L, CharPtr tname) {
		  lua_getfield(L, LUA_REGISTRYINDEX, tname);  /* get registry.name */
		  if (!lua_isnil(L, -1))  /* name already in use? */
			return 0;  /* leave previous value on top, but return 0 */
		  lua_pop(L, 1);
		  lua_newtable(L);  /* create metatable */
		  lua_pushvalue(L, -1);
		  lua_setfield(L, LUA_REGISTRYINDEX, tname);  /* registry.name = metatable */
		  return 1;
		}


		public static object luaL_testudata (lua_State L, int ud, CharPtr tname) {
		  object p = lua_touserdata(L, ud);
		  if (p != null) {  /* value is a userdata? */
			if (lua_getmetatable(L, ud) != 0) {  /* does it have a metatable? */
			  lua_getfield(L, LUA_REGISTRYINDEX, tname);  /* get correct metatable */
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
		  if (p == null) luaL_typeerror(L, ud, tname);
		  return p;
		}

		/* }====================================================== */


		/*
		** {======================================================
		** Argument check functions
		** =======================================================
		*/

		public static int luaL_checkoption (lua_State L, int narg, CharPtr def,
										 CharPtr [] lst) {
		  CharPtr name = (def != null) ? luaL_optstring(L, narg, def) :
									 luaL_checkstring(L, narg);
		  int i;
		  for (i=0; i<lst.Length; i++)
			if (strcmp(lst[i], name)==0)
			  return i;
		  return luaL_argerror(L, narg,
							   lua_pushfstring(L, "invalid option " + LUA_QS, name));
		}


		public static void luaL_checkstack (lua_State L, int space, CharPtr msg) {
		  if (lua_checkstack(L, space)==0) {
		    if (msg != null)
		      luaL_error(L, "stack overflow (%s)", msg);
		    else
		      luaL_error(L, "stack overflow");
		  }
		}


		public static void luaL_checktype (lua_State L, int narg, int t) {
		  if (lua_type(L, narg) != t)
			tag_error(L, narg, t);
		}


		public static void luaL_checkany (lua_State L, int narg) {
		  if (lua_type(L, narg) == LUA_TNONE)
			luaL_argerror(L, narg, "value expected");
		}


		public static CharPtr luaL_checklstring(lua_State L, int narg) {uint len; return luaL_checklstring(L, narg, out len);}
		public static CharPtr luaL_checklstring (lua_State L, int narg, out uint len) {
		  CharPtr s = lua_tolstring(L, narg, out len);
		  if (s==null) tag_error(L, narg, LUA_TSTRING);
		  return s;
		}


		public static CharPtr luaL_optlstring (lua_State L, int narg, CharPtr def) {
			uint len; return luaL_optlstring (L, narg, def, out len); }
		public static CharPtr luaL_optlstring (lua_State L, int narg, CharPtr def, out uint len) {
		  if (lua_isnoneornil(L, narg)) {
			len = (uint)((def != null) ? strlen(def) : 0);
			return def;
		  }
		  else return luaL_checklstring(L, narg, out len);
		}


		public static lua_Number luaL_checknumber (lua_State L, int narg) {
		  lua_Number d = lua_tonumber(L, narg);
		  if ((d == 0) && (lua_isnumber(L, narg)==0))  /* avoid extra test when d is not 0 */
			tag_error(L, narg, LUA_TNUMBER);
		  return d;
		}


		public static lua_Number luaL_optnumber (lua_State L, int narg, lua_Number def) {
		  return luaL_opt(L, luaL_checknumber, narg, def);
		}


		public static lua_Integer luaL_checkinteger (lua_State L, int narg) {
		  lua_Integer d = lua_tointeger(L, narg);
		  if (d == 0 && lua_isnumber(L, narg)==0)  /* avoid extra test when d is not 0 */
			tag_error(L, narg, LUA_TNUMBER);
		  return d;
		}


		public static lua_Integer luaL_optinteger (lua_State L, int narg, lua_Integer def) {
		  return luaL_opt_integer(L, luaL_checkinteger, narg, def);
		}


        /* }====================================================== */


		/*
		** {======================================================
		** Generic Buffer manipulation
		** =======================================================
		*/


		private static int bufflen(luaL_Buffer B)	{return B.p;}
		private static int bufffree(luaL_Buffer B)	{return LUAL_BUFFERSIZE - bufflen(B);}

		public const int LIMIT = LUA_MINSTACK / 2;


		private static int emptybuffer (luaL_Buffer B) {
		  uint l = (uint)bufflen(B);
		  if (l == 0) return 0;  /* put nothing on stack */
		  else {
			lua_pushlstring(B.L, B.buffer, l);
			B.p = 0;
			B.lvl++;
			return 1;
		  }
		}


		private static void adjuststack (luaL_Buffer B) {
		  if (B.lvl > 1) {
			lua_State L = B.L;
			int toget = 1;  /* number of levels to concat */
			uint toplen = lua_rawlen(L, -1);
			do {
			  uint l = lua_rawlen(L, -(toget+1));
			  if (B.lvl - toget + 1 >= LIMIT || toplen > l) {
				toplen += l;
				toget++;
			  }
			  else break;
			} while (toget < B.lvl);
			lua_concat(L, toget);
			B.lvl = B.lvl - toget + 1;
		  }
		}


		public static CharPtr luaL_prepbuffer (luaL_Buffer B) {
		  if (emptybuffer(B) != 0)
			adjuststack(B);
			return new CharPtr(B.buffer, B.p);
		}


		public static void luaL_addlstring (luaL_Buffer B, CharPtr s, uint l) {
			while (l-- != 0)
			{
				char c = s[0];
				s = s.next();
				luaL_addchar(B, c);
			}
		}


		public static void luaL_addstring (luaL_Buffer B, CharPtr s) {
		  luaL_addlstring(B, s, (uint)strlen(s));
		}


		public static void luaL_pushresult (luaL_Buffer B) {
		  emptybuffer(B);
		  lua_concat(B.L, B.lvl);
		  B.lvl = 1;
		}


		public static void luaL_addvalue (luaL_Buffer B) {
		  lua_State L = B.L;
		  uint vl;
		  CharPtr s = lua_tolstring(L, -1, out vl);
		  if (vl <= bufffree(B)) {  /* fit into buffer? */
			CharPtr dst = new CharPtr(B.buffer.chars, B.buffer.index + B.p);
			CharPtr src = new CharPtr(s.chars, s.index);
			for (uint i = 0; i < vl; i++)
				dst[i] = src[i];
			B.p += (int)vl;
			lua_pop(L, 1);  /* remove from stack */
		  }
		  else {
			if (emptybuffer(B) != 0)
			  lua_insert(L, -2);  /* put buffer before new value */
			B.lvl++;  /* add new value into B stack */
			adjuststack(B);
		  }
		}


		public static void luaL_buffinit (lua_State L, luaL_Buffer B) {
          luaL_checkstack(L, LIMIT + LUA_MINSTACK, "no space for new buffer");
		  B.L = L;
		  B.p = /*B.buffer*/ 0;
		  B.lvl = 0;
		}

		/* }====================================================== */


		/*
		** {======================================================
		** Reference system
		** =======================================================
		*/

		/* number of prereserved references (for internal use) */
		private const int FREELIST_REF = (LUA_RIDX_LAST + 1);	/* free list of references */


		public static int luaL_ref (lua_State L, int t) {
		  int ref_;
		  t = abs_index(L, t);
		  if (lua_isnil(L, -1)) {
			lua_pop(L, 1);  /* remove from stack */
			return LUA_REFNIL;  /* `nil' has a unique fixed reference */
		  }
		  lua_rawgeti(L, t, FREELIST_REF);  /* get first free element */
		  ref_ = (int)lua_tointeger(L, -1);  /* ref = t[FREELIST_REF] */
		  lua_pop(L, 1);  /* remove it from stack */
		  if (ref_ != 0) {  /* any free element? */
			lua_rawgeti(L, t, ref_);  /* remove it from list */
			lua_rawseti(L, t, FREELIST_REF);  /* (t[FREELIST_REF] = t[ref]) */
		  }
		  else {  /* no free elements */
		    ref_ = (int)lua_rawlen(L, t) + 1;  /* get a new reference */
		    if (ref_ == FREELIST_REF) {  /* FREELIST_REF not initialized? */
		      lua_pushinteger(L, 0);
		      lua_rawseti(L, t, FREELIST_REF);
		      ref_ = FREELIST_REF + 1;
		    }
		  }
		  lua_rawseti(L, t, ref_);
		  return ref_;
		}


		public static void luaL_unref (lua_State L, int t, int ref_) {
		  if (ref_ >= 0) {
			t = abs_index(L, t);
			lua_rawgeti(L, t, FREELIST_REF);
			lua_rawseti(L, t, ref_);  /* t[ref] = t[FREELIST_REF] */
			lua_pushinteger(L, ref_);
			lua_rawseti(L, t, FREELIST_REF);  /* t[FREELIST_REF] = ref */
		  }
		}

        /* }====================================================== */
		

		/*
		** {======================================================
		** Load functions
		** =======================================================
		*/

		public class LoadF {
		  public int extraline;
		  public Stream f;
		  public CharPtr buff = new char[LUAL_BUFFERSIZE];
		};


		public static CharPtr getF (lua_State L, object ud, out uint size) {
		  size = 0;
		  LoadF lf = (LoadF)ud;
		  //(void)L;
		  if (lf.extraline != 0) {
			lf.extraline = 0;
			size = 1;
			return "\n";
		  }
		  /* 'fread' can return > 0 *and* set the EOF flag. If next call to
		     'getF' calls 'fread', terminal may still wait for user input.
		     The next check avoids this problem. */
		  if (feof(lf.f) != 0) return null;
		  size = (uint)fread(lf.buff, 1, lf.buff.chars.Length, lf.f);
		  return (size > 0) ? new CharPtr(lf.buff) : null;
		}


		private static int errfile (lua_State L, CharPtr what, int fnameindex) {
		  CharPtr serr = strerror(errno());
		  CharPtr filename = lua_tostring(L, fnameindex) + 1;
		  lua_pushfstring(L, "cannot %s %s: %s", what, filename, serr);
		  lua_remove(L, fnameindex);
		  return LUA_ERRFILE;
		}


		public static int luaL_loadfile (lua_State L, CharPtr filename) {
		  LoadF lf = new LoadF();
		  int status, readstatus;
		  int c;
		  int fnameindex = lua_gettop(L) + 1;  /* index of filename on the stack */
		  lf.extraline = 0;
		  if (filename == null) {
			lua_pushliteral(L, "=stdin");
			lf.f = stdin;
		  }
		  else {
			lua_pushfstring(L, "@%s", filename);
			lf.f = fopen(filename, "r");
			if (lf.f == null) return errfile(L, "open", fnameindex);
		  }
		  c = getc(lf.f);
		  if (c == '#') {  /* Unix exec. file? */
			lf.extraline = 1;
			while ((c = getc(lf.f)) != EOF && c != '\n') ;  /* skip first line */
			if (c == '\n') c = getc(lf.f);
		  }
		  if (c == LUA_SIGNATURE[0] && filename != null) {  /* binary file? */
			lf.f = freopen(filename, "rb", lf.f);  /* reopen in binary mode */
			if (lf.f == null) return errfile(L, "reopen", fnameindex);
			/* skip eventual `#!...' */
		    while ((c = getc(lf.f)) != EOF && c != LUA_SIGNATURE[0]) ;
			lf.extraline = 0;
		  }
		  ungetc(c, lf.f);
		  status = lua_load(L, getF, lf, lua_tostring(L, -1));
		  readstatus = ferror(lf.f);
		  if (filename != null) fclose(lf.f);  /* close file (even in case of errors) */
		  if (readstatus != 0) {
			lua_settop(L, fnameindex);  /* ignore results from `lua_load' */
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
		  //(void)L;
		  //if (ls.size == 0) return null;
		  size = ls.size;
		  ls.size = 0;
		  return ls.s;
		}


		public static int luaL_loadbuffer(lua_State L, CharPtr buff, uint size,
										CharPtr name) {
		  LoadS ls = new LoadS();
		  ls.s = new CharPtr(buff);
		  ls.size = size;
		  return lua_load(L, getS, ls, name);
		}


		public static int luaL_loadstring(lua_State L, CharPtr s) {
		  return luaL_loadbuffer(L, s, (uint)strlen(s), s);
		}



		/* }====================================================== */
        //FIXME:-------->
		public static int luaL_getmetafield (lua_State L, int obj, CharPtr event_) {
		  if (lua_getmetatable(L, obj)==0)  /* no metatable? */
			return 0;
		  lua_pushstring(L, event_);
		  lua_rawget(L, -2);
		  if (lua_isnil(L, -1)) {
			lua_pop(L, 2);  /* remove metatable and metafield */
			return 0;
		  }
		  else {
			lua_remove(L, -2);  /* remove only metatable */
			return 1;
		  }
		}


		public static int luaL_callmeta (lua_State L, int obj, CharPtr event_) {
		  obj = abs_index(L, obj);
		  if (luaL_getmetafield(L, obj, event_)==0)  /* no metafield? */
			return 0;
		  lua_pushvalue(L, obj);
		  lua_call(L, 1, 1);
		  return 1;
		}


		public static int luaL_len (lua_State L, int idx) {
		  int l;
		  lua_len(L, idx);
		  l = lua_tointeger(L, -1);
		  if (l == 0 && lua_isnumber(L, -1)==0)
		    luaL_error(L, "object length is not a number");
		  lua_pop(L, 1);  /* remove object */
		  return l;
		}


        public static CharPtr luaL_tolstring (lua_State L, int idx, out uint len) { //FIXME: size_t * -> out uint
		  if (luaL_callmeta(L, idx, "__tostring") == 0) {  /* no metafield? */
		    switch (lua_type(L, idx)) {
		      case LUA_TNUMBER:
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


		// we could just take the .Length member here, but let's try
		// to keep it as close to the C implementation as possible.
		private static int libsize (luaL_Reg[] l) {
		  int size = 0;
		  for (; l[size].name!=null; size++);
		  return size;
		}


		public static void luaL_register (lua_State L, CharPtr libname,
									  luaL_Reg[] l) {		  
          luaL_checkversion(L);
		  if (libname!=null) {
			/* check whether lib already exists */
			luaL_findtable(L, LUA_REGISTRYINDEX, "_LOADED", 1);
			lua_getfield(L, -1, libname);  /* get _LOADED[libname] */
			if (!lua_istable(L, -1)) {  /* not found? */
			  lua_pop(L, 1);  /* remove previous result */
			  /* try global variable (and create one if it does not exist) */
              lua_pushglobaltable(L);
			  if (luaL_findtable(L, 0, libname, libsize(l)) != null)
				luaL_error(L, "name conflict for module " + LUA_QS, libname);
			  lua_pushvalue(L, -1);
			  lua_setfield(L, -3, libname);  /* _LOADED[libname] = new table */
			}
			lua_remove(L, -2);  /* remove _LOADED table */
		  }
		  if (l == null) return;  /* nothing to register? */
		  int reg_num = 0;
		  for (; l[reg_num].name!=null; reg_num++) {  /* else fill the table with given functions */
		    lua_pushcfunction(L, l[reg_num].func);
		    lua_setfield(L, -2, l[reg_num].name);
		  }
		}



		public static CharPtr luaL_gsub (lua_State L, CharPtr s, CharPtr p,
																	   CharPtr r) {
		  CharPtr wild;
		  uint l = (uint)strlen(p);
		  luaL_Buffer b = new luaL_Buffer();
		  luaL_buffinit(L, b);
		  while ((wild = strstr(s, p)) != null) {
			luaL_addlstring(b, s, (uint)(wild - s));  /* push prefix */
			luaL_addstring(b, r);  /* push replacement in place of pattern */
			s = wild + l;  /* continue after `p' */
		  }
		  luaL_addstring(b, s);  /* push last suffix */
		  luaL_pushresult(b);
		  return lua_tostring(L, -1);
		}


		public static CharPtr luaL_findtable (lua_State L, int idx,
											   CharPtr fname, int szhint) {
		  CharPtr e;
		  if (idx != 0) lua_pushvalue(L, idx);
		  do {
			e = strchr(fname, '.');
			if (e == null) e = fname + strlen(fname);
			lua_pushlstring(L, fname, (uint)(e - fname));
			lua_rawget(L, -2);
			if (lua_isnil(L, -1)) {  /* no such field? */
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


		
        //FIXME:<--------

		private static object l_alloc (Type t) {
			return System.Activator.CreateInstance(t);
		}


		private static int panic (lua_State L) {
		  fprintf(stderr, "PANIC: unprotected error in call to Lua API (%s)\n",
						   lua_tostring(L, -1));
          return 0;  /* return to Lua to abort */
		}


		public static lua_State luaL_newstate()
		{
			lua_State L = lua_newstate(l_alloc, null);
		  if (L != null) lua_atpanic(L, panic);
		  return L;
		}


		public static void luaL_checkversion_ (lua_State L, lua_Number ver) {
		  lua_Number[] v = lua_version(L);
		  if (v != lua_version(null))
		    luaL_error(L, "multiple Lua VMs detected");
		  else if (v[0] != ver)
		    luaL_error(L, "version mismatch: app. needs %d, Lua core provides %f",
		                  ver, v[0]);
		}


		public static int luaL_cpcall (lua_State L, lua_CFunction f, int nargs,
		                            int nresults) {
		  nargs++;  /* to include function itself */
		  lua_rawgeti(L, LUA_REGISTRYINDEX, LUA_RIDX_CPCALL);
		  lua_insert(L, -nargs);
		  lua_pushlightuserdata(L, f);
		  lua_insert(L, -nargs);
		  return lua_pcall(L, nargs, nresults, 0);
		}
	}
}

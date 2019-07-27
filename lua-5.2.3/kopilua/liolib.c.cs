/*
** $Id: liolib.c,v 2.111 2013/03/21 13:57:27 roberto Exp $
** Standard I/O (and system) library
** See Copyright Notice in lua.h
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace KopiLua
{
	using lua_Number = System.Double;
	using lua_Integer = System.Int32;
	using LStream = KopiLua.Lua.luaL_Stream;
	using l_seeknum = System.Int64;


	public partial class Lua
	{


		/*
		** POSIX idiosyncrasy!
		** This definition must come before the inclusion of 'stdio.h'; it
		** should not affect non-POSIX systems
		*/
		//#if !defined(_FILE_OFFSET_BITS)
		private const int _FILE_OFFSET_BITS = 64;
		//#endif


		//#if !defined(lua_checkmode)

		/*
		** Check whether 'mode' matches '[rwa]%+?b?'.
		** Change this macro to accept other modes for 'fopen' besides
		** the standard ones.
		*/
		public static bool lua_checkmode(CharPtr mode) {
			if (!(mode[0] != '\0')) return false;
			if (!(strchr("rwa", mode[0]) != null)) { mode.inc(); return false;}
			mode.inc();
			if (!(mode[0] != '+')) return false;
			mode.inc(); /* skip if char is '+' */
			if (!(mode[0] != 'b')) return false;
			mode.inc(); /* skip if char is 'b' */
			if (!(mode[0] == '\0')) return false;
			return true;}

		//#endif

		/*
		** {======================================================
		** lua_popen spawns a new process connected to the current
		** one through the file streams.
		** =======================================================
		*/
		//#if !defined(lua_popen)	/* { */

		//#if defined(LUA_USE_POPEN)	/* { */

		//#define lua_popen(L,c,m)        ((void)L, fflush(NULL), popen(c,m))
		//#define lua_pclose(L,file)      ((void)L, pclose(file))

		//#elif defined(LUA_WIN)		/* }{ */

		private static StreamProxy lua_popen(lua_State L, CharPtr c, CharPtr m)  { /*(void)L,*/ return _popen(c,m); }
		private static int lua_pclose(lua_State L, StreamProxy file)  { /*(void)L,*/ return _pclose(file); }


		//#else				/* }{ */

		//#define lua_popen(L,c,m)        ((void)((void)c, m),  \
		//                luaL_error(L, LUA_QL("popen") " not supported"), (FILE*)0)
		//#define lua_pclose(L,file)              ((void)((void)L, file), -1)


		//#endif				/* } */

		//#endif			/* } */

		/* }====================================================== */


		/*
		** {======================================================
		** lua_fseek/lua_ftell: configuration for longer offsets
		** =======================================================
		*/

		//#if !defined(lua_fseek)	/* { */

		//#if defined(LUA_USE_POSIX)

		//#define l_fseek(f,o,w)		fseeko(f,o,w)
		//#define l_ftell(f)		ftello(f)
		//#define l_seeknum		off_t

		//#elif defined(LUA_WIN) && !defined(_CRTIMP_TYPEINFO) \
		//   && defined(_MSC_VER) && (_MSC_VER >= 1400)
		/* Windows (but not DDK) and Visual C++ 2005 or higher */

		//#define l_fseek(f,o,w)		_fseeki64(f,o,w)
		//#define l_ftell(f)		_ftelli64(f)
		//#define l_seeknum		__int64

		//#else

		private static int l_fseek(StreamProxy f, long o, int w) { return fseek(f,o,w);}
		private static int l_ftell(StreamProxy f) { return ftell(f);}
		//#define l_seeknum		long

		//#endif

		//#endif			/* } */

		/* }====================================================== */


		public const string IO_PREFIX = "_IO_";
		public const string IO_INPUT	= (IO_PREFIX + "input");
		public const string IO_OUTPUT	= (IO_PREFIX + "output");


		//typedef luaL_Stream LStream;


		public static LStream tolstream(lua_State L) { return (LStream)luaL_checkudata(L, 1, LUA_FILEHANDLE); }

        public static bool isclosed(LStream p) { return (p.closef == null); }


		private static int io_type (lua_State L) {
		  LStream p;
		  luaL_checkany(L, 1);
		  p = (LStream)luaL_testudata(L, 1, LUA_FILEHANDLE);
          if (p == null)
			lua_pushnil(L);  /* not a file */
		  else if (isclosed(p))
			lua_pushliteral(L, "closed file");
		  else
			lua_pushliteral(L, "file");
		  return 1;
		}


		private static int f_tostring (lua_State L) {
		  LStream p = tolstream(L);
		  if (isclosed(p))
		    lua_pushliteral(L, "file (closed)");
		  else
		    lua_pushfstring(L, "file (%p)", p.f);
		  return 1;
		}


		private static StreamProxy tofile (lua_State L) {
		  LStream p = tolstream(L);
		  if (isclosed(p))
			luaL_error(L, "attempt to use a closed file");
          lua_assert(p.f!=null);
		  return p.f;
		}


		/*
		** When creating file handles, always creates a `closed' file handle
		** before opening the actual file; so, if there is a memory error, the
		** file is not left opened.
		*/
		private static LStream newprefile (lua_State L) {
		  LStream p = (LStream)lua_newuserdata(L, typeof(LStream)); //FIXME:changed, sizeof->typeof
		  p.closef = null;  /* mark file handle as 'closed' */
		  luaL_setmetatable(L, LUA_FILEHANDLE);
		  return p;
		}


		private static int aux_close (lua_State L) {
		  LStream p = tolstream(L);
		  lua_CFunction cf = p.closef;
		  p.closef = null;  /* mark stream as closed */
		  return cf(L);  /* close it */
		}


		private static int io_close (lua_State L) {
		  if (lua_isnone(L, 1))  /* no argument? */
			lua_getfield(L, LUA_REGISTRYINDEX, IO_OUTPUT);  /* use standard output */
		  tofile(L);  /* make sure argument is an open stream */
		  return aux_close(L);
		}


		private static int f_gc (lua_State L) {
		  LStream p = tolstream(L);
		  if (!isclosed(p) && p.f != null)
			aux_close(L);  /* ignore closed and incompletely open files */
		  return 0;
		}


		/*
		** function to close regular files
		*/
		private static int io_fclose (lua_State L) {
		  LStream p = tolstream(L);
		  int res = fclose(p.f);
		  return luaL_fileresult(L, (res == 0)?1:0, null);
		}


		private static LStream newfile (lua_State L) {
		  LStream p = newprefile(L);
		  p.f = null;
		  p.closef = io_fclose;
		  return p;
		}


		private static void opencheck (lua_State L, CharPtr fname, CharPtr mode) {
		  LStream p = newfile(L);
		  p.f = fopen(fname, mode);
		  if (p.f == null)
		    luaL_error(L, "cannot open file " + LUA_QS + " (%s)", fname, strerror(errno));
		}


		private static int io_open (lua_State L) {
		  CharPtr filename = luaL_checkstring(L, 1);
		  CharPtr mode = luaL_optstring(L, 2, "r");
		  LStream p = newfile(L);
		  CharPtr md = mode;  /* to traverse/check mode */
		  luaL_argcheck(L, lua_checkmode(md), 2, "invalid mode");
		  p.f = fopen(filename, mode);
		  return (p.f == null) ? luaL_fileresult(L, 0, filename) : 1;
		}


		/*
		** function to close 'popen' files
		*/
		private static int io_pclose (lua_State L) {
		  LStream p = tolstream(L);
		  return luaL_execresult(L, lua_pclose(L, p.f));
		}


		private static int io_popen (lua_State L) {
		  CharPtr filename = luaL_checkstring(L, 1);
		  CharPtr mode = luaL_optstring(L, 2, "r");
		  LStream p = newprefile(L);
		  p.f = lua_popen(L, filename, mode);
          p.closef = io_pclose;
		  return (p.f == null) ? luaL_fileresult(L, 0, filename) : 1;
		}


		private static int io_tmpfile (lua_State L) {
		  LStream p = newfile(L);
		  p.f = tmpfile();
		  return (p.f == null) ? luaL_fileresult(L, 0, null) : 1;
		}


		private static StreamProxy getiofile (lua_State L, CharPtr findex) {
		  LStream p;
		  lua_getfield(L, LUA_REGISTRYINDEX, findex);
		  p = (LStream)lua_touserdata(L, -1);
		  if (isclosed(p))
			luaL_error(L, "standard %s file is closed", findex + strlen(IO_PREFIX));
		  return p.f;
		}


		private static int g_iofile (lua_State L, CharPtr f, CharPtr mode) {
		  if (!lua_isnoneornil(L, 1)) {
			CharPtr filename = lua_tostring(L, 1);
			if (filename != null)
			  opencheck(L, filename, mode);
			else {
			  tofile(L);  /* check that it's a valid file handle */
			  lua_pushvalue(L, 1);
			}
			lua_setfield(L, LUA_REGISTRYINDEX, f);
		  }
		  /* return current value */
		  lua_getfield(L, LUA_REGISTRYINDEX, f);
		  return 1;
		}


		private static int io_input (lua_State L) {
		  return g_iofile(L, IO_INPUT, "r");
		}


		private static int io_output (lua_State L) {
		  return g_iofile(L, IO_OUTPUT, "w");
		}


		//static int io_readline (lua_State *L);


		private static void aux_lines (lua_State L, int toclose) {
		  int i;
		  int n = lua_gettop(L) - 1;  /* number of arguments to read */
		  /* ensure that arguments will fit here and into 'io_readline' stack */
		  luaL_argcheck(L, n <= LUA_MINSTACK - 3, LUA_MINSTACK - 3, "too many options");
		  lua_pushvalue(L, 1);  /* file handle */
		  lua_pushinteger(L, n);  /* number of arguments to read */
		  lua_pushboolean(L, toclose);  /* close/not close file when finished */
		  for (i = 1; i <= n; i++) lua_pushvalue(L, i + 1);  /* copy arguments */
		  lua_pushcclosure(L, io_readline, 3 + n);
		}


		private static int f_lines (lua_State L) {
		  tofile(L);  /* check that it's a valid file handle */
		  aux_lines(L, 0);
		  return 1;
		}


		private static int io_lines (lua_State L) {
		  int toclose;
		  if (lua_isnone(L, 1)) lua_pushnil(L);  /* at least one argument */
		  if (lua_isnil(L, 1)) {  /* no file name? */
		    lua_getfield(L, LUA_REGISTRYINDEX, IO_INPUT);  /* get default input */
		    lua_replace(L, 1);   /* put it at index 1 */
		    tofile(L);  /* check that it's a valid file handle */
		    toclose = 0;  /* do not close it after iteration */
		  }
		  else {  /* open a new file */
		    CharPtr filename = luaL_checkstring(L, 1);
		    opencheck(L, filename, "r");
		    lua_replace(L, 1);  /* put file at index 1 */
		    toclose = 1;  /* close it after iteration */
		  }
		  aux_lines(L, toclose);
		  return 1;
		}


		/*
		** {======================================================
		** READ
		** =======================================================
		*/


		private static int read_number (lua_State L, StreamProxy f) {
		  //lua_Number d; //FIXME:???
		  object[] parms = { (object)(double)0.0 }; //FIXME:???
		  if (fscanf(f, LUA_NUMBER_SCAN, parms) == 1) {
			lua_pushnumber(L, (double)parms[0]); //FIXME:d???
			return 1;
		  }
		  else {
		   lua_pushnil(L);  /* "result" to be removed */
		   return 0;  /* read fails */
		  }
        }


		private static int test_eof (lua_State L, StreamProxy f) {
		  int c = getc(f);
		  ungetc(c, f);
		  lua_pushlstring(L, null, 0);
		  return (c != EOF) ? 1 : 0;
		}


		private static int read_line (lua_State L, StreamProxy f, int chop) {
		  luaL_Buffer b = new luaL_Buffer();
		  luaL_buffinit(L, b);
		  for (;;) {
			uint l;
			CharPtr p = luaL_prepbuffer(b);
			if (fgets(p, f) == null) {  /* eof? */
			  luaL_pushresult(b);  /* close buffer */
				return (lua_rawlen(L, -1) > 0) ? 1 : 0;  /* check whether read something */
			}
			l = (uint)strlen(p);
			if (l == 0 || p[l-1] != '\n')
			  luaL_addsize(b, (uint)(int)l); //FIXME:added, (uint)
			else {
			  luaL_addsize(b, (uint)(l - chop));  /* chop 'eol' if needed */ //FIXME:added, (uint)
			  luaL_pushresult(b);  /* close buffer */
			  return 1;  /* read at least an `eol' */
			}
		  }
		}


		private const int MAX_SIZE_T = MAX_INT;//(~(size_t)0); //FIXME:changed;

		private static void read_all (lua_State L, StreamProxy f) {
		  uint rlen = LUAL_BUFFERSIZE;  /* how much to read in each cycle */
		  luaL_Buffer b = new luaL_Buffer();
		  luaL_buffinit(L, b);
		  for (;;) {
		    CharPtr p = luaL_prepbuffsize(b, rlen);
		    uint nr = (uint)fread(p, 1/*sizeof(char)*/, (int)rlen, f); //FIXME:changed
		    luaL_addsize(b, nr);
		    if (nr < rlen) break;  /* eof? */
		    else if (rlen <= (MAX_SIZE_T / 4))  /* avoid buffers too large */
		      rlen *= 2;  /* double buffer size at each iteration */
		  }
		  luaL_pushresult(b);  /* close buffer */
		}


		private static int read_chars (lua_State L, StreamProxy f, uint n) {
		  uint nr;  /* number of chars actually read */
		  CharPtr p;
		  luaL_Buffer b = new luaL_Buffer();
		  luaL_buffinit(L, b);
		  p = luaL_prepbuffsize(b, n);  /* prepare buffer to read whole block */
		  nr = (uint)fread(p, 1/*sizeof(char)*/, (int)n, f);  /* try to read 'n' chars */ //FIXME:changed
		  luaL_addsize(b, nr);
		  luaL_pushresult(b);  /* close buffer */
		  return (nr > 0)?1:0;  /* true iff read something */
		}


		private static int g_read (lua_State L, StreamProxy f, int first) {
		  int nargs = lua_gettop(L) - 1;
		  int success;
		  int n;
		  clearerr(f);
		  if (nargs == 0) {  /* no arguments? */
			success = read_line(L, f, 1);
			n = first+1;  /* to return 1 result */
		  }
		  else {  /* ensure stack space for all results and for auxlib's buffer */
			luaL_checkstack(L, nargs+LUA_MINSTACK, "too many arguments");
			success = 1;
			for (n = first; (nargs-- != 0) && (success!=0); n++) {
			  if (lua_type(L, n) == LUA_TNUMBER) {
				uint l = (uint)lua_tointeger(L, n);
				success = (l == 0) ? test_eof(L, f) : read_chars(L, f, l);
			  }
			  else {
				CharPtr p = lua_tostring(L, n);
				luaL_argcheck(L, (p!=null) && (p[0] == '*'), n, "invalid option");
				switch (p[1]) {
				  case 'n':  /* number */
					success = read_number(L, f);
					break;
				  case 'l':  /* line */
					success = read_line(L, f, 1);
					break;
		          case 'L':  /* line with end-of-line */
		            success = read_line(L, f, 0);
		            break;
				  case 'a':  /* file */
					read_all(L, f);  /* read entire file */
					success = 1; /* always success */
					break;
				  default:
					return luaL_argerror(L, n, "invalid format");
				}
			  }
			}
		  }
		  if (ferror(f)!=0)
			return luaL_fileresult(L, 0, null);
		  if (success==0) {
			lua_pop(L, 1);  /* remove last result */
			lua_pushnil(L);  /* push nil instead */
		  }
		  return n - first;
		}


		private static int io_read (lua_State L) {
		  return g_read(L, getiofile(L, IO_INPUT), 1);
		}


		private static int f_read (lua_State L) {
		  return g_read(L, tofile(L), 2);
		}


		private static int io_readline (lua_State L) {
		  LStream p = (LStream)lua_touserdata(L, lua_upvalueindex(1));
		  int i;
		  int n = (int)lua_tointeger(L, lua_upvalueindex(2));
		  if (isclosed(p))  /* file is already closed? */
		    return luaL_error(L, "file is already closed");
		  lua_settop(L , 1);
		  for (i = 1; i <= n; i++)  /* push arguments to 'g_read' */
		    lua_pushvalue(L, lua_upvalueindex(3 + i));
		  n = g_read(L, p.f, 2);  /* 'n' is number of results */
		  lua_assert(n > 0);  /* should return at least a nil */
		  if (!lua_isnil(L, -n))  /* read at least one value? */
		    return n;  /* return them */
		  else {  /* first result is nil: EOF or error */
		    if (n > 1) {  /* is there error information? */
		      /* 2nd result is error message */
		      return luaL_error(L, "%s", lua_tostring(L, -n + 1));
		    }
		    if (lua_toboolean(L, lua_upvalueindex(3))!=0) {  /* generator created file? */
		      lua_settop(L, 0);
		      lua_pushvalue(L, lua_upvalueindex(1));
		      aux_close(L);  /* close it */
		    }
		    return 0;
		  }
		}

		/* }====================================================== */


		private static int g_write (lua_State L, StreamProxy f, int arg) {
		  int nargs = lua_gettop(L) - arg;
		  int status = 1;
		  for (; (nargs--) != 0; arg++) {
			if (lua_type(L, arg) == LUA_TNUMBER) {
			  /* optimization: could be done exactly as for strings */
			  status = ((status!=0) &&
				  (fprintf(f, LUA_NUMBER_FMT, lua_tonumber(L, arg)) > 0)) ? 1 : 0;
			}
			else {
			  uint l;
			  CharPtr s = luaL_checklstring(L, arg, out l);
			  status = ((status!=0) && (fwrite(s, 1, (int)l, f) == l)) ? 1 : 0; //FIXME:changed, sizeof(char)
			}
		  }
		  if (status != 0) return 1;  /* file handle already on stack top */
		  else return luaL_fileresult(L, status, null);
		}


		private static int io_write (lua_State L) {
		  return g_write(L, getiofile(L, IO_OUTPUT), 1);
		}


		private static int f_write (lua_State L) {
		  StreamProxy f = tofile(L); 
		  lua_pushvalue(L, 1);  /* push file at the stack top (to be returned) */
		  return g_write(L, f, 2);
		}
		

		private readonly static int[] f_seek_mode = { SEEK_SET, SEEK_CUR, SEEK_END }; //FIXME: ???static const???
		private readonly static CharPtr[] f_seek_modenames = { "set", "cur", "end", null }; //FIXME: ???static const???
		private static int f_seek (lua_State L) {
		  StreamProxy f = tofile(L);
		  int op = luaL_checkoption(L, 2, "cur", f_seek_modenames);
		  lua_Number p3 = luaL_optnumber(L, 3, 0);
		  l_seeknum offset = (l_seeknum)p3;
		  luaL_argcheck(L, (lua_Number)offset == p3, 3,
		                  "not an integer in proper range");
		  op = l_fseek(f, offset, f_seek_mode[op]);
		  if (op != 0)
			return luaL_fileresult(L, 0, null);  /* error */
		  else {
			lua_pushnumber(L, (lua_Number)l_ftell(f));
			return 1;
		  }
		}

		private readonly static int[] f_setvbuf_mode = {_IONBF, _IOFBF, _IOLBF}; //FIXME: ???static const???
		private readonly static CharPtr[] f_setvbuf_modenames = {"no", "full", "line", null}; //FIXME: ???static const???
		private static int f_setvbuf (lua_State L) {
		  StreamProxy f = tofile(L);
		  int op = luaL_checkoption(L, 2, null, f_setvbuf_modenames);
		  lua_Integer sz = luaL_optinteger(L, 3, LUAL_BUFFERSIZE);
		  int res = setvbuf(f, null, f_setvbuf_mode[op], (uint)sz);
		  return luaL_fileresult(L, (res == 0) ? 1 : 0, null);
		}



		private static int io_flush (lua_State L) {
		  return luaL_fileresult(L, (fflush(getiofile(L, IO_OUTPUT))==0)?1:0, null);
		}


		private static int f_flush (lua_State L) {
			return luaL_fileresult(L, (fflush(tofile(L))==0)?1:0, null);
		}


		/*
		** functions for 'io' library
		*/
		private readonly static luaL_Reg[] iolib = {
		  new luaL_Reg("close", io_close),
		  new luaL_Reg("flush", io_flush),
		  new luaL_Reg("input", io_input),
		  new luaL_Reg("lines", io_lines),
		  new luaL_Reg("open", io_open),
		  new luaL_Reg("output", io_output),
		  new luaL_Reg("popen", io_popen),
		  new luaL_Reg("read", io_read),
		  new luaL_Reg("tmpfile", io_tmpfile),
		  new luaL_Reg("type", io_type),
		  new luaL_Reg("write", io_write),
		  new luaL_Reg(null, null)
		};


		/*
		** methods for file handles
		*/
		private readonly static luaL_Reg[] flib = {
		  new luaL_Reg("close", io_close),
		  new luaL_Reg("flush", f_flush),
		  new luaL_Reg("lines", f_lines),
		  new luaL_Reg("read", f_read),
		  new luaL_Reg("seek", f_seek),
		  new luaL_Reg("setvbuf", f_setvbuf),
		  new luaL_Reg("write", f_write),
		  new luaL_Reg("__gc", f_gc),
		  new luaL_Reg("__tostring", f_tostring),
		  new luaL_Reg(null, null)
		};


		private static void createmeta (lua_State L) {
		  luaL_newmetatable(L, LUA_FILEHANDLE);  /* create metatable for file files */
		  lua_pushvalue(L, -1);  /* push metatable */
		  lua_setfield(L, -2, "__index");  /* metatable.__index = metatable */
		  luaL_setfuncs(L, flib, 0);  /* add file methods to new metatable */
  		  lua_pop(L, 1);  /* pop new metatable */
		}


		/*
		** function to (not) close the standard files stdin, stdout, and stderr
		*/
		private static int io_noclose (lua_State L) {
		  LStream p = tolstream(L);
		  p.closef = io_noclose;  /* keep file opened */
		  lua_pushnil(L);
		  lua_pushliteral(L, "cannot close standard file");
		  return 2;
		}


		private static void createstdfile (lua_State L, StreamProxy f, CharPtr k, 
		                                   CharPtr fname) {
		  LStream p = newprefile(L);
		  p.f = f;
          p.closef = io_noclose;
		  if (k != null) {
			lua_pushvalue(L, -1);
			lua_setfield(L, LUA_REGISTRYINDEX, k);  /* add file to registry */
		  }
		  lua_setfield(L, -2, fname);  /* add file to module */
		}


		public static int luaopen_io (lua_State L) {
		  luaL_newlib(L, iolib);  /* new module */
		  createmeta(L);
		  /* create (and set) default files */
		  createstdfile(L, stdin, IO_INPUT, "stdin");
		  createstdfile(L, stdout, IO_OUTPUT, "stdout");
		  createstdfile(L, stderr, null, "stderr");
		  return 1;
		}

	}
}

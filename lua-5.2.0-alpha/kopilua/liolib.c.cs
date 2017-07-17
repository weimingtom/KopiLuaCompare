/*
** $Id: liolib.c,v 2.94 2010/11/09 16:57:49 roberto Exp roberto $
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

	public class FilePtr
	{
		public Stream file;
	}

	public partial class Lua
	{

		private const uint MAX_SIZE_T = uint.MaxValue;//(int)(~(uint)0); //FIXME: ??? == 0xffffffff


		/*
		** lua_popen spawns a new process connected to the current one through
		** the file streams.
		*/
		//#if !defined(lua_popen)

		//#if defined(LUA_USE_POPEN)

		//#define lua_popen(L,c,m)        ((void)L, fflush(NULL), popen(c,m))
		//#define lua_pclose(L,file)      ((void)L, pclose(file))

		//#elif defined(LUA_WIN)

		private static Stream lua_popen(lua_State L, CharPtr c, CharPtr m)  { /*(void)L,*/ return _popen(c,m); }
		private static int lua_pclose(lua_State L, Stream file)  { /*(void)L,*/ return _pclose(file); }

		//#else

		//#define lua_popen(L,c,m)        ((void)((void)c, m),  \
		//                luaL_error(L, LUA_QL("popen") " not supported"), (FILE*)0)
		//#define lua_pclose(L,file)              ((void)((void)L, file), -1)

		//#endif

		//#endif



		public const int IO_INPUT	= 1;
		public const int IO_OUTPUT	= 2;

		private static readonly string[] fnames = { "input", "output" };


		private static int pushresult (lua_State L, int i, CharPtr filename) {
		  int en = errno();  /* calls to Lua API may change this value */
		  if (i != 0) {
			lua_pushboolean(L, 1);
			return 1;
		  }
		  else {
			lua_pushnil(L);
			if (filename != null)
				lua_pushfstring(L, "%s: %s", filename, strerror(en));
			else
				lua_pushfstring(L, "%s", strerror(en));
			lua_pushinteger(L, en);
			return 3;
		  }
		}


		private static void fileerror (lua_State L, int arg, CharPtr filename) {
		  lua_pushfstring(L, "%s: %s", filename, strerror(errno()));
		  luaL_argerror(L, arg, lua_tostring(L, -1));
		}


		public static FilePtr tofilep(lua_State L) { return (FilePtr)luaL_checkudata(L, 1, LUA_FILEHANDLE); }


		private static int io_type (lua_State L) {
		  object ud;
		  luaL_checkany(L, 1);
		  ud = luaL_testudata(L, 1, LUA_FILEHANDLE);
          if (ud == null)
			lua_pushnil(L);  /* not a file */
		  else if ( (ud as FilePtr).file == null)
			lua_pushliteral(L, "closed file");
		  else
			lua_pushliteral(L, "file");
		  return 1;
		}


		private static Stream tofile (lua_State L) {
		  FilePtr f = tofilep(L);
		  if (f.file == null)
			luaL_error(L, "attempt to use a closed file");
		  return f.file;
		}



		/*
		** When creating file files, always creates a `closed' file file
		** before opening the actual file; so, if there is a memory error, the
		** file is not left opened.
		*/
		private static FilePtr newprefile (lua_State L) {
		  FilePtr pf = (FilePtr)lua_newuserdata(L, typeof(FilePtr));
		  pf.file = null;  /* file file is currently `closed' */
		  luaL_setmetatable(L, LUA_FILEHANDLE);
		  return pf;
		}


		private static FilePtr newfile (lua_State L) {
		  FilePtr pf = newprefile(L);
		  lua_pushvalue(L, lua_upvalueindex(1));  /* set upvalue... */
		  lua_setuservalue(L, -2);  /* ... as environment for new file */
		  return pf;
		}


		/*
		** function to (not) close the standard files stdin, stdout, and stderr
		*/
		private static int io_noclose (lua_State L) {
		  lua_pushnil(L);
		  lua_pushliteral(L, "cannot close standard file");
		  return 2;
		}


		/*
		** function to close 'popen' files
		*/
		private static int io_pclose (lua_State L) {
		  FilePtr p = tofilep(L);
		  int stat = lua_pclose(L, p.file);
		  p.file = null;
		  if (stat == -1)  /* error? */
		    return pushresult(L, 0, null);
		  else {
		    lua_pushinteger(L, stat);
		    return 1;  /* return status */
		  }
		}


		/*
		** function to close regular files
		*/
		private static int io_fclose (lua_State L) {
		  FilePtr p = tofilep(L);
		  int ok = (fclose(p.file) == 0) ? 1 : 0;
		  p.file = null;
		  return pushresult(L, ok, null);
		}


		private static int aux_close (lua_State L) {
		  lua_getuservalue(L, 1);
		  lua_getfield(L, -1, "__close");
		  return (lua_tocfunction(L, -1))(L);
		}


		private static int io_close (lua_State L) {
		  if (lua_isnone(L, 1))
			lua_rawgeti(L, lua_upvalueindex(1), IO_OUTPUT);
		  tofile(L);  /* make sure argument is a file */
		  return aux_close(L);
		}


		private static int io_gc (lua_State L) {
		  Stream f = tofilep(L).file; //FIXME:FilePtr p = tofilep(L);
		  /* ignore closed files */
		  if (f != null)
			aux_close(L);
		  return 0;
		}


		private static int io_tostring (lua_State L) {
		  Stream f = tofilep(L).file; //FIXME:FilePtr p = tofilep(L);
		  if (f == null)
			lua_pushliteral(L, "file (closed)");
		  else
			lua_pushfstring(L, "file (%p)", f);
		  return 1;
		}


		private static int io_open (lua_State L) {
		  CharPtr filename = luaL_checkstring(L, 1);
		  CharPtr mode = luaL_optstring(L, 2, "r");
		  FilePtr pf;
		  int i = 0;
		  /* check whether 'mode' matches '[rwa]%+?b?' */
		  if (!(mode[i] != '\0' && strchr("rwa", mode[i++]) != null &&
		       (mode[i] != '+' || ++i != 0) &&    /* skip if char is '+' */
		       (mode[i] != 'b' || ++i != 0) &&    /* skip if char is 'b' */
		       (mode[i] == '\0')))
		    luaL_error(L, "invalid mode " + LUA_QL("%s") +
		                  " (should match " + LUA_QL("[rwa]%%+?b?") + ")", mode);
		  pf = newfile(L);
		  pf.file = fopen(filename, mode);
		  return (pf.file == null) ? pushresult(L, 0, filename) : 1;
		}


		/*
		** this function has a separated environment, which defines the
		** correct __close for 'popen' files
		*/
		private static int io_popen (lua_State L) {
		  CharPtr filename = luaL_checkstring(L, 1);
		  CharPtr mode = luaL_optstring(L, 2, "r");
		  FilePtr pf = newfile(L);
		  pf.file = lua_popen(L, filename, mode);
		  return (pf.file == null) ? pushresult(L, 0, filename) : 1;
		}


		private static int io_tmpfile (lua_State L) {
		  FilePtr pf = newfile(L);
		  pf.file = tmpfile();
		  return (pf.file == null) ? pushresult(L, 0, null) : 1;
		}


		private static Stream getiofile (lua_State L, int findex) {
		  Stream f;
		  lua_rawgeti(L, lua_upvalueindex(1), findex);
		  f = (lua_touserdata(L, -1) as FilePtr).file;
		  if (f == null)
			luaL_error(L, "standard %s file is closed", fnames[findex - 1]);
		  return f;
		}


		private static int g_iofile (lua_State L, int f, CharPtr mode) {
		  if (!lua_isnoneornil(L, 1)) {
			CharPtr filename = lua_tostring(L, 1);
			if (filename != null) {
			  FilePtr pf = newfile(L);
			  pf.file = fopen(filename, mode);
			  if (pf.file == null)
				fileerror(L, 1, filename);
			}
			else {
			  tofile(L);  /* check that it's a valid file file */
			  lua_pushvalue(L, 1);
			}
			lua_rawseti(L, lua_upvalueindex(1), f);
		  }
		  /* return current value */
		  lua_rawgeti(L, lua_upvalueindex(1), f);
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
		    lua_rawgeti(L, lua_upvalueindex(1), IO_INPUT);  /* get default input */
		    lua_replace(L, 1);   /* put it at index 1 */
		    tofile(L);  /* check that it's a valid file handle */
		    toclose = 0;  /* do not close it after iteration */
		  }
		  else {  /* open a new file */
		    CharPtr filename = luaL_checkstring(L, 1);
		    FilePtr pf = newfile(L);
		    pf.file = fopen(filename, "r");
		    if (pf.file == null)
		      fileerror(L, 1, filename);
		    lua_replace(L, 1);   /* put file at index 1 */
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


		private static int read_number (lua_State L, Stream f) {
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


		private static int test_eof (lua_State L, Stream f) {
		  int c = getc(f);
		  ungetc(c, f);
		  lua_pushlstring(L, null, 0);
		  return (c != EOF) ? 1 : 0;
		}


		private static int read_line (lua_State L, Stream f, int chop) {
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


		private static void read_all (lua_State L, Stream f) {
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


		private static int read_chars (lua_State L, Stream f, uint n) {
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


		private static int g_read (lua_State L, Stream f, int first) {
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
			return pushresult(L, 0, null);
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
		  Stream f = ((FilePtr)lua_touserdata(L, lua_upvalueindex(1))).file;
		  int i;
		  int n = (int)lua_tointeger(L, lua_upvalueindex(2));
		  if (f == null)  /* file is already closed? */
		    luaL_error(L, "file is already closed");
		  lua_settop(L , 1);
		  for (i = 1; i <= n; i++)  /* push arguments to 'g_read' */
		    lua_pushvalue(L, lua_upvalueindex(3 + i));
		  n = g_read(L, f, 2);  /* 'n' is number of results */
		  lua_assert(n > 0);  /* should return at least a nil */
		  if (!lua_isnil(L, -n))  /* read at least one value? */
		    return n;  /* return them */
		  else {  /* first result is nil: EOF or error */
		    if (!lua_isnil(L, -1))  /* is there error information? */
		      return luaL_error(L, "%s", lua_tostring(L, -1));  /* error */
		    /* else EOF */
		    if (lua_toboolean(L, lua_upvalueindex(3))!=0) {  /* generator created file? */
		      lua_settop(L, 0);
		      lua_pushvalue(L, lua_upvalueindex(1));
		      aux_close(L);  /* close it */
		    }
		    return 0;
		  }
		}

		/* }====================================================== */


		private static int g_write (lua_State L, Stream f, int arg) {
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
			  status = ((status!=0) && (fwrite(s, GetUnmanagedSize(typeof(char)), (int)l, f) == l)) ? 1 : 0;
			}
		  }
		  if (status != 0) return 1;  /* file handle already on stack top */
		  else return pushresult(L, status, null);
		}


		private static int io_write (lua_State L) {
		  return g_write(L, getiofile(L, IO_OUTPUT), 1);
		}


		private static int f_write (lua_State L) {
		  Stream f = tofile(L); 
		  lua_pushvalue(L, 1);  /* push file at the stack top (to be returned) */
		  return g_write(L, f, 2);
		}
		

		private static int f_seek (lua_State L) {
		  int[] mode = { SEEK_SET, SEEK_CUR, SEEK_END }; //FIXME: ???static const???
		  CharPtr[] modenames = { "set", "cur", "end", null }; //FIXME: ???static const???
		  Stream f = tofile(L);
		  int op = luaL_checkoption(L, 2, "cur", modenames);
		  long offset = luaL_optlong(L, 3, 0);
		  op = fseek(f, offset, mode[op]);
		  if (op != 0)
			return pushresult(L, 0, null);  /* error */
		  else {
			lua_pushinteger(L, ftell(f));
			return 1;
		  }
		}


		private static int f_setvbuf (lua_State L) {
		  int[] mode = {_IONBF, _IOFBF, _IOLBF}; //FIXME: ???static const???
		  CharPtr[] modenames = {"no", "full", "line", null}; //FIXME: ???static const???
		  Stream f = tofile(L);
		  int op = luaL_checkoption(L, 2, null, modenames);
		  lua_Integer sz = luaL_optinteger(L, 3, LUAL_BUFFERSIZE);
		  int res = setvbuf(f, null, mode[op], (uint)sz);
		  return pushresult(L, (res == 0) ? 1 : 0, null);
		}



		private static int io_flush (lua_State L) {
			int result = 1;//FIXME: added
			try {getiofile(L, IO_OUTPUT).Flush();} catch {result = 0;}//FIXME: added
		  return pushresult(L, result, null); //FIXME: changed
		}


		private static int f_flush (lua_State L) {
			int result = 1;//FIXME: added
			try {tofile(L).Flush();} catch {result = 0;} //FIXME: added
			return pushresult(L, result, null); //FIXME: changed
		}


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


		private readonly static luaL_Reg[] flib = {
		  new luaL_Reg("close", io_close),
		  new luaL_Reg("flush", f_flush),
		  new luaL_Reg("lines", f_lines),
		  new luaL_Reg("read", f_read),
		  new luaL_Reg("seek", f_seek),
		  new luaL_Reg("setvbuf", f_setvbuf),
		  new luaL_Reg("write", f_write),
		  new luaL_Reg("__gc", io_gc),
		  new luaL_Reg("__tostring", io_tostring),
		  new luaL_Reg(null, null)
		};


		private static void createmeta (lua_State L) {
		  luaL_newmetatable(L, LUA_FILEHANDLE);  /* create metatable for file files */
		  lua_pushvalue(L, -1);  /* push metatable */
		  lua_setfield(L, -2, "__index");  /* metatable.__index = metatable */
		  luaL_setfuncs(L, flib, 0);  /* add file methods to new metatable */
  		  lua_pop(L, 1);  /* pop new metatable */
		}


		private static void createstdfile (lua_State L, Stream f, int k, CharPtr fname) {
		  newprefile(L).file = f;
		  if (k > 0) {
			lua_pushvalue(L, -1);  /* copy new file */
			lua_rawseti(L, 1, k);  /* add it to common upvalue */
		  }
		  lua_pushvalue(L, 3);  /* get environment for default files */
		  lua_setuservalue(L, -2);  /* set it as environment for file */
		  lua_setfield(L, 2, fname);  /* add file to module */
		}


		/*
		** pushes a new table with {__close = cls}
		*/
		private static void newenv (lua_State L, lua_CFunction cls) {
		  lua_createtable(L, 0, 1);
		  lua_pushcfunction(L, cls);
		  lua_setfield(L, -2, "__close");
		}


		public static int luaopen_io (lua_State L) {
		  lua_settop(L, 0);
		  createmeta(L);
		  /* create (private) environment (with fields IO_INPUT, IO_OUTPUT, __close) */
		  newenv(L, io_fclose);  /* upvalue for all io functions at index 1 */
		  luaL_newlibtable(L, iolib);  /* new module at index 2 */
		  lua_pushvalue(L, 1);  /* copy of env to be consumed by 'setfuncs' */
		  luaL_setfuncs(L, iolib, 1);
		  /* create (and set) default files */
		  newenv(L, io_noclose);  /* environment for default files at index 3 */
		  createstdfile(L, stdin, IO_INPUT, "stdin");
		  createstdfile(L, stdout, IO_OUTPUT, "stdout");
		  createstdfile(L, stderr, 0, "stderr");
		  lua_pop(L, 1);  /* pop environment for default files */
		  lua_getfield(L, 2, "popen");
		  newenv(L, io_pclose);  /* create environment for 'popen' streams */
		  lua_setupvalue(L, -2, 1);  /* set it as upvalue for 'popen' */
		  lua_pop(L, 1);  /* pop 'popen' */
		  return 1;
		}

	}
}

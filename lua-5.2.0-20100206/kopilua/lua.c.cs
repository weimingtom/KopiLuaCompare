/*
** $Id: lua.c,v 1.183 2010/01/21 16:31:06 roberto Exp roberto $
** Lua stand-alone interpreter
** See Copyright Notice in lua.h
*/


using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using KopiLua;

namespace KopiLua
{
	public class Program
	{
		//#define lua_c

		//#include "lua.h"

		//#include "lauxlib.h"
		//#include "lualib.h"


		//#if !defined(LUA_PROMPT)
		private const string LUA_PROMPT	= "> ";
		private const string LUA_PROMPT2 = ">> ";
		//#endif

		//#if !defined(LUA_PROGNAME)
		private const string LUA_PROGNAME = "lua";
		//#endif

		//#if !defined(LUA_MAXINPUT)
		private const int LUA_MAXINPUT = 512;
		//#endif

		//#if !defined(LUA_INIT_VAR)
		private const string LUA_INIT_VAR = "LUA_INIT";
		//#endif


		/*
		** lua_stdin_is_tty detects whether the standard input is a 'tty' (that
		** is, whether we're running lua interactively).
		*/
		//#if defined(LUA_USE_ISATTY)
		//#include <unistd.h>
		//#define lua_stdin_is_tty()      isatty(0)
		//#elif defined(LUA_WIN)
		//#include <io.h>
		//#include <stdio.h>
		//#define lua_stdin_is_tty()      _isatty(_fileno(stdin))
		//#else
		//#define lua_stdin_is_tty()      1  /* assume stdin is a tty */
		//#endif


		/*
		** lua_readline defines how to show a prompt and then read a line from
		** the standard input.
		** lua_saveline defines how to "save" a read line in a "history".
		** lua_freeline defines how to free a line read by lua_readline.
		*/
		//#if defined(LUA_USE_READLINE)

		//#include <stdio.h>
		//#include <readline/readline.h>
		//#include <readline/history.h>
		//#define lua_readline(L,b,p)     ((void)L, ((b)=readline(p)) != NULL)
		//#define lua_saveline(L,idx) \
		//        if (lua_rawlen(L,idx) > 0)  /* non-empty line? */ \
		//          add_history(lua_tostring(L, idx));  /* add it to history */
		//#define lua_freeline(L,b)       ((void)L, free(b))

		//#elif !defined(lua_readline)

		//#define lua_readline(L,b,p)     \
		//        ((void)L, fputs(p, stdout), fflush(stdout),  /* show prompt */ \
		//        fgets(b, LUA_MAXINPUT, stdin) != NULL)  /* get line */
		//#define lua_saveline(L,idx)     { (void)L; (void)idx; }
		//#define lua_freeline(L,b)       { (void)L; (void)b; }

		//#endif




		static Lua.lua_State globalL = null;

		static Lua.CharPtr progname = Lua.LUA_PROGNAME;



		static void lstop(Lua.lua_State L, Lua.lua_Debug ar) {
            //(void)ar;  /* unused arg. */
			Lua.lua_sethook(L, null, 0, 0);
			Lua.luaL_error(L, "interrupted!");
		}


		static void laction(int i) {
			//signal(i, SIG_DFL); /* if another SIGINT happens before lstop,
			//						  terminate process (default action) */
			Lua.lua_sethook(globalL, lstop, Lua.LUA_MASKCALL | Lua.LUA_MASKRET | Lua.LUA_MASKCOUNT, 1);
		}


		static void print_usage(char badoption) {
			Console.Error.Write(
            "%s: unrecognized option '-%c'\n" + 
			"usage: {0} [options] [script [args]]\n" +
			"Available options are:\n" +
			"  -e stat  execute string " + Lua.LUA_QL("stat").ToString() + "\n" +
			"  -i       enter interactive mode after executing " + Lua.LUA_QL("script").ToString() + "\n" +
			"  -l name  require library " + Lua.LUA_QL("name").ToString() + "\n" +
			"  -v       show version information\n" +
			"  --       stop handling options\n" +
			"  -        stop handling options and execute stdin\n"
			,
			progname, badoption, progname);
			Console.Error.Flush();
		}


		static void l_message(Lua.CharPtr pname, Lua.CharPtr msg) {
			if (pname != null) Lua.fprintf(Lua.stderr, "%s: ", pname);
			Lua.fprintf(Lua.stderr, "%s\n", msg);
			Lua.fflush(Lua.stderr);
		}


		static int report(Lua.lua_State L, int status) {
			if ((status != Lua.LUA_OK) && !Lua.lua_isnil(L, -1)) {
				Lua.CharPtr msg = Lua.lua_tostring(L, -1);
				if (msg == null) msg = "(error object is not a string)";
				l_message(progname, msg);
				Lua.lua_pop(L, 1);
			    /* force a complete garbage collection in case of errors */
			    Lua.lua_gc(L, Lua.LUA_GCCOLLECT, 0);
			}
			return status;
		}


		/* the next function is called unprotected, so it must avoid errors */
		private static void finalreport (Lua.lua_State L, int status) {
		  if (status != LUA_OK) {
		    const char *msg = (lua_type(L, -1) == LUA_TSTRING) ? lua_tostring(L, -1)
		                                                       : NULL;
		    if (msg == NULL) msg = "(error object is not a string)";
		    l_message(progname, msg);
		    lua_pop(L, 1);
		  }
		}


		static int traceback(Lua.lua_State L) {
		  Lua.CharPtr msg = Lua.lua_tostring(L, 1);
		  if (msg != null)
		    Lua.luaL_traceback(L, L, msg, 1);
		  else if (!Lua.lua_isnoneornil(L, 1)) {  /* is there an error object? */
		    if (Lua.luaL_callmeta(L, 1, "__tostring") == 0)  /* try its 'tostring' metamethod */
		      Lua.lua_pushliteral(L, "(no error message)");
		  }
		  return 1;
		}


		static int docall(Lua.lua_State L, int narg, int clear) {
			int status;
			int base_ = Lua.lua_gettop(L) - narg;  /* function index */
			Lua.lua_pushcfunction(L, traceback);  /* push traceback function */
			Lua.lua_insert(L, base_);  /* put it under chunk and args */
            globalL = L;  /* to be available to 'laction' */
			//signal(SIGINT, laction);
			status = Lua.lua_pcall(L, narg, ((clear!=0) ? 0 : Lua.LUA_MULTRET), base_);
			//signal(SIGINT, SIG_DFL);
			Lua.lua_remove(L, base_);  /* remove traceback function */
			return status;
		}


		static void print_version() {
			Lua.printf("%s\n", Lua.LUA_COPYRIGHT);
		}


		static int getargs(Lua.lua_State L, string[] argv, int n) {
			int narg;
			int i;
			int argc = argv.Length;	/* count total number of arguments */ //FIXME:changed here
			narg = argc - (n + 1);  /* number of arguments to the script */
			Lua.luaL_checkstack(L, narg + 3, "too many arguments to script");
			for (i = n + 1; i < argc; i++)
			Lua.lua_pushstring(L, argv[i]);
			Lua.lua_createtable(L, narg, n + 1);
			for (i = 0; i < argc; i++) {
				Lua.lua_pushstring(L, argv[i]);
				Lua.lua_rawseti(L, -2, i - n);
			}
			return narg;
		}


		static int dofile(Lua.lua_State L, Lua.CharPtr name) {
			int status = Lua.luaL_loadfile(L, name);
            if (status == Lua.LUA_OK) status = docall(L, 0, 1);
			return report(L, status);
		}


		static int dostring(Lua.lua_State L, Lua.CharPtr s, Lua.CharPtr name) {
			int status = Lua.luaL_loadbuffer(L, s, (uint)Lua.strlen(s), name);
			if (status == Lua.LUA_OK) status = docall(L, 0, 1);
			return report(L, status);
		}


		static int dolibrary(Lua.lua_State L, Lua.CharPtr name) {
			Lua.lua_getfield(L, LUA_ENVIRONINDEX, "require");
			Lua.lua_pushstring(L, name);
			return report(L, docall(L, 1, 1));
		}


		static Lua.CharPtr get_prompt(Lua.lua_State L, int firstline) {
			Lua.CharPtr p;
			Lua.lua_getfield(L, Lua.LUA_ENVIRONINDEX, (firstline!=0) ? "_PROMPT" : "_PROMPT2");
			p = Lua.lua_tostring(L, -1);
			if (p == null) p = ((firstline!=0) ? Lua.LUA_PROMPT : Lua.LUA_PROMPT2);
			Lua.lua_pop(L, 1);  /* remove global */
			return p;
		}

		/* mark in error messages for incomplete statements */
		private static Lua.CharPtr mark	= "<eof>";
		private static int marklen = mark.chars.Length - 1; //FIXME:???

		static int incomplete(Lua.lua_State L, int status) {
			if (status == Lua.LUA_ERRSYNTAX) {
				uint lmsg;
				Lua.CharPtr msg = Lua.lua_tolstring(L, -1, out lmsg);
				if (lmsg >= marklen && Lua.strcmp(msg + lmsg - marklen, mark) == 0) {
					Lua.lua_pop(L, 1);
					return 1;
				}
			}
			return 0;  /* else... */
		}


		static int pushline(Lua.lua_State L, int firstline) {
			Lua.CharPtr buffer = new char[Lua.LUA_MAXINPUT];
			Lua.CharPtr b = new Lua.CharPtr(buffer);
			int l;
			Lua.CharPtr prmt = get_prompt(L, firstline);
			if (!Lua.lua_readline(L, b, prmt))
				return 0;  /* no input */
			l = Lua.strlen(b);
			if (l > 0 && b[l - 1] == '\n')  /* line ends with newline? */
				b[l - 1] = '\0';  /* remove it */
			if ((firstline!=0) && (b[0] == '='))  /* first line starts with `=' ? */
				Lua.lua_pushfstring(L, "return %s", b + 1);  /* change it to `return' */
			else
				Lua.lua_pushstring(L, b);
			Lua.lua_freeline(L, b);
			return 1;
		}


		static int loadline(Lua.lua_State L) {
			int status;
			Lua.lua_settop(L, 0);
			if (pushline(L, 1)==0)
				return -1;  /* no input */
			for (;;) {  /* repeat until gets a complete line */
			    uint l;
			    CharPtr line = lua_tolstring(L, 1, &l);
			    status = luaL_loadbuffer(L, line, l, "=stdin");
				if (incomplete(L, status)==0) break;  /* cannot try to add lines? */
				if (pushline(L, 0)==0)  /* no more input? */
					return -1;
				Lua.lua_pushliteral(L, "\n");  /* add a new line... */
				Lua.lua_insert(L, -2);  /* ...between the two lines */
				Lua.lua_concat(L, 3);  /* join them */
			}
			Lua.lua_saveline(L, 1);
			Lua.lua_remove(L, 1);  /* remove line */
			return status;
		}


		static void dotty(Lua.lua_State L) {
			int status;
			Lua.CharPtr oldprogname = progname;
			progname = null;
			while ((status = loadline(L)) != -1) {
				if (status == Lua.LUA_OK) status = docall(L, 0, 0);
				report(L, status);
				if (status == Lua.LUA_OK && Lua.lua_gettop(L) > 0) {  /* any result to print? */
				    luaL_checkstack(L, LUA_MINSTACK, "too many results to print");
				    lua_getfield(L, LUA_ENVIRONINDEX, "print");
					Lua.lua_insert(L, 1);
					if (Lua.lua_pcall(L, Lua.lua_gettop(L) - 1, 0, 0) != Lua.LUA_OK)
						l_message(progname, Lua.lua_pushfstring(L,
											   "error calling " + Lua.LUA_QL("print").ToString() + " (%s)",
											   Lua.lua_tostring(L, -1)));
				}
			}
			Lua.lua_settop(L, 0);  /* clear stack */
			Lua.luai_writestring("\n", 1);
			Lua.fflush(Lua.stdout);
			progname = oldprogname;
		}


		static int handle_script(Lua.lua_State L, string[] argv, int n) {
			int status;
			Lua.CharPtr fname;
			int narg = getargs(L, argv, n);  /* collect arguments */
			Lua.lua_setfield(L, LUA_ENVIRONINDEX, "arg");
			fname = argv[n];
			if (Lua.strcmp(fname, "-") == 0 && Lua.strcmp(argv[n - 1], "--") != 0)
				fname = null;  /* stdin */
			status = Lua.luaL_loadfile(L, fname);
			Lua.lua_insert(L, -(narg + 1));
			if (status == Lua.LUA_OK)
				status = docall(L, narg, 0);
			else
				Lua.lua_pop(L, narg);
			return report(L, status);
		}


		/* check that argument has no extra characters at the end */
		//#define noextrachars(x)	{if ((x)[2] != '\0') return -1;} //FIXME:???


		private static int collectargs(string[] argv, ref int pi, ref int pv, ref int pe) {
			int i;
			for (i = 1; i < argv.Length; i++) {
				if (argv[i][0] != '-')  /* not an option? */
					return i;
				switch (argv[i][1]) {  /* option */
					case '-':
						if (argv[i].Length != 2) return -1; //FIXME:???noextrachars
						return (i + 1) >= argv.Length ? i + 1 : 0;
					case '\0':
						return i;
					case 'i':
						if (argv[i].Length != 2) return -1; //FIXME:???noextrachars
						pi = 1; /* go through */ //FIXME:changed here
						if (argv[i].Length != 2) return -1; //FIXME:???noextrachars
						pv = 1;
						break;
					case 'v':
						if (argv[i].Length != 2) return -1; //FIXME:???noextrachars
						pv = 1;
						break;
					case 'e':
						pe = 1;  /* go through */ //FIXME:
						if (argv[i].Length == 2)
						{
							i++;
							if (argv[i] == null) return -1;
						}
						break;
					case 'l':
						if (argv[i].Length == 2) {
							i++;
							if (i >= argv.Length) return -1;
						}
						break;
					default:  /* invalid option; return the offendind character as a... */ 
					    return -(byte)argv[i][1];  /* ...negative value */
				}
			}
			return 0;
		}


		static int runargs(Lua.lua_State L, string[] argv, int n) {
			int i;
			for (i = 1; i < n; i++) {
				if (argv[i] == null) continue;
				Lua.lua_assert(argv[i][0] == '-');
				switch (argv[i][1]) {  /* option */
					case 'e': {
						string chunk = argv[i].Substring(2);
						if (chunk == "") chunk = argv[++i];
						Lua.lua_assert(chunk != null);
						if (dostring(L, chunk, "=(command line)") != Lua.LUA_OK)
							return 0;
						break;
					}
					case 'l': {
						string filename = argv[i].Substring(2);
						if (filename == "") filename = argv[++i];
						Lua.lua_assert(filename != null);
						if (dolibrary(L, filename) != Lua.LUA_OK)
							return 0;  /* stop if file fails */
						break;
					}
					default: break;
				}
			}
			return 1;
		}


		static int handle_luainit(Lua.lua_State L) {
			Lua.CharPtr init = Lua.getenv(Lua.LUA_INIT_VAR);
			if (init == null) return Lua.LUA_OK;  /* status OK */
			else if (init[0] == '@')
				return dofile(L, init + 1);
			else
				return dostring(L, init, "=" + Lua.LUA_INIT_VAR);
		}


		static int pmain(Lua.lua_State L) {
			int argc = lua_tointeger(L, 1);
  		    char **argv = (char **)lua_touserdata(L, 2);
			int script;
			int has_i = 0, has_v = 0, has_e = 0;
			if ((argv.Length>0) && (argv[0]!="")) progname = argv[0];
			script = collectargs(argv, ref has_i, ref has_v, ref has_e);
			if (script < 0) {  /* invalid arg? */
				print_usage(-script);  /* '-script' is the offending argument */
				return 0;
			}
			if (has_v!=0) print_version();
			/* open standard libraries */
			luaL_checkversion(L);
			lua_gc(L, LUA_GCSTOP, 0);  /* stop collector during initialization */
			luaL_openlibs(L);  /* open libraries */
			lua_gc(L, LUA_GCRESTART, 0);
			/* run LUA_INIT */
			if (handle_luainit(L) != LUA_OK) return 0;
			/* execute arguments -e and -l */
			if (!runargs(L, argv, (script > 0) ? script : argc)) return 0;
			/* execute main script (if there is one) */
			if (script && handle_script(L, argv, script) != LUA_OK) return 0;
			if (has_i!=0)  /* -i option? */
				dotty(L);
			else if ((script==0) && (has_e==0) && (has_v==0)) {  /* no arguments? */
				if (Lua.lua_stdin_is_tty()!=0) {
					print_version();
					dotty(L);
				}
				else dofile(L, null);  /* executes stdin as a file */
			}
		    lua_pushboolean(L, 1);  /* signal no errors */
		    return 1;
		}


		public static int Main(string[] args) {
			//FIXME: added
			// prepend the exe name to the arg list as it's done in C
			// so that we don't have to change any of the args indexing
			// code above
			List<string> newargs = new List<string>(args);
			newargs.Insert(0, Assembly.GetExecutingAssembly().Location);
			args = (string[])newargs.ToArray();

			int status, result;
			Lua.lua_State L = Lua.luaL_newstate();  /* create state */
			if (L == null) {
				l_message(args[0], "cannot create state: not enough memory");
				return Lua.EXIT_FAILURE;
			}
			/* call 'pmain' in protected mode */
			lua_pushinteger(L, argc);  /* 1st argument */
			lua_pushlightuserdata(L, argv); /* 2nd argument */
			status = luaL_cpcall(L, &pmain, 2, 1);
			result = lua_toboolean(L, -1);  /* get result */
			finalreport(L, status);
			lua_close(L);
			return (result && status == LUA_OK) ? EXIT_SUCCESS : EXIT_FAILURE;
		}

	}
}

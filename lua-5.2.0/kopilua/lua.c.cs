/*
** $Id: lua.c,v 1.203 2011/12/12 16:34:03 roberto Exp $
** Lua stand-alone interpreter
** See Copyright Notice in lua.h
*/

//#define DEBUG

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

		//#if !defined(LUA_INIT)
		private const string LUA_INIT = "LUA_INIT";
		//#endif

		private const string LUA_INITVERSION = 
			LUA_INIT + "_" + Lua.LUA_VERSION_MAJOR + "_" + Lua.LUA_VERSION_MINOR;


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
		//FIXME:???
		public static int lua_stdin_is_tty() {return 1;}
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

		private static int lua_readline(Lua.lua_State L, Lua.CharPtr b, Lua.CharPtr p) {
		        /*(void)L,*/ Lua.fputs(p, Lua.stdout); Lua.fflush(Lua.stdout);  /* show prompt */
		        return (Lua.fgets(b/*, LUA_MAXINPUT*/, Lua.stdin) != null) ? 1 : 0;}  /* get line */ //FIXME: no Lua_MAXINPUT
		private static void lua_saveline(Lua.lua_State L, int idx)     { /*(void)L; (void)idx;*/ }
		private static void lua_freeline(Lua.lua_State L, object b)       { /*(void)L; (void)b;*/ }

		//#endif




		static Lua.lua_State globalL = null;

		static Lua.CharPtr progname = LUA_PROGNAME;



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


		static void print_usage(Lua.CharPtr badoption) {
		  Lua.luai_writestringerror("%s: ", progname);
		  if (badoption[1] == 'e' || badoption[1] == 'l')
		    Lua.luai_writestringerror("'%s' needs argument\n", badoption);
		  else
		    Lua.luai_writestringerror("unrecognized option '%s'\n", badoption);
		  Lua.luai_writestringerror( //FIXME:???%s
            "usage: %s [options] [script [args]]\n" +
			"Available options are:\n" +
			"  -e stat  execute string " + Lua.LUA_QL("stat").ToString() + "\n" +
			"  -i       enter interactive mode after executing " + Lua.LUA_QL("script").ToString() + "\n" +
			"  -l name  require library " + Lua.LUA_QL("name").ToString() + "\n" +
			"  -v       show version information\n" +
  			"  -E       ignore environment variables\n" +
			"  --       stop handling options\n" +
			"  -        stop handling options and execute stdin\n"
			,
			progname);
		}


		static void l_message(Lua.CharPtr pname, Lua.CharPtr msg) {
			if (pname != null) Lua.luai_writestringerror("%s: ", pname);
  			Lua.luai_writestringerror("%s\n", msg);
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
		  if (status != Lua.LUA_OK) {
		    Lua.CharPtr msg = (Lua.lua_type(L, -1) == Lua.LUA_TSTRING) ? Lua.lua_tostring(L, -1)
		                                                       : null;
		    if (msg == null) msg = "(error object is not a string)";
		    l_message(progname, msg);
		    Lua.lua_pop(L, 1);
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


		static int docall(Lua.lua_State L, int narg, int nres) {
			int status;
			int base_ = Lua.lua_gettop(L) - narg;  /* function index */
			Lua.lua_pushcfunction(L, traceback);  /* push traceback function */
			Lua.lua_insert(L, base_);  /* put it under chunk and args */
            globalL = L;  /* to be available to 'laction' */
			//signal(SIGINT, laction); //FIXME:removed
			status = Lua.lua_pcall(L, narg, nres, base_);
			//signal(SIGINT, SIG_DFL); //FIXME:removed
			Lua.lua_remove(L, base_);  /* remove traceback function */
			return status;
		}


		static void print_version() {
		  Lua.luai_writestring(Lua.LUA_COPYRIGHT, (uint)Lua.strlen(Lua.LUA_COPYRIGHT)); //FIXME:changed, (uint)
		  Lua.luai_writeline();
		}


		static int getargs(Lua.lua_State L, string[] argv, int n) {
			int narg;
			int i;
			int argc = 0;	/* count total number of arguments */ //FIXME:changed here
            argc = argv.Length;  /* count total number of arguments */ //FIXME: changed here
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
            if (status == Lua.LUA_OK) status = docall(L, 0, 0);
			return report(L, status);
		}


		static int dostring(Lua.lua_State L, Lua.CharPtr s, Lua.CharPtr name) {
			int status = Lua.luaL_loadbuffer(L, s, (uint)Lua.strlen(s), name);
			if (status == Lua.LUA_OK) status = docall(L, 0, 0);
			return report(L, status);
		}


		static int dolibrary(Lua.lua_State L, Lua.CharPtr name) {
		  int status;
		  Lua.lua_pushglobaltable(L);
		  Lua.lua_getfield(L, -1, "require");
		  Lua.lua_pushstring(L, name);
		  status = docall(L, 1, 1);
		  if (status == Lua.LUA_OK) {
		    Lua.lua_setfield(L, -2, name);  /* global[name] = require return */
		    Lua.lua_pop(L, 1);  /* remove global table */
		  }
		  else
		    Lua.lua_remove(L, -2);  /* remove global table (below error msg.) */
		  return report(L, status);
		}


		static Lua.CharPtr get_prompt(Lua.lua_State L, int firstline) {
			Lua.CharPtr p;
			Lua.lua_getglobal(L, (firstline!=0) ? "_PROMPT" : "_PROMPT2");
			p = Lua.lua_tostring(L, -1);
			if (p == null) p = ((firstline!=0) ? LUA_PROMPT : LUA_PROMPT2);
			Lua.lua_pop(L, 1);  /* remove global */
			return p;
		}

		/* mark in error messages for incomplete statements */
		private static Lua.CharPtr EOFMARK	= "<eof>";
		private static int marklen = EOFMARK.chars.Length - 1; //FIXME:changed, (sizeof(EOFMARK)/sizeof(char) - 1), ???

		static int incomplete(Lua.lua_State L, int status) {
			if (status == Lua.LUA_ERRSYNTAX) {
				uint lmsg;
				Lua.CharPtr msg = Lua.lua_tolstring(L, -1, out lmsg);
				if (lmsg >= marklen && Lua.strcmp(msg + lmsg - marklen, EOFMARK) == 0) {
					Lua.lua_pop(L, 1);
					return 1;
				}
			}
			return 0;  /* else... */
		}


		static int pushline(Lua.lua_State L, int firstline) {
			Lua.CharPtr buffer = new char[LUA_MAXINPUT];
			Lua.CharPtr b = new Lua.CharPtr(buffer);
			int l;
			Lua.CharPtr prmt = get_prompt(L, firstline);
			if (lua_readline(L, b, prmt) == 0)
				return 0;  /* no input */
			l = Lua.strlen(b);
			if (l > 0 && b[l - 1] == '\n')  /* line ends with newline? */
				b[l - 1] = '\0';  /* remove it */
			if ((firstline!=0) && (b[0] == '='))  /* first line starts with `=' ? */
				Lua.lua_pushfstring(L, "return %s", b + 1);  /* change it to `return' */
			else
				Lua.lua_pushstring(L, b);
			lua_freeline(L, b);
			return 1;
		}


		static int loadline(Lua.lua_State L) {
			int status;
			Lua.lua_settop(L, 0);
			if (pushline(L, 1)==0)
				return -1;  /* no input */
			for (;;) {  /* repeat until gets a complete line */
			    uint l;
			    Lua.CharPtr line = Lua.lua_tolstring(L, 1, out l);
			    status = Lua.luaL_loadbuffer(L, line, l, "=stdin");
				if (incomplete(L, status)==0) break;  /* cannot try to add lines? */
				if (pushline(L, 0)==0)  /* no more input? */
					return -1;
				Lua.lua_pushliteral(L, "\n");  /* add a new line... */
				Lua.lua_insert(L, -2);  /* ...between the two lines */
				Lua.lua_concat(L, 3);  /* join them */
			}
			lua_saveline(L, 1);
			Lua.lua_remove(L, 1);  /* remove line */
			return status;
		}


		static void dotty(Lua.lua_State L) {
			int status;
			Lua.CharPtr oldprogname = progname;
			progname = null;
			while ((status = loadline(L)) != -1) {
				if (status == Lua.LUA_OK) status = docall(L, 0, Lua.LUA_MULTRET);
				report(L, status);
				if (status == Lua.LUA_OK && Lua.lua_gettop(L) > 0) {  /* any result to print? */
				    Lua.luaL_checkstack(L, Lua.LUA_MINSTACK, "too many results to print");
				    Lua.lua_getglobal(L, "print");
					Lua.lua_insert(L, 1);
					if (Lua.lua_pcall(L, Lua.lua_gettop(L) - 1, 0, 0) != Lua.LUA_OK)
						l_message(progname, Lua.lua_pushfstring(L,
											   "error calling " + Lua.LUA_QL("print").ToString() + " (%s)",
											   Lua.lua_tostring(L, -1)));
				}
			}
			Lua.lua_settop(L, 0);  /* clear stack */
			Lua.luai_writeline();
			progname = oldprogname;
		}


		static int handle_script(Lua.lua_State L, string[] argv, int n) {
			int status;
			Lua.CharPtr fname;
			int narg = getargs(L, argv, n);  /* collect arguments */
			Lua.lua_setglobal(L, "arg");
			fname = argv[n];
			if (Lua.strcmp(fname, "-") == 0 && Lua.strcmp(argv[n - 1], "--") != 0)
				fname = null;  /* stdin */
			status = Lua.luaL_loadfile(L, fname);
			Lua.lua_insert(L, -(narg + 1));
			if (status == Lua.LUA_OK)
				status = docall(L, narg, Lua.LUA_MULTRET);
			else
				Lua.lua_pop(L, narg);
			return report(L, status);
		}


		/* check that argument has no extra characters at the end */
		//#define noextrachars(x)	{if ((x)[2] != '\0') return -1;} //FIXME:???


		/* indices of various argument indicators in array args */
		private const int has_i = 0;	/* -i */
		private const int has_v = 1;	/* -v */
		private const int has_e = 2;	/* -e */
		private const int has_E = 3;	/* -E */

		private const int num_has = 4;	/* number of 'has_*' */


		private static int collectargs(string[] argv, int[] args) {
			int i;
			for (i = 1; i < argv.Length; i++) {
				if (argv[i][0] != '-')  /* not an option? */
					return i;
				switch (argv[i][1]) {  /* option */
				  case '-':
					if(argv[i].Length>1) return -1; //FIXME:changed, noextrachars(argv[i]);
			        return (argv[i+1] != null ? i+1 : 0);
			  	  case '\0':
					return i;
				  case 'E':
			        args[has_E] = 1;
			        break;
	   		      case 'i':
				    if(argv[i].Length>1) return -1; //FIXME:changed, noextrachars(argv[i]);
				    args[has_i] = 1;  /* go through */
				    goto case 'v';//FIXME:added
				  case 'v':
				    if(argv[i].Length>1) return -1; //FIXME:changed, noextrachars(argv[i]);
				    args[has_v] = 1;
				    break;
				  case 'e':
				    args[has_e] = 1;  /* go through */
				    goto case 'l';//FIXME:added
				  case 'l':  /* both options need an argument */
				    if (argv[i].Length == 2) {  /* no concatenated argument? */ //FIXME:changed
					  i++;  /* try next 'argv' */
				    if (i >= argv.Length || argv[i][0] == '-')  //FIXME: changed
					  return -(i - 1);  /* no next argument or it is another option */
				    }
				    break;
				  default:  /* invalid option; return its index... */
				    return -i;  /* ...as a negative value */
				}
			}
			return 0;
		}


		static int runargs(Lua.lua_State L, string[] argv, int n) {
			int i;
			for (i = 1; i < n; i++) {
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
		  Lua.CharPtr name = "=" + LUA_INITVERSION;
		  Lua.CharPtr init = Lua.getenv(name + 1);
		  if (init == null) {
		    name = "=" + LUA_INIT;
		    init = Lua.getenv(name + 1);  /* try alternative name */
		  }
		  if (init == null) return Lua.LUA_OK;  /* status OK */
		  else if (init[0] == '@')
			return dofile(L, init + 1);
		  else
			return dostring(L, init, name);
		}


		static int pmain(Lua.lua_State L) {
		  int argc = (int)Lua.lua_tointeger(L, 1);
		  string[] argv = (string[])Lua.lua_touserdata(L, 2);
		  int script;
		  int[] args = new int[num_has];
		  args[has_i] = args[has_v] = args[has_e] = args[has_E] = 0;
		  if (argv.Length > 0 && argv[0].Length > 0) progname = argv[0];
		  script = collectargs(argv, args);
		  if (script < 0) {  /* invalid arg? */
		    print_usage(argv[-script]);
		    return 0;
		  }
		  if (args[has_v]!=0) print_version();
		  if (args[has_E]!=0) {  /* option '-E'? */
		    Lua.lua_pushboolean(L, 1);  /* signal for libraries to ignore env. vars. */
		    Lua.lua_setfield(L, Lua.LUA_REGISTRYINDEX, "LUA_NOENV");
		  }
		  /* open standard libraries */
		  Lua.luaL_checkversion(L);
		  Lua.lua_gc(L, Lua.LUA_GCSTOP, 0);  /* stop collector during initialization */
		  Lua.luaL_openlibs(L);  /* open libraries */
		  Lua.lua_gc(L, Lua.LUA_GCRESTART, 0);
		  if (args[has_E]==0 && handle_luainit(L) != Lua.LUA_OK)
		    return 0;  /* error running LUA_INIT */
		  /* execute arguments -e and -l */
		  if (runargs(L, argv, (script > 0) ? script : argc)==0) return 0;
		  /* execute main script (if there is one) */
		  if (script!=0 && handle_script(L, argv, script) != Lua.LUA_OK) return 0;
		  if (args[has_i]!=0)  /* -i option? */
		    dotty(L);
		  else if (script == 0 && args[has_e]==0 && args[has_v]==0) {  /* no arguments? */
			if (lua_stdin_is_tty()!=0) {
				print_version();
				dotty(L);
			}	
			else dofile(L, null);  /* executes stdin as a file */
		  }
		  Lua.lua_pushboolean(L, 1);  /* signal no errors */
		  return 1;
		}


		public static int Main(string[] args) {
			//Main_(args);
			
			//FIXME: added
			// prepend the exe name to the arg list as it's done in C
			// so that we don't have to change any of the args indexing
			// code above
			List<string> newargs = new List<string>(args);
			newargs.Insert(0, Assembly.GetExecutingAssembly().Location);
			args = (string[])newargs.ToArray();
			int argc = args.Length; //FIXME:???
			string[] argv = args;//FIXME:???

			int status, result;
			Lua.lua_State L = Lua.luaL_newstate();  /* create state */
			if (L == null) {
				l_message(args[0], "cannot create state: not enough memory");
				return Lua.EXIT_FAILURE;
			}
			/* call 'pmain' in protected mode */
            Lua.lua_pushcfunction(L, pmain);
			Lua.lua_pushinteger(L, argc);  /* 1st argument */
			Lua.lua_pushlightuserdata(L, argv); /* 2nd argument */
			status = Lua.lua_pcall(L, 2, 1, 0);
			result = Lua.lua_toboolean(L, -1);  /* get result */
			finalreport(L, status);
			Lua.lua_close(L);
			return (result != 0 && status == Lua.LUA_OK) ? Lua.EXIT_SUCCESS : Lua.EXIT_FAILURE;
		}

		//----------------------------------------
		public const bool DEBUG = false;
		public static int docall_(Lua.lua_State L, int narg, int nres) {
			int status;
			int base_ = Lua.lua_gettop(L) - narg;  /* function index */
			status = Lua.lua_pcall(L, narg, nres, base_);
			return status;
		}
		public static void l_message_(Lua.CharPtr pname, Lua.CharPtr msg) {
			if (pname != null) Lua.luai_writestringerror("%s: ", pname);
  			Lua.luai_writestringerror("%s\n", msg);
		}
		public static Lua.lua_State L_;
		public static string dolua_(string message)
		{
			if (DEBUG)
			{
				Lua.fprintf(Lua.stdout, "%s\n", "==============>" + message);
			}
			if (L_ == null) 
			{
				L_ = Lua.luaL_newstate();
				Lua.luaL_openlibs(L_);
			}

			if (DEBUG)
			{
				Lua.fprintf(Lua.stdout, "%s\n", "==============>2");
			}

			string output = null;
			bool printResult = true;
			int status = Lua.luaL_loadbuffer(L_, message, (uint)Lua.strlen(message), "=stdin");
			if (status == Lua.LUA_OK) {
				if (DEBUG)
				{
					Lua.fprintf(Lua.stdout, "%s\n", "==============>3");
				}
				status = docall_(L_, 0, printResult ? Lua.LUA_MULTRET : 0);
			}
			if ((status != Lua.LUA_OK) && !Lua.lua_isnil(L_, -1)) {
				if (DEBUG)
				{
					Lua.fprintf(Lua.stdout, "%s\n", "==============>4");
				}
				Lua.CharPtr msg = Lua.lua_tostring(L_, -1);
				if (msg == null) msg = "(error object is not a string)";
				output = msg.ToString();
				Lua.lua_pop(L_, 1);
				/* force a complete garbage collection in case of errors */
				Lua.lua_gc(L_, Lua.LUA_GCCOLLECT, 0);
			} 
			if (printResult)
			{
				//see Lua.LUA_MULTRET
				if (status == Lua.LUA_OK && Lua.lua_gettop(L_) > 0) {  /* any result to print? */
					Lua.luaL_checkstack(L_, Lua.LUA_MINSTACK, "too many results to print");
				    Lua.lua_getglobal(L_, "print");
					Lua.lua_insert(L_, 1);
					if (Lua.lua_pcall(L_, Lua.lua_gettop(L_) - 1, 0, 0) != Lua.LUA_OK)
						l_message_(progname, Lua.lua_pushfstring(L_,
											   "error calling " + Lua.LUA_QL("print").ToString() + " (%s)",
											   Lua.lua_tostring(L_, -1)));
				}
			}

			return output;
		}		
		
		public static int Main_(string[] args) {
			Lua.fprintf(Lua.stdout, "%s\n", "hello");
			string result;
			result = dolua_("a = 100");
			if (result != null)
			{
				Lua.fprintf(Lua.stdout, "%s\n", result);
			}
			result = dolua_("print(a)");
			if (result != null)
			{
				Lua.fprintf(Lua.stdout, "%s\n", result);
			}
			return 0;
		}
	}
}

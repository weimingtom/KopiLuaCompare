/*
** $Id: lua.c,v 1.211 2014/06/05 20:42:06 roberto Exp $
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

		//#if !defined(LUA_INIT_VAR)
		private const string LUA_INIT_VAR = "LUA_INIT";
		//#endif

		private const string LUA_INITVARVERSION = 
			LUA_INIT_VAR + "_" + Lua.LUA_VERSION_MAJOR + "_" + Lua.LUA_VERSION_MINOR;


		/*
		** lua_stdin_is_tty detects whether the standard input is a 'tty' (that
		** is, whether we're running lua interactively).
		*/
		//#if !defined(lua_stdin_is_tty)	/* { */

		//#if defined(LUA_USE_POSIX)	/* { */
		
		//#include <unistd.h>
		//#define lua_stdin_is_tty()	isatty(0)
		
		//#elif defined(LUA_WIN)		/* }{ */
		
		//#include <io.h>
		//#define lua_stdin_is_tty()	_isatty(_fileno(stdin))
		
		//#else				/* }{ */
		
		/* ANSI definition */
		//#define lua_stdin_is_tty()	1  /* assume stdin is a tty */
		//FIXME:???
		public static int lua_stdin_is_tty() {return 1;}
		
		//#endif				/* } */
		
		//#endif				/* } */
		
		
		/*
		** lua_readline defines how to show a prompt and then read a line from
		** the standard input.
		** lua_saveline defines how to "save" a read line in a "history".
		** lua_freeline defines how to free a line read by lua_readline.
		*/
		//#if !defined(lua_readline)	/* { */

		//#if defined(LUA_USE_READLINE)	/* { */

		//#include <readline/readline.h>
		//#include <readline/history.h>
		//#define lua_readline(L,b,p)	((void)L, ((b)=readline(p)) != NULL)
		//#define lua_saveline(L,idx) \
		//        if (lua_rawlen(L,idx) > 0)  /* non-empty line? */ \
		//          add_history(lua_tostring(L, idx));  /* add it to history */
		//#define lua_freeline(L,b)	((void)L, free(b))

		//#else				/* }{ */

		private static int lua_readline(Lua.lua_State L, Lua.CharPtr b, Lua.CharPtr p)	{
		        /*(void)L,*/ Lua.fputs(p, Lua.stdout); Lua.fflush(Lua.stdout);  /* show prompt */
		        return (Lua.fgets(b/*, LUA_MAXINPUT*/, Lua.stdin) != null) ? 1 : 0;}  /* get line */ //FIXME: no Lua_MAXINPUT
		private static void lua_saveline(Lua.lua_State L, int idx)	{ /*(void)L; (void)idx;*/ }
		private static void lua_freeline(Lua.lua_State L, object b)	{ /*(void)L; (void)b;*/ }

		//#endif				/* } */

		//#endif				/* } */




		static Lua.lua_State globalL = null;

		static Lua.CharPtr progname = LUA_PROGNAME;


		/*
		** Hook set by signal function to stop the interpreter.
		*/
		static void lstop(Lua.lua_State L, Lua.lua_Debug ar) {
            //(void)ar;  /* unused arg. */
			Lua.lua_sethook(L, null, 0, 0);  /* reset hook */
			Lua.luaL_error(L, "interrupted!");
		}


		/*
		** Function to be called at a C signal. Because a C signal cannot
		** just change a Lua state (as there is no proper syncronization),
		** this function only sets a hook that, when called, will stop the
		** interpreter.
		*/
		static void laction(int i) {
			//signal(i, SIG_DFL); /* if another SIGINT happens, terminate process */
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


		/*
		** Prints an error message, adding the program name in front of it
		** (if present)
		*/
		static void l_message(Lua.CharPtr pname, Lua.CharPtr msg) {
			if (pname != null) Lua.luai_writestringerror("%s: ", pname);
  			Lua.luai_writestringerror("%s\n", msg);
		}


		/*
		** Check whether 'status' is not OK and, if so, prints the error
		** message on the top of the stack. Because this function can be called
		** unprotected, it only accepts actual strings as error messages. (A
		** coercion could raise a memory error.)
		*/
		static int report(Lua.lua_State L, int status) {
		  if (status != Lua.LUA_OK) {
		    Lua.CharPtr msg = (Lua.lua_type(L, -1) == Lua.LUA_TSTRING) ? Lua.lua_tostring(L, -1)
		                                                       : null;
		    if (msg == null) msg = "(error object is not a string)";
		    l_message(progname, msg);
		    Lua.lua_pop(L, 1);
		  }
		  return status;
		}


		/*
		** Message handler to be used to run all chunks
		*/
		static int msghandler (Lua.lua_State L) {
		  Lua.CharPtr msg = Lua.lua_tostring(L, 1);
		  if (msg != null)  /* is error object a string? */
		    Lua.luaL_traceback(L, L, msg, 1);  /* use standard traceback */
		  else if (!Lua.lua_isnoneornil(L, 1)) {  /* non-string error object? */
		    if (Lua.luaL_callmeta(L, 1, "__tostring") == 0)  /* try its 'tostring' metamethod */
		      Lua.lua_pushliteral(L, "(no error message)");
		  }  /* else no error object, does nothing */
		  return 1;
		}


		/*
		** Interface to 'lua_pcall', which sets appropriate message function
		** and C-signal handler. Used to run all chunks.
		*/
		static int docall(Lua.lua_State L, int narg, int nres) {
			int status;
			int base_ = Lua.lua_gettop(L) - narg;  /* function index */
			Lua.lua_pushcfunction(L, msghandler);  /* push message handler */
			Lua.lua_insert(L, base_);  /* put it under function and args */
            globalL = L;  /* to be available to 'laction' */
			//signal(SIGINT, laction);  /* set C-signal handler */ //FIXME:removed
			status = Lua.lua_pcall(L, narg, nres, base_);
			//signal(SIGINT, SIG_DFL); /* reset C-signal handler */ //FIXME:removed
			Lua.lua_remove(L, base_);  /* remove message handler from the stack */
			return status;
		}


		static void print_version() {
		  Lua.luai_writestring(Lua.LUA_COPYRIGHT, (uint)Lua.strlen(Lua.LUA_COPYRIGHT)); //FIXME:changed, (uint)
		  Lua.luai_writeline();
		  Lua.WriteLog(">>>>print_version");
		}


		/*
		** Create the 'arg' table, which stores all arguments from the
		** command line ('argv'). It should be aligned so that, at index 0,
		** it has 'argv[script]', which is the script name. The arguments
		** to the script (everything after 'script') go to positive indices;
		** other arguments (before the script name) go to negative indices.
		** If there is no script name, assume interpreter's name as base.
		*/
		static void createargtable (Lua.lua_State L, string[] argv, int argc, int script) {
		  int i, narg;
		  if (script == argc) script = 0;  /* no script name? */
		  narg = argc - (script + 1);  /* number of positive indices */
		  Lua.lua_createtable(L, narg, script + 1);
		  for (i = 0; i < argc; i++) {
		    Lua.lua_pushstring(L, argv[i]);
		    Lua.lua_rawseti(L, -2, i - script);
		  }
		  Lua.lua_setglobal(L, "arg");
		}


		static int dochunk (Lua.lua_State L, int status) {
		  if (status == Lua.LUA_OK) status = docall(L, 0, 0);
		  return report(L, status);
		}


		static int dofile(Lua.lua_State L, Lua.CharPtr name) {
			return dochunk(L, Lua.luaL_loadfile(L, name));
		}


		static int dostring(Lua.lua_State L, Lua.CharPtr s, Lua.CharPtr name) {
			return dochunk(L, Lua.luaL_loadbuffer(L, s, (uint)Lua.strlen(s), name));
		}


		/*
		** Calls 'require(name)' and stores the result in a global variable
		** with the given name.
		*/
		static int dolibrary(Lua.lua_State L, Lua.CharPtr name) {
		  int status;
		  Lua.lua_getglobal(L, "require");
		  Lua.lua_pushstring(L, name);
		  status = docall(L, 1, 1);  /* call 'require(name)' */
		  if (status == Lua.LUA_OK)
		    Lua.lua_setglobal(L, name);  /* global[name] = require return */
		  return report(L, status);
		}


		/*
		** Returns the string to be used as a prompt by the interpreter.
		*/
		static Lua.CharPtr get_prompt(Lua.lua_State L, int firstline) {
			Lua.CharPtr p;
			Lua.lua_getglobal(L, (firstline!=0) ? "_PROMPT" : "_PROMPT2");
			p = Lua.lua_tostring(L, -1);
			if (p == null) p = ((firstline!=0) ? LUA_PROMPT : LUA_PROMPT2);
			return p;
		}

		/* mark in error messages for incomplete statements */
		private static Lua.CharPtr EOFMARK	= "<eof>";
		private static int marklen = EOFMARK.chars.Length - 1; //FIXME:changed, (sizeof(EOFMARK)/sizeof(char) - 1), ???


		/*
		** Check whether 'status' signals a syntax error and the error
		** message at the top of the stack ends with the above mark for
		** incoplete statements.
		*/
		static int incomplete (Lua.lua_State L, int status) {
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


		/*
		** Prompt the user, read a line, and push it into the Lua stack.
		*/
		static int pushline(Lua.lua_State L, int firstline) {
			Lua.CharPtr buffer = new char[LUA_MAXINPUT];
			Lua.CharPtr b = new Lua.CharPtr(buffer);
			int l;
			Lua.CharPtr prmt = get_prompt(L, firstline);
			int readstatus = lua_readline(L, b, prmt);
			if (readstatus == 0)
				return 0;  /* no input */
			Lua.lua_pop(L, 1);  /* remove prompt */
			l = Lua.strlen(b);
			if (l > 0 && b[l - 1] == '\n')  /* line ends with newline? */
				b[l - 1] = '\0';  /* remove it */
			if ((firstline!=0) && (b[0] == '='))  /* for compatibility with 5.2, ... */
				Lua.lua_pushfstring(L, "return %s", b + 1);  /* change '=' to 'return' */
			else
				Lua.lua_pushstring(L, b);
			lua_freeline(L, b);
			return 1;
		}


		/*
		** Try to compile line on the stack as 'return <line>'; on return, stack
		** has either compiled chunk or original line (if compilation failed).
		*/
		private static int addreturn (Lua.lua_State L) {
		  int status;
		  uint len; Lua.CharPtr line;
		  Lua.lua_pushliteral(L, "return ");
		  Lua.lua_pushvalue(L, -2);  /* duplicate line */
		  Lua.lua_concat(L, 2);  /* new line is "return ..." */
		  line = Lua.lua_tolstring(L, -1, out len);
		  if ((status = Lua.luaL_loadbuffer(L, line, len, "=stdin")) == Lua.LUA_OK)
		    Lua.lua_remove(L, -3);  /* remove original line */
		  else
		    Lua.lua_pop(L, 2);  /* remove result from 'luaL_loadbuffer' and new line */
		  return status;
		}


		/*
		** Read multiple lines until a complete Lua statement
		*/
		private static int multiline (Lua.lua_State L) {
		  for (;;) {  /* repeat until gets a complete statement */
		    uint len;
		    Lua.CharPtr line = Lua.lua_tolstring(L, 1, out len);  /* get what it has */
		    int status = Lua.luaL_loadbuffer(L, line, len, "=stdin");  /* try it */
		    if (0==incomplete(L, status) || 0==pushline(L, 0))
		      return status;  /* cannot or should not try to add continuation line */
		    Lua.lua_pushliteral(L, "\n");  /* add newline... */
		    Lua.lua_insert(L, -2);  /* ...between the two lines */
		    Lua.lua_concat(L, 3);  /* join them */
		  }
		}


		/*
		** Read a line and try to load (compile) it first as an expression (by
		** adding "return " in front of it) and second as a statement. Return
		** the final status of load/call with the resulting function (if any)
		** in the top of the stack.
		*/
		private static int loadline (Lua.lua_State L) {
		  int status;
		  Lua.lua_settop(L, 0);
		  if (0==pushline(L, 1))
		    return -1;  /* no input */
		  if ((status = addreturn(L)) != Lua.LUA_OK)  /* 'return ...' did not work? */
		    status = multiline(L);  /* try as command, maybe with continuation lines */
		  lua_saveline(L, 1);  /* keep history */
		  Lua.lua_remove(L, 1);  /* remove line from the stack */
		  Lua.lua_assert(Lua.lua_gettop(L) == 1);
		  return status;
		}


		/*
		** Prints (calling the Lua 'print' function) any values on the stack
		*/
		static void l_print (Lua.lua_State L) {
		  int n = Lua.lua_gettop(L);
		  if (n > 0) {  /* any result to be printed? */
		    Lua.luaL_checkstack(L, Lua.LUA_MINSTACK, "too many results to print");
		    Lua.lua_getglobal(L, "print");
		    Lua.lua_insert(L, 1);
		    if (Lua.lua_pcall(L, n, 0, 0) != Lua.LUA_OK)
		      l_message(progname, Lua.lua_pushfstring(L,
		                             "error calling " + Lua.LUA_QL("print") + " (%s)",
		                             Lua.lua_tostring(L, -1)));
		  }
		}


		/*
		** Do the REPL: repeatedly read (load) a line, evaluate (call) it, and
		** print any results.
		*/
		static void doREPL (Lua.lua_State L) {
			int status;
			Lua.CharPtr oldprogname = progname;
			progname = null;  /* no 'progname' on errors in interactive mode */
			while ((status = loadline(L)) != -1) {
			  if (status == Lua.LUA_OK)
			    status = docall(L, 0, Lua.LUA_MULTRET);
			  if (status == Lua.LUA_OK) l_print(L);
			  else report(L, status);
			}
			Lua.lua_settop(L, 0);  /* clear stack */
			Lua.luai_writeline();
			progname = oldprogname;
		}


		/*
		** Push on the stack 'n' strings from 'argv'
		*/
		static void pushargs (Lua.lua_State L, Lua.StringPtr argv, int n) {
		  int i;
		  Lua.luaL_checkstack(L, n + 3, "too many arguments to script");
		  for (i = 1; i < n; i++)  /* skip 0 (the script name) */
		    Lua.lua_pushstring(L, argv.at(i));
		}


		static int handle_script(Lua.lua_State L, Lua.StringPtr argv, int n) {
			int status;
		  Lua.CharPtr fname = new Lua.CharPtr(argv.at(0));
		  if (Lua.strcmp(fname, "-") == 0 && Lua.strcmp(argv.at(-1), "--") != 0)
		    fname = null;  /* stdin */
		  status = Lua.luaL_loadfile(L, fname);
		  if (status == Lua.LUA_OK) {
		    pushargs(L, argv, n);  /* push arguments to script */
		    status = docall(L, n - 1, Lua.LUA_MULTRET);
		  }
		  return report(L, status);
		}



		/* bits of various argument indicators in 'args' */
		private const int has_error	= 1;	/* bad option */
		private const int has_i = 2;	/* -i */
		private const int has_v = 4;	/* -v */
		private const int has_e = 8;	/* -e */
		private const int has_E = 16;	/* -E */

		/*
		** Traverses all arguments from 'argv', returning a mask with those
		** needed before running any Lua code (or an error code if it finds
		** any invalid argument). 'first' returns the first not-handled argument 
		** (either the script name or a bad argument in case of error).
		*/
		private static int collectargs(string[] argv, ref int first) {
		  int args = 0;
		  int i;
		  for (i = 1; i < argv.Length && argv[i] != null; i++) { //FIXME:mod, original argv[i] != null 
		    first = i;
		    if (argv[i][0] != '-')  /* not an option? */
		        return args;  /* stop handling options */
		    switch (argv[i][1]) {  /* else check option */
		      case '-':  /* '--' */
		        if (argv[i][2] != '\0')  /* extra characters after '--'? */
		          return has_error;  /* invalid option */
		        first = i + 1;
		        return args;
		      case '\0':  /* '-' */
		        return args;  /* script "name" is '-' */
		      case 'E':
		        if (argv[i][2] != '\0')  /* extra characters after 1st? */
		          return has_error;  /* invalid option */
		        args |= has_E;
		        break;
		      case 'i':
		        args |= has_i;  /* goes through  (-i implies -v) */
		        goto case 'v'; //FIXME:added
		      case 'v':
		        if (argv[i][2] != '\0')  /* extra characters after 1st? */
		          return has_error;  /* invalid option */
		        args |= has_v;
		        break;
		      case 'e':
		        args |= has_e;  /* go through */
		        goto case 'l'; //FIXME:added
		      case 'l':  /* both options need an argument */
		        if (argv[i][2] == '\0') {  /* no concatenated argument? */
		          i++;  /* try next 'argv' */
		          if (argv[i] == null || argv[i][0] == '-')
		            return has_error;  /* no next argument or it is another option */
		        }
		        break;
		      default:  /* invalid option */
		        return has_error;
		    }
		  }
		  first = i;  /* no script name */
		  return args;
		}


		/*
		** Processes options 'e' and 'l', which involve running Lua code.
		** Returns 0 if some code raises an error.
		*/
		static int runargs(Lua.lua_State L, string[] argv, int n) {
		  int i;
		  for (i = 1; i < n; i++) {
		      int status;
			  int option = argv[i][1];
			  Lua.lua_assert(argv[i][0] == '-');  /* already checked */
			  if (option == 'e' || option == 'l') {
			  	Lua.CharPtr extra = new Lua.CharPtr(argv[i]) + 2;  /* both options need an argument */
			    if (extra[0] == '\0') extra = argv[++i];
			    Lua.lua_assert(extra != null);
			    if (option == 'e')
			      status = dostring(L, extra, "=(command line)");
			    else
			      status = dolibrary(L, extra);
			    if (status != Lua.LUA_OK) return 0;
		      }
			}
			return 1;
		}


		static int handle_luainit(Lua.lua_State L) {
		  Lua.CharPtr name = "=" + LUA_INITVARVERSION;
		  Lua.CharPtr init = Lua.getenv(name + 1);
		  if (init == null) {
		    name = "=" + LUA_INIT_VAR;
		    init = Lua.getenv(name + 1);  /* try alternative name */
		  }
		  if (init == null) return Lua.LUA_OK;  /* status OK */
		  else if (init[0] == '@')
			return dofile(L, init + 1);
		  else
			return dostring(L, init, name);
		}


		/*
		** Main body of stand-alone interpreter (to be called in protected mode).
		** Reads the options and handles them all.
		*/
		static int pmain(Lua.lua_State L) {
		  int argc = (int)Lua.lua_tointeger(L, 1);
		  string[] argv = (string[])Lua.lua_touserdata(L, 2);
		  int script = 0;
		  int args = collectargs(argv, ref script);
		  Lua.luaL_checkversion(L);  /* check that interpreter has correct version */
		  if (argv[0]!=null && argv[0][0]!='\0') progname = argv[0];
		  if (args == has_error) {  /* bad arg? */
		    print_usage(argv[script]);  /* 'script' has index of bad arg. */
		    return 0;
		  }
		  if (0!=(args & has_v))  /* option '-v'? */
		    print_version();
		  if (0!=(args & has_E)) {  /* option '-E'? */
		    Lua.lua_pushboolean(L, 1);  /* signal for libraries to ignore env. vars. */
		    Lua.lua_setfield(L, Lua.LUA_REGISTRYINDEX, "LUA_NOENV");
		  }
		  Lua.luaL_openlibs(L);  /* open standard libraries */
		  createargtable(L, argv, argc, script);  /* create table 'arg' */
		  if (0==(args & has_E)) {  /* no option '-E'? */
		    if (handle_luainit(L) != Lua.LUA_OK)  /* run LUA_INIT */
		      return 0;  /* error running LUA_INIT */
		  }
		  if (0==runargs(L, argv, script))  /* execute arguments -e and -l */
		    return 0;  /* something failed */
		  if (script < argc &&  /* execute main script (if there is one) */
		    handle_script(L, new Lua.StringPtr(argv, script), argc - script) != Lua.LUA_OK)
		    return 0;
		  if (0!=(args & has_i))  /* -i option? */
		    doREPL(L);  /* do read-eval-print loop */
		  else if (script == argc && 0==(args & (has_e | has_v))) {  /* no arguments? */
		    if (0!=lua_stdin_is_tty()) {  /* running in interactive mode? */
		      print_version();
		      doREPL(L);  /* do read-eval-print loop */
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
		  Lua.lua_pushcfunction(L, pmain);  /* to call 'pmain' in protected mode */
		  Lua.lua_pushinteger(L, argc);  /* 1st argument */
		  Lua.lua_pushlightuserdata(L, argv); /* 2nd argument */
		  status = Lua.lua_pcall(L, 2, 1, 0);  /* do the call */
		  result = Lua.lua_toboolean(L, -1);  /* get result */
		  report(L, status);
		  Lua.lua_close(L);
		  return (result != 0 && status == Lua.LUA_OK) ? Lua.EXIT_SUCCESS : Lua.EXIT_FAILURE;
		}

		//----------------------------------------
		public const bool DEBUG_ = false;
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
		public static string dolua_(string message) {
			if (DEBUG_) {
				Lua.fprintf(Lua.stdout, "%s\n", "==============>" + message);
			}
			if (L_ == null) {
				L_ = Lua.luaL_newstate();
				Lua.luaL_openlibs(L_);
			}

			if (DEBUG_) {
				Lua.fprintf(Lua.stdout, "%s\n", "==============>2");
			}

			string errorMessage = null;
			bool printResult = true;
			int status = Lua.luaL_loadbuffer(L_, message, (uint)Lua.strlen(message), "=stdin");
			if (status == Lua.LUA_OK) {
				if (DEBUG_) {
					Lua.fprintf(Lua.stdout, "%s\n", "==============>3");
				}
				status = docall_(L_, 0, printResult ? Lua.LUA_MULTRET : 0);
			}
			if ((status != Lua.LUA_OK) && !Lua.lua_isnil(L_, -1)) {
				if (DEBUG_) {
					Lua.fprintf(Lua.stdout, "%s\n", "==============>4");
				}
				Lua.CharPtr msg = Lua.lua_tostring(L_, -1);
				if (msg == null) msg = "(error object is not a string)";
				errorMessage = msg.ToString();
				Lua.lua_pop(L_, 1);
				/* force a complete garbage collection in case of errors */
				Lua.lua_gc(L_, Lua.LUA_GCCOLLECT, 0);
			} 
			if (printResult) {
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
			return errorMessage;
		}		
		
		public static int Main_(string[] args) {
			Lua.fprintf(Lua.stdout, "%s\n", "hello");
			string errorMessage;
			errorMessage = dolua_("a = 100");
			if (errorMessage != null) {
				Lua.fprintf(Lua.stdout, "%s\n", errorMessage);
			}
			errorMessage = dolua_("print(a)");
			if (errorMessage != null) {
				Lua.fprintf(Lua.stdout, "%s\n", errorMessage);
			}
			return 0;
		}
	}
}

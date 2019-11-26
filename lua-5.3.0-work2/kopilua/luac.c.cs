/*
** $Id: luac.c,v 1.69 2011/11/29 17:46:33 lhf Exp $
** Lua compiler (saves bytecodes to files; also list bytecodes)
** See Copyright Notice in lua.h
*/
#define LUA_CORE

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KopiLua
{
	using Instruction = System.UInt32;
	using TValue = Lua.lua_TValue;
	using OpCode = Lua.OpCode;
	using OpMode = Lua.OpMode;
	using OpArgMask = Lua.OpArgMask;
	using lua_Number = System.Double;
	
	public class Program_luac
	{
		//#include <errno.h>
		//#include <stdio.h>
		//#include <stdlib.h>
		//#include <string.h>

		//#define luac_c
		//#define LUA_CORE

		//#include "lua.h"
		//#include "lauxlib.h"

		//#include "lobject.h"
		//#include "lstate.h"
		//#include "lundump.h"

		//static void PrintFunction(const Proto* f, int full);
		public static void luaU_print(Lua.Proto f, int full) { PrintFunction(f, full);}

		static Lua.CharPtr PROGNAME = "luac";		/* default program name */
		static Lua.CharPtr OUTPUT = PROGNAME + ".out"; /* default output file */

		static int listing=0;			/* list bytecodes? */
		static int dumping=1;			/* dump bytecodes? */
		static int stripping=0;			/* strip debug information? */
		static Lua.CharPtr Output=OUTPUT;	/* default output file name */
		static Lua.CharPtr output=Output;	/* actual output file name */
		static Lua.CharPtr progname=PROGNAME;	/* actual program name */

		static void fatal(Lua.CharPtr message)
		{
		 Lua.fprintf(Lua.stderr,"%s: %s\n",progname,message);
		 Environment.Exit(Lua.EXIT_FAILURE);
		}

		static void cannot(Lua.CharPtr what)
		{
		 Lua.fprintf(Lua.stderr,"%s: cannot %s %s: %s\n",progname,what,output,Lua.strerror(Lua.errno));
		 Environment.Exit(Lua.EXIT_FAILURE);
		}

		static void usage(Lua.CharPtr message)
		{
		 if (message[0]=='-')
		  Lua.fprintf(Lua.stderr,"%s: unrecognized option " + Lua.LUA_QS + "\n",progname,message);
		 else
		  Lua.fprintf(Lua.stderr,"%s: %s\n",progname,message);
		 Lua.fprintf(Lua.stderr,
		  "usage: %s [options] [filenames].\n" +
		  "Available options are:\n" +
		  "  -l       list (use -l -l for full listing)\n" + 
		  "  -o name  output to file " + Lua.LUA_QL("name") + " (default is \"%s\")\n" +
		  "  -p       parse only\n" +
		  "  -s       strip debug information\n" +
		  "  -v       show version information\n" +
		  "  --       stop handling options\n" +
          "  -        stop handling options and process stdin\n"
		  ,progname,Output);
		 Environment.Exit(Lua.EXIT_FAILURE);
		}

		//#define IS(s)	(strcmp(argv[i],s)==0)

		static int doargs(int argc, string[] argv)
		{
		 int i;
		 int version=0;
		 if ((argv.Length > 0) && (argv[0]!="")) progname=argv[0];
		 for (i=1; i<argc; i++)
		 {
		  if (argv[i][0]!='-')			/* end of options; keep it */
		   break;
		  else if (Lua.strcmp(argv[i], "--") == 0)			/* end of options; skip it */
		  {
		   ++i;
		   if (version!=0) ++version;
		   break;
		  }
		  else if (Lua.strcmp(argv[i], "-") == 0)			/* end of options; use stdin */
		   break;
		  else if (Lua.strcmp(argv[i], "-l") == 0)			/* list */
		   ++listing;
		  else if (Lua.strcmp(argv[i], "-o") == 0)			/* output file */
		  {
		   output=argv[++i];
		   if (output==null || (output[0]==0) || (output[0]=='-' && output[1]!=0))
		    usage(Lua.LUA_QL("-o") + " needs argument");
		   if (Lua.strcmp(argv[i], "-")==0) output = null;
		  }
		  else if (Lua.strcmp(argv[i], "-p") == 0)			/* parse only */
		   dumping=0;
		  else if (Lua.strcmp(argv[i], "-s") == 0)			/* strip debug information */
		   stripping=1;
		  else if (Lua.strcmp(argv[i], "-v") == 0)			/* show version */
		   ++version;
		  else					/* unknown option */
		   usage(argv[i]);
		 }
		 if (i==argc && ((listing!=0) || (dumping==0)))
		 {
		  dumping=0;
		  argv[--i]=Output.ToString();
		 }
		 if (version!=0)
		 {
		  Lua.printf("%s\n",Lua.LUA_COPYRIGHT);
		  if (version==argc-1) Environment.Exit(Lua.EXIT_SUCCESS);
		 }
		 return i;
		}


		private const string FUNCTION = "(function()end)();";

		private static Lua.CharPtr reader(Lua.lua_State L, object ud, out uint size)
		{
		 //UNUSED(L); //FIXME:changed
		 int ud_ = ((int[])ud)[0]; //FIXME:added
		 ((int[])ud)[0]--; //FIXME:added
		 if (ud_!=0)
		 {
		  size=(uint)FUNCTION.Length;//FIXME:changed, sizeof(FUNCTION)-1;
		  return FUNCTION;
		 }
		 else
		 {
		  size=0;
		  return null;
		 }
		}

		static Lua.Proto toproto(Lua.lua_State L, int i) {return Lua.getproto(L.top+i);}

		static Lua.Proto combine(Lua.lua_State L, int n)
		{
		 if (n==1)
		  return toproto(L,-1);
		 else
		 {
		  Lua.Proto f;
		  int i=n;
		  int[]i_ = new int[]{i}; //FIXME:added
		  if (Lua.lua_load(L,reader,i_,"=(" + PROGNAME +  ")",null)!=Lua.LUA_OK) fatal(Lua.lua_tostring(L,-1));
		  i=i_[0]; //FIXME:added
		  f=toproto(L,-1);
		  for (i=0; i<n; i++)
		  {
		   f.p[i]=toproto(L,i-n-1);
		   if (f.p[i].sizeupvalues>0) f.p[i].upvalues[0].instack=0;
		  }
		  f.sizelineinfo=0;
		  return f;
		 }
		}

		static int writer(Lua.lua_State L, Lua.CharPtr p, uint size, object u)
		{
		 //UNUSED(L);
		 return ((Lua.fwrite(p,(int)size,1,(Lua.StreamProxy)u)!=1) && (size!=0)) ? 1 : 0;
		}


		static int pmain(Lua.lua_State L)
		{
		 int argc=(int)Lua.lua_tointeger(L,1);
		 Lua.CharPtr[] argv=(Lua.CharPtr[])Lua.lua_touserdata(L,2);
		 Lua.Proto f;
		 int i;
		 if (Lua.lua_checkstack(L,argc)==0) fatal("too many input files");
		 for (i=0; i<argc; i++)
		 {
		  Lua.CharPtr filename=(Lua.strcmp(argv[i], "-")==0) ? null : argv[i];
		  if (Lua.luaL_loadfile(L,filename)!=Lua.LUA_OK) fatal(Lua.lua_tostring(L,-1));
		 }
		 f=combine(L,argc);
		 if (listing!=0) luaU_print(f,(listing>1)?1:0);
		 if (dumping!=0)
		 {
		  Lua.StreamProxy D= (output==null) ? Lua.stdout : Lua.fopen(output,"wb");
		  if (D==null) cannot("open");
		  Lua.lua_lock(L);
		  Lua.luaU_dump(L,f,writer,D,stripping);
		  Lua.lua_unlock(L);
		  if (Lua.ferror(D)!=0) cannot("write");
		  if (Lua.fclose(D)!=0) cannot("close");
		 }
		 return 0;
		}

		static int Main_luac(string[] args)
		{
		 // prepend the exe name to the arg list as it's done in C
		 // so that we don't have to change any of the args indexing
		 // code above
		 List<string> newargs = new List<string>(args);
		 newargs.Insert(0, Assembly.GetExecutingAssembly().Location);
		 args = (string[])newargs.ToArray();

		 Lua.lua_State L;
		 int argc = args.Length; //FIXME:added
		 Lua.CharPtr[] argv = new Lua.CharPtr[args.Length]; //FIXME:added
		 for (int kk = 0; kk < argv.Length; ++kk)
		 {
		 	argv[kk] = args[kk];
		 }
		 int i=doargs(argc,args);
		 newargs.RemoveRange(0, i);
		 argc -= i; args = (string[])newargs.ToArray();
		 if (argc<=0) usage("no input files given");
		 L=Lua.luaL_newstate();
		 if (L==null) fatal("cannot create state: not enough memory");
		 Lua.lua_pushcfunction(L,pmain);
		 Lua.lua_pushinteger(L,argc);
		 Lua.lua_pushlightuserdata(L,argv);
		 if (Lua.lua_pcall(L,2,0,0)!=Lua.LUA_OK) fatal(Lua.lua_tostring(L,-1));
         Lua.lua_close(L);
		 return Lua.EXIT_SUCCESS;
		}

		private static lua_Number nvalue__(TValue x) { return ((lua_Number)0); }
		private static int ttypenv__(TValue x) { return Lua.ttnov(x); }
/*
** $Id: print.c,v 1.69 2013/07/04 01:03:46 lhf Exp $
** print bytecodes
** See Copyright Notice in lua.h
*/


		public static object VOID(object p)	{ return ((object)(p));}

		public static void PrintString(Lua.TString ts)
		{
		 Lua.CharPtr s=Lua.getstr(ts);
		 uint i,n=ts.tsv.len;
		 Lua.printf("%c",'"');
		 for (i=0; i<n; i++)
		 {
		  int c=(int)(byte)s[i];
		  switch (c)
		  {
		   case '"':  Lua.printf("\\\""); break;
		   case '\\': Lua.printf("\\\\"); break;
		   case '\a': Lua.printf("\\a"); break;
		   case '\b': Lua.printf("\\b"); break;
		   case '\f': Lua.printf("\\f"); break;
		   case '\n': Lua.printf("\\n"); break;
		   case '\r': Lua.printf("\\r"); break;
		   case '\t': Lua.printf("\\t"); break;
		   case '\v': Lua.printf("\\v"); break;
		   default:	if (Lua.isprint(c))
   					Lua.printf("%c",c);
				else
					Lua.printf("\\%03d",c);
				break; //FIXME:added
		  }
		 }
		 Lua.printf("%c",'"');
		}

		private static void PrintConstant(Lua.Proto f, int i)
		{
		 /*const*/ TValue o=f.k[i];
		 switch (ttypenv__(o))
		 {
		  case Lua.LUA_TNIL:
			Lua.printf("nil");
			break;
		  case Lua.LUA_TBOOLEAN:
			Lua.printf(Lua.bvalue(o) != 0 ? "true" : "false");
			break;
		  case Lua.LUA_TNUMBER:
			Lua.printf(Lua.LUA_NUMBER_FMT,nvalue__(o));
			break;
		  case Lua.LUA_TSTRING:
			PrintString(Lua.rawtsvalue(o));
			break;
		  default:				/* cannot happen */
			Lua.printf("? type=%d",Lua.ttype(o));
			break;
		 }
		}

		private static Lua.CharPtr UPVALNAME(int x, Lua.Proto f) { return ((f.upvalues[x].name!=null) ? Lua.getstr(f.upvalues[x].name) : "-");}
		private static int MYK(int x) { return (-1-(x));}

		private static void PrintCode( Lua.Proto f)
		{
		 Instruction[] code = f.code;
		 int pc,n=f.sizecode;
		 for (pc=0; pc<n; pc++)
		 {
		  Instruction i = f.code[pc];
		  OpCode o=Lua.GET_OPCODE(i);
		  int a=Lua.GETARG_A(i);
		  int b=Lua.GETARG_B(i);
		  int c=Lua.GETARG_C(i);
          int ax=Lua.GETARG_Ax(i);
		  int bx=Lua.GETARG_Bx(i);
		  int sbx=Lua.GETARG_sBx(i);
		  int line=Lua.getfuncline(f,pc);
		  Lua.printf("\t%d\t",pc+1);
		  if (line>0) Lua.printf("[%d]\t",line); else Lua.printf("[-]\t");
		  Lua.printf("%-9s\t",Lua.luaP_opnames[(int)o]);
		  switch (Lua.getOpMode(o))
		  {
		   case OpMode.iABC:
		    Lua.printf("%d",a);
		    if (Lua.getBMode(o)!=OpArgMask.OpArgN) Lua.printf(" %d",Lua.ISK(b)!=0 ? (MYK(Lua.INDEXK(b))) : b);
		    if (Lua.getCMode(o)!=OpArgMask.OpArgN) Lua.printf(" %d",Lua.ISK(c)!=0 ? (MYK(Lua.INDEXK(c))) : c);
		    break;
		   case OpMode.iABx:
		    Lua.printf("%d",a);
		    if (Lua.getBMode(o)==OpArgMask.OpArgK) Lua.printf(" %d",MYK(bx));
		    if (Lua.getBMode(o)==OpArgMask.OpArgU) Lua.printf(" %d",bx);
		    break;
		   case OpMode.iAsBx:
		    Lua.printf("%d %d",a,sbx);
		    break;
		   case OpMode.iAx:
		    Lua.printf("%d",MYK(ax));
		    break;
		  }
		  switch (o)
		  {
		   case OpCode.OP_LOADK:
		    Lua.printf("\t; "); PrintConstant(f,bx);
		    break;
		   case OpCode.OP_GETUPVAL:
		   case OpCode.OP_SETUPVAL:
		    Lua.printf("\t; %s",UPVALNAME(b, f));
		    break;
		   case OpCode.OP_GETTABUP:
		    Lua.printf("\t; %s",UPVALNAME(b, f));
		    if (Lua.ISK(c)!=0) { Lua.printf(" "); PrintConstant(f,Lua.INDEXK(c)); }
		    break;
		   case OpCode.OP_SETTABUP:
		    Lua.printf("\t; %s",UPVALNAME(a, f));
		    if (Lua.ISK(b)!=0) { Lua.printf(" "); PrintConstant(f,Lua.INDEXK(b)); }
		    if (Lua.ISK(c)!=0) { Lua.printf(" "); PrintConstant(f,Lua.INDEXK(c)); }
		    break;
		   case OpCode.OP_GETTABLE:
		   case OpCode.OP_SELF:
			if (Lua.ISK(c) != 0) { Lua.printf("\t; "); PrintConstant(f,Lua.INDEXK(c)); }
			break;
		   case OpCode.OP_SETTABLE:
		   case OpCode.OP_ADD:
		   case OpCode.OP_SUB:
		   case OpCode.OP_MUL:
		   case OpCode.OP_DIV:
		   case OpCode.OP_POW:
		   case OpCode.OP_EQ:
		   case OpCode.OP_LT:
		   case OpCode.OP_LE:
			if (Lua.ISK(b)!=0 || Lua.ISK(c)!=0)
			{
			 Lua.printf("\t; ");
			 if (Lua.ISK(b) != 0) PrintConstant(f,Lua.INDEXK(b)); else Lua.printf("-");
			 Lua.printf(" ");
			 if (Lua.ISK(c) != 0) PrintConstant(f,Lua.INDEXK(c)); else Lua.printf("-");
			}
			break;
		   case OpCode.OP_JMP:
		   case OpCode.OP_FORLOOP:
		   case OpCode.OP_FORPREP:
           case OpCode.OP_TFORLOOP:
			Lua.printf("\t; to %d",sbx+pc+2);
			break;
		   case OpCode.OP_CLOSURE:
			Lua.printf("\t; %p",VOID(f.p[bx]));
			break;
		   case OpCode.OP_SETLIST:
		    if (c==0) Lua.printf("\t; %d",(int)code[++pc]); else Lua.printf("\t; %d",c);
		    break;
		   case OpCode.OP_EXTRAARG:
		    Lua.printf("\t; "); PrintConstant(f,ax);
		    break;
		   default:
			break;
		  }
		  Lua.printf("\n");
		 }
		}

		public static string SS(int x) { return (x == 1) ? "" : "s"; }
		//#define S(x)	(int)(x),SS(x)

		private static void PrintHeader(Lua.Proto f)
		{
		 Lua.CharPtr s=f.source!=null ? Lua.getstr(f.source) : "=?";
		 if (s[0]=='@' || s[0]=='=')
		  s  = s.next();
		 else if (s[0]==Lua.LUA_SIGNATURE[0])
		  s="(bstring)";
		 else
		  s="(string)";
		 Lua.printf("\n%s <%s:%d,%d> (%d Instruction%s at %p)\n",
 			(f.linedefined==0)?"main":"function",s,
			f.linedefined,f.lastlinedefined,
			(int)(f.sizecode),SS(f.sizecode), VOID(f));
		 Lua.printf("%d%s param%s, %d slot%s, %d upvalue%s, ",
			(int)(f.numparams),(f.is_vararg != 0) ? "+" : "", SS(f.numparams),
			(int)(f.maxstacksize),SS(f.maxstacksize),(int)f.sizeupvalues,SS(f.sizeupvalues));
		 Lua.printf("%d local%s, %d constant%s, %d function%s\n",
		    (int)(f.sizelocvars),SS(f.sizelocvars),(int)f.sizek,SS(f.sizek),(int)f.sizep,SS(f.sizep));
		}

		private static void PrintDebug(Lua.Proto f)
		{
		 int i,n;
		 n=f.sizek;
		 Lua.printf("constants (%d) for %p:\n",n,VOID(f));
		 for (i=0; i<n; i++)
		 {
		  Lua.printf("\t%d\t",i+1);
		  PrintConstant(f,i);
		  Lua.printf("\n");
		 }
		 n=f.sizelocvars;
		 Lua.printf("locals (%d) for %p:\n",n,VOID(f));
		 for (i=0; i<n; i++)
		 {
		  Lua.printf("\t%d\t%s\t%d\t%d\n",
		  i,Lua.getstr(f.locvars[i].varname),f.locvars[i].startpc+1,f.locvars[i].endpc+1);
		 }
		 n=f.sizeupvalues;
		 Lua.printf("upvalues (%d) for %p:\n",n,VOID(f));
		 for (i=0; i<n; i++)
		 {
		  Lua.printf("\t%d\t%s\t%d\t%d\n",
		  i,UPVALNAME(i, f),f.upvalues[i].instack,f.upvalues[i].idx);
		 }
		}

		private static void PrintFunction(Lua.Proto f, int full)
		{
		 int i,n=f.sizep;
		 PrintHeader(f);
		 PrintCode(f);
		 if (full!=0) PrintDebug(f);
		 for (i=0; i<n; i++) PrintFunction(f.p[i],full);
		}

	}
}


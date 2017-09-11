/*
** $Id: luac.c,v 1.54 2006/06/02 17:37:11 lhf Exp $
** Lua compiler (saves bytecodes to files; also list bytecodes)
** See Copyright Notice in lua.h
*/

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

		//#include "ldo.h"
		//#include "lfunc.h"
		//#include "lmem.h"
		//#include "lobject.h"
		//#include "lopcodes.h"
		//#include "lstring.h"
		//#include "lundump.h"

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
		 Lua.fprintf(Lua.stderr,"%s: cannot %s %s: %s\n",progname,what,output,Lua.strerror(Lua.errno()));
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
		 "  -        process stdin\n" +
		 "  -l       list\n" +
		 "  -o name  output to file " + Lua.LUA_QL("name") + " (default is \"%s\")\n" +
		 "  -p       parse only\n" +
		 "  -s       strip debug information\n" +
		 "  -v       show version information\n" +
		 "  --       stop handling options\n",
		 progname,Output);
		 Environment.Exit(Lua.EXIT_FAILURE);
		}

		//#define	IS(s)	(strcmp(argv[i],s)==0)

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
		   if (output==null || (output[0]==0)) usage(Lua.LUA_QL("-o") + " needs argument");
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
		  Lua.printf("%s  %s\n",Lua.LUA_RELEASE,Lua.LUA_COPYRIGHT);
		  if (version==argc-1) Environment.Exit(Lua.EXIT_SUCCESS);
		 }
		 return i;
		}

		static Lua.Proto toproto(Lua.lua_State L, int i) {return Lua.clvalue(L.top+(i)).l.p;}

		static Lua.Proto combine(Lua.lua_State L, int n)
		{
		 if (n==1)
		  return toproto(L,-1);
		 else
		 {
		  int i,pc;
		  Lua.Proto f=Lua.luaF_newproto(L);
		  Lua.setptvalue2s(L,L.top,f); Lua.incr_top(L);
		  f.source=Lua.luaS_newliteral(L,"=(" + PROGNAME + ")");
		  f.maxstacksize=1;
		  pc=2*n+1;
		  f.code = (Instruction[])Lua.luaM_newvector<Instruction>(L, pc);
		  f.sizecode=pc;
		  f.p = Lua.luaM_newvector<Lua.Proto>(L, n);
		  f.sizep=n;
		  pc=0;
		  for (i=0; i<n; i++)
		  {
		   f.p[i]=toproto(L,i-n-1);
		   f.code[pc++]=(uint)Lua.CREATE_ABx(Lua.OpCode.OP_CLOSURE,0,i);
		   f.code[pc++]=(uint)Lua.CREATE_ABC(Lua.OpCode.OP_CALL,0,1,1);
		  }
		  f.code[pc++]=(uint)Lua.CREATE_ABC(Lua.OpCode.OP_RETURN,0,1,0);
		  return f;
		 }
		}

		static int writer(Lua.lua_State L, Lua.CharPtr p, uint size, object u)
		{
		 //UNUSED(L);
		 return ((Lua.fwrite(p,(int)size,1,(Stream)u)!=1) && (size!=0)) ? 1 : 0;
		}

		public class Smain {
		 public int argc;
		 public string[] argv;
		};

		static int pmain(Lua.lua_State L)
		{
		 Smain s = (Smain)Lua.lua_touserdata(L, 1);
		 int argc=s.argc;
		 string[] argv=s.argv;
		 Lua.Proto f;
		 int i;
		 if (Lua.lua_checkstack(L,argc)==0) fatal("too many input files");
		 for (i=0; i<argc; i++)
		 {
		  Lua.CharPtr filename=(Lua.strcmp(argv[i], "-")==0) ? null : argv[i];
		  if (Lua.luaL_loadfile(L,filename)!=0) fatal(Lua.lua_tostring(L,-1));
		 }
		 f=combine(L,argc);
		 if (listing!=0) Lua.luaU_print(f,(listing>1)?1:0);
		 if (dumping!=0)
		 {
		  Stream D= (output==null) ? Lua.stdout : Lua.fopen(output,"wb");
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
		 Smain s = new Smain();
		 int argc = args.Length;
		 int i=doargs(argc,args);
		 newargs.RemoveRange(0, i);
		 argc -= i; args = (string[])newargs.ToArray();
		 if (argc<=0) usage("no input files given");
		 L=Lua.lua_open();
		 if (L==null) fatal("not enough memory for state");
		 s.argc=argc;
		 s.argv=args;
		 if (Lua.lua_cpcall(L,pmain,s)!=0) fatal(Lua.lua_tostring(L,-1));
		 Lua.lua_close(L);
		 return Lua.EXIT_SUCCESS;
		}

/*
** $Id: print.c,v 1.55a 2006/05/31 13:30:05 lhf Exp $
** print bytecodes
** See Copyright Notice in lua.h
*/
		public static void luaU_print(Proto f, int full) {PrintFunction(f, full);}

		//#define Sizeof(x)	((int)sizeof(x))
		//#define VOID(p)		((const void*)(p))

		public static void PrintString(TString ts)
		{
		 CharPtr s=getstr(ts);
		 uint i,n=ts.tsv.len;
		 putchar('"');
		 for (i=0; i<n; i++)
		 {
		  int c=s[i];
		  switch (c)
		  {
		   case '"': printf("\\\""); break;
		   case '\\': printf("\\\\"); break;
		   case '\a': printf("\\a"); break;
		   case '\b': printf("\\b"); break;
		   case '\f': printf("\\f"); break;
		   case '\n': printf("\\n"); break;
		   case '\r': printf("\\r"); break;
		   case '\t': printf("\\t"); break;
		   case '\v': printf("\\v"); break;
		   default:	if (isprint((byte)c))
   					putchar(c);
				else
					printf("\\%03u",(byte)c);
				break;
		  }
		 }
		 putchar('"');
		}

		private static void PrintConstant(Proto f, int i)
		{
		 /*const*/ TValue o=f.k[i];
		 switch (ttype(o))
		 {
		  case LUA_TNIL:
			printf("nil");
			break;
		  case LUA_TBOOLEAN:
			printf(bvalue(o) != 0 ? "true" : "false");
			break;
		  case LUA_TNUMBER:
			printf(LUA_NUMBER_FMT,nvalue(o));
			break;
		  case LUA_TSTRING:
			PrintString(rawtsvalue(o));
			break;
		  default:				/* cannot happen */
			printf("? type=%d",ttype(o));
			break;
		 }
		}

		private static void PrintCode( Proto f)
		{
		 Instruction[] code = f.code;
		 int pc,n=f.sizecode;
		 for (pc=0; pc<n; pc++)
		 {
		  Instruction i = f.code[pc];
		  OpCode o=GET_OPCODE(i);
		  int a=GETARG_A(i);
		  int b=GETARG_B(i);
		  int c=GETARG_C(i);
		  int bx=GETARG_Bx(i);
		  int sbx=GETARG_sBx(i);
		  int line=getline(f,pc);
		  printf("\t%d\t",pc+1);
		  if (line>0) printf("[%d]\t",line); else printf("[-]\t");
		  printf("%-9s\t",luaP_opnames[(int)o]);
		  switch (getOpMode(o))
		  {
		   case OpMode.iABC:
			printf("%d",a);
			if (getBMode(o) != OpArgMask.OpArgN) printf(" %d", (ISK(b) != 0) ? (-1 - INDEXK(b)) : b);
			if (getCMode(o) != OpArgMask.OpArgN) printf(" %d", (ISK(c) != 0) ? (-1 - INDEXK(c)) : c);
			break;
		   case OpMode.iABx:
			if (getBMode(o)==OpArgMask.OpArgK) printf("%d %d",a,-1-bx); else printf("%d %d",a,bx);
			break;
		   case OpMode.iAsBx:
			if (o==OpCode.OP_JMP) printf("%d",sbx); else printf("%d %d",a,sbx);
			break;
		  }
		  switch (o)
		  {
		   case OpCode.OP_LOADK:
			printf("\t; "); PrintConstant(f,bx);
			break;
		   case OpCode.OP_GETUPVAL:
		   case OpCode.OP_SETUPVAL:
			printf("\t; %s", (f.sizeupvalues>0) ? getstr(f.upvalues[b]) : "-");
			break;
		   case OpCode.OP_GETGLOBAL:
		   case OpCode.OP_SETGLOBAL:
			printf("\t; %s",svalue(f.k[bx]));
			break;
		   case OpCode.OP_GETTABLE:
		   case OpCode.OP_SELF:
			if (ISK(c) != 0) { printf("\t; "); PrintConstant(f,INDEXK(c)); }
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
			if (ISK(b)!=0 || ISK(c)!=0)
			{
			 printf("\t; ");
			 if (ISK(b) != 0) PrintConstant(f,INDEXK(b)); else printf("-");
			 printf(" ");
			 if (ISK(c) != 0) PrintConstant(f,INDEXK(c)); else printf("-");
			}
			break;
		   case OpCode.OP_JMP:
		   case OpCode.OP_FORLOOP:
		   case OpCode.OP_FORPREP:
			printf("\t; to %d",sbx+pc+2);
			break;
		   case OpCode.OP_CLOSURE:
			printf("\t; %p",VOID(f.p[bx]));
			break;
		   case OpCode.OP_SETLIST:
			if (c==0) printf("\t; %d",(int)code[++pc]);
			else printf("\t; %d",c);
			break;
		   default:
			break;
		  }
		  printf("\n");
		 }
		}

		public static string SS(int x) { return (x == 1) ? "" : "s"; }
		//#define S(x)	x,SS(x)

		private static void PrintHeader(Proto f)
		{
		 CharPtr s=getstr(f.source);
		 if (s[0]=='@' || s[0]=='=')
		  s  = s.next();
		 else if (s[0]==LUA_SIGNATURE[0])
		  s="(bstring)";
		 else
		  s="(string)";
		 printf("\n%s <%s:%d,%d> (%d Instruction%s, %d bytes at %p)\n",
 			(f.linedefined==0)?"main":"function",s,
			f.linedefined,f.lastlinedefined,
			f.sizecode, SS(f.sizecode), f.sizecode * GetUnmanagedSize(typeof(Instruction)), VOID(f));
		 printf("%d%s param%s, %d slot%s, %d upvalue%s, ",
			f.numparams,(f.is_vararg != 0) ? "+" : "", SS(f.numparams),
			f.maxstacksize, SS(f.maxstacksize), f.nups, SS(f.nups));
		 printf("%d local%s, %d constant%s, %d function%s\n",
			f.sizelocvars, SS(f.sizelocvars), f.sizek, SS(f.sizek), f.sizep, SS(f.sizep));
		}

		private static void PrintConstants(Proto f)
		{
		 int i,n=f.sizek;
		 printf("constants (%d) for %p:\n",n,VOID(f));
		 for (i=0; i<n; i++)
		 {
		  printf("\t%d\t",i+1);
		  PrintConstant(f,i);
		  printf("\n");
		 }
		}

		private static void PrintLocals(Proto f)
		{
		 int i,n=f.sizelocvars;
		 printf("locals (%d) for %p:\n",n,VOID(f));
		 for (i=0; i<n; i++)
		 {
		  printf("\t%d\t%s\t%d\t%d\n",
		  i,getstr(f.locvars[i].varname),f.locvars[i].startpc+1,f.locvars[i].endpc+1);
		 }
		}

		private static void PrintUpvalues(Proto f)
		{
		 int i,n=f.sizeupvalues;
		 printf("upvalues (%d) for %p:\n",n,VOID(f));
		 if (f.upvalues==null) return;
		 for (i=0; i<n; i++)
		 {
		  printf("\t%d\t%s\n",i,getstr(f.upvalues[i]));
		 }
		}

		public static void PrintFunction(Proto f, int full)
		{
		 int i,n=f.sizep;
		 PrintHeader(f);
		 PrintCode(f);
		 if (full != 0)
		 {
		  PrintConstants(f);
		  PrintLocals(f);
		  PrintUpvalues(f);
		 }
		 for (i=0; i<n; i++) PrintFunction(f.p[i],full);
		}

	}
}


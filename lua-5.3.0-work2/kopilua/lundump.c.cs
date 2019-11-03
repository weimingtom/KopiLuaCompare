/*
** $Id: lundump.c,v 2.34 2014/03/11 18:56:27 roberto Exp $
** load precompiled Lua chunks
** See Copyright Notice in lua.h
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using lua_Number = System.Double;
	using lu_byte = System.Byte;
	using StkId = Lua.lua_TValue;
	using Instruction = System.UInt32;
	using lua_Integer = System.Int32;

	public partial class Lua
	{


		//#if !defined(luai_verifycode)
		public static void luai_verifycode(lua_State L, Mbuffer b, Proto f) { /* empty */ }
		//#endif


		public class LoadState{
		  public lua_State L;
		  public ZIO Z;
		  public Mbuffer b;
		  public CharPtr name;
		};


		public static void/*l_noret*/ error(LoadState S, CharPtr why) {
		  luaO_pushfstring(S.L,"%s: %s precompiled chunk",S.name,why);
		  luaD_throw(S.L,LUA_ERRSYNTAX);
		}

		/*
		** All high-level loads go through LoadVector; you can change it to
		** adapt to the endianess of the input
		*/
		#define LoadVector(S,b,n)	LoadBlock(S,b,(n)*sizeof((b)[0]))


		private static void LoadBlock(LoadState S, CharPtr b, int size) {
		  if (luaZ_read(S.Z, b, (uint)size) != 0) //FIXME:(uint) 
		    error(S, "truncated");
		}


		#define LoadVar(S,x)		LoadVector(S,&x,1)


		private static lu_byte LoadByte(LoadState S) {
		  return (lu_byte)LoadVar(S, typeof(char)); //FIXME: changed
		}


		private static int LoadInt(LoadState S) {
		  int x;
		  x = (int)LoadVar(S, typeof(int)); //FIXME: changed
		  return x;
		}


		private static lua_Number LoadNumber(LoadState S) {
		  return (lua_Number)LoadVar(S, typeof(lua_Number));
		}


		private static lua_Integer LoadInteger(LoadState S) {
		  return (lua_Integer)LoadVar(S, typeof(lua_Integer));
		}


		private static TString LoadString(LoadState S) {
		  size_t size = LoadByte(S);
		  if (size == 0xFF)
		    LoadVar(S, size);
		  if (size == 0)
		    return NULL;
		  else {
		    char *s = luaZ_openspace(S->L, S->b, --size);
		    LoadVector(S, s, size);
		    return luaS_newlstr(S->L, s, size);
		  }
		}

		private static void LoadCode(LoadState S, Proto f) {
		  int n=LoadInt(S);
		  f.code = luaM_newvector<Instruction>(S.L, n);
		  f.sizecode=n;
		  f.code = (Instruction[])LoadVector(S, typeof(Instruction), n);
		}


		//static void LoadFunction(LoadState* S, Proto* f);


		private static void LoadConstants(LoadState S, Proto f) {
		  int i, n;
		  n = LoadInt(S);
		  f.k = luaM_newvector<TValue>(S.L, n);
		  f.sizek = n;
		  for (i = 0; i < n; i++) 
		    setnilvalue(f.k[i]);
		  for (i=0; i<n; i++) {
		    TValue o = f.k[i];
		    int t = LoadByte(S);
		    switch (t) {
		    case LUA_TNIL:
   			  setnilvalue(o);
			  break;
		    case LUA_TBOOLEAN:
			  setbvalue(o, LoadByte(S)!=0 ? 1 : 0); //FIXME:???!=0->(empty)
			  break;
		    case LUA_TNUMFLT:
			  setnvalue(o, LoadNumber(S));
			  break;
		    case LUA_TNUMINT:
			  setivalue(o, LoadInteger(S));
			  break;
		    case LUA_TSHRSTR: 
			case LUA_TLNGSTR:
			  setsvalue2n(S.L, o, LoadString(S));
			  break;
		    default: 
			  lua_assert(0); 
			  break;
		    }
		  }
		  n = LoadInt(S);
		  f.p = luaM_newvector<Proto>(S.L, n);
		  f.sizep = n;
		  for (i = 0; i < n; i++) 
		    f.p[i] = null;
		  for (i = 0; i < n; i++) 
		  {
		    f.p[i] = luaF_newproto(S.L);
		 	LoadFunction(S, f.p[i]);
		  }
		}

		private static void LoadUpvalues(LoadState S, Proto f) {
		  int i, n;
		  n = LoadInt(S);
		  f.upvalues = luaM_newvector<Upvaldesc>(S.L, n);
		  f.sizeupvalues = n;
		  for (i=0; i<n; i++) 
		    f.upvalues[i].name = null;
		  for (i=0; i<n; i++) {
		    f.upvalues[i].instack = LoadByte(S);
		    f.upvalues[i].idx = LoadByte(S);
		  }
		}

		private static void LoadDebug(LoadState S, Proto f) {
		  int i,n;
          f.source = LoadString(S);
		  n = LoadInt(S);
		  f.lineinfo=luaM_newvector<int>(S.L, n);
		  f.sizelineinfo=n;
		  f.lineinfo = (int[])LoadVector(S, typeof(int), n);
		  n = LoadInt(S);
		  f.locvars=luaM_newvector<LocVar>(S.L, n);
		  f.sizelocvars=n;
		  for (i=0; i<n; i++) 
		    f.locvars[i].varname = null;
		  for (i=0; i<n; i++) {
		    f.locvars[i].varname=LoadString(S);
		    f.locvars[i].startpc=LoadInt(S);
		    f.locvars[i].endpc=LoadInt(S);
		  }
		  n=LoadInt(S);
		  for (i = 0; i < n; i++) 
		    f.upvalues[i].name = LoadString(S);
		}

		private static void LoadFunction(LoadState S, Proto f) {
		  f.linedefined=LoadInt(S);
		  f.lastlinedefined=LoadInt(S);
		  f.numparams=LoadByte(S);
		  f.is_vararg=LoadByte(S);
		  f.maxstacksize=LoadByte(S);
		  LoadCode(S,f);
		  LoadConstants(S,f);
          LoadUpvalues(S,f);
		  LoadDebug(S,f);
		}

		private static void checkliteral (LoadState *S, const char *s, const char *msg) {
		  char buff[sizeof(LUA_SIGNATURE) + sizeof(LUAC_DATA)]; /* larger than both */
		  int len = strlen(s);
		  LoadVector(S, buff, len);
		  if (memcmp(s, buff, len) != 0)
		    error(S, msg);
		}


		private static void fchecksize (LoadState *S, size_t size, const char *tname) {
		  if (LoadByte(S) != size)
		    error(S, luaO_pushfstring(S->L, "%s size mismatch in", tname));
		}


		#define checksize(S,t)	fchecksize(S,sizeof(t),#t)

		private static void checkHeader (LoadState *S) {
		  checkliteral(S, LUA_SIGNATURE + 1, "not a");  /* 1st char already checked */
		  if (LoadByte(S) != LUAC_VERSION)
		    error(S, "version mismatch in");
		  if (LoadByte(S) != LUAC_FORMAT)
		    error(S, "format mismatch in");
		  checkliteral(S, LUAC_DATA, "corrupted");
		  checksize(S, int);
		  checksize(S, size_t);
		  checksize(S, Instruction);
		  checksize(S, lua_Integer);
		  checksize(S, lua_Number);
		  if (LoadInteger(S) != LUAC_INT)
		    error(S, "endianess mismatch in");
		  if (LoadNumber(S) != LUAC_NUM)
		    error(S, "float format mismatch in");
		}

		/*
		** load precompiled chunk
		*/
		public static Closure luaU_undump (lua_State L, ZIO Z, Mbuffer buff, 
		                                   CharPtr name) {
		  LoadState S = new LoadState();
		  Closure cl;
		  if (name[0] == '@' || name[0] == '=')
		    S.name = name+1;
		  else if (name[0]==LUA_SIGNATURE[0])
		    S.name="binary string";
		  else
		    S.name=name;
		  S.L=L;
		  S.Z=Z;
		  S.b=buff;
		  checkHeader(&S);
		  cl = luaF_newLclosure(L, LoadByte(&S));
		  setclLvalue(L, L->top, cl);
		  incr_top(L);
		  cl->l.p = luaF_newproto(L);
		  LoadFunction(&S, cl->l.p);
		  lua_assert(cl->l.nupvalues == cl->l.p->sizeupvalues);
		  luai_verifycode(L,buff,cl.l.p);
		  return cl;
		}

	}
}

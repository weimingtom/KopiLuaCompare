/*
** $Id: lundump.c,v 2.22.1.1 2013/04/12 18:48:47 roberto Exp $
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

	public partial class Lua
	{

		public class LoadState{
			public lua_State L;
			public ZIO Z;
			public Mbuffer b;
			public CharPtr name;
		};

		public static void/*l_noret*/ error(LoadState S, CharPtr why)
		{
		 luaO_pushfstring(S.L,"%s: %s precompiled chunk",S.name,why);
		 luaD_throw(S.L,LUA_ERRSYNTAX);
		}

		public static object LoadMem(LoadState S, Type t) //FIXME: changed
		{
			int size = Marshal.SizeOf(t);
			CharPtr str = new char[size];
			LoadBlock(S, str, size);
			byte[] bytes = new byte[str.chars.Length];
			for (int i = 0; i < str.chars.Length; i++)
				bytes[i] = (byte)str.chars[i];
			GCHandle pinnedPacket = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			object b = Marshal.PtrToStructure(pinnedPacket.AddrOfPinnedObject(), t);
			pinnedPacket.Free();
			return b;
		}

		public static object LoadMem(LoadState S, Type t, int n) //FIXME: changed, ref->return
		{
			ArrayList array = new ArrayList();
			for (int i=0; i<n; i++)
				array.Add(LoadMem(S, t));
			return array.ToArray(t);
		}
		public static lu_byte LoadByte(LoadState S)		{return (lu_byte)LoadChar(S);}
		public static object LoadVar(LoadState S, Type t) { return LoadMem(S, t); } //FIXME:changed, ref->return
		public static object LoadVector(LoadState S, Type t, int n) {return LoadMem(S, t, n);} //FIXME:changed, ref->return

		//#if !defined(luai_verifycode)
		public static void luai_verifycode(lua_State L, Mbuffer b, Proto f) { /* empty */ }
		//#endif

		private static void LoadBlock(LoadState S, CharPtr b, int size)
		{
		 if (luaZ_read(S.Z, b, (uint)size)!=0) error(S,"truncated"); //FIXME:(uint)
		}

		private static int LoadChar(LoadState S) 
		{
		 return (char)LoadVar(S, typeof(char)); //FIXME: changed
		}

		private static int LoadInt(LoadState S)
		{
		 int x;
		 x = (int)LoadVar(S, typeof(int)); //FIXME: changed
		 if (x<0) error(S,"corrupted");
		 return x;
		}

		private static lua_Number LoadNumber(LoadState S)
		{
		 return (lua_Number)LoadVar(S, typeof(lua_Number));
		}

		private static TString LoadString(LoadState S)
		{
		 uint size = (uint)LoadVar(S, typeof(uint));
		 if (size==0)
		  return null;
		 else
		 {
		  CharPtr s=luaZ_openspace(S.L,S.b,size);
		  LoadBlock(S, s, (int)size*1); //FIXME:changed, sizeof(char)
		  return luaS_newlstr(S.L,s,size-1);		/* remove trailing '\0' */
		 }
		}

		private static void LoadCode(LoadState S, Proto f)
		{
		 int n=LoadInt(S);
		 f.code = luaM_newvector<Instruction>(S.L, n);
		 f.sizecode=n;
		 f.code = (Instruction[])LoadVector(S, typeof(Instruction), n);
		}

		//static void LoadFunction(LoadState* S, Proto* f);

		private static void LoadConstants(LoadState S, Proto f)
		{
		 int i,n;
		 n=LoadInt(S);
		 f.k = luaM_newvector<TValue>(S.L, n);
		 f.sizek=n;
		 for (i=0; i<n; i++) setnilvalue(f.k[i]);
		 for (i=0; i<n; i++)
		 {
		  TValue o=f.k[i];
		  int t=LoadChar(S);
		  switch (t)
		  {
		   case LUA_TNIL:
   			setnilvalue(o);
			break;
		   case LUA_TBOOLEAN:
			setbvalue(o, LoadChar(S)!=0 ? 1 : 0); //FIXME:???!=0->(empty)
			break;
		   case LUA_TNUMBER:
			setnvalue(o, LoadNumber(S));
			break;
		   case LUA_TSTRING:
			setsvalue2n(S.L, o, LoadString(S));
			break;
		   default: lua_assert(0); break;
		  }
		 }
		 n=LoadInt(S);
		 f.p=luaM_newvector<Proto>(S.L,n);
		 f.sizep=n;
		 for (i=0; i<n; i++) f.p[i]=null;
		 for (i=0; i<n; i++) 
		 {
		    f.p[i]=luaF_newproto(S.L);
		 	LoadFunction(S,f.p[i]);
		 }
		}

		private static void LoadUpvalues(LoadState S, Proto f)
		{
		 int i,n;
		 n=LoadInt(S);
		 f.upvalues=luaM_newvector<Upvaldesc>(S.L,n);
		 f.sizeupvalues=n;
		 for (i=0; i<n; i++) f.upvalues[i].name=null;
		 for (i=0; i<n; i++)
		 {
		  f.upvalues[i].instack=LoadByte(S);
		  f.upvalues[i].idx=LoadByte(S);
		 }
		}

		private static void LoadDebug(LoadState S, Proto f)
		{
		 int i,n;
         f.source=LoadString(S);
		 n=LoadInt(S);
		 f.lineinfo=luaM_newvector<int>(S.L,n);
		 f.sizelineinfo=n;
		 f.lineinfo = (int[])LoadVector(S, typeof(int), n);
		 n=LoadInt(S);
		 f.locvars=luaM_newvector<LocVar>(S.L,n);
		 f.sizelocvars=n;
		 for (i=0; i<n; i++) f.locvars[i].varname=null;
		 for (i=0; i<n; i++)
		 {
		  f.locvars[i].varname=LoadString(S);
		  f.locvars[i].startpc=LoadInt(S);
		  f.locvars[i].endpc=LoadInt(S);
		 }
		 n=LoadInt(S);
		 for (i=0; i<n; i++) f.upvalues[i].name=LoadString(S);
		}

		private static void LoadFunction(LoadState S, Proto f)
		{
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

		/* the code below must be consistent with the code in luaU_header */
		private readonly static int N0 = LUAC_HEADERSIZE;
		private readonly static int N1 = LUA_SIGNATURE.Length; //FIXME:changed, (sizeof(LUA_SIGNATURE)-sizeof(char));
		private readonly static int N2 = N1+2;
		private readonly static int N3 = N2+6;

		private static void LoadHeader(LoadState S)
		{
		 CharPtr h = new char[LUAC_HEADERSIZE]; //FIXME:changed, lu_byte[]
		 CharPtr s = new char[LUAC_HEADERSIZE]; //FIXME:changed, lu_byte[]
		 luaU_header(h);
		 memcpy(s,h,sizeof(char));			/* first char already read */
		 LoadBlock(S,s+sizeof(char),LUAC_HEADERSIZE-sizeof(char));
		 if (memcmp(h,s,N0)==0) return;
		 if (memcmp(h,s,N1)!=0) error(S,"not a");
		 if (memcmp(h,s,N2)!=0) error(S,"version mismatch in");
		 if (memcmp(h,s,N3)!=0) error(S,"incompatible"); else error(S,"corrupted");
		}

		/*
		** load precompiled chunk
		*/
		public static Closure luaU_undump (lua_State L, ZIO Z, Mbuffer buff, CharPtr name)
		{
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
		 LoadHeader(S);
		 cl=luaF_newLclosure(L,1);
		 setclLvalue(L,L.top,cl); incr_top(L);
		 cl.l.p=luaF_newproto(L);
		 LoadFunction(S,cl.l.p);
		 if (cl.l.p.sizeupvalues != 1)
		 {
		  Proto p=cl.l.p;
		  cl=luaF_newLclosure(L,cl.l.p.sizeupvalues);
		  cl.l.p=p;
		  setclLvalue(L,L.top-1,cl);
		 }
		 luai_verifycode(L,buff,cl.l.p);
		 return cl;
		}

		private static int MYINT(CharPtr s) { return (s[0]-'0'); }
		private readonly static int VERSION = MYINT(LUA_VERSION_MAJOR)*16+MYINT(LUA_VERSION_MINOR);
		private const int FORMAT = 0;		/* this is the official format */

		/*
		* make header for precompiled chunks
		* if you change the code below be sure to update LoadHeader and FORMAT above
		* and LUAC_HEADERSIZE in lundump.h
		*/
		public static void luaU_header(CharPtr h) //FIXME:changed, lu_byte*
		{
		 int x=1;
		 memcpy(h, LUA_SIGNATURE, (uint)LUA_SIGNATURE.Length); //FIXME:changed, sizeof(LUA_SIGNATURE)-sizeof(char)
		 h = h.add(LUA_SIGNATURE.Length); //FIXME:changed, sizeof(LUA_SIGNATURE)-sizeof(char);
		 h[0] = (char)(byte)VERSION; h.inc(); //FIXME:changed, (char)
		 h[0] = (char)(byte)FORMAT; h.inc(); //FIXME:changed, (char)
		 h[0] = (char)(byte)x; h.inc();				/* endianness */ //FIXME:changed, *h++=cast_byte(*(char*)&x); //FIXME:changed, (char)
		 h[0] = (char)(byte)GetUnmanagedSize(typeof(int)); h.inc(); //FIXME:changed, (char)
		 h[0] = (char)(byte)GetUnmanagedSize(typeof(uint)); h.inc(); //FIXME:changed, (char)
		 h[0] = (char)(byte)GetUnmanagedSize(typeof(Instruction)); h.inc(); //FIXME:changed, (char)
		 h[0] = (char)(byte)GetUnmanagedSize(typeof(lua_Number)); h.inc(); //FIXME:changed, (char)
         h[0] = (char)(byte)(((lua_Number)0.5)==0 ? 1 : 0); h.inc();		/* is lua_Number integral? */ //FIXME:???always 0 on this build //FIXME:changed, (char)
         memcpy(h,LUAC_TAIL,(uint)LUAC_TAIL.Length); //FIXME:changed, sizeof(LUAC_TAIL)-sizeof(char)
		}

	}
}

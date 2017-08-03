/*
** $Id: lundump.c,v 1.69 2011/05/06 13:35:17 lhf Exp $
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

		static void error(LoadState S, CharPtr why)
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

		public static object LoadMem(LoadState S, Type t, int n) //FIXME: changed
		{
			ArrayList array = new ArrayList();
			for (int i=0; i<n; i++)
				array.Add(LoadMem(S, t));
			return array.ToArray(t);
		}
		public static lu_byte LoadByte(LoadState S)		{return (lu_byte)LoadChar(S);}
		public static object LoadVar(LoadState S, Type t) { return LoadMem(S, t); }
		public static object LoadVector(LoadState S, Type t, int n) {return LoadMem(S, t, n);}

		//#if !defined(luai_verifycode)
		public static void luai_verifycode(L,b,f) { return f; }
		//#endif

		private static void LoadBlock(LoadState S, CharPtr b, int size)
		{
		 if (luaZ_read(S.Z, b, (uint)size)!=0) error(S,"corrupted"); //FIXME:(uint)
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
		  }
		 }
		 n=LoadInt(S);
		 f.p=luaM_newvector<Proto>(S.L,n);
		 f.sizep=n;
		 for (i=0; i<n; i++) f.p[i]=null;
		 for (i=0; i<n; i++) f.p[i]=LoadFunction(S);
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
		  f.upvalues[i].instack=(byte)LoadChar(S); //FIXME:(byte)
		  f.upvalues[i].idx=(byte)LoadChar(S); //FIXME:(byte)
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

		private static Proto LoadFunction(LoadState S)
		{
		 Proto f=luaF_newproto(S.L);
		 setptvalue2s(S.L,S.L.top,f); incr_top(S.L);
		 f.linedefined=LoadInt(S);
		 f.lastlinedefined=LoadInt(S);
		 f.numparams=LoadByte(S);
		 f.is_vararg=LoadByte(S);
		 f.maxstacksize=LoadByte(S);
		 LoadCode(S,f);
		 LoadConstants(S,f);
         LoadUpvalues(S,f);
		 LoadDebug(S,f);
		 StkId.dec(ref S.L.top);
		 return f;
		}

		/* the code below must be consistent with the code in luaU_header */
		private const int N0 = LUAC_HEADERSIZE;
		private const int N1 = (sizeof(LUA_SIGNATURE)-sizeof(char));
		private const int N2 = N1+2;
		private const int N3 = N2+6;

		private static void LoadHeader(LoadState S)
		{
		 CharPtr h = new lu_byte[LUAC_HEADERSIZE]; //FIXME:changed, lu_byte[]
		 CharPtr s = new lu_byte[LUAC_HEADERSIZE]; //FIXME:changed, lu_byte[]
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
		public static Proto luaU_undump (lua_State L, ZIO Z, Mbuffer buff, CharPtr name)
		{
		 LoadState S = new LoadState();
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
		 return luai_verifycode(L,buff,LoadFunction(&S));
		}

		private static int MYINT(s) { return (s[0]-'0'); }
		private const int VERSION = MYINT(LUA_VERSION_MAJOR)*16+MYINT(LUA_VERSION_MINOR);
		private const int FORMAT = 0;		/* this is the official format */

		/*
		* make header for precompiled chunks
		* if you change the code below be sure to update LoadHeader and FORMAT above
		* and LUAC_HEADERSIZE in lundump.h
		*/
		public static void luaU_header(lu_byte[] h) //FIXME:changed, lu_byte*
		{
		 int x=1;
		 memcpy(h, LUA_SIGNATURE, LUA_SIGNATURE.Length); //FIXME:changed, sizeof(LUA_SIGNATURE)-sizeof(char) 
		 h = h.add(LUA_SIGNATURE.Length); //FIXME:changed, sizeof(LUA_SIGNATURE)-sizeof(char);
		 h[0] = (byte)LUAC_VERSION; h.inc();
		 h[0] = (byte)LUAC_FORMAT; h.inc();
		 h[0] = (byte)x; h.inc();				/* endianness */ //FIXME:changed, *h++=cast_byte(*(char*)&x);
		 h[0] = (byte)GetUnmanagedSize(typeof(int)); h.inc();
		 h[0] = (byte)GetUnmanagedSize(typeof(uint)); h.inc();
		 h[0] = (byte)GetUnmanagedSize(typeof(Instruction)); h.inc();
		 h[0] = (byte)GetUnmanagedSize(typeof(lua_Number)); h.inc();
         h[0] = (byte)(((lua_Number)0.5)==0 ? 1 : 0); h.inc();		/* is lua_Number integral? */ //FIXME:???always 0 on this build
		 memcpy(h,LUAC_TAIL,sizeof(LUAC_TAIL)-sizeof(char));
		}

	}
}

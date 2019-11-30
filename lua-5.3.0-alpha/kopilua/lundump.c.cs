/*
** $Id: lundump.c,v 2.40 2014/06/19 18:27:20 roberto Exp $
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
		private static void luai_verifycode(lua_State L, Mbuffer b, Proto f) { /* empty */ }
		//#endif


		private class LoadState{
		  public lua_State L;
		  public ZIO Z;
		  public Mbuffer b;
		  public CharPtr name;
		};


		private static void/*l_noret*/ error(LoadState S, CharPtr why) {
		  luaO_pushfstring(S.L,"%s: %s precompiled chunk",S.name,why);
		  luaD_throw(S.L,LUA_ERRSYNTAX);
		}

		/*
		** All high-level loads go through LoadVector; you can change it to
		** adapt to the endianness of the input
		*/
		private static void LoadVector(LoadState S, CharPtr b, int n) { LoadBlock(S,b,n /* *sizeof((b)[0])*/ ); }
		private static void LoadVector(LoadState S, Instruction[] b, int n)	{ LoadBlock(S,b,n /* *sizeof((b)[0])*/ ); }
		private static void LoadVector(LoadState S, int[] b, int n)	{ LoadBlock(S,b,n/**sizeof((b)[0])*/ ); }
		private static void LoadVector(LoadState S, ref lu_byte b) { LoadBlock(S,ref b); }
		private static void LoadVector(LoadState S, ref lua_Number b) { LoadBlock(S,ref b); }
		private static void LoadVector(LoadState S, ref lua_Integer b) { LoadBlock(S,ref b); }
		private static void LoadVector(LoadState S, ref uint b) { LoadBlock(S,ref b); }
		
		private static void LoadBlock (LoadState S, int[] b, int size) {
		  //-----------------
		  //not implemented, use ref???
		  int s_ = Marshal.SizeOf(b[0]);
		  CharPtr b__ = new char[size * s_];			
		  //-----------------
		  
		  if (luaZ_read(S.Z, b__, (uint)(size * s_)) != 0) { //FIXME:(uint)
		    error(S, "truncated");
		  } else {
			//FIXME:added, from byte[] to object[]
			//FIXME:not check
			for (int k = 0; k < size; ++k)
			{
				byte[] bytes = new byte[s_];
				for (int i = 0; i < s_; i++)
					bytes[i] = (byte)b__.chars[k * s_ + i];
				GCHandle pinnedPacket = GCHandle.Alloc(bytes, GCHandleType.Pinned);
				int b2 = (int)Marshal.PtrToStructure(pinnedPacket.AddrOfPinnedObject(), b[k].GetType());
				pinnedPacket.Free();
				b[k] = (int)b2;
			}
		  }
		}
		private static void LoadBlock (LoadState S, Instruction[] b, int size) {
		  //-----------------
		  //not implemented, use ref???
		  int s_ = Marshal.SizeOf(b[0]);
		  CharPtr b__ = new char[size * s_];			
		  //-----------------
		  
		  if (luaZ_read(S.Z, b__, (uint)(size * s_)) != 0) { //FIXME:(uint)
		    error(S, "truncated");
		  } else {
			//FIXME:added, from byte[] to object[]
			//FIXME:not check
			for (int k = 0; k < size; ++k)
			{
				byte[] bytes = new byte[s_];
				for (int i = 0; i < s_; i++)
					bytes[i] = (byte)b__.chars[k * s_ + i];
				GCHandle pinnedPacket = GCHandle.Alloc(bytes, GCHandleType.Pinned);
				Instruction b2 = (Instruction)Marshal.PtrToStructure(pinnedPacket.AddrOfPinnedObject(), b[k].GetType());
				pinnedPacket.Free();
				b[k] = (Instruction)b2;
			}
		  }
		}
		private static void LoadBlock (LoadState S, CharPtr b, int size) {
		  //-----------------
		  //not implemented, use ref???
		  int s_ = Marshal.SizeOf(b[0]);
		  CharPtr b__ = new char[size * s_];			
		  //-----------------
		  
		  if (luaZ_read(S.Z, b__, (uint)(size * s_)) != 0) { //FIXME:(uint)
		    error(S, "truncated");
		  } else {
		  	for (int k = 0; k < size; ++k)
		  	{
		  		b[k] = b__[k];
		  	}
		  }
		}
		private static void LoadBlock (LoadState S, ref lu_byte b) {
		  //-----------------
		  //not implemented, use ref???
		  int s_ = Marshal.SizeOf(b);
		  CharPtr b__ = new char[1 * s_];			
		  //-----------------
		  
		  if (luaZ_read(S.Z, b__, (uint)(s_ * 1)) != 0) { //FIXME:(uint)
		    error(S, "truncated");
		  } else {
			//FIXME:added, from byte[] to object[]
			//FIXME:not check
			byte[] bytes = new byte[s_];
			for (int i = 0; i < s_; i++)
				bytes[i] = (byte)b__.chars[i];
			GCHandle pinnedPacket = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			lu_byte b2 = (lu_byte)Marshal.PtrToStructure(pinnedPacket.AddrOfPinnedObject(), b.GetType());
			pinnedPacket.Free();
			b = (lu_byte)b2;
		  }
		}
		private static void LoadBlock (LoadState S, ref lua_Number b) {
		  //-----------------
		  //not implemented, use ref???
		  int s_ = Marshal.SizeOf(b);
		  CharPtr b__ = new char[1 * s_];			
		  //-----------------
		  
		  if (luaZ_read(S.Z, b__, (uint)(s_ * 1)) != 0) { //FIXME:(uint)
		    error(S, "truncated");
		  } else {
			//FIXME:added, from byte[] to object[]
			//FIXME:not check
			byte[] bytes = new byte[s_];
			for (int i = 0; i < s_; i++)
				bytes[i] = (byte)b__.chars[i];
			GCHandle pinnedPacket = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			lua_Number b2 = (lua_Number)Marshal.PtrToStructure(pinnedPacket.AddrOfPinnedObject(), b.GetType());
			pinnedPacket.Free();
			b = (lua_Number)b2;
		  }
		}
		private static void LoadBlock (LoadState S, ref lua_Integer b) {
		  //-----------------
		  //not implemented, use ref???
		  int s_ = Marshal.SizeOf(b);
		  CharPtr b__ = new char[1 * s_];			
		  //-----------------
		  
		  if (luaZ_read(S.Z, b__, (uint)(s_ * 1)) != 0) { //FIXME:(uint)
		    error(S, "truncated");
		  } else {
			//FIXME:added, from byte[] to object[]
			//FIXME:not check
			byte[] bytes = new byte[s_];
			for (int i = 0; i < s_; i++)
				bytes[i] = (byte)b__.chars[i];
			GCHandle pinnedPacket = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			lua_Integer b2 = (lua_Integer)Marshal.PtrToStructure(pinnedPacket.AddrOfPinnedObject(), b.GetType());
			pinnedPacket.Free();
			b = (lua_Integer)b2;
		  }
		}		
		private static void LoadBlock (LoadState S, ref uint b) {
		  //-----------------
		  //not implemented, use ref???
		  int s_ = Marshal.SizeOf(b);
		  CharPtr b__ = new char[1 * s_];			
		  //-----------------
		  
		  if (luaZ_read(S.Z, b__, (uint)(s_ * 1)) != 0) { //FIXME:(uint)
		    error(S, "truncated");
		  } else {
			//FIXME:added, from byte[] to object[]
			//FIXME:not check
			byte[] bytes = new byte[s_];
			for (int i = 0; i < s_; i++)
				bytes[i] = (byte)b__.chars[i];
			GCHandle pinnedPacket = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			uint b2 = (uint)Marshal.PtrToStructure(pinnedPacket.AddrOfPinnedObject(), b.GetType());
			pinnedPacket.Free();
			b = (uint)b2;
		  }
		}
		
		private static void LoadVar(LoadState S, ref lu_byte x)		{ LoadVector(S,ref x/*,1*/); }
		private static void LoadVar(LoadState S, ref lua_Number x)		{ LoadVector(S,ref x/*,1*/); }
		private static void LoadVar(LoadState S, ref lua_Integer x)		{ LoadVector(S,ref x/*,1*/); }
		private static void LoadVar(LoadState S, ref uint x)		{ LoadVector(S,ref x/*,1*/); }

		private static lu_byte LoadByte (LoadState S) {
		  lu_byte x = 0;
		  LoadVar(S, ref x);
		  return x;
		}


		private static int LoadInt (LoadState S) {
		  int x = 0;
		  LoadVar(S, ref x);
		  return x;
		}


		private static lua_Number LoadNumber (LoadState S) {
		  lua_Number x = 0;
		  LoadVar(S, ref x);
		  return x;
		}


		private static lua_Integer LoadInteger (LoadState S) {
		  lua_Integer x = 0;
		  LoadVar(S, ref x);
		  return x;
		}


		private static TString LoadString (LoadState S) {
		  uint size = LoadByte(S);
		  if (size == 0xFF)
		    LoadVar(S, ref size);
		  if (size == 0)
		    return null;
		  else {
		    CharPtr s = luaZ_openspace(S.L, S.b, --size);
		    LoadVector(S, s, (int)size);
		    return luaS_newlstr(S.L, s, size);
		  }
		}

		private static void LoadCode (LoadState S, Proto f) {
		  int n = LoadInt(S);
		  f.code = luaM_newvector<Instruction>(S.L, n);
		  f.sizecode=n;
		  LoadVector(S, f.code, n);
		}


		//static void LoadFunction(LoadState* S, Proto* f, TString *psource);


		private static void LoadConstants (LoadState S, Proto f) {
		  int i;
		  int n = LoadInt(S);
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
			  setfltvalue(o, LoadNumber(S));
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
		}


		private static void LoadProtos (LoadState S, Proto f) {
		  int i;
		  int n = LoadInt(S);
		  f.p = luaM_newvector<Proto>(S.L, n);
		  f.sizep = n;
		  for (i = 0; i < n; i++) 
		    f.p[i] = null;
		  for (i = 0; i < n; i++) 
		  {
		    f.p[i] = luaF_newproto(S.L);
		 	LoadFunction(S, f.p[i], f.source);
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
		  int i, n;
		  n = LoadInt(S);
		  f.lineinfo = luaM_newvector<int>(S.L, n);
		  f.sizelineinfo = n;
		  LoadVector(S, f.lineinfo, n);
		  n = LoadInt(S);
		  f.locvars = luaM_newvector<LocVar>(S.L, n);
		  f.sizelocvars = n;
		  for (i = 0; i < n; i++) 
		    f.locvars[i].varname = null;
		  for (i = 0; i < n; i++) {
		    f.locvars[i].varname = LoadString(S);
		    f.locvars[i].startpc = LoadInt(S);
		    f.locvars[i].endpc = LoadInt(S);
		  }
		  n = LoadInt(S);
		  for (i = 0; i < n; i++) 
		    f.upvalues[i].name = LoadString(S);
		}

		private static void LoadFunction(LoadState S, Proto f, TString psource) {
		  f.source = LoadString(S);
		  if (f.source == null)  /* no source in dump? */
		    f.source = psource;  /* reuse parent's source */		
		  f.linedefined = LoadInt(S);
		  f.lastlinedefined = LoadInt(S);
		  f.numparams = LoadByte(S);
		  f.is_vararg = LoadByte(S);
		  f.maxstacksize = LoadByte(S);
		  LoadCode(S, f);
		  LoadConstants(S, f);
          LoadUpvalues(S, f);
		  LoadProtos(S, f);
		  LoadDebug(S, f);
		}

		private static void checkliteral (LoadState S, CharPtr s, CharPtr msg) {
		  CharPtr buff = new CharPtr(new char[(LUA_SIGNATURE.Length + 1) + (LUAC_DATA.Length + 1)]); /* larger than both */
		  uint len = (uint)strlen(s);
		  LoadVector(S, buff, (int)len);
		  if (memcmp(s, buff, len) != 0)
		    error(S, msg);
		}


		private static void fchecksize (LoadState S, uint size, CharPtr tname) {
		  if (LoadByte(S) != size)
		    error(S, luaO_pushfstring(S.L, "%s size mismatch in", tname));
		}


		private static void checksize(LoadState S, Type t, string sharp_t) { fchecksize(S, (uint)GetUnmanagedSize(t), new CharPtr(sharp_t));}

		private static void checkHeader (LoadState S) {
		  checkliteral(S, LUA_SIGNATURE + 1, "not a");  /* 1st char already checked */
		  if (LoadByte(S) != LUAC_VERSION)
		    error(S, "version mismatch in");
		  if (LoadByte(S) != LUAC_FORMAT)
		    error(S, "format mismatch in");
		  checkliteral(S, LUAC_DATA, "corrupted");
		  checksize(S, typeof(int), "int");
		  checksize(S, typeof(uint), "size_t");
		  checksize(S, typeof(Instruction), "Instruction");
		  checksize(S, typeof(lua_Integer), "lua_Integer");
		  checksize(S, typeof(lua_Number), "lua_Number");
		  if (LoadInteger(S) != LUAC_INT)
		    error(S, "endianness mismatch in");
		  if (LoadNumber(S) != LUAC_NUM)
		    error(S, "float format mismatch in");
		}

		/*
		** load precompiled chunk
		*/
		public static LClosure luaU_undump (lua_State L, ZIO Z, Mbuffer buff, 
		                                   CharPtr name) {
		  LoadState S = new LoadState();
		  LClosure cl;
		  if (name[0] == '@' || name[0] == '=')
		    S.name = name + 1;
		  else if (name[0] == LUA_SIGNATURE[0])
		    S.name = "binary string";
		  else
		    S.name = name;
		  S.L = L;
		  S.Z = Z;
		  S.b = buff;
		  checkHeader(S);
		  cl = luaF_newLclosure(L, LoadByte(S));
		  setclLvalue(L, L.top, cl);
		  incr_top(L);
		  cl.p = luaF_newproto(L);
		  LoadFunction(S, cl.p, null);
		  lua_assert(cl.nupvalues == cl.p.sizeupvalues);
		  luai_verifycode(L, buff, cl.p);
		  return cl;
		}

	}
}

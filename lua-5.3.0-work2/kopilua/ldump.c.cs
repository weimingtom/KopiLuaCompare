/*
** $Id: ldump.c,v 2.27 2014/03/11 18:56:27 roberto Exp $
** save precompiled Lua chunks
** See Copyright Notice in lua.h
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;


namespace KopiLua
{
	using lua_Number = System.Double;
	using TValue = Lua.lua_TValue;
	using lu_byte = System.Byte;
	using lua_Integer = System.Int32;
	using Instruction = System.UInt32;

	public partial class Lua
	{

		public class DumpState {
		  public lua_State L;
		  public lua_Writer writer;
		  public object data;
		  public int strip;
		  public int status;
		};


		/*
		** All high-level dumps go through DumpVector; you can change it to
		** change the endianess of the result
		*/
		public static void DumpVector(object[] v, int n, DumpState D)	{ throw new Exception(); /*DumpBlock(v,(n)*sizeof(v[0]),D);*/ }
		public static void DumpVector(uint[] v, int n, DumpState D)	{ throw new Exception(); /*DumpBlock(v,(n)*sizeof(v[0]),D);*/ }
		public static void DumpVector(CharPtr v, int n, DumpState D)	{ throw new Exception(); /*DumpBlock(v,(n)*sizeof(v[0]),D);*/ }
		public static void DumpVector(int[] v, int n, DumpState D)	{ throw new Exception(); /*DumpBlock(v,(n)*sizeof(v[0]),D);*/ }
		
		public static void DumpLiteral(string s, DumpState D)	{ throw new Exception(); DumpBlock(new CharPtr(s), (uint)((s.Length + 1) - 1/*sizeof(char)*/), D); }
/*		
		public static void DumpMem(object b, DumpState D)
		{
			int size = Marshal.SizeOf(b);
			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(b, ptr, false);
			byte[] bytes = new byte[size];
			Marshal.Copy(ptr, bytes, 0, size);
			char[] ch = new char[bytes.Length];
			for (int i = 0; i < bytes.Length; i++)
				ch[i] = (char)bytes[i];
			CharPtr str = ch;
			DumpBlock(str, (uint)str.chars.Length, D);
		}

		public static void DumpMem(object b, int n, DumpState D)
		{
			Array array = b as Array;
			Debug.Assert(array.Length == n);
			for (int i = 0; i < n; i++)
				DumpMem(array.GetValue(i), D);
		}

		public static void DumpVar(object x, DumpState D)
		{
			DumpMem(x, D);
		}
*/
		private static void DumpBlock(CharPtr b, uint size, DumpState D) {
		  if (D.status==0) {
		    lua_unlock(D.L);
		    D.status=D.writer(D.L,b,size,D.data);
		    lua_lock(D.L);
		  }
		}


		public static void DumpVar(double x, DumpState D)	{ throw new Exception(); /*DumpVector(&x,1,D);*/ }


		private static void DumpByte (int y, DumpState D) {
		  lu_byte x=(lu_byte)y;
		  DumpVar(x, D);
		}


		private static void DumpInt (int x, DumpState D) {
		  DumpVar(x, D);
		}


		private static void DumpNumber (lua_Number x, DumpState D) {
		  DumpVar(x, D);
		}


		private static void DumpInteger (lua_Integer x, DumpState D) {
		  DumpVar(x, D);
		}


		private static void DumpString(TString s, DumpState D) {
		  if (s == null)
		    DumpByte(0, D);
		  else {
		    uint size = s.tsv.len + 1;  /* include trailing '\0' */
		    if (size < 0xFF)
		      DumpByte((int)size, D);
		    else {
		      DumpByte(0xFF, D);
		      DumpVar(size, D);
		    }
		    DumpVector(getstr(s), (int)(size - 1), D);  /* no need to save '\0' */
		  }
		}


		private static void DumpCode (Proto f, DumpState D) {
		  DumpInt(f.sizecode, D);
		  DumpVector(f.code, f.sizecode, D);
		}
		
		
        //static void DumpFunction(const Proto* f, DumpState* D);

		private static void DumpConstants (Proto f, DumpState D) {
		  int i;
		  int n = f.sizek;
		  DumpInt(n,D);
		  for (i=0; i<n; i++) {
		    /*const*/ TValue o=f.k[i];
		    DumpByte(ttype(o),D);
		    switch (ttype(o)) {
		    case LUA_TNIL:
			  break;
		    case LUA_TBOOLEAN:
			  DumpByte(bvalue(o),D);
			  break;
		    case LUA_TNUMFLT:
			  DumpNumber(fltvalue(o),D);
			  break;
		    case LUA_TNUMINT:
			  DumpInteger(ivalue(o),D);
			  break;
		    case LUA_TSHRSTR: 
			case LUA_TLNGSTR:
		      DumpString(rawtsvalue(o),D);
			  break;
		    default: 
			  lua_assert(0); 
			  break;
		    }  
		  }
		  n=f.sizep;
		  DumpInt(n,D);
		  for (i = 0; i < n; i++) 
		    DumpFunction(f.p[i],D);
		}


		private static void DumpUpvalues (Proto f, DumpState D) {
		  int i, n = f.sizeupvalues;
		  DumpInt(n, D);
		  for (i = 0; i < n; i++) {
		    DumpByte(f.upvalues[i].instack, D);
		    DumpByte(f.upvalues[i].idx, D);
		  }
		}

		private static void DumpDebug (Proto f, DumpState D) {
		  int i,n;
          DumpString((D.strip!=0) ? null : f.source, D);
		  n = (D.strip != 0) ? 0 : f.sizelineinfo;
		  DumpInt(n, D);
		  DumpVector(f.lineinfo, n, D);
		  n= (D.strip != 0) ? 0 : f.sizelocvars;
		  DumpInt(n,D);
		  for (i=0; i<n; i++) {
		    DumpString(f.locvars[i].varname, D);
		    DumpInt(f.locvars[i].startpc, D);
		    DumpInt(f.locvars[i].endpc, D);
		  }
		  n= (D.strip != 0) ? 0 : f.sizeupvalues;
		  DumpInt(n, D);
		  for (i = 0; i < n; i++) 
		    DumpString(f.upvalues[i].name, D);
		}

		private static void DumpFunction (Proto f, DumpState D) {
		  DumpInt(f.linedefined, D);
		  DumpInt(f.lastlinedefined, D);
		  DumpByte(f.numparams, D);
		  DumpByte(f.is_vararg, D);
		  DumpByte(f.maxstacksize, D);
		  DumpCode(f, D);
		  DumpConstants(f, D);
          DumpUpvalues(f, D);
		  DumpDebug(f, D);
		}

		private static void DumpHeader (DumpState D) {
		  DumpLiteral(LUA_SIGNATURE, D);
		  DumpByte(LUAC_VERSION, D);
		  DumpByte(LUAC_FORMAT, D);
		  DumpLiteral(LUAC_DATA, D);
		  DumpByte(sizeof(int), D);
		  DumpByte(sizeof(/*size_t*/uint), D);
		  DumpByte(sizeof(Instruction), D);
		  DumpByte(sizeof(lua_Integer), D);
		  DumpByte(sizeof(lua_Number), D);
		  DumpInteger(LUAC_INT, D);
		  DumpNumber(LUAC_NUM, D);
		}

		/*
		** dump Lua function as precompiled chunk
		*/
		public static int luaU_dump(lua_State L, Proto f, lua_Writer w, object data, 
		                            int strip) {
		  DumpState D = new DumpState();
		  D.L = L;
		  D.writer = w;
		  D.data = data;
		  D.strip = strip;
		  D.status = 0;
		  DumpHeader(D);
		  DumpByte(f.sizeupvalues, D);
		  DumpFunction(f, D);
		  return D.status;
		}
	}
}

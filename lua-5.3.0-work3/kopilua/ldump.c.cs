/*
** $Id: ldump.c,v 2.32 2014/06/18 18:35:43 roberto Exp $
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
		** change the endianness of the result
		*/
		public static void DumpVector(object v, uint n, DumpState D)	{ DumpBlock(v, n/* *sizeof((v)[0])*/, D); } //FIXME: here no need to use *sizeof(v[0])
		public static void DumpVector(object v, DumpState D)	{ DumpBlock(v, D); } //FIXME: NOTE!!!if n == 1, use this version
		
		public static void DumpLiteral(string s, DumpState D)	{ DumpBlock(new CharPtr(s), (uint)((s.Length + 1) - 1/*sizeof(char)*/), D); }
		
		
		private static void DumpBlock(object b, uint size, DumpState D) { //FIXME:b as array
		  Array array = b as Array;
		  Debug.Assert(array.Length == size);
		  List<char[]> arrB = new List<char[]>();
		  int arrBLen = 0;
		  for (int i = 0; i < size; i++)
		  {
		    object b_ = array.GetValue(i);
			int size_ = Marshal.SizeOf(b_);
			IntPtr ptr = Marshal.AllocHGlobal(size_);
			Marshal.StructureToPtr(b_, ptr, false);
			byte[] bytes = new byte[size_];
			Marshal.Copy(ptr, bytes, 0, size_);
			char[] ch = new char[bytes.Length];
			for (int i_ = 0; i_ < bytes.Length; i_++)
				ch[i_] = (char)bytes[i_];
			arrB.Add(ch);
			arrBLen += ch.Length;
		  }
		  char[] strB = new char[arrBLen];
		  int pos = 0;
		  for (int i = 0; i < arrB.Count; ++i)
		  {
		  	for (int i_ = 0; i_ < arrB[i].Length; i_++)
		  		strB[pos + i_] = (char)arrB[i][i_];
		  	pos += arrB[i].Length;
		  }
		  CharPtr b__ = strB;
		  uint size__ = (uint)strB.Length;
		  
		  if (D.status==0) {
		    lua_unlock(D.L);
		    D.status=D.writer(D.L,b__,size__,D.data);
		    lua_lock(D.L);
		  }
		}
		private static void DumpBlock(object b, DumpState D) { //FIXME:b as not array
		  object b_ = b;
		  int size_ = Marshal.SizeOf(b_);
		  IntPtr ptr = Marshal.AllocHGlobal(size_);
		  Marshal.StructureToPtr(b_, ptr, false);
		  byte[] bytes = new byte[size_];
		  Marshal.Copy(ptr, bytes, 0, size_);
		  char[] ch = new char[bytes.Length];
		  for (int i_ = 0; i_ < bytes.Length; i_++)
		    ch[i_] = (char)bytes[i_];
		  CharPtr b__ = ch;
		  uint size__ = (uint)ch.Length;
		  
		  if (D.status==0) {
		    lua_unlock(D.L);
		    D.status=D.writer(D.L,b__,size__,D.data);
		    lua_lock(D.L);
		  }
		}		

		public static void DumpVar(object x, DumpState D)	{ DumpVector(x/*,1*/,D); }


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
		    DumpVector(getstr(s), size - 1, D);  /* no need to save '\0' */
		  }
		}


		private static void DumpCode (Proto f, DumpState D) {
		  DumpInt(f.sizecode, D);
		  DumpVector(f.code, (uint)f.sizecode, D);
		}
		
		
        //static void DumpFunction(const Proto* f, TString *psource, DumpState* D);

		private static void DumpConstants (Proto f, DumpState D) {
		  int i;
		  int n = f.sizek;
		  DumpInt(n,D);
		  for (i=0; i<n; i++) {
		    /*const*/ TValue o=f.k[i];
		    DumpByte(ttype(o), D);
		    switch (ttype(o)) {
		    case LUA_TNIL:
			  break;
		    case LUA_TBOOLEAN:
			  DumpByte(bvalue(o), D);
			  break;
		    case LUA_TNUMFLT:
			  DumpNumber(fltvalue(o), D);
			  break;
		    case LUA_TNUMINT:
			  DumpInteger(ivalue(o), D);
			  break;
		    case LUA_TSHRSTR: 
			case LUA_TLNGSTR:
		      DumpString(rawtsvalue(o), D);
			  break;
		    default: 
			  lua_assert(0); 
			  break;
		    }  
		  }
		}


		private static void DumpProtos (Proto f, DumpState D) {
		  int i;
		  int n = f.sizep;
		  DumpInt(n, D);
		  for (i = 0; i < n; i++)
		    DumpFunction(f.p[i], f.source, D);
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
		  int i, n;
		  n = (D.strip != 0) ? 0 : f.sizelineinfo;
		  DumpInt(n, D);
		  DumpVector(f.lineinfo, (uint)n, D);
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

		private static void DumpFunction (Proto f, TString psource, DumpState D) {
		  if (D->strip || f->source == psource)
		    DumpString(NULL, D);  /* no debug info or same source as its parent */
		  else
		    DumpString(f->source, D);		
		  DumpInt(f.linedefined, D);
		  DumpInt(f.lastlinedefined, D);
		  DumpByte(f.numparams, D);
		  DumpByte(f.is_vararg, D);
		  DumpByte(f.maxstacksize, D);
		  DumpCode(f, D);
		  DumpConstants(f, D);
          DumpUpvalues(f, D);
		  DumpProtos(f, D);
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
		  DumpFunction(f, null, D);
		  return D.status;
		}
	}
}

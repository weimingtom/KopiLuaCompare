/*
** $Id: ldump.c,v 1.18 2011/05/06 13:35:17 lhf Exp $
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

	public partial class Lua
	{

		public class DumpState {
		 public lua_State L;
		 public lua_Writer writer;
		 public object data;
		 public int strip;
		 public int status;
		};

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

		private static void DumpBlock(CharPtr b, uint size, DumpState D)
		{
		 if (D.status==0)
		 {
		  lua_unlock(D.L);
		  D.status=D.writer(D.L,b,size,D.data);
		  lua_lock(D.L);
		 }
		}

		private static void DumpChar(int y, DumpState D)
		{
		 char x=(char)y;
		 DumpVar(x,D);
		}

		private static void DumpInt(int x, DumpState D)
		{
		 DumpVar(x,D);
		}

		private static void DumpNumber(lua_Number x, DumpState D)
		{
		 DumpVar(x,D);
		}

		static void DumpVector(object b, int n, DumpState D)
		{
		 DumpInt(n,D);
		 DumpMem(b, n, D);
		}

		private static void DumpString(TString s, DumpState D)
		{
		 if (s==null)
		 {
		  uint size=0;
		  DumpVar(size,D);
		 }
		 else
		 {
		  uint size=s.tsv.len+1;		/* include trailing '\0' */
		  DumpVar(size,D);
		  DumpBlock(getstr(s),size*1,D); //FIXME:changed, *sizeof(char)
		 }
		}

		private static void DumpCode(Proto f,DumpState D) { DumpVector(f.code, f.sizecode, D); } //FIXME:no sizeof(Instruction)

        //static void DumpFunction(const Proto* f, DumpState* D);

		private static void DumpConstants(Proto f, DumpState D)
		{
		 int i,n=f.sizek;
		 DumpInt(n,D);
		 for (i=0; i<n; i++)
		 {
		  /*const*/ TValue o=f.k[i];
		  DumpChar(ttype(o),D);
		  switch (ttype(o))
		  {
		   case LUA_TNIL:
			break;
		   case LUA_TBOOLEAN:
			DumpChar(bvalue(o),D);
			break;
		   case LUA_TNUMBER:
			DumpNumber(nvalue(o),D);
			break;
		   case LUA_TSTRING:
			DumpString(rawtsvalue(o),D);
			break;
		  }
		 }
		 n=f.sizep;
		 DumpInt(n,D);
		 for (i=0; i<n; i++) DumpFunction(f.p[i],D);
		}

		private static void DumpUpvalues(Proto f, DumpState D)
		{
		 int i,n=f.sizeupvalues;
		 DumpInt(n,D);
		 for (i=0; i<n; i++)
		 {
		  DumpChar(f.upvalues[i].instack, D);
		  DumpChar(f.upvalues[i].idx, D);
		 }
		}

		private static void DumpDebug(Proto f, DumpState D)
		{
		 int i,n;
         DumpString((D.strip!=0) ? null : f.source,D);
		 n= (D.strip != 0) ? 0 : f.sizelineinfo;
		 DumpVector(f.lineinfo, n, D);
		 n= (D.strip != 0) ? 0 : f.sizelocvars;
		 DumpInt(n,D);
		 for (i=0; i<n; i++)
		 {
		  DumpString(f.locvars[i].varname,D);
		  DumpInt(f.locvars[i].startpc,D);
		  DumpInt(f.locvars[i].endpc,D);
		 }
		 n= (D.strip != 0) ? 0 : f.sizeupvalues;
		 DumpInt(n,D);
		 for (i=0; i<n; i++) DumpString(f.upvalues[i].name,D);
		}

		private static void DumpFunction(Proto f, DumpState D)
		{
		 DumpInt(f.linedefined,D);
		 DumpInt(f.lastlinedefined,D);
		 DumpChar(f.numparams,D);
		 DumpChar(f.is_vararg,D);
		 DumpChar(f.maxstacksize,D);
		 DumpCode(f,D);
		 DumpConstants(f,D);
         DumpUpvalues(f,D);
		 DumpDebug(f,D);
		}

		private static void DumpHeader(DumpState D)
		{
		 CharPtr h = new char[LUAC_HEADERSIZE]; //FIXME:???CharPtr //FIXME:changed, lu_byte->char
		 luaU_header(h);
		 DumpBlock(h,(uint)LUAC_HEADERSIZE,D); //FIXME:changed, (uint)
		}

		/*
		** dump Lua function as precompiled chunk
		*/
		public static int luaU_dump (lua_State L, Proto f, lua_Writer w, object data, int strip)
		{
		 DumpState D = new DumpState();
		 D.L=L;
		 D.writer=w;
		 D.data=data;
		 D.strip=strip;
		 D.status=0;
		 DumpHeader(D);
		 DumpFunction(f,D);
		 return D.status;
		}
	}
}

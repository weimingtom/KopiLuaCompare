/*
** $Id: ldebug.h,v 2.14.1.1 2017/04/19 17:20:42 roberto Exp $
** Auxiliary functions from Debug Interface module
** See Copyright Notice in lua.h
*/

using System.Diagnostics;

namespace KopiLua
{
	public partial class Lua
	{
		public static int pcRel(InstructionPtr pc, Proto p)
		{
			Debug.Assert(pc.codes == p.code);
			return pc.pc - 1;
		}
		public static int getfuncline(Proto f, int pc) { return (f.lineinfo != null) ? f.lineinfo[pc] : -1; }
		public static void resethookcount(lua_State L) { L.hookcount = L.basehookcount; }

	}
}

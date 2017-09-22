/*
** $Id: ldebug.h,v 2.14 2015/05/22 17:45:56 roberto Exp $
** Auxiliary functions from Debug Interface module
** See Copyright Notice in lua.h
*/
using System.Diagnostics;
	
namespace KopiLua
{

	public partial class Lua
	{


		public static int pcRel(InstructionPtr pc, Proto p)	{Debug.Assert(pc.codes == p.code); return pc.pc - 1;} //FIXME:

		public static int getfuncline(Proto f, int pc)	{return (f.lineinfo != null) ? f.lineinfo[pc] : 0;}

		public static void resethookcount(lua_State L)	{ L.hookcount = L.basehookcount; }


//LUAI_FUNC l_noret luaG_typeerror (lua_State *L, const TValue *o,
//                                                const char *opname);
//LUAI_FUNC l_noret luaG_concaterror (lua_State *L, const TValue *p1,
//                                                  const TValue *p2);
//LUAI_FUNC l_noret luaG_opinterror (lua_State *L, const TValue *p1,
//                                                 const TValue *p2,
//                                                 const char *msg);
//LUAI_FUNC l_noret luaG_tointerror (lua_State *L, const TValue *p1,
//                                                 const TValue *p2);
//LUAI_FUNC l_noret luaG_ordererror (lua_State *L, const TValue *p1,
//                                                 const TValue *p2);
//LUAI_FUNC l_noret luaG_runerror (lua_State *L, const char *fmt, ...);
//LUAI_FUNC const char *luaG_addinfo (lua_State *L, const char *msg,
//                                                  TString *src, int line);
//LUAI_FUNC l_noret luaG_errormsg (lua_State *L);
//LUAI_FUNC void luaG_traceexec (lua_State *L);
//

	}
}

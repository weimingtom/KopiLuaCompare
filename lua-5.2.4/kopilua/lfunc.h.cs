/*
** $Id: lfunc.h,v 2.8.1.1 2013/04/12 18:48:47 roberto Exp $
** Auxiliary functions to manipulate prototypes and closures
** See Copyright Notice in lua.h
*/
namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	
	public partial class Lua
	{
		public static uint sizeCclosure(int n) {
			return (uint)(GetUnmanagedSize(typeof(CClosure)) + GetUnmanagedSize(typeof(TValue)) * (n - 1));
		}

		public static uint sizeLclosure(int n) {
			return (uint)(GetUnmanagedSize(typeof(LClosure)) + GetUnmanagedSize(typeof(TValue)) * (n - 1));
		}

		//LUAI_FUNC Proto *luaF_newproto (lua_State *L);
		//LUAI_FUNC Closure *luaF_newCclosure (lua_State *L, int nelems);
		//LUAI_FUNC Closure *luaF_newLclosure (lua_State *L, int nelems);
		//LUAI_FUNC UpVal *luaF_newupval (lua_State *L);
		//LUAI_FUNC UpVal *luaF_findupval (lua_State *L, StkId level);
		//LUAI_FUNC void luaF_close (lua_State *L, StkId level);
		//LUAI_FUNC void luaF_freeproto (lua_State *L, Proto *f);
		//LUAI_FUNC void luaF_freeupval (lua_State *L, UpVal *uv);
		//LUAI_FUNC const char *luaF_getlocalname (const Proto *func, int local_number,
		//                                         int pc);
	}
}

/*
** $Id: lfunc.h,v 2.13 2014/02/18 13:39:37 roberto Exp $
** Auxiliary functions to manipulate prototypes and closures
** See Copyright Notice in lua.h
*/
namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using lu_mem = System.UInt32;
	
	public partial class Lua
	{
		public static uint sizeCclosure(int n) {
			return (uint)(GetUnmanagedSize(typeof(CClosure)) + GetUnmanagedSize(typeof(TValue)) * (n - 1));
		}

		public static uint sizeLclosure(int n) {
			return (uint)(GetUnmanagedSize(typeof(LClosure)) + GetUnmanagedSize(typeof(TValue)) * (n - 1));
		}


		/* test whether thread is in 'twups' list */
		public static bool isintwups(lua_State L)	{ return (L.twups != L); }


		/*
		** Upvalues for Lua closures
		*/
		public class UpVal : GCObject {
		  public TValue v = null;  /* points to stack or to its own value */
		  public lu_mem refcount;  /* reference counter */
		  public new UpVal_u u = new UpVal_u();
		};
	  	public class UpVal_u {  /* (when open) */
		  public UpVal_u_open open = new UpVal_u_open();
		  public TValue value_ = new TValue();  /* the value (when closed) */
	  	};
		public class UpVal_u_open {
		  public UpVal next = null;  /* linked list */
		  public int touched;  /* mark to avoid cycles with dead threads */
		};
		
		public static bool upisopen(UpVal up)	{ return (up.v != up.u.value_); } 



		//LUAI_FUNC Proto *luaF_newproto (lua_State *L);
		//LUAI_FUNC Closure *luaF_newCclosure (lua_State *L, int nelems);
		//LUAI_FUNC Closure *luaF_newLclosure (lua_State *L, int nelems);
		//LUAI_FUNC void luaF_initupvals (lua_State *L, LClosure *cl);
		//LUAI_FUNC UpVal *luaF_findupval (lua_State *L, StkId level);
		//LUAI_FUNC void luaF_close (lua_State *L, StkId level);
		//LUAI_FUNC void luaF_freeproto (lua_State *L, Proto *f);
		//LUAI_FUNC const char *luaF_getlocalname (const Proto *func, int local_number,
		//                                         int pc);
	}
}

/*
** $Id: lfunc.h,v 2.15 2015/01/13 15:49:11 roberto Exp $
** Auxiliary functions to manipulate prototypes and closures
** See Copyright Notice in lua.h
*/

namespace KopiLua
{
	public partial class Lua
	{


		public static int sizeCclosure(int n) { return (GetUnmanagedSize(typeof(CClosure)) +
		                                                GetUnmanagedSize(typeof(TValue)) * ((n)-1)); }

		public static int sizeLclosure(int n) { return (GetUnmanagedSize(typeof(LClosure)) +
		                                                GetUnmanagedSize(typeof(TValue)) * ((n)-1)); }


		/* test whether thread is in 'twups' list */
		public static bool isintwups(lua_State L) {return (L.twups != L);}


		/*
		** maximum number of upvalues in a closure (both C and Lua). (Value
		** must fit in a VM register.)
		*/
		public const int MAXUPVAL = 255;


		/*
		** Upvalues for Lua closures
		*/
		public struct UpVal {
		  	public TValue v;  /* points to stack or to its own value */
		  	public lu_mem refcount;  /* reference counter */
		  	public class u_cls {
			    public class open_cls {  /* (when open) */
			    	public UpVal next;  /* linked list */
			      	public int touched;  /* mark to avoid cycles with dead threads */
			    };
			    public open_cls open;
		  		public TValue value;  /* the value (when closed) */
			};
		  	public u_cls u;
		};

		public static bool upisopen(UpVal up) { return (up.v != up.u.value); }


//LUAI_FUNC Proto *luaF_newproto (lua_State *L);
//LUAI_FUNC CClosure *luaF_newCclosure (lua_State *L, int nelems);
//LUAI_FUNC LClosure *luaF_newLclosure (lua_State *L, int nelems);
//LUAI_FUNC void luaF_initupvals (lua_State *L, LClosure *cl);
//LUAI_FUNC UpVal *luaF_findupval (lua_State *L, StkId level);
//LUAI_FUNC void luaF_close (lua_State *L, StkId level);
//LUAI_FUNC void luaF_freeproto (lua_State *L, Proto *f);
//LUAI_FUNC const char *luaF_getlocalname (const Proto *func, int local_number,
//                                         int pc);

	}
}
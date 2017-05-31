namespace KopiLua
{
	using lua_Number = System.Double;
	using lua_Integer = System.Int32;
	
	public partial class Lua
	{



		/* extra error code for `luaL_load' */
		public const int LUA_ERRFILE     = (LUA_ERRERR+1);


		public class luaL_Reg {
		  public luaL_Reg(CharPtr name, lua_CFunction func) {
			  this.name = name;
			  this.func = func;
		  }

		  public CharPtr name;
		  public lua_CFunction func;
		};


		/*
		** ===============================================================
		** some useful macros
		** ===============================================================
		*/

		public static void luaL_argcheck(lua_State L, bool cond, int numarg, string extramsg) {
			if (!cond)
				luaL_argerror(L, numarg, extramsg);
		}
		public static CharPtr luaL_checkstring(lua_State L, int n) { return luaL_checklstring(L, n); }
		public static CharPtr luaL_optstring(lua_State L, int n, CharPtr d) { uint len; return luaL_optlstring(L, n, d, out len); }
		public static int luaL_checkint(lua_State L, int n)	{return (int)luaL_checkinteger(L, n);}
		public static int luaL_optint(lua_State L, int n, lua_Integer d)	{return (int)luaL_optinteger(L, n, d);}
		public static long luaL_checklong(lua_State L, int n)	{return luaL_checkinteger(L, n);}
		public static long luaL_optlong(lua_State L, int n, lua_Integer d)	{return luaL_optinteger(L, n, d);}

		public static CharPtr luaL_typename(lua_State L, int i)	{return lua_typename(L, lua_type(L,i));}

		//#define luaL_dofile(L, fn) \
		//    (luaL_loadfile(L, fn) || lua_pcall(L, 0, LUA_MULTRET, 0))

		//#define luaL_dostring(L, s) \
		//    (luaL_loadstring(L, s) || lua_pcall(L, 0, LUA_MULTRET, 0))

		public static void luaL_getmetatable(lua_State L, CharPtr n) { lua_getfield(L, LUA_REGISTRYINDEX, n); }

		public delegate lua_Number luaL_opt_delegate (lua_State L, int narg);		
		public static lua_Number luaL_opt(lua_State L, luaL_opt_delegate f, int n, lua_Number d) {
			return lua_isnoneornil(L, (n != 0) ? d : f(L, n)) ? 1 : 0;}

		public delegate lua_Integer luaL_opt_delegate_integer(lua_State L, int narg);
		public static lua_Integer luaL_opt_integer(lua_State L, luaL_opt_delegate_integer f, int n, lua_Number d) {
			return (lua_Integer)(lua_isnoneornil(L, n) ? d : f(L, (n)));
		}

		/*
		** {======================================================
		** Generic Buffer manipulation
		** =======================================================
		*/



		public class luaL_Buffer {
		  public int p;			/* current position in buffer */
		  public int lvl;  /* number of strings in the stack (level) */
		  public lua_State L;
		  public CharPtr buffer = new char[LUAL_BUFFERSIZE];
		};

		public static void luaL_addchar(luaL_Buffer B, char c) {
			if (B.p >= LUAL_BUFFERSIZE)
				luaL_prepbuffer(B);
			B.buffer[B.p++] = c;
		}

		///* compatibility only */
		public static void luaL_putchar(luaL_Buffer B, char c)	{luaL_addchar(B,c);}

		public static void luaL_addsize(luaL_Buffer B, int n)	{B.p += n;}

		/* }====================================================== */


		/* compatibility with ref system */

		/* pre-defined references */
		public const int LUA_NOREF       = (-2);
		public const int LUA_REFNIL      = (-1);

		//#define lua_ref(L,lock) ((lock) ? luaL_ref(L, LUA_REGISTRYINDEX) : \
		//      (lua_pushstring(L, "unlocked references are obsolete"), lua_error(L), 0))

		//#define lua_unref(L,ref)        luaL_unref(L, LUA_REGISTRYINDEX, (ref))

		//#define lua_getref(L,ref)       lua_rawgeti(L, LUA_REGISTRYINDEX, (ref))


		//#define luaL_reg	luaL_Reg


		/* This file uses only the official API of Lua.
		** Any function declared here could be written as an application function.
		*/

		//#define lauxlib_c
		//#define LUA_LIB
	}
}


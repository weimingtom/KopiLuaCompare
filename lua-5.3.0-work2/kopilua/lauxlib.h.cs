/*
** $Id: lauxlib.h,v 1.123 2014/01/05 14:04:46 roberto Exp $
** Auxiliary functions for building Lua libraries
** See Copyright Notice in lua.h
*/


#define LUA_COMPAT_MOD

using System.IO;

namespace KopiLua
{
	using lua_Number = System.Double;
	using lua_Integer = System.Int32;
	using lua_Unsigned = System.UInt32;
	
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

		private const int LUAL_NUMSIZES	= (sizeof(lua_Integer)*16 + sizeof(lua_Number));

		//LUALIB_API void (luaL_checkversion_) (lua_State *L, int ver, size_t sz);
		public static void luaL_checkversion(lua_State L) 
			{ luaL_checkversion_(L, LUA_VERSION_NUM, LUAL_NUMSIZES); }

		/* pre-defined references */
		public const int LUA_NOREF = (-2);
		public const int LUA_REFNIL = (-1);
		
		public static int luaL_loadfile(lua_State L, CharPtr f) { return luaL_loadfilex(L,f,null);}

		/*
		** ===============================================================
		** some useful macros
		** ===============================================================
		*/


		public static void luaL_newlibtable(lua_State L, luaL_Reg[] l) {
			lua_createtable(L, 0, l.Length-1); } //FIXME: changed, sizeof(l)/sizeof((l)[0]) - 1)

		public static void luaL_newlib(lua_State L, luaL_Reg[] l) 
			{ luaL_checkversion(L); luaL_newlibtable(L,l); luaL_setfuncs(L,l,0); }

		public static void luaL_argcheck(lua_State L, bool cond, int arg, string extramsg) {
			if (!cond)
				luaL_argerror(L, arg, extramsg);
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
		
		//FIXME:added
		public delegate lua_Unsigned luaL_opt_delegate_unsigned(lua_State L, int narg);
		public static lua_Unsigned luaL_opt_unsigned(lua_State L, luaL_opt_delegate_unsigned f, int n, lua_Unsigned u) {
			return (lua_Unsigned)(lua_isnoneornil(L, n) ? u : f(L, (n)));
		}
		
		public static int luaL_loadbuffer(lua_State L, CharPtr s, uint sz, CharPtr n)	{ return luaL_loadbufferx(L,s,sz,n,null); }
		
		/*
		** {======================================================
		** Generic Buffer manipulation
		** =======================================================
		*/

		public class luaL_Buffer {
		  public CharPtr b;  /* buffer address */
		  public uint size;  /* buffer size */
		  public uint n;  /* number of characters in buffer */
		  public lua_State L;
		  public CharPtr initb = new char[LUAL_BUFFERSIZE];  /* initial buffer */
		};

		public static void luaL_addchar(luaL_Buffer B, char c) {
			if (!(B.n < B.size)) luaL_prepbuffsize(B, 1);//FIXME: changed, ||->if
   			B.b[B.n++] = c;
		}


		public static void luaL_addsize(luaL_Buffer B, uint s)	{B.n += s;}

		public static CharPtr luaL_prepbuffer(luaL_Buffer B) { return luaL_prepbuffsize(B, LUAL_BUFFERSIZE); }
		/* }====================================================== */



		/*
		** {======================================================
		** File handles for IO library
		** =======================================================
		*/

		/*
		** A file handle is a userdata with metatable 'LUA_FILEHANDLE' and
		** initial structure 'luaL_Stream' (it may contain other fields
		** after that initial structure).
		*/

		public const string LUA_FILEHANDLE = "FILE*";


		public class luaL_Stream {
		  public StreamProxy f;  /* stream (NULL for incompletely created streams) */
		  public lua_CFunction closef;  /* to close stream (NULL for closed streams) */
		};

		/* }====================================================== */



		/* compatibility with old module system */
        //#if defined(LUA_COMPAT_MODULE)

		public static void luaL_register(lua_State L, CharPtr n, luaL_Reg[] l) { luaL_openlib(L,n,l,0); }

        //#endif

	}
}


/*
** LUA - Linguagem para Usuarios de Aplicacao
** Grupo de Tecnologia em Computacao Grafica
** TeCGraf - PUC-Rio
** $Id: lua.h,v 3.16 1995/01/27 17:19:06 celes Exp $
*/

namespace KopiLua
{
	using lua_Object = System.UInt32;
	
	public partial class Lua
	{
		//#ifndef lua_h
		//#define lua_h

		/* Private Part */
 
		public enum lua_Type
		{
		 LUA_T_NIL	= -1,
		 LUA_T_NUMBER	= -2,
		 LUA_T_STRING	= -3,
		 LUA_T_ARRAY	= -4,
		 LUA_T_FUNCTION	= -5,
		 LUA_T_CFUNCTION= -6,
		 LUA_T_USERDATA = 0
		}
 

		/* Public Part */

		public const lua_Object LUA_NOOBJECT = 0;

		public delegate void lua_CFunction();
		//typedef unsigned int lua_Object;

//		lua_Object     lua_setfallback		(char *name, lua_CFunction fallback);

//		void           lua_error		(char *s);
//		int            lua_dofile 		(char *filename);
//		int            lua_dostring 		(char *string);
//		int            lua_callfunction		(lua_Object function);
//		int	       lua_call			(char *funcname);

//		void	       lua_beginblock		(void);
//		void	       lua_endblock		(void);

//		lua_Object     lua_getparam 		(int number);
//		#define	       lua_getresult(_)		lua_getparam(_)

//		float          lua_getnumber 		(lua_Object object);
//		char          *lua_getstring 		(lua_Object object);
//		lua_CFunction  lua_getcfunction 	(lua_Object object);
//		void          *lua_getuserdata  	(lua_Object object);

//		void 	       lua_pushnil 		(void);
//		void           lua_pushnumber 		(float n);
//		void           lua_pushstring 		(char *s);
//		void           lua_pushliteral 		(char *s);
//		void           lua_pushcfunction	(lua_CFunction fn);
//		void           lua_pushusertag     	(void *u, int tag);
//		void           lua_pushobject       	(lua_Object object);

//		lua_Object     lua_getglobal 		(char *name);
//		void           lua_storeglobal		(char *name);

//		void           lua_storesubscript	(void);
//		lua_Object     lua_getsubscript         (void);

//		int            lua_type 		(lua_Object object);

//		int	       lua_lock			(void);
//		lua_Object     lua_getlocked		(int ref);
//		void	       lua_pushlocked		(int ref);
//		void	       lua_unlock		(int ref);

//		lua_Object     lua_createtable		(void);


		/* some useful macros */

		public static int lua_lockobject(lua_Object o) { lua_pushobject(o); return lua_lock(); }

		public static void lua_register(CharPtr n, lua_CFunction f, string name) { lua_pushcfunction(f, name); lua_storeglobal(n); }

		public static void lua_pushuserdata(object u) { lua_pushusertag(u, (int)lua_Type.LUA_T_USERDATA); }

		public static int lua_isnil (lua_Object obj) { return (lua_type(obj) == (int)lua_Type.LUA_T_NIL) ? 1 : 0; }
		public static int lua_isnumber (lua_Object obj) { return (lua_type(obj) == (int)lua_Type.LUA_T_NUMBER) ? 1 : 0; }
		public static int lua_isstring (lua_Object obj) { return (lua_type(obj) == (int)lua_Type.LUA_T_STRING) ? 1 : 0; }
		public static int lua_istable (lua_Object obj) { return (lua_type(obj) == (int)lua_Type.LUA_T_ARRAY) ? 1 : 0; }
		public static int lua_isfunction (lua_Object obj) { return (lua_type(obj) == (int)lua_Type.LUA_T_FUNCTION) ? 1 : 0; }
		public static int lua_iscfunction (lua_Object obj) { return (lua_type(obj) == (int)lua_Type.LUA_T_CFUNCTION) ? 1 : 0; }
		public static int lua_isuserdata (lua_Object obj) { return (lua_type(obj) >= (int)lua_Type.LUA_T_USERDATA) ? 1 : 0; }
		

		/* for lua 1.1 compatibility. Avoid using these macros */

		public static lua_Object lua_getindexed(lua_Object o, float n) {lua_pushobject(o); lua_pushnumber(n); return lua_getsubscript(); }
		public static lua_Object lua_getfield(lua_Object o, CharPtr f) {lua_pushobject(o); lua_pushliteral(f); return lua_getsubscript(); }

		public static CharPtr lua_copystring(lua_Object o) { return (strdup(lua_getstring(o))); }

//		#endif
	}
}

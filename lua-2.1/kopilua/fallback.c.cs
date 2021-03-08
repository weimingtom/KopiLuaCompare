/*
** fallback.c
** TecCGraf - PUC-Rio
*/

namespace KopiLua
{
	using lua_Object = System.UInt32;
	using Word = System.UInt16;
	using real = System.Single;	
	
	public partial class Lua
	{
		//char *rcs_fallback="$Id: fallback.c,v 1.11 1995/02/06 19:34:03 roberto Exp $";
		
		//#include <stdio.h>
		//#include <string.h>
		 
		//#include "mem.h"
		//#include "fallback.h"
		//#include "opcode.h"
		//#include "inout.h"
		//#include "lua.h"
		
		
		//static void errorFB (void);
		//static void indexFB (void);
		//static void gettableFB (void);
		//static void arithFB (void);
		//static void concatFB (void);
		//static void orderFB (void);
		//static void GDFB (void);
		//static void funcFB (void);
		
		
		/*
		** Warning: This list must be in the same order as the #define's
		*/
		public static FB[] luaI_fallBacks = {
			new FB("error", new Object_(lua_Type.LUA_T_CFUNCTION, errorFB)),
			new FB("index", new Object_(lua_Type.LUA_T_CFUNCTION, indexFB)),
			new FB("gettable", new Object_(lua_Type.LUA_T_CFUNCTION, gettableFB)),
			new FB("arith", new Object_(lua_Type.LUA_T_CFUNCTION, arithFB)),
			new FB("order", new Object_(lua_Type.LUA_T_CFUNCTION, orderFB)),
			new FB("concat", new Object_(lua_Type.LUA_T_CFUNCTION, concatFB)),
			new FB("settable", new Object_(lua_Type.LUA_T_CFUNCTION, gettableFB)),
			new FB("gc", new Object_(lua_Type.LUA_T_CFUNCTION, GDFB)),
			new FB("function", new Object_(lua_Type.LUA_T_CFUNCTION, funcFB)),
		};
		
		private static int N_FB = luaI_fallBacks.Length;
		
		public static void luaI_setfallback ()
		{
			int i;
		  	CharPtr name = lua_getstring(lua_getparam(1));
		  	lua_Object func = lua_getparam(2);
		  	if (name == null || !(0!=lua_isfunction(func) || 0!=lua_iscfunction(func)))
		  	{
		    	lua_pushnil();
		    	return;
		  	}
		  	for (i = 0; i < N_FB; i++)
		  	{
		    	if (strcmp(luaI_fallBacks[i].kind, name) == 0)
		    	{
		      		luaI_pushobject(luaI_fallBacks[i].function);
		      		luaI_fallBacks[i].function.set(luaI_Address(func));
		      		return;
		    	}
		  	}
		  	/* name not found */
		  	lua_pushnil();
		}
		
		
		private static void errorFB ()
		{
			lua_Object o = lua_getparam(1);
		  	if (0!=lua_isstring(o))
		    	fprintf(stderr, "lua: %s\n", lua_getstring(o));
		  	else
		    	fprintf(stderr, "lua: unknown error\n");
		}
		 
		
		private static void indexFB ()
		{
		  	lua_pushnil();
		}
		 
		
		private static void gettableFB ()
		{
			lua_reportbug("indexed expression not a table");
		}
		 
		
		private static void arithFB ()
		{
		  	lua_reportbug("unexpected type at conversion to number");
		}
		
		static void concatFB ()
		{
		  	lua_reportbug("unexpected type at conversion to string");
		}
		
		
		private static void orderFB ()
		{
		  	lua_reportbug("unexpected type at comparison");
		}
		
		private static void GDFB () { }
		
		private static void funcFB ()
		{
		  	lua_reportbug("call expression not a function");
		}
		
		
		/*
		** Lock routines
		*/
		
		private static Object_[] lockArray = null; //ObjectRef
		private static Word lockSize = 0;
		
		public static int luaI_lock (Object_ @object)
		{
		  	Word i;
		  	Word oldSize;
		  	if (tag(@object) == lua_Type.LUA_T_NIL)
		  		return -1;
		 	for (i = 0; i < lockSize; i++)
		 		if (tag(lockArray[i]) == lua_Type.LUA_T_NIL)
		   		{
		 			lockArray[i].set(@object);
		      		return i;
		    	}
		  	/* no more empty spaces */
		  	oldSize = lockSize;
		  	if (lockArray == null)
		  	{
		  		lockSize = 10;
		  		lockArray = newvector_Object(lockSize);
		  	}
		  	else
		  	{
		  		lockSize = (Word)(3 * oldSize / 2 + 5);
		    	lockArray = growvector_Object(lockArray, lockSize);
		  	}
		  	for (i=oldSize; i<lockSize; i++)
		  		tag(lockArray[i], lua_Type.LUA_T_NIL);
		  	lockArray[oldSize].set(@object);
		  	return oldSize;
		}
		
		
		public static void lua_unlock (int @ref)
		{
			tag(lockArray[@ref], lua_Type.LUA_T_NIL);
		}
		
		
		public static Object_ luaI_getlocked (int @ref)
		{
			return lockArray[@ref];
		}
		
		public delegate void luaI_travlock_fn(Object_ obj);
		public static void luaI_travlock (luaI_travlock_fn fn)
		{
			Word i;
		  	for (i = 0; i < lockSize; i++)
		  		fn(lockArray[i]);
		}
	}
}


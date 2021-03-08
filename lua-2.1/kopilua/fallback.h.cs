/*
** $Id: fallback.h,v 1.7 1994/11/21 18:22:58 roberto Stab $
*/

namespace KopiLua
{
	public partial class Lua
	{
		//#ifndef fallback_h
		//#define fallback_h
		
		//#include "opcode.h"
		
		public class FB {
			public CharPtr kind;
		  	public Object_ function;
		  
		  	public FB(CharPtr kind, Object_ function)
		  	{
		  		this.kind = new CharPtr(kind);
		  		this.function = function;
		  	}
		}
		//luaI_fallBacks[];
		
		public const int FB_ERROR = 0;
		public const int FB_INDEX = 1;
		public const int FB_GETTABLE = 2;
		public const int FB_ARITH = 3;
		public const int FB_ORDER = 4;
		public const int FB_CONCAT = 5;
		public const int FB_SETTABLE = 6;
		public const int FB_GC = 7;
		public const int FB_FUNCTION = 8;
		
		//void luaI_setfallback (void);
		//int luaI_lock (Object *object);
		//Object *luaI_getlocked (int ref);
		//void luaI_travlock (void (*fn)(Object *));
		
		//#endif
	}
}

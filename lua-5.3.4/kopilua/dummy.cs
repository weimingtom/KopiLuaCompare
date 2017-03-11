using System;

namespace KopiLua
{
	public partial class Lua
	{
		public Lua()
		{
			
		}
		
		public static void api_check(lua_State L, bool b, string str)
		{
			
		}
		
		public static int luaK_codeABx(FuncState fs, OpCode o, int A, int sBx)
		{
			return 0;
		}
		
		public static void luaK_setreturns(FuncState fs, expdesc e, int arg3)
		{
			
		}
		
		public static int luaK_jump(FuncState fs)
		{
			return 0;
		}
		
		public static void luaK_patchlist(FuncState fs, int arg2, int t)
		{
			
		}
		
		public static int LUA_MULTRET = 0;
		public static int MAXARG_sBx = 0;
	}
	
	public class lua_State 
	{
		public int top;

		public class ci_cls {
			public int top;
			public int func;
		}
		
		public ci_cls ci;
	}
	
	public class Test
	{
		static void Main(string[] args)
		{
			
		}
	}
	
	public class InstructionPtr
	{
		public InstructionPtr(int arg1, int arg2){}
	}
	
	public class FuncState
	{
		public class f_cls {public int code;};
		public f_cls f;
	}
	
	public class expdesc
	{
		public class u_cls {public int info;};
		public u_cls u;
	}
	
	public class OpCode
	{
		
	}
}

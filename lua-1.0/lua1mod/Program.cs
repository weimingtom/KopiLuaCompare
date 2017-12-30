/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 2017-10-17
 * Time: 21:02
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
//#define TEST_LUA 
 
using System; 
using KopiLua;

namespace lua1mod
{
	class Program
	{
		public static void Main(string[] args)
		{
			//Console.WriteLine("Hello World!");
			
			// TODO: Implement Functionality Here
			
			//Console.WriteLine("atof() = " + KopiLua.Lua.atof("12.34"));
			
#if !TEST_LUA			
			Lua.CharPtr[] argv;
			argv = new Lua.CharPtr[args.Length + 1];
			argv[0] = new Lua.CharPtr("lua.exe");
			for (int i = 0; i < args.Length; ++i)
			{
				argv[i+1] = args[i];
			}
			Lua.main(argv.Length, argv);

#else
			Lua.CharPtr[] argv;
			argv = new Lua.CharPtr[] {"lua.exe", "print.lua"}; //ok
//			argv = new Lua.CharPtr[] {"lua.exe", "array.lua"}; //ok
//			argv = new Lua.CharPtr[] {"lua.exe", "globals.lua"}; //ok
//			argv = new Lua.CharPtr[] {"lua.exe", "save.lua"}; //ok
//			argv = new Lua.CharPtr[] {"lua.exe", "sort.lua", "main"}; //ok
//			argv = new Lua.CharPtr[] {"lua.exe", "test.lua", "retorno_multiplo"}; //ok
//			argv = new Lua.CharPtr[] {"lua.exe", "type.lua"}; //ok
			Lua.main(argv.Length, argv);
			
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
#endif
		}
	}
}

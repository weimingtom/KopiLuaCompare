/*
** mem.c
** TecCGraf - PUC-Rio
*/

namespace KopiLua
{
	public partial class Lua
	{
		//char *rcs_mem = "$Id: mem.c,v 1.5 1995/02/06 19:34:03 roberto Exp $";
		
		//#include <stdlib.h>
		//#include <string.h>
		
		//#include "mem.h"
		//#include "lua.h"
		
		public static void luaI_free_NodeRef (NodeRef block)
		{
			block.set(null);//*((int *)block) = -1;  /* to catch errors */
		  	//free(block);
		}
		public static void luaI_free_Hash(ref Hash block)
		{
		  	block = null;//*((int *)block) = -1;  /* to catch errors */
		  	//free(block);
		}
		public static void luaI_free_FuncStackNode(ref FuncStackNode block)
		{
			block = null;//*((int *)block) = -1;  /* to catch errors */
		  	//free(block);
		}
		public static void luaI_free_StringNode(ref StringNode block)
		{
			block = null;//*((int *)block) = -1;  /* to catch errors */
		  	//free(block);
		}
		public static void luaI_free_CharPtr(ref CharPtr block)
		{
			block = null;//*((int *)block) = -1;  /* to catch errors */
		  	//free(block);
		}
		public static void luaI_free_BytePtr(ref BytePtr block)
		{
			block = null;//*((int *)block) = -1;  /* to catch errors */
		  	//free(block);
		}
		
		public static CharPtr luaI_malloc (ulong size)
		{
			CharPtr block = malloc(size);
		  	if (block == null)
		    	lua_error("not enough memory");
		  	return block;
		}
		public static TreeNode luaI_malloc_TreeNode (ulong size) {
			return new TreeNode(size);
		}
		public static StringNode luaI_malloc_StringNode(ulong size) {
			return new StringNode(size);
		}
		
		public static CharPtr luaI_realloc (CharPtr oldblock, ulong size)
		{
			CharPtr temp = new CharPtr(new char[size]); //(char *)realloc(s, n+1);
   			for (int i = 0; i < oldblock.chars.Length; ++i)
   			{
   				if (i < temp.chars.Length)
   				{
   					temp.chars[i] = oldblock.chars[i];
   				}
   			}
   			return temp;
   			
		  //void *block = realloc(oldblock, (size_t)size);
		  //if (block == NULL)
		  //  lua_error("not enough memory");
		  //return block;
		}
		
		
		public static CharPtr luaI_strdup (CharPtr str)
		{
		  	CharPtr newstr = luaI_malloc(strlen(str)+1);
		  	strcpy(newstr, str);
		  	return newstr;
		}
	}
}
/*
** tree.c
** TecCGraf - PUC-Rio
*/

namespace KopiLua
{	
	using lua_Object = System.UInt32;
	using Word = System.UInt16;
	using real = System.Single;	
	using Bool = System.Int32;
	using Long = System.Int32;	
	
	public partial class Lua
	{
		//char *rcs_tree="$Id: tree.c,v 1.13 1995/01/12 14:19:04 roberto Exp $";
		
		
		//#include <string.h>
		
		//#include "mem.h"
		//#include "lua.h"
		//#include "tree.h"
		//#include "table.h"
		
		
		//private static int lua_strcmp(CharPtr a, CharPtr b)	{ return (a[0]<b[0]?(-1):(a[0]>b[0]?(1):strcmp(a,b))); }
		
		
		public class StringNode {
			public StringNode next;
			public TaggedString ts = new TaggedString();
			public StringNode(ulong size)
			{
				this.ts.str = new CharPtr(new char[size + 1]);  /* \0 byte already reserved */
			}
		}
		
		static StringNode string_root = null;
		
		static TreeNode constant_root = null;
		
		/*
		** Insert a new constant/variable at the tree. 
		*/
		private static TreeNode tree_create (ref TreeNode node, CharPtr str)
		{
			if (node == null)
		 	{
				node = luaI_malloc_TreeNode(strlen(str));
				node.left = node.right = null;
				strcpy(node.ts.str, str);
				node.ts.marked = (char)0;
		  		node.ts.hash = 0;
		  		node.varindex = node.constindex = NOT_USED;
		  		return node;
		 	}
		 	else
		 	{
		  		int c = lua_strcmp(str, node.ts.str);
		  		if (c < 0) 
		  			return tree_create(ref node.left, str);
		  		else if (c > 0)
		  			return tree_create(ref node.right, str);
		  		else
		  			return node;
		 	}
		}
		
		public static TaggedString lua_createstring (CharPtr str) 
		{
		 	StringNode newString;
		  	if (str == null) return null;
		  	lua_pack();
		  	newString = luaI_malloc_StringNode(strlen(str));
		  	newString.ts.marked = (char)0;
		  	newString.ts.hash = 0;
		  	strcpy(newString.ts.str, str);
		  	newString.next = string_root;
		  	string_root = newString;
		  	return newString.ts;
		}
		
		
		public static TreeNode lua_constcreate (CharPtr str) 
		{
			return tree_create(ref constant_root, str);
		}
		
		
		/*
		** Garbage collection function.
		** This function traverse the string list freeing unindexed strings
		*/
		public static Long lua_strcollector ()
		{
			StringNode curr = string_root, prev = null;
		  	Long counter = 0;
		  	while (curr != null)
		  	{
		    	StringNode next = curr.next;
		    	if ((char)0==curr.ts.marked)
		    	{
		      		if (prev == null) string_root = next;
		      		else prev.next = next;
		      		luaI_free_StringNode(ref curr);
		      		++counter;
		    	}
		    	else
		    	{
		    		curr.ts.marked = (char)0;
		      		prev = curr;
		    	}
		    	curr = next;
		  	}
		  	return counter;
		}
		
		/*
		** Return next variable.
		*/
		private static TreeNode tree_next (TreeNode node, CharPtr str)
		{
			if (node == null) return null;
		 	else if (str == null) return node;
		 	else
		 	{
		  		int c = lua_strcmp(str, node.ts.str);
		  		if (c == 0)
		   			return node.left != null ? node.left : node.right;
		  		else if (c < 0)
		  		{
		   			TreeNode result = tree_next(node.left, str);
		   			return result != null ? result : node.right;
		  		}
		  		else
		   			return tree_next(node.right, str);
		 	}
		}
		
		public static TreeNode lua_varnext (CharPtr n)
		{
			TreeNode result;
		  	CharPtr name = n;
		  	while (true)
		  	{ /* repeat until a valid (non nil) variable */
		    	result = tree_next(constant_root, name);
		    	if (result == null) return null;
		    	if (result.varindex != NOT_USED &&
		        	s_tag(result.varindex) != lua_Type.LUA_T_NIL)
		      		return result;
		    	name = result.ts.str;
		  	}
		}
	}
}


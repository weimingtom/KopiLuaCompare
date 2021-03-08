/*
** tree.h
** TecCGraf - PUC-Rio
** $Id: tree.h,v 1.9 1995/01/12 14:19:04 roberto Exp $
*/

namespace KopiLua
{	
	public partial class Lua
	{
		//#ifndef tree_h
		//#define tree_h
		
		//#include "types.h"
		
		public const int NOT_USED = 0xFFFE;
		
		
		public class TaggedString
		{
			public ulong hash;  /* 0 if not initialized */
		  	public char marked;   /* for garbage collection */
		  	public CharPtr str = new CharPtr(new char[1]);   /* \0 byte already reserved */
		}
		 
		public class TreeNode
		{
		 	public TreeNode right;
		 	public TreeNode left;
		 	public ushort varindex;  /* != NOT_USED  if this is a symbol */
		 	public ushort constindex;  /* != NOT_USED  if this is a constant */
		 	public TaggedString ts = new TaggedString();
		 	
		 	public TreeNode(ulong size)
		 	{
		 		ts.str = new CharPtr(new char[size + 1]); /* \0 byte already reserved */
		 	}
		}
		
		
		//TaggedString *lua_createstring (char *str);
		//TreeNode *lua_constcreate  (char *str);
		//Long lua_strcollector (void);
		//TreeNode *lua_varnext      (char *n);
		
		//#endif
	}
}

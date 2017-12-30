/*
** hash.h
** hash manager for lua
** Luiz Henrique de Figueiredo - 17 Aug 90
** Modified by Waldemar Celes Filho
** 26 Apr 93
*/
using System;
using System.Collections.Generic;

namespace KopiLua
{
	public partial class Lua
	{	
		public class NodeRef
		{
			public int index = -1;
			public NodeRef(int index)
			{
				this.index = index;
			}
			
			public static NodeRef assign(Node node)
			{
				if (node == null)
				{
					return null;
				}
				else
				{
					return new NodeRef(node.index);
				}
			}
			
			public static NodeRef assign(NodeRef node)
			{
				if (node == null)
				{
					return null;
				}
				else
				{
					return new NodeRef(node.index);
				}
			}
			
			public Node get()
			{
				if (index < 0)
				{
					return null;
				}
				else
				{
					return Node.global_nodes[index];
				}
			}
		}
		public class Node
		{
			public Object_ @ref
			{
				get
				{
					return _ref;
				}
				set
				{
					_ref = value;
				}
			}
			public Object_ val
			{
				get
				{
					return _val;
				}
				set
				{
					_val = value;
				}				
			}
			
			public NodeRef next
			{
				get
				{
					return _next;
				}
				set
				{
//					if (value != null && value.index == 0)
//					{
//						Console.WriteLine("====================");
//					}
					_next = value;
				}
			}
			
			private Object_ _ref = new Object_();
		 	private Object_ _val = new Object_();
		 	private NodeRef _next;
		 	
		 	public Node()
		 	{
		 		this.index = Node.global_nodes.Count;
//		 		Console.WriteLine("============ Node " + index);
		 		Node.global_nodes.Add(this);
		 	}
		 	public int index = -1;
		 	public static List<Node> global_nodes = new List<Node>();
		}
		
		public class Hash
		{
		 	public char mark;
		 	public uint nhash;
		 	public Node[] list;
		}
		
		//#define markarray(t) ((t)->mark)
		public static char markarray(Hash t) { return t.mark; }
		public static void markarray(Hash t, char c) { t.mark = c; }
		
//		Hash 	*lua_hashcreate (unsigned int nhash);
//		void 	 lua_hashdelete (Hash *h);
//		Object 	*lua_hashdefine (Hash *t, Object *ref);
//		void 	 lua_hashmark   (Hash *h);
//		
//		void     lua_next (void);
	}
}

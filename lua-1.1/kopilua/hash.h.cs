/*
** hash.h
** hash manager for lua
** Luiz Henrique de Figueiredo - 17 Aug 90
** $Id: hash.h,v 2.1 1994/04/20 22:07:57 celes Exp $
*/
using System;
using System.Collections.Generic;

namespace KopiLua
{
	public partial class Lua
	{	
		//#ifndef hash_h
		//#define hash_h

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


		//Hash    *lua_createarray (int nhash);
		//void     lua_hashmark (Hash *h);
		//void     lua_hashcollector (void);
		//Object 	*lua_hashdefine (Hash *t, Object *ref);
		//void     lua_next (void);

		//#endif
	}
}


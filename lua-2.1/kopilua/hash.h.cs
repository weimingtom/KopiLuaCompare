/*
** hash.h
** hash manager for lua
** Luiz Henrique de Figueiredo - 17 Aug 90
** $Id: hash.h,v 2.8 1995/01/12 14:19:04 roberto Exp $
*/
using System;
using System.Collections.Generic;

namespace KopiLua
{
	using lua_Object = System.UInt32;
	using Word = System.UInt16;
	using real = System.Single;	
	
	public partial class Lua
	{	
		//#ifndef hash_h
		//#define hash_h

		//#include "types.h"
		/*
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
			
			public Node get(int idx)
			{
				if (index + idx < 0)
				{
					return null;
				}
				else
				{
					return Node.global_nodes[index + idx];
				}
			}
		}
		*/
		public class NodeRef
		{
			public int index = 0;
			public Node[] nodes = null;
			public NodeRef(Node[] nodes)
			{
				this.nodes = nodes;
				this.index = 0;
			}
			public void set(Node v)
			{
				this.nodes[index] = v;
			}
			public Node get()
			{
				if (index < 0)
				{
					return null;
				}
				else
				{
					return this.nodes[index];
				}
			}
			public Node get(int idx)
			{
				if (this.index + idx < 0)
				{
					return null;
				}
				else
				{
					return this.nodes[this.index + idx];
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
			
			
			private Object_ _ref = new Object_();
		 	private Object_ _val = new Object_();
		 	
		 	public Node()
		 	{
		 		this.index = Node.global_nodes.Count;
//		 		Console.WriteLine("============ Node " + index);
		 		Node.global_nodes.Add(this);
		 	}
		 	public int index = -1;
		 	public static List<Node> global_nodes = new List<Node>();
		 	
		 	public void set(Node n)
		 	{
		 		this._ref.set(n._ref);
		 		this._val.set(n._val);
		 	}
		}

		public class Hash
		{
			public Hash next;
		 	public char mark;
		 	public Word nhash;
			public Word nuse;
		 	public NodeRef node;
		}


		//Bool     lua_equalObj (Object *t1, Object *t2);
		//Hash    *lua_createarray (Word nhash);
		//void     lua_hashmark (Hash *h);
		//Long     lua_hashcollector (void);
		//Object  *lua_hashget (Hash *t, Object *ref);
		//Object 	*lua_hashdefine (Hash *t, Object *ref);
		//void     lua_next (void);

		//#endif
	}
}


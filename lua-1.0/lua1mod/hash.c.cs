/*
** hash.c
** hash manager for lua
** Luiz Henrique de Figueiredo - 17 Aug 90
** Modified by Waldemar Celes Filho
** 12 May 93
*/
using System;

namespace KopiLua
{				
	public partial class Lua
	{	
		//#define streq(s1,s2) (strcmp(s1,s2)==0)
		//#define strneq(s1,s2) (strcmp(s1,s2)!=0)
		private static bool strneq(CharPtr s1, CharPtr s2) {return strcmp(s1,s2)!=0;}
		
		//#define new(s) ((s *)malloc(sizeof(s)))
		//private static object new_(string s) { return malloc(sizeOf(s)); }
		private static Hash new_Hash() { return new Hash(); }
		private static Node new_Node() { return new Node(); }
		//#define newvector(n,s) ((s *)calloc(n,sizeof(s)))
		//private static object newvector(uint n, string s) { return calloc(n, sizeOf("Node")); }
		private static Node[] newvector_Node(uint n)
		{
			Node[] ret = new Node[n]; 
//			for (int i = 0; i < n; ++i)
//				ret[i] = new Node();
			return ret;
		}
		
		//#define nhash(t) ((t)->nhash)
		private static int nhash(Hash t) { return (int)t.nhash; }
		private static void nhash(Hash t, uint n) { t.nhash = n; }
		//#define nodelist(t) ((t)->list)
		private static Node[] nodelist(Hash t) { return t.list; }
		private static void nodelist(Hash t, Node[] nodes) { t.list = nodes; }
		//#define list(t,i) ((t)->list[i])
		private static NodeRef list(Hash t, int i) { return NodeRef.assign(t.list[i]); }
		private static void list(Hash t, int i, Node n) { t.list[i] = n; }
		//#define ref_tag(n) (tag(&(n)->ref))
		private static Type ref_tag(Node n) { return tag(n.@ref); }
		//#define ref_nvalue(n) (nvalue(&(n)->ref))
		private static float ref_nvalue(Node n) { return nvalue(n.@ref); }
		//#define ref_svalue(n) (svalue(&(n)->ref))
		private static CharPtr ref_svalue(Node n) { return svalue(n.@ref); }
		
		private static int head(Hash t, Object_ @ref)		/* hash function */
		{
			if (tag(@ref) == Type.T_NUMBER) return (((int)nvalue(@ref))%nhash(t));
			else if (tag(@ref) == Type.T_STRING)
			{
			  	int h;
				CharPtr name = svalue(@ref);
				for (h=0; name[0] != 0; name.inc())		/* interpret name as binary number */
			  	{
			   		h <<= 8;
			   		h += (byte) name[0];		/* avoid sign extension */
			   		h %= nhash(t);			/* make it a valid index */
			  	}
			  	return h;
			}
			else
			{
			  	lua_reportbug ("unexpected type to index table");
			  	return -1;
			}
		}
		
		private static NodeRef present(Hash t, Object_ @ref, int h)
		{
			NodeRef n=null, p=null;
			if (tag(@ref) == Type.T_NUMBER)
			{
				for (p=null,n=list(t,h); n!=null; p=n, n=NodeRef.assign(n.get().next))
					if (ref_tag(n.get()) == Type.T_NUMBER && nvalue(@ref) == ref_nvalue(n.get())) break;
			}
			else if (tag(@ref) == Type.T_STRING)
			{
				for (p=null,n=list(t,h); n!=null; p=n, n=NodeRef.assign(n.get().next))
				{
//					if (ref_tag(n.get()) == Type.T_STRING)
//					{
//						Console.WriteLine("=========================" + svalue(@ref).ToString() + ", " + ref_svalue(n.get()));
//					}
					if (ref_tag(n.get()) == Type.T_STRING && streq(svalue(@ref),ref_svalue(n.get())))
					{
//						Console.WriteLine("=========================");
						break;
					}
				}
			}
			if (n == null)				/* name not present */
				return null;
#if false
			if (p!=null)				/* name present but not first */
			{
			  	p.next=n.next;			/* move-to-front self-organization */
			  	n.next=list(t,h);
			  	list(t,h,n);
			}
#endif
			return n;
		}
		
		private static void freelist (NodeRef n)
		{
			 while (n!=null)
			 {
			 	NodeRef next = NodeRef.assign(n.get().next);
			  	free (n.get());
				n = next;
			 }
		}



		/*
		** Create a new hash. Return the hash pointer or NULL on error.
		*/
		public static Hash lua_hashcreate(uint nhash_)
		{
			Hash t = (Hash)new_Hash();//new_ ("Hash");
			if (t == null)
			{
				lua_error ("not enough memory");
			  	return null;
			}
			nhash(t, nhash_);
			markarray(t, (char)0);
			nodelist(t, newvector_Node(nhash_));//(Node[])newvector(nhash_, "Node"));
			if (nodelist(t) == null)
			{
				lua_error ("not enough memory");
			  	return null;
			}
			return t;
		}
		
		/*
		** Delete a hash
		*/
		public static void lua_hashdelete (Hash h)
		{
			int i;
			for (i=0; i<nhash(h); i++)
				freelist (list(h,i));
			free (nodelist(h));
			free(h);
		}
		
		/*
		** If the hash node is present, return its pointer, otherwise create a new
		** node for the given reference and also return its pointer.
		** On error, return NULL.
		*/
		public static Object_ lua_hashdefine (Hash t, Object_ @ref)
		{
			int   h;
			NodeRef n;
			h = head (t, @ref);
			if (h < 0) return null;
			
			n = present(t, @ref, h);
			if (n == null)
			{
				n = NodeRef.assign(new_Node());//(Node) new_("Node");
			  	if (n == null)
			  	{
			   		lua_error ("not enough memory");
			   		return null;
			  	}
			  	n.get().@ref.set(@ref);
			  	tag(n.get().val, Type.T_NIL);
			  	n.get().next = NodeRef.assign(list(t,h));			/* link node to head of list */
			  	list(t,h,n.get());
			 }
			 return (n.get().val);
		}
		
		/*
		** Mark a hash and check its elements 
		*/
		public static void lua_hashmark (Hash h)
		{
			int i;
		
			markarray(h, (char)1);
		
			for (i=0; i<nhash(h); i++)
			{
				NodeRef n;
				for (n = list(h,i); n != null; n = NodeRef.assign(n.get().next))
			  	{
					lua_markobject (n.get().@ref);
			   		lua_markobject (n.get().val);
			  	}
			}
		}
		
		/*
		** Internal function to manipulate arrays. 
		** Given an array object and a reference value, return the next element
		** in the hash.
		** This function pushs the element value and its reference to the stack.
		*/
		//#include "lua.h"
		private static void firstnode (Hash a, int h)
		{
			if (h < nhash(a))
			{
			  	int i;
			  	for (i=h; i<nhash(a); i++)
			  	{
			  		if (list(a,i) != null && tag(list(a,i).get().val) != Type.T_NIL)
			   		{
			  			lua_pushobject (list(a,i).get().@ref);
			  			lua_pushobject (list(a,i).get().val);
						return;
			   		}
			  	}
			 }
			 lua_pushnil();
			 lua_pushnil();
		}
		
		public static void lua_next ()
		{
			Hash a;
			Object_ o = lua_getparam (1);
			Object_ r = lua_getparam (2);
			if (o == null || r == null)
			{ lua_error ("too few arguments to function `next'"); return; }
			if (lua_getparam(3) != null)
			{ lua_error ("too many arguments to function `next'"); return; }
			if (tag(o) != Type.T_ARRAY)
			{ lua_error ("first argument of function `next' is not a table"); return; }
			a = avalue(o);
			if (tag(r) == Type.T_NIL)
			{
			  	firstnode (a, 0);
			  	return;
			}
			else
			{
			  	int h = head (a, r);
			  	if (h >= 0)
			  	{
			   		NodeRef n = list(a,h);
			   		while (n != null)
			   		{
			   			if (n.get().@ref.isEquals(r))
						{
				 			if (n.get().next == null)
				 			{
				  				firstnode (a, h+1);
				  				return;
				 			}
				 			else if (tag(n.get().next.get().val) != Type.T_NIL)
				 			{
				  				lua_pushobject (n.get().next.get().@ref);
				  				lua_pushobject (n.get().next.get().val);
				  				return;
				 			}
				 			else
				 			{
				 				NodeRef next = NodeRef.assign(n.get().next.get().next);
				 				while (next != null && tag(next.get().val) == Type.T_NIL) next = NodeRef.assign(next.get().next);
				  				if (next == null)
				  				{
				   					firstnode (a, h+1);
				   					return;
				  				}
				 	 			else
				  				{
				   					lua_pushobject (next.get().@ref);
				   					lua_pushobject (next.get().val);
				  				}
				  				return;
				 			}
						}
			   			n = NodeRef.assign(n.get().next);
			   		}
			   		if (n == null)
						lua_error ("error in function 'next': reference not found");
			   	}
			 }
		}		
	}
}

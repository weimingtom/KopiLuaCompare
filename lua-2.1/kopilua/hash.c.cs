/*
** hash.c
** hash manager for lua
*/
using System;

namespace KopiLua
{
	using lua_Object = System.UInt32;
	using Word = System.UInt16;
	using real = System.Single;	
	using Bool = System.Int32;
	using Long = System.Int32;
	using IntPoint = System.UInt32;
	
	public partial class Lua
	{	
		//char *rcs_hash="$Id: hash.c,v 2.24 1995/02/06 19:34:03 roberto Exp $";
		
		//#include <string.h>
		
		//#include "mem.h"
		//#include "opcode.h"
		//#include "hash.h"
		//#include "inout.h"
		//#include "table.h"
		//#include "lua.h"
		
		//#define streq(s1,s2)	(s1 == s2 || (*(s1) == *(s2) && strcmp(s1,s2)==0))
		private static int streq(CharPtr s1, CharPtr s2)	{ return (s1 == s2 || (s1[0] == s2[0] && strcmp(s1,s2)==0)) ? 1 : 0; }
		
		public static Word nhash(Hash t) { return t.nhash; }
		public static void nhash(Hash t, Word v) { t.nhash = v; }
		public static Word nuse(Hash t) { return t.nuse; }
		public static void nuse(Hash t, Word v) { t.nuse = v; }
		public static char markarray(Hash t) { return t.mark; }
		public static void markarray(Hash t, char v) { t.mark = v; }
		public static NodeRef nodevector(Hash t) { return t.node; }
		public static void nodevector(Hash t, NodeRef v) { t.node = v; }
		public static Node node(Hash t, int i) { return t.node.get(i); }
		public static Object_ ref_(Node n) { return n.@ref; }
		public static Object_ val_(Node n) { return n.@val; }
		
		
		private const float REHASH_LIMIT = 0.70f;	/* avoid more than this % full */
		
		
		private static Hash listhead = null;
		
		
		
		/* hash dimensions values */
		private static Word[] dimensions =
		 {3, 5, 7, 11, 23, 47, 97, 197, 397, 797, 1597, 3203, 6421,
		  12853, 25717, 51437, 65521, 0};  /* 65521 == last prime < MAX_WORD */
		
		private static Word redimension (Word nhash)
		{
			Word i;
		 	for (i=0; dimensions[i]!=0; i++)
			{
		  		if (dimensions[i] > nhash)
		   			return dimensions[i];
		 	}
		 	lua_error("table overflow");
		 	return 0;  /* to avoid warnings */
		}
		
		private static Word hashindex (Hash t, Object_ @ref)		/* hash function */
		{
			switch (tag(@ref))
		 	{
		  		case lua_Type.LUA_T_NIL:
		   			lua_reportbug ("unexpected type to index table");
		   			return (Word)((-1) & 0xffff);  /* UNREACHEABLE */
		  		case lua_Type.LUA_T_NUMBER:
		   			return (Word)(((Word)nvalue(@ref))%nhash(t));
		  		case lua_Type.LUA_T_STRING:
		  		{
		   			ulong h = tsvalue(@ref).hash;
		   			if (h == 0)
		   			{
		     			CharPtr name = svalue(@ref);
		     			while (name[0] != (char)0) {
		     				h = ((h<<5)-h)^(byte)(name[0]);
		     				name.inc();
		     			}
		     			tsvalue(@ref).hash = h;
		   			}
		   			return (Word)((Word)(h & 0xffff) % nhash(t));  /* make it a valid index */
		  		}
		  		case lua_Type.LUA_T_FUNCTION:
		   			return (Word)(((IntPoint)bvalue(@ref).GetHashCode())%nhash(t));
		  		case lua_Type.LUA_T_CFUNCTION:
		   			return (Word)(((IntPoint)fvalue(@ref).GetHashCode())%nhash(t));
		  		case lua_Type.LUA_T_ARRAY:
		   			return (Word)(((IntPoint)avalue(@ref).GetHashCode())%nhash(t));
		  		default:  /* user data */
		   			return (Word)(((IntPoint)uvalue(@ref).GetHashCode())%nhash(t));
		 	}
		}
		
		public static Bool lua_equalObj (Object_ t1, Object_ t2)
		{
			if (tag(t1) != tag(t2)) return 0;
		  	switch (tag(t1))
		  	{
		    	case lua_Type.LUA_T_NIL: return 1;
		    	case lua_Type.LUA_T_NUMBER: return nvalue(t1) == nvalue(t2) ? 1: 0;
		    	case lua_Type.LUA_T_STRING: return streq(svalue(t1), svalue(t2));
		    	case lua_Type.LUA_T_ARRAY: return avalue(t1) == avalue(t2) ? 1: 0;
		    	case lua_Type.LUA_T_FUNCTION: return bvalue(t1) == bvalue(t2) ? 1: 0;
		    	case lua_Type.LUA_T_CFUNCTION: return fvalue(t1) == fvalue(t2) ? 1: 0;
		    	default: return uvalue(t1) == uvalue(t2) ? 1: 0;
		  	}
		}
		
		private static Word present (Hash t, Object_ @ref)
		{ 
			Word h = hashindex(t, @ref);
		 	while (tag(ref_(node(t, h))) != lua_Type.LUA_T_NIL)
		 	{
		  		if (0!=lua_equalObj(@ref, ref_(node(t, h))))
		    		return h;
		  		h = (Word)((h+1) % nhash(t));
		 	}
		 	return h;
		}
		
		
		/*
		** Alloc a vector node 
		*/
		private static NodeRef hashnodecreate (Word nhash)
		{
			Word i;
			Node[] temp = new Node[nhash];
			for (int k = 0; k < temp.Length; ++k)
			{
				temp[k] = new Node();
			}
			Node[] v = temp;
			for (i=0; i<nhash; i++)
				tag(ref_(v[i]), lua_Type.LUA_T_NIL);
			return new NodeRef(v);
		}
		
		/*
		** Create a new hash. Return the hash pointer or NULL on error.
		*/
		private static Hash hashcreate (Word _nhash)
		{
			Hash t = new_Hash();
		 	_nhash = redimension((Word)((float)_nhash/REHASH_LIMIT));
		 	nodevector(t, hashnodecreate(_nhash));
		 	nhash(t, _nhash);
		 	nuse(t, 0);
		 	markarray(t, (char)0);
		 	return t;
		}
		
		/*
		** Delete a hash
		*/
		private static void hashdelete (ref Hash t)
		{
			luaI_free_NodeRef(nodevector(t));
			luaI_free_Hash(ref t);
		}
		
		
		/*
		** Mark a hash and check its elements 
		*/
		public static void lua_hashmark (Hash h)
		{
		 	if (markarray(h) == 0)
		 	{
		  		Word i;
		  		markarray(h, (char)1);
		  		for (i=0; i<nhash(h); i++)
		  		{
		   			Node n = node(h,i);
		   			if (tag(ref_(n)) != lua_Type.LUA_T_NIL)
		   			{
		    			lua_markobject(n.@ref);
		    			lua_markobject(n.val);
		   			}
		  		}
		 	} 
		}
		
		
		private static void call_fallbacks ()
		{
			Hash curr_array;
			Object_ t = new Object_();
			tag(t, lua_Type.LUA_T_ARRAY);
		  	for (curr_array = listhead; curr_array != null; curr_array = curr_array.next)
		  	{
		    	if (markarray(curr_array) != 1)
		    	{
		  			avalue(t, curr_array);
		      		luaI_gcFB(t);
		    	}
		  	}
		  	tag(t, lua_Type.LUA_T_NIL);
		  	luaI_gcFB(t);  /* end of list */
		}
		
		 
		/*
		** Garbage collection to arrays
		** Delete all unmarked arrays.
		*/
		public static Long lua_hashcollector ()
		{
			Hash curr_array = listhead, prev = null;
		 	Long counter = 0;
		 	call_fallbacks();
		 	while (curr_array != null)
		 	{
		  		Hash next = curr_array.next;
		  		if (markarray(curr_array) != 1)
		  		{
		   			if (prev == null) listhead = next;
		   			else              prev.next = next;
		   			hashdelete(ref curr_array);
		   			++counter;
		  		}
		  		else
		  		{
		  			markarray(curr_array, (char)0);
		   			prev = curr_array;
		  		}
		  		curr_array = next;
		 	}
		 	return counter;
		}
		
		
		/*
		** Create a new array
		** This function inserts the new array in the array list. It also
		** executes garbage collection if the number of arrays created
		** exceed a pre-defined range.
		*/
		public static Hash lua_createarray (Word nhash)
		{
			Hash array;
		 	lua_pack();
		 	array = hashcreate(nhash);
		 	array.next = listhead;
		 	listhead = array;
		 	return array;
		}
		
		
		/*
		** Re-hash
		*/
		private static void rehash (Hash t)
		{
			Word i;
		 	Word nold = nhash(t);
		 	NodeRef vold = nodevector(t);
		 	nhash(t, redimension(nhash(t)));
		 	nodevector(t, hashnodecreate(nhash(t)));
		 	for (i=0; i<nold; i++)
		 	{
		 		Node n = vold.get(i);
		  		if (tag(ref_(n)) != lua_Type.LUA_T_NIL && tag(val_(n)) != lua_Type.LUA_T_NIL)
		  			node(t, present(t, ref_(n))).set(n);  /* copy old node to new hahs */
		 	}
		 	luaI_free_NodeRef(vold);
		}
		
		/*
		** If the hash node is present, return its pointer, otherwise return
		** null.
		*/
		public static Object_ lua_hashget (Hash t, Object_ @ref)
		{
		 Word h = present(t, @ref);
		 if (tag(ref_(node(t, h))) != lua_Type.LUA_T_NIL) return val_(node(t, h));
		 else return null;
		}
		
		
		/*
		** If the hash node is present, return its pointer, otherwise create a new
		** node for the given reference and also return its pointer.
		*/
		public static Object_ lua_hashdefine (Hash t, Object_ _ref)
		{
			Word h;
		 	Node n;
		 	h = present(t, _ref);
		 	n = node(t, h);
		 	if (tag(ref_(n)) == lua_Type.LUA_T_NIL)
		 	{
		 		nuse(t, (ushort)(nuse(t) + 1));
		  		if ((float)nuse(t) > (float)nhash(t)*REHASH_LIMIT)
		  		{
		   			rehash(t);
		   			h = present(t, _ref);
		   			n = node(t, h);
		  		}
		  		ref_(n).set(_ref);
		  		tag(val_(n), lua_Type.LUA_T_NIL);
		 	}
		 	return (val_(n));
		}
		
		
		/*
		** Internal function to manipulate arrays. 
		** Given an array object and a reference value, return the next element
		** in the hash.
		** This function pushs the element value and its reference to the stack.
		*/
		private static void hashnext (Hash t, Word i)
		{
		 	if (i >= nhash(t))
		 	{
		  		lua_pushnil(); lua_pushnil();
		  		return;
		 	}
		 	while (tag(ref_(node(t,i))) == lua_Type.LUA_T_NIL || tag(val_(node(t,i))) == lua_Type.LUA_T_NIL)
		 	{
		  		if (++i >= nhash(t))
		  		{
		   			lua_pushnil(); lua_pushnil();
		   			return;
		  		}
		 	}
		 	luaI_pushobject(ref_(node(t,i)));
		 	luaI_pushobject(val_(node(t,i)));
		}
		
		public static void lua_next ()
		{
			Hash t;
		 	lua_Object o = lua_getparam(1);
		 	lua_Object r = lua_getparam(2);
		 	if (o == LUA_NOOBJECT || r == LUA_NOOBJECT)
		   		lua_error ("too few arguments to function `next'");
		 	if (lua_getparam(3) != LUA_NOOBJECT)
		   		lua_error ("too many arguments to function `next'");
		 	if (0==lua_istable(o))
		   		lua_error ("first argument of function `next' is not a table");
		 	t = avalue(luaI_Address(o));
		 	if (0!=lua_isnil(r))
		 	{
		  		hashnext(t, 0);
		 	}
		 	else
		 	{
		  		Word h = present (t, luaI_Address(r));
		  		hashnext(t, (Word)(h+1));
		 	}
		}
	}
}

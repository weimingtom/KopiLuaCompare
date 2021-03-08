/*
** mem.c
** memory manager for lua
** $Id: mem.h,v 1.2 1995/01/13 22:11:12 roberto Exp $
*/
using System;

namespace KopiLua
{
	public partial class Lua
	{
		//#ifndef mem_h
		//#define mem_h
		
		//#ifndef NULL
		//#define NULL 0
		//#endif
		
		//void luaI_free (void *block);
		//void *luaI_malloc (unsigned long size);
		//void *luaI_realloc (void *oldblock, unsigned long size);
		
		//char *luaI_strdup (char *str);
		
		//#define new(s)          ((s *)luaI_malloc(sizeof(s)))
		public static Hash new_Hash() {
			return new Hash();
		}
		public static FuncStackNode new_FuncStackNode() {
			return new FuncStackNode();
		}
		//#define newvector(n,s)  ((s *)luaI_malloc((n)*sizeof(s)))
		public static Object_[] newvector_Object(int maxstack) {
			Object_[] stack = new Object_[maxstack];
		 	for (int i = 0; i < stack.Length; ++i)
		 	{
		 		stack[i] = new Object_();
		 	}
		 	return stack;
		}
		public static CharPtr newvector_char(int lua_strconc_buffer_size) {
			return new CharPtr(new char[lua_strconc_buffer_size]);
		}
		public static BytePtr newvector_Byte(int CODE_BLOCK) {
			return new BytePtr(new Byte[CODE_BLOCK], 0);
		}
		public static Symbol[] newvector_Symbol(int lua_maxsymbol) {
			Symbol[] lua_table = new Symbol[lua_maxsymbol];
		 	for (int i = 0; i < lua_table.Length; ++i)
		 	{
		 		lua_table[i] = new Symbol();
		 	}
		 	return lua_table;
		}
		public static TaggedString[] newvector_TaggedString(int lua_maxconstant) {
			return new TaggedString[lua_maxconstant];
		}
		//#define growvector(old,n,s) ((s *)luaI_realloc(old,(n)*sizeof(s)))
		public static Object_[] growvector_Object(Object_[] stack, int maxstack)
		{
		   	Object_[] oldarr = stack;
	    	stack = new Object_[maxstack];
	    	for (int k = 0; k < stack.Length; ++k)
	    	{
	    		if (k < oldarr.Length)
	    		{
	    			stack[k] = oldarr[k];
	    		}
	    		else
	    		{
	    			stack[k] = new Object_();
	    		}
	    	}
	    	return stack;
		}
		public static Byte[] growvector_Byte(Byte[] chars, int index, int maxcurr) {
			if (index != 0) {
				throw new Exception("not support non zero index BytePtr!!!");
			}
			Byte[] oldarr = chars;
	  		chars = new Byte[maxcurr];
	    	for (int k = 0; k < chars.Length; ++k)
	    	{
	    		if (k < oldarr.Length)
	    		{
	    			chars[k] = oldarr[k];
	    		}
	    		else
	    		{
	    			chars[k] = (Byte)0;
	    		}
	    	} 
	    	return chars;
		}
		public static Symbol[] growvector_Symbol(Symbol[] lua_table, int lua_maxsymbol) {
			Symbol[] oldarr = lua_table;
	    	lua_table = new Symbol[lua_maxsymbol];
	    	for (int k = 0; k < lua_table.Length; ++k)
	    	{
	    		if (k < oldarr.Length)
	    		{
	    			lua_table[k] = oldarr[k];
	    		}
	    		else
	    		{
	    			lua_table[k] = new Symbol();
	    		}
	    	}
	    	return lua_table;
		}
		public static TaggedString[] growvector_TaggedString(TaggedString[] lua_constant, int lua_maxconstant) {
			TaggedString[] oldarr = lua_constant;
	    	lua_constant = new TaggedString[lua_maxconstant];
	    	for (int k = 0; k < lua_constant.Length; ++k)
	    	{
	    		if (k < oldarr.Length)
	    		{
	    			lua_constant[k] = oldarr[k];
	    		}
	    		else
	    		{
	    			lua_constant[k] = new TaggedString();
	    		}
	    	}
	    	return lua_constant;
		}
		    	
		//#endif 
	}
}


/*
** opcode.c
** TecCGraf - PUC-Rio
*/
using System;

namespace KopiLua
{
	using lua_Object = KopiLua.Lua.Object_;	
	using Word = System.UInt16;
	using real = System.Single; //???
	
	public partial class Lua
	{
		//char *rcs_opcode="$Id: opcode.c,v 2.1 1994/04/20 22:07:57 celes Exp $";

		//#include <stdio.h>
		//#include <stdlib.h>
		//#include <string.h>

		/* stdlib.h does not have this in SunOS */
		//extern double strtod(const char *, char **);

		//#include "mm.h"

		//#include "opcode.h"
		//#include "hash.h"
		//#include "inout.h"
		//#include "table.h"
		//#include "lua.h"

		//#define tonumber(o) ((tag(o) != T_NUMBER) && (lua_tonumber(o) != 0))
		private static bool tonumber(Object_ o) { return (tag(o) != Type.T_NUMBER) && (lua_tonumber(o) != 0); }
		//#define tostring(o) ((tag(o) != T_STRING) && (lua_tostring(o) != 0))
		private static bool tostring(Object_ o) { return (tag(o) != Type.T_STRING) && (lua_tostring(o) != 0); }
		
		//#ifndef MAXSTACK
		//#define MAXSTACK 256
		private const int MAXSTACK = 256;		
		//#endif		
		private static Object_[] _initstack()
		{
			Object_[] stack = new Object_[MAXSTACK];
			for (int i = 0; i < stack.Length; ++i)
			{
				stack[i] = new Object_();
			}
			stack[0].tag = Type.T_MARK;
			return stack;
		}

		
		private class ObjectRef 
		{
			public int index
			{
				get
				{
					return _index;
				}
				set
				{
//					if (value == 11 && this.obj[8].value.__name__.Equals("writeto"))
//					{
//						Console.WriteLine("====================");
//					}
					_index = value;
				}
			}
			
			public Object_[] obj;
			private int _index;
			public ObjectRef(ObjectRef _obj)
			{
				this.obj = _obj.obj;
				this.index = _obj.index;
			}
			public ObjectRef(ObjectRef _obj, int _index)
			{
				this.obj = _obj.obj;
				this.index = _obj.index + _index;
			}
			public ObjectRef(Object_[] _obj, int _index)
			{
				this.obj = _obj;
				this.index = _index;
			}
			
			public void inc() 
			{
				index++;
			}

			public void dec() 
			{
				index--;
			}
			
			public Object_ get()
			{
				return obj[index];
			}

			public Object_ get(int offset)
			{
				return obj[index + offset];
			}

			public ObjectRef getRef(int offset)
			{
				return new ObjectRef(obj, index + offset);
			}
			
			public void set(int offset, Object_ o)
			{
				obj[index + offset].set(o);
			}
			
			public void add(int offset) 
			{
				index += offset;
			}
			
			public bool notEqualsTo(Object_ o)
			{
				return obj[index] != o;
			}
			
			public bool isLessThan(Object_ o)
			{
				int idx = -1;
				bool found = false;
				for (int i = 0; i < obj.Length; ++i)
				{
					if (o == obj[i])
					{
						idx = i;
						found = true;
						break;
					}
				}
				if (found == false)
				{
					throw new Exception("objs not same");
				}
				return this.index < idx;				
			}
			
			public bool isLargerEquals(Object_[] objs)
			{
				if (this.obj != objs)
				{
					throw new Exception("objs not same");
				}
				return this.index >= 0;				
			}
			
			public bool isLessEquals(ObjectRef oref)
			{
				if (this.obj != oref.obj)
				{
					throw new Exception("objs not same");
				}
				return this.index <= oref.index;					
			}
			
			public void setRef(Object_ o)
			{
				bool found = false;
				for (int i = 0; i < obj.Length; ++i)
				{
					if (o == obj[i])
					{
						this.index = i;
						found = true;
						break;
					}
				}
				if (found == false)
				{
					throw new Exception("objs not same");
				}
			}
			
			public int minus(ObjectRef oref)
			{
				if (this.obj == oref.obj)
				{
					return this.index - oref.index;
				}
				throw new Exception("objs not same");
			}

			public int minus(Object_[] oarr)
			{
				if (this.obj == oarr)
				{
					return this.index - 0;
				}
				throw new Exception("objs not same");
			}
		}
		//static Object stack[MAXSTACK] = {{T_MARK, {NULL}}};
		private static Object_[] stack = _initstack();		
		//static Object *top=stack+1, *base=stack+1;
		private static ObjectRef top = new ObjectRef(stack, 1), @base = new ObjectRef(stack, 1);
		
		/*
		** Concatenate two given string, creating a mark space at the beginning.
		** Return the new string pointer.
		*/
		private static CharPtr lua_strconc(CharPtr l, CharPtr r)
		{
			CharPtr s = (CharPtr)calloc_char (strlen(l)+strlen(r)+2);
		 	if (s == null)
		 	{
		  		lua_error ("not enough memory");
		  		return null;
		 	}
		 	s[0] = '\0'; s.inc(); // create mark space
		 	return strcat(strcpy(s,l),r);
		}
	
		/*
		** Duplicate a string,  creating a mark space at the beginning.
		** Return the new string pointer.
		*/
		public static CharPtr lua_strdup(CharPtr l)
		{
			CharPtr s = (CharPtr)calloc_char (strlen(l)+2);
		 	if (s == null)
		 	{
		  		lua_error ("not enough memory");
		  		return null;
		 	}
		 	s[0] = '\0'; s.inc(); // create mark space
		 	return strcpy(s,l);
		}
	
		/*
		** Convert, if possible, to a number tag.
		** Return 0 in success or not 0 on error.
		*/ 
		private static int lua_tonumber(Object_ obj)
		{
		 	CharPtr ptr = null;
		 	if (tag(obj) != Type.T_STRING)
		 	{
		  		lua_reportbug ("unexpected type at conversion to number");
		  		return 1;
		 	}
		 	nvalue(obj, (float)strtod(svalue(obj), ref ptr));
		 	if (ptr[0] != '\0')
		 	{
		  		lua_reportbug ("string to number convertion failed");
		  		return 2;
		 	}
		 	tag(obj, Type.T_NUMBER);
		 	return 0;
		}
	
		private static Object_ lua_convtonumber_cvt = new Object_();
		/*
		** Test if is possible to convert an object to a number one.
		** If possible, return the converted object, otherwise return nil object.
		*/ 
		private static Object_ lua_convtonumber(Object_ obj)
		{
			// static object cvt;
	
		 	if (tag(obj) == Type.T_NUMBER)
		 	{
		  		lua_convtonumber_cvt = obj;
		  		return lua_convtonumber_cvt;
		 	}
	
		 	tag(lua_convtonumber_cvt, Type.T_NIL);
		 	if (tag(obj) == Type.T_STRING)
		 	{
		  		CharPtr ptr = null;
		  		nvalue(lua_convtonumber_cvt, (float)strtod(svalue(obj), ref ptr));
		  		if (ptr[0] == '\0')
		  		{
		  			tag(lua_convtonumber_cvt, Type.T_NUMBER);
		  		}
		 	}
		 	return lua_convtonumber_cvt;
		}
	
		private static CharPtr lua_tostring_s = new CharPtr(new char[256]);
		/*
		** Convert, if possible, to a string tag
		** Return 0 in success or not 0 on error.
		*/ 
		private static int lua_tostring(Object_ obj)
		{
			// static sbyte s[256];
		 	if (tag(obj) != Type.T_NUMBER)
		 	{
		  		lua_reportbug ("unexpected type at conversion to string");
		  		return 1;
		 	}
		 	if ((int) nvalue(obj) == nvalue(obj))
		  		sprintf (lua_tostring_s, "%d", (int) nvalue(obj));
		 	else
		 		sprintf (lua_tostring_s, "%g", nvalue(obj));
		 	svalue(obj, lua_createstring(lua_strdup(lua_tostring_s)));
		 	if (svalue(obj) == null)
		  		return 1;
		 	tag(obj, Type.T_STRING);
		 	return 0;
		}
	

		/*
		** Execute the given opcode. Return 0 in success or 1 on error.
		*/
		public static int lua_execute(BytePtr pc)
		{
			ObjectRef oldbase = new ObjectRef(@base);
			@base = new ObjectRef(top);
		 	while (true)
		 	{
		  		OpCode opcode;
		  		opcode = (OpCode)pc[0]; pc.inc();
		  		switch (opcode)
		  		{
		   		case OpCode.PUSHNIL:
			  		tag(top.get(), Type.T_NIL);
			  		top.inc();
			   		break;
	
		   		case OpCode.PUSH0:
			   		tag(top.get(), Type.T_NUMBER);
			   		nvalue(top.get(), 0);
			   		top.inc();
			   		break;
		   
			   	case OpCode.PUSH1:
			   		tag(top.get(), Type.T_NUMBER);
			   		nvalue(top.get(), 1);
			   		top.inc();
			   		break;
		   
			   	case OpCode.PUSH2:
			   		tag(top.get(), Type.T_NUMBER);
			   		nvalue(top.get(), 2);
			   		top.inc();
			   		break;
	
		   		case OpCode.PUSHBYTE:
			   		tag(top.get(), Type.T_NUMBER);
			   		nvalue(top.get(), pc[0]); pc.inc();
			   		top.inc();
			   		break;
   
		   		case OpCode.PUSHWORD: 
		   			{
			   			CodeWord code = new CodeWord();
					    get_word(code,pc);
					    tag(top.get(), Type.T_NUMBER);
					    nvalue(top.get(), code.w);
					    top.inc();
					}
		   			break;
   
		   		case OpCode.PUSHFLOAT:
		   			{
		   				CodeFloat code = new CodeFloat();
				    	get_float(code,pc);
				    	tag(top.get(), Type.T_NUMBER);
				    	nvalue(top.get(), code.f);
				    	top.inc();
				   	}
		   			break;

		   		case OpCode.PUSHSTRING:
		   			{
		   				CodeWord code = new CodeWord();
		    			get_word(code,pc);
		    			tag(top.get(), Type.T_STRING);
		    			svalue(top.get(), lua_constant[code.w]);
		    			top.inc();
		   			}
		   			break;
   
		   		case OpCode.PUSHLOCAL0: case OpCode.PUSHLOCAL1: case OpCode.PUSHLOCAL2:
		   		case OpCode.PUSHLOCAL3: case OpCode.PUSHLOCAL4: case OpCode.PUSHLOCAL5:
		   		case OpCode.PUSHLOCAL6: case OpCode.PUSHLOCAL7: case OpCode.PUSHLOCAL8:
		   		case OpCode.PUSHLOCAL9: 
		   			top.set(0, @base.get((int)(opcode-OpCode.PUSHLOCAL0)));
		   			top.inc();
		   			break;
   
		   		case OpCode.PUSHLOCAL: 
		   			top.set(0, @base.get(pc[0]));
		   			top.inc();
		   			pc.inc();
		   			break;
   
		   		case OpCode.PUSHGLOBAL: 
		   			{
		   				CodeWord code = new CodeWord();
		    			get_word(code,pc);
		    			top.set(0, s_object(code.w));
		    			top.inc();
		   			}
		   			break;
   
				case OpCode.PUSHINDEXED:
			   		top.dec();
			   		if (tag(top.get(-1)) != Type.T_ARRAY)
					{
				 		lua_reportbug ("indexed expression not a table");
				 		return 1;
					}
					{
			   			Object_ h = lua_hashdefine (avalue(top.get(- 1)), top.get());
				 		if (h == null) return 1;
				 		top.set(-1, h);
					}
			   		break;
		
			   	case OpCode.PUSHMARK:
			   		tag(top.get(), Type.T_MARK);
			   		top.inc();
				   	break;
		
			   	case OpCode.PUSHOBJECT:
				   	top.set(0, top.get(-3));
				   	top.inc();
				   	break;
   
		   		case OpCode.STORELOCAL0: case OpCode.STORELOCAL1: case OpCode.STORELOCAL2:
		   		case OpCode.STORELOCAL3: case OpCode.STORELOCAL4: case OpCode.STORELOCAL5:
		   		case OpCode.STORELOCAL6: case OpCode.STORELOCAL7: case OpCode.STORELOCAL8:
		   		case OpCode.STORELOCAL9: 
				   	top.dec();
				   	@base.set((int)(opcode-OpCode.STORELOCAL0), top.get());
				   	break;
    
		   		case OpCode.STORELOCAL: 
				   	top.dec();
				   	@base.set(pc[0], top.get());
				   	pc.inc();
				   	break;
   
		   		case OpCode.STOREGLOBAL:
		   			{
				   		CodeWord code = new CodeWord();
					    get_word(code,pc);
					    top.dec();
					    s_object(code.w, top.get());
					}
		   			break;

			   	case OpCode.STOREINDEXED0:
			   		if (tag(top.get(-3)) != Type.T_ARRAY)
			   		{
				 		lua_reportbug ("indexed expression not a table");
				 		return 1;
					}
					{
			   			Object_ h = lua_hashdefine(avalue(top.get(-3)), top.get(-2));
				 		if (h == null)
				 		{
					 		return 1;
				 		}
				 		h.set(top.get(-1));
					}
			   		top.add(-3);
			   		break;
		
			   	case OpCode.STOREINDEXED:
			   		{
			   			int n = pc[0]; pc.inc();
			   			if (tag(top.get(-3-n)) != Type.T_ARRAY)
						{
				 			lua_reportbug ("indexed expression not a table");
				 			return 1;
						}
						{
			   				Object_ h = lua_hashdefine (avalue(top.get(-3-n)), top.get(-2-n));
				 			if (h == null)
				 			{
					 			return 1;
				 			}
				 			h.set(top.get(-1));
						}
						top.dec();
			   		}
			   		break;
   
	   			case OpCode.STORELIST0:
	   			case OpCode.STORELIST:
	   				{
					    int m, n;
					    Object_ arr;
					    if (opcode == OpCode.STORELIST0) { m = 0; }
					    else { m = pc[0] * FIELDS_PER_FLUSH; pc.inc(); }
					    n = pc[0]; pc.inc();
					    arr = top.get(-n-1);
					    if (tag(arr) != Type.T_ARRAY)
					    {
					     	lua_reportbug ("internal error - table expected");
					     	return 1;
					    }
					    while (n != 0)
					    {
					    	tag(top.get(), Type.T_NUMBER); nvalue(top.get(), n+m);
					    	lua_hashdefine (avalue(arr), top.get()).set(top.get(-1));
					     	top.dec();
					     	n--;
					    }
					}
					break;
   
		   		case OpCode.STORERECORD:
		   			{
						int n = pc[0]; pc.inc();
						Object_ arr = top.get(-n-1);
					    if (tag(arr) != Type.T_ARRAY)
					    {
					     	lua_reportbug ("internal error - table expected");
					     	return 1;
					    }
					    while (n != 0)
					    {
					    	CodeWord code = new CodeWord();
					     	get_word(code,pc);
					     	tag(top.get(), Type.T_STRING); svalue(top.get(), lua_constant[code.w]);
					     	(lua_hashdefine (avalue(arr), top.get())).set(top.get(-1));
					     	top.dec();
					     	n--;
					    }
					}
					break;
   
		   		case OpCode.ADJUST:
		   			{
						ObjectRef newtop = @base.getRef(pc[0]); pc.inc();
						while (top.isLessThan(newtop.get())) { tag(top.get(), Type.T_NIL); top.inc(); }
					    top = newtop;  /* top could be bigger than newtop */
					}
					break;
   
			   	case OpCode.CREATEARRAY:
			   		if (tag(top.get(-1)) == Type.T_NIL)
			   			nvalue(top.get(-1), 101);
					else
					{
						if (tonumber(top.get(-1))) return 1;
						if (nvalue(top.get(-1)) <= 0) nvalue(top.get(-1), 101);
				 	}
					avalue(top.get(-1), (Hash)lua_createarray((int)nvalue(top.get(-1))));
					if (avalue(top.get(-1)) == null)
				 		return 1;
					tag(top.get(-1), Type.T_ARRAY);
			   		break;
   
			   	case OpCode.EQOP:
			   		{
			   			Object_ l = top.get(-2);
			   			Object_ r = top.get(-1);
			   			top.dec();
						if (tag(l) != tag(r))
							tag(top.get(-1), Type.T_NIL);
						else
						{
				 			switch (tag(l))
				 			{
				  			case Type.T_NIL:
				 				tag(top.get(-1), Type.T_NUMBER);
					  			break;
				  
					  		case Type.T_NUMBER:
					  			tag(top.get(-1), (nvalue(l) == nvalue(r)) ? Type.T_NUMBER : Type.T_NIL);
					  			break;
				  
					  		case Type.T_ARRAY:
					  			tag(top.get(-1), (avalue(l) == avalue(r)) ? Type.T_NUMBER : Type.T_NIL);
					  			break;
				  
					  		case Type.T_FUNCTION:
					  			tag(top.get(-1), (bvalue(l) == bvalue(r)) ? Type.T_NUMBER : Type.T_NIL);
					  			break;
				  
					  		case Type.T_CFUNCTION:
					  			tag(top.get(-1), (fvalue(l) == fvalue(r)) ? Type.T_NUMBER : Type.T_NIL);
					  			break;
				  
					  		case Type.T_USERDATA:
					  			tag(top.get(-1), (uvalue(l) == uvalue(r)) ? Type.T_NUMBER : Type.T_NIL);
					  			break;
				  
					  		case Type.T_STRING:
					  			tag(top.get(-1), (strcmp (svalue(l), svalue(r)) == 0) ? Type.T_NUMBER : Type.T_NIL);
					  			break;
				  
					  		case Type.T_MARK:
					  			return 1;
				 			}
						}
						nvalue(top.get(-1), 1);
			   		}
			   		break;
		
			   	case OpCode.LTOP:
			   		{
			   			Object_ l = top.get(-2);
			   			Object_ r = top.get(-1);
			   			top.dec();
						if (tag(l) == Type.T_NUMBER && tag(r) == Type.T_NUMBER)
						{
							tag(top.get(-1), (nvalue(l) < nvalue(r)) ? Type.T_NUMBER : Type.T_NIL);
						}
						else
						{
					 		if (tostring(l) || tostring(r))
					  			return 1;
					 		tag(top.get(-1), (strcmp (svalue(l), svalue(r)) < 0) ? Type.T_NUMBER : Type.T_NIL);
						}
						nvalue(top.get(-1), 1);
			   		}
			   		break;
		
			   	case OpCode.LEOP:
			   		{
			   			Object_ l = top.get(-2);
			   			Object_ r = top.get(-1);
			   			top.dec();
						if (tag(l) == Type.T_NUMBER && tag(r) == Type.T_NUMBER)
						{
							tag(top.get(-1), (nvalue(l) <= nvalue(r)) ? Type.T_NUMBER : Type.T_NIL);
						}
						else
						{
					 		if (tostring(l) || tostring(r))
					  			return 1;
					 		tag(top.get(-1), (strcmp (svalue(l), svalue(r)) <= 0) ? Type.T_NUMBER : Type.T_NIL);
						}
						nvalue(top.get(-1), 1);
				   	}
			   		break;
		
			   case OpCode.ADDOP:
			   		{
			   			Object_ l = top.get(-2);
			   			Object_ r = top.get(-1);
						if (tonumber(r) || tonumber(l))
				 			return 1;
						nvalue(l, nvalue(l) + nvalue(r));
						top.dec();
			   		}
			   		break;
		
			  	case OpCode.SUBOP:
			   		{
			   			Object_ l = top.get(-2);
			   			Object_ r = top.get(-1);
						if (tonumber(r) || tonumber(l))
				 			return 1;
						nvalue(l, nvalue(l) - nvalue(r));
						top.dec();
			   		}
			   		break;
		
			   	case OpCode.MULTOP:
			   		{
			   			Object_ l = top.get(-2);
			   			Object_ r = top.get(-1);
						if (tonumber(r) || tonumber(l))
				 			return 1;
						nvalue(l, nvalue(l) * nvalue(r));
						top.dec();
			   		}
			   		break;
		
			   	case OpCode.DIVOP:
			   		{
			   			Object_ l = top.get(-2);
			   			Object_ r = top.get(-1);
						if (tonumber(r) || tonumber(l))
				 			return 1;
						nvalue(l, nvalue(l) / nvalue(r));
						top.dec();
			   		}
			   		break;
		
			   	case OpCode.CONCOP:
			   		{
			   			Object_ l = top.get(-2);
			   			Object_ r = top.get(-1);
						if (tostring(r) || tostring(l))
				 			return 1;
						svalue(l, lua_createstring (lua_strconc(svalue(l),svalue(r))));
						if (svalue(l) == null)
				 			return 1;
						top.dec();
			   		}
			   		break;
		
			   	case OpCode.MINUSOP:
			   		if (tonumber(top.get(-1)))
						return 1;
					nvalue(top.get(-1), - nvalue(top.get(-1)));
			   		break;
		
			   	case OpCode.NOTOP:
			   		tag(top.get(-1), tag(top.get(-1)) == Type.T_NIL ? Type.T_NUMBER : Type.T_NIL);
			   		break;
   
		   		case OpCode.ONTJMP:
		   			{
			   			CodeWord code = new CodeWord();
					    get_word(code,pc);
					    if (tag(top.get(-1)) != Type.T_NIL) pc += code.w;
					}
					break;
   
		   		case OpCode.ONFJMP:	   
		   			{
						CodeWord code = new CodeWord();
				    	get_word(code,pc);
				    	if (tag(top.get(-1)) == Type.T_NIL) pc += code.w;
				   	}
		   			break;
   
		   		case OpCode.JMP:
		   			{
		   				CodeWord code = new CodeWord();
				    	get_word(code,pc);
				    	pc += code.w;
				   	}
				   	break;
    
		   		case OpCode.UPJMP:
		   			{
				   		CodeWord code = new CodeWord();
					    get_word(code,pc);
					    pc -= code.w;
					}
					break;
   
		   		case OpCode.IFFJMP:
		   			{
						CodeWord code = new CodeWord();
				    	get_word(code,pc);
				    	top.dec();
				    	if (tag(top.get()) == Type.T_NIL) pc += code.w;
				   	}
				   	break;

		   		case OpCode.IFFUPJMP:
		   			{
				   		CodeWord code = new CodeWord();
					    get_word(code,pc);
					    top.dec();
					    if (tag(top.get()) == Type.T_NIL) pc -= code.w;
				   }
		   			break;

			   	case OpCode.POP:
			   		top.dec();
				   	break;
		
			   	case OpCode.CALLFUNC:
			   		{
//				   		if (pc.chars != code.chars && pc.index == 4454)
//				   		{
//				   			Console.WriteLine("=================");
//				   		}
						BytePtr newpc;
						ObjectRef b_ = top.getRef(-1);
						while (tag(b_.get()) != Type.T_MARK) b_.dec();
//						if (b_.obj == stack)
//						{
//							Console.WriteLine("================");
//						}
						if (tag(b_.get(-1)) == Type.T_FUNCTION)
						{
				 			lua_debugline = 0;			/* always reset debug flag */
				 			newpc = bvalue(b_.get(-1));
				 			bvalue(b_.get(-1), pc);		        /* store return code */
				 			nvalue(b_.get(), @base.minus(stack));		/* store base value */
				 			@base = b_.getRef(+1);
				 			pc = new BytePtr(newpc);
				 			if (MAXSTACK-@base.minus(stack) < STACKGAP)
				 			{
				  				lua_error ("stack overflow");
				  				return 1;
				 			}
						}
						else if (tag(b_.get(-1)) == Type.T_CFUNCTION)
						{
				 			int nparam;
				 			lua_debugline = 0; // always reset debug flag
				 			nvalue(b_.get(), @base.minus(stack)); // store base value
				 			@base = b_.getRef(+1);
				 			nparam = top.minus(@base); // number of parameters
				 			(fvalue(b_.get(-1)))(); // call C function
		
				 			/* shift returned values */
							{
				  				int i;
				  				int nretval = top.minus(@base) - nparam;
				  				top = @base.getRef(-2);
				  				@base = new ObjectRef(stack, (int) nvalue(@base.get(-1)));
				  				for (i=0; i<nretval; i++)
				  				{
				  					top.get().set(top.get(nparam + 2));
				   					top.inc();
				  				}
				 			}
						}
						else
						{
				 			lua_reportbug ("call expression not a function");
				 			return 1;
						}
			   		}
			   		break;
		
			   	case OpCode.RETCODE:
			   		{
						int i;
						int shift = pc[0]; pc.inc();
						int nretval = top.minus(@base) - shift;
						top.setRef(@base.get(-2));
						pc = bvalue(@base.get(-2));
						@base = new ObjectRef(stack, (int) nvalue(@base.get(-1))); //FIXME:???new ObjectRef???
						for (i=0; i<nretval; i++)
						{
							top.get().set(top.get(shift + 2));
			 				top.inc();
						}
		   			}
		   			break;
   
			  	case OpCode.HALT:
		   			@base = new ObjectRef(oldbase);
			   		return 0; // success
		
		   		case OpCode.SETFUNCTION:
				   	{
			   			CodeWord file = new CodeWord(), func = new CodeWord();
				    	get_word(file,pc);
				    	get_word(func,pc);
				    	if (0 != lua_pushfunction (file.w, func.w))
				     		return 1;
				   	}
				   	break;
   
		   		case OpCode.SETLINE:
		   		   	{
				   		CodeWord code = new CodeWord();
				    	get_word(code,pc);
				    	lua_debugline = code.w;
				   	}
				   	break;
   
		   		case OpCode.RESET:
					lua_popfunction ();
		   			break;
	
		   		default:
					lua_error ("internal error - opcode didn't match");
		   			return 1;
			  	}
		 	}
		}
	
	
		public delegate void lua_travstack_fn(Object_ obj);
		/*
		** Traverse all objects on stack
		*/
		public static void lua_travstack (lua_travstack_fn fn)
		{
			ObjectRef o;
			for (o = top.getRef(-1); o.isLargerEquals(stack); o.dec())
		  		fn (o.get());
		}
	
		/*
		** Open file, generate opcode and execute global statement. Return 0 on
		** success or 1 on error.
		*/
		public static int lua_dofile (CharPtr filename)
		{
		 	if (lua_openfile (filename) != 0) return 1;
		 	if (lua_parse () != 0) { lua_closefile (); return 1; }
		 	lua_closefile ();
		 	return 0;
		}
	
		/*
		** Generate opcode stored on string and execute global statement. Return 0 on
		** success or 1 on error.
		*/
		public static int lua_dostring (CharPtr string_)
		{
			if (lua_openstring (string_) != 0) return 1;
		 	if (lua_parse () != 0) return 1;
		 	lua_closestring();
		 	return 0;
		}
	
		private static byte[] lua_call_startcode = {(byte)OpCode.CALLFUNC, (byte)OpCode.HALT};
		/*
		** Execute the given function. Return 0 on success or 1 on error.
		*/
		public static int lua_call(CharPtr functionname, int nparam)
		{
			// static Byte startcode[] = {CALLFUNC, HALT};
		 	int i;
		 	Object_ func = new Object_(); func.set(s_object(lua_findsymbol(functionname))); //FIXME:???copy???
		 	if (tag(func) != Type.T_FUNCTION) return 1;
		 	for (i = 1; i <= nparam; i++)
		 		top.get(-i+2).set(top.get(-i));
		 	top.add(2);
		 	tag(top.get(-nparam-1), Type.T_MARK);
		 	top.get(-nparam-2).set(func);
		 	return (lua_execute (new BytePtr(lua_call_startcode, 0)));
		}
	
		/*
		** Get a parameter, returning the object handle or NULL on error.
		** 'number' must be 1 to get the first parameter.
		*/
		public static Object_ lua_getparam(int number)
		{
			if (number <= 0 || number > top.minus(@base)) return null;
			return (@base.get(number-1));
		}
	
		/*
		** Given an object handle, return its number value. On error, return 0.0.
		*/
		public static real lua_getnumber(Object_ @object)
		{
		 	if (@object == null || tag(@object) == Type.T_NIL) return 0.0f;
			if (tonumber (@object)) return 0.0f;
			else                   return (nvalue(@object));
		}
	
		/*
		** Given an object handle, return its string pointer. On error, return NULL.
		*/
		public static CharPtr lua_getstring(Object_ @object)
		{
		 	if (@object == null || tag(@object) == Type.T_NIL) return null;
			if (tostring (@object)) return null;
			else                   return (svalue(@object));
		}
	
		/*
		** Given an object handle, return a copy of its string. On error, return NULL.
		*/
		public static CharPtr lua_copystring(Object_ @object)
		{
		 	if (@object == null|| tag(@object) == Type.T_NIL) return null;
			if (tostring (@object)) return null;
			else                   return (strdup(svalue(@object)));
		}
	
		/*
		** Given an object handle, return its cfuntion pointer. On error, return NULL.
		*/
		public static lua_CFunction lua_getcfunction(Object_ @object)
		{
		 	if (@object == null) return null;
			if (tag(@object) != Type.T_CFUNCTION) return null;
			else                            return (fvalue(@object));
		}
	
		/*
		** Given an object handle, return its user data. On error, return NULL.
		*/
		public static object lua_getuserdata(Object_ @object)
		{
			if (@object == null) return null;
			if (tag(@object) != Type.T_USERDATA) return null;
			else                           return (uvalue(@object));
		}
	
		/*
		** Given an object handle and a field name, return its field object.
		** On error, return NULL.
		*/
		public static Object_ lua_getfield(Object_ @object, CharPtr field)
		{
		 	if (@object == null) return null;
		 	if (tag(@object) != Type.T_ARRAY)
		  		return null;
		 	else
		 	{	
		 		Object_ @ref = new Object_();
		 		tag(@ref, Type.T_STRING);
		 		svalue(@ref, lua_createstring(lua_strdup(field)));
		  		return (lua_hashdefine(avalue(@object), @ref));
		 	}
		}

		/*
		** Given an object handle and an index, return its indexed object.
		** On error, return NULL.
		*/
		public static Object_ lua_getindexed(Object_ @object, float index)
		{
		 	if (@object == null) return null;
		 	if (tag(@object) != Type.T_ARRAY)
		  		return null;
		 	else
		 	{
		 		Object_ @ref = new Object_();
		 		tag(@ref, Type.T_NUMBER);
		 		nvalue(@ref, index);
		 	 	return (lua_hashdefine(avalue(@object), @ref));
		 	}
		}
	
		/*
		** Get a global object. Return the object handle or NULL on error.
		*/
		public static Object_ lua_getglobal (CharPtr name)
		{
			int n = lua_findsymbol(name);
			if (n < 0) return null;
			return s_object(n);
		}
	
		/*
		** Pop and return an object
		*/
		public static Object lua_pop ()
		{
			if (top.isLessEquals(@base)) return null;
			top.dec();
			return top;
		}
	
		/*
		** Push a nil object
		*/
		public static int lua_pushnil ()
		{
			if ((top.minus(stack)) >= MAXSTACK-1)
		 	{
		  		lua_error ("stack overflow");
		  		return 1;
		 	}
			tag(top.get(), Type.T_NIL);
		 	return 0;
		}
	
		/*
		** Push an object (tag=number) to stack. Return 0 on success or 1 on error.
		*/
		public static int lua_pushnumber (real n)
		{
			if ((top.minus(stack)) >= MAXSTACK-1)
		 	{
		  		lua_error ("stack overflow");
		  		return 1;
		 	}
			tag(top.get(), Type.T_NUMBER);
			nvalue(top.get(), (float)n); //FIXME:real->float
			top.inc();
		 	return 0;
		}
	
		/*
		** Push an object (tag=string) to stack. Return 0 on success or 1 on error.
		*/
		public static int lua_pushstring (CharPtr s)
		{
			if ((top.minus(stack)) >= MAXSTACK-1)
		 	{
		  		lua_error ("stack overflow");
		  		return 1;
		 	}
			tag(top.get(), Type.T_STRING);
		 	svalue(top.get(), lua_createstring(lua_strdup(s)));
			top.inc();
		 	return 0;
		}
	
		/*
		** Push an object (tag=cfunction) to stack. Return 0 on success or 1 on error.
		*/
		public static int lua_pushcfunction (lua_CFunction fn, string name)
		{
			if ((top.minus(stack)) >= MAXSTACK-1)
		 	{
		  		lua_error ("stack overflow");
		  		return 1;
		 	}
			tag(top.get(), Type.T_CFUNCTION);
			fvalue(top.get(), fn, name);
			top.inc();
		 	return 0;
		}
	
		/*
		** Push an object (tag=userdata) to stack. Return 0 on success or 1 on error.
		*/
		public static int lua_pushuserdata(object u)
		{
			if ((top.minus(stack)) >= MAXSTACK-1)
		 	{
		  		lua_error ("stack overflow");
		  		return 1;
		 	}
			tag(top.get(), Type.T_USERDATA);
			uvalue(top.get(), u);
			top.inc();
		 	return 0;
		}
	
		/*
		** Push an object to stack.
		*/
		public static int lua_pushobject(Object_ o)
		{
			if ((top.minus(stack)) >= MAXSTACK-1)
		 	{
		  		lua_error ("stack overflow");
		  		return 1;
			}
			top.get().set(o);
			top.inc();
		 	return 0;
		}
	
		/*
		** Store top of the stack at a global variable array field. 
		** Return 1 on error, 0 on success.
		*/
		public static int lua_storeglobal (CharPtr name)
		{
		 	int n = lua_findsymbol (name);
		 	if (n < 0) return 1;
		 	if (tag(top.get(-1)) == Type.T_MARK) return 1;
		 	top.dec(); s_object(n).set(top.get());
		 	return 0;
		}
	
		/*
		** Store top of the stack at an array field. Return 1 on error, 0 on success.
		*/
		public static int lua_storefield (lua_Object @object, CharPtr field)
		{
		 	if (tag(@object) != Type.T_ARRAY)
		  		return 1;
		 	else
		 	{
		 		Object_ @ref = new Object_(), h;
		  		tag(@ref, Type.T_STRING);
		  		svalue(@ref, lua_createstring(lua_strdup(field)));
		  		h = lua_hashdefine(avalue(@object), @ref);
		  		if (h == null) return 1;
		  		if (tag(top.get(-1)) == Type.T_MARK) return 1;
		  		top.dec(); h.set(top.get());
		 	}
		 	return 0;
		}
	
	
		/*
		** Store top of the stack at an array index. Return 1 on error, 0 on success.
		*/
		public static int lua_storeindexed (lua_Object @object, float index)
		{
		 	if (tag(@object) != Type.T_ARRAY)
		  		return 1;
		 	else
		 	{
		 		Object_ @ref = new Object_(), h;
		 		tag(@ref, Type.T_NUMBER);
		 		nvalue(@ref, index); //FIXME:real->float
		  		h = lua_hashdefine(avalue(@object), @ref);
		  		if (h == null) return 1;
		  		if (tag(top.get(-1)) == Type.T_MARK) return 1;
		  		top.dec(); h.set(top.get());
		 	}
		 	return 0;
		}
	
	
		/*
		** Given an object handle, return if it is nil.
		*/
		public static int lua_isnil (Object_ @object)
		{
			return (@object != null && tag(@object) == Type.T_NIL) ? 1 : 0;
		}
	
		/*
		** Given an object handle, return if it is a number one.
		*/
		public static int lua_isnumber (Object_ @object)
		{
		 	return (@object != null && tag(@object) == Type.T_NUMBER) ? 1 : 0;
		}
	
		/*
		** Given an object handle, return if it is a string one.
		*/
		public static int lua_isstring (Object_ @object)
		{
			return (@object != null && tag(@object) == Type.T_STRING) ? 1 : 0;
		}
	
		/*
		** Given an object handle, return if it is an array one.
		*/
		public static int lua_istable (Object_ @object)
		{
			return (@object != null && tag(@object) == Type.T_ARRAY) ? 1 : 0;
		}
	
		/*
		** Given an object handle, return if it is a cfunction one.
		*/
		public static int lua_iscfunction (Object_ @object)
		{
			return (@object != null && tag(@object) == Type.T_CFUNCTION) ? 1 : 0;
		}
	
		/*
		** Given an object handle, return if it is an user data one.
		*/
		public static int lua_isuserdata (Object_ @object)
		{
		 	return (@object != null && tag(@object) == Type.T_USERDATA) ? 1 : 0;
		}
	
		/*
		** Internal function: return an object type. 
		*/
		public static void lua_type ()
		{
		 	Object_ o = lua_getparam(1);
		 	lua_pushstring (lua_constant[(int)tag(o)]);
		}
	
		/*
		** Internal function: convert an object to a number
		*/
		public static void lua_obj2number ()
		{
		 	Object_ o = lua_getparam(1);
		 	lua_pushobject (lua_convtonumber(o));
		}
	
		/*
		** Internal function: print object values
		*/
		public static void lua_print ()
		{
		 	int i = 1;
		 	Object_ obj;
		 	while ((obj=lua_getparam (i++)) != null)
		 	{
				if      (0!=lua_isnumber(obj))    printf("%g\n",lua_getnumber (obj));
				else if (0!=lua_isstring(obj))    printf("%s\n",lua_getstring (obj));
				else if (0!=lua_iscfunction(obj)) printf("cfunction: %p\n",lua_getcfunction (obj));
				else if (0!=lua_isuserdata(obj))  printf("userdata: %p\n",lua_getuserdata (obj));
				else if (0!=lua_istable(obj))     printf("table: %p\n",obj);
				else if (0!=lua_isnil(obj))       printf("nil\n");
				else			         printf("invalid value to print\n");
		 	}
		}

		/*
		** Internal function: do a file
		*/
		public static void lua_internaldofile ()
		{
		 	lua_Object obj = lua_getparam (1);
		 	if (0!=lua_isstring(obj) && 0==lua_dofile(lua_getstring(obj)))
		  		lua_pushnumber(1);
		 	else
		  		lua_pushnil();
		}

		/*
		** Internal function: do a string
		*/
		public static void lua_internaldostring ()
		{
		 	lua_Object obj = lua_getparam (1);
		 	if (0!=lua_isstring(obj) && 0==lua_dostring(lua_getstring(obj)))
		  		lua_pushnumber(1);
		 	else
		  		lua_pushnil();
		}
   }
}

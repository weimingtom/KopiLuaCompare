//#define MY_DEBUG

/*
** opcode.c
** TecCGraf - PUC-Rio
*/

namespace KopiLua
{
	using lua_Object = System.UInt32;
	using Word = System.UInt16;
	using real = System.Single;	
	using Bool = System.Int32;
	using Long = System.Int32;
	using StkId = System.Int32;
	
	public partial class Lua
	{
		//char *rcs_opcode="$Id: opcode.c,v 3.34 1995/02/06 19:35:09 roberto Exp $";
		
		//#include <setjmp.h>
		//#include <stdlib.h>
		//#include <stdio.h>
		//#include <string.h>
		//#include <math.h>
		
		//#include "mem.h"
		//#include "opcode.h"
		//#include "hash.h"
		//#include "inout.h"
		//#include "table.h"
		//#include "lua.h"
		//#include "fallback.h"
		
		private static bool tonumber(Object_ o) { return ((tag(o) != lua_Type.LUA_T_NUMBER) && (lua_tonumber(o) != 0)); }
		private static bool tostring(Object_ o) { return ((tag(o) != lua_Type.LUA_T_STRING) && (lua_tostring(o) != 0)); }
		
		
		private const int STACK_BUFFER = (STACKGAP+128);
		
		//typedef int StkId;  /* index to stack elements */
		
		private static Long    maxstack = 0;
		private static Object_[] stack = null;
		private static ObjectRef top = null;
		
		
		/* macros to convert from lua_Object to (Object *) and back */
		 
		private static Object_ Address(lua_Object lo)     { return stack[((lo)+0-1)]; }
		private static lua_Object Ref(ObjectRef st)         { return (lua_Object)(ObjectRef.minus(st, stack)+1); }
		 
		
		private static StkId CBase = 0;  /* when Lua calls C or C calls Lua, points to */
		                          /* the first slot after the last parameter. */
		private static int CnResults = 0; /* when Lua calls C, has the number of parameters; */
		                         /* when C calls Lua, has the number of results. */
		
		private static  jmp_buf errorJmp = null; /* current error recover point */
		
		
		//static StkId lua_execute (Byte *pc, StkId base);
		//static void do_call (Object *func, StkId base, int nResults, StkId whereRes);
		
		
		
		public static Object_ luaI_Address (lua_Object o)
		{
			return Address(o);
		}
		
		
		/*
		** Error messages
		*/
		
		private static void lua_message (CharPtr s)
		{
		  	lua_pushstring(s);
		  	do_call(luaI_fallBacks[FB_ERROR].function, (ObjectRef.minus(top, stack)-1), 0, (ObjectRef.minus(top, stack)-1));
		}
		
		/*
		** Reports an error, and jumps up to the available recover label
		*/
		public static void lua_error (CharPtr s)
		{
		  	if (s!=null) lua_message(s);
		  	if (errorJmp!=null)
		    	longjmp(errorJmp, 1);
		  	else
		  	{
		    	fprintf (stderr, "lua: exit(1). Unable to recover\n");
		    	exit(1);
		  	}
		}
		
		
		/*
		** Init stack
		*/
		private static void lua_initstack ()
		{
		 	maxstack = STACK_BUFFER;
		 	stack = newvector_Object(maxstack);
		 	top = new ObjectRef(stack, 0);
		}
		
		
		/*
		** Check stack overflow and, if necessary, realloc vector
		*/
		private static void lua_checkstack(long n) { if ((Long)(n) > maxstack) checkstack((StkId)n); }
		
		private static void checkstack (StkId n)
		{
		 	StkId t;
		 	if (stack == null)
		   		lua_initstack();
		 	if (maxstack >= MAX_INT)
		   		lua_error("stack size overflow");
		 	t = ObjectRef.minus(top, stack);
		 	maxstack *= 2;
		 	if (maxstack >= MAX_INT)
		   		maxstack = MAX_INT;
		 	stack = growvector_Object(stack, maxstack);
		 	top = new ObjectRef(stack, t);
		}
		
		
		/*
		** Concatenate two given strings. Return the new string pointer.
		*/
		private static CharPtr lua_strconc_buffer = null;
		private static int lua_strconc_buffer_size = 0;
		private static CharPtr lua_strconc (CharPtr l, CharPtr r)
		{
			int nl = (int)strlen(l);
			int n = (int)(nl+strlen(r)+1);
		 	if (n > lua_strconc_buffer_size)
		  	{
		   		lua_strconc_buffer_size = n;
		   		if (lua_strconc_buffer != null)
		     		luaI_free_CharPtr(ref lua_strconc_buffer);
		   		lua_strconc_buffer = newvector_char(lua_strconc_buffer_size);
		  	}
		  	strcpy(lua_strconc_buffer,l);
		  	strcpy(lua_strconc_buffer+nl, r);
		  	return lua_strconc_buffer;
		}
		
		
		/*
		** Convert, if possible, to a number object.
		** Return 0 if success, not 0 if error.
		*/
		private static int lua_tonumber (Object_ obj)
		{
		 	float t = 0;
		 	char c = (char)0;
		 	if (tag(obj) != lua_Type.LUA_T_STRING)
		   		return 1;
		 	else if (sscanf(svalue(obj), "%f %c", t, c) == 1)
		 	{
		 		nvalue(obj, t);
		 		tag(obj, lua_Type.LUA_T_NUMBER);
		   		return 0;
		 	}
		 	else
		   		return 2;
		}
		
		
		/*
		** Convert, if possible, to a string tag
		** Return 0 in success or not 0 on error.
		*/
		private static CharPtr lua_tostring_s = new CharPtr(new char[256]);
		private static int lua_tostring (Object_ obj)
		{
		 	if (tag(obj) != lua_Type.LUA_T_NUMBER)
		   		return 1;
		 	if ((int) nvalue(obj) == nvalue(obj))
		   		sprintf (lua_tostring_s, "%d", (int) nvalue(obj));
		 	else
		   		sprintf (lua_tostring_s, "%g", nvalue(obj));
		 	tsvalue(obj, lua_createstring(lua_tostring_s));
		 	if (tsvalue(obj) == null)
		  		return 1;
		 	tag(obj, lua_Type.LUA_T_STRING);
		 	return 0;
		}
		
		
		/*
		** Adjust stack. Set top to the given value, pushing NILs if needed.
		*/
		private static void adjust_top (StkId newtop)
		{
			ObjectRef nt = new ObjectRef(stack, newtop);
			while (top.isLessThan(nt)) { tag(top.get(), lua_Type.LUA_T_NIL); top.inc(); }
			top = new ObjectRef(nt);  /* top could be bigger than newtop */
		}
		
		
		private static void adjustC (int nParams)
		{
		  	adjust_top(CBase+nParams);
		}
		
		
		/*
		** Call a C function. CBase will point to the top of the stack,
		** and CnResults is the number of parameters. Returns an index
		** to the first result from C.
		*/
		private static StkId callC (lua_CFunction func, StkId @base)
		{
		  	StkId oldBase = CBase;
		  	int oldCnResults = CnResults;
		  	StkId firstResult;
		  	CnResults = ObjectRef.minus(top, stack) - @base;
		  	/* incorporate parameters on the stack */
		  	CBase = @base+CnResults;
		  	func();
		  	firstResult = CBase;
		  	CBase = oldBase;
		  	CnResults = oldCnResults;
		  	return firstResult;
		}
		
		/*
		** Call the fallback for invalid functions (see do_call)
		*/
		private static void call_funcFB (Object_ func, StkId @base, int nResults, StkId whereRes)
		{
			StkId i;
		  	/* open space for first parameter (func) */
		  	for (i = ObjectRef.minus(top, stack); i > @base; i--)
		    	stack[i] = stack[i-1];
		  	top.inc();
		  	stack[@base].set(func);
		  	do_call(luaI_fallBacks[FB_FUNCTION].function, @base, nResults, whereRes);
		}
		
		
		/*
		** Call a function (C or Lua). The parameters must be on the stack,
		** between [stack+base,top). When returns, the results are on the stack,
		** between [stack+whereRes,top). The number of results is nResults, unless
		** nResults=MULT_RET.
		*/
		private static void do_call (Object_ func, StkId @base, int nResults, StkId whereRes)
		{
		  	StkId firstResult;
		  	if (tag(func) == lua_Type.LUA_T_CFUNCTION)
		    	firstResult = callC(fvalue(func), @base);
		  	else if (tag(func) == lua_Type.LUA_T_FUNCTION)
		    	firstResult = lua_execute(bvalue(func), @base);
		  	else
		  	{ /* func is not a function */
		    	call_funcFB(func, @base, nResults, whereRes);
		    	return;
		  	}
		  	/* adjust the number of results */
		  	if (nResults != MULT_RET && ObjectRef.minus(top, new ObjectRef(stack, firstResult)) != nResults)
		   		adjust_top(firstResult+nResults);
		  	/* move results to the given position */
		  	if (firstResult != whereRes)
		  	{
		    	int i;
		    	nResults = ObjectRef.minus(top, new ObjectRef(stack, firstResult));  /* actual number of results */
		    	for (i=0; i<nResults; i++)
		    		stack[whereRes+i].set(stack[firstResult+i]);
		    	top.add(-(firstResult-whereRes));
		  	}
		}
		
		
		/*
		** Function to index a table. Receives the table at top-2 and the index
		** at top-1.
		*/
		private static void pushsubscript ()
		{
			if (tag(top.get(-2)) != lua_Type.LUA_T_ARRAY)
		    	do_call(luaI_fallBacks[FB_GETTABLE].function, ObjectRef.minus(top, stack)-2, 1, ObjectRef.minus(top, stack)-2);
		  	else 
		  	{
		  		Object_ h = lua_hashget(avalue(top.get(-2)), top.get(-1));
		    	if (h == null || tag(h) == lua_Type.LUA_T_NIL)
		      		do_call(luaI_fallBacks[FB_INDEX].function, ObjectRef.minus(top, stack)-2, 1, ObjectRef.minus(top, stack)-2);
		    	else
		    	{
		    		top.dec();
		    		top.get(-1).set(h);
		    	}
		  	}
		}
		
		
		/*
		** Function to store indexed based on values at the top
		*/
		private static void storesubscript ()
		{
			if (tag(top.get(-3)) != lua_Type.LUA_T_ARRAY)
				do_call(luaI_fallBacks[FB_SETTABLE].function, ObjectRef.minus(top, stack)-3, 0, ObjectRef.minus(top, stack)-3);
		 	else
		 	{
		 		Object_ h = lua_hashdefine (avalue(top.get(-3)), top.get(-2));
		  		h.set(top.get(-1));
		  		top.add(-3);
		 	}
		}
		
		
		/*
		** Traverse all objects on stack
		*/
		//void (*fn)(Object *)
		public delegate void lua_travstack_fn(Object_ obj);
		private static void lua_travstack (lua_travstack_fn fn)
		{
			ObjectRef o;
			for (o = new ObjectRef(top, -1); o.isLargerEquals(stack); o.dec())
				fn (o.get());
		}
		
		
		/*
		** Execute a protected call. If function is null compiles the pre-set input.
		** Leave nResults on the stack.
		*/
		private static int do_protectedrun (Object_ function, int nResults)
		{
			jmp_buf myErrorJmp = new jmp_buf();
		  	int status;
		  	StkId oldCBase = CBase;
		  	jmp_buf oldErr = errorJmp;
		  	errorJmp = myErrorJmp;
		  	try //if (setjmp(myErrorJmp) == 0)
		  	{
		    	do_call(function, CBase, nResults, CBase);
		   		CnResults = ObjectRef.minus(top, stack) - CBase;  /* number of results */
		    	CBase += CnResults;  /* incorporate results on the stack */
		    	status = 0;
		  	}
		  	catch (LongjmpException) //else
		  	{
		    	CBase = oldCBase;
		    	top = new ObjectRef(stack, CBase);
		    	status = 1;
		  	}
		  	errorJmp = oldErr;
		  	return status;
		}
		
		
		private static int do_protectedmain ()
		{
		  	BytePtr code = null;
		  	int status;
		  	StkId oldCBase = CBase;
		  	jmp_buf myErrorJmp = new jmp_buf();
		  	jmp_buf oldErr = errorJmp;
		  	errorJmp = myErrorJmp;
		  	try //if (setjmp(myErrorJmp) == 0)
		  	{
		  		Object_ f = new Object_();
		    	lua_parse(ref code);
		    	tag(f, lua_Type.LUA_T_FUNCTION); bvalue(f, code);
		    	do_call(f, CBase, 0, CBase);
		    	status = 0;
		  	}
		  	catch (LongjmpException) { //else {
		    	status = 1;
		  	}
		  	if (code != null)
		    	luaI_free_BytePtr(ref code);
		  	errorJmp = oldErr;
		  	CBase = oldCBase;
		  	top = new ObjectRef(stack, CBase);
		  	return status;
		}
		
		
		/*
		** Execute the given lua function. Return 0 on success or 1 on error.
		*/
		public static int lua_callfunction (lua_Object function)
		{
		  	if (function == LUA_NOOBJECT)
		    	return 1;
		  	else
		    	return do_protectedrun (Address(function), MULT_RET);
		}
		
		
		public static int lua_call (CharPtr funcname)
		{
		 	Word n = luaI_findsymbolbyname(funcname);
			return do_protectedrun(s_object(n), MULT_RET);
		}
		
		
		/*
		** Open file, generate opcode and execute global statement. Return 0 on
		** success or 1 on error.
		*/
		public static int lua_dofile (CharPtr filename)
		{
			int status;
		  	CharPtr message = lua_openfile (filename);
		  	if (message != null)
		  	{
		    	lua_message(message);
		    	return 1;
		  	}
		  	status = do_protectedmain();
		  	lua_closefile();
		  	return status;
		}
		
		/*
		** Generate opcode stored on string and execute global statement. Return 0 on
		** success or 1 on error.
		*/
		public static int lua_dostring (CharPtr @string)
		{
		  	int status;
		  	CharPtr message = lua_openstring(@string);
		  	if (message != null)
		 	{
		    	lua_message(message);
		    	return 1;
		  	}
		  	status = do_protectedmain();
		  	lua_closestring();
		  	return status;
		}
		
		
		/*
		** API: set a function as a fallback
		*/
		private static Object_ lua_setfallback_func = new Object_(lua_Type.LUA_T_CFUNCTION, luaI_setfallback);
		public static lua_Object lua_setfallback (CharPtr name, lua_CFunction fallback, string name_)
		{
			adjustC(0);
		  	lua_pushstring(name);
		  	lua_pushcfunction(fallback, name_);
		  	do_protectedrun(lua_setfallback_func, 1);
		  	return (Ref(new ObjectRef(top, -1)));
		}
		
		
		/* 
		** API: receives on the stack the table and the index.
		** returns the value.
		*/
		public static lua_Object lua_getsubscript ()
		{
			adjustC(2);
		  	pushsubscript();
		  	CBase++;  /* incorporate object in the stack */
		  	return (Ref(new ObjectRef(top, -1)));
		}
		
		
		private const int MAX_C_BLOCKS = 10;
		
		private static int numCblocks = 0;
		private static StkId[] Cblocks = new StkId[MAX_C_BLOCKS];
		
		/*
		** API: starts a new block
		*/
		public static void lua_beginblock ()
		{
			if (numCblocks < MAX_C_BLOCKS)
		    	Cblocks[numCblocks] = CBase;
		  	numCblocks++;
		}
		
		/*
		** API: ends a block
		*/
		public static void lua_endblock ()
		{
		  	--numCblocks;
		  	if (numCblocks < MAX_C_BLOCKS)
		  	{
		    	CBase = Cblocks[numCblocks];
		    	adjustC(0);
		  	}
		}
		
		/* 
		** API: receives on the stack the table, the index, and the new value.
		*/
		public static void lua_storesubscript ()
		{
			adjustC(3);
		  	storesubscript();
		}
		
		/*
		** API: creates a new table
		*/
		public static lua_Object lua_createtable ()
		{
			adjustC(0);
			avalue(top.get(), lua_createarray(0));
			tag(top.get(), lua_Type.LUA_T_ARRAY);
			top.inc();
		  	CBase++;  /* incorporate object in the stack */
		  	return Ref(new ObjectRef(top, -1));
		}
		
		/*
		** Get a parameter, returning the object handle or LUA_NOOBJECT on error.
		** 'number' must be 1 to get the first parameter.
		*/
		public static lua_Object lua_getparam (int number)
		{
			if (number <= 0 || number > CnResults) return LUA_NOOBJECT;
		 	/* Ref(stack+(CBase-CnResults+number-1)) ==
		    stack+(CBase-CnResults+number-1)-stack+1 == */
		 	return (lua_Object)(CBase-CnResults+number);
		}
		
		/*
		** Given an object handle, return its number value. On error, return 0.0.
		*/
		public static real lua_getnumber (lua_Object @object)
		{
			if (@object == LUA_NOOBJECT || tag(Address(@object)) == lua_Type.LUA_T_NIL) return (real)0.0;
		 	if (tonumber (Address(@object))) return (real)0.0;
		 	else                   return (nvalue(Address(@object)));
		}
		
		/*
		** Given an object handle, return its string pointer. On error, return NULL.
		*/
		public static CharPtr lua_getstring (lua_Object @object)
		{
			if (@object == LUA_NOOBJECT || tag(Address(@object)) == lua_Type.LUA_T_NIL) return null;
		 	if (tostring (Address(@object))) return null;
		 	else return (svalue(Address(@object)));
		}
		
		/*
		** Given an object handle, return its cfuntion pointer. On error, return NULL.
		*/
		public static lua_CFunction lua_getcfunction (lua_Object @object)
		{
		 	if (@object == LUA_NOOBJECT || tag(Address(@object)) != lua_Type.LUA_T_CFUNCTION)
		   		return null;
		 	else return (fvalue(Address(@object)));
		}
		
		/*
		** Given an object handle, return its user data. On error, return NULL.
		*/
		public static object lua_getuserdata (lua_Object @object)
		{
			if (@object == LUA_NOOBJECT || tag(Address(@object)) < lua_Type.LUA_T_USERDATA)
		   		return null;
		 	else return (uvalue(Address(@object)));
		}
		
		
		public static lua_Object lua_getlocked (int @ref)
		{
			adjustC(0);
			top.get().set(luaI_getlocked(@ref));
		 	top.inc();
		 	CBase++;  /* incorporate object in the stack */
		 	return Ref(new ObjectRef(top, -1));
		}
		
		
		public static void lua_pushlocked (int @ref)
		{
			lua_checkstack(ObjectRef.minus(top, stack)+1);
			top.get().set(luaI_getlocked(@ref));
			top.inc();
		}
		
		
		public static int lua_lock ()
		{
			adjustC(1);
			top.dec();
			return luaI_lock(top.get());
		}
		
		
		/*
		** Get a global object. Return the object handle or NULL on error.
		*/
		public static lua_Object lua_getglobal (CharPtr name)
		{
			Word n = luaI_findsymbolbyname(name);
		 	adjustC(0);
		 	top.get().set(s_object(n));
		 	top.inc();
		 	CBase++;  /* incorporate object in the stack */
		 	return Ref(new ObjectRef(top, -1));
		}
		
		/*
		** Store top of the stack at a global variable array field.
		*/
		public static void lua_storeglobal (CharPtr name)
		{
			Word n = luaI_findsymbolbyname(name);
		 	adjustC(1);
		 	top.dec();
		 	s_object(n, top.get());
		}
		
		/*
		** Push a nil object
		*/
		public static void lua_pushnil ()
		{
			lua_checkstack(ObjectRef.minus(top, stack)+1);
			tag(top.get(), lua_Type.LUA_T_NIL); top.inc();
		}
		
		/*
		** Push an object (tag=number) to stack.
		*/
		public static void lua_pushnumber (real n)
		{
			lua_checkstack(ObjectRef.minus(top, stack)+1);
			tag(top.get(), lua_Type.LUA_T_NUMBER); nvalue(top.get(), n); top.inc();
		}
		
		/*
		** Push an object (tag=string) to stack.
		*/
		public static void lua_pushstring (CharPtr s)
		{
			lua_checkstack(ObjectRef.minus(top, stack)+1);
			tsvalue(top.get(), lua_createstring(s));
			tag(top.get(), lua_Type.LUA_T_STRING);
			top.inc();
		}
		
		/*
		** Push an object (tag=string) on stack and register it on the constant table.
		*/
		public static void lua_pushliteral (CharPtr s)
		{
			lua_checkstack(ObjectRef.minus(top, stack)+1);
			tsvalue(top.get(), lua_constant[luaI_findconstant(lua_constcreate(s))]);
			tag(top.get(), lua_Type.LUA_T_STRING);
			top.inc();
		}
		
		/*
		** Push an object (tag=cfunction) to stack.
		*/
		public static void lua_pushcfunction (lua_CFunction fn, string name)
		{
			lua_checkstack(ObjectRef.minus(top, stack)+1);
			tag(top.get(), lua_Type.LUA_T_CFUNCTION); fvalue(top.get(), fn, name); top.inc();
		}
		
		/*
		** Push an object (tag=userdata) to stack.
		*/
		public static void lua_pushusertag (object u, int tag_)
		{
			if (tag_ < (int)lua_Type.LUA_T_USERDATA) return;
			lua_checkstack(ObjectRef.minus(top, stack)+1);
			tag(top.get(), (lua_Type)tag_); uvalue(top.get(), u); top.inc();
		}
		
		/*
		** Push a lua_Object to stack.
		*/
		public static void lua_pushobject (lua_Object o)
		{
			lua_checkstack(ObjectRef.minus(top, stack)+1);
			top.get().set(Address(o)); top.inc();
		}
		
		/*
		** Push an object on the stack.
		*/
		public static void luaI_pushobject (Object_ o)
		{
			lua_checkstack(ObjectRef.minus(top, stack)+1);
			top.get().set(o); top.inc();
		}
		
		public static int lua_type (lua_Object o)
		{
			if (o == LUA_NOOBJECT)
				return (int)lua_Type.LUA_T_NIL;
		  	else
		  		return (int)tag(Address(o));
		}
		
		
		public static void luaI_gcFB (Object_ o)
		{
			top.get().set(o); top.inc();
		  	do_call(luaI_fallBacks[FB_GC].function, ObjectRef.minus(top, stack)-1, 0, ObjectRef.minus(top, stack)-1);
		}
		
		
		private static void call_arith (CharPtr op)
		{
		  	lua_pushstring(op);
		  	do_call(luaI_fallBacks[FB_ARITH].function, ObjectRef.minus(top, stack)-3, 1, ObjectRef.minus(top, stack)-3);
		}
		
		private static void comparison (lua_Type tag_less, lua_Type tag_equal, 
		                        lua_Type tag_great, CharPtr op)
		{
			Object_ l = top.get(-2);
			Object_ r = top.get(-1);
		  	int result;
		  	if (tag(l) == lua_Type.LUA_T_NUMBER && tag(r) == lua_Type.LUA_T_NUMBER)
		    	result = (nvalue(l) < nvalue(r)) ? -1 : (nvalue(l) == nvalue(r)) ? 0 : 1;
		  	else if (tostring(l) || tostring(r))
		  	{
		    	lua_pushstring(op);
		    	do_call(luaI_fallBacks[FB_ORDER].function, ObjectRef.minus(top, stack)-3, 1, ObjectRef.minus(top, stack)-3);
		    	return;
		  	}
		  	else
		    	result = strcmp(svalue(l), svalue(r));
		  	top.dec();
		  	nvalue(top.get(-1), 1);
		  	tag(top.get(-1), (result < 0) ? tag_less : ((result == 0) ? tag_equal : tag_great));
		}
		
		
		
		/*
		** Execute the given opcode, until a RET. Parameters are between
		** [stack+base,top). Returns n such that the the results are between
		** [stack+n,top).
		*/
		private static StkId lua_execute (BytePtr pc, StkId @base)
		{
			//pc = new BytePtr(pc);
		 	lua_checkstack(STACKGAP+MAX_TEMPS+@base);
		 	while (true)
		 	{
#if MY_DEBUG
		 		//printf(">>> %d,", ObjectRef.minus(top, stack));
#endif
		 		OpCode opcode;
		  		opcode = (OpCode)pc[0]; pc.inc();
		  		switch (opcode)
		  		{
		  			case OpCode.PUSHNIL: tag(top.get(), lua_Type.LUA_T_NIL); top.inc(); break;
				
				   	case OpCode.PUSH0: case OpCode.PUSH1: case OpCode.PUSH2:
		  				tag(top.get(), lua_Type.LUA_T_NUMBER);
		  				nvalue(top.get(), opcode-OpCode.PUSH0); top.inc();
				     	break;
				
				    case OpCode.PUSHBYTE: tag(top.get(), lua_Type.LUA_T_NUMBER); nvalue(top.get(), pc[0]); top.inc(); pc.inc(); break;
				
				   	case OpCode.PUSHWORD:
				   		{
				    		CodeWord code = new CodeWord();
						    get_word(code,pc);
						    tag(top.get(), lua_Type.LUA_T_NUMBER); nvalue(top.get(), code.w); top.inc();
						}
						break;
				
				   	case OpCode.PUSHFLOAT:
				   		{
							CodeFloat code = new CodeFloat();
						    get_float(code,pc);
						    tag(top.get(), lua_Type.LUA_T_NUMBER); nvalue(top.get(), code.f); top.inc();
						}
						break;
				
				   	case OpCode.PUSHSTRING:
				   		{
							CodeWord code = new CodeWord();
						    get_word(code,pc);
						    tag(top.get(), lua_Type.LUA_T_STRING); tsvalue(top.get(), lua_constant[code.w]); top.inc();
						}
						break;
				
				   	case OpCode.PUSHFUNCTION:
				   		{
							CodeCode code = new CodeCode();
				    		get_code(code,pc);
				    		tag(top.get(), lua_Type.LUA_T_FUNCTION); bvalue(top.get(), new BytePtr(code.b, 0)); top.inc();
				   		}
				   		break;
				
				   	case OpCode.PUSHLOCAL0: case OpCode.PUSHLOCAL1: case OpCode.PUSHLOCAL2:
				   	case OpCode.PUSHLOCAL3: case OpCode.PUSHLOCAL4: case OpCode.PUSHLOCAL5:
				   	case OpCode.PUSHLOCAL6: case OpCode.PUSHLOCAL7: case OpCode.PUSHLOCAL8:
				   	case OpCode.PUSHLOCAL9: top.get().set(stack[(@base) + (int)(opcode-OpCode.PUSHLOCAL0)]); top.inc(); break;
				
				   	case OpCode.PUSHLOCAL: top.get().set(stack[(@base) + pc[0]]); top.inc(); pc.inc(); break;
				
				   	case OpCode.PUSHGLOBAL:
				   		{
				   			CodeWord code = new CodeWord();
						    get_word(code,pc);
						    top.get().set(s_object(code.w)); top.inc();
						}
						break;
				
				   	case OpCode.PUSHINDEXED:
				   		pushsubscript();
				    	break;
				
				   	case OpCode.PUSHSELF:
				   		{
							Object_ receiver = top.get(-1);
							CodeWord code = new CodeWord();
						    get_word(code,pc);
						    tag(top.get(), lua_Type.LUA_T_STRING); tsvalue(top.get(), lua_constant[code.w]); top.inc();
						    pushsubscript();
						    top.get().set(receiver); top.inc();
						    break;
						}
				
				   	case OpCode.STORELOCAL0: case OpCode.STORELOCAL1: case OpCode.STORELOCAL2:
				   	case OpCode.STORELOCAL3: case OpCode.STORELOCAL4: case OpCode.STORELOCAL5:
				   	case OpCode.STORELOCAL6: case OpCode.STORELOCAL7: case OpCode.STORELOCAL8:
				   	case OpCode.STORELOCAL9:
				    	top.dec();
				    	stack[(@base) + (int)(opcode-OpCode.STORELOCAL0)].set(top.get());
				     	break;
				
				    case OpCode.STORELOCAL: top.dec(); stack[(@base) + pc[0]].set(top.get()); pc.inc(); break;
				
				   	case OpCode.STOREGLOBAL:
				   		{
				    		CodeWord code = new CodeWord();
						    get_word(code,pc);
						    top.dec();
						    s_object(code.w, top.get(0));
						}
						break;
				
				   	case OpCode.STOREINDEXED0:
				    	storesubscript();
				    	break;
				
				   	case OpCode.STOREINDEXED:
				   		{
				    		int n = pc[0]; pc.inc();
				    		if (tag(top.get(-3-n)) != lua_Type.LUA_T_ARRAY)
						    {
				    			top.get(+1).set(top.get(-1));
				    			top.get().set(top.get(-2-n));
				    			top.get(-1).set(top.get(-3-n));
						      	top.add(2);
						      	do_call(luaI_fallBacks[FB_SETTABLE].function, ObjectRef.minus(top, stack)-3, 0, ObjectRef.minus(top, stack)-3);
						    }
						    else
						    {
						    	Object_ h = lua_hashdefine (avalue(top.get(-3-n)), top.get(-2-n));
						    	h.set(top.get(-1));
						    	top.dec();
						    }
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
						    while (n!=0)
						    {
						    	tag(top.get(), lua_Type.LUA_T_NUMBER); nvalue(top.get(), n+m);
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
						    while (n!=0)
						    {
						    	CodeWord code = new CodeWord();
						     	get_word(code,pc);
						     	tag(top.get(), lua_Type.LUA_T_STRING); tsvalue(top.get(), lua_constant[code.w]);
						     	lua_hashdefine (avalue(arr), top.get()).set(top.get(-1));
						     	top.dec();
						     	n--;
						    }
						}
						break;
				
				   	case OpCode.ADJUST0:
				    	adjust_top(@base);
				     	break;
				
				   	case OpCode.ADJUST:
				     	adjust_top(@base + pc[0]); pc.inc();
				     	break;
				
				   	case OpCode.CREATEARRAY:
				   		{
				     		CodeWord size = new CodeWord();
						    get_word(size,pc);
						    avalue(top.get(), lua_createarray(size.w));
						    tag(top.get(), lua_Type.LUA_T_ARRAY);
						    top.inc();
						}
						break;
				
				   	case OpCode.EQOP:
				   		{
							int res = lua_equalObj(top.get(-2), top.get(-1));
						    top.dec();
						    tag(top.get(-1), res!=0 ? lua_Type.LUA_T_NUMBER : lua_Type.LUA_T_NIL);
						    nvalue(top.get(-1), 1);
						}
						break;
				
				    case OpCode.LTOP:
				      	comparison(lua_Type.LUA_T_NUMBER, lua_Type.LUA_T_NIL, lua_Type.LUA_T_NIL, "lt");
				      	break;
				
				   	case OpCode.LEOP:
				    	comparison(lua_Type.LUA_T_NUMBER, lua_Type.LUA_T_NUMBER, lua_Type.LUA_T_NIL, "le");
				      	break;
				
				   	case OpCode.GTOP:
				      	comparison(lua_Type.LUA_T_NIL, lua_Type.LUA_T_NIL, lua_Type.LUA_T_NUMBER, "gt");
				      	break;
				
				   	case OpCode.GEOP:
				    	comparison(lua_Type.LUA_T_NIL, lua_Type.LUA_T_NUMBER, lua_Type.LUA_T_NUMBER, "ge");
				      	break;
				
				   	case OpCode.ADDOP:
					   	{
				      		Object_ l = top.get(-2);
				      		Object_ r = top.get(-1);
					    	if (tonumber(r) || tonumber(l))
					      		call_arith("add");
					    	else
					    	{
					    		nvalue(l, nvalue(l) + nvalue(r));
					    		top.dec();
					    	}
					   	}
					   	break;
				
				   	case OpCode.SUBOP:
						{
					   		Object_ l = top.get(-2);
					   		Object_ r = top.get(-1);
						    if (tonumber(r) || tonumber(l))
						    	call_arith("sub");
						    else
						    {
						    	nvalue(l, nvalue(l) - nvalue(r));
						    	top.dec();
						    }
						}
						break;
				
				   	case OpCode.MULTOP:
				   		{
							Object_ l = top.get(-2);
							Object_ r = top.get(-1);
						    if (tonumber(r) || tonumber(l))
						    	call_arith("mul");
						    else
						    {
						    	nvalue(l, nvalue(l) * nvalue(r));
						      	top.dec();
						    }
						}
						break;
				
				   	case OpCode.DIVOP:
				  		{
							Object_ l = top.get(-2);
							Object_ r = top.get(-1);
					    	if (tonumber(r) || tonumber(l))
					      		call_arith("div");
					    	else
					    	{
					    		nvalue(l, nvalue(l) / nvalue(r));
					    		top.dec();
					    	}
					   	}
					  	break;
				
				   	case OpCode.POWOP:
				    	call_arith("pow");
				    	break;
				
				   	case OpCode.CONCOP:
				   		{
				    		Object_ l = top.get(-2);
				    		Object_ r = top.get(-1);
				    		if (tostring(r) || tostring(l))
				      			do_call(luaI_fallBacks[FB_CONCAT].function, ObjectRef.minus(top, stack)-2, 1, ObjectRef.minus(top, stack)-2);
				    		else
				    		{
				    			tsvalue(l, lua_createstring (lua_strconc(svalue(l),svalue(r))));
				      			top.dec();
				    		}
				   		}
				   		break;
				
				   	case OpCode.MINUSOP:
				   		if (tonumber(top.get(-1)))
					    {
					    	tag(top.get(), lua_Type.LUA_T_NIL); top.inc();
					      	call_arith("unm");
					    }
					    else
					    	nvalue(top.get(-1), - nvalue(top.get(-1)));
				   		break;
				
				   	case OpCode.NOTOP:
				   		tag(top.get(-1), (tag(top.get(-1)) == lua_Type.LUA_T_NIL) ? lua_Type.LUA_T_NUMBER : lua_Type.LUA_T_NIL);
				   		nvalue(top.get(-1), 1);
				   		break;
				
				   	case OpCode.ONTJMP:
				   		{
				   			CodeWord code = new CodeWord();
				    		get_word(code,pc);
				    		if (tag(top.get(-1)) != lua_Type.LUA_T_NIL) pc += code.w;
				   		}
				   		break;
				
				   	case OpCode.ONFJMP:	
					   	{
				   			CodeWord code = new CodeWord();
					    	get_word(code,pc);
					    	if (tag(top.get(-1)) == lua_Type.LUA_T_NIL) pc += code.w;
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
					    	if (tag(top.get()) == lua_Type.LUA_T_NIL) pc += code.w;
					   	}
					   	break;
				
				   	case OpCode.IFFUPJMP:
					   	{
					   		CodeWord code = new CodeWord();
					   		get_word(code,pc);
					   		top.dec();
					   		if (tag(top.get()) == lua_Type.LUA_T_NIL) pc -= code.w;
					   	}
					   	break;
				
					case OpCode.POP: top.dec(); break;
				
				   	case OpCode.CALLFUNC:
						{
							int nParams = pc[0]; pc.inc();
							int nResults = pc[0]; pc.inc();
							Object_ func = top.get(-1-nParams); /* function is below parameters */
					     	StkId newBase = ObjectRef.minus(top, stack)-nParams;
					     	do_call(func, newBase, nResults, newBase-1);
					   	}
				   		break;
				
				   	case OpCode.RETCODE0:
				   		return @base;
				
				   	case OpCode.RETCODE:
				   		return @base+pc[0];
				
				   	case OpCode.SETFUNCTION:
				   		{
				    		CodeCode file = new CodeCode();
				    		CodeWord func = new CodeWord();
						    get_code(file,pc);
						    get_word(func,pc);
						    lua_pushfunction (new CharPtr(file.b), func.w);
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
				    	lua_error ("internal error - opcode doesn't match");
				    	break;
				}
		 	}
		}
	}
}


debug code:
<1> PrintCode
<2> func begin
<3> >>>>>

------------------------
(1) %c

public static int putc(int ch, FILE fp)
		{
			//throw new NotImplementedException();
			//return 0;
			if (fp == null)
			{
				//FIXME:
------>				printf("%c", (char)ch);
			}
			return ch;
		}


				int i__ = input(); yytext[0] = (char)i__; unchecked { yyprevious = (sbyte)i__; }
breakpoint------>				if (yyprevious>0)
------>					output((char)yyprevious);
				yylastch=yytext;



------------------------

(2) debug yyt value

yywork[] yycrank
yyt - yycrank





c#: yyt.index
------
					else if(yyt.isLessThan(yycrank)) {		/* r < yycrank */
						yyr = new yyworkRef(yycrank, (-yyt.minus(yycrank))); yyt = new yyworkRef(yyr);
#if LEXDEBUG
						if(debug!=0)fprintf(yyout,"compressed state\n");
#endif
						yyt = yyt.getRef(yych);
1->						if(yyt.isLessEqualThan(yytop) && new yysvfRef(yyt.get().verify, yysvec).isEquals(yystate)){
							if(new yysvfRef(yyt.get().advance, yysvec).isEquals(YYLERR()))	/* error transitions */
------
			for(;;){
				lsp = new yysvfRef(yylstate, 0);
				yyestate = new yysvfArr(yybgin); yystate = new yysvfArr(yybgin);
				if (yyprevious==YYNEWLINE) yystate.inc();
				for (;;){fprintf(stdout,"state %d\n",yystate.minus(yysvec)-1);
#if LEXDEBUG
				if(debug!=0)fprintf(yyout,"state %d\n",yystate.minus(yysvec)-1);
#endif
					yyt = new yyworkRef(yystate.get().yystoff);
2->					if(yyt.isEquals(yycrank) && yyfirst==0){  /* may not be any transitions */
------
3->				if (yyprevious>0)
					output((char)yyprevious);
------

====>
C: yyt-yycrank or yycrank-yyt(see minus value, for example 3 is -3)
-----
	yyt = yystate->yystoff;
1->			if(yyt == yycrank && !yyfirst){  /* may not be any transitions */
----
# ifdef LEXDEBUG
				if(debug)fprintf(yyout,"compressed state\n");
# endif
				yyt = yyt + yych;
2->				if(yyt <= yytop && yyt->verify+yysvec == yystate){
----
		yyprevious = yytext[0] = input();
3->		if (yyprevious>0)
			output(yyprevious);
----


----------------------------------------------------------------------------------


(3) see state

C#--->				for (;;){fprintf(stdout,"state %d\n",yystate.minus(yysvec)-1);
#if LEXDEBUG


C--->		for (;;){fprintf(stdout,"state %d\n",yystate-yysvec-1);
# ifdef LEXDEBUG

(4) yytext

case 27:
			case 28:
		      {
C bp--->				       yylval.vWord = lua_findenclosedconstant (yytext);
				       return STRING;
				      }
				     

lua_findenclosedconstant
		public static int lua_findenclosedconstant(CharPtr s)
		{
C# breakpoint ->			int i, j, l = (int)strlen(s);



--------------------------------

(5) CALLFUNC: print not found

----------------------------------------------------

(6) vc6 remove //#line //# line
can't put breakpoint

(7) 
sizeof(void *) == 4

(8) 
search Console.WriteLine("================");

(9)
 			yyfirst=1;
 			if (yymorfg==0)
-				yylastch = yytext;
+				yylastch = new CharPtr(yytext);
 			else {
 				yymorfg=0;
-				yylastch = yytext+yyleng;
+				yylastch = new CharPtr(yytext.chars, yytext.index + yyleng);
 			}
 			for(;;
 
 
(10)
  		private static CharPtr yyerror_msg = new CharPtr(new char[256]);
-		public static void yyerror(string s)
+		public static void yyerror(CharPtr s)
 		{
 			//static char msg[256];
+			string lasttext = lua_lasttext ().ToString();
+			lasttext = lasttext.Replace("\r", "\\r");
 			sprintf (yyerror_msg,"%s near \"%s\" at line %d in file \"%s\"",
-			      s, lua_lasttext (), lua_linenumber, lua_filename());
+			         s.ToString(), lasttext, lua_linenumber, lua_filename());
+//			Console.WriteLine("===" + yyerror_msg.ToString());
 			lua_error (yyerror_msg);
 			err = 1;
 		}
 
 
 (11)
  int yylineno =1;
 # define YYU(x) x
 # define NLSTATE yyprevious=YYNEWLINE
-char yytext[YYLMAX];
+char yytext[YYLMAX];const char *yytext_buffer = yytext;

(12) maincode-code == 4356 see align_n: pc+1-code
		private static void align_n (uint size)
		{
		 	if (size > ALIGNMENT) size = ALIGNMENT;
		 	while (((pc+1-code)%size) != 0) // +1 to include BYTECODE
		 		code_byte ((byte)OpCode.NOP);
		}
		
patch--->

 			public BytePtr()
 			{
@@ -109,7 +109,14 @@ namespace KopiLua
 //				return new CharPtr(result);
 //			}
 			public static int operator -(BytePtr ptr1, BytePtr ptr2) {
-				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index - ptr2.index; }
+				//maincode-code == 4356
+				if (ptr1.chars == maincode.chars && ptr2.chars == code.chars)
+				{
+					int result = ptr1.index - ptr2.index + (1024 * 4 + 256 + 4);
+					return result;
+				}
+				Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index - ptr2.index;
+			}


(13)
 				{
 					this.yyother = null;
 				}
-				this.yystops = new IntegerPtr(yyref.yystops);
+				if (yyref.yystops != null)
+				{
+					this.yystops = new IntegerPtr(yyref.yystops);
+				}
+				else
+				{
+					this.yystops = null;
+				}
 			}
 
 (14)
  			case 2:
 				//#line 179 "lua.stx"
 				{
-					pc = basepc = maincode;
+					basepc = new BytePtr(maincode); pc = new BytePtr(basepc);
 					nlocalvar = 0;
 				}
 				break;
@@ -1278,7 +1277,7 @@ yydefault:
 			case 6:
 				//#line 184 "lua.stx"
 				{
-					pc = basepc = code;
+					basepc = new BytePtr(code); pc = new BytePtr(basepc);
 					nlocalvar = 0;
 				}
 				break;
 
 
 (15) index overflow
 
 			public yywork get()
			{
				if (this.index >= 0 && this.index < this.arr.Length)
				{
					return this.arr[this.index];
				}
				else
				{
--------->					return new yywork(0, 0);
				}
			}
	
			public void set(yysvfRef v)
			{
				if (arr[index + 0] == null)
				{
--------->					arr[index + 0] = new yysvf(null, null, null);
					arr[index + 0].set(v.get());
				}
				else
				{
					arr[index + 0].set(v.get());
				}
			}
			
(16) sharpdevelop char[] compare always return false


(17) b_   ----> stack
stack[1] point to "print" cfunction

			   	case OpCode.CALLFUNC:
			   		{
						BytePtr newpc;
---->						ObjectRef b_ = top.getRef(-1);
						while (tag(b_.get()) != Type.T_MARK) b_.dec();
						if (b_.obj == stack)
						{
------->							Console.WriteLine("================");
						}
						if (tag(b_.get(-1)) == Type.T_FUNCTION)
						{
				 			lua_debugline = 0;			/* always reset debug flag */
				 			newpc = bvalue(b_.get(-1));
				 			bvalue(b_.get(-1), pc);		        /* store return code */
				 			nvalue(b_.get(), @base.minus(stack));		/* store base value */
				 			@base = b_.getRef(+1);
				 			pc = newpc;
				 			if (MAXSTACK-@base.minus(stack) < STACKGAP)
				 			{
				  				lua_error ("stack overflow");
				  				return 1;
				 			}
						}
						
---------------------------------------------

(18)
VC6: maincode ---> can chaged, use this:
((Byte *)mainbuffer)[0]


-----------------------------------

(19) print yyval.vWord == 4 (compile/parse-time calculate, not runtime)

		public static int lua_findsymbol(CharPtr s)
		{
			if (s.ToString().Equals("print"))
		    {
----------->				Console.WriteLine("====================");
		    }
		    
		    
(20) check yytext.index changed

			public bool checkChange = false;
			public char[] chars;
			private int _index;
			public int index
			{
				get
				{
					return _index;
				}
				set
				{
					if (checkChange)
					{
----------->						Console.WriteLine("====================");
					}
					_index = value;
				}
			}

(21) merge buffer_mainbuffer_

#if false
		//private static long[] buffer = new long[MAXCODE];
		private static byte[] buffer_ = new byte[MAXCODE * 4];
		private static BytePtr code = new BytePtr(buffer_);
		//private static long[] mainbuffer = new long[MAXCODE];
		private static byte[] mainbuffer_ = new byte[MAXCODE * 4];
		private static BytePtr maincode = new BytePtr(mainbuffer_);
#else
		private const int BUFFER_SPACE_INNER = 256 + 4;
		private static byte[] buffer_mainbuffer_ = new byte[MAXCODE * 4 + BUFFER_SPACE_INNER + MAXCODE * 4];
		private static BytePtr code = new BytePtr(buffer_mainbuffer_, 0);
		private static BytePtr maincode = new BytePtr(buffer_mainbuffer_, 0 + MAXCODE * 4 + BUFFER_SPACE_INNER);
#endif



(22) maincode don't effect pc 

maincode = pc;
====>
maincode = new BytePtr(pc.chars, pc.index);

(23) PrintCode
public static int lua_parse()
		{
			BytePtr initcode = new BytePtr(maincode);
		 	err = 0;
		 	if (yyparse() != 0 || (err == 1)) return 1;
		 	maincode[0] = (byte)OpCode.HALT; maincode.inc();
--->		 	//PrintCode();
		 	if (lua_execute(initcode) != 0) return 1;
		 	maincode = new BytePtr(initcode.chars, initcode.index);
		 	return 0;
		}
---------------------

(24) incr_nvarbuffer()
nvarbuffer++

					nvarbuffer = 0;
					varbuffer[nvarbuffer] = (byte)yypvt[-0].vLong; incr_nvarbuffer();
					yyval.vInt = (yypvt[-0].vLong == 0) ? 1 : 0;

varbuffer[i]=33
varbuffer[i]=34

varbuffer[nvarbuffer] from vLong
vLong from here->vWord ->(33, 34)

					int local = lua_localname(yypvt[-0].vWord);
					if (local == -1)	/* global var */
						yyval.vLong = yypvt[-0].vWord + 1;		/* return positive value */
					else
						yyval.vLong = -(local + 1);		/* return negative value */
						
vWord from here->lua_findsymbol

				case 33:
					{
						yylval.vWord = (Word)lua_findsymbol(yytext);
						return NAME;

in lua_findsymbol:
bug fix:
s_name(lua_ntable, s);
->		 	
s_name(lua_ntable, strdup(s));

because: string s (is yytext) will be changed by lexer, must be cloned

--------------------------
(25) push int -> push float

		private static void code_number (float f)
		{
			int i = (int)f; //BitConverter.ToInt32(BitConverter.GetBytes(f), 0);

-----------------------

(26) if(...) {}
case 17:
//# line 244 "lua.stx"
{
        *(yypvt[-3].pByte) = IFFJMP;
        *((Word *)(yypvt[-3].pByte+1)) = pc - (yypvt[-3].pByte + sizeof(Word)+1);
        
        *(yypvt[-1].pByte) = UPJMP;
        *((Word *)(yypvt[-1].pByte+1)) = pc - yypvt[-6].pByte;
       } break;

IFFJMP 4
NOP
UPJMP 14


????excute 2 times code(NOP) before (see align_n)

VC6:cond breakpoint:
code[4373] == 55

=====================
input lua file:
--print("Hello, world!")
--print("abcd1234")
--a = @()
--a[2] = 3
--print ("a[".."] = ")


a = @()
i=0
while i<10 do
-- a[i] = i*i
-- i=i+1
end
=====================


break here: (write 0 first to a byte and write again IFFJMP in that byte) 
case 17:
//# line 244 "lua.stx"
{
---->        *(yypvt[-3].pByte) = IFFJMP;
        *((Word *)(yypvt[-3].pByte+1)) = pc - (yypvt[-3].pByte + sizeof(Word)+1);
 

pByte not correct (changed), point to pc (in code[])

C# bug fix:
.pByte = pc;
=>
.pByte = new BytePtr(pc);




note (how IFFJMP written):
					align(2);
					yyval.pByte = new BytePtr(pc);
put zero(NOP)--->					code_byte(0);		/* open space */
put zero arg---->					code_word (0);
					

{
replace zero(NOP) with IFFJMP ---->        *(yypvt[-3].pByte) = IFFJMP;
replace zero arg with a number---->        *((Word *)(yypvt[-3].pByte+1)) = pc - (yypvt[-3].pByte + sizeof(Word)+1);
        

--------------------------------------------------------------------------

(27) r,v=next(a,nil) r.tag == T_MASK, should be T_NUMBER

====================================
lua code:
--print("Hello, world!")
--print("abcd1234")
--a = @()
--a[2] = 3
--print ("a[".."] = ")


a = @()
i=0
while i<10 do
 a[i] = i*i
 i=i+1
end

r,v = next(a,nil)
while r ~= nil do
  print ("array["..r.."] = "..v)
--r,v = next(a,r)
end 
====================================

   case CONCOP:
   {
    Object *l = top-2;
    Object *r = top-1;
    if (tostring(r) || tostring(l))
     return 1;
    svalue(l) = lua_createstring (lua_strconc(svalue(l),svalue(r)));
    if (svalue(l) == NULL)
     return 1;
    --top;
   }
   break; 
   
   
 VC6: 
 l-stack   3
 r-stack   4
 
 top == stack+5, top-2==stack+3, top-1==stack+4
 
 VC6:
 breakpoint 
 stack[4].tag == 2 (==> r.tag == T_NUMBER)
 
 
 
 
   case PUSHGLOBAL: 
break here------>    *top++ = s_object(*((Word *)(pc))); pc += sizeof(Word);
   break;


lua_table[34].object

lua_table, stack


		private static void firstnode (Hash a, int h)
		{
			if (h < nhash(a))
			{
			  	int i;
			  	for (i=h; i<nhash(a); i++)
			  	{
			  		if (list(a,i) != null && tag(list(a,i).val) != Type.T_NIL)
			   		{
bug here ----->						lua_pushobject (list(a,i).@ref);
						lua_pushobject (list(a,i).val);
						return;
			   		}
			  	}
			 }
			 lua_pushnil();
			 lua_pushnil();
		}
		
a.list[i].@ref.tag == T_MASK (should be T_NUMBER)

C# bug fix:
.@ref = xxx
=>
.@ref.set(xxx)

.val = xxx
=>
.val.set(xxx)

---------------------------------------------------
(28)
//		 			if (_tag == Type.T_NUMBER && value == Type.T_MARK)
//		 			{
//		 				Console.WriteLine("================");
//		 			}

========================
lua code:
--print("Hello, world!")
--print("abcd1234")
--a = @()
--a[2] = 3
--print ("a[".."] = ")


a = @()
i=0
while i<10 do
 a[i] = i*i
 i=i+1
end

r,v = next(a,nil)
while r ~= nil do
  print ("array["..r.."] = "..v)
  r,v = next(a,r)
end 
========================


the second next(a,r) don't be executed to firstnode (a, h+1);

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
----------->				  				firstnode (a, h+1);
				  				return;
				 			}
				 			else if (tag(n.get().next.get().val) != Type.T_NIL)
				 			{
				  				lua_pushobject (n.get().next.get().@ref);
				  				lua_pushobject (n.get().next.get().val);
				  				return;
				 			}
				 			


bug fix:
remove new Node, so 'n.get().next == null' can be true

		private static Node[] newvector_Node(uint n)
		{
			Node[] ret = new Node[n]; 
//			for (int i = 0; i < n; ++i)
//				ret[i] = new Node();
			return ret;
		}

-------------------------------
(29) NodeRef can opt


--------------------------------

(30) 
varbuffer : long[], can be minus


(31) pc not only beside code, may point to a calloc memory in symbol table

//# line 197 "lua.stx"
{ 
                if (lua_debug) code_byte(RESET); 
	        code_byte(RETCODE); code_byte(nlocalvar);
	        s_tag(yypvt[-7].vWord) = T_FUNCTION;
save to symbol table(lua_table) ------------>	        s_bvalue(yypvt[-7].vWord) = calloc (pc-code, sizeof(Byte));
	        memcpy (s_bvalue(yypvt[-7].vWord), code, (pc-code)*sizeof(Byte));
	       } break;

like this:
breakpoint ---->   case CALLFUNC:
pc-code	4452
pc-code	2203453


(32) Copy ptr
		public static void svalue(Object_ o, CharPtr ptr) { o.value.s = ptr; }
		public static void bvalue(Object_ o, BytePtr b) { o.value.b = b; }
-->
		public static CharPtr svalue(Object_ o) { return o.value.s != null ? new CharPtr(o.value.s) : null; }
		public static void svalue(Object_ o, CharPtr ptr) { o.value.s = (ptr != null ? new CharPtr(ptr) : null); }
		public static BytePtr bvalue(Object_ o) { return o.value.b != null ? new BytePtr(o.value.b) : null; }
		public static void bvalue(Object_ o, BytePtr b) { o.value.b = (b != null ? new BytePtr(b) : null); }


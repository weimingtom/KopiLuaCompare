namespace KopiLua
{
	using Instruction = System.UInt32;
	
	public partial class Lua
	{
		/*===========================================================================
		  We assume that instructions are unsigned numbers.
		  All instructions have an opcode in the first 6 bits.
		  Instructions can have the following fields:
			`A' : 8 bits
			`B' : 9 bits
			`C' : 9 bits
			`Bx' : 18 bits (`B' and `C' together)
			`sBx' : signed Bx

		  A signed argument is represented in excess K; that is, the number
		  value is the unsigned value minus K. K is exactly the maximum value
		  for that argument (so that -max is represented by 0, and +max is
		  represented by 2*max), which is half the maximum for the corresponding
		  unsigned argument.
		===========================================================================*/


		public enum OpMode {iABC, iABx, iAsBx, iAx};  /* basic instruction format */


		/*
		** size and position of opcode arguments.
		*/
		public const int SIZE_C		= 9;
		public const int SIZE_B		= 9;
		public const int SIZE_Bx	= (SIZE_C + SIZE_B);
		public const int SIZE_A		= 8;
        public const int SIZE_Ax	= (SIZE_C + SIZE_B + SIZE_A);

		public const int SIZE_OP	= 6;

		public const int POS_OP		= 0;
		public const int POS_A		= (POS_OP + SIZE_OP);
		public const int POS_C		= (POS_A + SIZE_A);
		public const int POS_B		= (POS_C + SIZE_C);
		public const int POS_Bx		= POS_C;
        public const int POS_Ax		= POS_A;


		/*
		** limits for opcode arguments.
		** we use (signed) int to manipulate most arguments,
		** so they must fit in LUAI_BITSINT-1 bits (-1 for sign)
		*/
		//#if SIZE_Bx < LUAI_BITSINT-1
		public const int MAXARG_Bx         = ((1<<SIZE_Bx)-1);
		public const int MAXARG_sBx        = (MAXARG_Bx>>1);         /* `sBx' is signed */
		//#else
		//public const int MAXARG_Bx			= System.Int32.MaxValue;
		//public const int MAXARG_sBx			= System.Int32.MaxValue;
		//#endif

        //FIXME:???
		//#if SIZE_Ax < LUAI_BITSINT-1
		public const int MAXARG_Ax	= ((1<<SIZE_Ax)-1);
		//#else
		//public const int MAXARG_Ax	= MAX_INT;
		//#endif


		public const uint MAXARG_A        = (uint)((1 << (int)SIZE_A) -1);
		public const uint MAXARG_B		  = (uint)((1 << (int)SIZE_B) -1);
		public const uint MAXARG_C        = (uint)((1 << (int)SIZE_C) -1);


		/* creates a mask with `n' 1 bits at position `p' */
		//public static int MASK1(int n, int p) { return ((~((~(Instruction)0) << (n)) << (p)); }
		public static uint MASK1(int n, int p) { return (uint)((~((~0) << (n))) << (p)); }

		/* creates a mask with `n' 0 bits at position `p' */
		public static uint MASK0(int n, int p) { return (uint)(~MASK1(n, p)); }

		/*
		** the following macros help to manipulate instructions
		*/

		public static OpCode GET_OPCODE(Instruction i)
		{
			return (OpCode)((i >> POS_OP) & MASK1(SIZE_OP, 0));
		}
		public static OpCode GET_OPCODE(InstructionPtr i) { return GET_OPCODE(i[0]); }

		public static void SET_OPCODE(ref Instruction i, Instruction o)
		{
			i = (Instruction)(i & MASK0(SIZE_OP, POS_OP)) | ((o << POS_OP) & MASK1(SIZE_OP, POS_OP));
		}
		public static void SET_OPCODE(ref Instruction i, OpCode opcode)
		{
			i = (Instruction)(i & MASK0(SIZE_OP, POS_OP)) | (((uint)opcode << POS_OP) & MASK1(SIZE_OP, POS_OP));
		}
		public static void SET_OPCODE(InstructionPtr i, OpCode opcode) { SET_OPCODE(ref i.codes[i.pc], opcode); }

		//FIXME:???
		public static int getarg(Instruction i, int pos, int size) { return ((int)(((i)>>pos) & MASK1(size,0))); }
		public static void setarg(InstructionPtr i, int v, int pos, int size) { 
			//FIXME: changed here
			i[0] = (Instruction)((i[0] & MASK0(size,pos)) |
			              ( (((int)v) << pos) & MASK1(size, pos)));
		}
				

		public static int GETARG_A(Instruction i)
		{
			return getarg(i, POS_A, SIZE_A);
		}
		public static int GETARG_A(InstructionPtr i) { return GETARG_A(i[0]); } //FIXME:added

		public static void SETARG_A(InstructionPtr i, int v)
		{
			setarg(i, v, POS_A, SIZE_A);
		}

		public static int GETARG_B(Instruction i)
		{
			return getarg(i, POS_B, SIZE_B);
		}
		public static int GETARG_B(InstructionPtr i) { return GETARG_B(i[0]); } //FIXME: added

		public static void SETARG_B(InstructionPtr i, int v)
		{
			setarg(i, v, POS_B, SIZE_B);
		}

		public static int GETARG_C(Instruction i)
		{
			return getarg(i, POS_C, SIZE_C);
		}
		public static int GETARG_C(InstructionPtr i) { return GETARG_C(i[0]); } //FIXME: added

		public static void SETARG_C(InstructionPtr i, int v)
		{
			setarg(i, v, POS_C, SIZE_C);
		}

		public static int GETARG_Bx(Instruction i)
		{
			return getarg(i, POS_Bx, SIZE_Bx);
		}
		public static int GETARG_Bx(InstructionPtr i) { return GETARG_Bx(i[0]); } //FIXME: added

		public static void SETARG_Bx(InstructionPtr i, int v)
		{
			setarg(i, v, POS_Bx, SIZE_Bx);
		}

		public static int GETARG_Ax(Instruction i)
		{
			return getarg(i, POS_Ax, SIZE_Ax);
		}
		public static int GETARG_Ax(InstructionPtr i) { return GETARG_Ax(i[0]); } //FIXME: added

		public static void SETARG_Ax(InstructionPtr i, int v)
		{
			setarg(i, v, POS_Ax, SIZE_Ax);
		}


		public static int GETARG_sBx(Instruction i)
		{
			return (GETARG_Bx(i) - MAXARG_sBx);
		}
		public static int GETARG_sBx(InstructionPtr i) { return GETARG_sBx(i[0]); }

		public static void SETARG_sBx(InstructionPtr i, int b)
		{
			SETARG_Bx(i, b + MAXARG_sBx);
		}

		public static int CREATE_ABC(OpCode o, int a, int b, int c)
		{
			return (int)(((int)o << POS_OP) | (a << POS_A) | (b << POS_B) | (c << POS_C));
		}

		public static int CREATE_ABx(OpCode o, int a, int bc)
		{
			int result = (int)(((int)o << POS_OP) | (a << POS_A) | (bc << POS_Bx));
			return (int)(((int)o << POS_OP) | (a << POS_A) | (bc << POS_Bx));
		}

		public static int CREATE_Ax(OpCode o, int a)
		{
			return (int)(((((int)o)<<POS_OP)
			              | (((int)a)<<POS_A)));
		}


		/*
		** Macros to operate RK indices
		*/

		/* this bit 1 means constant (0 means register) */
		public readonly static int BITRK = 	(1 << (SIZE_B - 1));

		/* test whether value is a constant */
		public static int ISK(int x)		{return x & BITRK;}

		/* gets the index of the constant */
		public static int INDEXK(int r)	{return r & (~BITRK);}

		public static readonly int MAXINDEXRK = BITRK - 1;

		/* code a constant index as a RK value */
		public static int RKASK(int x)	{return x | BITRK;}


		/*
		** invalid register that fits in 8 bits
		*/
		public static readonly int NO_REG		= (int)MAXARG_A;


		/*
		** R(x) - register
		** Kst(x) - constant (in constant table)
		** RK(x) == if ISK(x) then Kst(INDEXK(x)) else R(x)
		*/


		/*
		** grep "ORDER OP" if you change these enums
		*/

		public enum OpCode {
		/*----------------------------------------------------------------------
		name		args	description
		------------------------------------------------------------------------*/
		OP_MOVE,/*	A B	R(A) := R(B)					*/
		OP_LOADK,/*	A Bx	R(A) := Kst(Bx)					*/
		OP_LOADBOOL,/*	A B C	R(A) := (Bool)B; if (C) pc++			*/
		OP_LOADNIL,/*	A B	R(A) := ... := R(B) := nil			*/
		OP_GETUPVAL,/*	A B	R(A) := UpValue[B]				*/

		OP_GETGLOBAL,/*	A Bx	R(A) := Gbl[Kst(Bx)]				*/
		OP_GETTABLE,/*	A B C	R(A) := R(B)[RK(C)]				*/

		OP_SETGLOBAL,/*	A Bx	Gbl[Kst(Bx)] := R(A)				*/
		OP_SETUPVAL,/*	A B	UpValue[B] := R(A)				*/
		OP_SETTABLE,/*	A B C	R(A)[RK(B)] := RK(C)				*/

		OP_NEWTABLE,/*	A B C	R(A) := {} (size = B,C)				*/

		OP_SELF,/*	A B C	R(A+1) := R(B); R(A) := R(B)[RK(C)]		*/

		OP_ADD,/*	A B C	R(A) := RK(B) + RK(C)				*/
		OP_SUB,/*	A B C	R(A) := RK(B) - RK(C)				*/
		OP_MUL,/*	A B C	R(A) := RK(B) * RK(C)				*/
		OP_DIV,/*	A B C	R(A) := RK(B) / RK(C)				*/
		OP_MOD,/*	A B C	R(A) := RK(B) % RK(C)				*/
		OP_POW,/*	A B C	R(A) := RK(B) ^ RK(C)				*/
		OP_UNM,/*	A B	R(A) := -R(B)					*/
		OP_NOT,/*	A B	R(A) := not R(B)				*/
		OP_LEN,/*	A B	R(A) := length of R(B)				*/

		OP_CONCAT,/*	A B C	R(A) := R(B).. ... ..R(C)			*/

		OP_JMP,/*	sBx	pc+=sBx					*/

		OP_EQ,/*	A B C	if ((RK(B) == RK(C)) ~= A) then pc++		*/
		OP_LT,/*	A B C	if ((RK(B) <  RK(C)) ~= A) then pc++		*/
		OP_LE,/*	A B C	if ((RK(B) <= RK(C)) ~= A) then pc++		*/

		OP_TEST,/*	A C	if not (R(A) <=> C) then pc++			*/
		OP_TESTSET,/*	A B C	if (R(B) <=> C) then R(A) := R(B) else pc++	*/ 

		OP_CALL,/*	A B C	R(A), ... ,R(A+C-2) := R(A)(R(A+1), ... ,R(A+B-1)) */
		OP_TAILCALL,/*	A B C	return R(A)(R(A+1), ... ,R(A+B-1))		*/
		OP_RETURN,/*	A B	return R(A), ... ,R(A+B-2)	(see note)	*/

		OP_FORLOOP,/*	A sBx	R(A)+=R(A+2);
					if R(A) <?= R(A+1) then { pc+=sBx; R(A+3)=R(A) }*/
		OP_FORPREP,/*	A sBx	R(A)-=R(A+2); pc+=sBx				*/

		OP_TFORCALL,/*	A C	R(A+3), ... ,R(A+2+C) := R(A)(R(A+1), R(A+2));	*/
		OP_SETLIST,/*	A B C	R(A)[(C-1)*FPF+i] := R(A+i), 1 <= i <= B	*/

		OP_CLOSE,/*	A	close all variables in the stack up to (>=) R(A)*/
		OP_CLOSURE,/*	A Bx	R(A) := closure(KPROTO[Bx], R(A), ... ,R(A+n))	*/

		OP_VARARG,/*	A B	R(A), R(A+1), ..., R(A+B-1) = vararg		*/
		
		OP_TFORLOOP,/*	A sBx	if R(A+1) ~= nil then { R(A)=R(A+1); pc += sBx }*/

		OP_EXTRAARG/*	Ax	extra argument for previous opcode		*/
		};


		public const int NUM_OPCODES	= (int)OpCode.OP_EXTRAARG + 1; //FIXME: ???---> + 1



		/*===========================================================================
		  Notes:
		  (*) In OP_CALL, if (B == 0) then B = top. If (C == 0), then `top' is
		  set to last_result+1, so next open instruction (OP_CALL, OP_RETURN,
		  OP_SETLIST) may use `top'.

		  (*) In OP_VARARG, if (B == 0) then use actual number of varargs and
		  set top (like in OP_CALL with C == 0).

		  (*) In OP_RETURN, if (B == 0) then return up to `top'.

		  (*) In OP_SETLIST, if (B == 0) then B = `top'; if (C == 0) then next
		  `instruction' is EXTRAARG(real C).

		  (*) For comparisons, A specifies what condition the test should accept
		  (true or false).

		  (*) All `skips' (pc++) assume that next instruction is a jump.

		  (*) The OP_CLOSURE instruction is followed by a sequence of
		  instructions coding the upvalues: OP_MOVE A B if upvalue is local B,
		  or OP_GETUPVAL A B if upvalue is enclosing upvalue B.

		===========================================================================*/


		/*
		** masks for instruction properties. The format is:
		** bits 0-1: op mode
		** bits 2-3: C arg mode
		** bits 4-5: B arg mode
		** bit 6: instruction set register A
		** bit 7: operator is a test (next instruction must be a jump)
		*/

		public enum OpArgMask {
		  OpArgN,  /* argument is not used */
		  OpArgU,  /* argument is used */
		  OpArgR,  /* argument is a register or a jump offset */
		  OpArgK   /* argument is a constant or register/constant */
		};

		public static OpMode getOpMode(OpCode m)	{return (OpMode)(luaP_opmodes[(int)m] & 3);}
		public static OpArgMask getBMode(OpCode m) { return (OpArgMask)((luaP_opmodes[(int)m] >> 4) & 3); }
		public static OpArgMask getCMode(OpCode m) { return (OpArgMask)((luaP_opmodes[(int)m] >> 2) & 3); }
		public static int testAMode(OpCode m) { return luaP_opmodes[(int)m] & (1 << 6); }
		public static int testTMode(OpCode m) { return luaP_opmodes[(int)m] & (1 << 7); }


		/* number of list items to accumulate before a SETLIST instruction */
		public const int LFIELDS_PER_FLUSH	= 50;
	}
}

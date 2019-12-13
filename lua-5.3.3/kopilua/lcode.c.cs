/*
** $Id: lcode.c,v 2.109 2016/05/13 19:09:21 roberto Exp $
** Code generator for Lua
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using lua_Number = System.Double;
	using Instruction = System.UInt32;
	using StkId = Lua.lua_TValue;
	using lua_Integer = System.Int32;

	public partial class Lua
	{

		/* Maximum number of registers in a Lua function (must fit in 8 bits) */
		private const int MAXREGS = 255;


		public static bool hasjumps(expdesc e)	{return e.t != e.f;}


		/*
		** If expression is a numeric constant, fills 'v' with its value
		** and returns 1. Otherwise, returns 0.
		*/
		private static int tonumeral(expdesc e, TValue v) {
		  if (hasjumps(e))
		    return 0;  /* not a numeral */
		  switch (e.k) {
		    case expkind.VKINT:
		      if (v!=null) setivalue(v, e.u.ival);
		      return 1;
		    case expkind.VKFLT:
		      if (v!=null) setfltvalue(v, e.u.nval);
		      return 1;
		    default: return 0;
		  }
		}


		/*
		** Create a OP_LOADNIL instruction, but try to optimize: if the previous
		** instruction is also OP_LOADNIL and ranges are compatible, adjust
		** range of previous instruction instead of emitting a new one. (For
		** instance, 'local a; local b' will generate a single opcode.)
		*/
		public static void luaK_nil (FuncState fs, int from, int n) {
		  InstructionPtr previous;
          int l = from + n - 1;  /* last register to set nil */
		  if (fs.pc > fs.lasttarget) {  /* no jumps to current position? */
		    previous = new InstructionPtr(fs.f.code, fs.pc-1);
		    if (GET_OPCODE(previous) == OpCode.OP_LOADNIL) {  /* previous is LOADNIL? */ //FIXME: no star
			  int pfrom = GETARG_A(previous);  /* get previous range */ //FIXME: no star
		      int pl = pfrom + GETARG_B(previous); //FIXME: no star
		      if ((pfrom <= from && from <= pl + 1) ||
		          (from <= pfrom && pfrom <= l + 1)) {  /* can connect both? */
		        if (pfrom < from) from = pfrom;  /* from = min(from, pfrom) */
		        if (pl > l) l = pl;  /* l = max(l, pl) */
		        SETARG_A(previous, from); //FIXME: no star
		        SETARG_B(previous, l - from); //FIXME: no star
			    return;
			  }
		    }  /* else go through */
		  }
		  luaK_codeABC(fs, OpCode.OP_LOADNIL, from, n - 1, 0);  /* else no optimization */
		}


		/*
		** Gets the destination address of a jump instruction. Used to traverse
		** a list of jumps.
		*/ 
		private static int getjump (FuncState fs, int pc) {
		  int offset = GETARG_sBx(fs.f.code[pc]);
		  if (offset == NO_JUMP)  /* point to itself represents end of list */
		    return NO_JUMP;  /* end of list */
		  else
		    return (pc+1)+offset;  /* turn offset into absolute position */
		}


		/*
		** Fix jump instruction at position 'pc' to jump to 'dest'.
		** (Jump addresses are relative in Lua)
		*/
		private static void fixjump (FuncState fs, int pc, int dest) {
		  InstructionPtr jmp = new InstructionPtr(fs.f.code, pc);
		  int offset = dest - (pc + 1);
		  lua_assert(dest != NO_JUMP);
		  if (Math.Abs(offset) > MAXARG_sBx)
			luaX_syntaxerror(fs.ls, "control structure too long");
		  SETARG_sBx(jmp, offset);
		}


		/*
		** Concatenate jump-list 'l2' into jump-list 'l1'
		*/
		public static void luaK_concat(FuncState fs, ref int l1, int l2) {
		  if (l2 == NO_JUMP) return;  /* nothing to concatenate? */
		  else if (l1 == NO_JUMP)  /* no original list? */
			l1 = l2;  /* 'l1' points to 'l2' */
		  else {
			int list = l1;
			int next;
			while ((next = getjump(fs, list)) != NO_JUMP)  /* find last element */
			  list = next;
			fixjump(fs, list, l2);  /* last element links to 'l2' */
		  }
		}

		/*
		** Create a jump instruction and return its position, so its destination
		** can be fixed later (with 'fixjump'). If there are jumps to
		** this position (kept in 'jpc'), link them all together so that
		** 'patchlistaux' will fix all them directly to the final destination.
		*/
		public static int luaK_jump (FuncState fs) {
		  int jpc = fs.jpc;  /* save list of jumps to here */
		  int j;
		  fs.jpc = NO_JUMP;  /* no more jumps to here */
		  j = luaK_codeAsBx(fs, OpCode.OP_JMP, 0, NO_JUMP);
		  luaK_concat(fs, ref j, jpc);  /* keep them on hold */
		  return j;
		}


		/*
		** Code a 'return' instruction
		*/
		public static void luaK_ret (FuncState fs, int first, int nret) {
		  luaK_codeABC(fs, OpCode.OP_RETURN, first, nret+1, 0);
		}


		/*
		** Code a "conditional jump", that is, a test or comparison opcode
		** followed by a jump. Return jump position.
		*/
		private static int condjump (FuncState fs, OpCode op, int A, int B, int C) {
		  luaK_codeABC(fs, op, A, B, C);
		  return luaK_jump(fs);
		}


		/*
		** returns current 'pc' and marks it as a jump target (to avoid wrong
		** optimizations with consecutive instructions not in the same basic block).
		*/
		public static int luaK_getlabel (FuncState fs) {
		  fs.lasttarget = fs.pc;
		  return fs.pc;
		}


		/*
		** Returns the position of the instruction "controlling" a given
		** jump (that is, its condition), or the jump itself if it is
		** unconditional.
		*/
		private static InstructionPtr getjumpcontrol (FuncState fs, int pc) {
		  InstructionPtr pi = new InstructionPtr(fs.f.code, pc);
		  if (pc >= 1 && 0!=testTMode(GET_OPCODE(pi[-1])))
		  	return InstructionPtr.minus(pi, 1);
		  else
		    return pi;
		}


		/*
		** Patch destination register for a TESTSET instruction.
		** If instruction in position 'node' is not a TESTSET, return 0 ("fails").
		** Otherwise, if 'reg' is not 'NO_REG', set it as the destination
		** register. Otherwise, change instruction to a simple 'TEST' (produces
		** no register value)
		*/
		private static int patchtestreg (FuncState fs, int node, int reg) {
		  InstructionPtr i = getjumpcontrol(fs, node);
		  if (GET_OPCODE(i[0]) != OpCode.OP_TESTSET)
		    return 0;  /* cannot patch other instructions */
		  if (reg != NO_REG && reg != GETARG_B(i[0]))
		  	SETARG_A(i, reg);
		  else {
		     /* no register to put value or register already has the value;
		        change instruction to simple test */
		    i[0] = (Instruction)CREATE_ABC(OpCode.OP_TEST, GETARG_B(i), 0, GETARG_C(i));
		  }
		  return 1;
		}


		/*
		** Traverse a list of tests ensuring no one produces a value
		*/
		private static void removevalues (FuncState fs, int list) {
		  for (; list != NO_JUMP; list = getjump(fs, list))
		      patchtestreg(fs, list, NO_REG);
		}


		/*
		** Traverse a list of tests, patching their destination address and
		** registers: tests producing values jump to 'vtarget' (and put their
		** values in 'reg'), other tests jump to 'dtarget'.
		*/
		private static void patchlistaux (FuncState fs, int list, int vtarget, int reg,
		                          int dtarget) {
		  while (list != NO_JUMP) {
		    int next = getjump(fs, list);
		    if (0!=patchtestreg(fs, list, reg))
		      fixjump(fs, list, vtarget);
		    else
		      fixjump(fs, list, dtarget);  /* jump to default target */
		    list = next;
		  }
		}


		/*
		** Ensure all pending jumps to current position are fixed (jumping
		** to current position with no values) and reset list of pending
		** jumps
		*/
		private static void dischargejpc (FuncState fs) {
		  patchlistaux(fs, fs.jpc, fs.pc, NO_REG, fs.pc);
		  fs.jpc = NO_JUMP;
		}


		/*
		** Add elements in 'list' to list of pending jumps to "here"
		** (current position)
		*/
		public static void luaK_patchtohere (FuncState fs, int list) {
		  luaK_getlabel(fs);  /* mark "here" as a jump target */
		  luaK_concat(fs, ref fs.jpc, list);
		}


		/*
		** Path all jumps in 'list' to jump to 'target'.
		** (The assert means that we cannot fix a jump to a forward address
		** because we only know addresses once code is generated.)
		*/
		public static void luaK_patchlist (FuncState fs, int list, int target) {
		  if (target == fs.pc)  /* 'target' is current position? */
		    luaK_patchtohere(fs, list);  /* add list to pending jumps */
		  else {
		    lua_assert(target < fs.pc);
		    patchlistaux(fs, list, target, NO_REG, target);
		  }
		}


		/*
		** Path all jumps in 'list' to close upvalues up to given 'level'
		** (The assertion checks that jumps either were closing nothing
		** or were closing higher levels, from inner blocks.)
		*/
		public static void luaK_patchclose (FuncState fs, int list, int level) {
		  level++;  /* argument is +1 to reserve 0 as non-op */
		  for (; list != NO_JUMP; list = getjump(fs, list)) {
		    lua_assert(GET_OPCODE(fs.f.code[list]) == OpCode.OP_JMP &&
		                (GETARG_A(fs.f.code[list]) == 0 ||
		                 GETARG_A(fs.f.code[list]) >= level));
		  	SETARG_A(new InstructionPtr(fs.f.code, list), level);
		  }
		}


		/*
		** Emit instruction 'i', checking for array sizes and saving also its
		** line information. Return 'i' position.
		*/
		private static int luaK_code (FuncState fs, Instruction i) {			
		  Proto f = fs.f;
		  dischargejpc(fs);  /* 'pc' will change */
		  /* put new instruction in code array */
		  luaM_growvector(fs.ls.L, ref f.code, fs.pc, ref f.sizecode,
						  MAX_INT, "opcodes");
		  f.code[fs.pc] = i;
		  /* save corresponding line information */
		  luaM_growvector(fs.ls.L, ref f.lineinfo, fs.pc, ref f.sizelineinfo,
						  MAX_INT, "opcodes");
		  f.lineinfo[fs.pc] = fs.ls.lastline;		  
		  return fs.pc++;
		}


		/*
		** Format and emit an 'iABC' instruction. (Assertions check consistency
		** of parameters versus opcode.)
		*/
		public static int luaK_codeABC (FuncState fs, OpCode o, int a, int b, int c) {
		  lua_assert(getOpMode(o) == OpMode.iABC);
		  lua_assert(getBMode(o) != OpArgMask.OpArgN || b == 0);
		  lua_assert(getCMode(o) != OpArgMask.OpArgN || c == 0);
          lua_assert(a <= MAXARG_A && b <= MAXARG_B && c <= MAXARG_C);
          return luaK_code(fs, (Instruction)CREATE_ABC(o, a, b, c)); //FIXME: added (Instruction)
		}


		/*
		** Format and emit an 'iABx' instruction.
		*/
		public static int luaK_codeABx (FuncState fs, OpCode o, int a, uint bc) {			
		  lua_assert(getOpMode(o) == OpMode.iABx || getOpMode(o) == OpMode.iAsBx);
		  lua_assert(getCMode(o) == OpArgMask.OpArgN);
          lua_assert(a <= MAXARG_A && bc <= MAXARG_Bx);
          return luaK_code(fs, (Instruction)CREATE_ABx(o, a, (int)bc)); //FIXME: added (Instruction) added (int)
		}


		/*
		** Emit an "extra argument" instruction (format 'iAx')
		*/
		private static int codeextraarg (FuncState fs, int a) {
		  lua_assert(a <= MAXARG_Ax);
		  return luaK_code(fs, (Instruction)CREATE_Ax(OpCode.OP_EXTRAARG, a)); //FIXME: added (Instruction)
		}


		/*
		** Emit a "load constant" instruction, using either 'OP_LOADK'
		** (if constant index 'k' fits in 18 bits) or an 'OP_LOADKX'
		** instruction with "extra argument".
		*/
		public static int luaK_codek (FuncState fs, int reg, int k) {
		  if (k <= MAXARG_Bx)
		  	return luaK_codeABx(fs, OpCode.OP_LOADK, reg, (uint)(k)); //FIXME: added (uint)
		  else {
		    int p = luaK_codeABx(fs, OpCode.OP_LOADKX, reg, 0);
		    codeextraarg(fs, k);
		    return p;
		  }
		}


		/*
		** Check register-stack level, keeping track of its maximum size
		** in field 'maxstacksize'
		*/
		public static void luaK_checkstack (FuncState fs, int n) {
		  int newstack = fs.freereg + n;
		  if (newstack > fs.f.maxstacksize) {
			if (newstack >= MAXREGS)
			  luaX_syntaxerror(fs.ls, 
			    "function or expression needs too many registers");
			fs.f.maxstacksize = cast_byte(newstack);
		  }
		}


		/*
		** Reserve 'n' registers in register stack
		*/
		public static void luaK_reserveregs (FuncState fs, int n) {
		  luaK_checkstack(fs, n);
		  fs.freereg += (byte)n; //FIXME:changed, (byte)
		}


		/*
		** Free register 'reg', if it is neither a constant index nor
		** a local variable.
		)
		*/
		private static void freereg (FuncState fs, int reg) {
		  if ((ISK(reg)==0) && reg >= fs.nactvar) {
			fs.freereg--;
			lua_assert(reg == fs.freereg);
		  }
		}


		/*
		** Free register used by expression 'e' (if any)
		*/
		private static void freeexp (FuncState fs, expdesc e) {
		  if (e.k == expkind.VNONRELOC)
			freereg(fs, e.u.info);
		}

		/*
		** Free registers used by expressions 'e1' and 'e2' (if any) in proper
		** order.
		*/
		private static void freeexps (FuncState fs, expdesc e1, expdesc e2) {
		  int r1 = (e1.k == expkind.VNONRELOC) ? e1.u.info : -1;
		  int r2 = (e2.k == expkind.VNONRELOC) ? e2.u.info : -1;
		  if (r1 > r2) {
		    freereg(fs, r1);
		    freereg(fs, r2);
		  }
		  else {
		    freereg(fs, r2);
		    freereg(fs, r1);
		  }
		}


		/*
		** Add constant 'v' to prototype's list of constants (field 'k').
		** Use scanner's table to cache position of constants in constant list
		** and try to reuse constants. Because some values should not be used
		** as keys (nil cannot be a key, integer keys can collapse with float
		** keys), the caller must provide a useful 'key' for indexing the cache.
		*/
		private static int addk (FuncState fs, TValue key, TValue v) {
		  lua_State L = fs.ls.L;
		  Proto f = fs.f;
		  TValue idx = luaH_set(L, fs.ls.h, key);  /* index scanner table */
		  int k, oldsize;
		  if (ttisinteger(idx)) {  /* is there an index there? */
		    k = (int)ivalue(idx);
			/* correct value? (warning: must distinguish floats from integers!) */
		    if (k < fs.nk && ttype(f.k[k]) == ttype(v) &&
		                      0!=luaV_rawequalobj(f.k[k], v))
		      return k;  /* reuse index */
		  }
		  /* constant not found; create a new entry */
		  oldsize = f.sizek;
		  k = fs.nk;
		  /* numerical value does not need GC barrier;
		     table has no metatable, so it does not need to invalidate cache */
		  setivalue(idx, k);
		  luaM_growvector(L, ref f.k, k, ref f.sizek, MAXARG_Ax, "constants");
		  while (oldsize < f.sizek) setnilvalue(f.k[oldsize++]);
		  setobj(L, f.k[k], v);
          fs.nk++;
		  luaC_barrier(L, f, v);
		  return k;
		}


		/*
		** Add a string to list of constants and return its index.
		*/
		public static int luaK_stringK (FuncState fs, TString s) {
		  TValue o = new TValue();
		  setsvalue(fs.ls.L, o, s);
		  return addk(fs, o, o);  /* use string itself as key */
		}


		/*
		** Add an integer to list of constants and return its index.
		** Integers use userdata as keys to avoid collision with floats with
		** same value; conversion to 'void*' is used only for hashing, so there
		** are no "precision" problems.
		*/
		public static int luaK_intK (FuncState fs, lua_Integer n) {
		  TValue k = new TValue(); TValue o = new TValue();
		  setpvalue(k, (object)((uint)n));
		  setivalue(o, n);
		  return addk(fs, k, o);
		}

		/*
		** Add a float to list of constants and return its index.
		*/
		private static int luaK_numberK (FuncState fs, lua_Number r) {
		  TValue o = new TValue();
		  setfltvalue(o, r);
		  return addk(fs, o, o);  /* use number itself as key */
		}


		/*
		** Add a boolean to list of constants and return its index.
		*/
		private static int boolK (FuncState fs, int b) {
		  TValue o = new TValue();
		  setbvalue(o, b);
		  return addk(fs, o, o);  /* use boolean itself as key */
		}


		/*
		** Add nil to list of constants and return its index.
		*/
		private static int nilK (FuncState fs) {
		  TValue k = new TValue(), v = new TValue();
		  setnilvalue(v);
		  /* cannot use nil as key; instead use table itself to represent nil */
		  sethvalue(fs.ls.L, k, fs.ls.h);
		  return addk(fs, k, v);
		}


		/*
		** Fix an expression to return the number of results 'nresults'.
		** Either 'e' is a multi-ret expression (function call or vararg)
		** or 'nresults' is LUA_MULTRET (as any expression can satisfy that).
		*/
		public static void luaK_setreturns (FuncState fs, expdesc e, int nresults) {
		  if (e.k == expkind.VCALL) {  /* expression is an open function call? */
		    SETARG_C(getinstruction(fs, e), nresults + 1);
		  }
		  else if (e.k == expkind.VVARARG) {
		    InstructionPtr pc = getinstruction(fs, e);
		    SETARG_B(pc, nresults + 1);
		    SETARG_A(pc, fs.freereg);
		    luaK_reserveregs(fs, 1);
		  }
		  else lua_assert(nresults == LUA_MULTRET);
		}


		/*
		** Fix an expression to return one result.
		** If expression is not a multi-ret expression (function call or
		** vararg), it already returns one result, so nothing needs to be done.
		** Function calls become VNONRELOC expressions (as its result comes
		** fixed in the base register of the call), while vararg expressions
		** become VRELOCABLE (as OP_VARARG puts its results where it wants).
		** (Calls are created returning one result, so that does not need
		** to be fixed.)
		*/
		public static void luaK_setoneret (FuncState fs, expdesc e) {
		  if (e.k == expkind.VCALL) {  /* expression is an open function call? */
		    /* already returns 1 value */
    		lua_assert(GETARG_C(getinstruction(fs, e)) == 2);
			e.k = expkind.VNONRELOC;  /* result has fixed position */
			e.u.info = GETARG_A(getinstruction(fs, e));
		  }
		  else if (e.k == expkind.VVARARG) {
			SETARG_B(getinstruction(fs, e), 2);
			e.k = expkind.VRELOCABLE;  /* can relocate its simple result */
		  }
		}


		/*
		** Ensure that expression 'e' is not a variable.
		*/
		public static void luaK_dischargevars (FuncState fs, expdesc e) {
		  switch (e.k) {
		    case expkind.VLOCAL: {  /* already in a register */
		      e.k = expkind.VNONRELOC;  /* becomes a non-relocatable value */
		      break;
		    }
		    case expkind.VUPVAL: {  /* move value to some (pending) register */
		      e.u.info = luaK_codeABC(fs, OpCode.OP_GETUPVAL, 0, e.u.info, 0);
		      e.k = expkind.VRELOCABLE;
		      break;
		    }
		    case expkind.VINDEXED: {
		      OpCode op;
		      freereg(fs, e.u.ind.idx);
		      if (e.u.ind.vt == (byte)expkind.VLOCAL) {  /* is 't' in a register? */
		        freereg(fs, e.u.ind.t);
		        op = OpCode.OP_GETTABLE;
		      }
		      else {
		        lua_assert(e.u.ind.vt == (byte)expkind.VUPVAL);
		        op = OpCode.OP_GETTABUP;  /* 't' is in an upvalue */
		      }
		      e.u.info = luaK_codeABC(fs, op, 0, e.u.ind.t, e.u.ind.idx);
		      e.k = expkind.VRELOCABLE;
		      break;
		    }
		    case expkind.VVARARG: case expkind.VCALL: {
		      luaK_setoneret(fs, e);
		      break;
		    }
		    default: break;  /* there is one value available (somewhere) */
		  }
		}


		/*
		** Ensures expression value is in register 'reg' (and therefore
		** 'e' will become a non-relocatable expression).
		*/
		private static void discharge2reg (FuncState fs, expdesc e, int reg) {
		  luaK_dischargevars(fs, e);
		  switch (e.k) {
			case expkind.VNIL: {
			  luaK_nil(fs, reg, 1);
			  break;
			}
			case expkind.VFALSE: case expkind.VTRUE: {
				luaK_codeABC(fs, OpCode.OP_LOADBOOL, reg, (e.k == expkind.VTRUE) ? 1 : 0, 0);
			  break;
			}
			case expkind.VK: {
			  luaK_codek(fs, reg, e.u.info);
			  break;
			}
			case expkind.VKFLT: {
		      luaK_codek(fs, reg, luaK_numberK(fs, e.u.nval));
		      break;
		    }
		    case expkind.VKINT: {
		      luaK_codek(fs, reg, luaK_intK(fs, e.u.ival));
		      break;
		    }
			case expkind.VRELOCABLE: {
			  InstructionPtr pc = getinstruction(fs, e);
			  SETARG_A(pc, reg);  /* instruction will put result in 'reg' */
			  break;
			}
			case expkind.VNONRELOC: {
			  if (reg != e.u.info)
				luaK_codeABC(fs, OpCode.OP_MOVE, reg, e.u.info, 0);
			  break;
			}
			default: {
			  lua_assert(e.k == expkind.VJMP);
			  return;  /* nothing to do... */
			}
		  }
		  e.u.info = reg;
		  e.k = expkind.VNONRELOC;
		}


		/*
		** Ensures expression value is in any register.
		*/
		private static void discharge2anyreg (FuncState fs, expdesc e) {
		  if (e.k != expkind.VNONRELOC) {  /* no fixed register yet? */
			luaK_reserveregs(fs, 1);  /* get a register */
			discharge2reg(fs, e, fs.freereg-1);  /* put value there */
		  }
		}


		private static int code_loadbool (FuncState fs, int A, int b, int jump) {
		  luaK_getlabel(fs);  /* those instructions may be jump targets */
		  return luaK_codeABC(fs, OpCode.OP_LOADBOOL, A, b, jump);
		}


		/*
		** check whether list has any jump that do not produce a value
		** or produce an inverted value
		*/
		private static int need_value (FuncState fs, int list) {
		  for (; list != NO_JUMP; list = getjump(fs, list)) {
		    InstructionPtr i = getjumpcontrol(fs, list);
		    if (GET_OPCODE(i) != OpCode.OP_TESTSET) return 1;
		  }
		  return 0;  /* not found */
		}


		/*
		** Ensures final expression result (including results from its jump
		** lists) is in register 'reg'.
		** If expression has jumps, need to patch these jumps either to
		** its final position or to "load" instructions (for those tests
		** that do not produce values).
		*/
		private static void exp2reg (FuncState fs, expdesc e, int reg) {
		  discharge2reg(fs, e, reg);
		  if (e.k == expkind.VJMP)  /* expression itself is a test? */
			luaK_concat(fs, ref e.t, e.u.info);  /* put this jump in 't' list */
		  if (hasjumps(e)) {
			int final;  /* position after whole expression */
			int p_f = NO_JUMP;  /* position of an eventual LOAD false */
			int p_t = NO_JUMP;  /* position of an eventual LOAD true */
			if (need_value(fs, e.t)!=0 || need_value(fs, e.f)!=0) {
			  int fj = (e.k == expkind.VJMP) ? NO_JUMP : luaK_jump(fs);
			  p_f = code_loadbool(fs, reg, 0, 1);
			  p_t = code_loadbool(fs, reg, 1, 0);
			  luaK_patchtohere(fs, fj);
			}
			final = luaK_getlabel(fs);
			patchlistaux(fs, e.f, final, reg, p_f);
			patchlistaux(fs, e.t, final, reg, p_t);
		  }
		  e.f = e.t = NO_JUMP;
		  e.u.info = reg;
		  e.k = expkind.VNONRELOC;
		}


		/*
		** Ensures final expression result (including results from its jump
		** lists) is in next available register.
		*/
		public static void luaK_exp2nextreg (FuncState fs, expdesc e) {
		  luaK_dischargevars(fs, e);
		  freeexp(fs, e);
		  luaK_reserveregs(fs, 1);
		  exp2reg(fs, e, fs.freereg - 1);
		}


		/*
		** Ensures final expression result (including results from its jump
		** lists) is in some (any) register and return that register.
		*/
		public static int luaK_exp2anyreg (FuncState fs, expdesc e) {
		  luaK_dischargevars(fs, e);
		  if (e.k == expkind.VNONRELOC) {  /* expression already has a register? */
			if (!hasjumps(e))  /* no jumps? */ 
			  return e.u.info;  /* result is already in a register */
			if (e.u.info >= fs.nactvar) {  /* reg. is not a local? */
			  exp2reg(fs, e, e.u.info);  /* put final result in it */
			  return e.u.info;
			}
		  }
		  luaK_exp2nextreg(fs, e);  /* otherwise, use next available register */
		  return e.u.info;
		}


		/*
		** Ensures final expression result is either in a register or in an
		** upvalue.
		*/
		public static void luaK_exp2anyregup (FuncState fs, expdesc e) { //FIXME:public ???
		  if (e.k != expkind.VUPVAL || hasjumps(e))
		    luaK_exp2anyreg(fs, e);
		}


		/*
		** Ensures final expression result is either in a register or it is
		** a constant.
		*/
		public static void luaK_exp2val (FuncState fs, expdesc e) {
		  if (hasjumps(e))
			luaK_exp2anyreg(fs, e);
		  else
			luaK_dischargevars(fs, e);
		}


		/*
		** Ensures final expression result is in a valid R/K index
		** (that is, it is either in a register or in 'k' with an index
		** in the range of R/K indices).
		** Returns R/K index.
		*/  
		public static int luaK_exp2RK (FuncState fs, expdesc e) {
		  luaK_exp2val(fs, e);
		  switch (e.k) {  /* move constants to 'k' */
		    case expkind.VTRUE: e.u.info = boolK(fs, 1); goto vk;
		    case expkind.VFALSE: e.u.info = boolK(fs, 0); goto vk;
		    case expkind.VNIL: e.u.info = nilK(fs); goto vk;
		    case expkind.VKINT: e.u.info = luaK_intK(fs, e.u.ival); goto vk;
		    case expkind.VKFLT: e.u.info = luaK_numberK(fs, e.u.nval); goto vk;
		    case expkind.VK:
		     vk:
		      e.k = expkind.VK;
		      if (e.u.info <= MAXINDEXRK)  /* constant fits in 'argC'? */
		        return RKASK(e.u.info);
		      else break;
		    default: break;
		  }
		  /* not a constant in the right range: put it in a register */
		  return luaK_exp2anyreg(fs, e);
		}


		/*
		** Generate code to store result of expression 'ex' into variable 'var'.
		*/
		public static void luaK_storevar (FuncState fs, expdesc var, expdesc ex) {
		  switch (var.k) {
			case expkind.VLOCAL: {
			  freeexp(fs, ex);
			  exp2reg(fs, ex, var.u.info);  /* compute 'ex' into proper place */
			  return;
			}
			case expkind.VUPVAL: {
			  int e = luaK_exp2anyreg(fs, ex);
			  luaK_codeABC(fs, OpCode.OP_SETUPVAL, e, var.u.info, 0);
			  break;
			}
			case expkind.VINDEXED: {
        	  OpCode op = (var.u.ind.vt == (byte)expkind.VLOCAL) ? OpCode.OP_SETTABLE : OpCode.OP_SETTABUP; //FIXME:added, (byte)
			  int e = luaK_exp2RK(fs, ex);
			  luaK_codeABC(fs, op, var.u.ind.t, var.u.ind.idx, e);
			  break;
			}
			default: lua_assert(0);  /* invalid var kind to store */
			  break;
		  }
		  freeexp(fs, ex);
		}


		/*
		** Emit SELF instruction (convert expression 'e' into 'e:key(e,').
		*/
		public static void luaK_self (FuncState fs, expdesc e, expdesc key) {
		  int ereg;
		  luaK_exp2anyreg(fs, e);
          ereg = e.u.info;  /* register where 'e' was placed */
		  freeexp(fs, e);
		  e.u.info = fs.freereg;  /* base register for op_self */
		  e.k = expkind.VNONRELOC;  /* self expression has a fixed register */
		  luaK_reserveregs(fs, 2);  /* function and 'self' produced by op_self */
		  luaK_codeABC(fs, OpCode.OP_SELF, e.u.info, ereg, luaK_exp2RK(fs, key));
		  freeexp(fs, key);
		}


		/*
		** Negate condition 'e' (where 'e' is a comparison).
		*/
		private static void negatecondition (FuncState fs, expdesc e) {
		  InstructionPtr pc = getjumpcontrol(fs, e.u.info);
		  lua_assert(testTMode(GET_OPCODE(pc[0])) != 0 && GET_OPCODE(pc[0]) != OpCode.OP_TESTSET &&
												   GET_OPCODE(pc[0]) != OpCode.OP_TEST);
		  SETARG_A(pc, (GETARG_A(pc[0]) == 0) ? 1 : 0);
		}


		/*
		** Emit instruction to jump if 'e' is 'cond' (that is, if 'cond'
		** is true, code will jump if 'e' is true.) Return jump position.
		** Optimize when 'e' is 'not' something, inverting the condition
		** and removing the 'not'.
		*/
		private static int jumponcond (FuncState fs, expdesc e, int cond) {
		  if (e.k == expkind.VRELOCABLE) {
			InstructionPtr ie = getinstruction(fs, e);
			if (GET_OPCODE(ie) == OpCode.OP_NOT) {
			  fs.pc--;  /* remove previous OpCode.OP_NOT */
			  return condjump(fs, OpCode.OP_TEST, GETARG_B(ie), 0, (cond==0) ? 1 : 0);
			}
			/* else go through */
		  }
		  discharge2anyreg(fs, e);
		  freeexp(fs, e);
		  return condjump(fs, OpCode.OP_TESTSET, NO_REG, e.u.info, cond);
		}


		/*
		** Emit code to go through if 'e' is true, jump otherwise.
		*/
		public static void luaK_goiftrue (FuncState fs, expdesc e) {
		  int pc;  /* pc of new jump */
		  luaK_dischargevars(fs, e);
		  switch (e.k) {
			case expkind.VJMP: {  /* condition? */
			  negatecondition(fs, e);  /* jump when it is false */
			  pc = e.u.info;  /* save jump position */
			  break;
			}
		    case expkind.VK: case expkind.VKFLT: case expkind.VKINT: case expkind.VTRUE: {
			  pc = NO_JUMP;  /* always true; do nothing */
			  break;
			}
			default: {
			  pc = jumponcond(fs, e, 0);  /* jump when false */
			  break;
			}
		  }
		  luaK_concat(fs, ref e.f, pc);  /* insert new jump in false list */
		  luaK_patchtohere(fs, e.t);  /* true list jumps to here (to go through) */
		  e.t = NO_JUMP;
		}


		/*
		** Emit code to go through if 'e' is false, jump otherwise.
		*/
		public static void luaK_goiffalse (FuncState fs, expdesc e) {
		  int pc;  /* pc of new jump */
		  luaK_dischargevars(fs, e);
		  switch (e.k) {
			case expkind.VJMP: {
			  pc = e.u.info;  /* already jump if true */
			  break;
			}
			case expkind.VNIL: case expkind.VFALSE: {
			  pc = NO_JUMP;  /* always false; do nothing */
			  break;
			}
			default: {
			  pc = jumponcond(fs, e, 1);  /* jump if true */
			  break;
			}
		  }
		  luaK_concat(fs, ref e.t, pc);  /* insert new jump in 't' list */
		  luaK_patchtohere(fs, e.f);  /* false list jumps to here (to go through) */
		  e.f = NO_JUMP;
		}


		/*
		** Code 'not e', doing constant folding.
		*/
		private static void codenot (FuncState fs, expdesc e) {
		  luaK_dischargevars(fs, e);
		  switch (e.k) {
			case expkind.VNIL: case expkind.VFALSE: {
			  e.k = expkind.VTRUE;  /* true == not nil == not false */
			  break;
			}
			case expkind.VK: case expkind.VKFLT: case expkind.VKINT: case expkind.VTRUE: {
			  e.k = expkind.VFALSE;  /* false == not "x" == not 0.5 == not 1 == not true */
			  break;
			}
			case expkind.VJMP: {
			  negatecondition(fs, e);
			  break;
			}
			case expkind.VRELOCABLE:
			case expkind.VNONRELOC: {
			  discharge2anyreg(fs, e);
			  freeexp(fs, e);
			  e.u.info = luaK_codeABC(fs, OpCode.OP_NOT, 0, e.u.info, 0);
			  e.k = expkind.VRELOCABLE;
			  break;
			}
			default: lua_assert(0);  /* cannot happen */
			  break;
		  }
		  /* interchange true and false lists */
		  { int temp = e.f; e.f = e.t; e.t = temp; }
		  removevalues(fs, e.f);  /* values are useless when negated */
		  removevalues(fs, e.t);
		}


		/*
		** Create expression 't[k]'. 't' must have its final result already in a
		** register or upvalue.
		*/
		public static void luaK_indexed (FuncState fs, expdesc t, expdesc k) {
		  lua_assert(!hasjumps(t) && (vkisinreg(t.k) || t.k == expkind.VUPVAL));
		  t.u.ind.t = (byte)t.u.info;  /* register or upvalue index */
		  t.u.ind.idx = (short)luaK_exp2RK(fs, k);  /* R/K index for key */
		  t.u.ind.vt = (t.k == expkind.VUPVAL) ? (byte)expkind.VUPVAL : (byte)expkind.VLOCAL;
		  t.k = expkind.VINDEXED;
		}
		

		/*
		** Return false if folding can raise an error.
		** Bitwise operations need operands convertible to integers; division
		** operations cannot have 0 as divisor.
		*/
		private static int validop (int op, TValue v1, TValue v2) {
		  switch (op) {
		    case LUA_OPBAND: case LUA_OPBOR: case LUA_OPBXOR:
		    case LUA_OPSHL: case LUA_OPSHR: case LUA_OPBNOT: {  /* conversion errors */
			  lua_Integer i = 0;
		      return (0!=tointeger(ref v1, ref i) && 0!=tointeger(ref v2, ref i))?1:0;
		    }
			case LUA_OPDIV: case LUA_OPIDIV: case LUA_OPMOD:  /* division by 0 */
		      return (nvalue(v2) != 0)?0:1;
		    default: return 1;  /* everything else is valid */
		  }
		}


		/*
		** Try to "constant-fold" an operation; return 1 iff successful.
		** (In this case, 'e1' has the final result.)
		*/
		private static int constfolding (FuncState fs, int op, expdesc e1, expdesc e2) {
		  TValue v1 = new TValue(), v2 = new TValue(), res = new TValue();
		  if (0==tonumeral(e1, v1) || 0==tonumeral(e2, v2) || 0==validop(op, v1, v2))
		    return 0;  /* non-numeric operands or not safe to fold */
		  luaO_arith(fs.ls.L, op, v1, v2, res);  /* does operation */
		  if (ttisinteger(res)) {
		    e1.k = expkind.VKINT;
		    e1.u.ival = ivalue(res);
		  }
		  else {  /* folds neither NaN nor 0.0 (to avoid problems with -0.0) */
		    lua_Number n = fltvalue(res);
		    if (luai_numisnan(n) || n == 0)
		      return 0;
		    e1.k = expkind.VKFLT;
		    e1.u.nval = n;
		  }
		  return 1;
		}


		/*
		** Emit code for unary expressions that "produce values"
		** (everything but 'not').
		** Expression to produce final result will be encoded in 'e'.
		*/
		private static void codeunexpval (FuncState fs, OpCode op, expdesc e, int line) {
		  int r = luaK_exp2anyreg(fs, e);  /* opcodes operate only on registers */
		  freeexp(fs, e);
		  e.u.info = luaK_codeABC(fs, op, 0, r, 0);  /* generate opcode */
		  e.k = expkind.VRELOCABLE;  /* all those operations are relocatable */
		  luaK_fixline(fs, line);
		}


		/*
		** Emit code for binary expressions that "produce values"
		** (everything but logical operators 'and'/'or' and comparison
		** operators).
		** Expression to produce final result will be encoded in 'e1'.
		*/
		private static void codebinexpval (FuncState fs, OpCode op,
		                           expdesc e1, expdesc e2, int line) {
		  int rk1 = luaK_exp2RK(fs, e1);  /* both operands are "RK" */
		  int rk2 = luaK_exp2RK(fs, e2);
		  freeexps(fs, e1, e2);
		  e1.u.info = luaK_codeABC(fs, op, 0, rk1, rk2);  /* generate opcode */
		  e1.k = expkind.VRELOCABLE;  /* all those operations are relocatable */
		  luaK_fixline(fs, line);
		}


		/*
		** Emit code for comparisons.
		** 'e1' was already put in R/K form by 'luaK_infix'.
		*/
		private static void codecomp (FuncState fs, BinOpr opr, expdesc e1, expdesc e2) {
		  int rk1 = (e1.k == expkind.VK) ? RKASK(e1.u.info)
		                          : (int)check_exp(e1.k == expkind.VNONRELOC, e1.u.info);
		  int rk2 = luaK_exp2RK(fs, e2);
		  freeexps(fs, e1, e2);
		  switch (opr) {
		    case BinOpr.OPR_NE: {  /* '(a ~= b)' ==> 'not (a == b)' */
		      e1.u.info = condjump(fs, OpCode.OP_EQ, 0, rk1, rk2);
		      break;
		    }
		    case BinOpr.OPR_GT: case BinOpr.OPR_GE: {
		      /* '(a > b)' ==> '(b < a)';  '(a >= b)' ==> '(b <= a)' */
		      OpCode op = (OpCode)((opr - BinOpr.OPR_NE) + OpCode.OP_EQ);
		      e1.u.info = condjump(fs, op, 1, rk2, rk1);  /* invert operands */
		      break;
		    }
		    default: {  /* '==', '<', '<=' use their own opcodes */
		  	  OpCode op = (OpCode)((opr - BinOpr.OPR_EQ) + OpCode.OP_EQ);
		      e1.u.info = condjump(fs, op, 1, rk1, rk2);
		      break;
		    }
		  }
		  e1.k = expkind.VJMP;
		}

		
		private static expdesc luaK_prefix_ef = luaK_prefix_ef_init();
		private static expdesc luaK_prefix_ef_init() {
			expdesc result = new expdesc();
			result.k = expkind.VKINT;
			//result.u = {0}
			result.t = NO_JUMP;
			result.f = NO_JUMP;
			return result;
		}
		/*
		** Aplly prefix operation 'op' to expression 'e'.
		*/
		public static void luaK_prefix (FuncState fs, UnOpr op, expdesc e, int line) {
		  //static expdesc ef = {VKINT, {0}, NO_JUMP, NO_JUMP};  /* fake 2nd operand */
		  switch (op) {
		    case UnOpr.OPR_MINUS: case UnOpr.OPR_BNOT:
		  	  if (0!=constfolding(fs, (int)op + LUA_OPUNM, e, luaK_prefix_ef))
		        break;
		      /* FALLTHROUGH */
		      goto case UnOpr.OPR_LEN;
		    case UnOpr.OPR_LEN:
		      codeunexpval(fs, (OpCode)((int)op + OpCode.OP_UNM), e, line);
		      break;
		    case UnOpr.OPR_NOT: codenot(fs, e); break;
		    default: lua_assert(0);
		      break;
		  }
		}


		/*
		** Process 1st operand 'v' of binary operation 'op' before reading
		** 2nd operand.
		*/
		public static void luaK_infix (FuncState fs, BinOpr op, expdesc v) {
		  switch (op) {
			case BinOpr.OPR_AND: {
			  luaK_goiftrue(fs, v);  /* go ahead only if 'v' is true */
			  break;
			}
			case BinOpr.OPR_OR: {
			  luaK_goiffalse(fs, v);  /* go ahead only if 'v' is false */
			  break;
			}
			case BinOpr.OPR_CONCAT: {
			  luaK_exp2nextreg(fs, v);  /* operand must be on the 'stack' */
			  break;
			}
		    case BinOpr.OPR_ADD: case BinOpr.OPR_SUB:
		    case BinOpr.OPR_MUL: case BinOpr.OPR_DIV: case BinOpr.OPR_IDIV:
		    case BinOpr.OPR_MOD: case BinOpr.OPR_POW:
		    case BinOpr.OPR_BAND: case BinOpr.OPR_BOR: case BinOpr.OPR_BXOR:
		    case BinOpr.OPR_SHL: case BinOpr.OPR_SHR: {
		      if (0==tonumeral(v, null))
		        luaK_exp2RK(fs, v);
		      /* else keep numeral, which may be folded with 2nd operand */
		      break;
		    }
			default: {
			  luaK_exp2RK(fs, v);
			  break;
			}
		  }
		}


		/*
		** Finalize code for binary operation, after reading 2nd operand.
		** For '(a .. b .. c)' (which is '(a .. (b .. c))', because
		** concatenation is right associative), merge second CONCAT into first
		** one.
		*/
		public static void luaK_posfix (FuncState fs, BinOpr op, 
		                                expdesc e1, expdesc e2, int line) {
		  switch (op) {
			case BinOpr.OPR_AND: {
			  lua_assert(e1.t == NO_JUMP);  /* list closed by 'luK_infix' */
			  luaK_dischargevars(fs, e2);
			  luaK_concat(fs, ref e2.f, e1.f);
			  e1.Copy(e2);
			  break;
			}
			case BinOpr.OPR_OR: {
			  lua_assert(e1.f == NO_JUMP);  /* list closed by 'luK_infix' */
			  luaK_dischargevars(fs, e2);
			  luaK_concat(fs, ref e2.t, e1.t);
			  e1.Copy(e2);
			  break;
			}
			case BinOpr.OPR_CONCAT: {
		      luaK_exp2val(fs, e2);
		      if (e2.k == expkind.VRELOCABLE &&
		          GET_OPCODE(getinstruction(fs, e2)) == OpCode.OP_CONCAT) {
		        lua_assert(e1.u.info == GETARG_B(getinstruction(fs, e2))-1);
		        freeexp(fs, e1);
		        SETARG_B(getinstruction(fs, e2), e1.u.info);
		        e1.k = expkind.VRELOCABLE; e1.u.info = e2.u.info;
		      }
		      else {
		        luaK_exp2nextreg(fs, e2);  /* operand must be on the 'stack' */
		        codebinexpval(fs, OpCode.OP_CONCAT, e1, e2, line);
		      }
			  break;
			}
		    case BinOpr.OPR_ADD: case BinOpr.OPR_SUB: case BinOpr.OPR_MUL: case BinOpr.OPR_DIV:
		    case BinOpr.OPR_IDIV: case BinOpr.OPR_MOD: case BinOpr.OPR_POW:
		    case BinOpr.OPR_BAND: case BinOpr.OPR_BOR: case BinOpr.OPR_BXOR:
		    case BinOpr.OPR_SHL: case BinOpr.OPR_SHR: {
			  if (0==constfolding(fs, (int)op + LUA_OPADD, e1, e2))
			  	codebinexpval(fs, (OpCode)((int)op + OpCode.OP_ADD), e1, e2, line);
		      break;
		    }
		    case BinOpr.OPR_EQ: case BinOpr.OPR_LT: case BinOpr.OPR_LE:
		    case BinOpr.OPR_NE: case BinOpr.OPR_GT: case BinOpr.OPR_GE: {
		      codecomp(fs, op, e1, e2);
		      break;
		    }
			default: lua_assert(0); break;
		  }
		}


		/*
		** Change line information associated with current position.
		*/
		public static void luaK_fixline (FuncState fs, int line) {
		  fs.f.lineinfo[fs.pc - 1] = line;
		}


		/*
		** Emit a SETLIST instruction.
		** 'base' is register that keeps table;
		** 'nelems' is #table plus those to be stored now;
		** 'tostore' is number of values (in registers 'base + 1',...) to add to
		** table (or LUA_MULTRET to add up to stack top).
		*/
		public static void luaK_setlist (FuncState fs, int base_, int nelems, int tostore) {
		  int c =  (nelems - 1)/LFIELDS_PER_FLUSH + 1;
		  int b = (tostore == LUA_MULTRET) ? 0 : tostore;
		  lua_assert(tostore != 0 && tostore <= LFIELDS_PER_FLUSH);
		  if (c <= MAXARG_C)
			luaK_codeABC(fs, OpCode.OP_SETLIST, base_, b, c);
		  else if (c <= MAXARG_Ax) {
			luaK_codeABC(fs, OpCode.OP_SETLIST, base_, b, 0);
			codeextraarg(fs, c);
		  }
		  else
		    luaX_syntaxerror(fs.ls, "constructor too long");
		  fs.freereg = (byte)(base_ + 1);  /* free registers with list values */ //changed, (byte)
		}

	}
}

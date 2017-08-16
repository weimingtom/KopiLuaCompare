/*
** $Id: lcode.c,v 2.60 2011/08/30 16:26:41 roberto Exp $
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


	public partial class Lua
	{

		public static bool hasjumps(expdesc e)	{return e.t != e.f;}


		private static int isnumeral(expdesc e) {
		  return (e.k == expkind.VKNUM && e.t == NO_JUMP && e.f == NO_JUMP) ? 1 : 0;
		}


		public static void luaK_nil (FuncState fs, int from, int n) {
		  InstructionPtr previous;
          int l = from + n - 1;  /* last register to set nil */
		  if (fs.pc > fs.lasttarget) {  /* no jumps to current position? */
		    previous = new InstructionPtr(fs.f.code, fs.pc-1);
		    if (GET_OPCODE(previous) == OpCode.OP_LOADNIL) { //FIXME: no star
			  int pfrom = GETARG_A(previous); //FIXME: no star
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


		public static int luaK_jump (FuncState fs) {
		  int jpc = fs.jpc;  /* save list of jumps to here */
		  int j;
		  fs.jpc = NO_JUMP;
		  j = luaK_codeAsBx(fs, OpCode.OP_JMP, 0, NO_JUMP);
		  luaK_concat(fs, ref j, jpc);  /* keep them on hold */
		  return j;
		}


		public static void luaK_ret (FuncState fs, int first, int nret) {
			luaK_codeABC(fs, OpCode.OP_RETURN, first, nret + 1, 0);
		}


		private static int condjump (FuncState fs, OpCode op, int A, int B, int C) {
		  luaK_codeABC(fs, op, A, B, C);
		  return luaK_jump(fs);
		}


		private static void fixjump (FuncState fs, int pc, int dest) {
		  InstructionPtr jmp = new InstructionPtr(fs.f.code, pc);
		  int offset = dest-(pc+1);
		  lua_assert(dest != NO_JUMP);
		  if (Math.Abs(offset) > MAXARG_sBx)
			luaX_syntaxerror(fs.ls, "control structure too long");
		  SETARG_sBx(jmp, offset);
		}


		/*
		** returns current `pc' and marks it as a jump target (to avoid wrong
		** optimizations with consecutive instructions not in the same basic block).
		*/
		public static int luaK_getlabel (FuncState fs) {
		  fs.lasttarget = fs.pc;
		  return fs.pc;
		}


		private static int getjump (FuncState fs, int pc) {
		  int offset = GETARG_sBx(fs.f.code[pc]);
		  if (offset == NO_JUMP)  /* point to itself represents end of list */
			return NO_JUMP;  /* end of list */
		  else
			return (pc+1)+offset;  /* turn offset into absolute position */
		}


		private static InstructionPtr getjumpcontrol (FuncState fs, int pc) {
		  InstructionPtr pi = new InstructionPtr(fs.f.code, pc);
		  if (pc >= 1 && (testTMode(GET_OPCODE(pi[-1]))!=0))
			return new InstructionPtr(pi.codes, pi.pc-1);
		  else
			return new InstructionPtr(pi.codes, pi.pc);
		}


		/*
		** check whether list has any jump that do not produce a value
		** (or produce an inverted value)
		*/
		private static int need_value (FuncState fs, int list) {
		  for (; list != NO_JUMP; list = getjump(fs, list)) {
			InstructionPtr i = getjumpcontrol(fs, list);
			if (GET_OPCODE(i[0]) != OpCode.OP_TESTSET) return 1;
		  }
		  return 0;  /* not found */
		}


		private static int patchtestreg (FuncState fs, int node, int reg) {
		  InstructionPtr i = getjumpcontrol(fs, node);
		  if (GET_OPCODE(i[0]) != OpCode.OP_TESTSET)
			return 0;  /* cannot patch other instructions */
		if (reg != NO_REG && reg != GETARG_B(i[0]))
			SETARG_A(i, reg);
		  else  /* no register to put value or register already has the value */
			i[0] = (uint)CREATE_ABC(OpCode.OP_TEST, GETARG_B(i[0]), 0, GETARG_C(i[0]));

		  return 1;
		}


		private static void removevalues (FuncState fs, int list) {
		  for (; list != NO_JUMP; list = getjump(fs, list))
			  patchtestreg(fs, list, NO_REG);
		}


		private static void patchlistaux (FuncState fs, int list, int vtarget, int reg,
								  int dtarget) {
		  while (list != NO_JUMP) {
			int next = getjump(fs, list);
			if (patchtestreg(fs, list, reg) != 0)
			  fixjump(fs, list, vtarget);
			else
			  fixjump(fs, list, dtarget);  /* jump to default target */
			list = next;
		  }
		}


		private static void dischargejpc (FuncState fs) {
		  patchlistaux(fs, fs.jpc, fs.pc, NO_REG, fs.pc);
		  fs.jpc = NO_JUMP;
		}


		public static void luaK_patchlist (FuncState fs, int list, int target) {
		  if (target == fs.pc)
			luaK_patchtohere(fs, list);
		  else {
			lua_assert(target < fs.pc);
			patchlistaux(fs, list, target, NO_REG, target);
		  }
		}


		public static void luaK_patchclose (FuncState fs, int list, int level) {
		  level++;  /* argument is +1 to reserve 0 as non-op */
		  while (list != NO_JUMP) {
		    int next = getjump(fs, list);
		    lua_assert(GET_OPCODE(fs.f.code[list]) == OpCode.OP_JMP &&
		                (GETARG_A(fs.f.code[list]) == 0 ||
		                 GETARG_A(fs.f.code[list]) >= level));
		    SETARG_A(new InstructionPtr(fs.f.code, list), level);
		    list = next;
		  }
		}


		public static void luaK_patchtohere (FuncState fs, int list) {
		  luaK_getlabel(fs);
		  luaK_concat(fs, ref fs.jpc, list);
		}


		public static void luaK_concat(FuncState fs, ref int l1, int l2) {
		  if (l2 == NO_JUMP) return;
		  else if (l1 == NO_JUMP)
			l1 = l2;
		  else {
			int list = l1;
			int next;
			while ((next = getjump(fs, list)) != NO_JUMP)  /* find last element */
			  list = next;
			fixjump(fs, list, l2);
		  }
		}

        //------------------>
		private static int luaK_code (FuncState fs, Instruction i) {			
		  Proto f = fs.f;
		  dischargejpc(fs);  /* `pc' will change */
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


		public static int luaK_codeABC (FuncState fs, OpCode o, int a, int b, int c) {
		  lua_assert(getOpMode(o) == OpMode.iABC);
		  lua_assert(getBMode(o) != OpArgMask.OpArgN || b == 0);
		  lua_assert(getCMode(o) != OpArgMask.OpArgN || c == 0);
          lua_assert(a <= MAXARG_A && b <= MAXARG_B && c <= MAXARG_C);
          return luaK_code(fs, (Instruction)CREATE_ABC(o, a, b, c)); //FIXME: added (Instruction)
		}


		public static int luaK_codeABx (FuncState fs, OpCode o, int a, uint bc) {			
		  lua_assert(getOpMode(o) == OpMode.iABx || getOpMode(o) == OpMode.iAsBx);
		  lua_assert(getCMode(o) == OpArgMask.OpArgN);
          lua_assert(a <= MAXARG_A && bc <= MAXARG_Bx);
          return luaK_code(fs, (Instruction)CREATE_ABx(o, a, (int)bc)); //FIXME: added (Instruction) added (int)
		}


		private static int codeextraarg (FuncState fs, int a) {
		  lua_assert(a <= MAXARG_Ax);
		  return luaK_code(fs, (Instruction)CREATE_Ax(OpCode.OP_EXTRAARG, a)); //FIXME: added (Instruction)
		}


		public static int luaK_codek (FuncState fs, int reg, int k) {
		  if (k <= MAXARG_Bx)
		  	return luaK_codeABx(fs, OpCode.OP_LOADK, reg, (uint)(k)); //FIXME: added (uint)
		  else {
		    int p = luaK_codeABx(fs, OpCode.OP_LOADKX, reg, 0);
		    codeextraarg(fs, k);
		    return p;
		  }
		}

        //<------------------

		public static void luaK_checkstack (FuncState fs, int n) {
		  int newstack = fs.freereg + n;
		  if (newstack > fs.f.maxstacksize) {
			if (newstack >= MAXSTACK)
			  luaX_syntaxerror(fs.ls, "function or expression too complex");
			fs.f.maxstacksize = cast_byte(newstack);
		  }
		}


		public static void luaK_reserveregs (FuncState fs, int n) {
		  luaK_checkstack(fs, n);
		  fs.freereg += (byte)n; //FIXME:changed, (byte)
		}


		private static void freereg (FuncState fs, int reg) {
		  if ((ISK(reg)==0) && reg >= fs.nactvar) {
			fs.freereg--;
			lua_assert(reg == fs.freereg);
		  }
		}


		private static void freeexp (FuncState fs, expdesc e) {
		  if (e.k == expkind.VNONRELOC)
			freereg(fs, e.u.info);
		}


		private static int addk (FuncState fs, TValue key, TValue v) {
		  lua_State L = fs.ls.L;
		  TValue idx = luaH_set(L, fs.h, key);
		  Proto f = fs.f;
		  int k, oldsize;
		  if (ttisnumber(idx)) {
		    lua_Number n = nvalue(idx);
		    lua_number2int(out k, n);
			if (luaV_rawequalobj(f.k[k], v) != 0)
		      return k;
		    /* else may be a collision (e.g., between 0.0 and "\0\0\0\0\0\0\0\0");
		       go through and create a new entry for this value */
		  }
		  /* constant not found; create a new entry */
		  oldsize = f.sizek;
		  k = fs.nk;
		  /* numerical value does not need GC barrier;
		     table has no metatable, so it does not need to invalidate cache */
		  setnvalue(idx, cast_num(fs.nk));
		  luaM_growvector(L, ref f.k, k, ref f.sizek, MAXARG_Ax, "constants");
		  while (oldsize < f.sizek) setnilvalue(f.k[oldsize++]);
		  setobj(L, f.k[k], v);
          fs.nk++;
		  luaC_barrier(L, f, v);
		  return k;
		}


		public static int luaK_stringK (FuncState fs, TString s) {
		  TValue o = new TValue();
		  setsvalue(fs.ls.L, o, s);
		  return addk(fs, o, o);
		}


		public static int luaK_numberK (FuncState fs, lua_Number r) {
		  int n;
		  lua_State L = fs.ls.L;
		  TValue o = new TValue();
		  setnvalue(o, r);
		  if (r == 0 || luai_numisnan(null, r)) {  /* handle -0 and NaN */
		    /* use raw representation as key to avoid numeric problems */
		    setsvalue(L, L.top, luaS_newlstr(L, CharPtr.FromNumber(r), (uint)GetUnmanagedSize(typeof(lua_Number)))); //FIXME:???
		     incr_top(L);
		     n = addk(fs, L.top - 1, o);
		     StkId.dec(ref L.top);
		  }
		  else
		    n = addk(fs, o, o);  /* regular case */
		  return n;
		}


		private static int boolK (FuncState fs, int b) {
		  TValue o = new TValue();
		  setbvalue(o, b);
		  return addk(fs, o, o);
		}


		private static int nilK (FuncState fs) {
		  TValue k = new TValue(), v = new TValue();
		  setnilvalue(v);
		  /* cannot use nil as key; instead use table itself to represent nil */
		  sethvalue(fs.ls.L, k, fs.h);
		  return addk(fs, k, v);
		}


		public static void luaK_setreturns (FuncState fs, expdesc e, int nresults) {
		  if (e.k == expkind.VCALL) {  /* expression is an open function call? */
			SETARG_C(getcode(fs, e), nresults+1);
		  }
		  else if (e.k == expkind.VVARARG) {
			SETARG_B(getcode(fs, e), nresults+1);
			SETARG_A(getcode(fs, e), fs.freereg);
			luaK_reserveregs(fs, 1);
		  }
		}


		public static void luaK_setoneret (FuncState fs, expdesc e) {
		  if (e.k == expkind.VCALL) {  /* expression is an open function call? */
			e.k = expkind.VNONRELOC;
			e.u.info = GETARG_A(getcode(fs, e));
		  }
		  else if (e.k == expkind.VVARARG) {
			SETARG_B(getcode(fs, e), 2);
			e.k = expkind.VRELOCABLE;  /* can relocate its simple result */
		  }
		}


		public static void luaK_dischargevars (FuncState fs, expdesc e) {
		  switch (e.k) {
			case expkind.VLOCAL: {
			  e.k = expkind.VNONRELOC;
			  break;
			}
			case expkind.VUPVAL: {
			  e.u.info = luaK_codeABC(fs, OpCode.OP_GETUPVAL, 0, e.u.info, 0);
			  e.k = expkind.VRELOCABLE;
			  break;
			}
			case expkind.VINDEXED: {
		      OpCode op = OpCode.OP_GETTABUP;  /* assume 't' is in an upvalue */
		      freereg(fs, e.u.ind.idx);
		      if (e.u.ind.vt == (byte)expkind.VLOCAL) {  /* 't' is in a register? */ //FIXME:changed, (byte)
		        freereg(fs, e.u.ind.t);
		        op = OpCode.OP_GETTABLE;
		      }
		      e.u.info = luaK_codeABC(fs, op, 0, e.u.ind.t, e.u.ind.idx);
		      e.k = expkind.VRELOCABLE;
		      break;
			}
			case expkind.VVARARG:
			case expkind.VCALL: {
			  luaK_setoneret(fs, e);
			  break;
			}
			default: break;  /* there is one value available (somewhere) */
		  }
		}


		private static int code_label (FuncState fs, int A, int b, int jump) {
		  luaK_getlabel(fs);  /* those instructions may be jump targets */
		  return luaK_codeABC(fs, OpCode.OP_LOADBOOL, A, b, jump);
		}


		private static void discharge2reg (FuncState fs, expdesc e, int reg) {
		  luaK_dischargevars(fs, e);
		  switch (e.k) {
			case expkind.VNIL: {
			  luaK_nil(fs, reg, 1);
			  break;
			}
			case expkind.VFALSE:  case expkind.VTRUE: {
				luaK_codeABC(fs, OpCode.OP_LOADBOOL, reg, (e.k == expkind.VTRUE) ? 1 : 0, 0);
			  break;
			}
			case expkind.VK: {
			  luaK_codek(fs, reg, e.u.info);
			  break;
			}
			case expkind.VKNUM: {
			  luaK_codek(fs, reg, luaK_numberK(fs, e.u.nval));
			  break;
			}
			case expkind.VRELOCABLE: {
			  InstructionPtr pc = getcode(fs, e);
			  SETARG_A(pc, reg);
			  break;
			}
			case expkind.VNONRELOC: {
			  if (reg != e.u.info)
				luaK_codeABC(fs, OpCode.OP_MOVE, reg, e.u.info, 0);
			  break;
			}
			default: {
			  lua_assert(e.k == expkind.VVOID || e.k == expkind.VJMP);
			  return;  /* nothing to do... */
			}
		  }
		  e.u.info = reg;
		  e.k = expkind.VNONRELOC;
		}


		private static void discharge2anyreg (FuncState fs, expdesc e) {
		  if (e.k != expkind.VNONRELOC) {
			luaK_reserveregs(fs, 1);
			discharge2reg(fs, e, fs.freereg-1);
		  }
		}


		private static void exp2reg (FuncState fs, expdesc e, int reg) {
		  discharge2reg(fs, e, reg);
		  if (e.k == expkind.VJMP)
			luaK_concat(fs, ref e.t, e.u.info);  /* put this jump in `t' list */
		  if (hasjumps(e)) {
			int final;  /* position after whole expression */
			int p_f = NO_JUMP;  /* position of an eventual LOAD false */
			int p_t = NO_JUMP;  /* position of an eventual LOAD true */
			if (need_value(fs, e.t)!=0 || need_value(fs, e.f)!=0) {
			  int fj = (e.k == expkind.VJMP) ? NO_JUMP : luaK_jump(fs);
			  p_f = code_label(fs, reg, 0, 1);
			  p_t = code_label(fs, reg, 1, 0);
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


		public static void luaK_exp2nextreg (FuncState fs, expdesc e) {
		  luaK_dischargevars(fs, e);
		  freeexp(fs, e);
		  luaK_reserveregs(fs, 1);
		  exp2reg(fs, e, fs.freereg - 1);
		}


		public static int luaK_exp2anyreg (FuncState fs, expdesc e) {
		  luaK_dischargevars(fs, e);
		  if (e.k == expkind.VNONRELOC) {
			if (!hasjumps(e)) return e.u.info;  /* exp is already in a register */
			if (e.u.info >= fs.nactvar) {  /* reg. is not a local? */
			  exp2reg(fs, e, e.u.info);  /* put value on it */
			  return e.u.info;
			}
		  }
		  luaK_exp2nextreg(fs, e);  /* default */
		  return e.u.info;
		}


		public static void luaK_exp2anyregup (FuncState fs, expdesc e) { //FIXME:public ???
		  if (e.k != expkind.VUPVAL || hasjumps(e))
		    luaK_exp2anyreg(fs, e);
		}


		public static void luaK_exp2val (FuncState fs, expdesc e) {
		  if (hasjumps(e))
			luaK_exp2anyreg(fs, e);
		  else
			luaK_dischargevars(fs, e);
		}


		public static int luaK_exp2RK (FuncState fs, expdesc e) {
		  luaK_exp2val(fs, e);
		  switch (e.k) {
			case expkind.VTRUE:
			case expkind.VFALSE:
			case expkind.VNIL: {
			  if (fs.nk <= MAXINDEXRK) {  /* constant fit in RK operand? */
		  		e.u.info = (e.k == expkind.VNIL)  ? nilK(fs) : boolK(fs, (e.k == expkind.VTRUE) ? 1 : 0);
				e.k = expkind.VK;
				return RKASK(e.u.info);
			  }
			  else break;
			}
		    case expkind.VKNUM: {
		      e.u.info = luaK_numberK(fs, e.u.nval);
		      e.k = expkind.VK;
		      /* go through */
			  goto case expkind.VK;//FIXME:
		    }
			case expkind.VK: {
			  if (e.u.info <= MAXINDEXRK)  /* constant fit in argC? */
				return RKASK(e.u.info);
			  else break;
			}
			default: break;
		  }
		  /* not a constant in the right range: put it in a register */
		  return luaK_exp2anyreg(fs, e);
		}


		public static void luaK_storevar (FuncState fs, expdesc var, expdesc ex) {
		  switch (var.k) {
			case expkind.VLOCAL: {
			  freeexp(fs, ex);
			  exp2reg(fs, ex, var.u.info);
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
			default: {
			  lua_assert(0);  /* invalid var kind to store */
			  break;
			}
		  }
		  freeexp(fs, ex);
		}


		public static void luaK_self (FuncState fs, expdesc e, expdesc key) {
		  int ereg;
		  luaK_exp2anyreg(fs, e);
          ereg = e.u.info;  /* register where 'e' was placed */
		  freeexp(fs, e);
		  e.u.info = fs.freereg;  /* base register for op_self */
		  e.k = expkind.VNONRELOC;
		  luaK_reserveregs(fs, 2);  /* function and 'self' produced by op_self */
		  luaK_codeABC(fs, OpCode.OP_SELF, e.u.info, ereg, luaK_exp2RK(fs, key));
		  freeexp(fs, key);
		}


		private static void invertjump (FuncState fs, expdesc e) {
		  InstructionPtr pc = getjumpcontrol(fs, e.u.info);
		  lua_assert(testTMode(GET_OPCODE(pc[0])) != 0 && GET_OPCODE(pc[0]) != OpCode.OP_TESTSET &&
												   GET_OPCODE(pc[0]) != OpCode.OP_TEST);
		  SETARG_A(pc, (GETARG_A(pc[0]) == 0) ? 1 : 0);
		}


		private static int jumponcond (FuncState fs, expdesc e, int cond) {
		  if (e.k == expkind.VRELOCABLE) {
			InstructionPtr ie = getcode(fs, e);
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


		public static void luaK_goiftrue (FuncState fs, expdesc e) {
		  int pc;  /* pc of last jump */
		  luaK_dischargevars(fs, e);
		  switch (e.k) {
			case expkind.VJMP: {
			  invertjump(fs, e);
			  pc = e.u.info;
			  break;
			}
		    case expkind.VK: case expkind.VKNUM: case expkind.VTRUE: {
			  pc = NO_JUMP;  /* always true; do nothing */
			  break;
			}
			default: {
			  pc = jumponcond(fs, e, 0);
			  break;
			}
		  }
		  luaK_concat(fs, ref e.f, pc);  /* insert last jump in `f' list */
		  luaK_patchtohere(fs, e.t);
		  e.t = NO_JUMP;
		}


		public static void luaK_goiffalse (FuncState fs, expdesc e) {
		  int pc;  /* pc of last jump */
		  luaK_dischargevars(fs, e);
		  switch (e.k) {
			case expkind.VJMP: {
			  pc = e.u.info;
			  break;
			}
			case expkind.VNIL: case expkind.VFALSE: {
			  pc = NO_JUMP;  /* always false; do nothing */
			  break;
			}
			default: {
			  pc = jumponcond(fs, e, 1);
			  break;
			}
		  }
		  luaK_concat(fs, ref e.t, pc);  /* insert last jump in `t' list */
		  luaK_patchtohere(fs, e.f);
		  e.f = NO_JUMP;
		}


		private static void codenot (FuncState fs, expdesc e) {
		  luaK_dischargevars(fs, e);
		  switch (e.k) {
			case expkind.VNIL: case expkind.VFALSE: {
				e.k = expkind.VTRUE;
			  break;
			}
			case expkind.VK: case expkind.VKNUM: case expkind.VTRUE: {
			  e.k = expkind.VFALSE;
			  break;
			}
			case expkind.VJMP: {
			  invertjump(fs, e);
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
			default: {
			  lua_assert(0);  /* cannot happen */
			  break;
			}
		  }
		  /* interchange true and false lists */
		  { int temp = e.f; e.f = e.t; e.t = temp; }
		  removevalues(fs, e.f);
		  removevalues(fs, e.t);
		}


		public static void luaK_indexed (FuncState fs, expdesc t, expdesc k) {
		  lua_assert(!hasjumps(t));
		  t.u.ind.t = (byte)t.u.info; //FIXME:added, (byte)
		  t.u.ind.idx = (short)luaK_exp2RK(fs, k); //FIXME:added, (short)
		  t.u.ind.vt = (t.k == expkind.VUPVAL) ? (byte)expkind.VUPVAL //FIXME:added, (byte)
		  	                                   : (byte)check_exp(vkisinreg(t.k), (byte)expkind.VLOCAL); //FIXME:added, (byte)
		  t.k = expkind.VINDEXED;
		}


		private static int constfolding (OpCode op, expdesc e1, expdesc e2) {
		  lua_Number r;
		  if ((isnumeral(e1)==0) || (isnumeral(e2)==0)) return 0;
		  if ((op == OpCode.OP_DIV || op == OpCode.OP_MOD) && e2.u.nval == 0)
		    return 0;  /* do not attempt to divide by 0 */
		  r = luaO_arith(op - OpCode.OP_ADD + LUA_OPADD, e1.u.nval, e2.u.nval);
		  e1.u.nval = r;
		  return 1;
		}


		private static void codearith (FuncState fs, OpCode op, 
		                               expdesc e1, expdesc e2, int line) {
		  if (constfolding(op, e1, e2) != 0)
			return;
		  else {
			int o2 = (op != OpCode.OP_UNM && op != OpCode.OP_LEN) ? luaK_exp2RK(fs, e2) : 0;
			int o1 = luaK_exp2RK(fs, e1);
		    if (o1 > o2) {
		      freeexp(fs, e1);
		      freeexp(fs, e2);
		    }
		    else {
		      freeexp(fs, e2);
		      freeexp(fs, e1);
		    }
			e1.u.info = luaK_codeABC(fs, op, 0, o1, o2);
			e1.k = expkind.VRELOCABLE;
            luaK_fixline(fs, line);
		  }
		}


		private static void codecomp (FuncState fs, OpCode op, int cond, expdesc e1,
																  expdesc e2) {
		  int o1 = luaK_exp2RK(fs, e1);
		  int o2 = luaK_exp2RK(fs, e2);
		  freeexp(fs, e2);
		  freeexp(fs, e1);
		  if (cond == 0 && op != OpCode.OP_EQ) {
			int temp;  /* exchange args to replace by `<' or `<=' */
			temp = o1; o1 = o2; o2 = temp;  /* o1 <==> o2 */
			cond = 1;
		  }
		  e1.u.info = condjump(fs, op, cond, o1, o2);
		  e1.k = expkind.VJMP;
		}


		public static void luaK_prefix (FuncState fs, UnOpr op, expdesc e, int line) {
		  expdesc e2 = new expdesc();
		  e2.t = e2.f = NO_JUMP; e2.k = expkind.VKNUM; e2.u.nval = 0;
		  switch (op) {
			case UnOpr.OPR_MINUS: {
			  if (isnumeral(e) != 0)  /* minus constant? */
		        e.u.nval = luai_numunm(null, e.u.nval);  /* fold it */
		      else {
				luaK_exp2anyreg(fs, e);
			    codearith(fs, OpCode.OP_UNM, e, e2, line);
              }
			  break;
			}
			case UnOpr.OPR_NOT: codenot(fs, e); break;
			case UnOpr.OPR_LEN: {
			  luaK_exp2anyreg(fs, e);  /* cannot operate on constants */
			  codearith(fs, OpCode.OP_LEN, e, e2, line);
			  break;
			}
			default: lua_assert(0); break;
		  }
		}


		public static void luaK_infix (FuncState fs, BinOpr op, expdesc v) {
		  switch (op) {
			case BinOpr.OPR_AND: {
			  luaK_goiftrue(fs, v);
			  break;
			}
			case BinOpr.OPR_OR: {
			  luaK_goiffalse(fs, v);
			  break;
			}
			case BinOpr.OPR_CONCAT: {
			  luaK_exp2nextreg(fs, v);  /* operand must be on the `stack' */
			  break;
			}
		    case BinOpr.OPR_ADD: case BinOpr.OPR_SUB: case BinOpr.OPR_MUL: case BinOpr.OPR_DIV:
		    case BinOpr.OPR_MOD: case BinOpr.OPR_POW: {
		      if (isnumeral(v) == 0) luaK_exp2RK(fs, v);
		      break;
		    }
			default: {
			  luaK_exp2RK(fs, v);
			  break;
			}
		  }
		}


		public static void luaK_posfix (FuncState fs, BinOpr op, 
		                                expdesc e1, expdesc e2, int line) {
		  switch (op) {
			case BinOpr.OPR_AND: {
			  lua_assert(e1.t == NO_JUMP);  /* list must be closed */
			  luaK_dischargevars(fs, e2);
			  luaK_concat(fs, ref e2.f, e1.f);
			  e1.Copy(e2);
			  break;
			}
			case BinOpr.OPR_OR: {
			  lua_assert(e1.f == NO_JUMP);  /* list must be closed */
			  luaK_dischargevars(fs, e2);
			  luaK_concat(fs, ref e2.t, e1.t);
			  e1.Copy(e2);
			  break;
			}
			case BinOpr.OPR_CONCAT: {
			  luaK_exp2val(fs, e2);
			  if (e2.k == expkind.VRELOCABLE && GET_OPCODE(getcode(fs, e2)) == OpCode.OP_CONCAT) {
				lua_assert(e1.u.info == GETARG_B(getcode(fs, e2))-1);
				freeexp(fs, e1);
				SETARG_B(getcode(fs, e2), e1.u.info);
				e1.k = expkind.VRELOCABLE; e1.u.info = e2.u.info;
			  }
			  else {
				luaK_exp2nextreg(fs, e2);  /* operand must be on the 'stack' */
				codearith(fs, OpCode.OP_CONCAT, e1, e2, line);
			  }
			  break;
			}
		    case BinOpr.OPR_ADD: case BinOpr.OPR_SUB: case BinOpr.OPR_MUL: case BinOpr.OPR_DIV:
		    case BinOpr.OPR_MOD: case BinOpr.OPR_POW: {
		      codearith(fs, (OpCode)(op - BinOpr.OPR_ADD + OpCode.OP_ADD), e1, e2, line);
		      break;
		    }
		    case BinOpr.OPR_EQ: case BinOpr.OPR_LT: case BinOpr.OPR_LE: {
		      codecomp(fs, (OpCode)(op - BinOpr.OPR_EQ + OpCode.OP_EQ), 1, e1, e2);
		      break;
		    }
		    case BinOpr.OPR_NE: case BinOpr.OPR_GT: case BinOpr.OPR_GE: {
		      codecomp(fs, (OpCode)(op - BinOpr.OPR_NE + OpCode.OP_EQ), 0, e1, e2);
		      break;
		    }
			default: lua_assert(0); break;
		  }
		}


		public static void luaK_fixline (FuncState fs, int line) {
		  fs.f.lineinfo[fs.pc - 1] = line;
		}


		public static void luaK_setlist (FuncState fs, int base_, int nelems, int tostore) {
		  int c =  (nelems - 1)/LFIELDS_PER_FLUSH + 1;
		  int b = (tostore == LUA_MULTRET) ? 0 : tostore;
		  lua_assert(tostore != 0);
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

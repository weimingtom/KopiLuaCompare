/*
** $Id: lcode.h,v 1.63 2013/12/30 20:47:58 roberto Exp $
** Code generator for Lua
** See Copyright Notice in lua.h
*/

namespace KopiLua
{
	public partial class Lua
	{
		/*
		** Marks the end of a patch list. It is an invalid value both as an absolute
		** address, and as a list link (would link an element to itself).
		*/
		public const int NO_JUMP = (-1);


		/*
		** grep "ORDER OPR" if you change these enums  (ORDER OP)
		*/
		public enum BinOpr {
		  OPR_ADD, OPR_SUB, OPR_MUL, OPR_MOD, OPR_POW,
		  OPR_DIV,
		  OPR_IDIV,
		  OPR_BAND, OPR_BOR, OPR_BXOR,
		  OPR_SHL, OPR_SHR,
		  OPR_CONCAT,
		  OPR_EQ, OPR_LT, OPR_LE,
		  OPR_NE, OPR_GT, OPR_GE,
		  OPR_AND, OPR_OR,
		  OPR_NOBINOPR
		};


		public enum UnOpr { OPR_MINUS, OPR_BNOT, OPR_NOT, OPR_LEN, OPR_NOUNOPR };


		public static InstructionPtr getcode(FuncState fs, expdesc e)	{return new InstructionPtr(fs.f.code, e.u.info);}

		public static int luaK_codeAsBx(FuncState fs, OpCode o, int A, int sBx)	{return luaK_codeABx(fs,o,A,(uint)(sBx+MAXARG_sBx));} //FIXME:added (uint)

		public static void luaK_setmultret(FuncState fs, expdesc e)	{luaK_setreturns(fs, e, LUA_MULTRET);}
		
		public static void luaK_jumpto(FuncState fs, int t)	{luaK_patchlist(fs, luaK_jump(fs), t);}
		
	}
}

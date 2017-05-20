/*
** $Id: lopcodes.c,v 1.37 2005/11/08 19:45:36 roberto Exp roberto $
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace KopiLua
{
	using lu_byte = System.Byte;
	using Instruction = System.UInt32;

	public partial class Lua
	{


		/* ORDER OP */

		private readonly static CharPtr[] luaP_opnames = {
		  "MOVE",
		  "LOADK",
		  "LOADBOOL",
		  "LOADNIL",
		  "GETUPVAL",
		  "GETGLOBAL",
		  "GETTABLE",
		  "SETGLOBAL",
		  "SETUPVAL",
		  "SETTABLE",
		  "NEWTABLE",
		  "SELF",
		  "ADD",
		  "SUB",
		  "MUL",
		  "DIV",
		  "MOD",
		  "POW",
		  "UNM",
		  "NOT",
		  "LEN",
		  "CONCAT",
		  "JMP",
		  "EQ",
		  "LT",
		  "LE",
		  "TEST",
		  "TESTSET",
		  "CALL",
		  "TAILCALL",
		  "RETURN",
		  "FORLOOP",
		  "FORPREP",
		  "TFORLOOP",
		  "SETLIST",
		  "CLOSE",
		  "CLOSURE",
		  "VARARG",
		};


		private static lu_byte opmode(lu_byte t, lu_byte a, OpArgMask b, OpArgMask c, OpMode m)
		{
			return (lu_byte)(((t) << 7) | ((a) << 6) | (((lu_byte)b) << 4) | (((lu_byte)c) << 2) | ((lu_byte)m));
		}

		private readonly static lu_byte[] luaP_opmodes = {
		/*       T  A    B       C     mode		   opcode	*/
		  opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iABC) 	/* OP_MOVE */
		 ,opmode(0, 1, OpArgMask.OpArgK, OpArgMask.OpArgN, OpMode.iABx)		/* OP_LOADK */
		 ,opmode(0, 1, OpArgMask.OpArgU, OpArgMask.OpArgU, OpMode.iABC)		/* OP_LOADBOOL */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iABC)		/* OP_LOADNIL */
		 ,opmode(0, 1, OpArgMask.OpArgU, OpArgMask.OpArgN, OpMode.iABC)		/* OP_GETUPVAL */
		 ,opmode(0, 1, OpArgMask.OpArgK, OpArgMask.OpArgN, OpMode.iABx)		/* OP_GETGLOBAL */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgK, OpMode.iABC)		/* OP_GETTABLE */
		 ,opmode(0, 0, OpArgMask.OpArgK, OpArgMask.OpArgN, OpMode.iABx)		/* OP_SETGLOBAL */
		 ,opmode(0, 0, OpArgMask.OpArgU, OpArgMask.OpArgN, OpMode.iABC)		/* OP_SETUPVAL */
		 ,opmode(0, 0, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_SETTABLE */
		 ,opmode(0, 1, OpArgMask.OpArgU, OpArgMask.OpArgU, OpMode.iABC)		/* OP_NEWTABLE */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgK, OpMode.iABC)		/* OP_SELF */
		 ,opmode(0, 1, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_ADD */
		 ,opmode(0, 1, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_SUB */
		 ,opmode(0, 1, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_MUL */
		 ,opmode(0, 1, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_DIV */
		 ,opmode(0, 1, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_MOD */
		 ,opmode(0, 1, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_POW */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iABC)		/* OP_UNM */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iABC)		/* OP_NOT */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iABC)		/* OP_LEN */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgR, OpMode.iABC)		/* OP_CONCAT */
		 ,opmode(0, 0, OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iAsBx)		/* OP_JMP */
		 ,opmode(1, 0, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_EQ */
		 ,opmode(1, 0, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_LT */
		 ,opmode(1, 0, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_LE */
		 ,opmode(1, 1, OpArgMask.OpArgR, OpArgMask.OpArgU, OpMode.iABC)		/* OP_TEST */
		 ,opmode(1, 1, OpArgMask.OpArgR, OpArgMask.OpArgU, OpMode.iABC)		/* OP_TESTSET */
		 ,opmode(0, 1, OpArgMask.OpArgU, OpArgMask.OpArgU, OpMode.iABC)		/* OP_CALL */
		 ,opmode(0, 1, OpArgMask.OpArgU, OpArgMask.OpArgU, OpMode.iABC)		/* OP_TAILCALL */
		 ,opmode(0, 0, OpArgMask.OpArgU, OpArgMask.OpArgN, OpMode.iABC)		/* OP_RETURN */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iAsBx)		/* OP_FORLOOP */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iAsBx)		/* OP_FORPREP */
		 ,opmode(1, 0, OpArgMask.OpArgN, OpArgMask.OpArgU, OpMode.iABC)		/* OP_TFORLOOP */
		 ,opmode(0, 0, OpArgMask.OpArgU, OpArgMask.OpArgU, OpMode.iABC)		/* OP_SETLIST */
		 ,opmode(0, 0, OpArgMask.OpArgN, OpArgMask.OpArgN, OpMode.iABC)		/* OP_CLOSE */
		 ,opmode(0, 1, OpArgMask.OpArgU, OpArgMask.OpArgN, OpMode.iABx)		/* OP_CLOSURE */
		 ,opmode(0, 1, OpArgMask.OpArgU, OpArgMask.OpArgN, OpMode.iABC)		/* OP_VARARG */
		};

	}
}

/*
** $Id: lparser.c,v 2.91 2010/08/23 17:32:34 roberto Exp roberto $
** Lua Parser
** See Copyright Notice in lua.h
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace KopiLua
{
	using lu_byte = System.Byte;
	using lua_Number = System.Double;
	using StkId = Lua.lua_TValue;

	public partial class Lua
	{

		/* maximum number of local variables per function (must be smaller
		   than 250, due to the bytecode format) */
		private const int MAXVARS = 200;


		public static int hasmultret(expkind k)		{return ((k) == expkind.VCALL || (k) == expkind.VVARARG) ? 1 : 0;}



		/*
		** nodes for block list (list of active blocks)
		*/
		public class BlockCnt {
		  public BlockCnt previous;  /* chain */
		  public int breaklist;  /* list of jumps out of this loop */
		  public lu_byte nactvar;  /* # active locals outside the breakable structure */
		  public lu_byte upval;  /* true if some variable in the block is an upvalue */
		  public lu_byte isbreakable;  /* true if `block' is a loop */
		};



		private static void anchor_token (LexState ls) {
		  /* last token from outer function must be EOS */
		  lua_assert(ls.fs != null || ls.t.token == (int)RESERVED.TK_EOS);
		  if (ls.t.token == (int)RESERVED.TK_NAME || ls.t.token == (int)RESERVED.TK_STRING) {
			TString ts = ls.t.seminfo.ts;
			luaX_newstring(ls, getstr(ts), ts.tsv.len);
		  }
		}


		private static void error_expected (LexState ls, int token) {
		  luaX_syntaxerror(ls,
			  luaO_pushfstring(ls.L, "%s expected", luaX_token2str(ls, token)));
		}


		private static void errorlimit (FuncState fs, int limit, CharPtr what) {
		  CharPtr msg;
          int line = fs.f.linedefined;
		  CharPtr where = (line == 0) 
		                  ? "main function" 
						  : luaO_pushfstring(fs.L, "function at line %d", line);
		  msg = luaO_pushfstring(fs.L, "too many %s (limit is %d) in %s",
		                                what, limit, where);
		  luaX_syntaxerror(fs.ls, msg);
		}


		private static void checklimit (FuncState fs, int v, int l, CharPtr what) {
		  if (v > l) errorlimit(fs, l, what);
		}


		private static int testnext (LexState ls, int c) {
		  if (ls.t.token == c) {
			luaX_next(ls);
			return 1;
		  }
		  else return 0;
		}


		private static void check (LexState ls, int c) {
		  if (ls.t.token != c)
			error_expected(ls, c);
		}

		private static void checknext (LexState ls, int c) {
		  check(ls, c);
		  luaX_next(ls);
		}


		public static void check_condition(LexState ls, bool c, CharPtr msg)	{
			if (!(c)) luaX_syntaxerror(ls, msg);
		}

		private static void check_match (LexState ls, int what, int who, int where) {
		  if (testnext(ls, what)==0) {
			if (where == ls.linenumber)
			  error_expected(ls, what);
			else {
			  luaX_syntaxerror(ls, luaO_pushfstring(ls.L,
					 "%s expected (to close %s at line %d)",
					  luaX_token2str(ls, what), luaX_token2str(ls, who), where));
			}
		  }
		}


		private static TString str_checkname (LexState ls) {
		  TString ts;
		  check(ls, (int)RESERVED.TK_NAME);
		  ts = ls.t.seminfo.ts;
		  luaX_next(ls);
		  return ts;
		}


		private static void init_exp (expdesc e, expkind k, int i) {
		  e.f = e.t = NO_JUMP;
		  e.k = k;
		  e.u.info = i;
		}


		private static void codestring (LexState ls, expdesc e, TString s) {
			init_exp(e, expkind.VK, luaK_stringK(ls.fs, s));
		}


		private static void checkname (LexState ls, expdesc e) {
		  codestring(ls, e, str_checkname(ls));
		}


		private static int registerlocalvar (LexState ls, TString varname) {
		  FuncState fs = ls.fs;
		  Proto f = fs.f;
		  int oldsize = f.sizelocvars;
		  luaM_growvector(ls.L, ref f.locvars, fs.nlocvars, ref f.sizelocvars,
						  (int)SHRT_MAX, "local variables");
		  while (oldsize < f.sizelocvars) f.locvars[oldsize++].varname = null;
		  f.locvars[fs.nlocvars].varname = varname;
		  luaC_objbarrier(ls.L, f, varname);
		  return fs.nlocvars++;
		}


		private static void new_localvar (LexState ls, TString name) {
		  FuncState fs = ls.fs;
		  Varlist vl = ls.varl;
		  int reg = registerlocalvar(ls, name);
		  checklimit(fs, vl.nactvar + 1 - fs.firstlocal,
		                  MAXVARS, "local variables");
		  luaM_growvector<vardesc>(ls.L, ref vl.actvar, vl.nactvar + 1,
		                  ref vl.actvarsize/*, vardesc*/, Int32.MaxValue, "local variables");
		  vl.actvar[vl.nactvar++].idx = (ushort)(reg);
		}


		private static void new_localvarliteral_ (LexState ls, CharPtr name, uint sz) {
		  new_localvar(ls, luaX_newstring(ls, name, sz));
		}

		private static void new_localvarliteral(LexState ls, string v) {
			new_localvarliteral_(ls, "" + v, /*(sizeof(v)/sizeof(char))-1)*/(uint)v.Length); }


		private static LocVar getlocvar (FuncState fs, int i) {
		  int idx = fs.ls.varl.actvar[fs.firstlocal + i].idx;
		  lua_assert(idx < fs.nlocvars);
		  return fs.f.locvars[idx];
		}


		private static void adjustlocalvars (LexState ls, int nvars) {
		  FuncState fs = ls.fs;
		  fs.nactvar = cast_byte(fs.nactvar + nvars);
		  for (; nvars!=0; nvars--) {
			getlocvar(fs, fs.nactvar - nvars).startpc = fs.pc;
		  }
		}


		private static void removevars (FuncState fs, int tolevel) {
          fs.ls.varl.nactvar -= (fs.nactvar - tolevel);
		  while (fs.nactvar > tolevel)
			getlocvar(fs, --fs.nactvar).endpc = fs.pc;
		}


		private static int searchupvalue (FuncState fs, TString name) {
		  int i;
		  Upvaldesc up = fs.f.upvalues;
		  for (i = 0; i < fs.nups; i++) {
		    if (eqstr(up[i].name, name)) return i;
		  }
		  return -1;  /* not found */
		}


		private static int newupvalue (FuncState fs, TString name, expdesc v) {
		  Proto f = fs.f;
		  int oldsize = f.sizeupvalues;
		  checklimit(fs, fs.nups + 1, MAXUPVAL, "upvalues");
		  luaM_growvector(fs.L, f.upvalues, fs.nups, f.sizeupvalues,
		                  Upvaldesc, MAXUPVAL, "upvalues");
		  while (oldsize < f.sizeupvalues) f.upvalues[oldsize++].name = null;
		  f.upvalues[fs.nups].instack = (v.k == expkind.VLOCAL);
		  f.upvalues[fs.nups].idx = (byte)(v.u.info); //FIXME:cast_byte
		  f.upvalues[fs.nups].name = name;
		  luaC_objbarrier(fs.L, f, name);
		  return fs.nups++;
		}


		private static int searchvar (FuncState fs, TString n) {
		  int i;
		  for (i=fs.nactvar-1; i >= 0; i--) {
			if (eqstr(n, getlocvar(fs, i).varname)
			  return i;
		  }
		  return -1;  /* not found */
		}


		/*
		  Mark block where variable at given level was defined
		  (to emit OP_CLOSE later).
		*/
		private static void markupval (FuncState fs, int level) {
		  BlockCnt bl = fs.bl;
		  while ((bl!=null) && bl.nactvar > level) bl = bl.previous;
		  if (bl != null) bl.upval = 1;
		}


		/*
		  Find variable with given name 'n'. If it is an upvalue, add this
		  upvalue into all intermediate functions.
		*/
		private static int singlevaraux(FuncState fs, TString n, expdesc var, int base_) {
		  if (fs == null)  /* no more levels? */
			return expkind.VVOID;  /* default is global */
		  else {
		    int v = searchvar(fs, n);  /* look up locals at current level */
		    if (v >= 0) {  /* found? */
		      init_exp(var, expkind.VLOCAL, v);  /* variable is local */
		      if (!base_)
		        markupval(fs, v);  /* local will be used as an upval */
		      return expkind.VLOCAL;
		    }
		    else {  /* not found as local at current level; try upvalues */
		      int idx = searchupvalue(fs, n);  /* try existing upvalues */
		      if (idx < 0) {  /* not found? */
		        if (singlevaraux(fs.prev, n, var, 0) == expkind.VVOID) /* try upper levels */
		          return expkind.VVOID;  /* not found; is a global */
		        /* else was LOCAL or UPVAL */
		        idx  = newupvalue(fs, n, var);  /* will be a new upvalue */
		      }
		      init_exp(var, expkind.VUPVAL, idx);
		      return expkind.VUPVAL;
		    }
		  }
		}


		private static void singlevar (LexState ls, expdesc var) {
		  TString varname = str_checkname(ls);
		  FuncState fs = ls.fs;
		  if (singlevaraux(fs, varname, var, 1) == expkind.VVOID) {  /* global name? */
		    expdesc key;
		    singlevaraux(fs, ls.envn, var, 1);  /* get environment variable */
		    lua_assert(var.k == expkind.VLOCAL || var.k == expkind.VUPVAL);
		    codestring(ls, &key, varname);  /* key is variable name */
		    luaK_indexed(fs, var, &key);  /* env[varname] */
		  }
		}


		private static void adjust_assign (LexState ls, int nvars, int nexps, expdesc e) {
		  FuncState fs = ls.fs;
		  int extra = nvars - nexps;
		  if (hasmultret(e.k) != 0) {
			extra++;  /* includes call itself */
			if (extra < 0) extra = 0;
			luaK_setreturns(fs, e, extra);  /* last exp. provides the difference */
			if (extra > 1) luaK_reserveregs(fs, extra-1);
		  }
		  else {
			if (e.k != expkind.VVOID) luaK_exp2nextreg(fs, e);  /* close last expression */
			if (extra > 0) {
			  int reg = fs.freereg;
			  luaK_reserveregs(fs, extra);
			  luaK_nil(fs, reg, extra);
			}
		  }
		}


		private static void enterlevel (LexState ls) {
		  global_State g = G(ls.L);
		  ++g.nCcalls;
		  checklimit(ls.fs, g.nCcalls, LUAI_MAXCCALLS, "syntax levels");
		}


		private static void leavelevel(LexState ls) { G(ls.L).nCcalls--; }


		private static void enterblock (FuncState fs, BlockCnt bl, lu_byte isbreakable) {
		  bl.breaklist = NO_JUMP;
		  bl.isbreakable = isbreakable;
		  bl.nactvar = fs.nactvar;
		  bl.upval = 0;
		  bl.previous = fs.bl;
		  fs.bl = bl;
		  lua_assert(fs.freereg == fs.nactvar);
		}


		private static void leaveblock (FuncState fs) {
		  BlockCnt bl = fs.bl;
		  fs.bl = bl.previous;
		  removevars(fs, bl.nactvar);
		  if (bl.upval != 0)
			luaK_codeABC(fs, OpCode.OP_CLOSE, bl.nactvar, 0, 0);
		  /* a block either controls scope or breaks (never both) */
		  lua_assert((bl.isbreakable==0) || (bl.upval==0));
		  lua_assert(bl.nactvar == fs.nactvar);
		  fs.freereg = fs.nactvar;  /* free registers */
		  luaK_patchtohere(fs, bl.breaklist);
		}


		/*
		** adds prototype being created into its parent list of prototypes
		** and codes instruction to create new closure
		*/
		private static void codeclosure (LexState ls, Proto clp, expdesc v) {
		  FuncState fs = ls.fs.prev;
		  Proto f = fs.f;  /* prototype of function creating new closure */
		  if (fs->np >= f->sizep) {
		    int oldsize = f.sizep;
		    luaM_growvector<Proto>(ls.L, ref f.p, fs.np, ref f.sizep, /*Proto *,*/
		                  MAXARG_Bx, "functions");
		    while (oldsize < f.sizep) f.p[oldsize++] = null;
		  }
		  f.p[fs.np++] = clp;
		  luaC_objbarrier(ls.L, f, clp);
		  init_exp(v, expkind.VRELOCABLE, luaK_codeABx(fs, OpCode.OP_CLOSURE, 0, (uint)(fs.np-1)));
		}


		private static void open_func (LexState ls, FuncState fs) {
		  lua_State L = ls.L;
		  Proto f;
		  fs.prev = ls.fs;  /* linked list of funcstates */
		  fs.ls = ls;
		  fs.L = L;
		  ls.fs = fs;
		  fs.pc = 0;
		  fs.lasttarget = 0;
		  fs.jpc = NO_JUMP;
		  fs.freereg = 0;
		  fs.nk = 0;
		  fs.np = 0;
  		  fs.nups = 0;
		  fs.nlocvars = 0;
		  fs.nactvar = 0;
		  fs.firstlocal = ls.varl.nactvar;
		  fs.bl = null;
		  f = luaF_newproto(L);
		  fs.f = f;
		  f.source = ls.source;
		  f.maxstacksize = 2;  /* registers 0/1 are always valid */
		  /* anchor prototype (to avoid being collected) */
		  setptvalue2s(L, L.top, f);
		  incr_top(L);
		  fs.h = luaH_new(L);
		  /* anchor table of constants (to avoid being collected) */
		  sethvalue2s(L, L.top, fs.h);
		  incr_top(L);
		}

		static Proto lastfunc; //FIXME: added

		private static void close_func (LexState ls) {
		  lua_State L = ls.L;
		  FuncState fs = ls.fs;
		  Proto f = fs.f;
		  lastfunc = f; //FIXME:added, ???
		  luaK_ret(fs, 0, 0);  /* final return */
		  removevars(fs, 0);
		  luaM_reallocvector(L, ref f.code, f.sizecode, fs.pc/*, typeof(Instruction)*/);
		  f.sizecode = fs.pc;
		  luaM_reallocvector(L, ref f.lineinfo, f.sizelineinfo, fs.pc/*, typeof(int)*/);
		  f.sizelineinfo = fs.pc;
		  luaM_reallocvector(L, ref f.k, f.sizek, fs.nk/*, TValue*/);
		  f.sizek = fs.nk;
		  luaM_reallocvector(L, ref f.p, f.sizep, fs.np/*, Proto*/);		  
		  f.sizep = fs.np;
		  for (int i = 0; i < f.p.Length; i++)
		  {
			  f.p[i].protos = f.p;
			  f.p[i].index = i;
		  }
		  luaM_reallocvector(L, ref f.locvars, f.sizelocvars, fs.nlocvars/*, LocVar*/);
		  f.sizelocvars = fs.nlocvars;
		  luaM_reallocvector(L, ref f.upvalues, f.sizeupvalues, fs.nups/*, Upvaldesc*/);
		  f.sizeupvalues = fs.nups;
		  lua_assert(fs.bl == null);
		  ls.fs = fs.prev;
		  /* last token read was anchored in defunct function; must re-anchor it */
		  anchor_token(ls);
		  StkId.dec(ref L.top);  /* pop table of constants */
		  luaC_checkGC(L);
		  StkId.dec(ref L.top);  /* pop prototype (after possible collection) */
		}


		/*
		** opens the main function, which is a regular vararg function with an
		** upvalue named LUA_ENV
		*/
		private static void open_mainfunc (LexState ls, FuncState fs) {
		  expdesc v;
		  open_func(ls, fs);
		  fs->f->is_vararg = 1;  /* main function is always vararg */
		  init_exp(&v, VLOCAL, 0);
		  newupvalue(fs, ls->envn, &v);  /* create environment upvalue */
		}


		public static Proto luaY_parser (lua_State L, ZIO z, Mbuffer buff, Varlist varl, 
										 CharPtr name) {
		  LexState lexstate = new LexState();
		  FuncState funcstate = new FuncState();
		  TString tname = luaS_new(L, name);
		  setsvalue2s(L, L.top, tname);  /* push name to protect it */
		  incr_top(L);
		  lexstate.buff = buff;
          lexstate.varl = varl;
		  luaX_setinput(L, lexstate, z, tname);
		  open_mainfunc(&lexstate, &funcstate);
		  luaX_next(lexstate);  /* read first token */
		  chunk(lexstate);  /* read main chunk */
		  check(lexstate, (int)RESERVED.TK_EOS);
		  close_func(lexstate);
		  StkId.dec(ref L.top);  /* pop name */
		  lua_assert(!funcstate.prev && funcstate.nups == 1 && !lexstate.fs);
		  return funcstate.f;
		}



		/*============================================================*/
		/* GRAMMAR RULES */
		/*============================================================*/


		private static void fieldsel (LexState ls, expdesc v) {
		  /* fieldsel . ['.' | ':'] NAME */
		  FuncState fs = ls.fs;
		  expdesc key = new expdesc();
		  luaK_exp2anyregup(fs, v);
		  luaX_next(ls);  /* skip the dot or colon */
		  checkname(ls, key);
		  luaK_indexed(fs, v, key);
		}


		private static void yindex (LexState ls, expdesc v) {
		  /* index . '[' expr ']' */
		  luaX_next(ls);  /* skip the '[' */
		  expr(ls, v);
		  luaK_exp2val(ls.fs, v);
		  checknext(ls, ']');
		}


		/*
		** {======================================================================
		** Rules for Constructors
		** =======================================================================
		*/


		public class ConsControl {
		  public expdesc v = new expdesc();  /* last list item read */
		  public expdesc t;  /* table descriptor */
		  public int nh;  /* total number of `record' elements */
		  public int na;  /* total number of array elements */
		  public int tostore;  /* number of array elements pending to be stored */
		};


		private static void recfield (LexState ls, ConsControl cc) {
		  /* recfield . (NAME | `['exp1`]') = exp1 */
		  FuncState fs = ls.fs;
		  int reg = ls.fs.freereg;
		  expdesc key = new expdesc(), val = new expdesc();
		  int rkkey;
		  if (ls.t.token == (int)RESERVED.TK_NAME) {
			checklimit(fs, cc.nh, MAX_INT, "items in a constructor");
			checkname(ls, key);
		  }
		  else  /* ls.t.token == '[' */
			yindex(ls, key);
		  cc.nh++;
		  checknext(ls, '=');
		  rkkey = luaK_exp2RK(fs, key);
		  expr(ls, val);
		  luaK_codeABC(fs, OpCode.OP_SETTABLE, cc.t.u.info, rkkey, luaK_exp2RK(fs, val));
		  fs.freereg = reg;  /* free registers */
		}


		private static void closelistfield (FuncState fs, ConsControl cc) {
		  if (cc.v.k == expkind.VVOID) return;  /* there is no list item */
		  luaK_exp2nextreg(fs, cc.v);
		  cc.v.k = expkind.VVOID;
		  if (cc.tostore == LFIELDS_PER_FLUSH) {
			luaK_setlist(fs, cc.t.u.info, cc.na, cc.tostore);  /* flush */
			cc.tostore = 0;  /* no more items pending */
		  }
		}


		private static void lastlistfield (FuncState fs, ConsControl cc) {
		  if (cc.tostore == 0) return;
		  if (hasmultret(cc.v.k) != 0) {
			luaK_setmultret(fs, cc.v);
			luaK_setlist(fs, cc.t.u.info, cc.na, LUA_MULTRET);
			cc.na--;  /* do not count last expression (unknown number of elements) */
		  }
		  else {
			if (cc.v.k != expkind.VVOID)
			  luaK_exp2nextreg(fs, cc.v);
			luaK_setlist(fs, cc.t.u.info, cc.na, cc.tostore);
		  }
		}


		private static void listfield (LexState ls, ConsControl cc) {
          /* listfield -> exp */
		  expr(ls, cc.v);
		  checklimit(ls.fs, cc.na, MAX_INT, "items in a constructor");
		  cc.na++;
		  cc.tostore++;
		}


		private static void field (LexState ls, ConsControl cc) {
		  /* field -> listfield | recfield */
		  switch(ls.t.token) {
		  	case (int)RESERVED.TK_NAME: {  /* may be 'listfield' or 'recfield' */
		      if (luaX_lookahead(ls) != '=')  /* expression? */
		        listfield(ls, cc);
		      else
		        recfield(ls, cc);
		      break;
		    }
		    case '[': {
		      recfield(ls, cc);
		      break;
		    }
		    default: {
		      listfield(ls, cc);
		      break;
		    }
		  }
		}


		private static void constructor (LexState ls, expdesc t) {
		  /* constructor -> '{' [ field { sep field } [sep] ] '}' 
		     sep -> ',' | ';' */
		  FuncState fs = ls.fs;
		  int line = ls.linenumber;
		  int pc = luaK_codeABC(fs, OpCode.OP_NEWTABLE, 0, 0, 0);
		  ConsControl cc = new ConsControl();
		  cc.na = cc.nh = cc.tostore = 0;
		  cc.t = t;
		  init_exp(t, expkind.VRELOCABLE, pc);
		  init_exp(cc.v, expkind.VVOID, 0);  /* no value (yet) */
		  luaK_exp2nextreg(ls.fs, t);  /* fix it at stack top (for gc) */
		  checknext(ls, '{');
		  do {
			lua_assert(cc.v.k == expkind.VVOID || cc.tostore > 0);
			if (ls.t.token == '}') break;
			closelistfield(fs, cc);
			field(ls, cc);
		  } while ((testnext(ls, ',')!=0) || (testnext(ls, ';')!=0));
		  check_match(ls, '}', '{', line);
		  lastlistfield(fs, cc);
		  SETARG_B(new InstructionPtr(fs.f.code, pc), luaO_int2fb((uint)cc.na)); /* set initial array size */
		  SETARG_C(new InstructionPtr(fs.f.code, pc), luaO_int2fb((uint)cc.nh));  /* set initial table size */
		}

		/* }====================================================================== */



		private static void parlist (LexState ls) {
		  /* parlist . [ param { `,' param } ] */
		  FuncState fs = ls.fs;
		  Proto f = fs.f;
		  int nparams = 0;
		  f.is_vararg = 0;
		  if (ls.t.token != ')') {  /* is `parlist' not empty? */
			do {
			  switch (ls.t.token) {
				case (int)RESERVED.TK_NAME: {  /* param . NAME */
				  new_localvar(ls, str_checkname(ls));
                  nparams++;
				  break;
				}
				case (int)RESERVED.TK_DOTS: {  /* param . `...' */
				  luaX_next(ls);
				  f.is_vararg = 1;
				  break;
				}
				default: luaX_syntaxerror(ls, "<name> or " + LUA_QL("...") + " expected"); break;
			  }
			} while ((f.is_vararg==0) && (testnext(ls, ',')!=0));
		  }
		  adjustlocalvars(ls, nparams);
		  f.numparams = cast_byte(fs.nactvar);
		  luaK_reserveregs(fs, fs.nactvar);  /* reserve register for parameters */
		}


		private static void body (LexState ls, expdesc e, int needself, int line) {
		  /* body .  `(' parlist `)' chunk END */
		  FuncState new_fs = new FuncState();
		  open_func(ls, new_fs);
		  new_fs.f.linedefined = line;
		  checknext(ls, '(');
		  if (needself != 0) {
			new_localvarliteral(ls, "self");
			adjustlocalvars(ls, 1);
		  }
		  parlist(ls);
		  checknext(ls, ')');
		  chunk(ls);
		  new_fs.f.lastlinedefined = ls.linenumber;
		  check_match(ls, (int)RESERVED.TK_END, (int)RESERVED.TK_FUNCTION, line);
		  codeclosure(ls, new_fs.f, e);
		  close_func(ls);
		}


		private static int explist1 (LexState ls, expdesc v) {
		  /* explist1 . expr { `,' expr } */
		  int n = 1;  /* at least one expression */
		  expr(ls, v);
		  while (testnext(ls, ',') != 0) {
			luaK_exp2nextreg(ls.fs, v);
			expr(ls, v);
			n++;
		  }
		  return n;
		}


		private static void funcargs (LexState ls, expdesc f, int line) {
		  FuncState fs = ls.fs;
		  expdesc args = new expdesc();
		  int base_, nparams;
		  switch (ls.t.token) {
			case '(': {  /* funcargs . `(' [ explist1 ] `)' */
			  luaX_next(ls);
			  if (ls.t.token == ')')  /* arg list is empty? */
				args.k = expkind.VVOID;
			  else {
				explist1(ls, args);
				luaK_setmultret(fs, args);
			  }
			  check_match(ls, ')', '(', line);
			  break;
			}
			case '{': {  /* funcargs . constructor */
			  constructor(ls, args);
			  break;
			}
			case (int)RESERVED.TK_STRING: {  /* funcargs . STRING */
			  codestring(ls, args, ls.t.seminfo.ts);
			  luaX_next(ls);  /* must use `seminfo' before `next' */
			  break;
			}
			default: {
			  luaX_syntaxerror(ls, "function arguments expected");
			  return;
			}
		  }
		  lua_assert(f.k == expkind.VNONRELOC);
		  base_ = f.u.info;  /* base_ register for call */
		  if (hasmultret(args.k) != 0)
			nparams = LUA_MULTRET;  /* open call */
		  else {
			if (args.k != expkind.VVOID)
			  luaK_exp2nextreg(fs, args);  /* close last argument */
			nparams = fs.freereg - (base_+1);
		  }
		  init_exp(f, expkind.VCALL, luaK_codeABC(fs, OpCode.OP_CALL, base_, nparams + 1, 2));
		  luaK_fixline(fs, line);
		  fs.freereg = base_+1;  /* call remove function and arguments and leaves
									(unless changed) one result */
		}




		/*
		** {======================================================================
		** Expression parsing
		** =======================================================================
		*/


		private static void prefixexp (LexState ls, expdesc v) {
		  /* prefixexp . NAME | '(' expr ')' */
		  switch (ls.t.token) {
			case '(': {
			  int line = ls.linenumber;
			  luaX_next(ls);
			  expr(ls, v);
			  check_match(ls, ')', '(', line);
			  luaK_dischargevars(ls.fs, v);
			  return;
			}
			case (int)RESERVED.TK_NAME: {
			  singlevar(ls, v);
			  return;
			}
			default: {
			  luaX_syntaxerror(ls, "unexpected symbol");
			  return;
			}
		  }
		}


		private static void primaryexp (LexState ls, expdesc v) {
		  /* primaryexp .
				prefixexp { `.' NAME | `[' exp `]' | `:' NAME funcargs | funcargs } */
		  FuncState fs = ls.fs;
          int line = ls.linenumber;
		  prefixexp(ls, v);
		  for (;;) {
			switch (ls.t.token) {
			  case '.': {  /* fieldsel */
				fieldsel(ls, v);
				break;
			  }
			  case '[': {  /* `[' exp1 `]' */
				expdesc key = new expdesc();
				luaK_exp2anyregup(fs, v);
				yindex(ls, key);
				luaK_indexed(fs, v, key);
				break;
			  }
			  case ':': {  /* `:' NAME funcargs */
				expdesc key = new expdesc();
				luaX_next(ls);
				checkname(ls, key);
				luaK_self(fs, v, key);
				funcargs(ls, v, line);
				break;
			  }
			  case '(': case (int)RESERVED.TK_STRING: case '{': {  /* funcargs */
				luaK_exp2nextreg(fs, v);
				funcargs(ls, v, line);
				break;
			  }
			  default: return;
			}
		  }
		}


		private static void simpleexp (LexState ls, expdesc v) {
		  /* simpleexp . NUMBER | STRING | NIL | TRUE | FALSE | ... |
						  constructor | FUNCTION body | primaryexp */
		  switch (ls.t.token) {
			case (int)RESERVED.TK_NUMBER: {
			  init_exp(v, expkind.VKNUM, 0);
			  v.u.nval = ls.t.seminfo.r;
			  break;
			}
			case (int)RESERVED.TK_STRING: {
			  codestring(ls, v, ls.t.seminfo.ts);
			  break;
			}
			case (int)RESERVED.TK_NIL: {
			  init_exp(v, expkind.VNIL, 0);
			  break;
			}
			case (int)RESERVED.TK_TRUE: {
			  init_exp(v, expkind.VTRUE, 0);
			  break;
			}
			case (int)RESERVED.TK_FALSE: {
			  init_exp(v, expkind.VFALSE, 0);
			  break;
			}
			case (int)RESERVED.TK_DOTS: {  /* vararg */
			  FuncState fs = ls.fs;
			  check_condition(ls, fs.f.is_vararg!=0,
							  "cannot use " + LUA_QL("...") + " outside a vararg function");
			  init_exp(v, expkind.VVARARG, luaK_codeABC(fs, OpCode.OP_VARARG, 0, 1, 0));
			  break;
			}
			case '{': {  /* constructor */
			  constructor(ls, v);
			  return;
			}
			case (int)RESERVED.TK_FUNCTION: {
			  luaX_next(ls);
			  body(ls, v, 0, ls.linenumber);
			  return;
			}
			default: {
			  primaryexp(ls, v);
			  return;
			}
		  }
		  luaX_next(ls);
		}


		private static UnOpr getunopr (int op) {
		  switch (op) {
			case (int)RESERVED.TK_NOT: return UnOpr.OPR_NOT;
			case '-': return UnOpr.OPR_MINUS;
			case '#': return UnOpr.OPR_LEN;
			default: return UnOpr.OPR_NOUNOPR;
		  }
		}


		private static BinOpr getbinopr (int op) {
		  switch (op) {
			case '+': return BinOpr.OPR_ADD;
			case '-': return BinOpr.OPR_SUB;
			case '*': return BinOpr.OPR_MUL;
			case '/': return BinOpr.OPR_DIV;
			case '%': return BinOpr.OPR_MOD;
			case '^': return BinOpr.OPR_POW;
			case (int)RESERVED.TK_CONCAT: return BinOpr.OPR_CONCAT;
			case (int)RESERVED.TK_NE: return BinOpr.OPR_NE;
			case (int)RESERVED.TK_EQ: return BinOpr.OPR_EQ;
			case '<': return BinOpr.OPR_LT;
			case (int)RESERVED.TK_LE: return BinOpr.OPR_LE;
			case '>': return BinOpr.OPR_GT;
			case (int)RESERVED.TK_GE: return BinOpr.OPR_GE;
			case (int)RESERVED.TK_AND: return BinOpr.OPR_AND;
			case (int)RESERVED.TK_OR: return BinOpr.OPR_OR;
			default: return BinOpr.OPR_NOBINOPR;
		  }
		}


		private class priority_ {
			public priority_(lu_byte left, lu_byte right)
			{
				this.left = left;
				this.right = right;
			}

			public lu_byte left;  /* left priority for each binary operator */
			public lu_byte right; /* right priority */
		} 

		private static priority_[] priority = {  /* ORDER OPR */

			new priority_(6, 6),
			new priority_(6, 6),
			new priority_(7, 7),
			new priority_(7, 7),
			new priority_(7, 7),				/* `+' `-' `*' `/' `%' */

			new priority_(10, 9),
			new priority_(5, 4),				/* ^, .. (right associative) */

			new priority_(3, 3),
			new priority_(3, 3),				
            new priority_(3, 3),                /* ==, <, <= */
												
			new priority_(3, 3),
			new priority_(3, 3),
			new priority_(3, 3),                /* ~=, >, >= */

			new priority_(2, 2),
			new priority_(1, 1)					/* and, or */
		};

		public const int UNARY_PRIORITY	= 8;  /* priority for unary operators */


		/*
		** subexpr . (simpleexp | unop subexpr) { binop subexpr }
		** where `binop' is any binary operator with a priority higher than `limit'
		*/
		private static BinOpr subexpr (LexState ls, expdesc v, uint limit) {
		  BinOpr op = new BinOpr();
		  UnOpr uop = new UnOpr();
		  enterlevel(ls);
		  uop = getunopr(ls.t.token);
		  if (uop != UnOpr.OPR_NOUNOPR) {
            int line = ls.linenumber;
			luaX_next(ls);
			subexpr(ls, v, UNARY_PRIORITY);
			luaK_prefix(ls.fs, uop, v, line);
		  }
		  else simpleexp(ls, v);
		  /* expand while operators have priorities higher than `limit' */
		  op = getbinopr(ls.t.token);
		  while (op != BinOpr.OPR_NOBINOPR && priority[(int)op].left > limit) {
			expdesc v2 = new expdesc();
			BinOpr nextop;
            int line = ls.linenumber;
			luaX_next(ls);
			luaK_infix(ls.fs, op, v);
			/* read sub-expression with higher priority */
			nextop = subexpr(ls, v2, priority[(int)op].right);
			luaK_posfix(ls.fs, op, v, v2, line);
			op = nextop;
		  }
		  leavelevel(ls);
		  return op;  /* return first untreated operator */
		}


		private static void expr (LexState ls, expdesc v) {
		  subexpr(ls, v, 0);
		}

		/* }==================================================================== */



		/*
		** {======================================================================
		** Rules for Statements
		** =======================================================================
		*/


		private static int block_follow (int token) {
		  switch (token) {
			case (int)RESERVED.TK_ELSE: case (int)RESERVED.TK_ELSEIF: case (int)RESERVED.TK_END:
			case (int)RESERVED.TK_UNTIL: case (int)RESERVED.TK_EOS:
			  return 1;
			default: return 0;
		  }
		}


		private static void block (LexState ls) {
		  /* block . chunk */
		  FuncState fs = ls.fs;
		  BlockCnt bl = new BlockCnt();
		  enterblock(fs, bl, 0);
		  chunk(ls);
		  lua_assert(bl.breaklist == NO_JUMP);
		  leaveblock(fs);
		}


		/*
		** structure to chain all variables in the left-hand side of an
		** assignment
		*/
		public class LHS_assign {
		  public LHS_assign prev;
		  public expdesc v = new expdesc();  /* variable (global, local, upvalue, or indexed) */
		};


		/*
		** check whether, in an assignment to a local variable, the local variable
		** is needed in a previous assignment (to a table). If so, save original
		** local value in a safe place and use this safe copy in the previous
		** assignment.
		*/
		private static void check_conflict (LexState ls, LHS_assign lh, expdesc v) {
		  FuncState fs = ls.fs;
		  int extra = fs.freereg;  /* eventual position to save local variable */
		  int conflict = 0;
		  for (; lh!=null; lh = lh.prev) {
			/* conflict in table 't'? */
		    if (lh.v.u.ind.vt == v.k && lh.v.u.ind.t == v.u.info) {
		      conflict = 1;
		      lh.v.u.ind.vt = expkind.VLOCAL;
		      lh.v.u.ind.t = extra;  /* previous assignment will use safe copy */
		    }
		    /* conflict in index 'idx'? */
		    if (v.k == expkind.VLOCAL && lh.v.u.ind.idx == v.u.info) {
		      conflict = 1;
		      lh->v.u.ind.idx = extra;  /* previous assignment will use safe copy */
		    }
		  }
		  if (conflict != 0) {
		    OpCode op = (v.k == expkind.VLOCAL) ? OpCode.OP_MOVE : OpCode.OP_GETUPVAL;
		    luaK_codeABC(fs, op, fs.freereg, v.u.info, 0);  /* make copy */
			luaK_reserveregs(fs, 1);
		  }
		}


		private static void assignment (LexState ls, LHS_assign lh, int nvars) {
		  expdesc e = new expdesc();
		  check_condition(ls, vkisvar(lh.v.k), "syntax error");
		  if (testnext(ls, ',') != 0) {  /* assignment . `,' primaryexp assignment */
			LHS_assign nv = new LHS_assign();
			nv.prev = lh;
			primaryexp(ls, nv.v);
			if (nv.v.k != expkind.VINDEXED)
			  check_conflict(ls, lh, nv.v);
		    checklimit(ls.fs, nvars, LUAI_MAXCCALLS - G(ls.L).nCcalls,
		                    "variable names");
			assignment(ls, nv, nvars+1);
		  }
		  else {  /* assignment . `=' explist1 */
			int nexps;
			checknext(ls, '=');
			nexps = explist1(ls, e);
			if (nexps != nvars) {
			  adjust_assign(ls, nvars, nexps, e);
			  if (nexps > nvars)
				ls.fs.freereg -= nexps - nvars;  /* remove extra values */
			}
			else {
			  luaK_setoneret(ls.fs, e);  /* close last expression */
			  luaK_storevar(ls.fs, lh.v, e);
			  return;  /* avoid default */
			}
		  }
		  init_exp(e, expkind.VNONRELOC, ls.fs.freereg - 1);  /* default assignment */
		  luaK_storevar(ls.fs, lh.v, e);
		}


		private static int cond (LexState ls) {
		  /* cond . exp */
		  expdesc v = new expdesc();
		  expr(ls, v);  /* read condition */
		  if (v.k == expkind.VNIL) v.k = expkind.VFALSE;  /* `falses' are all equal here */
		  luaK_goiftrue(ls.fs, v);
		  return v.f;
		}


		private static void breakstat (LexState ls) {
		  FuncState fs = ls.fs;
		  BlockCnt bl = fs.bl;
		  int upval = 0;
		  while ((bl!=null) && (bl.isbreakable==0)) {
			upval |= bl.upval;
			bl = bl.previous;
		  }
		  if (bl==null)
			luaX_syntaxerror(ls, "no loop to break");
		  if (upval != 0)
			luaK_codeABC(fs, OpCode.OP_CLOSE, bl.nactvar, 0, 0);
		  luaK_concat(fs, ref bl.breaklist, luaK_jump(fs));
		}


		private static void whilestat (LexState ls, int line) {
		  /* whilestat . WHILE cond DO block END */
		  FuncState fs = ls.fs;
		  int whileinit;
		  int condexit;
		  BlockCnt bl = new BlockCnt();
		  luaX_next(ls);  /* skip WHILE */
		  whileinit = luaK_getlabel(fs);
		  condexit = cond(ls);
		  enterblock(fs, bl, 1);
		  checknext(ls, (int)RESERVED.TK_DO);
		  block(ls);
		  luaK_jumpto(fs, whileinit);
		  check_match(ls, (int)RESERVED.TK_END, (int)RESERVED.TK_WHILE, line);
		  leaveblock(fs);
		  luaK_patchtohere(fs, condexit);  /* false conditions finish the loop */
		}


		private static void repeatstat (LexState ls, int line) {
		  /* repeatstat . REPEAT block UNTIL cond */
		  int condexit;
		  FuncState fs = ls.fs;
		  int repeat_init = luaK_getlabel(fs);
		  BlockCnt bl1 = new BlockCnt(), bl2 = new BlockCnt();
		  enterblock(fs, bl1, 1);  /* loop block */
		  enterblock(fs, bl2, 0);  /* scope block */
		  luaX_next(ls);  /* skip REPEAT */
		  chunk(ls);
		  check_match(ls, (int)RESERVED.TK_UNTIL, (int)RESERVED.TK_REPEAT, line);
		  condexit = cond(ls);  /* read condition (inside scope block) */
		  if (bl2.upval==0) {  /* no upvalues? */
			leaveblock(fs);  /* finish scope */
			luaK_patchlist(fs, condexit, repeat_init);  /* close the loop */
		  }
		  else {  /* complete semantics when there are upvalues */
			breakstat(ls);  /* if condition then break */
			luaK_patchtohere(ls.fs, condexit);  /* else... */
			leaveblock(fs);  /* finish scope... */
			luaK_jumpto(fs, repeat_init);  /* and repeat */
		  }
		  leaveblock(fs);  /* finish loop */
		}


		private static int exp1 (LexState ls) {
		  expdesc e = new expdesc();
		  int reg;
		  expr(ls, e);
		  luaK_exp2nextreg(ls.fs, e);
		  lua_assert(e.k == expkind.VNONRELOC);
		  reg = e.u.info;
		  return reg;
		}


		private static void forbody (LexState ls, int base_, int line, int nvars, int isnum) {
		  /* forbody . DO block */
		  BlockCnt bl = new BlockCnt();
		  FuncState fs = ls.fs;
		  int prep, endfor;
		  adjustlocalvars(ls, 3);  /* control variables */
		  checknext(ls, (int)RESERVED.TK_DO);
		  prep = (isnum != 0) ? luaK_codeAsBx(fs, OpCode.OP_FORPREP, base_, NO_JUMP) : luaK_jump(fs);
		  enterblock(fs, bl, 0);  /* scope for declared variables */
		  adjustlocalvars(ls, nvars);
		  luaK_reserveregs(fs, nvars);
		  block(ls);
		  leaveblock(fs);  /* end of scope for declared variables */
		  luaK_patchtohere(fs, prep);
		  if (isnum != 0)  /* numeric for? */
		    endfor = luaK_codeAsBx(fs, OpCode.OP_FORLOOP, base_, NO_JUMP);
		  else {  /* generic for */
		    luaK_codeABC(fs, OpCode.OP_TFORCALL, base_, 0, nvars);
		    luaK_fixline(fs, line);
		    endfor = luaK_codeAsBx(fs, OpCode.OP_TFORLOOP, base_ + 2, NO_JUMP);
		  }
		  luaK_patchlist(fs, endfor, prep + 1);
		  luaK_fixline(fs, line);
		}


		private static void fornum (LexState ls, TString varname, int line) {
		  /* fornum . NAME = exp1,exp1[,exp1] forbody */
		  FuncState fs = ls.fs;
		  int base_ = fs.freereg;
		  new_localvarliteral(ls, "(for index)");
		  new_localvarliteral(ls, "(for limit)");
		  new_localvarliteral(ls, "(for step)");
		  new_localvar(ls, varname);
		  checknext(ls, '=');
		  exp1(ls);  /* initial value */
		  checknext(ls, ',');
		  exp1(ls);  /* limit */
		  if (testnext(ls, ',') != 0)
			exp1(ls);  /* optional step */
		  else {  /* default step = 1 */
			luaK_codek(fs, fs.freereg, luaK_numberK(fs, 1));
			luaK_reserveregs(fs, 1);
		  }
		  forbody(ls, base_, line, 1, 1);
		}


		private static void forlist (LexState ls, TString indexname) {
		  /* forlist . NAME {,NAME} IN explist1 forbody */
		  FuncState fs = ls.fs;
		  expdesc e = new expdesc();
		  int nvars = 4;  /* gen, state, control, plus at least one declared var */
		  int line;
		  int base_ = fs.freereg;
		  /* create control variables */
		  new_localvarliteral(ls, "(for generator)");
		  new_localvarliteral(ls, "(for state)");
		  new_localvarliteral(ls, "(for control)");
		  /* create declared variables */
		  new_localvar(ls, indexname);
		  while (testnext(ls, ',') != 0) {
			new_localvar(ls, str_checkname(ls));
            nvars++;
          }
		  checknext(ls, (int)RESERVED.TK_IN);
		  line = ls.linenumber;
		  adjust_assign(ls, 3, explist1(ls, e), e);
		  luaK_checkstack(fs, 3);  /* extra space to call generator */
		  forbody(ls, base_, line, nvars - 3, 0);
		}


		private static void forstat (LexState ls, int line) {
		  /* forstat . FOR (fornum | forlist) END */
		  FuncState fs = ls.fs;
		  TString varname;
		  BlockCnt bl = new BlockCnt();
		  enterblock(fs, bl, 1);  /* scope for loop and control variables */
		  luaX_next(ls);  /* skip `for' */
		  varname = str_checkname(ls);  /* first variable name */
		  switch (ls.t.token) {
			case '=': fornum(ls, varname, line); break;
			case ',':
			case (int)RESERVED.TK_IN:
				forlist(ls, varname);
				break;
			default: luaX_syntaxerror(ls, LUA_QL("=") + " or " + LUA_QL("in") + " expected"); break;
		  }
		  check_match(ls, (int)RESERVED.TK_END, (int)RESERVED.TK_FOR, line);
		  leaveblock(fs);  /* loop scope (`break' jumps to this point) */
		}


		private static int test_then_block (LexState ls) {
		  /* test_then_block . [IF | ELSEIF] cond THEN block */
		  int condexit;
		  luaX_next(ls);  /* skip IF or ELSEIF */
		  condexit = cond(ls);
		  checknext(ls, (int)RESERVED.TK_THEN);
		  block(ls);  /* `then' part */
		  return condexit;
		}


		private static void ifstat (LexState ls, int line) {
		  /* ifstat . IF cond THEN block {ELSEIF cond THEN block} [ELSE block] END */
		  FuncState fs = ls.fs;
		  int flist;
		  int escapelist = NO_JUMP;
		  flist = test_then_block(ls);  /* IF cond THEN block */
		  while (ls.t.token == (int)RESERVED.TK_ELSEIF) {
			luaK_concat(fs, ref escapelist, luaK_jump(fs));
			luaK_patchtohere(fs, flist);
			flist = test_then_block(ls);  /* ELSEIF cond THEN block */
		  }
		  if (ls.t.token == (int)RESERVED.TK_ELSE) {
			luaK_concat(fs, ref escapelist, luaK_jump(fs));
			luaK_patchtohere(fs, flist);
			luaX_next(ls);  /* skip ELSE (after patch, for correct line info) */
			block(ls);  /* `else' part */
		  }
		  else
			luaK_concat(fs, ref escapelist, flist);
		  luaK_patchtohere(fs, escapelist);
		  check_match(ls, (int)RESERVED.TK_END, (int)RESERVED.TK_IF, line);
		}


		private static void localfunc (LexState ls) {
		  expdesc v = new expdesc(), b = new expdesc();
		  FuncState fs = ls.fs;
		  new_localvar(ls, str_checkname(ls));
		  init_exp(v, expkind.VLOCAL, fs.freereg);
		  luaK_reserveregs(fs, 1);
		  adjustlocalvars(ls, 1);
		  body(ls, b, 0, ls.linenumber);
		  luaK_storevar(fs, v, b);
		}


		private static void localstat (LexState ls) {
		  /* stat . LOCAL NAME {`,' NAME} [`=' explist1] */
		  int nvars = 0;
		  int nexps;
		  expdesc e = new expdesc();
		  do {
		    new_localvar(ls, str_checkname(ls));
		    nvars++;
		  } while (testnext(ls, ',') != 0);
		  if (testnext(ls, '=') != 0)
			nexps = explist1(ls, e);
		  else {
			e.k = expkind.VVOID;
			nexps = 0;
		  }
		  adjust_assign(ls, nvars, nexps, e);
		  adjustlocalvars(ls, nvars);
		}


		private static int funcname (LexState ls, expdesc v) {
		  /* funcname -> NAME {fieldsel} [`:' NAME] */
		  int needself = 0;
		  singlevar(ls, v);
		  while (ls.t.token == '.')
			fieldsel(ls, v);
		  if (ls.t.token == ':') {
			needself = 1;
			fieldsel(ls, v);
		  }
		  return needself;
		}


		private static void funcstat (LexState ls, int line) {
		  /* funcstat . FUNCTION funcname body */
		  int needself;
		  expdesc v = new expdesc(), b = new expdesc();
		  luaX_next(ls);  /* skip FUNCTION */
		  needself = funcname(ls, v);
		  body(ls, b, needself, line);
		  luaK_storevar(ls.fs, v, b);
		  luaK_fixline(ls.fs, line);  /* definition `happens' in the first line */
		}


		private static void exprstat (LexState ls) {
		  /* stat . func | assignment */
		  FuncState fs = ls.fs;
		  LHS_assign v = new LHS_assign();
		  primaryexp(ls, v.v);
		  if (v.v.k == expkind.VCALL)  /* stat . func */
			SETARG_C(getcode(fs, v.v), 1);  /* call statement uses no results */
		  else {  /* stat . assignment */
			v.prev = null;
			assignment(ls, v, 1);
		  }
		}


		private static void retstat (LexState ls) {
		  /* stat . RETURN explist */
		  FuncState fs = ls.fs;
		  expdesc e = new expdesc();
		  int first, nret;  /* registers with returned values */
		  if ((block_follow(ls.t.token)!=0) || ls.t.token == ';')
			first = nret = 0;  /* return no values */
		  else {
			nret = explist1(ls, e);  /* optional return values */
			if (hasmultret(e.k) != 0) {
			  luaK_setmultret(fs, e);
			  if (e.k == expkind.VCALL && nret == 1) {  /* tail call? */
				SET_OPCODE(getcode(fs,e), OpCode.OP_TAILCALL);
				lua_assert(GETARG_A(getcode(fs,e)) == fs.nactvar);
			  }
			  first = fs.nactvar;
			  nret = LUA_MULTRET;  /* return all values */
			}
			else {
			  if (nret == 1)  /* only one single value? */
				first = luaK_exp2anyreg(fs, e);
			  else {
				luaK_exp2nextreg(fs, e);  /* values must go to the `stack' */
				first = fs.nactvar;  /* return all `active' values */
				lua_assert(nret == fs.freereg - first);
			  }
			}
		  }
		  luaK_ret(fs, first, nret);
		}


		private static int statement (LexState ls) {
		  int line = ls.linenumber;  /* may be needed for error messages */
		  switch (ls.t.token) {
			case ';': {  /* stat -> ';' (empty statement) */
		      luaX_next(ls);  /* skip ';' */
		      return 0;
		    }
			case (int)RESERVED.TK_IF: {  /* stat -> ifstat */
			  ifstat(ls, line);
			  return 0;
			}
			case (int)RESERVED.TK_WHILE: {  /* stat -> whilestat */
			  whilestat(ls, line);
			  return 0;
			}
			case (int)RESERVED.TK_DO: {  /* stat -> DO block END */
			  luaX_next(ls);  /* skip DO */
			  block(ls);
			  check_match(ls, (int)RESERVED.TK_END, (int)RESERVED.TK_DO, line);
			  return 0;
			}
			case (int)RESERVED.TK_FOR: {  /* stat -> forstat */
			  forstat(ls, line);
			  return 0;
			}
			case (int)RESERVED.TK_REPEAT: {  /* stat -> repeatstat */
			  repeatstat(ls, line);
			  return 0;
			}
			case (int)RESERVED.TK_FUNCTION: {  /* stat -> funcstat */
			  funcstat(ls, line);
			  return 0;
			}
			case (int)RESERVED.TK_LOCAL: {  /* stat -> localstat */
			  luaX_next(ls);  /* skip LOCAL */
			  if (testnext(ls, (int)RESERVED.TK_FUNCTION) != 0)  /* local function? */
				localfunc(ls);
			  else
				localstat(ls);
			  return 0;
			}
			case (int)RESERVED.TK_RETURN: {  /* stat -> retstat */
              luaX_next(ls);  /* skip RETURN */
			  retstat(ls);
			  return 1;  /* must be last statement */
			}
			case (int)RESERVED.TK_BREAK: {  /* stat -> breakstat */
			  luaX_next(ls);  /* skip BREAK */
			  breakstat(ls);
			  return 1;  /* must be last statement */
			}
			default: {  /* stat -> func | assignment */
			  exprstat(ls);
			  return 0;
			}
		  }
		}


		private static void chunk (LexState ls) {
		  /* chunk . { stat [`;'] } */
		  int islast = 0;
		  enterlevel(ls);
		  while ((islast==0) && (block_follow(ls.t.token)==0)) {
			islast = statement(ls);
            if (islast)
			  testnext(ls, ';');
			lua_assert(ls.fs.f.maxstacksize >= ls.fs.freereg &&
					   ls.fs.freereg >= ls.fs.nactvar);
			ls.fs.freereg = ls.fs.nactvar;  /* free registers */
		  }
		  leavelevel(ls);
		}

		/* }====================================================================== */

	}
}

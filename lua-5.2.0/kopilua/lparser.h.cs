/*
** $Id: lparser.h,v 1.69 2011/07/27 18:09:01 roberto Exp $
** Lua Parser
** See Copyright Notice in lua.h
*/
using System.Diagnostics;

namespace KopiLua
{
	using lu_byte = System.Byte;
	using lua_Number = System.Double;

	public partial class Lua
	{
		/*
		** Expression descriptor
		*/

		public enum expkind {
		  VVOID,	/* no value */
		  VNIL,
		  VTRUE,
		  VFALSE,
		  VK,		/* info = index of constant in `k' */
		  VKNUM,	/* nval = numerical value */
          VNONRELOC,	/* info = result register */
		  VLOCAL,	/* info = local register */
		  VUPVAL,       /* info = index of upvalue in 'upvalues' */
		  VINDEXED,	/* t = table register/upvalue; idx = index R/K */
		  VJMP,		/* info = instruction pc */
		  VRELOCABLE,	/* info = instruction pc */
		  VCALL,	/* info = instruction pc */
		  VVARARG	/* info = instruction pc */
		};

	
		public static bool vkisvar(expkind k) { return (expkind.VLOCAL <= k && k <= expkind.VINDEXED);}
		public static bool vkisinreg(expkind k) { return (k == expkind.VNONRELOC || k == expkind.VLOCAL);}

		public class expdesc {

			public void Copy(expdesc e)
			{
				this.k = e.k;
				this.u.Copy(e.u);
				this.t = e.t;
				this.f = e.f;
			}

			public expkind k;
			public class _u   /* for indexed variables (VINDEXED) */
			{
				public void Copy(_u u)
				{
					this.ind.Copy(u.ind);
					this.info = u.info;
					this.nval = u.nval;
				}

				public class _ind
				{
					public void Copy(_ind ind)
					{
						this.idx = ind.idx;
						this.t = ind.t;
                        this.vt = ind.vt;
					}
			      	public short idx;  /* index (R/K) */
			      	public lu_byte t;  /* table (register or upvalue) */
			      	public lu_byte vt;  /* whether 't' is register (VLOCAL) or upvalue (VUPVAL) */
				};
			    public _ind ind = new _ind();
				public int info;  /* for generic use */
				public lua_Number nval;  /* for VKNUM */
			};
			public _u u = new _u();

		  public int t;  /* patch list of `exit when true' */
		  public int f;  /* patch list of `exit when false' */
		};


		/* description of active local variable */
		public class Vardesc : ArrayElement {
			//-----------------------------------
			//FIXME:ArrayElement added
			private Vardesc[] values = null; 
			private int index = -1;

			public void set_index(int index)
			{
				this.index = index;
			}

			public void set_array(object array)
			{
				this.values = (Vardesc[])array;
				Debug.Assert(this.values != null);
			}
			//------------------------------------------
		  public short idx;  /* variable index in stack */
		};



		/* description of pending goto statements and label statements */
		public class Labeldesc : ArrayElement {
			//-----------------------------------
			//FIXME:ArrayElement added
			private Labeldesc[] values = null; 
			private int index = -1;

			public void set_index(int index)
			{
				this.index = index;
			}

			public void set_array(object array)
			{
				this.values = (Labeldesc[])array;
				Debug.Assert(this.values != null);
			}
			//------------------------------------------			
		  public TString name;  /* label identifier */
		  public int pc;  /* position in code */
		  public int line;  /* line where it appeared */
		  public lu_byte nactvar;  /* local level where it appears in current block */
		};


		/* list of labels or gotos */
		public class Labellist {
		  public Labeldesc[] arr;  /* array */
		  public int n;  /* number of entries in use */
		  public int size;  /* array size */
		};


		/* dynamic structures used by the parser */
		public class Dyndata {
		  public class actvar_ {  /* list of active local variables */
			public Vardesc[] arr;
		    public int n;
		    public int size;
		  };
		  public actvar_ actvar = new actvar_();
		  public Labellist gt = new Labellist();  /* list of pending gotos */
		  public Labellist label = new Labellist();   /* list of active labels */
		};


		/* control of blocks */
		//struct BlockCnt;  /* defined in lparser.c */


		/* state needed to generate code for a given function */
		public class FuncState {
		  public Proto f;  /* current function header */
		  public Table h;  /* table to find (and reuse) elements in `k' */
		  public FuncState prev;  /* enclosing function */
		  public LexState ls;  /* lexical state */
		  public BlockCnt bl;  /* chain of current blocks */
		  public int pc;  /* next position to code (equivalent to `ncode') */
		  public int lasttarget;   /* 'label' of last 'jump label' */
		  public int jpc;  /* list of pending jumps to `pc' */
		  public int nk;  /* number of elements in `k' */
		  public int np;  /* number of elements in `p' */
          public int firstlocal;  /* index of first local var (in Dyndata array) */
		  public short nlocvars;  /* number of elements in 'f->locvars' */
		  public lu_byte nactvar;  /* number of active local variables */
		  public lu_byte nups;  /* number of upvalues */
		  public lu_byte freereg;  /* first free register */
		};
		
		//LUAI_FUNC Proto *luaY_parser (lua_State *L, ZIO *z, Mbuffer *buff,
        //                      Dyndata *dyd, const char *name, int firstchar);				  
	}
}
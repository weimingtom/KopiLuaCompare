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



		public class vardesc : ArrayElement {
			//-----------------------------------
			//FIXME:ArrayElement added
			private vardesc[] values = null; 
			private int index = -1;

			public void set_index(int index)
			{
				this.index = index;
			}

			public void set_array(object array)
			{
				this.values = (vardesc[])array;
				Debug.Assert(this.values != null);
			}
			//------------------------------------------
		  public ushort idx;
		};



		/* list of all active local variables */
		public struct Varlist {
		  public vardesc[] actvar;
		  public int nactvar;
		  public int actvarsize;
		};





		/* state needed to generate code for a given function */
		public class FuncState {
		  public FuncState()
		  {
		  	//FIXME:removed
		  	//for (int i=0; i<this.upvalues.Length; i++)
			//	  this.upvalues[i] = new upvaldesc();
		  }

		  public Proto f;  /* current function header */
		  public Table h;  /* table to find (and reuse) elements in `k' */
		  public FuncState prev;  /* enclosing function */
		  public LexState ls;  /* lexical state */
		  public lua_State L;  /* copy of the Lua state */
		  public BlockCnt bl;  /* chain of current blocks */
		  public int pc;  /* next position to code (equivalent to `ncode') */
		  public int lasttarget;   /* `pc' of last `jump target' */
		  public int jpc;  /* list of pending jumps to `pc' */
		  public int freereg;  /* first free register */
		  public int nk;  /* number of elements in `k' */
		  public int np;  /* number of elements in `p' */
          public int firstlocal;  /* index of first local var of this function */
		  public short nlocvars;  /* number of elements in `locvars' */
		  public lu_byte nactvar;  /* number of active local variables */
		  public lu_byte nups;  /* number of upvalues */
		};
	}
}
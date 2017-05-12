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
		  VLOCAL,	/* info = local register */
		  VUPVAL,       /* info = index of upvalue in `upvalues' */
		  VGLOBAL,	/* info = index of table; aux = index of global name in `k' */
		  VINDEXED,	/* info = table register; aux = index register (or `k') */
		  VJMP,		/* info = instruction pc */
		  VRELOCABLE,	/* info = instruction pc */
		  VNONRELOC,	/* info = result register */
		  VCALL,	/* info = instruction pc */
		  VVARARG	/* info = instruction pc */
		};

	

		public class expdesc {

			public void Copy(expdesc e)
			{
				this.k = e.k;
				this.u.Copy(e.u);
				this.t = e.t;
				this.f = e.f;
			}

			public expkind k;
			public class _u
			{
				public void Copy(_u u)
				{
					this.s.Copy(u.s);
					this.nval = u.nval;
				}

				public class _s
				{
					public void Copy(_s s)
					{
						this.info = s.info;
						this.aux = s.aux;
					}
					public int info, aux;
				};
			    public _s s = new _s();
				public lua_Number nval;
			};
			public _u u = new _u();

		  public int t;  /* patch list of `exit when true' */
		  public int f;  /* patch list of `exit when false' */
		};


		public class upvaldesc {
		  public lu_byte k;
		  public lu_byte info;
		};


		/* state needed to generate code for a given function */
		public class FuncState {
		  public FuncState()
		  {
			  for (int i=0; i<this.upvalues.Length; i++)
				  this.upvalues[i] = new upvaldesc();
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
		  public short nlocvars;  /* number of elements in `locvars' */
		  public lu_byte nactvar;  /* number of active local variables */
		  public upvaldesc[] upvalues = new upvaldesc[LUAI_MAXUPVALUES];  /* upvalues */
		  public ushort[] actvar = new ushort[LUAI_MAXVARS];  /* declared-variable stack */
		};
	}
}
/*
** $Id: ltable.c,v 2.72 2012/09/11 19:37:16 roberto Exp $
** Lua tables (hash)
** See Copyright Notice in lua.h
*/

//#define DEBUG

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using lua_Number = System.Double;
	using lu_byte = System.Byte;
	
	public partial class Lua
	{
		/*
		** Implementation of tables (aka arrays, objects, or hash tables).
		** Tables keep its elements in two parts: an array part and a hash part.
		** Non-negative integer keys are all candidates to be kept in the array
		** part. The actual size of the array is the largest `n' such that at
		** least half the slots between 0 and n are in use.
		** Hash uses a mix of chained scatter table with Brent's variation.
		** A main invariant of these tables is that, if an element is not
		** in its main position (i.e. the `original' position that its hash gives
		** to it), then the colliding element is in its own main position.
		** Hence even when the load factor reaches 100%, performance remains good.
		*/



		/*
		** max size of array part is 2^MAXBITS
		*/
		//#if LUAI_BITSINT > 32
		public const int MAXBITS = 30;	/* in the dotnet port LUAI_BITSINT is 32 */
		//#else
		//public const int MAXBITS		= (LUAI_BITSINT-2);
		//#endif

		public const int MAXASIZE	= (1 << MAXBITS);


		//public static Node gnode(Table t, int i)	{return t.node[i];}
		public static Node hashpow2(Table t, lua_Number n)		{return gnode(t, (int)lmod(n, sizenode(t)));}
		
		public static Node hashstr(Table t, TString str)		{return hashpow2(t, str.tsv.hash);}
		public static Node hashboolean(Table t, int p)		{return hashpow2(t, p);}


		/*
		** for some types, it is better to avoid modulus by power of 2, as
		** they tend to have many 2 factors.
		*/
		public static Node hashmod(Table t, int n) { return gnode(t, (n % ((sizenode(t) - 1) | 1))); }


		public static Node hashpointer(Table t, object p) { return hashmod(t, p.GetHashCode()); }


        //FIXME:see below

		//#define dummynode		(&dummynode_)

		private static bool isdummy(Node n) {return ((n) == dummynode);}
		private static bool isdummy(Node[] n) {return ((n[0]) == dummynode);}


		//static const Node dummynode_ = {
		  //{NILCONSTANT},  /* value */
		  //{{NILCONSTANT, NULL}}  /* key */
		//};
		public static Node dummynode_ = new Node(new TValue(new Value(), LUA_TNIL), new TKey(new Value(), LUA_TNIL, null)); //FIXME:???
		public static Node dummynode = dummynode_;

		/*
		** hash for lua_Numbers
		*/
		private static Node hashnum (Table t, lua_Number n) {
		  int i;
		  luai_hashnum(out i, n);
		  if (i < 0) {
		    if ((uint)(i) == 0u - i)  /* use unsigned to avoid overflows */
		      i = 0;  /* handle INT_MIN */
		     i = -i;  /* must be a positive value */
		  }
		  return hashmod(t, i);
		}



		/*
		** returns the `main' position of an element in a table (that is, the index
		** of its hash value)
		*/
		private static Node mainposition (Table t, TValue key) {
		  switch (ttype(key)) {
			case LUA_TNUMBER:
			  return hashnum(t, nvalue(key));
			case LUA_TLNGSTR: {
		      TString s = rawtsvalue(key);
		      if (s.tsv.extra == 0) {  /* no hash? */
		        s.tsv.hash = luaS_hash(getstr(s), s.tsv.len, s.tsv.hash);
		        s.tsv.extra = 1;  /* now it has its hash */
		      }
		      return hashstr(t, rawtsvalue(key));
		    }
		    case LUA_TSHRSTR:
			  return hashstr(t, rawtsvalue(key));
			case LUA_TBOOLEAN:
			  return hashboolean(t, bvalue(key));
			case LUA_TLIGHTUSERDATA:
			  return hashpointer(t, pvalue(key));
		    case LUA_TLCF:
		      return hashpointer(t, fvalue(key));
			default:
				return hashpointer(t, gcvalue(key));
		  }
		}


		/*
		** returns the index for `key' if `key' is an appropriate key to live in
		** the array part of the table, -1 otherwise.
		*/
		private static int arrayindex (TValue key) {
		  if (ttisnumber(key)) {
			lua_Number n = nvalue(key);
			int k;
			lua_number2int(out k, n);
			if (luai_numeq(cast_num(k), n))
			  return k;
		  }
		  return -1;  /* `key' did not match some condition */
		}


		/*
		** returns the index of a `key' for table traversals. First goes all
		** elements in the array part, then elements in the hash part. The
		** beginning of a traversal is signaled by -1.
		*/
		private static int findindex (lua_State L, Table t, StkId key) {
		  int i;
		  if (ttisnil(key)) return -1;  /* first iteration */
		  i = arrayindex(key);
		  if (0 < i && i <= t.sizearray)  /* is `key' inside array part? */
			return i-1;  /* yes; that's the index (corrected to C) */
		  else {
			Node n = mainposition(t, key);
		    for (;;) {  /* check whether `key' is somewhere in the chain */
		      /* key may be dead already, but it is ok to use it in `next' */
		      if (luaV_rawequalobj(gkey(n), key)!=0 ||
		            (ttisdeadkey(gkey(n)) && iscollectable(key) &&
		             deadvalue(gkey(n)) == gcvalue(key))) {
		        i = cast_int(n - gnode(t, 0));  /* key index in hash table */
		        /* hash elements are numbered after array ones */
		        return i + t.sizearray;
		      }
		      else n = gnext(n);
		      if (n == null)
		        luaG_runerror(L, "invalid key to " + LUA_QL("next"));  /* key not found */
		    }
		  }
		}


		public static int luaH_next (lua_State L, Table t, StkId key) {
		  int i = findindex(L, t, key);  /* find original element */
		  for (i++; i < t.sizearray; i++) {  /* try first array part */
			if (!ttisnil(t.array[i])) {  /* a non-nil value? */
			  setnvalue(key, cast_num(i+1));
			  setobj2s(L, key+1, t.array[i]);
			  return 1;
			}
		  }
		  for (i -= t.sizearray; i < sizenode(t); i++) {  /* then hash part */
			if (!ttisnil(gval(gnode(t, i)))) {  /* a non-nil value? */
			  setobj2s(L, key, gkey(gnode(t, i)));
			  setobj2s(L, key+1, gval(gnode(t, i)));
			  return 1;
			}
		  }
		  return 0;  /* no more elements */
		}


		/*
		** {=============================================================
		** Rehash
		** ==============================================================
		*/


		private static int computesizes (int[] nums, ref int narray) {
		  int i;
		  int twotoi;  /* 2^i */
		  int a = 0;  /* number of elements smaller than 2^i */
		  int na = 0;  /* number of elements to go to array part */
		  int n = 0;  /* optimal size for array part */
		  for (i = 0, twotoi = 1; twotoi/2 < narray; i++, twotoi *= 2) {
			if (nums[i] > 0) {
			  a += nums[i];
			  if (a > twotoi/2) {  /* more than half elements present? */
				n = twotoi;  /* optimal size (till now) */
				na = a;  /* all elements smaller than n will go to array part */
			  }
			}
			if (a == narray) break;  /* all elements already counted */
		  }
		  narray = n;
		  lua_assert(narray/2 <= na && na <= narray);
		  return na;
		}


		private static int countint (TValue key, int[] nums) {
		  int k = arrayindex(key);
		  if (0 < k && k <= MAXASIZE) {  /* is `key' an appropriate array index? */
		  	nums[luaO_ceillog2((uint)k)]++;  /* count as such */
			return 1;
		  }
		  else
			return 0;
		}


		private static int numusearray (Table t, int[] nums) {
		  int lg;
		  int ttlg;  /* 2^lg */
		  int ause = 0;  /* summation of `nums' */
		  int i = 1;  /* count to traverse all array keys */
		  for (lg=0, ttlg=1; lg<=MAXBITS; lg++, ttlg*=2) {  /* for each slice */
			int lc = 0;  /* counter */
			int lim = ttlg;
			if (lim > t.sizearray) {
			  lim = t.sizearray;  /* adjust upper limit */
			  if (i > lim)
				break;  /* no more elements to count */
			}
			/* count elements in range (2^(lg-1), 2^lg] */
			for (; i <= lim; i++) {
			  if (!ttisnil(t.array[i-1]))
				lc++;
			}
			nums[lg] += lc;
			ause += lc;
		  }
		  return ause;
		}


		private static int numusehash (Table t, int[] nums, ref int pnasize) {
		  int totaluse = 0;  /* total number of elements */
		  int ause = 0;  /* summation of `nums' */
		  int i = sizenode(t);
		  while ((i--) != 0) {
			Node n = t.node[i];
			if (!ttisnil(gval(n))) {
			  ause += countint(gkey(n), nums);
			  totaluse++;
			}
		  }
		  pnasize += ause;
		  return totaluse;
		}


		private static void setarrayvector (lua_State L, Table t, int size) {
		  int i;
		  luaM_reallocvector<TValue>(L, ref t.array, t.sizearray, size/*, TValue*/);
		  for (i=t.sizearray; i<size; i++)
			 setnilvalue(t.array[i]);
		  t.sizearray = size;
		}


		private static void setnodevector (lua_State L, Table t, int size) {
		  int lsize;
		  if (size == 0) {  /* no elements to hash part? */
			  t.node = new Node[] { dummynode };  /* use common `dummynode' */
			lsize = 0;
		  }
		  else {
			int i;
			lsize = luaO_ceillog2((uint)size);
			if (lsize > MAXBITS)
			  luaG_runerror(L, "table overflow");
			size = twoto(lsize);
			Node[] nodes = luaM_newvector<Node>(L, size);
			t.node = nodes;
			for (i=0; i<size; i++) {
			  Node n = gnode(t, i);
			  gnext_set(n, null);
			  setnilvalue(gkey(n));
			  setnilvalue(gval(n));
			}
		  }
		  t.lsizenode = cast_byte(lsize);
		  t.lastfree = size;  /* all positions are free */
		}


		private static void luaH_resize (lua_State L, Table t, int nasize, int nhsize) {
		  int i;
		  int oldasize = t.sizearray;
		  int oldhsize = t.lsizenode;
		  Node[] nold = t.node;  /* save old hash ... */
		  if (nasize > oldasize)  /* array part must grow? */
			setarrayvector(L, t, nasize);
		  /* create new hash part with appropriate size */
		  setnodevector(L, t, nhsize);
		  if (nasize < oldasize) {  /* array part must shrink? */
			t.sizearray = nasize;
			/* re-insert elements from vanishing slice */
			for (i=nasize; i<oldasize; i++) {
			  if (!ttisnil(t.array[i]))
				luaH_setint(L, t, i+1, t.array[i]);
			}
			/* shrink array */
			luaM_reallocvector<TValue>(L, ref t.array, oldasize, nasize/*, TValue*/);
		  }
		  /* re-insert elements from hash part */
		  for (i = twoto(oldhsize) - 1; i >= 0; i--) {
			Node old = nold[i];
			if (!ttisnil(gval(old))) {
		      /* doesn't need barrier/invalidate cache, as entry was
		         already present in the table */
			  setobjt2t(L, luaH_set(L, t, gkey(old)), gval(old));
			}
		  }
		  if (!isdummy(nold))
			luaM_freearray(L, nold/*, cast(size_t, twoto(oldhsize))*/);  /* free old array */ //FIXME:???, changed
		}


		public static void luaH_resizearray (lua_State L, Table t, int nasize) {
		  int nsize = isdummy(t.node) ? 0 : sizenode(t);
		  luaH_resize(L, t, nasize, nsize);
		}


		private static void rehash (lua_State L, Table t, TValue ek) {
		  int nasize, na;
		  int[] nums = new int[MAXBITS+1];  /* nums[i] = number of keys with 2^(i-1) < k <= 2^i */
		  int i;
		  int totaluse;
		  for (i=0; i<=MAXBITS; i++) nums[i] = 0;  /* reset counts */
		  nasize = numusearray(t, nums);  /* count keys in array part */
		  totaluse = nasize;  /* all those keys are integer keys */
		  totaluse += numusehash(t, nums, ref nasize);  /* count keys in hash part */
		  /* count extra key */
		  nasize += countint(ek, nums);
		  totaluse++;
		  /* compute new size for array part */
		  na = computesizes(nums, ref nasize);
		  /* resize the table to new computed sizes */
		  luaH_resize(L, t, nasize, totaluse - na);
		}



		/*
		** }=============================================================
		*/


		public static Table luaH_new (lua_State L) {
		  Table t = luaC_newobj<Table>(L, LUA_TTABLE, (uint)GetUnmanagedSize(typeof(Table)), null, 0).h;
		  t.metatable = null;
		  t.flags = cast_byte(~0);
		  t.array = null;
		  t.sizearray = 0;
		  setnodevector(L, t, 0);
		  return t;
		}


		public static void luaH_free (lua_State L, Table t) {
		  if (!isdummy(t.node[0])) //FIXME:[0]
			luaM_freearray(L, t.node/*, cast(size_t, sizenode(t))*/); //FIXME:changed
		  luaM_freearray(L, t.array/*, t.sizearray*/); //FIXME:changed
		  luaM_free(L, t);
		}


		private static Node getfreepos (Table t) {
		  while (t.lastfree > 0) { //FIXME:t.lastfree > t.node, notice lastfree point to t.node[0...]
            t.lastfree--;
			if (ttisnil(gkey(t.node[t.lastfree])))
			  return t.node[t.lastfree];
		  }
		  return null;  /* could not find a free place */
		}



		/*
		** inserts a new key into a hash table; first, check whether key's main
		** position is free. If not, check whether colliding node is in its main
		** position or not: if it is not, move colliding node to an empty place and
		** put new key in its main position; otherwise (colliding node is in its main
		** position), new key goes to an empty position.
		*/
		private static TValue luaH_newkey (lua_State L, Table t, TValue key) {
		  Node mp;
		  if (ttisnil(key)) luaG_runerror(L, "table index is nil");
		  else if (ttisnumber(key) && luai_numisnan(L, nvalue(key)))
		    luaG_runerror(L, "table index is NaN");
		  mp = mainposition(t, key);
		  if (!ttisnil(gval(mp)) || isdummy(mp)) {  /* main position is taken? */
			Node othern;
			Node n = getfreepos(t);  /* get a free place */
			if (n == null) {  /* cannot find a free place? */
			  rehash(L, t, key);  /* grow table */
		      /* whatever called 'newkey' take care of TM cache and GC barrier */
		      return luaH_set(L, t, key);  /* insert key into grown table */
			}
			lua_assert(!isdummy(n));
			othern = mainposition(t, gkey(mp));
			if (othern != mp) {  /* is colliding node out of its main position? */
			  /* yes; move colliding node into free position */
			  while (gnext(othern) != mp) othern = gnext(othern);  /* find previous */
			  gnext_set(othern, n);  /* redo the chain with `n' in place of `mp' */
			  n.i_val = new TValue(mp.i_val);	/* copy colliding node into free pos. (mp.next also goes) */ //FIXME:???changed
			  n.i_key = new TKey(mp.i_key); //FIXME:???changed
			  gnext_set(mp, null);  /* now `mp' is free */
			  setnilvalue(gval(mp));
			}
			else {  /* colliding node is in its own main position */
			  /* new node will go into free position */
			  gnext_set(n, gnext(mp));  /* chain new position */
			  gnext_set(mp, n);
			  mp = n;
			}
		  }
		  setobj2t(L, gkey(mp), key);
		  luaC_barrierback(L, obj2gco(t), key);
		  lua_assert(ttisnil(gval(mp)));
		  return gval(mp);
		}


		/*
		** search function for integers
		*/
		public static TValue luaH_getint(Table t, int key)
		{
		  /* (1 <= key && key <= t.sizearray) */
		  if ((uint)(key-1) < (uint)t.sizearray)
			return t.array[key-1];
		  else {
			lua_Number nk = cast_num(key);
			Node n = hashnum(t, nk);
			do {  /* check whether `key' is somewhere in the chain */
			  if (ttisnumber(gkey(n)) && luai_numeq(nvalue(gkey(n)), nk))
				return gval(n);  /* that's it */
			  else n = gnext(n);
			} while (n != null);
			return luaO_nilobject;
		  }
		}


		/*
		** search function for short strings
		*/
		public static TValue luaH_getstr (Table t, TString key) {
		  Node n = hashstr(t, key);
		  lua_assert(key.tsv.tt == LUA_TSHRSTR);
		  do {  /* check whether `key' is somewhere in the chain */
			if (ttisshrstring(gkey(n)) && eqshrstr(rawtsvalue(gkey(n)), key))
			  return gval(n);  /* that's it */
			else n = gnext(n);
		  } while (n != null);
		  return luaO_nilobject;
		}


		/*
		** main search function
		*/
		public static TValue luaH_get (Table t, TValue key) {
		  switch (ttype(key)) {
			case LUA_TSHRSTR: return luaH_getstr(t, rawtsvalue(key));
			case LUA_TNIL: return luaO_nilobject;
			case LUA_TNUMBER: {
			  int k;
			  lua_Number n = nvalue(key);
			  lua_number2int(out k, n);
			  if (luai_numeq(cast_num(k), n)) /* index is int? */
				return luaH_getint(t, k);  /* use specialized version */
			  /* else go through */
			  goto default;
			}
			default: {
				Node node = mainposition(t, key); //FIXME: n->node
			  do {  /* check whether `key' is somewhere in the chain */
				if (luaV_rawequalobj(gkey(node), key) != 0)//FIXME: n->node
				  return gval(node);  /* that's it *///FIXME: n->node
				else node = gnext(node);//FIXME: n->node
			  } while (node != null);//FIXME: n->node
			  return luaO_nilobject;
			}
		  }
		}


		/*
		** beware: when using this function you probably need to check a GC
		** barrier and invalidate the TM cache.
		*/
		public static TValue luaH_set (lua_State L, Table t, TValue key) {
		  TValue p = luaH_get(t, key);
		  if (p != luaO_nilobject)
			return (TValue)p;
		  else return luaH_newkey(L, t, key);
		}


		public static void luaH_setint (lua_State L, Table t, int key, TValue value_) {
		  /*const */TValue p = luaH_getint(t, key);
		  TValue cell;
		  if (p != luaO_nilobject)
		  	cell = (TValue)(p);
		  else {
		  	TValue k = new TValue();
		    setnvalue(k, cast_num(key));
		    cell = luaH_newkey(L, t, k);
		  }
		  setobj2t(L, cell, value_);
		}



		public static int unbound_search (Table t, uint j) {
		  uint i = j;  /* i is zero or a present index */
		  j++;
		  /* find `i' and `j' such that i is present and j is not */
		  while (!ttisnil(luaH_getint(t, (int)j))) { //FIXME:(int)
			i = j;
			j *= 2;
			if (j > (uint)MAX_INT) {  /* overflow? */
			  /* table was built with bad purposes: resort to linear search */
			  i = 1;
			  while (!ttisnil(luaH_getint(t, (int)i))) i++; //FIXME:(int)
			  return (int)(i - 1);
			}
		  }
		  /* now do a binary search between them */
		  while (j - i > 1) {
			uint m = (i+j)/2;
			if (ttisnil(luaH_getint(t, (int)m))) j = m; //FIXME:(int)
			else i = m;
		  }
		  return (int)i;
		}


		/*
		** Try to find a boundary in table `t'. A `boundary' is an integer index
		** such that t[i] is non-nil and t[i+1] is nil (and 0 if t[1] is nil).
		*/
		public static int luaH_getn (Table t) {
		  uint j = (uint)t.sizearray;
		  if (j > 0 && ttisnil(t.array[j - 1])) {
			/* there is a boundary in the array part: (binary) search for it */
			uint i = 0;
			while (j - i > 1) {
			  uint m = (i+j)/2;
			  if (ttisnil(t.array[m - 1])) j = m;
			  else i = m;
			}
			return (int)i;
		  }
		  /* else must find a boundary in hash part */
		  else if (isdummy(t.node))  /* hash part is empty? */ //FIXME:[0]
			return (int)j;  /* that is easy... */
		  else return unbound_search(t, j);
		}



		//#if defined(LUA_DEBUG)

		//Node *luaH_mainposition (const Table *t, const TValue *key) {
		//  return mainposition(t, key);
		//}

		//int luaH_isdummy (Node *n) { return isdummy(n); }

		//#endif

	}
}

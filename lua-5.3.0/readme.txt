

(x:no need) loadlib.c.cs: some static functions no private prefix



---------------------

17:05 2019/12/5
linit.c
17:09 2019/12/5
loadlib.c
17:10 2019/12/5
lobject.h
17:10 2019/12/5
lopcodes.c
17:11 2019/12/5
ltable.c
17:14 2019/12/5
luac.c

---------------------

bug:

			if (othern != mp) {  /* is colliding node out of its main position? */
				//Debug.WriteLine("othern != mp, " + gnext(othern));
			  /* yes; move colliding node into free position */
			  while (Node.plus(othern, gnext(othern)) != mp)  /* find previous */
endless loop, gnext(othern) == 0, othern.i_key.nk.next==0 -------->		        Node.inc(ref othern, gnext(othern));
		      gnext_set(othern, cast_int(f - othern));  /* re-chain with 'f' in place of 'mp' */
		      f.Assign(mp);  /* copy colliding node into free pos. (mp->next also goes) */
		      if (gnext(mp) != 0) {
		      	//if (mp - f == 0)
		      	//{
		      	//	Debug.WriteLine("???");
		      	//}
		      	gnext_inc(f, cast_int(mp - f));  /* correct 'next' */
		        gnext_set(mp, 0);  /* now 'mp' is free */
		      }
			  setnilvalue(gval(mp));
			}
			else {  /* colliding node is in its own main position */
			  /* new node will go into free position */
		      if (gnext(mp) != 0)
		      	gnext_set(f, cast_int(Node.plus(mp, gnext(mp)) - f));  /* chain new position */
		      else lua_assert(gnext(f) == 0);
		      gnext_set(mp, cast_int(f - mp));
		      mp = f;
			}
		  }
		  
		  
/*
** $Id: lfunc.c,v 2.41 2014/02/18 13:39:37 roberto Exp $
** Auxiliary functions to manipulate prototypes and closures
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using Instruction = System.UInt32;

	public partial class Lua
	{

		public static Closure luaF_newCclosure (lua_State L, int n) {
		  Closure c = luaC_newobj<Closure>(L, LUA_TCCL, sizeCclosure(n)).cl;
		  c.c.nupvalues = cast_byte(n);
		  c.c.upvalue = new TValue[n]; //FIXME:added???
		  for (int i = 0; i < n; i++)  //FIXME:added???
			  c.c.upvalue[i] = new lua_TValue(); //FIXME:??? //FIXME:added???
		  return c;
		}


		public static Closure luaF_newLclosure (lua_State L, int n) {
		  Closure c = luaC_newobj<Closure>(L, LUA_TLCL, sizeLclosure(n)).cl;
		  c.l.p = null;
		  c.l.nupvalues = cast_byte(n);
		  c.l.upvals = new UpVal[n]; //FIXME:added???
		  /*
		  for (int i = 0; i < n; i++) //FIXME:added???
			  c.l.upvals[i] = new UpVal(); //FIXME:??? //FIXME:added???
		  while (n > 0) c.l.upvals[n] = null; //FIXME:added??? while (n--) c->l.upvals[n] = NULL;
		  */
		  while (n-- != 0) c.l.upvals[n] = null;
		  return c;
		}

		/*
		** fill a closure with new closed upvalues
		*/
		public static void luaF_initupvals (lua_State L, LClosure cl) {
		  int i;
		  for (i = 0; i < cl->nupvalues; i++) {
		    UpVal *uv = luaM_new(L, UpVal);
		    uv->refcount = 1;
		    uv->v = &uv->u.value;  /* make it closed */
		    setnilvalue(uv->v);
		    cl->upvals[i] = uv;
		  }
		}


		public static UpVal luaF_findupval (lua_State L, StkId level) {
		  UpVal **pp = &L->openupval;
		  UpVal *p;
		  UpVal *uv;
		  lua_assert(isintwups(L) || L->openupval == NULL);
		  while (*pp != NULL && (p = *pp)->v >= level) {
		    lua_assert(upisopen(p));
		    if (p->v == level)  /* found a corresponding upvalue? */
		      return p;  /* return it */
		    pp = &p->u.open.next;
		  }
		  /* not found: create a new upvalue */
		  uv = luaM_new(L, UpVal);
		  uv->refcount = 0;
		  uv->u.open.next = *pp;  /* link it to list of open upvalues */
		  uv->u.open.touched = 1;
		  *pp = uv;
		  uv->v = level;  /* current value lives in the stack */
		  if (!isintwups(L)) {  /* thread not in list of threads with upvalues? */
		    L->twups = G(L)->twups;  /* link it to the list */
		    G(L)->twups = L;
		  }
		  return uv;
		}


		public static void luaF_close (lua_State L, StkId level) {
		  UpVal *uv;
		  while (L->openupval != NULL && (uv = L->openupval)->v >= level) {
		    lua_assert(upisopen(uv));
		    L->openupval = uv->u.open.next;  /* remove from `open' list */
		    if (uv->refcount == 0)  /* no references? */
		      luaM_free(L, uv);  /* free upvalue */
		    else {
		      setobj(L, &uv->u.value, uv->v);  /* move value to upvalue slot */
		      uv->v = &uv->u.value;  /* now current value lives here */
		      luaC_upvalbarrier(L, uv);
		    }
		  }
		}


		public static Proto luaF_newproto (lua_State L) {
		  Proto f = luaC_newobj<Proto>(L, LUA_TPROTO, (uint)GetUnmanagedSize(typeof(Proto))).p;
		  f.k = null;
		  f.sizek = 0;
		  f.p = null;
		  f.sizep = 0;
		  f.code = null;
          f.cache = null;
		  f.sizecode = 0;
          f.lineinfo = null;
		  f.sizelineinfo = 0;
		  f.upvalues = null;
          f.sizeupvalues = 0;
		  f.numparams = 0;
		  f.is_vararg = 0;
		  f.maxstacksize = 0;
		  f.locvars = null;
		  f.sizelocvars = 0;
		  f.linedefined = 0;
		  f.lastlinedefined = 0;
		  f.source = null;
		  return f;
		}


		public static void luaF_freeproto (lua_State L, Proto f) {
		  luaM_freearray<Instruction>(L, f.code);
		  luaM_freearray<Proto>(L, f.p);
		  luaM_freearray<TValue>(L, f.k);
		  luaM_freearray<Int32>(L, f.lineinfo);
		  luaM_freearray<LocVar>(L, f.locvars);
		  luaM_freearray<Upvaldesc>(L, f.upvalues);
		  luaM_free(L, f);
		}


		/*
		** Look for n-th local variable at line `line' in function `func'.
		** Returns null if not found.
		*/
		public static CharPtr luaF_getlocalname (Proto f, int local_number, int pc) {
		  int i;
		  for (i = 0; i<f.sizelocvars && f.locvars[i].startpc <= pc; i++) {
			if (pc < f.locvars[i].endpc) {  /* is variable active? */
			  local_number--;
			  if (local_number == 0)
				return getstr(f.locvars[i].varname);
			}
		  }
		  return null;  /* not found */
		}

	}
}

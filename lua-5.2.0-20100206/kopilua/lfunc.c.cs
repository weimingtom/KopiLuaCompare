/*
** $Id: lfunc.c,v 2.18 2009/12/11 13:39:34 roberto Exp roberto $
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

		public static Closure luaF_newCclosure (lua_State L, int n, Table e) {
		  Closure c = luaC_newobj<Closure>(L, LUA_TFUNCTION, sizeCclosure(n), null, 0).cl;
		  c.c.isC = 1;
		  c.c.env = e;
		  c.c.nupvalues = cast_byte(n);
		  c.c.upvalue = new TValue[n];
		  for (int i = 0; i < n; i++)
			  c.c.upvalue[i] = new lua_TValue(); //FIXME:???
		  return c;
		}


		public static Closure luaF_newLclosure (lua_State L, int n, Table e) {
		  Closure c = luaC_newobj<Closure>(L, LUA_TFUNCTION, sizeLclosure(n), null, 0).cl;
		  c.l.isC = 0;
		  c.l.env = e;
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


		public static UpVal luaF_newupval (lua_State L) {
		  UpVal uv = luaC_newobj<UpVal>(L, LUA_TUPVAL, (uint)GetUnmanagedSize(typeof(UpVal)), null, 0).uv;
		  uv.v = uv.u.value;
		  setnilvalue(uv.v);
		  return uv;
		}

		public static UpVal luaF_findupval (lua_State L, StkId level) {
		  global_State g = G(L);
		  GCObjectRef pp = new OpenValRef(L);
		  UpVal p;
		  UpVal uv;
		  while (pp.get() != null && (p = gco2uv(pp.get())).v >= level) {
			lua_assert(p.v != p.u.value);
			if (p.v == level) {  /* found a corresponding upvalue? */
			  if (isdead(g, obj2gco(p)))  /* is it dead? */
				changewhite(obj2gco(p));  /* ressurrect it */
			  return p;
			}
			pp = new NextRef(p);
		  }
		  /* not found: create a new one */
		  uv = luaC_newobj<UpVal>(L, LUA_TUPVAL, (uint)GetUnmanagedSize(typeof(UpVal)), pp, 0).uv;
		  uv.v = level;  /* current value lives in the stack */
		  uv.u.l.prev = g.uvhead;  /* double link it in `uvhead' list */
		  uv.u.l.next = g.uvhead.u.l.next;
		  uv.u.l.next.u.l.prev = uv;
		  g.uvhead.u.l.next = uv;
		  lua_assert(uv.u.l.next.u.l.prev == uv && uv.u.l.prev.u.l.next == uv);
		  return uv;
		}


		private static void unlinkupval (UpVal uv) {
		  lua_assert(uv.u.l.next.u.l.prev == uv && uv.u.l.prev.u.l.next == uv);
		  uv.u.l.next.u.l.prev = uv.u.l.prev;  /* remove from `uvhead' list */
		  uv.u.l.prev.u.l.next = uv.u.l.next;
		}


		public static void luaF_freeupval (lua_State L, UpVal uv) {
		  if (uv.v != uv.u.value)  /* is it open? */
			unlinkupval(uv);  /* remove from open list */
		  luaM_free(L, uv);  /* free upvalue */
		}


		public static void luaF_close (lua_State L, StkId level) {
		  UpVal uv;
		  global_State g = G(L);
		  while (L.openupval != null && (uv = gco2uv(L.openupval)).v >= level) {
			GCObject o = obj2gco(uv);
			lua_assert(!isblack(o) && uv.v != uv.u.value);
			L.openupval = uv.next;  /* remove from `open' list */
			if (isdead(g, o))
			  luaF_freeupval(L, uv);  /* free upvalue */
			else {
			  unlinkupval(uv);
			  setobj(L, uv.u.value, uv.v);
			  uv.v = uv.u.value;  /* now current value lives here */
			  luaC_linkupval(L, uv);  /* link upvalue into `gcroot' list */
			}
		  }
		}


		public static Proto luaF_newproto (lua_State L) {
		  Proto f = luaC_newobj<Proto>(L, LUA_TPROTO, (uint)GetUnmanagedSize(typeof(Proto)), null, 0).p;
		  f.k = null;
		  f.sizek = 0;
		  f.p = null;
		  f.sizep = 0;
		  f.code = null;
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
		  f.envreg = (byte)NO_REG; //FIXME:(byte)
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

		// we have a gc, so nothing to do
		public static void luaF_freeclosure (lua_State L, Closure c) {
		  int size = (int)((c.c.isC != 0) ? sizeCclosure(c.c.nupvalues) :
			                  sizeLclosure(c.l.nupvalues)); //FIXME:(int)
		  //luaM_freemem(L, c, size);
		  SubtractTotalBytes(L, size);
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

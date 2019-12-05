13:46 2019/12/5
lgc.c
13:47 2019/12/5
luaconf.h
13:48 2019/12/5
lcode.c
13:49 2019/12/5
llimits.h
13:51 2019/12/5
lmem.h
13:53 2019/12/5
lobject.h
13:54 2019/12/5
lvm.c
13:57 2019/12/5
lzio.h


-----------------------

		public static void luaZ_resizebuffer(lua_State L, Mbuffer buff, int size) {
???--->			if (buff.buffer == null) //FIXME:added
				buff.buffer = new CharPtr(); //FIXME:added
				


		public static void luaZ_resizebuffer(lua_State L, Mbuffer buff, int size) {
			if (buff.buffer == null) //FIXME:added
				buff.buffer = new CharPtr(); //FIXME:added
			//luaM_reallocvector(L, ref buff.buffer.chars, (int)buff.buffsize, size);
			buff.buffer = luaM_reallocvchar(L, buff.buffer,
				buff.buffsize, size);
			buff.buffsize = (uint)buff.buffer.chars.Length; }
			
		/*
		** Arrays of chars do not need any test
		*/
		public static CharPtr luaM_reallocvchar(lua_State L, CharPtr b, int on,int n) {
b.chars------->			return (CharPtr)(char[])luaM_realloc_(L, (b.chars), /*(on)*1*//*sizeof(char)*//*,*/ (n)*1/*sizeof(char)*/); }
			


	
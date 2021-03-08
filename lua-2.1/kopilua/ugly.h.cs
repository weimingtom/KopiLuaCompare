/*
** ugly.h
** TecCGraf - PUC-Rio
** $Id: ugly.h,v 1.2 1994/11/13 14:39:04 roberto Stab $
*/
namespace KopiLua
{
	public partial class Lua
	{
		//#ifndef ugly_h
		//#define ugly_h
		
		/* This enum must have the same order of the array 'reserved' in lex.c */
		
		//enum {
		public const int U_and = 128;
		public const int U_do = 129;
		public const int U_else = 130;
		public const int U_elseif = 131;
		public const int U_end = 132;
		public const int U_function = 133;
		public const int U_if = 134;
		public const int U_local = 135;
		public const int U_nil = 136;
		public const int U_not = 137;
		public const int U_or = 138;
		public const int U_repeat = 139;
		public const int U_return = 140;
		public const int U_then = 141;
		public const int U_until = 142;
		public const int U_while = 143;
		public const int U_eq = '=' + 128;
		public const int U_le = '<' + 128;
		public const int U_ge = '>' + 128;
		public const int U_ne = '~' + 128;
		public const int U_sc = '.' + 128;
		//};
		
		//#endif
	}
}



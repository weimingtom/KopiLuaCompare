/*
** lua.c
** Linguagem para Usuarios de Aplicacao
*/

char *rcs_lua="$Id: lua.c,v 1.1 1993/12/17 18:41:19 celes Exp $";

#include <stdio.h>

#include "lua.h"
#include "lualib.h"

int main (int argc, char *argv[])
{
 int i;
 if (0) fprintf(stdout, "=================>iolib_open\n");
 iolib_open ();
 if (0) fprintf(stdout, "=================>strlib_open\n");
 strlib_open ();
 if (0) fprintf(stdout, "=================>mathlib_open\n");
 mathlib_open ();
 if (argc < 2)
 {
   char buffer[2048];
  if (0)  fprintf(stdout, "=================>lua_dostring\n");
   while (gets(buffer) != 0)
     lua_dostring(buffer);
 }
 else {
   if (0) fprintf(stdout, "=================>lua_dofile\n");
   for (i=1; i<argc; i++)
    lua_dofile (argv[i]);
    
  }
  return 0;
}

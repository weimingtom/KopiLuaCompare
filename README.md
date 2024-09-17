# KopiLuaCompare
Comparation of kopilua and lua, and porting kopilua to lua 5.2.x and 5.3.x  

	兵者，诡道也。  
	故能而示之不能，用而示之不用，近而示之远，远而示之近。  
	利而诱之，乱而取之，实而备之，强而避之，怒而挠之，卑而骄之，佚而劳之，亲而离之。  
	攻其无备，出其不意。此兵家之胜，不可先传也。  
	——《孙子兵法始计篇》  

## Status (C -> CSharp)      
* lua-1.0 (done, since 2017-12-23)  
* lua-1.1 (done, since 2020-10-17)  
* lua-2.1 (done, since 2021-03-06)  
* lua-5.1.4 (done, since 2017-03-04)  
* lua-5.1.5 (done, since 2017-05-12)  
* lua-5.2.0-2007 (done, since 2017-05-21)  
* lua-5.2.0-20071029 (done, since 2017-05-26)  
* lua-5.2.0-2008 (done, since 2017-05-28)  
* lua-5.2.0-2009 (done, since 2017-05-30)  
* lua-5.2.0-20090702 (done, since 2017-06-17)  
* lua-5.2.0-20100206 (done, since 2017-07-05)  
* lua-5.2.0-alpha (done, since 2017-07-17)  
* lua-5.2.0-beta (done, since 2017-08-05)  
* lua-5.2.0 (done, since 2017-08-16)  
* lua-5.2.1 (done, since 2019-07-06, not tested)  
* lua-5.2.2 (done, since 2019-07-21, not tested)  
* lua-5.2.3 (done, since 2019-07-28, not tested)  
* lua-5.2.4 (done, since 2019-08-03, not tested)  
* lua-5.3.0-work1 (done, since 2019-10-07, not tested)  
* lua-5.3.0-work2 (done, since 2019-11-19, not tested)  
* lua-5.3.0-work3 (done, since 2019-11-26, not tested)  
* lua-5.3.0-alpha (done, since 2019-12-01, not tested)  
* lua-5.3.0-beta (done, since 2019-12-02, not tested)  
* lua-5.3.0-rc0 (done, since 2019-12-05, not tested)  
* lua-5.3.0-rc1 (done, since 2019-12-05, not tested)  
* lua-5.3.0-rc2 (done, since 2019-12-05, not tested)  
* lua-5.3.0-rc3 (done, since 2019-12-05, not tested)  
* lua-5.3.0 (done, since 2019-12-05, not tested)  
* lua-5.3.1 (done, since 2019-12-07, not tested)  
* lua-5.3.2 (done, since 2019-12-13, not tested)  
* lua-5.3.3 (done, since 2019-12-13, not tested)  
* lua-5.3.4 (done, since 2019-12-14, not tested)  
* lua-5.3.5 (done, since 2019-12-14, not tested)  

## About status above    
* If not mentioned, luac is only compiled successfully but not tested, or removed.  
* If not mentioned, lua is only compiled successfully, and tested with very simple snippets, for example  
> return 1+1  
> print("hello")    
> return 1, 2  
> return nil  
> print("hello') -- test lua error  
> os.exit() -- test lua exiting  
* New tests (for 5.3.x):  
> 1+1  
> 1,2  
> a = 10  
> a  

## Compare Tool  
* Beyond Compare Version 3.0.11  

## References  
* https://github.com/lua/lua  
* https://github.com/gerich-home/kopilua  
* https://github.com/jintiao/cclua  
* https://github.com/cogwheel/lua-wow  

## Related Projects  
* https://github.com/weimingtom/Kuuko  
https://github.com/weimingtom/Kuuko/blob/master/lua_hack.md  
CSharp/Java port (done)    

* https://github.com/weimingtom/kurumi    
Java/Golang port (WIP, not active)    

* https://github.com/weimingtom/KuukoBack      
Kuuko Csharp/Java port for KopiLuaCompare (done)    

* https://github.com/weimingtom/kaleido  
Kurumi golang port (WIP, planning)  

* https://github.com/weimingtom/lua1mod  
Lua 1.0 CSharp port (done)    

* https://gitee.com/weimingtom/lua-5.1.4_profile  
Lua 5.1.4 performance profile   

* https://github.com/weimingtom/lua11mod  
Lua 1.1 csharp port (done)    

## some tips  
* On Windows, use Ctrl+Z then Enter to exit  

## About SciMark for Lua  
https://github.com/weimingtom/KuukoBack/blob/master/SciMark.txt  
High score is better  

## Other workspaces  
* https://github.com/weimingtom/konomi  
(TODO) dart port  
* https://github.com/weimingtom/LuaDardo_mod  
(TOOD) dart port  
* https://github.com/weimingtom/sluamod  
(TODO) C# dll  
* luachecker
yacc, c  
* https://github.com/weimingtom/luacheck  
* https://github.com/weimingtom/mlnjava  
Java and VS2013  
* https://github.com/weimingtom/LuaScriptCoreDotnetMod  
C# dll  
* https://github.com/weimingtom/luabind_hello  
c++ (TODO)  
* https://github.com/weimingtom/xluamod  
C# dll  
* https://github.com/weimingtom/mlnandroid  
Java, Android only, (TODO) migrate to pure java (done, see mlnjava)      
* (TODO) https://github.com/weimingtom/LuaInterfaceMod  
Running failed, dll runtime error    
* https://github.com/weimingtom/kaleido  
Porting not finished  
* https://github.com/weimingtom/kohaku  
Porting not finished  
* (TODO) https://github.com/weimingtom/kirin  
Running failed, runtime error    
* https://github.com/weimingtom/sharpluamod  
C# dll  
* https://github.com/weimingtom/KuukoBack  
* https://github.com/weimingtom/Kuuko/blob/master/README2.md  
* https://github.com/weimingtom/metamorphose  
* https://github.com/weimingtom/BiljanMod  
* https://github.com/weimingtom/sharplua-1  
* https://github.com/weimingtom/MoonSharpMod  
* https://github.com/weimingtom/xlua_playground  
* https://github.com/weimingtom/xlua_mingw  
* https://github.com/weimingtom/UniLuaMod  
* https://github.com/weimingtom/sena  

## Other projects for Lua  
* https://github.com/weimingtom/Kuuko/blob/master/lua_hack.md  
* https://github.com/weimingtom/Kuuko/blob/master/README2.md  

## Other workspaces for Python  
* see https://github.com/weimingtom/cecilia  

## Other workspaces for Ruby  
* see https://github.com/weimingtom/eriri  

## Other workspaces for JavaScript and C like  
* see https://github.com/weimingtom/njs_fork  

## Other workspaces for metamorphose (jill)    
https://github.com/weimingtom/metamorphose_ts  
https://github.com/weimingtom/metamorphose  
https://github.com/weimingtom/metamorphose_csharp  
https://github.com/weimingtom/metamorphose_js  
https://github.com/weimingtom/metamorphose_jsweet  
https://github.com/weimingtom/metamorphose_go  

## luau  
* search luau_v17_test_pass_success.rar  
* https://github.com/Roblox/luau  
target_link_libraries(Luau.Repl.CLI PRIVATE pthread)  
+target_link_libraries(Luau.Repl.CLI PRIVATE atomic)  
search baidupan, luau_v3_cli.rar  

## Other workspaces for yacc  
* https://github.com/weimingtom/wmt_yacc_study/blob/master/README.md  

## Other workspaces for JVM and other bytecode modern VM  
* https://github.com/weimingtom/wmt_jvm_study  

## TODO  
* https://github.com/TypeScriptToLua/TypeScriptToLua  
* https://github.com/kon9chunkit/GitHub-Chinese-Top-Charts/blob/ac1341847a1d844d682d38fa3645095fd49ec2df/README-Part2.md  
* https://github.com/oceancx/CXEngine  
* lua-rs  
https://github.com/lonng/lua-rs  
* https://github.com/Tencent/LuaHelper  

## Unity console (UConsole port), with old GUI (not uGUI or unity gui)    
* https://github.com/AustinSmith13/UConsole
* see vendor/unity/UConsoleTest_v2.7z  
* see vendor/unity/UConsoleTest_v1.7z  
* need define UNITY_5  
* add ConsoleGui to Camera
* press KeyCode.BackQuote to show or hide console  
* tested with unity 2020.1.6 for UConsoleTest_v2.7z  

## ELua, KopiLuaInterface    
* https://gitee.com/ximu/ELua  
* https://github.com/weimingtom/ELua_fork
* https://github.com/weimingtom/Kuuko/blob/master/lua_hack.md  

## (TODO) BrainDamage-1.2-src  
* 界面代码（含一个Lua代码编辑器）
* vc_ui.7z  

## TODO  
* lua-3.0 mingw version need to port lua.lex file
* (done a little, see sluamod) https://github.com/pangweiwei/slua  

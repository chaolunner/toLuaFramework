# toLuaFramework

Created by chaolun

toLua 生成 Wrap 报错解决方案
---

- 找到 **ToLuaExport.cs** 文件
- 将报错的函数添加到 **memberFilter** 列表里（注意，生成 Wrap 时不会再生成这个方法，Lua 中也就调用不到该函数了）
- 如果存在同名函数，且只想去掉报错的部分，可以在 **memberInfoFilter** 列表里写入精确的函数名

Build版本toLua报错？
---

纯净版的ToLua，是不会在Build的时候，帮你把 **Assets/Lua** 和 **Assets/ToLua/Lua** 文件夹下的 **.lua** 文件拷贝到 **Resources** 或者 **StreamingAssets** 文件夹下的。

第一种解决办法是你可以使用菜单栏下的 **Lua->Copy Lua files to Resources** 等类似选项，但是你仍然需要处理怎么从 **Resources** 或 **StreamingAssets** 文件夹中加载 **.lua** 文件的问题。

第二种解决办法是你可以使用菜单栏下的 **Lua->Build bundle files not jit** 等类似选项，但是你仍然需要处理怎么从 **AssetBundle** 加载 **.lua** 文件的问题。

第三种解决办法是这个项目正在使用的方法，你可以通过使用菜单栏下 **Tools->Build->All** 选项 (详情请查看 AddressablesEditor.cs 脚本)，它会在使用 **Addressables** 打包之前，做一份 **Assets/Lua** 和 **Assets/ToLua/Lua** 下的拷贝到 **Assets/Source/Lua** 文件夹下，并将这个文件夹添加到 **Addressables** 的 **ToLua** 组中，然后进行打包。

然后在程序运行的最开始，通过 **LuaFacade** 脚本讲这些标有 **lua** 标签（label）的资源拷贝到 **Persistent** 文件夹下的某个位置，具体取决于 **LuaConst.luaResDir** 的设置。

之所以使用 **LuaConst.luaResDir** 这个位置，是因为 **LuaState** 会在初始化的时候调用 **AddSearchPath(LuaConst.luaResDir)**， 而 **AddSearchPath** 会帮助我们自动加载这个目录下的 **.lua** 文件。

使用 Addressables 需要注意的问题
---

基础知识可以参考[这里](https://github.com/chaolunner/xLuaFramework/wiki/Addressable)

- 怎么进行一次干净的远程加载测试？
  - **Tools->Clean->All**
  - **Tools->Build->All**
  - 启动 Hosting Service（注意在启动之前要配置好你的 **RemoteLoadPath** ）

    注意格式应该像这个样子 （http://[PrivateIpAddress]:[HostingServicePort]） 我们只需要修改对应的 **PrivateIpAddress** 值就可以了，记得去掉 **/[BuildTarget]**

- 怎么进行增量更新？
  - **Tools->Build->Lua Only** （更新Lua脚本）
  - 接着按 Addressables 正常的增量更新流程走就可以了
    - **Tools->Check for Content Update Restrications**
    - **Build->Update a Previous Build**

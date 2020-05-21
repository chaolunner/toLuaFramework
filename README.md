# toLuaFramework

Created by chaolun

toLua 生成 Wrap 报错解决方案
---

- 找到 **ToLuaExport.cs** 文件
- 将报错的函数添加到 **memberFilter** 列表里（注意，生成 Wrap 时不会再生成这个方法，Lua 中也就调用不到该函数了）
- 如果存在同名函数，且只想去掉报错的部分，可以在 **memberInfoFilter** 列表里写入精确的函数名

使用 Addressables 需要注意的问题
---

基础知识可以参考[这里](https://github.com/chaolunner/xLuaFramework/wiki/Addressable)

- 怎么进行一次干净的远程加载测试？
    - Tools->Clean->All
    - Tools->Build Player Content
      你可以在 `Assets/Scripts/Addressables/Editor/AddressablesEditor.cs` 脚本查看具体方法。
    - 启动 Hosting Service（注意在启动之前要配置好你的 **RemoteLoadPath** ！）
      注意格式应该像这个样子 （`http://[PrivateIpAddress]:[HostingServicePort]`） 我们只需要修改对应的 **PrivateIpAddress** 值就可以了，记得去掉 **/[BuildTarget]**！
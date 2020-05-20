# toLuaFramework

Created by chaolun

toLua 生成 Wrap 报错解决方案
---

- 找到 **ToLuaExport.cs** 文件
- 将报错的函数添加到 **memberFilter** 列表里（注意，生成 Wrap 时不会再生成这个方法，Lua 中也就调用不到该函数了）
- 如果存在同名函数，且只想去掉报错的部分，可以在 **memberInfoFilter** 列表里写入精确的函数名
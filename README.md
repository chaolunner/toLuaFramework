# toLuaFramework

Created by chaolun

框架的特性
---

- 继承自 **Base.lua** 的类（**class(Base)**）
  - 可以使用 **get/set** ，比如你在 A 类中实现了一个 **function getLength() return 0 end** 方法，那么你可以直接使用 local a = A.new() print(a.length) 来调用该方法，且你只能通过实现 **function setLength()** 方法，才能设置 **a.length** 的值。
  - 可以隐式调用 **table elements**，如果你的类中有 **table elements**，那么你可以直接使用 local a = A.new() print(a[0])，这个和调用 print(a.elements[0]) 是一样的。

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

    这里需要注意，如果你也使用了 **protobuf** 那么你需要再运行 **Tools->Build->Proto Only** （更新Proto文件）

    或者你也可以直接使用 **Tools->Build->Lua + Proto** (更新Lua脚本和Proto文件)

  - 接着按 Addressables 正常的增量更新流程走就可以了
    - **Tools->Check for Content Update Restrications**
    - **Build->Update a Previous Build**

- 太麻烦，想要一键增量更新？ 看这里，看这里。首先 **Tools->Check for Content Update** （之所以分两步是因为你可以在分完组之后，对新生成的组进行配置上的调整），然后 **Tools->Build->Content Update**，搞定！

- 增量更新的结果与文档所说的不一致？
  
  Local Group 和 Static Remote Group 在 **增量更新** 之后 **依旧会生成新的包** 而不是文档所说的 **不会更改**。

  其实不难理解官方怎么做的用意，因为不再更新 Local Group 或 Static Remote Group，那么已经被移除的内容将会成为 **dead data** 一直存在于包中。

  而如果是一些新用户，之前没有下载过这些包，那么他也将不得不下载（只要你需要用到这个包中的部分资源）这些 **dead data** 。

  然而到目前为止（Addressables 1.9.2），增量更新生成的新 **catalog.json** 并不会添加一个指向旧包的索引，所以会导致加载旧包内的资源失败。

  已经有人发布了相关的问题 **[static group bundle name has changed in catalog when build for content update](https://forum.unity.com/threads/static-group-bundle-name-has-changed-in-catalog-when-build-for-content-update.703394/)**，可以等待官方的修复版本 或 使用官方提供的 **hack** 方案。

  hack 方案：
  
    选中 Group **Content Packing & Loading -> Advanced Options -> Bundle Naming** 选择 **Filename**。 这样就真的如文档所说的那样 **不会更改** 了。

    **还有一点需要注意，虽然这样做之后是不会生成新的包了，但是你可能依然无法加载旧包内的资源，因为在增量更新之后 Crc 检测会失败！**

    我的解决办法是，禁用 Group **Content Packing & Loading -> Advanced Options -> Use Asset Bundle Crc** 选项。

- 怎么发布到服务器？
  - 下载 [XAMPP](https://www.apachefriends.org/download.html) Apache Http Server
  - 域名映射
    - 先修改hosts，在hosts中添加你要绑定在Apache的多个域名

      **C:/WINDOWS/system32/drivers/etc/hosts**

    - 然后在最底部添加您要绑定的域名，格式如下：

      **127.0.0.1 chaolunner.toluaframework.com**

      添加完毕后，保存即可。Win7/Win10 遇到hosts文件无法修改，先把hosts文件复制到桌面，在桌面改好后再复制到 etc 文件夹下进行替换。
  - 虚拟主机绑定域名
    - 打开 Apache 配置文件 xampp/apache/conf/extra/httpd-vhosts.conf

      然后在httpd-vhosts.conf最底部直接添加以下代码：

      ```
      <VirtualHost *:80>
        ServerAdmin webmaster@chaolunner.toluaframework.comco
        DocumentRoot "<你安装Xampp的路径>/htdocs/chaolunner.toluaframework.com"
        ServerName chaolunner.toluaframework.com
      </VirtualHost>
      ```

      以上配置信息含义如下：

      - ServerAdmin 表示该网站的管理者。
      - DocumentRoot 表示你要绑定的网站的绝对路径（注意需要让PHP访问的到，配置到Xampp下的htdocs目录即可）
      - ServerName 这个就是你要绑定的域名了。如果是本地的，绑定前需要配置之前提到的 hosts 文件。
      
  - 完成上述步骤之后，在 XAMPP 中启动 Apache 服务，并在浏览器中 输入 http://chaolunner.toluaframework.com 就可以访问到 DocumentRoot （注意，chaolunner.toluaframework.com 这个文件夹是之后需要添加的，如果你要测试这一步的正确性，你可以先创建一个空的同名文件夹）
  - 但是，你也会发现使用 localhost 直接定位到了 DocumentRoot 下的内容，而我们期望的是还是定位到 <你安装Xampp的路径>/htdocs/ 的内容。也就是默认的httpd的设置失效了。解决办法就是把 localhost 的配置在 httpd-vhosts.conf 里配置回来。

    再在 httpd-vhosts.conf 文件的最后添加上如下内容，并重启 Apache。

    ```
    <VirtualHost *:80>
      DocumentRoot "<你安装Xampp的路径>/htdocs/"
      ServerName localhost
    </VirtualHost>
    ```

  - 打开 Addressables->Profiles，新建一个 Profile 命名为 XAMPP，并将 RemoteLoadPath 设置为 `http://chaolunner.toluaframework.com/[BuildTarget]`
  - 打开 Addressables->Groups，将 Profile 切换为 XAMPP，并做一次 Build，将打包出来的文件夹（默认是项目根目录下的 ServerData 文件夹）复制到 htdocs 文件夹下，并重命名为 chaolunner.toluaframework.com

  题外话：XAMPP会占用Serial Port端口，导致Arduino IDE之类的软件无法正常上传脚本。

集成 lua-protobuf 第三方pb3解析库
---

- [tolua_runtime](https://github.com/topameng/tolua_runtime) 源码下载
- [lua-protobuf](https://github.com/starwing/lua-protobuf) 源码下载
- [配置好的Msys2下载](https://pan.baidu.com/s/1c2JzvDQ)
- 命令行下跳转到 msys2 的目录下：如 c:\msys2 目录，**cd c:\msys2**
- 执行 **mingw32_shell.bat** 启动32位编译环境，只能编译32位的库
- 执行 **mingw64_shell.bat** 启动64位编译环境，只能编译64位的库
- 32位环境和64位环境不能交叉编译
- 将 lua-protobuf 中的两个文件解析文件（**pb.c 和 pb.h**）替换到 tolua_runtime 库中
- 将 **pb.c** 中的 **luaopen_pb** 函数替换为

  ```
  LUALIB_API int luaopen_pb(lua_State *L) {
    luaL_Reg libs[] = {
        { "pack",     Lbuf_pack     },
        { "unpack",   Lslice_unpack },
  #define ENTRY(name) { #name, Lpb_##name }
        ENTRY(clear),
        ENTRY(load),
        ENTRY(loadfile),
        ENTRY(encode),
        ENTRY(decode),
        ENTRY(types),
        ENTRY(fields),
        ENTRY(type),
        ENTRY(field),
        ENTRY(typefmt),
        ENTRY(enum),
        ENTRY(defaults),
        ENTRY(hook),
        ENTRY(tohex),
        ENTRY(fromhex),
        ENTRY(result),
        ENTRY(option),
        ENTRY(state),
  #undef  ENTRY
        { NULL, NULL }
    };
    luaL_Reg meta[] = {
        { "__gc", Lpb_delete },
        { "setdefault", Lpb_state },
        { NULL, NULL }
    };
    if (luaL_newmetatable(L, PB_STATE)) {
        luaL_setfuncs(L, meta, 0);
        lua_pushvalue(L, -1);
        lua_setfield(L, -2, "__index");
    }
    #if LUA_VERSION_NUM < 502
    luaL_register(L, "pb", libs);
    #else
    luaL_newlib(L, libs);
    #endif
    return 1;
  }
  ```

- 将 lua-protobuf 中的 **protoc.lua**、**serpent.lua**、**luaunit.lua** 以及 **test.lua** 复制到项目中的Lua文件夹下
- 编译库
  - windows库
    - 32位
      - 启动 msys2，32位编译环境，跳转到 tolua_runtime 目录下
      - 执行 **./build_win32.sh**
      - 在 **Plugins\x86** 目录下看见 **tolua.dll** 文件，便编译成功
    - 64位
      - 启动 msys2，64位编译环境，跳转到 tolua_runtime 目录下
      - 执行 **./build_win64.sh**
      - 在 **Plugins\x86_64** 目录下看见 **tolua.dll** 文件，便编译成功
  - Android库
    - 下载[NDK](https://developer.android.google.cn/ndk/downloads/older_releases.html)（建议使用 android-ndk-r15c 64位版本）
    - 下载完成后解压到不包含中文和空格的目录下
    - 将 **build_arm.sh**、**build_x86.sh** 以及 **build_arm64.sh** 文件中的 **NDK** 路径改为你解压后的NDK的路径，并将所有 **$NDK/ndk-build** 替换为 **$NDK/ndk-build.cmd**
    - 将 **link_arm64.bat** 文件中的 ndkPath 修改为你解压后的NDK的路径
    - armeabi-v7a
      - 启动 msys2，32位编译环境，跳转到 tolua_runtime 目录下
      - 执行 **./build_arm.sh**
      - 在 **Plugins\Android\libs\armeabi-v7a** 目录下看见 **libtolua.so** 文件，便编译成功
    - armeabi-v7a
      - 启动 msys2，32位编译环境，跳转到 tolua_runtime 目录下
      - 执行 **./build_x86.sh**
      - 在 **Plugins\Android\libs\x86** 目录下看见 **libtolua.so** 文件，便编译成功
    - armeabi-v7a
      - 启动 msys2，64位编译环境，跳转到 tolua_runtime 目录下
      - 执行 **./build_arm64.sh**
      - 在 **Plugins\Android\libs\arm64-v8a** 目录下看见 **libtolua.so** 文件，便编译成功

proto 生成 .pb 文件并加载
---

- 下载 [Visual Studio Code](https://code.visualstudio.com/Download)
- 在应用商店中搜索 protobuf 插件，选择 [vscode-proto3](https://github.com/zxh0/vscode-proto3) 安装
- 到刚刚安装的插件目录，vscode的插件放置的目录，一般在C:\Users\<UserName>\.vscode\extensions下
- 在插件的目录下有一个 example 文件夹，我们把 **.vscode** 文件夹和 **build.bat** 文件拷贝到自己的工程的目录下（也可以直接复制 toLuaFramework/protobuf 下，配置好的文件）
- 如果你不是直接复制的 toLuaFramework/protobuf 文件夹，那么你还需要到谷歌的 [protobuf](https://github.com/protocolbuffers/protobuf/releases) 下载 **win64** 版本
- 需要修改 **.vscode/settings.json** 文件和 **build.bat** 文件（具体可以参考 toLuaFramework/protobuf 下的相应文件）
- 在 vscode 终端中运行 build.bat，并在相应文件夹下生成 .pb 文件就算成功（toLuaFramework 项目的生成路径在 toLuaFramework/Assets/Proto 文件夹下）

  toLuaFramework 也支持运行 **Tools/Addressables/Proto Only** 来生成 .pb 文件（具有可以参考 **AddressablesEditor.cs**）

- 加载可以参考 **Assets/Lua/test.lua** 脚本（注,常用加载方式：**assert(pb.loadfile "Assets/Proto/test.pb")**）


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

  - 打开 Addressables->Profiles，新建一个 Profile 命名为 XAMPP，并将 RemoteLoadPath 设置为 http://chaolunner.toluaframework.com/[BuildTarget]
  - 打开 Addressables->Groups，将 Profile 切换为 XAMPP，并做一次 Build，将打包出来的文件夹（默认是项目根目录下的 ServerData 文件夹）复制到 htdocs 文件夹下，并重命名为 chaolunner.toluaframework.com
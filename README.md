
[![Build status](https://ci.appveyor.com/api/projects/status/6mbrmme08gfcb9lj?svg=true)](https://ci.appveyor.com/project/sanjusss/1fichier-sdk)
[![GitHub license](https://img.shields.io/github/license/sanjusss/1fichier.SDK.svg)](https://github.com/sanjusss/1fichier.SDK/blob/master/LICENSE)
[![GitHub tag](https://img.shields.io/github/tag/sanjusss/1fichier.SDK.svg?logo=GitHub)](https://github.com/sanjusss/1fichier.SDK/tags)
[![GitHub downloads](https://img.shields.io/github/downloads/sanjusss/1fichier.SDK/total.svg?logo=GitHub)](https://github.com/sanjusss/1fichier.SDK/releases)
[![NuGet](https://img.shields.io/nuget/v/1fichier.SDK.svg?logo=NuGet)](https://www.nuget.org/packages/1fichier.SDK/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/1fichier.SDK.svg?logo=NuGet)](https://www.nuget.org/packages/1fichier.SDK/)

# 安装
运行`Install-Package 1fichier.SDK`或`dotnet add package 1fichier.SDK`或`paket add 1fichier.SDK`或 [点击打开下载页](https://github.com/sanjusss/1fichier.SDK/releases/latest)

# API列表
等待补充……

# 使用
[参考测试用例](https://github.com/sanjusss/1fichier.SDK/blob/master/1fichier.SDK.Test/ClientTest.cs)

# 开发
使用单元测试前，需要添加\1fichier.SDK.Test\Properties\config.json文件，内容为：  
```json
{
    "APIKEY": "2NXqTrOHc9BgjupOd8Qy2SjfS3uP291N",
    "PROXY": "你的HTTP代理地址（选填）"
}

```
也可以设置APIKEY、PROXY到环境变量。
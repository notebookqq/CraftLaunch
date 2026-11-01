# CraftLaunch

一个使用原生 WPF (.NET 8) + Fluent Design 风格构建的 Minecraft 启动器，不依赖任何第三方 UI 库。

![.NET](https://img.shields.io/badge/.NET-8.0-blue) ![Platform](https://img.shields.io/badge/Platform-Windows%2010%2B-lightgrey)

## 功能

- **主页**：版本选择、一键启动 / 结束游戏（仅管理自己启动的进程）、游戏路径设置、内存分配滑块、实时运行日志
- **版本管理**：本地版本扫描 / 删除
- **下载**（在线获取，无需手动搜索）
  - 下载版本：Mojang 官方清单，启动即自动加载，支持快照/旧版
  - 下载模组 / 光影：Modrinth 搜索，按游戏版本与加载器筛选，一键下载
  - 下载整合包：`.mrpack` 自动安装（解压 overrides + 清单内全部文件下载，带 SHA1 校验）
- **设置**：深色 / 浅色主题即时切换、游戏目录、Java 路径（自动检测 + 版本校验）、代理
- **启动链路**：完整解析版本 JSON（新式 arguments / 旧式 minecraftArguments）、依赖库 classpath、natives 解压、资源索引
- 下载支持 8 路并行、SHA1 校验、断点续装、失败汇总
- 界面动画：页面切换淡入滑动、开关滑块、按钮按压反馈

## 下载

前往 [Releases](../../releases) 下载 `CraftLaunch-vX.X.X-win-x64.zip`，解压即用（内置 .NET 8 运行时，无需安装）。

### 首次运行提示

由于程序未做付费代码签名，Windows SmartScreen 可能对全新程序提示"无法识别"。两种处理方式任选：

- **推荐**：双击压缩包内的 **Start.bat**——它会自动解除系统对下载文件的网络锁定并启动程序，之后不会再有任何提示
- 或在 SmartScreen 弹窗上点"更多信息"→"仍要运行"（每次更新后可能需再点一次）

## 从源码构建

```
dotnet build CraftLaunch/CraftLaunch.csproj -c Release
```

需要 .NET 8 SDK (net8.0-windows)。

## 目录结构

```
CraftLaunch/
├─ Models/       实体与接口（McVersion、Modrinth、Mojang JSON…）
├─ Services/     版本扫描、下载器、启动参数构造、Java 定位、日志…
├─ ViewModels/   轻量 MVVM（无第三方依赖）
├─ Views/        页面（主页 / 版本管理 / 下载 / 设置 / 关于）
└─ Themes/       Fluent 风格主题与控件样式（浅色 / 深色）
```

## 说明

- 启动旧版本（1.7 以前）建议在设置中指定 Java 8
- 整合包安装后需确保对应 Minecraft 版本已下载

---
制作组：笔记本说电脑

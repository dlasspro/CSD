# Classworks Desktop 项目结构说明

为遵循业界通用的项目工程化规范，提升代码的可维护性与模块化，对 Classworks Desktop 项目的目录架构进行了重构。本说明旨在为后续开发维护提供清晰的指引。

## 目录结构总览

```text
CSD/
├── Assets/                 # 静态资源目录（图片、图标、字体等）
├── CSD.Package/            # Windows 应用打包工程（MSIX 等打包配置）
├── Helpers/                # 工具类及辅助方法目录
├── Models/                 # 数据模型与配置实体类目录
├── Properties/             # 项目属性及启动配置文件
├── Services/               # 核心服务及后台逻辑目录
├── Settings/               # 设置模块相关组件与接口
├── Views/                  # 视图组件及窗口目录 (UI 层)
├── icons/                  # 应用所需图标文件
└── ... (项目根文件如 App.xaml, CSD.csproj, README.md 等)
```

## 核心模块 (Core Modules)

### 1. Views（视图层）
负责所有的用户界面展示和 UI 交互逻辑，确保单一职责。所有的窗口（Windows）及页面均放置于此。
- `MainWindow.xaml` / `.cs` - 应用程序主窗口
- `InitializationWindow.xaml` / `.cs` - 应用初始化向导界面
- `SettingsWindow.cs` - 应用设置窗口
- `AttendanceWindow.xaml` / `.cs` - 考勤功能相关界面
- `CarouselWindow.xaml` / `.cs` - 轮播展示界面
- 其他如 `AboutWindow.cs`, `DebugWindow.cs`, `RandomPickerWindow.cs`

### 2. Services（服务层）
提供后台业务逻辑、数据处理及全局服务，独立于 UI。
- `UpdateService.cs` & `UpdateInstaller.cs` - 应用更新及安装服务
- `Logger.cs` - 全局日志记录服务
- `EditPreferencesSync.cs` - 编辑偏好同步服务
- `HomeworkPayloadMerge.cs` - 作业数据合并与处理服务

### 3. Models（模型层）
用于存放数据结构、业务实体类及配置文件的数据映射模型。
- `AppSettings.cs` - 应用全局配置模型
- `UpdateInfo.cs` - 更新信息实体类
- `RandomPickerSettings.cs` - 随机点名设置模型
- `AppJsonSerializerContext.cs` - JSON 序列化上下文（用于 AOT/性能优化）

### 4. Helpers（工具层）
包含通用性强、可在各个模块复用的静态方法或辅助类。
- `AnimationHelper.cs` - UI 动画辅助工具
- `MarkdownTextRenderer.cs` - Markdown 文本渲染工具

### 5. Settings（设置模块）
负责应用程序配置的管理与不同设置分类的 UI 模块解耦。
- `ISettingsModule.cs` & `SettingsModuleBase.cs` - 设置模块的基础接口及基类
- 包含如 `AccountSettingsModule.cs`, `DisplaySettingsModule.cs` 等具体的分类设置面板逻辑。

## 优化说明

1. **命名空间同步**：各文件移至对应目录后，统一更新了代码中的 `namespace`（如 `CSD.Views`、`CSD.Services`），以匹配文件夹层级。
2. **XAML 引用修正**：视图组件中的 `x:Class` 声明与相关 `xmlns` 引用路径均已重构，以保障 XAML 设计器正常解析。
3. **全局 Using 配置**：在源码中加入了模块的依赖引用，减少因拆分目录导致的引用报错，提升构建与开发效率。
4. **验证机制**：重构后已通过 `dotnet build` 编译，并进行本地启动测试，核心流程均无路径与模块引用异常。
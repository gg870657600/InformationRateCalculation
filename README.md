# 理论信息速率计算

卫星通信理论信息速率计算工具，基于 Avalonia UI 跨平台框架开发。

## 功能模块

### TDM 速率计算
- 支持 FEC 类型：LDPC、Turbo、LDPC 扩频、Turbo 扩频
- 调制方式：BPSK、QPSK、8PSK、16PSK、32PSK
- 自动计算封装效率、物理帧结构、导频开销
- 根据带宽、滚降因子、符号速率计算理论信息速率

### TDMA 速率计算
- 超帧周期、时隙类型（长时隙/短时隙）配置
- 控制时隙比例、登录参数设置
- 自动计算 TDMA 数据速率

### TDM 门限查询
- Es/N0 和 Eb/N0 门限数据表
- 支持 128 Ksps、1000 Ksps 不同符号速率
- 可按调制方式和编码速率筛选

### TDMA 门限查询
- WaveId、时隙类型、MODCOD 对应关系
- DVB-S2 门限及偏差计算

## 技术栈

| 技术 | 版本 |
|------|------|
| .NET | 9.0 |
| Avalonia UI | 11.3.6 |
| CommunityToolkit.Mvvm | 8.4.0 |

## 构建与发布

### 环境要求
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### 编译运行
```bash
dotnet run --project InformationRateCalculation.Desktop
```

### 发布单文件版本（Windows x64）
```bash
dotnet publish InformationRateCalculation.Desktop\InformationRateCalculation.Desktop.csproj \
  -c Release -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:EnableCompressionInSingleFile=true
```

输出位置：`InformationRateCalculation.Desktop\bin\Release\net9.0\publish\win-x64\`

### 发布 Android APK
```bash
cd InformationRateCalculation.Android
dotnet publish InformationRateCalculation.Android.csproj \
  -c Release -r android-arm64 \
  --self-contained true \
  -p:AndroidEnableProfiledAot=false
```

## 项目结构

```
InformationRateCalculation/
├── InformationRateCalculation/              # 共享核心库
│   ├── ViewModels/MainViewModel.cs         # 业务逻辑（计算引擎）
│   ├── Views/MainView.axaml                # 主界面（4标签页布局）
│   └── Views/MainWindow.axaml              # 桌面窗口
├── InformationRateCalculation.Desktop/      # 桌面端（Windows/Mac/Linux）
├── InformationRateCalculation.Android/      # Android 端
├── InformationRateCalculation.iOS/          # iOS 端
└── InformationRateCalculation.Browser/      # 浏览器端（WebAssembly）
```

## 许可证

MIT License

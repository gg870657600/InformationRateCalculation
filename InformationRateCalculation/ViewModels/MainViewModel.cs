
using Avalonia.Data.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace InformationRateCalculation.ViewModels;
/// <summary>
/// 生成安卓apk命令：
/// E:\工作\代码\InformationRateCalculation\InformationRateCalculation.Android> 目录下运行命令：
/// dotnet publish InformationRateCalculation.Android.csproj -c Release -r android-arm64 --self-contained true -p:AndroidEnableProfiledAot=false
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {

        OnSelectedFecChanged(SelectedFec);
        OnSelectedSlotLenChanged(SelectedSlotLen);
        FilteredThresholdData = new ObservableCollection<TdmThresholdOption>(_allThresholdData);
    }
    // ✅ FEC 选项列表（固定不变）
    #region TDM
    public List<string> FecOptions { get; } = new()
        {
            "LDPC",
            "Turbo",
            "LDPC 扩频",
            "Turbo 扩频"
        };
    // 当前选中的 FEC
    [ObservableProperty]
    private string? _selectedFec = "LDPC";
    //调制列表
    [ObservableProperty]
    private ObservableCollection<ModulationOption> _availableModulationOptions = new();
    // 当前选中的调制
    [ObservableProperty]
    private ModulationOption? _selectedModulationOption;
    // IP 包长
    [ObservableProperty]
    private decimal? _ipPktLen = 1024;
    // 封装后包长
    [ObservableProperty]
    private decimal? _pktLenAfterEncap = 1034;
    // 封装效率
    [ObservableProperty]
    private decimal? _encapEff;
    // WaveId
    [ObservableProperty]
    private int _waveId;
    // FEC 字节长度
    [ObservableProperty]
    private int _FecBytesLen;
    //物理帧符号长
    [ObservableProperty]
    private int _physicalFrameLength;

    // 可用调制选项
    [ObservableProperty]
    private ObservableCollection<string> _availableModulations = new();
    //带宽
    [ObservableProperty]
    private decimal? _bandwidth = 1000M;
    //滚降因子
    [ObservableProperty]
    private decimal? _roll = 0.2M;
    //符号速率
    [ObservableProperty]
    private decimal? _symRate = 833.3333M;
    //单个符号传输时间（us）
    [ObservableProperty]
    private decimal? _symbolTime = 1000 / 833.3333M;
    //传输一个包的理论时延（ms）
    [ObservableProperty]
    private decimal? _pktTheoLatency;
    //物理帧总个数
    [ObservableProperty]
    private decimal? _totalPhyFrames;
    //理论信息速率
    [ObservableProperty]
    private decimal? _infoRate;

    //不同前向纠错码下的导频
    public bool IsPilot8Visible => SelectedFec == "Turbo" || SelectedFec == "Turbo 扩频";
    public bool IsPilot16Visible => SelectedFec == "Turbo" || SelectedFec == "Turbo 扩频";
    public bool IsPilot32Visible => SelectedFec == "LDPC" || SelectedFec == "Turbo" ||
                                    SelectedFec == "LDPC 扩频" || SelectedFec == "Turbo 扩频";
    public bool IsPilot64Visible => SelectedFec == "LDPC" || SelectedFec == "LDPC 扩频";
    // TDM调制方式映射表
    private static readonly Dictionary<string, List<ModulationOption>> ModulationMap = new()
    {
        ["LDPC"] = new List<ModulationOption>
            {
                new() { Name = "BPSK 1/2", WaveId = 1, PackedLength = 1034, EncapsulationEfficiency = 0.99032862, FecBytesLen = 879,    FrameLen32 =  16896, FrameLen64 = 16640 },
                new() { Name = "BPSK 3/4", WaveId = 2, PackedLength = 1510, EncapsulationEfficiency = 0.99377483, FecBytesLen = 1464,   FrameLen32 =  16896, FrameLen64 = 16640 },
                new() { Name = "BPSK 8/9", WaveId = 3, PackedLength = 1034, EncapsulationEfficiency = 0.99032862, FecBytesLen = 1779,   FrameLen32 =  16896, FrameLen64 = 16640 },
                new() { Name = "QPSK 1/2", WaveId = 4, PackedLength = 266, EncapsulationEfficiency = 0.962406015, FecBytesLen = 879,    FrameLen32 =  8544, FrameLen64 = 8384 },
                new() { Name = "QPSK 3/4", WaveId = 5, PackedLength = 266, EncapsulationEfficiency = 0.962406015, FecBytesLen = 1464,   FrameLen32 =  8544, FrameLen64 = 8384 },
                new() { Name = "QPSK 8/9", WaveId = 6, PackedLength = 1034, EncapsulationEfficiency = 0.99032862, FecBytesLen = 1779,   FrameLen32 =  8544, FrameLen64 = 8384 },
                new() { Name = "8PSK 3/4", WaveId = 7, PackedLength = 266, EncapsulationEfficiency = 0.962406015, FecBytesLen = 1464,   FrameLen32 =  5728, FrameLen64 = 5696 },
                new() { Name = "8PSK 8/9", WaveId = 8, PackedLength = 1034, EncapsulationEfficiency = 0.99032862, FecBytesLen = 1779,   FrameLen32 =  5728, FrameLen64 = 5696 },
                new() { Name = "16PSK 3/4", WaveId = 9, PackedLength = 1034, EncapsulationEfficiency = 0.99032862, FecBytesLen = 1464,  FrameLen32 =  4352, FrameLen64 = 4288 },
                new() { Name = "16PSK 8/9", WaveId = 10, PackedLength = 1510, EncapsulationEfficiency = 0.99377483, FecBytesLen = 1779, FrameLen32 =  4352, FrameLen64 = 4288 },
                new() { Name = "32PSK 3/4", WaveId = 11, PackedLength = 1410, EncapsulationEfficiency = 0.992007801, FecBytesLen = 1464,FrameLen32 =  3520, FrameLen64 = 3456 },
                new() { Name = "32PSK 8/9", WaveId = 12, PackedLength = 1510, EncapsulationEfficiency = 0.99377483, FecBytesLen = 1779, FrameLen32 =  3520, FrameLen64 = 3456 }
            },

        ["Turbo"] = new List<ModulationOption>
            {
                new() { Name = "BPSK 1/3", WaveId = 2, PackedLength = 42, EncapsulationEfficiency = 0.993210475, FecBytesLen = 123, FrameLen8 = 3528, FrameLen16 = 3312, FrameLen32 = 3232 },
                new() { Name = "BPSK 1/2", WaveId = 4, PackedLength = 42, EncapsulationEfficiency = 0.993210475, FecBytesLen = 188, FrameLen8 = 3592, FrameLen16 = 3376, FrameLen32 = 3264 },
                new() { Name = "QPSK 1/3", WaveId = 8, PackedLength = 43, EncapsulationEfficiency = 0.993210475, FecBytesLen = 123, FrameLen8 = 1840, FrameLen16 = 1728, FrameLen32 = 1696 },
                new() { Name = "QPSK 1/2", WaveId = 2, PackedLength = 42, EncapsulationEfficiency = 0.993210475, FecBytesLen = 188, FrameLen8 = 1872, FrameLen16 = 1760, FrameLen32 = 1728 },
                new() { Name = "QPSK 2/3", WaveId = 4, PackedLength = 42, EncapsulationEfficiency = 0.993210475, FecBytesLen = 264, FrameLen8 = 1968, FrameLen16 = 1856, FrameLen32 = 1792 },
                new() { Name = "QPSK 3/4", WaveId = 8, PackedLength = 43, EncapsulationEfficiency = 0.993210475, FecBytesLen = 298, FrameLen8 = 1976, FrameLen16 = 1856, FrameLen32 = 1824 },
                new() { Name = "QPSK 4/5", WaveId = 4, PackedLength = 42, EncapsulationEfficiency = 0.993210475, FecBytesLen = 438, FrameLen8 = 2656, FrameLen16 = 2496, FrameLen32 = 2432 },
                new() { Name = "QPSK 5/6", WaveId = 8, PackedLength = 43, EncapsulationEfficiency = 0.993210475, FecBytesLen = 333, FrameLen8 = 1984, FrameLen16 = 1872, FrameLen32 = 1824 },
                new() { Name = "QPSK 6/7", WaveId = 4, PackedLength = 42, EncapsulationEfficiency = 0.993210475, FecBytesLen = 438, FrameLen8 = 2496, FrameLen16 = 2336, FrameLen32 = 2272 },
                new() { Name = "QPSK 7/8", WaveId = 8, PackedLength = 43, EncapsulationEfficiency = 0.993210475, FecBytesLen = 170, FrameLen8 = 1048, FrameLen16 = 992, FrameLen32 = 960 },
                new() { Name = "8PSK 2/3", WaveId = 4, PackedLength = 42, EncapsulationEfficiency = 0.993210475, FecBytesLen = 355, FrameLen8 = 1776, FrameLen16 = 1680, FrameLen32 = 1632 },
                new() { Name = "8PSK 3/4", WaveId = 8, PackedLength = 43, EncapsulationEfficiency = 0.993210475, FecBytesLen = 400, FrameLen8 = 1784, FrameLen16 = 1680, FrameLen32 = 1632 },
                new() { Name = "8PSK 5/6", WaveId = 4, PackedLength = 42, EncapsulationEfficiency = 0.993210475, FecBytesLen = 444, FrameLen8 = 1784, FrameLen16 = 1680, FrameLen32 = 1632 },
                new() { Name = "16APSK 3/4", WaveId = 8, PackedLength = 43, EncapsulationEfficiency = 0.993210475, FecBytesLen = 175, FrameLen8 = 688, FrameLen16 = 656, FrameLen32 = 640 },
                new() { Name = "16APSK 5/6", WaveId = 4, PackedLength = 42, EncapsulationEfficiency = 0.993210475, FecBytesLen = 599, FrameLen8 = 1800, FrameLen16 = 1696, FrameLen32 = 1664 }
            },

        // Turbo 扩频实现
        ["Turbo 扩频"] = new List<ModulationOption>
            {
                new() { Name = "BPSK 1/3", WaveId = 42, SF = 2, EncapsulationEfficiency = 0.993210475, FecBytesLen = 123, FrameLen8 = 7264, FrameLen16 = 6816, FrameLen32 = 6656 },
                new() { Name = "BPSK 1/3", WaveId = 42, SF = 4, EncapsulationEfficiency = 0.993210475, FecBytesLen = 123, FrameLen8 = 14528, FrameLen16 = 13632, FrameLen32 = 13312 },
                new() { Name = "BPSK 1/3", WaveId = 43, SF = 8, EncapsulationEfficiency = 0.993210475, FecBytesLen = 188, FrameLen8 = 29056, FrameLen16 = 27264, FrameLen32 = 26624 },
                new() { Name = "BPSK 1/2", WaveId = 42, SF = 2, EncapsulationEfficiency = 0.993210475, FecBytesLen = 123, FrameLen8 = 7280, FrameLen16 = 6944, FrameLen32 = 6784 },
                new() { Name = "BPSK 1/2", WaveId = 42, SF = 4, EncapsulationEfficiency = 0.993210475, FecBytesLen = 123, FrameLen8 = 14560, FrameLen16 = 13888, FrameLen32 = 13568 },
                new() { Name = "BPSK 1/2", WaveId = 43, SF = 8, EncapsulationEfficiency = 0.993210475, FecBytesLen = 188, FrameLen8 = 29450, FrameLen16 = 27776, FrameLen32 = 27136 }
            },

        // LDPC 扩频实现
        ["LDPC 扩频"] = new List<ModulationOption>
            {
                new() { Name = "BPSK 1/2", WaveId = 1, SF = 2, EncapsulationEfficiency = 0.993210475, FecBytesLen = 879, FrameLen32 = 34240, FrameLen64 = 33792 },
                new() { Name = "BPSK 1/2", WaveId = 1, SF = 4, EncapsulationEfficiency = 0.993210475, FecBytesLen = 879, FrameLen32 = 68480, FrameLen64 = 67584 },
                new() { Name = "BPSK 1/2", WaveId = 1, SF = 8, EncapsulationEfficiency = 0.993210475, FecBytesLen = 879, FrameLen32 = 136960, FrameLen64 = 135168 },
                new() { Name = "BPSK 1/2", WaveId = 1, SF = 16, EncapsulationEfficiency = 0.993210475, FecBytesLen = 879, FrameLen32 = 273920, FrameLen64 = 270336 },
                new() { Name = "BPSK 1/2", WaveId = 1, SF = 32, EncapsulationEfficiency = 0.993210475, FecBytesLen = 879, FrameLen32 = 547840, FrameLen64 = 540672 },
                new() { Name = "BPSK 3/4", WaveId = 2, SF = 2, EncapsulationEfficiency = 0.993210475, FecBytesLen = 1464, FrameLen32 = 34240, FrameLen64 = 33792 },
                new() { Name = "BPSK 3/4", WaveId = 2, SF = 4, EncapsulationEfficiency = 0.993210475, FecBytesLen = 1464, FrameLen32 = 68480, FrameLen64 = 67584 },
                new() { Name = "BPSK 3/4", WaveId = 2, SF = 8, EncapsulationEfficiency = 0.993210475, FecBytesLen = 1464, FrameLen32 = 135936, FrameLen64 = 134144 },
                new() { Name = "BPSK 8/9", WaveId = 3, SF = 2, EncapsulationEfficiency = 0.993210475, FecBytesLen = 1779, FrameLen32 = 34240, FrameLen64 = 33792 },
                new() { Name = "BPSK 8/9", WaveId = 3, SF = 4, EncapsulationEfficiency = 0.993210475, FecBytesLen = 1779, FrameLen32 = 68480, FrameLen64 = 67584 },
                new() { Name = "BPSK 8/9", WaveId = 3, SF = 8, EncapsulationEfficiency = 0.993210475, FecBytesLen = 1779, FrameLen32 = 135936, FrameLen64 = 134144 }
            }
    };
    partial void OnSelectedFecChanged(string? value)
    {
        // 获取调制列表
        if (string.IsNullOrEmpty(value) || !ModulationMap.TryGetValue(value, out var modulationList))
        {
            // 清空
            AvailableModulationOptions = new(); // 清空列表
            SelectedModulationOption = null;
            return;
        }


        // ✅ 直接将 ModulationOption 列表转换为 ObservableCollection
        AvailableModulationOptions = new ObservableCollection<ModulationOption>(modulationList);

        // 👇 关键：设置默认选中项（必须在 AvailableModulations 更新之后！）
        SelectedModulationOption = modulationList.FirstOrDefault(m => m.Name == "BPSK 1/2")
                           ?? modulationList.First(); // 否则选中第一个 


        // 封装后包长
        PktLenAfterEncap = (value == "LDPC" || value == "Turbo") ? _ipPktLen + 10 : _ipPktLen + 7;
        
        //封装效率
        EncapEff = IpPktLen / PktLenAfterEncap;
        UpdateData();
        // 刷新导频可见性
        OnPropertyChanged(nameof(IsPilot8Visible));
        OnPropertyChanged(nameof(IsPilot16Visible));
        OnPropertyChanged(nameof(IsPilot32Visible));
        OnPropertyChanged(nameof(IsPilot64Visible));


    }


    partial void OnSelectedModulationOptionChanged(ModulationOption? value)
    {
        // 👈 这个 `value` 就是当前选中的 ModulationOption 对象！
        //物理帧符号
        if (Bandwidth.HasValue && value != null)
        {
            if (_selectedFec == "LDPC" || _selectedFec == "LDPC 扩频")
            {
                PhysicalFrameLength = (int)(Bandwidth.Value > 500 ? value.FrameLen64 : value.FrameLen32);
            }
            else
            {
                if (_bandwidth < 100)
                {
                    PhysicalFrameLength = (int)value.FrameLen8;
                }
                else
                {
                    PhysicalFrameLength = (int)(_bandwidth > 500 ? value.FrameLen32 : value.FrameLen16);
                }

            }
        }
        //传输一个包的理论时延（ms）更新
        UpdateData();
    }


    partial void OnIpPktLenChanged(decimal? value)
    {
        if(value == null) { IpPktLen = 0;}
        // 当 前向纠错码 改变时，自动更新 PktLenAfterEncap
        if (SelectedFec == "LDPC" || SelectedFec == "Turbo")
        { PktLenAfterEncap = IpPktLen + 10; }
        else
        { PktLenAfterEncap = IpPktLen + 7; }
        EncapEff = IpPktLen / PktLenAfterEncap;

        UpdateData();
    }

    #region 带宽、滚降系数、符号速率换算
    // 添加更新锁防止循环触发
    private bool _isUpdating;

    partial void OnBandwidthChanged(decimal? value)
    {
        if (_isUpdating) return;

        _isUpdating = true;
        UpdateSymRate();
        _isUpdating = false;
        //物理帧符号长度计算
        if (value != null)
        {
            if (_selectedFec == "LDPC" || _selectedFec == "LDPC 扩频")
            {
                PhysicalFrameLength = (int)(_bandwidth > 500 ? _selectedModulationOption.FrameLen64 : _selectedModulationOption.FrameLen32);
            }
            else
            {
                if (_bandwidth < 100)
                {
                    PhysicalFrameLength = (int)_selectedModulationOption.FrameLen8;
                }
                else
                {
                    PhysicalFrameLength = (int)(_bandwidth > 500 ? _selectedModulationOption.FrameLen32 : _selectedModulationOption.FrameLen16);
                }

            }
        }
        else
        {
            Bandwidth = 0m;
        }
        //传输一个包的理论时延（ms）更新
        UpdateData();
    }

    partial void OnRollChanged(decimal? value)
    {
        if (_isUpdating) return;

        _isUpdating = true;
        UpdateSymRate();
        _isUpdating = false;
    }

    partial void OnSymRateChanged(decimal? value)
    {
        //单个符号传输时间（us）
        if (value != 0)
        {
            SymbolTime = 1000 / SymRate;
        }

        if (_isUpdating) return;

        _isUpdating = true;
        UpdateBandwidth();
        _isUpdating = false;
        UpdateData();
    }

    private void UpdateSymRate()
    {

        // 处理空值或无效滚降因子
        if (Roll == null || Bandwidth == null)
        {
            SymRate = 0;
            return;
        }

        // 计算符号速率 (带宽 / (1 + 滚降因子))
        var divisor = 1m + Roll.Value;
        if (divisor == 0)
        {
            // 避免除零错误（理论上不会发生）
            SymRate = null;
            return;
        }

        SymRate = Bandwidth.Value / divisor;

        UpdateData();
    }

    private void UpdateBandwidth()
    {
        // 处理空值或无效滚降因子
        if (Roll == null || SymRate == null)
        {
            Bandwidth = null;
            return;
        }

        // 计算带宽 (符号速率 * (1 + 滚降因子))
        Bandwidth = SymRate.Value * (1m + Roll.Value);
    }
    #endregion
    //传输一个包的理论时延（ms）、物理帧总个数、理论信息速率
    private void UpdateData()
    {
        if (SelectedModulationOption is null) return;
        //传输一个包的理论时延（ms）
        PktTheoLatency = Math.Ceiling((PktLenAfterEncap ?? 0m) / SelectedModulationOption.FecBytesLen) * PhysicalFrameLength * SymbolTime / 1000;
        //物理帧总个数
        TotalPhyFrames = 1 * 1000000 / (PhysicalFrameLength * SymbolTime);
        //理论信息速率
        InfoRate = TotalPhyFrames * SelectedModulationOption.FecBytesLen * 8 * EncapEff / 1000;
    }
    #endregion

    #region TDMA理论信息速率
    //符号速率
    public List<decimal> TdmaSymRate { get; } = new()
        {
            128,160,192,256,320,384,512,640,800,960,1000,1280,1600,2000,2560,3200,4096,6400,8000,10000
        };
    //当前选中的符号速率
    [ObservableProperty]
    private decimal _selectedSymRate = 1600m;
    //控制时隙分配占比
    [ObservableProperty]
    private decimal? _ctrlSlotRatio = 0.08m;
    //登录时隙长度（ms）
    [ObservableProperty]
    private decimal? _loginSlotLen = 2m;
    //测试包长（Byte）
    [ObservableProperty]
    private decimal? _testPktLen = 1500;
    //登录载波否
    public List<string> LoginOptions { get; } = new()
        {
            "是",
            "否"
        };
    // 当前选中的载波登录：是/否
    [ObservableProperty]
    private string _selectedLoginOptions = "是";
    // 时隙长短列表
    public List<string> SlotLenOptions { get; } = new()
        {
            "长时隙",
            "短时隙"
        };
    // 当前选中的时隙长短
    [ObservableProperty]
    private string? _selectedSlotLen = "长时隙";
    //调制列表
    [ObservableProperty]
    private ObservableCollection<ModulationOption2> _availableModulationOptions2 = new();
    // 当前选中的调制
    [ObservableProperty]
    private ModulationOption2? _selectedModulationOption2;

    partial void OnSelectedSlotLenChanged(string? value)
    {
        string? old_Modulation_CodeRate = SelectedModulationOption2 != null ? SelectedModulationOption2.Modulation_CodeRate : null;
        // 获取调制列表
        if (string.IsNullOrEmpty(value) || !ModulationMap2.TryGetValue(value, out var modulationList2))
        {
            // 清空
            AvailableModulationOptions2 = new(); // 清空列表
            SelectedModulationOption2 = null;
            return;
        }


        // 直接将 ModulationOption 列表转换为 ObservableCollection
        AvailableModulationOptions2 = new ObservableCollection<ModulationOption2>(modulationList2);

        // 设置默认选中项（必须在 AvailableModulations 更新之后！）

        // 如果已存在已选择的调制，则保持不变,否则选中第一个
        SelectedModulationOption2 = modulationList2.FirstOrDefault(m => m.Modulation_CodeRate == old_Modulation_CodeRate)
                                ?? modulationList2.First();

        //SelectedModulationOption2 = modulationList2.FirstOrDefault(m => m.Modulation_CodeRate == "QPSK 1/3")
        //                       ?? modulationList2.First(); // 否则选中第一个



        Update();
    }
    // TDM调制方式映射表
    private static readonly Dictionary<string, List<ModulationOption2>> ModulationMap2 = new()
    {
        ["短时隙"] = new List<ModulationOption2>
        {
            new() { DvbId = 3, Modulation = "QPSK", CodeRate = "1/3", SF = 1, PayloadLength = 38, PayloadSymbolCount = 456, TotalChipCount = 536, PreambleLength = 27, PostambleLength = 27, PilotPeriod = 18, PilotBlockLength = 1, PreambleTotal = 26, FpduSize = 36, SlotLength = 544, SlotBitEfficiency = 52.00m, AvailableSlotsPerSuperframe = 1464, ControlSlotsPerSuperframe = 118, MaxServiceSlotsPerSuperframe = 118, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 4, Modulation = "QPSK", CodeRate = "1/2", SF = 1, PayloadLength = 59, PayloadSymbolCount = 472, TotalChipCount = 536, PreambleLength = 22, PostambleLength = 22, PilotPeriod = 24, PilotBlockLength = 1, PreambleTotal = 20, FpduSize = 57, SlotLength = 544, SlotBitEfficiency = 83.00m, AvailableSlotsPerSuperframe = 1464, ControlSlotsPerSuperframe = 118, MaxServiceSlotsPerSuperframe = 118, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 5, Modulation = "QPSK", CodeRate = "2/3", SF = 1, PayloadLength = 85, PayloadSymbolCount = 510, TotalChipCount = 536, PreambleLength = 13, PostambleLength = 13, PilotPeriod = 0, PilotBlockLength = 1, PreambleTotal = 0, FpduSize = 83, SlotLength = 544, SlotBitEfficiency = 122.00m, AvailableSlotsPerSuperframe = 1464, ControlSlotsPerSuperframe = 118, MaxServiceSlotsPerSuperframe = 118, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 6, Modulation = "QPSK", CodeRate = "3/4", SF = 1, PayloadLength = 96, PayloadSymbolCount = 512, TotalChipCount = 536, PreambleLength = 12, PostambleLength = 12, PilotPeriod = 0, PilotBlockLength = 1, PreambleTotal = 0, FpduSize = 94, SlotLength = 544, SlotBitEfficiency = 138.00m, AvailableSlotsPerSuperframe = 1464, ControlSlotsPerSuperframe = 118, MaxServiceSlotsPerSuperframe = 118, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 7, Modulation = "QPSK", CodeRate = "5/6", SF = 1, PayloadLength = 108, PayloadSymbolCount = 519, TotalChipCount = 536, PreambleLength = 9, PostambleLength = 8, PilotPeriod = 0, PilotBlockLength = 1, PreambleTotal = 0, FpduSize = 106, SlotLength = 544, SlotBitEfficiency = 155.00m, AvailableSlotsPerSuperframe = 1464, ControlSlotsPerSuperframe = 118, MaxServiceSlotsPerSuperframe = 118, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 8, Modulation = "8PSK", CodeRate = "2/3", SF = 1, PayloadLength = 115, PayloadSymbolCount = 460, TotalChipCount = 536, PreambleLength = 10, PostambleLength = 9, PilotPeriod = 9, PilotBlockLength = 1, PreambleTotal = 57, FpduSize = 113, SlotLength = 544, SlotBitEfficiency = 166.00m, AvailableSlotsPerSuperframe = 1464, ControlSlotsPerSuperframe = 118, MaxServiceSlotsPerSuperframe = 118, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 9, Modulation = "8PSK", CodeRate = "3/4", SF = 1, PayloadLength = 130, PayloadSymbolCount = 463, TotalChipCount = 536, PreambleLength = 8, PostambleLength = 8, PilotPeriod = 9, PilotBlockLength = 1, PreambleTotal = 57, FpduSize = 128, SlotLength = 544, SlotBitEfficiency = 188.00m, AvailableSlotsPerSuperframe = 1464, ControlSlotsPerSuperframe = 118, MaxServiceSlotsPerSuperframe = 118, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 10, Modulation = "8PSK", CodeRate = "5/6", SF = 1, PayloadLength = 144, PayloadSymbolCount = 462, TotalChipCount = 536, PreambleLength = 9, PostambleLength = 8, PilotPeriod = 9, PilotBlockLength = 1, PreambleTotal = 57, FpduSize = 142, SlotLength = 544, SlotBitEfficiency = 208.00m, AvailableSlotsPerSuperframe = 1464, ControlSlotsPerSuperframe = 118, MaxServiceSlotsPerSuperframe = 118, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 11, Modulation = "16QAM", CodeRate = "3/4", SF = 1, PayloadLength = 175, PayloadSymbolCount = 468, TotalChipCount = 536, PreambleLength = 9, PostambleLength = 8, PilotPeriod = 10, PilotBlockLength = 1, PreambleTotal = 51, FpduSize = 173, SlotLength = 544, SlotBitEfficiency = 254.00m, AvailableSlotsPerSuperframe = 1464, ControlSlotsPerSuperframe = 118, MaxServiceSlotsPerSuperframe = 118, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 12, Modulation = "16QAM", CodeRate = "5/6", SF = 1, PayloadLength = 194, PayloadSymbolCount = 466, TotalChipCount = 536, PreambleLength = 10, PostambleLength = 9, PilotPeriod = 10, PilotBlockLength = 1, PreambleTotal = 51, FpduSize = 192, SlotLength = 544, SlotBitEfficiency = 282.00m, AvailableSlotsPerSuperframe = 1464, ControlSlotsPerSuperframe = 118, MaxServiceSlotsPerSuperframe = 118, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 }
        },
        ["长时隙"] = new List<ModulationOption2>
        {
            new() { DvbId = 13, Modulation = "QPSK", CodeRate = "1/3", SF = 1, PayloadLength = 123, PayloadSymbolCount = 1476, TotalChipCount = 1616, PreambleLength = 32, PostambleLength = 31, PilotPeriod = 20, PilotBlockLength = 1, PreambleTotal = 77, FpduSize = 121, SlotLength = 1624, SlotBitEfficiency = 59.00m, AvailableSlotsPerSuperframe = 490, ControlSlotsPerSuperframe = 120, MaxServiceSlotsPerSuperframe = 450, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 14, Modulation = "QPSK", CodeRate = "1/2", SF = 1, PayloadLength = 188, PayloadSymbolCount = 1504, TotalChipCount = 1616, PreambleLength = 25, PostambleLength = 25, PilotPeriod = 25, PilotBlockLength = 1, PreambleTotal = 62, FpduSize = 186, SlotLength = 1624, SlotBitEfficiency = 91.00m, AvailableSlotsPerSuperframe = 490, ControlSlotsPerSuperframe = 120, MaxServiceSlotsPerSuperframe = 450, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 15, Modulation = "QPSK", CodeRate = "2/3", SF = 1, PayloadLength = 264, PayloadSymbolCount = 1584, TotalChipCount = 1616, PreambleLength = 16, PostambleLength = 16, PilotPeriod = 0, PilotBlockLength = 1, PreambleTotal = 0, FpduSize = 262, SlotLength = 1624, SlotBitEfficiency = 129.00m, AvailableSlotsPerSuperframe = 490, ControlSlotsPerSuperframe = 120, MaxServiceSlotsPerSuperframe = 450, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 16, Modulation = "QPSK", CodeRate = "3/4", SF = 1, PayloadLength = 298, PayloadSymbolCount = 1590, TotalChipCount = 1616, PreambleLength = 13, PostambleLength = 13, PilotPeriod = 0, PilotBlockLength = 1, PreambleTotal = 0, FpduSize = 296, SlotLength = 1624, SlotBitEfficiency = 145.00m, AvailableSlotsPerSuperframe = 490, ControlSlotsPerSuperframe = 120, MaxServiceSlotsPerSuperframe = 450, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 17, Modulation = "QPSK", CodeRate = "5/6", SF = 1, PayloadLength = 333, PayloadSymbolCount = 1599, TotalChipCount = 1616, PreambleLength = 9, PostambleLength = 8, PilotPeriod = 0, PilotBlockLength = 1, PreambleTotal = 0, FpduSize = 331, SlotLength = 1624, SlotBitEfficiency = 163.00m, AvailableSlotsPerSuperframe = 490, ControlSlotsPerSuperframe = 120, MaxServiceSlotsPerSuperframe = 450, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 18, Modulation = "8PSK", CodeRate = "2/3", SF = 1, PayloadLength = 355, PayloadSymbolCount = 1420, TotalChipCount = 1616, PreambleLength = 10, PostambleLength = 9, PilotPeriod = 9, PilotBlockLength = 1, PreambleTotal = 177, FpduSize = 353, SlotLength = 1624, SlotBitEfficiency = 173.00m, AvailableSlotsPerSuperframe = 490, ControlSlotsPerSuperframe = 120, MaxServiceSlotsPerSuperframe = 450, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 19, Modulation = "8PSK", CodeRate = "3/4", SF = 1, PayloadLength = 400, PayloadSymbolCount = 1423, TotalChipCount = 1616, PreambleLength = 8, PostambleLength = 8, PilotPeriod = 9, PilotBlockLength = 1, PreambleTotal = 177, FpduSize = 398, SlotLength = 1624, SlotBitEfficiency = 196.00m, AvailableSlotsPerSuperframe = 490, ControlSlotsPerSuperframe = 120, MaxServiceSlotsPerSuperframe = 450, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 20, Modulation = "8PSK", CodeRate = "5/6", SF = 1, PayloadLength = 444, PayloadSymbolCount = 1422, TotalChipCount = 1616, PreambleLength = 9, PostambleLength = 8, PilotPeriod = 9, PilotBlockLength = 1, PreambleTotal = 177, FpduSize = 442, SlotLength = 1624, SlotBitEfficiency = 217.00m, AvailableSlotsPerSuperframe = 490, ControlSlotsPerSuperframe = 120, MaxServiceSlotsPerSuperframe = 450, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 21, Modulation = "16QAM", CodeRate = "3/4", SF = 1, PayloadLength = 539, PayloadSymbolCount = 1438, TotalChipCount = 1616, PreambleLength = 10, PostambleLength = 9, PilotPeriod = 10, PilotBlockLength = 1, PreambleTotal = 159, FpduSize = 537, SlotLength = 1624, SlotBitEfficiency = 264.00m, AvailableSlotsPerSuperframe = 490, ControlSlotsPerSuperframe = 120, MaxServiceSlotsPerSuperframe = 450, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 },
            new() { DvbId = 22, Modulation = "16QAM", CodeRate = "5/6", SF = 1, PayloadLength = 599, PayloadSymbolCount = 1438, TotalChipCount = 1616, PreambleLength = 10, PostambleLength = 9, PilotPeriod = 10, PilotBlockLength = 1, PreambleTotal = 159, FpduSize = 597, SlotLength = 1624, SlotBitEfficiency = 294.00m, AvailableSlotsPerSuperframe = 490, ControlSlotsPerSuperframe = 120, MaxServiceSlotsPerSuperframe = 450, MaxUnslicedServiceDataRate = 0, AdjustedMaxServiceDataRate = 0 }
        }
    };
    // 为所有相关属性添加变化监听
    partial void OnSelectedSymRateChanged(decimal value) => Update();
    partial void OnSelectedLoginOptionsChanged(string value) => Update();
    partial void OnLoginSlotLenChanged(decimal? value) => Update();
    partial void OnTestPktLenChanged(decimal? value) => Update();

    partial void OnCtrlSlotRatioChanged(decimal? value) => Update();
    // 当选择新的调制方式时也需要更新计算
    partial void OnSelectedModulationOption2Changed(ModulationOption2? value) => Update();
    private void Update()
    {
        if (SelectedModulationOption2 == null) return;
        
        //超帧周期可分配时隙总数
        if (SelectedLoginOptions == "是")
        {
            SelectedModulationOption2.AvailableSlotsPerSuperframe = Math.Truncate((SelectedSymRate * 0.5m * 1000 -
                Math.Ceiling(SelectedSymRate * (LoginSlotLen ?? 0m))) / SelectedModulationOption2.SlotLength);
        }
        else
        {
            SelectedModulationOption2.AvailableSlotsPerSuperframe = Math.Truncate((SelectedSymRate * 0.5m * 1000) /
                SelectedModulationOption2.SlotLength);
        }
        //超帧周期内控制时隙分配数（个）、超帧周期内可分配最大业务时隙（个）
        if (SelectedSlotLen == "长时隙")
        {
            SelectedModulationOption2.ControlSlotsPerSuperframe = Math.Ceiling(SelectedModulationOption2.AvailableSlotsPerSuperframe * (CtrlSlotRatio ?? 0m)) * 3;
            SelectedModulationOption2.MaxServiceSlotsPerSuperframe = SelectedModulationOption2.AvailableSlotsPerSuperframe - SelectedModulationOption2.ControlSlotsPerSuperframe / 3;
        }
        else
        {
            SelectedModulationOption2.ControlSlotsPerSuperframe = Math.Ceiling(SelectedModulationOption2.AvailableSlotsPerSuperframe * (CtrlSlotRatio ?? 0m));
            SelectedModulationOption2.MaxServiceSlotsPerSuperframe = SelectedModulationOption2.AvailableSlotsPerSuperframe - SelectedModulationOption2.ControlSlotsPerSuperframe;
        }
        //不切片最大业务信息速率（kbps）
        SelectedModulationOption2.MaxUnslicedServiceDataRate = SelectedModulationOption2.MaxServiceSlotsPerSuperframe * (SelectedModulationOption2.FpduSize - 2) * 8 / 1000 * 2;
        //切片调整后最大业务信息速率（kbps）
        if (TestPktLen != SelectedModulationOption2.FpduSize - 2 && TestPktLen.HasValue)
        {
            SelectedModulationOption2.AdjustedMaxServiceDataRate = SelectedModulationOption2.MaxUnslicedServiceDataRate - SelectedModulationOption2.MaxUnslicedServiceDataRate * 1000
                / TestPktLen.Value / 8 * 3 * 8 / 1000;
        }
        else
        {
            SelectedModulationOption2.AdjustedMaxServiceDataRate = SelectedModulationOption2.MaxServiceSlotsPerSuperframe;
        }


        ////SelectedModulationOption2 = null;          // 先清引用
        //SelectedModulationOption2 = SelectedModulationOption2;           // 再设回去 → 触发 PropertyChanged
    }
    #endregion

    #region TDM门限
    public List<decimal> ThresholdSymRateOptions { get; } = new() { 128, 1000 };
    public List<string> ThresholdModulationOptions { get; } = new() { "QPSK", "8PSK", "16APSK", "32APSK" };

    private static readonly Dictionary<string, List<string>> ModulationCodeRateMap = new()
    {
        ["QPSK"]   = new() { "1/2", "3/4", "8/9" },
        ["8PSK"]   = new() { "3/4", "8/9" },
        ["16APSK"] = new() { "3/4", "8/9" },
        ["32APSK"] = new() { "8/9" }
    };

    [ObservableProperty]
    private List<string> _thresholdCodeRateOptions = new() { "1/2", "3/4", "8/9" };

    [ObservableProperty]
    private decimal? _selectedThresholdSymRate;

    [ObservableProperty]
    private string? _selectedThresholdModulation;

    [ObservableProperty]
    private string? _selectedThresholdCodeRate;

    [ObservableProperty]
    private ObservableCollection<TdmThresholdOption> _filteredThresholdData = new();

    private List<TdmThresholdOption> _allThresholdData = new()
    {
        // 128Ksps
        new() { SymbolRate=128, Modulation="QPSK",   CodeRate="1/2", RollOff=0.25m, SpectralEfficiency=1.0000m, TheoreticalThreshold=0.95m,  EsPlusN0=3.908m,  InitialEsPlusN0=3.908m },
        new() { SymbolRate=128, Modulation="QPSK",   CodeRate="3/4", RollOff=0.25m, SpectralEfficiency=1.5000m, TheoreticalThreshold=4.48m,  EsPlusN0=6.259m,  InitialEsPlusN0=6.259m },
        new() { SymbolRate=128, Modulation="QPSK",   CodeRate="8/9", RollOff=0.25m, SpectralEfficiency=1.7778m, TheoreticalThreshold=6.60m,  EsPlusN0=7.644m,  InitialEsPlusN0=7.644m },
        new() { SymbolRate=128, Modulation="8PSK",   CodeRate="3/4", RollOff=0.25m, SpectralEfficiency=2.2500m, TheoreticalThreshold=8.47m,  EsPlusN0=9.634m,  InitialEsPlusN0=9.634m },
        new() { SymbolRate=128, Modulation="8PSK",   CodeRate="8/9", RollOff=0.25m, SpectralEfficiency=2.6667m, TheoreticalThreshold=11.26m, EsPlusN0=12.276m, InitialEsPlusN0=12.276m },
        new() { SymbolRate=128, Modulation="16APSK", CodeRate="3/4", RollOff=0.25m, SpectralEfficiency=3.0000m, TheoreticalThreshold=10.77m, EsPlusN0=11.847m, InitialEsPlusN0=11.847m },
        new() { SymbolRate=128, Modulation="16APSK", CodeRate="8/9", RollOff=0.25m, SpectralEfficiency=3.5556m, TheoreticalThreshold=13.46m, EsPlusN0=14.024m, InitialEsPlusN0=14.024m },
        new() { SymbolRate=128, Modulation="32APSK", CodeRate="8/9", RollOff=0.25m, SpectralEfficiency=4.4444m, TheoreticalThreshold=16.41m, EsPlusN0=17.157m, InitialEsPlusN0=17.157m },
        // 1000Ksps
        new() { SymbolRate=1000, Modulation="QPSK",   CodeRate="1/2", RollOff=0.25m, SpectralEfficiency=1.0000m, TheoreticalThreshold=0.95m,  EsPlusN0=3.962m,  InitialEsPlusN0=3.962m },
        new() { SymbolRate=1000, Modulation="QPSK",   CodeRate="3/4", RollOff=0.25m, SpectralEfficiency=1.5000m, TheoreticalThreshold=4.48m,  EsPlusN0=6.174m,  InitialEsPlusN0=6.174m },
        new() { SymbolRate=1000, Modulation="QPSK",   CodeRate="8/9", RollOff=0.25m, SpectralEfficiency=1.7778m, TheoreticalThreshold=6.60m,  EsPlusN0=7.695m,  InitialEsPlusN0=7.695m },
        new() { SymbolRate=1000, Modulation="8PSK",   CodeRate="3/4", RollOff=0.25m, SpectralEfficiency=2.2500m, TheoreticalThreshold=8.47m,  EsPlusN0=9.631m,  InitialEsPlusN0=9.631m },
        new() { SymbolRate=1000, Modulation="8PSK",   CodeRate="8/9", RollOff=0.25m, SpectralEfficiency=2.6667m, TheoreticalThreshold=11.26m, EsPlusN0=12.008m, InitialEsPlusN0=12.008m },
        new() { SymbolRate=1000, Modulation="16APSK", CodeRate="3/4", RollOff=0.25m, SpectralEfficiency=3.0000m, TheoreticalThreshold=10.77m, EsPlusN0=11.614m, InitialEsPlusN0=11.614m },
        new() { SymbolRate=1000, Modulation="16APSK", CodeRate="8/9", RollOff=0.25m, SpectralEfficiency=3.5556m, TheoreticalThreshold=13.46m, EsPlusN0=14.318m, InitialEsPlusN0=14.318m },
        new() { SymbolRate=1000, Modulation="32APSK", CodeRate="8/9", RollOff=0.25m, SpectralEfficiency=4.4444m, TheoreticalThreshold=16.41m, EsPlusN0=17.8m,   InitialEsPlusN0=17.8m }
    };

    partial void OnSelectedThresholdSymRateChanged(decimal? value) => UpdateFilteredThresholdData();
    partial void OnSelectedThresholdModulationChanged(string? value)
    {
        // 调制方式变化时，更新编码码率选项并重置为第一个可用码率
        if (!string.IsNullOrEmpty(value) && ModulationCodeRateMap.TryGetValue(value, out var rates))
        {
            ThresholdCodeRateOptions = rates;
            SelectedThresholdCodeRate = rates.First();
        }
        UpdateFilteredThresholdData();
    }
    partial void OnSelectedThresholdCodeRateChanged(string? value) => UpdateFilteredThresholdData();

    private void UpdateFilteredThresholdData()
    {
        IEnumerable<TdmThresholdOption> query = _allThresholdData;
        if (SelectedThresholdSymRate.HasValue)
            query = query.Where(t => t.SymbolRate == SelectedThresholdSymRate.Value);
        if (!string.IsNullOrEmpty(SelectedThresholdModulation))
            query = query.Where(t => t.Modulation == SelectedThresholdModulation);
        if (!string.IsNullOrEmpty(SelectedThresholdCodeRate))
            query = query.Where(t => t.CodeRate == SelectedThresholdCodeRate);
        FilteredThresholdData = new ObservableCollection<TdmThresholdOption>(query);
    }

    public void RestoreThresholdDefaults()
    {
        foreach (var item in _allThresholdData)
        {
            item.EsPlusN0 = item.InitialEsPlusN0;
        }
        SelectedThresholdSymRate = null;
        SelectedThresholdModulation = null;
        SelectedThresholdCodeRate = null;
        ThresholdCodeRateOptions = new() { "1/2", "3/4", "8/9" };
        FilteredThresholdData = new ObservableCollection<TdmThresholdOption>(_allThresholdData);
    }
    #endregion

    #region TDMA门限
    public List<string> TdmaWaveIdOptions { get; } = new()
    {
        "wave_id4","wave_id5","wave_id6","wave_id7","wave_id8","wave_id9","wave_id10","wave_id11","wave_id12","wave_id13",
        "wave_id14","wave_id15","wave_id16","wave_id17","wave_id18","wave_id19","wave_id20","wave_id21","wave_id22","wave_id23"
    };
    public List<string> TdmaSlotTypeOptions { get; } = new() { "短时隙", "长时隙" };
    public List<string> TdmaModcodOptions { get; } = new()
    {
        "QPSK_1/3","QPSK_1/2","QPSK_2/3","QPSK_3/4","QPSK_5/6",
        "8PSK_2/3","8PSK_3/4","8PSK_5/6",
        "16QAM_3/4","16QAM_5/6"
    };

    [ObservableProperty] private string? _selectedTdmaWaveId;
    [ObservableProperty] private string? _selectedTdmaSlotType;
    [ObservableProperty] private string? _selectedTdmaModcod;

    [ObservableProperty]
    private ObservableCollection<TdmaThresholdOption> _filteredTdmaThresholdData = new();

    private readonly List<TdmaThresholdOption> _allTdmaThresholdData = new()
    {
        // 短时隙 (wave_id4 - wave_id13)
        new() { ClockMode="内时钟", WaveId="wave_id4",  SlotType="短时隙", Modcod="QPSK_1/3",  SpectralEfficiency=0.6667m, EsN0Theory=1.82m,  DvbS2Threshold=0.22m,  RollOff=0.20m, EsPlusN0=3.921m,  InitialEsPlusN0=3.921m },
        new() { ClockMode="内时钟", WaveId="wave_id5",  SlotType="短时隙", Modcod="QPSK_1/2",  SpectralEfficiency=1.0000m, EsN0Theory=3.10m,  DvbS2Threshold=2.34m,  RollOff=0.20m, EsPlusN0=4.556m,  InitialEsPlusN0=4.556m },
        new() { ClockMode="内时钟", WaveId="wave_id6",  SlotType="短时隙", Modcod="QPSK_2/3",  SpectralEfficiency=1.3333m, EsN0Theory=4.86m,  DvbS2Threshold=4.29m,  RollOff=0.20m, EsPlusN0=5.961m,  InitialEsPlusN0=5.961m },
        new() { ClockMode="内时钟", WaveId="wave_id7",  SlotType="短时隙", Modcod="QPSK_3/4",  SpectralEfficiency=1.5000m, EsN0Theory=5.73m,  DvbS2Threshold=5.36m,  RollOff=0.20m, EsPlusN0=6.558m,  InitialEsPlusN0=6.558m },
        new() { ClockMode="内时钟", WaveId="wave_id8",  SlotType="短时隙", Modcod="QPSK_5/6",  SpectralEfficiency=1.6667m, EsN0Theory=7.03m,  DvbS2Threshold=6.68m,  RollOff=0.20m, EsPlusN0=7.663m,  InitialEsPlusN0=7.663m },
        new() { ClockMode="内时钟", WaveId="wave_id9",  SlotType="短时隙", Modcod="8PSK_2/3",  SpectralEfficiency=2.0000m, EsN0Theory=8.58m,  DvbS2Threshold=8.08m,  RollOff=0.20m, EsPlusN0=9.087m,  InitialEsPlusN0=9.087m },
        new() { ClockMode="内时钟", WaveId="wave_id10", SlotType="短时隙", Modcod="8PSK_3/4",  SpectralEfficiency=2.2500m, EsN0Theory=9.90m,  DvbS2Threshold=9.31m,  RollOff=0.20m, EsPlusN0=10.452m, InitialEsPlusN0=10.452m },
        new() { ClockMode="内时钟", WaveId="wave_id11", SlotType="短时隙", Modcod="8PSK_5/6",  SpectralEfficiency=2.5000m, EsN0Theory=11.40m, DvbS2Threshold=10.85m, RollOff=0.20m, EsPlusN0=11.589m, InitialEsPlusN0=11.589m },
        new() { ClockMode="内时钟", WaveId="wave_id12", SlotType="短时隙", Modcod="16QAM_3/4", SpectralEfficiency=3.0000m, EsN0Theory=11.87m, DvbS2Threshold=11.17m, RollOff=0.20m, EsPlusN0=12.288m, InitialEsPlusN0=12.288m },
        new() { ClockMode="内时钟", WaveId="wave_id13", SlotType="短时隙", Modcod="16QAM_5/6", SpectralEfficiency=3.3333m, EsN0Theory=13.49m, DvbS2Threshold=12.56m, RollOff=0.20m, EsPlusN0=13.828m, InitialEsPlusN0=13.828m },
        // 长时隙 (wave_id14 - wave_id23)
        new() { ClockMode="内时钟", WaveId="wave_id14", SlotType="长时隙", Modcod="QPSK_1/3",  SpectralEfficiency=0.6667m, EsN0Theory=0.28m,  DvbS2Threshold=-0.51m, RollOff=0.20m, EsPlusN0=3.031m,  InitialEsPlusN0=3.031m },
        new() { ClockMode="内时钟", WaveId="wave_id15", SlotType="长时隙", Modcod="QPSK_1/2",  SpectralEfficiency=1.0000m, EsN0Theory=2.21m,  DvbS2Threshold=1.71m,  RollOff=0.20m, EsPlusN0=4.008m,  InitialEsPlusN0=4.008m },
        new() { ClockMode="内时钟", WaveId="wave_id16", SlotType="长时隙", Modcod="QPSK_2/3",  SpectralEfficiency=1.3333m, EsN0Theory=4.11m,  DvbS2Threshold=3.69m,  RollOff=0.20m, EsPlusN0=5.184m,  InitialEsPlusN0=5.184m },
        new() { ClockMode="内时钟", WaveId="wave_id17", SlotType="长时隙", Modcod="QPSK_3/4",  SpectralEfficiency=1.5000m, EsN0Theory=5.00m,  DvbS2Threshold=4.73m,  RollOff=0.20m, EsPlusN0=6.119m,  InitialEsPlusN0=6.119m },
        new() { ClockMode="内时钟", WaveId="wave_id18", SlotType="长时隙", Modcod="QPSK_5/6",  SpectralEfficiency=1.6667m, EsN0Theory=6.32m,  DvbS2Threshold=5.94m,  RollOff=0.20m, EsPlusN0=6.953m,  InitialEsPlusN0=6.953m },
        new() { ClockMode="内时钟", WaveId="wave_id19", SlotType="长时隙", Modcod="8PSK_2/3",  SpectralEfficiency=2.0000m, EsN0Theory=7.91m,  DvbS2Threshold=7.49m,  RollOff=0.20m, EsPlusN0=8.423m,  InitialEsPlusN0=8.423m },
        new() { ClockMode="内时钟", WaveId="wave_id20", SlotType="长时隙", Modcod="8PSK_3/4",  SpectralEfficiency=2.2500m, EsN0Theory=9.19m,  DvbS2Threshold=8.77m,  RollOff=0.20m, EsPlusN0=9.507m,  InitialEsPlusN0=9.507m },
        new() { ClockMode="内时钟", WaveId="wave_id21", SlotType="长时隙", Modcod="8PSK_5/6",  SpectralEfficiency=2.5000m, EsN0Theory=10.61m, DvbS2Threshold=10.23m, RollOff=0.20m, EsPlusN0=10.972m, InitialEsPlusN0=10.972m },
        new() { ClockMode="内时钟", WaveId="wave_id22", SlotType="长时隙", Modcod="16QAM_3/4", SpectralEfficiency=3.0000m, EsN0Theory=11.50m, DvbS2Threshold=10.72m, RollOff=0.20m, EsPlusN0=11.779m, InitialEsPlusN0=11.779m },
        new() { ClockMode="内时钟", WaveId="wave_id23", SlotType="长时隙", Modcod="16QAM_5/6", SpectralEfficiency=3.3333m, EsN0Theory=13.03m, DvbS2Threshold=12.04m, RollOff=0.20m, EsPlusN0=13.174m, InitialEsPlusN0=13.174m }
    };

    partial void OnSelectedTdmaWaveIdChanged(string? value) => UpdateFilteredTdmaThresholdData();
    partial void OnSelectedTdmaSlotTypeChanged(string? value) => UpdateFilteredTdmaThresholdData();
    partial void OnSelectedTdmaModcodChanged(string? value) => UpdateFilteredTdmaThresholdData();

    private void UpdateFilteredTdmaThresholdData()
    {
        IEnumerable<TdmaThresholdOption> query = _allTdmaThresholdData;
        if (!string.IsNullOrEmpty(SelectedTdmaSlotType))
            query = query.Where(t => t.SlotType == SelectedTdmaSlotType);
        if (!string.IsNullOrEmpty(SelectedTdmaModcod))
            query = query.Where(t => t.Modcod == SelectedTdmaModcod);
        if (!string.IsNullOrEmpty(SelectedTdmaWaveId))
            query = query.Where(t => t.WaveId == SelectedTdmaWaveId);
        FilteredTdmaThresholdData = new ObservableCollection<TdmaThresholdOption>(query);
    }

    public void RestoreTdmaThresholdDefaults()
    {
        foreach (var item in _allTdmaThresholdData)
        {
            item.EsPlusN0 = item.InitialEsPlusN0;
        }
        SelectedTdmaWaveId = null;
        SelectedTdmaSlotType = null;
        SelectedTdmaModcod = null;
        FilteredTdmaThresholdData = new ObservableCollection<TdmaThresholdOption>(_allTdmaThresholdData);
    }
    #endregion
}
public class ModulationOption
{
    public string? Name { get; set; } // 如 "BPSK 1/2"
    public int WaveId { get; set; }  // wave id
    public int PackedLength { get; set; } // 封装后包长
    public double EncapsulationEfficiency { get; set; } // 封装效率
    public int FecBytesLen { get; set; } // LDPC-FEC 字节长度
    public int PhysicalFrameLength { get; set; } // 物理帧长（含导频）
    public int? FrameLen8 { get; set; }
    public int? FrameLen16 { get; set; }
    public int? FrameLen32 { get; set; }
    public int? FrameLen64 { get; set; }

    /// <summary>
    /// 仅用于 扩频模式：扩频因子（Spreading Factor）
    /// </summary>
    public int? SF { get; set; }
    //调制方式+扩频倍数拼接
    public string DisplayName =>
        SF.HasValue ? $"{Name} 扩频:{SF}" : Name;
}

public partial class ModulationOption2 : ObservableObject
{
    [ObservableProperty]
    private string _slotType = string.Empty; // 长时隙、短时隙

    [ObservableProperty]
    private int _dvbId;  // DVB附录 id

    [ObservableProperty]
    private string _modulation = string.Empty; // 调制方式

    [ObservableProperty]
    private string _codeRate = string.Empty; // 码率

    [ObservableProperty]
    private decimal _sF; // 扩频因子

    [ObservableProperty]
    private decimal _payloadLength; // 有效载荷（byte）

    [ObservableProperty]
    private decimal _payloadSymbolCount; // 有效符号（sym）

    [ObservableProperty]
    private decimal _totalChipCount; // 总符号数（chip）

    [ObservableProperty]
    private decimal _preambleLength; // 前导长度（sym）

    [ObservableProperty]
    private decimal _postambleLength; // 后导长度（sym）

    [ObservableProperty]
    private decimal _pilotPeriod; // 导频周期（sym）

    [ObservableProperty]
    private decimal _pilotBlockLength; // 导频块长度（sym）

    [ObservableProperty]
    private decimal _preambleTotal; // 导频总数（sym）

    [ObservableProperty]
    private decimal _fpduSize; //FPDU总长度（字节）

    [ObservableProperty]
    private decimal _slotLength; //时隙长度（sym）

    [ObservableProperty]
    private decimal _slotBitEfficiency; //时隙突发bit效率  %

    [ObservableProperty]
    private decimal _availableSlotsPerSuperframe; //超帧周期可分配时隙总数（个）

    [ObservableProperty]
    private decimal _controlSlotsPerSuperframe; //超帧周期内控制时隙分配数（个）

    [ObservableProperty]
    private decimal _maxServiceSlotsPerSuperframe; //超帧周期内可分配最大业务时隙（个）

    [ObservableProperty]
    private decimal _maxUnslicedServiceDataRate; //不切片最大业务信息速率（kbps）

    [ObservableProperty]
    private decimal _adjustedMaxServiceDataRate; //切片调整后最大业务信息速率（kbps）

    // 只读属性，依赖其他属性计算
    public string Modulation_CodeRate => $"{_modulation} {_codeRate}";
}

public class DecimalFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal dec)
        {
            return dec.ToString("F2"); // 格式化为两位小数
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            decimal result;
            if (decimal.TryParse(str, out result))
            {
                return result;
            }
        }
        return 0m; // 默认返回0
    }
}

public partial class TdmThresholdOption : ObservableObject
{
    [ObservableProperty]
    private decimal _symbolRate;

    [ObservableProperty]
    private string _modulation = string.Empty;

    [ObservableProperty]
    private string _codeRate = string.Empty;

    [ObservableProperty]
    private decimal _rollOff;

    [ObservableProperty]
    private decimal _spectralEfficiency;

    [ObservableProperty]
    private decimal _theoreticalThreshold;

    [ObservableProperty]
    private decimal _esPlusN0;

    public decimal InitialEsPlusN0 { get; set; }

    public decimal EsN0 => 10m * (decimal)Math.Log10((double)(decimal)Math.Pow(10, (double)EsPlusN0 / 10) - 1);

    public decimal EbN0 => EsN0 - 10m * (decimal)Math.Log10((double)SpectralEfficiency);

    public decimal ThresholdDeviation => EsN0 - TheoreticalThreshold;

    partial void OnEsPlusN0Changed(decimal value)
    {
        OnPropertyChanged(nameof(EsN0));
        OnPropertyChanged(nameof(EbN0));
        OnPropertyChanged(nameof(ThresholdDeviation));
    }
}

public class KspsConverter : IValueConverter
{
    public static readonly KspsConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is decimal d ? $"{d} Ksps" : string.Empty;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public partial class TdmaThresholdOption : ObservableObject
{
    [ObservableProperty] private string _clockMode = string.Empty;
    [ObservableProperty] private string _waveId = string.Empty;
    [ObservableProperty] private string _slotType = string.Empty;
    [ObservableProperty] private string _modcod = string.Empty;
    [ObservableProperty] private decimal _spectralEfficiency;
    [ObservableProperty] private decimal _esN0Theory;
    [ObservableProperty] private decimal _dvbS2Threshold;
    [ObservableProperty] private decimal _rollOff;
    [ObservableProperty] private decimal _esPlusN0;

    public decimal InitialEsPlusN0 { get; set; }

    public decimal EsN0 => 10m * (decimal)Math.Log10((double)(decimal)Math.Pow(10, (double)EsPlusN0 / 10) - 1);
    public decimal EbN0 => EsN0 - 10m * (decimal)Math.Log10((double)SpectralEfficiency);
    public decimal TheoryDiff => EsN0 - EsN0Theory;
    public decimal DvbS2Diff => EsN0 - DvbS2Threshold;

    partial void OnEsPlusN0Changed(decimal value)
    {
        OnPropertyChanged(nameof(EsN0));
        OnPropertyChanged(nameof(EbN0));
        OnPropertyChanged(nameof(TheoryDiff));
        OnPropertyChanged(nameof(DvbS2Diff));
    }
}

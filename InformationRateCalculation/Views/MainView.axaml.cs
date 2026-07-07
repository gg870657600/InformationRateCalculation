using Avalonia.Controls;
using Avalonia.Data.Converters;
using InformationRateCalculation.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Globalization;

namespace InformationRateCalculation.Views;

public partial class MainView : UserControl
{
    MainViewModel vm = new MainViewModel();
    public MainView()
    {
        InitializeComponent();
        DataContext = vm;

    }
}


using Avalonia.Controls;
using Avalonia.Interactivity;
using ScottPlot.Avalonia;

namespace ScottPlotQuickStartApp.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        double[] dataX = { 1, 2, 3, 4, 5 };
        double[] dataY = { 1, 4, 9, 16, 25 };

        AvaPlot1.Plot.Add.Scatter(dataX, dataY);
        AvaPlot1.Refresh();
    }
}

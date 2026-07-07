using System.Windows;
using ChequeMate.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ChequeMate.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainViewModel>();

        // Navigate to template editor by default
        var vm = (MainViewModel)DataContext;
        vm.NavigateToTemplatesCommand.Execute(null);
    }
}

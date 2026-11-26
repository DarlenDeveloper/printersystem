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

        // The client workflow starts with choosing a supported bank.
        var vm = (MainViewModel)DataContext;
        vm.NavigateToChequeEntryCommand.Execute(null);
    }
}

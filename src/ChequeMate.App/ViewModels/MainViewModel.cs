using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChequeMate.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _currentViewName = "Templates";

    private readonly IServiceProvider _services;

    public MainViewModel(IServiceProvider services)
    {
        _services = services;
    }

    [RelayCommand]
    private void NavigateToTemplates()
    {
        CurrentView = _services.GetService(typeof(TemplateEditorViewModel));
        CurrentViewName = "Template Editor";
    }

    [RelayCommand]
    private void NavigateToChequeEntry()
    {
        CurrentView = _services.GetService(typeof(ChequeEntryViewModel));
        CurrentViewName = "New Cheque";
    }

    [RelayCommand]
    private void NavigateToPrintQueue()
    {
        CurrentView = _services.GetService(typeof(PrintQueueViewModel));
        CurrentViewName = "Print Queue";
    }
}

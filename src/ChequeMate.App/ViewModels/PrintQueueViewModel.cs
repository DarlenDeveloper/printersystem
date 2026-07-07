using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChequeMate.Core.Models;
using ChequeMate.Core.Services;

namespace ChequeMate.App.ViewModels;

public partial class PrintQueueViewModel : ObservableObject
{
    private readonly IChequeService _chequeService;
    private readonly IPrintService _printService;

    [ObservableProperty] private ObservableCollection<ChequeEntry> _pendingCheques = new();
    [ObservableProperty] private ChequeEntry? _selectedCheque;
    [ObservableProperty] private ObservableCollection<string> _printers = new();
    [ObservableProperty] private string? _selectedPrinter;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isPrinting;

    public PrintQueueViewModel(IChequeService chequeService, IPrintService printService)
    {
        _chequeService = chequeService;
        _printService = printService;
        LoadData();
    }

    private void LoadData()
    {
        _ = RefreshQueueAsync();
        var printerList = _printService.GetAvailablePrinters();
        Printers = new ObservableCollection<string>(printerList);
        if (Printers.Count > 0)
            SelectedPrinter = Printers[0];
    }

    [RelayCommand]
    private async Task RefreshQueueAsync()
    {
        var pending = await _chequeService.GetPendingEntriesAsync();
        PendingCheques = new ObservableCollection<ChequeEntry>(pending);
        StatusMessage = $"{pending.Count} cheque(s) in queue.";
    }

    [RelayCommand]
    private async Task PrintSelected()
    {
        if (SelectedCheque == null)
        {
            StatusMessage = "Select a cheque to print.";
            return;
        }

        IsPrinting = true;
        try
        {
            var result = await _printService.PrintChequeAsync(SelectedCheque, SelectedPrinter);
            StatusMessage = result.Success
                ? $"Printed cheque for {SelectedCheque.Payee} successfully."
                : $"Print failed: {result.ErrorMessage}";
            await RefreshQueueAsync();
        }
        finally
        {
            IsPrinting = false;
        }
    }

    [RelayCommand]
    private async Task PrintAll()
    {
        if (PendingCheques.Count == 0)
        {
            StatusMessage = "No cheques in queue.";
            return;
        }

        IsPrinting = true;
        try
        {
            var results = await _printService.PrintBatchAsync(
                PendingCheques.ToList(), SelectedPrinter);
            var successCount = results.Count(r => r.Success);
            StatusMessage = $"Printed {successCount}/{results.Count} cheques.";
            await RefreshQueueAsync();
        }
        finally
        {
            IsPrinting = false;
        }
    }

    [RelayCommand]
    private async Task RemoveFromQueue()
    {
        if (SelectedCheque == null) return;
        await _chequeService.DeleteEntryAsync(SelectedCheque.Id);
        await RefreshQueueAsync();
    }
}

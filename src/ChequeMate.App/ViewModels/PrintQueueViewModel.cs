using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChequeMate.Core.Enums;
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
    [ObservableProperty] private ObservableCollection<string> _paperSources = new();
    [ObservableProperty] private string? _selectedPaperSource;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isPrinting;

    public Func<string, bool>? ConfirmAction { get; set; }

    public PrintQueueViewModel(IChequeService chequeService, IPrintService printService)
    {
        _chequeService = chequeService;
        _printService = printService;

        Printers = new ObservableCollection<string>(_printService.GetAvailablePrinters());
        SelectedPrinter = Printers.FirstOrDefault();
        _ = RefreshQueueAsync();
    }

    partial void OnSelectedPrinterChanged(string? value)
    {
        var sources = string.IsNullOrWhiteSpace(value)
            ? new List<string>()
            : _printService.GetAvailablePaperSources(value);
        PaperSources = new ObservableCollection<string>(sources);
        SelectedPaperSource = FindPreferredPaperSource(sources);
    }

    [RelayCommand]
    private async Task RefreshQueueAsync()
    {
        try
        {
            var selectedId = SelectedCheque?.Id;
            var pending = await _chequeService.GetPendingEntriesAsync();
            PendingCheques = new ObservableCollection<ChequeEntry>(pending);
            SelectedCheque = selectedId.HasValue
                ? PendingCheques.FirstOrDefault(entry => entry.Id == selectedId.Value)
                : PendingCheques.FirstOrDefault();
            StatusMessage = $"{pending.Count} cheque(s) require attention.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to refresh the queue: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task PrintSelected()
    {
        if (SelectedCheque == null)
        {
            StatusMessage = "Select a cheque to print.";
            return;
        }

        await SubmitChequeAsync(SelectedCheque);
    }

    [RelayCommand]
    private async Task PrintNext()
    {
        var next = PendingCheques.FirstOrDefault(entry =>
            entry.PrintStatus is PrintStatus.Pending or PrintStatus.Failed);
        if (next == null)
        {
            StatusMessage = "There are no pending cheques to submit.";
            return;
        }

        SelectedCheque = next;
        await SubmitChequeAsync(next);
    }

    [RelayCommand]
    private async Task ConfirmPrinted()
    {
        if (SelectedCheque?.PrintStatus != PrintStatus.Submitted)
        {
            StatusMessage = "Select a submitted cheque that you have inspected.";
            return;
        }

        if (ConfirmAction?.Invoke(
                $"Confirm that cheque {DescribeCheque(SelectedCheque)} printed correctly and was removed from the printer?") != true)
            return;

        try
        {
            await _chequeService.ConfirmPrintedAsync(SelectedCheque.Id);
            StatusMessage = $"Cheque {DescribeCheque(SelectedCheque)} confirmed as printed.";
            await RefreshQueueAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not confirm the cheque: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task Requeue()
    {
        if (SelectedCheque == null || SelectedCheque.PrintStatus == PrintStatus.Printed)
        {
            StatusMessage = "Select an unconfirmed cheque to requeue.";
            return;
        }

        if (SelectedCheque.PrintStatus is PrintStatus.Submitted or PrintStatus.Printing &&
            ConfirmAction?.Invoke(
                "This job may already have reached the printer. Requeueing it can produce a duplicate cheque. Continue only if you verified that it did not print.") != true)
            return;

        try
        {
            await _chequeService.RequeueAsync(SelectedCheque.Id);
            StatusMessage = "Cheque returned to pending status.";
            await RefreshQueueAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not requeue the cheque: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RemoveFromQueue()
    {
        if (SelectedCheque == null) return;
        if (ConfirmAction?.Invoke(
                $"Permanently remove cheque {DescribeCheque(SelectedCheque)} from the queue?") != true)
            return;

        try
        {
            await _chequeService.DeleteEntryAsync(SelectedCheque.Id);
            await RefreshQueueAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not remove the cheque: {ex.Message}";
        }
    }

    private async Task SubmitChequeAsync(ChequeEntry cheque)
    {
        if (cheque.PrintStatus is not (PrintStatus.Pending or PrintStatus.Failed))
        {
            StatusMessage = cheque.PrintStatus == PrintStatus.Submitted
                ? "This job is already submitted. Confirm the physical output or explicitly requeue it."
                : $"A cheque with status {cheque.PrintStatus} cannot be submitted.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedPrinter) || string.IsNullOrWhiteSpace(SelectedPaperSource))
        {
            StatusMessage = "Select a printer and its rear/manual paper source.";
            return;
        }

        var template = cheque.Template;
        var size = template == null
            ? "the configured size"
            : $"{template.PaperWidthMm:0.#} x {template.PaperHeightMm:0.#} mm";
        var prompt =
            $"Load exactly one blank cheque ({size}) into '{SelectedPaperSource}' on '{SelectedPrinter}'.\n\n" +
            $"Ready to submit cheque {DescribeCheque(cheque)}?";
        if (ConfirmAction?.Invoke(prompt) != true)
            return;

        IsPrinting = true;
        try
        {
            var result = await _printService.PrintChequeAsync(
                cheque,
                SelectedPrinter,
                SelectedPaperSource);
            StatusMessage = result.Success
                ? $"Job submitted for {DescribeCheque(cheque)}. Inspect it, then click Confirm Printed."
                : $"Print submission failed: {result.ErrorMessage}";
            await RefreshQueueAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Print submission failed: {ex.Message}";
        }
        finally
        {
            IsPrinting = false;
        }
    }

    private static string DescribeCheque(ChequeEntry cheque) =>
        string.IsNullOrWhiteSpace(cheque.ChequeNumber)
            ? $"for {cheque.Payee}"
            : $"#{cheque.ChequeNumber} for {cheque.Payee}";

    private static string? FindPreferredPaperSource(IEnumerable<string> sources) =>
        sources.FirstOrDefault(source =>
            source.Contains("rear", StringComparison.OrdinalIgnoreCase) ||
            source.Contains("manual", StringComparison.OrdinalIgnoreCase) ||
            source.Contains("bypass", StringComparison.OrdinalIgnoreCase));
}

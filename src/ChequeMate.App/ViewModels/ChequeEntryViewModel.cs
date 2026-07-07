using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChequeMate.Core.Models;
using ChequeMate.Core.Services;

namespace ChequeMate.App.ViewModels;

public partial class ChequeEntryViewModel : ObservableObject
{
    private readonly IChequeService _chequeService;
    private readonly ITemplateService _templateService;
    private readonly IAmountToWordsService _amountToWords;

    [ObservableProperty] private ObservableCollection<ChequeTemplate> _templates = new();
    [ObservableProperty] private ChequeTemplate? _selectedTemplate;
    [ObservableProperty] private string _payee = string.Empty;
    [ObservableProperty] private decimal _amountFigures;
    [ObservableProperty] private string _amountWords = string.Empty;
    [ObservableProperty] private DateTime _chequeDate = DateTime.Today;
    [ObservableProperty] private string _chequeNumber = string.Empty;
    [ObservableProperty] private string _memo = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ChequeEntryViewModel(
        IChequeService chequeService,
        ITemplateService templateService,
        IAmountToWordsService amountToWords)
    {
        _chequeService = chequeService;
        _templateService = templateService;
        _amountToWords = amountToWords;
        _ = LoadTemplatesAsync();
    }

    partial void OnAmountFiguresChanged(decimal value)
    {
        AmountWords = _amountToWords.Convert(value);
    }

    private async Task LoadTemplatesAsync()
    {
        var templates = await _templateService.GetAllTemplatesAsync();
        Templates = new ObservableCollection<ChequeTemplate>(templates);
    }

    [RelayCommand]
    private async Task AddToQueue()
    {
        if (SelectedTemplate == null)
        {
            StatusMessage = "Please select a template.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Payee))
        {
            StatusMessage = "Please enter a payee name.";
            return;
        }

        if (AmountFigures <= 0)
        {
            StatusMessage = "Please enter a valid amount.";
            return;
        }

        var entry = new ChequeEntry
        {
            TemplateId = SelectedTemplate.Id,
            Payee = Payee,
            AmountFigures = AmountFigures,
            AmountWords = AmountWords,
            ChequeDate = ChequeDate,
            ChequeNumber = string.IsNullOrWhiteSpace(ChequeNumber) ? null : ChequeNumber,
            Memo = string.IsNullOrWhiteSpace(Memo) ? null : Memo
        };

        await _chequeService.CreateEntryAsync(entry);
        StatusMessage = $"Cheque for {Payee} added to queue.";

        // Reset form
        Payee = string.Empty;
        AmountFigures = 0;
        ChequeNumber = string.Empty;
        Memo = string.Empty;
    }

    [RelayCommand]
    private async Task ImportCsv()
    {
        if (SelectedTemplate == null)
        {
            StatusMessage = "Please select a template first.";
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "CSV files|*.csv",
            Title = "Import cheques from CSV"
        };

        if (dialog.ShowDialog() == true)
        {
            var entries = await _chequeService.ImportFromCsvAsync(dialog.FileName, SelectedTemplate.Id);
            StatusMessage = $"Imported {entries.Count} cheques from CSV.";
        }
    }
}

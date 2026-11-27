using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChequeMate.Core.Enums;
using ChequeMate.Core.Models;
using ChequeMate.Core.Services;

namespace ChequeMate.App.ViewModels;

public partial class ChequeEntryViewModel : ObservableObject
{
    private readonly IChequeService _chequeService;
    private readonly ITemplateService _templateService;
    private readonly IBankProfileService _bankService;
    private readonly IAmountToWordsService _amountToWords;

    [ObservableProperty] private ObservableCollection<BankProfile> _banks = new();
    [ObservableProperty] private BankProfile? _selectedBank;
    [ObservableProperty] private ObservableCollection<ChequeTemplate> _templates = new();
    [ObservableProperty] private ChequeTemplate? _selectedTemplate;
    [ObservableProperty] private string _payee = string.Empty;
    [ObservableProperty] private decimal _amountFigures;
    [ObservableProperty] private string _amountWords = string.Empty;
    [ObservableProperty] private string _currencyDescription = "Choose a cheque type.";
    [ObservableProperty] private DateTime _chequeDate = DateTime.Today;
    [ObservableProperty] private string _chequeNumber = string.Empty;
    [ObservableProperty] private string _memo = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _templateStatusMessage = string.Empty;

    public ChequeEntryViewModel(
        IChequeService chequeService,
        ITemplateService templateService,
        IBankProfileService bankService,
        IAmountToWordsService amountToWords)
    {
        _chequeService = chequeService;
        _templateService = templateService;
        _bankService = bankService;
        _amountToWords = amountToWords;
        UpdateAmountWords();
        _ = LoadBanksAsync();
    }

    partial void OnAmountFiguresChanged(decimal value)
    {
        UpdateAmountWords();
    }

    partial void OnSelectedBankChanged(BankProfile? value) =>
        _ = LoadTemplatesForBankAsync(value?.Id);

    partial void OnSelectedTemplateChanged(ChequeTemplate? value)
    {
        CurrencyDescription = value == null
            ? "Choose a cheque type."
            : $"{value.CurrencyDescription} — currency wording is preprinted on this leaf";
        TemplateStatusMessage = value == null
            ? "Choose a cheque type."
            : value.VerificationStatus == TemplateVerificationStatus.Verified
                ? "VERIFIED: this layout has been approved for live use."
                : $"TEST ONLY: specimen {value.SpecimenReference ?? "reference unavailable"}; " +
                  "use plain paper or a VOID leaf. Do not use a live cheque.";
        UpdateAmountWords();
    }

    private void UpdateAmountWords()
    {
        var template = SelectedTemplate;
        AmountWords = _amountToWords.Convert(
            AmountFigures,
            template is { IncludeCurrencyInWords: true } ? template.CurrencyName : string.Empty,
            template?.FractionalUnitName ?? "Cents");
    }

    private async Task LoadBanksAsync()
    {
        try
        {
            var banks = await _bankService.GetAllAsync();
            Banks = new ObservableCollection<BankProfile>(banks);
            SelectedBank = Banks.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to load supported banks: {ex.Message}";
        }
    }

    private async Task LoadTemplatesForBankAsync(int? bankId)
    {
        SelectedTemplate = null;
        Templates.Clear();
        if (!bankId.HasValue)
            return;

        try
        {
            var templates = await _templateService.GetTemplatesByBankAsync(bankId.Value);
            Templates = new ObservableCollection<ChequeTemplate>(
                templates.OrderBy(template => template.PaperWidthMm)
                    .ThenBy(template => template.PaperHeightMm));
            SelectedTemplate = Templates.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to load cheque types: {ex.Message}";
        }
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

        if (!string.IsNullOrWhiteSpace(ChequeNumber))
        {
            var existing = await _chequeService.GetAllEntriesAsync();
            if (existing.Any(entry => string.Equals(
                    entry.ChequeNumber,
                    ChequeNumber.Trim(),
                    StringComparison.OrdinalIgnoreCase)))
            {
                StatusMessage = $"Cheque number '{ChequeNumber.Trim()}' already exists.";
                return;
            }
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

        try
        {
            await _chequeService.CreateEntryAsync(entry);
            StatusMessage = $"Cheque for {Payee} added to queue.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to add cheque: {ex.Message}";
            return;
        }

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
            try
            {
                var entries = await _chequeService.ImportFromCsvAsync(
                    dialog.FileName,
                    SelectedTemplate.Id);
                StatusMessage = $"Imported {entries.Count} cheques from CSV.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"CSV import failed: {ex.Message}";
            }
        }
    }
}

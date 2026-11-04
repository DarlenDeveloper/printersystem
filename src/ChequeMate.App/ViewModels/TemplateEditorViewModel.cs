using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChequeMate.Core.Enums;
using ChequeMate.Core.Models;
using ChequeMate.Core.Services;
using Microsoft.Win32;

namespace ChequeMate.App.ViewModels;

public partial class TemplateEditorViewModel : ObservableObject
{
    private readonly ITemplateService _templateService;
    private readonly IBankProfileService _bankService;
    private readonly IPrintService _printService;

    [ObservableProperty] private ObservableCollection<ChequeTemplate> _templates = new();
    [ObservableProperty] private ObservableCollection<BankProfile> _banks = new();
    [ObservableProperty] private ChequeTemplate? _selectedTemplate;
    [ObservableProperty] private BitmapImage? _chequeImage;
    [ObservableProperty] private string _templateName = string.Empty;
    [ObservableProperty] private double _paperWidthMm = 170;
    [ObservableProperty] private double _paperHeightMm = 70;
    [ObservableProperty] private double _offsetXMm;
    [ObservableProperty] private double _offsetYMm;
    [ObservableProperty] private int? _selectedBankId;
    [ObservableProperty] private string _newBankName = string.Empty;
    [ObservableProperty] private ObservableCollection<TemplateFieldViewModel> _fields = new();
    [ObservableProperty] private TemplateFieldViewModel? _selectedField;
    [ObservableProperty] private ObservableCollection<string> _printers = new();
    [ObservableProperty] private string? _selectedPrinter;
    [ObservableProperty] private ObservableCollection<string> _paperSources = new();
    [ObservableProperty] private string? _selectedPaperSource;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;

    public Func<FieldType?>? RequestFieldType { get; set; }
    public Func<string, bool>? ConfirmAction { get; set; }

    public TemplateEditorViewModel(
        ITemplateService templateService,
        IBankProfileService bankService,
        IPrintService printService)
    {
        _templateService = templateService;
        _bankService = bankService;
        _printService = printService;

        Printers = new ObservableCollection<string>(_printService.GetAvailablePrinters());
        SelectedPrinter = Printers.FirstOrDefault();
        _ = LoadDataAsync();
    }

    partial void OnSelectedFieldChanged(TemplateFieldViewModel? oldValue, TemplateFieldViewModel? newValue)
    {
        if (oldValue != null) oldValue.IsSelected = false;
        if (newValue != null) newValue.IsSelected = true;
    }

    partial void OnSelectedPrinterChanged(string? value)
    {
        var sources = string.IsNullOrWhiteSpace(value)
            ? new List<string>()
            : _printService.GetAvailablePaperSources(value);
        PaperSources = new ObservableCollection<string>(sources);
        SelectedPaperSource = FindPreferredPaperSource(sources);
    }

    private async Task LoadDataAsync(int? selectTemplateId = null)
    {
        try
        {
            var templates = await _templateService.GetAllTemplatesAsync();
            Templates = new ObservableCollection<ChequeTemplate>(templates);

            var banks = await _bankService.GetAllAsync();
            Banks = new ObservableCollection<BankProfile>(banks);

            if (selectTemplateId.HasValue)
            {
                SelectedTemplate = Templates.FirstOrDefault(t => t.Id == selectTemplateId.Value);
                LoadTemplate();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to load template data: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ImportChequeImage()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.tiff;*.tif;*.bmp",
            Title = "Select scanned cheque image"
        };

        if (dialog.ShowDialog() != true)
            return;

        var bitmap = LoadBitmap(dialog.FileName);
        ChequeImage = bitmap;

        if (bitmap.DpiX > 0 && bitmap.DpiY > 0)
        {
            PaperWidthMm = Math.Round(bitmap.PixelWidth / bitmap.DpiX * 25.4, 1);
            PaperHeightMm = Math.Round(bitmap.PixelHeight / bitmap.DpiY * 25.4, 1);
        }

        StatusMessage = $"Image loaded: {bitmap.PixelWidth}x{bitmap.PixelHeight} @ {bitmap.DpiX:0.#} DPI";
    }

    [RelayCommand]
    private async Task AddBank()
    {
        var name = NewBankName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "Enter a bank name first.";
            return;
        }

        var existing = Banks.FirstOrDefault(b =>
            string.Equals(b.BankName, name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            SelectedBankId = existing.Id;
            StatusMessage = "That bank already exists and has been selected.";
            return;
        }

        try
        {
            var bank = await _bankService.CreateAsync(new BankProfile { BankName = name });
            NewBankName = string.Empty;
            await LoadDataAsync();
            SelectedBankId = bank.Id;
            StatusMessage = $"Bank '{bank.BankName}' added.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to add bank: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddField()
    {
        var fieldType = RequestFieldType?.Invoke();
        if (fieldType == null) return;

        var field = new TemplateFieldViewModel
        {
            FieldType = fieldType.Value,
            Label = fieldType.Value.ToString(),
            PositionXMm = 20,
            PositionYMm = 20,
            WidthMm = DefaultWidthForField(fieldType.Value),
            HeightMm = 8,
            FontFamily = "Arial",
            FontSizePt = 10
        };
        Fields.Add(field);
        SelectedField = field;
    }

    [RelayCommand]
    private void RemoveField()
    {
        if (SelectedField == null) return;
        Fields.Remove(SelectedField);
        SelectedField = null;
    }

    [RelayCommand]
    private void NewTemplate()
    {
        SelectedTemplate = null;
        TemplateName = string.Empty;
        PaperWidthMm = 170;
        PaperHeightMm = 70;
        OffsetXMm = 0;
        OffsetYMm = 0;
        SelectedBankId = null;
        ChequeImage = null;
        Fields.Clear();
        SelectedField = null;
        StatusMessage = "New template started.";
    }

    [RelayCommand]
    private void LoadTemplate()
    {
        if (SelectedTemplate == null) return;

        TemplateName = SelectedTemplate.TemplateName;
        PaperWidthMm = SelectedTemplate.PaperWidthMm;
        PaperHeightMm = SelectedTemplate.PaperHeightMm;
        OffsetXMm = SelectedTemplate.OffsetXMm;
        OffsetYMm = SelectedTemplate.OffsetYMm;
        SelectedBankId = SelectedTemplate.BankProfileId;
        ChequeImage = !string.IsNullOrEmpty(SelectedTemplate.ChequeImagePath) &&
                      File.Exists(SelectedTemplate.ChequeImagePath)
            ? LoadBitmap(SelectedTemplate.ChequeImagePath)
            : null;

        Fields = new ObservableCollection<TemplateFieldViewModel>(
            SelectedTemplate.Fields.Select(ToFieldViewModel));
        SelectedField = null;
        StatusMessage = $"Loaded template: {TemplateName}";
    }

    [RelayCommand]
    private async Task SaveTemplate()
    {
        var validationError = ValidateEditor();
        if (validationError != null)
        {
            StatusMessage = validationError;
            return;
        }

        try
        {
            IsBusy = true;
            var template = BuildTemplateSnapshot();
            if (template.Id == 0)
                await _templateService.CreateTemplateAsync(template);
            else
                await _templateService.UpdateTemplateAsync(template);

            await LoadDataAsync(template.Id);
            StatusMessage = "Template saved successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving template: {GetInnermostMessage(ex)}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteTemplate()
    {
        if (SelectedTemplate == null)
        {
            StatusMessage = "Select a saved template to delete.";
            return;
        }

        if (ConfirmAction?.Invoke(
                $"Delete template '{SelectedTemplate.TemplateName}'? This cannot be undone.") != true)
            return;

        try
        {
            await _templateService.DeleteTemplateAsync(SelectedTemplate.Id);
            NewTemplate();
            await LoadDataAsync();
            StatusMessage = "Template deleted.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Template could not be deleted: {GetInnermostMessage(ex)}";
        }
    }

    [RelayCommand]
    private async Task PrintAlignmentTest()
    {
        var validationError = ValidateEditor();
        if (validationError != null)
        {
            StatusMessage = validationError;
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedPrinter) || string.IsNullOrWhiteSpace(SelectedPaperSource))
        {
            StatusMessage = "Select a printer and its rear/manual paper source.";
            return;
        }

        var prompt =
            $"Load one plain sheet cut to {PaperWidthMm:0.#} x {PaperHeightMm:0.#} mm into " +
            $"'{SelectedPaperSource}' on '{SelectedPrinter}'. Print the alignment grid now?";
        if (ConfirmAction?.Invoke(prompt) != true)
            return;

        try
        {
            IsBusy = true;
            await _printService.PrintAlignmentTestAsync(
                BuildTemplateSnapshot(),
                SelectedPrinter,
                SelectedPaperSource);
            StatusMessage = "Alignment job submitted. Compare the output with the cheque before continuing.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Alignment print failed: {GetInnermostMessage(ex)}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private ChequeTemplate BuildTemplateSnapshot()
    {
        var template = new ChequeTemplate
        {
            Id = SelectedTemplate?.Id ?? 0,
            TemplateName = TemplateName.Trim(),
            BankProfileId = SelectedBankId,
            ChequeImagePath = ChequeImage?.UriSource is { IsFile: true } uri ? uri.LocalPath : null,
            PaperWidthMm = PaperWidthMm,
            PaperHeightMm = PaperHeightMm,
            OffsetXMm = OffsetXMm,
            OffsetYMm = OffsetYMm,
            CreatedAt = SelectedTemplate?.CreatedAt ?? DateTime.UtcNow
        };

        foreach (var field in Fields)
        {
            template.Fields.Add(new TemplateField
            {
                FieldType = field.FieldType,
                Label = field.Label,
                PositionXMm = field.PositionXMm,
                PositionYMm = field.PositionYMm,
                WidthMm = field.WidthMm,
                HeightMm = field.HeightMm,
                FontFamily = field.FontFamily,
                FontSizePt = field.FontSizePt,
                Alignment = field.Alignment,
                IsBold = field.IsBold
            });
        }

        return template;
    }

    private string? ValidateEditor()
    {
        if (string.IsNullOrWhiteSpace(TemplateName))
            return "Please enter a template name.";
        if (!SelectedBankId.HasValue)
            return "Please add or select a bank.";
        if (!double.IsFinite(PaperWidthMm) || !double.IsFinite(PaperHeightMm) ||
            PaperWidthMm <= 0 || PaperHeightMm <= 0)
            return "Paper width and height must be positive numbers.";
        if (Fields.Count == 0)
            return "Add at least one printable field.";

        var invalidField = Fields.FirstOrDefault(field =>
            field.PositionXMm < 0 || field.PositionYMm < 0 ||
            field.WidthMm <= 0 || field.HeightMm <= 0 ||
            field.PositionXMm + field.WidthMm > PaperWidthMm ||
            field.PositionYMm + field.HeightMm > PaperHeightMm);
        return invalidField == null
            ? null
            : $"Field '{invalidField.DisplayName}' lies outside the cheque boundaries.";
    }

    private static BitmapImage LoadBitmap(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(path);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private static TemplateFieldViewModel ToFieldViewModel(TemplateField field) => new()
    {
        FieldType = field.FieldType,
        Label = field.Label,
        PositionXMm = field.PositionXMm,
        PositionYMm = field.PositionYMm,
        WidthMm = field.WidthMm,
        HeightMm = field.HeightMm,
        FontFamily = field.FontFamily,
        FontSizePt = field.FontSizePt,
        Alignment = field.Alignment,
        IsBold = field.IsBold
    };

    private static double DefaultWidthForField(FieldType type) => type switch
    {
        FieldType.AmountInWords => 120,
        FieldType.AmountInFigures => 50,
        FieldType.Date => 40,
        FieldType.ChequeNumber => 40,
        FieldType.AccountPayee => 60,
        _ => 80
    };

    private static string? FindPreferredPaperSource(IEnumerable<string> sources) =>
        sources.FirstOrDefault(source =>
            source.Contains("rear", StringComparison.OrdinalIgnoreCase) ||
            source.Contains("manual", StringComparison.OrdinalIgnoreCase) ||
            source.Contains("bypass", StringComparison.OrdinalIgnoreCase));

    private static string GetInnermostMessage(Exception exception)
    {
        while (exception.InnerException != null)
            exception = exception.InnerException;
        return exception.Message;
    }
}

public partial class TemplateFieldViewModel : ObservableObject
{
    [ObservableProperty] private FieldType _fieldType;
    [ObservableProperty] private string? _label;
    [ObservableProperty] private double _positionXMm;
    [ObservableProperty] private double _positionYMm;
    [ObservableProperty] private double _widthMm;
    [ObservableProperty] private double _heightMm;
    [ObservableProperty] private string _fontFamily = "Arial";
    [ObservableProperty] private double _fontSizePt = 10;
    [ObservableProperty] private TextAlignment _alignment = TextAlignment.Left;
    [ObservableProperty] private bool _isBold;
    [ObservableProperty] private bool _isSelected;

    public string DisplayName => Label ?? FieldType.ToString();
}

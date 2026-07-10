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
    [ObservableProperty] private ObservableCollection<TemplateFieldViewModel> _fields = new();
    [ObservableProperty] private TemplateFieldViewModel? _selectedField;
    [ObservableProperty] private string _statusMessage = string.Empty;

    partial void OnSelectedFieldChanged(TemplateFieldViewModel? oldValue, TemplateFieldViewModel? newValue)
    {
        if (oldValue != null) oldValue.IsSelected = false;
        if (newValue != null) newValue.IsSelected = true;
    }

    public TemplateEditorViewModel(ITemplateService templateService, IBankProfileService bankService)
    {
        _templateService = templateService;
        _bankService = bankService;
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var templates = await _templateService.GetAllTemplatesAsync();
        Templates = new ObservableCollection<ChequeTemplate>(templates);

        var banks = await _bankService.GetAllAsync();
        Banks = new ObservableCollection<BankProfile>(banks);
    }

    [RelayCommand]
    private void ImportChequeImage()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.tiff;*.tif;*.bmp",
            Title = "Select scanned cheque image"
        };

        if (dialog.ShowDialog() == true)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(dialog.FileName);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            ChequeImage = bitmap;

            // Try to extract DPI info for calibration
            if (bitmap.DpiX > 0 && bitmap.DpiY > 0)
            {
                // Auto-calculate paper dimensions from image size and DPI
                double imageWidthInches = bitmap.PixelWidth / bitmap.DpiX;
                double imageHeightInches = bitmap.PixelHeight / bitmap.DpiY;
                PaperWidthMm = Math.Round(imageWidthInches * 25.4, 1);
                PaperHeightMm = Math.Round(imageHeightInches * 25.4, 1);
            }

            StatusMessage = $"Image loaded: {bitmap.PixelWidth}x{bitmap.PixelHeight} @ {bitmap.DpiX} DPI";
        }
    }

    /// <summary>
    /// Set by the View so the ViewModel can request a field type from the user
    /// without taking a hard dependency on WPF Window classes.
    /// </summary>
    public Func<FieldType?>? RequestFieldType { get; set; }

    [RelayCommand]
    private void AddField()
    {
        var fieldType = RequestFieldType?.Invoke();
        if (fieldType == null) return; // user cancelled

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

    private static double DefaultWidthForField(FieldType type) => type switch
    {
        FieldType.AmountInWords => 120,
        FieldType.AmountInFigures => 50,
        FieldType.Date => 40,
        FieldType.ChequeNumber => 40,
        FieldType.AccountPayee => 60,
        _ => 80
    };

    [RelayCommand]
    private void RemoveField()
    {
        if (SelectedField != null)
        {
            Fields.Remove(SelectedField);
            SelectedField = null;
        }
    }

    [RelayCommand]
    private async Task SaveTemplate()
    {
        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            StatusMessage = "Please enter a template name.";
            return;
        }

        try
        {
            var template = SelectedTemplate ?? new ChequeTemplate();
            template.TemplateName = TemplateName;
            template.BankProfileId = SelectedBankId;
            template.PaperWidthMm = PaperWidthMm;
            template.PaperHeightMm = PaperHeightMm;
            template.OffsetXMm = OffsetXMm;
            template.OffsetYMm = OffsetYMm;

            // Safely resolve the image path whether it came from a Uri or a local file dialog
            if (ChequeImage?.UriSource != null)
                template.ChequeImagePath = ChequeImage.UriSource.IsFile
                    ? ChequeImage.UriSource.LocalPath
                    : ChequeImage.UriSource.AbsolutePath;

            template.Fields.Clear();
            foreach (var fieldVm in Fields)
            {
                template.Fields.Add(new TemplateField
                {
                    FieldType = fieldVm.FieldType,
                    Label = fieldVm.Label,
                    PositionXMm = fieldVm.PositionXMm,
                    PositionYMm = fieldVm.PositionYMm,
                    WidthMm = fieldVm.WidthMm,
                    HeightMm = fieldVm.HeightMm,
                    FontFamily = fieldVm.FontFamily,
                    FontSizePt = fieldVm.FontSizePt,
                    Alignment = fieldVm.Alignment,
                    IsBold = fieldVm.IsBold
                });
            }

            if (template.Id == 0)
                await _templateService.CreateTemplateAsync(template);
            else
                await _templateService.UpdateTemplateAsync(template);

            StatusMessage = "Template saved successfully!";
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.InnerException?.Message
                        ?? ex.InnerException?.Message
                        ?? ex.Message;
            StatusMessage = $"Error saving template: {inner}";
        }
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

        if (!string.IsNullOrEmpty(SelectedTemplate.ChequeImagePath) && File.Exists(SelectedTemplate.ChequeImagePath))
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(SelectedTemplate.ChequeImagePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            ChequeImage = bitmap;
        }

        Fields.Clear();
        foreach (var field in SelectedTemplate.Fields)
        {
            Fields.Add(new TemplateFieldViewModel
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

        StatusMessage = $"Loaded template: {TemplateName}";
    }
}

/// <summary>
/// ViewModel for a single draggable field on the template canvas.
/// </summary>
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

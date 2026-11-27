using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChequeMate.Core.Enums;
using ChequeMate.Core.Models;
using ChequeMate.Core.Services;
using ChequeMate.Infrastructure.Printing;

namespace ChequeMate.App.ViewModels;

public sealed record ChoiceOption<T>(T Value, string Label);

public partial class PrintQueueViewModel : ObservableObject
{
    private const string DefaultPaperSource = "(Use printer default)";

    private readonly IChequeService _chequeService;
    private readonly IPrintService _printService;
    private readonly IPrinterProfileService _profileService;
    private bool _loadingProfile;

    [ObservableProperty] private ObservableCollection<ChequeEntry> _pendingCheques = new();
    [ObservableProperty] private ChequeEntry? _selectedCheque;
    [ObservableProperty] private ObservableCollection<string> _printers = new();
    [ObservableProperty] private string? _selectedPrinter;
    [ObservableProperty] private ObservableCollection<string> _paperSources = new();
    [ObservableProperty] private string? _selectedPaperSource;
    [ObservableProperty] private ChoiceOption<PrintMode>? _selectedPrintMode;
    [ObservableProperty] private ChoiceOption<FeedOrientation>? _selectedFeedOrientation;
    [ObservableProperty] private ChoiceOption<PaperAlignment>? _selectedPaperAlignment;
    [ObservableProperty] private double _offsetXMm;
    [ObservableProperty] private double _offsetYMm;
    [ObservableProperty] private string _loadingGuide = "Select a queued cheque and printer to see the loading guide.";
    [ObservableProperty] private string _setupStatusMessage = "Setup is saved separately for each printer and cheque size.";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isPrinting;

    public ObservableCollection<ChoiceOption<PrintMode>> PrintModes { get; } =
    [
        new(PrintMode.A4Positioned, "A4 positioned (recommended)"),
        new(PrintMode.ExactChequeSize, "Exact cheque size (advanced)")
    ];

    public ObservableCollection<ChoiceOption<FeedOrientation>> FeedOrientations { get; } =
    [
        new(FeedOrientation.LongEdgeFirst, "Long edge first"),
        new(FeedOrientation.ShortEdgeFirst, "Short edge first / end-first")
    ];

    public ObservableCollection<ChoiceOption<PaperAlignment>> PaperAlignments { get; } =
    [
        new(PaperAlignment.Right, "Right guide"),
        new(PaperAlignment.Centre, "Centre guide"),
        new(PaperAlignment.Left, "Left guide")
    ];

    public Func<string, bool>? ConfirmAction { get; set; }
    public Action<string>? ShowErrorAction { get; set; }

    public PrintQueueViewModel(
        IChequeService chequeService,
        IPrintService printService,
        IPrinterProfileService profileService)
    {
        _chequeService = chequeService;
        _printService = printService;
        _profileService = profileService;

        SelectedPrintMode = PrintModes[0];
        SelectedFeedOrientation = FeedOrientations[0];
        SelectedPaperAlignment = PaperAlignments[0];
        Printers = new ObservableCollection<string>(_printService.GetAvailablePrinters());
        SelectedPrinter = Printers.FirstOrDefault();
        _ = RefreshQueueAsync();
    }

    partial void OnSelectedPrinterChanged(string? value)
    {
        var sources = string.IsNullOrWhiteSpace(value)
            ? new List<string>()
            : _printService.GetAvailablePaperSources(value);
        PaperSources = new ObservableCollection<string>(new[] { DefaultPaperSource }.Concat(sources));
        SelectedPaperSource = FindPreferredPaperSource(sources) ?? DefaultPaperSource;
        LoadPrinterSetup();
    }

    partial void OnSelectedChequeChanged(ChequeEntry? value) => LoadPrinterSetup();
    partial void OnSelectedPrintModeChanged(ChoiceOption<PrintMode>? value) => UpdateLoadingGuide();
    partial void OnSelectedFeedOrientationChanged(ChoiceOption<FeedOrientation>? value) => UpdateLoadingGuide();
    partial void OnSelectedPaperAlignmentChanged(ChoiceOption<PaperAlignment>? value) => UpdateLoadingGuide();

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
    private void SavePrinterSetup()
    {
        try
        {
            SaveCurrentProfile();
            SetupStatusMessage = "Printer setup saved for this cheque size.";
        }
        catch (Exception ex)
        {
            SetupStatusMessage = $"Setup could not be saved: {ex.Message}";
            ShowErrorAction?.Invoke(SetupStatusMessage);
        }
    }

    [RelayCommand]
    private void ClearPrinterSetup()
    {
        var template = SelectedCheque?.Template;
        if (template == null || string.IsNullOrWhiteSpace(SelectedPrinter))
        {
            SetupStatusMessage = "Select a printer and queued cheque first.";
            return;
        }

        if (ConfirmAction?.Invoke(
                "Clear the saved print method, feed direction, guide position and X/Y calibration for this printer and cheque size?") != true)
            return;

        _profileService.Clear(SelectedPrinter, template.PaperWidthMm, template.PaperHeightMm);
        LoadPrinterSetup();
        SetupStatusMessage = "Saved setup cleared. Recommended defaults restored.";
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
    private async Task PrintAlignmentTest()
    {
        var template = SelectedCheque?.Template;
        if (template == null)
        {
            StatusMessage = "Select a queued cheque to choose its bank layout.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedPrinter))
        {
            StatusMessage = "Select a printer.";
            return;
        }

        var options = BuildOptions();
        var paperInstruction = options.Mode == PrintMode.A4Positioned
            ? $"Load one full plain A4 sheet into {DescribeSource()} on '{SelectedPrinter}'. " +
              "The printed outline shows exactly where the cheque area will be."
            : $"Load plain test paper cut to {template.PaperWidthMm:0.#} x {template.PaperHeightMm:0.#} mm " +
              $"into {DescribeSource()} on '{SelectedPrinter}'.";
        var prompt = $"TEST ONLY. {paperInstruction}\n\n{LoadingGuide}\n\nNever use a live cheque for alignment. Continue?";
        if (ConfirmAction?.Invoke(prompt) != true)
            return;

        IsPrinting = true;
        try
        {
            SaveCurrentProfile();
            await _printService.PrintAlignmentTestAsync(
                template,
                SelectedPrinter,
                GetPaperSourceForPrint(),
                options);
            StatusMessage = options.Mode == PrintMode.A4Positioned
                ? "A4 placement grid submitted. Check the ruler and outline, then adjust X/Y in millimetres if required."
                : "Exact-size alignment grid submitted. Check the ruler and outline before printing cheque data.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Alignment print failed: {ex.Message}";
            ShowErrorAction?.Invoke(StatusMessage);
        }
        finally
        {
            IsPrinting = false;
        }
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

        if (string.IsNullOrWhiteSpace(SelectedPrinter))
        {
            StatusMessage = "Select a printer.";
            return;
        }

        var template = cheque.Template;
        if (template == null)
        {
            StatusMessage = "The selected cheque has no loaded bank template.";
            return;
        }

        var options = BuildOptions();
        var size = $"{template.PaperWidthMm:0.#} x {template.PaperHeightMm:0.#} mm";
        var isVerified = template.VerificationStatus == TemplateVerificationStatus.Verified;
        var isKnownNonImpactPrinter = SelectedPrinter.Contains("L220", StringComparison.OrdinalIgnoreCase) ||
                                      SelectedPrinter.Contains("GX70", StringComparison.OrdinalIgnoreCase) ||
                                      SelectedPrinter.Contains("GX7000", StringComparison.OrdinalIgnoreCase);
        var unsupportedLeaf = PaperSizeHelper.IsKnownDirectFeedUnsupported(
            SelectedPrinter, template.PaperWidthMm, template.PaperHeightMm);

        var stockInstruction = options.Mode == PrintMode.A4Positioned && unsupportedLeaf
            ? "Load a full plain A4 test sheet. Do not feed the cut cheque leaf into this printer"
            : isVerified
                ? "Load exactly one approved cheque using the saved guide position"
                : "TEST ONLY: load plain paper or one clearly marked VOID specimen using the saved guide position";
        var prompt =
            $"{stockInstruction} ({size}) into {DescribeSource()} on '{SelectedPrinter}'.\n\n" +
            $"{LoadingGuide}\n\n" +
            (isVerified ? string.Empty : "Do not use a live cheque with this unverified layout.\n\n") +
            (isKnownNonImpactPrinter
                ? "This is a non-impact inkjet printer. Bank approval is required before live use.\n\n"
                : string.Empty) +
            $"Ready to submit cheque {DescribeCheque(cheque)}?";
        if (ConfirmAction?.Invoke(prompt) != true)
            return;

        IsPrinting = true;
        try
        {
            SaveCurrentProfile();
            var result = await _printService.PrintChequeAsync(
                cheque,
                SelectedPrinter,
                GetPaperSourceForPrint(),
                options);
            var resultMessage = result.Success
                ? $"Job submitted for {DescribeCheque(cheque)}. Inspect it, then click Confirm Printed."
                : $"Print submission failed: {result.ErrorMessage ?? "The printer returned no details."}";
            await RefreshQueueAsync();
            StatusMessage = resultMessage;
            if (!result.Success)
                ShowErrorAction?.Invoke(resultMessage);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Print submission failed: {ex.Message}";
            ShowErrorAction?.Invoke(StatusMessage);
        }
        finally
        {
            IsPrinting = false;
        }
    }

    private void LoadPrinterSetup()
    {
        if (_loadingProfile)
            return;

        var template = SelectedCheque?.Template;
        if (template == null || string.IsNullOrWhiteSpace(SelectedPrinter))
        {
            UpdateLoadingGuide();
            return;
        }

        try
        {
            _loadingProfile = true;
            var profile = _profileService.GetOrCreate(
                SelectedPrinter, template.PaperWidthMm, template.PaperHeightMm);
            SelectedPrintMode = PrintModes.First(item => item.Value == profile.Options.Mode);
            SelectedFeedOrientation = FeedOrientations.First(item => item.Value == profile.Options.FeedOrientation);
            SelectedPaperAlignment = PaperAlignments.First(item => item.Value == profile.Options.PaperAlignment);
            OffsetXMm = profile.Options.OffsetXMm;
            OffsetYMm = profile.Options.OffsetYMm;
            SelectedPaperSource = profile.PaperSourceName != null && PaperSources.Contains(profile.PaperSourceName)
                ? profile.PaperSourceName
                : FindPreferredPaperSource(PaperSources.Where(item => item != DefaultPaperSource)) ?? DefaultPaperSource;
            SetupStatusMessage = profile.UpdatedAt == default
                ? profile.Options.OffsetXMm == -10 &&
                  SelectedPrinter.Contains("L220", StringComparison.OrdinalIgnoreCase) &&
                  Math.Abs(template.PaperWidthMm - 203) < 0.05 &&
                  Math.Abs(template.PaperHeightMm - 101.6) < 0.05
                    ? "Field-tested Epson L220 setup loaded (X -10 mm, Y 0 mm). Run Alignment Test, then save any corrections."
                    : "Recommended setup loaded. Run Alignment Test, then save any corrections."
                : $"Saved setup loaded (updated {profile.UpdatedAt.ToLocalTime():g}).";
        }
        catch (Exception ex)
        {
            SetupStatusMessage = $"Saved setup could not be loaded: {ex.Message}";
        }
        finally
        {
            _loadingProfile = false;
            UpdateLoadingGuide();
        }
    }

    private void SaveCurrentProfile()
    {
        var template = SelectedCheque?.Template
            ?? throw new InvalidOperationException("Select a queued cheque first.");
        var printer = SelectedPrinter
            ?? throw new InvalidOperationException("Select a printer first.");

        _profileService.Save(new PrinterProfile
        {
            PrinterName = printer,
            ChequeWidthMm = template.PaperWidthMm,
            ChequeHeightMm = template.PaperHeightMm,
            PaperSourceName = GetPaperSourceForPrint(),
            Options = BuildOptions()
        });
    }

    private PrintJobOptions BuildOptions() => new()
    {
        Mode = SelectedPrintMode?.Value ?? PrintMode.A4Positioned,
        FeedOrientation = SelectedFeedOrientation?.Value ?? FeedOrientation.LongEdgeFirst,
        PaperAlignment = SelectedPaperAlignment?.Value ?? PaperAlignment.Right,
        OffsetXMm = OffsetXMm,
        OffsetYMm = OffsetYMm
    };

    private void UpdateLoadingGuide()
    {
        var template = SelectedCheque?.Template;
        if (template == null || string.IsNullOrWhiteSpace(SelectedPrinter))
        {
            LoadingGuide = "Select a queued cheque and printer to see the loading guide.";
            return;
        }

        var options = BuildOptions();
        var orientation = options.FeedOrientation == FeedOrientation.LongEdgeFirst
            ? "Feed the cheque's long edge first"
            : "Feed the cheque end-first (short edge first)";
        var guide = options.PaperAlignment switch
        {
            PaperAlignment.Left => "against the left paper guide",
            PaperAlignment.Centre => "centred between the paper guides",
            _ => "against the right paper guide"
        };
        var mode = options.Mode == PrintMode.A4Positioned
            ? "A4 positioned: Windows receives a normal portrait A4 page; no custom form is required."
            : "Exact size: ChequeMate sends the dimensions directly; no named Windows form is required.";
        var support = PaperSizeHelper.DescribeMediaSupport(
            SelectedPrinter, template.PaperWidthMm, template.PaperHeightMm, options.Mode);

        LoadingGuide =
            $"{mode}\n{orientation}, {guide}, face-up. Size: " +
            $"{template.PaperWidthMm:0.#} x {template.PaperHeightMm:0.#} mm.\n{support}";
    }

    private string? GetPaperSourceForPrint() =>
        string.IsNullOrWhiteSpace(SelectedPaperSource) || SelectedPaperSource == DefaultPaperSource
            ? null
            : SelectedPaperSource;

    private string DescribeSource() => GetPaperSourceForPrint() is { } source
        ? $"the '{source}' source"
        : "the printer's default source";

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

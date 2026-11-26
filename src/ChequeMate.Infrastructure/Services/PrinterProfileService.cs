using System.Text.Json;
using ChequeMate.Core.Models;
using ChequeMate.Core.Services;

namespace ChequeMate.Infrastructure.Services;

public sealed class PrinterProfileService : IPrinterProfileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _settingsPath;
    private readonly object _sync = new();

    public PrinterProfileService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChequeMate",
            "printer-profiles.json"))
    {
    }

    public PrinterProfileService(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public PrinterProfile GetOrCreate(string printerName, double chequeWidthMm, double chequeHeightMm)
    {
        ValidateKey(printerName, chequeWidthMm, chequeHeightMm);

        lock (_sync)
        {
            var profile = ReadProfiles().FirstOrDefault(item => Matches(
                item, printerName, chequeWidthMm, chequeHeightMm));

            return profile ?? new PrinterProfile
            {
                PrinterName = printerName,
                ChequeWidthMm = chequeWidthMm,
                ChequeHeightMm = chequeHeightMm
            };
        }
    }

    public void Save(PrinterProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ValidateKey(profile.PrinterName, profile.ChequeWidthMm, profile.ChequeHeightMm);
        ArgumentNullException.ThrowIfNull(profile.Options);

        if (!double.IsFinite(profile.Options.OffsetXMm) ||
            !double.IsFinite(profile.Options.OffsetYMm) ||
            Math.Abs(profile.Options.OffsetXMm) > 25 ||
            Math.Abs(profile.Options.OffsetYMm) > 25)
        {
            throw new InvalidOperationException("Calibration offsets must be between -25 mm and 25 mm.");
        }

        lock (_sync)
        {
            var profiles = ReadProfiles();
            profiles.RemoveAll(item => Matches(
                item, profile.PrinterName, profile.ChequeWidthMm, profile.ChequeHeightMm));
            profile.UpdatedAt = DateTime.UtcNow;
            profiles.Add(profile);
            WriteProfiles(profiles);
        }
    }

    public void Clear(string printerName, double chequeWidthMm, double chequeHeightMm)
    {
        ValidateKey(printerName, chequeWidthMm, chequeHeightMm);

        lock (_sync)
        {
            var profiles = ReadProfiles();
            if (profiles.RemoveAll(item => Matches(
                    item, printerName, chequeWidthMm, chequeHeightMm)) > 0)
            {
                WriteProfiles(profiles);
            }
        }
    }

    private List<PrinterProfile> ReadProfiles()
    {
        if (!File.Exists(_settingsPath))
            return new List<PrinterProfile>();

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<List<PrinterProfile>>(json, JsonOptions)
                   ?? new List<PrinterProfile>();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            throw new InvalidDataException(
                $"Printer setup file '{_settingsPath}' could not be read. " +
                "Rename it and reopen ChequeMate to restore defaults.",
                ex);
        }
    }

    private void WriteProfiles(List<PrinterProfile> profiles)
    {
        var directory = Path.GetDirectoryName(_settingsPath)
            ?? throw new InvalidOperationException("Printer setup path has no directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = _settingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(profiles, JsonOptions));
        File.Move(temporaryPath, _settingsPath, true);
    }

    private static bool Matches(
        PrinterProfile profile,
        string printerName,
        double chequeWidthMm,
        double chequeHeightMm) =>
        string.Equals(profile.PrinterName, printerName, StringComparison.OrdinalIgnoreCase) &&
        Math.Abs(profile.ChequeWidthMm - chequeWidthMm) < 0.05 &&
        Math.Abs(profile.ChequeHeightMm - chequeHeightMm) < 0.05;

    private static void ValidateKey(string printerName, double chequeWidthMm, double chequeHeightMm)
    {
        if (string.IsNullOrWhiteSpace(printerName))
            throw new ArgumentException("Printer name is required.", nameof(printerName));
        if (!double.IsFinite(chequeWidthMm) || !double.IsFinite(chequeHeightMm) ||
            chequeWidthMm <= 0 || chequeHeightMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chequeWidthMm), "Cheque dimensions must be positive.");
        }
    }
}

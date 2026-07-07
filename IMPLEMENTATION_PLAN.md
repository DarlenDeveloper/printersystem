# ChequeMate - Cheque Printing System Implementation Plan

## Overview

A Windows desktop application (C# / WPF / .NET 8) that prints variable data (payee, date, amount) onto pre-printed bank cheque leaves with precise alignment. Users scan their cheque, visually map field positions, save templates per bank, and batch-print cheques.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| UI Framework | WPF (.NET 8) |
| Architecture | MVVM (CommunityToolkit.Mvvm) |
| Database | SQLite (via EF Core) |
| Printing | System.Drawing.Printing + WPF PrintVisual |
| Image Handling | System.Drawing / WPF BitmapImage |
| DI | Microsoft.Extensions.DependencyInjection |
| PDF Export | QuestPDF (optional, for preview/archive) |

---

## Project Structure

```
ChequeMate/
├── ChequeMate.sln
├── src/
│   ├── ChequeMate.Core/              # Domain models, interfaces, services
│   │   ├── Models/
│   │   │   ├── ChequeTemplate.cs     # Template definition (bank, paper size, fields)
│   │   │   ├── TemplateField.cs      # Single field (position, size, font, type)
│   │   │   ├── ChequeEntry.cs        # Data for one cheque (payee, amount, date)
│   │   │   ├── PrintJob.cs           # Batch print job
│   │   │   └── BankProfile.cs        # Bank metadata
│   │   ├── Services/
│   │   │   ├── ITemplateService.cs
│   │   │   ├── IPrintService.cs
│   │   │   ├── IAmountToWordsService.cs
│   │   │   └── IChequeService.cs
│   │   └── Enums/
│   │       ├── FieldType.cs           # Date, Payee, AmountWords, AmountFigures, Custom
│   │       └── PrintStatus.cs
│   │
│   ├── ChequeMate.Infrastructure/     # Implementations
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── Migrations/
│   │   ├── Services/
│   │   │   ├── TemplateService.cs
│   │   │   ├── PrintService.cs
│   │   │   ├── AmountToWordsService.cs
│   │   │   └── ChequeService.cs
│   │   └── Printing/
│   │       ├── ChequePrintEngine.cs   # Core print logic (renders fields at coordinates)
│   │       ├── PaperSizeHelper.cs     # Custom paper size management
│   │       └── AlignmentCalibrator.cs # Offset/DPI calibration
│   │
│   └── ChequeMate.App/               # WPF application
│       ├── App.xaml
│       ├── MainWindow.xaml
│       ├── ViewModels/
│       │   ├── MainViewModel.cs
│       │   ├── TemplateEditorViewModel.cs
│       │   ├── ChequeEntryViewModel.cs
│       │   ├── PrintQueueViewModel.cs
│       │   └── SettingsViewModel.cs
│       ├── Views/
│       │   ├── TemplateEditorView.xaml       # WYSIWYG canvas editor
│       │   ├── ChequeEntryView.xaml          # Data entry form
│       │   ├── PrintQueueView.xaml           # Batch queue management
│       │   ├── TemplateListView.xaml         # Saved templates browser
│       │   ├── PrintPreviewView.xaml         # Preview before printing
│       │   └── SettingsView.xaml             # Printer/calibration settings
│       ├── Controls/
│       │   ├── DraggableField.xaml           # Draggable/resizable field control
│       │   └── ChequeCanvas.xaml             # Canvas with cheque background
│       ├── Converters/
│       └── Resources/
│           └── Styles/
│
└── tests/
    └── ChequeMate.Tests/
```

---

## Implementation Phases

### Phase 1: Foundation (Core + Database)

**Goal:** Models, database, and basic service layer.

- [ ] Create solution and project structure
- [ ] Define domain models (`ChequeTemplate`, `TemplateField`, `ChequeEntry`, `BankProfile`)
- [ ] `FieldType` enum: Date, Payee, AmountInWords, AmountInFigures, AccountPayee, Custom
- [ ] Set up SQLite + EF Core with migrations
- [ ] Implement `AmountToWordsService` (number → "Five Thousand Only")
- [ ] Implement `TemplateService` (CRUD for templates)
- [ ] Implement `ChequeService` (CRUD for cheque entries)
- [ ] Unit tests for amount-to-words conversion

### Phase 2: Template Editor (The Heart)

**Goal:** WYSIWYG canvas where user scans a cheque image and maps field positions.

- [ ] Import cheque scan (JPEG/PNG/TIFF) — file picker or scanner integration (WIA)
- [ ] Display scanned cheque as canvas background at actual DPI scale
- [ ] DPI/scale calibration:
  - User inputs physical cheque dimensions (width × height in mm)
  - System calculates pixels-per-mm ratio from image resolution
  - All field positions stored in mm (not pixels) for print accuracy
- [ ] Draggable/resizable field rectangles on the canvas
  - Each field has: type, position (X, Y in mm), width, height, font family, font size, alignment, padding
- [ ] Field property panel (edit font, size, alignment per field)
- [ ] Save template (stores: bank name, cheque image path, paper dimensions, field positions)
- [ ] Load/edit existing templates
- [ ] Delete template

### Phase 3: Print Engine

**Goal:** Render only the text fields at exact coordinates onto the physical cheque.

- [ ] `ChequePrintEngine` — takes a `ChequeEntry` + `ChequeTemplate`, renders text at field coordinates
- [ ] Custom paper size setup (programmatically set via Windows print spooler API or printer driver)
- [ ] `PaperSizeHelper` — creates/selects custom paper size matching cheque dimensions
- [ ] Coordinate mapping: template mm positions → print document coordinates (accounting for margins, DPI)
- [ ] Print only text (no background image, no borders — just the field values)
- [ ] Alignment offset settings (global X/Y offset in mm to compensate for printer feed variance)
- [ ] Alignment test print: prints a grid/crosshair pattern on plain paper so user can overlay on cheque and verify
- [ ] Printer selection (enumerate available printers, let user pick and set as default for cheque printing)

### Phase 4: Cheque Entry & Batch Printing

**Goal:** Data entry form and print queue for multiple cheques.

- [ ] Cheque entry form: payee, amount (auto-generates words), date, select template/bank
- [ ] Amount-to-words auto-fill as user types the amount
- [ ] Validation (required fields, amount limits)
- [ ] Print preview: shows the cheque image with rendered field text overlaid (WYSIWYG preview)
- [ ] Single cheque print
- [ ] Batch print queue: add multiple cheques, print sequentially
- [ ] Print status tracking (pending, printed, failed)
- [ ] CSV/Excel import for batch entries (payee, amount, date columns)

### Phase 5: History & Audit

**Goal:** Record keeping and duplicate detection.

- [ ] Print history log (cheque number, payee, amount, date printed, template used, user)
- [ ] Search/filter print history
- [ ] Duplicate detection: warn if same payee+amount printed within configurable window
- [ ] Export history to CSV/PDF

### Phase 6: Polish & Settings

**Goal:** UX refinements and configuration.

- [ ] Settings page: default printer, default template, alignment offsets, currency symbol
- [ ] Multiple currency support for amount-to-words
- [ ] Dark/light theme
- [ ] Template import/export (share templates between machines)
- [ ] Auto-update check (optional)
- [ ] Keyboard shortcuts for common actions

---

## Key Technical Challenges & Solutions

### 1. Precise Alignment (Most Critical)

**Problem:** Field text must land exactly where the bank expects on the physical cheque.

**Solution:**
- Store all positions in millimeters (absolute physical units)
- At print time, convert mm → device units using printer DPI (typically 600 DPI for LaserJet)
- Provide global X/Y offset adjustment to compensate for printer-specific feed variance
- Alignment test page: prints reference marks that user overlays on a real cheque to verify

### 2. Custom Paper Size

**Problem:** Cheques are non-standard paper sizes (e.g., 8.25" × 3.5").

**Solution:**
- Use Windows `DEVMODE` structure or `PrintDocument.DefaultPageSettings.PaperSize` with custom dimensions
- Store paper size per template
- Guide user to also set custom size in HP printer driver (manual feed tray)

### 3. Scanner/Image Import DPI

**Problem:** Scanned image must map 1:1 to physical cheque for accurate field placement.

**Solution:**
- Read DPI metadata from scanned image
- If DPI unknown, require user to input physical dimensions → calculate scale
- Display image at true scale (1mm on screen ≈ 1mm on print) when possible, or show a zoom with scale indicator

### 4. Concurrent Printing

**Problem:** Multiple cheques in queue shouldn't block the UI.

**Solution:**
- Background print queue using `Task.Run` / async pattern
- User feeds one cheque at a time (manual tray) — system waits for "next" confirmation or uses a short delay between jobs
- Print status updates via INotifyPropertyChanged binding

---

## Database Schema (SQLite)

```sql
CREATE TABLE BankProfiles (
    Id INTEGER PRIMARY KEY,
    BankName TEXT NOT NULL,
    Country TEXT,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE ChequeTemplates (
    Id INTEGER PRIMARY KEY,
    BankProfileId INTEGER REFERENCES BankProfiles(Id),
    TemplateName TEXT NOT NULL,
    ChequeImagePath TEXT,
    PaperWidthMm REAL NOT NULL,
    PaperHeightMm REAL NOT NULL,
    ImageDpi REAL,
    OffsetXMm REAL DEFAULT 0,
    OffsetYMm REAL DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE TABLE TemplateFields (
    Id INTEGER PRIMARY KEY,
    TemplateId INTEGER REFERENCES ChequeTemplates(Id) ON DELETE CASCADE,
    FieldType TEXT NOT NULL,
    Label TEXT,
    PositionXMm REAL NOT NULL,
    PositionYMm REAL NOT NULL,
    WidthMm REAL NOT NULL,
    HeightMm REAL NOT NULL,
    FontFamily TEXT DEFAULT 'Arial',
    FontSizePt REAL DEFAULT 10,
    Alignment TEXT DEFAULT 'Left',
    IsBold INTEGER DEFAULT 0
);

CREATE TABLE ChequeEntries (
    Id INTEGER PRIMARY KEY,
    TemplateId INTEGER REFERENCES ChequeTemplates(Id),
    Payee TEXT NOT NULL,
    AmountFigures REAL NOT NULL,
    AmountWords TEXT NOT NULL,
    ChequeDate TEXT NOT NULL,
    ChequeNumber TEXT,
    Memo TEXT,
    PrintStatus TEXT DEFAULT 'Pending',
    PrintedAt TEXT,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE PrintHistory (
    Id INTEGER PRIMARY KEY,
    ChequeEntryId INTEGER REFERENCES ChequeEntries(Id),
    PrintedAt TEXT NOT NULL,
    PrinterName TEXT,
    Success INTEGER NOT NULL,
    ErrorMessage TEXT
);
```

---

## Getting Started (Dev Setup)

```bash
# Prerequisites
# - .NET 8 SDK
# - Visual Studio 2022 or Rider

# Create solution
dotnet new sln -n ChequeMate
dotnet new classlib -n ChequeMate.Core -o src/ChequeMate.Core
dotnet new classlib -n ChequeMate.Infrastructure -o src/ChequeMate.Infrastructure
dotnet new wpf -n ChequeMate.App -o src/ChequeMate.App
dotnet new xunit -n ChequeMate.Tests -o tests/ChequeMate.Tests

# Add projects to solution
dotnet sln add src/ChequeMate.Core
dotnet sln add src/ChequeMate.Infrastructure
dotnet sln add src/ChequeMate.App
dotnet sln add tests/ChequeMate.Tests

# Add references
dotnet add src/ChequeMate.Infrastructure reference src/ChequeMate.Core
dotnet add src/ChequeMate.App reference src/ChequeMate.Core
dotnet add src/ChequeMate.App reference src/ChequeMate.Infrastructure
dotnet add tests/ChequeMate.Tests reference src/ChequeMate.Core
dotnet add tests/ChequeMate.Tests reference src/ChequeMate.Infrastructure

# Key NuGet packages
dotnet add src/ChequeMate.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/ChequeMate.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/ChequeMate.App package CommunityToolkit.Mvvm
dotnet add src/ChequeMate.App package Microsoft.Extensions.DependencyInjection
```

---

## Notes

- The system prints ONLY variable text onto pre-printed cheque stock — it never prints the cheque background, bank logo, or MICR line
- Each bank has a different cheque layout, hence the need for per-bank templates
- HP LaserJet manual/bypass tray is used to feed individual cheque leaves
- Physical cheque dimensions vary by country (common: ~8.25" × 3.5" or ~200mm × 90mm)
- All positioning is in millimeters for print accuracy regardless of screen resolution

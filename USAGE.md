# ChequeMate - Usage Guide

## Prerequisites

- Windows 10/11
- .NET 8 SDK installed ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- HP LaserJet (or any printer) connected and configured
- A scanner or phone camera to scan your blank cheque leaves

---

## Getting Started

### 1. Build & Run

```bash
dotnet restore
dotnet build
dotnet run --project src/ChequeMate.App
```

The app opens with the Template Editor view. The SQLite database is auto-created on first launch — no setup needed.

---

## Workflow

### Step 1: Create a Bank Profile

Before creating templates, you need at least one bank in the system. The app stores templates per bank so you can manage multiple chequebooks.

### Step 2: Scan Your Cheque

Take your blank cheque leaf and either:
- Scan it using your HP LaserJet's scanner (or a flatbed scanner) at 300+ DPI
- Take a clear, flat photo with your phone

Save as JPEG or PNG.

### Step 3: Create a Template

1. Click **"Template Editor"** in the sidebar
2. Click **"Import Cheque Image"** → select your scanned cheque image
3. The app auto-detects the image DPI and calculates the physical cheque dimensions (width × height in mm). Verify these match your actual cheque — adjust manually if needed.
4. Click **"+ Add Field"** to add a field (Payee, Date, Amount in Words, Amount in Figures, etc.)
5. For each field, set:
   - **Position X/Y (mm)** — where the field starts from the top-left corner of the cheque
   - **Width/Height (mm)** — how much space the text has
   - **Font, size, bold** — to match what looks natural on the cheque
6. Repeat for all fields on your cheque (typically: Date, Payee, Amount in Words, Amount in Figures, A/C Payee)
7. Enter a **Template Name** (e.g., "Standard Bank - Personal Cheque")
8. Click **"Save Template"**

**Tip:** The scanned image shows at 60% opacity as a background guide. The red rectangles represent your field positions. These rectangles show where text will be printed.

### Step 4: Alignment Test (Important!)

Before printing on a real cheque:

1. Load a sheet of plain paper into your printer's manual/bypass tray
2. Print an alignment test (grid + field outlines print on the paper)
3. Hold the printed paper up against your actual cheque leaf
4. Check if the field outlines line up with the printed areas on the cheque
5. If they're off, adjust the **Offset X/Y** values in the template (e.g., if text is 2mm too far right, set Offset X to -2)
6. Repeat until alignment is perfect
7. Save the template again

You only need to do this once per template. After calibration, every cheque prints correctly.

### Step 5: Print a Cheque

1. Click **"New Cheque"** in the sidebar
2. Select your template/bank from the dropdown
3. Fill in:
   - **Payee** — who the cheque is for
   - **Amount** — type the number, amount in words auto-generates
   - **Date** — defaults to today
   - **Cheque Number** (optional) — for your records
4. Click **"Add to Print Queue"**
5. Navigate to **"Print Queue"**
6. Select your printer from the dropdown
7. Feed a cheque leaf into the printer's manual tray
8. Click **"Print Selected"** or **"Print All"**

### Step 6: Batch Printing (CSV Import)

For multiple cheques at once:

1. Prepare a CSV file with this format:

```csv
Payee,Amount,Date,ChequeNumber,Memo
John Smith,5000,2024-12-15,000123,Invoice #456
ABC Corp,12500.50,2024-12-15,000124,Monthly rent
```

2. In the **New Cheque** view, select a template
3. Click **"Import CSV"** and select your file
4. All cheques appear in the Print Queue
5. Print them one by one (feed each cheque leaf into the tray between prints)

---

## Printer Setup Tips

### HP LaserJet Custom Paper Size

Since cheques are smaller than standard paper, configure a custom size:

1. Open **Windows Settings → Printers & Scanners**
2. Select your HP LaserJet → **Printing Preferences**
3. Go to the **Paper** tab
4. Click **Custom** and create a new size matching your cheque (e.g., 200mm × 90mm)
5. Save it with a name like "Bank Cheque"

### Manual Feed Tray

- Use the **manual/bypass tray** (usually the fold-down tray at the front)
- Place the cheque face-up with the top edge going in first
- Set the paper guides snug against the cheque edges
- The printer won't auto-pull from main tray when you select the manual source

---

## Common Cheque Dimensions

| Region | Typical Size |
|--------|-------------|
| US / North America | 8.5" × 3.5" (216 × 89 mm) |
| UK | 7.0" × 3.25" (178 × 83 mm) |
| India | 8.25" × 3.66" (210 × 93 mm) |
| UAE / Middle East | 8.27" × 3.5" (210 × 89 mm) |
| Singapore | 7.5" × 3.25" (190 × 83 mm) |

Measure your actual cheque with a ruler to be sure.

---

## Tips

- Always do an alignment test print on plain paper first before using a real cheque
- Keep offset adjustments small (0.5mm increments)
- If your scanner saves at 300 DPI, the auto-detected dimensions should be accurate
- If using a phone photo, manually enter the cheque dimensions in mm
- The "A/C Payee Only" field auto-fills the text — just position where you want it printed
- Amount in words supports Lakhs/Crores numbering system by default
- Save different templates for different cheque books, even from the same bank (personal vs. business)

---

## Data Location

- Database: `%LocalAppData%\ChequeMate\chequemate.db`
- Scanned images: stored wherever you save them (the template stores the file path)

---

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Text prints too far left/right | Adjust Offset X in template settings |
| Text prints too high/low | Adjust Offset Y in template settings |
| Text gets cut off | Increase the field Width/Height |
| Printer pulls from wrong tray | Set manual tray in printer preferences + app printer selection |
| Image dimensions seem wrong | Manually enter physical cheque size in mm |
| Font looks too small/big on cheque | Adjust Font Size (pt) for that field in template editor |

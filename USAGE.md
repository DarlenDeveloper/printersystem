# ChequeMate - Usage Guide

## Prerequisites

- Windows 10/11
- .NET 8 SDK installed ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- A Windows-compatible printer connected with its manufacturer's full driver
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

In **Template Editor**, enter the bank name under **Add a bank**, click **Add**, and select it from the Bank list. The app stores templates per bank so you can manage multiple chequebooks.

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

1. Cut plain paper to the exact measured cheque size and load one sheet in the rear/manual feed
2. Select the calibration printer and its physical paper source in Template Editor
3. Click **Print Alignment Test** (a grid and field outlines print on the paper)
4. Hold the printed paper up against your actual cheque leaf
5. Check if the field outlines line up with the printed areas on the cheque
6. If they're off, adjust the **Offset X/Y** values in the template (e.g., if text is 2mm too far right, set Offset X to -2)
7. Repeat until alignment is correct, then save the template again

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
7. Select the rear/manual paper source and feed exactly one cheque leaf
8. Click **Print Selected** or **Print Next**, then confirm the safety prompt
9. Inspect and remove the physical cheque. Click **Confirm Printed** only when it is correct.

Windows accepting a print job changes its status to **Submitted**, not **Printed**. If it did not physically print, use **Requeue** only after checking the printer queue to avoid duplicates.

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
5. Use **Print Next** and confirm each physical cheque separately. The application never sends the whole batch unattended.

---

## Supported Printer Setup

Always install the manufacturer's full Windows driver. Windows class drivers may omit custom paper sizes or physical paper-source names.

### Canon MAXIFY GX7040

- Use the **rear tray**, selected in both ChequeMate and the Canon driver/printer screen.
- Custom media must be 89–216 mm on the short edge and 127–1200 mm on the long edge.
- Plain paper must be 64–105 g/m².
- A 200 × 90 mm cheque is supported; a 170 × 70 mm cheque is outside Canon's specified minimum.

### Epson L220

- Use the **rear paper feed**; it does not have a dedicated bypass tray.
- Load one cheque at a time for controlled sequencing.
- Epson specifies plain paper at 64–90 g/m².
- Confirm that the installed Epson driver accepts the exact measured custom size before calibration.

### Custom Paper Size

Since cheques are smaller than standard paper, configure a custom size:

1. Open **Windows Settings → Printers & Scanners**
2. Select your printer → **Printing Preferences**
3. Go to the **Paper** tab
4. Click **Custom** and create a new size matching your cheque (e.g., 200mm × 90mm)
5. Save it with a name like "Bank Cheque"

### Rear or Manual Feed

- Use the printer's rear/manual paper source selected in ChequeMate
- Follow the printer guide for face-up/face-down orientation and run an alignment test before a real cheque
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
- Amount wording uses the international Thousand/Million/Billion system. Currency and fractional-unit wording are editable on the New Cheque screen.
- Save different templates for different cheque books, even from the same bank (personal vs. business)

---

## Data Location

- Database: `%LocalAppData%\ChequeMate\chequemate.db`
- Pre-upgrade database backups: `%LocalAppData%\ChequeMate\Backups`
- Scanned images: stored wherever you save them (the template stores the file path)

---

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Text prints too far left/right | Adjust Offset X in template settings |
| Text prints too high/low | Adjust Offset Y in template settings |
| Text gets cut off | Increase the field Width/Height |
| Printer pulls from wrong tray | Select the same rear/manual source in the printer, driver, and ChequeMate |
| Image dimensions seem wrong | Manually enter physical cheque size in mm |
| Font looks too small/big on cheque | Adjust Font Size (pt) for that field in template editor |

## Acceptance Test Before Live Cheques

1. Build and run the automated tests on the Windows deployment PC: `dotnet test`.
2. Print the alignment grid on at least 10 cheque-sized plain sheets.
3. Confirm every required field is inside the printer's physical margins.
4. Check feed skew and repeatability; adjust offsets only after the paper feed is consistent.
5. Test the Pending → Submitted → Confirm Printed workflow, including a deliberate cancel and requeue.
6. Only then test one voided/sample cheque. Do not start with a live cheque.

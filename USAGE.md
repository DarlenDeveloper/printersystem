# ChequeMate 2.1.2 - Windows testing guide

ChequeMate prints variable text onto pre-printed Ugandan bank cheque leaves. It does not print bank artwork or MICR encoding.

## Safety status

- The included specimen-derived bank layouts are marked **TEST ONLY**.
- Use full A4 test sheets, cut blank sheets, or cheques clearly marked **VOID**.
- Do not use a live cheque until its bank-specific layout and printing method have been approved.
- The bottom MICR clear band and right-hand restraint area remain protected.

## Requirements

- Windows 10 or Windows 11, 64-bit
- The printer manufacturer's full Windows driver
- Epson L220, Canon MAXIFY GX7040, or another installed Windows printer
- Plain A4 sheets for the first alignment test

Clients do not import scans or PDFs and do not create Windows paper forms. They choose a built-in bank, physical leaf size and currency. ChequeMate automatically omits currency wording when it is already preprinted on the leaf.

## Included specimen presets

- Bank of India (Uganda): 203 × 101.6 mm, UGX and USD
- Exim Bank Uganda: 203 × 101.6 mm, USD
- Stanbic Bank Uganda: 203 × 101.6 mm, UGX and USD
- Centenary Bank: 178 × 76 mm USD and 203 × 101.6 mm UGX
- DFCU Bank: 203 × 101.6 mm, USD

The 203 × 101.6 mm presets use one field-tested writable geometry for Bank of
India, DFCU, Stanbic, Exim and Centenary UGX. Centenary USD is a different
178 × 76 mm leaf and keeps its own geometry. The shared 203 mm geometry was
confirmed on the Bank of India USD field test `DOC531.pdf` on 27 November 2026.

ChequeMate enforces the BOU paper families and protected geometry:

- Personal: 165 × 73 mm
- Corporate: 178 × 76 mm or 203 × 101.6 mm
- Bottom 16 mm: protected MICR clear band; ordinary application text is blocked
- Right 51 mm above the clear band: controlled restraint area for date and amount fields
- Long payees or amount wording are reduced within a safe font range; printing stops instead of adding an ellipsis when text still cannot fit
- Amount figures print as plain Arial Bold 10 pt numbers such as `5,000`, without surrounding asterisks or unnecessary `.00`; real cents remain visible

## First-time workflow

### 1. Create a queued cheque

Open **New Cheque**, choose the bank and cheque type, enter the data, and select **Add to Print Queue**.

### 2. Open printer setup

Open **Print Queue** and select the queued cheque. Printer settings are saved separately for every printer and physical cheque size. Layouts with the same physical size automatically share the saved printer setup.

Choose:

- **Printer**: the installed Windows printer.
- **Paper source**: rear/manual feed when exposed by the driver, or **Use printer default**.
- **Print method**: start with **A4 positioned (recommended)**.
- **Feed direction**: normally **Long edge first** for the 203 x 101.6 mm corporate format.
- **Paper guide position**: start with **Right guide** and use the same guide every time.
- **Calibration X/Y**: leave both at `0` for the first test.

For the Epson L220 used in the 27 November 2026 Bank of India test, the confirmed
setup was A4 positioned, rear paper feed, long edge first, right guide,
Calibration X `-10` mm and Y `0` mm. Save it once and ChequeMate will reuse it
for all 203 × 101.6 mm presets on that printer. Do not assume the same offset for
a different printer.

No custom paper form is required in A4-positioned mode.

### 3. Print the alignment sheet

1. Load one full plain A4 sheet in portrait orientation.
2. Select **Print Alignment Test**.
3. The sheet shows the cheque outline, 10 mm grid, fields and protected areas at the selected feed position.
4. Place a VOID cheque or cut blank specimen over the printed outline.
5. Measure any horizontal and vertical difference in millimetres.
6. Enter those differences as Calibration X and Y, then repeat the test.
7. Select **Save Setup** when aligned.

Use **Clear Setup** to remove saved method, orientation, guide and offsets for the selected printer and cheque size.

### 4. Test cheque data

Select **Print Selected**. For unverified layouts, ChequeMate requires a plain sheet or clearly marked VOID specimen. Windows accepting a job changes it to **Submitted**, not **Printed**. Inspect the physical result and then select **Confirm Printed**.

## Print methods

### A4 positioned (recommended)

Windows receives a normal portrait A4 job. ChequeMate places the cheque content at the selected left, centre or right guide position. This avoids the registered custom-paper form problem.

Always begin with a full A4 alignment sheet. Only test a cut sheet when the printer officially supports its dimensions.

### Exact cheque size (advanced)

ChequeMate sends the width and height directly as a custom Windows page. A named Windows form is not required, but the driver may still reject or substitute unsupported media. Run the alignment test before sending cheque data.

## Epson L220

Epson's documented minimum single sheet is 89 x 127 mm.

- **203 x 101.6 mm corporate:** within the documented physical range; test A4-positioned mode first.
- **165 x 73 mm personal:** direct leaf feeding is not supported; use a full A4 test sheet.
- **178 x 76 mm corporate-small:** direct leaf feeding is not supported; use a full A4 test sheet.

## Canon MAXIFY GX7040

Canon documents a custom-size range of 89 x 127 mm through 216 x 1200 mm.

- **203 x 101.6 mm corporate:** within the documented range.
- **165 x 73 mm and 178 x 76 mm:** below the minimum short edge; use full A4 tests or supported cheque hardware.

## Compliance warning

The included layouts remain test-only. Obtain written approval from the issuing bank before live use with an inkjet, laser or other non-impact printer.

## Data locations

- Database: `%LocalAppData%\ChequeMate\chequemate.db`
- Printer profiles: `%LocalAppData%\ChequeMate\printer-profiles.json`
- Database backups: `%LocalAppData%\ChequeMate\Backups`

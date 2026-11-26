# ChequeMate - Windows Testing Guide

ChequeMate prints variable text onto pre-printed Ugandan cheque leaves. It does not print bank artwork or MICR encoding.

## Safety status

- The included Uganda-standard layouts are marked **TEST ONLY**.
- Use plain paper cut to size or a cheque clearly marked **VOID**.
- Do not use a live cheque until its bank-specific layout and printer method have been approved.
- The application protects the bottom 16 mm MICR clear band and the right-hand restraint area.

## Requirements

- Windows 10 or Windows 11, 64-bit
- The manufacturer's full Windows printer driver
- A rear/manual paper source
- Plain test sheets cut to the selected cheque dimensions

## Client workflow

### 1. Choose the bank

Open **New Cheque** and select one of the supported Ugandan banks. Banks are built into the application; clients cannot import scans or create arbitrary bank layouts.

### 2. Choose the cheque type

Choose the physical format printed on the cheque book:

- Personal - 165 x 73 mm
- Corporate - 178 x 76 mm
- Corporate - 203 x 101.6 mm

All three initial formats are test-only standard starting points. The bank name alone does not prove that its current cheque leaf uses a particular format.

### 3. Enter the cheque data

Enter the payee, amount, date and optional cheque number/memo. The amount in words is generated automatically. Add the item to the print queue.

### 4. Run an alignment test

In **Print Queue**:

1. Select the printer and its rear/manual paper source.
2. Select a queued cheque so its bank layout is available.
3. Click **Alignment Test**.
4. Follow the displayed load instruction. Epson L220 undersized-cheque tests use A4; other supported printers use paper cut to the displayed cheque size.
5. Compare the grid with a VOID specimen. Orange areas are protected and must contain no added text.

### 5. Test variable printing

For an unverified layout, ChequeMate explicitly asks for plain paper or a VOID specimen. Submit one item at a time, inspect it, and only then use **Confirm Printed**.

Windows accepting a print job changes the record to **Submitted**, not **Printed**. Requeue only after verifying that the earlier job did not print.

## Printer notes

### Canon MAXIFY GX7040

The rear tray custom-size range is 89-216 mm on the short edge and 127-1200 mm on the long edge. The 73 mm and 76 mm cheque formats are below that minimum and ChequeMate blocks them. The 203 x 101.6 mm format may be mechanically accepted, but it remains a non-impact inkjet test.

### Epson L220

The L220's smallest supported single sheet is 89 x 127 mm. Uganda's 165 x 73 mm personal and 178 x 76 mm corporate-small leaves are below that limit, so ChequeMate blocks direct printing instead of allowing the driver to substitute A4.

For these undersized layouts, **Alignment Test** deliberately prints the cheque outline and fields at the top-left of an A4 sheet. Overlay a VOID cheque on that outline to inspect positioning. Do not feed a live cheque directly into the L220.

For a size that is within the printer's range, first create and save that exact **User-Defined** size in the Epson Windows Printing Preferences, then restart ChequeMate.

## Compliance warning

The publicly indexed Uganda cheque standard describes impact printing for cheque completion and prohibits laser/other non-impact completion. Obtain written approval from the issuing bank before any live use with either inkjet printer.

## Windows acceptance checklist

1. Install on a Windows test PC.
2. Run `dotnet test` from source if the .NET SDK is available.
3. Confirm all 29 banks appear and each has three test formats.
4. Print at least ten plain-paper alignment sheets to check feed skew and repeatability.
5. Verify that Canon rejects the 165 x 73 mm and 178 x 76 mm formats.
6. Exercise Pending -> Submitted -> Confirm Printed, including cancel/requeue.
7. Test one VOID specimen only after plain-paper alignment is consistent.

## Data location

- Database: `%LocalAppData%\ChequeMate\chequemate.db`
- Pre-upgrade database backups: `%LocalAppData%\ChequeMate\Backups`

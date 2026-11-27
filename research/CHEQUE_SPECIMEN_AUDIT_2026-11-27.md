# ChequeMate Uganda cheque specimen audit

Date: 27 November 2026  
Scope: the seven original specimen PDFs `DOC517.pdf`–`DOC523.pdf`, plus the later DFCU test scan `DOC530.pdf`.

## Decision

ChequeMate must not use one generic Personal/Corporate layout for every bank. A printable preset must identify:

1. bank;
2. leaf series/size;
3. currency;
4. specimen revision or verification date; and
5. field coordinates in cheque millimetres.

Printer calibration is a separate final X/Y correction. It cannot repair a wrong bank template.

Only specimen-backed presets should be selectable for printing. Banks without a verified specimen should remain visible as **Not calibrated** and printing should be disabled for them.

## Regulatory baseline

- UNBS still lists **US 127:2000 — National cheque — Specification** as compulsory. The catalogue says it covers personal and corporate cheques: <https://webstore.unbs.go.ug/store.php>
- The publicly indexed Bank of Uganda guide specifies 165 × 73 mm for a personal cheque and 178 × 76 mm or 203 × 101.6 mm for a corporate/business cheque. It also defines a 16 mm clear MICR band along the entire bottom edge and a 51 mm restraint area along the right edge above that band. The full UNBS standard should be purchased/confirmed before legal production sign-off: <https://www.scribd.com/document/81289772/Uganda-Cheque-Book-Guidelines>
- Uganda Bankers' Association says non-standard leaves have not been processed since 1 July 2019 under the automated clearing system: <https://ugandabankers.org/publication/view/uba-notice-on-cheque-books>

The national standard fixes protected zones and nominal paper families; it does **not** give ChequeMate a single safe payee/date/amount coordinate set for every bank design. The supplied leaves visibly place those fields differently.

### Geometry that must be locked in code

For a cheque of width `W` and height `H`, measured from the physical top-left corner:

- MICR clear band: `x=0`, `y=H-16`, `width=W`, `height=16`. ChequeMate must never render ordinary text in it.
- Restraint area: `x=W-51`, `y=0`, `width=51`, `height=H-16`.
- Unrestrained area: `x=0`, `y=0`, `width=W-51`, `height=H-16`.
- The restraint area is **not** a general front-side bank-stamp area. The standard places controlled items there, including the date, currency symbol and amount box. Optional payee/collecting-bank/paying-bank/clearing-house endorsement areas are on the reverse of the cheque, above the clear band.
- The standard amount box is 41 mm wide and about 9.5 mm high, with its right edge 5 mm from the cheque's right edge. Its normal vertical range is 35–44 mm from the bottom; the permitted corporate alternative is 44–54 mm from the bottom.
- The date-line baseline is 54 mm from the bottom; the corporate alternative is 63.5 mm from the bottom.
- Publicly indexed tolerances are approximately ±1.3 mm horizontally and ±1.6 mm vertically. Printer and scan errors therefore cannot be allowed to accumulate unchecked.

## Scan inventory

All seven original PDFs contain one sideways, approximately 300-DPI JPEG inside a portrait PDF page. Rotation is a scan-storage issue only; it does not mean the cheque should be printed in portrait.

| Source | Identified leaf | Embedded image after normalization | Raw scan footprint at 300 DPI | Nominal family to test | Confidence |
|---|---|---:|---:|---:|---|
| DOC517 | Bank of India (Uganda), UGX | 2430 × 1151 px | 205.7 × 97.5 mm | 203 × 101.6 mm | high on layout; medium on exact paper size |
| DOC518 | Exim Bank Uganda, USD | 2459 × 1140 px | 208.2 × 96.5 mm | 203 × 101.6 mm | high on layout; medium on exact paper size |
| DOC519 | Stanbic Bank Uganda, USD | 2322 × 1068 px | 196.6 × 90.4 mm | 203 × 101.6 mm | high on layout; low on exact paper size |
| DOC520 | Stanbic Bank Uganda, UGX | 2250 × 1056 px | 190.5 × 89.4 mm | 203 × 101.6 mm | high on layout; low on exact paper size |
| DOC521 | Centenary Bank, USD | 2045 × 816 px | 173.1 × 69.1 mm | probably 178 × 76 mm | high on distinct layout; medium-low on family |
| DOC522 | Centenary Bank, UGX | 2435 × 1140 px | 206.2 × 96.5 mm | 203 × 101.6 mm | high on layout; medium on exact paper size |
| DOC523 | Bank of India (Uganda), USD | 2424 × 1158 px | 205.2 × 98.0 mm | 203 × 101.6 mm | high on layout; medium on exact paper size |
| DOC530 | DFCU, USD, plus alignment sheet and print output | 2442 × 2958 px composite | not suitable as a paper-size measurement | 203 × 101.6 mm from test leaf | high on relative layout |

The raw scan footprints are evidence of scanner cropping/scaling, not valid Windows paper sizes. Exact paper dimensions must be taken from the physical leaf with a millimetre ruler. No supplied specimen proves a 165 × 73 mm personal-cheque layout.

## Specimen-derived first-pass field map

Coordinates below are measured from the normalized images and rounded to whole millimetres. `X` is from the physical left edge; `Y` is from the top. They are research candidates for an A4 overlay test, **not live-cheque approval**. Width and height describe the printable text rectangle.

The bank-specific evidence is mainly the vertical placement of the payee and amount-in-words lines. The right-side date and figures fields must use the standard formula and the permitted normal/corporate alternative already printed on that leaf; they must not be freely invented from a scan.

| Preset candidate | Payee (x,y,w,h) | Amount words (x,y,w,h) | Restraint-field standard variant |
|---|---:|---:|---|
| Bank of India large, UGX/USD | 10,43,133,8 | 30,56,113,19 | 203 × 101.6 corporate alternative |
| Exim large, USD | 8,45,137,8 | 31,57,106,20 | 203 × 101.6 corporate alternative |
| Stanbic large, UGX/USD | 9,39,146,8 | 35,54,120,20 | 203 × 101.6 corporate alternative |
| Centenary small, USD | 10,26,107,8 | 30,37,87,17 | probable 178 × 76 normal position |
| Centenary large, UGX | 9,44,137,8 | 30,59,115,18 | 203 × 101.6 corporate alternative |
| DFCU large, USD | 10,44,133,8 | 29,56,112,20 | 203 × 101.6 corporate alternative |

For the 203 × 101.6 mm corporate-alternative family, the amount box formula gives approximately `x=157`, `y=47.6`, `w=41`, `h=9.5`, and the date baseline is `y=38.1` from the top. For a 178 × 76 mm normal-position leaf, the amount box begins at approximately `x=132`, `y=32`, and the date baseline is `y=22` from the top.

Important interpretation:

- The DFCU payee starts roughly 24 mm lower than ChequeMate's current generic corporate payee (`Y=20`). This confirms a bank-template error, not a printer offset. The current generic amount-box formula for the 203 mm corporate alternative is close to the national formula; it is the bank-specific payee/words placement that failed most visibly.
- Bank of India and Exim are similar but not identical. They should not share a preset.
- Stanbic UGX and USD use the same visual family in these specimens, but their scans were cropped differently. One physical measurement is still required before choosing the nominal paper family.
- Centenary has two genuinely different leaf families. A bank-only selector is insufficient.
- Every candidate keeps all variable printing above the bottom 16 mm MICR clear band.
- Amount-words text needs controlled wrapping to a second line; ellipsis truncation is unsafe on a cheque.

## Software blockers found by the audit

1. **Generic templates:** the current seeder adds 165 × 73, 178 × 76, and 203 × 101.6 generic layouts to every institution. That creates false support.
2. **Currency is not part of the template:** the leaf itself may be printed for UGX or USD, and the amount label/box position can vary.
3. **Currency is duplicated in words:** ChequeMate currently generates strings such as `Uganda Shillings One Million Only`, while the supplied leaves already print `Uganda Shillings` or `US Dollars` before the writing line. Presets need an `include currency prefix` rule; these specimens should normally print only `One Million Only` on the words line.
4. **No leaf-series identity:** two leaves from one bank can have different sizes or geometry. A preset needs a stable series/revision label, not only `Personal` or `Corporate`.
5. **Unsafe truncation:** the renderer uses ellipsis when words do not fit. A cheque must instead reduce font size within an approved range or wrap predictably and reject input that still cannot fit.
6. **No 165 × 73 evidence:** the personal layout must remain unverified until an actual specimen is supplied and measured.
7. **Scan scaling cannot certify paper size:** PDF page dimensions and 300-DPI metadata disagree with the national nominal families by several millimetres. Physical ruler measurements are required.

## What existing systems and GitHub actually show

No credible public GitHub dataset was found containing Ugandan bank-by-bank cheque coordinates. Projects claiming broad bank support are mainly for India or North America and their coordinates are not transferable.

The useful, repeatable pattern is consistent across the systems reviewed:

- store a separate coordinate set per cheque stock/template;
- use absolute millimetre placement;
- keep printer-wide X/Y offsets separate from template coordinates;
- test on plain paper at 100% scale;
- permit fine per-field adjustment only in an admin/template workflow.

Examples:

- LineLedger keeps every field baseline in a cheque configuration plus global `offset_x`/`offset_y`: <https://github.com/lineledger/lineledger>
- An open browser cheque printer exposes bank presets, whole-page calibration, and per-field millimetre fine-tuning: <https://chequeprinter.github.io/print/>
- Commercial template workflows create a template from a particular scanned leaf and pin each field, explicitly warning that a new bank layout needs a new template: <https://mail.khatabilling.in/services/cheque-printing>

This supports the architecture above, but none of those sources can replace measurements from the supplied Ugandan leaves.

## Safe implementation order

1. Replace generic printable templates with the six specimen-derived coordinate families above (including DFCU), split into eight currency/leaf presets.
2. Add leaf series, currency, verification status, source specimen, and revision date to each preset.
3. Hide or disable printing for all other banks and for 165 × 73 until a specimen exists.
4. Make amount wording preset-aware so preprinted currency text is not duplicated.
5. Replace ellipsis with deterministic two-line fitting and validation.
6. Generate an A4 overlay for each preset and test at Actual Size/100%.
7. Record physical width/height and overlay corrections. Only then mark that preset verified.
8. Keep printer calibration as a small per-printer adjustment after the preset aligns on A4.

## Measurements still needed from the physical leaves

For each distinct leaf, record with a ruler:

- exact width and height to the nearest 0.5 mm;
- whether the date end or logo end enters the printer first;
- distance from the left edge to the start of the payee writing line;
- distance from the top edge to each writing baseline;
- exact amount-box rectangle;
- confirmation that the bottom 16 mm clear band remains untouched.

Those values turn the candidate coordinates into verified production presets. Printer calibration should not be used to hide missing measurements.

# docs/Project regeneration, PAYTON-LEGION2, 2026-10-19

Payton, relayed by OMAR-VICE#: "1: A (regenerate docs/Project/*.md from the LEGION2 store and present it as its own commit for review; do not push until Payton approves)".

## Run

- 19:19:18 local: `Invoke-ProjectDocsRegen.ps1` (analyzer 0, WhatIf first) copied the then-current projection to `before-regen/` (hash-verified, kept local, not committed) and called `workflow.requirements.generateDocument` through plugin 1.121.0.
- All 5 regenerated files are byte-identical to the 16:22 export written by the R0 apply, so the LEGION2 store did not change between 16:22 and 19:19.

File hashes after regeneration (SHA256 prefix, bytes):

- Functional-Requirements.md: 8BB4C803, 324,935
- Technical-Requirements.md: 5736D907, 167,001
- Testing-Requirements.md: 66E9F655, 95,876
- TR-per-FR-Mapping.md: 6F2F9C60, 18,619
- Requirements-Matrix.md: 32AD2951, 31,552

## Diff against 9221488 (last committed projection, 2026-10-16)

Full lists are in `regen-analysis.txt` and `regen-analysis.json`. "Today" means the id is in a 2026-10-19 write: the D8 status-correction journals, the R0 packet (16 records, 9 mapping rows) or the step 6 capture (TR-BUILD-001, TEST-BUILD-001). "Earlier" means a store change made between 2026-10-16 and 2026-10-19 by other sessions, or a cross-reference line.

- Functional-Requirements.md: 25 FR sections changed (15 today, 10 earlier). Section count unchanged at 235.
- Technical-Requirements.md: 41 TR sections changed (31 today, 10 earlier). Section count unchanged at 139.
- Testing-Requirements.md: 30 ids on changed lines (12 today, 18 earlier or cross-reference).
- TR-per-FR-Mapping.md: 51 ids on changed lines (18 today, 33 earlier or cross-reference).
- Requirements-Matrix.md: 2 ids on changed lines (TEST-BUILD-001 today, TEST-ROMM-LAUNCH-001 earlier).

## Duplicate headings

10 FR records render two headings each: FR-CFG-002, FR-INP-006, FR-MED-002, FR-MED-003, FR-ROMM-LAUNCH-001, FR-SID-013, FR-SNP-001, FR-SNP-002, FR-UI-003, FR-VIC20-002. These are exactly the 10 FR "earlier" changes.

Cause is store content, not the generator. Each record's title was shortened (FR-MED-002 is now "Video Recording") and its description begins with an embedded copy of the old rendered section, for example `## FR-MED-002: Video Recording` followed by `**Title:** Video Recording (MP4 via FFmpeg)`. The export prints its own heading and then the description, so the old heading appears a second time. Fixing it means editing those 10 records' descriptions in the store; this commit does not change the store.

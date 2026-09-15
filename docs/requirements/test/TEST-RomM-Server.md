# RomM server test requirements

## TEST-ROMM-001: Platform directories

On a running stack, `/romm/library/roms` contains `c64`, `c128`, `c-plus-4`, and `vic-20` (empty allowed). HVSC is at `/romm/library/hvsc`, not under `roms/`. Docs and compose do not use `plus4` or `vic20` as folder names.

## TEST-ROMM-002: Bind persistence

A probe file under host `roms/vic-20/` is visible at `/romm/library/roms/vic-20/` in the container.

## TEST-ROMM-003: Redeploy skip/import

Existing `config.yml` plus populated `roms/c64` leaves config byte-identical and skips GB64 import. Empty `roms/c64` plus `gb64/Games` runs library build.

## TEST-ROMM-004: SID and screenshot path resolution

Fixture HVSC and screenshot trees under `runtime/library` resolve NFO relative paths. Compose has a single library bind.

## TEST-ROMM-005: REST contract

Against a running RomM (or recorded fixtures matching OpenAPI 5.0.0): heartbeat, platforms list/get, roms list with `platform_ids`/`search_term`/`limit`/`offset`, rom detail, authenticated download, scan task start/status. Covered in `F:\GitHub\RomM\tests\RomM.Client.Tests` (`RomMCoreClientTests`, `OpenApiCoverageTests`, `RomListQueryTests`).

## TEST-ROMM-006: Collections

List/create/add/remove/rename/delete collections; smart collections are read-only.

## TEST-GB64-001: GB64 organizer landing

Packages land under `roms/c64` with `(gb64-{id})` tags, never letter-bucket parents. `tools/Gb64Import.Tests`.

## TEST-GB64-002: Screenshot sync skip/run

Given empty `library/screenshots` and present `gb64/Screenshots`, prepare robocopies and writes `.screenshots-synced`. Given existing screenshots, second prepare SKIPs unless Force.

## TEST-GB64-003: NFO tags

`NfoAndTagTests` parse Unique-ID, Name, Language, Pal/NTSC, Screenshot, SID, TrueDrive and assert stem contains `(En)`, `(E)`, `(PAL)`, `(rev-01)`, `(gb64-26153)`, `(TrueDrive)` for the sample NFO. Sanitize strips `/`. Multi-language emits multiple tags.

## TEST-GB64-004: Conditional import and resume

Prepare with media or `.gb64-library-built` SKIPs import. Empty `roms/c64` plus zips RUNs Gb64Import and creates the marker. State file skips already-imported Unique-IDs unless `--force`.

## TEST-GB64-005: SID NFO fixer

Given HVSC containing a SID under a different relative path than NFO `SID:`, `--fix-sids` remaps. Missing SID becomes `(None)`. Dry-run leaves ZIPs unchanged.

## TEST-GB64-006: Asset path validation

`--validate-assets` counts ShotOk/ShotMissing and SidOk/SidMissing against fixture screenshot and HVSC trees.

## TEST-HVSC-001: HVSC not in image

Dockerfile does not COPY HVSC. Compose bind is present. `Resolve-SidPath` works against the host tree.

## TEST-HVSC-002: Fetch dest and MUSICIANS

`Download-Hvsc.ps1` with a fixture archive or `HVSC_SKIP_DOWNLOAD=1` plus cached archive produces `runtime/library/hvsc/MUSICIANS`. Default dest is that path.

## TEST-HVSC-003: Prepare warns if HVSC missing

Prepare with empty `library/hvsc` logs WARN and does not throw solely for missing HVSC.

## TEST-CSDB-001: Search fixtures

HTML/XML fixtures yield release/SID ids and types; kind maps to `roms/c64/` writes.

## TEST-CSDB-002: Ingest cap and archives

1..20 explicit items; archives extract; archive file not retained. Partial failures recorded.

## TEST-CSDB-003: SID hardlink

Mocked downloads write `(csdb-{id})` under `roms/c64/`. SID hardlinks resolve from a fixture HVSC tree.

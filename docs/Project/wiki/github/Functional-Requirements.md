# Functional Requirements (MCP Server)

## FR-CSDB-001 CSDb search

Users can search CSDb for SID, demo, and crack via the CSDb bridge (:8090) and/or RomM.Client.Csdb. Results include CSDb id, title, type, and kind.
Scope: layer-1+

## FR-CSDB-002 Selective CSDb ingest

Ingest is explicit ids only, max 20 per request. Writes Structure A under roms/c64/ with (csdb-{id}) tags. Archives extract; the archive file is not kept.
Scope: layer-1+

## FR-CSDB-003 CSDb SID via HVSC

When CSDb provides HVSCPath and the file exists under HVSC_ROOT, ingest hardlinks or copies into roms/c64/. HVSC is not a RomM platform folder.
Scope: layer-1+

## FR-GB64-001 GameBase64 source tree and Structure A landing

GB64 remains host-sourced under ./gb64 (Games ZIPs, Screenshots, ROMs). C64 game media is imported only under runtime/library/roms/c64/. Letter buckets (a1, b2, 0) are never RomM platform or parent folders. gb64/ROMs firmware is not auto-imported into roms/ (optional library/bios is a separate root).

Acceptance:
1. Import output is under roms/c64/ only.
2. No letter-bucket directory is created as a RomM parent.
3. Missing gb64/Games causes prepare to SKIP game import with a log line.
Scope: layer-1+

## FR-GB64-002 Screenshot staging into library/screenshots

NFO Screenshot: values are GB64-relative (A\Alfabug.png). Prepare shall robocopy gb64/Screenshots into runtime/library/screenshots when the library screenshot tree is empty (or ForceLibraryBuild), then write marker .screenshots-synced. Runtime resolution does not require mounting the whole gb64 tree.

Acceptance:
1. After sync, Screenshot: A\file.png resolves to library/screenshots/A/file.png.
2. Existing screenshot library data skips sync unless Force.
3. Missing gb64/Screenshots logs SKIP and does not throw.
Scope: layer-1+

## FR-GB64-003 VERSION.NFO tagging and extract

tools/Gb64Import shall unzip each GB64 game ZIP, parse VERSION.NFO, and name output from NFO Name (sanitized) plus RomM tags per docs/gb64-romm-tag-mapping.md: language, PAL/NTSC, revision, (gb64-id), TrueDrive when NFO says Yes. Multi-file media becomes a folder; single media a file. VERSION.NFO is not left in the scannable ROM tree.
Scope: layer-1+

## FR-GB64-004 Conditional import, resume, force, and markers

Prepare runs Gb64Import only when C64 library media is missing (no media files and no .gb64-library-built). Importer persists .gb64-import-state.txt (Unique-ID keys) so a crash is resumable. ForceLibraryBuild rewrites state and re-imports. Success requires the game marker file.

Acceptance:
1. Second prepare with media present reports SKIP.
2. Force re-imports.
3. Mid-run crash resume skips IDs already in the state file.
4. Importer nonzero exit fails prepare.
Scope: layer-1+

## FR-GB64-005 SID NFO fixer against HVSC

Gb64Import --fix-sids shall index HVSC basenames under runtime/library/hvsc and rewrite VERSION.NFO SID: fields: keep if resolvable; remap if the SID exists under a different HVSC-relative path; set SID: (None) if unresolvable. Optional dry-run and receipt log runtime/library/sid-nfo-fix-receipt.txt.

Acceptance:
1. A miss-located but existing SID path is rewritten to the actual HVSC-relative path.
2. A missing SID is cleared to (None).
3. Dry-run does not modify ZIPs.
Scope: layer-1+

## FR-GB64-006 Asset path validation

Gb64Import --validate-assets shall check that each NFO Screenshot: and SID: relative path resolves under attached library/screenshots and library/hvsc (same rules as Resolve-ScreenshotPath.ps1 / Resolve-SidPath.ps1) and report counts of ok/missing.

Acceptance:
1. Declared screenshot that exists under letter folders counts ShotOk.
2. Declared SID that exists under HVSC counts SidOk.
3. Missing paths are sampled in the report.
Scope: layer-1+

## FR-HVSC-001 HVSC lives on attached library storage

The High Voltage SID Collection (HVSC) shall live on the host under runtime/library/hvsc (container /romm/library/hvsc). Layout includes MUSICIANS/, GAMES/, DEMOS/. HVSC is gitignored, not a RomM platform under roms/, and not baked into Docker image layers.

Acceptance:
1. Compose bind makes HVSC visible at /romm/library/hvsc.
2. Dockerfile does not COPY or RUN an HVSC fetch into the image.
3. SID: MUSICIANS\W\Whittaker_David\180.sid resolves under that root.
Scope: layer-1+

## FR-HVSC-002 HVSC download and normalize

Operators fetch HVSC with scripts/Download-Hvsc.ps1 which runs tools/HvscFetch (C#, no Python). Default dest is runtime/library/hvsc. The tool discovers the archive URL (or HVSC_URL), downloads, extracts, and normalizes so MUSICIANS/GAMES/DEMOS sit at the dest root. HVSC_SKIP_DOWNLOAD=1 reuses a cached archive in HVSC_WORK_DIR.

Acceptance:
1. Default dest is runtime/library/hvsc.
2. After a successful fetch, MUSICIANS exists at dest.
3. Nonzero HvscFetch exit fails the script.
Scope: layer-1+

## FR-HVSC-003 Prepare does not download HVSC

Prepare-RomMLibrary.ps1 shall not download HVSC. If library/hvsc lacks MUSICIANS or any .sid, it logs WARN and the Download-Hvsc command line.

Acceptance:
1. Prepare with missing HVSC still creates platform dirs and does not throw solely for missing HVSC.
2. Log contains WARN and points at Download-Hvsc.ps1.
Scope: layer-1+

## FR-ROMM-001 Structure A and built-in Commodore platforms

The RomM stack shall treat upstream RomM as the library authority. Storage follows Structure A: library/roms/{platform}/. First-class Commodore roots use built-in slugs only: c64, c128, c-plus-4, vic-20. Optional: c16, cpet, commodore-cdtv. Do not invent aliases (plus4, vic20) as folder names.

Acceptance:
1. Host runtime/library/roms/ uses only RomM slugs.
2. Empty platform folders persist across container recreate.
3. Custom side folders remap via config.yml to a built-in slug or are retired.
Scope: layer-1+

## FR-ROMM-002 Distinct C64, C128, Plus/4, and VIC-20 libraries

The operator library shall include distinct Structure A trees roms/c64/, roms/c128/, roms/c-plus-4/, and roms/vic-20/. Content is placed in the matching platform. Host paths under runtime/library/roms/{slug}/ exist and are bind-mounted.

Acceptance:
1. Compose up lists those four directories on host and at /romm/library/roms/.
2. A probe file under host roms/vic-20/ is visible in the container.
3. VIC-20 or Plus/4 titles are not dumped into c64.
Scope: layer-1+

## FR-ROMM-003 Preserve config on redeploy

Redeploy shall preserve .env, runtime/config/config.yml, runtime/assets, runtime/library, runtime/csdb, host HVSC, and gb64. Prepare-RomMLibrary.ps1 never overwrites a non-empty config.yml. GB64 game import and screenshot sync run only when the corresponding attached library data is missing, unless ForceLibraryBuild.

Acceptance:
1. Existing non-empty config.yml is byte-identical after redeploy.
2. Populated roms/c64 or marker .gb64-library-built skips game import.
3. Empty roms/c64 plus gb64/Games runs Gb64Import.
4. Populated library/screenshots or marker .screenshots-synced skips screenshot robocopy.
Scope: layer-1+

## FR-ROMM-004 HVSC and screenshots on the attached library

NFO SID: and Screenshot: relative paths resolve against attached library roots library/hvsc and library/screenshots under runtime/library (container /romm/library). HVSC is not baked into the image and is not a RomM platform under roms/.

Acceptance:
1. Compose mounts ./runtime/library to /romm/library including both roots.
2. Resolve-SidPath finds a fixture SID under library/hvsc.
3. Resolve-ScreenshotPath finds Letter/file.png under library/screenshots.
Scope: layer-1+

## FR-ROMM-005 REST heartbeat and authentication

The RomM HTTP API on port 8080 shall expose GET /api/heartbeat and accept authenticated calls with Authorization: Bearer (client token rmm_...) or the documented OAuth/password flow. Tokens must not appear in URL query strings. Unauthenticated protected routes return 401.

Acceptance:
1. Heartbeat returns JSON without requiring a ROM download.
2. Bearer token is sent on protected /api/* calls.
3. 401 when the token is missing or invalid.
Scope: layer-1+

## FR-ROMM-006 Platforms API

GET /api/platforms shall list platforms with numeric id and slug. GET /api/platforms/{id} shall return one platform. Slugs include c64, c128, c-plus-4, vic-20 when those libraries exist.

Acceptance:
1. List returns JSON objects with id and slug.
2. Required slugs resolve to numeric ids.
3. Get-by-id matches list.
Scope: layer-1+

## FR-ROMM-007 ROM list, search, page, and char index

GET /api/roms shall support search_term, platform_ids, limit, offset, order_by, order_dir. The page payload shall include items, total, offset, and a character index suitable for A-Z jump.

Acceptance:
1. platform_ids scopes results.
2. search_term filters by name.
3. limit/offset page the result.
4. Response includes total and a char index for letters that have titles.
Scope: layer-1+

## FR-ROMM-008 ROM detail

GET /api/roms/{id} shall return detailed ROM metadata including files (fs_name), cover fields, summary, and enough data to decide launchability.

Acceptance:
1. Detail includes at least one file with fs_name when the ROM has content.
2. Cover URL or path fields are present when artwork exists.
3. Unknown id returns 404.
Scope: layer-1+

## FR-ROMM-009 ROM content download

Authenticated download of a ROM file by id and file name shall stream bytes.

Acceptance:
1. Download returns the file stream for a known id and fs_name.
2. Missing file returns 404.
3. Download requires auth on the content endpoint.
Scope: layer-1+

## FR-ROMM-010 Library scan task

The server shall expose a tasks API so a client can trigger a library scan after ingest and poll status until complete.

Acceptance:
1. Scan can be started via the documented tasks endpoint.
2. Status can be polled until finished.
3. Newly ingested files appear in /api/roms after scan completes.
Scope: layer-1+

## FR-ROMM-011 Collections (lists)

The server shall persist user collections (/api/collections): list, create, rename, delete, add roms, remove roms. Smart/virtual collections are read-only.

Acceptance:
1. Create returns a collection id.
2. Add/remove accept rom_ids.
3. Smart/virtual collections cannot be mutated.
Scope: layer-1+

## FR-ROMM-012 Cover art fetch

Cover images shall be fetchable. Public url_cover may be unauthenticated. Server path_cover_* resources under the RomM assets prefix require the same Bearer token as the API.

Acceptance:
1. url_cover (when set) fetches without a token.
2. Authenticated cover paths require Bearer.
3. Missing artwork does not 500 the ROM list.
Scope: layer-1+


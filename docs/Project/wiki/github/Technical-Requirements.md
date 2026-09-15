# Technical Requirements (MCP Server)

## TR-CSDB-ARCH-001

**CSDb sidecar, not a RomM fork** — CSDb is csdb-bridge (ASP.NET) on :8090 and/or in-process RomM.Client.Csdb. Do not fork RomM Python core. Share Structure A LIBRARY_ROMS_ROOT.
**Covered by:** FR: FR-CSDB-001, FR-CSDB-002; TEST: TEST-CSDB-001, TEST-CSDB-002, TEST-CSDB-003
**Status:** pending
Scope: layer-1+

## TR-CSDB-INGEST-001

**Ingest pipeline** — POST /csdb/v1/ingest accepts 1..20 {kind, csdb_id} items. Extract archives. Rate-limit CSDb HTTP. Optional RomM scan after write.
**Covered by:** FR: FR-CSDB-002, FR-CSDB-003; TEST: TEST-CSDB-002, TEST-CSDB-003, TEST-HVSC-001
**Status:** pending
Scope: layer-1+

## TR-GB64-IMPORT-001

**C# Gb64Import** — Game import is tools/Gb64Import (.NET), invoked by prepare via dotnet run. No Python. Args: --games, --out, --hvsc, --screenshots, --force, --limit, --fix-sids, --validate-assets, --dry-run.
**Covered by:** FR: FR-GB64-001, FR-GB64-003, FR-GB64-004, FR-ROMM-003; TEST: TEST-GB64-001, TEST-GB64-004, TEST-GB64-003, TEST-GB64-002, TEST-ROMM-003
**Status:** pending
Scope: layer-1+

## TR-GB64-SIDFIX-001

**SID field repair** — SidNfoFixer builds an HVSC basename index and rewrites NFO SID: inside GB64 ZIPs (remap or (None)). Receipt at runtime/library/sid-nfo-fix-receipt.txt unless overridden.
**Covered by:** FR: FR-GB64-005; TEST: TEST-GB64-005
**Status:** pending
Scope: layer-1+

## TR-GB64-STATE-001

**Import resume and markers** — Importer state file .gb64-import-state.txt under roms/c64 (Unique-ID per line). Prepare game marker roms/.gb64-library-built. Screenshot marker library/screenshots/.screenshots-synced. Force clears import state.
**Covered by:** FR: FR-GB64-002, FR-GB64-004, FR-ROMM-003; TEST: TEST-GB64-002, TEST-GB64-004, TEST-ROMM-003
**Status:** pending
Scope: layer-1+

## TR-GB64-TAG-001

**Tag mapping from VERSION.NFO** — Filename/folder tags follow docs/gb64-romm-tag-mapping.md and RomTagBuilder: Unique-ID (gb64-{id}), GB-Version (rev-NN), Language split on /, Pal/NTSC to region+video tags, True Drive Emul. Yes -> (TrueDrive).
**Covered by:** FR: FR-GB64-003; TEST: TEST-GB64-001, TEST-GB64-003
**Status:** pending
Scope: layer-1+

## TR-GB64-VALID-001

**Asset path validator** — AssetPathValidator applies the same relative-path rules as Resolve-SidPath.ps1 and Resolve-ScreenshotPath.ps1 against attached library roots.
**Covered by:** FR: FR-GB64-006; TEST: TEST-GB64-006
**Status:** pending
Scope: layer-1+

## TR-HVSC-FETCH-001

**HvscFetch normalize** — HvscFetch uses HVSC version API https://www.hvsc.c64.org/api/v1/version/7z unless HVSC_URL is set, extracts with SharpCompress, and flattens so HVSC trees are at dest root. Timeout 30 minutes. User-Agent RomM-GB64-HVSC/1.0.
**Covered by:** FR: FR-HVSC-002; TEST: TEST-HVSC-002
**Status:** pending
Scope: layer-1+

## TR-HVSC-HOST-001

**Host HVSC tool** — scripts/Download-Hvsc.ps1 and tools/HvscFetch write to runtime/library/hvsc by default.
**Covered by:** FR: FR-CSDB-003, FR-GB64-005, FR-HVSC-001, FR-HVSC-002, FR-HVSC-003, FR-ROMM-004; TEST: TEST-CSDB-003, TEST-HVSC-001, TEST-GB64-005, TEST-ROMM-004, TEST-HVSC-002, TEST-HVSC-003
**Status:** pending
Scope: layer-1+

## TR-ROMM-API-001

**Pinned REST surface** — Server HTTP is RomM 5.x compatible with pinned snapshot openapi/romm-5.0.0.json in F:\GitHub\RomM. Required paths: /api/heartbeat, /api/platforms, /api/roms, ROM content download, tasks/scan, collections. JSON field names match RomM.Client (search_term, platform_ids, fs_name, url_cover).
**Covered by:** FR: FR-ROMM-005, FR-ROMM-006, FR-ROMM-007, FR-ROMM-008, FR-ROMM-009, FR-ROMM-010, FR-ROMM-012; TEST: TEST-ROMM-005
**Status:** pending
Scope: layer-1+

## TR-ROMM-AUTH-001

**Bearer and 401** — Protected /api routes require Authorization: Bearer. Client tokens use the rmm_ prefix when issued as API keys. Tokens never appear in query strings. 401 on missing or invalid credentials.
**Covered by:** FR: FR-ROMM-005, FR-ROMM-009, FR-ROMM-012; TEST: TEST-ROMM-005
**Status:** pending
Scope: layer-1+

## TR-ROMM-CFG-001

**config.yml remaps only when needed** — Preferred end state: no staging folders. If staging remains, each remap target is a built-in slug.
**Covered by:** FR: FR-ROMM-001; TEST: TEST-ROMM-001, TEST-ROMM-002
**Status:** pending
Scope: layer-1+

## TR-ROMM-COLL-001

**Collections REST** — Collections use /api/collections with scopes collections.read and collections.write. Mutating a smart/virtual collection is rejected.
**Covered by:** FR: FR-ROMM-011; TEST: TEST-ROMM-006
**Status:** pending
Scope: layer-1+

## TR-ROMM-DEPLOY-001

**Preserve-and-prepare** — Deploy stashes and restores .env, runtime/config, runtime/library, runtime/assets, runtime/csdb, hvsc, gb64. Prepare-RomMLibrary.ps1 creates required platform dirs and imports GB64 only when roms/c64 lacks media.
**Covered by:** FR: FR-HVSC-003, FR-ROMM-003; TEST: TEST-HVSC-003, TEST-GB64-002, TEST-GB64-004, TEST-ROMM-003
**Status:** pending
Scope: layer-1+

## TR-ROMM-MEDIA-001

**HVSC and screenshot roots** — HVSC_ROOT=/romm/library/hvsc, SCREENSHOTS_ROOT=/romm/library/screenshots. csdb-bridge uses /data/library/hvsc and /data/library/screenshots on the same host bind. Dockerfile does not bake HVSC.
**Covered by:** FR: FR-GB64-002, FR-GB64-006, FR-HVSC-001, FR-ROMM-004; TEST: TEST-GB64-002, TEST-GB64-006, TEST-HVSC-001, TEST-ROMM-004
**Status:** pending
Scope: layer-1+

## TR-ROMM-PLAT-001

**Built-in Commodore slugs** — Use exact slugs c64, c128, c-plus-4, vic-20. Optional: c16, cpet, commodore-cdtv. config.yml must not redefine those identities. Temporary c64-csdb-* folders remap to c64.
**Covered by:** FR: FR-ROMM-001, FR-ROMM-002, FR-ROMM-006; TEST: TEST-ROMM-001, TEST-ROMM-002, TEST-ROMM-005
**Status:** pending
Scope: layer-1+

## TR-ROMM-STRUCT-001

**Structure A layout** — RomM storage is runtime/library/roms/{slug}/ on the host, /romm/library/roms/{slug}/ in the container. docker-compose binds ./runtime/library to /romm/library.
**Covered by:** FR: FR-GB64-001, FR-ROMM-001, FR-ROMM-002; TEST: TEST-GB64-001, TEST-GB64-004, TEST-ROMM-001, TEST-ROMM-002
**Status:** pending
Scope: layer-1+


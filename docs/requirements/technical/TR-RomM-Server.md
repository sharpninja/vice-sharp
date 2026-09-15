# RomM server technical requirements

## TR-ROMM-STRUCT-001: Structure A layout

RomM storage is `runtime/library/roms/{slug}/` on the host, `/romm/library/roms/{slug}/` in the container. docker-compose binds `./runtime/library` to `/romm/library`.

## TR-ROMM-PLAT-001: Built-in Commodore slugs

Use exact slugs `c64`, `c128`, `c-plus-4`, `vic-20`. Optional: `c16`, `cpet`, `commodore-cdtv`. config.yml must not redefine those identities. Temporary `c64-csdb-*` folders remap to `c64`.

## TR-ROMM-CFG-001: config.yml remaps only when needed

Preferred end state: no staging folders. If staging remains, each remap target is a built-in slug.

## TR-ROMM-DEPLOY-001: Preserve-and-prepare

Deploy stashes and restores `.env`, `runtime/config`, `runtime/library`, `runtime/assets`, `runtime/csdb`, `hvsc`, `gb64`. `Prepare-RomMLibrary.ps1` creates required platform dirs and imports GB64 only when `roms/c64` lacks media.

## TR-ROMM-MEDIA-001: HVSC and screenshot roots

`HVSC_ROOT=/romm/library/hvsc`, `SCREENSHOTS_ROOT=/romm/library/screenshots`. csdb-bridge uses `/data/library/hvsc` and `/data/library/screenshots` on the same host bind. Dockerfile does not bake HVSC.

## TR-ROMM-API-001: Pinned REST surface

Server HTTP is RomM 5.x compatible with the pinned snapshot `openapi/romm-5.0.0.json` in `F:\GitHub\RomM`. Required paths: `/api/heartbeat`, `/api/platforms`, `/api/platforms/{id}`, `/api/roms`, `/api/roms/{id}`, ROM content download, tasks/scan, collections. JSON field names match `RomM.Client` (`search_term`, `platform_ids`, `fs_name`, `url_cover`, `char_index` when advertised).

## TR-ROMM-AUTH-001: Bearer and 401

Protected `/api` routes require `Authorization: Bearer`. Client tokens use the `rmm_` prefix when issued as API keys. Tokens never appear in query strings. 401 on missing/invalid credentials.

## TR-ROMM-COLL-001: Collections REST

Collections use `/api/collections` with scopes `collections.read` / `collections.write`. Mutating a smart/virtual collection is rejected.

## TR-CSDB-ARCH-001: CSDb sidecar, not a RomM fork

CSDb is `csdb-bridge` (ASP.NET) on :8090 and/or in-process `RomM.Client.Csdb`. Do not fork RomM Python core. Share Structure A `LIBRARY_ROMS_ROOT`.

## TR-CSDB-INGEST-001: Ingest pipeline

`POST /csdb/v1/ingest` accepts 1..20 `{kind, csdb_id}` items. Extract archives. Rate-limit CSDb HTTP. Optional RomM scan after write.

## TR-HVSC-HOST-001: Host HVSC tool

`scripts/Download-Hvsc.ps1` and `tools/HvscFetch` write to `runtime/library/hvsc` by default.

## TR-GB64-IMPORT-001: C# Gb64Import

Game import is `tools/Gb64Import` (.NET), invoked by prepare via `dotnet run --project tools/Gb64Import`. No Python. Args: `--games`, `--out`, `--hvsc`, `--screenshots`, `--force`, `--limit`, `--fix-sids`, `--validate-assets`, `--dry-run`. Media extensions for "has library media" include d64/g64/t64/tap/crt/prg/p00/sid/zip/7z/rar.

## TR-GB64-TAG-001: Tag mapping from VERSION.NFO

Filename/folder tags follow `docs/gb64-romm-tag-mapping.md` and `RomTagBuilder`: Unique-ID `(gb64-{id})`, GB-Version `(rev-NN)`, Language split on `/`, Pal/NTSC to region+video tags, True Drive Emul. Yes -> `(TrueDrive)`. ZIP stem `{SHORTNAME}_{id}_{rev}.zip` supplies fallback id/rev.

## TR-GB64-STATE-001: Import resume and markers

Importer state file `.gb64-import-state.txt` under `roms/c64` (Unique-ID per line). Prepare game marker `roms/.gb64-library-built`. Screenshot marker `library/screenshots/.screenshots-synced`. Force clears import state.

## TR-GB64-SIDFIX-001: SID field repair

`SidNfoFixer` builds an HVSC basename index and rewrites NFO `SID:` inside GB64 ZIPs (remap or `(None)`). Receipt at `runtime/library/sid-nfo-fix-receipt.txt` unless overridden.

## TR-GB64-VALID-001: Asset path validator

`AssetPathValidator` applies the same relative-path rules as `Resolve-SidPath.ps1` and `Resolve-ScreenshotPath.ps1` against attached library roots.

## TR-HVSC-FETCH-001: HvscFetch normalize

`HvscFetch` uses HVSC version API `https://www.hvsc.c64.org/api/v1/version/7z` unless `HVSC_URL` is set, extracts with SharpCompress, and flattens so HVSC trees are at dest root. Timeout 30 minutes. User-Agent `RomM-GB64-HVSC/1.0`.

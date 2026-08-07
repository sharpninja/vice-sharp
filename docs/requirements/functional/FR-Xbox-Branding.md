# Functional Requirements - Xbox / product branding and third-party marks

## Document Information

| Field | Value |
| --- | --- |
| Project | ViceSharp |
| Area | XBOXGPL / branding |
| Last Updated | 2026-08-06 |
| Status | Active |

MCP store is authoritative. This file is the canonical markdown projection for brand-mark FRs.

---

## FR-XBOXGPL-007: Official Commodore C= logo source and CC BY-SA 4.0 attribution

**Priority:** high  
**Status:** completed  
**Area:** XBOXGPL

### Description

When ViceSharp product branding uses the Commodore C= logo mark, the logo asset must be the officially published Wikimedia Commons file **Commodore C= logo.svg** (CC BY-SA 4.0, author **Alien426**):

https://commons.wikimedia.org/wiki/File:Commodore_C%3D_logo.svg

The project must retain a local copy of the source SVG (and any rasterization used for packaging) plus a written attribution. Attribution text must be disclosed in:

1. `THIRD_PARTY_NOTICES.md`
2. Every NuGet package payload (root `THIRD_PARTY_NOTICES.md` via `Directory.Build.props`)
3. Product and package `README.md` files
4. About dialogs for Xbox UWP and Avalonia desktop heads (`AboutInfo.LogoAttributionText`)

ShareAlike obligations apply to derivative branding that includes the mark. This requirement is separate from **FR-XBOXGPL-006** (GPL/VICE license payload and no-ROM packaging).

### Acceptance criteria

| ID | Criterion | Evidence |
| --- | --- | --- |
| ac-logo-source | Branding assets use the Wikimedia C= logo (or a direct rasterization), not an unattributed redraw | `docs/xbox/store-screenshots/Commodore_C_logo.svg`, `LOGO-ATTRIBUTION-CC-BY-SA-4.0.txt` |
| ac-logo-notices | `THIRD_PARTY_NOTICES.md` names file, Alien426, CC BY-SA 4.0, commons URL | `THIRD_PARTY_NOTICES.md`; `XboxGplComplianceTests` |
| ac-logo-nuget | Every NuGet package packs `THIRD_PARTY_NOTICES.md` at package root | `Directory.Build.props` |
| ac-logo-readme | Root and package READMEs include C= attribution | `README.md`; `src/**/README.md` |
| ac-logo-about | About surfaces expose logo attribution with CC BY-SA 4.0, Alien426, commons URL | `AboutInfo.cs`; Xbox `AboutPage`; Avalonia About; `AboutViewModelTests` |

### Traceability

| Kind | ID |
| --- | --- |
| TR | TR-XBOXGPL-BRAND-001 |
| TEST | TEST-XBOXGPL-002 |

### Related

- FR-XBOXGPL-006 (MSIX GPL compliance payload)
- FR-XBOXUI-008 (About GPL / VICE / source offer; extended by logo line on About UI)

# GameBase64 → RomM Tag Mapping

Maps metadata from GameBase64 (GB64) game packages to RomM filename tags, multi-file folder tags, and related library conventions.

**Sources of GB64 metadata (per game ZIP):**

| Source | Location | Role |
|--------|----------|------|
| Package name | `{SHORTNAME}_{UniqueID}_{GB-Version}.zip` | Stable ID + revision |
| `VERSION.NFO` | Inside each ZIP (header + GAME INFO + VERSION INFO) | Primary structured tags |
| Screenshot path | NFO `Screenshot:` field | Link to `./gb64/Screenshots/...` |
| SID path | NFO `SID:` field | HVSC-relative path; resolves under `/romm/library/hvsc/` in the container (see `docs/hvsc-in-container.md`) |

**RomM tag mechanisms** (official folder-structure docs):

| Mechanism | Syntax | Use |
|-----------|--------|-----|
| Language | `(En)`, `(De)`, … or full name | Built-in language codes |
| Region | `(E)`, `(U)`, `(PD)`, … | Built-in region codes |
| Custom region/language | `(reg-MyLabel)` / `(reg MyLabel)` | Values without a built-in code |
| Revision | `(rev-01)`, `(rev v1)` | Version / GB-Version |
| Arbitrary tags | `(Trainers-3)`, `(PAL)`, … | Everything else in `()` or `[]` |
| Multi-file subfolders | `hack/`, `manual/`, `prototype/`, `demo/`, `translation/`, … | Structural tags under a game folder |
| Provider force-match | `(igdb-1234)` | Optional; not derived from GB64 alone |

Recommended output shape when extracting into the image:

```text
/romm/library/roms/c64/
  {SanitizedName} (En) (E) (rev-01) (gb64-12134) (PAL).t64
  {SanitizedName} (De) (rev-02) (gb64-14766) (TrueDrive)/
    disk1.d64
    disk2.d64
```

`VERSION.NFO` is **not** left in the scannable ROM tree (metadata is applied to the **filename** / folder name at extract time).

---

## 1. Package / filename fields

| GB64 field | Example | RomM mapping | Output example | Notes |
|------------|---------|--------------|----------------|-------|
| `SHORTNAME` (ZIP stem prefix) | `4ACESPIN` | Base title **fallback** only | Prefer NFO `Name:` for display base | Shortname is GameBase file key, often truncated |
| `Unique-ID` / ZIP middle number | `12134` | Arbitrary tag `(gb64-{id})` | `(gb64-12134)` | Stable cross-ref; also ties screenshots if needed |
| `GB-Version` / ZIP suffix | `01`, `02` | Revision `(rev-{NN})` | `(rev-01)`, `(rev-02)` | Zero-pad to 2 digits from ZIP/NFO |
| ZIP internal media names | `A-MAZIN1.T64` | File base if single media; else files inside multi-file folder | Keep media extension | Multi-disk ZIPs → one game **folder** |

**Base display name:** use NFO `Name:` (sanitized for filesystem: replace `/ \ : * ? " < > |` and collapse spaces). Append RomM tags after the name.

---

## 2. GAME INFO block → RomM tags

### 2.1 `Language` → language tag(s)

RomM built-in codes: `Ar Da De El En Es Fi Fr It Ja Ko Nl No Pl Pt Ru Sr Sv Zh nolang`.

| GB64 `Language` value (observed) | RomM tag(s) | Notes |
|----------------------------------|-------------|-------|
| `English` | `(En)` | |
| `German` | `(De)` | |
| `Italian` | `(It)` | |
| `Spanish` | `(Es)` | |
| `Dutch` | `(Nl)` | |
| `French` | `(Fr)` | |
| `Finnish` | `(Fi)` | |
| `Swedish` | `(Sv)` | |
| `Norwegian` | `(No)` | |
| `Polish` | `(Pl)` | |
| `Danish` | `(Da)` | |
| `Arabic` | `(Ar)` | |
| `Hungarian` | `(reg-Hungarian)` | No built-in Hu code in RomM list |
| `Czech` | `(reg-Czech)` | No built-in |
| `Slovenian` | `(reg-Slovenian)` | No built-in |
| `Serbo-Croatian` | `(Sr)` | Closest built-in; or `(reg-Serbo-Croatian)` |
| `(No Text)` | `(nolang)` | |
| `English / German` | `(En)(De)` | Emit one tag per language |
| `English / Italian` | `(En)(It)` | |
| `English / Finnish` | `(En)(Fi)` | |
| `English / Spanish` | `(En)(Es)` | |
| `English / French` | `(En)(Fr)` | |
| `English / Polish` | `(En)(Pl)` | |
| `English / French / German / Italian / Spanish` | `(En)(Fr)(De)(It)(Es)` | Split on `/` |
| `English / French / German / Spanish` | `(En)(Fr)(De)(Es)` | |
| `English / Serbo-Croatian` | `(En)(Sr)` | |
| Unknown / empty | omit language tags | Do not invent |

**Rule:** split on `/`, trim, map each token; unknown tokens → `(reg-{TokenWithoutSpaces})`.

### 2.2 `Pal/NTSC` → region / video tags

GB64 does not use classic (U)/(J) cartridge regions. Map video standard + optional Europe bias:

| GB64 `Pal/NTSC` | RomM tags | Rationale |
|-----------------|-----------|-----------|
| `PAL` | `(E) (PAL)` | Europe region + explicit video tag |
| `NTSC` | `(U) (NTSC)` | USA region + explicit video tag |
| `PAL+NTSC` | `(W) (PAL) (NTSC)` | World + both standards |
| `PAL(+NTSC?)` | `(E) (PAL) (NTSC-maybe)` | Dominant PAL; uncertain NTSC as arbitrary tag |

If you prefer fewer tags: map only video as arbitrary `(PAL)` / `(NTSC)` / `(PAL+NTSC)` and skip region codes.

### 2.3 `Published` → region / structural tags (heuristic)

| Pattern in `Published` | RomM mapping | Notes |
|------------------------|--------------|-------|
| Contains `(Public Domain)` | `(PD)` | Built-in region PD |
| Contains `(Preview)` or name has `[Preview]` / `[Peview]` | multi-file folder tag **or** filename `(prototype)` / parent folder `prototype/` | RomM multi-file tag `prototype` |
| Contains `(Not Published)` | `(Unreleased)` arbitrary | |
| Year + publisher only | no region tag from this field | Metadata providers can enrich |
| Contains `PD` / public domain wording | `(PD)` | |

### 2.4 `Name` → base filename + optional structural hints

| Pattern in `Name` | RomM mapping |
|-------------------|--------------|
| Contains `[Preview]`, `[Peview]`, `[Demo]` | `(prototype)` or `(demo)` arbitrary; or extract into `demo/` / `prototype/` subfolder if multi-file |
| Contains `Demo` as standalone word | `(demo)` |
| Normal title | Sanitized base name only |

### 2.5 `Genre` → arbitrary tags

RomM has no first-class genre filename codes. Map to a single sanitized arbitrary tag:

| Rule | Example GB64 | RomM tag |
|------|--------------|----------|
| Take full genre string; replace spaces and `/` with `-`; strip quotes | `Arcade - Pinball` | `(Arcade-Pinball)` |
| Uncategorized | `[uncategorized]` | omit or `(uncategorized)` |
| Optional: also emit top-level family only | `Shoot'em Up - H-Scrolling` | `(Shoot-em-Up)` **or** full tag |

**Recommendation:** one full-genre tag only, to avoid noise. Genre is better as RomM metadata from providers after match; treat GB64 genre as **optional** filename tag `(gb64-genre-…)`.

### 2.6 `Players` → arbitrary tags

| GB64 `Players` | RomM tag(s) |
|----------------|-------------|
| `1P Only` | `(1P)` |
| `2P Only` | `(2P)` |
| `4P Only` | `(4P)` |
| `0P Only` | `(0P)` |
| `1 - 2` | `(1-2P)` |
| `1 - 2 (Simultaneous)` | `(1-2P) (Simultaneous)` |
| `1 - 4` | `(1-4P)` |
| `1 - 4 (Simultaneous)` | `(1-4P) (Simultaneous)` |
| `2P Only (Simultaneous)` | `(2P) (Simultaneous)` |
| `1 - 3 (Simultaneous)` | `(1-3P) (Simultaneous)` |
| `(Unknown)` | omit |
| Other ranges `A - B` | `({A}-{B}P)` + `(Simultaneous)` if present |

### 2.7 `Control` → arbitrary tags

| GB64 `Control` | RomM tag |
|----------------|----------|
| `Joystick Port 2` | `(Joy2)` |
| `Joystick Port 1` | `(Joy1)` |
| `Keyboard` | `(Keyboard)` |
| `Light Gun` | `(LightGun)` |
| `Light Pen` | `(LightPen)` |
| `Mouse` | `(Mouse)` |

### 2.8 Credits / free text (optional, usually omit from filename)

| GB64 field | RomM mapping | Default |
|------------|--------------|---------|
| `Developer` | omit from filename (too long / often `(Unknown)`) | Skip unless value is short and not Unknown |
| `Coding`, `Graphics`, `Music` | omit | Use providers or sidecar JSON if needed |
| `Comment` (GAME INFO) | omit from filename | May inform `demo`/`prototype` heuristics |
| `Unique-ID` | `(gb64-{id})` | **Always emit** (see package fields) |

---

## 3. VERSION INFO block → RomM tags

| GB64 field | Values | RomM mapping | Notes |
|------------|--------|--------------|-------|
| `GB-Version` | `1`, `2`, … | `(rev-01)` etc. | Prefer ZIP suffix / this field |
| `Cracked/Crunched` | `(None)` | omit | |
| `Cracked/Crunched` | group name e.g. `Fairlight (FLT)` | `(Cracked) (FLT)` or `(crack-FLT)` | Prefer short group code in parens if present; else sanitized group name |
| `Cracked/Crunched` | multi e.g. `Chromance (CHR) / (None)` | Parse first non-`(None)` group | |
| `Trainers` | `0` | omit | |
| `Trainers` | `N` > 0 | `(Trainers-{N})` and optional multi-file `hack/` if you split trained builds | RomM multi-file tag `hack` is structural |
| `High Score Saver` | `Yes` | `(HiscoreSaver)` | |
| `High Score Saver` | `No` | omit | |
| `Loading Screen` | `Yes` | `(LoadingScreen)` | |
| `Loading Screen` | `No` | omit | |
| `Included Docs` | `Yes` | Prefer multi-file folder `manual/` if docs extracted; else `(Docs)` | Aligns with RomM `manual` subfolder tag |
| `Included Docs` | `No` | omit | |
| `True Drive Emul.` | `Yes` | `(TrueDrive)` | Important for C64 disk accuracy |
| `True Drive Emul.` | `No` | omit | |
| `Game Length` | `73 Blocks`, `1 Disk`, … | optional `(Len-73Blocks)` | Usually omit (noise) |
| `Comment` (VERSION) | free text | Heuristic only (e.g. “trained”) | Do not dump full comment into filename |

### Cracked/Crunched normalization

```text
(None)                    → (no tag)
Fairlight (FLT)           → (Cracked) (FLT)
Triad (3AD)               → (Cracked) (3AD)
Rio Baan                  → (Cracked) (Rio-Baan)
Chromance (CHR) / (None)  → (Cracked) (CHR)
```

---

## 4. Header block (paths, not RomM ROM tags)

| GB64 field | Example | Mapping |
|------------|---------|---------|
| `Screenshot` | `A\Alfabug.png` | Resolve under **attached** `runtime/library/screenshots/A/Alfabug.png` (and `_N` siblings). Source copy from `./gb64/Screenshots` via `Prepare-RomMLibrary.ps1`. Container: `/romm/library/screenshots/…` (`SCREENSHOTS_ROOT`). |
| `Filename` | `a1\ALFABUG_10690_01.zip` | Source package path under `./gb64/Games`; not a RomM tag |
| `SID` | `MUSICIANS\P\...\Alderan.sid` | Resolve under **attached** `runtime/library/hvsc/` + path (`\` → `/`). Container: `/romm/library/hvsc/…` (`HVSC_ROOT`). Download with `scripts/Download-Hvsc.ps1`. Not under `roms/c64`. |
| `GB-Version` | `1` | Same as revision mapping |

Runtime path roots (must be on bind-mounted library storage):

```text
runtime/library/                    →  /romm/library
  roms/c64/…                        games (Structure A)
  screenshots/{Letter}/{file}.png   Screenshot: Letter\file.png
  hvsc/MUSICIANS/…                  SID: MUSICIANS\…
```

Screenshot staging:

1. Operator keeps or copies GameBase under `./gb64` (source).  
2. `Prepare-RomMLibrary.ps1` syncs `gb64/Screenshots` → `runtime/library/screenshots` when library screenshots are missing.  
3. Resolvers use `SCREENSHOTS_ROOT` / `scripts/Resolve-ScreenshotPath.ps1`. Keep `(gb64-{id})` on ROM filenames as join key.

---

## 5. Multi-file / structural tags (RomM folders)

Use when extract produces a **directory** per game (multi-disk or extras):

| Condition | RomM folder / tag |
|-----------|-------------------|
| ZIP contains multiple `.d64`/`.t64`/`.crt` | Game folder with disk files at folder root (not letter buckets) |
| `Included Docs: Yes` and separate doc files exist | `manual/` subfolder |
| `Trainers` > 0 and you ship a distinct trained build | `hack/` subfolder **or** filename `(Trainers-N)` |
| Preview / unreleased | `prototype/` or `(prototype)` |
| Demo | `demo/` or `(demo)` |
| Fan translation (if detected in Name/Comment) | `translation/` or `(translation)` |

RomM recognized multi-file subfolder names: `dlc`, `hack`, `manual`, `mod`, `patch`, `update`, `demo`, `translation`, `prototype`.

---

## 6. Default emit profile (recommended)

To keep filenames usable, emit this **minimum** set by default:

```text
{Name} ({Lang…}) ({Video/Region…}) (rev-{GB-Version}) (gb64-{Unique-ID}) {optional flags}.ext
```

**Always:**

- Sanitized `Name`
- Language tag(s)
- Pal/NTSC mapping tags
- `(rev-NN)` from GB-Version
- `(gb64-{Unique-ID})`

**If true:**

- `(TrueDrive)` when True Drive = Yes  
- `(Trainers-N)` when Trainers > 0  
- `(Cracked)` + group code when Cracked/Crunched ≠ `(None)`  
- `(PD)` / `(prototype)` / `(demo)` from Published/Name heuristics  
- `(HiscoreSaver)`, `(Docs)`, `(LoadingScreen)` only if an “extended tags” mode is enabled  

**Usually omit from filename:** Genre, Players, Control, Game Length, developer credits (optional extended profile).

### Example conversions

**Input ZIP:** `4ACESPIN_12134_01.zip`  
**NFO:** Name `4 Aces Pinball`, Language `English`, Pal/NTSC `PAL(+NTSC?)`, Unique-ID `12134`, GB-Version `1`, Trainers `0`, Cracked `(None)`

```text
4 Aces Pinball (En) (E) (PAL) (NTSC-maybe) (rev-01) (gb64-12134).t64
```

**Input ZIP:** `BRUSHUP2_14766_02.zip`  
**NFO:** Name `Brush Up Your English II`, Language `German`, True Drive `Yes`, Unique-ID `14766`, GB-Version `2`

```text
Brush Up Your English II (De) (E) (PAL) (NTSC-maybe) (rev-02) (gb64-14766) (TrueDrive).d64
```

**Input ZIP:** multi-disk with trainers  
**NFO:** Trainers `3`, Cracked `Fairlight (FLT)`, Language `English`

```text
Some Game (En) (E) (PAL) (rev-01) (gb64-99999) (Trainers-3) (Cracked) (FLT)/
  disk0.d64
  disk1.d64
```

---

## 7. Fields with no direct RomM tag

| GB64 | Why unmapped to filename tags | Alternative |
|------|-------------------------------|-------------|
| `Developer`, `Coding`, `Graphics`, `Music` | Long free text; high Unknown rate | Metadata providers / notes |
| `Game Length` | Not a RomM concept | Optional extended tag only |
| `SID` path | Not a ROM | Optional assets path |
| Full `Comment` | Free text | Parse for heuristics only |
| Genre (default profile) | Better from providers | Optional extended tag |
| Letter bucket (`a1`, `s3`) | Sharding only | Never use as RomM folder |

---

## 8. Implementation checklist (extract pipeline)

1. Open each `./gb64/Games/{bucket}/*.zip`.  
2. Parse `VERSION.NFO` (key: value lines under GAME INFO / VERSION INFO / header).  
3. List media entries (`.t64`, `.d64`, `.crt`, …); ignore `VERSION.NFO` in output tree.  
4. Build RomM-tagged base name from the mapping tables (default profile).  
5. If one media file → write `/romm/library/roms/c64/{tagged}{ext}`.  
6. If multiple media files → write `/romm/library/roms/c64/{tagged}/{originalMediaNames}`.  
7. Record `Screenshot:` → host path under `./gb64/Screenshots` for art import.  
8. Never create `roms/c64/a1/` style buckets.

---

## 9. Sample coverage notes (from local collection)

From a 2,000-NFO sample under `./gb64/Games`:

- Languages dominated by English; multi-language strings use `/`.  
- `Pal/NTSC` almost always `PAL(+NTSC?)` or `PAL+NTSC`.  
- `Cracked/Crunched` is `(None)` ~half the time; otherwise scene group names (hundreds of distinct values).  
- `Trainers` is `0` for most titles; non-zero should become `(Trainers-N)`.  
- `Screenshot:` is present on essentially all packages and is the reliable art key.

---

## 10. Revision history

| Date | Change |
|------|--------|
| 2026-07-14 | Initial mapping from VERSION.NFO schema + RomM folder-structure tag rules |

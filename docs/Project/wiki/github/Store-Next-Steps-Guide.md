# ViceSharp Microsoft Store: step-by-step next moves

**Audience:** operator (Payton)  
**Goal:** first live Microsoft Store listing for the **Xbox UWP** head (`ViceSharp.Xbox`), Xbox + Desktop device families.  
**Status baseline:** Phase 0 docs + listing/privacy done (2026-08-05). Partner Center identity, ADO wiring, screenshots, S42 console, and publish still open.

**Avalonia desktop** stays on winget/GitHub MSI for now:  
[winget-pkgs PR #412774](https://github.com/microsoft/winget-pkgs/pull/412774) · [GitHub release v1.2.1](https://github.com/sharpninja/vice-sharp/releases/tag/v1.2.1)

---

## Quick index

| Step | What | ~Time | Blocker? |
| --- | --- | --- | --- |
| [0](#0-read-these-first-15-min) | Read prior decisions | 15 min | No |
| [1](#1-partner-center-account--app-reservation) | Partner Center account + reserve name | 30–60 min | Yes |
| [2](#2-product-identity--copy-store-values) | Copy Store identity values | 10 min | Yes |
| [3](#3-entra-app-for-msstore-cli) | Entra app + Partner Center API access | 20–40 min | Yes |
| [4](#4-azure-devops-variable-group--environment) | ADO secrets + approval env | 15 min | Yes |
| [5](#5-register-the-store-pipeline) | Register `azure-pipelines-xbox-store.yml` | 10 min | Yes |
| [6](#6-fill-the-store-listing) | Listing, privacy URL, IARC | 45–90 min | Yes (first submit) |
| [7](#7-screenshots) | Capture marketing screenshots | 30–60 min | Yes (first submit) |
| [8](#8-optional-local-package--wack) | Local `.msixupload` + WACK | 30–90 min | Recommended |
| [9](#9-s42-console-validation) | Dev Mode console matrix | 1–3 hr | Strongly recommended |
| [10](#10-pipeline-build-then-publish) | Build artifact → approve publish | 30 min + cert wait | Yes |
| [11](#11-after-certification) | Retail install smoke + hygiene | 30 min | Yes for “done” |

---

## 0. Read these first (15 min)

| Doc | Link |
| --- | --- |
| GPL section-6 decision (**GO with mitigations**) | [docs/xbox/gpl-store-section6-review.md](gpl-store-section6-review.md) |
| Privacy policy (listing URL source) | [docs/PRIVACY.md](../PRIVACY.md) · raw: [github.com/.../docs/PRIVACY.md](https://github.com/sharpninja/vice-sharp/blob/main/docs/PRIVACY.md) |
| Listing copy + screenshot table | [docs/xbox/store-listing-copy.md](store-listing-copy.md) |
| Pipeline / package design | [docs/xbox-store-publishing.md](../xbox-store-publishing.md) |
| Full checklist | [docs/xbox/microsoft-store-publishing-checklist.md](microsoft-store-publishing-checklist.md) |
| ADO command cheatsheet | [docs/xbox/ado-store-setup.md](ado-store-setup.md) |
| Console runbook | [docs/xbox/on-console-setup-runbook.md](on-console-setup-runbook.md) |
| Phase 0 receipt | [docs/receipts/store-phase0-20260805.md](../receipts/store-phase0-20260805.md) |
| Pipeline YAML in repo | [azure-pipelines-xbox-store.yml](../../azure-pipelines-xbox-store.yml) |
| Identity stamp script | [build/Set-StoreIdentity.ps1](../../build/Set-StoreIdentity.ps1) |

Public source offer (also in About): [https://github.com/sharpninja/vice-sharp](https://github.com/sharpninja/vice-sharp)

---

## 1. Partner Center account + app reservation

### 1.1 Open Partner Center

1. Sign in: [https://partner.microsoft.com/dashboard](https://partner.microsoft.com/dashboard)  
2. If you need an account: [Microsoft Partner Center overview](https://learn.microsoft.com/windows/apps/publish/partner-center/overview)  
3. Individual developer enrollment is a one-time fee in many regions (see current Partner Center pricing on the enrollment page).

### 1.2 Confirm Xbox eligibility

UWP apps can target Xbox without a separate ID@Xbox / GDK title registration when you are **not** using Xbox Live multiplayer services. Device family is set on the product/packages.

Docs: [Device family support](https://learn.microsoft.com/windows/uwp/xbox-apps/) · [Publish to Xbox](https://learn.microsoft.com/windows/uwp/xbox-apps/publishing)

### 1.3 Create / reserve the app

1. Dashboard → **Apps and games** → **New product** → **MSIX or PWA app**  
   Direct entry (may redirect after login): [https://partner.microsoft.com/dashboard/products/overview](https://partner.microsoft.com/dashboard/products/overview)
2. Reserve name: **ViceSharp** (try alternates if taken).
3. Note the product’s **Store ID** / app id (used as `STORE_APP_ID`).

**Done when:** product exists and name is reserved.

---

## 2. Product identity (copy Store values)

1. Open the product → **Product management** → **Product identity**  
   (from the product page; path is under Product management in Partner Center).
2. Copy into a password manager / notes (do **not** commit to git):

| Partner Center field | ADO variable |
| --- | --- |
| Package/Identity/Name | `STORE_IDENTITY_NAME` (= `10557PaytonByrd.Vice`) |
| Package/Identity/Publisher (`CN=...`) | `STORE_PUBLISHER` (= `CN=45CF5BAC-327F-4E0C-B949-F93013DE843B`) |
| Publisher display name | `STORE_PUBLISHER_DISPLAY_NAME` (= `Sharp Ninja`) |
| Package Family Name (PFN) | informational: `10557PaytonByrd.Vice_5k14v45qyff0t` |
| Package SID | informational: `S-1-15-2-2231542120-...` (full value in [store-product-identity.md](store-product-identity.md)) |
| Store ID / product id | `STORE_APP_ID` (still needed) |

Repo keeps **dev** identity `CN=ViceSharpDev` in [Package.appxmanifest](../../src/ViceSharp.Xbox/Package.appxmanifest). CI stamps Store values via [Set-StoreIdentity.ps1](../../build/Set-StoreIdentity.ps1) at pack time only.

**Done when:** four `STORE_*` values are recorded offline.

---

## 3. Entra app for `msstore` CLI

Pipeline uses [Microsoft Store Developer CLI](https://learn.microsoft.com/windows/apps/publish/msstore-dev-cli/overview) (`msstore reconfigure` + `msstore publish`).

### 3.1 Create / pick an Entra app registration

1. [Azure Portal → App registrations](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade)  
2. **New registration** (or reuse an existing Partner Center automation app).  
3. Copy **Application (client) ID** → `PARTNER_CENTER_CLIENT_ID`  
4. Copy **Directory (tenant) ID** → `PARTNER_CENTER_TENANT_ID`  
5. **Certificates & secrets** → new client secret → `PARTNER_CENTER_CLIENT_SECRET` (secret)

### 3.2 Associate in Partner Center

1. Partner Center → [Account settings](https://partner.microsoft.com/dashboard/account/v3/organization/legalinfo) / User management  
2. **User management** → **Microsoft Entra applications** (or “Azure AD applications”)  
   Learn: [Create a Microsoft Entra application for Partner Center](https://learn.microsoft.com/windows/apps/publish/partner-center/manage-azure-ad-applications-in-partner-center)  
3. Add the app; grant **Manager** (or role that can create submissions).  
4. Seller ID: **Account settings** → **Organization profile** → Legal / organization info → `PARTNER_CENTER_SELLER_ID`

**Done when:** all four `PARTNER_CENTER_*` values exist and the app can manage submissions.

---

## 4. Azure DevOps: variable group + environment

Org/project:

- Org: [https://dev.azure.com/McpServer](https://dev.azure.com/McpServer)  
- Project: [https://dev.azure.com/McpServer/VICE-Sharp](https://dev.azure.com/McpServer/VICE-Sharp)

### 4.1 Variable group `xbox-store-publish`

1. Open: [Library (variable groups)](https://dev.azure.com/McpServer/VICE-Sharp/_library?itemType=VariableGroups)  
2. **+ Variable group** → name: `xbox-store-publish`  
3. Add all eight variables (mark `PARTNER_CENTER_CLIENT_SECRET` as **secret**):

| Variable | Secret |
| --- | --- |
| `PARTNER_CENTER_TENANT_ID` | no |
| `PARTNER_CENTER_SELLER_ID` | no |
| `PARTNER_CENTER_CLIENT_ID` | no |
| `PARTNER_CENTER_CLIENT_SECRET` | **yes** |
| `STORE_APP_ID` | no |
| `STORE_IDENTITY_NAME` | no |
| `STORE_PUBLISHER` | no |
| `STORE_PUBLISHER_DISPLAY_NAME` | no |

4. Pipeline permissions: allow the Store pipeline to use this group (Authorize when prompted on first run).

CLI alternative: [ado-store-setup.md](ado-store-setup.md)

### 4.2 Environment `xbox-store` (manual approval)

1. Open: [Environments](https://dev.azure.com/McpServer/VICE-Sharp/_environments)  
2. **New environment** → name: `xbox-store`  
3. **Approvals and checks** → **Approvals** → add yourself (and any co-approver).  
4. This gates the **PublishToStore** stage in [azure-pipelines-xbox-store.yml](../../azure-pipelines-xbox-store.yml).

**Done when:** group has 8 vars; environment requires your approval.

---

## 5. Register the Store pipeline

YAML already in repo: [azure-pipelines-xbox-store.yml](../../azure-pipelines-xbox-store.yml)

### UI path

1. [New pipeline](https://dev.azure.com/McpServer/VICE-Sharp/_build?definitionScope=%5C) → **New pipeline**  
2. **Azure Repos Git** → repo **VICE-Sharp**  
3. **Existing Azure Pipelines YAML file**  
4. Path: `/azure-pipelines-xbox-store.yml`  
5. Name it e.g. **VICE-Sharp-Xbox-Store**  
6. Save (**do not** run publish yet; first run can be Build-only after registration).

### CLI path

```pwsh
az pipelines create `
  --organization https://dev.azure.com/McpServer `
  --project VICE-Sharp `
  --name "VICE-Sharp-Xbox-Store" `
  --repository VICE-Sharp `
  --repository-type tfsgit `
  --branch main `
  --yml-path azure-pipelines-xbox-store.yml `
  --skip-first-run true
```

**Agent requirement:** self-hosted pool with **VS 18 MSBuild + UWP workload + .NET 10 SDK** (default parameter `agentPool: Default`). Hosted `windows-latest` is not enough per [xbox-store-publishing.md](../xbox-store-publishing.md).

**Done when:** pipeline definition exists and can start a manual run.

---

## 6. Fill the Store listing

Partner Center product → **Store listings** / submission draft.

### 6.1 Paste content

Use [store-listing-copy.md](store-listing-copy.md):

- Short description  
- Long description (includes GPL + no-ROM + source URL language)  
- Features list  
- What’s new  

### 6.2 Required URLs

| Field | Value |
| --- | --- |
| Privacy policy | Prefer wiki export after publish, or until then: [https://github.com/sharpninja/vice-sharp/blob/main/docs/PRIVACY.md](https://github.com/sharpninja/vice-sharp/blob/main/docs/PRIVACY.md) |
| Support | [https://github.com/sharpninja/vice-sharp/issues](https://github.com/sharpninja/vice-sharp/issues) |
| Website | [https://github.com/sharpninja/vice-sharp](https://github.com/sharpninja/vice-sharp) |

Source policy text: [docs/PRIVACY.md](../PRIVACY.md)

### 6.3 Age rating (IARC)

Partner Center walks the IARC questionnaire in the submission. Notes for answers are in [store-listing-copy.md](store-listing-copy.md) (tool, no publisher-hosted UGC, user-configured network).

### 6.4 Device families and pricing

- Enable **Windows 10/11 Xbox** and **Desktop** (matches manifest).  
- Price: **Free**  
- Markets: worldwide free (or your preference)

Learn: [Create app Store listings](https://learn.microsoft.com/windows/apps/publish/publish-your-app/msix/create-app-store-listing) · [Age ratings](https://learn.microsoft.com/windows/apps/publish/publish-your-app/msix/age-ratings)

**Done when:** Partner Center submission shows no missing required listing fields (package can still be empty).

---

## 7. Screenshots

Capture plan: [store-listing-copy.md § Screenshot capture runbook](store-listing-copy.md)

| # | Shot | Target |
| --- | --- | --- |
| 1 | Home | 1920×1080 |
| 2 | Emulation running | 1920×1080 |
| 3 | RomM Library | 1920×1080 |
| 4 | Settings | 1920×1080 |
| 5 | About (GPL + source URL) | 1920×1080 |
| 6 | Controls (optional) | 1920×1080 |

Store files under `docs/xbox/store-screenshots/` (`01-home.png`, …).

**Xbox capture:** Dev Mode Device Portal on the console, or HDMI capture.  
**PC capture:** UWP head on desktop (Desktop device family).

Upload in Partner Center → Store listings → Screenshots for each device family.

**Done when:** minimum screenshot counts for Xbox (and Desktop if enabled) are satisfied.

---

## 8. Optional: local package + WACK

Recommended before first submit so cert failures are cheaper.

### 8.1 Build Store upload package (VS MSBuild)

From repo root (PowerShell). Use real Partner Center values; **do not commit** the stamped manifest:

```pwsh
./build/Set-StoreIdentity.ps1 `
  -ManifestPath src/ViceSharp.Xbox/Package.appxmanifest `
  -IdentityName '<STORE_IDENTITY_NAME>' `
  -Publisher '<STORE_PUBLISHER>' `
  -PublisherDisplayName '<STORE_PUBLISHER_DISPLAY_NAME>'

$msb = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
  -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1

& $msb src/ViceSharp.Xbox/ViceSharp.Xbox.csproj `
  /p:Configuration=Release-UWP /p:Platform=x64 `
  /t:Restore,Build `
  /p:UapAppxPackageBuildMode=StoreUpload `
  /p:GenerateAppxPackageOnBuild=true `
  /p:AppxPackageSigningEnabled=false `
  /p:AppxPackageDir="artifacts/store-upload/" `
  /v:m /nologo
```

Restore dev identity afterward (`git checkout -- src/ViceSharp.Xbox/Package.appxmanifest`).

### 8.2 WACK (interactive elevated session)

```pwsh
./build.ps1 ValidateStorePackage --store-package-path <path-to.msixupload>
```

Docs: [Windows App Certification Kit](https://learn.microsoft.com/windows/uwp/debug-test-perf/windows-app-certification-kit) · Nuke target in [build/Build.cs](../../build/Build.cs) (`ValidateStorePackage`).

### 8.3 Unit gate (already green for GPL)

```pwsh
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~XboxGplComplianceTests"
```

**Done when:** `.msixupload` exists; WACK PASS if run; GPL tests still green.

---

## 9. S42 console validation

Use: [on-console-setup-runbook.md](on-console-setup-runbook.md)

Minimum matrix before retail submit:

1. Dev Mode on Xbox; pair Device Portal.  
2. Sideload Release-UWP / deploy path (`DeployXbox` / VS Remote Machine).  
3. Gamepad-only: Home, Settings, Controls, About, Library (manual URL if LAN fails).  
4. Cold start, suspend/resume, terminate.  
5. TV-safe margins; Guide does not brick focus.  
6. Decide LAN scan: works on console **or** document desktop-only / manual URL (capability `privateNetworkClientServer`).

Write receipt: `docs/receipts/s42-store-console-validation-YYYYMMDD.md`

**Done when:** no P0 crash; LAN decision recorded.

---

## 10. Pipeline: Build, then Publish

### 10.1 Build stage only first

1. Open the Store pipeline in [Pipelines](https://dev.azure.com/McpServer/VICE-Sharp/_build)  
2. **Run pipeline** (manual; YAML has `trigger: none`)  
3. Parameters: `runWack` only if agent is interactive + admin  
4. Confirm artifact **store-upload** contains `.msixupload` / `.appxupload`

### 10.2 Approve PublishToStore

Only after steps 1–7 (and ideally 8–9):

1. Environment **xbox-store** approval prompt → Approve  
2. Pipeline runs `msstore reconfigure` + `msstore publish`  
3. Watch Partner Center certification  

CLI status (local, after `msstore reconfigure`):

```text
msstore submission status <STORE_APP_ID>
```

Learn: [msstore publish](https://learn.microsoft.com/windows/apps/publish/msstore-dev-cli/commands) · [App certification process](https://learn.microsoft.com/windows/apps/publish/publish-your-app/msix/app-certification-process)

**Done when:** submission is In certification or Certified (not failed).

---

## 11. After certification

1. Publish / rollout in Partner Center (immediate or flight).  
2. Retail smoke: install from Microsoft Store on **Xbox** and **Windows Desktop**.  
3. Launch → **About** shows version + source URL.  
4. Record listing URL + package version in [HANDOFF.md](../../HANDOFF.md).  
5. Keep submitted `.msixupload` + WACK report as release artifacts.  
6. For updates: bump version (revision **0**), re-run pipeline, re-approve publish.

Microsoft Store consumer entry (after live): search **ViceSharp** on Xbox or [Microsoft Store web](https://apps.microsoft.com/) (exact URL appears in Partner Center after publish).

---

## Cheat sheet: eight secrets/vars

| Variable | Where you get it |
| --- | --- |
| `STORE_APP_ID` | Partner Center product / Store id |
| `STORE_IDENTITY_NAME` | Product identity → Package/Identity/Name |
| `STORE_PUBLISHER` | Product identity → Publisher `CN=...` |
| `STORE_PUBLISHER_DISPLAY_NAME` | Product identity / publisher display name |
| `PARTNER_CENTER_TENANT_ID` | Entra → Directory (tenant) ID |
| `PARTNER_CENTER_CLIENT_ID` | Entra app → Application (client) ID |
| `PARTNER_CENTER_CLIENT_SECRET` | Entra app → client secret value |
| `PARTNER_CENTER_SELLER_ID` | Partner Center organization / seller id |

---

## If something fails

| Symptom | What to check |
| --- | --- |
| Build stage: no `.msixupload` | VS UWP workload on agent; MSBuild path; Release-UWP config |
| Stamp identity fails | Variable group linked; names exact |
| `msstore publish` 401 | Entra app Manager role; secret not expired; tenant/seller ids |
| Cert fail: capabilities | Justify `privateNetworkClientServer` or gate Scan LAN |
| Cert fail: ROMs / IP | Reaffirm no ROMs; About + listing language |
| Cert fail: incomplete listing | Screenshots, privacy URL, IARC, description |
| winget still on 1.1.0 | Unrelated channel; [winget PR](https://github.com/microsoft/winget-pkgs/pull/412774) already merged for 1.2.1 MSI |

---

## Suggested order for a single focused day

1. Morning: Partner Center reserve + identity + Entra (steps 1–3)  
2. Midday: ADO vars, environment, pipeline (steps 4–5)  
3. Afternoon: listing + screenshots (steps 6–7)  
4. Evening: local package or pipeline Build (steps 8, 10.1)  
5. Next session with console: S42 (step 9) → approve Publish (step 10.2) → retail smoke (step 11)

When Partner Center values are ready, you can hand the eight variables to an agent and ask it to **create the variable group and register the pipeline** (those actions need explicit approval in this lab environment).

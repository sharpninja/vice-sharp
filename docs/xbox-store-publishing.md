# Publishing the Xbox UWP head to the Microsoft Store

FEAT-XSTOREPIPE-001 (PLAN-XBOXUWP S42). The pipeline `azure-pipelines-xbox-store.yml`
builds the Release-UWP head, produces an UNSIGNED Store upload package
(`.msixupload`), and publishes it to Partner Center through the official Microsoft
Store Developer CLI (msstore), behind a manual approval.

## One-time Partner Center setup

1. **Developer account**: a Partner Center developer account
   (https://partner.microsoft.com/dashboard). Publishing to **Xbox consoles** requires
   the app submission's device families to include Xbox (see step 4).
2. **Reserve the app name**: Apps and Games > New product > MSIX or PWA app. Note the
   **Store/product ID** (this becomes `STORE_APP_ID`).
3. **Store association values** (Product > Product identity):
   - `Package/Identity/Name` -> `STORE_IDENTITY_NAME`
   - `Package/Identity/Publisher` (the `CN={GUID}` form) -> `STORE_PUBLISHER`
   - Publisher display name -> `STORE_PUBLISHER_DISPLAY_NAME`
   The repo keeps the DEV identity in `Package.appxmanifest`; the pipeline stamps these
   Store values in at build time via `build/Set-StoreIdentity.ps1`.
4. **Xbox device family**: in the submission's **Packages** section, keep the
   `Windows 10/11 Xbox` device family enabled so the package is offered to consoles.
   The manifest's `Windows.Universal` target family covers Xbox; console-specific
   constraints (TV-safe UI, gamepad input, ROM provisioning) are already part of the
   head.
5. **API access**: Partner Center > Account settings > User management > **Microsoft
   Entra applications**: associate (or create) an Entra app registration, grant it the
   **Manager** role. Collect:
   - Entra tenant ID -> `PARTNER_CENTER_TENANT_ID`
   - Seller ID (Account settings > Organization profile > Legal info) -> `PARTNER_CENTER_SELLER_ID`
   - Application (client) ID -> `PARTNER_CENTER_CLIENT_ID`
   - A client secret -> `PARTNER_CENTER_CLIENT_SECRET`

## One-time Azure DevOps setup (dev.azure.com/McpServer/VICE-Sharp)

1. **Variable group** `xbox-store-publish` (Pipelines > Library) holding the eight
   variables above. Mark `PARTNER_CENTER_CLIENT_SECRET` as **secret**.
2. **Environment** `xbox-store` (Pipelines > Environments) with an **Approvals** check
   listing the approver(s). The publish stage will pause there until approved.
3. **Self-hosted agent**: the build needs the PROVEN toolchain: Visual Studio 18
   MSBuild with the UWP/XAML tooling plus the .NET 10 SDK (the hosted `windows-latest`
   image does not carry it). Register the dev PC as a self-hosted agent and pass its
   pool name via the `agentPool` parameter (default `Default`).
4. **Create the pipeline**: Pipelines > New pipeline > Azure Repos Git > VICE-Sharp >
   Existing Azure Pipelines YAML file > `/azure-pipelines-xbox-store.yml`.

## Running a submission

1. Run the pipeline manually (`trigger: none` keeps publishes deliberate).
2. The **Build** stage runs the Category=Xbox suite, stamps the Store identity, builds
   `Release-UWP|x64`, and produces the `.msixupload` artifact. The package is
   intentionally UNSIGNED (`UapAppxPackageBuildMode=StoreUpload`): the Store signs
   published packages.
3. The **PublishToStore** stage waits on the `xbox-store` environment approval, then:
   `msstore reconfigure` (secret variables) and `msstore publish -i <msixupload> -id
   $(STORE_APP_ID)`, which uploads the package to the app's draft submission and
   commits it for certification.
4. Track certification in Partner Center (or `msstore submission status $(STORE_APP_ID)`).

## Known caveats (stated honestly)

- **Native AOT**: Release-UWP arms `PublishAot`; the store package is produced by the
  BUILD (not `dotnet publish`), so it ships optimized JIT (ReadyToRun-less) managed
  code today. The S0 finding stands: the VS-MSBuild `Publish`+`PublishAot` pipeline for
  modern UWP produced only a managed apphost in this environment. Revisit AOT-in-MSIX
  when the .NET UWP tooling matures; nothing in the pipeline changes except the build
  step gaining `/t:Publish`.
- **First submission**: the very first Store submission usually wants listing assets
  (screenshots, descriptions) filled in Partner Center; `msstore publish` updates the
  PACKAGE of the submission. Complete the listing once by hand; subsequent runs are
  fully pipeline-driven.
- **GPL compliance**: the MSIX already carries `Licenses/COPYING` +
  `Licenses/THIRD_PARTY_NOTICES.md` and ships no Commodore ROMs (FR-XBOXGPL-006 /
  FR-XROM-003).

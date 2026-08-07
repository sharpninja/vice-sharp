# Azure DevOps setup for Microsoft Store publish

One-time operator steps for FEAT-XSTOREPIPE-001. Repo already has:

- `azure-pipelines-xbox-store.yml`
- `build/Set-StoreIdentity.ps1`
- docs in `docs/xbox-store-publishing.md`

**Status 2026-08-05:** project has `VICE-Sharp-CI` and `VICE-Sharp-Release` only. Variable group list was empty. Store pipeline not registered yet.

## 1. Variable group `xbox-store-publish`

Pipelines > Library > + Variable group.

| Variable | Secret? | Source |
| --- | --- | --- |
| `PARTNER_CENTER_TENANT_ID` | no | Entra tenant of Partner Center account |
| `PARTNER_CENTER_SELLER_ID` | no | Partner Center seller id |
| `PARTNER_CENTER_CLIENT_ID` | no | Entra app registration client id |
| `PARTNER_CENTER_CLIENT_SECRET` | **yes** | Entra client secret |
| `STORE_APP_ID` | no | Partner Center product / Store id |
| `STORE_IDENTITY_NAME` | no | `10557PaytonByrd.Vice` (see [store-product-identity.md](store-product-identity.md)) |
| `STORE_PUBLISHER` | no | `CN=45CF5BAC-327F-4E0C-B949-F93013DE843B` |
| `STORE_PUBLISHER_DISPLAY_NAME` | no | `Sharp Ninja` |

Full reserved identity including PFN / Package SID: [store-product-identity.md](store-product-identity.md).

CLI alternative (after secrets are ready; fill values yourself):

```pwsh
az pipelines variable-group create `
  --organization https://dev.azure.com/McpServer `
  --project VICE-Sharp `
  --name xbox-store-publish `
  --authorize true `
  --variables `
    PARTNER_CENTER_TENANT_ID=REPLACE `
    PARTNER_CENTER_SELLER_ID=REPLACE `
    PARTNER_CENTER_CLIENT_ID=REPLACE `
    STORE_APP_ID=REPLACE `
    STORE_IDENTITY_NAME=REPLACE `
    STORE_PUBLISHER=REPLACE `
    STORE_PUBLISHER_DISPLAY_NAME=REPLACE

# Then add the secret separately in the portal (or variable-group variable create --secret true)
```

## 2. Environment `xbox-store`

Pipelines > Environments > New environment `xbox-store` > Approvals and checks > Approvals > add yourself.

## 3. Register the pipeline

```pwsh
az pipelines create `
  --organization https://dev.azure.com/McpServer `
  --project VICE-Sharp `
  --name "VICE-Sharp-Xbox-Store" `
  --description "Build Release-UWP msixupload; publish to Partner Center via msstore" `
  --repository VICE-Sharp `
  --repository-type tfsgit `
  --branch main `
  --yml-path azure-pipelines-xbox-store.yml `
  --skip-first-run true
```

Or: New pipeline > Azure Repos Git > Existing YAML > `/azure-pipelines-xbox-store.yml`.

Agent pool default is `Default` (self-hosted with VS 18 UWP + .NET 10). Override via parameter `agentPool` if needed.

## 4. First run order

1. Run pipeline manually with **Publish** stage left unapproved until listing + S42 are ready.
2. Confirm Build stage produces `store-upload` artifact (`.msixupload`).
3. Optional: set parameter `runWack: true` only if the agent is interactive + elevated.
4. Approve environment only for real Partner Center submit.

## 5. Partner Center (before vars are real)

1. Developer account enrolled.
2. New MSIX app > reserve **ViceSharp**.
3. Product identity -> fill STORE_* vars.
4. Entra app associated with Manager role -> PARTNER_CENTER_* vars.
5. Paste listing from `store-listing-copy.md` and privacy URL from `docs/PRIVACY.md`.

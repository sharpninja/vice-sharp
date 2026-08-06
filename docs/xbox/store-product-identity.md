# Partner Center product identity (ViceSharp Store)

**Reserved 2026-08-05.** Use these values when stamping packages and filling the ADO `xbox-store-publish` variable group. Do **not** commit the Store publisher into the day-to-day `Package.appxmanifest` (repo stays on `CN=ViceSharpDev` for Dev Mode sideload). Pipeline / local Store packs use `build/Set-StoreIdentity.ps1`.

## Identity (stamped into AppxManifest)

| Field | Value | ADO variable |
| --- | --- | --- |
| Package/Identity/Name | `10557PaytonByrd.Vice` | `STORE_IDENTITY_NAME` |
| Package/Identity/Publisher | `CN=45CF5BAC-327F-4E0C-B949-F93013DE843B` | `STORE_PUBLISHER` |
| Package/Properties/PublisherDisplayName | `Sharp Ninja` | `STORE_PUBLISHER_DISPLAY_NAME` |
| Package/Properties/DisplayName | `Vice#` | reserved app name (must match Partner Center exactly) |
| Default language | `en-US` | (not `x-generate`) |

## Derived (informational; not stamped)

| Field | Value |
| --- | --- |
| Package Family Name (PFN) | `10557PaytonByrd.Vice_5k14v45qyff0t` |
| Package SID | `S-1-15-2-2231542120-158891871-596044206-2003317803-3667661490-3193014165-380664165` |

PFN is `{Name}_{PublisherIdHash}`. It must match after install:

```text
%LOCALAPPDATA%\Packages\10557PaytonByrd.Vice_5k14v45qyff0t\
```

Package SID is used by the Windows security model for the package identity; Partner Center shows it for reference. Neither field is written into `Package.appxmanifest` by hand.

## Stamp + pack (local Store upload)

```pwsh
./build/Set-StoreIdentity.ps1 `
  -ManifestPath src/ViceSharp.Xbox/Package.appxmanifest `
  -IdentityName '10557PaytonByrd.Vice' `
  -Publisher 'CN=45CF5BAC-327F-4E0C-B949-F93013DE843B' `
  -PublisherDisplayName 'Sharp Ninja'

# then set Version (revision 0), Publish Release-UWP, makeappx pack
# restore: git checkout -- src/ViceSharp.Xbox/Package.appxmanifest
```

## Packages produced with this identity

| File | Notes |
| --- | --- |
| `artifacts/store-upload/ViceSharp_1.2.1.0_x64_Store.msix` | Store Name/Publisher, version 1.2.1.0, unsigned |
| `artifacts/store-upload/ViceSharp_1.2.1.0_x64_Store.msixupload` | Zip of the MSIX for Partner Center |

## Still needed for pipeline publish

| Variable | Status |
| --- | --- |
| `STORE_APP_ID` | Fill from Partner Center product / Store id |
| `PARTNER_CENTER_TENANT_ID` | Entra tenant |
| `PARTNER_CENTER_SELLER_ID` | Seller id |
| `PARTNER_CENTER_CLIENT_ID` | Entra app |
| `PARTNER_CENTER_CLIENT_SECRET` | Secret |
| `STORE_IDENTITY_NAME` | known (above) |
| `STORE_PUBLISHER` | known (above) |
| `STORE_PUBLISHER_DISPLAY_NAME` | known (above) |

See [ado-store-setup.md](ado-store-setup.md) and [store-next-steps-guide.md](store-next-steps-guide.md).

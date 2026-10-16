# Receipt: Microsoft Store Phase 0/2 docs + GPL gate

**Date:** 2026-08-05  
**Plan:** Finish ViceSharp on Microsoft Store (session plan.md)  
**Decision:** GO with mitigations (see `docs/xbox/gpl-store-section6-review.md`)

## Artifacts created

| Path | Purpose |
| --- | --- |
| `docs/xbox/gpl-store-section6-review.md` | Phase 0 legal gate + operator GO |
| `docs/PRIVACY.md` | Public privacy policy for Partner Center URL |
| `docs/xbox/store-listing-copy.md` | Listing text + screenshot runbook |
| `docs/wiki.yaml` | privacy + store docs added to wiki export |
| `docs/xbox/microsoft-store-publishing-checklist.md` | Progress checkboxes updated |
| `HANDOFF.md` | Track 3 Store status |

## Validation

```text
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~XboxGplComplianceTests"
Passed!  - Failed: 0, Passed: 5, Skipped: 0, Total: 5
```

## ADO readiness (observation)

- Pipelines present: `VICE-Sharp-CI` (15), `VICE-Sharp-Release` (16)
- Variable groups list: empty (`[]`) -> `xbox-store-publish` not created
- Store YAML exists at repo root: `azure-pipelines-xbox-store.yml` (not registered as a definition yet)

## Blocked on operator

1. Partner Center: account, reserve name, product identity, Entra app + secret
2. ADO: create `xbox-store-publish` variable group; `xbox-store` environment with approval; register Store pipeline  
   (exact commands: `docs/xbox/ado-store-setup.md`; agent was blocked from creating the pipeline without explicit approval)
3. Screenshots under `docs/xbox/store-screenshots/`
4. S42 console validation receipt
5. Human approval of PublishToStore stage

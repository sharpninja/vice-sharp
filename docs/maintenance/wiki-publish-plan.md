# Wiki Publish Plan (2026-05-16)

Owner: REPO-MAINT-001 phase 1.

This plan documents the pipeline that will publish the ViceSharp requirements wiki to its public target. The plan is documentation-only; no actual wiki push is performed in this slice.

## Targets

| Platform | Public URL | Source mirror in repo |
|---|---|---|
| GitHub Wiki | https://github.com/sharpninja/vice-sharp/wiki | `docs/Project/wiki/github/` |
| Azure DevOps Wiki | https://dev.azure.com/McpServer/VICE-Sharp/_wiki/wikis/VICE-Sharp.wiki | `docs/Project/wiki/azure/` |

GitHub is the **mirror** target (per the global rule that Azure DevOps is the primary). Both targets are published from the same MCP-generated source set, but each gets its own platform-formatted output (GitHub uses `_Sidebar.md`/`_Footer.md`; Azure uses `.order`).

## Source set (both targets)

The MCP requirements generator already emits the canonical wiki content into the repo. The complete page set per platform is:

| Page | Source generator | Description |
|---|---|---|
| `Home.md`                    | `POST /mcpserver/requirements/generate?target=home`      | Landing page with TOC links |
| `Functional-Requirements.md` | `POST /mcpserver/requirements/generate?target=fr`        | All FRs, one section per requirement |
| `Technical-Requirements.md`  | `POST /mcpserver/requirements/generate?target=tr`        | All TRs, one section per requirement |
| `Testing-Requirements.md`    | `POST /mcpserver/requirements/generate?target=test`      | All TEST requirements, one section per requirement |
| `TR-per-FR-Mapping.md`       | `POST /mcpserver/requirements/generate?target=mapping`   | FR -> TR traceability matrix |
| `_Sidebar.md` (GitHub only)  | static                                                   | Nav links |
| `_Footer.md` (GitHub only)   | static                                                   | Attribution stamp |
| `.order` (Azure only)        | static                                                   | Page ordering hints |
| `.mcp-requirements-manifest.json` | generator                                           | Per-target manifest with `generatedAtUtc` and `documents` list (already produced) |

`.mcp-requirements-manifest.json` is the integrity record. Each wiki publish run should bump `generatedAtUtc` and verify the `documents` list still matches what is present on disk.

## Source of truth -> wiki mapping

```
docs/requirements/functional/FR-*.md  ----+
docs/requirements/technical/TR-*.md   ----+--> MCP Server requirements store
docs/requirements/<TEST sections>     ----+      |
                                                 |
                                                 v
                          /mcpserver/requirements/generate (per target)
                                                 |
                                                 v
                                +----------------+----------------+
                                |                                 |
                                v                                 v
                  docs/Project/wiki/github/*.md       docs/Project/wiki/azure/*.md
                                |                                 |
                                v                                 v
                     [github wiki repo push]            [azure wiki repo push]
```

## Gating rules

A wiki publish run is allowed only when **every** gate below is green.

### G1 -- Requirements ingestion parity

The MCP store and the local Markdown corpus must agree on requirement IDs (modulo the documented `TR-GRPC-001` -> `TR-GRPC-BOUNDARY-001` rename). Reference: `docs/maintenance/requirements-import-check-2026-05-16.md`.

Verify with:

```
diff <(grep -rEoh '\bFR-[A-Z]+-[0-9]{3}\b'   docs/requirements/functional/ | sort -u) \
     <(curl -s "$MCP/mcpserver/requirements/fr" -H "X-Api-Key: $KEY" | jq -r '.[].id' | sort -u)
# (repeat for tr, test)
```

The diff must be empty (or only contain the documented superseded ID).

### G2 -- All tests green

`dotnet test ./ViceSharp.slnx --nologo` must report all tests passing. As of 2026-05-16 the project floor is 664+ passing tests. The publish operator must paste the green count into the publish PR description.

### G3 -- Manifest freshly generated

`docs/Project/wiki/github/.mcp-requirements-manifest.json` and `docs/Project/wiki/azure/.mcp-requirements-manifest.json` must have a `generatedAtUtc` within the last 24 hours of the publish run. Stale manifests are a stop condition.

### G4 -- No uncommitted edits to source pages

`git status --short docs/Project/wiki/` must be empty. The wiki source is regenerated end-to-end; hand-edits are not permitted because the next regeneration will silently overwrite them.

### G5 -- Operator authorization

Wiki pushes are credentialed write operations against an external service. The wiki publish step is **not** part of any automated commit on the source branch. It must be explicitly authorized by the repository owner (per the global rule: do not push to `github` remote, including its wiki, without explicit instruction). For the Azure DevOps target, the operator must hold wiki contributor rights on the project.

### G6 -- Per-platform formatting check

- GitHub: the renderer requires `[Title]` link text and `(Page-Name)` URL form (no `.md` suffix). Verify by spot-checking five links per page.
- Azure DevOps: `.order` must list every `.md` file in the directory. Pages outside `.order` are hidden from navigation.

## Publish procedure (deferred to phase 2)

> This procedure is documented for future execution. It is **not** to be run as part of this commit.

### Azure DevOps wiki (primary)

```
# Working from a scratch clone of the wiki repo
git clone https://dev.azure.com/McpServer/VICE-Sharp/_git/VICE-Sharp.wiki wiki-azure
cp docs/Project/wiki/azure/*.md docs/Project/wiki/azure/.order wiki-azure/
cd wiki-azure
git add -A
git commit -m "docs(wiki): refresh from <vice-sharp commit sha>"
git push origin master   # azure wiki default branch is wikiMaster or master per repo
```

### GitHub wiki (mirror)

The user must explicitly authorize a GitHub-side push. Standing rule: never push to the `github` remote (including its wiki) without an instruction in the current conversation.

```
# Working from a scratch clone of the wiki repo (only after explicit authorization)
git clone https://github.com/sharpninja/vice-sharp.wiki.git wiki-github
cp docs/Project/wiki/github/*.md \
   docs/Project/wiki/github/_Sidebar.md \
   docs/Project/wiki/github/_Footer.md \
   wiki-github/
cd wiki-github
git add -A
git commit -m "docs(wiki): refresh from <vice-sharp commit sha>"
git push origin master
```

## Known gaps to close before phase-2 publish

1. **Add the three missing TR Markdown files** noted in `docs/maintenance/requirements-import-check-2026-05-16.md` (`TR-HOST-STATUS-001`, `TR-INPUT-VKM-001`, `TR-UI-SHELL-001`) so they make it into the regenerated wiki pages.
2. **Re-run `POST /mcpserver/requirements/generate`** for each target so the manifests and per-page Markdown reflect those three TRs.
3. **Decide on the chip dead-code deletions** (`docs/maintenance/chip-deadcode-audit-2026-05-16.md`) before the wiki publishes, so the architecture pages do not need to be revised mid-flight.
4. **Confirm wiki target branch names** for both platforms; Azure DevOps wikis historically default to `wikiMaster`, GitHub wikis to `master`. The procedure above must be adjusted at run time to match what each remote actually has.

## Validation

- This plan was authored against the in-repo mirrors at `docs/Project/wiki/github/` and `docs/Project/wiki/azure/` as they exist on `worktree-agent-a611869f4763ea1e8` at the commit captured below.
- No wiki push was performed. No edits were made to any wiki content file. No new dependencies were introduced.

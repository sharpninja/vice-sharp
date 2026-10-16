# PLAN-LEGACY-FR-RENAME-20260929: Rename 4 legacy FRs to FR-* (create + remap + delete)

Byrd Development Process v4 planning document. **PLAN ONLY — do not execute until operator approval gate below is signed.**

| Field | Value |
| --- | --- |
| Status | DRAFT — awaiting operator approval |
| Machine | PAYTON-LEGION2 |
| Workspace | `F:\GitHub\vice-sharp` |
| McpServer | live `1.0.0+deda7d6a` (Healthy); **no UpdateService / redeploy** |
| Trigger | Slice5b permanent skips (`permanent-skips-legacy-fr.json`); PUT `/fr` rejects non-`FR-*` ids |
| Parent receipts | commit `f71cdfb` (Slice5b mapping-chase); prior `92e35d2` (Slice5 apply) |
| Effective counts (post-chase) | FR=232 TR=138 TEST=140 MAP=231 |

## 1. Context

Four live functional requirements use pre-`FR-` legacy ids. They are **readable and mapped**, but **mutation is blocked**: Recovery `FrIdPattern` and live `PUT /mcpserver/requirements/fr` both reject the id shape (`400` "must match the FR identifier shape"). Slice5b chase fixed 19/23 skipped ids; these four remain as `permanent-skip-mutation`.

Goal: rename each legacy FR to a valid `FR-*` id by **create new → copy content → migrate mapping → delete old**, without inventing product scope. No McpServer code change required if live CRUD + mapping APIs suffice.

## 2. Locked decisions (proposed; confirm at approval)

1. **Id mapping = `FR-` + legacy id** (already validated in `skipped-id-analysis.json`: `proposedNorm` / `normValid=true` / `normAlreadyLive=false`).
2. **Live CRUD + mapping API only** — no recovery-bundle rename, no UpdateService redeploy.
3. **Per-FR atomicity** — complete create+remap+verify for one FR before deleting that FR's legacy id; process all four in one approved session unless operator pauses mid-batch.
4. **No push** of any resulting receipts until a separate operator-authorized commit.
5. **Title/body/status/priority/tags copied verbatim** from live GET of the legacy FR — rename only; do not edit product text.

## 3. Proposed new FR-* ids

| Legacy id (live+mapped) | Proposed new id | Norm valid | New id already live? |
| --- | --- | --- | --- |
| `ARCH-TRUEDRIVE-1541-002` | `FR-ARCH-TRUEDRIVE-1541-002` | yes | no (as of Slice5b chase) |
| `BACKFILL-MEDIA-001` | `FR-BACKFILL-MEDIA-001` | yes | no |
| `BACKFILL-VIDEO-001` | `FR-BACKFILL-VIDEO-001` | yes | no |
| `RUNTIME-TAPE-002` | `FR-RUNTIME-TAPE-002` | yes | no |

Known mapping rows from Slice5b `mapping-after.json` sample (confirm + complete for `RUNTIME-TAPE-002` during Step 0 snapshot):

| Legacy frId | trIds | testIds |
| --- | --- | --- |
| `ARCH-TRUEDRIVE-1541-002` | `TR-DRV-EDGE-001`, `TR-IEC-EDGE-001` | `TEST-DRV-MOTOR-001`, `TEST-IEC-TIMING-001` |
| `BACKFILL-MEDIA-001` | `TR-DRV-EDGE-001`, `TR-TAP-EDGE-001`, `TR-TAPE-EDGE-001` | `TEST-DRV-MOTOR-001`, `TEST-TAPE-RAMP-001`, `TEST-TAPE-SENSE-001` |
| `BACKFILL-VIDEO-001` | `TR-CYCLE-001`, `TR-VIC-EDGE-001`…`006` (7 TRs) | `TEST-VIC-CHECKPOINT-001`, `TEST-VIC-RC-001` |
| `RUNTIME-TAPE-002` | **TBD in Step 0 live GET** | **TBD in Step 0 live GET** |

## 4. Surfaces (live; no redeploy)

| Op | Surface |
| --- | --- |
| Read FR | `GET /mcpserver/requirements/fr/{id}` (legacy ids still readable) |
| Create FR | `POST /mcpserver/requirements/fr` body with new `FR-*` id + copied fields |
| Delete FR | `DELETE /mcpserver/requirements/fr/{id}` |
| List / read mapping | `GET /mcpserver/requirements/mapping` |
| Upsert mapping | `PUT /mcpserver/requirements/mapping/{frId}` body `{ trIds[], testIds[] }` |
| Delete mapping (if exposed) | confirm before execute; else overwrite-empty then delete FR only after new mapping verified |

Plugin equivalents (optional): `RequirementsClient` / `RequirementsWorkflow` `ListMappingsAsync`, `CreateMappingAsync` / `UpsertMappingAsync`.

## 5. Execution steps (DO NOT RUN until §8 approval)

### Step 0 — Snapshot (reversible, read-only + receipt write)

1. Record pre-counts: FR/TR/TEST/MAP.
2. For each of the 4 legacy ids: GET FR document → save JSON under  
   `docs/receipts/requirements-recovery/legacy-fr-rename-<timestamp>/snapshot/{legacyId}.json`.
3. GET full mapping; extract the 4 rows → `snapshot/mappings-legacy.json`.
4. Confirm none of the 4 proposed `FR-*` ids already exist (GET must 404).
5. Write `precheck.json` with counts + id existence matrix.

**Gate:** stop and re-ask operator if any proposed id is already live, or if a legacy FR is missing/unmapped unexpectedly.

### Step 1 — Create new FR-* (one at a time)

For each pair `(legacy → new)`:

1. POST new FR with **same title, body, status, priority, tags, acceptance criteria** as snapshot; `Id` = proposed `FR-*`.
2. GET new FR; assert fields match snapshot (id differs only).
3. Receipt: `create-results.json` append `CREATE_OK` / fail detail.

**Do not delete legacy yet.** Expected FR count after all four creates: **236** (+4).

### Step 2 — Migrate mappings

For each pair:

1. `PUT /mapping/{newFrId}` with **identical** `trIds` / `testIds` from snapshot.
2. GET mapping; assert new frId row matches; legacy row still present until Step 3.
3. Receipt: `mapping-migrate-results.json`.

Expected MAP count after four creates: **235** (+4), until legacy mappings removed.

### Step 3 — Delete legacy FRs (+ their mapping rows)

For each pair, only after Step 1–2 verify for that pair:

1. Delete mapping for legacy frId if API supports it; otherwise delete FR and confirm mapping row gone.
2. `DELETE /fr/{legacyId}`.
3. Assert GET legacy → 404; GET new → 200; mapping has new id only.
4. Receipt: `delete-results.json`.

Expected final counts: **FR=232** (net 0), **MAP=231** (net 0), TR/TEST unchanged.

### Step 4 — Verify

1. Re-GET counts; compare to post-chase baseline (FR=232 TR=138 TEST=140 MAP=231).
2. Grep/list mapping: zero of the four legacy ids; all four new ids present with expected TR/TEST sets.
3. Spot-check: attempt `PUT /fr` on each new id (no-op field touch or same body) — must succeed (proves mutation unblocked).
4. Write `effective-counts-after.json` + `PLAN-LEGACY-FR-RENAME-receipts.txt`.
5. **Do not commit/push** unless operator authorizes a separate receipts commit.

## 6. Risks

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Create succeeds, remap fails | Orphan FR-* with no mapping; legacy still authoritative | Keep legacy until remap verified; delete new on failure |
| Delete legacy before remap | Mapping gap / broken FR→TR→TEST chain | Strict order: create → remap → verify → delete |
| Mapping PUT replaces wrong FR | Data loss on unrelated FR | PUT only `{newFrId}`; never write empty body to wrong key |
| Concurrent Slice5/recovery apply | Race on counts/ids | Freeze other recovery mutations for the session |
| Docs/wiki still cite legacy ids | Stale references | Follow-up doc sweep (out of scope for mutate); optional receipt note |
| Soft-delete / tombstone leaves legacy readable | Counts confusing | Re-GET list; if tombstone, document and ask operator |
| `RUNTIME-TAPE-002` mapping not in sample | Incomplete remap | Step 0 mandatory live extract |

## 7. Rollback

| Failure point | Rollback |
| --- | --- |
| After create, before delete | `DELETE` the new `FR-*` ids; leave legacy untouched. Counts return to baseline. |
| After remap, before delete | Delete new mapping row + new FR; legacy mapping still intact. |
| After delete (partial batch) | From Step 0 snapshot: re-POST legacy FR (if API allows legacy create — **likely blocked** by FrIdPattern). If legacy create is blocked, restore from DB/backup or temporary pattern exception — **escalate to operator; do not invent a second id**. Prefer completing remaining renames rather than half-rolling. |
| Full disaster | Restore requirements store from pre-session backup / McpServer data snapshot if one exists; else recreate from `snapshot/*.json` under approved exception. |

**Pre-flight ask (approval item):** does operator have a restorable requirements DB / workspace backup before Step 3 deletes?

## 8. Operator approval gate

**STOP. Do not execute Steps 1–4 until the operator explicitly approves all of the following:**

- [ ] Approve the four id pairs in §3 (or supply alternate `FR-*` ids).
- [ ] Approve live CRUD+mapping approach (no UpdateService / no FrIdPattern relaxation).
- [ ] Confirm requirements backup / accept rollback limitation on legacy re-create.
- [ ] Authorize execution window on PAYTON-LEGION2 / workspace `F:\GitHub\vice-sharp`.
- [ ] Confirm: leave plan + any run receipts **uncommitted** until a separate commit order.

Approval signature (operator): ______________________ Date/time (CT): __________

## 9. Out of scope

- Renaming TRs/TESTs or relaxing `TrIdPattern` / `FrIdPattern` in McpServer.
- Recovery API extension for atomic rename.
- Wiki / Functional-Requirements.md link rewrites (track as follow-up).
- git push of Slice5b or this plan.

## 10. References

- `docs/receipts/requirements-recovery/slice5b-mapping-chase-receipts.txt`
- `docs/receipts/requirements-recovery/slice5b-mapping-chase-20260929T084706/permanent-skips-legacy-fr.json`
- `docs/receipts/requirements-recovery/slice5b-mapping-chase-20260929T084706/skipped-id-analysis.json` (`proposedNorm`)
- BDPv4 process: `F:\GitHub\McpServer\docs\Development-Process-draft-v4.md`

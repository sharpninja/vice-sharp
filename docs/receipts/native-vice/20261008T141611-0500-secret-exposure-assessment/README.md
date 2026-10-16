# F-02 secret-exposure evidence assessment

Captured locally on PAYTON-LEGION2 at 2026-10-08T14:16:11-05:00. This is a read-only assessment, not credential remediation or an accepted hostile-review verdict.

## Observations

- `assessment.json` records sanitized comparisons only. The active label in Global `MEMORY-LAB-002` version 5 equals `keyId` in the operator-authorized local credential file, not its full `authKey`. Both fields were nonempty. No secret or identifier value is reproduced here.
- The selected tool-output evidence contains three identifier disclosures. The full current local Tailscale auth key and current workspace API key were absent from the 22 selected outputs. The selection covers the reported marker window and outputs containing the memory ID, not every transcript or earlier credential.
- The retained marker output at 2026-10-08T07:15:14.961-05:00, call `call_04652996e80c4e4cacd27dc5ebf6b247`, contains one redaction placeholder and no comparable historical API-key value. This does not disprove the previously reported exposure.
- Current marker metadata places server startup after that output. The marker says keys rotate on restart. These facts support an inference, not direct proof that the exposed historical key is invalid.
- The original handoff was copied to `HANDOFF.before.md` and verified by SHA-256 before editing. The original remains in place.

## Method and limits

The assessment read the fresh supported MCP memory response, current marker, authorized local credential file and retained local tool-output records. It used private exact string comparisons and published only booleans, counts, timestamps, paths and call IDs. The earlier `tskey-` regex returned no matches because the memory labels are identifiers; that empty regex result was not treated as proof of no exposure. No hidden reasoning was inspected or copied. No complete raw transcript or credential file was copied into this receipt directory.

No remote provider query was performed. No credential, service, plugin, native source, implementation or acceptance state was changed. This assessment cannot establish provider validity or absence of full-secret exposure outside its explicit selection. The original F-02 finding and all original evidence are preserved.

## Disposition

The operator has been asked whether to leave credentials unchanged with these limitations recorded or prepare a rotation plan for explicit approval. The answer is pending. No rotation, revocation or restart is authorized by this assessment. F-02 is not marked resolved, and the quota-interrupted Opus max review has not accepted this evidence.

Parent MCP session: `Codex-20261007T135626Z-plugin-session`.
Parent turn: `req-20261008T190255Z-prompt-b8db`.

Repin the changed handoff and this additive receipt before resuming that review. Do not rewrite its original verdict, findings or scores.

Co-Authored-By: Codex <noreply@openai.com>

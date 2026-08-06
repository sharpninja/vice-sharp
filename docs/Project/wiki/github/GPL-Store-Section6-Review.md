# GPL-2.0-or-later vs Microsoft Store: section-6 review

**Slice:** DOCS-STORE-001 / PLAN-XBOXUWP S42 Phase 0  
**Date:** 2026-08-05  
**Product:** ViceSharp Xbox UWP head (Microsoft Store submission)  
**License:** GPL-2.0-or-later (VICE derivative)

This memo is the Phase 0 legal gate from the Store finish plan. It is **not** a substitute for independent legal counsel. It records the project facts, the tension with the Store license, mitigations already implemented, and the operator decision needed before any public Store publish.

---

## 1. Facts about this distribution

| Fact | Evidence |
| --- | --- |
| License | `GPL-2.0-or-later` (`Directory.Build.props` PackageLicenseExpression, root COPYING / LICENSE) |
| Upstream | VICE Team; clean-room C# port; attribution in `THIRD_PARTY_NOTICES.md` and About |
| Corresponding source | Public GitHub mirror `https://github.com/sharpninja/vice-sharp` (`AboutInfo.SourceUrl`); Azure DevOps is primary of record |
| In-app source offer | About page binds `LicenseIdentifier`, `AttributionText`, `SourceOfferText`, `SourceUrl` |
| Bundled license text | MSIX Content under `Licenses/` (COPYING + THIRD_PARTY_NOTICES) |
| Bundled VICE data | GPL `*.vkm` keymaps only under `Assets/vice-data/C64/` |
| Commodore ROMs | **Never** packaged; runtime user import or verified HTTPS fetch (FR-XROM-003) |
| Automated gate | `XboxGplComplianceTests` (Category=Xbox) |

## 2. GPL section 6 concern (what we are deciding)

GPL-2.0 section 6 (and the GPL-2.0-or-later election) requires that when you distribute object code, you do not impose further restrictions on the recipient's exercise of rights granted by the GPL. The Microsoft Store Standard Application License Terms (SALT) and Partner Center distribution terms apply additional conditions on the binary as delivered through the Store (revocation, geo, age, platform sandbox, and terms that are not the GPL itself).

Common open-source community analysis of "GPL apps on app stores" is mixed:

- Many GPL desktop apps ship on the Microsoft Store and similar catalogs with a clear source offer and no attempt to relicense the code under a proprietary EULA owned by the publisher.
- Critics argue that any exclusive Store EULA that restricts reverse engineering, redistribution of the Store package, or further sharing of the binary can conflict with section 6.
- Microsoft has historically allowed GPL-licensed apps when the publisher still offers source and does not claim to strip GPL rights from the covered work itself.

**Inference (not legal advice):** The residual risk is real but manageable for a free, source-available emulator if mitigations below are enforced and the publisher does not add extra proprietary license language that pretends the app is closed source.

## 3. Mitigations (required for GO)

These are **already implemented** or **required on the Store listing**:

1. **Written source offer in-app** (About): license id, VICE attribution, complete corresponding source at `AboutInfo.SourceUrl`.
2. **License files in the package** under `Licenses/`.
3. **Listing text** must state GPL-2.0-or-later, point to the source URL, and state that Store terms apply to the Store delivery channel without changing the license of the covered source.
4. **No proprietary "all rights reserved" product EULA** that contradicts GPL for the ViceSharp code. Optional Store privacy policy is separate (data practices, not copyright relicensing).
5. **No Commodore ROMs** in the package; listing states user supplies ROMs/media.
6. **Parallel free distribution** remains: GitHub releases + winget MSI (already live for 1.2.1). Users are never Store-only for obtaining binaries or source.
7. **GitHub release hygiene:** continue attaching source archive (`git archive` or equivalent) alongside binaries when tagging Store-correlated builds.

Recommended listing boilerplate (short):

> ViceSharp is free software under GPL-2.0-or-later (a VICE-derived work). Complete corresponding source: https://github.com/sharpninja/vice-sharp. The Microsoft Store delivery channel is subject to Microsoft's terms; it does not relicense the ViceSharp source. No Commodore ROMs are included; provide your own legally obtained ROMs and software.

## 4. Residual risks (accept or stop)

- Certification or legal challenge from a third party alleging section-6 conflict with SALT.
- Store review questions on emulator policy and ROM handling (policy risk, not GPL).
- Publisher account liability if listing language implies endorsement by Commodore/Microsoft.

**Residual risk if GO:** accepted by the operator as a product decision, with winget/GitHub as the primary GPL-faithful channel and Store as an additional convenience channel.

## 5. Decision

| Option | Meaning |
| --- | --- |
| **GO with mitigations** | Proceed with Partner Center reservation, pipeline, cert, publish. Keep mitigations 1-7. |
| **NO-GO** | Do not submit to Microsoft Store. Keep GitHub + winget only. |
| **GO delayed** | Finish counsel review before Phase 5 publish; continue Phases 1-4 prep. |

### Operator conclusion

**Recommended:** **GO with mitigations** (section 3).

**Recorded status:** **GO with mitigations** for plan execution and submission prep (operator directed plan execution 2026-08-05: "yes, go!"). Formal counsel review remains optional; Phase 5 **publish approval** still requires a human on the `xbox-store` ADO environment.

| Field | Value |
| --- | --- |
| Decision | GO with mitigations |
| Decided by | Payton Byrd (operator), via plan execution approval |
| Date | 2026-08-05 |
| Counsel review | Not completed (optional); project risk accepted for free GPL emulator + dual channel |

If this decision is later reversed to NO-GO, stop before approving the PublishToStore stage; leave Build-stage artifacts only.

## 6. Exit criteria for Phase 0

- [x] This memo exists in-repo.
- [x] About source offer and license packaging are documented and test-gated.
- [x] Explicit operator decision recorded (GO with mitigations).
- [ ] Phase 5 human approval still required before `msstore publish`.

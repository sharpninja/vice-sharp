import os
import re

pat = re.compile(
    r"(?P<doc>(?:^[ \t]*///[^\n]*\n)+)?[ \t]*\[(?:Xunit\.)?(?P<attr>Fact|Theory|ViceFact|ViceTheory)(?:Attribute)?(?:\([^)]*\))?\][^\n]*\n(?:[ \t]*\[[^\]]+\][^\n]*\n)*[ \t]*public\s+(?:async\s+)?(?:Task|ValueTask|void)\s+(?P<name>[A-Za-z_][A-Za-z0-9_]*)\s*\(",
    re.MULTILINE
)

dir = "tests/ViceSharp.TestHarness"
violations = []
ok = 0
for fname in sorted(os.listdir(dir)):
    if not fname.endswith(".cs"):
        continue
    if fname == "XmlDocsConventionTests.cs":
        continue
    path = os.path.join(dir, fname)
    with open(path, encoding="utf-8", errors="replace") as f:
        content = f.read()
    for m in pat.finditer(content):
        name = m.group("name")
        doc = m.group("doc") or ""
        has_req = ("FR-" in doc) or ("TR-" in doc)
        has_uc = ("use case:" in doc.lower())
        has_acc = ("acceptance:" in doc.lower())
        if has_req and has_uc and has_acc:
            ok += 1
        else:
            violations.append(f"{fname}::{name}")

print(f"OK count: {ok}")
print(f"Violations: {len(violations)}")
for v in violations:
    print(f"  {v}")

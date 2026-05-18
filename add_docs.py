"""Tool to insert XMLDOCS comments before [Fact]/[Theory]/etc attributes.

Reads a file and a list of (test_name, doc_summary, fr, uc, acc) tuples.
For each entry where the test currently lacks a /// summary directly
above its attribute, prepend a four-line XMLDOC summary.

Usage:
  python add_docs.py <path> <yaml_recipe>

This stays a one-shot helper - we just need a fast way to bulk-insert
docs for the dozens of identical wrapper tests.
"""

import re
import sys

def insert_docs(path, entries):
    with open(path, encoding="utf-8") as f:
        content = f.read()
    original = content
    # entries: list of (name, doc_text) tuples. doc_text is the multi-line
    # XML doc summary without trailing newline.
    # We find each name's attribute line preceded by no /// line.
    for name, doc in entries:
        # build the regex to match the attribute and method signature
        # We accept [Fact] or [Theory] or [ViceFact] or [ViceTheory] with any optional Attribute suffix.
        # The previous physical line must NOT start with /// (we'd be duplicating).
        attr_re = re.compile(
            rf"(?<![/])    \[(?:Fact|Theory|ViceFact|ViceTheory)(?:Attribute)?(?:\([^)]*\))?\]\n(?:    \[[^\]]+\]\n)*    public\s+(?:async\s+)?(?:Task|ValueTask|void)\s+{re.escape(name)}\s*\(",
            re.MULTILINE
        )
        m = attr_re.search(content)
        if not m:
            print(f"  WARN: could not locate {name}", file=sys.stderr)
            continue
        start = m.start()
        # Walk backwards to check if the previous non-blank line is a doc comment.
        # If so, skip (already has docs).
        prev_end = content.rfind("\n", 0, start)
        # Look at last line content
        prev_line_start = content.rfind("\n", 0, prev_end) + 1
        prev_line = content[prev_line_start:prev_end].strip() if prev_end > 0 else ""
        if prev_line.startswith("///"):
            # Already documented
            continue
        # Insert doc block right before the attribute line
        insert = f"    /// <summary>\n"
        for line in doc.split("\n"):
            insert += f"    /// {line}\n"
        insert += "    /// </summary>\n"
        content = content[:start] + insert + content[start:]
    if content != original:
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(content)
    return content != original


if __name__ == "__main__":
    pass

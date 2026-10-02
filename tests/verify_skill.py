#!/usr/bin/env python3
"""Verify the portable MZpack Strategies API 2.4.17 skill bundle."""

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
EXPECTED_FILES = (
    "SKILL.md",
    "VERSION",
    "skill-manifest.json",
    "AGENTS.md",
    "docs/api-surface.md",
    "docs/catalog.md",
    "docs/pitfalls.md",
    "templates/StrategyTemplate/StrategyTemplate.csproj",
    "templates/DashboardTemplate/DashboardTemplate.csproj",
    "templates/ControlPanelTemplate/ControlPanelTemplate.csproj",
)
FUTURE_API_PATTERNS = (
    r"public\s+override\s+void\s+DeclareRequirements\s*\(",
    r"FootprintCapabilities\.",
    r"\bRequire\s*\(\s*footprint",
)


def normalized(value):
    return value.replace("\r\n", "\n").replace("\r", "\n").rstrip("\n")


def fail(errors, message):
    errors.append(message)


def git_show(product_root, product_ref, path):
    result = subprocess.run(
        ["git", "-c", "safe.directory={0}".format(product_root), "-C", str(product_root), "show", "{0}:{1}".format(product_ref, path)],
        text=True,
        encoding="utf-8",
        capture_output=True,
    )
    if result.returncode:
        raise RuntimeError(result.stderr.strip() or "git show failed for {0}".format(path))
    return result.stdout


def verify_bundle(errors):
    for relative in EXPECTED_FILES:
        if not (ROOT / relative).is_file():
            fail(errors, "missing required file: {0}".format(relative))

    manifest = json.loads((ROOT / "skill-manifest.json").read_text(encoding="utf-8"))
    version = (ROOT / "VERSION").read_text(encoding="utf-8").strip()
    if manifest["version"] != version:
        fail(errors, "VERSION and skill-manifest.json disagree")
    if manifest["strategiesApiVersion"] != "2.4.17":
        fail(errors, "skill must target Strategies API 2.4.17")

    skill = (ROOT / "SKILL.md").read_text(encoding="utf-8")
    if not re.match(r"\A---\nname: mzpack-strategies\ndescription: .+\n---\n", skill, re.DOTALL):
        fail(errors, "SKILL.md front matter is incomplete")

    sample_count = len(list((ROOT / "samples").rglob("*.cs")))
    template_count = len([item for item in (ROOT / "templates").iterdir() if item.is_dir()])
    pitfall_count = len(re.findall(r"^## \d+\. ", (ROOT / "docs/pitfalls.md").read_text(encoding="utf-8"), re.MULTILINE))
    if sample_count != 16:
        fail(errors, "expected 16 C# samples, found {0}".format(sample_count))
    if template_count != 3:
        fail(errors, "expected 3 templates, found {0}".format(template_count))
    if pitfall_count != 14:
        fail(errors, "expected 14 pitfalls, found {0}".format(pitfall_count))

    for relative in ("AGENTS.md", "docs/api-surface.md", "docs/pitfalls.md"):
        text = (ROOT / relative).read_text(encoding="utf-8")
        for pattern in FUTURE_API_PATTERNS:
            if re.search(pattern, text):
                fail(errors, "{0} instructs an unsupported API 2.4.18 call: {1}".format(relative, pattern))


def verify_product_snapshot(errors, product_root, product_ref):
    manifest = json.loads((ROOT / "skill-manifest.json").read_text(encoding="utf-8"))
    try:
        base = git_show(product_root, product_ref, "MZpack.NT8/Algo/MZpackStrategyBase.cs")
    except RuntimeError as error:
        fail(errors, str(error))
        return

    match = re.search(r"Version\s*=\s*@?\"([^\"]+)\"", base)
    if not match or match.group(1) != manifest["strategiesApiVersion"]:
        actual = match.group(1) if match else "not found"
        fail(errors, "product ref exposes API {0}, expected {1}".format(actual, manifest["strategiesApiVersion"]))

    mapping = json.loads((ROOT / "tests/api-2.4.17-sample-sources.json").read_text(encoding="utf-8"))
    if len(mapping) != 16:
        fail(errors, "sample source manifest must map all 16 samples")
    for corpus_path, source_path in mapping.items():
        sample = ROOT / corpus_path
        if not sample.is_file():
            fail(errors, "sample source manifest references missing {0}".format(corpus_path))
            continue
        try:
            product_text = git_show(product_root, product_ref, source_path)
        except RuntimeError as error:
            fail(errors, str(error))
            continue
        if normalized(sample.read_text(encoding="utf-8")) != normalized(product_text):
            fail(errors, "sample drift: {0} != {1}@{2}".format(corpus_path, source_path, product_ref))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--product-root", type=Path, help="MZpack product repository for snapshot parity")
    parser.add_argument("--product-ref", default="API-2.4.17", help="immutable product release ref")
    args = parser.parse_args()

    errors = []
    verify_bundle(errors)
    if args.product_root:
        verify_product_snapshot(errors, args.product_root.resolve(), args.product_ref)
    if errors:
        for error in errors:
            print("FAIL: {0}".format(error))
        return 1
    print("PASS: MZpack AI Skill {0} for Strategies API 2.4.17".format((ROOT / "VERSION").read_text(encoding="utf-8").strip()))
    return 0


if __name__ == "__main__":
    sys.exit(main())

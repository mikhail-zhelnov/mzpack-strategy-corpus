#!/usr/bin/env python3
"""Verify the portable MZpack Strategies skill bundle and its pinned product snapshots."""

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
    "README.md",
    "CHANGELOG.md",
    "Directory.Build.props",
    "docs/api-surface.md",
    "docs/release-2.4.18.md",
    "docs/signal-probe.md",
    "docs/filter-calibration.md",
    "docs/catalog.md",
    "docs/pitfalls.md",
    "templates/StrategyTemplate/StrategyTemplate.csproj",
    "templates/DashboardTemplate/DashboardTemplate.csproj",
    "templates/ControlPanelTemplate/ControlPanelTemplate.csproj",
)
UNSUPPORTED_CSHARP_PATTERNS = (
    r"\bEntrySignals\s*\.\s*Add\s*\(",
    r"\.\s*AddSignal\s*\(",
)
UNSUPPORTED_STANDALONE_PATTERNS = (
    r"\b(?:public|protected\s+internal)\s+override\s+void\s+(?:OnBeforeSignalProbePass|OnConfigureSignalProbe)\s*\(",
)


def normalized(value):
    return value.lstrip("\ufeff").replace("\r\n", "\n").replace("\r", "\n").rstrip("\n")


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


def sample_mapping_path(manifest):
    return ROOT / "tests/api-{0}-sample-sources.json".format(manifest["strategiesApiVersion"])


def verify_bundle(errors, manifest):
    missing = False
    for relative in EXPECTED_FILES:
        if not (ROOT / relative).is_file():
            fail(errors, "missing required file: {0}".format(relative))
            missing = True
    if missing:
        return

    version = (ROOT / "VERSION").read_text(encoding="utf-8").strip()
    if manifest["version"] != version:
        fail(errors, "VERSION and skill-manifest.json disagree")
    api_version = manifest["strategiesApiVersion"]
    if api_version != "2.4.18":
        fail(errors, "this skill release must target Strategies API 2.4.18")
    if not re.fullmatch(r"[0-9a-f]{40}", manifest.get("productSourceRef", "")):
        fail(errors, "productSourceRef must pin a full immutable product commit")
    if manifest.get("corpusReleaseTag") != "skill-v{0}".format(version):
        fail(errors, "corpusReleaseTag must match the skill VERSION")
    if manifest.get("corpusReleaseTagStatus") not in ("planned", "published"):
        fail(errors, "corpusReleaseTagStatus must distinguish a planned tag from a published release")
    if manifest.get("framework") != ".NET Framework 4.8" or manifest.get("language") != "C# 7.3":
        fail(errors, "skill requires .NET Framework 4.8 and C# 7.3")

    skill = (ROOT / "SKILL.md").read_text(encoding="utf-8")
    if not re.match(r"\A---\nname: mzpack-strategies\ndescription: [^\n]+\n---\n", skill):
        fail(errors, "SKILL.md front matter is incomplete")

    samples = sorted((ROOT / "samples").rglob("*.cs"))
    sample_count = len(samples)
    template_count = len([item for item in (ROOT / "templates").iterdir() if item.is_dir()])
    pitfalls = (ROOT / "docs/pitfalls.md").read_text(encoding="utf-8")
    pitfall_numbers = [int(value) for value in re.findall(r"^## (\d+)\. ", pitfalls, re.MULTILINE)]
    if sample_count != 17:
        fail(errors, "expected 17 C# samples, found {0}".format(sample_count))
    if template_count != 3:
        fail(errors, "expected 3 templates, found {0}".format(template_count))
    if pitfall_numbers != list(range(1, 18)):
        fail(errors, "expected 17 consecutively numbered pitfalls, found {0}".format(pitfall_numbers))

    for relative in ("SKILL.md", "README.md", "AGENTS.md", "docs/api-surface.md"):
        text = (ROOT / relative).read_text(encoding="utf-8")
        if api_version not in text:
            fail(errors, "{0} does not identify API {1}".format(relative, api_version))
    for relative in ("AGENTS.md", "docs/api-surface.md", "docs/pitfalls.md"):
        text = (ROOT / relative).read_text(encoding="utf-8")
        if "DeclareRequirements()" not in text or "FootprintCapabilities." not in text:
            fail(errors, "{0} must document API 2.4.18 capability declarations".format(relative))

    readme = (ROOT / "README.md").read_text(encoding="utf-8")
    if "Skill **{0}**".format(version) not in readme:
        fail(errors, "README.md skill version disagrees with VERSION")
    install_tags = re.findall(r"git clone --branch (\S+)", readme)
    if len(install_tags) != 3 or any(tag != manifest["corpusReleaseTag"] for tag in install_tags):
        fail(errors, "all three README installation commands must pin corpusReleaseTag")
    if manifest.get("corpusReleaseTagStatus") == "planned" and "publication target" not in readme:
        fail(errors, "README.md must disclose that the skill release tag is planned")
    if "## {0} ".format(version) not in (ROOT / "CHANGELOG.md").read_text(encoding="utf-8"):
        fail(errors, "CHANGELOG.md has no entry for VERSION")

    props = (ROOT / "Directory.Build.props").read_text(encoding="utf-8")
    if "<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>" not in props:
        fail(errors, "Directory.Build.props must target .NET Framework 4.8")
    if "<LangVersion>7.3</LangVersion>" not in props:
        fail(errors, "Directory.Build.props must enforce C# 7.3")

    mapping_file = sample_mapping_path(manifest)
    if not mapping_file.is_file():
        fail(errors, "missing source manifest: {0}".format(mapping_file.relative_to(ROOT).as_posix()))
    else:
        mapping = json.loads(mapping_file.read_text(encoding="utf-8"))
        actual = set(path.relative_to(ROOT).as_posix() for path in samples)
        if set(mapping) != actual:
            fail(errors, "sample source manifest must map every C# sample exactly once")
        for source_path in mapping.values():
            if not source_path.startswith("MZpack.NT8/") or ".." in Path(source_path).parts:
                fail(errors, "sample source is outside the working product project: {0}".format(source_path))

    for path in samples + sorted((ROOT / "templates").rglob("*.cs")):
        text = path.read_text(encoding="utf-8-sig")
        relative = path.relative_to(ROOT).as_posix()
        patterns = UNSUPPORTED_CSHARP_PATTERNS
        if relative.startswith("templates/"):
            patterns += UNSUPPORTED_STANDALONE_PATTERNS
        for pattern in patterns:
            if re.search(pattern, text):
                fail(errors, "{0} uses an unsupported API call or standalone override: {1}".format(relative, pattern))


def verify_product_snapshot(errors, product_root, product_ref, manifest):
    try:
        base = git_show(product_root, product_ref, "MZpack.NT8/Algo/MZpackStrategyBase.cs")
    except RuntimeError as error:
        fail(errors, str(error))
        return

    match = re.search(r"Version\s*=\s*@?\"([^\"]+)\"", base)
    if not match or match.group(1) != manifest["strategiesApiVersion"]:
        actual = match.group(1) if match else "not found"
        fail(errors, "product ref exposes API {0}, expected {1}".format(actual, manifest["strategiesApiVersion"]))

    mapping_file = sample_mapping_path(manifest)
    if not mapping_file.is_file():
        return
    try:
        mapping = json.loads(mapping_file.read_text(encoding="utf-8"))
    except (OSError, ValueError) as error:
        fail(errors, "invalid sample source manifest: {0}".format(error))
        return
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
        try:
            sample_text = sample.read_text(encoding="utf-8")
        except OSError as error:
            fail(errors, "cannot read mapped sample {0}: {1}".format(corpus_path, error))
            continue
        if normalized(sample_text) != normalized(product_text):
            fail(errors, "sample drift: {0} != {1}@{2}".format(corpus_path, source_path, product_ref))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--product-root", type=Path, help="MZpack product repository for snapshot parity")
    parser.add_argument("--product-ref", help="override the manifest's immutable productSourceRef for diagnostics")
    args = parser.parse_args()

    errors = []
    try:
        manifest = json.loads((ROOT / "skill-manifest.json").read_text(encoding="utf-8"))
        verify_bundle(errors, manifest)
    except (OSError, ValueError, KeyError) as error:
        fail(errors, "invalid or incomplete skill bundle: {0}".format(error))
        manifest = None
    if args.product_root:
        if manifest is not None:
            product_ref = args.product_ref or manifest.get("productSourceRef")
            if product_ref:
                verify_product_snapshot(errors, args.product_root.resolve(), product_ref, manifest)
    if errors:
        for error in errors:
            print("FAIL: {0}".format(error))
        return 1
    print("PASS: MZpack AI Skill {0} for Strategies API {1}".format(manifest["version"], manifest["strategiesApiVersion"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())

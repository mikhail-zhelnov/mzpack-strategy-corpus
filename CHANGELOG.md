# Changelog

## 1.1.0 — 2026-10-06 (unpublished)

- Updated the corpus to MZpack Strategies API 2.4.18, pinned to product source commit
  `185d67621238dd0ea1f08c1a358f7c84da0600d4`.
- Documented signal capability declarations, independent probe outcomes/ranks, session filter calibration,
  built-in signals, DOM-pressure filters, realtime export, dashboard controls and DOM aggregation.
- Corrected signal constructor/reset guidance and probe-hook access in standalone strategies.
- Enforced C# 7.3 in the shared build props.
- Refreshed the product source snapshots and added the indicator-data export example.
- Expanded the verifier to check release metadata, source provenance, sample coverage, and the
  standalone C# templates.
- The target publication tag is `skill-v1.1.0`; this entry does not assert that it has been published.

## 1.0.3 — 2026-10-02

- Added a stable latest-release ZIP URL for documentation and site pages.
- Added automatic GitHub Release publishing for `skill-v*` tags.
- Corrected the Codex install command to create its skill directory first.

## 1.0.2 — 2026-10-02

- Pin all installation commands to the matching immutable skill release tag.

## 1.0.1 — 2026-10-02

- Documented project-local PowerShell installs for Claude Code and Cursor.

## 1.0.0 — 2026-10-02

- First portable MZpack AI Skill bundle for MZpack Strategies API 2.4.17.
- Added a release manifest, compatibility boundary, and deterministic corpus verifier.
- Corrected the agent guidance to configure gated indicator data directly, which is the API 2.4.17 mechanism.

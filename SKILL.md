---
name: mzpack-strategies
description: Create or modify NinjaTrader 8 strategies against MZpack Strategies API 2.4.17 using the bundled API guide, templates, examples, and failure-mode guidance. Use for MZpack strategy code, not for the Indicators-only product or a different Strategies API version.
---

Create a buildable MZpack Strategies API 2.4.17 project that reflects the user's stated trading rules.

## Compatibility boundary

This skill targets only MZpack Strategies API 2.4.17. Before generating code, confirm the installed
`MZpackStrategyBase.Version` is `2.4.17`. If it is absent or different, stop and tell the user to install the
matching skill/API release; do not guess compatibility.

Do not generate API members absent from 2.4.17, including `DeclareRequirements()` and `Require(...)`.
For gated footprint data, configure the `StrategyFootprintIndicator` directly in the host as described in
`docs/pitfalls.md` section 1.

## Workflow

1. Read `AGENTS.md` in full. It is the canonical implementation workflow for this release.
2. Read `docs/catalog.md`, then only the sample and documentation relevant to the requested pattern. Read
   `docs/pitfalls.md` before diagnosing a strategy that compiles but does not signal.
3. Start a new standalone strategy from the template that matches the requested UI: `StrategyTemplate`,
   `DashboardTemplate`, or `ControlPanelTemplate`. Keep `SPEC.md` beside the strategy and update it before
   changing behavior.
4. Use `docs/api-surface.md` as the API contract. Do not invent members, copy product-only `#if` guards into
   a customer strategy, or put trading logic in the NinjaScript host.
5. Build first with `-p:DeployToNinjaTrader=false`. Do not launch NinjaTrader or enable live trading. A user
   must explicitly request any deployment or trading automation.

Report the chosen template, assumptions, generated or changed files, and the build result. Do not claim that a
strategy is profitable or that a backtest proves live performance.

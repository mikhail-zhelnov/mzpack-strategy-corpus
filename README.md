# MZpack AI Skill — Strategy Corpus

Everything an AI coding agent needs to write NinjaTrader 8 strategies and indicators on the
MZpack API: the API surface, a written guide to the framework, 17 worked examples and three
buildable templates.

This repository is an installable Agent Skill. Point your agent at this folder and ask it for what you want.
It will know the API instead of guessing at it.

**Requires MZpack Strategies** (or Full Suite) — not the Indicators package alone. Everything here
is built on `MZpackStrategyBase` and the `MZpack.NT8.Algo` engine, which ship in
`MZpack.NT8.Pro.dll`, and that assembly comes with Strategies. Without it nothing in this corpus
compiles. If you own Indicators only, see
[Product Selection](https://www.mzpack.pro/product-selection/).

Skill **1.1.0** is written against **Strategies API 2.4.18**. Strategies has its own version line,
separate from the Indicators one — so a 4.x Indicators installation tells you nothing about
whether you have the strategy engine.

Also required: NinjaTrader 8, .NET Framework 4.8, and MSBuild (Visual Studio Build Tools are
enough — see below). The language version is C# 7.3.

This working tree prepares the `skill-v1.1.0` release. That tag is the publication target, not a
claim that the release is already available. Until publication, install from this local corpus;
the pinned clone commands below become available when the tag is published.

---

## Quick start

```
1. Install or clone this skill into your agent's skill directory, or place it next to your strategy project.
2. Point your agent at `SKILL.md` (see below).
3. Copy templates/StrategyTemplate into your own folder, rename the project and namespace.
4. Ask: "add a delta divergence signal to this strategy, following AGENTS.md"
5. Build:  msbuild YourStrategy.csproj /p:DeployToNinjaTrader=false
```

### Download the latest release ZIP

Download [the latest MZpack AI Skill ZIP](https://github.com/mikhail-zhelnov/mzpack-strategy-corpus/releases/latest/download/mzpack-ai-skill.zip),
then extract it into your agent's skill directory so that `SKILL.md` is at
`.codex\skills\mzpack-strategies\SKILL.md`, `.claude\skills\mzpack-strategies\SKILL.md`, or
`.cursor\skills\mzpack-strategies\SKILL.md`. This URL always supplies the latest published skill;
the versioned tags remain available when you need a specific, reproducible version. Until
`skill-v1.1.0` is published, this URL may still supply a previous skill/API version; check its manifest.

## Building, if you have never built a NinjaScript add-on

You need MSBuild. Three ways, easiest first.

**Install Visual Studio 2026 Community** — it is free. Open
`templates/StrategyTemplate/StrategyTemplate.csproj` in it and build with Ctrl+Shift+B. Nothing to
configure, and you get a debugger and IntelliSense over the MZpack API, which is worth having while
you are still learning it.

**Already have Visual Studio?** Open **Developer PowerShell for VS 2026** from the Start menu.
MSBuild is on `PATH` there — it is *not* on `PATH` in an ordinary PowerShell window, which is the
single most common first stumble:

```
msbuild templates\StrategyTemplate\StrategyTemplate.csproj /p:DeployToNinjaTrader=false
```

**Command line only, no IDE?** Install **Build Tools for Visual Studio** with the
*.NET desktop build tools* workload — that is MSBuild without the IDE. Then use the Developer
PowerShell as above.

If you would rather stay in an ordinary shell, `dotnet msbuild <project>.csproj` usually works.
`docs/pitfalls.md` §11 has the recipe for locating MSBuild directly, for scripts and CI.

> The commands above compile without deployment. When deployment is requested, close NinjaTrader
> first: it can hold the destination DLL open and the copy target then fails even after a successful
> compile. With NinjaTrader closed, build with `/p:DeployToNinjaTrader=true` to copy the DLL to
> `bin\Custom`. Ask before closing a running trading platform.

> Build the **template**, not the repository root. `samples/` is deliberately not part of any
> project, and there is nothing to build at the top level.

### Codex

Codex discovers a project skill under `.codex/skills/`, or a personal skill under your Codex skills directory.
For a project-local install, clone this repository as:

```powershell
New-Item -ItemType Directory -Force .codex\skills | Out-Null
git clone --branch skill-v1.1.0 --depth 1 https://github.com/mikhail-zhelnov/mzpack-strategy-corpus.git .codex\skills\mzpack-strategies
```

### Claude Code

Claude Code discovers a project skill under `.claude/skills/`. From the root of the strategy
project, install it with:

```powershell
New-Item -ItemType Directory -Force .claude\skills | Out-Null
git clone --branch skill-v1.1.0 --depth 1 https://github.com/mikhail-zhelnov/mzpack-strategy-corpus.git .claude\skills\mzpack-strategies
```

Then start Claude Code and ask for an MZpack strategy task, or invoke `/mzpack-strategies`.

If the corpus already lives in the working tree, a `CLAUDE.md` at the project root can instead
point Claude Code to it:

```markdown
Read ./mzpack-corpus/AGENTS.md before writing any MZpack code.
API surface: ./mzpack-corpus/docs/api-surface.md
Worked examples: ./mzpack-corpus/samples/
Known pitfalls: ./mzpack-corpus/docs/pitfalls.md
```

### Existing Codex projects

`AGENTS.md` remains a compatibility entry point when the corpus sits in the working tree. If the
skill is kept elsewhere, install it as above rather than copying its instructions into every project.

### Cursor

Cursor discovers a project skill under `.cursor/skills/`. From the root of the strategy project,
install it with:

```powershell
New-Item -ItemType Directory -Force .cursor\skills | Out-Null
git clone --branch skill-v1.1.0 --depth 1 https://github.com/mikhail-zhelnov/mzpack-strategy-corpus.git .cursor\skills\mzpack-strategies
```

Open a new Agent chat and ask for an MZpack strategy task, or invoke `/mzpack-strategies`.

---

## What is here

| Path | What it is |
|---|---|
| `AGENTS.md` | **Start here.** How an MZpack strategy is put together: host, algo class, signals, the signals tree, entries, risk, the signal probe, build and deploy. |
| `docs/api-surface.md` | Types, members and enums of `MZpack.NT8.Algo` in one place. |
| `docs/release-2.4.18.md` | Built-in signals, streaming CSV, DOM-pressure filters, dashboard and DOM aggregation. |
| `docs/signal-probe.md` | Independent signal observations, outcomes, ranks and report limits. |
| `docs/filter-calibration.md` | Session baselines, readiness, thresholds and percentile ranks. |
| `docs/pitfalls.md` | Things that compile and then do nothing, and why. Read this before you debug. |
| `docs/catalog.md` | Which sample to open for which pattern. |
| `samples/` | 17 worked examples, with a README explaining the techniques in each group. |
| `templates/` | Three buildable scaffolds: plain strategy, Pattern Dashboard, Control Panel. |
| `Directory.Build.props` | Central paths to NinjaTrader and MZpack. Edit once, or set the environment variables. |

## Set your paths once — if you need to at all

**Most people do not.** `Directory.Build.props` already assumes a standard installation:

```
NinjaTrader     %ProgramFiles%\NinjaTrader 8
Your NT8 data   %USERPROFILE%\Documents\NinjaTrader 8
MZpack          %USERPROFILE%\Documents\NinjaTrader 8\bin\Custom\MZpack.NT8.Pro.dll
```

If that is where yours live, build the template and skip this section. Come back only if the build
reports that it cannot find `MZpack.NT8.Pro` or `NinjaTrader.Core`.

Every reference in every project comes from that one file — never from a `HintPath` in a `.csproj`.
There are two ways to correct it.

### The quick way: edit the file

Open `Directory.Build.props` and change the three defaults at the top:
`NinjaTraderInstall`, `NinjaTraderUser`, `MZpackDll`. Done — but the edit belongs to this copy of
the corpus, and you will redo it the next time you pull or unzip a new version.

### The durable way: environment variables

Set them once for your Windows account and every project you ever build picks them up, on this
machine, forever. Nothing to edit and nothing to redo.

Paste this into PowerShell. The first three lines are the only ones you might need to change, and
`Test-Path` tells you whether they are right **before** anything is written:

```powershell
$nt     = "$env:ProgramW6432\NinjaTrader 8"
$ntUser = "$env:USERPROFILE\Documents\NinjaTrader 8"
$mzpack = "$ntUser\bin\Custom\MZpack.NT8.Pro.dll"

Test-Path $nt, $ntUser, $mzpack        # want three True; if not, fix the paths above and re-run

[Environment]::SetEnvironmentVariable("NINJATRADER_INSTALL", $nt,     "User")
[Environment]::SetEnvironmentVariable("NINJATRADER_USER",    $ntUser, "User")
[Environment]::SetEnvironmentVariable("MZPACK_DLL",          $mzpack, "User")
```

A `False` here is worth a minute of your time. Setting a path that does not exist succeeds
silently, and the mistake surfaces much later as `NinjaTrader.Core could not be found`, where it is
far harder to connect to its cause.

Or without a terminal: press Win, type *environment variables*, choose **Edit environment variables
for your account**, and add the three under **User variables**.

**Close and reopen your shell, Visual Studio included, afterwards.** Windows hands environment
variables to processes when they start, so anything already running will not see the new values —
which looks exactly like the setting not having worked.

In the new window, `$env:MZPACK_DLL` should print your path back. If it prints nothing, the
variable was set in a different account or the window is an old one.

## Samples do not compile as they stand

They are verbatim snapshots from the product sources, so they still carry `#if STRAT`,
`#if DATA` and `#if APISAMPLE` directives. Remove the directives when you copy a sample into
your own project. They are kept as-is on purpose: an edited snapshot drifts from the original,
an unedited one does not.

## Version

The corpus is written against **Strategies API 2.4.18** — `MZpackStrategyBase.Version` in the
installed assembly. Check yours against that number before you blame your agent for the code it
produced.

If `MZpack.NT8.Pro.dll` is not present at all, you have the Indicators package rather than
Strategies. The strategy engine is a separate product, and nothing here will build without it.

If it is present but older, some of what the corpus describes will not exist yet and the code will
not compile. Update Strategies first.

The source snapshots are pinned by `productSourceRef` in `skill-manifest.json` to released product
commit `185d67621238dd0ea1f08c1a358f7c84da0600d4`. No `API-2.4.18` Git tag is assumed.
The manifest's `corpusReleaseTag` is the planned skill publication tag; `corpusReleaseTagStatus`
records that it is still `planned`.

For a repository checkout, validate the bundle, then optionally compare every snapshot with the pinned
product source. The `tests/` maintenance tools are excluded from the portable skill ZIP.

```powershell
python tests\verify_skill.py
python tests\verify_skill.py --product-root ..
```

The second command expects the MZpack product repository one directory above this corpus. Both
checks use the manifest; `--product-ref` explicitly overrides the source ref for diagnostics.

---

## Support

**This corpus is provided as is.** It is free, it is not a supported product, and questions about
building your own strategies are not covered by MZpack support.

If you get stuck and want it built for you, that is what the **Coding Service** is for —
see www.mzpack.pro.

**MZpack Research** — measuring and tuning your own signals instead of guessing at them — is in
development. If that is what you actually need, say so: what gets asked for shapes what gets built.

Found a mistake in the corpus, or hit a pitfall that is not written down? Open an issue.
Corrections go straight back into this repository and everybody gets them — `docs/pitfalls.md`
is only as good as the reports behind it.

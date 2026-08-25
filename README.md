# MZpack Strategy Corpus

Everything an AI coding agent needs to write NinjaTrader 8 strategies and indicators on the
MZpack API: the API surface, a written guide to the framework, 16 worked examples and three
buildable templates.

Point your agent at this folder and ask it for what you want. It will know the API instead of
guessing at it.

**Requires:** NinjaTrader 8, MZpack 4.x installed, .NET Framework 4.8, MSBuild (Visual Studio
Build Tools are enough).

---

## Quick start

```
1. Clone or unzip this folder next to your strategy project.
2. Point your agent at it (see below).
3. Copy templates/StrategyTemplate into your own folder, rename the project and namespace.
4. Ask: "add a delta divergence signal to this strategy, following AGENTS.md"
5. Build:  msbuild YourStrategy.csproj
```

### Claude Code

Put the corpus inside your project folder, or add a `CLAUDE.md` at the project root:

```markdown
Read ./mzpack-corpus/AGENTS.md before writing any MZpack code.
API surface: ./mzpack-corpus/docs/api-surface.md
Worked examples: ./mzpack-corpus/samples/
Known pitfalls: ./mzpack-corpus/docs/pitfalls.md
```

### Codex CLI

`AGENTS.md` is picked up automatically when the corpus sits in the working tree. If you keep it
elsewhere, add the same four lines to your project's own `AGENTS.md`.

### Cursor / other agents

Add the corpus folder to the workspace and reference `AGENTS.md` in your rules file.

---

## What is here

| Path | What it is |
|---|---|
| `AGENTS.md` | **Start here.** How an MZpack strategy is put together: host, algo class, signals, the signals tree, entries, risk, the signal probe, build and deploy. |
| `docs/api-surface.md` | Types, members and enums of `MZpack.NT8.Algo` in one place. |
| `docs/pitfalls.md` | Things that compile and then do nothing, and why. Read this before you debug. |
| `docs/catalog.md` | Which sample to open for which pattern. |
| `samples/` | 16 worked examples, one technique each, with a README explaining what it shows. |
| `templates/` | Three buildable scaffolds: plain strategy, Pattern Dashboard, Control Panel. |
| `Directory.Build.props` | Central paths to NinjaTrader and MZpack. Edit once, or set the environment variables. |

## Set your paths once

`Directory.Build.props` holds every reference. Either edit the defaults in it, or set:

```
NINJATRADER_INSTALL   e.g. C:\Program Files\NinjaTrader 8
NINJATRADER_USER      e.g. %USERPROFILE%\Documents\NinjaTrader 8
MZPACK_DLL            e.g. %USERPROFILE%\Documents\NinjaTrader 8\bin\Custom\MZpack.NT8.Pro.dll
```

With the environment variables set, the build is portable and no `.csproj` needs editing.

## Samples do not compile as they stand

They are verbatim snapshots from the product sources, so they still carry `#if STRAT`,
`#if DATA` and `#if APISAMPLE` directives. Remove the directives when you copy a sample into
your own project. They are kept as-is on purpose: an edited snapshot drifts from the original,
an unedited one does not.

## Version

The corpus describes the MZpack 4.x API. If your installed `MZpack.NT8.Pro.dll` is older, the
code your agent writes from this corpus will not compile against it — update MZpack first.

---

## Support

**This corpus is provided as is.** It is free, it is not a supported product, and questions about
building your own strategies are not covered by MZpack support.

If you get stuck: **MZpack Research** (measuring and tuning your own signals) or the
**Coding Service** (we build it for you) — see www.mzpack.pro.

Found a mistake in the corpus, or a pitfall that is not written down? Tell us — corrections go
straight back into this folder and everyone gets them.

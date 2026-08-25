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
msbuild templates\StrategyTemplate\StrategyTemplate.csproj
```

**Command line only, no IDE?** Install **Build Tools for Visual Studio** with the
*.NET desktop build tools* workload — that is MSBuild without the IDE. Then use the Developer
PowerShell as above.

If you would rather stay in an ordinary shell, `dotnet msbuild <project>.csproj` usually works.
`docs/pitfalls.md` §11 has the recipe for locating MSBuild directly, for scripts and CI.

> **Close NinjaTrader before you build.** While it is running it holds the assemblies in
> `bin\Custom`, so the build cannot put yours there — and it still reports success. You get a clean
> build and no strategy in the list. Build with NT8 closed, then start it: the DLL is copied for
> you, there is nothing to move by hand.

> Build the **template**, not the repository root. `samples/` is deliberately not part of any
> project, and there is nothing to build at the top level.

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

If you get stuck and want it built for you, that is what the **Coding Service** is for —
see www.mzpack.pro.

**MZpack Research** — measuring and tuning your own signals instead of guessing at them — is in
development. If that is what you actually need, say so: what gets asked for shapes what gets built.

Found a mistake in the corpus, or hit a pitfall that is not written down? Open an issue.
Corrections go straight back into this repository and everybody gets them — `docs/pitfalls.md`
is only as good as the reports behind it.

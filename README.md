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

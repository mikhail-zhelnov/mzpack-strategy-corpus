# Pitfalls — things that compile and then do nothing

The MZpack API is easy to call and easy to call in a way that silently produces nothing. Almost
every "my strategy doesn't work" turns out to be one of the items below. Read this before you
start debugging, and give it to your agent along with `AGENTS.md`.

Each entry is written as **symptom → cause → fix**, because the symptom is what you have.

---

## 1. The signal never fires, and there is no error

**Symptom.** The strategy runs, bars go by, the signal never validates. No exception, no log
entry, nothing in the dashboard. Indistinguishable from a market that simply never met your
condition.

**Cause.** You are reading indicator data that is only calculated when the matching setting is
on — footprint absorptions, imbalance S/R zones, bar value area, session value area, delta rate
and others. If the indicator was never asked to calculate it, the collection is empty on every
single bar.

**Fix.** In API 2.4.17 configure the indicator in the host, in `State.Configure`, before
`Strategy.Initialize(...)`:

```csharp
var footprint = GetIndicator(FOOTPRINT) as StrategyFootprintIndicator;
if (footprint != null)
{
    footprint.ShowAbsorption = true;          // before reading bar.Absorptions
    // footprint.ShowImbalanceSRZones = true; // before reading imbalance S/R zones
}
```

Enable the setting for what you **read**, on the indicator instance owned by this strategy.

Data that is always calculated needs no extra configuration: `bar.Delta`, `bar.Volume`, the per-level
rows, `bar.POC`, `bar.MinDelta` / `MaxDelta`, `session.POCs`.

> `DeclareRequirements()` / `Require(...)` belong to a later API and must not be generated for this
> 2.4.17-compatible skill. If a signal has never fired, check the host's indicator configuration first.

---

## 2. `EntrySignals.Add(...)` or `AddSignal(...)` does not compile

**Cause.** Those methods do not exist. The tree is built out of nodes, not out of a flat list.

**Fix.**

```csharp
var conj = new LogicalNode(Logic.Conjunction);
pattern.Signals.Root.AddChild(conj);          // AddChild — not AddSignal
conj.AddChild(new Signals.MySignal(Strategy) { Name = "Long", Allowed = SignalDirection.Long });
```

---

## 3. You cannot override stop loss, target or trailing on the strategy

**Symptom.** You look for `GetStopLossValue()` or similar on `Strategy` and it isn't there.

**Cause.** Exits are declarative, or they are extensions — never strategy overrides.

**Fix.** Two models, pick the one that fits:

- Fixed values → fields on the `Entry` object: `StopLossTicks`, `ProfitTargetTicks`,
  `IsBreakEven`, `Trail = new Trail(...)`.
- Custom logic → inherit and override: `class MyEntry : Entry` (`GetStopLossValue()`,
  `GetEntryPrice(double)`), `class MyExit : ExitBase` (`CheckExit(...)`),
  `class MyTrail : TrailBase`. Put your subclass straight into the `Entry[]`.
  See `samples/extensions/`.

Overriding `GetStopLossValue()` in older projects was inheritance from `Entry` — not a
strategy method that has since been removed.

---

## 4. Nothing happens at all — no indicators, no signals

**Cause.** `OnStateChange()` was overridden without calling `base.OnStateChange()`. The engine
wires everything up in the base call; without it the delegates are never invoked.

**Fix.** Call the base first, then do your own work in the relevant `State`.

Same rule for `OnOrderUpdate` and `OnPositionUpdate` on the algo class: **they require a base
call.** Skipping it leaves order and position bookkeeping half-updated, which shows up much later
as behaviour that looks random.

---

## 5. The signal works in one strategy and not in another

**Cause.** The signal reaches for its indicator through the host:

```csharp
((MyStrategy)Strategy.MZpackStrategy).SomeIndicator      // ties the signal to one host class
```

**Fix.** Take the indicator through the constructor and keep it in a field. A signal that owns
its inputs is movable between strategies; one that reaches through the host is not.

---

## 6. `Initialize` throws, or the probe corrupts the pattern

**Cause.** Signal **instances** were passed to the probe instead of factories, or a signal that
already belongs to the tree was passed. Tree signals are stateful; evaluating one twice per bar
breaks the pattern.

**Fix.** Pass factories. `Initialize` rebuilds the probe on every call and needs its own
instances:

```csharp
Strategy.Initialize(CreateEntryPattern(), null, entries, 0, probeEnabled ? new Func<Signal>[]
{
    () => new Signals.MySignal(Strategy, footprint) { Name = "My signal" },
} : null);
```

---

## 7. The probe report has blank rows

**Cause.** `Name` was not set inside the factory. The name is what identifies the signal in the
report — an unnamed signal is measured but unreadable.

**Fix.** Set `Name` in the factory, not after construction.

And list **every** signal worth observing, including the ones switched off for trading. Whether
a signal also trades is worked out from the pattern and recorded as `ProbeEvent.IsInTree` — that
is exactly what makes an observed-only signal measurable.

---

## 8. The signal fires on every tick when you wanted bar close (or the reverse)

**Cause.** The third constructor argument. `SignalCalculate.OnBarClose` evaluates once per closed
bar; `SignalCalculate.OnEachTick` evaluates continuously.

**Fix.** Set it in the constructor — it is not changeable later, and it is easy to copy the wrong
one from a sample that had different intent.

---

## 9. A sample does not compile when you paste it in

**Cause.** Samples are verbatim snapshots from the product sources and still carry `#if STRAT`,
`#if DATA` or `#if APISAMPLE`.

**Fix.** Remove the directive and its `#endif` when you copy the sample into your own project.
The snapshots are deliberately left unedited so they never drift from the originals.

---

## 10. The build cannot find MZpack or NinjaTrader

**Cause.** A `HintPath` was written into the `.csproj`, or the machine's paths differ from the
defaults.

**Fix.** References live in `Directory.Build.props` only. Either edit it once, or set
`NINJATRADER_INSTALL`, `NINJATRADER_USER` and `MZPACK_DLL` and leave the file alone — then the
same project builds on any machine.

---

## 11. `msbuild` is not a recognised command

**Cause.** MSBuild is only on `PATH` inside the Visual Studio developer shell. In an ordinary
PowerShell or cmd window it is not there, even with Visual Studio installed.

**Fix, in order of effort.**

Open **Developer PowerShell for VS** from the Start menu and build from there.

Or use the SDK copy, which works from any shell:

```
dotnet msbuild <Name>.csproj
```

Or locate the real MSBuild yourself — the version to use in scripts and CI:

```powershell
$msb = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
& $msb <Name>.csproj
```

If MSBuild is not installed at all: **Build Tools for Visual Studio** with the *.NET desktop build
tools* workload, or Visual Studio Community.

**Related.** Building the repository root does nothing — there is no project there. Build
`templates\StrategyTemplate\StrategyTemplate.csproj`, or your own project.

---

## 12. The DLL builds but NinjaTrader does not see the strategy

**Cause.** In almost every case: **NinjaTrader was running during the build.** While it runs it
holds the assemblies in `bin\Custom`, so the `DeployToNinjaTrader` step cannot write your DLL over
the old one — and the build still reports success. A clean build and no strategy in the list is
this, nearly every time.

The other cause is that deploy was switched off with `-p:DeployToNinjaTrader=false`, which is
correct for CI and for building on a machine without NT8, and wrong here.

**Fix.** Close NinjaTrader, build, start it again. The build copies the DLL into
`Documents\NinjaTrader 8\bin\Custom` itself — there is nothing to move by hand.

Do not launch NinjaTrader from the build; it interferes with agent and CI runs.

---

## 13. Trading logic ended up in the host class

**Symptom.** The strategy works but cannot be changed without breaking something, and signals
cannot be reused.

**Cause.** The three-part split was not kept: **host** = UI parameters and configuration only;
**algo class** = execution, overriding only genuinely virtual hooks; **signal** = one entry
condition, no UI parameters.

**Fix.** Move the condition into a `Signal`. If a signal needs a number the user sets, pass it in
through the constructor from the host.

---

## 14. One project, one strategy, one DLL

**Cause.** Two strategies in one project, or a namespace that does not match the project name.

**Fix.** Namespace = project name. Host class and assembly = strategy name. Signals in
`<Project>.Signals`, one file per signal, `Signal` suffix.

---

## Something bit you that is not here?

Tell us and it goes into this file. This document is only as good as the reports behind it, and
every entry above started as somebody's lost afternoon.

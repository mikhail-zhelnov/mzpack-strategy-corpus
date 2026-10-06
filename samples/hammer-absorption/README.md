# HammerWithAbsorption — hammer with absorption

Source (canonical): `MZpack.NT8\Algo\Strategies\FootprintAction\HammerWithAbsorption.cs`. A snapshot for learning; edit the original.

Pattern: absorption (trapped sellers/buyers) in the wick of a hammer candle.
- LONG: bullish hammer + buy absorption (trapped sellers) in the wick.
- SHORT: bearish hammer + sell absorption (trapped buyers) in the wick.

What it demonstrates:
- working with `bar.Absorptions[(int)TradeSide.Bid|Ask]` (`.Count`, `.First()/.Last().Key` — the zone price);
- candle geometry: `UpperWick/LowerWick`, `GetLowerWickPercent()/GetUpperWickPercent()`, `IsBullish/IsBearish`;
- an optional POC filter on the absorption's position;
- an injected footprint and `DeclareRequirements()` with `FootprintCapabilities.Absorptions`, so
  calculation is enabled even for a probe-only signal;
- `FootprintAction`-specific geometry settings and bar filters: adapt these when copying to a standalone host.

API 2.4.18 also includes the distinct built-in `MZpack.NT8.Algo.Signals.FootprintAbsoprtionSignal`
(public spelling). It detects per-level absorption rather than this hammer pattern; see
`../../docs/release-2.4.18.md` for its constructor and host settings.

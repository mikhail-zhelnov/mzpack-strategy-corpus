# HammerWithAbsorption — hammer with absorption

Source (canonical): `MZpack.NT8\Algo\Strategies\FootprintAction\HammerWithAbsorption.cs`. A snapshot for learning; edit the original.

Pattern: absorption (trapped sellers/buyers) in the wick of a hammer candle.
- LONG: bullish hammer + buy absorption (trapped sellers) in the wick.
- SHORT: bearish hammer + sell absorption (trapped buyers) in the wick.

What it demonstrates:
- working with `bar.Absorptions[(int)TradeSide.Bid|Ask]` (`.Count`, `.First()/.Last().Key` — the zone price);
- candle geometry: `UpperWick/LowerWick`, `GetLowerWickPercent()/GetUpperWickPercent()`, `IsBullish/IsBearish`;
- an optional POC filter on the absorption's position;
- a reference for the absorption pattern (there is no separate AbsorptionSignal in the product).

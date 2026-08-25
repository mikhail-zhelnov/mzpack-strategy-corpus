using System;
using MZpack.NT8.Algo;
using NinjaTrader.Cbi;

namespace StrategyTemplate
{
    // Algo class: override ONLY the genuinely virtual hooks (see AGENTS.md §5).
    // SL/TP/trailing are NOT overridden here — they are declarative via Entry.
    public class StrategyTemplateAlgo : MZpack.NT8.Algo.Strategy
    {
        public StrategyTemplateAlgo(string name, StrategyTemplate ninjaStrategy)
            : base(name, ninjaStrategy) { }

        // One logical entry per bar (example of a position-open filter).
        public override bool OnPositionOpenFilter(DateTime time)
        {
            return base.OnPositionOpenFilter(time);
        }

        // Additional entry validation filter (true = allow the entry).
        public override bool OnValidateEntryPatternFilter(object e, MarketDataSource source)
        {
            return true;
        }
    }
}

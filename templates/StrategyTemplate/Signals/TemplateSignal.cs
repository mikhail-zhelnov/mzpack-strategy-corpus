using System;
using MZpack;
using MZpack.NT8.Algo;
using NinjaTrader.Data;

namespace StrategyTemplate.Signals
{
    // Entry pattern recognition. One signal = one condition.
    // Allowed is set at creation time in CreateEntryPattern().
    public class TemplateSignal : Signal
    {
        public SignalDirection Allowed { get; set; }

        public TemplateSignal(MZpack.NT8.Algo.Strategy strategy)
            : base(strategy, MarketDataSource.Level1, SignalCalculate.OnBarClose, true) { }

        public override void OnCalculate(MarketDataEventArgs e, int barIdx, SignalDirection allowed)
        {
            var strategy = (StrategyTemplate)Strategy.MZpackStrategy;
            SignalDirection direction = SignalDirection.None;

            if (!strategy.FootprintIndicator.FootprintBars.TryGetValue(barIdx, out IFootprintBar bar))
                return;

            if (Allowed == SignalDirection.Long)
            {
                // TODO: long conditions, e.g.: if (bar.Delta <= 0) return;
                if (true)
                    direction = ResolveDirection(SignalDirection.Long, allowed);
            }
            else if (Allowed == SignalDirection.Short)
            {
                // TODO: short conditions
                if (true)
                    direction = ResolveDirection(SignalDirection.Short, allowed);
            }

            if (IsDetermined(direction))
            {
                Direction   = direction;
                Time        = e.Time;
                EntryPrice  = GetBestEntryPrice(direction);
                ChartRange  = new ChartRange { MinBarIdx = barIdx, MaxBarIdx = barIdx, Low = EntryPrice, High = EntryPrice };
                Description = $" => Delta = {bar.Delta}";
            }
        }
    }
}

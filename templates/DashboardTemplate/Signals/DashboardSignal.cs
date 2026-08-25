using System;
using MZpack;
using MZpack.NT8.Algo;
using NinjaTrader.Data;

namespace DashboardTemplate.Signals
{
    public class DashboardSignal : Signal
    {
        public SignalDirection Allowed { get; set; }

        public DashboardSignal(MZpack.NT8.Algo.Strategy strategy)
            : base(strategy, MarketDataSource.Level1, SignalCalculate.OnBarClose, true) { }

        public override void OnCalculate(MarketDataEventArgs e, int barIdx, SignalDirection allowed)
        {
            var strategy = (DashboardTemplate)Strategy.MZpackStrategy;
            SignalDirection direction = SignalDirection.None;

            if (!strategy.FootprintIndicator.FootprintBars.TryGetValue(barIdx, out IFootprintBar bar))
                return;

            if (Allowed == SignalDirection.Long)
            {
                if (bar.Delta > 0) direction = ResolveDirection(SignalDirection.Long, allowed);
            }
            else if (Allowed == SignalDirection.Short)
            {
                if (bar.Delta < 0) direction = ResolveDirection(SignalDirection.Short, allowed);
            }

            if (IsDetermined(direction))
            {
                Direction = direction; Time = e.Time; EntryPrice = GetBestEntryPrice(direction);
                ChartRange = new ChartRange { MinBarIdx = barIdx, MaxBarIdx = barIdx, Low = EntryPrice, High = EntryPrice };
                Description = $" => Delta = {bar.Delta}";   // shows up in the log / dashboard legend
            }
        }
    }
}

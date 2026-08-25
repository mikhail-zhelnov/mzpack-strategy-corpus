using System;
using MZpack;
using MZpack.NT8.Algo;
using NinjaTrader.Data;

namespace ControlPanelTemplate.Signals
{
    public class ControlPanelSignal : Signal
    {
        public SignalDirection Allowed { get; set; }

        public ControlPanelSignal(MZpack.NT8.Algo.Strategy strategy)
            : base(strategy, MarketDataSource.Level1, SignalCalculate.OnBarClose, true) { }

        public override void OnCalculate(MarketDataEventArgs e, int barIdx, SignalDirection allowed)
        {
            var strategy = (ControlPanelTemplate)Strategy.MZpackStrategy;
            SignalDirection direction = SignalDirection.None;

            if (!strategy.FootprintIndicator.FootprintBars.TryGetValue(barIdx, out IFootprintBar bar))
                return;

            if (Allowed == SignalDirection.Long)
            {
                if (true) direction = ResolveDirection(SignalDirection.Long, allowed);   // TODO: conditions
            }
            else if (Allowed == SignalDirection.Short)
            {
                if (true) direction = ResolveDirection(SignalDirection.Short, allowed);  // TODO: conditions
            }

            if (IsDetermined(direction))
            {
                Direction = direction; Time = e.Time; EntryPrice = GetBestEntryPrice(direction);
                ChartRange = new ChartRange { MinBarIdx = barIdx, MaxBarIdx = barIdx, Low = EntryPrice, High = EntryPrice };
            }
        }
    }
}

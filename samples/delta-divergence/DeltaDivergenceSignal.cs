#if STRAT
using System;
using NinjaTrader.Data;
using MZpack.NT8.Algo.Indicators;
using MZpack;
using MZpack.NT8.Algo;
using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.Strategies.MZpackStrategies.FootprintActionStrategy.Signals
{
    /// <summary>
    /// The Delta Divergence is a trend reversal signal.
    /// LONG:
    ///- Price makes a new low with a bullish candle and positive delta
    /// SHORT:
    ///- Price makes a new high with a bearish candle and negative delta
    /// </summary>
    public class DeltaDivergenceSignal : Signal
    {
        public DeltaDivergenceSignal(MZpack.NT8.Algo.Strategy strategy) : base(strategy, MarketDataSource.Level1, SignalCalculate.OnBarClose, true)
        {
        }

        public override void OnCalculate(MarketDataEventArgs e, int barIdx, SignalDirection allowed)
        {
            FootprintAction strategy = (FootprintAction)Strategy.MZpackStrategy;
            SignalDirection direction = SignalDirection.None;
            IFootprintBar bar;

            if (barIdx < 1) 
                return;

            ICandle candle = Strategy.MZpackStrategy.GetCandle(1);  // Closed candle
            ICandle candle_1 = Strategy.MZpackStrategy.GetCandle(2);  // Before closed candle

            if (candle_1 == null || candle == null)
                return;

            if (!strategy.FootprintIndicator.FootprintBars.TryGetValue(barIdx, out bar))
                return;

            if (!strategy.CheckBarFilters(bar, strategy.Strategy_DeltaDivergence_OverrideFilters, strategy.Strategy_DeltaDivergence_MinBarVolume, strategy.Strategy_DeltaDivergence_MinBarDelta, strategy.Strategy_DeltaDivergence_MinBarDeltaPercent))
                return;

            // LONG
            if (candle.IsBullish() && bar.Delta > 0 && candle.Low < candle_1.Low)
            {
                direction = Signal.ResolveDirection(SignalDirection.Long, allowed);
            }
            else
            // SHORT
            if (candle.IsBearish() && bar.Delta < 0 && candle.High > candle_1.High)
            {
                direction = Signal.ResolveDirection(SignalDirection.Short, allowed);
            }

            if (Signal.IsDetermined(direction))
            {
                Direction = direction;
                Time = e.Time;
                EntryPrice = GetBestEntryPrice(direction);
                ChartRange = new ChartRange()
                {
                    MinBarIdx = barIdx,
                    MaxBarIdx = barIdx,
                    Low = EntryPrice,
                    High = EntryPrice
                };

                Description = $" => Delta = {bar.Delta}";
            }
        }

    }
}
#endif
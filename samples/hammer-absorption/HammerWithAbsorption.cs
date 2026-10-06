#if STRAT
using System;
using NinjaTrader.Data;
using MZpack.NT8.Algo.Indicators;
using MZpack;
using MZpack.NT8.Algo;
using System.Collections.Generic;
using System.Linq;

namespace NinjaTrader.NinjaScript.Strategies.MZpackStrategies.FootprintActionStrategy.Signals
{
    /// <summary>
    /// The Hammer with Absorption signal will open a trade if thereis an absoprtion (trapped sellers or buyers) in a hammer candle.
    /// POC filter can be applied - absorption must be at POC or below/above POC level
    /// LONG:
    /// - A bullish hammer candle with buy absorption (trapped sellers) in the wick
    /// SHORT:
    /// - A bearish hammer candle with sell absorption (trapped buyers) in the wick
    /// </summary>
    public class HammerWithAbsorption : Signal
    {
        readonly StrategyFootprintIndicator footprint;

        public HammerWithAbsorption(MZpack.NT8.Algo.Strategy strategy, StrategyFootprintIndicator footprint) : base(strategy, MarketDataSource.Level1, SignalCalculate.OnBarClose, true)
        {
            this.footprint = footprint;
        }

        // bar.Absorptions is empty unless the indicator was asked to calculate absorptions: AbsorptionParams is
        // filled only under ShowAbsorption, and the calculation is gated on that list being non-empty. Nothing
        // used to set it, so this signal could not fire even when it was enabled for trading.
        public override void DeclareRequirements()
        {
            Require(footprint, FootprintCapabilities.Absorptions);
        }

        public override void OnCalculate(MarketDataEventArgs e, int barIdx, SignalDirection allowed)
        {
            FootprintAction strategy = (FootprintAction)Strategy.MZpackStrategy;
            SignalDirection direction = SignalDirection.None;

            if (!footprint.FootprintBars.TryGetValue(barIdx, out IFootprintBar bar))
                return;

            if (!strategy.CheckBarFilters(bar, strategy.Strategy_HammerWithAbsorption_FilterMode, strategy.Strategy_HammerWithAbsorption_Percentile,strategy.Strategy_HammerWithAbsorption_MinBarVolume, strategy.Strategy_HammerWithAbsorption_MinBarDelta, strategy.Strategy_HammerWithAbsorption_MinBarDeltaPercent))
                return;

            ICandle candle = strategy.GetCandle(GetCurrentBarAgo(0));
            if (candle == null)
                return;

            // LONG
            if (candle.IsBullish() && candle.UpperWick == 0 && candle.GetLowerWickPercent() >= strategy.Strategy_HammerAbsorption_WickPercent)  // Bullish hammer candle
            {
                if (bar.Absorptions[(int)TradeSide.Bid].Count > 0)  // Bar has some sell absorptions
                {
                    if (strategy.Strategy_HammerAbsorption_POC && bar.Absorptions[(int)TradeSide.Bid].First().Key > bar.POC)  // Check price of lowest absorption
                        return;

                    direction = Signal.ResolveDirection(SignalDirection.Long, allowed);
                }
            }

            // SHORT
            if (direction == SignalDirection.None)
            {
                if (candle.IsBearish() && candle.LowerWick == 0 && candle.GetUpperWickPercent() >= strategy.Strategy_HammerAbsorption_WickPercent)  // Bearish hammer candle
                {
                    if (bar.Absorptions[(int)TradeSide.Ask].Count > 0)  // Bar has some buy absorptions
                    {
                        if (strategy.Strategy_HammerAbsorption_POC && bar.Absorptions[(int)TradeSide.Ask].Last().Key < bar.POC)  // Check price of highest absorption
                            return;

                        direction = Signal.ResolveDirection(SignalDirection.Short, allowed);
                    }
                }
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

                //Description = $" => ";
            }
        }
    }
}
#endif

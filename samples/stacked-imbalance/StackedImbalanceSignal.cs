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
    /// Stacked Imbalances occur when we have given number (or more) imbalances stacked on top of each other. 
    /// LONG:
    /// - Given number or more buy imbalances 
    /// SHORT:
    /// - Given number or more sell imbalances 
    /// </summary>
    public class StackedImbalanceSignal : Signal
    {
        readonly StrategyFootprintIndicator footprint;

        public StackedImbalanceSignal(MZpack.NT8.Algo.Strategy strategy, StrategyFootprintIndicator footprint) : base(strategy, MarketDataSource.Level1, SignalCalculate.OnBarClose, true)
        {
            this.footprint = footprint;
        }

        // bar.ImbalanceSRZones is empty unless the indicator was asked to build imbalance S/R zones - the
        // signal reads nothing else that is gated. Declaring it here is what lets the signal be measured while
        // it is switched off for trading; the strategy used to tie the indicator setting to that same switch.
        public override void DeclareRequirements()
        {
            Require(footprint, FootprintCapabilities.ImbalanceSRZones);
        }

        public override void OnCalculate(MarketDataEventArgs e, int barIdx, SignalDirection allowed)
        {
            FootprintAction strategy = (FootprintAction)Strategy.MZpackStrategy;
            SignalDirection direction = SignalDirection.None;
            IFootprintBar bar;

            if (footprint.FootprintBars.TryGetValue(barIdx, out bar))
            {
                if (!strategy.CheckBarFilters(bar, strategy.Strategy_StackedImbalances_FilterMode, strategy.Strategy_StackedImbalances_Percentile,strategy.Strategy_StackedImbalances_MinBarVolume, strategy.Strategy_StackedImbalances_MinBarDelta, strategy.Strategy_StackedImbalances_MinBarDeltaPercent))
                    return;

                if (bar.ImbalanceSRZones.Zones[(int)TradeSide.Bid].Count > 0 && bar.ImbalanceSRZones.Zones[(int)TradeSide.Ask].Count > 0)  // Undefined direction
                    return;

                // Short
                if (bar.ImbalanceSRZones.Zones[(int)TradeSide.Bid].Count > 0)  // Bar has some sell imbalance zones. The number of stacked imbalances in a zone has set in indicator settings
                {
                    // POC above highest zone filter
                    if (strategy.Strategy_StackedImbalances_AboveBelowPOC)
                    {
                        var highestZone = bar.ImbalanceSRZones.Zones[(int)TradeSide.Bid].OrderBy(x => x.Hi).Last();
                        if (bar.POC <= highestZone.Hi)
                            return;
                    }

                    direction = ResolveDirection(strategy.Strategy_StackedImbalances_Reverse ? SignalDirection.Long : SignalDirection.Short, allowed);
                }

                // Long
                if (bar.ImbalanceSRZones.Zones[(int)TradeSide.Ask].Count > 0)
                {
                    // POC below lowest zone filter
                    if (strategy.Strategy_StackedImbalances_AboveBelowPOC)
                    {
                        var highestZone = bar.ImbalanceSRZones.Zones[(int)TradeSide.Ask].OrderBy(x => x.Lo).First();
                        if (bar.POC >= highestZone.Lo)
                            return;
                    }

                    direction = ResolveDirection(strategy.Strategy_StackedImbalances_Reverse ? SignalDirection.Short : SignalDirection.Long, allowed);
                }
            }

            if (IsDetermined(direction))
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

                //Description = $" => Delta = {bar.Delta}";
            }
        }
    }
}
#endif

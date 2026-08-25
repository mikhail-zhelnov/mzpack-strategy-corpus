#if STRAT
using NinjaTrader.Data;
using MZpack;
using MZpack.NT8.Algo;
using System.Collections.Generic;
using System.Linq;

namespace NinjaTrader.NinjaScript.Strategies.MZpackStrategies.GhostResistanceStrategy.Signals
{
    /// <summary>
    /// On bar close.
    /// LONG:
    /// - One or more sell big trades at bar lower wick (trade POC)
    /// SHORT:
    /// - One or more buy big trades at bar upper wick (trade POC)
    /// </summary>
    public class BigTradeSignal : Signal
    {
        public BigTradeSignal(MZpack.NT8.Algo.Strategy strategy) : base(strategy, MarketDataSource.Level1, SignalCalculate.OnBarClose, true)
        {
        }

        public override void OnCalculate(MarketDataEventArgs e, int barIdx, SignalDirection allowed)
        {
            GhostResistance strategy = (GhostResistance)Strategy.MZpackStrategy;
            SignalDirection direction = SignalDirection.None;

            ICandle candle = strategy.GetCandle(GetCurrentBarAgo(0));
            var trades = strategy.BigTradeIndicator.Trades.ToList();

            trades.Reverse();
            List<ITrade> barTrades = new List<ITrade>();
            foreach (var trade in trades)  // Backward
            {
                if (trade.StartBarIdx == barIdx)
                    barTrades.Add(trade);
                else
                if (trade.StartBarIdx < barIdx)
                    break;
            }

            var sellTrades = barTrades.Where(x => x.Side == TradeSide.Bid && x.POC < candle.LowerBody);
            var buyTrades = barTrades.Where(x => x.Side == TradeSide.Ask && x.POC > candle.UpperBody);

            if (sellTrades.Count() > 0 && buyTrades.Count() > 0)
                direction = SignalDirection.Any;
            else
            if (buyTrades.Count() > 0)
                direction = Signal.ResolveDirection(SignalDirection.Short, allowed);
            else
            if (sellTrades.Count() > 0)
                direction = Signal.ResolveDirection(SignalDirection.Long, allowed);

            if (Signal.IsDetermined(direction))
            {
                var trds = direction == SignalDirection.Long ? sellTrades : buyTrades;

                Direction = direction;
                Time = e.Time;
                if (HasPrice)
                {
                    if (strategy.Position_EnterByLimitOrder)
                        EntryPrice = direction == SignalDirection.Long ? trds.Min(x => x.POC) : trds.Max(x => x.POC);  // Enter at best big trade POC price
                    else
                        EntryPrice = GetBestEntryPrice(direction);
                }
                ChartRange = new ChartRange()
                {
                    MinBarIdx = barIdx,
                    MaxBarIdx = barIdx,
                    Low = EntryPrice,
                    High = EntryPrice
                };

                Description = $" => trades: {string.Join(";", trds)}";
            }
        }
    }
}
#endif
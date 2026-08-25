#if DATA
using System;
using MZpack.NT8.Algo.Extensions.Entries;
using NinjaTrader.Cbi;

namespace MZpack.NT8.Algo.Extensions.Trails
{
    /// <summary>
    /// Class to trail an entry. The base for trailing price is bar High for Short entry, and bar Low for Long entry.
    /// </summary>
    public class BarHiLoTrail : TrailBase
    {
        /// <summary>
        /// Set 0 for High/Low of the current bar; set 1 for High/Low of previous bar, etc.
        /// </summary>
        public int BarsAgo { get; set; } = 1;

        /// <summary>
        /// Shift from High/Low of the bar in ticks. ShiftTicks can be negative also.
        /// </summary>
        public int ShiftTicks { get; set; } = 0;

        public BarHiLoTrail()
        {
        }

        public override bool IsBeginTrailing(MarketPosition position, Entry entry)
        {
            return BarsAgo <= entry.Strategy.MZpackStrategy.BarsSinceEntryExecution(entry.SignalName);
        }

        public override bool IsTrailingStep(MarketPosition position, Entry entry)
        {
            if (position == MarketPosition.Long)
                return entry.Strategy.MZpackStrategy.Low[BarsAgo] > TrailingPrice;

            if (position == MarketPosition.Short)
                return entry.Strategy.MZpackStrategy.High[BarsAgo] < TrailingPrice;

            return false;
        }

        public override double GetTrailingPrice(MarketPosition position, Entry entry)
        {
            if (position == MarketPosition.Long)
                return Math.Min(entry.Strategy.MZpackStrategy.GetCurrentBid(), entry.Strategy.MZpackStrategy.Instrument.MasterInstrument.RoundToTickSize(entry.Strategy.MZpackStrategy.Low[BarsAgo] - entry.Strategy.TicksToPrice(ShiftTicks) * (ShiftTicks > 0 ? 1 : -1)));

            if (position == MarketPosition.Short)
                return Math.Max(entry.Strategy.MZpackStrategy.GetCurrentAsk(), entry.Strategy.MZpackStrategy.Instrument.MasterInstrument.RoundToTickSize(entry.Strategy.MZpackStrategy.High[BarsAgo] + entry.Strategy.TicksToPrice(ShiftTicks) * (ShiftTicks > 0 ? 1 : -1)));

            return 0;
        }
    }
}
#endif
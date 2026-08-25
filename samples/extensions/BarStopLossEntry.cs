#if DATA
using System;
using NinjaTrader.Cbi;

namespace MZpack.NT8.Algo.Extensions.Entries
{
    /// <summary>
    /// The entry with stop loss order relative to high/low of the choosen previous bar (stop loss bar).
    /// The profit target caluclation mode by default is Tick.
    /// </summary>
    public class BarStopLossEntry : Entry
    {
        /// <summary>
        /// Bars ago for stop loss bar.
        /// </summary>
        public int StopLossBarsAgo { get; set; } = 1;
        /// <summary>
        /// Shift from High/Low of the bar in ticks. Default is 1. ShiftTicks can be negative also.
        /// </summary>
        public int StopLossShiftTicks { get; set; } = 1;

        public BarStopLossEntry()
        { 
        }

        public BarStopLossEntry(Strategy strategy) : base(strategy)
        {
            StopLossCalculationMode = NinjaTrader.NinjaScript.CalculationMode.Price;
        }

        public override double GetStopLossValue()
        {
            if (Strategy.MZpackStrategy.Position != null)
            {
                double spread = Strategy.MZpackStrategy.GetCurrentAsk() - Strategy.MZpackStrategy.GetCurrentBid();
                if (Position.Direction == SignalDirection.Long)
                    return Math.Min(Strategy.MZpackStrategy.Instrument.MasterInstrument.RoundToTickSize(Strategy.MZpackStrategy.GetCurrentBid() - spread), Strategy.MZpackStrategy.Instrument.MasterInstrument.RoundToTickSize(Strategy.MZpackStrategy.Low[StopLossBarsAgo] - Strategy.TicksToPrice(StopLossShiftTicks) * (StopLossShiftTicks > 0 ? 1 : -1)));

                if (Position.Direction == SignalDirection.Short)
                    return Math.Max(Strategy.MZpackStrategy.Instrument.MasterInstrument.RoundToTickSize(Strategy.MZpackStrategy.GetCurrentAsk()) + spread, Strategy.MZpackStrategy.Instrument.MasterInstrument.RoundToTickSize(Strategy.MZpackStrategy.High[StopLossBarsAgo] + Strategy.TicksToPrice(StopLossShiftTicks) * (StopLossShiftTicks > 0 ? 1 : -1)));
            }

            return 0;
        }


    }
}
#endif
#if DATA
using System;
using NinjaTrader.Cbi;

namespace MZpack.NT8.Algo.Extensions.Entries
{
    /// <summary>
    /// The entry with stop loss order when the price is calculated inside signal.
    /// The profit target caluclation mode by default is Tick.
    /// </summary>
    public class SignalStopLossEntry : Entry
    {
        /// <summary>
        /// Shift from calculated stop loss price in ticks. Default is 1. ShiftTicks can be negative also.
        /// </summary>
        public int StopLossShiftTicks { get; set; } = 1;

        public SignalStopLossEntry()
        {
        }

        public SignalStopLossEntry(Strategy strategy) : base(strategy)
        {
            StopLossCalculationMode = NinjaTrader.NinjaScript.CalculationMode.Price;
        }

        public override double GetStopLossValue()
        {
            if (Strategy.MZpackStrategy.Position != null)
            {
                MasterInstrument instrument =  Strategy.MZpackStrategy.Instrument.MasterInstrument;
                double spread = Strategy.MZpackStrategy.GetCurrentAsk() - Strategy.MZpackStrategy.GetCurrentBid();
                if (Position.Direction == SignalDirection.Long)
                    return Math.Min(instrument.RoundToTickSize(Strategy.MZpackStrategy.GetCurrentBid() - spread), instrument.RoundToTickSize(StopLossPrice - Strategy.TicksToPrice(StopLossShiftTicks) * (StopLossShiftTicks > 0 ? 1 : -1)));

                if (Position.Direction == SignalDirection.Short)
                    return Math.Max(instrument.RoundToTickSize(Strategy.MZpackStrategy.GetCurrentAsk()) + spread, instrument.RoundToTickSize(StopLossPrice + Strategy.TicksToPrice(StopLossShiftTicks) * (StopLossShiftTicks > 0 ? 1 : -1)));
            }

            return 0;
        }


    }
}
#endif
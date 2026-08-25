#if DATA
using System;
using NinjaTrader.Cbi;

namespace MZpack.NT8.Algo.Extensions.Entries
{
    /// <summary>
    /// Entry based on Fibonacci retracements
    /// </summary>
    public class FiboRetracementEntry : Entry
    {
        /// <summary>
        /// Fibonacci retracement %
        /// </summary>
        public double Fibo { get; set; } = 50.0;
        /// <summary>
        /// Swing for retracement, highest price
        /// </summary>
        public double SwingHigh { get; set; }
        /// <summary>
        /// Swing for retracement, lowest price
        /// </summary>
        public double SwingLow { get; set; }

        public FiboRetracementEntry()
        {
        }

        public FiboRetracementEntry(Strategy strategy) : base(strategy)
        {
            EntryMethod = EntryMethod.Limit;
        }

        public override double GetEntryPrice(double entry)
        {
            double retracemnet = (SwingHigh - SwingLow) * Fibo / 100;

            if (Strategy.Pattern.Direction == SignalDirection.Long)
            {
                double e = Strategy.MZpackStrategy.Instrument.MasterInstrument.RoundToTickSize(SwingHigh - retracemnet);
                if (e < Strategy.MZpackStrategy.GetCurrentAsk())
                    return e;
                else
                {
                    Strategy.Log(LogLevel.ENTRY, DateTime.MinValue, $"The price for BUY limit entry must be below current Ask @{Strategy.MZpackStrategy.GetCurrentAsk()}: '{SignalName}' {Position.Direction} {EntryMethod} @{e} {Quantity}-Lot");
                    return 0;
                }
            }
            else if (Strategy.Pattern.Direction == SignalDirection.Short)
            {
                double e = Strategy.MZpackStrategy.Instrument.MasterInstrument.RoundToTickSize(SwingLow + retracemnet);
                if (e > Strategy.MZpackStrategy.GetCurrentBid())
                    return e;
                else
                {
                    Strategy.Log(LogLevel.ENTRY, DateTime.MinValue, $"The price for SELL limit entry must be above current Bid @{Strategy.MZpackStrategy.GetCurrentBid()}: '{SignalName}' {Position.Direction} {EntryMethod} @{e} {Quantity}-Lot");
                    return 0;
                }
            }

            return 0;
        }
    }
}
#endif
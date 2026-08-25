// MZpack API sample
//
// www.mzpack.pro
// WARNING: to compile this sample in NinjaTrader 8 remove '#if APISAMPLE' and '#endif' directives
#if APISAMPLE
using System;
using System.ComponentModel.DataAnnotations;
using MZpack;
using MZpack.NT8;
using MZpack.NT8.Algo;
using MZpack.NT8.Algo.Extensions.Entries;
using MZpack.NT8.Algo.Indicators;
using MZpack.NT8.Algo.Signals;
using NinjaTrader.Data;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Strategies.MZpackAPISamples
{
    /// <summary>
    /// Demonstrates TradingTimes feature.
    /// Opens LONG for Up-bar, opens SHORT for Down-bar.
    /// The strategy is OnBarClose.
    /// </summary>
    [CategoryOrder("Common", 1)]
    [CategoryOrder("Position", 2)]
    public class TradingTimes : MZpackStrategyBase
    {
        // Entry name
        static readonly string ENTRY_NAME = @"TradingTimes";


        public TradingTimes() : base()
        {
            // Create MZpack algo strategy object to use built-in pattern and position management
            OnCreateAlgoStrategy = new OnCreateAlgoStrategyDelegate(CreateAlgoStrategy);
        }

        // Create MZpack Algo Strategy object to support pattern and trade management (ATM).
        protected MZpack.NT8.Algo.Strategy CreateAlgoStrategy()
        {
            MZpack.NT8.Algo.Strategy strategy = new MZpack.NT8.Algo.Strategy(@"Trading Times", this) 
            {
                OppositePatternAction = OppositePatternAction.None,
                LogLevel = LogLevel, // Set log level of algo from strategy UI
                LogTarget = LogTarget, // Set log target of algo from strategy UI
                LogTime = LogTime
            };

            // Trading times
            if (Time1_Enable)
                strategy.TradingTimes.Add(new TradingTime() { Begin = TryParseDateTime(Time1_Begin), End = TryParseDateTime(Time1_End) });  // Add TradingTime object to TradingTimes list for enabled item
            if (Time2_Enable)
                strategy.TradingTimes.Add(new TradingTime() { Begin = TryParseDateTime(Time2_Begin), End = TryParseDateTime(Time2_End) });  // Add TradingTime object to TradingTimes list for enabled item

            strategy.SessionBreak = !(Time1_Enable || Time2_Enable); // No session break if a time schedule enabled

            return strategy;
        }

        DateTime TryParseDateTime(string s)
        {
            if (!DateTime.TryParse(s, out DateTime time))
                NTMessageBox.Show("Error in date/time format: '" + s + "'", Name, System.Windows.MessageBoxImage.None);
            return time;
        }

        protected override void OnStateChange()
        {
            // Base OnStateChange() call is required
            base.OnStateChange();

            lock (Sync) // Sync state handler
            {
                if (State == State.SetDefaults)
                {
                    BarsRequiredToTrade = 1;

                    // Default values for trading times
                    Time1_Enable = true;
                    Time1_Begin = "08:30:00";
                    Time1_End = "15:15:00";
                    Time2_Begin = "15:30:00";
                    Time2_End = "17:00:00";

                    // Position allowed directions, quantity and stop/target
                    Position_Direction = SignalDirection.Any;
                    Position_Quantity = 2;
                    Position_StopLoss = 6;
                    Position_ProfitTarget = 12;
                }
                else
                if (State == State.Configure)
                {
                    EntriesPerDirection = 1;

                    // Create entry
                    Entry[] entries = new Entry[EntriesPerDirection];
                    entries[0] = new Entry(Strategy)
                    {
                        EntryMethod = EntryMethod.Market,
                        Quantity = Position_Quantity,
                        SignalName = ENTRY_NAME,
                        StopLossTicks = Position_StopLoss,
                        ProfitTargetTicks = Position_ProfitTarget,
                    };

                    // Create pattern
                    Pattern pattern = new Pattern(Strategy,
                        Logic.And, // Logic of root node of signals tree
                        null, // No range for signals
                        true);  // Short-cuircit for AND evaluation

                    // Create signals logical tree
                    pattern.Signals.Root.AddChild(new UpDownBarSignal(Strategy));

                    // Initialize strategy
                    Strategy.Initialize(pattern, entries, 3);
                }
            }
        }

        // UpDown bar signal. Set LONG direction for Up-bar, set SHORT direction for Down-bar.
        class UpDownBarSignal : Signal
        {
            public UpDownBarSignal(MZpack.NT8.Algo.Strategy strategy) : base(strategy, MarketDataSource.Level1, SignalCalculate.OnBarClose, true)
            {
            }

            public override void OnCalculate(MarketDataEventArgs e, int barIdx, SignalDirection allowed)
            {
                SignalDirection direction = SignalDirection.None;

                // Down-bar    
                if (Signal.IsLongAllowed(allowed) && Strategy.MZpackStrategy.Open[1] > Strategy.MZpackStrategy.Close[1]) 
                    direction = Signal.ResolveDirection(SignalDirection.Short, allowed);
                else
                // Up-bar
                if (Signal.IsLongAllowed(allowed) && Strategy.MZpackStrategy.Open[1] < Strategy.MZpackStrategy.Close[1])
                    direction = Signal.ResolveDirection(SignalDirection.Long, allowed);

                if (Signal.IsDetermined(direction))
                {
                    Direction = direction;
                    ChartRange = new ChartRange() { MinBarIdx = barIdx, MaxBarIdx = barIdx };
                    Time = e.Time;
                    EntryPrice = e.Price;
                }
            }
        }


        // --------------------------------- Common
        [Display(Name = "Trading time #1: enable", GroupName = "Common", Order = 1, Description = "")]
        public bool Time1_Enable { get; set; }

        [Display(Name = "Trading time #1: begin", GroupName = "Common", Order = 2, Description = "")]
        public string Time1_Begin { get; set; }

        [Display(Name = "Trading time #1: end", GroupName = "Common", Order = 3, Description = "")]
        public string Time1_End { get; set; } = @"15:15:00";

        [Display(Name = "Trading time #2: enable", GroupName = "Common", Order = 4, Description = "")]
        public bool Time2_Enable { get; set; }

        [Display(Name = "Trading time #2: begin", GroupName = "Common", Order = 5, Description = "")]
        public string Time2_Begin { get; set; }

        [Display(Name = "Trading time #2: end", GroupName = "Common", Order = 6, Description = "")]
        public string Time2_End { get; set; }

        // ----------------------------- Position
        [Display(Name = "Direction", GroupName = "Position", Order = 0, Description = "")]
        public SignalDirection Position_Direction
        { get; set; }

        [Display(Name = "Quantity", GroupName = "Position", Order = 1, Description = "")]
        [Range(1, int.MaxValue)]
        public int Position_Quantity
        { get; set; }

        [Display(Name = "Stop loss, ticks", GroupName = "Position", Order = 2, Description = "")]
        [Range(1, int.MaxValue)]
        public int Position_StopLoss
        { get; set; }

        [Display(Name = "Profit target, ticks", GroupName = "Position", Order = 3, Description = "")]
        [Range(1, int.MaxValue)]
        public int Position_ProfitTarget
        { get; set; }
    }
}
#endif

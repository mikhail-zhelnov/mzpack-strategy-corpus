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
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Strategies.MZpackAPISamples
{
    /// <summary>
    /// Demonstrates RiskManagement class.
    /// Opens LONG for Up-bar, opens SHORT for Down-bar.
    /// The strategy is OnBarClose.
    /// </summary>
    [CategoryOrder("Common", 1)]
    [CategoryOrder("Position", 2)]
    public class RiskManagement : MZpackStrategyBase
    {
        // Entry name
        static readonly string ENTRY_NAME = @"RiskManagement";


        public RiskManagement() : base()
        {
            // Create MZpack algo strategy object to use built-in pattern and position management
            OnCreateAlgoStrategy = new OnCreateAlgoStrategyDelegate(CreateAlgoStrategy);
        }

        // Create MZpack Algo Strategy object to support pattern and trade management (ATM).
        protected MZpack.NT8.Algo.Strategy CreateAlgoStrategy()
        {
            MZpack.NT8.Algo.Strategy strategy = new MZpack.NT8.Algo.Strategy(@"Risk Management", this) 
            { 
                OppositePatternAction = OppositePatternAction.None,
                LogLevel = LogLevel, // Set log level of algo from strategy UI
                LogTarget = LogTarget, // Set log target of algo from strategy UI
                LogTime = LogTime
            };

            // Risk
            strategy.RiskManagement = new MZpack.NT8.Algo.RiskManagement(strategy)
            {
                Currency = Currency.UsDollar,  // Currency of the account
                EntryName = ENTRY_NAME,  // Name of the entry to monitor trades number.
                DailyLossLimitEnable = Common_DailyLossLimit_Enable,
                DailyLossLimit = Common_DailyLossLimit,
                DailyProfitLimitEnable = Common_DailyProfitLimit_Enable,
                DailyProfitLimit = Common_DailyProfitLimit,
                DailyTradesLimitEnable = Common_DailyTradesLimit_Enable,
                DailyTradesLimit = Common_DailyTradesLimit
            };

            return strategy;
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

                    // Default values for risk management
                    Common_DailyLossLimit_Enable = true;
                    Common_DailyLossLimit = 300;
                    Common_DailyProfitLimit_Enable = false;
                    Common_DailyProfitLimit = 3000;
                    Common_DailyTradesLimit_Enable = false;
                    Common_DailyTradesLimit = 5;

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
        [Display(Name = "Daily loss limit: enable", GroupName = "Common", Order = 40, Description = "")]
        [NinjaScriptProperty]
        public bool Common_DailyLossLimit_Enable { get; set; }

        [Display(Name = "Daily loss limit", GroupName = "Common", Order = 41, Description = "")]
        [NinjaScriptProperty]
        public int Common_DailyLossLimit { get; set; }

        [Display(Name = "Daily profit limit: enable", GroupName = "Common", Order = 42, Description = "")]
        [NinjaScriptProperty]
        public bool Common_DailyProfitLimit_Enable { get; set; }

        [Display(Name = "Daily profit limit", GroupName = "Common", Order = 43, Description = "")]
        [NinjaScriptProperty]
        public int Common_DailyProfitLimit { get; set; }

        [Display(Name = "Daily trades limit: enable", GroupName = "Common", Order = 44, Description = "")]
        [NinjaScriptProperty]
        public bool Common_DailyTradesLimit_Enable { get; set; }

        [Display(Name = "Daily trades limit", GroupName = "Common", Order = 45, Description = "")]
        [NinjaScriptProperty]
        public int Common_DailyTradesLimit { get; set; }

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

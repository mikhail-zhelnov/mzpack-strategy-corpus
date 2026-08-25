using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using MZpack;
using MZpack.NT8;
using MZpack.NT8.Algo;
using MZpack.NT8.Algo.Indicators;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript;

namespace StrategyTemplate
{
    // NinjaScript host class: UI parameters and configuration. NOT trading logic.
    [CategoryOrder("Strategy", 2)]
    [CategoryOrder("Position", 3)]
    [CategoryOrder("Trading time", 4)]
    [CategoryOrder("Risk management", 5)]
    [CategoryOrder("Footprint", 6)]
    public class StrategyTemplate : MZpackStrategyBase
    {
        [Browsable(false)] [XmlIgnore]
        public StrategyFootprintIndicator FootprintIndicator { get; private set; }

        static readonly string ENTRY1 = @"T1.1";

        // Example UI parameters (add your own with [Display]/[NinjaScriptProperty] attributes).
        int Position_Quantity   = 1;
        int Position_StopLoss   = 12;
        int Position_ProfitTgt  = 20;
        SignalDirection position_Direction = SignalDirection.Any;

        public StrategyTemplate() : base()
        {
            OnCreateAlgoStrategy = new OnCreateAlgoStrategyDelegate(CreateAlgoStrategy);
            OnCreateIndicators   = new OnCreateIndicatorsDelegate(CreateIndicators);
        }

        // 1) Create and configure the algo strategy (risk, trading windows). The pattern is built in Configure.
        protected MZpack.NT8.Algo.Strategy CreateAlgoStrategy()
        {
            var strategy = new StrategyTemplateAlgo(@"StrategyTemplate v1.0", this)
            {
                OppositePatternAction = OppositePatternAction.None,
                LogLevel  = LogLevel,
                LogTarget = LogTarget,
                LogTime   = LogTime
            };

            strategy.RiskManagement = new RiskManagement(strategy)
            {
                Currency  = Currency.UsDollar,
                EntryName = ENTRY1
            };

            return strategy;
        }

        // 2) Return the indicators. Detailed setup is done below in Configure.
        protected List<TickIndicator> CreateIndicators()
        {
            var indicators = new List<TickIndicator>();
            indicators.Add(new StrategyFootprintIndicator(this, @"Footprint"));
            return indicators;
        }

        // 3) In Configure: get the indicators, build Entry[], and initialize the strategy.
        protected override void OnStateChange()
        {
            base.OnStateChange();

            if (State == State.Configure)
            {
                FootprintIndicator = GetIndicator(@"Footprint") as StrategyFootprintIndicator;

                var entries = new Entry[1];
                entries[0] = new Entry(Strategy)
                {
                    EntryMethod       = EntryMethod.Limit,
                    Quantity          = Position_Quantity,
                    SignalName        = ENTRY1,
                    StopLossTicks     = Position_StopLoss,
                    ProfitTargetTicks = Position_ProfitTgt
                };

                Strategy.Initialize(CreateEntryPattern(), null, entries);
            }
        }

        // 4) Entry signals tree.
        Pattern CreateEntryPattern()
        {
            var pattern = new Pattern(Strategy, Logic.And, null, false);
            pattern.AllowedDirection = position_Direction;

            var conj = new LogicalNode(Logic.Conjunction);
            pattern.Signals.Root.AddChild(conj);
            conj.AddChild(new Signals.TemplateSignal(Strategy) { Name = "Long",  Allowed = SignalDirection.Long });
            conj.AddChild(new Signals.TemplateSignal(Strategy) { Name = "Short", Allowed = SignalDirection.Short });

            return pattern;
        }
    }
}

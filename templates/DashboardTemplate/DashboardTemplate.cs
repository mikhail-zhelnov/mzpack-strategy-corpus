using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using MZpack;
using MZpack.NT8;
using MZpack.NT8.Algo;
using MZpack.NT8.Algo.Indicators;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript;

namespace DashboardTemplate
{
    // Strategy template with the MZpack Pattern Dashboard (see FootprintAction.cs).
    // The Dashboard is a built-in grid that visualizes the signals tree and its validation + a legend.
    // Enabled via flags in SetDefaults; custom rendering is NOT needed. For the legend, each signal
    // must have a Name set (see CreateEntryPattern).
    [CategoryOrder("Strategy", 1)]
    [CategoryOrder("Position", 5)]
    [CategoryOrder("Risk management", 7)]
    public class DashboardTemplate : MZpackStrategyBase
    {
        [Browsable(false)] [XmlIgnore]
        public StrategyFootprintIndicator FootprintIndicator { get; private set; }

        static readonly string ENTRY1 = @"Entry 1";

        int Position_Quantity   = 1;
        int Position_StopLoss   = 12;
        int Position_ProfitTgt  = 20;
        SignalDirection Position_Direction = SignalDirection.Any;

        public class DashboardTemplateAlgo : MZpack.NT8.Algo.Strategy
        {
            public DashboardTemplateAlgo(string name, DashboardTemplate host) : base(name, host) { }
        }

        public DashboardTemplate() : base()
        {
            OnCreateAlgoStrategy = new OnCreateAlgoStrategyDelegate(CreateAlgoStrategy);
            OnCreateIndicators   = new OnCreateIndicatorsDelegate(CreateIndicators);
        }

        protected MZpack.NT8.Algo.Strategy CreateAlgoStrategy()
        {
            var strategy = new DashboardTemplateAlgo(@"DashboardTemplate v1.0", this)
            {
                OppositePatternAction = OppositePatternAction.None,
                LogLevel = LogLevel, LogTarget = LogTarget, LogTime = LogTime
            };
            strategy.RiskManagement = new RiskManagement(strategy)
            {
                Currency = Currency.UsDollar, EntryName = ENTRY1
            };
            return strategy;
        }

        protected List<TickIndicator> CreateIndicators()
        {
            var indicators = new List<TickIndicator>();
            indicators.Add(new StrategyFootprintIndicator(this, @"Footprint"));
            return indicators;
        }

        protected override void OnStateChange()
        {
            base.OnStateChange();

            if (State == State.SetDefaults)
            {
                // --- MZpack Pattern Dashboard configuration ---
                ShowPatternsDashboard     = true;                       // show the patterns dashboard
                DashboardGridShowLegend   = true;                       // show the legend (signal names)
                DashboardGridViewPosition = DashboardViewPosition.Top;  // grid position on the chart
                DashboardGridViewOffset   = -30;                        // 30 px from Top; rendering uses absolute distance

                // The panel is used as a host for the dashboard toggles
                ControlPanelShow           = true;
                ControlPanelPropertiesShow = false;
                ControlPanelWidth          = 250;
            }
            else if (State == State.Configure)
            {
                FootprintIndicator = GetIndicator(@"Footprint") as StrategyFootprintIndicator;

                var entries = new Entry[1];
                entries[0] = new Entry(Strategy)
                {
                    EntryMethod = EntryMethod.Limit, Quantity = Position_Quantity,
                    SignalName = ENTRY1, StopLossTicks = Position_StopLoss, ProfitTargetTicks = Position_ProfitTgt
                };
                Strategy.Initialize(CreateEntryPattern(), null, entries);
            }
        }

        Pattern CreateEntryPattern()
        {
            var pattern = new Pattern(Strategy, Logic.And, null, false);
            pattern.AllowedDirection = Position_Direction;

            // IMPORTANT for the dashboard: each signal's Name is shown in the legend.
            var conj = new LogicalNode(Logic.Conjunction);
            pattern.Signals.Root.AddChild(conj);
            conj.AddChild(new Signals.DashboardSignal(Strategy) { Name = "Delta Long",  Allowed = SignalDirection.Long });
            conj.AddChild(new Signals.DashboardSignal(Strategy) { Name = "Delta Short", Allowed = SignalDirection.Short });
            return pattern;
        }

        // Dashboard control properties surfaced on the Control Panel via [ControlPanel].
        #region MZpack
        [Display(Name = "Entry/Exit: markup", GroupName = "MZpack", Order = 30, Description = "")]
        [ControlPanel]
        public override EntryMarkup EntryMarkup { get; set; }

        [Display(Name = "Pattern dashboard: show", GroupName = "MZpack", Order = 40, Description = "")]
        [ControlPanel]
        public override bool ShowPatternsDashboard { get; set; }

        [Display(Name = "Pattern dashboard: legend", GroupName = "MZpack", Order = 50, Description = "")]
        [ControlPanel]
        public override bool DashboardGridShowLegend { get; set; }

        [Display(Name = "Pattern dashboard: position", GroupName = "MZpack", Order = 51, Description = "")]
        [ControlPanel]
        public override DashboardViewPosition DashboardGridViewPosition { get; set; }
        #endregion
    }
}

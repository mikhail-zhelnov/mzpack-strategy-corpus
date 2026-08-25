using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Serialization;
using MZpack;
using MZpack.NT8;
using MZpack.NT8.Algo;
using MZpack.NT8.Algo.Indicators;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript;

namespace ControlPanelTemplate
{
    // Strategy template with a custom MZpack Control Panel (see AdvancedTemplate.cs).
    // Demonstrates: panel buttons, handler attachment, [ControlPanel] properties.
    [CategoryOrder("General", 0)]
    [CategoryOrder("Strategy", 1)]
    [CategoryOrder("Position", 5)]
    [CategoryOrder("Risk management", 7)]
    public class ControlPanelTemplate : MZpackStrategyBase
    {
        [Browsable(false)] [XmlIgnore]
        public StrategyFootprintIndicator FootprintIndicator { get; private set; }

        static readonly string ENTRY1 = @"Entry 1";

        // --- Custom MZpack Control Panel elements ---
        Button tradingOnOffButton, cancelCloseButton, breakEvenButton;

        int Position_Quantity   = 1;
        int Position_StopLoss   = 12;
        int Position_ProfitTgt  = 20;
        SignalDirection Position_Direction = SignalDirection.Any;

        // Nested algo class. Refreshes the panel buttons after a position change.
        public class ControlPanelTemplateAlgo : MZpack.NT8.Algo.Strategy
        {
            public ControlPanelTemplateAlgo(string name, ControlPanelTemplate host) : base(name, host) { }

            public override void OnPositionUpdate(NinjaTrader.Cbi.Position position, double averagePrice,
                                                  int quantity, MarketPosition marketPosition)
            {
                base.OnPositionUpdate(position, averagePrice, quantity, marketPosition);
                ((ControlPanelTemplate)MZpackStrategy).UpdateButtons();
            }
        }

        public ControlPanelTemplate() : base()
        {
            OnCreateAlgoStrategy = new OnCreateAlgoStrategyDelegate(CreateAlgoStrategy);
            OnCreateIndicators   = new OnCreateIndicatorsDelegate(CreateIndicators);
        }

        protected MZpack.NT8.Algo.Strategy CreateAlgoStrategy()
        {
            var strategy = new ControlPanelTemplateAlgo(@"ControlPanelTemplate v1.0", this)
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
                // --- MZpack Control Panel configuration ---
                ControlPanelShow           = true;   // show the panel
                ControlPanelPropertiesShow = false;  // do not show the properties block by default
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

            var conj = new LogicalNode(Logic.Conjunction);
            pattern.Signals.Root.AddChild(conj);
            conj.AddChild(new Signals.ControlPanelSignal(Strategy) { Name = "Long",  Allowed = SignalDirection.Long });
            conj.AddChild(new Signals.ControlPanelSignal(Strategy) { Name = "Short", Allowed = SignalDirection.Short });
            return pattern;
        }

        // ===== MZpack Control Panel: custom buttons =====

        // Create the panel elements (called by the framework).
        public override UIElement[] CreateControlPanelElements()
        {
            var ui = new UIElement[3];
            ui[0] = tradingOnOffButton = new Button { Name = "Trading_OnOff",     Content = getTradingOnOffButtonText() };
            ui[1] = cancelCloseButton  = new Button { Name = "CancelCloseButton", Content = "Close" };
            ui[2] = breakEvenButton    = new Button { Name = "BreakEvenButton",   Content = "Break Even" };
            return ui;
        }

        string getTradingOnOffButtonText() => Strategy.IsOpeningPositionEnabled ? @"Trading: ON" : @"Trading: OFF";

        // Attach event handlers (called by the framework).
        public override void ControlPanel_AttachEventHandlers()
        {
            if (tradingOnOffButton != null) tradingOnOffButton.Click += tradingOnOffButton_Click;
            if (cancelCloseButton  != null) cancelCloseButton.Click  += cancelCloseButton_Click;
            if (breakEvenButton    != null) breakEvenButton.Click    += breakEvenButton_Click;
        }

        // Detach event handlers (called by the framework).
        public override void ControlPanel_DetachEventHandlers()
        {
            if (tradingOnOffButton != null) tradingOnOffButton.Click -= tradingOnOffButton_Click;
            if (cancelCloseButton  != null) cancelCloseButton.Click  -= cancelCloseButton_Click;
            if (breakEvenButton    != null) breakEvenButton.Click    -= breakEvenButton_Click;
        }

        void tradingOnOffButton_Click(object sender, RoutedEventArgs e)
        {
            Strategy.IsOpeningPositionEnabled = !Strategy.IsOpeningPositionEnabled;
            ((Button)sender).Content = getTradingOnOffButtonText();
        }
        void cancelCloseButton_Click(object sender, RoutedEventArgs e)
            => Strategy?.Positions.CancelClose(false, "UI Cancel/Close", DateTime.Now);
        void breakEvenButton_Click(object sender, RoutedEventArgs e)
            => Strategy?.Positions.BreakEven("UI Break Even", DateTime.Now);

        // Refresh the button states from the UI thread.
        public void UpdateButtons()
        {
            if (ChartControl != null)
                ChartControl.Dispatcher.InvokeAsync((System.Action)(() =>
                {
                    if (tradingOnOffButton != null)
                        tradingOnOffButton.Content = getTradingOnOffButtonText();
                }));
        }

        // ===== Properties surfaced on the panel via [ControlPanel] =====
        // The [ControlPanel] attribute on an OVERRIDDEN inherited property surfaces it in the Control Panel.
        #region MZpack
        [Display(Name = "Operating", GroupName = "MZpack", Order = 2, Description = "")]
        [ControlPanel]
        public override StrategyOperating Operating { get; set; }

        [Display(Name = "Entry/Exit: markup", GroupName = "MZpack", Order = 30, Description = "")]
        [ControlPanel]
        public override EntryMarkup EntryMarkup { get; set; }
        #endregion

        // A custom property can also be surfaced on the panel with the [ControlPanel] attribute.
        [ControlPanel, Display(Name = "Opposite Pattern Action", GroupName = "General", Order = 1, Description = "")]
        public OppositePatternAction General_OppositePatternAction { get; set; } = OppositePatternAction.Close;
    }
}

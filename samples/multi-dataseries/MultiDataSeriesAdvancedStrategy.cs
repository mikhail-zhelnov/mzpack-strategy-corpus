// MZpack API sample
//
// www.mzpack.pro
// WARNING: to compile this sample in NinjaTrader 8 remove '#if APISAMPLE' and '#endif' directives
#if APISAMPLE
using System;
using System.Collections.Generic;
using MZpack;
using MZpack.NT8;
using MZpack.NT8.Algo;
using MZpack.NT8.Algo.Indicators;
using MZpack.NT8.Algo.Signals;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NinjaTrader.Gui;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.Strategies.MZpackAPISamples
{
    /// <summary>
    /// Demonstrates trading on second dataseries
    /// </summary>
    [CategoryOrder("Strategy", 0)]
    public class MultiDataSeriesAdvancedStrategy : MZpackStrategyBase
    {
        public MultiDataSeriesAdvancedStrategy() : base()
        {
            // Set OnCreateAlgoStrategy delegate
            OnCreateAlgoStrategy = new OnCreateAlgoStrategyDelegate(CreateAlgoStrategy);
        }

        protected MZpack.NT8.Algo.Strategy CreateAlgoStrategy()
        {
            MZpack.NT8.Algo.Strategy strategy = new MZpack.NT8.Algo.Strategy(@"Multi-DataSeries Advanced", this)
            {
                IsUnmanaged = Strategy_Instrument_Enable ? true : false, // mandatory for multi-dataseries strategy
            };

            return strategy;
        }

        protected override void OnStateChange()
        {
            // Base OnStateChange() call is required
            base.OnStateChange();

            lock (Sync) // Sync OnStateChange handler
            {
                if (State == State.SetDefaults)
                {
                    EntriesPerDirection = 1;
                    BarsRequiredToTrade = 1;
                }
                else if (State == State.Configure)
                {
                    EntriesPerDirection = 1;

                    if (Strategy_Instrument_Enable)
                    {
                        WorkingDataSeriesIdx = -1;  // pass all dataseries to the core

                        AddDataSeries(Strategy_Instrument_Name, Strategy_Instrument_BarsPeriodType, Strategy_Instrument_BarsPeriodValue); // Add trading dataseries
                        TradingDataSeriesIdx = 1;  // set index of trading dataseries
                    }

                    // Create entries
                    Entry[] entries = new Entry[EntriesPerDirection];
                    entries[0] = new Entry(Strategy)
                    {
                        Quantity = 1,
                        SignalName = "MDSA",
                        StopLossCalculationMode = CalculationMode.Ticks,
                        StopLossTicks = 10,
                        ProfitTargetCalculationMode = CalculationMode.Ticks,
                        ProfitTargetTicks = 20,
                    };

                    // Create entry pattern 
                    Pattern entryPattern = new Pattern(Strategy, Logic.And, null, true);
                    // Add signal 
                    entryPattern.Signals.Root.AddChild(new UpDownBarSignal(Strategy) { Name = "Up/Down" });
                    Pattern exitPattern = null;

                    // Init strategy
                    Strategy.Initialize(entryPattern, exitPattern, entries);
                }
            }
        }

        [Display(Name = "Trading Instrument: enable", GroupName = "Strategy", Order = 2, Description = "")]
        public bool Strategy_Instrument_Enable { get; set; } = true;

        [Display(Name = "Trading Instrument: name", GroupName = "Strategy", Order = 3, Description = "")]
        public string Strategy_Instrument_Name { get; set; } = @"MES 12-24";

        [Display(Name = "Trading Instrument: period", GroupName = "Strategy", Order = 4, Description = "")]
        public BarsPeriodType Strategy_Instrument_BarsPeriodType { get; set; } = BarsPeriodType.Minute;

        [Display(Name = "Trading Instrument: value", GroupName = "Strategy", Order = 5, Description = "")]
        [Range(1, int.MaxValue)]
        public int Strategy_Instrument_BarsPeriodValue { get; set; } = 1;

    }
}
#endif
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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NinjaTrader.Gui;
using NinjaTrader.Data;
using System.ComponentModel;
using System.Xml.Serialization;

namespace NinjaTrader.NinjaScript.Strategies.MZpackAPISamples
{
    /// <summary>
    /// Demonstrates how to access data of MZpack mzFootprint indicator (StrategyFootprintIndicator class).
    /// See installation folder for the source code.
    /// </summary>
    [CategoryOrder("Settings", 1)]
    public class CustomPlots : MZpackStrategyBase
    {
        [Browsable(false), XmlIgnore()] public StrategyFootprintIndicator FootprintIndicator { get; set; }
        CustomPlotsIndicator customPlotsIndicator;
        CustomPlotsOnPanelIndicator customPlotsOnPanelIndicator;

        // Indicator for custom plots
        class CustomPlotsIndicator : StrategyPlotIndicator
        {
            int c;

            public CustomPlotsIndicator(MZpackStrategyBase strategy, NinjaTrader.NinjaScript.ISeries<double> input) : 
                base(strategy, input, new MZpack.Series<double>[2])  // Create 2 custom plots via MZpack.Series<double> array
            {
            }

            public override string IndicatorName()
            {
                return "Custom Plots";
            }

            public override void BarUpdate()
            {
                if (__CurrentBar > 0)
                {
                    int currBarIdx = __CurrentBar - 1;
                    Values[0][currBarIdx] = __High[1] + 2 * strategy.TickSize;  // Get High of just closed bar - it's one bar ago from current (just opened) bar: __High[1]

                    c++;
                    if (c <= 4)
                    {
                        Values[1][currBarIdx] = __Low[1] - 2 * strategy.TickSize;
                    }
                    else if (c >= 8)
                    {
                        c = 0;
                    }
                }
            }
        }

        // Indicator for custom plots on its own panel over chart
        class CustomPlotsOnPanelIndicator : StrategyPlotIndicator
        {
            public CustomPlotsOnPanelIndicator(MZpackStrategyBase strategy, NinjaTrader.NinjaScript.ISeries<double> input) :
                base(strategy, input, new MZpack.Series<double>[2])  // Create 2 custom plots via MZpack.Series<double> array
            {
            }

            public override string IndicatorName()
            {
                return "Custom Plots on Panel";
            }

            public override void BarUpdate()
            {
                if (__CurrentBar > 0)
                {
                    int currBarIdx = __CurrentBar - 1;
                    IFootprintBar bar;
                    if ((strategy as CustomPlots).FootprintIndicator.FootprintBars.TryGetValue(currBarIdx, out bar))
                    {
                        Values[0][currBarIdx] = bar.COTHigh;
                        Values[1][currBarIdx] = bar.COTLow;
                    }
                }
            }
        }


        public CustomPlots() : base()
        {
            // Set OnCreateIndicators delegate
            OnCreateIndicators = new OnCreateIndicatorsDelegate(CreateIndicators);
        }

        /// <summary>
        /// Create indicator here and return the list with one item as a result.
        /// </summary>
        /// <returns>Indicators list</returns>
        protected List<TickIndicator> CreateIndicators()
        {
            // Initialize new indicators list
            List<TickIndicator> indicators = new List<TickIndicator>();

            // Create StrategyFootprintIndicator instance 
            FootprintIndicator = new StrategyFootprintIndicator(this, @"Footprint")
            {
                // Override defaults if required
                // See IFootprintIndicator interface
                LeftFootprintStyle = FootprintStyle.Delta,
                RightFootprintStyle = FootprintStyle.Volume,
                
                //
                TicksPerLevel = 1,

                // Show developing levels (POC, VAH, VAL) of session volume profile
                ShowSessionPOC = true,
                ShowSessionVA = true,
                SessionDailyProfileMode = SessionDailyProfileMode.Session,
                SessionVAIsDeveloping = true,
                
                // Bar volume profile levels
                ShowBarVA = true,
                BarVAPercentage = 70,
                ShowBarPOC = true,
                ShowBarPOCCount = 3,  // Show 3 POCs of the bar
                
                // Bar stat
                ShowBarVolume = true,
                ShowBarDelta = true,
                ShowBarAbsoluteDeltaAverage = false,
                ShowBarMinMaxDelta = false,
                ShowBarDeltaPercent = false,
                ShowBarCOT = true,
                ShowBarRatioNumbers = true,
                BarRatioNumbersBoundsLow = 0.4,
                BarRatioNumbersBoundsHigh = 77,

                // Imbalances (diagonal)
                ShowImbalance = true,
                ImbalancePercentage = 200,  // 200%
                ImbalanceFilter = 20, // Imbalance filter in contracts
                ImbalanceMarker = FootprintImbalanceMarker.Always, // Always show imbalance markers
                ImbalanceMarkerPosition = FootprintImbalanceMarkerPosition.Outer,
                ImbalanceHighlightValues = false,  // Don't highlight imbalance values since ImbalanceMarker = FootprintImbalanceMarker.Always

                // Imbalance SR Zones
                ShowImbalanceSRZones = true,
                ImbalanceSRZonesConsecutiveLevels = 2,
                ImbalanceSRZonesVolumeFilter = 200,  
                ImbalanceSRZoneEnding = SRZoneEnding.ByBarPOC,
                ImbalanceSRZonesBreakOnSession = true,
                ImbalanceSRZoneAlert = true,

                // Absorptions (diagonal)
                ShowAbsorption = true,
                AbsorptionPercentage = 300,  // 300%
                AbsorptionDepth = 2, // Depth is 2 ticks
                AbsorptionFilter = 200,  // Absorption filter in contracts

                // Absorption SR Zones
                ShowAbsorptionSRZones = false,

                // Unfinished Auction
                ShowUnfinishedAuction = true
            };

            // Cluster zones
            FootprintIndicator.FootprintPresentation[FootprintBaseMVC.RIGHT_FOOTPRINT].ClusterZonesShow = true;
            FootprintIndicator.FootprintPresentation[FootprintBaseMVC.RIGHT_FOOTPRINT].ClusterZonesFilterMin = 4000;
            FootprintIndicator.FootprintPresentation[FootprintBaseMVC.RIGHT_FOOTPRINT].ClusterZonesFilterMax = -1;
            FootprintIndicator.FootprintPresentation[FootprintBaseMVC.RIGHT_FOOTPRINT].ClusterZonesBreakOnSession = false;
            FootprintIndicator.FootprintPresentation[FootprintBaseMVC.RIGHT_FOOTPRINT].ClusterZonesStyle = LevelPresentationStyle.Line;

            // Statistic Grid
            FootprintIndicator.StatisticGridShow = false;

            // Add indicator to the list
            indicators.Add(FootprintIndicator);

            // Create CustomPlotsIndicator instance
            customPlotsIndicator = new CustomPlotsIndicator(this, Close);
            // On bar close
            customPlotsIndicator.Calculate = Calculate.OnBarClose;
            // Set plots color and width
            customPlotsIndicator.Strokes[0] = new Stroke(System.Windows.Media.Brushes.Red, 2);
            customPlotsIndicator.Strokes[1] = new Stroke(System.Windows.Media.Brushes.Green, 2);
            customPlotsIndicator.Visible = true;
            // Add indicator to the list
            indicators.Add(customPlotsIndicator);

            customPlotsOnPanelIndicator = new CustomPlotsOnPanelIndicator(this, Close);
            // On bar close
            customPlotsOnPanelIndicator.Calculate = Calculate.OnBarClose;
            customPlotsOnPanelIndicator.IsOnPanel = true;
            customPlotsOnPanelIndicator.PanelHeight = 400;
            customPlotsOnPanelIndicator.IsUpperPanel = false;
            // Set plots color and width
            customPlotsOnPanelIndicator.Strokes[0] = new Stroke(System.Windows.Media.Brushes.DodgerBlue, 2);
            customPlotsOnPanelIndicator.Strokes[1] = new Stroke(System.Windows.Media.Brushes.Yellow, 2);
            customPlotsOnPanelIndicator.Visible = true;
            // Add indicator to the list
            indicators.Add(customPlotsOnPanelIndicator);

            return indicators;
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

                    // To print data from hisotrical bars
                    EnableBacktesting = true;
                }
                else if (State == State.Configure)
                {
                    FootprintIndicator.Calculate = Calculate.OnBarClose;  // Calculate.OnBarClose is required for mzFootprint if the logic is in OnBarCloseHandler
                }
            }
        }
    }
}
#endif
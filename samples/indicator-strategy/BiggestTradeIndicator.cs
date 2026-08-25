#if APISAMPLE
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MZpack;
using MZpack.NT8;
using NinjaTrader.Gui;
using MZpack.NT8.Algo;
using MZpack.NT8.Algo.Indicators;
using System.Xml.Serialization;
using System.ComponentModel;
using NinjaTrader.Gui.Chart;
using SharpDX.Direct2D1;
using System.Linq;

namespace NinjaTrader.NinjaScript.Strategies.MZpackAPISamples
{
    /// <summary>
    /// Indicator marks the biggest trade on the chart with rectangle.
    /// </summary>
    public class BiggestTradeIndicator : MZpackStrategyBase
    {
        [Browsable(false)] [XmlIgnore] public StrategyBigTradeIndicator BigTradeIndicator { get; private set; }

        public BiggestTradeIndicator() : base()
        {
            OnCreateIndicators = new OnCreateIndicatorsDelegate(CreateIndicators);
        }

        protected List<TickIndicator> CreateIndicators()
        {
            // Initialize new indicators list
            List<TickIndicator> indicators = new List<TickIndicator>();

            BigTradeIndicator = new StrategyBigTradeIndicator(this, @"BiggestTrade")
            {
                TradeFilterMin = TradeFilterMin,
                TradeFilterMax = TradeFilterMax,
                ShowVersionInfo = false
            };
            indicators.Add(BigTradeIndicator);

            return indicators;
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            // Render big trades
            base.OnRender(chartControl, chartScale);

            // Find biggest trade on the chart
            BigTradeViewItem biggestTradeView;
            List<ITradeView> chartTradesViews = new List<ITradeView>();
            // Loop by the chart bars - collect trades views
            for (int i = BigTradeIndicator.__ChartBars.FromIndex; i <= BigTradeIndicator.__ChartBars.ToIndex; i++)
            {
                List<ITradeView> tradesOfBar;
                if (((BigTradeBaseMVC)BigTradeIndicator.IndicatorMVC).TradesView.TryGetValue(i, out tradesOfBar))
                {
                    chartTradesViews.AddRange(tradesOfBar);
                }
            }

            // Sort views by trade volume
            chartTradesViews.Sort((a, b) => a.Trade.Volume.CompareTo(b.Trade.Volume));
            biggestTradeView = chartTradesViews.LastOrDefault() as BigTradeViewItem;

            // Render biggest trade marker
            if (biggestTradeView != null)
            {
                // Get bounds of trade marker
                System.Drawing.RectangleF bounds = new System.Drawing.RectangleF(biggestTradeView.ShapeBounds.Location, biggestTradeView.ShapeBounds.Size);
                // Inflate bounds by 5 px
                bounds.Inflate(new System.Drawing.SizeF(5, 5));
                // Draw rectangle around trade marker
                RenderTarget.DrawRectangle(MZpack.NT8.Helper.FromSystemDrawingRetcangleF(bounds), BiggestTradeMarkerStroke.BrushDX, BiggestTradeMarkerStroke.Width, BiggestTradeMarkerStroke.StrokeStyle);
            }
        }

        public override void OnRenderTargetChanged()
        {
            base.OnRenderTargetChanged();

            if (RenderTarget != null)
            {
                BiggestTradeMarkerStroke.RenderTarget = RenderTarget;
            }
        }

        [View(), Display(Name = "Trade: min volume", GroupName = "Settings", Order = 1, Description = "Minimal trade size")]
        [Range(0, double.MaxValue)]
        public double TradeFilterMin { get; set; } = 150;

        [View(), Display(Name = "Trade: max volume", GroupName = "Settings", Order = 2, Description = "Maximal trade size")]
        [Range(-1, double.MaxValue)]
        public double TradeFilterMax { get; set; } = -1;

        [Display(Name = "Biggest trade marker", GroupName = "Settings", Order = 3, Description = "")]
        public Stroke BiggestTradeMarkerStroke { get; set; } = new Stroke(System.Windows.Media.Brushes.LightBlue) { Width = 3 };

    }
}
#endif
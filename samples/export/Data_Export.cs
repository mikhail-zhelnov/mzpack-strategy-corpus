// www.mzpack.pro
// To compile in NinjaTrader 8 remove '#if STRAT' and '#endif' directives, and add reference to MZpack.NT8.Pro.dll
#if STRAT
using System;
using System.Collections.Generic;
using MZpack;
using MZpack.NT8;
using MZpack.NT8.Algo;
using MZpack.NT8.Algo.Indicators;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Gui;
using NinjaTrader.Data;
using MZpack.NT8.Algo.DataExport;

namespace NinjaTrader.NinjaScript.Strategies.MZpackStrategies
{
    /// <summary>
    /// A tool for exporting values of indicators
    /// </summary>
    [CategoryOrder("Export", 0)]
    [CategoryOrder("Footprint", 1)]
    [CategoryOrder("VolumeProfile", 2)]
    [CategoryOrder("BigTrade", 3)]
    [CategoryOrder("MarketDepth", 10)]
    public class Data_Export : MZpackStrategyBase
    {
        StrategyFootprintIndicator footprintIndicator;
        StrategyVolumeProfileIndicator volumeProfileIndicator;
        StrategyBigTradeIndicator bigTradeIndicator;
        StrategyMarketDepthIndicator marketDepthIndicator;
        //IndicatorExport quantativeDepthDataExport; // Quantative Depth export on interval handler
        //Pipeline pipeline;
        static readonly string FOOTPRINT = @"Footprint";
        static readonly string VOLUMEPROFILE = @"VolumeProfile";
        static readonly string BIGTRADE = @"BigTrade";
        static readonly string MARKETDEPTH = @"MarketDepth";


        public Data_Export() : base()
        {
            // Set OnCreateIndicators delegate
            OnCreateIndicators = new OnCreateIndicatorsDelegate(CreateIndicators);
            //OnBarCloseHandler = new OnTickDelegate(StrategyOnBarCloseHandler);
            //OnEachTickHandler = new OnTickDelegate(StrategyOnEachTickHandler);
        }

        /// <summary>
        /// Create indicator here and return the list with one item as a result.
        /// </summary>
        /// <returns>Indicators list</returns>
        protected List<TickIndicator> CreateIndicators()
        {
            // Initialize new indicators list
            List<TickIndicator> indicators = new List<TickIndicator>();

            if (Footprint_Export)
            {
                indicators.Add(new StrategyFootprintIndicator(this, FOOTPRINT)
                {
                    SaveSettings = true,  // Save settings in xml file
                    ShowImbalanceSRZones = true,
                    ImbalanceSRZonesConsecutiveLevels = 1,
                    ShowAbsorptionSRZones = true,
                    AbsorptionSRZonesConsecutiveLevels = 1,
                    ShowVersionInfo = false
                });
            }

            if (VolumeProfile_Export)
            {
                indicators.Add(new StrategyVolumeProfileIndicator(this, VOLUMEPROFILE)
                {
                    SaveSettings = true, // Save settings in xml file
                    ShowVersionInfo = false,
                    ShowProfileType = ProfileType.VP,
                    StackedShowProfileType1 = ProfileType.None,
                    VWAPMode = VWAPMode.Dynamic
                });
            }

            if (BigTrade_Export)
            {
                indicators.Add(new StrategyBigTradeIndicator(this, BIGTRADE)
                {
                    SaveSettings = true,  // Save settings in xml file
                    ShowVersionInfo = false
                });
            }

            if (MarketDepth_Export) // || MarketDepth_ExportQuantativeDepth)
            {
                indicators.Add(new StrategyMarketDepthIndicator(this, MARKETDEPTH)
                {
                    SaveSettings = true,  // Save settings in xml file
                    ShowVersionInfo = false
                });
            }

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
                    Name = "Data_Export v1.1";

                    EntriesPerDirection = 1;
                    BarsRequiredToTrade = 1;

                    // To export historical values
                    EnableBacktesting = true;

                    // Export defaults
                    //Export_End = DateTime.Now;
                    Export_Delimiter = ";";

                    // Default exported values
                    Footprint_Export = true;
                    Footprint_Open = true;
                    Footprint_Close = true;
                    Footprint_Volume = true;
                    Footprint_Delta = true;
                }
                else if (State == State.Configure)
                {
                    if (Footprint_Export)
                    {
                        footprintIndicator = GetIndicator(FOOTPRINT) as StrategyFootprintIndicator;  // Get indicator instance loaded from xml
                        footprintIndicator.Calculate = Calculate.OnBarClose;  // Optimize calculation
                        footprintIndicator.ModelIncrementRefresh = ModelIncrementRefresh.HistoricalRealtime;  // In case VP levels exported historically.
                    }
                    if (VolumeProfile_Export)
                    {
                        volumeProfileIndicator = GetIndicator(VOLUMEPROFILE) as StrategyVolumeProfileIndicator;  // Get indicator instance loaded from xml
                        // Optimize profiles calculation
                        volumeProfileIndicator.ModelIncrementRefresh = Volumeprofile_ExportTemporality == ExportTemporality.Historical ? ModelIncrementRefresh.HistoricalRealtime : ModelIncrementRefresh.Realtime;  // In case VP levels exported historically.
                        volumeProfileIndicator.ModelRefreshGranularity = VolumeProfile_ExportGranularity == ExportGranularity.Bar ? ModelRefreshGranularity.Bar : ModelRefreshGranularity.Tick;
                    }

                    if (BigTrade_Export)
                        bigTradeIndicator = GetIndicator(BIGTRADE) as StrategyBigTradeIndicator;  // Get indicator instance loaded from xml

                    if (MarketDepth_Export) // || MarketDepth_ExportQuantativeDepth)
                        marketDepthIndicator = GetIndicator(MARKETDEPTH) as StrategyMarketDepthIndicator;  // Get indicator instance loaded from xml
                }
                else if (State == State.DataLoaded)
                {
                    char delimiter = !string.IsNullOrEmpty(Export_Delimiter) ? Export_Delimiter[0] : Export.DELIMITER;

                    if (Footprint_Export)
                    {
                        // Create data export object.
                        // By default export format is CSV to file Documents\NinjaTrader 8\mzpack\strategy\<namespace>\<strategy class name>\data\<random alphanumeric>.csv
                        var footprintDataExport = new IndicatorExport(this, footprintIndicator,
                            ExportDataSource.Level1, Footprint_ExportTemporality, ExportGranularity.Bar,
                            new ExportArgs()
                            {
                                IsExportWhileCollecting = Footprint_Realtime,  // Live export while collecting data. When off, export starts when you disable the strategy.
                                IsHeader = Export_Header,  // Set header
                                IsTime = Export_Time,
                                SignedVolume = Export_SignedVolume,  // Volumes have sign: + buy, - sell
                                Delimiter = delimiter,  // Values delimiter
                                IsFile = true,
                                FileName = Footprint_File,  // File name with path (optional)
                                IsBatch = Export_Batch,
                            });
                        footprintDataExport.DataSet.Schema = getFootprintExportSchema(footprintDataExport); // Set export schema

                        Register(footprintDataExport);  // Register export
                    }

                    if (VolumeProfile_Export)
                    {
                        var volumeProfileDataExport = new IndicatorExport(this, volumeProfileIndicator,
                            ExportDataSource.Level1, Volumeprofile_ExportTemporality, VolumeProfile_ExportGranularity,
                            new ExportArgs()
                            {
                                IsExportWhileCollecting = VolumeProfile_Realtime,
                                IsHeader = Export_Header,  // Set header
                                IsTime = Export_Time,
                                SignedVolume = Export_SignedVolume,  // Volumes have sign: + buy, - sell
                                Delimiter = delimiter,  // Values delimiter
                                IsFile = true,
                                FileName = VolumeProfile_File,  // File name with path (optional)
                                IsBatch = Export_Batch,
                            });
                        volumeProfileDataExport.DataSet.Schema = getVolumeProfileExportSchema(volumeProfileDataExport); // Set export schema

                        Register(volumeProfileDataExport);  // Register export
                    }

                    if (BigTrade_Export)
                    {
                        var bigTradeDataExport = new IndicatorExport(this, bigTradeIndicator,
                            ExportDataSource.Level1, BigTrade_ExportTemporality, ExportGranularity.Tick,
                            new ExportArgs()
                            {
                                IsExportWhileCollecting = BigTrade_Realtime,
                                IsHeader = Export_Header,  // Set header
                                IsTime = Export_Time,
                                SignedVolume = Export_SignedVolume,  // Volumes have sign: + buy, - sell
                                Delimiter = delimiter,  // Values delimiter
                                IsFile = true,
                                FileName = BigTrade_File,  // File name with path (optional)
                                IsBatch = Export_Batch,
                            });
                        bigTradeDataExport.DataSet.Schema = getBigTradeExportSchema(bigTradeDataExport); // Set export schema

                        Register(bigTradeDataExport);  // Register export
                    }

                    if (MarketDepth_Export)
                    {
                        var orderbookExport = new IndicatorExport(this, marketDepthIndicator,
                            ExportDataSource.Level2, ExportTemporality.Realtime, ExportGranularity.Update,  // Export orderbook values on each update
                            new ExportArgs()
                            {
                                IsExportWhileCollecting = MarketDepth_Realtime,
                                FlushIntervalMs = 250,  // The orderbook updates thousands of times per second, batch the flushes
                                IsHeader = Export_Header,  // Set header
                                IsTime = Export_Time,
                                SignedVolume = Export_SignedVolume,  // Volumes have sign: + buy, - sell
                                Delimiter = delimiter,  // Values delimiter
                                IsFile = true,
                                FileName = MarketDepth_File,  // File name with path (optional)
                                IsBatch = Export_Batch,
                            });
                        orderbookExport.DataSet.Schema = getMarketDepthExportSchema(orderbookExport); // Set exported values

                        Register(orderbookExport);  // Register export
                    }

                    // 'Export real-time' only appends live rows for an export whose Temporality is Realtime.
                    // With Temporality = Historical the rows are produced during the historical load instead,
                    // so nothing appears while the strategy runs, whatever the checkbox says.
                    warnIfHistorical(Footprint_Export && Footprint_Realtime, Footprint_ExportTemporality, FOOTPRINT);
                    warnIfHistorical(VolumeProfile_Export && VolumeProfile_Realtime, Volumeprofile_ExportTemporality, VOLUMEPROFILE);
                    warnIfHistorical(BigTrade_Export && BigTrade_Realtime, BigTrade_ExportTemporality, BIGTRADE);
                }
                else if (State == State.Transition)
                {
                    //if (MarketDepth_ExportQuantativeDepth)
                    //{
                    //    char delimiter = !string.IsNullOrEmpty(Export_Delimiter) ? Export_Delimiter[0] : Export.DELIMITER;

                    //    quantativeDepthDataExport = new IndicatorExport(this, marketDepthIndicator, new ExportArgs()
                    //    {
                    //        IsExportWhileCollecting = false,
                    //        IsHeader = Export_Header,  // Set header
                    //        IsTime = Export_Time,
                    //        SignedVolume = Export_SignedVolume,  // Volumes have sign: + buy, - sell
                    //        Delimiter = delimiter,  // Values delimiter
                    //        IsFile = true,
                    //        FileName = "qdepth/",  // File name with path (optional)
                    //        IsBatch = Export_Batch,
                    //    });
                    //    quantativeDepthDataExport.DataSet.Schema = getQuantativeDepthExportValues(); // Set exported values

                    //    if (marketDepthIndicator.QuantativeDepth != null)  // Set Quantative Depth handler
                    //    {
                    //        marketDepthIndicator.OnQuantativeDepthIntervalHandler = new MarketDepthBaseMVC.OnQuantativeDepthIntervalDelegate(quantativeDepthDataExport.ExportOnEvent);
                    //    }
                    //}

                    // Export pipeline in per bar mode. 
                    // Save data of all exported indicator in one text line. One line represents one bar of data.
                    //if (Export_Pipeline)
                    //{
                    //    pipeline = new Pipeline(this, new ExportArgs()  // Create export pipeline
                    //    {
                    //        FileName = !string.IsNullOrEmpty(Export_PipelineFileName) ? Export_PipelineFileName : null,  // file name if any
                    //        // Set common parameters for every data exports in pipeline
                    //        Delimiter = delimiter,  // Values delimiter
                    //        IsHeader = Export_Header,  // Set header
                    //    });

                    //    pipeline
                    //       .Add(add: Footprint_Export, Export: footprintDataExport)  // Add footprint export to pipeline
                    //       .Add(add: VolumeProfile_Export, Export: volumeProfileDataExport)   // Add profile export to pipeline
                    //       .Add(add: BigTrade_Export, Export: bigTradeDataExport)   // Add big trades export to pipeline
                    //       .ExportDates(begin: Export_Begin, end: Export_End, message: true); // Export choosen indicators values within dates 
                    //}
                }
            }
        }

        //void exportDates(Export export)
        //{
        //    export.ExportDates(Export_Begin, Export_End, true);  // Export choosen indicators values within dates 
        //}

        //protected void StrategyOnBarCloseHandler(MarketDataEventArgs e, int currentBarIdx)
        //{
        //    if (State == State.Realtime && MarketDepth_ExportOnBarClose)
        //    {
        //        // Export market depth values of last closed bar
        //        marketDepthDataExport.ExportArgs.Shift = -1;
        //        marketDepthDataExport.ExportLast();
        //    }
        //}

        //protected void StrategyOnEachTickHandler(MarketDataEventArgs e, int currentBarIdx)
        //{
        //    if (State == State.Realtime && MarketDepth_OnEachTickExport)
        //    {
        //        // Export real-time DOM values
        //        if (MarketDepth_OnEachTickRealtimeValuesExport)
        //            marketDepthOnEachTickDataExport.ExportArgs.Shift = 1;  // Shift to unexisting bar, so only real-time DOM values exported
        //        marketDepthOnEachTickDataExport.ExportLast();
        //    }
        //}

        void warnIfHistorical(bool isRealtimeExport, ExportTemporality temporality, string indicatorName)
        {
            if (isRealtimeExport && temporality == ExportTemporality.Historical)
                Print($"{indicatorName}: Temporality = Historical, so rows are written during the historical load, not while the strategy runs. Set Temporality = Realtime for 'Export real-time' to append rows live.");
        }

        DataSchema getFootprintExportSchema(IndicatorExport export)
        {
            DataSchema v = new DataSchema(export.DataSet);
            if (Footprint_Open) v.Append(IndValue.Open);
            if (Footprint_Close) v.Append(IndValue.Close);
            if (Footprint_High) v.Append(IndValue.High);
            if (Footprint_Low) v.Append(IndValue.Low);
            if (Footprint_RangeTicks) v.Append(IndValue.RangeTicks);
            if (Footprint_RangeLevels) v.Append(IndValue.RangeLevels);
            if (Footprint_Direction) v.Append(IndValue.Side);
            if (Footprint_DurationMs) v.Append(IndValue.DurationMs);
            if (Footprint_Volumes) v.Append(IndValue.Volumes);
            if (Footprint_Bids) v.Append(IndValue.Bids);
            if (Footprint_Asks) v.Append(IndValue.Asks);
            if (Footprint_Deltas) v.Append(IndValue.Deltas);
            if (Footprint_TradesNumbers) v.Append(IndValue.TradesNumbers);
            if (Footprint_TradesNumber) v.Append(IndValue.TradesNumber);
            if (Footprint_Volume) v.Append(IndValue.Volume);
            if (Footprint_BuyVolume) v.Append(IndValue.BuyVolume);
            if (Footprint_SellVolume) v.Append(IndValue.SellVolume);
            if (Footprint_Delta) v.Append(IndValue.Delta);
            if (Footprint_VAH) v.Append(IndValue.VAH);
            if (Footprint_VAL) v.Append(IndValue.VAL);
            if (Footprint_POC) v.Append(IndValue.POC);
            if (Footprint_POCVolume) v.Append(IndValue.POCVolume);
            if (Footprint_BuyPercentage) v.Append(IndValue.BuyPercentage);
            if (Footprint_SellPercentage) v.Append(IndValue.SellPercentage);
            if (Footprint_DeltaPercentage) v.Append(IndValue.DeltaPercentage);
            if (Footprint_MinDelta) v.Append(IndValue.MinDelta);
            if (Footprint_MaxDelta) v.Append(IndValue.MaxDelta);
            if (Footprint_DeltaChange) v.Append(IndValue.DeltaChange);
            if (Footprint_DeltaCumulative) v.Append(IndValue.DeltaCumulative);
            if (Footprint_DeltaRate) v.Append(IndValue.DeltaRate);
            if (Footprint_DeltaRateHigh) v.Append(IndValue.DeltaRateHigh);
            if (Footprint_DeltaRateLow) v.Append(IndValue.DeltaRateLow);
            if (Footprint_AbsoluteDeltaAverage) v.Append(IndValue.AbsoluteDeltaAverage);
            if (Footprint_AbsoluteDeltaTotal) v.Append(IndValue.AbsoluteDeltaTotal);
            if (Footprint_COTHigh) v.Append(IndValue.COTHigh);
            if (Footprint_COTLow) v.Append(IndValue.COTLow);
            if (Footprint_RatioNumbers) v.Append(IndValue.RatioNumbers);
            if (Footprint_VolumePerSecond) v.Append(IndValue.VolumePerSecond);
            if (Footprint_UnfinishedAuctionHigh) v.Append(IndValue.UnfinishedAuctionHigh);
            if (Footprint_UnfinishedAuctionLow) v.Append(IndValue.UnfinishedAuctionLow);
            if (Footprint_BuyImbalanceCount) v.Append(IndValue.BuyImbalanceCount);
            if (Footprint_SellImbalanceCount) v.Append(IndValue.SellImbalanceCount);
            if (Footprint_BuyAbsorptionCount) v.Append(IndValue.BuyAbsorptionCount);
            if (Footprint_SellAbsorptionCount) v.Append(IndValue.SellAbsorptionCount);
            if (Footprint_BuyStackedImbalanceCount) v.Append(IndValue.BuyStackedImbalanceCount);
            if (Footprint_SellStackedImbalanceCount) v.Append(IndValue.SellStackedImbalanceCount);
            if (Footprint_BuyStackedAbsorptionCount) v.Append(IndValue.BuyStackedAbsorptionCount);
            if (Footprint_SellStackedAbsorptionCount) v.Append(IndValue.SellStackedAbsorptionCount);
            if (Footprint_BuyStackedImbalanceMaxConsec) v.Append(IndValue.BuyStackedImbalanceMaxConsec);
            if (Footprint_SellStackedImbalanceMaxConsec) v.Append(IndValue.SellStackedImbalanceMaxConsec);
            if (Footprint_BuyStackedAbsorptionMaxConsec) v.Append(IndValue.BuyStackedAbsorptionMaxConsec);
            if (Footprint_SellStackedAbsorptionMaxConsec) v.Append(IndValue.SellStackedAbsorptionMaxConsec);

            if (Footprint_DeltaDivergence) v.Append(IndValue.DeltaDivergence);
            if (Footprint_SessionOpen) v.Append(IndValue.SessionOpen);
            if (Footprint_SessionClose) v.Append(IndValue.SessionClose);
            if (Footprint_SessionHigh) v.Append(IndValue.SessionHigh);
            if (Footprint_SessionLow) v.Append(IndValue.SessionLow);
            if (Footprint_SeesionVAH) v.Append(IndValue.SessionVAH);
            if (Footprint_SeesionVAL) v.Append(IndValue.SessionVAL);
            if (Footprint_SessionPOC) v.Append(IndValue.SessionPOC);

            return v;
        }

        DataSchema getVolumeProfileExportSchema(IndicatorExport export)
        {
            DataSchema v = new DataSchema(export.DataSet);
            if (VolumeProfile_Open) v.Append(IndValue.Open);
            if (VolumeProfile_Close) v.Append(IndValue.Close);
            if (VolumeProfile_High) v.Append(IndValue.High);
            if (VolumeProfile_Low) v.Append(IndValue.Low);
            if (VolumeProfile_RangeTicks) v.Append(IndValue.RangeTicks);
            if (VolumeProfile_MID) v.Append(IndValue.MID);
            if (VolumeProfile_DurationMs) v.Append(IndValue.DurationMs);
            if (VolumeProfile_TradesNumber) v.Append(IndValue.TradesNumber);
            if (VolumeProfile_Volume) v.Append(IndValue.Volume);
            if (VolumeProfile_BuyVolume) v.Append(IndValue.BuyVolume);
            if (VolumeProfile_SellVolume) v.Append(IndValue.SellVolume);
            if (VolumeProfile_Delta) v.Append(IndValue.Delta);
            if (VolumeProfile_VAH) v.Append(IndValue.VAH);
            if (VolumeProfile_VAL) v.Append(IndValue.VAL);
            if (VolumeProfile_POC) v.Append(IndValue.POC);
            if (VolumeProfile_POCVolume) v.Append(IndValue.POCVolume);
            if (VolumeProfile_TickPOC) v.Append(IndValue.TickPOC);
            if (VolumeProfile_TickPOCVolume) v.Append(IndValue.TickPOCVolume);
            if (VolumeProfile_VWAP) v.Append(IndValue.VWAP);
            if (VolumeProfile__1StdDeviationPos) v.Append(IndValue._1StdDeviationPos);
            if (VolumeProfile__1StdDeviationNeg) v.Append(IndValue._1StdDeviationNeg);
            if (VolumeProfile__2StdDeviationPos) v.Append(IndValue._2StdDeviationPos);
            if (VolumeProfile__2StdDeviationNeg) v.Append(IndValue._2StdDeviationNeg);
            if (VolumeProfile_DeltaPercentage) v.Append(IndValue.DeltaPercentage);
            if (VolumeProfile_BuyPOCVolume) v.Append(IndValue.BuyPOCVolume);
            if (VolumeProfile_SellPOCVolume) v.Append(IndValue.SellPOCVolume);
            if (VolumeProfile_VAVolume) v.Append(IndValue.VAVolume);
            if (VolumeProfile_TPO_POC) v.Append(IndValue.TPO_POC);
            if (VolumeProfile_TPO_VAH) v.Append(IndValue.TPO_VAH);
            if (VolumeProfile_TPO_VAL) v.Append(IndValue.TPO_VAL);
            if (VolumeProfile_TPOLettersCount) v.Append(IndValue.TPOLettersCount);

            return v;
        }

        DataSchema getBigTradeExportSchema(IndicatorExport export)
        {
            DataSchema v = new DataSchema(export.DataSet);
            if (BigTrade_High) v.Append(IndValue.High);
            if (BigTrade_Low) v.Append(IndValue.Low);
            if (BigTrade_RangeTicks) v.Append(IndValue.RangeTicks);
            if (BigTrade_Direction) v.Append(IndValue.Side);
            if (BigTrade_Volume) v.Append(IndValue.Volume);
            if (BigTrade_IcebergVolume) v.Append(IndValue.IcebergVolume);
            if (BigTrade_POC) v.Append(IndValue.POC);
            if (BigTrade_POCVolume) v.Append(IndValue.POCVolume);
            if (BigTrade_DomSupportVolume) v.Append(IndValue.DomSupportVolume);
            if (BigTrade_DomPressureVolume) v.Append(IndValue.DomPressureVolume);
            if (BigTrade_Smart) v.Append(IndValue.Smart);
            if (BigTrade_TicksNumber) v.Append(IndValue.TicksNumber);
            if (BigTrade_Ticks) v.Append(IndValue.Ticks);

            return v;
        }

        DataSchema getMarketDepthExportSchema(IndicatorExport export)
        {
            DataSchema v = new DataSchema(export.DataSet);
            if (MarketDepth_MarketDepth) v.Append(IndValue.MarketDepth);
            if (MarketDepth_RealMarketDepth) v.Append(IndValue.RealMarketDepth);
            if (MarketDepth_BestBid) v.Append(IndValue.BestBid);
            if (MarketDepth_BestOffer) v.Append(IndValue.BestOffer);
            if (MarketDepth_RealtimeBidsWithPrices) v.Append(IndValue.RealtimeBidsWithPrices);
            if (MarketDepth_RealtimeOffersWithPrices) v.Append(IndValue.RealtimeOffersWithPrices);
            if (MarketDepth_RealtimeBids) v.Append(IndValue.RealtimeBids);
            if (MarketDepth_RealtimeOffers) v.Append(IndValue.RealtimeOffers);
            if (MarketDepth_RealtimeBidVolume) v.Append(IndValue.RealtimeBidsTotal);
            if (MarketDepth_RealtimeOfferVolume) v.Append(IndValue.RealtimeOffersTotal);
            //if (MarketDepth_Open) v.Append(IndValue.Open);
            //if (MarketDepth_Close) v.Append(IndValue.Close);
            //if (MarketDepth_High) v.Append(IndValue.High);
            //if (MarketDepth_Low) v.Append(IndValue.Low);
            //if (MarketDepth_HisotricalDepthHigh) v.Append(IndValue.HistoricalDepthHigh);
            //if (MarketDepth_HistoricalDepthLow) v.Append(IndValue.HistoricalDepthLow);
            //if (MarketDepth_HistoricalBids) v.Append(IndValue.HistoricalBids);
            //if (MarketDepth_HistoricalOffers) v.Append(IndValue.HistoricalOffers);
            //if (MarketDepth_HistoricalBidsVolume) v.Append(IndValue.HistoricalBidsVolume);
            //if (MarketDepth_HistoricalOffersVolume) v.Append(IndValue.HistoricalOffersVolume);

            // Per bar orderbook values
            //if (MarketDepth_LiquidityOpen) v.Append(IndValue.LiquidityOpen);
            //if (MarketDepth_LiquidityClose) v.Append(IndValue.LiquidityClose);
            //if (MarketDepth_LiquidityHigh) v.Append(IndValue.LiquidityHigh);
            //if (MarketDepth_LiquidityLow) v.Append(IndValue.LiquidityLow);
            //if (MarketDepth_LiquidityMigrationOpen) v.Append(IndValue.LiquidityMigrationOpen);
            //if (MarketDepth_LiquidityMigrationClose) v.Append(IndValue.LiquidityMigrationClose);
            //if (MarketDepth_LiquidityMigrationHigh) v.Append(IndValue.LiquidityMigrationHigh);
            //if (MarketDepth_LiquidityMigrationLow) v.Append(IndValue.LiquidityMigrationLow);

            return v;
        }

        //DataSchema getQuantativeDepthExportValues()
        //{
        //    DataSchema v = new DataSchema(quantativeDepthDataExport.DataSet);

        //    if (MarketDepth_HQA) v.Append(IndValue.HQA);
        //    if (MarketDepth_UQ) v.Append(IndValue.UQ);
        //    if (MarketDepth_AC) v.Append(IndValue.AC);

        //    return v;
        //}


        #region Export
        //[Display(Name = "Pipeline", GroupName = "Export", Order = 0, Description = "")]
        //public bool Export_Pipeline { get; set; } = false;

        //[Display(Name = "Pipeline file", GroupName = "Export", Order = 1, Description = "")]
        //public string Export_PipelineFileName { get; set; }

        [Display(Name = "Header", GroupName = "Export", Order = 3, Description = "")]
        public bool Export_Header { get; set; } = true;

        [Display(Name = "Time", GroupName = "Export", Order = 4, Description = "")]
        public bool Export_Time { get; set; } = true;

        [Display(Name = "Batch", GroupName = "Export", Order = 5, Description = "")]
        public bool Export_Batch { get; set; }

        [Display(Name = "Signed volume", GroupName = "Export", Order = 6, Description = "Volumes have sign: + buy, - sell")]
        public bool Export_SignedVolume { get; set; }

        [Display(Name = "Delimiter", GroupName = "Export", Order = 7, Description = "")]
        public string Export_Delimiter { get; set; }

        //[Display(Name = "Begin", GroupName = "Export", Order = 10, Description = "")]
        //public DateTime Export_Begin { get; set; }

        //[Display(Name = "End", GroupName = "Export", Order = 11, Description = "")]
        //public DateTime Export_End { get; set; }

        #endregion Export

        #region Footprint
        [Display(Name = "Export", GroupName = "Footprint", Order = 1, Description = "")]
        public bool Footprint_Export { get; set; }

        [Display(Name = "Temporality", GroupName = "Footprint", Order = 2, Description = "")]
        public ExportTemporality Footprint_ExportTemporality { get; set; } = ExportTemporality.Historical;

        [Display(Name = "File", GroupName = "Footprint", Order = 3, Description = "")]
        public string Footprint_File { get; set; } = "footprint\\";

        [Display(Name = "Export real-time", GroupName = "Footprint", Order = 4, Description = "Append rows to the file as they are collected, while the strategy is running. When off, all rows are written at once when the strategy is disabled.")]
        public bool Footprint_Realtime { get; set; } = false;

        //[Display(Name = "Export DataFrame", GroupName = "Footprint", Order = 2, Description = "")]
        //public bool Footprint_ExportDataframe { get; } = false;

        //[Display(Name = "DataFrame size", GroupName = "Footprint", Order = 3, Description = "Size in bars")]
        //public int Footprint_DataframeSize { get; } = 4;

        [Display(Name = "Open", GroupName = "Footprint", Order = 10, Description = "")]
        public bool Footprint_Open { get; set; }

        [Display(Name = "Close", GroupName = "Footprint", Order = 11, Description = "")]
        public bool Footprint_Close { get; set; }

        [Display(Name = "High", GroupName = "Footprint", Order = 12, Description = "")]
        public bool Footprint_High { get; set; }

        [Display(Name = "Low", GroupName = "Footprint", Order = 13, Description = "")]
        public bool Footprint_Low { get; set; }

        [Display(Name = "RangeTicks", GroupName = "Footprint", Order = 14, Description = "")]
        public bool Footprint_RangeTicks { get; set; }

        [Display(Name = "RangeLevels", GroupName = "Footprint", Order = 15, Description = "")]
        public bool Footprint_RangeLevels { get; set; }

        [Display(Name = "Direction", GroupName = "Footprint", Order = 16, Description = "")]
        public bool Footprint_Direction { get; set; }

        [Display(Name = "DurationMs", GroupName = "Footprint", Order = 19, Description = "Bar duration milliseconds")]
        public bool Footprint_DurationMs { get; set; }

        [Display(Name = "Volumes", GroupName = "Footprint", Order = 20, Description = "Volume ladder")]
        public bool Footprint_Volumes { get; set; }

        [Display(Name = "Bids", GroupName = "Footprint", Order = 21, Description = "Bid ladder")]
        public bool Footprint_Bids { get; set; }

        [Display(Name = "Asks", GroupName = "Footprint", Order = 22, Description = "Ask ladder")]
        public bool Footprint_Asks { get; set; }

        [Display(Name = "Deltas", GroupName = "Footprint", Order = 23, Description = "Delta ladder")]
        public bool Footprint_Deltas { get; set; }

        [Display(Name = "TradesNumbers", GroupName = "Footprint", Order = 24, Description = "Trades numbers ladder")]
        public bool Footprint_TradesNumbers { get; set; }

        [Display(Name = "TradesNumber", GroupName = "Footprint", Order = 30, Description = "Total trades number in a bar")]
        public bool Footprint_TradesNumber { get; set; }

        [Display(Name = "Volume", GroupName = "Footprint", Order = 31, Description = "Bar volume")]
        public bool Footprint_Volume { get; set; }

        [Display(Name = "BuyVolume", GroupName = "Footprint", Order = 32, Description = "")]
        public bool Footprint_BuyVolume { get; set; }

        [Display(Name = "SellVolume", GroupName = "Footprint", Order = 33, Description = "")]
        public bool Footprint_SellVolume { get; set; }

        [Display(Name = "Delta", GroupName = "Footprint", Order = 34, Description = "")]
        public bool Footprint_Delta { get; set; }

        [Display(Name = "VAH", GroupName = "Footprint", Order = 35, Description = "")]
        public bool Footprint_VAH { get; set; }

        [Display(Name = "VAL", GroupName = "Footprint", Order = 36, Description = "")]
        public bool Footprint_VAL { get; set; }

        [Display(Name = "POC", GroupName = "Footprint", Order = 37, Description = "")]
        public bool Footprint_POC { get; set; }

        [Display(Name = "POCVolume", GroupName = "Footprint", Order = 38, Description = "")]
        public bool Footprint_POCVolume { get; set; }

        [Display(Name = "BuyPercentage", GroupName = "Footprint", Order = 40, Description = "")]
        public bool Footprint_BuyPercentage { get; set; }

        [Display(Name = "SellPercentage", GroupName = "Footprint", Order = 41, Description = "")]
        public bool Footprint_SellPercentage { get; set; }

        [Display(Name = "DeltaPercentage", GroupName = "Footprint", Order = 42, Description = "")]
        public bool Footprint_DeltaPercentage { get; set; }

        [Display(Name = "MinDelta", GroupName = "Footprint", Order = 43, Description = "")]
        public bool Footprint_MinDelta { get; set; }

        [Display(Name = "MaxDelta", GroupName = "Footprint", Order = 44, Description = "")]
        public bool Footprint_MaxDelta { get; set; }

        [Display(Name = "DeltaChange", GroupName = "Footprint", Order = 45, Description = "")]
        public bool Footprint_DeltaChange { get; set; }

        [Display(Name = "DeltaCumulative", GroupName = "Footprint", Order = 46, Description = "")]
        public bool Footprint_DeltaCumulative { get; set; }

        [Display(Name = "DeltaRate", GroupName = "Footprint", Order = 47, Description = "")]
        public bool Footprint_DeltaRate { get; set; }

        [Display(Name = "DeltaRateHigh", GroupName = "Footprint", Order = 48, Description = "")]
        public bool Footprint_DeltaRateHigh { get; set; }

        [Display(Name = "DeltaRateLow", GroupName = "Footprint", Order = 49, Description = "")]
        public bool Footprint_DeltaRateLow { get; set; }

        [Display(Name = "AbsoluteDeltaAverage", GroupName = "Footprint", Order = 50, Description = "")]
        public bool Footprint_AbsoluteDeltaAverage { get; set; }

        [Display(Name = "AbsoluteDeltaTotal", GroupName = "Footprint", Order = 51, Description = "")]
        public bool Footprint_AbsoluteDeltaTotal { get; set; }

        [Display(Name = "COTHigh", GroupName = "Footprint", Order = 52, Description = "")]
        public bool Footprint_COTHigh { get; set; }

        [Display(Name = "COTLow", GroupName = "Footprint", Order = 53, Description = "")]
        public bool Footprint_COTLow { get; set; }

        [Display(Name = "RatioNumbers", GroupName = "Footprint", Order = 54, Description = "")]
        public bool Footprint_RatioNumbers { get; set; }

        [Display(Name = "VolumePerSecond", GroupName = "Footprint", Order = 55, Description = "")]
        public bool Footprint_VolumePerSecond { get; set; }

        [Display(Name = "UnfinishedAuctionHigh", GroupName = "Footprint", Order = 60, Description = "")]
        public bool Footprint_UnfinishedAuctionHigh { get; set; }

        [Display(Name = "UnfinishedAuctionLow", GroupName = "Footprint", Order = 61, Description = "")]
        public bool Footprint_UnfinishedAuctionLow { get; set; }

        [Display(Name = "BuyImbalanceCount", GroupName = "Footprint", Order = 70, Description = "")]
        public bool Footprint_BuyImbalanceCount { get; set; }

        [Display(Name = "SellImbalanceCount", GroupName = "Footprint", Order = 71, Description = "")]
        public bool Footprint_SellImbalanceCount { get; set; }

        [Display(Name = "BuyAbsorptionCount", GroupName = "Footprint", Order = 72, Description = "")]
        public bool Footprint_BuyAbsorptionCount { get; set; }

        [Display(Name = "SellAbsorptionCount", GroupName = "Footprint", Order = 73, Description = "")]
        public bool Footprint_SellAbsorptionCount { get; set; }

        [Display(Name = "BuyStackedImbalanceCount", GroupName = "Footprint", Order = 74, Description = "")]
        public bool Footprint_BuyStackedImbalanceCount { get; set; }

        [Display(Name = "SellStackedImbalanceCount", GroupName = "Footprint", Order = 75, Description = "")]
        public bool Footprint_SellStackedImbalanceCount { get; set; }

        [Display(Name = "BuyStackedAbsorptionCount", GroupName = "Footprint", Order = 76, Description = "")]
        public bool Footprint_BuyStackedAbsorptionCount { get; set; }

        [Display(Name = "SellStackedAbsorptionCount", GroupName = "Footprint", Order = 77, Description = "")]
        public bool Footprint_SellStackedAbsorptionCount { get; set; }

        [Display(Name = "BuyStackedImbalanceMaxConsec", GroupName = "Footprint", Order = 78, Description = "")]
        public bool Footprint_BuyStackedImbalanceMaxConsec { get; set; }

        [Display(Name = "SellStackedImbalanceMaxConsec", GroupName = "Footprint", Order = 79, Description = "")]
        public bool Footprint_SellStackedImbalanceMaxConsec { get; set; }

        [Display(Name = "BuyStackedAbsorptionMaxConsec", GroupName = "Footprint", Order = 80, Description = "")]
        public bool Footprint_BuyStackedAbsorptionMaxConsec { get; set; }

        [Display(Name = "SellStackedAbsorptionMaxConsec", GroupName = "Footprint", Order = 81, Description = "")]
        public bool Footprint_SellStackedAbsorptionMaxConsec { get; set; }

        [Display(Name = "DeltaDivergence", GroupName = "Footprint", Order = 85, Description = "")]
        public bool Footprint_DeltaDivergence { get; set; }

        [Display(Name = "SessionOpen", GroupName = "Footprint", Order = 90, Description = "")]
        public bool Footprint_SessionOpen { get; set; }

        [Display(Name = "SessionClose", GroupName = "Footprint", Order = 91, Description = "")]
        public bool Footprint_SessionClose { get; set; }

        [Display(Name = "SessionHigh", GroupName = "Footprint", Order = 92, Description = "")]
        public bool Footprint_SessionHigh { get; set; }

        [Display(Name = "SessionLow", GroupName = "Footprint", Order = 93, Description = "")]
        public bool Footprint_SessionLow { get; set; }

        [Display(Name = "SeesionVAH", GroupName = "Footprint", Order = 94, Description = "")]
        public bool Footprint_SeesionVAH { get; set; }

        [Display(Name = "SeesionVAL", GroupName = "Footprint", Order = 95, Description = "")]
        public bool Footprint_SeesionVAL { get; set; }

        [Display(Name = "SessionPOC", GroupName = "Footprint", Order = 96, Description = "")]
        public bool Footprint_SessionPOC { get; set; }
        #endregion Footprint

        #region VolumeProfile
        [Display(Name = "Export", GroupName = "VolumeProfile", Order = 1, Description = "")]
        public bool VolumeProfile_Export { get; set; }

        [Display(Name = "Granularity", GroupName = "VolumeProfile", Order = 2, Description = "")]
        public ExportGranularity VolumeProfile_ExportGranularity { get; set; } = ExportGranularity.Bar;

        [Display(Name = "Temporality", GroupName = "VolumeProfile", Order = 3, Description = "")]
        public ExportTemporality Volumeprofile_ExportTemporality { get; set; } = ExportTemporality.Historical;

        [Display(Name = "File", GroupName = "VolumeProfile", Order = 4, Description = "")]
        public string VolumeProfile_File { get; set; } = "volumeprofile\\";

        [Display(Name = "Export real-time", GroupName = "VolumeProfile", Order = 5, Description = "Append rows to the file as they are collected, while the strategy is running. When off, all rows are written at once when the strategy is disabled.")]
        public bool VolumeProfile_Realtime { get; set; } = false;

        [Display(Name = "Open", GroupName = "VolumeProfile", Order = 9, Description = "")]
        public bool VolumeProfile_Open { get; set; }

        [Display(Name = "Close", GroupName = "VolumeProfile", Order = 10, Description = "")]
        public bool VolumeProfile_Close { get; set; }

        [Display(Name = "High", GroupName = "VolumeProfile", Order = 11, Description = "")]
        public bool VolumeProfile_High { get; set; }

        [Display(Name = "Low", GroupName = "VolumeProfile", Order = 12, Description = "")]
        public bool VolumeProfile_Low { get; set; }

        [Display(Name = "RangeTicks", GroupName = "VolumeProfile", Order = 13, Description = "")]
        public bool VolumeProfile_RangeTicks { get; set; }

        [Display(Name = "MID", GroupName = "VolumeProfile", Order = 14, Description = "")]
        public bool VolumeProfile_MID { get; set; }

        [Display(Name = "DurationMs", GroupName = "VolumeProfile", Order = 15, Description = "")]
        public bool VolumeProfile_DurationMs { get; set; }

        [Display(Name = "TradesNumber", GroupName = "VolumeProfile", Order = 20, Description = "")]
        public bool VolumeProfile_TradesNumber { get; set; }

        [Display(Name = "Volume", GroupName = "VolumeProfile", Order = 21, Description = "")]
        public bool VolumeProfile_Volume { get; set; }

        [Display(Name = "BuyVolume", GroupName = "VolumeProfile", Order = 23, Description = "")]
        public bool VolumeProfile_BuyVolume { get; set; }

        [Display(Name = "SellVolume", GroupName = "VolumeProfile", Order = 24, Description = "")]
        public bool VolumeProfile_SellVolume { get; set; }

        [Display(Name = "Delta", GroupName = "VolumeProfile", Order = 25, Description = "")]
        public bool VolumeProfile_Delta { get; set; }

        [Display(Name = "VAH", GroupName = "VolumeProfile", Order = 26, Description = "")]
        public bool VolumeProfile_VAH { get; set; }

        [Display(Name = "VAL", GroupName = "VolumeProfile", Order = 27, Description = "")]
        public bool VolumeProfile_VAL { get; set; }

        [Display(Name = "POC", GroupName = "VolumeProfile", Order = 28, Description = "")]
        public bool VolumeProfile_POC { get; set; }

        [Display(Name = "POCVolume", GroupName = "VolumeProfile", Order = 29, Description = "")]
        public bool VolumeProfile_POCVolume { get; set; }

        [Display(Name = "TickPOC", GroupName = "VolumeProfile", Order = 30, Description = "")]
        public bool VolumeProfile_TickPOC { get; set; }

        [Display(Name = "TickPOCVolume", GroupName = "VolumeProfile", Order = 31, Description = "")]
        public bool VolumeProfile_TickPOCVolume { get; set; }

        [Display(Name = "VWAP", GroupName = "VolumeProfile", Order = 40, Description = "")]
        public bool VolumeProfile_VWAP { get; set; }

        [Display(Name = "_1StdDeviationPos", GroupName = "VolumeProfile", Order = 41, Description = "")]
        public bool VolumeProfile__1StdDeviationPos { get; set; }

        [Display(Name = "_1StdDeviationNeg", GroupName = "VolumeProfile", Order = 42, Description = "")]
        public bool VolumeProfile__1StdDeviationNeg { get; set; }

        [Display(Name = "_2StdDeviationPos", GroupName = "VolumeProfile", Order = 43, Description = "")]
        public bool VolumeProfile__2StdDeviationPos { get; set; }

        [Display(Name = "_2StdDeviationNeg", GroupName = "VolumeProfile", Order = 44, Description = "")]
        public bool VolumeProfile__2StdDeviationNeg { get; set; }

        [Display(Name = "DeltaPercentage", GroupName = "VolumeProfile", Order = 50, Description = "")]
        public bool VolumeProfile_DeltaPercentage { get; set; }

        [Display(Name = "BuyPOCVolume", GroupName = "VolumeProfile", Order = 51, Description = "")]
        public bool VolumeProfile_BuyPOCVolume { get; set; }

        [Display(Name = "SellPOCVolume", GroupName = "VolumeProfile", Order = 52, Description = "")]
        public bool VolumeProfile_SellPOCVolume { get; set; }

        [Display(Name = "VAVolume", GroupName = "VolumeProfile", Order = 53, Description = "")]
        public bool VolumeProfile_VAVolume { get; set; }

        [Display(Name = "TPO_POC", GroupName = "VolumeProfile", Order = 60, Description = "")]
        public bool VolumeProfile_TPO_POC { get; set; }

        [Display(Name = "TPO_VAH", GroupName = "VolumeProfile", Order = 61, Description = "")]
        public bool VolumeProfile_TPO_VAH { get; set; }

        [Display(Name = "TPO_VAL", GroupName = "VolumeProfile", Order = 62, Description = "")]
        public bool VolumeProfile_TPO_VAL { get; set; }

        [Display(Name = "TPOLettersCount", GroupName = "VolumeProfile", Order = 63, Description = "")]
        public bool VolumeProfile_TPOLettersCount { get; set; }
        #endregion VolumeProfile

        #region BigTrade
        [Display(Name = "Export", GroupName = "BigTrade", Order = 1, Description = "")]
        public bool BigTrade_Export { get; set; }

        [Display(Name = "Temporality", GroupName = "BigTrade", Order = 2, Description = "")]
        public ExportTemporality BigTrade_ExportTemporality { get; set; } = ExportTemporality.Historical;

        [Display(Name = "File", GroupName = "BigTrade", Order = 3, Description = "")]
        public string BigTrade_File { get; set; } = "bigtrade\\";

        [Display(Name = "Export real-time", GroupName = "BigTrade", Order = 4, Description = "Append rows to the file as they are collected, while the strategy is running. When off, all rows are written at once when the strategy is disabled.")]
        public bool BigTrade_Realtime { get; set; } = false;

        [Display(Name = "High", GroupName = "BigTrade", Order = 10, Description = "")]
        public bool BigTrade_High { get; set; }

        [Display(Name = "Low", GroupName = "BigTrade", Order = 11, Description = "")]
        public bool BigTrade_Low { get; set; }

        [Display(Name = "Volume", GroupName = "BigTrade", Order = 12, Description = "")]
        public bool BigTrade_Volume { get; set; }

        [Display(Name = "IcebergVolume", GroupName = "BigTrade", Order = 13, Description = "")]
        public bool BigTrade_IcebergVolume { get; set; }

        [Display(Name = "Direction", GroupName = "BigTrade", Order = 14, Description = "Buy 1; Sell -1")]
        public bool BigTrade_Direction { get; set; }

        [Display(Name = "RangeTicks", GroupName = "BigTrade", Order = 15, Description = "")]
        public bool BigTrade_RangeTicks { get; set; }

        [Display(Name = "POC", GroupName = "BigTrade", Order = 20, Description = "")]
        public bool BigTrade_POC { get; set; }

        [Display(Name = "POCVolume", GroupName = "BigTrade", Order = 21, Description = "")]
        public bool BigTrade_POCVolume { get; set; }

        [Display(Name = "DomPressureVolume", GroupName = "BigTrade", Order = 22, Description = "")]
        public bool BigTrade_DomPressureVolume { get; set; }

        [Display(Name = "DomSupportVolume", GroupName = "BigTrade", Order = 23, Description = "")]
        public bool BigTrade_DomSupportVolume { get; set; }

        [Display(Name = "Smart/Predatory", GroupName = "BigTrade", Order = 24, Description = "")]
        public bool BigTrade_Smart { get; set; }

        [Display(Name = "TicksNumber", GroupName = "BigTrade", Order = 25, Description = "Number of ticks (> 1 for reconstructed trade)")]
        public bool BigTrade_TicksNumber { get; set; }

        [Display(Name = "Ticks", GroupName = "BigTrade", Order = 26, Description = "")]
        public bool BigTrade_Ticks { get; set; }
        #endregion BigTrade

        #region MarketDepth
        [Display(Name = "Export", GroupName = "MarketDepth", Order = 1, Description = "")]
        public bool MarketDepth_Export { get; set; }

        [Display(Name = "File", GroupName = "MarketDepth", Order = 3, Description = "")]
        public string MarketDepth_File { get; set; } = "orderbook\\";

        [Display(Name = "Export real-time", GroupName = "MarketDepth", Order = 5, Description = "Append rows to the file as they are collected, while the strategy is running. WARNING: the orderbook is exported on every DOM update (thousands per second), so real-time writing produces very large files quickly.")]
        public bool MarketDepth_Realtime { get; set; } = false;

        //[Display(Name = "Export on each tick", GroupName = "MarketDepth", Order = 2, Description = "Export real-time DOM values on each tick")]
        //public bool MarketDepth_OnEachTickExport { get; set; }

        //[Display(Name = "Export on each tick real-time DOM only", GroupName = "MarketDepth", Order = 3, Description = "Export only real-time DOM values on each tick")]
        //public bool MarketDepth_OnEachTickRealtimeValuesExport { get; } = true;

        //[Display(Name = "Export Quantative Depth", GroupName = "MarketDepth", Order = 4, Description = "")]
        //public bool MarketDepth_ExportQuantativeDepth { get; set; }

        [Display(Name = "MarketDepth", GroupName = "MarketDepth", Order = 9, Description = "")]
        public bool MarketDepth_MarketDepth { get; set; }

        [Display(Name = "RealMarketDepth", GroupName = "MarketDepth", Order = 10, Description = "Real (actual) size of market depth")]
        public bool MarketDepth_RealMarketDepth { get; set; }

        //[Display(Name = "Open", GroupName = "MarketDepth", Order = 11, Description = "Bar open")]
        //public bool MarketDepth_Open { get; set; }

        //[Display(Name = "Close", GroupName = "MarketDepth", Order = 12, Description = "Bar close")]
        //public bool MarketDepth_Close { get; set; }

        //[Display(Name = "High", GroupName = "MarketDepth", Order = 13, Description = "Bar high")]
        //public bool MarketDepth_High { get; set; }

        //[Display(Name = "Low", GroupName = "MarketDepth", Order = 14, Description = "Bar low")]
        //public bool MarketDepth_Low { get; set; }

        [Display(Name = "BestBid", GroupName = "MarketDepth", Order = 18, Description = "")]
        public bool MarketDepth_BestBid { get; set; }

        [Display(Name = "BestOffer", GroupName = "MarketDepth", Order = 19, Description = "")]
        public bool MarketDepth_BestOffer { get; set; }

        //[Display(Name = "DepthHigh", GroupName = "MarketDepth", Order = 20, Description = "")]
        //public bool MarketDepth_HisotricalDepthHigh { get; set; }

        //[Display(Name = "DepthLow", GroupName = "MarketDepth", Order = 21, Description = "")]
        //public bool MarketDepth_HistoricalDepthLow { get; set; }

        //[Display(Name = "HistoricalBids", GroupName = "MarketDepth", Order = 22, Description = "")]
        //public bool MarketDepth_HistoricalBids { get; set; }

        //[Display(Name = "HistoricalOffers", GroupName = "MarketDepth", Order = 23, Description = "")]
        //public bool MarketDepth_HistoricalOffers { get; set; }

        //[Display(Name = "HistoricalBidsVolume", GroupName = "MarketDepth", Order = 24, Description = "")]
        //public bool MarketDepth_HistoricalBidsVolume { get; set; }

        //[Display(Name = "HistoricalOffersVolume", GroupName = "MarketDepth", Order = 25, Description = "")]
        //public bool MarketDepth_HistoricalOffersVolume { get; set; }

        [Display(Name = "RealtimeBids", GroupName = "MarketDepth", Order = 28, Description = "")]
        public bool MarketDepth_RealtimeBids { get; set; }

        [Display(Name = "RealtimeOffers", GroupName = "MarketDepth", Order = 29, Description = "")]
        public bool MarketDepth_RealtimeOffers { get; set; }

        [Display(Name = "RealtimeBidsWithPrices", GroupName = "MarketDepth", Order = 30, Description = "")]
        public bool MarketDepth_RealtimeBidsWithPrices { get; set; }

        [Display(Name = "RealtimeOffersWithPrices", GroupName = "MarketDepth", Order = 31, Description = "")]
        public bool MarketDepth_RealtimeOffersWithPrices { get; set; }

        [Display(Name = "RealtimeBidVolume", GroupName = "MarketDepth", Order = 32, Description = "")]
        public bool MarketDepth_RealtimeBidVolume { get; set; }

        [Display(Name = "RealtimeOfferVolume", GroupName = "MarketDepth", Order = 33, Description = "")]
        public bool MarketDepth_RealtimeOfferVolume { get; set; }

        //[Display(Name = "LiquidityOpen", GroupName = "MarketDepth", Order = 50, Description = "")]
        //public bool MarketDepth_LiquidityOpen { get; set; }

        //[Display(Name = "LiquidityClose", GroupName = "MarketDepth", Order = 51, Description = "")]
        //public bool MarketDepth_LiquidityClose { get; set; }

        //[Display(Name = "LiquidityHigh", GroupName = "MarketDepth", Order = 52, Description = "")]
        //public bool MarketDepth_LiquidityHigh { get; set; }

        //[Display(Name = "LiquidityLow", GroupName = "MarketDepth", Order = 53, Description = "")]
        //public bool MarketDepth_LiquidityLow { get; set; }

        //[Display(Name = "LiquidityMigrationOpen", GroupName = "MarketDepth", Order = 60, Description = "")]
        //public bool MarketDepth_LiquidityMigrationOpen { get; set; }

        //[Display(Name = "LiquidityMigrationClose", GroupName = "MarketDepth", Order = 61, Description = "")]
        //public bool MarketDepth_LiquidityMigrationClose { get; set; }

        //[Display(Name = "LiquidityMigrationHigh", GroupName = "MarketDepth", Order = 62, Description = "")]
        //public bool MarketDepth_LiquidityMigrationHigh { get; set; }

        //[Display(Name = "LiquidityMigrationLow", GroupName = "MarketDepth", Order = 63, Description = "")]
        //public bool MarketDepth_LiquidityMigrationLow { get; set; }

        //[Display(Name = "HQA", GroupName = "MarketDepth", Order = 70, Description = "")]
        //public bool MarketDepth_HQA { get; set; }

        //[Display(Name = "UQ", GroupName = "MarketDepth", Order = 71, Description = "")]
        //public bool MarketDepth_UQ { get; set; }

        //[Display(Name = "AC", GroupName = "MarketDepth", Order = 72, Description = "")]
        //public bool MarketDepth_AC { get; set; }


        #endregion MarketDepth
    }
}
#endif
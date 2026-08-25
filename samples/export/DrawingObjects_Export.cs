// www.mzpack.pro
// To compile in NinjaTrader 8 remove '#if STRAT' and '#endif' directives, and add reference to MZpack.NT8.Pro.dll
#if STRAT
using MZpack;
using MZpack.NT8;
using MZpack.NT8.Algo;
using MZpack.NT8.Algo.DataExport;
using NinjaTrader.Gui;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Windows;

namespace NinjaTrader.NinjaScript.Strategies.MZpackStrategies
{
    /// <summary>
    /// A tool for exporting chart objects to .csv files.
    /// </summary>
    [CategoryOrder("Export", 0)]
    public class DrawingObjects_Export : MZpackStrategyBase
    {
        protected override void OnStateChange()
        {
            // Base OnStateChange() call is required
            base.OnStateChange();

            lock (Sync) // Sync OnStateChange handler
            {
                if (State == State.SetDefaults)
                {
                    BarsRequiredToTrade = 1;

                    // To export historical values
                    EnableBacktesting = true;

                    // Export defaults
                    //Export_End = DateTime.Now;
                    Export_Delimiter = ";";
                }
                else if (State == State.Configure)
                {
                }
                else if (State == State.DataLoaded)
                {
                    char delimiter = !string.IsNullOrEmpty(Export_Delimiter) ? Export_Delimiter[0] : Export.DELIMITER;

                    // Create data export object.
                    // By default export format is CSV to file Documents\NinjaTrader 8\mzpack\strategy\<namespace>\<strategy class name>\data\<random alphanumeric>.csv
                    var export = new DrawingObjectsExport(this,
                        new ExportArgs()
                        {
                            IsExportWhileCollecting = false,
                            IsHeader = Export_Header,  // Set header
                            IsTime = Export_Time,
                            Delimiter = delimiter,  // Values delimiter
                            IsFile = true,
                            FileName = Export_File,  // File name with path (optional)
                            IsBatch = Export_Batch,
                        });

                    string path = GetBasePath(this) + "\\" + Export_SchemaFile;
                    if (System.IO.File.Exists(path))
                    {
                        export.DataSet.Schema = DataSchema.LoadFromXml(this, path); 
                    }
                    else
                    {
                        ShowMessageBoxAsync($"Schema file not found: {path}", DisplayName, MessageBoxImage.Information);

                        // Create sample schema
                        DataSchema schema = new DataSchema();
                        schema.Append("BOB", ValueKind.Feature, new ChartObjectDescriptor() { Script = "ninZaBossOrderBlock", Map = new List<MapItem>() 
                        { 
                            new MapItem() { Tool = DrawingTool.Text, Text = "▲", Value = 1.0 }, 
                            new MapItem() { Tool = DrawingTool.Text, Text = "▼", Value = -1.0 } 
                        } });
                        //schema.Append("BOB", ValueKind.Feature, new ChartObjectDescriptor() { Tag = "ninZaBossOrderBlock", Map = new List<MapItem>() { new MapItem() { Text = "bullish", Value = 1.0 }, new MapItem() { Text = "bearish", Value = -1.0 } } });
                        //schema.Append("IPL IR", ValueKind.Feature, new ChartObjectDescriptor() { Tag = "ninZaImbalanceProfileLidar.marker.imbalancereturn", Map = new List<MapItem>() { new MapItem() { Text = "bullish", Value = 1.0 }, new MapItem() { Text = "bearish", Value = -1.0 } } });
                        //schema.Append("IPL VR", ValueKind.Feature, new ChartObjectDescriptor() { Tag = "ninZaImbalanceProfileLidar.marker.volumereturn", Map = new List<MapItem>() { new MapItem() { Text = "bullish", Value = 1.0 }, new MapItem() { Text = "bearish", Value = -1.0 } } });
                        //schema.Append("IPL breakup", ValueKind.Feature, new ChartObjectDescriptor() { Tag = "ninZaImbalanceProfileLidar.marker", Map = new List<MapItem>() { new MapItem() { Text = "breakup", Value = 1.0 } } });
                        //schema.Append("IPL breakdown", ValueKind.Feature, new ChartObjectDescriptor() { Tag = "ninZaImbalanceProfileLidar.marker", Map = new List<MapItem>() { new MapItem() { Text = "breakdown", Value = -1.0 } } });

                        schema.SaveToXml(path);  // Save to xml

                        ShowMessageBoxAsync($"Sample schema has been created. Saved to file: {path}.", DisplayName, MessageBoxImage.Information);

                        export.DataSet.Schema = schema;
                    }

                    Register(export);  // Register export
                }
            }
        }


        #region Export
        [Display(Name = "Schema file", GroupName = "Export", Order = 1, Description = "")]
        public string Export_SchemaFile { get; set; } = "schema.xml";

        [Display(Name = "Temporality", GroupName = "Export", Order = 2, Description = "")]
        public ExportTemporality Export_Temporality { get; set; } = ExportTemporality.Historical;

        [Display(Name = "File", GroupName = "Export", Order = 3, Description = "")]
        public string Export_File { get; set; } = "";

        [Display(Name = "Header", GroupName = "Export", Order = 4, Description = "")]
        public bool Export_Header { get; set; } = true;

        [Display(Name = "Time", GroupName = "Export", Order = 5, Description = "")]
        public bool Export_Time { get; set; } = true;

        [Display(Name = "Batch", GroupName = "Export", Order = 6, Description = "")]
        public bool Export_Batch { get; set; }

        [Display(Name = "Delimiter", GroupName = "Export", Order = 7, Description = "")]
        public string Export_Delimiter { get; set; }
        #endregion Export
    }
}
#endif
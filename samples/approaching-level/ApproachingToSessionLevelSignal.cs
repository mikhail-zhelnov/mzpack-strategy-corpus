#if DATA
using System;
using System.Collections.Generic;
using System.Linq;
using MZpack;
using MZpack.NT8.Algo;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.Strategies.MZpackStrategies.GhostResistanceStrategy.Signals
{
    /// <summary>
    /// Generates ANY direction if the price is approaching to session volume profile levels.
    /// </summary>
    public class ApproachingToSessionLevelSignal : Signal
    {
        public ApproachingToSessionLevelSignal(MZpack.NT8.Algo.Strategy strategy) : base(strategy, MarketDataSource.Level1, MZpack.NT8.Algo.SignalCalculate.OnBarClose, true)
        {
            HasPrice = true;
        }

        public ApproachingToSessionLevelSignal() : base()
        {
        }

        public override void OnCalculate(MarketDataEventArgs e, int barIdx, SignalDirection allowed)
        {
            GhostResistance strategy = Strategy.MZpackStrategy as GhostResistance;
            IVolumeProfile profile = strategy.VolumeProfileIndicator.Profiles.LastOrDefault() as IVolumeProfile;

            if (profile == null)
                return;

            profile = profile.Prior as IVolumeProfile;  // Get profile for prior session
            if (profile == null)
                return;

            IVolumeProfile profileRTH = profile.IsRTH() ? profile : null;  // Get profile for RTH session
            IVolumeProfile profileETH = profile.IsETH() ? profile : null;  // Get profile for ETH session

            profile = profile.Prior as IVolumeProfile;  // Get profile for prior session
            if (profile == null)
                return;

            profileRTH = profile.IsRTH() ? profile : null;  // Get profile for RTH session
            profileETH = profile.IsETH() ? profile : null;  // Get profile for ETH session

            allowed = strategy.AbsorptionSignal.Direction;  // Get allowed from Abs signal
            //if (allowed == SignalDirection.None)  // Short sercuit
            //    return;
            SignalDirection direction = SignalDirection.None;

            ICandle candle = strategy.GetCandle(GetCurrentBarAgo(0));
            if (candle == null) return;

            List<string> levels = new List<string>();
            double current = allowed == SignalDirection.Long ? candle.Low : candle.High; // The direction is Long or Short

            // Find overnight levels
            if (profileETH != null)
            {
                if (strategy.Strategy_OvernightVP_HighLow)
                {
                    if (strategy.IsPriceApproaching(current, profileETH.High, strategy.Strategy_SessionVP_Approaching))
                    {
                        direction = allowed;
                        levels.Add("Overnight High");
                    }
                    else
                    if (strategy.IsPriceApproaching(current, profileETH.Low, strategy.Strategy_SessionVP_Approaching))
                    {
                        direction = allowed;
                        levels.Add("Overnight Low");
                    }
                }
                if (strategy.Strategy_OvernightVP_POC)
                {
                    if (strategy.IsPriceApproaching(current, profileETH.POC, strategy.Strategy_SessionVP_Approaching))
                    {
                        direction = allowed;
                        levels.Add("Overnight POC");
                    }
                }
                if (strategy.Strategy_OvernightVP_VAHVAL)
                {
                    if (strategy.IsPriceApproaching(current, profileETH.VAH, strategy.Strategy_SessionVP_Approaching))
                    {
                        direction = allowed;
                        levels.Add("Overnight VAH");
                    }
                    else
                    if (strategy.IsPriceApproaching(current, profileETH.VAL, strategy.Strategy_SessionVP_Approaching))
                    {
                        direction = allowed;
                        levels.Add("Overnight VAL");
                    }
                }
            }

            // Find levels of prior RTH session 
            if (profileRTH != null) 
            {
                if (strategy.Strategy_RTHVP_HighLow)
                {
                    if (strategy.IsPriceApproaching(current, profileRTH.High, strategy.Strategy_SessionVP_Approaching))
                    {
                        direction = allowed;
                        levels.Add("RTH High");
                    }
                    else
                    if (strategy.IsPriceApproaching(current, profileRTH.Low, strategy.Strategy_SessionVP_Approaching))
                    {
                        direction = allowed;
                        levels.Add("RTH Low");
                    }
                }
                if (strategy.Strategy_RTHVP_POC)
                {
                    if (strategy.IsPriceApproaching(current, profileRTH.POC, strategy.Strategy_SessionVP_Approaching))
                    {
                        direction = allowed;
                        levels.Add("RTH POC");
                    }
                }
                if (strategy.Strategy_RTHVP_VAHVAL)
                {
                    if (strategy.IsPriceApproaching(current, profileRTH.VAH, strategy.Strategy_SessionVP_Approaching))
                    {
                        direction = allowed;
                        levels.Add("RTH VAH");
                    }
                    else
                    if (strategy.IsPriceApproaching(current, profileRTH.VAL, strategy.Strategy_SessionVP_Approaching))
                    {
                        direction = allowed;
                        levels.Add("RTH VAL");
                    }
                }
            }

            if (IsDetermined(direction))
            {
                Direction = direction;
                Time = e.Time;
                if (HasPrice) EntryPrice = GetEntryPrice(e, direction);
                ChartRange = new ChartRange()
                {
                    MinBarIdx = barIdx,
                    MaxBarIdx = barIdx
                };

                Description = $" => {string.Join(", ",levels)}";
            }
        }
    }
}
#endif


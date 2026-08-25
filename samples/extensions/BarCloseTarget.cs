#if DATA
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZpack.NT8.Algo
{
    /// <summary>
    /// Exit position on bar close
    /// </summary>
    public class BarCloseTarget : ExitBase
    {
        
        public BarCloseTarget() : base()
        {
            Calculate = NinjaTrader.NinjaScript.Calculate.OnBarClose;
        }

        public override bool CheckExit(MarketDataEventArgs e, Entry entry, MarketPosition marketPosition, out string reason)
        {
            if (entry.Strategy.MZpackStrategy.CurrentBar > entry.FilledBarIdx)
            {
                reason = @"Bar close";
                return true;
            }

            reason = @"";
            return false;
        }
    }
}
#endif
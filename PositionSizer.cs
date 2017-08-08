#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class PositionSizer : Strategy
	{
		///  money management
		public double 	shares;
		public int 	initialBalance;
		public	int 	cashAvailiable;
		public	double 	priorTradesCumProfit;
		public	int 	priorTradesCount;
		public	double 	sharesFraction;
		
//		protected override void OnStateChange()
//		{
//			if (State == State.SetDefaults)
//			{
//				Description									= @"Custom Class To Calculate Position Sizes";
//				Name										= "PositionSizer";
//				Calculate									= Calculate.OnBarClose;
//				EntriesPerDirection							= 1;
//				EntryHandling								= EntryHandling.AllEntries;
//				IsExitOnSessionCloseStrategy				= true;
//				ExitOnSessionCloseSeconds					= 30;
//				IsFillLimitOnTouch							= false;
//				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
//				OrderFillResolution							= OrderFillResolution.Standard;
//				Slippage									= 0;
//				StartBehavior								= StartBehavior.WaitUntilFlat;
//				TimeInForce									= TimeInForce.Gtc;
//				TraceOrders									= false;
//				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
//				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
//				BarsRequiredToTrade							= 20;
//				// Disable this property for performance gains in Strategy Analyzer optimizations
//				// See the Help Guide for additional information
//				IsInstantiatedOnEachOptimizationIteration	= true;
//			}
//			else if (State == State.Configure)
//			{
//			}
//		}

//		protected override void OnBarUpdate()
//		{
//			//Add your custom strategy logic here.
//		}
		
		/// <summary>
		/// Caksulate the positions size given portfolio size and number of strategies
		/// </summary>
		/// <param name="theClose"></param>
		/// <param name="totalProfit"></param>
		/// <returns></returns>
		public int calcPositionSizes(double theClose, double totalProfit, int systems, int capital) {
			/// d. Maximum 20% of capital in any single index , Maximum 10 positions 
			/// e. Buy in equal dollar amounts
			/// c. Maximum 10% of capital in any single position 
			/// Store the strategy's prior cumulated realized profit and number of trades
			priorTradesCumProfit = totalProfit;	//SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
			/// cal initialBalance s portion  of portfoli / num systems
			initialBalance = capital / systems ;
			/// Adjust position size for profit and loss
			cashAvailiable = initialBalance + (int)priorTradesCumProfit;
			/// calc positionsize
			sharesFraction = cashAvailiable / theClose; // Close[0]
			return (int)sharesFraction;
		}
	}
}
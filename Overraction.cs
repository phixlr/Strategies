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
	public class Overraction : Strategy
	{
		
		public double 	entryPrice;
		public int 		entryBar;
		public int 		entryBar2;
		public bool 	entryOne	 	= false;
		public bool 	entryTwo 		= false;
		/// short
		public int		entryBar2short 	= 1;
		public bool 	entryTwoShort 	= true;
		
		
		///  money management
		public double 	shares;
		public int  	portfolioSize	= 826000;
		public int		numSystems		= 10;
		private int 	initialBalance;
		private	int 	cashAvailiable;
		private	double 	priorTradesCumProfit;
		private	int 	priorTradesCount;
		private	double 	sharesFraction;
		
		/// stops
		private double 	stopLine;
		private double 	trailStop;
		private double  lossInPonts;
		private bool	autoStop = true;
		
		///  indicators
		private MarketCondition MarketCondition1;
		private SMA		sma0;
		
		///  reporting
		private double gainInPoints; 
		private	double riskInLastTrade; 
		private	double rValue; 
		private	double totalR; 
		public string 	ComputerName	= "MBP";
		
		private Order entryOrder = null;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "Over Reaction System";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 2;
				EntryHandling								= EntryHandling.UniqueEntries;
				IsExitOnSessionCloseStrategy				= false;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				USIndex							= true;
				SecondSignal					= true;
				NumberOfSystems					= 10;
				ExitAfterNbars					= false;
				StartingCapital					= 826000;
				/// Set inputs to public
				UseMarketCondition 				= false;
				StopPct 						= 3;
				AutoSetStopFromMarket			= true;
				ExitAfterBArs 					= 7;
				UseTrailStop 					= false;
			}
			else if (State == State.Configure)
			{
				AddChartIndicator(MarketCondition(showBands: false, showVolatilityText: false));
			}
			else if (State == State.DataLoaded)
			{
				sma0				= SMA(200);
				ClearOutputWindow(); 
			}
		}

		/// <summary>
		/// Universe: SPY, QQQ, DIA, MDY, IWM, EFA, ILF, EEM, EPP,  IEV (USO Good too)
		/// Do NOT Trade on QQQ, too volatile and stops are missed on entry bar
		/// MARK: - TODO - Do Not Auto Trade this because of entry bar stop problem
		/// Performance:  cash 75% of the time,Win Pct 65%, average wins >= average losses
		/// </summary>
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 												ON BAR  UPDATE
		/// 
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 20 ) { return; }
			/// Long Channel Conditions + Entry
			setLongSentry(stopPct: StopPct, marketCond: UseMarketCondition);
			setShortEntry(stopPct: StopPct, marketCond: UseMarketCondition);
			
			setLongTarget();
			setShortTarget();
			
			setInitialStop(pct: StopPct, auto: AutoSetStopFromMarket);
			setInitialStopShort(pct: StopPct, auto: AutoSetStopFromMarket);
			
			exitAfterNBars(bars: ExitAfterBArs, active: ExitAfterNbars); 
			
			setTrailOnClose(isOn: UseTrailStop);
			/// stats in GUI
			setTextBox(textInBox: popuateStatsTextBox());
			
			setSecondEntry(stopPct: StopPct);
			setSecondEntryShort(stopPct: StopPct);
	
			/// TODO: Save file as stream
			/// TODO: Open Profit > 5%
		
		}
		
		protected override void OnOrderUpdate(Cbi.Order order, double limitPrice, double stopPrice,
                                    int quantity, int filled, double averageFillPrice,
                                    Cbi.OrderState orderState, DateTime time, Cbi.ErrorCode error, string comment)
		{
		  //Print("The most current order state is: " + order.OrderState);   // OrderState.PartFilled
		  //Print("This particular order update state is: " + orderState); // OrderState.Working
			if( filled != null && ( order.Name == "ORs 1" || order.Name == "ORs 2" )  ) {
				Print(order);
				Print("This particular order update state is: " + filled +"\t"+ order.OrderType+"\t"+ order.Name ); // OrderState.Working
				setInitialStopShort(pct: StopPct, auto: AutoSetStopFromMarket);
			}
			
			if( filled != null && ( order.Name == "OR 1" || order.Name == "OR 2" )  ) {
				Print(order);
				Print("This particular order update state is: " + filled +"\t"+ order.OrderType+"\t"+ order.Name ); // OrderState.Working
				setInitialStop(pct: StopPct, auto: AutoSetStopFromMarket);
			}
			
		}
		/// Advanced: use the word market model to but the strongest index
		/* Optional: 
		10. Optional decisions/rules: 
		a. For broad US indices (DIA, SPY, QQQQ, MDY, IWM): use a 3% initial capital preservation stop loss. Use a trailing stop. b. For Semiconductors (IGW) and international indices use a 5% initial capital preservation stop loss. Use a trailing stop. c. Windfall profit exit: if you a 5% gain in a position, either cash it or tighten your trailing stop to 1% trailing stop. d. Time exit: exit at the open of the 8th day if you are still in the trade and no other exit has been triggered. e. To reduce volatility, you could elect to take only the US index signals f. To reduce volatility, you could elect to take long-only signals g. To operate within a retirement account, you could employ Powershares inverse ETFs to go long on inverse ETFs and actually be taking a short side position. h. To reduce volatility, you could elect to not take signals when price is within 2% of the index’s 200 day MA, since there is more whipsawing when the long term trend is not fully established. i. To reduce high tech exposure you could elect to not take IGW signals when you get both QQQQ and IGW signals on the same day j. To try for more profits, you could elect to trail successful trades with a 1x ATR% trailing stop or 3% trailing stop and try to convert this trade into a longer term trend following position.
		*/

		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 												POSTION SIZE
		/// 
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////		
		protected int calcPositionSize() {
			/// d. Maximum 20% of capital in any single index , Maximum 10 positions 
			/// e. Buy in equal dollar amounts
			/// c. Maximum 10% of capital in any single position 
			/// Store the strategy's prior cumulated realized profit and number of trades
			priorTradesCumProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
			/// cal initialBalance s portion  of portfoli / num systems
			initialBalance = portfolioSize / numSystems ;
			/// Adjust position size for profit and loss
			cashAvailiable = initialBalance + (int)priorTradesCumProfit;
			/// calc positionsize
			sharesFraction = cashAvailiable / Close[0];
			return (int)sharesFraction;
		}
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 												MARKET CONDITION
		/// 
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////		
		protected bool setMarketConditionFilter(bool isOn) {
			bool signal = false;
			if (isOn) {
				int conditionNumber =MarketCondition(showBands: false, showVolatilityText: false).MarketCond[0];
				// Console.WriteLine($"Full name - {person.firstName} {person.lastName}");
				Print("\tMarket Cindition is on\t" + conditionNumber);
				///if 1 2 3 6
				if ( conditionNumber != 0 && ( conditionNumber < 4 || conditionNumber == 6 )) {
					signal = true;
				}
			} 
			if (!isOn) {
				signal = true;
				Print("\tMarket Condition is OFF !!!\t");
			}
			return signal;
		}

		protected bool setMarketConditionFilterShort(bool isOn) {
			bool signal = false;
			if (isOn) {
				int conditionNumber = MarketCondition(showBands: false, showVolatilityText: false).MarketCond[0];
				Print("\tMarket Condition is on\t" + conditionNumber);
				///if 7 8 9
				if ( conditionNumber > 6 ) {
					signal = true;
				}
			} 
			if (!isOn) {
				signal = true;
				Print("\tMarket Cindition is OFF !!!\t");
			}
			return signal;
		}

		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 												ENTER LONG
		/// 
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		protected bool entryConditions()
		{
			bool signal = false;
			double onePercent = Close[0] * 0.01;
			if ((Close[0] > Math.Abs(sma0[0])  && High[0] < SMA(10)[0] && Close[0] < ( SMA(10)[0]- ATR(14)[0])) ||
				(Close[0] > Math.Abs(sma0[0])  && High[0] < SMA(10)[0] && Close[0] < ( SMA(10)[0]- onePercent )) ){
				signal = true;
				BarBrush = Brushes.Cyan;
				CandleOutlineBrush = Brushes.Cyan;
			}
			return signal;
		}
		
		protected void setLongSentry(int stopPct, bool marketCond) {
			/// Entry #1
			if ( Position.MarketPosition == MarketPosition.Flat && entryConditions() && setMarketConditionFilter( isOn: marketCond ) ) {
				shares = calcPositionSize();
				EnterLong(Convert.ToInt32(shares), "LE 1");
				entryPrice = Close[0];
				entryBar = 1;
				entryOne = true;
				string entryData = "------->  LE on " +Instrument.MasterInstrument.Name + " shares " + shares + "  $" + cashAvailiable;
				Print(entryData);
				
				string entryTxt = "LE 1\n" + shares+" shares" + "\n$"+cashAvailiable.ToString("0");
				
				calcInitialStop(pct: stopPct);
				// calc hard stop loss in $$$
				/// entryPrice - stopline * shares
				// show loss at stop line
				lossInPonts =  ( stopLine - entryPrice );
				double lossInTrade = shares * lossInPonts;
				double lossInPct  = lossInTrade / cashAvailiable;
				string traderLossStats = "\t$" + lossInTrade.ToString("0") + "\t%" +lossInPct.ToString("0.00") ; //lossInPonts.ToString("0.00") + 
				Draw.Text(this, "loss"+CurrentBar, traderLossStats, 0, stopLine - (TickSize* 40), Brushes.Crimson);
			}
		}
		
		/// Entry #2
		protected void setSecondEntry(int stopPct) {
			if (Position.MarketPosition == MarketPosition.Long && entryConditions() && SecondSignal && !entryTwo && Close[0] > stopLine) {
				EnterLong(Convert.ToInt32(shares), "LE 2");
				entryBar2 = 1;
				entryTwo = true;
			}
		}
		
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 												ENTER SHORT
		/// 
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		protected bool entryConditionsShort()
		{
			bool signal = false;
			double onePercent = Close[0] * 0.01;
			if ((Close[0] < Math.Abs(sma0[0])  && Low[0] > SMA(10)[0] && Close[0] > ( SMA(10)[0]+ ATR(14)[0])) ||
				(Close[0] < Math.Abs(sma0[0])  && Low[0] > SMA(10)[0] && Close[0] > ( SMA(10)[0]+ onePercent )) ){
				signal = true;
				BarBrush = Brushes.Magenta;
				CandleOutlineBrush = Brushes.Magenta;
			}
			return signal;
		}
		
		protected void setShortEntry(int stopPct, bool marketCond) {
			/// Entry #1
			if ( Position.MarketPosition == MarketPosition.Flat && entryConditionsShort() && setMarketConditionFilterShort( isOn: marketCond ) ) {
				shares = calcPositionSize();
				EnterShort(Convert.ToInt32(shares), "ORs 1");
				entryPrice = Close[0];
				entryBar = 1;
				entryOne = true;
				string entryData = "------->  SE on " +Instrument.MasterInstrument.Name + " shares " + shares + "  $" + cashAvailiable;
				Print(entryData);
				
				string entryTxt = "ORs 1\n" + shares+" shares" + "\n$"+cashAvailiable.ToString("0");
				
				calcInitialStopShort(pct: stopPct);
				// calc hard stop loss in $$$
				/// entryPrice - stopline * shares
				// show loss at stop line
				lossInPonts =  ( stopLine - entryPrice );
				double lossInTrade = shares * lossInPonts;
				double lossInPct  = lossInTrade / cashAvailiable;
				string traderLossStats = "\t$" + lossInTrade.ToString("0") + "\t%" +lossInPct.ToString("0.00"); //lossInPonts.ToString("0.00") +
				Draw.Text(this, "loss"+CurrentBar, traderLossStats, 0, stopLine + (TickSize* 40), Brushes.Crimson);
			}
		}
		
		/// Entry #2 Short
		protected void setSecondEntryShort(int stopPct) {
			if (Position.MarketPosition == MarketPosition.Short && entryConditionsShort() && SecondSignal && !entryTwoShort && Close[0] < stopLine) {
				EnterShort(Convert.ToInt32(shares), "SE 2");
				entryBar2short = 1;
				entryTwoShort = true;
			}
		}
		
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 												STOP LONG
		/// 
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		protected void calcInitialStop(int pct) {
			double convertedPct = 0.03;
			if ( !autoStop ) {
				convertedPct = pct * 0.01;
			}
			/// Auto Set Position stop to 3% US SPY DIA MDY IWM, 5% QQQ EFA ILF EEM EPP IEV
			if( autoStop ) {
				if ( Instrument.MasterInstrument.Name == "SPY" ||
						Instrument.MasterInstrument.Name == "DIA" ||
						Instrument.MasterInstrument.Name == "MDY" ||
						Instrument.MasterInstrument.Name == "IWM" 
					) {
						Print("Stop is 3% for US Indexes");
						convertedPct = 3 * 0.01;	// 3% for US Indexes
				} else {
					convertedPct = 5 * 0.01;		// 5% for forign indexes
					Print("Stop is 5% for forign indexes");
				}
			}
			stopLine =  entryPrice - ( entryPrice * convertedPct);
			trailStop = stopLine;
		}
		
		///  initial stop
		protected void setInitialStop(int pct, bool auto) {
			/// show entry + stop line
			if ( Position.MarketPosition == MarketPosition.Long ){
				Draw.Text(this, "EL"+CurrentBar, "-", 0, entryPrice, Brushes.LimeGreen);
				Draw.Text(this, "sL"+CurrentBar, "-", 0, stopLine, Brushes.Crimson);
				ExitLongStopMarket(stopLine, "stop", "");
			}
		}
		
		protected void exitAfterNBars(int bars, bool active)
		{
			if ( !active ) { return; }
			if ( Position.MarketPosition == MarketPosition.Long ) {
				/// Stop after n Bars
				Draw.Text(this, "TIME1c"+CurrentBar, entryBar.ToString(), 0, High[0], Brushes.White);
				if (entryBar > bars) {
					ExitLong("Time1", "LE 1");
					resetEntry();
					tradeUpdate(tradeType: " LX_Time on "+Instrument.MasterInstrument.Name);
					Draw.Text(this, "TIME"+CurrentBar, "T1", 0, High[0]+ (TickSize * 60), Brushes.Red);
					
				}
				entryBar ++;
				
				if (entryTwo) {
					
					Draw.Text(this, "TIME2a"+CurrentBar, entryBar2.ToString(), 0, Low[0], Brushes.Red);
					if (entryBar2 > bars ) {
						Draw.Text(this, "TIME2c"+CurrentBar, "T2", 0, Low[0]- (TickSize * 60), Brushes.Red);
						ExitLong("Time2", "LE 2");
						tradeUpdate(tradeType: " LX_Time2 on "+Instrument.MasterInstrument.Name);
						/// be careful with this var - its the only place its reset
						entryTwo = false;
						entryBar2 = 0;
					}
					entryBar2++;
				}
			}
			/// fucked up way to reset entry 2
			if ( Position.MarketPosition == MarketPosition.Flat ) {
				entryBar2 = 0;
	 			entryTwo = false;
			}
		}
		
		/// trail stopadjusted for close > close[1]
		protected void setTrailOnClose(bool isOn) {
			if ( !isOn ) { return; }
			if ( Position.MarketPosition == MarketPosition.Long && BarsSinceEntryExecution() >= 1 ){
				if (Close[0] > Close[1] ) {
					double newTrailStop = Close[0]  + lossInPonts;
					/// stop only moves up
					if (newTrailStop > trailStop ) {
						trailStop = newTrailStop;
					}
					
				}
				Draw.Text(this, "trail"+CurrentBar, "*", 0, trailStop, Brushes.Crimson);
			}
			exitOnTrailStop(isOn: isOn);
		}
		/// trail stop
		protected void exitOnTrailStop(bool isOn) {
			if ( !isOn ) { return; }
			if ( Position.MarketPosition == MarketPosition.Long && Close[0] < trailStop ) {
				ExitLong(Convert.ToInt32(shares));
				resetEntry();
				tradeUpdate(tradeType: " LX_Trail on "+Instrument.MasterInstrument.Name);
				Draw.Text(this, "LX_Trail"+CurrentBar, "LX_Trail", 0, Low[0]- (TickSize * 40), Brushes.Crimson);
			}
		}
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 												TARGET LONG
		/// 
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// RSI Target does better
		protected void setLongTarget() {
			/// %R Target
			if ( Position.MarketPosition == MarketPosition.Long && Close[0] > SMA(10)[0] ) {
				ExitLong("LX SMA", "");
				resetEntry();
				tradeUpdate(tradeType: " LX_Target on "+Instrument.MasterInstrument.Name);
			}
		}
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 												STOP SHORT
		/// 
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		protected void calcInitialStopShort(int pct) {
			double convertedPct = 0.03;
			if ( !autoStop ) {
				convertedPct = pct * 0.01;
			}
			/// Auto Set Position stop to 3% US SPY DIA MDY IWM, 5% QQQ EFA ILF EEM EPP IEV
			if( autoStop ) {
				if ( Instrument.MasterInstrument.Name == "SPY" ||
						Instrument.MasterInstrument.Name == "DIA" ||
						Instrument.MasterInstrument.Name == "MDY" ||
						Instrument.MasterInstrument.Name == "IWM" 
					) {
						Print("Stop is 3% for US Indexes");
						convertedPct = 3 * 0.01;	// 3% for US Indexes
				} else {
					convertedPct = 5 * 0.01;		// 5% for forign indexes
					Print("Stop is 5% for forign indexes");
				}
			}
			stopLine =  entryPrice + ( entryPrice * convertedPct);
			trailStop = stopLine;
		}
		///  initial stop short
		protected void setInitialStopShort(int pct, bool auto) {
			/// show entry + stop line
			if ( Position.MarketPosition == MarketPosition.Short ){
				Draw.Text(this, "ES"+CurrentBar, "-", 0, entryPrice, Brushes.LimeGreen);
				Draw.Text(this, "sS"+CurrentBar, "-", 0, stopLine, Brushes.Crimson);
				/// git this to work but dicey
				/// limit, stop -- reduce limit by 3 cents
				ExitShortStopLimit(stopLine+0.02, stopLine,"stop", "");
			}
		}
		
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 												TARGET SHORT
		/// 
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		protected void setShortTarget() {
			/// %R Target
			if ( Position.MarketPosition == MarketPosition.Short && Close[0] < SMA(10)[0] ) {
				ExitShort("OR SMA", "");
				resetEntry();
				tradeUpdate(tradeType: " SX_Target on "+Instrument.MasterInstrument.Name);
			}
		}

		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 												STATS
		/// 
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/// 
		public void tradeUpdate(string tradeType) {
			//var titleMessage = tradeType;
			var bodyMessage = ComputerName + ", " + tradeType + ", occurred on, " + Time[0].ToString() +", "+Close[0] +",";
			bodyMessage = bodyMessage +"Trade count,  " + SystemPerformance.AllTrades.TradesPerformance.TradesCount.ToString("0")+", ";
			bodyMessage = bodyMessage +"Net profit, " + SystemPerformance.AllTrades.TradesPerformance.NetProfit.ToString("0.0")+", ";
			bodyMessage = bodyMessage +"Profit factor," + SystemPerformance.AllTrades.TradesPerformance.ProfitFactor.ToString("0.00")+", ";
			bodyMessage = bodyMessage +"----->   Detailed Report   <-----,";
			bodyMessage = bodyMessage +"Max # of consecutive losers is," + SystemPerformance.AllTrades.TradesPerformance.MaxConsecutiveLoser+", ";
			bodyMessage = bodyMessage +"Largest loss of all trades is, $," + SystemPerformance.AllTrades.TradesPerformance.Currency.LargestLoser.ToString("0.0")+", ";
			bodyMessage = bodyMessage +"Largest win of all trades is $," + SystemPerformance.AllTrades.TradesPerformance.Currency.LargestWinner.ToString("0.0")+", ";
			bodyMessage = bodyMessage +"Average monthly profit is $," + SystemPerformance.AllTrades.TradesPerformance.Currency.ProfitPerMonth.ToString("0.0");
			
			if (SystemPerformance.AllTrades.WinningTrades.Count != 0) {
					double winPct = (Convert.ToDouble( SystemPerformance.AllTrades.WinningTrades.Count) / Convert.ToDouble(SystemPerformance.AllTrades.TradesPerformance.TradesCount));
					winPct = winPct * 100;
					bodyMessage = bodyMessage + ", " + "Win Percent is, " + winPct.ToString("0.0")+ "%";
			}
			
//			/// cant figure out hoe to call the on trade update method
//			if( SystemPerformance.AllTrades.Count  > 1 ) {
//				Trade lastTrade = SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count -1];
//				string thisTrade = "*****\t"+Time[0].ToShortDateString() + "\t"+ lastTrade.Entry.Price.ToString("0.00") + "\t"+  lastTrade.Exit.Price.ToString("0.00") + "\t"+  lastTrade.ProfitPoints.ToString("0.00");
//				Print(thisTrade);
//			}
			gainInPoints = Close[0] - entryPrice;
			
			riskInLastTrade = lossInPonts;
			
			if ( gainInPoints > 0 ) {
				rValue = (gainInPoints / riskInLastTrade) *-1;
				
				totalR = totalR + rValue;
				
				string rCalc = "    GAIN\t"+gainInPoints.ToString("0.00") +"\tRISK:\t"+lossInPonts.ToString("0.00") +"\tR:\t"+rValue.ToString("0.00")+"\tTotal R:\t"+totalR.ToString("0.00");
				
				bodyMessage = bodyMessage + rCalc;
			}

			/// TODO: save file as stream
			string messageToDisplay = ComputerName+" Trade at " +Time[0].ToString()  + " " + Instrument.MasterInstrument.Name;
			string messageToFile = messageToDisplay + ", " + bodyMessage;
			//appendConnectionFile(message: messageToFile);
			Print(messageToFile);
		}
		
		protected string popuateStatsTextBox() {
			double winPct = 0.00;
			if (SystemPerformance.AllTrades.WinningTrades.Count != 0) {
					winPct = (Convert.ToDouble( SystemPerformance.AllTrades.WinningTrades.Count) / Convert.ToDouble(SystemPerformance.AllTrades.TradesPerformance.TradesCount));
					winPct = winPct * 100;		
			}
	
			double roi = ( priorTradesCumProfit / initialBalance ) * 100;
			
			string bodyMessage = "\n\t";
			bodyMessage = bodyMessage +SystemPerformance.AllTrades.TradesPerformance.TradesCount.ToString("0")+" Trades";
			bodyMessage = bodyMessage +"\tNP $" + SystemPerformance.AllTrades.TradesPerformance.NetProfit.ToString("0")+"\t\n";
			bodyMessage = bodyMessage  + "\t" + winPct.ToString("0.0")+ "%";
			bodyMessage = bodyMessage +"\t" + SystemPerformance.AllTrades.TradesPerformance.ProfitFactor.ToString("0.00")+" PF\t";
			bodyMessage = bodyMessage  + SystemPerformance.AllTrades.TradesPerformance.MaxConsecutiveLoser +" LR";
			double avgTrade = SystemPerformance.AllTrades.TradesPerformance.GrossProfit / SystemPerformance.AllTrades.TradesPerformance.TradesCount;
			bodyMessage = bodyMessage +"\n\tAverage Trade \t$" + avgTrade.ToString("0");
			bodyMessage = bodyMessage +"\n\tOutliers $" + SystemPerformance.AllTrades.TradesPerformance.Currency.LargestLoser.ToString("0");
			bodyMessage = bodyMessage +"\t$" + SystemPerformance.AllTrades.TradesPerformance.Currency.LargestWinner.ToString("0");
			bodyMessage = bodyMessage +"\n\tMonthly\t$" + SystemPerformance.AllTrades.TradesPerformance.Currency.ProfitPerMonth.ToString("0") ;
			bodyMessage = bodyMessage +"\tROI  " + roi.ToString("0.00") + "%"+"\n\tTotal R:\t"+totalR.ToString("0.00")+"\t\n";
			return bodyMessage;
		}
		
		protected void setTextBox(string textInBox)
		{
			/// show market condition
			TextFixed myTF = Draw.TextFixed(this, "tradeStat", textInBox, TextPosition.BottomLeft);
			myTF.TextPosition = TextPosition.BottomLeft;
			myTF.AreaBrush = Brushes.DimGray;
			myTF.AreaOpacity = 90;
			myTF.TextBrush = Brushes.Black;
		}
		
		protected void resetEntry()
		{
			entryBar = 0;
			entryOne = false;
			entryBar2short = 0;
			entryTwoShort = false;
			
		}
		
		#region Properties
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="US Index", Order=1, GroupName="NinjaScriptStrategyParameters")]
		public bool USIndex
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="Second Signal", Order=2, GroupName="NinjaScriptStrategyParameters")]
		public bool SecondSignal
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="Exit After N bars", Order=3, GroupName="NinjaScriptStrategyParameters")]
		public bool ExitAfterNbars
		{ get; set; }
		

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Number Of Systems", Order=4, GroupName="NinjaScriptStrategyParameters")]
		public int NumberOfSystems
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Starting Capital", Order=5, GroupName="NinjaScriptStrategyParameters")]
		public int StartingCapital
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="Use Market Condition Filter", Order=6, GroupName="NinjaScriptStrategyParameters")]
		public bool UseMarketCondition
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="Auto Set Stop From Market", Order=7, GroupName="NinjaScriptStrategyParameters")]
		public bool AutoSetStopFromMarket
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="Use Trail Stop", Order=8, GroupName="NinjaScriptStrategyParameters")]
		public bool UseTrailStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Stop Pct", Order=9, GroupName="NinjaScriptStrategyParameters")]
		public int StopPct
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Exit After Bars", Order=10, GroupName="NinjaScriptStrategyParameters")]
		public int ExitAfterBArs
		{ get; set; }
		
		#endregion

	}
}

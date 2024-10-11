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

#endregion

//This namespace holds Bars types in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.BarsTypes
{
	public class RealRenkoBarsType : BarsType
	{
		// Vendor Licensing
		// public RealRenkoBarsType() {
		// 	VendorLicense("LDQTrading", "RealRenko", "https://ldqtrading.gumroad.com/", "ldqtrading.nt@gmail.com", null);
		// }
		
		private double renkoHigh;
		private double renkoLow;
		private double trendThreshold;
		private double brickSize;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Customizable renko bars with true OHLC values.";
				Name						= "RealRenko";
				BarsPeriod					= new BarsPeriod { BarsPeriodType = (BarsPeriodType) 54321 };
				BuiltFrom					= BarsPeriodType.Tick;
				DaysToLoad					= 3;
				IsIntraday					= true;
				DefaultChartStyle			= Gui.Chart.ChartStyleType.CandleStick; 
				
			}
			else if (State == State.Configure)
			{
				Name = "RealRenko " + BarsPeriod.Value + "/" + BarsPeriod.Value2; // Set display name to include brick size and trend threshold
				
				// Remove properties that are not used
                Properties.Remove(Properties.Find("BaseBarsPeriodType",			true));
				Properties.Remove(Properties.Find("BaseBarsPeriodValue",		true));
				Properties.Remove(Properties.Find("PointAndFigurePriceType",	true));
				Properties.Remove(Properties.Find("ReversalType",				true));

				SetPropertyName("Value", "Brick Size");
				SetPropertyName("Value2", "Trend Threshold");
            }
		}
		
		public override bool IsRemoveLastBarSupported { get { return true; } } // Must be true to use RemoveLastBar();

		public override int GetInitialLookBackDays(BarsPeriod barsPeriod, TradingHours tradingHours, int barsBack)
		{
			return 5;
		}

		public override void ApplyDefaultBasePeriodValue(BarsPeriod period)
		{
			
		}

		public override void ApplyDefaultValue(BarsPeriod period)
		{
			period.BarsPeriodTypeName = "RealRenkoBarsType";
			period.Value = 8; // Brick size
			period.Value2 = 4; // Trend threshold
		}

		public override string ChartLabel(DateTime dateTime)
		{
			return dateTime.ToString("T", Core.Globals.GeneralOptions.CurrentCulture);
		}

		public override double GetPercentComplete(Bars bars, DateTime now)
		{
			return 0;
		}
		
		protected override void OnDataPoint(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isBar, double bid, double ask)
		{
			// Initialize session iterator if it hasn't been initialized
			if (SessionIterator == null)
				SessionIterator = new SessionIterator(bars);

			// Calculate brick size and trend threshold from bar period settings
			brickSize = bars.BarsPeriod.Value * bars.Instrument.MasterInstrument.TickSize;
			trendThreshold = bars.BarsPeriod.Value2 * bars.Instrument.MasterInstrument.TickSize;

			// Get the latest bar values
			double barOpen = bars.GetOpen(bars.Count - 1);
			double barHigh = bars.GetHigh(bars.Count - 1);
			double barLow = bars.GetLow(bars.Count - 1);
			double barClose = bars.GetClose(bars.Count - 1);
			long barVolume = bars.GetVolume(bars.Count - 1);
			DateTime barTime = bars.GetTime(bars.Count - 1);

			// Check if we are in a new session
			bool isNewSession = SessionIterator.IsNewSession(time, isBar);
			if (isNewSession)
			{
				// Move to the next session if we are in a new session
				SessionIterator.GetNextSession(time, isBar);
			}

			// Handle resetting bars at the start of a new trading day or when the bar count is zero
			if (bars.Count == 0 || (bars.IsResetOnNewTradingDay && isNewSession))
			{
				// If there are existing bars, remove the last bar and add a new one
				if (bars.Count > 0)
				{
					RemoveLastBar(bars);
					AddBar(bars, barOpen, barHigh, barLow, barClose, barTime, barVolume);
				}

				// Initialize Renko high and low levels based on the close price and trend threshold
				renkoHigh = close + trendThreshold;
				renkoLow = close - trendThreshold;

				// Check for new session again after updating the bars
				isNewSession = SessionIterator.IsNewSession(time, isBar);
				if (isNewSession)
					SessionIterator.GetNextSession(time, isBar);

				// Add a new Renko bar with the same open, high, low, and close values
				AddBar(bars, close, close, close, close, time, volume);
				bars.LastPrice = close;
				return;
			}

			// Initialize Renko high and low if they haven't been set yet
			if (renkoHigh.ApproxCompare(0.0) == 0 || renkoLow.ApproxCompare(0.0) == 0)
			{
				if (bars.Count == 1)
				{
					// Set initial Renko levels based on the first bar open price
					renkoHigh = barOpen + trendThreshold;
					renkoLow = barOpen - trendThreshold;
				}
				else
				{
					// Set Renko levels based on the close price of the previous bar
					double previousClose = bars.GetClose(bars.Count - 2);
					if (previousClose > bars.GetOpen(bars.Count - 2))
					{
						renkoHigh = previousClose + trendThreshold;
						renkoLow = renkoHigh - brickSize * 2;
					}
					else
					{
						renkoLow = previousClose - trendThreshold;
						renkoHigh = renkoLow + brickSize * 2;
					}
				}
			}

			// Check if the current price is above the Renko high level
			if (close.ApproxCompare(renkoHigh) > 0)
			{
				// Update the last bar with new values before adding a new bar
				if (bars.Count > 1) UpdateBar(bars, renkoHigh, Math.Min(renkoHigh - trendThreshold, low), renkoHigh, time, volume - 1);

				// Update Renko levels for the next bar
				renkoLow = renkoHigh - 2.0 * brickSize + trendThreshold;
				renkoHigh = renkoHigh + trendThreshold;

				// Check if we are in a new session
				isNewSession = SessionIterator.IsNewSession(time, isBar);
				if (isNewSession)
				{
					SessionIterator.GetNextSession(time, isBar);
				}

				// Add empty bars to fill gaps if price jumps significantly
				while (close.ApproxCompare(renkoHigh) > 0)
				{
					AddBar(bars, renkoHigh - trendThreshold, renkoHigh, (bars.GetClose(bars.Count - 1) < bars.GetOpen(bars.Count - 1) ? renkoLow : renkoHigh - trendThreshold), (bars.GetClose(bars.Count - 1) < bars.GetOpen(bars.Count - 1) ? renkoLow : renkoHigh), time, volume);
					renkoLow = renkoHigh - 2.0 * brickSize + trendThreshold;
					renkoHigh = renkoHigh + trendThreshold;
				}

				// Add the final partial bar to account for the current close price
				AddBar(bars, renkoHigh - trendThreshold, Math.Min(renkoHigh, close), renkoHigh - trendThreshold, close, time, volume);
			}
			else
			{
				// Check if the current price is below the Renko low level
				if (close.ApproxCompare(renkoLow) < 0)
				{
					// Update the last bar with new values before adding a new bar
					if (bars.Count > 1) UpdateBar(bars, Math.Max(renkoLow + trendThreshold, high), renkoLow, renkoLow, time, volume - 1);

					// Update Renko levels for the next bar
					renkoHigh = renkoLow + 2.0 * brickSize - trendThreshold;
					renkoLow = renkoLow - trendThreshold;

					// Check if we are in a new session
					isNewSession = SessionIterator.IsNewSession(time, isBar);
					if (isNewSession)
					{
						SessionIterator.GetNextSession(time, isBar);
					}

					// Add empty bars to fill gaps if price drops significantly
					while (close.ApproxCompare(renkoLow) < 0)
					{
						AddBar(bars, renkoLow + trendThreshold, (bars.GetClose(bars.Count - 1) > bars.GetOpen(bars.Count - 1) ? renkoHigh : renkoLow + trendThreshold), renkoLow + trendThreshold, (bars.GetClose(bars.Count - 1) > bars.GetOpen(bars.Count - 1) ? renkoHigh : renkoLow), time, volume);
						renkoHigh = renkoLow + 2.0 * brickSize - trendThreshold;
						renkoLow = renkoLow - trendThreshold;
					}

					// Add the final partial bar to account for the current close price
					AddBar(bars, renkoLow + trendThreshold, renkoLow + trendThreshold, Math.Max(renkoLow, close), close, time, volume);
				}
				else
				{
					// Update the current bar with the latest high, low, and close values
					UpdateBar(bars, high, low, close, time, volume);
				}
			}

			// Set the last price of the bars to the current close price
			bars.LastPrice = close;
		}	
	}
}

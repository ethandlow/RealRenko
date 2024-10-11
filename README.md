# RealRenko
![Screenshot 2024-10-08 121852](https://github.com/user-attachments/assets/f7a696e5-b6aa-452d-8518-76e2209d7653)
## Overview

This project provides a custom implementation of Renko bars for NinjaTrader. Renko bars are a type of chart, developed in Japan, that are built using price movement rather than standard time intervals. The unique construction of Renko bars helps to filter out market noise and provides a clear visualization of trends, making them useful for identifying key support and resistance levels.

The code included in this project leverages NinjaTrader's platform capabilities to build Renko bars using custom calculations. It handles different scenarios such as new trading sessions, price gaps, and automated recalculation of Renko levels.

## Features
- Custom Renko Bar Construction: Utilizes a brick size and trend threshold to determine Renko bar levels dynamically.
- True OHLC: Includes true OHLC levels, rather than artificially filling each bar like other traditional Rekno bars.
- Session Management: Integrates session handling to accurately manage the formation of Renko bars across different trading sessions.
- Price Jump Management: Adds partial bars to account for sudden price movements that exceed the current Renko levels.

## Parameters
- Brick Size: Determines the size of each Renko brick in terms of price movement.
- Trend Threshold: Determines the minimum price movement required to form a new Renko bar, helping to filter out small fluctuations.

## Installation
Copy the `RealRenkoBarsType.cs` file to your `NinjaTrader 8/bin/Custom/BarsTypes` folder.

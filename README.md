# PolygonWPFDemo
Polygon.io WPF connectivity demo

PolygonWPFDemo

Summary
This is a basic demo of advising and receiving JSON trades, quotes, etc. It uses C# events and event handlers.

•	PolygonWPFDemoWindow

	o	Enter/paste your Api key or set it in PGonApiKey in PGApi.cs
	o	Simulation mode is available for pre/post market hours
			Uses Testing\PGonJSONTextTest.txt
			Api key is not required for simulation mode
	o	Display of text is selectable 
			Raw JSON sent by Polygon servers
			Values from deserialzed C# classes
	o	Streaming/simulation can be paused or restarted
	o	Advising symbols
			A default symbol is advised
			Symbols are advised on selection
			Symbols can be typed into the Symbol combo box to add them to advised symbols
	•	Manual entered symbols are not saved when the app is closed
		o	The Escape key will close the window
	




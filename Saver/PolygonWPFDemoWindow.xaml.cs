using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Newtonsoft.Json;
using PolygonApi;
using PolygonApi.Clusters;
using PolygonApi.Data;
using System.Reflection;
using PolygonWPFDemo.Controls;
using WebSocket4Net.Command;

namespace PolygonWPFDemo
{
	/// <summary>
	/// Interaction logic for PolygonWPFDemo.xaml
	/// </summary>
	public partial class PolygonWPFDemoWindow : Window
	{
		#region Variables

		// Simulation
		bool SimulationModeStarted;
		int LineNum = 0;
		List<string> JSONTextLines = new List<string>();
		string PGonJSONTextTestFile = @"Testing\PGonJSONTextTest.txt";
		DispatcherTimer SimulationTimer;

		public bool VerboseInfo;
		private string ApiKey;

		List<string> EquitiesList = new List<string>()
		{ "AMZN","AAPL","BABA","BAC","DIA","FB","MSFT","NFLX","THC","TSLA","X","IWM" };

		List<string> ForexList = new List<string>()
		{ "USD/EUR", "EUR/USD","USD/CNH","GPD/USD","GPD/JPY","NZD/USD","USD/JPY" };

		List<string> CryptoList = new List<string>()
		{ "BTC-USD", "BTC/ETH","ETH/BTC","XRP/BTC","XRP/LTC","BCH/ETH","ETH/XRP" };

		#endregion

		#region Properties

		public PGonApi PGApi
		{
			get { return _PGApi = _PGApi ?? new PGonApi(); }
			set { _PGApi = value; }
		}
		PGonApi _PGApi = null;

		public static Dispatcher DispatcherRef
		{
			get { return _Dispatcher ?? Application.Current.Dispatcher; }
			set { _Dispatcher = value; }
		}
		static Dispatcher _Dispatcher = null;

		public bool IsSimulationMode
		{
			get { return _IsSimulationMode; }
			set
			{
				_IsSimulationMode = value;

				int Offset = _IsSimulationMode ? 2 : 0;
				SetBorder( SimulationBorder, Offset );

				StartButton.Margin = new Thickness( 0, Offset, 0, 0 );
			}
		}
		bool _IsSimulationMode;

		public bool IsJSONTextMode
		{
			get { return _IsJSONTextMode; }
			set { _IsJSONTextMode = value; }
		}
		bool _IsJSONTextMode;

		public bool IsPaused
		{
			get { return _IsPaused; }
			set { _IsPaused = value; }
		}
		bool _IsPaused;

		#endregion

		#region Events

		#endregion

		public PolygonWPFDemoWindow()
		{
			InitializeComponent();
		}

		#region Intialization

		private void InitPolygonWPFDemo()
		{
			PGBase.Dispatcher = DispatcherRef;

			VerboseInfo = true;

			InitEquities( VerboseInfo );
			InitForex( VerboseInfo );
			InitCrypto( VerboseInfo );

			IsPaused = true;

			// YAYA - BA: remove API Key !!!
			ApiKey = "CvLXamG5_CS_jpAD2QP6sz_ba0GfIbeWRpcFWu";
			PGApi.SetApiKey( ApiKey );
			AppendText( "YAYA - BA: remove API Key !!! ===>>> \n" );

			PGApi.InitPGClusters();
			PGApi.InitEvents();
			PGApi.Equities.SetConsolidateLevel1( true );

			InitClearTextPrompt();
		}

		private void InitEquities( bool VerboseInfo = false )
		{
			EquitiesList.Sort();
			Equities.ItemsSource = EquitiesList;
			Equities.Tag = PGApi.Equities.ClusterName;
			PGApi.Equities.VerboseInfo = VerboseInfo;

			// Note: unless 'overridden' by the Channels param during
			// Cluster.SubscribeSymbol( Symbol, List<string> Channels = null )
			// these Channels will be used by default
			List<string> DefaultChannels = new List<string>()
				{
					PGEquityChannels.Trades, PGEquityChannels.Quotes,
					PGEquityChannels.AggMinute, PGEquityChannels.AggSecond
				};
			PGApi.Equities.InitDefaultChannels( DefaultChannels );
		}

		private void InitForex( bool VerboseInfo = false )
		{
			ForexList.Sort();
			Forex.ItemsSource = ForexList;
			Forex.Tag = PGApi.Forex.ClusterName;
			PGApi.Forex.VerboseInfo = VerboseInfo;

			// Note: unless 'overridden' by the Channels param during
			// Cluster.SubscribeSymbol( Pair, List<string> Channels = null )
			// these Channels will be used by default
			List<string> DefaultChannels = new List<string>()
				{   
					PGForexChannels.CurrenciesForex, PGForexChannels.Commodities,
					PGForexChannels.Bonds, PGForexChannels.CFAgg, PGForexChannels.Metals
				};
			PGApi.Forex.InitDefaultChannels( DefaultChannels );
		}

		private void InitCrypto( bool VerboseInfo = false )
		{
			CryptoList.Sort();
			Crypto.ItemsSource = CryptoList;
			Crypto.Tag = PGApi.Crypto.ClusterName;
			PGApi.Crypto.VerboseInfo = VerboseInfo;

			// Note: unless 'overridden' by the Channels param during
			// Cluster.SubscribeSymbol( Pair, List<string> Channels = null )
			// these Channels will be used by default
			List<string> DefaultChannels = new List<string>()
				{   PGCryptoChannels.Trades, PGCryptoChannels.Quotes,
					PGCryptoChannels.Level2Books,
					//PGCryptoChannels.ConsolidatedTape, 
				};
			PGApi.Crypto.InitDefaultChannels( DefaultChannels );
		}

		#endregion


		#region InitEvents/UnInitEvents

		public void InitEvents()
		{
			UnInitEvents();

			// PGApi.OnTextInfoEvent is a single source for TextInfo
			PGApi.OnTextInfoEvent += OnClusterTextInfo;

			PGApi.Equities.OnTradeEvent += OnEquitiesTrade;
			PGApi.Equities.OnQuoteEvent += OnEquitiesQuote;
			PGApi.Equities.OnPGClusterReadyEvent += OnPGClusterReady;
			PGApi.Forex.OnForexQuoteEvent += OnForexQuote;
			PGApi.Forex.OnForexAggEvent += OnForexAgg;
			PGApi.Crypto.OnCryptoTradeEvent += OnCryptoTrade;
			PGApi.Crypto.OnCryptoQuoteEvent += OnCryptoQuote;
			PGApi.Crypto.OnCryptoAggEvent += OnCryptoAgg;
			PGApi.Crypto.OnCryptoLevel2Event += OnCryptoLevel2;
		}

		public void UnInitEvents()
		{
			PGApi.OnTextInfoEvent -= OnClusterTextInfo;

			PGApi.Equities.OnTradeEvent -= OnEquitiesTrade;
			PGApi.Equities.OnQuoteEvent -= OnEquitiesQuote;
			PGApi.Equities.OnPGClusterReadyEvent -= OnPGClusterReady;
			PGApi.Forex.OnForexQuoteEvent -= OnForexQuote;
			PGApi.Forex.OnForexAggEvent -= OnForexAgg;
			PGApi.Crypto.OnCryptoTradeEvent -= OnCryptoTrade;
			PGApi.Crypto.OnCryptoQuoteEvent -= OnCryptoQuote;
			PGApi.Crypto.OnCryptoAggEvent -= OnCryptoAgg;
			PGApi.Crypto.OnCryptoLevel2Event -= OnCryptoLevel2;
		}

		#endregion

		#region Window events

		private void Window_Loaded( object sender, RoutedEventArgs e )
		{
			InitPolygonWPFDemo();
			InitEvents();
		}

		private void Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
		{
			PGApi.TerminateApiInterface();
		}

		protected void OnWindow_KeyDown( object sender, KeyEventArgs e )
		{
			bool AltKey = Keyboard.IsKeyDown( Key.LeftAlt ) || Keyboard.IsKeyDown( Key.RightAlt );
			bool CtrlKey = Keyboard.IsKeyDown( Key.LeftCtrl ) || Keyboard.IsKeyDown( Key.RightCtrl );

			switch ( e.Key )
			{
				case Key.Escape:
					Close();		// Close on Escape
					break;

				default:
					break;
			}
		}

		private void AppTextBox_MouseDoubleClick( object sender, MouseButtonEventArgs e )
		{
			AppTextBox.Text = string.Empty;
		}

		private void JSONText_Checked( object sender, RoutedEventArgs e )
		{
			IsJSONTextMode = true;
		}

		private void ClassValues_Checked( object sender, RoutedEventArgs e )
		{
			IsJSONTextMode = false;
		}

		private void ExitButton_Click( object sender, RoutedEventArgs e )
		{
			Close();
		}

		#endregion

		#region PGApi

		private void OnPGClusterReady( string ClusterName, bool IsReconnect )
		{
			ComboBox CBox = null;
			switch ( ClusterName )
			{
				case PGClusterNames.Equities:
					CBox = Equities;
					break;
				case PGClusterNames.Forex:
					CBox = Forex;
					break;
				case PGClusterNames.Crypto:
					CBox = Crypto;
					break;
				default:
					throw new Exception( string.Format( "Unknown ClusterName: {0}", ClusterName ) );
			}
			SubscribeChannelsForSymbol( CBox, true );
		}

		// PGApi.OnTextInfoEvent is a single source for TextInfo
		private void OnClusterTextInfo( string Text )
		{
			AppendText( Text );
		}

		#endregion

		#region Equities

		private void OnEquitiesTrade( Trade TradeRef )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( TradeRef );
			else
			{
				Text = string.Format( "Trade: Symbol: {0}, " +
										"Price: {1}, Size: {2}, " +
										"Trade ID: {3}, Exchange ID: {4}, " +
										"Time: {5} EST",
										TradeRef.sym,
										TradeRef.p, TradeRef.s,
										TradeRef.i, TradeRef.x,
										PGBase.DateTimeStringFromUnixTimestampMillis( TradeRef.t ) );
			}
			AppendText( Text );
		}

		private void OnEquitiesQuote( Quote QuoteRef )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( QuoteRef );
			else
			{
				Text = string.Format( "Quote: Symbol: {0}, " +
										"Bid: {1}, Bid Size: {2}, " +
										"Ask: {3}, Ask Size: {4}, " +
										"Bid ID: {5}, Ask ID: {6} " +
										"Time: {7} EST",
										QuoteRef.sym,
										QuoteRef.bp, QuoteRef.bs,
										QuoteRef.bp, QuoteRef.bs,
										QuoteRef.bx, QuoteRef.bx,
										PGBase.DateTimeStringFromUnixTimestampMillis( QuoteRef.t ));
			}
			AppendText( Text );
		}

		protected void OnLastTrade( LastTrade LastTradeRef )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( LastTradeRef );
			else
			{
				Text = string.Format( "Trade: Symbol: {0}, " +
										"Price: {1}, Size: {2}, " +
										"Exchange ID: {3}, " +
										"Time: {4} EST",
										LastTradeRef.symbol,
										LastTradeRef.last.price, LastTradeRef.last.size,
										LastTradeRef.last.exchange,
										PGBase.DateTimeStringFromUnixTimestampMillis( LastTradeRef.last.timestamp ) );
			}
			AppendText( Text );
		}

		protected void OnLastQuote( LastQuote LastQuoteRef )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( LastQuoteRef );
			else
			{
				Text = string.Format( "LastQuote: Symbol: {0}, " +
										"Bid: {1}, Bid Size: {2}, " +
										"Ask: {3}, Ask Size: {4}, " +
										"Bid ID: {5}, Ask ID: {6} " +
										"Time: {7} EST",
										LastQuoteRef.symbol,
										LastQuoteRef.last.bidprice, LastQuoteRef.last.bidsize,
										LastQuoteRef.last.askprice, LastQuoteRef.last.asksize,
										LastQuoteRef.last.bidexchange, LastQuoteRef.last.askexchange,
										PGBase.DateTimeStringFromUnixTimestampMillis( LastQuoteRef.last.timestamp ) );
			}
			AppendText( Text );
		}

		#endregion

		#region Forex

		private void OnForexQuote( ForexQuote QuoteRef )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( QuoteRef );
			else
			{
				Text = string.Format( "Quote: Pair: {0}, " +
										"Bid: {1}, Ask: {2}, " +
										"FX Exchange: {3}, " +
										"Time: {4} EST",
										QuoteRef.p,
										QuoteRef.b, QuoteRef.a,
										QuoteRef.x,
										PGBase.DateTimeStringFromUnixTimestampMillis( QuoteRef.t ) );
			}
			AppendText( Text );
		}

		private void OnForexAgg( ForexAggregate ForexAggRef )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( ForexAggRef );
			else
			{
				Text = string.Format( "Quote: Pair: {0}, " +
										"Open: {1}, Close: {2}, " +
										"High: {3}, Low: {4}, " +
										"Volume: {5} EST",
										"Tick Start: {6} EST",
										ForexAggRef.pair,
										ForexAggRef.o, ForexAggRef.c,
										ForexAggRef.h, ForexAggRef.l,
										ForexAggRef.v,
										PGBase.DateTimeStringFromUnixTimestampMillis( ForexAggRef.s ) );
			}
			AppendText( Text );
		}

		#endregion

		#region Crypto

		private void OnCryptoTrade( CryptoTrade TradeRef )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( TradeRef );
			else
			{
				Text = string.Format( "Trade: Pair: {0}, " +
										"Price: {1}, Size: {2}, " +
										"Trade ID: {3}, Exchange ID: {4}, " +
										"Time: {5} EST",
										TradeRef.pair,
										TradeRef.p, TradeRef.s,
										TradeRef.i, TradeRef.xt,
										PGBase.DateTimeStringFromUnixTimestampMillis( TradeRef.t ) );
			}
			AppendText( Text );
		}

		private void OnCryptoQuote( CryptoQuote QuoteRef )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( QuoteRef );
			else
			{
				Text = string.Format( "Trade: Pair: {0}, " +
										"Last Trade: {1}, Last Trade Size: {2}, " +
										"Bid: {3}, Bid Size: {4}, " +
										"Ask: {5}, Ask Size: {6}, " +
										"Exchange ID: {7}, " +
										"Exchange Timestamp: {8} EST",
										QuoteRef.pair,
										QuoteRef.lp, QuoteRef.ls,
										QuoteRef.bp, QuoteRef.bs,
										QuoteRef.ap, QuoteRef.asz,
										QuoteRef.xt,
										PGBase.DateTimeStringFromUnixTimestampMillis( QuoteRef.t ) );
			}
			AppendText( Text );
		}
		private void OnCryptoAgg( CryptoAggregate CryptoAggRef )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( CryptoAggRef );
			else
			{
				Text = string.Format( "Trade: Pair: {0}, " +
										"Open: {1}, Open Exchange: {2}, " +
										"High: {3}, High Exchange: {4}, " +
										"Low: {5}, Low Exchange: {6}, " +
										"Close: {7}, Close Exchange: {8}, " +
										"Volume: {9}, " +
										"Tick Start: {10} EST",
										"Tick End: {11} EST",
										CryptoAggRef.pair,
										CryptoAggRef.o, CryptoAggRef.ox,
										CryptoAggRef.h, CryptoAggRef.hx,
										CryptoAggRef.l, CryptoAggRef.lx,
										CryptoAggRef.cl, CryptoAggRef.cx,
										CryptoAggRef.v,
										PGBase.DateTimeStringFromUnixTimestampMillis( CryptoAggRef.s ),
										PGBase.DateTimeStringFromUnixTimestampMillis( CryptoAggRef.e )
										);
			}
			AppendText( Text );
		}

		private void OnCryptoLevel2( CryptoLevel2 CryptoLevel2Ref )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( CryptoLevel2Ref );
			else
			{
				Text = string.Format( "Trade: Pair: {0}, " +
										"Bid Prices: {1}, Ask Prices: {2}, " +
										"Exchange ID: {3}, " +
										"Time: {4} EST",
										CryptoLevel2Ref.pair,
										CryptoLevel2Ref.b.ToString(), CryptoLevel2Ref.a.ToString(),
										CryptoLevel2Ref.xt,
										PGBase.DateTimeStringFromUnixTimestampMillis( CryptoLevel2Ref.t ) );
			}
			AppendText( Text );
		}

		#endregion

		#region Go/Pause

		private void StartButtonButton_Click( object sender, RoutedEventArgs e )
		{
			if ( !IsSimulationMode )
			{
				// check for an Api Key, prompt if not present
				if ( string.IsNullOrEmpty( PGApi.ApiKey ) )
				{
					AppendText( string.Format( "Please enter/paste an Api Key..." ) );
					return;
				}
			}

			IsPaused = !IsPaused;
			StartButton.Content = IsPaused ? "Start" : "Pause";

			string Text = string.Format( "App is {0}...", !IsPaused ? "Running" : "Paused" );
			AppendText( Text );

			if ( !IsPaused )
			{
				if ( IsSimulationMode && !SimulationModeStarted )
				{
					StartSimulationMode();
				}
				else if ( !IsSimulationMode )
				{
					// connect
					StartCluster( PGApi.Equities );
					StartCluster( PGApi.Forex );
					StartCluster( PGApi.Crypto );
				}
			}
		}

		private void StartCluster( PGClusterBase Cluster )
		{
			if ( !Cluster.PGLogonSucceeded )
			{
				AppendText( string.Format( "Starting...{0}", Cluster.ClusterName ) );
				Cluster.Start();
			}
		}

		private void RestartButton_Click( object sender, RoutedEventArgs e )
		{
			if ( IsSimulationMode )
			{
				AppendText( "Restarting Simulation..." );

				EndSimulationMode();
				StartSimulationMode();
			}
			else
			{
				AppendText( "Restarting...\n" );

				PGApi.Equities.UnSubscribeAll();

				PGApi.Equities.IsStarted = false;
				PGApi.Equities.Start();
			}
		}

		private void Simulation_Click( object sender, RoutedEventArgs e )
		{
			IsSimulationMode = Simulation.IsChecked.Value;
			if ( SimulationModeStarted )
				EndSimulationMode();
		}

		#endregion

		#region ApiKey

		EnterApiKeyControl ApiKeyControlRef;

		private void ApiKeyImage_MouseLeftButtonUp( object sender, MouseButtonEventArgs e )
		{
			if ( ApiKeyControlRef == null )
			{
				ApiKeyControlRef = new EnterApiKeyControl( ApiKey );
				ApiKeyControlRef.OnApiKeyEvent += OnApiKeyEntered;
				ApiKeyControlRef.OnOKEvent += OnApiKeyOK;
				ApiKeyControlRef.OnCancelEvent += OnCloseEnterApiKey;

				Grid.SetRow( ApiKeyControlRef, 0 );
				Grid.SetRowSpan( ApiKeyControlRef, 3 );
				Grid.SetColumn( ApiKeyControlRef, 0 );
				Grid.SetColumnSpan( ApiKeyControlRef, 19 );
				LayoutRoot.Children.Add( ApiKeyControlRef );
			}
		}

		private void OnApiKeyOK()
		{
			string ApiKey = ApiKeyControlRef.ApiKeyText.Text;
			OnApiKeyEntered( ApiKey );

			OnCloseEnterApiKey();
		}

		private void OnCloseEnterApiKey()
		{
			if ( ApiKeyControlRef != null )
			{
				LayoutRoot.Children.Remove( ApiKeyControlRef );
				ApiKeyControlRef = null;
			}
		}

		private void OnApiKeyEntered( string ApiKey )
		{
			if ( !string.IsNullOrEmpty( ApiKey ) )
			{
				ApiKeyImage.ToolTip = ApiKey;
				PGApi.SetApiKey( ApiKey );
			}
		}

		private void OnApiKeyPaste( object sender, DataObjectPastingEventArgs e )
		{
			var IsText = e.SourceDataObject.GetDataPresent( DataFormats.UnicodeText, true );
			if ( !IsText )
				return;

			string ApiKey = e.SourceDataObject.GetData( DataFormats.UnicodeText ) as string;
			PGApi.SetApiKey( ApiKey );

			e.Handled = true;
		}

		#endregion

		#region UI Helpers

		private void OnClusterComboBoxEnterKeyDown( object sender, KeyEventArgs e )
		{
			ComboBox CBox = ( sender as ComboBox );
			string ClusterName = CBox.Tag as string;
			string Symbol = CBox.Text;
			List<string> SymbolsList = CBox.ItemsSource as List<string>;

			// if the Symbol is already there, UnSubscribe on Enter
			if ( SymbolsList.Contains( Symbol ) )
			{
				SubscribeChannelsForSymbol( CBox, false );
			}
			else
			{
				SymbolsList.Add( Symbol );
				SymbolsList.Sort();
				SubscribeChannelsForSymbol( CBox, true );
			}
			e.Handled = true;
		}

		private void OnClusterComboBoxSelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			// allow UI to populate ComboBox text
			DispatcherRef.BeginInvoke( new Action( () =>
			{
				SubscribeChannelsForSymbol( ( sender as ComboBox ), true );
			} ) );
		}

		#endregion

		#region SubscribeChannels

		private void SubscribeChannelsForSymbol( ComboBox CBox, bool Subscribe )
		{
			// Note: it is benign to Subscribe and already Subscribed symbol
			string Symbol = CBox.Text;
			if ( string.IsNullOrEmpty( Symbol ) )
				return;

			string ClusterName = CBox.Tag as string;

			PGClusterBase Cluster = null;
			switch ( ClusterName )
			{
				case PGClusterNames.Equities:
					Cluster = PGApi.Equities;
					break;
				case PGClusterNames.Forex:
					Cluster = PGApi.Forex;
					break;
				case PGClusterNames.Crypto:
					Cluster = PGApi.Crypto;
					break;
				default:
					throw new Exception( string.Format( "Unknown ClusterName: {0}", ClusterName ) );
			}

			if ( !Cluster.PGLogonSucceeded )
			{
				AppendText( string.Format( "{0} has not been started, use the Start button", ClusterName ) );
				return;
			}

			if ( Subscribe )
				Cluster.SubscribeSymbol( Symbol );
			else
				Cluster.UnSubscribeSymbol( Symbol );
		}

		#endregion

		#region Simulation

		private void StartSimulationMode()
		{
			IsPaused = false;
			IsSimulationMode = true;
			SimulationModeStarted = true;

			StartButton.Content = "Pause";

			string AppBasePath = System.AppDomain.CurrentDomain.BaseDirectory;
			string FileName = string.Format( @"{0}\{1}", AppBasePath, PGonJSONTextTestFile );
			JSONTextLines = File.ReadAllLines( FileName ).ToList();

			LineNum = 0;

			NextJSONTextLine();

			SimulationTimer = new DispatcherTimer();
			SimulationTimer.Tick -= OnNextJSONTextLine;
			SimulationTimer.Tick += OnNextJSONTextLine;
			SimulationTimer.Interval = new TimeSpan( 0, 0, 0, 0, 1000 );
			SimulationTimer.Start();
		}

		private void EndSimulationMode()
		{
			if ( SimulationTimer != null )
			{
				SimulationTimer.Tick -= OnNextJSONTextLine;
				SimulationTimer.Stop();
			}

			AppTextBox.AppendText( string.Format( "\nSimulation ended at {0} lines of {1}\n", LineNum, JSONTextLines.Count ) );
			StartButton.Content = "Go";

			SimulationModeStarted = false;
			IsSimulationMode = false;
		}

		private void OnNextJSONTextLine( object sender, EventArgs e )
		{
			NextJSONTextLine();
		}

		private void NextJSONTextLine()
		{
			if ( IsPaused )
				return;

			// emit one line
			if ( LineNum < JSONTextLines.Count )
			{
				string JSONText = JSONTextLines[LineNum++];
				AppendText( JSONText );
			}
			else
				EndSimulationMode();
		}

		#endregion

		#region Miscellaneous

		private void SymbolDataTextInfo( string Type, SymbolDataRec SymbolData )
		{
			string Text = string.Format( "{0}: Symbol: {1}, Price: {2}, Size: {3}, " +
									"Bid: {4}, Ask: {5}, " +
									"BidSize: {6}, AskSize: {7} " +
									"Time: {8} EST",
									Type, SymbolData.Symbol,
									SymbolData.LastPrice, SymbolData.LastSize,
									SymbolData.Bid, SymbolData.Ask,
									SymbolData.BidSize, SymbolData.AskSize,
									SymbolData.TimeStamp );
			AppendText( Text );
		}

		public void AppendText( string Text )
		{
			if ( IsSimulationMode )
			{
				if ( LineNum is 0 )
					AppTextBox.AppendText( string.Format( "Simulation started...\n" ) );
				else
					AppTextBox.AppendText( string.Format( "Line number: {0} of {1}:\n", LineNum, JSONTextLines.Count ) );
			}

			Text = string.Format( "{0}\n", Text );
			AppTextBox.AppendText( Text );

			AppTextBox.ScrollToEnd();
		}

		private void SetBorder( Border BorderRef, int Thickness )
		{
			BorderRef.BorderThickness = new Thickness( Thickness );
		}

		private void InitClearTextPrompt()
		{
			PopupText.Text = "DoubleClick on text display to clear text";
			MessagePopup.IsOpen = true;
			Storyboard PopupFader = (Storyboard)Resources["PopupFader"];

			var Animation = (DoubleAnimation)PopupFader.Children[0];
			Animation.Duration = new Duration( new TimeSpan( 0, 0, 0, 2 ) );

			PopupFader.Begin( MessagePopup );
		}

		private void PopupFader_Completed( object sender, EventArgs e )
		{
			MessagePopup.IsOpen = false;
		}

		#endregion

		private void OnDisplaySubscriptions( object sender, RoutedEventArgs e )
		{
			DisplayInfoGrid( true );

			//foreach ( var Cluster in PGApi.PGClusters )
			//{
			//	Cluster.UnSubscribeAll();
			//}
		}

		private void CloseInfoButton_Click( object sender, RoutedEventArgs e )
		{
			DisplayInfoGrid( false );
		}
		private void DisplayInfoGrid( bool Display )
		{
			InfoGrid.Visibility = Display ? Visibility.Visible : Visibility.Collapsed;
			AppTextBox.Height = Display ? AppTextBox.Height - InfoGrid.Height : AppTextBox.Height + InfoGrid.Height;
		}

	}

}

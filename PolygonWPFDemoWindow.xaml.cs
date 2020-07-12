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
using PolygonWPFDemo.Helpers;
using System.Diagnostics;

namespace PolygonWPFDemo
{
	public delegate void OnRTDataRowMouseDoubleClickDel( string ClusterName, string Symbol );
	
	public partial class PolygonWPFDemoWindow : Window
	{
		#region Variables

		string InitialMessage =
		"\n *** =>>> HOW TO USE YOUR Polygon.io API KEY ***" + "\n\n" +
		@"  1. You can enter your ApiKey in the json\PGDemoConfig.json file" + "\n" +
		"\te.g." + @" ""appValues"": { ""ApiKey"": ""mypolygonioapikey"" }" + "\n\n" +
		"  2. You can use the UI to enter/paste your ApiKey" + "\n" +
		"\tClick on the 'ApiKey label' or key image (upper right)," + "\n" +
		"\tPaste or enter the key, then hit Enter to start the app." + "\n\n" +
		"  3. From your custom app: call PGonApi.SetApiKey( ApiKey ); to set the ApiKey." + "\n\n" +
		"  4. You can also hard-code your ApiKey\n" +
		"\t(e.g.) ApiKey = " + @"""mypolygonioapikey"";" + "\n" +
		"\tHard-coding your ApiKey will auto-start this app using the hard-coded ApiKey.\n\n" +
		"See the " + @"Docs\PolygonWPFDemo.docx file for more information." + "\n";

		private string ApiKey = "";
		bool InitialMessageDisplayed;

		AppValues appValues;

		// Note: values are read from json\PGDemoConfig.json
		bool VerboseInfo;
		bool CloseWindowOnEscape;

		// Simulation -> for use when the market is closed
		// ( Equities only, but modify test file as needed )
		string PGonJSONTextTestFile = @"Testing\PGonJSONTextTest.txt";
		bool SimulationModeStarted;
		int LineNum = 0;
		List<string> JSONTextLines = new List<string>();
		TimerHelper SimulationTimer;

		double RTDataGridHeight;

		#endregion

		#region Properties

		public PGonApi PGApi
		{
			get { return _PGApi = _PGApi ?? new PGonApi(); }
			set { _PGApi = value; }
		}
		PGonApi _PGApi = null;

		public ConfigHelper configHelper
		{
			get { return _ConfigHelper = _ConfigHelper ?? new ConfigHelper(); }
			set { _ConfigHelper = value; }
		}
		ConfigHelper _ConfigHelper = null;

		public static Dispatcher dispatcher
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

			InitConfig();

			if ( string.IsNullOrEmpty( ApiKey ) )
			{
				AppTextBox.Text = InitialMessage;
				InitialMessageDisplayed = true;
			}

		}

		private void InitConfig()
		{
			try
			{
				// load app's json config file
				configHelper.LoadConfig();

				// Note: data is read from json\PGDemoConfig.json
				appValues = ConfigHelper.Instance.appConfig.appValues;

				// check for hard-coded ApiKey
				if ( string.IsNullOrEmpty( ApiKey ) )
					ApiKey = appValues.ApiKey;
				// check for ApiKey in config file
				if ( !string.IsNullOrEmpty( ApiKey ) )
					PGApi.SetApiKey( ApiKey );

				// VerboseInfo will provide more text info
				VerboseInfo = appValues.VerboseInfo;
				CloseWindowOnEscape = appValues.CloseWindowOnEscape;
			}
			catch ( System.Exception ex )
			{
				string ErrorMessage = $"InitConfig() error: {ex.Message}";
				Debug.WriteLine( ErrorMessage );
			}
		}

		#region Intialization

		private void InitPolygonWPFDemo()
		{
			PGBase.Dispatcher = dispatcher;

			// capture initial RTDataGridHeight
			RTDataGridHeight = RTDataControlsGrid.Height;
			DisplayRTDataGrid( false, false );

			// local initialization
			PGApi.SetVerboseInfo( VerboseInfo );

			InitEquitiesUI();
			InitForexUI();
			InitCryptoUI();

			// PGApi initialization
			PGApi.InitPGClusters();
			PGApi.InitEvents();

			InitPGClusterEvents();

			// Note: optional, used for consolidated Trade/Quote
			// unused in this app
			PGApi.Equities.SetConsolidateLevel1( true );

			InitClearTextPrompt();
			InitEvents();

			// realtime data initialization
			InitClustersRTData();

			if ( !string.IsNullOrEmpty( ApiKey ) )
				OnApiKeyEntered( ApiKey );
		}

		private void InitPGClusterEvents()
		{
			UnInitPGClusterEvents();

			foreach ( var Cluster in PGApi.PGClusters )
			{
				Cluster.OnSubscribeEvent += RTDataHelper.Instance.OnSubscribe;
				Cluster.OnUnSubscribeEvent += RTDataHelper.Instance.OnUnSubscribe;
			}
		}

		private void UnInitPGClusterEvents()
		{
			foreach ( var Cluster in PGApi.PGClusters )
			{
				Cluster.OnSubscribeEvent -= RTDataHelper.Instance.OnSubscribe;
				Cluster.OnUnSubscribeEvent -= RTDataHelper.Instance.OnUnSubscribe;
			}
		}

		private void InitEquitiesUI()
		{
			configHelper.EquitiesSymbols.Sort();
			EquitiesCBox.ItemsSource = configHelper.EquitiesSymbols;
			EquitiesCBox.Tag = PGApi.Equities.ClusterName;
			ApplyComboBoxCharacterCasing( EquitiesCBox );
		}

		private void InitForexUI()
		{
			configHelper.ForexSymbols.Sort();
			ForexCBox.ItemsSource = configHelper.ForexSymbols;
			ForexCBox.Tag = PGApi.Forex.ClusterName;
			ApplyComboBoxCharacterCasing( ForexCBox );
		}

		private void InitCryptoUI()
		{
			configHelper.CryptoSymbols.Sort();
			CryptoCBox.ItemsSource = configHelper.CryptoSymbols;
			CryptoCBox.Tag = PGApi.Crypto.ClusterName;
			ApplyComboBoxCharacterCasing( CryptoCBox );
		}

		#endregion


		#region InitEvents/UnInitEvents

		public void InitEvents()
		{
			UnInitEvents();

			RTDataHelper.Instance.OnSubscribedSymbolEvent += OnSubscribedSymbol;
			RTDataHelper.Instance.OnUnSubscribedSymbolEvent += OnUnSubscribedSymbol;

			EquitiesRTData.OnRTDataRowMouseDoubleClickEvent += OnRTDataRowMouseDoubleClick;
			ForexRTData.OnRTDataRowMouseDoubleClickEvent += OnRTDataRowMouseDoubleClick;
			CryptoRTData.OnRTDataRowMouseDoubleClickEvent += OnRTDataRowMouseDoubleClick;

			// Note: PGApi.OnTextInfoEvent is a single source for TextInfo
			PGApi.OnTextInfoEvent += OnClusterTextInfo;

			PGApi.Equities.OnTradeEvent += OnEquitiesTrade;
			PGApi.Equities.OnQuoteEvent += OnEquitiesQuote;

			PGApi.Equities.OnDailyOpenCloseEvent += RTDataHelper.Instance.OnDailyOpenClose;
			PGApi.Equities.OnPreviousCloseEvent += RTDataHelper.Instance.OnPreviousClose;
			PGApi.Equities.OnTradeEvent += RTDataHelper.Instance.OnEquitiesTrade;
			PGApi.Equities.OnQuoteEvent += RTDataHelper.Instance.OnEquitiesQuote;
			PGApi.Equities.OnPGClusterReadyEvent += OnPGClusterReady;

			PGApi.Forex.OnForexQuoteEvent += OnForexQuote;
			PGApi.Forex.OnForexAggEvent += OnForexAgg;

			PGApi.Forex.OnDailyOpenCloseEvent += RTDataHelper.Instance.OnDailyOpenClose;
			PGApi.Forex.OnPreviousCloseEvent += RTDataHelper.Instance.OnPreviousClose;
			PGApi.Forex.OnForexQuoteEvent += RTDataHelper.Instance.OnForexQuote;
			PGApi.Forex.OnForexAggEvent += RTDataHelper.Instance.OnForexAgg;
			PGApi.Forex.OnPGClusterReadyEvent += OnPGClusterReady;

			PGApi.Crypto.OnCryptoTradeEvent += OnCryptoTrade;
			PGApi.Crypto.OnCryptoQuoteEvent += OnCryptoQuote;
			PGApi.Crypto.OnCryptoAggEvent += OnCryptoAgg;
			PGApi.Crypto.OnCryptoLevel2Event += OnCryptoLevel2;

			PGApi.Crypto.OnDailyOpenCloseEvent += RTDataHelper.Instance.OnDailyOpenClose;
			PGApi.Crypto.OnPreviousCloseEvent += RTDataHelper.Instance.OnPreviousClose;
			PGApi.Crypto.OnCryptoTradeEvent += RTDataHelper.Instance.OnCryptoTrade;
			PGApi.Crypto.OnCryptoQuoteEvent += RTDataHelper.Instance.OnCryptoQuote;
			PGApi.Crypto.OnCryptoAggEvent += RTDataHelper.Instance.OnCryptoAgg;
			PGApi.Crypto.OnCryptoLevel2Event += RTDataHelper.Instance.OnCryptoLevel2;
			PGApi.Crypto.OnPGClusterReadyEvent += OnPGClusterReady;
		}

		public void UnInitEvents()
		{
			RTDataHelper.Instance.OnSubscribedSymbolEvent -= OnSubscribedSymbol;
			RTDataHelper.Instance.OnUnSubscribedSymbolEvent -= OnUnSubscribedSymbol;

			// Note: PGApi.OnTextInfoEvent is a single source for TextInfo
			PGApi.OnTextInfoEvent -= OnClusterTextInfo;

			PGApi.Equities.OnTradeEvent -= OnEquitiesTrade;
			PGApi.Equities.OnQuoteEvent -= OnEquitiesQuote;

			PGApi.Equities.OnTradeEvent -= RTDataHelper.Instance.OnEquitiesTrade;
			PGApi.Equities.OnQuoteEvent -= RTDataHelper.Instance.OnEquitiesQuote;
			PGApi.Equities.OnPGClusterReadyEvent -= OnPGClusterReady;

			PGApi.Forex.OnForexQuoteEvent -= OnForexQuote;
			PGApi.Forex.OnForexAggEvent -= OnForexAgg;

			PGApi.Forex.OnForexQuoteEvent -= RTDataHelper.Instance.OnForexQuote;
			PGApi.Forex.OnForexAggEvent -= RTDataHelper.Instance.OnForexAgg;
			PGApi.Forex.OnPGClusterReadyEvent -= OnPGClusterReady;

			PGApi.Crypto.OnCryptoTradeEvent -= OnCryptoTrade;
			PGApi.Crypto.OnCryptoQuoteEvent -= OnCryptoQuote;
			PGApi.Crypto.OnCryptoAggEvent -= OnCryptoAgg;
			PGApi.Crypto.OnCryptoLevel2Event -= OnCryptoLevel2;

			PGApi.Crypto.OnCryptoTradeEvent -= RTDataHelper.Instance.OnCryptoTrade;
			PGApi.Crypto.OnCryptoQuoteEvent -= RTDataHelper.Instance.OnCryptoQuote;
			PGApi.Crypto.OnCryptoAggEvent -= RTDataHelper.Instance.OnCryptoAgg;
			PGApi.Crypto.OnCryptoLevel2Event -= RTDataHelper.Instance.OnCryptoLevel2;
			PGApi.Crypto.OnPGClusterReadyEvent -= OnPGClusterReady;
		}

		#endregion

		#region Window events

		private void Window_Loaded( object sender, RoutedEventArgs e )
		{
			InitPolygonWPFDemo();
		}

		private void Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
		{
			PGApi.TerminateApiInterface();
		}

		protected void OnWindow_KeyDown( object sender, KeyEventArgs e )
		{
			switch ( e.Key )
			{
				case Key.Escape:
					if ( CloseWindowOnEscape )
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
			UseJSONText.IsChecked = true;
			UseJSONText.Foreground = new SolidColorBrush( Colors.LightSteelBlue );
			UseClassValues.Foreground = new SolidColorBrush( Colors.White );
			IsJSONTextMode = true;
		}

		private void ClassValues_Checked( object sender, RoutedEventArgs e )
		{
			UseClassValues.IsChecked = true;
			UseClassValues.Foreground = new SolidColorBrush( Colors.LightSteelBlue );
			UseJSONText.Foreground = new SolidColorBrush( Colors.White );
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
					CBox = EquitiesCBox;
					break;
				case PGClusterNames.Forex:
					CBox = ForexCBox;
					break;
				case PGClusterNames.Crypto:
					CBox = CryptoCBox;
					break;
				default:
					throw new Exception( $"Unknown ClusterName: {ClusterName}" );
			}

			if ( !IsReconnect )
				SubscribeChannelsForSymbol( CBox, true );
		}

		private void SubscribeClusterPresetSymbols( string ClusterName )
		{
			AppendText( $"Subscribing to {ClusterName} Preset Symbols...  " );

			// 100 msecs delay to allow for UI updates...
			TimerHelper UITimer = new TimerHelper( SubscribeClusterPresets, Data: ClusterName )
			{ IsOneShot = true };
		}

		private void SubscribeClusterPresets( object Data )
		{
			string ClusterName = Data as string;
			switch ( ClusterName )
			{
				case PGClusterNames.Equities:
					SubscribeClusterSymbolChannels( PGApi.Equities, configHelper.EquitiesPresetSymbols, PGEquities.DefaultEquitiesChannels );
					break;
				case PGClusterNames.Forex:
					SubscribeClusterSymbolChannels( PGApi.Forex, configHelper.ForexPresetSymbols, PGForex.DefaultForexChannels );
					break;
				case PGClusterNames.Crypto:
					SubscribeClusterSymbolChannels( PGApi.Crypto, configHelper.CryptoPresetSymbols, PGCrypto.DefaultCryptoChannels );
					break;
				default:
					throw new Exception( $"Unknown ClusterName: {ClusterName}" );
			}
		}

		private string SubscribeClusterSymbolChannels( PGClusterBase Cluster, List<string> Symbols, List<string> Channels )
		{
			// format Params by adding Channel.Symbol for each Symbol
			string Params = string.Empty;
			foreach ( var Symbol in Symbols )
			{
				Params = string.Empty;
				foreach ( var Channel in Channels )
					Params += $"{Channel}.{Symbol},";

				AppendText( $"Subscribing to {Params}" );
				Cluster.SendSubscribeMessage( Params );
			}
			return Params.TrimEnd(',');
		}

		// Note: PGApi.OnTextInfoEvent is a single source for TextInfo
		private void OnClusterTextInfo( string Text )
		{
			AppendText( Text );
		}

		#endregion

		#region Equities

		private void OnEquitiesTrade( Trade trade )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( trade );
			else
			{
				Text = $"Trade: Symbol: {trade.sym}, " +
										$"Price: {trade.p}, Size: {trade.s}, " +
										$"Trade ID: {trade.i}, Exchange ID: {trade.x}, " +
										$"Time: {PGBase.UnixTimestampMillisToESTDateTime( trade.t )} EST";
			}
			AppendText( Text );
		}

		private void OnEquitiesQuote( Quote quote )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( quote );
			else
			{
				Text = $"Quote: Symbol: {quote.sym}, " +
										$"Bid: {quote.bp}, Bid Size: {quote.bs}, " +
										$"Ask: {quote.ap}, Ask Size: {quote.asz}, " +
										$"Bid ID: {quote.bx}, Ask ID: {quote.ax} " +
										$"Time: {PGBase.UnixTimestampMillisToESTDateTime( quote.t )} EST";
			}
			AppendText( Text );
		}

		protected void OnLastTrade( LastTrade lastTrade )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( lastTrade );
			else
			{
				Text = $"Trade: Symbol: {lastTrade.symbol}, " +
										$"Price: {lastTrade.last.price}, Size: {lastTrade.last.size}, " +
										$"Exchange ID: {lastTrade.last.exchange}, " +
										$"Time: {PGBase.UnixTimestampMillisToESTDateTime( lastTrade.last.timestamp )}";
			}
			AppendText( Text );
		}

		protected void OnLastQuote( LastQuote LastQuote )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( LastQuote );
			else
			{
				Text = $"LastQuote: Symbol: {LastQuote.symbol}, " +
										$"Bid: {LastQuote.last.bidprice}, Bid Size: {LastQuote.last.bidsize}, " +
										$"Ask: {LastQuote.last.askprice}, Ask Size: {LastQuote.last.asksize}, " +
										$"Bid ID: {LastQuote.last.bidexchange}, Ask ID: {LastQuote.last.askexchange} " +
										$"Time: {PGBase.UnixTimestampMillisToESTDateTime( LastQuote.last.timestamp )} EST";
			}
			AppendText( Text );
		}

		#endregion

		#region Forex

		private void OnForexQuote( ForexQuote quote )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( quote );
			else
			{
				Text = $"Quote: Pair: {quote.p}, " +
										$"Bid: {quote.b}, Ask: {quote.a}, " +
										$"FX Exchange: {quote.x}, " +
										$"Time: {PGBase.UnixTimestampMillisToESTDateTime( quote.t )} EST";
			}
			AppendText( Text );
		}

		private void OnForexAgg( ForexAggregate forexAgg )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( forexAgg );
			else
			{
				Text = $"Quote: Pair: {forexAgg.pair}, " +
										$"Open: {forexAgg.o}, Close: {forexAgg.c}, " +
										$"High: {forexAgg.h}, Low: {forexAgg.l}, " +
										$"Volume: {forexAgg.v} EST" +
										$"Tick Start: {PGBase.UnixTimestampMillisToESTDateTime( forexAgg.s )} EST";
			}
			AppendText( Text );
		}

		#endregion

		#region Crypto

		private void OnCryptoTrade( CryptoTrade trade )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( trade );
			else
			{
				Text = $"Trade: Pair: {trade.pair}, " +
										$"Price: {trade.p}, Size: {trade.s}, " +
										$"Trade ID: {trade.i}, Exchange ID: {trade.xt}, " +
										$"Time: {PGBase.UnixTimestampMillisToESTDateTime( trade.t )} EST";
			}
			AppendText( Text );
		}

		private void OnCryptoQuote( CryptoQuote quote )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( quote );
			else
			{
				Text = $"Trade: Pair: {quote.pair}, " +
										$"Last Trade: {quote.lp}, Last Trade Size: {quote.ls}, " +
										$"Bid: {quote.bp}, Bid Size: {quote.bs}, " +
										$"Ask: {quote.ap}, Ask Size: {quote.asz}, " +
										$"Exchange ID: {quote.xt}, " +
										$"Exchange Timestamp: {PGBase.UnixTimestampMillisToESTDateTime( quote.t )} EST";
			}
			AppendText( Text );
		}

		private void OnCryptoAgg( CryptoAggregate cryptoAgg )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( cryptoAgg );
			else
			{
				Text = $"Trade: Pair: {cryptoAgg.pair}, " +
										$"Open: {cryptoAgg.o}, Open Exchange: {cryptoAgg.ox}, " +
										$"High: {cryptoAgg.h}, High Exchange: {cryptoAgg.hx}, " +
										$"Low: {cryptoAgg.l}, Low Exchange: {cryptoAgg.lx}, " +
										$"Close: {cryptoAgg.cl}, Close Exchange: {cryptoAgg.cx}, " +
										$"Volume: {cryptoAgg.v}, " +
										$"Tick Start: {PGBase.UnixTimestampMillisToESTDateTime( cryptoAgg.s )} EST" +
										$"Tick End: {PGBase.UnixTimestampMillisToESTDateTime( cryptoAgg.e )} EST";
			}
			AppendText( Text );
		}

		private void OnCryptoLevel2( CryptoLevel2 cryptoLevel2 )
		{
			string Text;
			if ( IsJSONTextMode )
				Text = JsonConvert.SerializeObject( cryptoLevel2 );
			else
			{
				Text = $"Trade: Pair: {cryptoLevel2.pair}, " +
										$"Bid Prices: {cryptoLevel2.b.ToString()}" +
										$"Ask Prices: {cryptoLevel2.a.ToString()}, " +
										$"Exchange ID: {cryptoLevel2.xt}, " +
										$"Time: {PGBase.UnixTimestampMillisToESTDateTime( cryptoLevel2.t )} EST";
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
				if ( !CheckApiKey() )
					return;
			}

			bool IsNowPaused = !IsPaused;

			string Text = string.Format( "App is {0}...", !IsNowPaused ? "Running" : "Paused" );
			AppendText( Text );

			IsPaused = IsNowPaused;
			StartButton.Content = IsPaused ? "Start" : "Pause";

			if ( !IsPaused )
				StartApp();
		}

		private void StartApp()
		{
			if ( IsSimulationMode && !SimulationModeStarted )
			{
				StartSimulationMode();
			}
			else if ( !IsSimulationMode )
			{
				// start/connect
				foreach ( var Cluster in PGApi.PGClusters )
					StartCluster( Cluster );
			}
		}

		private void StartCluster( PGClusterBase Cluster )
		{
			if ( !Cluster.PGStatus.PGLogonSucceeded )
			{
				AppendText( $"Starting...{Cluster.ClusterName}" );
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
				AppendText( "\nRestarting.Clusters..\n" );

				foreach ( var Cluster in PGApi.PGClusters )
				{
					Cluster.UnSubscribeAll();

					Cluster.IsStarted = false;
					Cluster.Start();
				}
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

		private bool CheckApiKey( bool Report = true )
		{
			bool IsApiKey = !string.IsNullOrEmpty( PGApi.ApiKey );
			if ( !IsApiKey && Report )
				AppendText( $"Please enter/paste an Api Key..." );

			return IsApiKey;
		}

		EnterApiKeyControl ApiKeyControl;

		private void ApiKey_MouseLeftButtonUp( object sender, MouseButtonEventArgs e )
		{
			if ( ApiKeyControl == null )
			{
				ApiKeyControl = new EnterApiKeyControl( ApiKey );
				ApiKeyControl.OnApiKeyEvent += OnApiKeyEntered;
				ApiKeyControl.OnOKEvent += OnApiKeyOK;
				ApiKeyControl.OnCancelEvent += OnCloseEnterApiKey;

				Grid.SetRow( ApiKeyControl, 0 );
				Grid.SetRowSpan( ApiKeyControl, 3 );
				Grid.SetColumn( ApiKeyControl, 0 );
				Grid.SetColumnSpan( ApiKeyControl, 19 );
				LayoutRoot.Children.Add( ApiKeyControl );
			}
		}

		private void OnApiKeyOK()
		{
			string ApiKey = ApiKeyControl.ApiKeyText.Text;
			OnApiKeyEntered( ApiKey );

			OnCloseEnterApiKey();
		}

		private void OnCloseEnterApiKey()
		{
			if ( ApiKeyControl != null )
			{
				LayoutRoot.Children.Remove( ApiKeyControl );
				ApiKeyControl = null;
			}
		}

		private void OnApiKeyEntered( string ApiKey )
		{
			if ( !string.IsNullOrEmpty( ApiKey ) )
			{
				ApiKeyText.Foreground = new SolidColorBrush( (Color)ColorConverter.ConvertFromString( "LightGreen" ));
				PGApi.SetApiKey( ApiKey );
				IsPaused = false;
				StartApp();
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

		private void OnRTDataRowMouseDoubleClick( string ClusterName, string Symbol )
		{
			PGClusterBase Cluster = PGApi.GetCluster( ClusterName );
			bool IsSubscribed = Cluster.IsSymbolSubscribed( Symbol );

			// UnSubscribe on RTDataRow DoubleClick
			if ( IsSubscribed )
				Cluster.UnSubscribeSymbol( Symbol );
		}


		private void OnSubscribeEquitiesPresets( object sender, RoutedEventArgs e )
		{
			SubscribeClusterPresetSymbols( PGClusterNames.Equities );
			e.Handled = true;
		}
		private void OnSubscribeForexPresets( object sender, RoutedEventArgs e )
		{
			SubscribeClusterPresetSymbols( PGClusterNames.Forex );
			e.Handled = true;
		}
		private void OnSubscribeCryptoPresets( object sender, RoutedEventArgs e )
		{
			SubscribeClusterPresetSymbols( PGClusterNames.Crypto );
			e.Handled = true;
		}

		private void ClusterComboBoxMouseDoubleClick( object sender, MouseButtonEventArgs e )
		{
			ComboBox CBox = ( sender as ComboBox );
			string ClusterName = CBox.Tag as string;
			string Symbol = CBox.Text;

			List<string> SymbolsList = CBox.ItemsSource as List<string>;
	
			// remove on DoubleClick
			if ( SymbolsList.Contains( Symbol ) )
				SymbolsList.Remove( Symbol );

			e.Handled = true;
		}

		private void OnClusterComboBoxKeyDown( object sender, KeyEventArgs e )
		{
			if ( e.Key == Key.Enter )
			{
				ComboBox CBox = ( sender as ComboBox );
				string ClusterName = CBox.Tag as string;
				string Symbol = CBox.Text;

				if ( !string.IsNullOrEmpty( Symbol ) )
				{
					List<string> SymbolsList = CBox.ItemsSource as List<string>;

					// is Symbol already there?
					if ( SymbolsList.Contains( Symbol ) )
					{
						PGClusterBase Cluster = PGApi.GetCluster( ClusterName );
						bool IsSubscribed = Cluster.IsSymbolSubscribed( Symbol );

						// Subscribe on Enter
						if ( !IsSubscribed )
							SubscribeChannelsForSymbol( CBox, true );
					}
					else if ( !string.IsNullOrEmpty( Symbol ) )
					{
						// add a new Symbol
						SymbolsList.Add( Symbol );
						SymbolsList.Sort();
						SubscribeChannelsForSymbol( CBox, true );
					}
					e.Handled = true;
				}
			}
		}

		private void OnClusterComboBoxDropDownClosed( object sender, EventArgs e )
		{
			ComboBox CBox = ( sender as ComboBox );
			string ClusterName = CBox.Tag as string;
			string Symbol = CBox.Text;

			if ( !string.IsNullOrEmpty( Symbol ) )
			{
				PGClusterBase Cluster = PGApi.GetCluster( ClusterName );
				bool IsSubscribed = Cluster.IsSymbolSubscribed( Symbol );
				if ( IsSubscribed )
					return;

				SubscribeChannelsForSymbol( ( sender as ComboBox ), true );
			}
		}
		private void OnClusterComboBoxSelectionChanged( object sender, SelectionChangedEventArgs e )
		{
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

			PGClusterBase Cluster = PGApi.GetPGCluster( ClusterName );
			if ( Cluster == null )
				throw new Exception( $"Unknown ClusterName: {ClusterName}" );

			// check for PGApi Cluster ready
			if ( !Cluster.PGStatus.PGLogonSucceeded )
				AppendText( $"{ClusterName} has not been started (use the Start button)" );

			if ( Subscribe )
				Cluster.SubscribeSymbol( Symbol );
			else
				Cluster.UnSubscribeSymbol( Symbol );
		}

		private void RequestLast( RTDataRec rtDataRec )
		{
			AppendText( $"Request Last for {rtDataRec.Symbol}" );

			switch ( rtDataRec.ClusterName )
			{
				case PGClusterNames.Equities:
					LastTrade lastTrade = PGApi.Equities.RequestLastTrade( rtDataRec.Symbol );
					if ( lastTrade != null )
					{
						// check invalid symbol
						if ( lastTrade.status == PGStatusMessages.NotFound )
							HandleInvalidSymbol( rtDataRec );
						else if ( lastTrade != null && lastTrade.last != null )
						{
							rtDataRec.Price = lastTrade.last.price;
							rtDataRec.LastSize = lastTrade.last.size;
						}
					}

					LastQuote LastQuote = PGApi.Equities.RequestLastQuote( rtDataRec.Symbol );
					if ( LastQuote != null )
					{
						if ( LastQuote != null && LastQuote.last != null )
						{
							rtDataRec.Ask = LastQuote.last.askprice;
							rtDataRec.Bid = LastQuote.last.bidprice;
						}
					}
					break;
				case PGClusterNames.Forex:
					ForexLastQuote ForexLastquote = PGApi.Forex.RequestForexLastQuote( rtDataRec.Symbol );
					if ( ForexLastquote != null )
					{
						// check invalid symbol
						if ( ForexLastquote.status == PGStatusMessages.NotFound )
							HandleInvalidSymbol( rtDataRec );
						else if ( ForexLastquote != null && ForexLastquote.last != null )
						{
							rtDataRec.Ask = ForexLastquote.last.ask;
							rtDataRec.Bid = ForexLastquote.last.bid;
						}
					}
					break;
				case PGClusterNames.Crypto:
					CryptoLastTrade CryptoLasttrade = PGApi.Crypto.RequestCryptoLastTrade( rtDataRec.Symbol );
					if ( CryptoLasttrade != null )
					{
						// check invalid symbol
						if ( CryptoLasttrade.status == PGStatusMessages.NotFound )
							HandleInvalidSymbol( rtDataRec );
						else if ( CryptoLasttrade != null && CryptoLasttrade.last != null )
						{
							rtDataRec.Price = CryptoLasttrade.last.price;
							rtDataRec.CryptoSize = CryptoLasttrade.last.size;
						}
					}
					break;
				default:
					throw new Exception( $"Unknown ClusterName: {rtDataRec.ClusterName}" );
			}

			// get Previous values
			PGClusterBase Cluster = PGApi.GetPGCluster( rtDataRec.ClusterName );
			if ( Cluster != null )
			{
				Cluster.RequestPreviousClose( rtDataRec.Symbol );
				Cluster.RequestDailyOpenClose( rtDataRec.Symbol );
			}
		}

		private void HandleInvalidSymbol( RTDataRec rtDataRec )
		{
			AppendText( $"{rtDataRec.Symbol} is an invalid symbol", true );

			PGClusterBase Cluster = PGApi.GetPGCluster( rtDataRec.ClusterName );
			if ( Cluster == null )
				throw new Exception( $"Unknown ClusterName: {rtDataRec.ClusterName}" );

			Cluster.RemoveSymbol( rtDataRec.Symbol );
			RTDataHelper.Instance.RemoveRTData( rtDataRec );
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
			string FileName = $@"{AppBasePath}\{PGonJSONTextTestFile}";
			JSONTextLines = File.ReadAllLines( FileName ).ToList();

			LineNum = 0;

			NextJSONTextLine();

			SimulationTimer = new TimerHelper( OnNextJSONTextLine, 1000 );
		}

		private void EndSimulationMode()
		{
			if ( SimulationTimer != null )
			{
				SimulationTimer.StopTimer();
				SimulationTimer = null;
			}

			AppTextBox.AppendText( $"\nSimulation ended at {LineNum} lines of {JSONTextLines.Count}\n" );
			StartButton.Content = "Go";

			SimulationModeStarted = false;
			IsSimulationMode = false;
		}

		private void OnNextJSONTextLine( object Data )
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

				if ( IsJSONTextMode )
					AppendText( JSONText );
				else
					PGApi.Equities.OnWebSocketJSONText( JSONText );
			}
			else
				EndSimulationMode();
		}

		#endregion

		#region Miscellaneous

		private void SymbolDataTextInfo( string Type, SymbolDataRec SymbolData )
		{
			string Text = $"{Type}: Symbol: {SymbolData.Symbol}, Price: {SymbolData.LastPrice}, Size: {SymbolData.LastSize}, " +
									$"Bid: {SymbolData.Bid}, Ask: {SymbolData.Ask}, " +
									$"BidSize: {SymbolData.BidSize}, AskSize: {SymbolData.AskSize} " +
									$"Time: {SymbolData.TimeStamp} EST";
			AppendText( Text );
		}

		public void AppendText( string Text, bool Force =  false )
		{
			if ( IsPaused && !Force )
				return;
			
			if ( InitialMessageDisplayed )
			{
				InitialMessageDisplayed = false;
				AppTextBox.Text = string.Empty;
			}

			// handoff to Dispatcher at normal priority
			// to allow for UI thread access
			Application.Current.Dispatcher.BeginInvoke( new Action( () =>
			{
				if ( IsSimulationMode )
				{
					if ( LineNum is 0 )
						AppTextBox.AppendText( $"Simulation started...\n" );
					else
						AppTextBox.AppendText( $"Line number: {LineNum} of {JSONTextLines.Count}:\n" );
				}

				Text = $"{Text}\n";
				AppTextBox.AppendText( Text );

				AppTextBox.ScrollToEnd();
			} ) );
		}

		private void SetBorder( Border border, int Thickness )
		{
			border.BorderThickness = new Thickness( Thickness );
		}

		private static void ApplyComboBoxCharacterCasing( ComboBox comboBox, CharacterCasing Casing = CharacterCasing.Upper )
		{
			var TextBox = comboBox.Template.FindName( "PART_EditableTextBox", comboBox ) as TextBox;
			if ( TextBox != null )
				TextBox.CharacterCasing = Casing;
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

		#region Realtime data

		private void OnSubscribedSymbol( RTDataRec rtDataRec )
		{
			switch ( rtDataRec.ClusterName )
			{
				case PGClusterNames.Equities:
					EquitiesRTData.AddSymbolRTDataRow( rtDataRec );
					EquitiesRTData.Visibility = Visibility.Visible;
					break;
				case PGClusterNames.Forex:
					ForexRTData.AddSymbolRTDataRow( rtDataRec );
					ForexRTData.Visibility = Visibility.Visible;
					break;
				case PGClusterNames.Crypto:
					CryptoRTData.AddSymbolRTDataRow( rtDataRec );
					CryptoRTData.Visibility = Visibility.Visible;
					break;
				default:
					throw new Exception( $"Unknown ClusterName: {rtDataRec.ClusterName}" );
			}

			DisplayRTDataGrid( true );

			if ( rtDataRec.RequestLast )
				RequestLast( rtDataRec );
		}

		private void OnUnSubscribedSymbol( RTDataRec rtDataRec )
		{
			switch ( rtDataRec.ClusterName )
			{
				case PGClusterNames.Equities:
					EquitiesRTData.RemoveSymbolRTDataRow( rtDataRec );
					if (EquitiesRTData.RTDataRows.Count == 0 )
						EquitiesRTData.Visibility = Visibility.Collapsed;
					break;
				case PGClusterNames.Forex:
					ForexRTData.RemoveSymbolRTDataRow( rtDataRec );
					if ( ForexRTData.RTDataRows.Count == 0 )
						ForexRTData.Visibility = Visibility.Collapsed;
					break;
				case PGClusterNames.Crypto:
					CryptoRTData.RemoveSymbolRTDataRow( rtDataRec );
					if ( CryptoRTData.RTDataRows.Count == 0 )
						CryptoRTData.Visibility = Visibility.Collapsed;
					break;
				default:
					throw new Exception( $"Unknown ClusterName: {rtDataRec.ClusterName}" );
			}

			int TotalRows = EquitiesRTData.RTDataRows.Count + ForexRTData.RTDataRows.Count + CryptoRTData.RTDataRows.Count;
			if ( TotalRows == 0 )
				DisplayRTDataGrid( false );
		}

		private void InitClustersRTData()
		{
			EquitiesRTData.InitRTClusterData( PGApi.Equities );
			EquitiesRTData.OnRTDataHeightChangeEvent += OnRTDataHeightChange;

			ForexRTData.InitRTClusterData( PGApi.Forex );
			CryptoRTData.InitRTClusterData( PGApi.Crypto );
		}

		private void OnRTDataHeightChange( string ClusterName, double Height )
		{
			RTDataControlsGrid.Height = EquitiesRTData.Height + ForexRTData.Height + CryptoRTData.Height;
		}

		private void ToggleRTDataButton_Click( object sender, RoutedEventArgs e )
		{
			DisplayRTDataGrid( RTDataControlsGrid.Visibility == Visibility.Collapsed );
		}

		private void DisplayRTDataGrid( bool Display, bool AdjustWindowHeight = true )
		{
			Visibility viz = Display ? Visibility.Visible : Visibility.Collapsed;
			if ( RTDataControlsGrid.Visibility != viz )
			{
				RTDataControlsGrid.Visibility = viz;
				LayoutRoot.RowDefinitions[2].Height = new GridLength( Display ? RTDataGridHeight : 0 );

				if ( AdjustWindowHeight )
					this.Height = Display ? Height + RTDataGridHeight : Height - RTDataGridHeight;
			}
		}

		#endregion

		#region ContextMenu handlers

		private void UnSubscribeAll( object sender, RoutedEventArgs e )
		{
			PGApi.UnSubscribeAllChannels();
		}

		private void UnSubscribeAllEquities( object sender, RoutedEventArgs e )
		{
			PGApi.Equities.UnSubscribeAll();
		}

		private void UnSubscribeAllForex( object sender, RoutedEventArgs e )
		{
			PGApi.Forex.UnSubscribeAll();
		}

		private void UnSubscribeAllCrypto( object sender, RoutedEventArgs e )
		{
			PGApi.Crypto.UnSubscribeAll();
		}

		private void OnSaveConfig( object sender, RoutedEventArgs e )
		{
			configHelper.SaveConfig();
		}

		#endregion

	}

}

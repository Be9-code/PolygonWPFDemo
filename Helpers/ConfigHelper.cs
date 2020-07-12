using Newtonsoft.Json;
using PolygonApi.Clusters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonWPFDemo.Helpers
{
	public class ConfigHelper
	{
		#region Variables

		string PGDemoConfigFile = @"json\PGDemoConfig.json";
		public static string AppBasePath = System.AppDomain.CurrentDomain.BaseDirectory;

		public AppConfig appConfig;

		#endregion

		#region Properties

		public static ConfigHelper Instance
		{
			get { return _Instance = _Instance ?? new ConfigHelper(); }
			set { _Instance = value; }
		}
		static ConfigHelper _Instance;

		public List<string> EquitiesSymbols = new List<string>()
		{ "AMZN","AAPL","BABA","BAC","DIA","FB","MSFT","NFLX","THC","TSLA","X","IWM" };

		public List<string> EquitiesPresetSymbols = new List<string>()
		{ "AMZN","AAPL","FB","MSFT","NFLX","TSLA" };

		public List<string> ForexSymbols = new List<string>()
		{ "USD/EUR","EUR/USD","USD/CNH","NZD/USD","USD/JPY" };

		public List<string> ForexPresetSymbols = new List<string>()
		{ "USD/EUR","USD/CNH" };

		public List<string> CryptoSymbols = new List<string>()
		{ "BTC/USD", "BTC/ETH","ETH/BTC","XRP/BTC","XRP/LTC","BCH/ETH","ETH/XRP" };

		public List<string> CryptoPresetSymbols = new List<string>()
		{ "BTC/USD" };

		// in case of new channels being added, changes can be made here -
		// e.g. to ValidEquitiesChannels
		// This will allow 'overriding' of hard-coded defaults in PGClusterBase
		public static List<string> ValidEquitiesChannels = new List<string>()
		{ "T","Q","A","AM" };
		public static List<string> ValidForexChannels = new List<string>()
		{ "C","CA","BONDS","COMMODITIES","METALS" };
		public static List<string> ValidCryptoChannels = new List<string>()
		{ "XT","XQ","XS","XL2" };
		
		#endregion

		#region Events

		#endregion

		public ConfigHelper()
		{
			Instance = this;
		}

		internal void LoadConfig()
		{
			string FileName = $@"{AppBasePath}\{PGDemoConfigFile}";
			if ( !File.Exists( FileName ) )
				SaveConfig( FileName );
			else
				ReadConfig( FileName );
		}

		public void SaveConfig( string FileName = "" )
		{
			if ( string.IsNullOrEmpty( FileName ) )
				FileName = $@"{AppBasePath}\{PGDemoConfigFile}";

			appConfig.ListContainers = new List<ListContainer>()
			{
				new ListContainer( ListTypes.EquitiesSymbols, EquitiesSymbols ),
				new ListContainer( ListTypes.ForexSymbols, ForexSymbols ),
				new ListContainer( ListTypes.CryptoSymbols, CryptoSymbols ),
				new ListContainer( ListTypes.EquitiesPresetSymbols, EquitiesPresetSymbols ),
				new ListContainer( ListTypes.ForexPresetSymbols, ForexPresetSymbols ),
				new ListContainer( ListTypes.CryptoPresetSymbols, CryptoPresetSymbols ),

				// in case of new channels being added, changes can be made here -
				// e.g. to ValidEquitiesChannels in this class.
				// This will allow 'overriding' of hard-coded defaults in PGClusterBase
				new ListContainer( ListTypes.ValidEquitiesChannels, ValidEquitiesChannels ),
				new ListContainer( ListTypes.ValidForexChannels, ValidForexChannels ),
				new ListContainer( ListTypes.ValidCryptoChannels, ValidCryptoChannels ),
			};

			string JSONText = JsonConvert.SerializeObject( appConfig, Formatting.Indented );

			File.WriteAllText( FileName, JSONText );
		}

		public void ReadConfig( string FileName = "" )
		{
			string JSONText = File.ReadAllText( FileName );

			// save the defaults on empty file
			if ( string.IsNullOrEmpty( JSONText ) )
			{
				SaveConfig( FileName );
				return;
			}

			try
			{
				appConfig = JsonConvert.DeserializeObject<AppConfig>( JSONText );

				foreach ( var Container in appConfig.ListContainers )
				{
					switch ( Container.ListType )
					{
						case ListTypes.EquitiesSymbols:
							EquitiesSymbols = Container.ListRef;
							break;
						case ListTypes.ForexSymbols:
							ForexSymbols = Container.ListRef;
							break;
						case ListTypes.CryptoSymbols:
							CryptoSymbols = Container.ListRef;
							break;
						case ListTypes.EquitiesPresetSymbols:
							EquitiesPresetSymbols = Container.ListRef;
							break;
						case ListTypes.ForexPresetSymbols:
							ForexPresetSymbols = Container.ListRef;
							break;
						case ListTypes.CryptoPresetSymbols:
							CryptoPresetSymbols = Container.ListRef;
							break;
						case ListTypes.ValidEquitiesChannels:
							PGClusterBase.ValidEquitiesChannels = Container.ListRef;
							break;
						case ListTypes.ValidForexChannels:
							PGClusterBase.ValidForexChannels = Container.ListRef;
							break;
						case ListTypes.ValidCryptoChannels:
							PGClusterBase.ValidCryptoChannels = Container.ListRef;
							break;

						default:
							break;
					}
				}
			}
			catch ( System.Exception ex )
			{
				string ErrorMessage = $"ReadConfig() error: {ex.Message}";
				Debug.WriteLine( ErrorMessage );
			}
		}
	}

	public class AppValues
	{
		public string ApiKey = string.Empty;
		public bool VerboseInfo = true;
		public bool CloseWindowOnEscape = true;
	}

	public class AppConfig
	{
		public AppValues appValues;
		public List<ListContainer> ListContainers;

		public AppConfig()
		{
			appValues = new AppValues();
		}
	}

	public class ListContainer
	{
		public string ListType;
		public List<string> ListRef;
		public ListContainer( string ListType, List<string> ListRef )
		{
			this.ListType = ListType;
			this.ListRef = ListRef;
		}
	}

	public static class ListTypes
	{
		public const string EquitiesSymbols = "EquitiesSymbols";
		public const string ForexSymbols = "ForexSymbols";
		public const string CryptoSymbols = "CryptoSymbols";
		public const string EquitiesPresetSymbols = "EquitiesPresetSymbols";
		public const string ForexPresetSymbols = "ForexPresetSymbols";
		public const string CryptoPresetSymbols = "CryptoPresetSymbols";
		public const string ValidEquitiesChannels = "ValidEquitiesChannels";
		public const string ValidForexChannels = "ValidForexChannels";
		public const string ValidCryptoChannels = "ValidCryptoChannels";
	}

}

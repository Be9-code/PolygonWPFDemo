using Newtonsoft.Json;
using PolygonApi;
using PolygonApi.Clusters;
using PolygonApi.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonWPFDemo.Helpers
{
	public class RTDataHelper
	{
		#region Variables

		#endregion

		#region Properties

		public static RTDataHelper Instance
		{
			get { return _Instance = _Instance ?? new RTDataHelper(); }
			set { _Instance = value; }
		}
		static RTDataHelper _Instance;

		public Dictionary<string, RTData> RTDataDict
		{
			get { return _RTDataDict = _RTDataDict ?? new Dictionary<string, RTData>(); }
			set { _RTDataDict = value; }
		}
		Dictionary<string, RTData> _RTDataDict;

		#endregion

		#region Events

		#endregion

		public RTDataHelper()
		{
			Instance = this;
		}

		internal void OnSubscribe( string ClusterName, string Params )
		{
		}

		internal void OnUnSubscribe( string ClusterName, string Params )
		{
		}

		internal void OnEvTypeJSONText( string ClusterName, string ev, string ObjJSONText )
		{
			switch ( ClusterName )
			{
				case PGClusterNames.Equities:
					HandleEquitiesJSONText( ev, ObjJSONText );
					break;
				case PGClusterNames.Forex:
					HandleForexJSONText( ev, ObjJSONText );
					break;
				case PGClusterNames.Crypto:
					HandleCryptoJSONText( ev, ObjJSONText );
					break;
				default:
					throw new Exception( string.Format( "Unknown ClusterName: {0}", ClusterName ) );
			}
		}

		private void HandleEquitiesJSONText( string ev, string ObjJSONText )
		{
			switch ( ev )
			{
				case "Q":
					Quote QuoteRef = JsonConvert.DeserializeObject<Quote>( ObjJSONText );
					if ( QuoteRef != null )
					{
					}
					break;

				case "T":
					Trade TradeRef = JsonConvert.DeserializeObject<Trade>( ObjJSONText );
					if ( TradeRef != null )
					{
					}
					break;

				case "status":
					Status StatusRef = JsonConvert.DeserializeObject<Status>( ObjJSONText );
					break;

				case "A":
					ASecond ASecRef = JsonConvert.DeserializeObject<ASecond>( ObjJSONText );
					if ( ASecRef != null )
					{
					}
					break;

				case "AM":
					AMinute AMinRef = JsonConvert.DeserializeObject<AMinute>( ObjJSONText );
					if ( AMinRef != null )
					{
					}
					break;

				default:
					break;
			}
		}

		private void HandleForexJSONText( string ev, string ObjJSONText )
		{
			switch ( ev )
			{
				case "C":
					ForexQuote QuoteRef = JsonConvert.DeserializeObject<ForexQuote>( ObjJSONText );
					if ( QuoteRef != null )
					{
					}
					break;

				case "CA":
					ForexAggregate ForexAggRef = JsonConvert.DeserializeObject<ForexAggregate>( ObjJSONText );
					if ( ForexAggRef != null )
					{
					}
					break;

				case "status":
					Status StatusRef = JsonConvert.DeserializeObject<Status>( ObjJSONText );
					break;

				default:
					break;
			}
		}

		private void HandleCryptoJSONText( string ev, string ObjJSONText )
		{
			switch ( ev )
			{
				case "XQ":
					CryptoQuote QuoteRef = JsonConvert.DeserializeObject<CryptoQuote>( ObjJSONText );
					if ( QuoteRef != null )
					{
					}
					break;

				case "XT":
					CryptoTrade TradeRef = JsonConvert.DeserializeObject<CryptoTrade>( ObjJSONText );
					if ( TradeRef != null )
					{
					}
					break;

				case "XA":
					CryptoAggregate CryptoAggRef = JsonConvert.DeserializeObject<CryptoAggregate>( ObjJSONText );
					if ( CryptoAggRef != null )
					{
					}
					break;

				case "XS":
					CryptoSIP CryptoSIPRef = JsonConvert.DeserializeObject<CryptoSIP>( ObjJSONText );
					if ( CryptoSIPRef != null )
					{
					}
					break;

				case "XL2":
					CryptoLevel2 CryptoLevel2Ref = JsonConvert.DeserializeObject<CryptoLevel2>( ObjJSONText );
					if ( CryptoLevel2Ref != null )
					{
					}
					break;

				case "status":
					Status StatusRef = JsonConvert.DeserializeObject<Status>( ObjJSONText );
					//HandleStatusMessage( StatusRef );
					break;

				default:
					break;
			}
		}
	}

	public class RTData : INotifyBase
	{
		public string Symbol
		{
			get { return _Symbol; }
			set { _Symbol = value; OnPropertyChanged( nameof( Symbol ) ); }
		}
		string _Symbol;
		public double Price
		{
			get { return _Price; }
			set { _Price = value; OnPropertyChanged( nameof( Price ) ); }
		}
		double _Price;
		public double Bid
		{
			get { return _Bid; }
			set { _Bid = value; OnPropertyChanged( nameof( Bid ) ); }
		}
		double _Bid;
		public TickType BidTick
		{
			get { return _BidTick; }
			set { _BidTick = value; OnPropertyChanged( nameof( BidTick ) ); }
		}
		TickType _BidTick;
		public double Ask
		{
			get { return _Ask; }
			set { _Ask = value; OnPropertyChanged( nameof( Ask ) ); }
		}
		double _Ask;
		public TickType AskTick
		{
			get { return _AskTick; }
			set { _AskTick = value; OnPropertyChanged( nameof( AskTick ) ); }
		}
		TickType _AskTick;

		public RTData( string Symbol )
		{
			this.Symbol = Symbol;
		}
	}

	public class INotifyBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		// Create the OnPropertyChanged method to raise the event
		protected void OnPropertyChanged( string propertyName )
		{
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
		}
	}

	public enum TickType
	{
		UpTick,
		FlatTick,
		DownTick
	}

}

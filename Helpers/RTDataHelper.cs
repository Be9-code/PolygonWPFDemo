using Newtonsoft.Json;
using PolygonApi;
using PolygonApi.Clusters;
using PolygonApi.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace PolygonWPFDemo.Helpers
{
	public delegate void OnSubscribedSymbolDel( RTDataRec rtDataRec );

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

		public Dictionary<string, RTDataRec> RTDataDict
		{
			get { return _RTDataDict = _RTDataDict ?? new Dictionary<string, RTDataRec>(); }
			set { _RTDataDict = value; }
		}
		Dictionary<string, RTDataRec> _RTDataDict;

		#endregion

		#region Events

		public event OnSubscribedSymbolDel OnSubscribedSymbolEvent;
		public event OnSubscribedSymbolDel OnUnSubscribedSymbolEvent;

		#endregion

		public RTDataHelper()
		{
			Instance = this;
		}

		internal void OnSubscribe( string ClusterName, Dictionary<string, SubscribeSymbolRec> SubscribedSymbols )
		{
			var SubscribedSymbolsCopy = new Dictionary<string, SubscribeSymbolRec>( SubscribedSymbols );
			foreach ( var kvPair in SubscribedSymbolsCopy )
			{
				string Symbol = kvPair.Key;
				SubscribeSymbolRec SubscribeRec = kvPair.Value;

				if ( SubscribeRec.IsNew )
				{
					RTDataRec Rec = GetRTDataRec( Symbol, ClusterName );
					OnSubscribedSymbolEvent?.Invoke( Rec );
				}
			}
		}

		internal void OnUnSubscribe( string ClusterName, Dictionary<string, SubscribeSymbolRec> SubscribedSymbols )
		{
			var SubscribedSymbolsCopy = new Dictionary<string, SubscribeSymbolRec>( SubscribedSymbols );
			foreach ( var kvPair in SubscribedSymbolsCopy )
			{
				string Symbol = kvPair.Key;
				SubscribeSymbolRec SubscribeRec = kvPair.Value;

				if ( SubscribeRec.ChannelsWereRemoved )
				{
					RTDataRec Rec = GetRTDataRec( Symbol, ClusterName );
					OnUnSubscribedSymbolEvent?.Invoke( Rec );

					RTDataDict.Remove( Symbol );
				}
			}
		}

		#region Equities

		internal void OnEquitiesTrade( Trade trade )
		{
			RTDataRec Rec = GetRTDataRec( trade.sym, PGClusterNames.Equities );
			Rec.PriceTick = trade.p == 0 ? TickType.FlatTick
							: Rec.Price > trade.p ? TickType.DownTick : TickType.UpTick;
			Rec.Price = trade.p;
			Rec.LastSize = trade.s;
			string Change = (Math.Max( Rec.PreviousClose, trade.p ) - Math.Min( Rec.PreviousClose, trade.p )).
							ToString( "F2" );
			Rec.Change = double.Parse( Change );
			Rec.ChangeTick = Rec.Change == 0 ? TickType.FlatTick
							: Rec.Change < 0 ? TickType.DownTick : TickType.UpTick;
		}

		internal void OnEquitiesQuote( Quote quote )
		{
			RTDataRec Rec = GetRTDataRec( quote.sym, PGClusterNames.Equities );
			Rec.AskTick = quote.ap == 0 ? TickType.FlatTick
							: Rec.Ask > quote.ap ? TickType.DownTick : TickType.UpTick;
			Rec.Ask = quote.ap;
			Rec.BidTick = quote.bp == 0 ? TickType.FlatTick
						: Rec.Bid > quote.bp ? TickType.DownTick : TickType.UpTick;
			Rec.Bid = quote.bp;
			Rec.Spread = ( Rec.Ask - Rec.Bid ).ToString( "0.00" );
		}

		#endregion

		#region Forex

		internal void OnForexQuote( ForexQuote quote )
		{
			RTDataRec Rec = GetRTDataRec( quote.p, PGClusterNames.Forex );
			Rec.AskTick = quote.a == 0 ? TickType.FlatTick
						: Rec.Ask > quote.a ? TickType.DownTick : TickType.UpTick;
			Rec.Ask = quote.a;
			Rec.BidTick = quote.b == 0 ? TickType.FlatTick
						: Rec.Bid > quote.b ? TickType.DownTick : TickType.UpTick;
			Rec.Bid = quote.b;
			Rec.Spread = ( Rec.Ask - Rec.Bid ).ToString( "0.00000" );
		}

		internal void OnForexAgg( ForexAggregate forexAgg )
		{
			RTDataRec Rec = GetRTDataRec( forexAgg.pair, PGClusterNames.Crypto );
			Rec.LastSize = forexAgg.v;
		}

		#endregion

		#region Crypto

		internal void OnCryptoTrade( CryptoTrade trade )
		{
			RTDataRec Rec = GetRTDataRec( trade.pair, PGClusterNames.Crypto );
			Rec.PriceTick = trade.p == 0 ? TickType.FlatTick
						: Rec.Price > trade.p ? TickType.DownTick : TickType.UpTick;
			Rec.Price = trade.p;
			Rec.CryptoSize = trade.s;
			string Change = ( Math.Max( Rec.PreviousClose, trade.p ) - Math.Min( Rec.PreviousClose, trade.p ) ).
							ToString( "0:00" );
			Rec.Change = double.Parse( Change );
			Rec.ChangeTick = Rec.Change == 0 ? TickType.FlatTick
							: Rec.Change < 0 ? TickType.DownTick : TickType.UpTick;
		}

		internal void OnCryptoQuote( CryptoQuote quote )
		{
			RTDataRec Rec = GetRTDataRec( quote.pair , PGClusterNames.Crypto );
			Rec.AskTick = quote.ap == 0 ? TickType.FlatTick
						: Rec.Ask > quote.ap ? TickType.DownTick : TickType.UpTick;
			Rec.Ask = quote.ap;
			Rec.BidTick = quote.bp == 0 ? TickType.FlatTick
						: Rec.Bid > quote.bp ? TickType.DownTick : TickType.UpTick;
			Rec.Bid = quote.bp;
			Rec.Spread = ( Rec.Ask - Rec.Bid ).ToString( "0.00000" );
		}

		internal void OnCryptoAgg( CryptoAggregate cryptoAgg )
		{
		}

		internal void OnCryptoLevel2( CryptoLevel2 cryptoLevel2 )
		{
		}

		#endregion

		#region Cluster Common

		public void OnPreviousClose( string ClusterName, PreviousClose previousClose )
		{
			RTDataRec Rec = GetRTDataRec( previousClose.ticker, ClusterName );
			Rec.PreviousClose = previousClose.results.First().c;
		}

		public void OnDailyOpenClose( string ClusterName, DailyOpenClose dailyOpenClose )
		{
		}

		#endregion

		internal RTDataRec GetRTDataRec( string Symbol, string ClusterName, bool Create = true )
		{
			RTDataRec Rec;
			if ( !RTDataDict.TryGetValue( Symbol, out Rec ) && Create )
			{
				Rec = new RTDataRec( Symbol, ClusterName );
				RTDataDict[Symbol] = Rec;

				OnSubscribedSymbolEvent?.Invoke( Rec );
				Rec.RequestLast = true;
			}
			return Rec;
		}

		public void RemoveRTData( RTDataRec RecIn )
		{
			RTDataRec Rec = GetRTDataRec( RecIn.Symbol, RecIn.ClusterName, false );
			if ( Rec != null )
			{
				OnUnSubscribedSymbolEvent?.Invoke( Rec );
				RTDataDict.Remove( Rec.Symbol );
			}
		}

	}

	public class RTDataRec : INotifyBase
	{
		public string ClusterName;
		public double PreviousClose;
		public bool RequestLast;
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
		public TickType PriceTick
		{
			get { return _PriceTick; }
			set { _PriceTick = value; OnPropertyChanged( nameof( PriceTick ) ); }
		}
		TickType _PriceTick;
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
		public long LastSize
		{
			get { return _LastSize; }
			set { _LastSize = value; OnPropertyChanged( nameof( LastSize ) ); }
		}
		long _LastSize;
		public double CryptoSize
		{
			get { return _CryptoSize; }
			set { _CryptoSize = value; OnPropertyChanged( nameof( CryptoSize ) ); }
		}
		double _CryptoSize;
		public double Change
		{
			get { return _Change; }
			set { _Change = value; OnPropertyChanged( nameof( Change ) ); }
		}
		double _Change;
		public TickType ChangeTick
		{
			get { return _ChangeTick; }
			set { _ChangeTick = value; OnPropertyChanged( nameof( ChangeTick ) ); }
		}
		TickType _ChangeTick;
		public string Spread
		{
			get { return _Spread; }
			set { _Spread = value; OnPropertyChanged( nameof( Spread ) ); }
		}
		string _Spread;


		public RTDataRec( string Symbol, string ClusterName )
		{
			this.Symbol = PGBase.GetSymbolOnly( Symbol );
			this.ClusterName = ClusterName;
			_PriceTick = _AskTick = _BidTick = _ChangeTick = TickType.FlatTick;
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

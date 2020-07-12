using PolygonApi.Clusters;
using PolygonWPFDemo.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PolygonWPFDemo.Controls
{
	public partial class RTDataBase : UserControl
	{
		#region Variables

		public PGClusterBase Cluster;
		public string ClusterName;
		public List<RTDataRowBase> RTDataRows;

		protected TextBlock ClusterNameTextBlock;
		protected Grid RTClusterDataGrid;
		protected Grid RTClusterDataHeader;

		#endregion

		#region Events

		public event OnRTDataRowMouseDoubleClickDel OnRTDataRowMouseDoubleClickEvent;
		public event OnRTDataHeightChangeDel OnRTDataHeightChangeEvent;

		#endregion

		public RTDataBase()
		{
			RTDataRows = new List<RTDataRowBase>();
		}

		#region InitEvents/UnInitEvents

		public void InitEvents()
		{
			UnInitEvents();
		}

		public void UnInitEvents()
		{
		}

		#endregion

		internal void InitRTClusterData( PGClusterBase Cluster )
		{
			this.Cluster = Cluster;
			ClusterNameTextBlock.Text = ClusterName = Cluster.ClusterName;
		}

		internal void AddSymbolRTDataRow( RTDataRec rtDataRec )
		{
			RTDataRowBase RTDataRow = RTDataRows.Find( x => x.Symbol == rtDataRec.Symbol );
			if ( RTDataRow == null )
			{
				switch ( ClusterName )
				{
					case PGClusterNames.Equities:
						RTDataRow = new RTEquitiesDataRow( rtDataRec.Symbol );
						break;
					case PGClusterNames.Forex:
						RTDataRow = new RTForexDataRow( rtDataRec.Symbol );
						break;
					case PGClusterNames.Crypto:
						RTDataRow = new RTCryptoDataRow( rtDataRec.Symbol );
						break;
					default:
						throw new Exception( $"Unknown ClusterName: {ClusterName}" );
				}

				RTDataRow.RowDef = new RowDefinition();
				RTDataRow.OnRTDataRowMouseDoubleClickEvent += FireOnRTDataRowMouseDoubleClick;
				RTDataRows.Add( RTDataRow );

				// binding
				RTDataRow.DataContext = rtDataRec;

				InitRTDataRows();
				SetRTClusterDataGridHeight();
			}
		}

		internal void RemoveSymbolRTDataRow( RTDataRec rtDataRec )
		{
			RTDataRowBase RTDataRow = RTDataRows.Find( x => x.Symbol == rtDataRec.Symbol );
			if ( RTDataRow != null )
			{
				RTDataRow.OnRTDataRowMouseDoubleClickEvent -= FireOnRTDataRowMouseDoubleClick;
				RTDataRow.DataContext = null;

				RTDataRows.Remove( RTDataRow );

				InitRTDataRows();
				SetRTClusterDataGridHeight();
			}
		}

		private void InitRTDataRows()
		{
			RTClusterDataGrid.Children.Clear();
			RTClusterDataGrid.RowDefinitions.Clear();

			RTDataRows = RTDataRows.OrderBy( x => x.Symbol ).ToList();
			for ( int i = 0; i < RTDataRows.Count; i++ )
			{
				RTClusterDataGrid.RowDefinitions.Add( RTDataRows[i].RowDef );
				RTClusterDataGrid.Children.Add( RTDataRows[i] );
				Grid.SetRow( RTDataRows[i], i );
			}
		}

		private void SetRTClusterDataGridHeight()
		{
			RTClusterDataGrid.Height = RTClusterDataHeader.Height +
				( RTDataRows.Count > 0 ? ( RTDataRows.Count * RTDataRows[0].Height ) : 0 );

			OnRTDataHeightChangeEvent?.Invoke( Cluster.ClusterName, RTClusterDataGrid.Height );
		}

		private void FireOnRTDataRowMouseDoubleClick( string ClusterName, string Symbol )
		{
			OnRTDataRowMouseDoubleClickEvent?.Invoke( ClusterName, Symbol );
		}

	}
}

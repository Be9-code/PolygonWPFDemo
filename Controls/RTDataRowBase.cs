using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace PolygonWPFDemo.Controls
{
	public partial class RTDataRowBase : UserControl
	{
		#region Variables

		public string Symbol;
		public string ClusterName;
		public RowDefinition RowDef;

		#endregion

		#region Events

		public event OnRTDataRowMouseDoubleClickDel OnRTDataRowMouseDoubleClickEvent;

		#endregion

		public RTDataRowBase( string Symbol )
		{
			this.Symbol = Symbol;
		}

		protected void OnRTDataRowMouseDoubleClick( object sender, MouseButtonEventArgs e )
		{
			OnRTDataRowMouseDoubleClickEvent?.Invoke( ClusterName, Symbol );
		}
	}
}

using PolygonApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PolygonWPFDemo.Controls
{
	public partial class EnterApiKey : UserControl
	{
		#region Variables

		public string Message;

		#endregion

		#region Properties

		#endregion

		#region Events

		public event OnNoParamsDel OnOKEvent;
		public event OnNoParamsDel OnCancelEvent;

		#endregion

		public EnterApiKey( string Message )
		{
			this.Message = Message;
			InitializeComponent();
		}

		private void OnWindowLoaded( object Sender, RoutedEventArgs EventArgs )
		{
			MessageText.Text = Message;
		}

		private void OnOKButtonClicked( object Sender, RoutedEventArgs EventArgs )
		{
			if ( OnOKEvent != null )
				OnOKEvent.Invoke();
		}

		private void Cancel_Click( object sender, RoutedEventArgs e )
		{
			if ( OnCancelEvent != null )
				OnCancelEvent.Invoke();
		}
	}
}

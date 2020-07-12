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
	public partial class EnterApiKeyControl : UserControl
	{
		#region Variables

		public string Message;
		private bool InitialEntry = true;

		#endregion

		#region Properties

		#endregion

		#region Events

		public event OnTextParamDel OnApiKeyEvent;
		public event OnNoParamsDel OnOKEvent;
		public event OnNoParamsDel OnCancelEvent;

		#endregion

		public EnterApiKeyControl( string Message = "" )
		{
			this.Message = Message;
			InitializeComponent();
		}

		private void OnApiKeyControlLoaded( object Sender, RoutedEventArgs EventArgs )
		{
			if ( !string.IsNullOrEmpty( Message ) )
				ApiKeyText.Text = Message;

			ApiKeyText.Focus();
		}

		#region ApiKey

		private void OnApiKeyPaste( object sender, DataObjectPastingEventArgs e )
		{
			var IsText = e.SourceDataObject.GetDataPresent( DataFormats.UnicodeText, true );
			if ( !IsText )
				return;

			if ( InitialEntry )
				ApiKeyText.Text = string.Empty;

			string ApiKey = e.SourceDataObject.GetData( DataFormats.UnicodeText ) as string;
			ApiKeyText.Text = ApiKey;
			OnApiKeyEvent?.Invoke( ApiKey );

			e.Handled = true;
		}

		#endregion
		
		private void OnOKButtonClicked( object Sender, RoutedEventArgs EventArgs )
		{
			OnOKEvent?.Invoke();
		}

		private void Cancel_Click( object sender, RoutedEventArgs e )
		{
			OnCancelEvent?.Invoke();
		}

		private void UserControl_KeyDown( object sender, KeyEventArgs e )
		{
			bool AltKey = Keyboard.IsKeyDown( Key.LeftAlt ) || Keyboard.IsKeyDown( Key.RightAlt );
			bool CtrlKey = Keyboard.IsKeyDown( Key.LeftCtrl ) || Keyboard.IsKeyDown( Key.RightCtrl );

			if ( InitialEntry )
				ApiKeyText.Text = string.Empty;
			
			InitialEntry = false;

			switch ( e.Key )
			{
				case Key.Escape:
					{
						e.Handled = true;
					}
					break;
				case Key.Enter:
					{
						OnOKEvent?.Invoke();
						e.Handled = true;
					}
					break;

				default:
					break;
			}
		}

		private void ApiKeyText_KeyDown( object sender, KeyEventArgs e )
		{
			switch ( e.Key )
			{
				case Key.Escape:
					{
						ApiKeyText.Text = string.Empty;
						e.Handled = true;
					}
					break;
				case Key.Enter:
					{
						OnOKEvent?.Invoke();
						e.Handled = true;
					}
					break;

				default:
					break;
			}
		}
	}
}

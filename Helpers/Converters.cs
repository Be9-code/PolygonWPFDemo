using PolygonWPFDemo.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace PolygonWPFDemo.Converters
{
	public class TickBrushConverter : IValueConverter
	{
		public static Brush TextBrush = new SolidColorBrush( (Color)ColorConverter.ConvertFromString( "#FFFFFFFF" ) );
		public static Brush FlatTickBrush = new SolidColorBrush( (Color)ColorConverter.ConvertFromString( "#FFFFFFFF" ) );
		public static Brush UpTickBrush = new SolidColorBrush( (Color)ColorConverter.ConvertFromString( "#FF00FF00" ) );
		public static Brush DownTickBrush = new SolidColorBrush( (Color)ColorConverter.ConvertFromString( "#FFFF6060" ) );
		public object Convert( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
		{
			try
			{
				TickType Tick = (TickType)value;
				switch ( Tick )
				{
					case TickType.UpTick:
						return UpTickBrush;
					case TickType.DownTick:
						return DownTickBrush;
					case TickType.FlatTick:
						return FlatTickBrush;
				}
			}
			catch ( Exception )
			{
			}
			return TextBrush;
		}

		public object ConvertBack( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
		{
			return System.Convert.ToDouble( value );
		}
	}

}

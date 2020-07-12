using PolygonApi;
using PolygonApi.Clusters;
using PolygonWPFDemo.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
	public delegate void OnRTDataHeightChangeDel( string ClusterName, double Height );

	public partial class RTEquitiesData : RTDataBase
	{
		public RTEquitiesData()
		{
			InitializeComponent();
			ClusterNameTextBlock = EquitiesClusterName;
			RTClusterDataGrid = EquitiesRTClusterDataGrid;
			RTClusterDataHeader = EquitiesRTClusterDataHeader;
		}

	}

}

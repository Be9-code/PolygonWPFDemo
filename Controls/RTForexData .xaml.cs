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
	public partial class RTForexData : RTDataBase
	{
		public RTForexData()
		{
			InitializeComponent();
			ClusterNameTextBlock = ForexClusterName;
			RTClusterDataGrid = ForexRTClusterDataGrid;
			RTClusterDataHeader = ForexRTClusterDataHeader;
		}

	}

}

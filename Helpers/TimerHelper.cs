using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PolygonWPFDemo.Helpers
{
	public delegate void OnTimerHelperDel( object Data );

	public class TimerHelper
	{
		public TimerHelper Instance;
		DispatcherTimer Timer;

		OnTimerHelperDel MethodToCall;
		public object Data;
		public bool IsOneShot;

		public TimerHelper( OnTimerHelperDel MethodToCall, int MSecs = 100, object Data = null )
		{
			this.MethodToCall = MethodToCall;
			this.Data = Data;

			Instance = this;
			StartTimer( MSecs );
		}
		public void StartTimer( int MSecs )
		{
			Timer = new DispatcherTimer();
			Timer.Tick += TimerHandler;
			Timer.Interval = new TimeSpan( 0, 0, 0, 0, MSecs );
			Timer.Start();
		}

		private void TimerHandler( object sender, EventArgs e )
		{
			if ( IsOneShot )
				StopTimer();

			MethodToCall( Data );
		}

		public void StopTimer()
		{
			Timer.Tick -= TimerHandler;
			Timer.IsEnabled = false;
			Timer.Stop();
		}
	}

}

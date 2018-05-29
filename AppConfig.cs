using System;

namespace pinger
{
	/// <summary>
	/// Summary description for AppConfig.
	/// </summary>
 [Serializable]
	public class AppConfig
	{
		public string server;
		public int ping_interval;
		public int ping_timeout;
		public int run_sample_count;
	 public int failure_count;
	 public bool display_all;
		public AppConfig()
		{
			//
			// TODO: Add constructor logic here
			//
		
		}
	}
}

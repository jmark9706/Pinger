using System;
using System.Net;

namespace pinger
{
	/// <summary>
	/// Summary description for Sample.
	/// </summary>
	/// 
	public class Sample
	{
		public Sample()
		{
			//
			// TODO: Add constructor logic here
			//
			exception_text="";
			exception_thrown=false;
			error_msg="";
		}

		public int status;
		public ushort seqno;
		public string server;
		public IPAddress ipa;
		public DateTime start_time;
		public bool exception_thrown;
		public string exception_text;
		public int elapsed_time;   // milliseconds
		public string error_msg;
		public int trans_seqno;
	}
}

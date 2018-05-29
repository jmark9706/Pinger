using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Threading;
using System.Collections;
using System.Runtime.InteropServices;


namespace pinger
{
	/// <summary>
	/// 
	/// </summary>
	public class PingSite
	{
		public int consecutive_fail_count;
		public int total_pings;
		public int total_elapsed_time;
		public Queue q;
		public int timeout;
		public int delay_time;
		public int min,max,sum,count;
		public double percent_good_pings;
		public PingSite()
		{
			// 
			// TODO: Add constructor logic here
			//
		}

		public int OnePing(IPAddress hostaddress, int delay, ushort id,
			ushort seq,ref TimeSpan elapsed, ref Sample smp)
		{
			// return code = 0 for OK, 1 for fail -- delay in seconds
			DateTime t1, t2;
			t1=DateTime.Now;
			smp.start_time=t1;
			smp.seqno=seq;
			byte[] pkt, rpkt;
			int rc = 0;
			rpkt = new byte[200];  // receive packet area
			IPEndPoint hostEndPoint;
			ushort cksum;
			int ptr;
			// length of outgoing packet
			int len = 60;
			pkt = new byte[len];
			pkt[0]=8;
			pkt[1]=0;
			hostEndPoint = new IPEndPoint(hostaddress,0);
			// get a socket
			Socket s = new Socket(AddressFamily.InterNetwork,SocketType.Raw,ProtocolType.Icmp);
			// bind to a local endpoint
			IPEndPoint ipe = new IPEndPoint(0,0);
			s.Bind(ipe);
			// build the ICMP packet
			cksum = 0;
			this.SetByteWithUshort(cksum,ref pkt,2);
			this.SetByteWithUshort(id,ref pkt,4);
			this.SetByteWithUshort(seq,ref pkt,6);
			// get the current date and time
			t1 = DateTime.Now;
			// compute checksum
			this.ComputeCksum(pkt,0,len,ref cksum);
			this.SetByteWithUshort(cksum,ref pkt,2);
			try
			{
				// send the packet
				s.SendTo(pkt,hostEndPoint);
			}
			catch (System.Exception e)
			{
				smp.exception_thrown=true;
				smp.exception_text=e.ToString();
				smp.status=1;
				rc=1;
			}

			// wait for the response
			
			bool stuff = s.Poll(delay * 1000000,SelectMode.SelectRead)& (rc==0);
			int rcnt;
			rc = 0;
			if(stuff)
			{
				
				try 
				{
					rcnt = s.Receive(rpkt);
					t2 = DateTime.Now;
					elapsed = t2.Subtract(t1);
					smp.elapsed_time=elapsed.Milliseconds;
					smp.status=0;
					// get the IP header length
					uint hl = (uint)rpkt[0]; // first byte has headerlength 
					hl = ( hl & 0xf ) * 4;  // displacement to start of the packet
					// get the sequence number we sent
					ushort rseq = 0;
					int pptr = (int)(hl+6);
					this.GetUshortFromByte(ref rseq,ref rpkt,pptr);
					// get the ICMP packet type code
					ushort rpktype = rpkt[hl];
					// verify the check sum
					ushort ncksum=0;
					int clen = rcnt - (int)hl;
					int phl = (int)hl;
					this.ComputeCksum(rpkt,phl ,clen ,ref ncksum);
					if (ncksum != 0 )
					{
						smp.error_msg = "Bad checksum on received packet";
						rc =1;
					} 
					
						if(rseq != seq)
					{
							smp.error_msg +=" Incorrect received sequence number";
						rc = 1;
					}
					
						if (rpktype !=0)
					{
						smp.error_msg+=" Incorrect ICMP type code = "+
							rpktype.ToString()+" ";
						rc = 1;
						}
				}
				catch (System.Exception e)
				{
					rc=1;  // indicate failure
					smp.exception_thrown=true;
					smp.exception_text=e.ToString();
					smp.error_msg="Packet receive threw exception";
					smp.status=1;

				}
			}
			else
			{// set return code to indicate timeout
				rc = 2;
				smp.status=2;
					smp.error_msg="Timeout";
			}
			// set return codes

			return rc;
		}

		public void SetByteWithUshort(ushort val, ref byte[] barray, int ptr)
		{
			ushort tmpl,tmpr;
			tmpr = (ushort)(val & (ushort)0xff);
			tmpl = (ushort)((val >> 8) & (ushort)0xff);
			barray[ptr]=(byte)tmpl;
			barray[ptr+1]=(byte)tmpr;
		
		}

		public void GetUshortFromByte(ref ushort val, ref byte[] barray, int ptr)
		{
				ushort tmp, tmp2;
			uint a;
			tmp = (ushort)(barray[ptr]);
			tmp2 = (ushort)(barray[ptr+1]);
			tmp = (ushort)(tmp <<  8); 
			val =(ushort)(tmp | tmp2);
			
		
		}

		public void ComputeCksum(byte[] barray,int start, int length, ref ushort cksum)
		{ 
			
			int sum,cnt;
			ushort tmp = 0;
			sum=0;
			int ptr = start;
			cnt = length;
			while ( cnt > 1 )
			{
				this.GetUshortFromByte(ref tmp,ref barray,ptr);
				cnt = cnt -2;
				ptr = ptr + 2;
				sum = sum + (0xffff & tmp);
			}
			// for an odd number of bytes
			if (cnt == 1)
			{
				sum = sum + (0xff & (int)(barray[ptr]));
			}

			sum = (sum >> 16) + (sum &0xffff);
			sum = sum + (sum >> 16);
			cksum = (ushort)( ~(sum & 0xffff));
		
		}

		public int ResolveHost(string hostname, ref IPAddress address)
		{ // resolve the host address
			int rc = 0;
			// returns 0 of OK, 1 if not resolved
			// resolve the server name

			// first try to parse the string as a numeric address
			// if fails, we will then try a dns resolve
			try
			{
				address = IPAddress.Parse(hostname);
			}
			catch (System.Exception e)
			{
				rc = 1;  // note the numerical parse failed
			}
			if (rc == 1)
			{
				try
				{
					IPHostEntry hostInfo = Dns.Resolve(hostname);
					IPAddress[] IPaddresses = hostInfo.AddressList;
					address = IPaddresses[0];
					rc = 0;
				}
				catch (System.Exception e)
				{
					rc = 1;
					MessageBox.Show("The host name "+hostname+" failed to resolve");
				}
			}
			return rc;
		}
	
		public void ThreadProc()
		{  // thread for pinging
			int res;
			
			
			  TimeSpan elapsed = new TimeSpan(0);
			count=0;
			double avg;
			min=9999999;
			max=0;
			sum=0;
			count_good=0;
			count_fail=0;
			total_pings=0;
			
			while(go)
			{
				Sample s = new Sample();    // create new sample instance
				s.trans_seqno = bptr.trans_seqno++;  
				res = this.OnePing(taddress,bptr.ping_timeout,5,tseqno,  ref elapsed, ref s);
			tseqno++;
				Monitor.Enter(q);
				q.Enqueue(s);
				Monitor.Exit(q);
				total_pings++;
				if (res==0) 
				{
				count_good++;
				count=elapsed.Milliseconds;
				sum = sum + count ;
					if(count > max) max= count;
					if (count < min) min = count;
					
				 } 
				else
				{
				count_fail++;
				}
				Thread.Sleep(bptr.ping_interval*1000);
				avg = (double)sum/(double)count_good;
				this.percent_good_pings= ((double)count_good/(double)total_pings)*100.0D;
				bptr.textBox3.Text=count_good.ToString();
				bptr.textBox4.Text=count_fail.ToString();
			    bptr.textBox6.Text=min.ToString();
				bptr.textBox7.Text=max.ToString();
				bptr.textBox8.Text=avg.ToString("f2");
				bptr.label14.Text=percent_good_pings.ToString("f2")+"% Successful Pings";
				bptr.Invalidate();
				bptr.Update();
				
			}
		
		} 

		public string tserver
		{
			get
			{
				return null;
			}
			set
			{
				tserver=value;
			}
		}

		public ushort tseqno;

		
		

		public Form1 bptr;
		public Thread mythread;
		public int count_good;
		public int count_fail;

		public IPAddress taddress;
		public bool go;
		
	}
}

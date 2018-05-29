
using System;
using System.Collections;
using System.Net;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;
namespace pinger
{
	/// <summary>
	/// Summary description for Analyze.
	/// </summary>
	public class Analyze
	{
		public Queue q;
		public Form1 f;  // main form instance
		public Sample smp, fail_smp;
		public int current_fail_count=0;
		bool flag;
		public bool state, prev_state;
		public bool start,go;
		public bool display_all;
		public int[] hist;
		public int current_count;
		public int hist_ptr;
		public int today;
		public bool newday;
		public DateTime fail_seg_end;
		public TimeSpan elapsed;
		public int temp;
		public double duration;
		public string seg_msg;
        // arraylist to hold values for std dev computation
        public ArrayList samples;
        public double ssum;
        public double savg;
        public double sdiff;

		public Analyze()
			
			
	{
		//
		// TODO: Add constructor logic here
		//
			state=true;
			start=false;
			hist=new int[1000];
			current_count=0;
			hist_ptr=0;
			today=89;
            // init the array list for std dev samples
            samples = new ArrayList();
           
			
	}

	public void tproc()
{
	int nn = 0;
		int tt;
		f.link_status=Form1.link_state.OK;
		while(!start){Thread.Sleep(1000);};
		while (go)
{
	flag = false;
	Monitor.Enter(q);
	tt=q.Count;
	Monitor.Exit(q);
		if (tt  > 0 )
		{
			Monitor.Enter(q);
			flag=true;	
			smp = (Sample) q.Dequeue();  // get sample to analyze
			Monitor.Exit(q);
			// debug mode hook to simulate failures
			if (f.fail_state)
				smp.status=1; // force a failure
			// set state variable
			bool incoming = smp.status == 0;
			// smp.status == 0 if ping successful
			// set the link_status
			f.prev_link_status=f.link_status;
			if (incoming)
			{// the ping was good
				current_fail_count=0;
				f.label13.ForeColor=Color.Green;
				f.label13.Text="Link State OK";
				if(f.prev_link_status!=Form1.link_state.OK)
				{ note_state_change();}
				f.link_status=Form1.link_state.OK;
			}
			else
			{ // the ping failed
				note_ping_failure();
				f.label13.ForeColor=Color.Red;
				current_fail_count++;
				if (current_fail_count>=f.ac.failure_count)
				{ //
					note_link_failure();
					f.link_status=Form1.link_state.link_failed;
					
				}
				else
				{ // still counting up to the link state change
					f.link_status=Form1.link_state.last_ping_failed;
					f.label13.Text="Last Ping Failed";
				}
			}
			
			f.state=incoming;
			// analyze sample
			//  add time to sample history
			if (incoming)  // if ping failed, do not add to 
				// the running average
			{
                samples.Add(smp.elapsed_time);  // save every sample value from this run
                hist[this.hist_ptr++]=smp.elapsed_time;  
				if (hist_ptr>=f.run_samples_count)hist_ptr=0;
				if (current_count<f.run_samples_count)current_count++;
				// compute running average of the last f.run_samples_count
				double hsum = 0;
				for (int i=0;i<current_count;i++)
					hsum+=(double)hist[i];
				double avg = hsum / (double)current_count;
                // compute std deviation over all samples
                // compute average first
                ssum=0;
                foreach (object osamp in samples)
                { ssum=ssum+(int)(osamp);
                }
                savg=ssum/(double)samples.Count;
                // compute std dev
                ssum = 0;
                foreach (object osamp in samples)
                {
                    sdiff = Math.Pow(((int)osamp - savg) ,2) + sdiff;
                }
                sdiff = sdiff / (double)samples.Count;
                // finally compute std dev
                sdiff = Math.Sqrt(sdiff);
				string xxx = Convert.ToString(avg);
				f.textBox12.Text=String.Format("{0:f2}",avg);
				f.textBox13.Text=current_count.ToString();
                f.textBox15.Text = samples.Count.ToString();
                f.textBox16.Text = String.Format("{0:f2}", savg);
                f.textBox17.Text = String.Format("{0:f2}", sdiff);
				f.Invalidate();
			}
			// check for new day
			newday=today!=smp.start_time.Day;
			today=smp.start_time.Day;
			
			prev_state = state;
			if (incoming) state = true;
			else
				state = false;
			if (smp.status > 0 || this.display_all || newday)
			{
				if (smp.status > 0 )
				{ f.textBox9.ForeColor = Color.Black; }
				else
				{ f.textBox9.ForeColor = Color.Black; }
				//  build display line
				string et = smp.elapsed_time.ToString() + " ms ";
				string sq = " "+ smp.seqno.ToString() + " " ;
				string t = smp.error_msg;

				// omit the exception text message - very verbose
				// string u = smp.exception_text;
				string u = " ";
				DateTime n = smp.start_time;
				string ct = " "+n.ToLongTimeString()+" ";
				string tsn = " seqno=" + smp.trans_seqno.ToString() + ": ";
				string sep="\r\n";
				if(newday) sep="\r\n___________________________________";

				string y =  n.ToLongDateString() +ct+ sq + " " + t+" "+u + " " + et;
				y = y+sep;
				if (flag )
				{
					f.textBox9.Text = y  + f.textBox9.Text;
						
				}
			}
			// 
		}
		else
		{
				Thread.Sleep(2000);
		}
	
	
}
}

		public void note_state_change()
		{ // we got a good ping
			// see if we have a link outage message to save
			if(f.link_status==Form1.link_state.link_failed)
			{
				f.textBox14.ForeColor=Color.Red;
				string tmp = f.textBox14.Text;
				tmp = seg_msg+"\r\n"+tmp;
				
				f.textBox14.Text=tmp;
			}
		
		}

		public void note_ping_failure()
		{
			if(f.prev_link_status==Form1.link_state.OK)
			{ this.fail_smp=smp;  // save first failed ping
			}

		}

		public void note_link_failure()
		{  // we are now a link outage, display message
			fail_seg_end=DateTime.Now;
			elapsed=fail_seg_end.Subtract( fail_smp.start_time);
			
			duration = elapsed.TotalSeconds/60.0D;
			seg_msg=" Started: "+fail_smp.start_time.ToShortDateString()+" "+
				fail_smp.start_time.ToShortTimeString()+" Duration: "+duration.ToString("F2")+" Minutes";
		f.label13.Text="Link Outage:"+seg_msg;
		
		}
}
}

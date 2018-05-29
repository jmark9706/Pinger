using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using PlayAudio;
namespace pinger
{
	/// <summary>
    /// 3.02 -- added standard deviation computation
	/// Summary description for Form1.
	/// 2.23 -- fixed cpu spin on start var in analyze 12/12/05
	///			also worked on iconsrc to do expanding circle
	///			The size of the box was not set to 16
	/// 2.21 -- added expanding notifyicon
	/// 2.20 -- fixed ping so it would stop when link restored
	/// version 2.19 - added second timer for audio
	/// version 2.18 - added outage segment code
	/// enable / disabled start/stop ping buttons
	/// deleted display of exception text messages
	/// held start of analyze thread until start of ping thread
	/// -- it seemed to fix thread exit exception when the debug
	/// breakpoint was set in analyze... this had not happened before
	/// some debugger funny????
	/// version 2.17 - added audio
	/// version 2.16 - adding IconSequence class for tray icons,
	///					added debug variable
	/// version 2.15 - started adding Icon arrays for the tray icon
	/// version 2.14 - started adding link outage logic and data structures
	///				- started adding global state enums
	/// version 2.13 - added the tray icon, crude version of blinking indicator
	/// green for ok, red for not
	/// version 2.12 - started adding tray icon
	/// </summary>
	public class Form1 : System.Windows.Forms.Form
	{
		public AutoResetEvent autoEvent;
		public IconSource iconsrc;
		public bool debug_switch=false; // debug control variable
		public bool fail_state=false;  // debug only
		public bool audio_state=false;
		public MenuItem miAudio;
		public  enum link_state  {OK,last_ping_failed,link_failed};
		public link_state link_status, prev_link_status;
		private System.Drawing.Size oldsz;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.TextBox textBox2;
		static public string version = "3.02";
		static public string date = "May 08, 2012";
		
		public IconSequence good, bad;
		public System.Windows.Forms.Timer blink;
		public System.Threading.Timer ping;
		ushort seqno;
		public bool toggle=false;
		public bool state=true;
		public NotifyIcon ni;
		public Thread pthread;
		public Thread athread;
		public System.Threading.TimerCallback timerDelegate;
		public Form f;
		public PingSite p;
		public Queue q;
		public Analyze a;
		public int ping_timeout=5;
		public int ping_interval=30;
		public int run_samples_count=20;
		public int trans_seqno;
		public bool pthread_started=false;
		DirectoryInfo di;
		public string AppConfigFileName = "PingerConfigFile V"+version;
        public string AppLogFileName = "PingerLogFile";
		public AppConfig ac;
		public DateTime begin;

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button3;
		public System.Windows.Forms.TextBox textBox3;
		public System.Windows.Forms.TextBox textBox4;
		private System.Windows.Forms.TextBox textBox5;
		public System.Windows.Forms.TextBox textBox6;
		public System.Windows.Forms.TextBox textBox7;
		public System.Windows.Forms.TextBox textBox8;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label8;
		public System.Windows.Forms.TextBox textBox9;
		private System.Windows.Forms.CheckBox checkBox1;
		private System.Windows.Forms.TextBox textBox10;
		private System.Windows.Forms.TextBox textBox11;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label10;
		public System.Windows.Forms.TextBox textBox12;
		public System.Windows.Forms.TextBox textBox13;
		public System.Windows.Forms.Label label11;
		public System.Windows.Forms.Label label12;
		public System.Windows.Forms.Label label13;
		private MenuItem miLinkFail;
		public System.Windows.Forms.Label label14;
		public System.Windows.Forms.TextBox textBox14;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.Label label16;
        private Label label17;
        private Label label18;
        private Label label19;
        public TextBox textBox15;
        public TextBox textBox16;
        public TextBox textBox17;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public Form1()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
			// get the start time
			begin = DateTime.Now;
			link_status=link_state.OK;
			MenuItem miOpen = new MenuItem("Select Database",
				new EventHandler(MenuFileOpenOnClick));

			MenuItem miExit = new MenuItem("Exit",
				new EventHandler(MenuFileExitOnClick));

			MenuItem miSetParms = new MenuItem("Set Parameters",
				new EventHandler(MenuFileSetParmsOnClick));

			miLinkFail = new MenuItem("Simulate Link Failure",
				new EventHandler(MenuHelpLinkFailOnClick));

			miAudio = new MenuItem("Audio Enabled",
				new EventHandler(MenuHelpAudioOnClick));

			MenuItem miAbout = new MenuItem("About",
				new EventHandler(MenuHelpAboutOnClick));

			MenuItem miFile = new MenuItem("&File",
				new MenuItem[] {miSetParms,miAudio, miExit});

			MenuItem miHelp = new MenuItem("&Help",
				new MenuItem[] {miLinkFail,miAbout});
			//Main menu
			Menu= new MainMenu(new MenuItem[] {miFile,miHelp});
			//
			// TODO: Add any constructor code after InitializeComponent call
			//
			oldsz = this.Size;
			Icon ic = new Icon(GetType(),"titlebaricon.ico");
			this.Icon=ic;
			this.components = new System.ComponentModel.Container();
			ni = new NotifyIcon(this.components);
			
			iconsrc = new IconSource(16);
			iconsrc.radius=1;
			ni.Text="Pinger";
			ni.DoubleClick+=new System.EventHandler(this.tray_dclick);
			//  set up timer to blink the icon
			blink=new System.Windows.Forms.Timer();
			blink.Tick+=new System.EventHandler(blink_tick);
			blink.Interval=1000;
			blink.Start();
			// audio timer
			 timerDelegate = 
				new TimerCallback(this.ping_tick);
			 
			audio_state=false;
			AutoResetEvent autoEvent     = new AutoResetEvent(false);

			ping = new System.Threading.Timer(timerDelegate,autoEvent,Timeout.Infinite,2000);

			di = new DirectoryInfo(".\\");
			AppConfigFileName = di.FullName + AppConfigFileName;
			// read the config file - prog defaults set in the Config file code
			GetConfig();
			seqno = 0;
			f=this;
			q = new Queue();
			a = new Analyze();
			a.f = this;
			a.q = q;
            // start the Analyze polling loop
			a.go=true;
			checkBox1.Checked=ac.display_all;
			athread = new Thread(new ThreadStart(a.tproc));
			// start the analysis thread
			athread.Start();
			textBox10.Text=ping_timeout.ToString();
			textBox11.Text=ping_interval.ToString();
			trans_seqno = 1;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.textBox5 = new System.Windows.Forms.TextBox();
            this.textBox6 = new System.Windows.Forms.TextBox();
            this.textBox7 = new System.Windows.Forms.TextBox();
            this.textBox8 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox9 = new System.Windows.Forms.TextBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.textBox10 = new System.Windows.Forms.TextBox();
            this.textBox11 = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.textBox12 = new System.Windows.Forms.TextBox();
            this.textBox13 = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.textBox14 = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.textBox15 = new System.Windows.Forms.TextBox();
            this.textBox16 = new System.Windows.Forms.TextBox();
            this.textBox17 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(104, 24);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(144, 20);
            this.textBox1.TabIndex = 0;
            this.textBox1.Text = "www.robojoe.com";
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(40, 56);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(80, 24);
            this.button1.TabIndex = 1;
            this.button1.Text = "Single Ping";
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(112, 96);
            this.textBox2.Name = "textBox2";
            this.textBox2.ReadOnly = true;
            this.textBox2.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox2.Size = new System.Drawing.Size(176, 20);
            this.textBox2.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(40, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 24);
            this.label1.TabIndex = 3;
            this.label1.Text = "Hostname";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(120, 128);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(80, 23);
            this.button2.TabIndex = 4;
            this.button2.Text = "Start Pinging";
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Enabled = false;
            this.button3.Location = new System.Drawing.Point(232, 128);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 5;
            this.button3.Text = "End Pinging";
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(120, 192);
            this.textBox3.Name = "textBox3";
            this.textBox3.ReadOnly = true;
            this.textBox3.Size = new System.Drawing.Size(96, 20);
            this.textBox3.TabIndex = 6;
            // 
            // textBox4
            // 
            this.textBox4.Location = new System.Drawing.Point(120, 216);
            this.textBox4.Name = "textBox4";
            this.textBox4.ReadOnly = true;
            this.textBox4.Size = new System.Drawing.Size(96, 20);
            this.textBox4.TabIndex = 7;
            // 
            // textBox5
            // 
            this.textBox5.Location = new System.Drawing.Point(200, 56);
            this.textBox5.Name = "textBox5";
            this.textBox5.ReadOnly = true;
            this.textBox5.Size = new System.Drawing.Size(136, 20);
            this.textBox5.TabIndex = 8;
            // 
            // textBox6
            // 
            this.textBox6.Location = new System.Drawing.Point(120, 256);
            this.textBox6.Name = "textBox6";
            this.textBox6.ReadOnly = true;
            this.textBox6.Size = new System.Drawing.Size(96, 20);
            this.textBox6.TabIndex = 9;
            // 
            // textBox7
            // 
            this.textBox7.Location = new System.Drawing.Point(120, 280);
            this.textBox7.Name = "textBox7";
            this.textBox7.ReadOnly = true;
            this.textBox7.Size = new System.Drawing.Size(96, 20);
            this.textBox7.TabIndex = 10;
            // 
            // textBox8
            // 
            this.textBox8.Location = new System.Drawing.Point(120, 304);
            this.textBox8.Name = "textBox8";
            this.textBox8.ReadOnly = true;
            this.textBox8.Size = new System.Drawing.Size(64, 20);
            this.textBox8.TabIndex = 11;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(48, 96);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(48, 16);
            this.label2.TabIndex = 12;
            this.label2.Text = "Status";
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(48, 192);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(72, 16);
            this.label3.TabIndex = 13;
            this.label3.Text = "Successful";
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(144, 56);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(64, 16);
            this.label4.TabIndex = 14;
            this.label4.Text = "Time (ms)";
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(56, 216);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(56, 16);
            this.label5.TabIndex = 15;
            this.label5.Text = "Failures";
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(24, 256);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(88, 16);
            this.label6.TabIndex = 16;
            this.label6.Text = "Min Time (ms)";
            this.label6.Click += new System.EventHandler(this.label6_Click);
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(24, 280);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(88, 16);
            this.label7.TabIndex = 17;
            this.label7.Text = "Max time (ms)";
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(24, 304);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(96, 16);
            this.label8.TabIndex = 18;
            this.label8.Text = "Average (ms)";
            // 
            // textBox9
            // 
            this.textBox9.AcceptsReturn = true;
            this.textBox9.Location = new System.Drawing.Point(384, 296);
            this.textBox9.Multiline = true;
            this.textBox9.Name = "textBox9";
            this.textBox9.ReadOnly = true;
            this.textBox9.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox9.Size = new System.Drawing.Size(552, 256);
            this.textBox9.TabIndex = 19;
            // 
            // checkBox1
            // 
            this.checkBox1.Location = new System.Drawing.Point(120, 360);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(136, 24);
            this.checkBox1.TabIndex = 20;
            this.checkBox1.Text = "Display all ping results";
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // textBox10
            // 
            this.textBox10.Location = new System.Drawing.Point(120, 392);
            this.textBox10.Name = "textBox10";
            this.textBox10.ReadOnly = true;
            this.textBox10.Size = new System.Drawing.Size(48, 20);
            this.textBox10.TabIndex = 21;
            this.textBox10.Text = "10";
            this.textBox10.TextChanged += new System.EventHandler(this.textBox10_TextChanged);
            // 
            // textBox11
            // 
            this.textBox11.Location = new System.Drawing.Point(120, 424);
            this.textBox11.Name = "textBox11";
            this.textBox11.ReadOnly = true;
            this.textBox11.Size = new System.Drawing.Size(48, 20);
            this.textBox11.TabIndex = 22;
            this.textBox11.Text = "30";
            // 
            // label9
            // 
            this.label9.Location = new System.Drawing.Point(0, 392);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(112, 16);
            this.label9.TabIndex = 23;
            this.label9.Text = "Ping Timeout (secs)";
            // 
            // label10
            // 
            this.label10.Location = new System.Drawing.Point(8, 424);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(112, 23);
            this.label10.TabIndex = 24;
            this.label10.Text = "Ping Interval (secs)";
            // 
            // textBox12
            // 
            this.textBox12.Location = new System.Drawing.Point(120, 456);
            this.textBox12.Name = "textBox12";
            this.textBox12.ReadOnly = true;
            this.textBox12.Size = new System.Drawing.Size(56, 20);
            this.textBox12.TabIndex = 26;
            // 
            // textBox13
            // 
            this.textBox13.Location = new System.Drawing.Point(136, 496);
            this.textBox13.Name = "textBox13";
            this.textBox13.ReadOnly = true;
            this.textBox13.Size = new System.Drawing.Size(56, 20);
            this.textBox13.TabIndex = 27;
            // 
            // label11
            // 
            this.label11.Location = new System.Drawing.Point(16, 448);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(88, 32);
            this.label11.TabIndex = 28;
            this.label11.Text = "Running Average (ms)";
            // 
            // label12
            // 
            this.label12.Location = new System.Drawing.Point(16, 488);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(112, 32);
            this.label12.TabIndex = 29;
            this.label12.Text = "Number of Samples in Running Average";
            // 
            // label13
            // 
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.ForeColor = System.Drawing.Color.Crimson;
            this.label13.Location = new System.Drawing.Point(296, 16);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(672, 32);
            this.label13.TabIndex = 30;
            this.label13.Click += new System.EventHandler(this.label13_Click);
            // 
            // label14
            // 
            this.label14.Location = new System.Drawing.Point(72, 168);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(216, 16);
            this.label14.TabIndex = 32;
            // 
            // textBox14
            // 
            this.textBox14.ForeColor = System.Drawing.Color.Red;
            this.textBox14.Location = new System.Drawing.Point(384, 88);
            this.textBox14.Multiline = true;
            this.textBox14.Name = "textBox14";
            this.textBox14.ReadOnly = true;
            this.textBox14.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox14.Size = new System.Drawing.Size(544, 160);
            this.textBox14.TabIndex = 33;
            // 
            // label15
            // 
            this.label15.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label15.Location = new System.Drawing.Point(488, 264);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(232, 24);
            this.label15.TabIndex = 34;
            this.label15.Text = "Ping Results";
            this.label15.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label16
            // 
            this.label16.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.Location = new System.Drawing.Point(496, 56);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(184, 24);
            this.label16.TabIndex = 35;
            this.label16.Text = "Link Outages";
            this.label16.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(266, 392);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(74, 13);
            this.label17.TabIndex = 36;
            this.label17.Text = "Total Samples";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(266, 434);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(74, 13);
            this.label18.TabIndex = 37;
            this.label18.Text = "Total Average";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(266, 476);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(98, 13);
            this.label19.TabIndex = 38;
            this.label19.Text = "Standard Deviation";
            // 
            // textBox15
            // 
            this.textBox15.Location = new System.Drawing.Point(269, 408);
            this.textBox15.Name = "textBox15";
            this.textBox15.Size = new System.Drawing.Size(100, 20);
            this.textBox15.TabIndex = 39;
            // 
            // textBox16
            // 
            this.textBox16.Location = new System.Drawing.Point(269, 450);
            this.textBox16.Name = "textBox16";
            this.textBox16.Size = new System.Drawing.Size(100, 20);
            this.textBox16.TabIndex = 40;
            // 
            // textBox17
            // 
            this.textBox17.Location = new System.Drawing.Point(269, 496);
            this.textBox17.Name = "textBox17";
            this.textBox17.Size = new System.Drawing.Size(100, 20);
            this.textBox17.TabIndex = 41;
            // 
            // Form1
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(992, 566);
            this.Controls.Add(this.textBox17);
            this.Controls.Add(this.textBox16);
            this.Controls.Add(this.textBox15);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.textBox14);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.textBox13);
            this.Controls.Add(this.textBox12);
            this.Controls.Add(this.textBox11);
            this.Controls.Add(this.textBox10);
            this.Controls.Add(this.textBox9);
            this.Controls.Add(this.textBox8);
            this.Controls.Add(this.textBox7);
            this.Controls.Add(this.textBox6);
            this.Controls.Add(this.textBox5);
            this.Controls.Add(this.textBox4);
            this.Controls.Add(this.textBox3);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.Form1_Closing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Form1 f = new Form1();
			f.Text="Pinger "+version;
			Application.Run(f);
		}

		private void button1_Click(object sender, System.EventArgs e)
		{ // single ping
			TimeSpan elapsed = new TimeSpan(0);
			string server = textBox1.Text;
			ushort id = 1;
			ushort seq = 1;
			IPAddress address = null;
			seqno+=1;
			textBox2.Text="Resolving address";
			this.Invalidate();
			this.Update();
			PingSite p = new PingSite();
			Thread t = new Thread(new ThreadStart(p.ThreadProc));
			int rc = p.ResolveHost(server,ref address);
			if (rc == 0 )
			{
					textBox2.Text="Pinging";
				this.Invalidate();
				this.Update();
				Sample smp = new Sample();
				rc = p.OnePing(address, 5,id,seqno,ref elapsed,ref smp);
				if (rc ==0)
				{
					textBox2.Text="OK "+ seqno.ToString();
					textBox5.Text=elapsed.Milliseconds.ToString();
				}
				else
				{
					textBox2.Text="No response";
				}
			}
			else
			{
				textBox2.Text = "Unable to resolve hostname";
			}
			
		}

		private void button2_Click(object sender, System.EventArgs e)
		{/// start second pinging thread
				
		{
			string server = textBox1.Text;
			ushort id = 1;
			ushort seq = 1;
			IPAddress address = null;
			seqno+=1;
			textBox2.Text="Resolving address";
			this.Invalidate();
			this.Update();
			p = new PingSite();
			pthread = new Thread(new ThreadStart(p.ThreadProc));
			int rc = p.ResolveHost(server,ref address);
			p.taddress=address;
			p.tseqno=0;
			p.go=true;
			p.bptr=this;
			p.q=q;
			p.mythread=pthread;
			if (rc == 0 )
			{// start the thread
				a.start=true;  // start the analysis thread
				textBox1.ReadOnly=true;
				button2.Enabled=false;
				button3.Enabled=true;
				textBox2.Text="Pinging";
				this.Invalidate();
				this.Update();
				pthread_started=true;
				pthread.Start();
			
			}
			else
			{
				pthread = null;
				textBox2.Text = "Unable to resolve hostname";
			}
			
		}
		}

		private void button3_Click(object sender, System.EventArgs e)
		{
			p.go=false;
			pthread_started=false;
			button2.Enabled=true;
			button3.Enabled=false;
			textBox1.ReadOnly=false;
		}

		private void Form1_Load(object sender, System.EventArgs e)
		{
		
		}

		private void label6_Click(object sender, System.EventArgs e)
		{
		
		}

		private void checkBox1_CheckedChanged(object sender, System.EventArgs e)
		{
			a.display_all = checkBox1.Checked;
		}

		private void textBox10_TextChanged(object sender, System.EventArgs e)
		{
		
		}

		private void button4_Click(object sender, System.EventArgs e)
		{
			ping_timeout= Convert.ToInt32(textBox10.Text);
			ping_interval=Convert.ToInt32(textBox11.Text);
		}

		private void Form1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			
			a.go=false;
			if(pthread_started) // see if started
			{
				p.go=false;  // stop the pinging thread
				pthread.Abort();pthread.Join();
			}
			athread.Abort();
			athread.Join();
			ac.server=textBox1.Text;
			ac.run_sample_count=this.run_samples_count;
			ac.ping_interval=this.ping_interval;
			ac.ping_timeout=this.ping_timeout;
			ac.display_all=checkBox1.Checked;
			SetConfig();
			ni.Dispose();
			
		}
		public void MenuFileExitOnClick(object obj, EventArgs ea)
		{
			
			Close();
		
		}
	 
		protected override void OnResize(EventArgs e)
		{
			if (this.WindowState == FormWindowState.Minimized)
			{
				this.Hide();
			}
			base.OnResize (e);
			
		}

		public void GetConfig()
		{
			if (File.Exists(AppConfigFileName))
			{ // retrieve last configuration
				IFormatter formatter = new BinaryFormatter();
				Stream s = new FileStream(AppConfigFileName,FileMode.Open, FileAccess.Read,
					FileShare.Read);
				ac = (AppConfig) formatter.Deserialize(s);
				s.Close();
				// set locals from config file, not a good design
				textBox1.Text=ac.server;
				
				this.ping_interval=ac.ping_interval;
				this.ping_timeout=ac.ping_timeout;
				this.run_samples_count=ac.run_sample_count;
				textBox10.Text=ac.ping_timeout.ToString();
				textBox11.Text=ac.ping_interval.ToString();
			}
			else
			{ // Create a config file - deafault values;
				ac = new AppConfig();
				ac.server=this.textBox1.Text;
				ac.display_all=true;
				ac.failure_count=3;
				ac.ping_interval=30;
				ac.ping_timeout=5;
				ac.run_sample_count=20;
				SetConfig();
			}
		
		}

		public void SetConfig()
		{ // this assumes that the object has been initialized with the values
			
			IFormatter formatter = new BinaryFormatter();
			Stream s = new FileStream(AppConfigFileName,FileMode.Create, FileAccess.Write,
				FileShare.None);
			formatter.Serialize(s,ac);
			s.Close();
		}

		public void MenuFileOpenOnClick(object obj, EventArgs ea)
		{
		}
		public void blink_tick(object obj, EventArgs ea)
		{
		
		{
			ni.Visible=true;
			if (this.link_status==link_state.OK)
			{//ni.Icon=good.next_icon;
				iconsrc.mycolor=Color.Green;
			ni.Icon=iconsrc.next_icon();}
			else
			{
				// ni.Icon=bad.next_icon;
				iconsrc.mycolor=Color.Red;
				ni.Icon=iconsrc.next_icon();
				// if(audio_state)Wave.Play("radarping.wav",Wave.SND_NOWAIT);
			}
		}
		}
			public void MenuHelpAboutOnClick(object o,EventArgs ea)
			{
				About a = new About(version,date);
				a.ShowDialog();
			}
		private void tray_dclick(object o,EventArgs ea)
		{
			//if (this.WindowState == FormWindowState.Minimized)
			
			
			// Activate the form.
			this.Visible=true;
			this.Activate();
			this.WindowState = FormWindowState.Normal;
		}
		public void MenuFileSetParmsOnClick(object obj,EventArgs ea)
		{
			InfoDialog id = new InfoDialog();
			id.ping_interval=ping_interval;
			id.ping_timeout=ping_timeout;
			id.run_samples_count=run_samples_count;
			id.failure_count=ac.failure_count;
			id.init();
			if(id.ShowDialog()==DialogResult.OK)
			{
				ping_interval=id.ping_interval;
				ping_timeout=id.ping_timeout;
				textBox10.Text=ping_timeout.ToString();
				textBox11.Text=ping_interval.ToString();
				Invalidate();
				run_samples_count=id.run_samples_count;
				ac.failure_count=id.failure_count;
			}

		}
		private void MenuHelpLinkFailOnClick(object o,EventArgs ea)
		{
			fail_state=!fail_state;
			miLinkFail.Checked=fail_state;
		}

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void MenuHelpAudioOnClick(object o,EventArgs ea)
		{
				audio_state=!audio_state;
			miAudio.Checked=audio_state;
			ping.Change(0,2000);
		}
		private void ping_tick(object o)
		{
			if(audio_state &(link_status!=Form1.link_state.OK))Wave.Play("radarping.wav",Wave.SND_NOWAIT);
		}
	
		
		
	}
}

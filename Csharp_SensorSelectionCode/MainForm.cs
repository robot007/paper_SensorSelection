using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using HermesMiddleware.CommonLibrary;
using HermesMiddleware.MoteLibrary;
using HermesMiddleware.DBLibrary;
using HermesMiddleware.GUILibrary;
using Cassini;
using System.IO;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.InteropServices;
using System.Xml;
using System.Data;
using System.Data.OleDb;

// DSN sensor selection
using System.Text;
using Mapack;


namespace DataAcquisitionService
{	/// <summary>
	/// Summary description for MainForm.
	/// </summary>
	public class MainForm : System.Windows.Forms.Form
	{
		// main service doing whole business logic
		public MainService mainService;

		// global stuff
		public Global global;

		// worker thread
		Thread workerThread;

		// DB stuff
		DBAccess dbAccess;
		// archive snapshot
		DataTable tblArchiveSnapshot;

		// original color of button
		Color colorButton;

        // for RTLS beacon config
        Telos_Msg managementMsg;
        // for DSNdemo
        public int InitSamplingRate = 17; //15*17=255

        enum BeaconControlMode
        {
            Sleep = 1,
            Wakeup,
            Config,
            Idel
        }
        [FlagsAttribute]
        enum ConfigMask : ushort
        {
            MASK_CHANNEL15 = 0x0001,
            MASK_CHANNEL20 = 0x0002,
            MASK_CHANNEL25 = 0x0004,
            MASK_CHANNEL26 = 0x0008,
            MASK_TXPOWER3 = 0x0010,
            MASK_TXPOWER7 = 0x0020,
            MASK_TXPOWER11 = 0x0040,
            MASK_TXPOWER19 = 0x0080,
            MASK_TXPOWER31 = 0x0100
        }
        BeaconControlMode BeaconMode;
        private int nSleepPacket = 0, nWakeupPacket = 0, nConfigPacket = 0;
        Thread sendMsgThread;

		// delegates for thread-safe calls from other threads
		public delegate void DisplayByteDel(byte b, RichTextBox richTextBox);
		public delegate void DisplayMsgDel(TOS_Msg msg, RichTextBox richTextBox);
		public delegate void PrintStatusDel(string sStatus);
		public delegate void PrintMsgStatusDel(long lReceivedMsg, long lSentMsg, long lProcessedMsg, long lToDoMsg);

		public delegate void DisplayStringTextBoxDel(string sMsg, RichTextBox richTextBox);

		private System.Windows.Forms.LinkLabel linkLabelWebService;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Button buttonClose;
		private System.Windows.Forms.TabControl tabControlSettings;
		private System.Windows.Forms.Button buttonSet;
		private System.Windows.Forms.Label label4;
		public System.Windows.Forms.RichTextBox richTextBoxReceivedBytes;
		private System.Windows.Forms.TabPage tabPageReceived;
		private System.Windows.Forms.Label label6;
		public System.Windows.Forms.RichTextBox richTextBoxLastReceivedMessage;
		private System.Windows.Forms.TabPage tabPageSent;
		public System.Windows.Forms.RichTextBox richTextBoxLastSentMessage;
		private System.Windows.Forms.Label label7;
		public System.Windows.Forms.TextBox textBoxStatus;
		private System.Windows.Forms.TabPage tabPageSensors;
		private CustomDataGrid dataGridSensors;
		private System.Windows.Forms.Label label8;
		public System.Windows.Forms.RichTextBox richTextBoxSentBytes;
		private System.Windows.Forms.CheckBox checkBoxCom;
		private System.Windows.Forms.Label label3;
		public System.Windows.Forms.TextBox textBoxReceivedMessages;
		private System.Windows.Forms.Label label5;
		public System.Windows.Forms.TextBox textBoxSentMessages;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label13;
		private System.ComponentModel.IContainer components;
        private System.Windows.Forms.Label label12;
        public CustomDataGrid dataGridVariables;
		private System.Windows.Forms.TabPage tabPageSubscriptions;
		private System.Windows.Forms.Label label14;
		public HermesMiddleware.GUILibrary.CustomDataGrid dataGridSubscriptions;
		private System.Windows.Forms.TabPage tabPageSettings;
		private System.Windows.Forms.TabPage tabPageData;
		public HermesMiddleware.GUILibrary.CustomDataGrid dataGridSettings;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Timer timerTester;
		private System.Windows.Forms.Timer timerDataGridUpdater;
		public System.Windows.Forms.TextBox textBoxProcessedMessages;
		private System.Windows.Forms.Label label10;
		public System.Windows.Forms.TextBox textBoxToDoMessages;
		private System.Windows.Forms.Label label11;
		private TabPage tabPageP2PNetwork;
		private Label label15;
		public CustomDataGrid dataGridPublishedVariables;
		private Label label16;
		public CustomDataGrid dataGridPeers;
		private TextBox textBoxRole;
		private Label label17;
		private CheckBox checkBoxP2PConnected;
		private TabPage tabPageQueries;
		private Label label18;
		public CustomDataGrid dataGridQueries;
		private ContextMenuStrip queryTableMenuStrip1;
		private ToolStripMenuItem clearToolStripMenuItem;
		private ContextMenuStrip subscriptionMenuStrip;
		private ToolStripMenuItem removeToolStripMenuItem;
		private ContextMenuStrip peerMenuStrip;
		private ToolStripMenuItem removeSelectedPeerToolStripMenuItem;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripMenuItem removeAllToolStripMenuItem;
        private TabPage tabPageRTLSBeacons;
        private Label label19;
        private Button BeaconSleep;
        private Button BeaconWakeup;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private Button ChangeSetting;
        private Label ListenTime;
        private TextBox SleepTime;
        private Label label20;
        private TextBox ListenTimeDuringSleep;
        private Label SleepStatus;
        private Label WakeupStatus;
        private Label label21;
        private CheckBox Channel15;
        private CheckBox Channel20;
        private CheckBox Channel26;
        private CheckBox Channel25;
        private CheckBox TxPower3;
        private Label label22;
        private CheckBox TxPower7;
        private CheckBox TxPower11;
        private CheckBox TxPower19;
        private CheckBox TxPower31;
        private Label ChangeSettingPacketSent;
        private Button DefaultSetting;
        private Button DefaultSleepSetting;
        private System.Windows.Forms.Timer BeaconControlMsgTimer;
        private Label label23;
        private Label label24;
        private RichTextBox AwakenBeaconRichTextBox;
        private GroupBox WakupGroupBox;
        private PictureBox pictureBox1;
        private Label SniffingChannel;
        private GroupBox SniffGroupBox;
        private Label SleepMoteLabel;
        private ComboBox SleepModeComboBox;
        private Button SnifferButton;
        private System.Windows.Forms.Timer SnifferResetTimer;
		private ToolTip toolTipChangeSetting;
		private ContextMenuStrip dataTableMenuStrip;
		private ToolStripMenuItem queryToolStripMenuItem;
		private ToolStripMenuItem subscriptionToolStripMenuItem;
		private ToolStripMenuItem increasePeriodToolStripMenuItem;
		private ToolStripMenuItem decreasePeriodToolStripMenuItem;
		private ToolStripMenuItem changeAddressToolStripMenuItem;
		private TabPage tabPageCluster;
		public TreeView treeViewNodes;
		private Button buttonSubscribe;
		private Button buttonQuery;
		private TextBox textBoxPeriod;
		private Button buttonUnsubscribe;
		private Button buttonActivate;
		private Button buttonInterval;
		private TextBox textBoxInterval;
		public Button buttonDSN;
        

        // for model-based event estimation
        public SensorSelection DSNSensorSelection;
        private Label label26;
        private GroupBox groupBox3;
        private TextBox TotalSampNumTextBox;
        private Label TotalSampNumText;
        private RadioButton radioButton2;
        private RadioButton radioButtonTotalSampNum;

		public DSNPerformanceForm performanceForm;
		private Button buttonResetSeqNum;
        public TextBox textBoxAmbientLight;
        private Label label25;

		// form loaded flag
		bool bFormLoaded = false;

		public MainForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			try
			{
				Application.Run(new MainForm());
			}
			catch (Exception exp)
			{
				Console.WriteLine(exp);
			}
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("DAS Base");
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.linkLabelWebService = new System.Windows.Forms.LinkLabel();
            this.label9 = new System.Windows.Forms.Label();
            this.buttonClose = new System.Windows.Forms.Button();
            this.tabControlSettings = new System.Windows.Forms.TabControl();
            this.tabPageSensors = new System.Windows.Forms.TabPage();
            this.label13 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxReceivedMessages = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxSentMessages = new System.Windows.Forms.TextBox();
            this.checkBoxCom = new System.Windows.Forms.CheckBox();
            this.dataGridSensors = new HermesMiddleware.GUILibrary.CustomDataGrid();
            this.textBoxProcessedMessages = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.textBoxToDoMessages = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.tabPageCluster = new System.Windows.Forms.TabPage();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.textBoxAmbientLight = new System.Windows.Forms.TextBox();
            this.label25 = new System.Windows.Forms.Label();
            this.buttonResetSeqNum = new System.Windows.Forms.Button();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButtonTotalSampNum = new System.Windows.Forms.RadioButton();
            this.TotalSampNumTextBox = new System.Windows.Forms.TextBox();
            this.textBoxPeriod = new System.Windows.Forms.TextBox();
            this.buttonInterval = new System.Windows.Forms.Button();
            this.buttonDSN = new System.Windows.Forms.Button();
            this.buttonActivate = new System.Windows.Forms.Button();
            this.label26 = new System.Windows.Forms.Label();
            this.TotalSampNumText = new System.Windows.Forms.Label();
            this.textBoxInterval = new System.Windows.Forms.TextBox();
            this.buttonUnsubscribe = new System.Windows.Forms.Button();
            this.buttonSubscribe = new System.Windows.Forms.Button();
            this.buttonQuery = new System.Windows.Forms.Button();
            this.treeViewNodes = new System.Windows.Forms.TreeView();
            this.dataTableMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.queryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.subscriptionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.increasePeriodToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.decreasePeriodToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeAddressToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabPageData = new System.Windows.Forms.TabPage();
            this.label12 = new System.Windows.Forms.Label();
            this.dataGridVariables = new HermesMiddleware.GUILibrary.CustomDataGrid();
            this.tabPageReceived = new System.Windows.Forms.TabPage();
            this.label6 = new System.Windows.Forms.Label();
            this.richTextBoxLastReceivedMessage = new System.Windows.Forms.RichTextBox();
            this.richTextBoxReceivedBytes = new System.Windows.Forms.RichTextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tabPageSent = new System.Windows.Forms.TabPage();
            this.label8 = new System.Windows.Forms.Label();
            this.richTextBoxSentBytes = new System.Windows.Forms.RichTextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.richTextBoxLastSentMessage = new System.Windows.Forms.RichTextBox();
            this.tabPageP2PNetwork = new System.Windows.Forms.TabPage();
            this.checkBoxP2PConnected = new System.Windows.Forms.CheckBox();
            this.textBoxRole = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.dataGridPeers = new HermesMiddleware.GUILibrary.CustomDataGrid();
            this.peerMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.removeSelectedPeerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dataGridPublishedVariables = new HermesMiddleware.GUILibrary.CustomDataGrid();
            this.tabPageSubscriptions = new System.Windows.Forms.TabPage();
            this.label14 = new System.Windows.Forms.Label();
            this.dataGridSubscriptions = new HermesMiddleware.GUILibrary.CustomDataGrid();
            this.subscriptionMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.removeAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabPageQueries = new System.Windows.Forms.TabPage();
            this.label18 = new System.Windows.Forms.Label();
            this.dataGridQueries = new HermesMiddleware.GUILibrary.CustomDataGrid();
            this.queryTableMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabPageSettings = new System.Windows.Forms.TabPage();
            this.dataGridSettings = new HermesMiddleware.GUILibrary.CustomDataGrid();
            this.buttonSet = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPageRTLSBeacons = new System.Windows.Forms.TabPage();
            this.SniffGroupBox = new System.Windows.Forms.GroupBox();
            this.SnifferButton = new System.Windows.Forms.Button();
            this.SniffingChannel = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.AwakenBeaconRichTextBox = new System.Windows.Forms.RichTextBox();
            this.label24 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.DefaultSetting = new System.Windows.Forms.Button();
            this.ChangeSettingPacketSent = new System.Windows.Forms.Label();
            this.TxPower31 = new System.Windows.Forms.CheckBox();
            this.TxPower19 = new System.Windows.Forms.CheckBox();
            this.TxPower11 = new System.Windows.Forms.CheckBox();
            this.TxPower7 = new System.Windows.Forms.CheckBox();
            this.TxPower3 = new System.Windows.Forms.CheckBox();
            this.label22 = new System.Windows.Forms.Label();
            this.Channel26 = new System.Windows.Forms.CheckBox();
            this.Channel25 = new System.Windows.Forms.CheckBox();
            this.Channel20 = new System.Windows.Forms.CheckBox();
            this.label21 = new System.Windows.Forms.Label();
            this.Channel15 = new System.Windows.Forms.CheckBox();
            this.ChangeSetting = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.SleepModeComboBox = new System.Windows.Forms.ComboBox();
            this.SleepMoteLabel = new System.Windows.Forms.Label();
            this.DefaultSleepSetting = new System.Windows.Forms.Button();
            this.SleepStatus = new System.Windows.Forms.Label();
            this.ListenTimeDuringSleep = new System.Windows.Forms.TextBox();
            this.ListenTime = new System.Windows.Forms.Label();
            this.SleepTime = new System.Windows.Forms.TextBox();
            this.label20 = new System.Windows.Forms.Label();
            this.BeaconSleep = new System.Windows.Forms.Button();
            this.label19 = new System.Windows.Forms.Label();
            this.WakupGroupBox = new System.Windows.Forms.GroupBox();
            this.BeaconWakeup = new System.Windows.Forms.Button();
            this.WakeupStatus = new System.Windows.Forms.Label();
            this.textBoxStatus = new System.Windows.Forms.TextBox();
            this.timerTester = new System.Windows.Forms.Timer(this.components);
            this.timerDataGridUpdater = new System.Windows.Forms.Timer(this.components);
            this.BeaconControlMsgTimer = new System.Windows.Forms.Timer(this.components);
            this.SnifferResetTimer = new System.Windows.Forms.Timer(this.components);
            this.toolTipChangeSetting = new System.Windows.Forms.ToolTip(this.components);
            this.tabControlSettings.SuspendLayout();
            this.tabPageSensors.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridSensors)).BeginInit();
            this.tabPageCluster.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.dataTableMenuStrip.SuspendLayout();
            this.tabPageData.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridVariables)).BeginInit();
            this.tabPageReceived.SuspendLayout();
            this.tabPageSent.SuspendLayout();
            this.tabPageP2PNetwork.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridPeers)).BeginInit();
            this.peerMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridPublishedVariables)).BeginInit();
            this.tabPageSubscriptions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridSubscriptions)).BeginInit();
            this.subscriptionMenuStrip.SuspendLayout();
            this.tabPageQueries.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridQueries)).BeginInit();
            this.queryTableMenuStrip1.SuspendLayout();
            this.tabPageSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridSettings)).BeginInit();
            this.tabPageRTLSBeacons.SuspendLayout();
            this.SniffGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.WakupGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // linkLabelWebService
            // 
            this.linkLabelWebService.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.linkLabelWebService.Location = new System.Drawing.Point(159, 269);
            this.linkLabelWebService.Name = "linkLabelWebService";
            this.linkLabelWebService.Size = new System.Drawing.Size(397, 16);
            this.linkLabelWebService.TabIndex = 0;
            this.linkLabelWebService.TabStop = true;
            this.linkLabelWebService.Text = "http://";
            this.linkLabelWebService.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnLinkClick);
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(66, 269);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(91, 16);
            this.label9.TabIndex = 7;
            this.label9.Text = "Web Service:";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonClose
            // 
            this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonClose.Location = new System.Drawing.Point(524, 336);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(56, 24);
            this.buttonClose.TabIndex = 9;
            this.buttonClose.Text = "&Close";
            this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
            // 
            // tabControlSettings
            // 
            this.tabControlSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControlSettings.Controls.Add(this.tabPageSensors);
            this.tabControlSettings.Controls.Add(this.tabPageCluster);
            this.tabControlSettings.Controls.Add(this.tabPageData);
            this.tabControlSettings.Controls.Add(this.tabPageReceived);
            this.tabControlSettings.Controls.Add(this.tabPageSent);
            this.tabControlSettings.Controls.Add(this.tabPageP2PNetwork);
            this.tabControlSettings.Controls.Add(this.tabPageSubscriptions);
            this.tabControlSettings.Controls.Add(this.tabPageQueries);
            this.tabControlSettings.Controls.Add(this.tabPageSettings);
            this.tabControlSettings.Controls.Add(this.tabPageRTLSBeacons);
            this.tabControlSettings.Location = new System.Drawing.Point(8, 8);
            this.tabControlSettings.Name = "tabControlSettings";
            this.tabControlSettings.SelectedIndex = 0;
            this.tabControlSettings.Size = new System.Drawing.Size(572, 320);
            this.tabControlSettings.TabIndex = 10;
            this.tabControlSettings.SelectedIndexChanged += new System.EventHandler(this.tabControlSettings_SelectedIndexChanged);
            // 
            // tabPageSensors
            // 
            this.tabPageSensors.Controls.Add(this.label13);
            this.tabPageSensors.Controls.Add(this.label2);
            this.tabPageSensors.Controls.Add(this.label3);
            this.tabPageSensors.Controls.Add(this.textBoxReceivedMessages);
            this.tabPageSensors.Controls.Add(this.label5);
            this.tabPageSensors.Controls.Add(this.textBoxSentMessages);
            this.tabPageSensors.Controls.Add(this.checkBoxCom);
            this.tabPageSensors.Controls.Add(this.dataGridSensors);
            this.tabPageSensors.Controls.Add(this.textBoxProcessedMessages);
            this.tabPageSensors.Controls.Add(this.label10);
            this.tabPageSensors.Controls.Add(this.textBoxToDoMessages);
            this.tabPageSensors.Controls.Add(this.label11);
            this.tabPageSensors.Location = new System.Drawing.Point(4, 22);
            this.tabPageSensors.Name = "tabPageSensors";
            this.tabPageSensors.Size = new System.Drawing.Size(564, 294);
            this.tabPageSensors.TabIndex = 3;
            this.tabPageSensors.Text = "Sensors";
            this.tabPageSensors.UseVisualStyleBackColor = true;
            // 
            // label13
            // 
            this.label13.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.Location = new System.Drawing.Point(8, 240);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(96, 16);
            this.label13.TabIndex = 20;
            this.label13.Text = "Communication:";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(8, 8);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 16);
            this.label2.TabIndex = 19;
            this.label2.Text = "Sensors:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(176, 240);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 16);
            this.label3.TabIndex = 17;
            this.label3.Text = "Received:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBoxReceivedMessages
            // 
            this.textBoxReceivedMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textBoxReceivedMessages.Location = new System.Drawing.Point(248, 240);
            this.textBoxReceivedMessages.Name = "textBoxReceivedMessages";
            this.textBoxReceivedMessages.ReadOnly = true;
            this.textBoxReceivedMessages.Size = new System.Drawing.Size(64, 20);
            this.textBoxReceivedMessages.TabIndex = 16;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(176, 266);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 16);
            this.label5.TabIndex = 18;
            this.label5.Text = "Sent:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBoxSentMessages
            // 
            this.textBoxSentMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textBoxSentMessages.Location = new System.Drawing.Point(248, 264);
            this.textBoxSentMessages.Name = "textBoxSentMessages";
            this.textBoxSentMessages.ReadOnly = true;
            this.textBoxSentMessages.Size = new System.Drawing.Size(64, 20);
            this.textBoxSentMessages.TabIndex = 15;
            // 
            // checkBoxCom
            // 
            this.checkBoxCom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxCom.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxCom.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkBoxCom.Location = new System.Drawing.Point(8, 264);
            this.checkBoxCom.Name = "checkBoxCom";
            this.checkBoxCom.Size = new System.Drawing.Size(56, 24);
            this.checkBoxCom.TabIndex = 14;
            this.checkBoxCom.Text = "Off";
            this.checkBoxCom.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxCom.CheckedChanged += new System.EventHandler(this.checkBoxCom_CheckedChanged);
            // 
            // dataGridSensors
            // 
            this.dataGridSensors.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridSensors.CaptionVisible = false;
            this.dataGridSensors.DataMember = "";
            this.dataGridSensors.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.dataGridSensors.Location = new System.Drawing.Point(8, 24);
            this.dataGridSensors.Name = "dataGridSensors";
            this.dataGridSensors.RowHeaderWidth = 6;
            this.dataGridSensors.Size = new System.Drawing.Size(548, 208);
            this.dataGridSensors.TabIndex = 0;
            // 
            // textBoxProcessedMessages
            // 
            this.textBoxProcessedMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textBoxProcessedMessages.Location = new System.Drawing.Point(392, 240);
            this.textBoxProcessedMessages.Name = "textBoxProcessedMessages";
            this.textBoxProcessedMessages.ReadOnly = true;
            this.textBoxProcessedMessages.Size = new System.Drawing.Size(64, 20);
            this.textBoxProcessedMessages.TabIndex = 16;
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(320, 240);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(64, 16);
            this.label10.TabIndex = 17;
            this.label10.Text = "Processed:";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBoxToDoMessages
            // 
            this.textBoxToDoMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textBoxToDoMessages.Location = new System.Drawing.Point(392, 264);
            this.textBoxToDoMessages.Name = "textBoxToDoMessages";
            this.textBoxToDoMessages.ReadOnly = true;
            this.textBoxToDoMessages.Size = new System.Drawing.Size(64, 20);
            this.textBoxToDoMessages.TabIndex = 16;
            // 
            // label11
            // 
            this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(320, 264);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(64, 16);
            this.label11.TabIndex = 17;
            this.label11.Text = "To Do:";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tabPageCluster
            // 
            this.tabPageCluster.Controls.Add(this.groupBox3);
            this.tabPageCluster.Controls.Add(this.buttonUnsubscribe);
            this.tabPageCluster.Controls.Add(this.buttonSubscribe);
            this.tabPageCluster.Controls.Add(this.buttonQuery);
            this.tabPageCluster.Controls.Add(this.treeViewNodes);
            this.tabPageCluster.Location = new System.Drawing.Point(4, 22);
            this.tabPageCluster.Name = "tabPageCluster";
            this.tabPageCluster.Size = new System.Drawing.Size(564, 294);
            this.tabPageCluster.TabIndex = 9;
            this.tabPageCluster.Text = "Cluster";
            this.tabPageCluster.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.textBoxAmbientLight);
            this.groupBox3.Controls.Add(this.label25);
            this.groupBox3.Controls.Add(this.buttonResetSeqNum);
            this.groupBox3.Controls.Add(this.radioButton2);
            this.groupBox3.Controls.Add(this.radioButtonTotalSampNum);
            this.groupBox3.Controls.Add(this.TotalSampNumTextBox);
            this.groupBox3.Controls.Add(this.textBoxPeriod);
            this.groupBox3.Controls.Add(this.buttonInterval);
            this.groupBox3.Controls.Add(this.buttonDSN);
            this.groupBox3.Controls.Add(this.buttonActivate);
            this.groupBox3.Controls.Add(this.label26);
            this.groupBox3.Controls.Add(this.TotalSampNumText);
            this.groupBox3.Controls.Add(this.textBoxInterval);
            this.groupBox3.Location = new System.Drawing.Point(268, 45);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(293, 232);
            this.groupBox3.TabIndex = 32;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "DSN setting";
            // 
            // textBoxAmbientLight
            // 
            this.textBoxAmbientLight.Location = new System.Drawing.Point(177, 119);
            this.textBoxAmbientLight.Name = "textBoxAmbientLight";
            this.textBoxAmbientLight.Size = new System.Drawing.Size(91, 20);
            this.textBoxAmbientLight.TabIndex = 36;
            this.textBoxAmbientLight.Text = "2000";
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(177, 97);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(95, 13);
            this.label25.TabIndex = 35;
            this.label25.Text = "Ambient Light Shift";
            // 
            // buttonResetSeqNum
            // 
            this.buttonResetSeqNum.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonResetSeqNum.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonResetSeqNum.Location = new System.Drawing.Point(103, 194);
            this.buttonResetSeqNum.Name = "buttonResetSeqNum";
            this.buttonResetSeqNum.Size = new System.Drawing.Size(99, 24);
            this.buttonResetSeqNum.TabIndex = 34;
            this.buttonResetSeqNum.Text = "Reset SeqNum";
            this.buttonResetSeqNum.Click += new System.EventHandler(this.buttonResetSeqNum_Click);
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Checked = true;
            this.radioButton2.Location = new System.Drawing.Point(6, 97);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(151, 17);
            this.radioButton2.TabIndex = 33;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "Per Sensor Sampling Num.";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.CheckedChanged += new System.EventHandler(this.radioButton2_CheckedChanged);
            // 
            // radioButtonTotalSampNum
            // 
            this.radioButtonTotalSampNum.AutoSize = true;
            this.radioButtonTotalSampNum.Location = new System.Drawing.Point(6, 23);
            this.radioButtonTotalSampNum.Name = "radioButtonTotalSampNum";
            this.radioButtonTotalSampNum.Size = new System.Drawing.Size(123, 17);
            this.radioButtonTotalSampNum.TabIndex = 32;
            this.radioButtonTotalSampNum.TabStop = true;
            this.radioButtonTotalSampNum.Text = "Total Sampling Num.";
            this.radioButtonTotalSampNum.UseVisualStyleBackColor = true;
            this.radioButtonTotalSampNum.CheckedChanged += new System.EventHandler(this.radioButtonTotalSampNum_CheckedChanged);
            // 
            // TotalSampNumTextBox
            // 
            this.TotalSampNumTextBox.Enabled = false;
            this.TotalSampNumTextBox.Location = new System.Drawing.Point(57, 46);
            this.TotalSampNumTextBox.Name = "TotalSampNumTextBox";
            this.TotalSampNumTextBox.Size = new System.Drawing.Size(71, 20);
            this.TotalSampNumTextBox.TabIndex = 24;
            this.TotalSampNumTextBox.Text = "255";
            // 
            // textBoxPeriod
            // 
            this.textBoxPeriod.Location = new System.Drawing.Point(57, 120);
            this.textBoxPeriod.Name = "textBoxPeriod";
            this.textBoxPeriod.Size = new System.Drawing.Size(71, 20);
            this.textBoxPeriod.TabIndex = 24;
            this.textBoxPeriod.Text = "0";
            // 
            // buttonInterval
            // 
            this.buttonInterval.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonInterval.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonInterval.Location = new System.Drawing.Point(218, 164);
            this.buttonInterval.Name = "buttonInterval";
            this.buttonInterval.Size = new System.Drawing.Size(69, 24);
            this.buttonInterval.TabIndex = 28;
            this.buttonInterval.Text = "Set Interval";
            this.buttonInterval.Click += new System.EventHandler(this.buttonInterval_Click);
            // 
            // buttonDSN
            // 
            this.buttonDSN.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDSN.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonDSN.Location = new System.Drawing.Point(103, 164);
            this.buttonDSN.Name = "buttonDSN";
            this.buttonDSN.Size = new System.Drawing.Size(99, 24);
            this.buttonDSN.TabIndex = 29;
            this.buttonDSN.Text = "DSN demo GUI";
            this.buttonDSN.Click += new System.EventHandler(this.buttonDSN_Click);
            // 
            // buttonActivate
            // 
            this.buttonActivate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonActivate.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonActivate.Location = new System.Drawing.Point(26, 164);
            this.buttonActivate.Name = "buttonActivate";
            this.buttonActivate.Size = new System.Drawing.Size(69, 24);
            this.buttonActivate.TabIndex = 26;
            this.buttonActivate.Text = "Activate";
            this.buttonActivate.Click += new System.EventHandler(this.buttonActivate_Click);
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(174, 23);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(86, 13);
            this.label26.TabIndex = 31;
            this.label26.Text = "Tras. Period (ms)";
            // 
            // TotalSampNumText
            // 
            this.TotalSampNumText.AutoSize = true;
            this.TotalSampNumText.Location = new System.Drawing.Point(23, 30);
            this.TotalSampNumText.Name = "TotalSampNumText";
            this.TotalSampNumText.Size = new System.Drawing.Size(0, 13);
            this.TotalSampNumText.TabIndex = 30;
            // 
            // textBoxInterval
            // 
            this.textBoxInterval.Location = new System.Drawing.Point(177, 46);
            this.textBoxInterval.Name = "textBoxInterval";
            this.textBoxInterval.Size = new System.Drawing.Size(71, 20);
            this.textBoxInterval.TabIndex = 27;
            this.textBoxInterval.Text = "1000";
            // 
            // buttonUnsubscribe
            // 
            this.buttonUnsubscribe.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonUnsubscribe.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonUnsubscribe.Location = new System.Drawing.Point(181, 75);
            this.buttonUnsubscribe.Name = "buttonUnsubscribe";
            this.buttonUnsubscribe.Size = new System.Drawing.Size(69, 24);
            this.buttonUnsubscribe.TabIndex = 25;
            this.buttonUnsubscribe.Text = "Unsubscr";
            this.buttonUnsubscribe.Click += new System.EventHandler(this.buttonUnsubscribe_Click);
            // 
            // buttonSubscribe
            // 
            this.buttonSubscribe.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSubscribe.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonSubscribe.Location = new System.Drawing.Point(181, 45);
            this.buttonSubscribe.Name = "buttonSubscribe";
            this.buttonSubscribe.Size = new System.Drawing.Size(69, 24);
            this.buttonSubscribe.TabIndex = 23;
            this.buttonSubscribe.Text = "Subscribe";
            this.buttonSubscribe.Click += new System.EventHandler(this.buttonSubscribe_Click);
            // 
            // buttonQuery
            // 
            this.buttonQuery.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonQuery.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonQuery.Location = new System.Drawing.Point(181, 15);
            this.buttonQuery.Name = "buttonQuery";
            this.buttonQuery.Size = new System.Drawing.Size(69, 24);
            this.buttonQuery.TabIndex = 22;
            this.buttonQuery.Text = "Query";
            this.buttonQuery.Click += new System.EventHandler(this.buttonQuery_Click);
            // 
            // treeViewNodes
            // 
            this.treeViewNodes.ContextMenuStrip = this.dataTableMenuStrip;
            this.treeViewNodes.Location = new System.Drawing.Point(3, 15);
            this.treeViewNodes.Name = "treeViewNodes";
            treeNode1.Name = "Node0";
            treeNode1.Text = "DAS Base";
            this.treeViewNodes.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode1});
            this.treeViewNodes.Size = new System.Drawing.Size(151, 262);
            this.treeViewNodes.TabIndex = 0;
            // 
            // dataTableMenuStrip
            // 
            this.dataTableMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.queryToolStripMenuItem,
            this.subscriptionToolStripMenuItem,
            this.changeAddressToolStripMenuItem});
            this.dataTableMenuStrip.Name = "dataTableMenuStrip";
            this.dataTableMenuStrip.Size = new System.Drawing.Size(154, 70);
            // 
            // queryToolStripMenuItem
            // 
            this.queryToolStripMenuItem.Name = "queryToolStripMenuItem";
            this.queryToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.queryToolStripMenuItem.Text = "Query";
            // 
            // subscriptionToolStripMenuItem
            // 
            this.subscriptionToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.increasePeriodToolStripMenuItem,
            this.decreasePeriodToolStripMenuItem});
            this.subscriptionToolStripMenuItem.Name = "subscriptionToolStripMenuItem";
            this.subscriptionToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.subscriptionToolStripMenuItem.Text = "Subscription";
            // 
            // increasePeriodToolStripMenuItem
            // 
            this.increasePeriodToolStripMenuItem.Name = "increasePeriodToolStripMenuItem";
            this.increasePeriodToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.increasePeriodToolStripMenuItem.Text = "Increase Period";
            this.increasePeriodToolStripMenuItem.Click += new System.EventHandler(this.increasePeriodToolStripMenuItem_Click);
            // 
            // decreasePeriodToolStripMenuItem
            // 
            this.decreasePeriodToolStripMenuItem.Name = "decreasePeriodToolStripMenuItem";
            this.decreasePeriodToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.decreasePeriodToolStripMenuItem.Text = "Decrease Period";
            // 
            // changeAddressToolStripMenuItem
            // 
            this.changeAddressToolStripMenuItem.Name = "changeAddressToolStripMenuItem";
            this.changeAddressToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.changeAddressToolStripMenuItem.Text = "Change Address";
            // 
            // tabPageData
            // 
            this.tabPageData.Controls.Add(this.label12);
            this.tabPageData.Controls.Add(this.dataGridVariables);
            this.tabPageData.Location = new System.Drawing.Point(4, 22);
            this.tabPageData.Name = "tabPageData";
            this.tabPageData.Size = new System.Drawing.Size(564, 294);
            this.tabPageData.TabIndex = 4;
            this.tabPageData.Text = "Data";
            this.tabPageData.UseVisualStyleBackColor = true;
            // 
            // label12
            // 
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.Location = new System.Drawing.Point(8, 8);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(72, 16);
            this.label12.TabIndex = 20;
            this.label12.Text = "Data:";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dataGridVariables
            // 
            this.dataGridVariables.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridVariables.CaptionVisible = false;
            this.dataGridVariables.ContextMenuStrip = this.dataTableMenuStrip;
            this.dataGridVariables.DataMember = "";
            this.dataGridVariables.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.dataGridVariables.Location = new System.Drawing.Point(8, 24);
            this.dataGridVariables.Name = "dataGridVariables";
            this.dataGridVariables.ReadOnly = true;
            this.dataGridVariables.RowHeadersVisible = false;
            this.dataGridVariables.RowHeaderWidth = 6;
            this.dataGridVariables.Size = new System.Drawing.Size(548, 230);
            this.dataGridVariables.TabIndex = 1;
            this.dataGridVariables.MouseUp += new System.Windows.Forms.MouseEventHandler(this.dataGridVariables_MouseUp);
            // 
            // tabPageReceived
            // 
            this.tabPageReceived.Controls.Add(this.label6);
            this.tabPageReceived.Controls.Add(this.richTextBoxLastReceivedMessage);
            this.tabPageReceived.Controls.Add(this.richTextBoxReceivedBytes);
            this.tabPageReceived.Controls.Add(this.label4);
            this.tabPageReceived.Location = new System.Drawing.Point(4, 22);
            this.tabPageReceived.Name = "tabPageReceived";
            this.tabPageReceived.Size = new System.Drawing.Size(564, 294);
            this.tabPageReceived.TabIndex = 1;
            this.tabPageReceived.Text = "Received";
            this.tabPageReceived.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(8, 8);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(88, 16);
            this.label6.TabIndex = 11;
            this.label6.Text = "Raw Bytes:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // richTextBoxLastReceivedMessage
            // 
            this.richTextBoxLastReceivedMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxLastReceivedMessage.Location = new System.Drawing.Point(8, 160);
            this.richTextBoxLastReceivedMessage.Name = "richTextBoxLastReceivedMessage";
            this.richTextBoxLastReceivedMessage.ReadOnly = true;
            this.richTextBoxLastReceivedMessage.Size = new System.Drawing.Size(548, 128);
            this.richTextBoxLastReceivedMessage.TabIndex = 10;
            this.richTextBoxLastReceivedMessage.Text = "";
            // 
            // richTextBoxReceivedBytes
            // 
            this.richTextBoxReceivedBytes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxReceivedBytes.Location = new System.Drawing.Point(8, 24);
            this.richTextBoxReceivedBytes.Name = "richTextBoxReceivedBytes";
            this.richTextBoxReceivedBytes.ReadOnly = true;
            this.richTextBoxReceivedBytes.Size = new System.Drawing.Size(548, 120);
            this.richTextBoxReceivedBytes.TabIndex = 9;
            this.richTextBoxReceivedBytes.Text = "";
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(8, 144);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(88, 16);
            this.label4.TabIndex = 8;
            this.label4.Text = "Last Message:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tabPageSent
            // 
            this.tabPageSent.Controls.Add(this.label8);
            this.tabPageSent.Controls.Add(this.richTextBoxSentBytes);
            this.tabPageSent.Controls.Add(this.label7);
            this.tabPageSent.Controls.Add(this.richTextBoxLastSentMessage);
            this.tabPageSent.Location = new System.Drawing.Point(4, 22);
            this.tabPageSent.Name = "tabPageSent";
            this.tabPageSent.Size = new System.Drawing.Size(564, 294);
            this.tabPageSent.TabIndex = 2;
            this.tabPageSent.Text = "Sent";
            this.tabPageSent.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(8, 8);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(88, 16);
            this.label8.TabIndex = 13;
            this.label8.Text = "Raw Bytes:";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // richTextBoxSentBytes
            // 
            this.richTextBoxSentBytes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxSentBytes.Location = new System.Drawing.Point(8, 24);
            this.richTextBoxSentBytes.Name = "richTextBoxSentBytes";
            this.richTextBoxSentBytes.ReadOnly = true;
            this.richTextBoxSentBytes.Size = new System.Drawing.Size(548, 120);
            this.richTextBoxSentBytes.TabIndex = 12;
            this.richTextBoxSentBytes.Text = "";
            // 
            // label7
            // 
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(8, 144);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(88, 16);
            this.label7.TabIndex = 11;
            this.label7.Text = "Last Message:";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // richTextBoxLastSentMessage
            // 
            this.richTextBoxLastSentMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxLastSentMessage.Location = new System.Drawing.Point(8, 160);
            this.richTextBoxLastSentMessage.Name = "richTextBoxLastSentMessage";
            this.richTextBoxLastSentMessage.ReadOnly = true;
            this.richTextBoxLastSentMessage.Size = new System.Drawing.Size(548, 128);
            this.richTextBoxLastSentMessage.TabIndex = 10;
            this.richTextBoxLastSentMessage.Text = "";
            // 
            // tabPageP2PNetwork
            // 
            this.tabPageP2PNetwork.Controls.Add(this.checkBoxP2PConnected);
            this.tabPageP2PNetwork.Controls.Add(this.textBoxRole);
            this.tabPageP2PNetwork.Controls.Add(this.label16);
            this.tabPageP2PNetwork.Controls.Add(this.label17);
            this.tabPageP2PNetwork.Controls.Add(this.label15);
            this.tabPageP2PNetwork.Controls.Add(this.dataGridPeers);
            this.tabPageP2PNetwork.Controls.Add(this.dataGridPublishedVariables);
            this.tabPageP2PNetwork.Location = new System.Drawing.Point(4, 22);
            this.tabPageP2PNetwork.Name = "tabPageP2PNetwork";
            this.tabPageP2PNetwork.Size = new System.Drawing.Size(564, 294);
            this.tabPageP2PNetwork.TabIndex = 6;
            this.tabPageP2PNetwork.Text = "P2P Network";
            this.tabPageP2PNetwork.UseVisualStyleBackColor = true;
            // 
            // checkBoxP2PConnected
            // 
            this.checkBoxP2PConnected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxP2PConnected.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxP2PConnected.AutoSize = true;
            this.checkBoxP2PConnected.Location = new System.Drawing.Point(8, 265);
            this.checkBoxP2PConnected.Name = "checkBoxP2PConnected";
            this.checkBoxP2PConnected.Size = new System.Drawing.Size(83, 23);
            this.checkBoxP2PConnected.TabIndex = 12;
            this.checkBoxP2PConnected.Text = "Disconnected";
            this.checkBoxP2PConnected.UseVisualStyleBackColor = true;
            this.checkBoxP2PConnected.CheckedChanged += new System.EventHandler(this.checkBoxP2PConnected_CheckedChanged);
            // 
            // textBoxRole
            // 
            this.textBoxRole.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textBoxRole.Location = new System.Drawing.Point(149, 266);
            this.textBoxRole.Name = "textBoxRole";
            this.textBoxRole.ReadOnly = true;
            this.textBoxRole.Size = new System.Drawing.Size(61, 20);
            this.textBoxRole.TabIndex = 26;
            // 
            // label16
            // 
            this.label16.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.Location = new System.Drawing.Point(8, 113);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(135, 13);
            this.label16.TabIndex = 24;
            this.label16.Text = "Published Variables:";
            this.label16.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label17
            // 
            this.label17.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label17.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label17.Location = new System.Drawing.Point(104, 270);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(43, 13);
            this.label17.TabIndex = 24;
            this.label17.Text = "Role:";
            this.label17.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label15
            // 
            this.label15.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label15.Location = new System.Drawing.Point(8, 7);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(135, 13);
            this.label15.TabIndex = 24;
            this.label15.Text = "Peers:";
            this.label15.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dataGridPeers
            // 
            this.dataGridPeers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridPeers.CaptionVisible = false;
            this.dataGridPeers.ContextMenuStrip = this.peerMenuStrip;
            this.dataGridPeers.DataMember = "";
            this.dataGridPeers.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.dataGridPeers.Location = new System.Drawing.Point(8, 23);
            this.dataGridPeers.Name = "dataGridPeers";
            this.dataGridPeers.ReadOnly = true;
            this.dataGridPeers.RowHeadersVisible = false;
            this.dataGridPeers.RowHeaderWidth = 6;
            this.dataGridPeers.Size = new System.Drawing.Size(548, 86);
            this.dataGridPeers.TabIndex = 23;
            this.dataGridPeers.MouseUp += new System.Windows.Forms.MouseEventHandler(this.dataGridPeers_MouseUp);
            this.dataGridPeers.Navigate += new System.Windows.Forms.NavigateEventHandler(this.dataGridPeers_Navigate);
            // 
            // peerMenuStrip
            // 
            this.peerMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.removeSelectedPeerToolStripMenuItem});
            this.peerMenuStrip.Name = "peerMenuStrip";
            this.peerMenuStrip.Size = new System.Drawing.Size(183, 26);
            // 
            // removeSelectedPeerToolStripMenuItem
            // 
            this.removeSelectedPeerToolStripMenuItem.Name = "removeSelectedPeerToolStripMenuItem";
            this.removeSelectedPeerToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.removeSelectedPeerToolStripMenuItem.Text = "Remove Selected Peer";
            this.removeSelectedPeerToolStripMenuItem.Click += new System.EventHandler(this.removeSelectedPeerToolStripMenuItem_Click);
            // 
            // dataGridPublishedVariables
            // 
            this.dataGridPublishedVariables.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridPublishedVariables.CaptionVisible = false;
            this.dataGridPublishedVariables.DataMember = "";
            this.dataGridPublishedVariables.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.dataGridPublishedVariables.Location = new System.Drawing.Point(8, 129);
            this.dataGridPublishedVariables.Name = "dataGridPublishedVariables";
            this.dataGridPublishedVariables.ReadOnly = true;
            this.dataGridPublishedVariables.RowHeadersVisible = false;
            this.dataGridPublishedVariables.RowHeaderWidth = 6;
            this.dataGridPublishedVariables.Size = new System.Drawing.Size(548, 130);
            this.dataGridPublishedVariables.TabIndex = 23;
            // 
            // tabPageSubscriptions
            // 
            this.tabPageSubscriptions.Controls.Add(this.label14);
            this.tabPageSubscriptions.Controls.Add(this.dataGridSubscriptions);
            this.tabPageSubscriptions.Location = new System.Drawing.Point(4, 22);
            this.tabPageSubscriptions.Name = "tabPageSubscriptions";
            this.tabPageSubscriptions.Size = new System.Drawing.Size(564, 294);
            this.tabPageSubscriptions.TabIndex = 5;
            this.tabPageSubscriptions.Text = "Subscriptions";
            this.tabPageSubscriptions.UseVisualStyleBackColor = true;
            // 
            // label14
            // 
            this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.Location = new System.Drawing.Point(8, 7);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(88, 16);
            this.label14.TabIndex = 22;
            this.label14.Text = "Subscriptions:";
            this.label14.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dataGridSubscriptions
            // 
            this.dataGridSubscriptions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridSubscriptions.CaptionVisible = false;
            this.dataGridSubscriptions.ContextMenuStrip = this.subscriptionMenuStrip;
            this.dataGridSubscriptions.DataMember = "";
            this.dataGridSubscriptions.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.dataGridSubscriptions.Location = new System.Drawing.Point(8, 23);
            this.dataGridSubscriptions.Name = "dataGridSubscriptions";
            this.dataGridSubscriptions.ReadOnly = true;
            this.dataGridSubscriptions.RowHeadersVisible = false;
            this.dataGridSubscriptions.RowHeaderWidth = 6;
            this.dataGridSubscriptions.Size = new System.Drawing.Size(548, 264);
            this.dataGridSubscriptions.TabIndex = 21;
            this.dataGridSubscriptions.MouseUp += new System.Windows.Forms.MouseEventHandler(this.dataGridSubscriptions_MouseUp);
            // 
            // subscriptionMenuStrip
            // 
            this.subscriptionMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.removeToolStripMenuItem,
            this.toolStripSeparator1,
            this.removeAllToolStripMenuItem});
            this.subscriptionMenuStrip.Name = "subscriptionMenuStrip";
            this.subscriptionMenuStrip.Size = new System.Drawing.Size(158, 54);
            // 
            // removeToolStripMenuItem
            // 
            this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            this.removeToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.removeToolStripMenuItem.Text = "Remove Selected";
            this.removeToolStripMenuItem.Click += new System.EventHandler(this.removeToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(154, 6);
            // 
            // removeAllToolStripMenuItem
            // 
            this.removeAllToolStripMenuItem.Name = "removeAllToolStripMenuItem";
            this.removeAllToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.removeAllToolStripMenuItem.Text = "Remove All";
            this.removeAllToolStripMenuItem.Click += new System.EventHandler(this.removeAllToolStripMenuItem_Click);
            // 
            // tabPageQueries
            // 
            this.tabPageQueries.Controls.Add(this.label18);
            this.tabPageQueries.Controls.Add(this.dataGridQueries);
            this.tabPageQueries.Location = new System.Drawing.Point(4, 22);
            this.tabPageQueries.Name = "tabPageQueries";
            this.tabPageQueries.Size = new System.Drawing.Size(564, 294);
            this.tabPageQueries.TabIndex = 7;
            this.tabPageQueries.Text = "Queries";
            this.tabPageQueries.UseVisualStyleBackColor = true;
            // 
            // label18
            // 
            this.label18.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label18.Location = new System.Drawing.Point(8, 7);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(88, 16);
            this.label18.TabIndex = 24;
            this.label18.Text = "Queries:";
            this.label18.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dataGridQueries
            // 
            this.dataGridQueries.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridQueries.CaptionVisible = false;
            this.dataGridQueries.ContextMenuStrip = this.queryTableMenuStrip1;
            this.dataGridQueries.DataMember = "";
            this.dataGridQueries.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.dataGridQueries.Location = new System.Drawing.Point(8, 23);
            this.dataGridQueries.Name = "dataGridQueries";
            this.dataGridQueries.ReadOnly = true;
            this.dataGridQueries.RowHeadersVisible = false;
            this.dataGridQueries.RowHeaderWidth = 6;
            this.dataGridQueries.Size = new System.Drawing.Size(548, 264);
            this.dataGridQueries.TabIndex = 23;
            this.dataGridQueries.MouseUp += new System.Windows.Forms.MouseEventHandler(this.dataGridQueries_MouseUp);
            // 
            // queryTableMenuStrip1
            // 
            this.queryTableMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearToolStripMenuItem});
            this.queryTableMenuStrip1.Name = "queryTableMenuStrip1";
            this.queryTableMenuStrip1.Size = new System.Drawing.Size(100, 26);
            // 
            // clearToolStripMenuItem
            // 
            this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            this.clearToolStripMenuItem.Size = new System.Drawing.Size(99, 22);
            this.clearToolStripMenuItem.Text = "Clear";
            this.clearToolStripMenuItem.Click += new System.EventHandler(this.clearToolStripMenuItem_Click);
            // 
            // tabPageSettings
            // 
            this.tabPageSettings.Controls.Add(this.dataGridSettings);
            this.tabPageSettings.Controls.Add(this.label9);
            this.tabPageSettings.Controls.Add(this.linkLabelWebService);
            this.tabPageSettings.Controls.Add(this.buttonSet);
            this.tabPageSettings.Controls.Add(this.label1);
            this.tabPageSettings.Location = new System.Drawing.Point(4, 22);
            this.tabPageSettings.Name = "tabPageSettings";
            this.tabPageSettings.Size = new System.Drawing.Size(564, 294);
            this.tabPageSettings.TabIndex = 0;
            this.tabPageSettings.Text = "Settings";
            this.tabPageSettings.UseVisualStyleBackColor = true;
            // 
            // dataGridSettings
            // 
            this.dataGridSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridSettings.CaptionVisible = false;
            this.dataGridSettings.DataMember = "";
            this.dataGridSettings.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.dataGridSettings.Location = new System.Drawing.Point(8, 24);
            this.dataGridSettings.Name = "dataGridSettings";
            this.dataGridSettings.RowHeadersVisible = false;
            this.dataGridSettings.RowHeaderWidth = 6;
            this.dataGridSettings.Size = new System.Drawing.Size(548, 232);
            this.dataGridSettings.TabIndex = 22;
            // 
            // buttonSet
            // 
            this.buttonSet.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonSet.Location = new System.Drawing.Point(8, 264);
            this.buttonSet.Name = "buttonSet";
            this.buttonSet.Size = new System.Drawing.Size(56, 24);
            this.buttonSet.TabIndex = 9;
            this.buttonSet.Text = "&Set";
            this.buttonSet.Click += new System.EventHandler(this.buttonSet_Click);
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 16);
            this.label1.TabIndex = 7;
            this.label1.Text = "Settings:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tabPageRTLSBeacons
            // 
            this.tabPageRTLSBeacons.Controls.Add(this.SniffGroupBox);
            this.tabPageRTLSBeacons.Controls.Add(this.pictureBox1);
            this.tabPageRTLSBeacons.Controls.Add(this.groupBox2);
            this.tabPageRTLSBeacons.Controls.Add(this.groupBox1);
            this.tabPageRTLSBeacons.Controls.Add(this.label19);
            this.tabPageRTLSBeacons.Controls.Add(this.WakupGroupBox);
            this.tabPageRTLSBeacons.Location = new System.Drawing.Point(4, 22);
            this.tabPageRTLSBeacons.Name = "tabPageRTLSBeacons";
            this.tabPageRTLSBeacons.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageRTLSBeacons.Size = new System.Drawing.Size(564, 294);
            this.tabPageRTLSBeacons.TabIndex = 8;
            this.tabPageRTLSBeacons.Text = "RTLS Beacons";
            this.tabPageRTLSBeacons.UseVisualStyleBackColor = true;
            // 
            // SniffGroupBox
            // 
            this.SniffGroupBox.Controls.Add(this.SnifferButton);
            this.SniffGroupBox.Controls.Add(this.SniffingChannel);
            this.SniffGroupBox.Controls.Add(this.label23);
            this.SniffGroupBox.Controls.Add(this.AwakenBeaconRichTextBox);
            this.SniffGroupBox.Controls.Add(this.label24);
            this.SniffGroupBox.Location = new System.Drawing.Point(207, 50);
            this.SniffGroupBox.Name = "SniffGroupBox";
            this.SniffGroupBox.Size = new System.Drawing.Size(130, 237);
            this.SniffGroupBox.TabIndex = 11;
            this.SniffGroupBox.TabStop = false;
            this.SniffGroupBox.Text = "Sniff";
            // 
            // SnifferButton
            // 
            this.SnifferButton.Location = new System.Drawing.Point(27, 15);
            this.SnifferButton.Name = "SnifferButton";
            this.SnifferButton.Size = new System.Drawing.Size(75, 23);
            this.SnifferButton.TabIndex = 12;
            this.SnifferButton.Text = "Sniff";
            this.SnifferButton.UseVisualStyleBackColor = true;
            this.SnifferButton.Click += new System.EventHandler(this.SnifferButton_Click);
            // 
            // SniffingChannel
            // 
            this.SniffingChannel.AutoSize = true;
            this.SniffingChannel.Location = new System.Drawing.Point(92, 65);
            this.SniffingChannel.Name = "SniffingChannel";
            this.SniffingChannel.Size = new System.Drawing.Size(33, 13);
            this.SniffingChannel.TabIndex = 10;
            this.SniffingChannel.Text = "None";
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(6, 50);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(86, 39);
            this.label23.TabIndex = 7;
            this.label23.Text = "Sniffing Channel\r\n(change it in the \r\n\"settings\" tab)";
            // 
            // AwakenBeaconRichTextBox
            // 
            this.AwakenBeaconRichTextBox.Location = new System.Drawing.Point(24, 124);
            this.AwakenBeaconRichTextBox.Name = "AwakenBeaconRichTextBox";
            this.AwakenBeaconRichTextBox.Size = new System.Drawing.Size(78, 105);
            this.AwakenBeaconRichTextBox.TabIndex = 9;
            this.AwakenBeaconRichTextBox.Text = "None";
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(6, 100);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(91, 13);
            this.label24.TabIndex = 8;
            this.label24.Text = "Awaken Beacons";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(7, 7);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(68, 91);
            this.pictureBox1.TabIndex = 11;
            this.pictureBox1.TabStop = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.DefaultSetting);
            this.groupBox2.Controls.Add(this.ChangeSettingPacketSent);
            this.groupBox2.Controls.Add(this.TxPower31);
            this.groupBox2.Controls.Add(this.TxPower19);
            this.groupBox2.Controls.Add(this.TxPower11);
            this.groupBox2.Controls.Add(this.TxPower7);
            this.groupBox2.Controls.Add(this.TxPower3);
            this.groupBox2.Controls.Add(this.label22);
            this.groupBox2.Controls.Add(this.Channel26);
            this.groupBox2.Controls.Add(this.Channel25);
            this.groupBox2.Controls.Add(this.Channel20);
            this.groupBox2.Controls.Add(this.label21);
            this.groupBox2.Controls.Add(this.Channel15);
            this.groupBox2.Controls.Add(this.ChangeSetting);
            this.groupBox2.Location = new System.Drawing.Point(343, 50);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(218, 237);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Change Setting";
            // 
            // DefaultSetting
            // 
            this.DefaultSetting.Enabled = false;
            this.DefaultSetting.Location = new System.Drawing.Point(134, 15);
            this.DefaultSetting.Name = "DefaultSetting";
            this.DefaultSetting.Size = new System.Drawing.Size(64, 23);
            this.DefaultSetting.TabIndex = 13;
            this.DefaultSetting.Text = "Default";
            this.DefaultSetting.UseVisualStyleBackColor = true;
            this.DefaultSetting.Click += new System.EventHandler(this.DefaultSetting_Click);
            // 
            // ChangeSettingPacketSent
            // 
            this.ChangeSettingPacketSent.AutoSize = true;
            this.ChangeSettingPacketSent.Location = new System.Drawing.Point(6, 216);
            this.ChangeSettingPacketSent.Name = "ChangeSettingPacketSent";
            this.ChangeSettingPacketSent.Size = new System.Drawing.Size(81, 13);
            this.ChangeSettingPacketSent.TabIndex = 12;
            this.ChangeSettingPacketSent.Text = "Packets sent: 0";
            // 
            // TxPower31
            // 
            this.TxPower31.AutoSize = true;
            this.TxPower31.Location = new System.Drawing.Point(99, 148);
            this.TxPower31.Name = "TxPower31";
            this.TxPower31.Size = new System.Drawing.Size(68, 17);
            this.TxPower31.TabIndex = 11;
            this.TxPower31.Text = "0 db (31)";
            this.TxPower31.UseVisualStyleBackColor = true;
            this.TxPower31.CheckedChanged += new System.EventHandler(this.TxPower31_CheckedChanged);
            // 
            // TxPower19
            // 
            this.TxPower19.AutoSize = true;
            this.TxPower19.Location = new System.Drawing.Point(99, 124);
            this.TxPower19.Name = "TxPower19";
            this.TxPower19.Size = new System.Drawing.Size(71, 17);
            this.TxPower19.TabIndex = 10;
            this.TxPower19.Text = "-5 db (19)";
            this.TxPower19.UseVisualStyleBackColor = true;
            this.TxPower19.CheckedChanged += new System.EventHandler(this.TxPower19_CheckedChanged);
            // 
            // TxPower11
            // 
            this.TxPower11.AutoSize = true;
            this.TxPower11.Checked = true;
            this.TxPower11.CheckState = System.Windows.Forms.CheckState.Checked;
            this.TxPower11.Location = new System.Drawing.Point(10, 174);
            this.TxPower11.Name = "TxPower11";
            this.TxPower11.Size = new System.Drawing.Size(77, 17);
            this.TxPower11.TabIndex = 9;
            this.TxPower11.Text = "-10 db (11)";
            this.TxPower11.UseVisualStyleBackColor = true;
            this.TxPower11.CheckedChanged += new System.EventHandler(this.TxPower11_CheckedChanged);
            // 
            // TxPower7
            // 
            this.TxPower7.AutoSize = true;
            this.TxPower7.Checked = true;
            this.TxPower7.CheckState = System.Windows.Forms.CheckState.Checked;
            this.TxPower7.Location = new System.Drawing.Point(10, 150);
            this.TxPower7.Name = "TxPower7";
            this.TxPower7.Size = new System.Drawing.Size(71, 17);
            this.TxPower7.TabIndex = 8;
            this.TxPower7.Text = "-15 db (7)";
            this.TxPower7.UseVisualStyleBackColor = true;
            this.TxPower7.CheckedChanged += new System.EventHandler(this.TxPower7_CheckedChanged);
            // 
            // TxPower3
            // 
            this.TxPower3.AutoSize = true;
            this.TxPower3.Checked = true;
            this.TxPower3.CheckState = System.Windows.Forms.CheckState.Checked;
            this.TxPower3.Location = new System.Drawing.Point(10, 126);
            this.TxPower3.Name = "TxPower3";
            this.TxPower3.Size = new System.Drawing.Size(71, 17);
            this.TxPower3.TabIndex = 7;
            this.TxPower3.Text = "-25 db (3)";
            this.TxPower3.UseVisualStyleBackColor = true;
            this.TxPower3.CheckedChanged += new System.EventHandler(this.TxPower3_CheckedChanged);
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(7, 107);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(52, 13);
            this.label22.TabIndex = 6;
            this.label22.Text = "TxPower ";
            // 
            // Channel26
            // 
            this.Channel26.AutoSize = true;
            this.Channel26.Location = new System.Drawing.Point(143, 76);
            this.Channel26.Name = "Channel26";
            this.Channel26.Size = new System.Drawing.Size(38, 17);
            this.Channel26.TabIndex = 5;
            this.Channel26.Text = "26";
            this.Channel26.UseVisualStyleBackColor = true;
            this.Channel26.CheckedChanged += new System.EventHandler(this.Channel26_CheckedChanged);
            // 
            // Channel25
            // 
            this.Channel25.AutoSize = true;
            this.Channel25.Location = new System.Drawing.Point(99, 76);
            this.Channel25.Name = "Channel25";
            this.Channel25.Size = new System.Drawing.Size(38, 17);
            this.Channel25.TabIndex = 4;
            this.Channel25.Text = "25";
            this.Channel25.UseVisualStyleBackColor = true;
            this.Channel25.CheckedChanged += new System.EventHandler(this.Channel25_CheckedChanged);
            // 
            // Channel20
            // 
            this.Channel20.AutoSize = true;
            this.Channel20.Location = new System.Drawing.Point(54, 76);
            this.Channel20.Name = "Channel20";
            this.Channel20.Size = new System.Drawing.Size(38, 17);
            this.Channel20.TabIndex = 3;
            this.Channel20.Text = "20";
            this.Channel20.UseVisualStyleBackColor = true;
            this.Channel20.CheckedChanged += new System.EventHandler(this.Channel20_CheckedChanged);
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(7, 50);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(51, 13);
            this.label21.TabIndex = 2;
            this.label21.Text = "Channels";
            // 
            // Channel15
            // 
            this.Channel15.AutoSize = true;
            this.Channel15.Checked = true;
            this.Channel15.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Channel15.Location = new System.Drawing.Point(10, 76);
            this.Channel15.Name = "Channel15";
            this.Channel15.Size = new System.Drawing.Size(38, 17);
            this.Channel15.TabIndex = 1;
            this.Channel15.Text = "15";
            this.Channel15.UseVisualStyleBackColor = true;
            this.Channel15.CheckedChanged += new System.EventHandler(this.Channel15_CheckedChanged);
            // 
            // ChangeSetting
            // 
            this.ChangeSetting.Location = new System.Drawing.Point(6, 15);
            this.ChangeSetting.Name = "ChangeSetting";
            this.ChangeSetting.Size = new System.Drawing.Size(75, 23);
            this.ChangeSetting.TabIndex = 0;
            this.ChangeSetting.Text = "Change";
            this.toolTipChangeSetting.SetToolTip(this.ChangeSetting, "Important notice: the configuration of the beacons\n must be the same as the confi" +
                    "guration that\n hardcoded in the Walker mote. Otherwise, the Walker can\'t submit " +
                    "\n the position message!");
            this.ChangeSetting.UseVisualStyleBackColor = true;
            this.ChangeSetting.Click += new System.EventHandler(this.ChangeSetting_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.SleepModeComboBox);
            this.groupBox1.Controls.Add(this.SleepMoteLabel);
            this.groupBox1.Controls.Add(this.DefaultSleepSetting);
            this.groupBox1.Controls.Add(this.SleepStatus);
            this.groupBox1.Controls.Add(this.ListenTimeDuringSleep);
            this.groupBox1.Controls.Add(this.ListenTime);
            this.groupBox1.Controls.Add(this.SleepTime);
            this.groupBox1.Controls.Add(this.label20);
            this.groupBox1.Controls.Add(this.BeaconSleep);
            this.groupBox1.Location = new System.Drawing.Point(3, 115);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(198, 172);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Sleep Setting";
            // 
            // SleepModeComboBox
            // 
            this.SleepModeComboBox.FormattingEnabled = true;
            this.SleepModeComboBox.Items.AddRange(new object[] {
            "1: on CPU, off radio"});
            this.SleepModeComboBox.Location = new System.Drawing.Point(120, 129);
            this.SleepModeComboBox.Name = "SleepModeComboBox";
            this.SleepModeComboBox.Size = new System.Drawing.Size(71, 21);
            this.SleepModeComboBox.TabIndex = 10;
            // 
            // SleepMoteLabel
            // 
            this.SleepMoteLabel.AutoSize = true;
            this.SleepMoteLabel.Location = new System.Drawing.Point(3, 129);
            this.SleepMoteLabel.Name = "SleepMoteLabel";
            this.SleepMoteLabel.Size = new System.Drawing.Size(111, 13);
            this.SleepMoteLabel.TabIndex = 9;
            this.SleepMoteLabel.Text = "Sleep Mote (reserved)";
            // 
            // DefaultSleepSetting
            // 
            this.DefaultSleepSetting.Location = new System.Drawing.Point(116, 25);
            this.DefaultSleepSetting.Name = "DefaultSleepSetting";
            this.DefaultSleepSetting.Size = new System.Drawing.Size(75, 23);
            this.DefaultSleepSetting.TabIndex = 8;
            this.DefaultSleepSetting.Text = "Default";
            this.DefaultSleepSetting.UseVisualStyleBackColor = true;
            this.DefaultSleepSetting.Click += new System.EventHandler(this.DefaultSleepSetting_Click);
            // 
            // SleepStatus
            // 
            this.SleepStatus.AutoSize = true;
            this.SleepStatus.Location = new System.Drawing.Point(6, 151);
            this.SleepStatus.Name = "SleepStatus";
            this.SleepStatus.Size = new System.Drawing.Size(81, 13);
            this.SleepStatus.TabIndex = 7;
            this.SleepStatus.Text = "Packets sent: 0";
            // 
            // ListenTimeDuringSleep
            // 
            this.ListenTimeDuringSleep.Location = new System.Drawing.Point(106, 97);
            this.ListenTimeDuringSleep.Name = "ListenTimeDuringSleep";
            this.ListenTimeDuringSleep.Size = new System.Drawing.Size(85, 20);
            this.ListenTimeDuringSleep.TabIndex = 6;
            this.ListenTimeDuringSleep.Text = "10";
            this.ListenTimeDuringSleep.TextChanged += new System.EventHandler(this.ListenTimeDuringSleep_TextChanged);
            // 
            // ListenTime
            // 
            this.ListenTime.AutoSize = true;
            this.ListenTime.Location = new System.Drawing.Point(9, 97);
            this.ListenTime.Name = "ListenTime";
            this.ListenTime.Size = new System.Drawing.Size(83, 13);
            this.ListenTime.TabIndex = 5;
            this.ListenTime.Text = "Listen time (sec)";
            // 
            // SleepTime
            // 
            this.SleepTime.Location = new System.Drawing.Point(106, 62);
            this.SleepTime.Name = "SleepTime";
            this.SleepTime.Size = new System.Drawing.Size(85, 20);
            this.SleepTime.TabIndex = 4;
            this.SleepTime.Text = "600";
            this.SleepTime.TextChanged += new System.EventHandler(this.SleepTime_TextChanged);
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(6, 65);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(82, 13);
            this.label20.TabIndex = 3;
            this.label20.Text = "Sleep time (sec)";
            // 
            // BeaconSleep
            // 
            this.BeaconSleep.Location = new System.Drawing.Point(19, 25);
            this.BeaconSleep.Name = "BeaconSleep";
            this.BeaconSleep.Size = new System.Drawing.Size(75, 23);
            this.BeaconSleep.TabIndex = 2;
            this.BeaconSleep.Text = "Sleep";
            this.BeaconSleep.UseVisualStyleBackColor = true;
            this.BeaconSleep.Click += new System.EventHandler(this.BeaconSleep_Click);
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label19.ForeColor = System.Drawing.SystemColors.MenuText;
            this.label19.Location = new System.Drawing.Point(76, 7);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(475, 26);
            this.label19.TabIndex = 0;
            this.label19.Text = "This page controls the beacons of the RTLS";
            // 
            // WakupGroupBox
            // 
            this.WakupGroupBox.Controls.Add(this.BeaconWakeup);
            this.WakupGroupBox.Controls.Add(this.WakeupStatus);
            this.WakupGroupBox.Location = new System.Drawing.Point(88, 50);
            this.WakupGroupBox.Name = "WakupGroupBox";
            this.WakupGroupBox.Size = new System.Drawing.Size(113, 64);
            this.WakupGroupBox.TabIndex = 10;
            this.WakupGroupBox.TabStop = false;
            this.WakupGroupBox.Text = "Wakeup";
            // 
            // BeaconWakeup
            // 
            this.BeaconWakeup.Location = new System.Drawing.Point(9, 14);
            this.BeaconWakeup.Name = "BeaconWakeup";
            this.BeaconWakeup.Size = new System.Drawing.Size(75, 23);
            this.BeaconWakeup.TabIndex = 1;
            this.BeaconWakeup.Text = "Wakeup";
            this.BeaconWakeup.UseVisualStyleBackColor = true;
            this.BeaconWakeup.Click += new System.EventHandler(this.BeaconWakeup_Click);
            // 
            // WakeupStatus
            // 
            this.WakeupStatus.AutoSize = true;
            this.WakeupStatus.Location = new System.Drawing.Point(6, 44);
            this.WakeupStatus.Name = "WakeupStatus";
            this.WakeupStatus.Size = new System.Drawing.Size(81, 13);
            this.WakeupStatus.TabIndex = 5;
            this.WakeupStatus.Text = "Packets sent: 0";
            // 
            // textBoxStatus
            // 
            this.textBoxStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxStatus.Location = new System.Drawing.Point(8, 339);
            this.textBoxStatus.Name = "textBoxStatus";
            this.textBoxStatus.ReadOnly = true;
            this.textBoxStatus.Size = new System.Drawing.Size(510, 20);
            this.textBoxStatus.TabIndex = 11;
            this.textBoxStatus.Text = "Ready.";
            // 
            // timerTester
            // 
            this.timerTester.Interval = 1;
            this.timerTester.Tick += new System.EventHandler(this.timerTester_Tick);
            // 
            // timerDataGridUpdater
            // 
            this.timerDataGridUpdater.Interval = 250;
            this.timerDataGridUpdater.Tick += new System.EventHandler(this.timerDataGridUpdater_Tick);
            // 
            // BeaconControlMsgTimer
            // 
            this.BeaconControlMsgTimer.Interval = 1000;
            this.BeaconControlMsgTimer.Tick += new System.EventHandler(this.BeaconControlMsgTimer_Tick);
            // 
            // SnifferResetTimer
            // 
            this.SnifferResetTimer.Interval = 2000;
            this.SnifferResetTimer.Tick += new System.EventHandler(this.SnifferResetTimer_Tick);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(592, 366);
            this.Controls.Add(this.tabControlSettings);
            this.Controls.Add(this.buttonClose);
            this.Controls.Add(this.textBoxStatus);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(600, 400);
            this.Name = "MainForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DataAcquisitionService";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.MainForm_Closing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.tabControlSettings.ResumeLayout(false);
            this.tabPageSensors.ResumeLayout(false);
            this.tabPageSensors.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridSensors)).EndInit();
            this.tabPageCluster.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.dataTableMenuStrip.ResumeLayout(false);
            this.tabPageData.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridVariables)).EndInit();
            this.tabPageReceived.ResumeLayout(false);
            this.tabPageSent.ResumeLayout(false);
            this.tabPageP2PNetwork.ResumeLayout(false);
            this.tabPageP2PNetwork.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridPeers)).EndInit();
            this.peerMenuStrip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridPublishedVariables)).EndInit();
            this.tabPageSubscriptions.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridSubscriptions)).EndInit();
            this.subscriptionMenuStrip.ResumeLayout(false);
            this.tabPageQueries.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridQueries)).EndInit();
            this.queryTableMenuStrip1.ResumeLayout(false);
            this.tabPageSettings.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridSettings)).EndInit();
            this.tabPageRTLSBeacons.ResumeLayout(false);
            this.tabPageRTLSBeacons.PerformLayout();
            this.SniffGroupBox.ResumeLayout(false);
            this.SniffGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.WakupGroupBox.ResumeLayout(false);
            this.WakupGroupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		private void OnLinkClick(Object sender, LinkLabelLinkClickedEventArgs e)
		{
			try
			{
				linkLabelWebService.Links[linkLabelWebService.Links.IndexOf(e.Link)].Visited = true;
				System.Diagnostics.Process.Start(linkLabelWebService.Text);
			}
			catch (Exception exp)
			{
				Console.WriteLine(exp);
			}
		}

		private void MainForm_Load(object sender, System.EventArgs e)
		{

			// init main service
			mainService = new MainService();
			mainService.Init(this);
			global = mainService.global;

			// prepare link
			linkLabelWebService.Text = "http://" + System.Environment.MachineName + ":" + global.portNumber + global.sVirtRoot + global.sASMX;

			// init DB access
			dbAccess = new DBAccess("DataAcquisitionService.mdb");
			// get snapshot of archive
            tblArchiveSnapshot = dbAccess.GetTable("tblArchive");

			// prepare grids
			dataGridSensors.CustomInit(this, mainService.tblSensors, null, null);
			dataGridVariables.CustomInit(this, tblArchiveSnapshot, null, null);
			dataGridSubscriptions.CustomInit(this, mainService.tblSubscriptions, null, null);
			dataGridQueries.CustomInit(this, mainService.tblQueries, null, null);
			dataGridSettings.CustomInit(this, mainService.tblSettings, null, null);
			dataGridPeers.CustomInit(this, mainService.tblPeers, null, null);
			dataGridPublishedVariables.CustomInit(this, mainService.tblPublishedVariables, null, null);

			// try to connect to P2P network
			checkBoxP2PConnected.Checked = true;

			if (mainService.bActivateCommunicationUponStart)
			{
				// try to start communication with motes
				checkBoxCom.Checked = true;
			}
            
			// form is completely loaded
			bFormLoaded = true;

		}

		private void buttonClose_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}

		private void buttonSet_Click(object sender, System.EventArgs e)
		{
			try
			{
				// update settings
				if (mainService.UpdateSettings())
				{
					// store settings
					mainService.settings.StoreToDB();
				}

				PrintStatus("Settings changed...");
			}
			catch (Exception exp)
			{
				Console.WriteLine(exp);
				PrintStatus("Error: Could not store settings to the DB. Exception: " + exp.Message);
				ShowMessageBox("Error: Could not store settings to the DB. Exception: " + exp.Message, MessageBoxIcon.Error);
			}
		}

		private void checkBoxCom_CheckedChanged(object sender, System.EventArgs e)
		{
			if (checkBoxCom.Checked)
			{
				checkBoxCom.Text = "On";
				colorButton = checkBoxCom.BackColor;
				checkBoxCom.BackColor = Color.DarkSeaGreen;
				checkBoxCom.Refresh();

				if (!mainService.Start())
				{
					checkBoxCom.Checked = false;
					return;
				}

				// clear table
				tblArchiveSnapshot.Clear();

				// start updating of grid
				timerDataGridUpdater.Start();

				if (mainService.bDemoMode)
				{
					// just for test purposes (fast generating of messages)
					timerTester.Interval = mainService.nDemoModeInterval;
					timerTester.Start();
				}
			}
			else
			{
				// stop updating of grid
				timerDataGridUpdater.Stop();


				if (timerTester.Enabled)
				{
					// just for test purposes (fast generating of messages)
					timerTester.Stop();
				}

				// stop service
				mainService.Stop();
				checkBoxCom.Text = "Off";
				checkBoxCom.BackColor = colorButton;
			}
		}

		[DllImport("user32.dll")]
		private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);
		// scroll windows message
		const int WM_VSCROLL = 0x115;
		const int SB_BOTTOM = 1;

		public void DisplayByte(byte b, RichTextBox richTextBox)
		{
			// return if we are closing application
			if (mainService.bClosing) return;

			try
			{
				if (this.InvokeRequired)
				{
					// this is not main/GUI thread => do invoke from main thread
					DisplayByteDel d = new DisplayByteDel(DisplayByte);
					this.BeginInvoke(d, new object[] { b, richTextBox });
				}
				else
				{
					// display only if particular tab page is selected
					if (tabControlSettings.SelectedTab == (TabPage)richTextBox.Parent)
					{
						// get hexa string
						string sByte = b.ToString("X2");

						if (richTextBox.Text.Length > 10000)
						{
							// delete the text if too long
							richTextBox.Text = "";
						}

						richTextBox.AppendText(" " + sByte);
						// send scroll windows message
						SendMessage(richTextBox.Handle, WM_VSCROLL, SB_BOTTOM, new IntPtr(0));
					}
				}
			}
			catch (Exception exp)
			{
				Console.WriteLine(exp);
			}
		}

        //Display a string in a given richtextbox
        public void DisplayStringTextBox( string sMsg, RichTextBox richTextBox ) {

			if (this.InvokeRequired)
			{
				DisplayStringTextBoxDel dString = new DisplayStringTextBoxDel(DisplayStringTextBox);
				this.BeginInvoke(dString, new object[] { sMsg, richTextBox });
			}
			else
			{

				if (richTextBox.Text.Length > 10000)
				{
					// delete the text if too long
					richTextBox.Text = "";
				}
				if (tabControlSettings.SelectedTab == (TabPage)richTextBox.Parent)
				{
					richTextBox.AppendText(sMsg + "\n");
				}
			}		
        
        }

		public void DisplayMsg(TOS_Msg msg, RichTextBox richTextBox)
		{
			// return if we are closing application
			if (mainService.bClosing) return;

			try
			{
				if (this.InvokeRequired)
				{
					// this is not main/GUI thread => do invoke from main thread
					DisplayMsgDel d = new DisplayMsgDel(DisplayMsg);
					this.BeginInvoke(d, new object[] { msg, richTextBox });
				}
				else
				{
					// display only if particular tab page is selected
					if (tabControlSettings.SelectedTab == (TabPage)richTextBox.Parent)
					{
						string sMsg = String.Format("PacketType={0}, Prefix={1}, MessageType={2}, Address={3}, GroupID={4}, Data:", msg.PacketType, msg.Prefix, msg.MessageType.ToString("X2"), msg.Address.ToString("X2"), msg.GroupID.ToString("X2"));
						string sData = "";
						bool bFirstTime = true;

						if (msg.Data != null)
						{
							foreach (byte b in msg.Data)
							{
								if (!bFirstTime)
								{
									sData += ", ";
								}
								else
								{
									bFirstTime = false;
								}
								sData += b.ToString("X2");
							}
						}

						if (richTextBox.Text.Length > 10000)
						{
							// delete the text if too long
							richTextBox.Text = "";
						}

						if (richTextBox.Text != "")
						{
							richTextBox.AppendText("######## Message sent/received on " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + " ########\n");
						}

						richTextBox.AppendText(sMsg + "\n");
						richTextBox.AppendText("{" + sData + "}\n");

						// send scroll windows message
						SendMessage(richTextBox.Handle, WM_VSCROLL, SB_BOTTOM, new IntPtr(0));
					}
				}
			}
			catch (Exception exp)
			{
				Console.WriteLine(exp);
			}
		}

		public void PrintStatus(string sStatus)
		{
			if (this.InvokeRequired)
			{
				// this is not main/GUI thread => do invoke from main thread
				PrintStatusDel d = new PrintStatusDel(PrintStatus);
				this.BeginInvoke(d, new object[] { sStatus });
			}
			else
			{
				textBoxStatus.Text = sStatus;
			}
		}

		public void PrintMsgStatus(long lReceivedMsg, long lSentMsg, long lProcessedMsg, long lToDoMsg)
		{
			if (this.InvokeRequired)
			{
				// this is not main/GUI thread => do invoke from main thread
				PrintMsgStatusDel d = new PrintMsgStatusDel(PrintMsgStatus);
				this.BeginInvoke(d, new object[] { lReceivedMsg, lSentMsg, lProcessedMsg, lToDoMsg });
			}
			else
			{
				if (lReceivedMsg >= 0)
					textBoxReceivedMessages.Text = lReceivedMsg.ToString();
				if (lSentMsg >= 0)
					textBoxSentMessages.Text = lSentMsg.ToString();
				if (lProcessedMsg >= 0)
					textBoxProcessedMessages.Text = lProcessedMsg.ToString();
				if (lToDoMsg >= 0)
					textBoxToDoMessages.Text = lToDoMsg.ToString();
			}
		}

		private void MainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (mainService.tblSettings.DataSet.HasChanges() || mainService.tblSensors.DataSet.HasChanges())
			{
				DialogResult dlgRes = MessageBox.Show(
					"Do you want to store changes to the database?",
					"Warning",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Question);

				if (dlgRes == DialogResult.Yes)
				{
					buttonSet_Click(null, null);
					e.Cancel = false;
				}
				else if (dlgRes == DialogResult.No)
				{
					e.Cancel = false;
				}
				else
				{
					// cancel case
					e.Cancel = true;
					return;
				}
			}

			// close the service
			mainService.Close();
		}

		public void ShowMessageBox(string sMessage, MessageBoxIcon icon)
		{
			if (bFormLoaded)
			{
				string sTitle = "Info";
				switch (icon)
				{
					case MessageBoxIcon.Error:
						sTitle = "Error";
						break;
					case MessageBoxIcon.Warning:
						sTitle = "Warning";
						break;
				}
				MessageBox.Show(this, sMessage, sTitle, MessageBoxButtons.OK, icon);
			}
		}

		public delegate void VariableRowDeletedDel(DataRow oldRow);

		public void VariableRowDeleted (DataRow oldRow)
		{

			if (this.InvokeRequired)
			{
				VariableRowDeletedDel dVariableRowDeleted = new VariableRowDeletedDel(VariableRowDeleted);
				this.Invoke(dVariableRowDeleted, new object[] { oldRow });
			}
			else
			{
				lock (mainService.tblPublishedVariables)
				{
					mainService.tblPublishedVariables.Rows.Remove(oldRow);
				}
			}

			dataGridPublishedVariables.CurrentRowIndex = mainService.tblPublishedVariables.Rows.Count - 1;

		}



		public delegate void VariableRowAddedDel(DataRow newRow);

		public void VariableRowAdded(DataRow newRow)
		{

			if (this.InvokeRequired)
			{
				VariableRowAddedDel dVariableRowAdded = new VariableRowAddedDel(VariableRowAdded);
				this.Invoke(dVariableRowAdded, new object[] { newRow });
			}
			else
			{
				lock(mainService.tblPublishedVariables){
					mainService.tblPublishedVariables.Rows.Add(newRow);
				}
			}

			dataGridPublishedVariables.CurrentRowIndex = mainService.tblPublishedVariables.Rows.Count - 1;

		}



		public delegate void QueryRowAddedDel(DataRow newRow);

		public void QueryRowAdded(DataRow newRow)
		{

			if (this.InvokeRequired)
			{
				QueryRowAddedDel dQueryRowAdded = new QueryRowAddedDel(QueryRowAdded);
				this.Invoke(dQueryRowAdded, new object[] { newRow });
			}
			else
			{
				mainService.tblQueries.Rows.Add(newRow);
			}

			dataGridQueries.CurrentRowIndex = mainService.tblQueries.Rows.Count -1;

		}


		private void timerTester_Tick(object sender, System.EventArgs e)
		{
			Telos_Msg msg = new Telos_Msg();
           
			byte[] data = new byte[4];
			(new System.Random()).NextBytes(data);

            //added for testing purposes so weight values would be much lower
            data[1] = 5;

			data[2] = (byte)(new System.Random()).Next(6);
			data[3] = 0;
			byte prefix = (byte)(new System.Random()).Next(255);
			msg.Init(PacketTypes.P_PACKET_ACK, prefix, MessageTypes.WeigthScaleMessage, MoteAddresses.BroadcastAddress, MoteGroups.DefaultGroup, data);
			mainService.MsgReceived(msg);
		}

		private void timerDataGridUpdater_Tick(object sender, System.EventArgs e)
		{

			if (mainService.bGUI == false)
			{

				return;
			}

			try
			{
				//tblArchiveSnapshot.Clear();
				int nCount = mainService.queueValueUpdates.Count;
				string lastVariableChanged = "";
				string lastSensorIDChanged = "";

//				Object[] o = mainService.queueValueUpdates.ToArray();

//				MyComparerClass comparer = new MyComparerClass();

				//Array.Sort( o, 0, o.Length, comparer);
                
				while (nCount > 0)
				{
					

					// dequeue the changed row from worker thread
					DataRow rowChanged = (DataRow) mainService.queueValueUpdates.Dequeue();// (DataRow)o[nCount - 1];

					if (rowChanged.RowState != DataRowState.Detached) //This row might have been removed already.
					{

						if (lastVariableChanged.CompareTo(rowChanged["Variable"].ToString()) != 0 && lastSensorIDChanged.CompareTo(rowChanged["SensorID"].ToString()) != 0)
						{

							string sStatement = String.Format("SensorID='{0}' AND Variable='{1}'", rowChanged["SensorID"].ToString(), rowChanged["Variable"].ToString());
							DataRow[] rows = tblArchiveSnapshot.Select(sStatement);

							foreach (DataRow dr in rows)
							{
								tblArchiveSnapshot.Rows.Remove(dr);
							}

							lastVariableChanged = rowChanged["Variable"].ToString();
							lastSensorIDChanged = rowChanged["SensorID"].ToString();

						}

						object[] values = new object[rowChanged.ItemArray.GetLength(0)];
						rowChanged.ItemArray.CopyTo(values, 0);

						// update snapshot of archive
						tblArchiveSnapshot.LoadDataRow(values, true);

						// decrement count
						nCount--;
					}
					else {
						nCount--;					
					}
				}

                List<DataRow> rowsToBeKept = new List<DataRow>();
                foreach (DataRow row in tblArchiveSnapshot.Rows)
                {
                    bool addNewRow = true;
                    for(int rowIter = 0; rowIter < rowsToBeKept.Count; rowIter++)
                    {
                        if ((string)row["Variable"] == (string)rowsToBeKept[rowIter]["Variable"] && (string)row["SensorID"] == (string)rowsToBeKept[rowIter]["SensorID"])
                        {
                            addNewRow = false;
                            if (long.Parse((string)row["Time"]) > long.Parse((string)rowsToBeKept[rowIter]["Time"]))
                            {
                                rowsToBeKept[rowIter] = row;
                            }
                            continue;
                        }
                    }
                    if (addNewRow)
                    {
                        rowsToBeKept.Add(row);
                    }
                }
               
                List<DataRow> rowsToBeDeleted = new List<DataRow>();
                foreach (DataRow row in tblArchiveSnapshot.Rows)
                {
                    if (!rowsToBeKept.Contains(row))
                    {
                        rowsToBeDeleted.Add(row);
                    }
                }

                foreach (DataRow row in rowsToBeDeleted)
                {
                    tblArchiveSnapshot.Rows.Remove(row);
                }

				// refresh grid
				dataGridVariables.ResizeColumns(ColumnResizeType.FitToContent);
			}
			catch (Exception exp)
			{
				Console.WriteLine(exp);
			}
		}

		private void checkBoxP2PConnected_CheckedChanged(object sender, EventArgs e)
		{
			if (checkBoxP2PConnected.Checked)
			{
				checkBoxP2PConnected.Text = "Connected";
				colorButton = checkBoxP2PConnected.BackColor;
				checkBoxP2PConnected.BackColor = Color.DarkSeaGreen;
				checkBoxP2PConnected.Refresh();

				// try to connect to P2P network
				if (!mainService.ConnectP2PNetwork())
				{
					checkBoxP2PConnected.Checked = false;
					return;
				}

				if (global.bP2PMaster)
				{
					textBoxRole.Text = "Master";
					textBoxRole.BackColor = Color.Cornsilk;
				}
				else
				{
					textBoxRole.Text = "Peer";
					textBoxRole.BackColor = colorButton;
				}
			}
			else
			{
				// disconnect from P2P network
				if (mainService.DisconnectP2PNetwork())
				{
					checkBoxP2PConnected.Text = "Disconnected";
					checkBoxP2PConnected.BackColor = colorButton;
					textBoxRole.Text = "";
					textBoxRole.BackColor = colorButton;
				}
			}

		}

		//Clearing the query grid
		private void clearToolStripMenuItem_Click(object sender, EventArgs e)
		{

			mainService.tblQueries.Clear();
			dataGridQueries.Refresh();

		}

		//Hard remove of the subscriptions
		private void removeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DataRow currentRow = mainService.tblSubscriptions.Rows[dataGridSubscriptions.CurrentRowIndex];
			global.RemoveSubscriber(currentRow["GUID"].ToString());

		}


		private void dataGridVariables_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e) {


			System.Drawing.Point pt = new Point(e.X, e.Y);
			DataGrid.HitTestInfo hti = dataGridVariables.HitTest(pt);
			if (hti.Type == DataGrid.HitTestType.Cell)
			{
				dataGridVariables.CurrentCell = new DataGridCell(hti.Row, hti.Column);
				dataGridVariables.Select(hti.Row);
			}
			else
			{

				if (dataGridVariables.CurrentRowIndex >= 0)
				{
					dataGridVariables.UnSelect(dataGridVariables.CurrentRowIndex);
				}
			}
		}

		private void dataGridQueries_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			

			if (dataGridQueries.CurrentRowIndex >= 0)
			{
				clearToolStripMenuItem.Enabled = true;
			}
			else
			{
				clearToolStripMenuItem.Enabled = false;
			}

		}

		private void dataGridPeers_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{


			if (dataGridPeers.CurrentRowIndex >= 0 && dataGridPeers.IsSelected(dataGridPeers.CurrentRowIndex))
			{
				removeSelectedPeerToolStripMenuItem.Enabled = true;
			}
			else
			{
				removeSelectedPeerToolStripMenuItem.Enabled = false;
			}


			System.Drawing.Point pt = new Point(e.X, e.Y);
			DataGrid.HitTestInfo hti = dataGridPeers.HitTest(pt);
			if (hti.Type == DataGrid.HitTestType.Cell)
			{
				dataGridPeers.CurrentCell = new DataGridCell(hti.Row, hti.Column);
				dataGridPeers.Select(hti.Row);
			}
			else
			{

				if (dataGridPeers.CurrentRowIndex >= 0)
				{
					dataGridPeers.UnSelect(dataGridPeers.CurrentRowIndex);
				}
			}
		}


		//Selecting thesubscription row.
		private void dataGridSubscriptions_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (dataGridSubscriptions.CurrentRowIndex >= 0 && dataGridSubscriptions.IsSelected(dataGridSubscriptions.CurrentRowIndex))
			{
				removeToolStripMenuItem.Enabled = true;
			}
			else
			{
				removeToolStripMenuItem.Enabled = false;
			}

			if (dataGridSubscriptions.VisibleRowCount > 0)
			{
				removeAllToolStripMenuItem.Enabled = true;
			}
			else
			{
				removeAllToolStripMenuItem.Enabled = false;			
			}


			System.Drawing.Point pt = new Point(e.X, e.Y);
			DataGrid.HitTestInfo hti = dataGridSubscriptions.HitTest(pt);
			if (hti.Type == DataGrid.HitTestType.Cell)
			{
				dataGridSubscriptions.CurrentCell = new DataGridCell(hti.Row, hti.Column);
				dataGridSubscriptions.Select(hti.Row);
			}
			else
			{

				if (dataGridSubscriptions.CurrentRowIndex >= 0)
				{
					dataGridSubscriptions.UnSelect(dataGridSubscriptions.CurrentRowIndex);
				}
			}
		}

		private void removeSelectedPeerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DataRow currentRow = mainService.tblPeers.Rows[dataGridPeers.CurrentRowIndex];

			if (currentRow["Name"].ToString() == System.Environment.MachineName)
			{
				MessageBox.Show("Error!! Can not remove myself!!", "Master Peer Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			else
			{
				global.UnregisterPeer(currentRow["Name"].ToString());
			}
		}

		private void removeAllToolStripMenuItem_Click(object sender, EventArgs e)
		{

			while (mainService.tblSubscriptions.Rows.Count > 0)
			{
				DataRow currentRow = mainService.tblSubscriptions.Rows[0];
				global.RemoveSubscriber(currentRow["GUID"].ToString());
			}
		}
        private void DefaultSleepSetting_Click(object sender, EventArgs e)
        {
            // dbg setting
            // for real use: thinking to use sleep 600(s) and wake 10(s)
            SleepTime.Text = "5";
            ListenTimeDuringSleep.Text = "5";
            DefaultSleepSetting.Enabled = false;
        }

        private void ListenTimeDuringSleep_TextChanged(object sender, EventArgs e)
        {
            DefaultSleepSetting.Enabled = true;
        }

        private void SleepTime_TextChanged(object sender, EventArgs e)
        {
            DefaultSleepSetting.Enabled = true;
        }

        private void DefaultSetting_Click(object sender, EventArgs e)
        {
            Channel15.Checked = true;
            Channel20.Checked = false;
            Channel25.Checked = false;
            Channel26.Checked = false;

            TxPower3.Checked = true;
            TxPower7.Checked = true;
            TxPower11.Checked = true;
            TxPower19.Checked = false;
            TxPower31.Checked = false;

            DefaultSetting.Enabled = false;

        }

        private void Channel15_CheckedChanged(object sender, EventArgs e)
        {
            DefaultSetting.Enabled = true;
        }

        private void Channel20_CheckedChanged(object sender, EventArgs e)
        {
            DefaultSetting.Enabled = true;
        }

        private void Channel25_CheckedChanged(object sender, EventArgs e)
        {
            DefaultSetting.Enabled = true;
        }

        private void Channel26_CheckedChanged(object sender, EventArgs e)
        {
            DefaultSetting.Enabled = true;
        }

        private void TxPower3_CheckedChanged(object sender, EventArgs e)
        {
            DefaultSetting.Enabled = true;
        }

        private void TxPower7_CheckedChanged(object sender, EventArgs e)
        {
            DefaultSetting.Enabled = true;
        }

        private void TxPower11_CheckedChanged(object sender, EventArgs e)
        {
            DefaultSetting.Enabled = true;
        }

        private void TxPower19_CheckedChanged(object sender, EventArgs e)
        {
            DefaultSetting.Enabled = true;
        }

        private void TxPower31_CheckedChanged(object sender, EventArgs e)
        {
            DefaultSetting.Enabled = true;
        }

        private void ChangeSetting_Click(object sender, EventArgs e)
        {
            ushort tmp;
            if (ChangeSetting.Text == "Change")
            {
                if (!Channel15.Checked &&
                    !Channel20.Checked &&
                    !Channel25.Checked &&
                    !Channel26.Checked)
                {
                    MessageBox.Show("You must choose at least 1 channel.");
                    Channel15.Checked = true;
                }

                if (!TxPower3.Checked &&
                    !TxPower7.Checked &&
                    !TxPower11.Checked &&
                    !TxPower19.Checked &&
                    !TxPower31.Checked)
                {
                    MessageBox.Show("You must choose at least 1 TxPower.");
                    TxPower31.Checked = true;
                }
                ConfigWord = 0;
                if (Channel15.Checked)
                    ConfigWord |= (ushort)ConfigMask.MASK_CHANNEL15;
                if (Channel20.Checked)
                    ConfigWord |= (ushort)ConfigMask.MASK_CHANNEL20;
                if (Channel25.Checked)
                    ConfigWord |= (ushort)ConfigMask.MASK_CHANNEL25;
                if (Channel26.Checked)
                    ConfigWord |= (ushort)ConfigMask.MASK_CHANNEL26;
                if (TxPower3.Checked)
                    ConfigWord |= (ushort)ConfigMask.MASK_TXPOWER3;
                if (TxPower7.Checked)
                    ConfigWord |= (ushort)ConfigMask.MASK_TXPOWER7;
                if (TxPower11.Checked)
                    ConfigWord |= (ushort)ConfigMask.MASK_TXPOWER11;
                if (TxPower19.Checked)
                    ConfigWord |= (ushort)ConfigMask.MASK_TXPOWER19;
                if (TxPower31.Checked)
                    ConfigWord |= (ushort)ConfigMask.MASK_TXPOWER31;

                managementMsg = new Telos_Msg();

                // prepare data for TOS message
                Queue queueData = new Queue();

                // set action
                queueData.Enqueue((byte)ManagementActions.ChangeConfig);

                // set config word
                tmp = (ushort)ConfigWord;
                queueData.Enqueue((byte)(tmp >> 8)); // low bits
                queueData.Enqueue((byte)(tmp & 0x00FF)); // high bits


                // copy data from queue to byte array
                byte[] data = new byte[queueData.Count];
                queueData.CopyTo(data, 0);

                //if (SleepTime.Text)
                managementMsg.Init(PacketTypes.P_PACKET_ACK,	// with acknowledgement
                    0,											// prefix
                    MessageTypes.ManagementMessage,				// management message
                    MoteAddresses.BroadcastAddress,				// broadcast to all
                    MoteGroups.DefaultRTLSBeaconGroup,			// use default group (noninitialized motes have default group
                    data);

                //mainService.MsgSent(managementMsg);
                mainService.SendManagementTOSMsg(data, MoteAddresses.BroadcastAddress, MoteGroups.DefaultRTLSBeaconGroup);

                BeaconMode = BeaconControlMode.Config;
            }
            else
            {
                mainService.Stop();
            }
            UpdateBeaconControlGUIModeAndTimer(BeaconMode);
        }

        private void BeaconSleep_Click(object sender, EventArgs e)
        {
            ushort tmp;

            if (BeaconSleep.Text == "Sleep")
            {
                int sleeptime = 0, listentime = 0;
                try
                {
                    sleeptime = (int)Convert.ToInt32(SleepTime.Text);
                    listentime = (int)Convert.ToInt32(ListenTimeDuringSleep.Text);
                }
                catch
                {
                    MessageBox.Show("Time must be integers");
                }
                if (listentime >= sleeptime)
                {
                    MessageBox.Show("The sleep time must be larger than the listen time.");
                    return;
                }

                managementMsg = new Telos_Msg();


                // prepare data for TOS message
                Queue queueData = new Queue();

                // set action
                queueData.Enqueue((byte)ManagementActions.StartSleepMode);

                // Telos Mote is Big Endian: higher weight bits go to lower address

                tmp = (ushort)Convert.ToUInt16(SleepTime.Text);
                queueData.Enqueue((byte)(tmp >> 8)); // low bits
                queueData.Enqueue((byte)(tmp & 0x00FF)); // high bits



                tmp = (ushort)Convert.ToUInt16(ListenTimeDuringSleep.Text);
                queueData.Enqueue((byte)(tmp >> 8)); // low bits
                queueData.Enqueue((byte)(tmp & 0x00FF)); // high bits



                // sleep level, reserved
                queueData.Enqueue((byte)0);

                // copy data from queue to byte array
                byte[] data = new byte[queueData.Count];
                queueData.CopyTo(data, 0);

                //if (SleepTime.Text)
                managementMsg.Init(PacketTypes.P_PACKET_ACK,	// with acknowledgement
                    0,											// prefix
                    MessageTypes.ManagementMessage,				// management message
                    MoteAddresses.BroadcastAddress,				// broadcast to all
                    MoteGroups.DefaultRTLSBeaconGroup,			// use default group (noninitialized motes have default group
                    data);

                mainService.SendManagementTOSMsg(data, MoteAddresses.BroadcastAddress, MoteGroups.DefaultRTLSBeaconGroup);
                BeaconMode = BeaconControlMode.Sleep;
            }
            else
            {
                mainService.Stop();
            }
            UpdateBeaconControlGUIModeAndTimer(BeaconMode);

        }

        private uint ConfigWord = 0;

        void UpdateBeaconControlGUIModeAndTimer(BeaconControlMode Mode)
        {
            BeaconControlMsgTimer.Interval = 100; // ms
            switch (Mode)
            {
                case BeaconControlMode.Sleep:
                    if (BeaconSleep.Text == "Sleep")
                    {
                        nSleepPacket = 0;
                        BeaconSleep.Text = "Stop";
                        BeaconWakeup.Enabled = false;
                        ChangeSetting.Enabled = false;
                        BeaconControlMsgTimer.Start();
                    }
                    else
                    {
                        BeaconSleep.Text = "Sleep";
                        BeaconWakeup.Enabled = true;
                        ChangeSetting.Enabled = true;
                        BeaconControlMsgTimer.Stop();
                        BeaconMode = BeaconControlMode.Idel;
                    }
                    break;
                case BeaconControlMode.Wakeup:
                    if (BeaconWakeup.Text == "Wakeup")
                    {
                        nWakeupPacket = 0;
                        BeaconWakeup.Text = "Stop";
                        BeaconSleep.Enabled = false;
                        ChangeSetting.Enabled = false;
                        BeaconControlMsgTimer.Start();
                    }
                    else
                    {
                        BeaconWakeup.Text = "Wakeup";
                        BeaconSleep.Enabled = true;
                        ChangeSetting.Enabled = true;
                        BeaconControlMsgTimer.Stop();
                        BeaconMode = BeaconControlMode.Idel;
                    }
                    break;
                case BeaconControlMode.Config:
                    if (ChangeSetting.Text == "Change")
                    {
                        nConfigPacket = 0;
                        ChangeSetting.Text = "Stop";
                        BeaconSleep.Enabled = false;
                        BeaconWakeup.Enabled = false;
                        BeaconControlMsgTimer.Start();
                    }
                    else
                    {
                        ChangeSetting.Text = "Change";
                        BeaconSleep.Enabled = true;
                        BeaconWakeup.Enabled = true;
                        BeaconMode = BeaconControlMode.Idel;
                    }
                    break;
                case BeaconControlMode.Idel:
                default:
                    BeaconSleep.Enabled = true;
                    BeaconWakeup.Enabled = true;
                    ChangeSetting.Enabled = true;
                    BeaconControlMsgTimer.Stop();
                    break;
            }
        }

        private void TakeAuthorityOfSerial()
        {
            if (checkBoxCom.Checked)
            {
                checkBoxCom.Text = "On";
                colorButton = checkBoxCom.BackColor;
                checkBoxCom.BackColor = Color.DarkSeaGreen;
                checkBoxCom.Refresh();

                if (!mainService.Start())
                {
                    checkBoxCom.Checked = false;
                    return;
                }

                // clear table
                tblArchiveSnapshot.Clear();

                // start updating of grid
                timerDataGridUpdater.Start();

                if (mainService.bDemoMode)
                {
                    // just for test purposes (fast generating of messages)
                    timerTester.Interval = mainService.nDemoModeInterval;
                    timerTester.Start();
                }
            }
            else
            {
                // stop updating of grid
                timerDataGridUpdater.Stop();


                if (timerTester.Enabled)
                {
                    // just for test purposes (fast generating of messages)
                    timerTester.Stop();
                }

                // stop service
                mainService.Stop();
                checkBoxCom.Text = "Off";
                checkBoxCom.BackColor = colorButton;
            }
        }

        private void BeaconControlMsgTimer_Tick(object sender, EventArgs e)
        {
            switch (BeaconMode)
            {
                case BeaconControlMode.Sleep:
                    SleepStatus.Text = "Packet sent: " + (++nSleepPacket);
                    break;
                case BeaconControlMode.Config:
                    ChangeSettingPacketSent.Text = "Packet sent: " + (++nConfigPacket);
                    break;
                case BeaconControlMode.Wakeup:
                    WakeupStatus.Text = "Packet sent: " + (++nWakeupPacket);
                    break;
                case BeaconControlMode.Idel:
                default:
                    BeaconControlMsgTimer.Stop();
                    break;
            }
            this.Refresh();
        }

        private void BeaconWakeup_Click(object sender, EventArgs e)
        {
            if (BeaconWakeup.Text == "Wakeup")
            {
                managementMsg = new Telos_Msg();

                // prepare data for TOS message
                Queue queueData = new Queue();

                // set action
                queueData.Enqueue((byte)ManagementActions.WakeupMode);
                // copy data from queue to byte array
                byte[] data = new byte[queueData.Count];
                queueData.CopyTo(data, 0);

                managementMsg.Init(PacketTypes.P_PACKET_ACK,	// with acknowledgement
                    0,											// prefix
                    MessageTypes.ManagementMessage,				// management message
                    MoteAddresses.BroadcastAddress,				// broadcast to all
                    MoteGroups.DefaultRTLSBeaconGroup,			// use default group (noninitialized motes have default group
                    data);

                mainService.SendManagementTOSMsg(data, MoteAddresses.BroadcastAddress, MoteGroups.DefaultRTLSBeaconGroup);

                BeaconMode = BeaconControlMode.Wakeup;

            }
            else
            {
                mainService.Stop();
            }
            UpdateBeaconControlGUIModeAndTimer(BeaconMode);
        }

        private int HermesGroupIDToChannelHashFunction(MoteGroups GID)
        {
            int channel;
            int gid = (int)(GID);
            switch (gid % 4)
            {
                case 0:
                    channel = 15;
                    break;
                case 1:
                    channel = 20;
                    break;
                case 2:
                    channel = 25;
                    break;
                case 3:
                default:
                    channel = 26;
                    break;
            }
            return channel;
        }

        private void tabControlSettings_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControlSettings.SelectedIndex == 9)
            {
                if (!checkBoxCom.Checked)
                {
                    MessageBox.Show("Click the `Communication' button in the `sensors' tab before using this tab.");
                    BeaconSleep.Enabled = false;
                    BeaconWakeup.Enabled = false;
                    ChangeSetting.Enabled = false;
                }
                else
                {
                    BeaconSleep.Enabled = true;
                    BeaconWakeup.Enabled = true;
                    ChangeSetting.Enabled = true;
                }

            }
            else // if switch to other tabs, close the sniffer
            {
                if (SnifferButton.Text == "Stop")
                {
                    mainService.StopBeaconSnifferThread();
                    SnifferResetTimer.Stop();
                    SnifferButton.Text = "Sniff";
                }
            }
            // display the sniffing channel
            SniffingChannel.Text = GroupToChannel(mainService.groupID).ToString();
        }

        private int GroupToChannel(byte groupidByte)
        {
            int groupid = Convert.ToInt32(groupidByte);
            int chan = 0;
            switch (groupid % 4)
            {
                case 0:
                    chan = 15;
                    break;
                default:
                case 1:
                    chan = 20;
                    break;
                case 2:
                    chan = 25;
                    break;
                case 3:
                    chan = 26;
                    break;
            }
            return chan;
        }

        private void SnifferButton_Click(object sender, EventArgs e)
        {
            if (SnifferButton.Text == "Sniff")
            {
                mainService.ResetAwakenBeaconIDs();
                mainService.StartBeaconSnifferThread();
                SnifferButton.Text = "Stop";
                SnifferResetTimer.Start();
            }
            else
            {
                mainService.SuspendBeaconSnifferThread();
                SnifferResetTimer.Stop();
                SnifferButton.Text = "Sniff";
            }
        }

        private void SnifferResetTimer_Tick(object sender, EventArgs e)
        {
            int[] AwakenBeaconOnGUI;
            AwakenBeaconOnGUI = new int[mainService.AwakenBeaconIDs.Length];
            mainService.AwakenBeaconIDs.CopyTo(AwakenBeaconOnGUI, 0);

            AwakenBeaconRichTextBox.Text = "";
            foreach (int beaconID in AwakenBeaconOnGUI)
            {
                AwakenBeaconRichTextBox.Text += beaconID + "\n";
            }
            mainService.ResetAwakenBeaconIDs();
        }


		private byte[] GetSensorIDfromName(string name) { 
		
			byte[] retvalue = new byte[2];

			if (name.Equals("Light")) {

				retvalue[0] = 0x10;
				retvalue[1] = 0x02;

			}
			else if (name.Equals("Temperature"))
			{

				retvalue[0] = 0x10;
				retvalue[1] = 0x01;

			}
			else if (name.Equals("Humidity"))
			{

				retvalue[0] = 0x10;
				retvalue[1] = 0x03;

			}
			else if (name.Equals("Battery"))
			{

				retvalue[0] = 0x00;
				retvalue[1] = 0x01;

			}

			else {

				return null;
			}

			return retvalue;
		}

		private void buttonQuery_Click(object sender, EventArgs e)
		{
			TOS_Msg queryMessage = new Telos_Msg();
			TreeNode nodeSelected = treeViewNodes.SelectedNode;

			byte[] sensorID = GetSensorIDfromName( nodeSelected.Text );

			byte[] queryData = new byte[4];

			if (sensorID == null)
			{
				//Whole node selected
				queryData[0] = 0xFF;
				queryData[1] = 0xFF;
			}
			else {

				queryData[0] = sensorID[0];
				queryData[1] = sensorID[1];
				nodeSelected = nodeSelected.Parent;
			}

			queryData[2] = 0x00;
			queryData[3] = 0x00;

			queryMessage.Init(PacketTypes.P_PACKET_ACK,
				0,
				MessageTypes.HermesDataRequestMessage,
				(MoteAddresses)((SensorTreeNode)nodeSelected).address,
				//MoteAddresses.BroadcastAddress,				// broadcast to all
				(MoteGroups)mainService.groupID,
				//MoteGroups.DefaultRTLSBeaconGroup,			// use default group (noninitialized motes have default group
				queryData);

			mainService.serial.SendMsg(queryMessage);
		}

		private void SubscribeArbitrary() {

			TOS_Msg subscribeMessage = new Telos_Msg();

			TreeNode nodeSelected = treeViewNodes.SelectedNode;

			byte[] sensorID = GetSensorIDfromName(nodeSelected.Text);
			byte[] subscribeData = new byte[4];


			if (sensorID == null)
			{
				//Whole node selected
				subscribeData[0] = 0xFF;
				subscribeData[1] = 0xFF;
			}
			else
			{

				subscribeData[0] = sensorID[0];
				subscribeData[1] = sensorID[1];
				nodeSelected = nodeSelected.Parent;
			}

			UInt16 period = UInt16.Parse(textBoxPeriod.Text);

			subscribeData[2] = (byte)(period & 0xFF);
			subscribeData[3] = (byte)(period >> 8);

			subscribeMessage.Init(PacketTypes.P_PACKET_ACK,
				0,
				MessageTypes.HermesDataRequestMessage,
				MoteAddresses.BroadcastAddress,				// broadcast to all
				(MoteGroups)mainService.groupID,
				//MoteGroups.DefaultRTLSBeaconGroup,			// use default group (noninitialized motes have default group
				subscribeData);

			mainService.serial.SendMsg(subscribeMessage);
		
		
		}

		private void buttonSubscribe_Click(object sender, EventArgs e)
		{

			SubscribeArbitrary();

		}

		private void dataGridPeers_Navigate(object sender, NavigateEventArgs ne)
		{

		}

		private void increasePeriodToolStripMenuItem_Click(object sender, EventArgs e)
		{
		}

		private void buttonUnsubscribe_Click(object sender, EventArgs e)
		{
			textBoxPeriod.Text = "65535";
			SubscribeArbitrary();

		}

		public void RefreshTreeView_MoteAckReceived(string id)
		{

			//I tried to avoid this loop but the key hashing does not look like working...
			foreach (TreeNode t in treeViewNodes.Nodes[0].Nodes){

				if (t.Text.Equals(id)) {
					((SensorTreeNode)t).Connected();
					break;
				
				}
			}

		}

		int cycleNo = 0;

		//Use this to assign arbitrary periods to the users.
		//param: byte array size of number of users.
		//periodsToAssign[0] -> period to assign to 1st user..
		//so on, so forth.
		public void SendDSNActivation(byte[] periodsToAssign){

			TOS_Msg activateMessage = new Telos_Msg();
			byte[] activateData = new byte[17];

			cycleNo++;
			byte cycleHigh = (byte)(cycleNo & 0xFF);
			byte cycleLow = (byte)(cycleNo >> 8);

			//CycleNo
			activateData[0] = cycleHigh;
			activateData[1] = cycleLow;


			//Sample No's
			for (int i = 0; i < 15; i++)
			{

				activateData[i + 2] = periodsToAssign[i];

			}

			activateMessage.Init(PacketTypes.P_PACKET_NO_ACK,
			0,
			MessageTypes.DSNDemoActivationMessage,
			MoteAddresses.BroadcastAddress,
			(MoteGroups)mainService.groupID,
			activateData
			);


			//mainService.serial.SendMsg(activateMessage);
			performanceForm.Broadcast(activateMessage);

		
		}


		
		private void buttonActivate_Click(object sender, EventArgs e)
		{
			TOS_Msg activateMessage = new Telos_Msg();
			byte[] activateData = new byte[17];

            byte high;
            byte low;


			UInt16 period;

            if (radioButtonTotalSampNum.Checked)
            {
                int totalsample = int.Parse(TotalSampNumTextBox.Text);
                period = (ushort) (totalsample / SensorSelection.MaxSennum);
                InitSamplingRate = (int) period;
                // InitSamplingRate is accessed by SensorSelection when restarting the optimization iteration.
            }
            else
            {
                 period = UInt16.Parse(textBoxPeriod.Text);
                 InitSamplingRate = (int)period;
                 // InitSamplingRate is accessed by SensorSelection when restarting the optimization iteration.
            }
            // this button can be clicked only after the mainService thread
            global.mainService.DSNSensorSelection.AssignUniformSamplingRate(InitSamplingRate);

            high = (byte)(period & 0xFF);
            low = (byte)(period >> 8);

			cycleNo++;
			byte cycleHigh = (byte)(cycleNo & 0xFF);
			byte cycleLow  = (byte)(cycleNo >> 8);

			//CycleNo
			activateData[0] = cycleHigh;
			activateData[1] = cycleLow ;
 

			//Sample No's
			for (int i = 0; i < 15; i++) {

				activateData[i + 2] =  high;

			}

			//activateData[6] = 1; //ID = 5;
			//activateData[8] = 2;//ID= 7
			//activateData[12] = 10;//ID=10

			activateMessage.Init(PacketTypes.P_PACKET_NO_ACK,
				0,
				MessageTypes.DSNDemoActivationMessage,
				MoteAddresses.BroadcastAddress,
				(MoteGroups) mainService.groupID,
				activateData
				);


			//mainService.serial.SendMsg(activateMessage);
			performanceForm.Broadcast(activateMessage);
		
            // 10/13: cancel the ambient light
            global.mainService.DSNSensorSelection.Reset();
            global.mainService.DSNSensorSelection.CurrentAmbientLight = Int32.Parse(textBoxAmbientLight.Text);

		}


		public void SendDSNDebug(UInt16 index, UInt16 interval) {

			TOS_Msg debugMessage = new Telos_Msg();

			byte[] messageData = new byte[4];

			byte indexHigh = (byte)(index & 0xFF);
			byte indexLow = (byte)(index >> 8);

			byte intervalHigh = (byte)(interval & 0xFF);
			byte intervalLow = (byte)(interval >> 8);

			messageData[0] = indexHigh;
			messageData[1] = indexLow;

			messageData[2] = intervalHigh;
			messageData[3] = intervalLow;

			debugMessage.Init(PacketTypes.P_PACKET_NO_ACK,
				0,
				MessageTypes.DSNDemoDebugMessge,
				MoteAddresses.BroadcastAddress,
				(MoteGroups)mainService.groupID,
				messageData
				);

			//mainService.serial.SendMsg(debugMessage);
			performanceForm.Broadcast(debugMessage);
		
		}

		private void buttonInterval_Click(object sender, EventArgs e)
		{			

			UInt16 interval = UInt16.Parse(textBoxInterval.Text);

			SendDSNDebug(1, interval);
		
		}
		private void buttonDSN_Click(object sender, EventArgs e)
		{
            // the signal to indicates that performanceForm is running
            //buttonDSN.Enabled = false;

            //buttonActivate.Enabled = true;
            
            // for event observation (DSN demo)
            DBAccess dbAccess = new DBAccess("DataAcquisitionService.mdb");
            DataTable tblEventObservationDemo = dbAccess.GetTable("tblEventObservationDemo");
            double [,] SensorPos = new double[SensorSelection.dim, SensorSelection.MaxSennum];  // 2 for 2D observation
            int sensorID,i;
            
            foreach (DataRow dataRow in tblEventObservationDemo.Rows)
            {
                sensorID = (int)dataRow["SensorID"];
                SensorPos[0, sensorID-1] = (double)dataRow["x"];
                SensorPos[1, sensorID-1] = (double)dataRow["y"];
            }
			performanceForm = new DSNPerformanceForm(this, SensorPos);

            //performanceForm.AddSensorPosition(SensorPos);
            
			performanceForm.Show();

		}

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
                TotalSampNumTextBox.Enabled = false;
            else
                TotalSampNumTextBox.Enabled = true;
        }

        private void radioButtonTotalSampNum_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonTotalSampNum.Checked)
                textBoxPeriod.Enabled = false;
            else
                textBoxPeriod.Enabled = true;
        }

		private void buttonResetSeqNum_Click(object sender, EventArgs e)
		{
			SendDSNDebug(2, 0);
		}
	}



	public class SensorTreeNode : TreeNode {


		public ushort address;

		public SensorTreeNode(string name, ushort address):base(name) {

			this.address = address;
			this.BackColor = Color.Red;//Disconnected
		
		}

		public void Connected() {

			this.BackColor = Color.BlueViolet;
		
		}
	}

	public class MyComparerClass : IComparer
	{
		// Calls CaseInsensitiveComparer.Compare with the parameters reversed.
		int IComparer.Compare(Object x, Object y)
		{
			DataRow dX = (DataRow)x;
			DataRow dY = (DataRow)y;
			return -1*(dX["Variable"].ToString().CompareTo(dY["Variable"].ToString()));
		}
	}	
}

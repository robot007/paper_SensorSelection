using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using HermesMiddleware.CameraLibrary;
using HermesMiddleware.CommonLibrary;
using HermesMiddleware.MoteLibrary;
using HermesMiddleware.DBLibrary;
using HermesMiddleware.GUILibrary;
using HermesMiddleware.DataAcquisitionServiceServer;
using HermesMiddleware.DataAcquisitionServiceProxy;
using MoteLocalization;
using GraphLibrary;
using Cassini;
using System.IO;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.InteropServices;
using System.Xml;
using System.Data;
using System.Data.OleDb;

namespace DataAcquisitionService
{
    /// <summary>
    /// Main Service functionality.
    /// </summary>
    public class MainService
    {
        // dbg
        int noise = 0;
        
        // global stuff
        public Global global;

        // settings
        public Settings settings;

        // serial communication
        public SerialCOM serial;

        // queue of TOS messages
        Queue queueMessages = Queue.Synchronized(new Queue());

        // queue for value updates
        public Queue queueValueUpdates = Queue.Synchronized(new Queue());

        // worker thread for message processing
        Thread workerThread;
        Thread snifferThread;

        //send msg tread
        Thread sendMsgThread;

        //Master Time Publish Thread
        Thread masterTimeThread;

        // flag for closing of application
        public bool bClosing = false;

        // flag for presence of GUI
        public bool bGUI = false;

        // demo mode flag
        public bool bDemoMode = false;
        public int nDemoModeInterval = 10;

        // message counters
        long lReceivedMsg = 0;
        long lProcessedMsg = 0;
        long lToDoMsg = 0;
        long lSentMsg = 0;

        // default path for XML output
        const string sXMLFilePath = "C:\\Windows\\Storage\\WS\\";

        // DB stuff
        public DBAccess dbAccess;
        public DataTable tblArchive;
        public DataTable tblSensors;
        public DataTable tblSubscriptions;
        public DataTable tblQueries;
        public DataTable tblSettings;
        public DataTable tblPeers;
        public DataTable tblPublishedVariables;
        public DataTable tblMarkers;
        // multihop
        public DataTable tblMultihopSensorRooms;

        public DataTable tblPositions;

        // for event observation (DSNdemo): the position of light sensors
        public DataTable tblEventObservationDemo;
        public SensorSelection DSNSensorSelection;
        double[,] SensorPos;
        int[] ValidSensorID;
        const int MaxSensorNum = 15;
        int LastSensorID = 0;
        double[] PosEst;
        // end for DSNdemo

        // lookup table size for battery life calculation
        const int tableSize = 100;
        int[] BatterLifeLookUpTable = new int[tableSize];
        int segmentLength = tableSize / 5;

        // mote stuff
        PointF ScaleMotePtToPhysicalPt;

        // used group ID
        public byte groupID = 0;
        // management message
        ArrayList managementMsgArray;

        // instantiate TOS message
        Telos_Msg managementMsg;

        // interval for sending of management messages 
        int nMessageInterval = 0;

        // data collection
        int nLastCollectionTickCount = 0;
        int nDataCollectionInterval = 1000;

        // default base mote port
        short nBaseMotePort = 0;

        // default baud rate (base mote)
        int nBaudRate = 57600;

        // flag for activation
        public bool bActivateCommunicationUponStart = true;

        Hashtable motestats;

        Queue<UInt64> rfidBulkReading;
        SortedList<UInt64, KeyValuePair<DateTime, bool>> rfidReadingTimestamps;
        double rfidTimeout;
        private delegate void dummyDelegate();
        //PTZCamera camera;
        CameraControl camera_;
        CameraControl camera;

        SortedList<UInt16, List<RSSISignature>> rssiSignatures_; // associates mote ids to the rssi signatures
        SortedList<UInt16, ILocalizator> localizators_;

        ////////////////////ONGOING WORK - CIHAN - BEGIN///////////////////////////////////
        UInt32 latestTimeStamp = 0;
        ////////////////////ONGOING WORK - CIHAN - END///////////////////////////////////

        // for event observation (DSN demo)
        long CurrentSensorReadingTimeStamp = 0;
        long AllowedDelayForEventObservation = 1000; // unit: ms
        double []InitEventPositionEstimation={0,0};
        double[] AfterOptPos;
        double[] BeforeOptPos;
        double AfterOptErr;
        double BeforeOptErr;
        double[] moveDist;
        byte[] SamplingNum;
        // if two packets' time stamp is within this range, they are considered as if received at the same time.


        PointF ConvertMoteToPhysicalPosition;

        // for RTLS beacon configurtion
        public int[] AwakenBeaconIDs;
        int lightThreshold = 0;

        public MainService()
        {
            ConvertMoteToPhysicalPosition = new PointF((float)(1225.0 / 2000.0), (float)(640.0 / 800.0));
        }

        public void Init(MainForm mainForm)
        {
            // name the current thread for debugging purposes
            Thread.CurrentThread.Name = "MainService thread";

            // set global stuff
            global = new Global();
            global.RegisterRemotableServerClass(Global.sClassName, Global.port, true, global);
            global.mainForm = mainForm;
            global.mainService = this;
            global.bP2PConnected = false;
            global.bP2PMaster = false;



            if (mainForm != null)
            {
                bGUI = true;
            }

            // init DB stuff
            try
            {
                dbAccess = new DBAccess("DataAcquisitionService.mdb");
                tblArchive = dbAccess.GetTable("tblArchive");
                tblSensors = dbAccess.GetTable("tblSensors");
                tblSubscriptions = dbAccess.GetTable("tblSubscriptions");
                tblQueries = dbAccess.GetTable("tblQueries");
                tblSettings = dbAccess.GetTable("tblSettings");
                tblPeers = dbAccess.GetTable("tblPeers");
                tblPublishedVariables = dbAccess.GetTable("tblPublishedVariables");
                tblMarkers = dbAccess.GetTable("tblMarkers");

                tblPositions = dbAccess.GetTable("tblPositions");
                // multihop
                tblMultihopSensorRooms = dbAccess.GetTable("tblMultihopSensorRooms");

                // for event observation (DSN demo)
                tblEventObservationDemo = dbAccess.GetTable("tblEventObservationDemo");
                SensorPos = new double[2, MaxSensorNum];  // 2 for 2D observation
                int sensorID,i;
                //for (i = 0; i < MaxSensorNum; i++)
                //    ValidSensorID[i] = 0;
                foreach (DataRow dataRow in tblEventObservationDemo.Rows)
                {
                    sensorID = (int)dataRow["SensorID"];
                    SensorPos[0, sensorID-1] = (double)dataRow["x"];
                    SensorPos[1, sensorID-1] = (double)dataRow["y"];
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp);
                PrintStatus("Exception: " + exp.Message);
            }

            // settings
            settings = new Settings(dbAccess, tblSettings);

            // update settings from DB
            UpdateSettings();

            if (settings.GetBoolean("Use Camera for Localization") && (!settings.GetBoolean("Use IP Camera for Localization")))
            {
                camera = PTZCamera.Instance;
                ((PTZCamera)camera).RegisterRemotableServerClass(PTZCamera.sClassName, PTZCamera.port, true, (PTZCamera)camera);
            }


            motestats = new Hashtable();

            rfidBulkReading = new Queue<UInt64>();
            rfidReadingTimestamps = new SortedList<ulong, KeyValuePair<DateTime, bool>>();

            rssiSignatures_ = new SortedList<ushort, List<RSSISignature>>();
            localizators_ = new SortedList<ushort, ILocalizator>();

            // start own web server
            string sAppPath = Path.GetDirectoryName(FileFunctions.GetFilePath(null, "DataAcquisitionService.asmx"));

            try
            {
                global.webServer = new Cassini.Server(global.portNumber, global.sVirtRoot, sAppPath);
                global.webServer.Start();
                PrintStatus("HTTP server is listening on port " + global.portNumber + "...");
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp);
                PrintStatus("Exception: " + exp.Message);
            }

            // generate battery lookup table
            GenerateBatteryLifeLookUpTable();

            // init serial comm
            serial = new SerialCOM(null, new SerialCOM.MsgReceivedDel(MsgReceived), new SerialCOM.ByteSentDel(ByteSent), new SerialCOM.MsgSentDel(MsgSent), new SerialCOM.DefineBaseMotePortDel(DefineBaseMotePort), settings.GetString("Ember Pan ID") );
            //			serial = new Serial(null, new Serial.MsgReceivedDel(MsgReceived), new Serial.ByteSentDel(ByteSent), new Serial.MsgSentDel(MsgSent), new Serial.DefineBaseMotePortDel(DefineBaseMotePort));
            // create thread for message processing
            workerThread = new Thread(new ThreadStart(WorkerThread));
            workerThread.Name = "Worker thread for message processing";
            workerThread.IsBackground = true;
            workerThread.Start();

            if (settings.GetBoolean("Use Camera for Localization") && (!settings.GetBoolean("Use IP Camera for Localization")))
            {
                camera_ = PTZCamera.Instance;
                ((PTZCamera)camera_).RegisterRemotableServerClass(PTZCamera.sClassName, PTZCamera.port, true, (PTZCamera)camera_);
                camera_.SetPositionUpdateCallback(UpdateMotePosition);

            }

            if (!settings.GetBoolean("Use Camera for Localization") && settings.GetBoolean("Use IP Camera for Localization"))
            {
                camera_ = new SonyIPCamera();
                camera_.SetPositionUpdateCallback(UpdateMotePosition);

            }

            if (settings.GetBoolean("Use Camera for Localization") && settings.GetBoolean("Use IP Camera for Localization"))
            {
                Console.WriteLine("Check only one camera for use");
                throw new Exception("Check only one camera for use");
            }

            /*
                        // test subscription
                        EventSubscription subscription = new EventSubscription();
                        subscription.sVariable = "Weight";
                        Range range = new Range();
                        range.min = 0;
                        range.max = 99999999;
                        range.sName = "Value of Weight";
                        range.bStrict = false;
                        subscription.elements.Add(range);
                        global.AddSubscriber("http://PCCS516C:5555/SensorNetworkClient/SensorNetworkClient.asmx", "XXXXX", subscription);
            */
            if (global.sP2PMaster == "")
            {
                //Start Time Publishing
                StartTimePublishThread();
            }
            AwakenBeaconIDs = new int[1];




            // initialize Sensor Selection for DSNdemo
            DSNSensorSelection = new SensorSelection(SensorPos, 20.0);
            //Step: 1e-5
            DSNSensorSelection.ConfigAlgorithm(1000, 7000, 1e-6, 0.01, 1e-5);
            AfterOptPos = new double[SensorSelection.dim];
            BeforeOptPos = new double[SensorSelection.dim];
            moveDist = new double[SensorSelection.dim];
            PosEst = new double[SensorSelection.dim];
            for (int i = 0; i < SensorSelection.dim; i++)
            {
                AfterOptPos[i] = 0.0;
                BeforeOptPos[i] = 0.0;
                moveDist[i] = 0.0;
                PosEst[i] = 0.0;
            }
            SamplingNum = new byte[SensorSelection.MaxSennum];
            ResetDSNPosEst();
        }

        private void ResetDSNPosEst()
        {
            for (int i = 0; i < 2; i++)
            {
                AfterOptPos[i] = 0;
                BeforeOptPos[i] = 0;
            }
            AfterOptErr = 0;
            BeforeOptErr = 0;
            
        }

        // Check which beacon is working
        
        public void StartBeaconSnifferThread()
        {
            serial = new SerialCOM(null, new SerialCOM.MsgReceivedDel(MsgReceived), new SerialCOM.ByteSentDel(ByteSent), new SerialCOM.MsgSentDel(MsgSent), new SerialCOM.DefineBaseMotePortDel(DefineBaseMotePort), null);
            // create thread for message processing
            snifferThread = new Thread(new ThreadStart(WorkerThread));
            snifferThread.Name = "Sniffer thread for message processing";
            snifferThread.IsBackground = true;
            if (!snifferThread.IsAlive)
            {
                snifferThread.Start();
            }
            else
            {
                snifferThread.Resume();
            }
        }

        public void StopBeaconSnifferThread()
        {
            snifferThread.Abort();
        }

        public void SuspendBeaconSnifferThread()
        {
            snifferThread.Suspend();
        }



        //Creates small sized fragments of a large sized management messages. i.e. number of motes
        //to activate is greater than 3, TOS can handle only 28 bytes...
        public void CreateMsgsFromData(byte[] data)
        {

            int action = data[0];
            int groudID = data[1];
            int length = data[2];


            for (int i = 0; i <= (length - 1) / 3; i++)
            {

                byte[] dTemp = new byte[28];//Tos payload

                dTemp[0] = (byte)action;
                dTemp[1] = (byte)groudID;


                dTemp[2] = (byte)(length - i * 3);

                if (dTemp[2] > 3)
                {
                    dTemp[2] = 3;
                }

                for (int k = 0; k < dTemp[2]; k++)
                {

                    for (int j = 0; j < 8; j++)
                    {

                        dTemp[3 + k * 8 + j] = data[i * 24 + 3 + k * 8 + j];

                    }

                }

                managementMsg = new Telos_Msg();

                // init TOS message
                managementMsg.Init(PacketTypes.P_PACKET_ACK,	// with acknowledgement
                    0,											// prefix
                    MessageTypes.ManagementMessage,				// management message
                    MoteAddresses.BroadcastAddress,				// broadcast to all
                    MoteGroups.DefaultGroup,					// use default group (noninitialized motes have default group
                    dTemp);										// data

                managementMsgArray.Add(managementMsg);

            }

        }

        public bool Start()
        {
            motestats.Clear();

            // check if mote is attached to USB
            if (!serial.HasMote)
            {
                PrintStatus("Warning: No attached mote to USB found!");
//                ShowMessageBox("No attached mote to USB found - communication will not start.", MessageBoxIcon.Warning);
//               return false;
            }

            // set deafult baude rate
            serial.nBaudRate = nBaudRate;

            // set default port
            serial.nDefaultPort = nBaseMotePort;

            // try to init serial communication again
            if (!serial.Init())
            {
                ShowMessageBox("Could not initialize COM port " + serial.Port + " - communication will not start.", MessageBoxIcon.Warning);
                return false;
            }

            // get all rows from sensor table with checked use
            // get GUI flag
            DataRow[] rows = null;
            try
            {
                rows = tblSensors.Select("Use='True'");
                bGUI = settings.GetBoolean("Update GUI");
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp);
            }

            if (rows == null || rows.Length == 0)
            {
                ShowMessageBox("No sensor was selected for data acquisition - communication will not start.", MessageBoxIcon.Warning);
                return false;
            }

            // reset started column
            foreach (DataRow row in tblSensors.Rows)
            {
                row["Started"] = DBNull.Value;
            }

            managementMsgArray = new ArrayList();

            // prepare data for TOS message
            Queue queueData = new Queue();

            // set action
            queueData.Enqueue((byte)ManagementActions.StartSendingData);

            // add groupID
            queueData.Enqueue(groupID);

            // add number of sensors
            queueData.Enqueue((byte)rows.Length);

            global.mainForm.treeViewNodes.Nodes.Clear();
            global.mainForm.treeViewNodes.Nodes.Add(new SensorTreeNode("DAS Base", 0));

            // add requested sensors IDs
            foreach (DataRow row in rows)
            {
                string sID = row["ID"].ToString();
                try
                {
                    // get unique ID
                    UInt64 id = UInt64.Parse(sID, System.Globalization.NumberStyles.HexNumber);
                    byte[] bytes = BitConverter.GetBytes(id);
                    for (int i = bytes.Length - 1; i >= 0; i--)
                    {
                        queueData.Enqueue(bytes[i]);
                    }
                }
                catch (Exception exp)
                {
                    ShowMessageBox("Could not convert ID " + sID + " to byte array. ID has to be hexadecimal.", MessageBoxIcon.Error);
                    Console.WriteLine(exp);
                    return false;
                }

                SensorTreeNode baseNode = (SensorTreeNode)global.mainForm.treeViewNodes.Nodes[0];




                SensorTreeNode newNode = new SensorTreeNode(row["ID"].ToString(), ushort.Parse(row["SensorID"].ToString()));

                newNode.Nodes.Add(new TreeNode("Light"));
                newNode.Nodes.Add(new TreeNode("Temperature"));
                newNode.Nodes.Add(new TreeNode("Humidity"));
                newNode.Nodes.Add(new TreeNode("Battery"));


                baseNode.Nodes.Add(newNode);
            }

            // copy data from queue to byte array
            byte[] data = new byte[queueData.Count];
            queueData.CopyTo(data, 0);

            CreateMsgsFromData(data);
            // delete all queues
            queueMessages.Clear();
            queueValueUpdates.Clear();

            // delete archive
            tblArchive.Clear();
            tblQueries.Clear();

            // open serial communication
            if (!serial.Open())
            {
                PrintStatus("Error: Could not open serial communication on port " + serial.Port + "...");
                ShowMessageBox("Could not open serial communication on port " + serial.Port + " - communication will not start.", MessageBoxIcon.Warning);
                return false;
            }

            PrintStatus("Serial communication via port " + serial.Port + " is started...");

            //Start baconing thread
            sendMsgThread = new Thread(new ThreadStart(SendMsgThread));
            sendMsgThread.Name = "Beacon Serial Sending Thread";
            sendMsgThread.IsBackground = true;
            sendMsgThread.Start();

            try
            {
                DataRow[] rows_x = tblSensors.Select("x IS NOT NULL");
                foreach (DataRow row in rows_x)
                {
                    if (((string)row["x"] != "") && ((string)row["y"] != "") && ((string)row["z"] != "")&&((bool)row["Use"] == true))
                    {
                        DataRow newRow = tblPositions.NewRow();
                        newRow["SensorID"] = row["SensorID"];
                        newRow["x"] = row["x"];
                        newRow["y"] = row["y"];
                        newRow["z"] = row["z"];
                        newRow["Type"] = "System.Double";
                        newRow["Time"] = DateTime.Now.Ticks;
                        tblPositions.Rows.Add(newRow);
                        Publish("Position");
                    }
                }

            }
            catch (Exception exp)
            {
                Console.WriteLine(exp);
            }

            return true;
        }

        // Send ManagementTOSMsg to the serial port. 
        // If the serial port is not open, initialize it
        // Used for BeaconSetting in the Mainform.cs

        public bool SendManagementTOSMsg(byte[] TOSdata, MoteAddresses TargetAddress, MoteGroups TargetGroupID)
        {
            // instantiate TOS message
            managementMsg = new Telos_Msg();
            try
            {
                managementMsg.Init(PacketTypes.P_PACKET_ACK,	// with acknowledgement
                    0,											// prefix
                    MessageTypes.ManagementMessage,				// management message
                    TargetAddress,				// broadcast to all
                    TargetGroupID,					// use default group (noninitialized motes have default group
                    TOSdata);
                if (!serial.Open())
                {
                    // try to init serial communication again
                    if (!serial.Init())
                    {
                        ShowMessageBox("Could not initialize COM port " + serial.Port + " - communication will not start.", MessageBoxIcon.Warning);
                        return false;
                    }
                }
                //Start baconing thread
                sendMsgThread = new Thread(new ThreadStart(SendMsgThread));
                sendMsgThread.Name = "Beacon Serial Sending Thread";
                sendMsgThread.IsBackground = true;
                sendMsgThread.Start();// publish known locations of sensors
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp);
            }

            return true;
        }

        // Comment: terminate communication threads and close the serial port
        // Compare: Close()

        public void Stop()
        {
            if (sendMsgThread != null && sendMsgThread.IsAlive)
            {

                try
                {
                    if (sendMsgThread.IsAlive)
                    {
                        sendMsgThread.Abort();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }
            if (camera_ != null)
            {
                camera_.StopTrackingMarkers();
            }


            if (managementMsg != null)
            {
                // prepare data for TOS message
                byte[] data = new byte[1];

                // set action
                data[0] = (byte)ManagementActions.StopSendingData;

                // init TOS message
                managementMsg.Init(PacketTypes.P_PACKET_ACK,	// with acknowledgement
                    0,											// prefix
                    MessageTypes.ManagementMessage,				// management message
                    MoteAddresses.BroadcastAddress,				// broadcast to all
                    (MoteGroups)groupID,						// use current group
                    data);										// data

                if (serial != null)
                {
                    // send prepared TOS message
                    serial.SendMsg(managementMsg);
                }
            }

            if (serial != null && serial.IsOpen)
            {
                serial.Close();
                PrintStatus("Serial communication via port " + serial.Port + " is stopped...");
            }
        }

        public void ByteSent(byte b)
        {
            if (bGUI)
            {
                global.mainForm.DisplayByte(b, global.mainForm.richTextBoxSentBytes);
            }
        }

        public void ByteReceived(byte b)
        {
            if (bGUI)
            {
                global.mainForm.DisplayByte(b, global.mainForm.richTextBoxReceivedBytes);
            }
        }

        public void MsgSent(TOS_Msg msg)
        {
            try
            {
                // return if we are closing application
                if (bClosing) return;

                // increment counter
                lSentMsg++;

                if (bGUI)
                {
                    global.mainForm.PrintMsgStatus(-1, lSentMsg, -1, -1);

                    // display message if appropriate tab is selected
                    global.mainForm.DisplayMsg(msg, global.mainForm.richTextBoxLastSentMessage);
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp);
            }
        }

        public void MsgReceived(TOS_Msg msg)
        {
            try
            {
                // return if we are closing application
                if (bClosing) return;

                global.mainForm.DisplayStringTextBox(msg.getRawMsg(), global.mainForm.richTextBoxReceivedBytes);

                if (msg != null)
                {
                    if (msg.PacketType != (byte)PacketTypes.P_ACK)
                    {
                        // increment counter (do not count ACK messages)
                        lReceivedMsg++;

                        if (bGUI)
                        {
                            global.mainForm.PrintMsgStatus(lReceivedMsg, -1, -1, -1);

                            // display message if appropriate tab is selected
                            global.mainForm.DisplayMsg(msg, global.mainForm.richTextBoxLastReceivedMessage);
                        }

                        // just enqueue message -> processing is in worker thread
                        queueMessages.Enqueue(msg);
                    }
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp);
            }
        }

        //DSNDemo lamp position Error, precision
        double ErrR = 0;
        bool bDSNDemoStarting = true;

        public void ProcessMsg(TOS_Msg msg)
        {
            try
            {
                if (msg != null)
                {
                    string sSensorID;
                    string sMoteType;
                    string sVariable;
                    double dValue;

                    // get date & time of creation of the TOS message
                    long lTimestamp = msg.CreationTime;

                    // let's decode the data and put them in database
                    if (msg.PacketType == (byte)PacketTypes.P_ACK)
                    {
                        // this is special acknowlegment message (from base mote or other motes => do nothing for now
                    }
                    else if (msg.MessageType == (byte)MessageTypes.RFIDMessage)
                    {
                        // RFID message - decode message data

                        // get RFID (is in Big-Endian format => invertion needed)
                        byte[] rfidInv = new byte[8];
                        for (int i = 0; i < 8; i++)
                        {
                            rfidInv[7 - i] = msg.Data[i];
                        }
                        UInt64 rfid = BitConverter.ToUInt64(rfidInv, 0);
                        UInt16 userID = BitConverter.ToUInt16(msg.Data, msg.Data.Length - 2);

                        // prepare values to store
                        sSensorID = userID.ToString();
                        sMoteType = "Telos Mote";
                        sVariable = "RFID";
                        dValue = rfid;


                        // update DB
                        UpdateDatabase(sSensorID, sMoteType, sVariable, lTimestamp, rfid);
                    }
                    else if (msg.MessageType == (byte)MessageTypes.PulseOxyMessage)
                    {
                        // pulse oxymeter message - decode message data
                        // 7E												sync code
                        //		42											packet type
                        //		0A											data length
                        //		01 08										fcf
                        //		CF											dsn
                        //		FF FF										destpan
                        //		FF FF										address
                        //		D1											message type
                        //		79											group ID
                        //			14, 00									data: source address
                        //			72, 00									data: sequence number
                        //			61, 00									data: sp02
                        //			54, 00									data: pulse
                        //			3B, 00									data: waveform
                        //		B4 C9										CRC
                        // 7E												sync code

                        // data format: UInt16 sourceAddress, UInt16 sequenceNumber, UInt16 sp02, UInt16 waveform

                        // decode significant variables
                        UInt16 sourceAddress = BitConverter.ToUInt16(msg.Data, 0);
                        UInt16 sp02 = BitConverter.ToUInt16(msg.Data, 4);
                        UInt16 pulse = BitConverter.ToUInt16(msg.Data, 6);
                        UInt16 waveform = BitConverter.ToUInt16(msg.Data, 8);

                        // prepare variables to store
                        sSensorID = sourceAddress.ToString();
                        sMoteType = "MicaZ Mote";

                        // update DB
                        UpdateDatabase(sSensorID, sMoteType, "OxyHemoglobin", lTimestamp, sp02);
                        UpdateDatabase(sSensorID, sMoteType, "PulseRate", lTimestamp, pulse);
                        UpdateDatabase(sSensorID, sMoteType, "WaveForm", lTimestamp, waveform);
                    }else if(msg.MessageType == (byte)MessageTypes.DSNDemoDataMessage){

                            UInt16 source = BitConverter.ToUInt16(msg.Data, 0);
                            UInt16 seqNum = BitConverter.ToUInt16(msg.Data, 2);
                            UInt16 cycleNo = BitConverter.ToUInt16(msg.Data, 4);
                            UInt16 value = BitConverter.ToUInt16(msg.Data, 6);

                            UInt16 lightValue = (UInt16)(1e6 * (value / 4096.0 * 2.5) / 100);

                            int NumOfActiveSensor = SensorSelection.MaxSennum;
                            int i;

                            if (bDSNDemoStarting && global.mainForm.performanceForm != null)
                            {

                                global.mainForm.performanceForm.AddToActiveSensors(NumOfActiveSensor);
                                bDSNDemoStarting = false;

                            }

                            UpdateDatabase(source.ToString(), "DSN Demo", "Light", lTimestamp, lightValue);
                            UpdateDatabase(source.ToString(), "DSN Demo", "CycleNo", lTimestamp, cycleNo);
                            UpdateDatabase(source.ToString(), "DSN Demo", "seqNum", lTimestamp, seqNum);

                            // return;
                            if (global.mainForm.performanceForm != null)
                            {

                                global.mainForm.performanceForm.UpdateNodeSeqNum(source - 1, seqNum);

                            }

                            //return;
                            double[] PosEst = new double[SensorSelection.dim];
                            // after using timer: always add the new data 
                            DSNSensorSelection.AddOneSensorReading(source, lightValue);
                            
                            // use delegater to modified this later
                            //  global.mainForm.AvgLightText.Text = DSNSensorSelection.GetAvgLight().ToString();
                            
                            //// obsoleted code. this code is modified and copied to DSNoperation()
                            ////
                            //if ( (LastSensorID > source) 
                            //// Sensors are using TDMA scheduling. When LastSensorID > source, one round of data collection is done
                            //    || (LastSensorID == source) 
                            //// If LastSensorID == source, only one sensor is available.
                            //    || (lTimestamp > CurrentSensorReadingTimeStamp + AllowedDelayForEventObservation)
                            //// When PRR is very low this "time out" feature stop possible dead lock
                            //    )
                            //{
                            //    if (DSNSensorSelection.isDataValid())
                            //    {
                            //        // DSNSensorSelection.isDataValid() == true
                            //        if (DSNSensorSelection.EstimatePosition(InitEventPositionEstimation, out PosEst, out ErrR))
                            //        {
                            //            for (i = 0; i < SensorSelection.dim; i++)
                            //                InitEventPositionEstimation[i] = PosEst[i];

                            //            if (DSNSensorSelection.isAfterSensorSelection)
                            //            {
                            //                UpdateDatabase("1000", "DSN After Selection", "x", lTimestamp, PosEst[0]);
                            //                UpdateDatabase("1000", "DSN After Selection", "y", lTimestamp, PosEst[1]);
                            //                UpdateDatabase("1000", "DSN After Selection", "r", lTimestamp, ErrR);

                            //                //dbg
                            //                Console.WriteLine("after (x,y,r) inch=" + PosEst[0] / 2.54 + "," + PosEst[1] / 2.54 + "," + ErrR / 2.54);

                            //                global.mainForm.performanceForm.SetAftertimizationPosition(PosEst, ErrR);
                            //                for (i = 0; i < SensorSelection.dim; i++)
                            //                {
                            //                    AfterOptPos[i] = PosEst[i];
                            //                    moveDist[i] = AfterOptPos[i] - BeforeOptPos[i];
                            //                }
                            //                AfterOptErr = ErrR;

                            //                // if the error is too much, restart the event observation

                            //                //if (Math.Sqrt(moveDist[0] * moveDist[0] + moveDist[1] * moveDist[1]) > SensorSelection.MoveThresholdRatio * ErrR)
                            //                if (Math.Sqrt(moveDist[0] * moveDist[0] + moveDist[1] * moveDist[1]) > SensorSelection.MoveThresholdDist )
                            //                {
                            //                    // if detects 30cm movement, start the iteration again.
                            //                    DSNSensorSelection.Reset();
                            //                    ResetDSNPosEst();
                            //                }
                            //            }
                            //            else
                            //            {
                            //                UpdateDatabase("1001", "DSN Before Selection", "x", lTimestamp, PosEst[0]);
                            //                UpdateDatabase("1001", "DSN Before Selection", "y", lTimestamp, PosEst[1]);
                            //                UpdateDatabase("1001", "DSN Before Selection", "r", lTimestamp, ErrR);
                            //                for (i = 0; i < SensorSelection.dim; i++)
                            //                {
                            //                    BeforeOptPos[i] = PosEst[0];
                            //                }
                            //                BeforeOptErr = ErrR;

                            //                global.mainForm.performanceForm.SetBeforeOptimizationPosition(PosEst, ErrR);

                            //                //dbg
                            //                Console.WriteLine("before (x,y,r) inch=" + PosEst[0] / 2.54 + "," + PosEst[1] / 2.54 + "," + ErrR / 2.54);

                            //                byte[] SamplingNum = new byte[SensorSelection.MaxSennum];
                            //                if (DSNSensorSelection.OptimizeRealSamplingRate(out SamplingNum)) 
                            //                {
                            //                    // guarantee 3 sensors being selected.
                            //                    global.mainForm.SendDSNActivation(SamplingNum);

                            //                    string strdbg = "Sample Num: ";
                            //                    for (i = 0; i < SensorSelection.MaxSennum; i++)
                            //                    {
                            //                        strdbg += i + ":" + SamplingNum[i] + " ";
                            //                    }
                            //                    Console.WriteLine(strdbg);


                            //                    global.mainForm.performanceForm.SetSamplingRate(SamplingNum);
                            //                    NumOfActiveSensor = 0;
                            //                    for (i = 0; i < SensorSelection.MaxSennum; i++)
                            //                    {
                            //                        if (SamplingNum[i] != 0)
                            //                        {
                            //                            NumOfActiveSensor++;
                            //                        }
                            //                    }

                            //                    global.mainForm.performanceForm.AddToActiveSensors(NumOfActiveSensor);
                            //                    //dbg
                            //                    Console.WriteLine("Num of active sensors " + NumOfActiveSensor);
                            //                }
                            //            }

                            //            global.mainForm.performanceForm.AddToPrecision(ErrR);
                            //            DSNSensorSelection.ClearAll();
                            //        }
                            //        else
                            //        {
                            //            // position estimation is not valid
                            //            // 
                            //            DSNSensorSelection.WeightCenterOfValidSensors(out InitEventPositionEstimation);

                            //        }
                            //    }
                            //    else
                            //    {
                            //        // data not valid, should we restart?
                            //        if (DSNSensorSelection.TooManyInvalidDataSets())
                            //        {
                            //            // restart the optimization iteration. trigger all the sensors to sample!
                            //            DSNSensorSelection.Reset();
                            //            byte[] SamplingNum = new byte[SensorSelection.MaxSennum];
                            //            for (i = 0; i < SensorSelection.MaxSennum; i++)
                            //            {
                            //                SamplingNum[i] = (byte)global.mainForm.InitSamplingRate;
                            //            }

                            //            // all the sensors start to collect samples
                            //            global.mainForm.SendDSNActivation(SamplingNum);
                            //            // use the sampling rate for error estimation
                            //            DSNSensorSelection.AssignUniformSamplingRate(global.mainForm.InitSamplingRate);
                            //        }
                            //    }
                                
                            //    CurrentSensorReadingTimeStamp = lTimestamp;
                            //}
                            //else                        // Update sensor reading, 
                            ////(lTimestamp < CurrentSensorReadingTimeStamp + AllowedDelayForEventObservation)
                            //{
                            //    DSNSensorSelection.AddOneSensorReading(source, lightValue);
                            //}

                            //LastSensorID = source;
					}
                    else if (msg.MessageType == (byte)MessageTypes.EKGMessage)
                    {

                        // EKG message

                        int nEKGvalues = (msg.Data.Length - 4) / 2;

                        HermesMiddleware.DataAcquisitionServiceServer.dataItem[] newItems = new HermesMiddleware.DataAcquisitionServiceServer.dataItem[nEKGvalues];
                        // the offsets are obtained from EKG.h
                        UInt16 ekgSourceAddress = BitConverter.ToUInt16(msg.Data, 0);
                        //UInt16 ekg = BitConverter.ToUInt16(msg.Data, 22);

                        sSensorID = ekgSourceAddress.ToString();
                        sMoteType = "Telos Mote";

                        for (int i = 0; i < nEKGvalues; i++)
                        {
                            UInt16 ekg = BitConverter.ToUInt16(msg.Data, i * 2 + 4);

                            newItems[i] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                            newItems[i].timestamp = lTimestamp.ToString();
                            newItems[i].value = ekg;

                            lTimestamp += 10000;
                        }

                        UpdateDatabase(sSensorID, sMoteType, "EKG", newItems);
                        // update database
                        //UpdateDatabase(sSensorID, sMoteType, "EKG", lTimestamp, ekg);
                    }
                    else if (msg.MessageType == (byte)MessageTypes.TLHMessage)
                    {
                        // temperature/light/humidity messages

                        // data format: UInt16 temp1, UInt16 temp2, UInt16 temp3,
                        //				UInt16 hum1, UInt16 hum2, UInt16 hum3,
                        //				UInt16 light1, UInt16 light2, UInt16 light3,
                        //				UInt16 voltage,
                        //				UInt16 moteID

                        // decode significant variables
                        UInt16 temp1 = (UInt16)(-39.60 + 0.01 * BitConverter.ToUInt16(msg.Data, 6));
                        UInt16 hum1 = BitConverter.ToUInt16(msg.Data, 12);
                        hum1 = (UInt16)(-4.0 + 0.0405 * hum1 + -2.8e-6 * hum1 * hum1);
                        UInt16 light1 = BitConverter.ToUInt16(msg.Data, 18);
                        light1 = (UInt16)(1e6 * (light1 / 4096.0 * 2.5) / 100);
                        UInt16 voltage = BitConverter.ToUInt16(msg.Data, 24);
                        UInt16 batteryLife = (UInt16)GetRemainingBatteryPower(voltage / 2);
                        UInt16 moteID = BitConverter.ToUInt16(msg.Data, 0);
                        UInt16 last_seq_num = BitConverter.ToUInt16(msg.Data, 2);

                        // prepare variables to store
                        sSensorID = moteID.ToString();
                        sMoteType = "Telos Mote";

                        if (motestats.Contains(sSensorID))
                        {
                            ((MoteStatistics)motestats[sSensorID]).update_seq_num((int)last_seq_num);
                        }
                        else
                        {
                            motestats.Add(sSensorID, new MoteStatistics((int)last_seq_num));
                        }

                        // update DB
                        UpdateDatabase(sSensorID, sMoteType, "Temperature", lTimestamp, temp1);
                        UpdateDatabase(sSensorID, sMoteType, "Humidity", lTimestamp, hum1);
                        UpdateDatabase(sSensorID, sMoteType, "Light", lTimestamp, light1);
                        UpdateDatabase(sSensorID, sMoteType, "BatteryVoltage", lTimestamp, voltage);
                        UpdateDatabase(sSensorID, sMoteType, "BatteryLife", lTimestamp, batteryLife);
                        UpdateDatabase(sSensorID, sMoteType, "SeqNo", lTimestamp, last_seq_num);
                        UpdateDatabase(sSensorID, sMoteType, "Performance", lTimestamp, ((MoteStatistics)(motestats[sSensorID])).performance_percentage());

                        //// to log brightness  
                        //string BrightLog = new string(new char[20]); ;
                        //BrightLog = last_seq_num.ToString()+","+light1.ToString();
                        //Console.WriteLine(BrightLog);
                    }
                    else if (msg.MessageType == (byte)MessageTypes.ManagementMessage)
                    {
                        // management message - decode message data
                        byte action = msg.Data[0];

                        if (action == (byte)ManagementActions.SendingDataStarted)
                        {
                            // get moteID (is in Big-Endian format => invertion needed)
                            byte[] moteIDInv = new byte[8];
                            for (int i = 0; i < 8; i++)
                            {
                                moteIDInv[7 - i] = msg.Data[1 + i];
                            }

                            // 
                            //get 64-bit value
                            UInt64 moteID = BitConverter.ToUInt64(moteIDInv, 0);

                            // convert to hexastring
                            string sMoteID = moteID.ToString("X16");

                            // find it in the table
                            DataRow[] rows = tblSensors.Select("ID='" + sMoteID + "'");
                            if (rows.Length > 0)
                            {
                                rows[0]["Started"] = DateTime.Now;
                                global.mainForm.RefreshTreeView_MoteAckReceived(sMoteID);
                            }

                            int nUseCount = 0;
                            int nStartedCount = 0;
                            foreach (DataRow row in tblSensors.Rows)
                            {
                                if ((bool)row["Use"] == true)
                                {
                                    nUseCount++;
                                }

                                if (!row.IsNull("Started"))
                                {
                                    // some date from aknowlegdement is there => count it
                                    nStartedCount++;
                                }
                            }

                            if (nUseCount == nStartedCount)
                            {

                                //Stop the beaconing thread here

                                try
                                {

                                    if (sendMsgThread.IsAlive)
                                    {
                                        sendMsgThread.Abort();
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }

                            }
                        }
                    }
                    else if (msg.MessageType == (byte)MessageTypes.WeigthScaleMessage)
                    {
                        // wight scale message

                        // the offsets are obtained from EKG.h
                        UInt16 voltage = BitConverter.ToUInt16(msg.Data, 0);
                        double weightLb = Voltage2Weight(voltage);
                        UInt16 sourceAddress = BitConverter.ToUInt16(msg.Data, 2);

                        sSensorID = sourceAddress.ToString();
                        sMoteType = "Telos Mote";

                        // update database
                        UpdateDatabase(sSensorID, sMoteType, "Weight", lTimestamp, weightLb);
                    }
                    else if (msg.MessageType == (byte)MessageTypes.SmartTrayMessage)
                    {
                        UInt64 rfid = BitConverter.ToUInt64(msg.Data, 0);

                        UInt16 currentTag = msg.Data[9];
                        UInt16 totalTagCount = msg.Data[8];
                        UInt16 voltage = BitConverter.ToUInt16(msg.Data, 12);
                        if (totalTagCount == 0)
                        {
                            UInt16 sourceAddress = BitConverter.ToUInt16(msg.Data, 10);
                            HermesMiddleware.DataAcquisitionServiceServer.dataItem[] items = new HermesMiddleware.DataAcquisitionServiceServer.dataItem[1];
                            items[0] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                            items[0].timestamp = lTimestamp.ToString();
                            items[0].value = (ulong)0;
                            UpdateDatabase(BitConverter.ToUInt16(msg.Data, 10).ToString(), "Telos Mote", "RFID", items);
                            UpdateDatabase(BitConverter.ToUInt16(msg.Data, 10).ToString(), "Telos Mote", "BatteryVoltage", lTimestamp, voltage);
                            List<UInt64> readingsToDelete = new List<ulong>();
                            DateTime now = DateTime.Now;
                            List<HermesMiddleware.DataAcquisitionServiceServer.dataItem> additionList = new List<HermesMiddleware.DataAcquisitionServiceServer.dataItem>();
                            List<HermesMiddleware.DataAcquisitionServiceServer.dataItem> deletionList = new List<HermesMiddleware.DataAcquisitionServiceServer.dataItem>();
                            foreach (UInt64 tag in rfidReadingTimestamps.Keys)
                            {
                                if (rfidReadingTimestamps[tag].Value == true)
                                {
                                    HermesMiddleware.DataAcquisitionServiceServer.dataItem newItem = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                                    newItem.timestamp = lTimestamp.ToString();
                                    newItem.value = tag;
                                    additionList.Add(newItem);
                                }
                                if (IsRFIDTagTooOld(now, rfidReadingTimestamps[tag].Key))
                                {
                                    HermesMiddleware.DataAcquisitionServiceServer.dataItem newItem = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                                    newItem.timestamp = lTimestamp.ToString();
                                    newItem.value = tag;
                                    deletionList.Add(newItem);
                                    readingsToDelete.Add(tag);
                                }
                            }

                            SortedList<UInt64, KeyValuePair<DateTime, bool>> newRFIDReadingTimestamps = new SortedList<ulong, KeyValuePair<DateTime, bool>>();
                            foreach (UInt64 tag in rfidReadingTimestamps.Keys)
                            {
                                if (!readingsToDelete.Contains(tag))
                                {
                                    newRFIDReadingTimestamps.Add(tag, new KeyValuePair<DateTime, bool>(rfidReadingTimestamps[tag].Key, false));
                                }
                            }
                            rfidReadingTimestamps = newRFIDReadingTimestamps;
                            UpdateDatabase(sourceAddress.ToString(), "Telos Mote", "RFID_Count", lTimestamp, rfidReadingTimestamps.Count);
                            if (additionList.Count != 0)
                            {
                                UpdateDatabase(sourceAddress.ToString(), "Telos Mote", "RFID_Additions", additionList.ToArray());
                            }
                            if (deletionList.Count != 0)
                            {
                                UpdateDatabase(sourceAddress.ToString(), "Telos Mote", "RFID_Deletions", deletionList.ToArray());
                            }
                        }
                        else
                        {
                            rfidBulkReading.Enqueue(rfid);
                            if ((currentTag + 1) == totalTagCount)
                            {
                                UInt16 sourceAddress = BitConverter.ToUInt16(msg.Data, 10);
                                HermesMiddleware.DataAcquisitionServiceServer.dataItem[] items = new HermesMiddleware.DataAcquisitionServiceServer.dataItem[rfidBulkReading.Count];
                                int tagIndex = 0;
                                foreach (UInt64 tag in rfidBulkReading)
                                {
                                    HermesMiddleware.DataAcquisitionServiceServer.dataItem newItem = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                                    newItem.timestamp = lTimestamp.ToString();
                                    newItem.value = tag;
                                    items[tagIndex++] = newItem;
                                    if (rfidReadingTimestamps.Keys.Contains(tag))
                                    {
                                        rfidReadingTimestamps[tag] = new KeyValuePair<DateTime, bool>(DateTime.Now, false);
                                    }
                                    else
                                    {
                                        rfidReadingTimestamps.Add(tag, new KeyValuePair<DateTime, bool>(DateTime.Now, true));
                                    }
                                }
                                rfidBulkReading.Clear();
                                UpdateDatabase(sourceAddress.ToString(), "Telos Mote", "RFID", items);
                                UpdateDatabase(sourceAddress.ToString(), "Telos Mote", "BatteryVoltage", lTimestamp, voltage);
                                List<UInt64> readingsToDelete = new List<ulong>();
                                DateTime now = DateTime.Now;
                                List<HermesMiddleware.DataAcquisitionServiceServer.dataItem> additionList = new List<HermesMiddleware.DataAcquisitionServiceServer.dataItem>();
                                List<HermesMiddleware.DataAcquisitionServiceServer.dataItem> deletionList = new List<HermesMiddleware.DataAcquisitionServiceServer.dataItem>();
                                foreach (UInt64 tag in rfidReadingTimestamps.Keys)
                                {
                                    if (rfidReadingTimestamps[tag].Value == true)
                                    {
                                        HermesMiddleware.DataAcquisitionServiceServer.dataItem newItem = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                                        newItem.timestamp = lTimestamp.ToString();
                                        newItem.value = tag;
                                        additionList.Add(newItem);
                                    }
                                    if (IsRFIDTagTooOld(now, rfidReadingTimestamps[tag].Key))
                                    {
                                        HermesMiddleware.DataAcquisitionServiceServer.dataItem newItem = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                                        newItem.timestamp = lTimestamp.ToString();
                                        newItem.value = tag;
                                        deletionList.Add(newItem);
                                        readingsToDelete.Add(tag);
                                    }
                                }

                                SortedList<UInt64, KeyValuePair<DateTime, bool>> newRFIDReadingTimestamps = new SortedList<ulong, KeyValuePair<DateTime, bool>>();
                                foreach (UInt64 tag in rfidReadingTimestamps.Keys)
                                {
                                    if (!readingsToDelete.Contains(tag))
                                    {
                                        newRFIDReadingTimestamps.Add(tag, new KeyValuePair<DateTime, bool>(rfidReadingTimestamps[rfid].Key, false));
                                    }
                                }
                                rfidReadingTimestamps = newRFIDReadingTimestamps;
                                UpdateDatabase(sourceAddress.ToString(), "Telos Mote", "RFID_Count", lTimestamp, rfidReadingTimestamps.Count);
                                if (additionList.Count != 0)
                                {
                                    UpdateDatabase(sourceAddress.ToString(), "Telos Mote", "RFID_Additions", additionList.ToArray());
                                }
                                if (deletionList.Count != 0)
                                {
                                    UpdateDatabase(sourceAddress.ToString(), "Telos Mote", "RFID_Deletions", deletionList.ToArray());

                                }
                            }
                        }
                    }
                    else if (msg.MessageType == (byte)MessageTypes.LocationMessage)
                    {
                        // Parse location message from midified MoteTrack 2.0
                        // The TOS_Msg is the same as the struct of ReplyLocEstMsg in the MoteTrack 

                        UInt16 srcAddr = BitConverter.ToUInt16(msg.Data, 0);
                        UInt16 signatureID = BitConverter.ToUInt16(msg.Data, 2);
                        UInt16 x = BitConverter.ToUInt16(msg.Data, 4);
                        UInt16 y = BitConverter.ToUInt16(msg.Data, 6);
//                        UInt16 z = BitConverter.ToUInt16(msg.Data, 8);
                        UInt16 z = (UInt16)(120);
                        // RTLS does not estimate z value. Assume the mobile Mote is on a cart. So its height is about 1.2m
                        x = Convert.ToUInt16((float)(ConvertMoteToPhysicalPosition.X * (float)x));
                        y = Convert.ToUInt16((float)(ConvertMoteToPhysicalPosition.Y * (float)y));

                        HermesMiddleware.DataAcquisitionServiceServer.dataItem[] MotePosition = new HermesMiddleware.DataAcquisitionServiceServer.dataItem[6];

                        //timestr.ToString(lTimestamp);
                        MotePosition[0] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                        MotePosition[1] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                        MotePosition[2] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                        MotePosition[3] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                        MotePosition[4] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                        MotePosition[5] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();

                        MotePosition[0].timestamp = System.Convert.ToString(lTimestamp);
                        MotePosition[0].value = x;
                        MotePosition[1].timestamp = MotePosition[0].timestamp;
                        MotePosition[1].value = y;
                        MotePosition[2].timestamp = MotePosition[0].timestamp;
                        MotePosition[2].value = z;
                        MotePosition[3].timestamp = System.Convert.ToString(lTimestamp);
                        MotePosition[3].value = 0;
                        MotePosition[4].timestamp = MotePosition[0].timestamp;
                        MotePosition[4].value = 0;
                        MotePosition[5].timestamp = MotePosition[0].timestamp;
                        MotePosition[5].value = 0;

                        System.Console.WriteLine("x value: {0}", x);
                        System.Console.WriteLine("y value: {0}", y);
                        System.Console.WriteLine("z value: {0}", z);

                        DataRow[] rows = tblMarkers.Select("SensorID=" + srcAddr);
                        int markerId = -1;
                        if (rows.Length != 0)
                        {
                            markerId = Int32.Parse((string)rows[0]["MarkerNumber"]);
                        }

                        bool useCamera = (settings.GetBoolean("Use Camera for Localization")) || (settings.GetBoolean("Use IP Camera for Localization"));


                        if (markerId == -1 || !useCamera)
                        {
                            UpdateDatabase(System.Convert.ToString(srcAddr), "Telos Mote", "Position", MotePosition);
                        }
                        else
                        {
                            camera_.UpdateMarkerPosition(markerId, x, y, z);
                        }

                        //UInt16 sourceAddress = BitConverter.ToUInt16(msg.Data, 10);

                        //node enters restricted area and triggers camera to point to restricted coordintes


                        // data format: Int32 x_node,
                        //				Int32 y_node,
                        //				Int32 z_node,
                        //				UInt16 moteID

                        // decode significant variables
                        //Int16 x_node = BitConverter.ToInt16(msg.Data, 6);
                        //Int16 y_node = BitConverter.ToInt16(msg.Data, 8);
                        //Int16 z_node = BitConverter.ToInt16(msg.Data, 10);

                        //UInt16 moteID = BitConverter.ToUInt16(msg.Data, 0);
                        //UInt16 last_seq_num = BitConverter.ToUInt16(msg.Data, 2);

                        //DataRow[] rows_x = tblSensors.Select("X-position != ''");
                        //DataRow[] rows_y = tblSensors.Select("Y-position != ''");


                        //try
                        //{
                        //    if ((rows_x.Length <= 0)||(rows_y.Length <= 0))
                        //        throw new Exception("There are no valid coordinates for mote");
                        //}
                        //catch (Exception exp)
                        //{
                        //    ShowMessageBox("There are no valid coordinates for mote", MessageBoxIcon.Error);
                        //    Console.WriteLine(exp);
                        //    return;
                        //}

                        // coordinates should be transformed here into PTZ parameters....
                        // here, send a signal to RailProtectServer: x and y coordinates from tblSensors
                    }
                    else if (msg.MessageType == (byte)MessageTypes.LocationBeaconMessage)
                    {
                        //struct BeaconMsg
                        //{
                        //    uint16_t srcAddr; // The 16-bit source node address.
                        //    uint16_t sqnNbr;  // The 16-bit sequence number. This value is incremented for each
                        //                      // subsequent message sent by a node, and wraps around to 0.
                        //    uint8_t freqChan;
                        //    uint8_t txPower;
                        //} __attribute__ ((packed));
                        UInt16 srcAddr = BitConverter.ToUInt16(msg.Data, 0);
                        int newID = (int)srcAddr;
                        //if AwakenBeaconIDs.
                        bool isNotInArray = true;
                        foreach (int awakenID in AwakenBeaconIDs)
                        {
                            if (awakenID == newID)
                            {
                                isNotInArray = false;
                                break;
                            }
                        }
                        if (isNotInArray)
                        {
                            if (AwakenBeaconIDs.Length == 1)
                            {
                                AwakenBeaconIDs[0] = newID;
                            }
                            else
                            {
                                int[] tmpint = new int[AwakenBeaconIDs.Length];
                                AwakenBeaconIDs.CopyTo(tmpint, 0);
                                AwakenBeaconIDs = new int[AwakenBeaconIDs.Length + 1];
                                tmpint.CopyTo(AwakenBeaconIDs, 0);
                                AwakenBeaconIDs[AwakenBeaconIDs.Length - 1] = newID;
                                System.Array.Sort(AwakenBeaconIDs);
                            }
                        }
                    }

                    else if (msg.MessageType == (byte)MessageTypes.EmberMessage)
                    {
                        //EMBER Message
                        for (int i = 0; i < msg.Data.Length / 2; i++)
                        {
                            byte sensorID = msg.Data[i * 2];
                            UInt16 readingValue;

                            if (sensorID == 0x01)
                            {
                                //This is a temperature reading
                                readingValue = BitConverter.ToUInt16(msg.Data, i * 2);
                                readingValue &= 0xFF00;
                                readingValue >>= 8;

                                double dResult = 1.8639 - (double)readingValue / 100;

                                //From http://cache.national.com/ds/LM/LM20.pdf
                                dResult = -1481.96 + 1000 * Math.Sqrt(dResult / 3.88 + 2.1962);

                                UpdateDatabase(msg.Address.ToString(), "EMBER", "Temperature", lTimestamp, dResult);
                            }
                            else if (sensorID == 0x02)
                            {
                                int dResult = 0;
                                int readingValue1;
                                //Inside Door Sensor
                                readingValue1 = msg.Data[(i * 2) + 1];
                                if (readingValue1 >= 128)
                                {
                                    dResult = 0;
                                }
                                else
                                {
                                    dResult = 1;
                                }
                                UpdateDatabase(msg.Address.ToString(), "EMBER", "Door", lTimestamp, dResult);
                            }
                            else
                            {
                                //Unknown Ember Sensor Type.
                                Console.WriteLine("Unknown EMBER Sensor type " + sensorID + " received.");

                            }
                        }
                    }
                    else if (msg.MessageType == (byte)MessageTypes.LocationSignatureMessage)
                    {
                        UpdateLocationSignatures(msg);
                    }
                    else if (msg.MessageType == (byte)MessageTypes.HermesDataMessage)
                    {

                        ////////////////////ONGOING WORK - CIHAN - BEGIN///////////////////////////////////

                        //This is a New Generation Data Packet.

                        //Get the network header
                        UInt16 dest = BitConverter.ToUInt16(msg.Data, 0);
                        UInt16 source = BitConverter.ToUInt16(msg.Data, 2);
                        UInt16 service_TTL = BitConverter.ToUInt16(msg.Data, 4);

                        UInt16 sensorID = BitConverter.ToUInt16(msg.Data, 6);
                        byte valueLength = msg.Data[8];
                        byte fragment = msg.Data[9];
                        UInt32 timestamp = BitConverter.ToUInt32(msg.Data, 10);
                        byte[] value = new byte[valueLength];

                        for (int i = 0; i < valueLength; i++)
                        {

                            value[i] = msg.Data[14 + i];

                        }

                        if (sensorID == 0x0210)
                        {

                            UInt16 lightReading = BitConverter.ToUInt16(value, 0);
                            lightReading = (UInt16)(1e6 * (lightReading / 4096.0 * 2.5) / 100);

                            UpdateDatabase(source.ToString(), "TMOTE", "Light", timestamp, lightReading);
                            Console.WriteLine("New value " + timestamp + " old value " + latestTimeStamp + " diff " + (timestamp - latestTimeStamp));
                        }
                        else if (sensorID == 0x0110)
                        {
                            UInt16 temp1 = (UInt16)(-39.60 + 0.01 * BitConverter.ToUInt16(value, 0));

                            UpdateDatabase(source.ToString(), "TMOTE", "Temperature", timestamp, temp1);
                        }
                        else if (sensorID == 0x0310)
                        {
                            UInt16 hum1 = BitConverter.ToUInt16(value, 0);
                            hum1 = (UInt16)(-4.0 + 0.0405 * hum1 + -2.8e-6 * hum1 * hum1);

                            UpdateDatabase(source.ToString(), "TMOTE", "Humidity", timestamp, hum1);
                        }
                        else if (sensorID == 0x0100)
                        {

                            UInt16 voltage = BitConverter.ToUInt16(value, 0);
                            UInt16 batteryLife = (UInt16)GetRemainingBatteryPower(voltage / 2);

                            UpdateDatabase(source.ToString(), "TMOTE", "BatteryVoltage", timestamp, voltage);
                            UpdateDatabase(source.ToString(), "TMOTE", "BatteryLife", timestamp, batteryLife);



                        }


                        latestTimeStamp = timestamp;
                        ////////////////////ONGOING WORK - CIHAN - END///////////////////////////////////

                    }
                    else if (msg.MessageType == (byte)MessageTypes.MagneticFieldMessage)
                    {
                        // magnetic field message
                        lTimestamp = msg.CreationTime;


                        // Depending on the Channel Add a new value to the database
                        UInt16 channel = BitConverter.ToUInt16(msg.Data, 4);

                        // One message contains 10 values: use the average!
                        UInt16 Sum = 0;
                        for (int i = 0; i < 10; i++)
                        {
                            UInt16 actualValue = BitConverter.ToUInt16(msg.Data, 6 + 2 * i);
                            Sum += actualValue;

                        }

                        UInt16 magneticFieldValue = (UInt16)(Sum / 10);            // !!!!!!!!!!!!!!!!!!!!!!!!!


                        UInt16 sourceAddress = BitConverter.ToUInt16(msg.Data, 0);

                        sSensorID = sourceAddress.ToString();
                        sMoteType = "Mica2 Mote";

                        // update database
                        string variable = "";
                        switch (channel)
                        {
                            case 0:
                                variable = "MagneticFieldX";
                                break;

                            case 1:
                                variable = "MagneticFieldY";
                                break;
                            case 2:
                                variable = "MagneticFieldZ";
                                break;
                            default:
                                break;
                        }
                        UpdateDatabase(sSensorID, sMoteType, variable, lTimestamp, magneticFieldValue);

                    }
                    else if (msg.MessageType == (byte)MessageTypes.EasySenMessage)
                    {
                        // light,acoustic,infrared,temperature,accelerometer,magnetometer message

                        // data format: UInt16 light, UInt16 acoustic, UInt16 infrared,
                        //				UInt16 temp, UInt16 accX, UInt16 accY,
                        //				UInt16 magX, UInt16 magY, UInt16 voltage,
                        //				UInt16 moteID

                        // decode significant variables
                        UInt16 light = BitConverter.ToUInt16(msg.Data, 6);
                        UInt16 acoustic = BitConverter.ToUInt16(msg.Data, 8);
                        UInt16 infrared = BitConverter.ToUInt16(msg.Data, 10);
                        UInt16 temp = BitConverter.ToUInt16(msg.Data, 12);
                        temp = (UInt16)((temp - 400) / 19.53);
                        UInt16 accX = BitConverter.ToUInt16(msg.Data, 14);
                        Int16 accX1 = (Int16)((accX - 2048) * .8);
                        UInt16 accY = BitConverter.ToUInt16(msg.Data, 16);
                        Int16 accY1 = (Int16)((accY - 2048) * .8);
                        UInt16 magX = BitConverter.ToUInt16(msg.Data, 18);
                        UInt16 magY = BitConverter.ToUInt16(msg.Data, 20);
                        UInt16 voltage = BitConverter.ToUInt16(msg.Data, 22);
                        UInt16 batteryLife = (UInt16)GetRemainingBatteryPower(voltage / 2);
                        UInt16 moteID = BitConverter.ToUInt16(msg.Data, 0);
                        UInt16 last_seq_num = BitConverter.ToUInt16(msg.Data, 2);

                        // prepare variables to store
                        sSensorID = moteID.ToString();
                        sMoteType = "Telos Mote";
                        
                        //if (motestats.Contains(sSensorID))
                        //{
                        //    ((MoteStatistics)motestats[sSensorID]).update_seq_num((int)last_seq_num);
                        //}
                        //else
                        //{
                        //    motestats.Add(sSensorID, new MoteStatistics((int)last_seq_num));
                        //}
                        
                        // update DB
                        UpdateDatabase(sSensorID, sMoteType, "Visual Light", lTimestamp, light);
                        UpdateDatabase(sSensorID, sMoteType, "Acoustic", lTimestamp, acoustic);
                        UpdateDatabase(sSensorID, sMoteType, "Infrared", lTimestamp, infrared);
                        UpdateDatabase(sSensorID, sMoteType, "Temperature (C)", lTimestamp, temp);
                        UpdateDatabase(sSensorID, sMoteType, "Accelerometer X-axis (mg)", lTimestamp, accX1);
                        UpdateDatabase(sSensorID, sMoteType, "Accelerometer Y-axis (mg)", lTimestamp, accY1);
                        UpdateDatabase(sSensorID, sMoteType, "Magnetometer X-axis", lTimestamp, magX);
                        UpdateDatabase(sSensorID, sMoteType, "Magnetometer Y-axis", lTimestamp, magY);
                        UpdateDatabase(sSensorID, sMoteType, "BatteryVoltage", lTimestamp, voltage);
                        UpdateDatabase(sSensorID, sMoteType, "BatteryLife", lTimestamp, batteryLife);
                        UpdateDatabase(sSensorID, sMoteType, "SeqNo", lTimestamp, last_seq_num);
                        //UpdateDatabase(sSensorID, sMoteType, "Performance", lTimestamp, ((MoteStatistics)(motestats[sSensorID])).performance_percentage());
                    }
                    else if (msg.MessageType == (byte)MessageTypes.MultihopMessage)
                    {
                        // Simple multihop message. Commands are broadcasted using Bcast.
                        //typedef struct MultihopMsg {
                        //  uint16_t sourceaddr;
                        //  uint16_t originaddr;
                        //  int16_t seqno;
                        //  int16_t originseqno;
                        //  uint16_t hopcount;
                        //  uint8_t data[(TOSH_DATA_LENGTH - 10)];
                        //} TOS_MHopMsg;

                        // Currently, there are 2 types messages that can be encapsulated inside this packet 
                        //1.SNOOZER_TYPE2_ACK
                        //typedef struct mhack
                        // {
                        //     uint8_t  type2;//only valid is type2 == SNOOZER_TYPE2_ACK
                        //     uint8_t moteID[8];
                        // } __attribute__((packed)) *MHAckPtr, MHAck;

                        //2. SNOOZER_TYPE2_DATA 
                        //typedef struct mhdata
                        //{
                        //    uint8_t type2; // type of the 2nd layer
                        //    uint8_t reserve;
                        //    uint16_t temperature;
                        //    uint16_t light;
                        //    uint16_t humidity;
                        //    uint16_t voltage;
                        //} __attribute__((packed))  *MHDataPtr, MHData;
                        const byte SNOOZER_TYPE2_ACK = 0x04;
                        const byte SNOOZER_TYPE2_DATA = 0x05;

                        // multihop header
                        byte type2 = (byte) BitConverter.ToChar(msg.Data, 11-1);
                        if (type2 == SNOOZER_TYPE2_ACK)
                        {
                            byte []moteIDInv = new byte[8];

                            for (int i = 0; i < 8; i++)
                            {
                                moteIDInv[7 - i] = msg.Data[11 + i];
                            }

                            // 
                            //get 64-bit value
                            UInt64 moteID = BitConverter.ToUInt64(moteIDInv, 0);

                            // convert to hexastring
                            string sMoteID = moteID.ToString("X16");

                            // find it in the table
                            DataRow[] rows = tblSensors.Select("ID='" + sMoteID + "'");
                            if (rows.Length > 0)
                            {
                                rows[0]["Started"] = DateTime.Now;
                                global.mainForm.RefreshTreeView_MoteAckReceived(sMoteID);
                            }

                            int nUseCount = 0;
                            int nStartedCount = 0;
                            foreach (DataRow row in tblSensors.Rows)
                            {
                                if ((bool)row["Use"] == true)
                                {
                                    nUseCount++;
                                }

                                if (!row.IsNull("Started"))
                                {
                                    // some date from aknowlegdement is there => count it
                                    nStartedCount++;
                                }
                            }

                            if (nUseCount == nStartedCount)
                            {

                                //Stop the beaconing thread here

                                try
                                {

                                    if (sendMsgThread.IsAlive)
                                    {
                                        sendMsgThread.Abort();
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }

                            }
                            // how to stop chaning the channel?
                        }
                        else if (type2 == SNOOZER_TYPE2_DATA)
                        {
                            UInt16 temperature = BitConverter.ToUInt16(msg.Data, 13 - 1);
                            //temperature = ConvertEndinUInt16(temperature);

                            UInt16 light = BitConverter.ToUInt16(msg.Data, 15-1);
                            light = (UInt16)(1e6 * (light / 4096.0 * 2.5) / 100);
                            //UInt16 humidity = BitConverter.ToUInt16(msg.Data, 17 - 1);
                            //UInt16 voltage  = BitConverter.ToUInt16(msg.Data, 19 - 1);

                            UInt16 sensorID = BitConverter.ToUInt16(msg.Data, 3-1);
                            
                            byte parentID = (byte) BitConverter.ToChar(msg.Data, 12 - 1);

                            HermesMiddleware.DataAcquisitionServiceServer.dataItem[] MultihopData = new HermesMiddleware.DataAcquisitionServiceServer.dataItem[4];

                            MultihopData[0] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                            MultihopData[0].timestamp = System.Convert.ToString(lTimestamp);
                            MultihopData[0].value = sensorID;
                            MultihopData[1] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                            MultihopData[1].timestamp = System.Convert.ToString(lTimestamp);
                            MultihopData[1].value = temperature;
                            MultihopData[2] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                            MultihopData[2].timestamp = System.Convert.ToString(lTimestamp);
                            MultihopData[2].value = light;
                            MultihopData[3] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                            MultihopData[3].timestamp = System.Convert.ToString(lTimestamp);
                            MultihopData[3].value = parentID;
                            UpdateDatabase(sensorID.ToString(), "Multihop Tmote", "MultihopData", MultihopData);

                            foreach(DataRow row in tblMultihopSensorRooms.Rows)
                            {
                                if (sensorID == System.Convert.ToUInt16(row["sensorID"]))
                                {
                                    UpdateDatabase(sensorID.ToString(), "Multihop Tmote", (string)row["room"], lTimestamp, light);
                                }
                            }

                            //UpdateDatabase(sensorID.ToString(), "Multihop", "Temperature", lTimestamp, temperature);
                            //UpdateDatabase(sensorID.ToString(), "Multihop", "Light", lTimestamp, light);
                            ////UpdateDatabase(sensorID.ToString(), "Multihop", "humidity", lTimestamp, humidity);
                            ////UpdateDatabase(sensorID.ToString(), "Multihop", "voltage", lTimestamp, voltage);
                            //UpdateDatabase(sensorID.ToString(), "Multihop", "parentID", lTimestamp, parentID);
                        }

                    }


                    else
                    {
                        // unknown message
                        Console.WriteLine("Unknown message type " + msg.MessageType + " received.");
                    }
                }

                
                if (msg.MessageType == (byte)MessageTypes.MagneticFieldMessage)
                {
                    // magnetic field message
                    long lTimestamp = msg.CreationTime;

                    
                    // Depending on the Channel Add a new value to the database
                    UInt16 channel =  BitConverter.ToUInt16(msg.Data, 4);
                    
                    // One message contains 10 values: use the average!
                    UInt16  Sum =0;
                    for (int i=0;i<10;i++)
                    {
                        UInt16 actualValue = BitConverter.ToUInt16(msg.Data, 6 +2*i);
                        Sum += actualValue;
                    
                    }

                    UInt16 magneticFieldValue = (UInt16 )(Sum/10);            // !!!!!!!!!!!!!!!!!!!!!!!!!
                    
                    
                    UInt16 sourceAddress = BitConverter.ToUInt16(msg.Data, 0);

                    string sSensorID = sourceAddress .ToString ();
                    string sMoteType = "Mica2 Mote";

                    // update database
                    string variable = "";
                    switch(channel)
                    {
                        case 0:
                            variable =  "MagneticFieldX";                           
                            break;

                        case 1:
                            variable =  "MagneticFieldY";                           
                            break;
                        case 2:
                            variable =  "MagneticFieldZ";                           
                            break;
                        default:
                            break;
                    }
                    UpdateDatabase(sSensorID, sMoteType, variable, lTimestamp, magneticFieldValue);
                    
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp);
            }
        }



        public int nLastNumActiveSensors = 0;
        public void DSNOperations() 
        {

            lock (DSNSensorSelection.AlgLock)
            {
                // called from DSNPerformanceform.

                // this code is copied from ProcessMessage and modified

                double ErrR;
                int NumOfActiveSensor = 0, i;
                //byte[] SamplingNum = new byte[SensorSelection.MaxSennum];

                if (DSNSensorSelection.isDataValid()) // at least 3 valid data
                {
                    // if the lamp is moved, restart 
                    bool tmp = DSNSensorSelection.isLampMoved();
                    if (tmp)
                    {
                        // if detects lamp move, restart the network.
                        DSNSensorSelection.Reset();
                        ResetDSNPosEst();

                        // restart the network
                        //byte[] SamplingNum = new byte[SensorSelection.MaxSennum];
                        for (i = 0; i < SensorSelection.MaxSennum; i++)
                        {
                            // take the sampling rate from the GUI
                            SamplingNum[i] = (byte)global.mainForm.InitSamplingRate;
                        }
                        global.mainForm.SendDSNActivation(SamplingNum);
                        DSNSensorSelection.UpdateSamplingNum(SamplingNum);
                        global.mainForm.performanceForm.SetSamplingRate(SamplingNum);

                        global.mainForm.performanceForm.AddToActiveSensors(SensorSelection.MaxSennum);

                        Console.WriteLine("LampMoved");
                        // obsoleted
                        //DSNSensorSelection.ByPassTheNextIsLampMoved();
                    }
                    else
                    {
                        if (DSNSensorSelection.isInsideBoard(PosEst))
                        {
                            // reasonable position estimation
                            for (i = 0; i < SensorSelection.dim; i++)
                                InitEventPositionEstimation[i] = PosEst[i];
                        }
                        else
                        {
                            DSNSensorSelection.WeightCenterOfValidSensors(out InitEventPositionEstimation);
                            //// Obsolete: improper initial value result to local minimum (ambiguous position)
                            //// if invalid, set the InitEventPositionEstimation in the middle of the board.
                            //InitEventPositionEstimation[0] = 30.0;
                            //InitEventPositionEstimation[1] = 20.0;

                            Console.WriteLine("Outside of the board");
                        }

                        // lamp is not moved
                        if (DSNSensorSelection.EstimatePosition(InitEventPositionEstimation, out PosEst, out ErrR))
                        {

                            if (DSNSensorSelection.isAfterSensorSelection)
                            {
                                //UpdateDatabase("1000", "DSN After Selection", "x", lTimestamp, PosEst[0]);
                                //UpdateDatabase("1000", "DSN After Selection", "y", lTimestamp, PosEst[1]);
                                //UpdateDatabase("1000", "DSN After Selection", "r", lTimestamp, ErrR);

                                //dbg
                                Console.WriteLine("after (x,y,r) inch=" + PosEst[0] / 2.54 + "," + PosEst[1] / 2.54 + "," + ErrR / 2.54);

                                global.mainForm.performanceForm.SetAftertimizationPosition(PosEst, ErrR);
                                for (i = 0; i < SensorSelection.dim; i++)
                                {
                                    AfterOptPos[i] = PosEst[i];
                                    moveDist[i] = AfterOptPos[i] - BeforeOptPos[i];
                                }
                                AfterOptErr = ErrR;

                            }
                            else
                            {
                                //UpdateDatabase("1001", "DSN Before Selection", "x", lTimestamp, PosEst[0]);
                                //UpdateDatabase("1001", "DSN Before Selection", "y", lTimestamp, PosEst[1]);
                                //UpdateDatabase("1001", "DSN Before Selection", "r", lTimestamp, ErrR);
                                for (i = 0; i < SensorSelection.dim; i++)
                                {
                                    BeforeOptPos[i] = PosEst[0];
                                }
                                BeforeOptErr = ErrR;

                                global.mainForm.performanceForm.SetBeforeOptimizationPosition(PosEst, ErrR);

                                //dbg
                                Console.WriteLine("before (x,y,r) inch=" + PosEst[0] / 2.54 + "," + PosEst[1] / 2.54 + "," + ErrR / 2.54);

                                if (DSNSensorSelection.OptimizeRealSamplingRate(out SamplingNum))
                                {
                                    // guarantee 3 sensors being selected.
                                    global.mainForm.SendDSNActivation(SamplingNum);
                                    DSNSensorSelection.UpdateSamplingNum(SamplingNum);

                                    string strdbg = "Sample Num: ";
                                    for (i = 0; i < SensorSelection.MaxSennum; i++)
                                    {
                                        strdbg += i + ":" + SamplingNum[i] + " ";
                                    }
                                    Console.WriteLine(strdbg);

                                    NumOfActiveSensor = 0;
                                    for (i = 0; i < SensorSelection.MaxSennum; i++)
                                    {
                                        if (SamplingNum[i] != 0)
                                        {
                                            NumOfActiveSensor++;
                                        }
                                    }

                                    nLastNumActiveSensors = NumOfActiveSensor;
                                    //global.mainForm.performanceForm.SetSamplingRate(SamplingNum);
                                    //global.mainForm.performanceForm.AddToActiveSensors(NumOfActiveSensor);
                                    //dbg
                                    Console.WriteLine("Num of active sensors " + NumOfActiveSensor);
                                }
                            }

                            global.mainForm.performanceForm.AddToPrecision(ErrR);
                            global.mainForm.performanceForm.SetSamplingRate(SamplingNum);
                            global.mainForm.performanceForm.AddToActiveSensors(nLastNumActiveSensors);

                        }
                        else
                        {
                            // position estimation is not valid
                            DSNSensorSelection.WeightCenterOfValidSensors(out InitEventPositionEstimation);
                            if (DSNSensorSelection.TooManyInvalidPosEstimation())
                            {
                                // the lamp maybe moved and the isLampMoved function does not work. 
                                // reset the network

                                DSNSensorSelection.Reset();
                                ResetDSNPosEst();

                                // restart the network
                                //byte[] SamplingNum = new byte[SensorSelection.MaxSennum];
                                for (i = 0; i < SensorSelection.MaxSennum; i++)
                                {
                                    // take the sampling rate from the GUI
                                    if (global.mainForm.InitSamplingRate > 0)
                                    {
                                        SamplingNum[i] = (byte)global.mainForm.InitSamplingRate;
                                    }
                                    else
                                    {
                                        SamplingNum[i] = (byte)17; // 17*15=255
                                    }
                                }
                                global.mainForm.SendDSNActivation(SamplingNum);
                                DSNSensorSelection.AssignUniformSamplingRate(global.mainForm.InitSamplingRate);

                                global.mainForm.performanceForm.SetSamplingRate(SamplingNum);
                                global.mainForm.performanceForm.AddToActiveSensors(SensorSelection.MaxSennum);

                                Console.WriteLine("Too many invalide position estimations. Reset");
                            }

                        }
                    }// end of "if (DSNSensorSelection.isDataValid())... else ..."
                }
                else
                {
                    // data not valid, should we restart?
                    if (DSNSensorSelection.TooManyInvalidDataSets())
                    {
                        // restart the optimization iteration. trigger all the sensors to sample!
                        DSNSensorSelection.Reset();
                        //byte[] SamplingNum = new byte[SensorSelection.MaxSennum];
                        for (i = 0; i < SensorSelection.MaxSennum; i++)
                        {
                            SamplingNum[i] = (byte)global.mainForm.InitSamplingRate;
                        }
                        // all the sensors start to collect samples
                        global.mainForm.SendDSNActivation(SamplingNum);
                        DSNSensorSelection.UpdateSamplingNum(SamplingNum);

                        global.mainForm.performanceForm.AddToActiveSensors(SensorSelection.MaxSennum);
                        // oct. 23
                        global.mainForm.performanceForm.SetSamplingRate(SamplingNum);

                        Console.WriteLine("Too many invlid data sets. Reset."); 
                    }
                }
            } // end lock
        }

        public void ResetAwakenBeaconIDs()
        {
            AwakenBeaconIDs = new int[1];
        }

        //Converts an array of rows to an array of dataItems
        private HermesMiddleware.DataAcquisitionServiceServer.dataItem[] rows2dataItem(DataRow[] rows)
        {
            HermesMiddleware.DataAcquisitionServiceServer.dataItem[] returnItem = new HermesMiddleware.DataAcquisitionServiceServer.dataItem[rows.Length];

            for (int i = 0; i < rows.Length; i++)
            {
                returnItem[i] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                returnItem[i].value = ReflectiveParseWithType(rows[i]["Value"].ToString(), rows[0]["Type"].ToString());
                returnItem[i].timestamp = rows[i]["Time"].ToString();

            }

            return returnItem;
        }


        private void PrepareNotifications()
        {
            string sBaseStationID = System.Environment.MachineName;

            try
            {
                // find updated variables
				DataRow[] rows;
				lock (tblArchive)
				{
					 rows = tblArchive.Select("Updated='True'");
				}
                foreach (DataRow row in rows)
                {
                    // check if value change happened
                    if ((bool)row["Updated"])
                    {
                        string sVariable = row["Variable"].ToString();
                        string sSensorID = row["SensorID"].ToString();

                        string sStatement = String.Format("SensorID='{0}' AND Variable='{1}'", sSensorID, sVariable);
                        lock (tblArchive)
                        {
                            DataRow[] allValues = tblArchive.Select(sStatement);

                            Event myEvent = new Event(
                                "",
                                global.sDefaultService,
                                sVariable,
                                sBaseStationID,
                                sSensorID,
                                rows2dataItem(allValues));

                            // add event to interrested subscribers
                            global.AddEvent2Subscribers(myEvent);

                            foreach (DataRow added in allValues)
                            {

                                added["Changed"] = false;
                                //rowUpdated["Changed"] = false;
                            }
                        }
                    }

                    // reset update flag
                    row["Updated"] = false;
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp);
            }
        }

        private void CreateXMLfromDB()
        {
            try
            {
                // create XML file from DB
                string sFileName = sXMLFilePath + global.sDefaultService + ".XML";
                XmlTextWriter writer = new XmlTextWriter(sFileName, System.Text.Encoding.UTF8);
                writer.Formatting = Formatting.Indented;

                // main tag
                writer.WriteStartElement("mpi:P2PWebService");
                writer.WriteAttributeString("xmlns:mpi", "http://www.search-on-siemens.com/MPI");

                // service name tag
                writer.WriteElementString("P2PWS", global.sDefaultService);

                string sSensorID = "";
                int field = 0;

                // write all variables
                foreach (DataRow row in tblArchive.Select("", "SensorID ASC"))
                {
                    if (row["SensorID"].ToString() != sSensorID)
                    {
                        sSensorID = row["SensorID"].ToString();
                        field = 1;

                        // new sensor => prepare default tags

                        // MoteType tag
                        writer.WriteStartElement("MoteType");
                        writer.WriteAttributeString("Field", field.ToString());
                        writer.WriteAttributeString("id", sSensorID);
                        writer.WriteAttributeString("parseXML", "String_Exact");
                        writer.WriteString(row["MoteType"].ToString());
                        writer.WriteEndElement();
                        field++;

                        // BaseStation tag
                        writer.WriteStartElement("BaseStation");
                        writer.WriteAttributeString("Field", field.ToString());
                        writer.WriteAttributeString("id", sSensorID);
                        writer.WriteAttributeString("parseXML", "String_Exact");
                        writer.WriteString(System.Environment.MachineName);
                        writer.WriteEndElement();
                        field++;

                        // MoteID tag
                        writer.WriteStartElement("MoteID");
                        writer.WriteAttributeString("Field", field.ToString());
                        writer.WriteAttributeString("id", sSensorID);
                        writer.WriteAttributeString("parseXML", "String_Exact");
                        writer.WriteString(sSensorID);
                        writer.WriteEndElement();
                        field++;
                    }

                    // variable tag itself
                    writer.WriteStartElement(row["Variable"].ToString());
                    writer.WriteAttributeString("Field", field.ToString());
                    writer.WriteAttributeString("id", sSensorID);
                    writer.WriteAttributeString("parseXML", "Float_Range");
                    string sFileTimeUTC = row["Time"].ToString();
                    // put time in filetime UTC format as attribute
                    writer.WriteAttributeString("time", sFileTimeUTC);
                    writer.WriteString(row["Value"].ToString());
                    writer.WriteEndElement();
                    field++;
                }

                // close main tag
                writer.WriteEndElement();

                // close
                writer.Close();
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp);
            }
        }

        //This accepts an array of values
        void UpdateDatabase(string sSensorID, string sMoteType, string sVariable, HermesMiddleware.DataAcquisitionServiceServer.dataItem[] dI)
        {
            lock (tblArchive)
            {
                try
                {
                    // find the variable, if exists
                    string sStatement = String.Format("SensorID='{0}' AND Variable='{1}'", sSensorID, sVariable);
                    DataRow[] rows = tblArchive.Select(sStatement);
                    bool bNewValue = false;

                    //Check if the values coming up are different.
                    if (rows.Length != dI.Length || sVariable == "RFID_Additions" || sVariable == "RFID_Deletions")
                    {

                        bNewValue = true;

                    }
                    else
                    {

                        //Check one-by-one

                        for (int j = 0; j < dI.Length; j++)
                        {
                            bool bSame;

                            bSame = false;

                            for (int i = 0; i < rows.Length; i++)
                            {
                                if (rows[j]["Value"].ToString().CompareTo(dI[j].value.ToString()) == 0)
                                {
                                    //We found the value								
                                    bSame = true;
                                    rows[j]["Updated"] = true;
                                    rows[j]["Changed"] = false;
                                    global.mainForm.Invoke((dummyDelegate)delegate() { global.mainForm.dataGridVariables.Refresh(); });
                                    break;
                                }
                            }

                            if (!bSame)
                            {
                                //We could not found this entry in the table.
                                bNewValue = true;
                                break;
                            }

                        }


                    }

                    if (bNewValue)
                    {

                        //Remove all of these rows
                        foreach (DataRow dr in rows)
                        {
                            global.mainForm.Invoke((dummyDelegate)delegate() { tblArchive.Rows.Remove(dr); });
                            //Our own queue.Remove() function.
                            for (int i = 0; i < queueValueUpdates.Count; i++)
                            {
                                DataRow tempRow;

								lock (queueValueUpdates)
								{
									if ((tempRow = (DataRow)queueValueUpdates.Dequeue()) != dr)
									{
										queueValueUpdates.Enqueue(tempRow);
									}
									else
									{
										break;
									}
								}
                            }

                        }

                        //Add all of the new rows
                        for (int i = 0; i < dI.Length; i++)
                        {
                            DataRow newRow = tblArchive.NewRow();
                            newRow["SensorID"] = sSensorID;
                            newRow["MoteType"] = sMoteType;
                            newRow["Variable"] = sVariable;
                            newRow["Time"] = dI[i].timestamp.ToString();
                            newRow["Value"] = dI[i].value.ToString();
                            newRow["Updated"] = true;
                            newRow["Changed"] = true;
                            newRow["Type"] = dI[i].value.GetType().ToString();
                            global.mainForm.Invoke((dummyDelegate)delegate() { tblArchive.Rows.Add(newRow); });
                            global.mainForm.Invoke((dummyDelegate)delegate() { global.mainForm.dataGridVariables.Refresh(); });

                            //Console.WriteLine("Value " + newRow["Value"] + " timestamp " + newRow["Time"]);

                            // enqueue the changed row for main thread & timer
                            queueValueUpdates.Enqueue(newRow);

                        }
                        Console.WriteLine(tblArchive.Rows.Count);
                        global.mainForm.dataGridVariables.Invoke((dummyDelegate)delegate() { global.mainForm.dataGridVariables.Refresh(); });
                        // publish variable
                        Publish(sVariable);
                    }
                    else
                    {

                        foreach (DataRow dr in rows)
                        {

                            dr["Updated"] = true;
                            dr["Changed"] = false;
                            global.mainForm.Invoke((dummyDelegate)delegate() { global.mainForm.dataGridVariables.Refresh(); });
                        }

                    }
                }
                catch (Exception exp)
                {
                    Console.WriteLine(exp);
                }
            }
        }

        private bool IsRFIDTagTooOld(DateTime now, DateTime then)
        {
            TimeSpan timeDiff = now.Subtract(then);
            if ((timeDiff.Milliseconds + timeDiff.Seconds * 1000 + timeDiff.Minutes * 60000) > rfidTimeout)
                return true;
            return false;
        }



        public object ReflectiveParseWithType(string sValue, string sType)
        {

            //We need a type conversion here.

            //Get the associated type
            Type typeObject = System.Type.GetType(sType);

            //Get the parse method for this type
            Type[] typeArray = new Type[1];
            typeArray.SetValue(typeof(string), 0);

            System.Reflection.MethodInfo parseMetod = typeObject.GetMethod("Parse", typeArray);

            if (parseMetod != null)
            {
                return parseMetod.Invoke(new object(), new object[] { sValue });
            }
            else
            {

                return sValue;
            }


        }

        UInt16 oldvalue = 0;

        public void UpdateDatabase(string sSensorID, string sMoteType, string sVariable, long lTimestamp, object dValue)
        {
            lock (tblArchive)
            {
                try
                {
                    // find the variable, if exists7
                    string sStatement = String.Format("SensorID='{0}' AND Variable='{1}'", sSensorID, sVariable);
                    DataRow[] rows = tblArchive.Select(sStatement);
                    DataRow rowUpdated = null;

                    if (rows != null && rows.Length > 0)
                    {
 
                        if (sVariable.Contains("MagneticFieldY") && (oldvalue != (UInt16)dValue)) {


                            Console.Write("asdf");
                            oldvalue =(UInt16 )dValue;
                        
                        }

                        // variable found => update it (just one row is expected here)
                        rowUpdated = rows[0];
                        rowUpdated["Time"] = lTimestamp.ToString();

                        object dOldValue = ReflectiveParseWithType(rowUpdated["Value"].ToString(), rowUpdated["Type"].ToString());// (object)rowUpdated["Value"];

                        rowUpdated["Value"] = dValue.ToString();
                        rowUpdated["Type"] = dValue.GetType().ToString();
                        rowUpdated["Updated"] = true;

                        if (((IComparable)dValue).CompareTo(dOldValue) != 0)
                        {
                            // value changed
                            rowUpdated["Changed"] = true;
                        }
                        else
                        {
                            // the same value
                            //rowUpdated["Changed"] = false;
                        }

                    }
                    else
                    {


                        // variable not found => add new row
                        rowUpdated = tblArchive.NewRow();
                        rowUpdated["SensorID"] = sSensorID;
                        rowUpdated["MoteType"] = sMoteType;
                        rowUpdated["Variable"] = sVariable;
                        rowUpdated["Time"] = lTimestamp.ToString();
                        rowUpdated["Value"] = dValue.ToString();
                        rowUpdated["Updated"] = true;
                        rowUpdated["Changed"] = true;
                        rowUpdated["Type"] = dValue.GetType().ToString();
                        tblArchive.Rows.Add(rowUpdated);
                    }

                    // enqueue the changed row for main thread & timer
                    queueValueUpdates.Enqueue(rowUpdated);
                }
                catch (Exception exp)
                {
                    Console.WriteLine(exp);
                }
            }

            try
            {

                // publish variable
                Publish(sVariable);

            }
            catch (Exception e)
            {

                Console.WriteLine(e);

            }
        }

        private void Publish(string sVariable)
        {
            // local host 
            string sLocalHost = System.Environment.MachineName;

            // prepare variable array
            string[] sVariables = null;
            if (sVariable != null && sVariable != "")
            {
                sVariables = new string[1];
                sVariables[0] = sVariable;
            }

            if (global.sP2PMaster == "" || String.Compare(global.sP2PMaster, sLocalHost, true) == 0)
            {
                // this is P2P master => call directly Publish
                global.Publish(global.sDefaultService, sVariables, sLocalHost);
            }
            else
            {
                // call P2P master
                try
                {
                    // initialize service proxy
                    HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService serviceProxy = new HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService();
                    serviceProxy.Url = "http://" + global.sP2PMaster + ":" + global.portNumber + global.sVirtRoot + global.sASMX;

                    // call service proxy method
                    serviceProxy.Publish(global.sDefaultService, sVariables, sLocalHost);
                }
                catch (Exception exp)
                {
                    Console.WriteLine(exp);
                }
            }

        }

        /// <summary>
        /// updates the position of the mote with the given marker
        /// </summary>
        /// <param name="markerId">the id of a marker</param>
        /// <param name="x">x-coordinate of a mote</param>
        /// <param name="y">y-coordinate of a mote</param>
        /// <param name="z">z-coordinate of a mote</param>
        public void UpdateMotePosition(int markerId, int xRough, int yRough, int zRough,
            int xPrecise, int yPrecise, int zPrecise)
        {
            DataRow[] rows = tblMarkers.Select("MarkerNumber=" + markerId);
            if (rows.Length == 0)
            {
                return;
            }
            HermesMiddleware.DataAcquisitionServiceServer.dataItem[] position = new HermesMiddleware.DataAcquisitionServiceServer.dataItem[6];
            position[0] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
            position[1] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
            position[2] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
            position[3] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
            position[4] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
            position[5] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();

            long timestamp = DateTime.Now.Ticks;
            position[0].timestamp = timestamp.ToString();
            position[0].value = (UInt16)xRough;
            position[1].timestamp = timestamp.ToString();
            position[1].value = (UInt16)yRough;
            position[2].timestamp = timestamp.ToString();
            position[2].value = (UInt16)zRough;
            position[3].timestamp = timestamp.ToString();
            position[3].value = (UInt16)xPrecise;
            position[4].timestamp = timestamp.ToString();
            position[4].value = (UInt16)yPrecise;
            position[5].timestamp = timestamp.ToString();
            position[5].value = (UInt16)zPrecise;
            UpdateDatabase(rows[0]["SensorID"].ToString(), "TelosB", "Position", position);
        }

        public void PrintStatus(string sStatus)
        {
            if (bGUI)
            {
                global.mainForm.PrintStatus(sStatus);
            }
        }

        private float Voltage2Weight(UInt16 voltage)
        {
            const float maxVoltage = 2.5f;          // maximum voltage that can be measured
            const float voltageAtNoWeight = 0.415f; // the voltage at the scale if there's no weight on it
            const float voltageAt100Lb = 0.62f;     // the voltage at the scale at a weight of 100lb
            const float kgLbFactor = 0.45359f;      // the factor between kg and lb

            float voltageAnalog = maxVoltage * voltage / 4096f;

            if (voltageAnalog < 0.1) // too low voltage, wrong measurement
            {
                return -1.0f;
            }

            float weightLb = 0.0f;
            float weightKg = 0.0f;

            if (voltageAnalog < 1.03f * voltageAtNoWeight) // weight close to zero kg
            {
                return 0.0f;
            }

            weightLb = (voltageAnalog - voltageAtNoWeight) * 100f / (voltageAt100Lb - voltageAtNoWeight);
            weightKg = weightLb * kgLbFactor;

            return weightLb;
        }

        private void GenerateBatteryLifeLookUpTable()
        {
            // Generate look up table for battery life assesment.
            // Number of entries in look up table: LOOKUPTABLE (define in TinyXMLDlg.h)

            int maxVoltage;
            int i;
            int minVoltage;
            int minTime;
            int maxTime;
            int voltageDecr;
            double slope;
            double currentVoltage;
            double currentTime;
            int BatteryLife = 3000; // in mins from energizer curve

            // Number of piece-wise linear segments: 5.
            // Each segment has equal number of points

            segmentLength = tableSize / 5;

            // data taken from the energizer curve

            maxVoltage = 1500;
            minVoltage = 1400;
            maxTime = (int)(4 * 1.25 * 60);
            minTime = 0;
            slope = ((double)(maxTime - minTime)) / ((double)(minVoltage - maxVoltage));
            voltageDecr = (maxVoltage - minVoltage) / segmentLength;  // 20 points per each piece-wise linear segment

            for (i = 0; i < segmentLength; i++)
            {
                currentVoltage = (double)(maxVoltage - i * voltageDecr);
                currentTime = (double)minTime + (slope * (currentVoltage - maxVoltage));
                BatterLifeLookUpTable[i] = BatteryLife - (int)currentTime;
            }

            maxVoltage = minVoltage;  // 1400
            minVoltage = 1300;
            minTime = maxTime; // 4*1.25*60
            maxTime = (int)(minTime + (9 * 1.25 * 60));
            slope = ((double)(maxTime - minTime)) / ((double)(minVoltage - maxVoltage));
            voltageDecr = (maxVoltage - minVoltage) / segmentLength;  // 20 points per each piece-wise linear segment

            for (i = 0; i < segmentLength; i++)
            {
                currentVoltage = (double)(maxVoltage - i * voltageDecr);
                currentTime = (double)minTime + (slope * (currentVoltage - maxVoltage));
                BatterLifeLookUpTable[i + segmentLength] = BatteryLife - (int)currentTime;
            }

            maxVoltage = minVoltage; // 1300
            minVoltage = 1200;
            minTime = maxTime; // (4*1.25*60) + (9*1.25*60);
            maxTime = (int)(minTime + (14 * 1.25 * 60));
            slope = ((double)(maxTime - minTime)) / ((double)(minVoltage - maxVoltage));
            voltageDecr = (maxVoltage - minVoltage) / segmentLength;  // 20 points per each piece-wise linear segment

            for (i = 0; i < segmentLength; i++)
            {
                currentVoltage = (double)(maxVoltage - i * voltageDecr);
                currentTime = (double)minTime + (slope * (currentVoltage - maxVoltage));
                BatterLifeLookUpTable[i + (segmentLength * 2)] = BatteryLife - (int)currentTime;
            }

            maxVoltage = minVoltage; // 1200
            minVoltage = 1100;
            minTime = maxTime; // (4*1.25*60) + (9*1.25*60) + (14*1.25*60); 
            maxTime = (int)(minTime + (7 * 1.25 * 60));
            slope = ((double)(maxTime - minTime)) / ((double)(minVoltage - maxVoltage));
            voltageDecr = (maxVoltage - minVoltage) / segmentLength;  // 20 points per each piece-wise linear segment

            for (i = 0; i < segmentLength; i++)
            {
                currentVoltage = (double)(maxVoltage - i * voltageDecr);
                currentTime = (double)minTime + (slope * (currentVoltage - maxVoltage));
                BatterLifeLookUpTable[i + (segmentLength * 3)] = BatteryLife - (int)currentTime;
            }

            maxVoltage = minVoltage; // 1100
            minVoltage = 900;
            minTime = maxTime; // (4*1.25*60) + (9*1.25*60) + (14*1.25*60) + (7*1.25*60); 
            maxTime = (int)(minTime + (6 * 1.25 * 60));
            slope = ((double)(maxTime - minTime)) / ((double)(minVoltage - maxVoltage));
            voltageDecr = (maxVoltage - minVoltage) / segmentLength;  // 20 points per each piece-wise linear segment

            for (i = 0; i < segmentLength; i++)
            {
                currentVoltage = (double)(maxVoltage - i * voltageDecr);
                currentTime = (double)minTime + (slope * (currentVoltage - maxVoltage));
                BatterLifeLookUpTable[i + (segmentLength * 4)] = BatteryLife - (int)currentTime;
            }
        }

        int GetRemainingBatteryPower(int currentBatteryVoltage)
        {
            int lookupIndex;
            int voltageDecr;
            int lookUpVoltage;

            if ((currentBatteryVoltage <= 1500) && (currentBatteryVoltage > 1400))
            {
                voltageDecr = 100 / segmentLength;
                lookUpVoltage = 1500 - currentBatteryVoltage;
                lookupIndex = lookUpVoltage / voltageDecr;
                return (BatterLifeLookUpTable[lookupIndex]);
            }
            else if ((currentBatteryVoltage <= 1400) && (currentBatteryVoltage > 1300))
            {
                voltageDecr = 100 / segmentLength;
                lookUpVoltage = 1400 - currentBatteryVoltage;
                lookupIndex = lookUpVoltage / voltageDecr;
                return (BatterLifeLookUpTable[segmentLength + lookupIndex]);
            }
            else if ((currentBatteryVoltage <= 1300) && (currentBatteryVoltage > 1200))
            {
                voltageDecr = 100 / segmentLength;
                lookUpVoltage = 1300 - currentBatteryVoltage;
                lookupIndex = lookUpVoltage / voltageDecr;
                return (BatterLifeLookUpTable[(segmentLength * 2) + lookupIndex]);
            }
            else if ((currentBatteryVoltage <= 1200) && (currentBatteryVoltage > 1100))
            {
                voltageDecr = 100 / segmentLength;
                lookUpVoltage = 1200 - currentBatteryVoltage;
                lookupIndex = lookUpVoltage / voltageDecr;
                return (BatterLifeLookUpTable[(segmentLength * 3) + lookupIndex]);
            }
            else if ((currentBatteryVoltage <= 1100) && (currentBatteryVoltage > 900))
            {
                voltageDecr = 200 / segmentLength;
                lookUpVoltage = 1100 - currentBatteryVoltage;
                lookupIndex = lookUpVoltage / voltageDecr;
                return (BatterLifeLookUpTable[(segmentLength * 4) + lookupIndex]);
            }
            else
            {
                return 0;
            }
        }

        // Comment: Close all service, including stop the serial port
        // Compare: Stop()
        
        public void Close()
        {
            // set closing flag
            bClosing = true;

            // stop service

            for (int i = 0; i < 20; i++)
            {
                //Send several times to avoid any collisions.
                Stop();
                Thread.Sleep(50);
            }
            // try to store sensor table
            dbAccess.Update(tblSensors);

            // disconnect from P2P network
            DisconnectP2PNetwork();
        }

        public void ShowMessageBox(string sMessage, MessageBoxIcon icon)
        {
            if (bGUI)
            {
                global.mainForm.ShowMessageBox(sMessage, icon);
            }
        }

        //This thread sends the beacons.
        void SendMsgThread()
        {
            MainService mainService = this;

            ////////////////////ONGOING WORK - CIHAN - BEGIN///////////////////////////////////

            TOS_Msg queryMessage = new Telos_Msg();

            byte[] queryData = new byte[4];

            queryData[0] = 0x10;
            queryData[1] = 0x01;
            queryData[2] = 0x00;
            queryData[3] = 0x00;

            queryMessage.Init(PacketTypes.P_PACKET_ACK,
                0,
                MessageTypes.HermesDataRequestMessage,
                MoteAddresses.BroadcastAddress,				// broadcast to all
                (MoteGroups)groupID,
                //MoteGroups.DefaultRTLSBeaconGroup,			// use default group (noninitialized motes have default group
                queryData);

            ////////////////////ONGOING WORK - CIHAN - END///////////////////////////////////


            while (true)
            {
                if (mainService.bClosing) return;

                if (mainService != null && mainService.managementMsg != null && !mainService.bClosing)
                {
                    // send prepared TOS message(s)

                    for (int i = 0; i < managementMsgArray.Count; i++)
                    {
                        mainService.serial.SendMsg((HermesMiddleware.MoteLibrary.TOS_Msg)(mainService.managementMsgArray[i]));
                    }
                    ////////////////////ONGOING WORK - CIHAN - BEGIN///////////////////////////////////
                    //mainService.serial.SendMsg(queryMessage);
                    ////////////////////ONGOING WORK - CIHAN - END///////////////////////////////////
                }

                Thread.Sleep(nMessageInterval);
            }
        }


        void WorkerThread()
        {
            while (true)
            {
                try
                {
                    // start data collecting for a period of time

                    // get current tick count
                    int nTickcount = System.Environment.TickCount;
                    nLastCollectionTickCount = nTickcount;

                    // get number of messages
                    int nMessageCount = queueMessages.Count;
                    int nCount = nMessageCount;

                    if (nMessageCount > 0)
                    {
                        // do data collecting while there are messages but no longer than nDataCollectionInterval milliseconds
                        while (nMessageCount > 0 && nTickcount < nLastCollectionTickCount + nDataCollectionInterval)
                        {
                            // get message from the queue
                            TOS_Msg msg = (TOS_Msg)queueMessages.Dequeue();

                            // process it
                            ProcessMsg(msg);

                            // prepare notifications for subscribers
                            PrepareNotifications();

                            // get tickcount
                            nTickcount = System.Environment.TickCount;

                            // decrement message count
                            nMessageCount--;
                        }

                        // notify subscribers
                        global.NotifySubscribers();

                        lProcessedMsg += nCount - nMessageCount;
                        lToDoMsg = queueMessages.Count;

                        if (bGUI)
                        {
                            global.mainForm.PrintMsgStatus(-1, -1, lProcessedMsg, lToDoMsg);
                        }

                        //Console.WriteLine("Processed messages: " + (nCount - nMessageCount).ToString() + ", to do: " + lToDoMsg.ToString());
                    }
                    else
                    {
                        // nothing to do => just sleep little bit
                        Thread.Sleep(10);
                    }
                }
                catch (System.Threading.ThreadAbortException)
                {
                    Console.WriteLine("Worker thread for message processing stopped.");
                    break;
                }
                catch (Exception exp)
                {
                    Console.WriteLine(exp);
                    break;
                }
            }
        }

        public bool UpdateSettings()
        {

            // get demo mode flag
            bDemoMode = settings.GetBoolean("Demo Mode");

            // get group ID
            try
            {
                string sGroupID = settings.GetString("Group ID");
                groupID = byte.Parse(sGroupID, System.Globalization.NumberStyles.HexNumber);
            }
            catch (Exception exp)
            {
                ShowMessageBox("Wrong group ID - must be a hexa number from 00 to FF!", MessageBoxIcon.Error);
                Console.WriteLine(exp);
                return false;
            }

            // get message interval
            try
            {
                nMessageInterval = (int)settings.GetDouble("Message Interval");
                if (nMessageInterval < 0)
                    throw new Exception("Message interval - invalid milliseconds.");
            }
            catch (Exception exp)
            {
                ShowMessageBox("Message interval - invalid milliseconds.", MessageBoxIcon.Error);
                Console.WriteLine(exp);
                return false;
            }

            // get demo mode interval
            try
            {
                nDemoModeInterval = (int)settings.GetDouble("Demo Mode Interval");
                if (nDemoModeInterval < 0)
                    throw new Exception("Demo mode interval - invalid milliseconds.");
            }
            catch (Exception exp)
            {
                ShowMessageBox("Demo mode interval - invalid milliseconds.", MessageBoxIcon.Error);
                Console.WriteLine(exp);
                return false;
            }

            // get data collection interval
            try
            {
                nDataCollectionInterval = (int)settings.GetDouble("Data Collection Interval");
                if (nDataCollectionInterval <= 0)
                    throw new Exception("Data collection interval - invalid milliseconds.");
            }
            catch (Exception exp)
            {
                ShowMessageBox("Data collection interval - invalid milliseconds.", MessageBoxIcon.Error);
                Console.WriteLine(exp);
                return false;
            }

            // get base mote port
            try
            {
                nBaseMotePort = (short)settings.GetDouble("Base Mote Port");
                if (nBaseMotePort < 0 || nBaseMotePort > 255)
                    throw new Exception("Invalid base mote port.");
            }
            catch (Exception exp)
            {
                ShowMessageBox("Invalid base mote port.", MessageBoxIcon.Error);
                Console.WriteLine(exp);
                return false;
            }

            try
            {
                rfidTimeout = settings.GetDouble("RFIDDeletionDelay");
                if (rfidTimeout < 0)
                    throw new Exception("Invalid RFID deletion delay");
            }
            catch (Exception exp)
            {
                ShowMessageBox("Invalid RFID deletion delay.", MessageBoxIcon.Error);
                Console.WriteLine(exp);
                return false;
            }

            // get demo mode interval
            try
            {
                nBaudRate = (int)settings.GetDouble("Base Mote Baud Rate");
                if (nBaudRate < 0)
                    throw new Exception("Invalid Base Mote Baud Rate.");
            }
            catch (Exception exp)
            {
                ShowMessageBox("Invalid Base Mote Baud Rate.", MessageBoxIcon.Error);
                Console.WriteLine(exp);
                return false;
            }

            // get activation flag
            bActivateCommunicationUponStart = settings.GetBoolean("Activate Communication Upon Start");

            // get P@P master
            global.sP2PMaster = settings.GetString("P2P Master");

            // get service name
            global.sDefaultService = settings.GetString("Service Name");

            return true;
        }

        short DefineBaseMotePort(ArrayList nFoundVirtualPorts)
        {
            if (nFoundVirtualPorts.Contains(nBaseMotePort))
            {
                return nBaseMotePort;
            }
            else
            {
                nBaseMotePort = (short)nFoundVirtualPorts[nFoundVirtualPorts.Count - 1];
                // save new default base mote port
                settings.SetDouble("Base Mote Port", nBaseMotePort);
                return nBaseMotePort;
            }
        }

        public bool ConnectP2PNetwork()
        {
            if (!global.bP2PConnected)
            {
                // local host 
                string sLocalHost = System.Environment.MachineName;

                // register this service as peer in P2P network
                if (global.sP2PMaster == "" || String.Compare(global.sP2PMaster, sLocalHost, true) == 0)
                {
                    // this is P2P master => call directly
                    global.RegisterPeer(sLocalHost, global.sDefaultService);
                    global.bP2PConnected = true;
                    global.bP2PMaster = true;
                }
                else
                {
                    try
                    {
                        // initialize service proxy
                        HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService serviceProxy = new HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService();
                        serviceProxy.Url = "http://" + global.sP2PMaster + ":" + global.portNumber + global.sVirtRoot + global.sASMX;

                        // call service proxy
                        serviceProxy.RegisterPeer(sLocalHost, global.sDefaultService);
                        global.bP2PConnected = true;
                        global.bP2PMaster = false;

                        SynchronizePeerClock();

                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine(exp);
                    }
                }
            }

            return global.bP2PConnected;
        }

        public bool DisconnectP2PNetwork()
        {
            if (global.bP2PConnected)
            {
                // local host 
                string sLocalHost = System.Environment.MachineName;

                // unregister this service as peer in P2P network
                if (global.sP2PMaster == "" || String.Compare(global.sP2PMaster, sLocalHost, true) == 0)
                {
                    // this is P2P master => call directly
                    global.UnregisterPeer(sLocalHost);
                }
                else
                {
                    try
                    {
                        // initialize service proxy
                        HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService serviceProxy = new HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService();
                        serviceProxy.Url = "http://" + global.sP2PMaster + ":" + global.portNumber + global.sVirtRoot + global.sASMX;

                        // call service proxy
                        serviceProxy.UnregisterPeer(sLocalHost);
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine(exp);
                        return false;
                    }
                }

                global.bP2PConnected = false;
            }

            return true;
        }

        private void UpdateLocationSignatures(TOS_Msg message)
        {
            UInt16 moteId = BitConverter.ToUInt16(message.Data, 0);
            List<RSSISignature> newSignatures = new List<RSSISignature>();
            for (int sigCount = 0; sigCount < 3; sigCount++)
            {
                UInt16 beaconId = BitConverter.ToUInt16(message.Data, 2 + 8 * sigCount);
                UInt16 power = message.Data[5 + 8 * sigCount];
                Int16 rssi = BitConverter.ToInt16(message.Data, 6 + 8 * sigCount);
                bool added = false;
                foreach (RSSISignature signature in rssiSignatures_[moteId])
                {
                    if (signature.beaconId == beaconId)
                    {
                        if (!signature.rssiValues.ContainsKey(power))
                        {
                            signature.rssiValues.Add(power, new List<short>());
                        }
                        signature.rssiValues[power].Add(rssi);
                        added = true;
                    }
                }
                if (!added)
                {
                    rssiSignatures_.Add(moteId, new List<RSSISignature>());
                    RSSISignature newSignature = new RSSISignature();
                    newSignature.beaconId = beaconId;
                    newSignature.rssiValues = new SortedList<ushort, List<short>>();
                    newSignature.rssiValues.Add(power, new List<short>());
                    newSignature.rssiValues[power].Add(rssi);
                    rssiSignatures_[moteId].Add(newSignature);
                }
            }
            UpdateLocationInformation();
        }

        private void UpdateLocationInformation()
        {
            foreach (UInt16 moteId in rssiSignatures_.Keys)
            {
                if (rssiSignatures_[moteId].Count < 4)
                {
                    continue;
                }
                int sigCount = 0;
                foreach (RSSISignature signature in rssiSignatures_[moteId])
                {
                    sigCount += signature.rssiValues.Count;
                }
                if (sigCount < 20)
                {
                    continue;
                }
                SortedList<IPoint2D, uint> distanceEstimations = new SortedList<IPoint2D, uint>();
                foreach (RSSISignature signature in rssiSignatures_[moteId])
                {
                    uint estimatedDistance = 0;
                    foreach (UInt16 powerLevel in signature.rssiValues.Keys)
                    {
                        int averageRSSI = 0;
                        foreach (Int16 rssi in signature.rssiValues[powerLevel])
                        {
                            averageRSSI += rssi;
                        }
                        averageRSSI /= signature.rssiValues[powerLevel].Count;
                        estimatedDistance += GetDistanceEstimation(powerLevel, (short)averageRSSI);
                    }
                    estimatedDistance /= (uint)signature.rssiValues.Keys.Count;
                    distanceEstimations.Add(GetBeaconPosition(signature.beaconId), estimatedDistance);
                }
                if (!localizators_.ContainsKey(moteId))
                {
                    localizators_.Add(moteId, new MonteCarloLocalizator(new MoteLocalization.Rectangle(
                        new Point2D(0, 0), new Point2D(6000, 12000))));
                }
                localizators_[moteId].PositionUpdate(distanceEstimations);
                IPoint2D estimatedPosition = localizators_[moteId].GetEstimatedPosition();
                UInt16 x = (UInt16)(estimatedPosition.X/10);
                UInt16 y = (UInt16)(estimatedPosition.Y/10);
                UInt16 z = 120;
                long lTimestamp = DateTime.Now.Ticks;

                HermesMiddleware.DataAcquisitionServiceServer.dataItem[] motePosition = new HermesMiddleware.DataAcquisitionServiceServer.dataItem[6];

                //timestr.ToString(lTimestamp);
                motePosition[0] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                motePosition[1] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                motePosition[2] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                motePosition[3] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                motePosition[4] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
                motePosition[5] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();

                motePosition[0].timestamp = lTimestamp.ToString();
                motePosition[0].value = x;
                motePosition[1].timestamp = lTimestamp.ToString();
                motePosition[1].value = y;
                motePosition[2].timestamp = lTimestamp.ToString();
                motePosition[2].value = z;
                motePosition[3].timestamp = lTimestamp.ToString();
                motePosition[3].value = 0;
                motePosition[4].timestamp = lTimestamp.ToString();
                motePosition[4].value = 0;
                motePosition[5].timestamp = lTimestamp.ToString();
                motePosition[5].value = 0;

                DataRow[] rows = tblMarkers.Select("SensorID=" + moteId);
                int markerId = -1;
                if (rows.Length != 0)
                {
                    markerId = Int32.Parse((string)rows[0]["MarkerNumber"]);
                }

                bool useCamera = (settings.GetBoolean("Use Camera for Localization")) || (settings.GetBoolean("Use IP Camera for Localization"));


                if (markerId == -1 || !useCamera)
                {
                    UpdateDatabase(moteId.ToString(), "Telos Mote", "Position", motePosition);
                }
                else
                {
                    camera_.UpdateMarkerPosition(markerId, x, y, z);
                }
            }
        }

        IPoint2D GetBeaconPosition(UInt16 moteId)
        {
            switch(moteId)
            {
                case 1:
                    return new Point2D(10220, 1020);
                case 2:
                    return new Point2D(6150, 1020);
                case 3:
                    return new Point2D(2090, 1020);
                case 4:
                    return new Point2D(8190, 3050);
                case 5:
                    return new Point2D(4120, 3050);
                case 6:
                    return new Point2D(570, 3050);
                case 7:
                    return new Point2D(10220, 5080);
                case 8:
                    return new Point2D(6150, 5080);
                case 9:
                    return new Point2D(2090, 5080);
                default:
                    throw new ArgumentException("There is no beacon mote with the given id!");
            }
        }

        uint GetDistanceEstimation(UInt16 powerLevel, Int16 rssi)
        {
            uint result = 0;
            if (powerLevel == 3)
            {
                result = GetDistForSigStrengthThree(rssi);
            }
            else if (powerLevel == 7)
            {
                result = GetDistForSigStrengthSeven(rssi);
            }
            else if (powerLevel == 11)
            {
                result = GetDistForSigStrengthEleven(rssi);
            }
            else
            {
                throw new ArgumentException("Only beacon signals with strength 3, 7 and 11 are supported!");
            }
            if (result < 1500)
            {
                return 0;
            }
            return ((uint)Math.Sqrt(result * result - 2250000)); // subtract the height of the beacons from the distance
        }

        /// <summary>
        /// gets the estimated distance for the given RSSI value at a singal strength of 3
        /// </summary>
        /// <param name="rssi">the rssi value of the signal received by a mobile mote</param>
        /// <returns>the estimated distance for the given rssi value at a signal strength of 3
        /// </returns>
        private uint GetDistForSigStrengthThree(int rssi)
        {
            if (rssi > -20)
                return 100;
            switch (rssi)
            {
                case -20:
                    return 150;
                case -21:
                    return 200;
                case -22:
                    return 300;
                case -23:
                    return 400;
                case -24:
                    return 500;
                case -25:
                    return 700;
                case -26:
                    return 850;
                case -27:
                    return 1000;
                case -28:
                    return 1200;
                case -29:
                    return 1500;
                case -30:
                    return 1750;
                case -31:
                    return 2000;
                case -32:
                    return 2200;
                case -33:
                    return 2400;
                case -34:
                    return 2600;
                case -35:
                    return 2800;
                case -36:
                    return 3000;
                case -37:
                    return 3200;
                case -38:
                    return 3400;
                case -39:
                    return 3600;
                case -40:
                    return 3800;
                case -41:
                    return 4000;
                case -42:
                    return 4200;
                case -43:
                    return 4500;
                case -44:
                    return 4750;
                case -45:
                    return 5000;
                case -46:
                    return 5500;
                case -47:
                    return 6000;
                case -48:
                    return 7000;
                default:
                    return 8500;
            }
        }

        /// <summary>
        /// gets the estimated distance for the given RSSI value at a singal strength of 7
        /// </summary>
        /// <param name="rssi">the rssi value of the signal received by a mobile mote</param>
        /// <returns>the estimated distance for the given rssi value at a signal strength of 7
        /// </returns>
        private uint GetDistForSigStrengthSeven(int rssi)
        {
            if (rssi > -5)
                return 100;
            switch (rssi)
            {
                case -5:
                    return 150;
                case -6:
                    return 200;
                case -7:
                    return 300;
                case -8:
                    return 400;
                case -9:
                    return 500;
                case -10:
                    return 650;
                case -11:
                    return 800;
                case -12:
                    return 900;
                case -13:
                    return 1000;
                case -14:
                    return 1100;
                case -15:
                    return 1200;
                case -16:
                    return 1300;
                case -17:
                    return 1400;
                case -18:
                    return 1550;
                case -19:
                    return 1700;
                case -20:
                    return 1850;
                case -21:
                    return 2000;
                case -22:
                    return 2200;
                case -23:
                    return 2400;
                case -24:
                    return 2600;
                case -25:
                    return 2800;
                case -26:
                    return 3000;
                case -27:
                    return 3500;
                case -28:
                    return 4000;
                case -29:
                    return 4500;
                case -30:
                    return 5000;
                case -31:
                    return 5500;
                case -32:
                    return 6000;
                case -33:
                    return 6750;
                case -34:
                    return 7500;
                case -35:
                    return 8500;
                default:
                    return 10000;
            }
        }

        /// <summary>
        /// gets the estimated distance for the given RSSI value at a singal strength of 11
        /// </summary>
        /// <param name="rssi">the rssi value of the signal received by a mobile mote</param>
        /// <returns>the estimated distance for the given rssi value at a signal strength of 11
        /// </returns>
        private uint GetDistForSigStrengthEleven(int rssi)
        {
            if (rssi > 5)
                return 100;
            switch (rssi)
            {
                case 5:
                    return 150;
                case 4:
                    return 200;
                case 3:
                    return 300;
                case 2:
                    return 400;
                case 1:
                    return 500;
                case 0:
                    return 700;
                case -1:
                    return 850;
                case -2:
                    return 1000;
                case -3:
                    return 1100;
                case -4:
                    return 1200;
                case -5:
                    return 1300;
                case -6:
                    return 1400;
                case -7:
                    return 1500;
                case -8:
                    return 1600;
                case -9:
                    return 1700;
                case -10:
                    return 1850;
                case -11:
                    return 2000;
                case -12:
                    return 2200;
                case -13:
                    return 2400;
                case -14:
                    return 2600;
                case -15:
                    return 2800;
                case -16:
                    return 3000;
                case -17:
                    return 3500;
                case -18:
                    return 4000;
                case -19:
                    return 4500;
                case -20:
                    return 5000;
                case -21:
                    return 5500;
                case -22:
                    return 6000;
                case -23:
                    return 6750;
                case -24:
                    return 7500;
                case -25:
                    return 8500;
                default:
                    return 10000;
            }
        }

        private struct RSSISignature
        {
            internal UInt16 beaconId;
            internal SortedList<UInt16, List<Int16>> rssiValues; // associates the signal strength to the rssi values measured
        }

        private struct SYSTEMTIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;
        }

        [DllImport("Kernel32.dll")]
        private extern static uint SetSystemTime(ref SYSTEMTIME lpSystemTime);

        //This is not a master, Synchronize the clock with the master.
        private void SynchronizePeerClock()
        {

            // get web service
            HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService myServiceProxy = new HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService();
            myServiceProxy.Url = "http://" + global.sP2PMaster + ":" + global.portNumber + global.sVirtRoot + global.sASMX;

            // prepare simple query
            HermesMiddleware.DataAcquisitionServiceProxy.ExpressionType[] expressions = new HermesMiddleware.DataAcquisitionServiceProxy.ExpressionType[1];
            expressions[0] = new HermesMiddleware.DataAcquisitionServiceProxy.ExpressionType();
            expressions[0].Items = new HermesMiddleware.DataAcquisitionServiceProxy.Expression[1];

            // use threshold as expression
            HermesMiddleware.DataAcquisitionServiceProxy.Threshold myThreshold = new HermesMiddleware.DataAcquisitionServiceProxy.Threshold();


            myThreshold.type = HermesMiddleware.DataAcquisitionServiceProxy.ThresholdType.above;
            myThreshold.value = null;
            myThreshold.strict = true;
            expressions[0].Items[0] = myThreshold;
            expressions[0].Items[0].variable = "P2P_Master_Time";

            // perform query, delegate is false.
            HermesMiddleware.DataAcquisitionServiceProxy.SensorData[] sensorData = myServiceProxy.Query(global.sDefaultService, expressions, false);

            if (sensorData != null)
            {

                //There is only one data, which is from master itself
                long masterTime = long.Parse(sensorData[0].dataArray[0].timestamp);

                DateTime dtMasterTime = DateTime.FromFileTimeUtc(masterTime);

                SYSTEMTIME systemTime;

                systemTime.wDay = (ushort)dtMasterTime.Day;
                systemTime.wDayOfWeek = (ushort)dtMasterTime.DayOfWeek;
                systemTime.wHour = (ushort)dtMasterTime.Hour;
                systemTime.wMilliseconds = (ushort)dtMasterTime.Millisecond;
                systemTime.wMinute = (ushort)dtMasterTime.Minute;
                systemTime.wMonth = (ushort)dtMasterTime.Month;
                systemTime.wSecond = (ushort)dtMasterTime.Second;
                systemTime.wYear = (ushort)dtMasterTime.Year;

                SetSystemTime(ref systemTime);

            }

        }


        private void MasterTimeThread()
        {

            long lTimestamp;
            while (true)
            {
                lTimestamp = DateTime.Now.ToFileTimeUtc();
                UpdateDatabase("0", "DAS_P2P_MASTER", "P2P_Master_Time", lTimestamp, lTimestamp);
                Thread.Sleep(100);
            }

        }


        //If we are the master, start time publishing
        private void StartTimePublishThread()
        {

            Console.WriteLine("Master Time Publish Thread Started");
            masterTimeThread = new Thread(new ThreadStart(MasterTimeThread));
            masterTimeThread.Name = "Master Time Publish Thread";
            masterTimeThread.IsBackground = true;
            masterTimeThread.Start();


        }



    }


    // This class keeps track of the statistics of the motes.
    public class MoteStatistics
    {

        public int packets_received; //total number of packets received
        public int last_seq_num;     //last biggest seq number
        public int interval_start;   //last smallest seq number

        public MoteStatistics(int last_seq_num)
        {

            this.last_seq_num = last_seq_num;
            this.packets_received = 1;
            this.interval_start = last_seq_num;

        }

        public void update_seq_num(int new_seq_num)
        {

            packets_received++;

            //Update your window parameters
            if (last_seq_num < new_seq_num)
            {
                this.last_seq_num = new_seq_num;
            }

            if (new_seq_num < interval_start)
            {
                this.interval_start = new_seq_num;
            }

        }

        public double performance_percentage()
        {

            double total_packets = last_seq_num - interval_start + 1;
            return ((packets_received) / total_packets * 100);

        }
    }
}

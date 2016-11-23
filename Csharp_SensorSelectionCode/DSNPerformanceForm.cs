using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using dotnetCHARTING.WinForms;

namespace DataAcquisitionService
{
	public partial class DSNPerformanceForm : Form
	{

		DSNNode[] nodes = new DSNNode[15];
		Series cumulativeSeries = new Series();
		Timer graphRefreshTimer;
		Timer timerToSend;
		MainForm parentForm;

        double[] BeforeOptPos;
        double BeforeOptErr;
        double[] AfterOptPos;
        double AfterOptErr;
        byte[] SamplingRate;
        int[] SelectedSensor;
        double[,] SenPos;

		public Queue queueMessagesToSend = Queue.Synchronized(new Queue());

		public class DSNNode
		{

			int numOfPackets = 0;
			public int lastSeqNum;
			public double performance;
			public Element element;
			public Series series;

			public DSNNode()
			{

				numOfPackets = 0;
				lastSeqNum = 0;
				performance = 0.0;
				//element.YValue = performance;
			}

			public void Reset() {

				numOfPackets = 0;
				lastSeqNum = 0;
				performance = 0.0;

				series.Background.Color = Color.Black;
				element.Color = Color.Black;
			
			}

			public void UpdateLastSeqNum(int seqNum)
			{

				numOfPackets++;
				if (seqNum > lastSeqNum)
				{
					lastSeqNum = seqNum;
				}
				if (lastSeqNum != 0)
				{
					performance = (double)numOfPackets / (double)lastSeqNum * 100.0;
				}
				else
				{

					performance = 0;
				}
				element.YValue = performance;

			}

		}


		public DSNPerformanceForm(MainForm parentForm, double[,] SenPosInput)
		{
			InitializeComponent();
			InitializeGraphs();

			this.parentForm = parentForm;

			graphRefreshTimer = new Timer();

			graphRefreshTimer.Interval = 1000;
			graphRefreshTimer.Tick += new EventHandler(graphRefreshTimer_Tick);
			graphRefreshTimer.Start();

            // init data array
            BeforeOptPos = new double[2];
            AfterOptPos = new double[2];
            SamplingRate = new byte[15];
            SelectedSensor = new int[15];
            SenPos = new double[2, 15];
            BeforeOptErr = 0;
            AfterOptErr = 0;

			AddSensorPosition(SenPosInput);

			DrawDemoTopView();
		}

        ~DSNPerformanceForm()
        {
            // if this form is closed, enable DSN button
            //this.parentForm.buttonDSN.Enabled = true;
        }

        public void ResetData()
        {
            // init data array
            int i;

            for (i = 0; i < 2; i++)
            {
                BeforeOptPos[i] = 0.0;
                AfterOptPos[i] = 0.0;
            }

            for (i = 0; i < 15; i++)
            {
                SamplingRate[i] = 0;
                SelectedSensor[i] = 0;
                SenPos[0,i] = 0;
                SenPos[1,i] = 0;
            }

            BeforeOptErr = 0;
            AfterOptErr = 0;
        }

		public void Broadcast(HermesMiddleware.MoteLibrary.TOS_Msg msg) {

			queueMessagesToSend.Enqueue(msg);

			if (timerToSend == null) {

				timerToSend = new Timer();
				timerToSend.Interval = 500;
				timerToSend.Tick += new EventHandler(timerToSend_Tick);
				timerToSend.Start();
			
			}

		}

		void timerToSend_Tick(object sender, EventArgs e)
		{
			while (queueMessagesToSend.Count > 0) {

				HermesMiddleware.MoteLibrary.TOS_Msg msg = (HermesMiddleware.MoteLibrary.TOS_Msg)queueMessagesToSend.Dequeue();
				parentForm.mainService.serial.SendMsg(msg);
			}

            //operations
            parentForm.mainService.DSNOperations();
		}


		void graphRefreshTimer_Tick(object sender, EventArgs e)
		{
			chartGauges.Refresh();
			chartGauges.RefreshChart();

			UpdateCumulativeChart();
            DrawDemoTopView();
		}

		Series seriesPrecision = new Series();
		Series seriesActiveNodes = new Series();

		public void InitializeGraphs()
		{

			SeriesCollection scPrecision = new SeriesCollection();
			scPrecision.Add(seriesPrecision);
			seriesPrecision.Line.Color = Color.Blue;	

			chartPrecision.SeriesCollection.Add(seriesPrecision);
			chartPrecision.LegendBox.Visible = false;
			//chartPrecision.LegendBox.Orientation = dotnetCHARTING.WinForms.Orientation.Top;
			chartPrecision.TitleBox.Position = TitleBoxPosition.Full;
			chartPrecision.TitleBox.Label.Alignment = StringAlignment.Center;
			chartPrecision.Title = "Position Error";
			chartPrecision.XAxis.Label = new dotnetCHARTING.WinForms.Label("Time");
			chartPrecision.YAxis.Label = new dotnetCHARTING.WinForms.Label("Position Error");
			chartPrecision.ChartArea.XAxis.Label = new dotnetCHARTING.WinForms.Label("Time");
			chartPrecision.ChartArea.YAxis.Label = new dotnetCHARTING.WinForms.Label("Position Error");
			chartPrecision.ChartArea.XAxis.TickLabel = new dotnetCHARTING.WinForms.Label("");
			chartPrecision.XAxis.TickLabel = new dotnetCHARTING.WinForms.Label("");
			//chartPrecision.DefaultSeries.DefaultElement.ShowValue = true;
			//chartPrecision.DefaultSeries.DefaultElement.LabelTemplate = "%Yvalue";

			SeriesCollection scActiveNodes = new SeriesCollection();
			scActiveNodes.Add(seriesActiveNodes);
			seriesActiveNodes.Line.Color = Color.Crimson;
			chartActiveSensors.Title = "Number of Active Sensors";

			chartActiveSensors.XAxis.Label = new dotnetCHARTING.WinForms.Label("Time");
			chartActiveSensors.YAxis.Label = new dotnetCHARTING.WinForms.Label("Number of Active Sensors");
			chartActiveSensors.ChartArea.XAxis.Label = new dotnetCHARTING.WinForms.Label ("Time");
			chartActiveSensors.ChartArea.YAxis.Label = new dotnetCHARTING.WinForms.Label ("Number of Active Sensors");
			chartActiveSensors.XAxis.TickLabel = new dotnetCHARTING.WinForms.Label(""); 

			chartActiveSensors.SeriesCollection.Add(scActiveNodes);
			chartActiveSensors.LegendBox.Visible = false;
			chartActiveSensors.TitleBox.Position = TitleBoxPosition.Full;
			chartActiveSensors.TitleBox.Label.Alignment = StringAlignment.Center;

			// Set the title.
			chartGauges.Title = "Packet Success Rates";

			// Set the directory where images are temporarily be stored.
			chartGauges.TempDirectory = "C:\\DOCUME~1\\vladimir\\LOCALS~1\\Temp\\";

			// Disable the legend box
			chartGauges.LegendBox.Position = LegendBoxPosition.None;

			// Set a default gauge face background color.
			chartGauges.DefaultSeries.Background.Color = Color.LightGray;

			// Set he chart size.
			chartGauges.Width = 600;
			chartGauges.Height = 350;

			// Specify the gauges chart type
			chartGauges.Type = ChartType.Gauges;

			chartGauges.Use3D = true;
			chartGauges.ClipGauges = false;


			for (int i = 0; i < 15; i++)
			{

				Series s1 = new Series("Node " + (i+1).ToString(), new Element("", 0));
				s1.YAxis = new Axis();

				s1.YAxis.Maximum = 100;
				s1.YAxis.Minimum = 0;
				chartGauges.SeriesCollection.Add(s1);

				nodes[i] = new DSNNode();
				nodes[i].element = s1.Elements[0];
				nodes[i].series = s1;
			}

			chartCumulative.Title = "Cumulative Packet Success Rates";
			// Set the directory where images are temporarily be stored.
            chartCumulative.TempDirectory = "C:\\DOCUME~1\\vladimir\\LOCALS~1\\Temp\\";

			// Disable the legend box
			chartCumulative.LegendBox.Position = LegendBoxPosition.Top;			

			// Set a default gauge face background color.
			chartCumulative.DefaultSeries.Background.Color = Color.LightGray;

			chartCumulative.Type = ChartType.Scatter;
			chartCumulative.SeriesCollection.Add(cumulativeSeries);
			UpdateCumulativeChart();

		}

		public delegate void AddToPrecisionDel(double value);

		public void AddToPrecision( double value ) {

			if (this.InvokeRequired) {

				AddToPrecisionDel addToPrecisionDel = new AddToPrecisionDel(AddToPrecision);
				this.Invoke(addToPrecisionDel, new object[] { value });
			}
			else
			{

				Element e = new Element();
				DateTime now;


				e.YValue = value;
				e.XDateTime = DateTime.Now;
				e.Color = Color.Blue;

				now = e.XDateTime;
				now = now.Subtract(new TimeSpan(0,1,0));

				chartPrecision.XAxis.Minimum = now;
				chartPrecision.XAxis.Maximum = e.XDateTime;

				seriesPrecision.Elements.Add(e);
				chartPrecision.Refresh();
			}
		}

		public delegate void AddToActiveSensorsDel(double value);

		public void AddToActiveSensors( double value ) {

			if (this.InvokeRequired)
			{
				AddToActiveSensorsDel addToActiveSensorsDel = new AddToActiveSensorsDel(AddToActiveSensors);
				this.Invoke(addToActiveSensorsDel, new object[] { value });

			}
			else
			{
				Element e = new Element();
				DateTime now;

				e.YValue = value;
				e.XDateTime = DateTime.Now;
				e.Color = Color.Crimson;

				now = e.XDateTime;
				now=now.Subtract(new TimeSpan(0,1,0));

				chartActiveSensors.XAxis.Minimum = now;
				chartActiveSensors.XAxis.Maximum = e.XDateTime;

				seriesActiveNodes.Elements.Add(e);
				chartActiveSensors.Refresh();
			}
		
		}

		private void buttonDemo_Click(object sender, EventArgs e)
		{
			Random random = new Random();
			AddToPrecision(random.NextDouble()*100);
			AddToActiveSensors(random.NextDouble()*15);
			g.FillEllipse(new SolidBrush(Color.SaddleBrown), 300, 400, 100, 100);		
		}

		private void buttonClose_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void UpdateCumulativeChart()
		{

			double sum = 0;
			int activeSensors = 0;

			for (int i = 0; i < 15; i++)
			{

				if (nodes[i].performance != 0)
				{
					sum += nodes[i].performance;
					activeSensors++;

					if (nodes[i].performance > 75)
					{
						nodes[i].element.Color = Color.White;
						nodes[i].series.Background.Color = Color.Green;
					}
					else if (nodes[i].performance > 50)
					{
						nodes[i].element.Color = Color.Black; 
						nodes[i].series.Background.Color = Color.Yellow;
					}
					else {
						nodes[i].element.Color = Color.White;
						nodes[i].series.Background.Color = Color.Red;										
					}
				}
				else {

					nodes[i].element.Color = Color.Black;
					nodes[i].series.Background.Color = Color.Black;
				}

			}

			if (activeSensors != 0)
			{
				sum /= activeSensors;
			}
			else {

				sum = 100.0;
			}

			Element e = new Element();

			e.XDateTime = DateTime.Now;
			e.YValue = sum;
			chartCumulative.YAxis.Maximum = 100;
			chartCumulative.YAxis.Minimum = 0;
			cumulativeSeries.Elements.Add(e);
			cumulativeSeries.LegendEntry.Value = sum.ToString();

			if (cumulativeSeries.LegendEntry.Value.Length >= 5)
			{
				cumulativeSeries.LegendEntry.Value = cumulativeSeries.LegendEntry.Value.Substring(0, 5);
			}
		
			chartCumulative.Refresh();
			chartCumulative.RefreshChart();
		}

		public delegate void UpdateNodeSeqNumDel(int nodenum, int lastSeqNum);

		public void UpdateNodeSeqNum(int nodeNum, int lastSeqNum) {

			if (this.InvokeRequired)
			{
				UpdateNodeSeqNumDel updateNodeSeqNumDel = new UpdateNodeSeqNumDel(UpdateNodeSeqNum);
				this.BeginInvoke(updateNodeSeqNumDel, new object[] { nodeNum, lastSeqNum });

			}
			else {

				nodes[nodeNum].UpdateLastSeqNum(lastSeqNum);
			}
		
		}

		private void button1_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < 15; i++)
			{

				nodes[i].UpdateLastSeqNum(i + nodes[i].lastSeqNum);

			}

			chartGauges.Refresh();
			chartGauges.RefreshChart();

			UpdateCumulativeChart();
		}

		private void buttonReset_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < 15; i++) {

				nodes[i].Reset();
			}
			chartGauges.Refresh();
			chartGauges.RefreshChart();

			UpdateCumulativeChart();


		}


		Graphics g;
        public delegate void DrawDemoTopViewDel();

        /// <summary>
        /// Draw all the sensors and position estimation circles 
        /// </summary>
        /// 
        public void DrawDemoTopView()
        {
            if (this.InvokeRequired)
            {
                DrawDemoTopViewDel drawDemoTopViewDel = new DrawDemoTopViewDel(DrawDemoTopView);
                this.Invoke(drawDemoTopViewDel, new object[] { });

            }
            else
            {

                g = Graphics.FromImage(SensorTopView.BackgroundImage);
				//g = Graphics.FromImage(new Bitmap("../DSNSiemensGridBoard.jpg"));

				double[] TmpWorldSenPos = new double[2];
				int[] TmpScreenSenPos = new int[2];
				for (int i = 0; i < SensorSelection.MaxSennum; i++)
				{

					TmpWorldSenPos[0] = SenPos[0, i];
					TmpWorldSenPos[1] = SenPos[1, i];
					ConvertWorldCorrdinateToScreenCoordinate(TmpWorldSenPos, out TmpScreenSenPos);

                    if (SamplingRate[i] > 0)
					{
						g.FillEllipse(new SolidBrush(Color.Green), TmpScreenSenPos[0], TmpScreenSenPos[1], 100, 100);
						//g.DrawString(SamplingRate[i].ToString(), new Font("Times New Roman", 7), new SolidBrush(Color.Red),
						//new PointF((float)TmpScreenSenPos[0], (float)TmpScreenSenPos[1]));
					}
					else {
						g.FillEllipse(new SolidBrush(Color.Red), TmpScreenSenPos[0], TmpScreenSenPos[1], 100, 100);										
					}
				}

				g.Flush();
				this.Refresh();
//				return;


                //const int LineWidth = 3;
                const int LineWidth = 5;

                Pen BeforeOptPen = new Pen(new SolidBrush(Color.Black), LineWidth);
                Pen AfterOptPen = new Pen(new SolidBrush(Color.Blue), LineWidth);
                DrawEstimatePos(BeforeOptPos, BeforeOptErr, ref g, BeforeOptPen);
                DrawEstimatePos(AfterOptPos, AfterOptErr, ref g, AfterOptPen);
                this.Refresh();

            }
        }

        void DrawEstimatePos(double[] Pos, double err, ref Graphics g, Pen PenToUse)
        {
            //const int HalfCrossSize = 2;
            const int HalfCrossSize = 20;
            int[] TmpScreenSenPos = new int[2];
            int iErr;
            iErr = ConvertLengthToScreen(err);

            TmpScreenSenPos[0] = (int)Pos[0];
            TmpScreenSenPos[1] = (int)Pos[1];
            ConvertWorldCorrdinateToScreenCoordinate(Pos, out TmpScreenSenPos);

            //// draw the background.
            //// comment out. it does not look good.
            //HatchBrush tbrush = new HatchBrush(HatchStyle.DashedDownwardDiagonal, Color.Yellow);
            //g.FillEllipse(tbrush, TmpScreenSenPos[0] - iErr, TmpScreenSenPos[1] - iErr, iErr * 2, iErr * 2);

            //// draw a cross
            g.DrawLine(PenToUse,
               TmpScreenSenPos[0] - HalfCrossSize, TmpScreenSenPos[1] ,
               TmpScreenSenPos[0] + HalfCrossSize, TmpScreenSenPos[1] );
            g.DrawLine(PenToUse,
               TmpScreenSenPos[0], TmpScreenSenPos[1] - HalfCrossSize,
               TmpScreenSenPos[0], TmpScreenSenPos[1] + HalfCrossSize);
            // // draw error bound circle
            g.DrawEllipse(PenToUse, TmpScreenSenPos[0] - iErr, TmpScreenSenPos[1] - iErr, iErr * 2, iErr * 2);

        }

        /// <summary>
        /// Add latest sampling rate to this class. Be ready to display on the screen.
        /// </summary>
        /// <param name="SamplingNum"></param>
        public void SetSamplingRate(byte[] SamplingNum)
        {
            int i;
            for (i = 0; i < 15; i++)
            {
                SamplingRate[i] = SamplingNum[i];
                //if (SamplingNum[i] == 0)
                //    isSensorValid[i] = 0;
                //else
                //    isSensorValid[i] = 1;
            }
        }

        /// <summary>
        /// Add the position estimation before the sensor selection (sampling rate optimization)
        /// </summary>
        /// <param name="Pos"></param>
        /// <param name="err"></param>
        public void SetBeforeOptimizationPosition(double[] Pos, double err)
        {
            BeforeOptPos[0] = Pos[0];
            BeforeOptPos[1] = Pos[1];
            BeforeOptErr = err;
        }

        /// <summary>
        /// Add the position estimation after the sensor selection (sampling rate optimization)
        /// </summary>
        /// <param name="Pos"></param>
        /// <param name="err"></param>
        public void SetAftertimizationPosition(double[] Pos, double err)
        {
            AfterOptPos[0] = Pos[0];
            AfterOptPos[1] = Pos[1];
            AfterOptErr = err;
        }

        /// <summary>
        /// Add sensor positions on this object 
        /// </summary>
        /// <param name="SenPosInput"></param>
        public void AddSensorPosition(double[,] SenPosInput )
        {
            for (int i = 0; i < 15; i++)
            {
                SenPos[0, i] = SenPosInput[0, i];
                SenPos[1, i] = SenPosInput[1, i];
            }
        }

        private void ConvertWorldCorrdinateToScreenCoordinate(double[] World, out int[] ScreenCord)
        {
            ScreenCord = new int[2];
            ScreenCord[0] = (int)(World[0]/(34*2.54)*SensorTopView.BackgroundImage.PhysicalDimension.Width);
            ScreenCord[1] = (int)(SensorTopView.BackgroundImage.PhysicalDimension.Height - (World[1] / (28 * 2.54) * SensorTopView.BackgroundImage.PhysicalDimension.Height)-150);
            return;
        }


        /// <summary>
        /// Convert real world length (unit in cm) to screen point number.
        /// </summary>
        /// <param name="RealLength"></param>
        /// <returns></returns>
        private int ConvertLengthToScreen(double RealLength)
        {
            double ratio;
            int ptnum;
            ratio = SensorTopView.BackgroundImage.PhysicalDimension.Width / (34 * 2.54);
            ptnum = (int)(RealLength * ratio);
            return ptnum;
        }

		private void buttonClear_Click(object sender, EventArgs e)
		{
			g.Clear(Color.White);
			//g.DrawImage(new Bitmap("../"), new Point(0,SensorTopView.BackgroundImage.Height));
			g.DrawImage(new Bitmap("../DSNSiemensGridBoard.jpg"), new Point(0,0));
			double[] TmpWorldSenPos = new double[2];
			int[] TmpScreenSenPos = new int[2];
			for (int i = 0; i < SensorSelection.MaxSennum; i++)
			{

				TmpWorldSenPos[0] = SenPos[0, i];
				TmpWorldSenPos[1] = SenPos[1, i];
				ConvertWorldCorrdinateToScreenCoordinate(TmpWorldSenPos, out TmpScreenSenPos);

				if (SamplingRate[i] > 0)
				{
					g.FillEllipse(new SolidBrush(Color.Green), TmpScreenSenPos[0], TmpScreenSenPos[1], 100, 100);
					//g.DrawString(SamplingRate[i].ToString(), new Font("Times New Roman", 7), new SolidBrush(Color.Red),
					//new PointF((float)TmpScreenSenPos[0], (float)TmpScreenSenPos[1]));
				}
				else
				{
					g.FillEllipse(new SolidBrush(Color.Red), TmpScreenSenPos[0], TmpScreenSenPos[1], 100, 100);
				}
			}
			
			this.Refresh();
		}
    }
}
using System;
using System.Collections.Generic;
using System.Text;

using HermesMiddleware.DataAcquisitionServiceServer;
using Mapack;


namespace DataAcquisitionService
{
    /// <summary>
    /// SensorSelection: model-based sensor selection optimization 
    /// 
    /// Based on the following unfinished paper:
    /// Zhen Song, Chellury Sastry and N. Cihan Tas, "A model-based optimal sensor selection method 
    /// for event estimation in wireless sensor network," 2006.
    /// </summary>
    /// 
    public class SensorSelection
    {
        public const int dim = 2;
        // accept 15 sensors for the maximum
        public const int MaxSennum = 15;
        // if the distance between two adjacent positions is greater than "MoveThresholdRatio*ErrRadius", 
        // consider the lamp "moved."
        double SensorReadingNormThresholdForMoving = 1500; // put it to a configuration file
        // used 1000, OK to detect large movements
        // 500: all sensors are active all the time 
        // 900: once moved, all sensors are active
        //double MaxDifferenceBetween2PosEst = 20; // to judge if the light is moved.
        double MaxDifferenceBetween2PosEst = 10; // to judge if the light is moved.
        //double MaxDifferenceBetweenAdjacent2ndPosEst = 40; // to judge if the light is moved.
        double MaxDifferenceBetweenAdjacent2ndPosEst = 10; // to judge if the light is moved.


        double MinDetFIMThreshold = 0.0001; //if below this, don't inverse the FIM matrix.
        //const double MaxAllowedPositionError = 20; // if the error is > 20cm, consider to reset the network
        //double MaxAllowedPositionError = 20; // if the error is > 20cm, consider to reset the network
        double MaxAllowedPositionError = 1.27; // if the error is > 1.27cm, reset the network
        double MaxAllowedPositionErrorRatio = 0.7; // if the after error > MaxAllowedPositionErrorRatio*BeforeError, reset the network

        public int sennum = MaxSennum;

        double[] sigmaArray;
        int[] validSensor;
        double[] LastPosEst;
        double[] BeforePosEst;
        double[] AfterPosEst;

        double rLS=0; // residue error
        double K, C; // used for Gaussian sensor model
        double Step, StoppingThreshold;
        double eta;
        const int DefaultTotalSamplingNum = 255; // choose 255 as the default, since it is guarantted not to overflow
        int TotalSamplingNum = DefaultTotalSamplingNum; 
        double []RealSampleNum;

        double[,] Sensitivity;
        double[,] SensorPositions;
        double[] SensorReading; // sensor reading  

        double[] BufEstPositionA, BufEstPositionB;
        double[] BufValidSensorReadingA, BufValidSensorReadingB;

        // model based on 35w Helogen light, 9in height.
        public double[] BrightCurve ={ 6720.0, -216.0, 2.524 }; // quadratic fit
        // public double[] StdCurve ={ 82.4, -3.626, 0.04577 };// robust quadratic fit. This is the Std for 100 samples, not 1 nominal sample
        public double[] StdCurve ={ 824.0, -36.26, 0.4577 };// robust quadratic fit
        const int AmbientLightForTheModel = 2000; // the ambient light value when we got the model
        public int CurrentAmbientLight = AmbientLightForTheModel;


        // if brightness is less this this, the data is invalue. 
        const int BrightnessThreshold = 2200; // choose 2200 to guarantee one-to-one mapping between the bright and distance, associate around 36cm
        //const int BrightnessThreshold = 1800; // choose 2200 to guarantee one-to-one mapping between the bright and distance, associate around 36cm
        
        // 10/03, find the proper Step size and StoppingThreshold by hardware experiments
        // 35W Halogen lamp with cap. 
        // lamp height: 10 inches from the table
        // 10/12: when Step = 5e-6; it seems the Loc. Finding may not converge 
        double NLOptLocFindStepSize = 5e-5;
        double NLOptLocFindStoppingThreshold = 0.01;

        int NumOfInvalidDataSets = 0;
        int NubOfInvalidPosEstimation = 0;
        const int UpperBoundNumOfInvalidDataSets = 5; 
        // The functionality of restarting is not implemented by this class. This class only count the number of continuous invalid Data Sets;
        const int MaxNumOfInvalidPosEstimation = 3;

        //private bool HoldForLampMove = false; // for isLampMoved()
        private bool NewDataCheckedByLampMove = false; // for isLampMoved(): 
        // always push new data into the buffer after run isLampMoved(), otherwise lamp motion may be ignored.
        bool ByPassTheNextIsLampMoved = false; // if sensor is moved, reset the network, wait for the next data set, and by pass the next data set.
        // So that the next data set can be processed by LSFindPosition and sampling rate optimization. If do not by pass the function, the network 
        // is always in the reset mode.
        int ValidPosEstSet = 0;

        // DSNoption is accessed from MainService and DSNPerformance. Therefore, it must be locked.
        public object AlgLock = new object();

        public SensorSelection()
        {
            sennum = MaxSennum;
            InitGlobalVariables();
        }

        public SensorSelection(double[,] senposition, double OneSigma)
        {
            if (senposition.GetLength(0) != dim)
            {
                Console.WriteLine("Warning: the dimension of the sensor position may be wrong.");
            }
            if (senposition.GetLength(1) != sennum)
            {
                Console.WriteLine("Warning: the dimension of the sensor position may be wrong.");
            }

            SensorPositions = new double[dim, sennum];
            for (int i = 0; i < dim; i++)
            {
                for (int j = 0; j < sennum ; j++)
                {
                    SensorPositions[i, j] = senposition[i, j];
                }
            }

            sigmaArray = new double[sennum];

            for (int i = 0; i<sennum; i++)
                sigmaArray[i] = OneSigma;

            validSensor = new int[sennum];
            for (int i = 0; i < sennum; i++)
                validSensor[i] = 0;

            InitGlobalVariables();
        }


        void InitGlobalVariables()
        {
            int i, j;

            LastPosEst = new double[dim];
            BeforePosEst = new double[dim];
            AfterPosEst = new double[dim];
            for (i = 0; i < dim; i++)
            {
                LastPosEst[i] = 0.0;
                BeforePosEst[i] = 0.0;
                AfterPosEst[i] = 0.0;
            }

            Sensitivity = new double[sennum, dim];
            for (i = 0; i < sennum; i++)
            {
                for (j = 0; j < dim; j++)
                {
                    Sensitivity[i, j] = 0.0;
                }
            }
            SensorReading = new double[sennum];
            RealSampleNum = new double[sennum];
            for (i = 0; i < sennum; i++)
            {
                SensorReading[i] = 0.0;
                RealSampleNum[i] = 1;
            }

            // default configuration for algorithms
            eta = 1e-5;

            // obsoleted
            K = 1000;
            C = 7000;

            // 10/03, find the proper Step size and StoppingThreshold by hardware experiments
            // 35W Halogen lamp with cap. 
            // lamp height: 10 inches from the table
            // 10/12: when Step = 5e-6; it seems the Loc. Finding may not converge
            Step = NLOptLocFindStepSize;
            // StoppingThreshold = 0.01;
            StoppingThreshold = NLOptLocFindStoppingThreshold;

            isAfterSensorSelection = false;
            NumOfInvalidDataSets = 0;

            BufEstPositionA = new double[dim];
            BufEstPositionB = new double[dim];

            BufValidSensorReadingA = new double[sennum];
            BufValidSensorReadingB = new double[sennum];
        }

        public void AddSensorReading(double[] SensorReadingInput)
        {
            int i;
            for (i = 0; i < sennum; i++)
                SensorReading[i] = SensorReadingInput[i];
        }

        /// <summary>
        /// Clear all sensor readings. Ready for the next set of sensor data.
        /// </summary>
        public void ClearAll()
        {
            for(int i=0; i< sennum; i++)
            {
                validSensor[i]  = 0;
                SensorReading[i] =0;
            }
            // don't need to clear LastPosEst
            NumOfInvalidDataSets = 0;
            // don't clear sampling rate
        }

        /// <summary>
        /// More than ClearAll: clear variables and reset the state (isAfterSensorSelection)
        /// </summary>
        public void Reset() 
        {
            ClearAll();
            isAfterSensorSelection = false;
            NubOfInvalidPosEstimation = 0;

            ValidPosEstSet = 0; // non of the position estimation is valid

            /// to be added later
            //AssignUniformSamplingRate(global.mainForm.InitSamplingRate);
            //TotalSamplingNum = global.mainForm.InitSamplingRate*sennum;
        }

        //public void AddOneSensorReading(int SensorID, double SensorReadingInput)
        //{
        //    // 10/13: ambient light cancellation
        //    SensorReadingInput = SensorReadingInput - (double)AmbientLightForTheModel + (double)CurrentAmbientLight;

        //    if (SensorReadingInput > (double)BrightnessThreshold)
        //    {
        //        validSensor[SensorID - 1] = 1;
        //        SensorReading[SensorID - 1] = SensorReadingInput;
        //    }
        //    else
        //    {
        //        validSensor[SensorID - 1] = 0;
        //        SensorReading[SensorID - 1] = 0;
        //    }

        //}

        public void AddOneSensorReading(int SensorID, int SensorReadingInput)
        {
            // 10/13: only check the high sampling rate sensors
            // ignore low rate sensors.
            // their data should not be received.
            // they are received due to network imperfectness.
            if (RealSampleNum[SensorID - 1] > 0)
            {
                // 10/13: ambient light cancellation
                SensorReadingInput = SensorReadingInput + AmbientLightForTheModel - CurrentAmbientLight;

                if (SensorReadingInput > BrightnessThreshold)
                {
                    validSensor[SensorID - 1] = 1;
                    SensorReading[SensorID - 1] = (double)SensorReadingInput;
                }
                else
                {
                    validSensor[SensorID - 1] = 0;
                    SensorReading[SensorID - 1] = 0;
                }
            }
            else
            {
                // low rate sensors.
                // their data should not be received.
                // they are received due to network imperfectness.
                validSensor[SensorID - 1] = 0;
                SensorReading[SensorID - 1] = 0;
            }
        }

        public void ConfigAlgorithm(double KIn, double CIn, double StepIn, double StoppingThresholdIn, double etaIn)
        {
            K = KIn;
            C = CIn;
            Step = StepIn;
            StoppingThreshold = StoppingThresholdIn;
            eta = etaIn;
        }

        /// <summary>
        /// ReadyForPositionEstimation
        /// 
        /// </summary>
        /// <returns></returns>
        public bool ReadyForPositionEstimation()
        {
            int sum=0;
            for (int i = 0; i < sennum; i++)
                sum += validSensor[i];

            if (sum >= 3)
                return true;
            else
                return false;
        }

        /// <summary>
        /// 1. Estimate the lamp's position using least square
        /// 2. Cooperate with isLampMoved()
        /// </summary>
        /// <param name="InitPos"></param>
        /// <param name="PosEst"></param>
        /// <param name="radLS"></param>
        /// <returns></returns>
        public bool EstimatePosition(double[] InitPos, out double[] PosEst, out double radLS)
        {
            bool ToReturn = false;

            if (ReadyForPositionEstimation())
            {
                if (LSFindPos(SensorReading, validSensor, SensorPositions, K, C, Step, StoppingThreshold, InitPos,
                    out PosEst, out  rLS, out  Sensitivity, RealSampleNum))
                {
                    radLS = rLS;
                    if (isInsideBoard(PosEst))
                    {
                        // position is estimated, valid!
                        
                        LastPosEst[0] = PosEst[0];
                        LastPosEst[1] = PosEst[1];

                        NubOfInvalidPosEstimation = 0;

                        if (isAfterSensorSelection)
                        {
                            AfterPosEst[0] = PosEst[0];
                            AfterPosEst[1] = PosEst[1];
                            ValidPosEstSet = 2;
                        }
                        else
                        {
                            BeforePosEst[0] = PosEst[0];
                            BeforePosEst[1] = PosEst[1];
                            ValidPosEstSet = 1;
                        }

                        ToReturn = true;
                    }
                    else
                    {
                        // wrong position!
                        NubOfInvalidPosEstimation++; 
                        ToReturn = false;
                    }
                }
                else
                {
                    // can't estimate position due to insufficent data
                    radLS = rLS;
                    NubOfInvalidPosEstimation++;
                    ToReturn = false;
                }
            }
            else
            {
                PosEst = new double[2];
                for (int i = 0; i < 2; i++)
                    PosEst[i] = 0.0;
                radLS = 0;
                // if simply not ready, do not increase NubOfInvalidPosEstimation
                ToReturn = false;
            }

            if (isSensorReadingInBufDifferent() || isEstimationErrorBig())
            //  check if the system is restarted or the lamp is moved
            {
                // Clear buffer: 
                // 1: SensorReading -> BufferA
                // 2: BufferA -> BufferB
                ClearAndFillBuffer(SensorReading, PosEst);
                ByPassTheNextIsLampMoved = true;
            }
            else
            {
                // Shift buffer: 
                // 1: BufferA -> BufferB
                // 2: SensorReading -> BufferA

                // in order to compare recent data to see if the lamp is moved.
                StoreResultInBuffer(SensorReading, PosEst);
                ByPassTheNextIsLampMoved = false;
            }

            // clear all sensor reading, ready for the next data set.
            ClearAll();
            return ToReturn;
        }



        public bool isAfterSensorSelection;

        /// <summary>
        /// Return normalized optimized sampling rate
        /// </summary>
        /// <param name="NormalizedRate"></param>
        //public void OptimizeRealSamplingRate(out double[] NormalizedRate)
        //{
        //    NormalizedRate = new double[sennum];
        //    NormalizedRate = AssignSamplingRate(eta, validSensor, Sensitivity);
        //    isAfterSensorSelection = true;

        //    // heuristic method
        //    // make sure 3 sensors are selected
        //    int SensorCounter = 0;
        //    int i;
        //    int newSensorID;

        //    int[] SelectedSensorID = new int[2];
        //    for (i = 0; i < 2; i++)
        //        SelectedSensorID[i] = 0;

        //    int pt = 0;
        //    for (i = 0; i < sennum; i++)
        //    {
        //        if (NormalizedRate[i] != 0)
        //        {
        //            SensorCounter++;
        //            if (pt < 2)
        //            {
        //                SelectedSensorID[pt] = i;
        //                pt++;
        //            }
        //        }
        //    }
        //    if (SensorCounter == 2)
        //    {
        //        // need to add one more sensor
        //        newSensorID = ClosestSensor(PosHat, validSensor);
        //        if (newSensorID >= 0)
        //        {
        //            NormalizedRate[newSensorID] = (byte)((NormalizedRate[validSensor[0]] + NormalizedRate[validSensor[1]]) / 3);
        //            NormalizedRate[validSensor[0]] = (byte)NormalizedRate[validSensor[0]] * 2 / 3;
        //            NormalizedRate[validSensor[1]] = (byte)NormalizedRate[validSensor[1]] * 2 / 3;
        //            int tmp=newSensorID+1;
        //            string str = "Sensor " + tmp + " is added";
        //            Console.WriteLine(str);
        //        }
        //        else
        //        {
        //            Console.WriteLine("Can't find the closest sensor");
        //        }
        //    }
        //}

        /// <summary>
        /// Select a sensor which is the closest to the position
        /// </summary>
        /// <param name="Pos"></param>
        /// <param name="excludedSensorID"></param>
        /// <returns></returns>
        int ClosestSensor(double[] Pos, int[] excludedSensorID)
        {
            int i,j;
            double MinDist = 10000; // longer than any real distance.
            double dist;
            int minDistSensorID = -1; // invalid sensorID;
            bool isSensorNotSelected;

            for (i = 0; i < sennum; i++)
            {
                isSensorNotSelected = true;
                for (j = 0; j < excludedSensorID.GetLength(0); j++)
                {
                    if (i == excludedSensorID[j])
                    {
                        isSensorNotSelected = false;
                        break;
                    }
                }
                if (isSensorNotSelected)
                {
                    dist = Math.Sqrt( Math.Pow(Pos[0]-SensorPositions[0,i], 2.0) +
                        Math.Pow(Pos[1] - SensorPositions[1, i], 2.0));
                    // distance between the estimated position and the current sensor
                    if (dist < MinDist)
                    {
                        minDistSensorID = i;
                        MinDist = dist;
                    }
                }
            }

            return minDistSensorID;
        }


        public void WeightCenterOfValidSensors(out double[] Pos)
        {
            Pos = new double[2];
            Pos[0] = 30; Pos[1] = 10;
            int ValidSensorCounter=0;
            for (int i = 0; i < sennum; i++)
            {
                if (SensorReading[i] > BrightnessThreshold)
                {
                    ValidSensorCounter++;
                    Pos[0] += SensorPositions[0,i];
                    Pos[1] += SensorPositions[1,i];
                }
            }
            Pos[0] /= ValidSensorCounter;
            Pos[1] /= ValidSensorCounter;
        }


        /// <summary>
        /// Return optimized sampling number (integer) for each time slot
        /// </summary>
        /// <param name="SamplingNum"></param>
        /// <param name="TotalNumSampling"></param>
        public void OptimizeRealSamplingRate(out int[] SamplingNum, out int NumSample)
        {
            double[] NormalizedSamplingRate = new double[sennum];
            NormalizedSamplingRate = AssignSamplingRate(eta, validSensor, Sensitivity);
            isAfterSensorSelection = true;
            
            SamplingNum = new int[sennum];
            for (int i = 0; i < sennum; i++)
            {
                SamplingNum[i] = (int)(TotalSamplingNum * NormalizedSamplingRate[i]);
                RealSampleNum[i] = (double)SamplingNum[i];
            }
            NumSample = TotalSamplingNum;
            // future work: check if each of SamplingNum is in the range
        }

        ///// <summary>
        ///// Returns TotalSamplingNum
        ///// </summary>
        ///// <returns></returns>
        //public byte[] OptimizeRealSamplingRate()
        //{
        //    byte [] tmp=new byte[sennum];
        //    int NSamp;
        //    OptimizeRealSamplingRate(out tmp, out NSamp);
        //    return NSamp;
        //}


        /// <summary>
        /// Returns TotalSamplingNum
        /// </summary>
        /// <returns></returns>
        public bool OptimizeRealSamplingRate(out byte[] SamplingNum)
        {
            int NSamples;
            return OptimizeRealSamplingRate(out SamplingNum, out NSamples);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="SamplingNum"></param>
        /// <param name="TotalNumSampling"></param>
        public bool OptimizeRealSamplingRate(out byte[] SamplingNum, out int NumSample)
        {
            int [] iSamplingNum = new int[sennum];
            int SensorCounter = 0;
            int i;
            int newSensorID;

            int[] SelectedSensorID = new int[2];



            NumSample = TotalSamplingNum;

            OptimizeRealSamplingRate(out iSamplingNum, out TotalSamplingNum);
            SamplingNum = new byte[sennum];
            for(i=0; i<sennum; i++)
                SamplingNum[i] = (byte)iSamplingNum[i];


            // heuristic method
            // make sure 3 sensors are selected
            for (i = 0; i < 2; i++)
                SelectedSensorID[i] = 0;

            int pt = 0;
            for (i = 0; i < sennum; i++)
            {
                if (SamplingNum[i] != 0)
                {
                    SensorCounter++;
                    if (pt < 2)
                    {
                        SelectedSensorID[pt] = i;
                        pt++;
                    }
                }
            }
            if (SensorCounter == 1)
            {
                return false;
            }

            if (SensorCounter == 2)
            {
                // WeightCenter may lead to ambiguous result!

                //double[] WeightCenter = new double[2];
                //WeightCenter[0] = (SensorPositions[0, SelectedSensorID[0]] + SensorPositions[0, SelectedSensorID[1]]) / 2;
                //WeightCenter[1] = (SensorPositions[1, SelectedSensorID[0]] + SensorPositions[1, SelectedSensorID[1]]) / 2;
                // newSensorID = ClosestSensor(WeightCenter, SelectedSensorID);

                // need to add one more sensor
                newSensorID = ClosestSensor(LastPosEst, SelectedSensorID);
                if (newSensorID >= 0)
                {
                    SamplingNum[newSensorID] = (byte)((SamplingNum[SelectedSensorID[0]] + SamplingNum[SelectedSensorID[1]]) / 3);
                    SamplingNum[SelectedSensorID[0]] = (byte)(SamplingNum[SelectedSensorID[0]] * 2 / 3);
                    SamplingNum[SelectedSensorID[1]] = (byte)(SamplingNum[SelectedSensorID[1]] * 2 / 3);
                    int tmp = newSensorID + 1;
                    string str = "Sensor " + tmp + " is added";
                    Console.WriteLine(str);
                }
                else
                {
                    Console.WriteLine("Can't find the closest sensor");
                    return false;
                }

                
            }

            for (i = 0; i < sennum; i++)
            {
                RealSampleNum[i] = (double)SamplingNum[i];
            }

            return true;
        }

        /// <summary>
        /// LSFindPos: estimate the event position using non-linear least square, using a constant step size.
        /// Applicable to 2D and 3D event position fittings.
        /// </summary>
        /// <param name="S">sensor reading (with noise)</param>
        /// <param name="sind">if sensor i is valid, sind[i]==1, otherwise sind[i]==0</param>
        /// <param name="SenPos">sensor positions. It is a 2 by N matrix for 2D.</param>
        /// <param name="K">the coefficient for event model</param>
        /// <param name="C">intensity of the event (such as the brightness of a light bulb)</param>
        /// <param name="Step">step size for the nonlinear optimization</param>
        /// <param name="StoppingThreshold">if the error is less than this threshold, stop the iteration</param>
        /// <param name="PosHat">the event's position. </param>
        /// <param name="rLS">output: the estimated positioning error</param>
        bool LSFindPos(double[] S, int[] sind, double[, ] SenPos, double K, double C, double Step, 
            double StoppingThreshold, double [] initPosHat, out double[] PosHat, out double rLS, out double[,] Sensitivity, 
            double[] RealSampleNumIn)
        {
            int i, cnt, j;
            double dSigTmp;
            int ValidSensorNum = 0;
            // it is applicable to 2D and 3D positions
            PosHat = new double[dim];
            Sensitivity = new double[dim, sennum];

            try
            {
                for (i = 0; i < dim; i++)
                    for (j = 0; j < sennum; j++)
                        Sensitivity[i, j] = 0;

                //// dbg
                //for (i = 0; i < 12; i++)
                //{
                //    S[i] = 0;
                //    sind[i] = 0;
                //}
                //S[4] = 4418; sind[4] = 1;
                //S[6] = 7092; sind[6] = 1;
                //S[10] = 5126; sind[10] = 1;
                //S[11] = 2728; sind[11] = 1;
                //Step = 5e-6;

                // check if sensor readings are meaningful
                ValidSensorNum = 0;
                for (i = 0; i < sennum; i++)
                {
                    ValidSensorNum += sind[i];
                }

                int iteration;


                Matrix SenST = new Matrix(S.GetLength(0), PosHat.GetLength(0));
                double ytmp;
                double PosErr, dist;
                //         double [] grad = new double[dim];

                Matrix mGrad = new Matrix(dim, 1);
                Matrix mPosHat = new Matrix(dim, 1);
                for (i = 0; i < dim; i++)
                    mPosHat[i, 0] = initPosHat[i];

                // SenPos is the sensor positions.
                Matrix mSenPos = new Matrix(SenPos.GetLength(0), SenPos.GetLength(1));
                for (i = 0; i < SenPos.GetLength(0); i++)
                    for (j = 0; j < SenPos.GetLength(1); j++)
                        mSenPos[i, j] = SenPos[i, j];

                Matrix mNewPosHat = new Matrix(dim, 1);
                Matrix mFIM = new Matrix(dim, dim);
                Matrix mTmp = new Matrix(dim, dim);

                //int [] AllRow = new int[dim];

                //for (i=0; i<dim; i++)
                //    AllRow[i]=i;

                iteration = 0;

                // Associated Matlab code (note: define of sind is different in the Matlab code)
                //step = 0.1; % step size for gradient-base convertence
                //err=[];
                //it = 1;
                //eventPosHat=[0;0];    % init position value 
                //SenN = size(SenPos,2);
                //sigma=(sigmaSqForOneSensor^(-2))*eye(SenN,SenN);
                //while it<100
                //    grad = zeros(2,1);
                //    for cnt2 = 1:length(sind)
                //        cnt = sind(cnt2);
                //        SenST(cnt,:)=(2/k)*exp(-norm(SenPos(:,cnt)-eventPosHat)^2/k)*(SenPos(:,cnt)-eventPosHat)';
                //        ytmp = exp(-norm(SenPos(:,cnt)-eventPosHat)^2/k);
                //        grad = grad + (ytmp-S(cnt))*SenST(cnt,:)';
                //    end 
                //    NewEventPosHat=eventPosHat - step*grad;

                //    err=[err norm(NewEventPosHat-eventPosHat)];                    
                //    if abs(err(end))<0.01;
                //        break;
                //    end  

                //    eventPosHat = NewEventPosHat;
                //    it = it+1;
                //end   

                while (iteration < 10000)
                {
                    // reset SenST
                    for (i = 0; i < S.GetLength(0); i++)
                        for (j = 0; j < PosHat.GetLength(0); j++)
                            SenST[i, j] = 0.0;
                    // reset Grad
                    for (j = 0; j < dim; j++)
                        mGrad[j, 0] = 0.0;

                    //              for (i = 0; i < sind.GetLength(0); i++)
                    for (cnt = 0; cnt < sennum; cnt++)
                    {
                        if (sind[cnt] == 1)
                        {
                            //for (j = 0; j < PosHat.GetLength(); j++)
                            //    PosHat[j]=SenPos[j,cnt] - PosHat[j];
                            // Submatrix(mSenPos,AllRow,cnt,cnt) : SenPos(:,cnt)
                            dist = Norm(mSenPos.Submatrix(0, dim - 1, cnt, cnt) - mPosHat);

                            for (j = 0; j < dim; j++)
                            {
                                if (Math.Abs(dist) > 0.1) // if dist is about 0, then skip this code to avoid "divided by 0" errors.
                                {
                                    //   SenST[cnt, j] = C * (2 / K) * Math.Exp(-distSquare / K) * (mSenPos[j, cnt] - mPosHat[j, 0]);
                                    SenST[cnt, j] = (BrightCurve[1] + 2 * BrightCurve[2] * dist) / dist * (mPosHat[j, 0] - mSenPos[j, cnt]);
                                }
                            }

                            // ytmp = Math.Exp(-distSquare / K);
                            ytmp = BrightCurve[0] + BrightCurve[1] * dist + BrightCurve[2] * dist * dist;
                            for (j = 0; j < dim; j++)
                            {
                                mGrad[j, 0] += (ytmp - S[cnt]) * SenST[cnt, j];
                            }
                        }
                    }

                    for (j = 0; j < dim; j++)
                    {
                        mNewPosHat[j, 0] = mPosHat[j, 0] - Step * mGrad[j, 0];
                    }

                    PosErr = Math.Sqrt(NormSquare(mNewPosHat - mPosHat));

                    if (Math.Abs(PosErr) < StoppingThreshold)
                        break;
                    else
                    {
                        // note: mPosHat = mNewPosHat is wrong !!!
                        // use mPosHat = mNewPosHat.Clone(); 
                        // can not overload = to assign values between matrices.
                        mPosHat = mNewPosHat.Clone();
                        iteration++;
                    }
                }

                // Associated Matlab code 
                //rLS=0;
                //M=zeros(2,2);
                //for cnt2 = 1:length(sind)
                //    cnt = sind(cnt2);
                //    LSfim(:,:,cnt)=SenST(cnt,:)'*SenST(cnt,:)*sigma(cnt,cnt);
                //    M=M+LSfim(:,:,cnt);
                //end

                rLS = 0.0;
                Matrix mTran = new Matrix(1, dim);

                // consider spatial variant std. (sigma)
                double distHat = 0;
                double[] sigma = new double[MaxSennum];

                for (i = 0; i < sennum; i++)
                {
                    distHat = Norm(mSenPos.Submatrix(0, dim - 1, i, i) - mPosHat);

                    sigma[i] = StdCurve[0] + StdCurve[1] * distHat + StdCurve[2] * distHat * distHat;
                }

                for (i = 0; i < sennum; i++)
                {
                    // cnt is sensor ID
                    if (sind[i] == 1)
                    {
                        cnt = i;
                        mTran = SenST.Submatrix(cnt, cnt, 0, dim - 1);
                        dSigTmp = sigma[cnt] / (Math.Sqrt(RealSampleNumIn[cnt]));
                        mFIM += mTran.Transpose() * mTran * Math.Pow(dSigTmp, -2.0);
                    }
                }

                //% find the mean of M
                //M=M/length(sind);
                //rLS = trace(inv(M))^(0.5);
                for (i = 0; i < mFIM.Rows; i++)
                    for (j = 0; j < mFIM.Columns; j++)
                        mFIM[i, j] /= (double)ValidSensorNum;




                // prepare to return
                for (i = 0; i < dim; i++)
                    PosHat[i] = mNewPosHat[i, 0];

                Sensitivity = new double[SenST.Rows, SenST.Columns];

                for (i = 0; i < SenST.Rows; i++)
                    for (j = 0; j < SenST.Columns; j++)
                        Sensitivity[i, j] = SenST[i, j];


                if (Math.Abs(mFIM.Determinant) > MinDetFIMThreshold)
                {
                    mTmp = mFIM.Inverse;
                    rLS = Math.Sqrt(mTmp.Trace);
                    return true;
                }
                else
                {
                    rLS = 100; // the matrix is singular, estimation is invalid
                    return false;
                }
            }
            catch (Exception ee)
            {
                Console.WriteLine("Exception in LocFinding");
                rLS = 100;
                return false;
            }
            
        }


        /// <summary>
        /// AssignSamplingRate: sensor selection by sampling rate optimization
        /// </summary>
        /// <param name="eta">Convergence stopping threshold</param>
        /// <param name="sind">Valid sensor index</param>
        /// <param name="SenST">Sensitivity</param>
        /// <returns></returns>
        double [] AssignSamplingRate(double eta, int[] sind,  double[,] SenST)
        {
            // associated Matlab code
                        
            //detM=[];
            //p=ones(SenN,1)/SenN;
            //dim=2;
            //atomfim=zeros(dim,dim,SenN);
            //for cnt=1:SenN
            //    atomfim(:,:,cnt)=SenST(cnt,:)'*SenST(cnt,:)*sigma(cnt,cnt)^(-2);
            //end
            //phi=zeros(SenN,1);
            //eta=1e-5;
            //itnum=0;
            //while 1
            //    M=zeros(dim,dim);
            //    for cnt=1:SenN
            //        M=M+atomfim(:,:,cnt)*p(cnt);
            //    end
            //    detM=[detM det(M)];
            //    for cnt=1:SenN
            //        phi(cnt)=sum(sum(inv(M).*atomfim(:,:,cnt),1),2);
            //    end
            //    if max(phi)/dim < 1+eta
            //        break;
            //    end
            //    p=p.*phi/dim;
            //    itnum=itnum+1;
            //end


            int SensNum = sennum;

            double [] P =new double[SensNum];
            double [] phi = new double[SensNum];
            double maxPhi = 0;
            int i, j, k;
            int itnum = 0;

            Matrix[] mAtomFIM = new Matrix[SensNum];

            try
            {
                for (i = 0; i < SensNum; i++)
                    mAtomFIM[i] = new Matrix(dim, dim);

                Matrix mSenST = new Matrix(SenST.GetLength(0), SenST.GetLength(1));
                for (i = 0; i < SenST.GetLength(0); i++)
                    for (j = 0; j < SenST.GetLength(1); j++)
                        mSenST[i, j] = SenST[i, j];

                Matrix mFIM = new Matrix(dim, dim);
                Matrix mScalar = new Matrix(1, 1);

                // init P
                for (i = 0; i < SensNum; i++)
                    P[i] = 1.0 / SensNum;

                for (i = 0; i < SensNum; i++)
                {
                    mAtomFIM[i] = mSenST.Submatrix(i, i, 0, dim - 1).Transpose() * mSenST.Submatrix(i, i, 0, dim - 1);
                    //                mAtomFIM[i] = mSenST.Submatrix(i, i, 0, dim - 1).Transpose() * mSenST.Submatrix(i, i, 0, dim - 1) * Math.Pow(sigma[i], -2.0);
                }

                for (itnum = 0; itnum < 1000; itnum++)
                //            while(true)
                {
                    for (i = 0; i < dim; i++)
                        for (j = 0; j < dim; j++)
                            mFIM[i, j] = 0.0;

                    for (k = 0; k < SensNum; k++)
                        for (i = 0; i < dim; i++)
                            for (j = 0; j < dim; j++)
                                mFIM[i, j] += mAtomFIM[k][i, j] * P[k];

                    // mFIM += mAtomFIM[i] * P[i];

                    for (i = 0; i < SensNum; i++)
                    {
                        mScalar = mSenST.Submatrix(i, i, 0, dim - 1) * mFIM.Inverse * mSenST.Submatrix(i, i, 0, dim - 1).Transpose();
                        phi[i] = mScalar[0, 0];
                        //                    phi[i] = mScalar[0,0] * Math.Pow(sigma[i], -2.0);
                    }

                    maxPhi = phi[0];
                    for (i = 1; i < SensNum; i++)
                    {
                        if (phi[i] > maxPhi)
                            maxPhi = phi[i];
                    }

                    if ((maxPhi / dim) < (1 + eta))
                    {
                        // System.Console.WriteLine("break");
                        break;
                    }
                    for (i = 0; i < SensNum; i++)
                        P[i] *= phi[i] / dim;
                }
            }
            catch(Exception ee)
            {
                Console.WriteLine("Exception in AssignSamplingRate");
            }
            return P;
        }


        double NormSquare(double[] array)
        {
            double total = 0.0;
            for (int i=0; i<array.GetLength(0); i++)
                total += array[i] * array[i];

            return total;
        }

        double NormSquare(Matrix array)
        {
            double total = 0.0;
            for (int i = 0; i < array.Rows ; i++)
                total += array[i,0] * array[i,0];

            return total;
        }

        double Norm(Matrix array)
        {
            return Math.Sqrt(NormSquare(array));
        }

        /// <summary>
        /// return ||A-B||, A and B must be of the same size
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        double Norm(double[] A, double[] B)
        {
            double sum=0;
            int i;
            for ( i=0; i < A.GetLength(0); i++)
            {
                sum += (A[i] - B[i]) * (A[i] - B[i]);
            }
            return Math.Sqrt(sum);
        }

        public void AssignUniformSamplingRate(int EachSensorSamplingNum)
        {
            for (int i = 0; i < sennum; i++)
            {
                RealSampleNum[i] = (double)EachSensorSamplingNum;
            }
        }


        public bool isDataValid()
        {
            int i;
            int ValidSensorNum = 0;
            // check if sensor readings are meaningful
            //for (i = 0; i < sennum ; i++)
            //{
            //    if (SensorReading[i] > BrightnessThreshold)
            //    {
            //        // valid
            //        ValidSensorNum++;
            //    }
            //}

            //10/13: only check the high sampling rate sensors
            for (i = 0; i < sennum; i++)
            {
                if (RealSampleNum[i] == 0)
                {
                    // ignore low rate sensor
                    // their data should not be received.
                    // they are received due to network imperfectness.
                    SensorReading[i] = 0;
                    validSensor[i] = 0;
                }
                else
                {
                    /// have been checked in AddOneSensorReading(), double check
                    if (SensorReading[i] > BrightnessThreshold)
                    {
                        // valid
                        ValidSensorNum++;
                    }
                }
            }


            // 10/13: if it is "before" position, need to have at least 5 valid reading!
            if (isAfterSensorSelection)
            {
                if (ValidSensorNum < 3)
                {
                    NumOfInvalidDataSets++;
                    // can't not have unique position estimation.
                    Console.WriteLine("Limited num. of sensors. (case after)");
                    return false;
                }
                else
                {
                    NumOfInvalidDataSets = 0;
                    return true;
                }
            }
            else
            {// 10/13: if it is "before" position, need to have at least 6 valid reading!
                if (ValidSensorNum < 4)
                {
                    NumOfInvalidDataSets++;
                    // can't not have unique position estimation.
                    Console.WriteLine("Limited num. of sensors. (case before)");
                    return false;
                }
                else
                {
                    NumOfInvalidDataSets = 0;
                    return true;
                }
            }
        }

        public bool TooManyInvalidDataSets()
        {
            if (NumOfInvalidDataSets >= UpperBoundNumOfInvalidDataSets)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TooManyInvalidPosEstimation()
        {
            if (NubOfInvalidPosEstimation >= MaxNumOfInvalidPosEstimation)
                return true;
            else
                return false;
        }


        public bool isInsideBoard(double[] Pos)
        {
            if (Pos[0] > 0 && Pos[0] < 86.36 && Pos[1] > 0 && Pos[1] < 71.12)
                return true;
            else
                return false;
        }

        /// <summary>
        /// If lamp is moved AND new sampling numbers have not been transmitted, then return true. Otherwise return false.
        /// Remember update sampling numbers to this object using "UpdateSamplingNum"
        /// </summary>
        /// <returns></returns>
        public bool isLampMoved()
        {
            //if (HoldForLampMove)
            //    return true;

            bool ToReturn = false; // reboot is better and wrong estimation

            // If sensor is moved, reset the network, wait for the next data set, and by pass the next data set.
            // Thus the "next data" set can be processed by LSFindPosition and the sampling rate optimization 
            // function. If we do not by pass the function for once, the network is always in the reset mode.
            if (ByPassTheNextIsLampMoved)
                return false; // return false, in order to execute LSFindPosition and the sampling rate optimization

            // once returned from this function, we can add more data into the buffer.
            NewDataCheckedByLampMove = true;


            //else
            //    ToReturn = false;
            if ( ValidPosEstSet==2 // get 2 PosEst and check if the lamp is moved. 
                && Math.Sqrt(NormSquare(AfterPosEst)) > 1 // AfterPosEst != [0,0], not the initial value
                && Math.Sqrt(NormSquare(BeforePosEst)) > 1 //
                )
            {
                // or, if ... reboot
                if (Norm(AfterPosEst, BeforePosEst) > MaxDifferenceBetween2PosEst) //  moved
                {
                    Console.WriteLine("Reboot due to big difference between the 1st and the 2nd position estimations.");
                    ToReturn = true;
                }

                // or, if ... reboot
                if (Norm(LastPosEst, AfterPosEst) > MaxDifferenceBetweenAdjacent2ndPosEst) // moved
                {
                    Console.WriteLine("Reboot due to big difference between the adjacent 2nd position estimations.");
                    ToReturn = true;
                }

                if (isEstimationErrorBig())
                {
                    Console.WriteLine("Reboot due to big error for position estimations.");
                    ToReturn = true;
                }

                if (SizeDifferenceSensorReadingInBuf() == 0)
                {
                    if (isSensorReadingInBufDifferent())
                    // If the latest data is not processed, don't put more data in the buffer. Otherwise, this function can't detect the lamp's motion.
                    {
                        Console.WriteLine("Reboot due to difference of adjacent sensor readings.");
                        ToReturn = true;
                    }
                }
            }

            if (ToReturn)
            {
                ByPassTheNextIsLampMoved = true;
                return true;
            }
            else
            {
                ByPassTheNextIsLampMoved = false;
                return false;
            }

        }

        private bool isSensorReadingInBufDifferent()
        {
            // method 1: check reading directly
            int i, SensorCounter = 0;
            for (i = 0; i < sennum; i++)
            {
                if (BufValidSensorReadingA[i] > BrightnessThreshold)
                    SensorCounter++;
            }

            double AvgDistReading = Norm(BufValidSensorReadingA, BufValidSensorReadingB) / SensorCounter;
            if (AvgDistReading > SensorReadingNormThresholdForMoving)
            {
                return true;
            }
            else
            {
                return false;
            }

            // method 2: check if the estimated position is moved

            //if (Math.Sqrt(moveDist[0] * moveDist[0] + moveDist[1] * moveDist[1]) > SensorSelection.MoveThresholdRatio * ErrR)
            //if (Math.Sqrt(moveDist[0] * moveDist[0] + moveDist[1] * moveDist[1]) > SensorSelection.MoveThresholdDist)
            //{
            //    // if detects 30cm movement, start the iteration again.
            //    DSNSensorSelection.Reset();
            //    ResetDSNPosEst();
            //}
        }

        private int SizeDifferenceSensorReadingInBuf()
        {
            int cntA=0, cntB = 0;
            int i;
            for (i = 0; i < sennum; i++)
            {
                if (BufValidSensorReadingA[i] > BrightnessThreshold)
                    cntA++;
                if (BufValidSensorReadingB[i] > BrightnessThreshold)
                    cntB++;
            }

            return Math.Abs(cntA - cntB);
        }

        private bool isEstimationErrorBig()
        {
            if (rLS > MaxAllowedPositionError)
                return true;
            else
                return false;
        }


        private bool StoreResultInBuffer(double[] SensorInput, double[] NewPos)
        {
            int i;
            int ValidSensorNum = 0;

            if (NewDataCheckedByLampMove)
            {
                //isLampMoved has processed the existing data

                for (i = 0; i < sennum; i++)
                {
                    if (SensorInput[i] > BrightnessThreshold)
                    {
                        // valid
                        ValidSensorNum++;
                    }
                    else
                    {
                        SensorInput[i] = 0;
                    }

                    BufValidSensorReadingB[i] = BufValidSensorReadingA[i];
                    BufValidSensorReadingA[i] = SensorInput[i];
                }
                BufEstPositionB[0] = BufEstPositionA[0]; // x
                BufEstPositionA[0] = NewPos[0];
                BufEstPositionB[1] = BufEstPositionA[1]; // y
                BufEstPositionA[1] = NewPos[1];

                NewDataCheckedByLampMove = false; // hold before isLampMoved check the buffer
                return true; // new data is stored
            }
            else
            {
                // existing data have not been checked. ignore the latest data
                return false;
            }
        }


        private bool ClearAndFillBuffer(double[] SensorInput, double[] NewPos)
        {
            int i;
            int ValidSensorNum = 0;

            if (NewDataCheckedByLampMove) 
            {
                //isLampMoved has processed the existing data

                for (i = 0; i < sennum; i++)
                {
                    if (SensorInput[i] > BrightnessThreshold)
                    {
                        // valid
                        ValidSensorNum++;
                    }
                    else
                    {
                        SensorInput[i] = 0;
                    }
                    BufValidSensorReadingA[i] = SensorInput[i];
                    BufValidSensorReadingB[i] = BufValidSensorReadingA[i];
                }
                BufEstPositionA[0] = NewPos[0];
                BufEstPositionB[0] = BufEstPositionA[0]; // x
                BufEstPositionA[1] = NewPos[1];
                BufEstPositionB[1] = BufEstPositionA[1]; // y

                NewDataCheckedByLampMove = false; // hold before isLampMoved check the buffer
                return true; // new data is stored
            }
            else
            {
                // existing data have not been checked. ignore the latest data
                return false;
            }
        }

        /// <summary>
        /// UpdateSamplingNum: always update sampling numbers after assign new numbers to the network. Wrong sampling numbers introduce problem
        /// for positioning error estimation.
        /// </summary>
        /// <param name="SamplingNumIn"></param>
        public void UpdateSamplingNum(byte[] SamplingNumIn)
        {
            for (int i = 0; i < sennum; i++)
            {
                RealSampleNum[i] = (double)SamplingNumIn[i];
            }

            // lamp is not moved
            // check isLampMoved()
            //HoldForLampMove = false; 
        }

        public double GetAvgLight()
        {
            double sum=0;
            for (int i = 0; i < sennum; i++)
            {
                sum += SensorReading[i];
            }
            sum /= (double)sennum;
            return sum;
        }
    }
}

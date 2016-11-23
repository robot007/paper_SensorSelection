/*	
  N. Cihan Tas
  started: 9/19/2006

  Implementation of DSN Demo Sampling Rate Protocol (DDSRP)
  motived for the DSN Demo.

  See the associated word document.
 */


includes DSNDemoMsg;

#define INTER_SENDING_TIME 10

module DSNDemoM{

  provides{

    interface StdControl;

  }

  uses{
    
    interface ReceiveMsg;
    interface ReceiveMsg    as DebugReceive;
    interface SendMsg;
    interface StdControl    as CommControl;
    interface Timer;
    interface Timer         as SendTimer;
    interface Leds;

    interface ADC           as Light;
    interface StdControl    as LightControl;

    interface CC2420Control as RadioTuner;
  }
}

  implementation{

    uint16_t cycleNo;
    uint16_t seqNum;
    int      status;
    uint8_t  timerPeriod;
    uint16_t timeInterval;

    TOS_Msg  outMessage;

    uint16_t dataValue;

    int      dataCounter;
    double   dataSum;
    uint16_t dataToSend;

    uint16_t lastdata;

    command result_t StdControl.init(){ 

      
      call CommControl.init();
      call Leds.init();
      call LightControl.init();
      
      return SUCCESS;
    }


    command result_t StdControl.start() {

      call RadioTuner.enableAutoAck();
      call RadioTuner.enableAddrDecode();
      call RadioTuner.TunePreset(26);

      call CommControl.start();
      call LightControl.start();

      //Only red is on, On but not activated.
      call Leds.greenOff();
      call Leds.yellowOff();
      call Leds.redOn();
      
      atomic{
        cycleNo      = 0;
        timeInterval = 1000;
        dataValue    = 0;
        seqNum = 1;
        dataSum = 0;
      }      

      return SUCCESS;
    }


    command result_t StdControl.stop() {

      call CommControl.stop();
      call LightControl.stop();

      return SUCCESS;

    }


    void SendData( uint16_t data ){


      DataMsg_t dataMessage  = (DataMsg_t)malloc( sizeof(DataMsg_s));

      atomic{
        dataValue  = data;
        dataMessage ->source  = TOS_LOCAL_ADDRESS;
        dataMessage ->seqNum  = seqNum++;
        dataMessage ->cycleNo = cycleNo;
        dataMessage ->value   = dataToSend;
      }


      memcpy( outMessage.data, dataMessage, sizeof(DataMsg_s) );

      call Leds.yellowToggle();

      //SendEvent

      call SendMsg.send( 0,//Send it to the base station
                         sizeof(DataMsg_s),
                         &outMessage);

      free( dataMessage );

    }

    async event result_t Light.dataReady(uint16_t data){

      atomic{

        dataSum+= data;
        dataCounter++;
        lastdata = data;
      }

      return SUCCESS;      
      

    }


    event result_t SendTimer.fired(){

      SendData( dataToSend );

      return SUCCESS;
    }

    event result_t Timer.fired(){

      
      if(dataCounter >= timerPeriod){

        //Send the data.
        atomic{
          dataSum /= dataCounter;
          dataToSend = (uint16_t) dataSum;
          dataSum     = 0;
          dataCounter = 0;
        }

        //SendData( dataToSend );
        call SendTimer.start(TIMER_ONE_SHOT, INTER_SENDING_TIME * TOS_LOCAL_ADDRESS);
        
      }else{

        //Collect More Data
        call Light.getData();
      }
      
      return SUCCESS;

    }

    void RestartTimer(){            

      uint32_t tempPeriod;

      atomic{

        if (timerPeriod != 0){

          //Find the time to tick.
          tempPeriod = (uint32_t)((double)timeInterval/((double)timerPeriod+1));

          atomic{
            dataCounter = 0;
            dataSum     = 0;
          }
          call Timer.start(TIMER_REPEAT,tempPeriod);
          call Leds.redOff();
          call Leds.greenOn();
          
        }else{


          call Leds.redOn();
          call Leds.greenOff();
          call Leds.yellowOff();
          call Timer.stop();
        }
      }
      
      
    }


    void ActivationReceived(ActivationMsg_t activationMsg){

      if ( activationMsg->cycleNo > cycleNo ){

        //Find the associated sampling rate assigned for this mote.      
        atomic{
          timerPeriod = (uint8_t) activationMsg->sample[TOS_LOCAL_ADDRESS-1];
          cycleNo     = activationMsg -> cycleNo;
        }
        RestartTimer();
      }else{

        //This is a repeat Message, ignore.
      }

    }

    event result_t SendMsg.sendDone(TOS_MsgPtr sent, result_t success){


      return SUCCESS;
    }

    event TOS_MsgPtr DebugReceive.receive( TOS_MsgPtr m ){

      DebugMsg_t msg = (DebugMsg_t) &(m->data);


      switch( msg -> index){

      case 1:
        //timeInterval debug message.
        //Find the associated sampling rate assigned for this mote.      
        atomic{ 
          timeInterval = msg -> value; 
        } 
        RestartTimer();
        break;
      case 2:
        atomic{
          cycleNo      = 0;
          timeInterval = 1000;
          dataValue    = 0;
          seqNum = 1;
          dataSum = 0;
        }
        break;
      default:
        break;
        

      }

      return m;

    }

    event TOS_MsgPtr ReceiveMsg.receive( TOS_MsgPtr m ){

      ActivationMsg_t msg = (ActivationMsg_t) &(m->data);
      
      ActivationReceived(msg);

      return m;
    }
    

  }//implementation

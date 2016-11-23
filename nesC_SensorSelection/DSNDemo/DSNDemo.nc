
configuration DSNDemo{

}

implementation{

  components Main, 
             DSNDemoM as Demo,
             GenericComm,
             TimerC,
             LedsC,
             HamamatsuC,
             CC2420ControlM; 


  Main.StdControl   -> Demo;

  Demo.ReceiveMsg   -> GenericComm.ReceiveMsg[0x11];//DSNDemoActivationMessage
  Demo.SendMsg      -> GenericComm.SendMsg[0x12];//DSNDemoDataMessage
  Demo.DebugReceive -> GenericComm.ReceiveMsg[0x14];//DSNDemoDebugMessage
  Demo.CommControl  -> GenericComm;
  Demo.Timer        -> TimerC.Timer[unique("Timer")];
  Demo.SendTimer    -> TimerC.Timer[unique("Timer")];
  Demo.Leds         -> LedsC;

  Demo.Light        -> HamamatsuC.PAR;
  Demo.LightControl -> HamamatsuC;
  
  Demo.RadioTuner   -> CC2420ControlM;

}

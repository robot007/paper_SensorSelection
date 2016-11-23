typedef struct ActivationMsg{

  uint16_t cycleNo;
  uint8_t sample[15];

}*ActivationMsg_t, ActivationMsg_s;


typedef struct DataMsg{

  uint16_t source;
  uint16_t seqNum;
  uint16_t cycleNo;
  uint16_t value;

}*DataMsg_t, DataMsg_s;

typedef struct DebugMsg{

  uint16_t index;
  uint16_t value;

}*DebugMsg_t, DebugMsg_s;

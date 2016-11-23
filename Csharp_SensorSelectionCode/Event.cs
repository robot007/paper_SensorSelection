using System;

namespace DataAcquisitionService
{
	/// <summary>
	/// Summary description for Event.
	/// </summary>
	[Serializable]
	public class Event
	{
		public string sEventName;
		public string sService;
		public string sVariable;
		public string sBaseStationID;
		public string sSensorID;
		public HermesMiddleware.DataAcquisitionServiceServer.dataItem[] dataArray;

		public Event(string sEventName, string sService, string sVariable, string sBaseStationID, string sSensorID, HermesMiddleware.DataAcquisitionServiceServer.dataItem[] dataArray)
		{
			this.sEventName = sEventName;
			this.sService = sService;
			this.sVariable = sVariable;
			this.sBaseStationID = sBaseStationID;
			this.sSensorID = sSensorID;
			this.dataArray = dataArray;
		}

		public Event(Event eventSource)
		{
			this.sEventName = eventSource.sEventName;
			this.sService = eventSource.sService;
			this.sVariable = eventSource.sVariable;
			this.sBaseStationID = eventSource.sBaseStationID;
			this.sSensorID = eventSource.sSensorID;
			this.dataArray = eventSource.dataArray;
		}


		public Event(HermesMiddleware.DataAcquisitionServiceProxy.Event hermesEvent) {

			this.sEventName = hermesEvent.name;

			//I am skipping the service name.
			//this.sService;

			this.sVariable = hermesEvent.sensorData.variable;
			this.sBaseStationID = hermesEvent.sensorData.baseStation;
			this.sSensorID = hermesEvent.sensorData.sensorID;

			this.dataArray = new HermesMiddleware.DataAcquisitionServiceServer.dataItem[hermesEvent.sensorData.dataArray.Length];
			for (int i = 0; i < hermesEvent.sensorData.dataArray.Length; i++)
			{
				this.dataArray[i] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
				this.dataArray[i].timestamp = hermesEvent.sensorData.dataArray[i].timestamp;
				this.dataArray[i].value = hermesEvent.sensorData.dataArray[i].value;
			}

		
		}

		//To make xml serialization
		public Event() { 
		
		
		}
	}
}

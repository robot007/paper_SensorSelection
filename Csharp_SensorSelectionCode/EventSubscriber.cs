using System;
using System.Threading;
using System.Collections;
using System.Web.Services.Protocols;

// For RailProtect

using System.Net;

namespace DataAcquisitionService
{
	/// <summary>
	/// Summary description for EventSubscriber.
	/// </summary>
	public class EventSubscriber
	{
		// callback URL
		public string sCallbackURL;

		// event subscription
		public EventSubscription subscription;

		// counters
		static public long lNotifyCounter = 0;
		static public long lEventsCounter = 0;

		// service
		public string sService;

		// reference to global stuff
		Global global;

		// events
		private ArrayList events = new ArrayList();

        bool camera1moved = false;
		
        // performance measurement
		static private int nLastPerformanceTickCount = 0;
		static private int nPerformanceTickCountDelta = 2000;
		static private int nPerformanceEventsCounter = 0;
		static private double dEventsPerSecond = 0;

		public EventSubscriber(Global global, string sCallbackURL, EventSubscription subscription)
		{
			this.global = global;
			this.sCallbackURL = sCallbackURL;
			this.subscription = subscription;
		}

		public void AddEvent(Event myEvent)
		{
			try
			{
				events.Add(myEvent);
			}
			catch(Exception exp)
			{
				Console.WriteLine(exp);
			}
		}

		public void Notify()
		{
			if (events.Count == 0)
			{
				// no events collected => just return
				return;
			}

			try
			{
				// prepare events
				HermesMiddleware.DataAcquisitionServiceProxy.Event[] tmpEvents = new HermesMiddleware.DataAcquisitionServiceProxy.Event[events.Count];

				for(int i = 0; i < events.Count; i++)
				{
					Event myEvent = (Event)events[i];
					// prepare data
					HermesMiddleware.DataAcquisitionServiceProxy.Event tmpEvent = new HermesMiddleware.DataAcquisitionServiceProxy.Event();
					// put name of the subscription as event name
					tmpEvent.name = subscription.sName;
					myEvent.sEventName = subscription.sName;
					tmpEvent.sensorData = new HermesMiddleware.DataAcquisitionServiceProxy.SensorData();
					tmpEvent.sensorData.variable = myEvent.sVariable;
					tmpEvent.sensorData.sensorID = myEvent.sSensorID;
					tmpEvent.sensorData.baseStation = myEvent.sBaseStationID;

					tmpEvent.sensorData.dataArray = new HermesMiddleware.DataAcquisitionServiceProxy.dataItem[myEvent.dataArray.Length];

					for (int k = 0; k < myEvent.dataArray.Length; k++)
					{					
						tmpEvent.sensorData.dataArray[k] = new HermesMiddleware.DataAcquisitionServiceProxy.dataItem();
						tmpEvent.sensorData.dataArray[k].timestamp = myEvent.dataArray[k].timestamp;
						tmpEvent.sensorData.dataArray[k].value = myEvent.dataArray[k].value ;
					}

					tmpEvents[i] = tmpEvent;
				}

				if (sCallbackURL.Contains("http"))
				{

					//This is a wsdl address.
					// initialize service proxy
					HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService serviceProxy = new HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService();
                    serviceProxy.Url = sCallbackURL;//"http\\localhost";

					// call client
					serviceProxy.EventCallback(sService, tmpEvents);

				}
				else {

					for (int i = 0; i < events.Count; i++)
					{
						RawClientCommunicator rwc = new RawClientCommunicator(sCallbackURL);
						rwc.SendEvent( (Event)events[i] );
						rwc.Close();
					}
				
				}

				// increment counters
				lNotifyCounter++;
				lEventsCounter += events.Count;

				// measure performance
				int nTickcount = System.Environment.TickCount;
				if (nTickcount > nLastPerformanceTickCount + nPerformanceTickCountDelta)
				{
					// calculate performance
					dEventsPerSecond = (double)nPerformanceEventsCounter / (double)(nTickcount - nLastPerformanceTickCount) * 1000;
 
					// reset counters
					nPerformanceEventsCounter = 0;
					nLastPerformanceTickCount = nTickcount;
				}
				else
				{
					nPerformanceEventsCounter += events.Count;
				}

				global.mainService.PrintStatus("Notifications: " + lNotifyCounter.ToString() + ", Events: " + lEventsCounter.ToString() + ", Events per Notification: " + ((int)(lEventsCounter / lNotifyCounter)).ToString() + ", Events per second: " + dEventsPerSecond.ToString());
				//Console.WriteLine("Event count: " + events.Count + ", Notifications: " + lNotifyCounter.ToString() + ", Events: " + lEventsCounter.ToString());
			}
			catch(Exception exp)
			{
				Console.WriteLine(exp);
			}

			// in every case clear events
			events.Clear();
		}
	}
}

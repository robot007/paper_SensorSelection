using System.Diagnostics;
using System.Web.Services;
using System.ComponentModel;
using System.Web.Services.Protocols;
using System;
using System.Xml.Serialization;
using System.Collections;
using HermesMiddleware.DataAcquisitionServiceServer;
using HermesMiddleware.DataAcquisitionServiceProxy;
using HermesMiddleware.CameraLibrary;

namespace DataAcquisitionService
{
	/// <remarks/>
	[System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
	[System.Web.Services.WebServiceAttribute(Namespace = "http://www.search-on-siemens.com/DataAcquisitionService")]
	[System.Web.Services.WebServiceBindingAttribute(Name = "DataAcquisitionServiceSoap", Namespace = "http://www.search-on-siemens.com/DataAcquisitionService")]
	public class DataAcquisitionService : HermesMiddleware.DataAcquisitionServiceServer.DataAcquisitionService
	{
		[System.Web.Services.WebMethodAttribute()]
		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("RegisterPeer", RequestNamespace = "http://www.search-on-siemens.com/DataAcquisitionService", OneWay = true, Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		public override void RegisterPeer(string hostName, string service)
		{
			// get global stuff (cross-application-domain)
			Global global = (Global)Global.GetObject(Global.sClassName, Global.port);

			// register peer
			global.RegisterPeer(hostName, service);
		}

		[System.Web.Services.WebMethodAttribute()]
		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("UnregisterPeer", RequestNamespace = "http://www.search-on-siemens.com/DataAcquisitionService", OneWay = true, Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		public override void UnregisterPeer(string hostName)
		{
			// get global stuff (cross-application-domain)
			Global global = (Global)Global.GetObject(Global.sClassName, Global.port);

			// unregister peer
			global.UnregisterPeer(hostName);
		}

		[System.Web.Services.WebMethodAttribute()]
		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("Publish", RequestNamespace = "http://www.search-on-siemens.com/DataAcquisitionService", OneWay = true, Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		public override void Publish(string service, [System.Xml.Serialization.XmlElementAttribute("variables")] string[] variables, string address)
		{
			// get global stuff (cross-application-domain)
			Global global = (Global)Global.GetObject(Global.sClassName, Global.port);

			// publish variables
			global.Publish(service, variables, address);
		}

		[System.Web.Services.WebMethodAttribute()]
		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("GetPeers", RequestElementName = "GetPeersRequest", RequestNamespace = "http://www.search-on-siemens.com/DataAcquisitionService", ResponseNamespace = "http://www.search-on-siemens.com/DataAcquisitionService", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		[return: System.Xml.Serialization.XmlElementAttribute("addresses")]
		public override string[] GetPeers(string service, string variable)
		{
			// get global stuff (cross-application-domain)
			Global global = (Global)Global.GetObject(Global.sClassName, Global.port);

			// get peers
			return global.GetPeers(service, variable);
		}

		[System.Web.Services.WebMethodAttribute()]
		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("Query", RequestElementName = "QueryRequest", RequestNamespace = "http://www.search-on-siemens.com/DataAcquisitionService", ResponseNamespace = "http://www.search-on-siemens.com/DataAcquisitionService", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		[return: System.Xml.Serialization.XmlElementAttribute("sensorData")]
		public override HermesMiddleware.DataAcquisitionServiceServer.SensorData[] Query(string service, [System.Xml.Serialization.XmlElementAttribute("expressions")] HermesMiddleware.DataAcquisitionServiceServer.ExpressionType[] expressions, bool delegateCall)
		{
			ArrayList sensorDataArray = new ArrayList(); 

			if (expressions == null || expressions.Length == 0 || expressions[0].Items == null)
			{
				return null;
			}

			// get global stuff (cross-application-domain)
			Global global = (Global)Global.GetObject(Global.sClassName, Global.port);

			// get addresses of peers publishing the variable
			string[] sPeerAddresses = _GetPeers(global, service, expressions[0].Items[0].variable);
            if (expressions[0].Items[0].variable == "x" || expressions[0].Items[0].variable == "y" || expressions[0].Items[0].variable == "z")
            {
                sPeerAddresses = _GetPeers(global, service, "Position");
            }

			if (sPeerAddresses != null)
			{
				foreach (string sPeerAddress in sPeerAddresses)
				{
					// get first part of name (e.g. PCC085C.us008.siemens.net)
					string[] sParts = sPeerAddress.Split('.');

					if (String.Compare(sParts[0], Environment.MachineName, true) == 0)
					{
						// peer is this machine => perform query
						global.Query(service, expressions, ref sensorDataArray);
					}
					else
					{
						if (delegateCall)
						{

							try
							{
								// peer is different machine => delegate it to its DataAcquisitionService

								// initialize service proxy
								HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService serviceProxy = new HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService();
								serviceProxy.Url = "http://" + sPeerAddress + ":" + global.portNumber + global.sVirtRoot + global.sASMX;

								HermesMiddleware.DataAcquisitionServiceProxy.ExpressionType[] tmpExpressions = new HermesMiddleware.DataAcquisitionServiceProxy.ExpressionType[expressions.Length];
								for (int i = 0; i < expressions.Length; i++)
								{
									tmpExpressions[i] = ConvertServer2Proxy(expressions[i]);
								}

								// call query (false = do not delegate further)
								HermesMiddleware.DataAcquisitionServiceProxy.SensorData[] sensorDataProxy = serviceProxy.Query(service, tmpExpressions, false);
								HermesMiddleware.DataAcquisitionServiceServer.SensorData[] tmpSensorData = this.ConvertProxy2Server(sensorDataProxy);

								if (tmpSensorData != null && tmpSensorData.Length > 0)
								{
									// add data to array
									foreach (HermesMiddleware.DataAcquisitionServiceServer.SensorData data in tmpSensorData)
									{
										sensorDataArray.Add(data);
									}
								}
							}
							catch (Exception exp)
							{
								Console.WriteLine(exp);
							}
						}
					}
				}
			}

			// copy result to simple array
			HermesMiddleware.DataAcquisitionServiceServer.SensorData[] sensorData = new HermesMiddleware.DataAcquisitionServiceServer.SensorData[sensorDataArray.Count];
			for (int i = 0; i < sensorDataArray.Count; i++)
			{
				sensorData[i] = (HermesMiddleware.DataAcquisitionServiceServer.SensorData)sensorDataArray[i];
			}

			return sensorData;
		}

		[System.Web.Services.WebMethodAttribute()]
		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("Subscribe", RequestNamespace = "http://www.search-on-siemens.com/DataAcquisitionService", OneWay = true, Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		public override void Subscribe(string service, [System.Xml.Serialization.XmlElementAttribute("subscriptions")] HermesMiddleware.DataAcquisitionServiceServer.Subscription[] subscriptions, string callbackURL, bool delegateCall)
		{
			// get global stuff (cross-application-domain)
			Global global = (Global)Global.GetObject(Global.sClassName, Global.port);

			// get addresses of peers (variable is null => we want all peers for given service)
			string[] sPeerAddresses = _GetPeers(global, service, null);
		
			if (sPeerAddresses != null)
			{
				foreach (string sPeerAddress in sPeerAddresses)
				{
					// get first part of name (e.g. PCC085C.us008.siemens.net)
					string[] sParts = sPeerAddress.Split('.');

					if (String.Compare(sParts[0], Environment.MachineName, true) == 0)
					{
						// peer is this machine => remember subscriptions
						foreach (HermesMiddleware.DataAcquisitionServiceServer.Subscription subscription in subscriptions)
						{
							// copy subscription
							EventSubscription eventSubscription = new EventSubscription();
							eventSubscription.Initialize(subscription);

							// add subscriber
							global.AddSubscriber(callbackURL, eventSubscription);
						}
					}
					else
					{
						if (delegateCall)
						{
							try
							{
								// peer is different machine => delegate it to its DataAcquisitionService

								// initialize service proxy
								HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService serviceProxy = new HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService();
								serviceProxy.Url = "http://" + sPeerAddress + ":" + global.portNumber + global.sVirtRoot + global.sASMX;

								HermesMiddleware.DataAcquisitionServiceProxy.Subscription[] tmpSubscriptions = new HermesMiddleware.DataAcquisitionServiceProxy.Subscription[subscriptions.Length];
								for (int i = 0; i < subscriptions.Length; i++)
								{
									tmpSubscriptions[i] = ConvertServer2Proxy(subscriptions[i]);
								}

								// subscribe
								serviceProxy.Subscribe(service, tmpSubscriptions, callbackURL, false);
							}
							catch (Exception exp)
							{
								Console.WriteLine(exp);
							}
						}
					}
				}
			}
		}

        [System.Web.Services.WebMethodAttribute()]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("UpdateDatabase", RequestNamespace = "http://www.search-on-siemens.com/DataAcquisitionService", OneWay = true, Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public override void UpdateDatabase(string sSensorId, string sMoteType, string sVariable, long lTimestamp, object oValue)
        {
            Global global = (Global)Global.GetObject(Global.sClassName, Global.port);
            global.UpdateDatabase(sSensorId, sMoteType, sVariable, lTimestamp, oValue);
        }

		[System.Web.Services.WebMethodAttribute()]
		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("Unsubscribe", RequestNamespace = "http://www.search-on-siemens.com/DataAcquisitionService", OneWay = true, Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		public override void Unsubscribe(string service, [System.Xml.Serialization.XmlElementAttribute("subscriptions")] HermesMiddleware.DataAcquisitionServiceServer.Subscription[] subscriptions, bool delegateCall)
		{
			// get global stuff (cross-application-domain)
			Global global = (Global)Global.GetObject(Global.sClassName, Global.port);

			// get addresses of peers (variable is null => we want all peers for given service)
			string[] sPeerAddresses = _GetPeers(global, service, null);

			if (sPeerAddresses != null)
			{
				foreach (string sPeerAddress in sPeerAddresses)
				{
					// get first part of name (e.g. PCC085C.us008.siemens.net)
					string[] sParts = sPeerAddress.Split('.');

					if (String.Compare(sParts[0], Environment.MachineName, true) == 0)
					{
						// peer is this machine => remove subscriptions
						foreach (HermesMiddleware.DataAcquisitionServiceServer.Subscription subscription in subscriptions)
						{
							// remove subscriber
							global.RemoveSubscriber(subscription.GUID);
						}
					}
					else
					{
						if (delegateCall)
						{
							try
							{
								// peer is different machine => delegate it to its DataAcquisitionService

								// initialize service proxy
								HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService serviceProxy = new HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService();
								serviceProxy.Url = "http://" + sPeerAddress + ":" + global.portNumber + global.sVirtRoot + global.sASMX;

								HermesMiddleware.DataAcquisitionServiceProxy.Subscription[] tmpSubscriptions = new HermesMiddleware.DataAcquisitionServiceProxy.Subscription[subscriptions.Length];
								for (int i = 0; i < subscriptions.Length; i++)
								{
									tmpSubscriptions[i] = ConvertServer2Proxy(subscriptions[i]);
								}

								// unsubscribe
								serviceProxy.Unsubscribe(service, tmpSubscriptions, false);
							}
							catch (Exception exp)
							{
								Console.WriteLine(exp);
							}
						}
					}
				}
			}
		}

		[System.Web.Services.WebMethodAttribute()]
		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("EventCallback", RequestNamespace = "http://www.search-on-siemens.com/DataAcquisitionService", OneWay = true, Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		public override void EventCallback(string service, [System.Xml.Serialization.XmlElementAttribute("events")] HermesMiddleware.DataAcquisitionServiceServer.Event[] events)
		{
			System.Windows.Forms.MessageBox.Show("Callback in DataAcquisitionService!");
		}

        [System.Web.Services.WebMethodAttribute()]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("GetCameraImage", RequestElementName = "GetCameraImageRequest", RequestNamespace = "http://www.search-on-siemens.com/DataAcquisitionService", ResponseNamespace = "http://www.search-on-siemens.com/DataAcquisitionService", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        [return: System.Xml.Serialization.XmlElementAttribute("rawImage")]
        public override byte[] GetCameraImage(int height, int width)
        {
            PTZCamera camera = (PTZCamera)PTZCamera.GetObject(PTZCamera.sClassName, PTZCamera.port);
            return camera.GetRawImage();
        }

        [System.Web.Services.WebMethodAttribute()]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("MoveCamera", RequestElementName = "MoveCameraRequest", RequestNamespace = "http://www.search-on-siemens.com/DataAcquisitionService", ResponseNamespace = "http://www.search-on-siemens.com/DataAcquisitionService", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public override void MoveCamera(int moveType)
        {
            //PTZCamera camera = (PTZCamera)PTZCamera.GetObject(PTZCamera.sClassName, PTZCamera.port);
            //camera.Move(moveType);
            PTZCamControl.Instance.Move(moveType);
        }

        [System.Web.Services.WebMethodAttribute()]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("MoveCameraTo", RequestElementName = "MoveCameraToRequest", RequestNamespace = "http://www.search-on-siemens.com/DataAcquisitionService", ResponseNamespace = "http://www.search-on-siemens.com/DataAcquisitionService", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public override void MoveCameraTo(int pan, int tilt, int zoom)
        {
            PTZCamControl.Instance.MoveToPosition(new CamPosition(pan, tilt, zoom));
        }

		public HermesMiddleware.DataAcquisitionServiceProxy.Subscription ConvertServer2Proxy(HermesMiddleware.DataAcquisitionServiceServer.Subscription sourceSubscription)
		{
			HermesMiddleware.DataAcquisitionServiceProxy.Subscription targetSubscription = new HermesMiddleware.DataAcquisitionServiceProxy.Subscription();
			targetSubscription.GUID = sourceSubscription.GUID;
			targetSubscription.name = sourceSubscription.name;
			targetSubscription.variable = sourceSubscription.variable;
			targetSubscription.Items = new HermesMiddleware.DataAcquisitionServiceProxy.Expression[sourceSubscription.Items.Length];

			for (int i = 0; i < sourceSubscription.Items.Length; i++)
			{
				targetSubscription.Items[i] = ConvertServer2Proxy(sourceSubscription.Items[i]);
			}

			return targetSubscription;
		}

		HermesMiddleware.DataAcquisitionServiceProxy.Expression ConvertServer2Proxy(HermesMiddleware.DataAcquisitionServiceServer.Expression sourceExpression)
		{
			HermesMiddleware.DataAcquisitionServiceProxy.Expression targetExpression = null;
			HermesMiddleware.DataAcquisitionServiceProxy.Range targetRange = null;
			HermesMiddleware.DataAcquisitionServiceProxy.Threshold targetThreshold = null;
			HermesMiddleware.DataAcquisitionServiceProxy.Equals targetEquals = null;

			HermesMiddleware.DataAcquisitionServiceServer.Range sourceRange = null;
			HermesMiddleware.DataAcquisitionServiceServer.Threshold sourceThreshold = null;
			HermesMiddleware.DataAcquisitionServiceServer.Equals sourceEquals = null;

			switch (sourceExpression.GetType().Name)
			{
				case "Range":
					sourceRange = (HermesMiddleware.DataAcquisitionServiceServer.Range)sourceExpression;
					targetRange = new HermesMiddleware.DataAcquisitionServiceProxy.Range();
					targetRange.max = sourceRange.max;
					targetRange.min = sourceRange.min;
					targetRange.strict = sourceRange.strict;
					targetRange.variable = sourceRange.variable;
					targetExpression = targetRange;
					break;
				case "Threshold":
					sourceThreshold = (HermesMiddleware.DataAcquisitionServiceServer.Threshold)sourceExpression;
					targetThreshold = new HermesMiddleware.DataAcquisitionServiceProxy.Threshold();
					targetThreshold.value = sourceThreshold.value;
					targetThreshold.type = (HermesMiddleware.DataAcquisitionServiceProxy.ThresholdType)sourceThreshold.type;
					targetThreshold.strict = sourceThreshold.strict;
					targetThreshold.variable = sourceThreshold.variable;
					targetExpression = targetThreshold;
					break;
				case "Equals":
					sourceEquals = (HermesMiddleware.DataAcquisitionServiceServer.Equals)sourceExpression;
					targetEquals = new HermesMiddleware.DataAcquisitionServiceProxy.Equals();
					targetEquals.value = sourceEquals.value;
					targetEquals.type = (HermesMiddleware.DataAcquisitionServiceProxy.EqualsType)sourceEquals.type;
					targetEquals.variable = sourceEquals.variable;
					targetExpression = targetEquals;
					break;
			}

			return targetExpression;
		}

		public HermesMiddleware.DataAcquisitionServiceProxy.ExpressionType ConvertServer2Proxy(HermesMiddleware.DataAcquisitionServiceServer.ExpressionType sourceExpressionType)
		{
			HermesMiddleware.DataAcquisitionServiceProxy.ExpressionType targetExpressionType = new HermesMiddleware.DataAcquisitionServiceProxy.ExpressionType();
			targetExpressionType.Items = new HermesMiddleware.DataAcquisitionServiceProxy.Expression[sourceExpressionType.Items.Length];
			
			for (int i = 0; i < sourceExpressionType.Items.Length; i++)
			{
				targetExpressionType.Items[i] = ConvertServer2Proxy(sourceExpressionType.Items[i]);
			}

			return targetExpressionType;
		}

		private HermesMiddleware.DataAcquisitionServiceServer.SensorData[] ConvertProxy2Server(HermesMiddleware.DataAcquisitionServiceProxy.SensorData[] sourceSensorData)
		{
			HermesMiddleware.DataAcquisitionServiceServer.SensorData[] targetSensorData = null;

			if (sourceSensorData != null)
			{
				targetSensorData = new HermesMiddleware.DataAcquisitionServiceServer.SensorData[sourceSensorData.Length];
				for (int i = 0; i < sourceSensorData.Length; i++)
				{
					targetSensorData[i] = new HermesMiddleware.DataAcquisitionServiceServer.SensorData();
					targetSensorData[i].baseStation = sourceSensorData[i].baseStation;
					targetSensorData[i].sensorID = sourceSensorData[i].sensorID;
					targetSensorData[i].variable = sourceSensorData[i].variable;

					targetSensorData[i].dataArray = new HermesMiddleware.DataAcquisitionServiceServer.dataItem[sourceSensorData[i].dataArray.Length];

					for(int j=0 ; j < targetSensorData[i].dataArray.Length ; j++)
					{
						targetSensorData[i].dataArray[j] = new HermesMiddleware.DataAcquisitionServiceServer.dataItem();
						targetSensorData[i].dataArray[j].value  = sourceSensorData[i].dataArray[j].value;
						targetSensorData[i].dataArray[j].timestamp = sourceSensorData[i].dataArray[j].timestamp;											
					}

				}
			}

			return targetSensorData;
		}

		private string[] _GetPeers(Global global, string sService, string sVariable)
		{
			string[] sPeerAddresses = null;

			if (global.bP2PMaster)
			{
				// this is P2P master => call directly GetPeers 
				sPeerAddresses = global.GetPeers(sService, sVariable);
			}
			else
			{
				// ask P2P master for all peers publishing variable
				try
				{
					// initialize service proxy
					HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService serviceProxy = new HermesMiddleware.DataAcquisitionServiceProxy.DataAcquisitionService();
					serviceProxy.Url = "http://" + global.sP2PMaster + ":" + global.portNumber + global.sVirtRoot + global.sASMX;

					// call service proxy
					sPeerAddresses = serviceProxy.GetPeers(sService, sVariable);
				}
				catch (Exception exp)
				{
					Console.WriteLine(exp);
                }
			}

			return sPeerAddresses;
		}
	}
}
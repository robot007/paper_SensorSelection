using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using HermesMiddleware.CommonLibrary;
using HermesMiddleware.GUILibrary;
using HermesMiddleware.CameraLibrary;
using HermesMiddleware.DataAcquisitionServiceServer;

namespace DataAcquisitionService
{
	/// <summary>
	/// Summary description for Global.
	/// </summary>
	public class Global : RemotableClass
	{
		// internal stuff
		static public string sClassName = "DataAcquisitionGlobal";
		static public int port = 1111;

		// global stuff 

		// reference to main form
		public MainForm mainForm;

		// reference to main service
		public MainService mainService;

		// the web server
		public Cassini.Server webServer;
		public int portNumber = 4444;
		public string sVirtRoot = "/DataAcquisitionService";
		public string sASMX = "/DataAcquisitionService.asmx";

		// P2P stuff
		public string sP2PMaster = "DEMETER";
		public bool bP2PConnected = false;
		public bool bP2PMaster = false;

		// default service name
		public string sDefaultService = "DataAcquisition"; 

		// array of all subscribers
		private Hashtable subscribers = new Hashtable(); 

        


		public Global()
		{
			// 
			// TODO: Add constructor logic here
			//
		}

		public void AddSubscriber(string sCallbackURL, EventSubscription subscription)
		{
			lock (subscribers)
			{
				try
				{
					// create new subscriber
					EventSubscriber subscriber = new EventSubscriber(this, sCallbackURL, subscription);

					// add it to the list
					subscribers.Add(subscription.sGUID, subscriber);

					// store it to the DB (for display purposes)
					DataRow newRow = mainService.tblSubscriptions.NewRow();
					newRow["Variable"] = subscription.sVariable;
					string sCondition;
					Range range = (Range)(subscription.conditions[0]);


					//Null means no limit
					if (range.min == null)
					{
						if (range.max == null)
						{
							sCondition = "upon change";
						}
						else
						{
							sCondition = "< " + range.max.ToString();
						}
					}
					else
					{
						if (range.max == null)
						{
							if (range.bStrict)
							{
								sCondition = "> " + range.min;
							}
							else
							{
								sCondition = ">= " + range.min;							}
						}
						else
						{
							if (range.bStrict)
							{
								sCondition = "Range=(" + range.min.ToString() + ";" + range.max.ToString() + ")";
							}
							else
							{
								sCondition = "Range=<" + range.min.ToString() + ";" + range.max.ToString() + ">";
							}
						}
					}
					newRow["Condition"] = sCondition;
					newRow["GUID"] = subscription.sGUID.ToString();
					newRow["Subscriber URL"] = subscriber.sCallbackURL;
					mainService.tblSubscriptions.Rows.Add(newRow);
					//mainForm.dataGridSubscriptions.ResizeColumns(ColumnResizeType.FitToContent);
				}
				catch(Exception exp)
				{
					Console.WriteLine(exp);
				}
			}
		}

		public void RemoveSubscriber(string sGUID)
		{
			lock (subscribers)
			{
				try
				{
					// remove it from the list
					subscribers.Remove(sGUID);
					
					// remove it from the DB
					DataRow[] rows = mainService.tblSubscriptions.Select("GUID='" + sGUID + "'");
					if (rows != null && rows.Length > 0)
					{
						mainService.tblSubscriptions.Rows.Remove(rows[0]);
					}
				}
				catch(Exception exp)
				{
					Console.WriteLine(exp);
				}
			}
		}

		public void AddEvent2Subscribers(Event myEvent)
		{
			// search subscriber
			foreach(DictionaryEntry entry in subscribers)
			{
				EventSubscriber subscriber = (EventSubscriber)entry.Value;

				if (subscriber.subscription.sVariable == myEvent.sVariable)
				{
					// found subscriber interested in given variable
					bool bNotify = false;
					Event tempEvent = new Event(myEvent);
					ArrayList arrayCallBackValues = new ArrayList();

					for (int i = 0; i < tempEvent.dataArray.Length; i++)
					{
						bNotify = false;

						// check his subscription (AND evaluation)
						foreach(Range range in subscriber.subscription.conditions)
						{

							object oTableValue = tempEvent.dataArray[i].value;
							

							// try to match
							if (range.Match(oTableValue))
							{
								// matched => notify
								bNotify = true;								
							}
							else
							{
								// first umatched => no notification at all (AND relation)
								bNotify = false;
							}							

						}

						if (bNotify)
						{
							// copy event
							arrayCallBackValues.Add(tempEvent.dataArray[i]);
						}
					}

					if( arrayCallBackValues.Count > 0 ){
					
						tempEvent.dataArray = new dataItem[arrayCallBackValues.Count];
						// add event to the subscriber
						for (int k = 0; k < tempEvent.dataArray.Length; k++)
						{
							tempEvent.dataArray[k] = new dataItem();
							tempEvent.dataArray[k] = (HermesMiddleware.DataAcquisitionServiceServer.dataItem)arrayCallBackValues[k];
						}

						subscriber.AddEvent(tempEvent);
											
					}


				}
			}
		}

        public void UpdateDatabase(string sSensorId, string sMoteType, string sVariable, long lTimestamp, object oValue)
        {
            mainService.UpdateDatabase(sSensorId, sMoteType, sVariable, lTimestamp, oValue);
        }

		public void NotifySubscribers()
		{
			lock (subscribers)
			{
				foreach (DictionaryEntry entry in subscribers)
				{
					EventSubscriber subscriber = (EventSubscriber)entry.Value;
					// notify subscriber (if there are collected events)
					subscriber.Notify();
				}
			}
		}

		public void Publish(string sService, string[] sVariables, string sHostName)
		{
				try
				{
					if (sVariables != null)
					{
						foreach(string sVariable in sVariables)
						{
							DataRow[] rows;

							lock (mainService.tblPublishedVariables)
							{
								// check if variable from the same peer and service exists
								 rows = mainService.tblPublishedVariables.Select("Variable='" + sVariable + "' AND Service='" + sService + "' AND Peer='" + sHostName + "'");
							}
							if (rows == null || rows.Length == 0)
							{
								// store it to the DB
								DataRow newRow = mainService.tblPublishedVariables.NewRow();
								newRow["Service"] = sService;
								newRow["Variable"] = sVariable;
								newRow["Peer"] = sHostName;
								newRow["Published On"] = DateTime.Now;
								mainForm.VariableRowAdded(newRow);
							}
						}
					}
				}
				catch (Exception exp)
				{
					Console.WriteLine(exp);
				}
			
		}

		public  string[] GetPeers(string sService, string sVariable)
		{
			string[] sPeers = null;

			lock (this)
			{
				try
				{
					if (sVariable == null || sVariable == "")
					{
						// no variable specified => get all peers publishing given service
						DataRow[] rows = mainService.tblPeers.Select("Service='" + sService + "'");
						if (rows != null && rows.Length > 0)
						{
							sPeers = new string[rows.Length];
							for (int i = 0; i < rows.Length; i++)
							{
								sPeers[i] = rows[i]["Name"].ToString();
							}
						}
					}
					else
					{
						// find 
						DataRow[] rows = mainService.tblPublishedVariables.Select("Service='" + sService + "' AND Variable='" + sVariable + "'");
						if (rows != null && rows.Length > 0)
						{
							sPeers = new string[rows.Length];

							for (int i = 0; i < rows.Length; i++)
							{
								sPeers[i] = rows[i]["Peer"].ToString();
							}
						}
					}
				}
				catch (Exception exp)
				{
					Console.WriteLine(exp);
				}
			}

			return sPeers;
		}

		public void RegisterPeer(string sHostName, string sService)
		{
			lock (this)
			{
				try
				{
					// just register as a peer
					DataRow newRow = mainService.tblPeers.NewRow();
					newRow["Name"] = sHostName;
					newRow["IP Address"] = DNSService.GetIPAddress(sHostName);
					newRow["Service"] = sService;
					newRow["Connected On"] = DateTime.Now;
					mainService.tblPeers.Rows.Add(newRow);

				}
				catch (Exception exp)
				{
					Console.WriteLine(exp);
				}
			}
		}

		public void UnregisterPeer(string sHostName)
		{
			lock (this)
			{
				try
				{
					// try to find this peer
					DataRow[] rows = mainService.tblPeers.Select("Name='" + sHostName + "'");

					if (rows != null && rows.Length > 0)
					{
						// delete row
						mainService.tblPeers.Rows.Remove(rows[0]);
					}

					// delete also all published variables
					rows = mainService.tblPublishedVariables.Select("Peer='" + sHostName + "'");
					if (rows != null)
					{
						for (int i = 0; i < rows.Length; i++)
						{
							// delete row
							mainForm.VariableRowDeleted(rows[i]);
						}
					}

				}
				catch (Exception exp)
				{
					Console.WriteLine(exp);
				}
			}
		}

		public void Query(string service, ExpressionType[] expressions, ref ArrayList sensorDataArray)
		{
			SortedList<string, List<SensorData>> results = new SortedList<string, List<SensorData>>();

			lock (mainService.tblArchive)
			{

				foreach (DataRow row in mainService.tblArchive.Rows)
				{
					string variable = row["Variable"].ToString();
					string value = row["Value"].ToString();
					string dummyCondition = "";
					object tableValue = mainService.ReflectiveParseWithType(value, row["Type"].ToString());

					foreach (ExpressionType expressionType in expressions)
					{
						// get expression
						Expression expression = expressionType.Items[0];

						// get variable names
						string expectedVariable = expression.variable;

						if ((variable != expectedVariable) && (expectedVariable != "x") && (expectedVariable != "y") && (expectedVariable != "z"))
						{
							break;
						}

						if ((expectedVariable == "x") || (expectedVariable == "y") || (expectedVariable == "z"))
						{
							foreach (DataRow dataRow in mainService.tblPositions.Rows)
							{

								double x_value = Double.Parse((string)dataRow["x"]);
								double y_value = Double.Parse((string)dataRow["y"]);
								double z_value = Double.Parse((string)dataRow["z"]);


								object x_tableValue = mainService.ReflectiveParseWithType(dataRow["x"].ToString(), dataRow["Type"].ToString());
								object y_tableValue = mainService.ReflectiveParseWithType(dataRow["y"].ToString(), dataRow["Type"].ToString());
								object z_tableValue = mainService.ReflectiveParseWithType(dataRow["z"].ToString(), dataRow["Type"].ToString());

								Condition condition_x = DecodeExpression(expressionType.Items[0], out dummyCondition);
								Condition condition_y = DecodeExpression(expressionType.Items[1], out dummyCondition);
								Condition condition_z = DecodeExpression(expressionType.Items[2], out dummyCondition);

								if ((condition_x.Match(x_tableValue)) && (condition_y.Match(y_tableValue)) && (condition_z.Match(z_tableValue)))
								{
									if (!results.Keys.Contains(dataRow["SensorID"].ToString()))
									{
										results.Add(dataRow["SensorID"].ToString(), new List<SensorData>());


										SensorData positionData = new SensorData();
										positionData.baseStation = Environment.MachineName;
										positionData.sensorID = (string)dataRow["SensorID"];
										positionData.variable = "Position";
										positionData.dataArray = new dataItem[3];

										positionData.dataArray[0] = new dataItem();
										positionData.dataArray[0].timestamp = dataRow["Time"].ToString();
										positionData.dataArray[0].value = x_tableValue;
										positionData.dataArray[1] = new dataItem();
										positionData.dataArray[1].timestamp = dataRow["Time"].ToString();
										positionData.dataArray[1].value = y_tableValue;
										positionData.dataArray[2] = new dataItem();
										positionData.dataArray[2].timestamp = dataRow["Time"].ToString();
										positionData.dataArray[2].value = z_tableValue;

										results[dataRow["SensorID"].ToString()].Add(positionData);
									}

								}

							}

						}

						if ((variable == expectedVariable) && (expectedVariable != "x") && (expectedVariable != "y") && (expectedVariable != "z"))
						{

							// decode expression
							Condition condition = DecodeExpression(expression, out dummyCondition);

							if (condition.Match(tableValue))
							{
								if (!results.Keys.Contains(row["SensorID"].ToString()))
								{
									results.Add(row["SensorID"].ToString(), new List<SensorData>());
								}
								
								SensorData newData = new SensorData();
								newData.baseStation = Environment.MachineName;
								newData.sensorID = (string)row["SensorID"];
								newData.variable = variable;
								newData.dataArray = new dataItem[10];
								newData.dataArray[0] = new dataItem();
								newData.dataArray[0].timestamp = row["Time"].ToString();
								newData.dataArray[0].value = tableValue;

								results[row["SensorID"].ToString()].Add(newData);
							}

						}
						else
						{
							if (results.ContainsKey(row["SensorID"].ToString()))
							{
								// results.Remove(row["SensorID"].ToString());
								//results.Remove(row["ID"].ToString());

							}
							break;
						}
					}

				}
				if (results.Count == 0)
				{
					return;
				}


				IList<string> IDs = results.Keys;

				foreach (string ids in IDs)
				{
					SensorData[] sensorData = results[ids].ToArray();
					for (int i = 0; i < sensorData.Length; i++)
					{
						SensorData newData = new SensorData();
						newData.sensorID = ids;
						newData.baseStation = sensorData[i].baseStation;
						newData.variable = sensorData[i].variable;
						newData.dataArray = sensorData[i].dataArray;
						sensorDataArray.Add(newData);
					}

				}

			}

		}
		private Condition DecodeExpression(Expression expression, out string sCondition)
		{
			Condition condition = null;
			Range range = null;
			Threshold threshold = null;
			Equals equals = null;
			sCondition = "";

			switch (expression.GetType().Name)
			{
				case "Range":
					range = new Range((HermesMiddleware.DataAcquisitionServiceServer.Range)expression);
					condition = range;
					if (range.bStrict)
					{
						sCondition = "Range=(" + range.min.ToString() + ";" + range.max.ToString() + ")";
					}
					else
					{
						sCondition = "Range=<" + range.min.ToString() + ";" + range.max.ToString() + ">";
					}
					break;
				case "Threshold":
					threshold = new Threshold((HermesMiddleware.DataAcquisitionServiceServer.Threshold)expression);
					condition = threshold;
					if (threshold.bStrict)
					{
						if (threshold.oValue == null) {

							sCondition = "Any";
						}
						else if (threshold.type == HermesMiddleware.DataAcquisitionServiceServer.ThresholdType.above)
						{
							sCondition = "> " + threshold.oValue.ToString();
						}
						else
						{
							sCondition = "< " + threshold.oValue.ToString();
						}
					}
					else
					{
						if (threshold.oValue == null)
						{

							sCondition = "Any";
						}
						else if (threshold.type == HermesMiddleware.DataAcquisitionServiceServer.ThresholdType.above)
						{
							sCondition = ">= " + threshold.oValue.ToString();
						}
						else
						{
							sCondition = "<= " + threshold.oValue.ToString();
						}
					}
					break;
				case "Equals":
					equals = new Equals((HermesMiddleware.DataAcquisitionServiceServer.Equals)expression);
					condition = equals;
					sCondition = "= " + equals.oValue;
					break;
			}

			return condition;
		}
	}
}

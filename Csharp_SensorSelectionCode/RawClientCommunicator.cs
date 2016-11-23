using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace DataAcquisitionService
{
	class RawClientCommunicator
	{
		private string ipaddress;
		private Int32 port;
		private TcpClient myClient;
		private Stream clientStream;
		private System.Xml.Serialization.XmlSerializer serializer;


		public RawClientCommunicator( string ipaddress, Int32 port ) {

			serializer = new System.Xml.Serialization.XmlSerializer(typeof(Event));

			this.ipaddress = ipaddress;
			this.port = port;			

			try{

				myClient = new TcpClient(ipaddress, port);
				clientStream = myClient.GetStream();

			}
			catch( Exception e ){

				Console.WriteLine(e);
			}
			
		}

		public RawClientCommunicator( string address ) {

			int semicolonLoc = address.IndexOf(":");

			this.ipaddress = address.Substring(0, semicolonLoc);
			this.port = Int32.Parse( address.Substring(semicolonLoc + 1, address.Length - semicolonLoc-1 ));

			serializer = new System.Xml.Serialization.XmlSerializer(typeof(Event));

			try
			{

				myClient = new TcpClient(ipaddress, port);
				clientStream = myClient.GetStream();

			}
			catch (Exception e)
			{

				Console.WriteLine(e);
			}
		
		}

		public void SendEvent( Event dasEvent ) {

			try
			{
				serializer.Serialize(clientStream, dasEvent);
				clientStream.Flush();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
				
		}

		//This function takes and event and sends it by serialiazing it.
		public void SendEvent( HermesMiddleware.DataAcquisitionServiceProxy.Event myEvent ){

			SendEvent(new Event(myEvent));
		
		}

		public void Close() {

			clientStream.Close();
			myClient.Close();
		}

	}
}

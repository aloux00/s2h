/*
    Midori's HTTP Tunnel - Socks Over HTTP - Client
    Copyright (C) 2005  Giuseppe amato (paipai at tiscali dot it)

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/
using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Web;
using System.Web.Services;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace SocksOverHttp
{
	/// <summary>
	/// Summary description for Service1.
	/// </summary>
	/// 

	[WebService(Namespace="http://www.paipai.net/")]
	public class s2h : System.Web.Services.WebService
	{

		public struct ExData
		{
			public string token;
			public byte[] data;
		}

		class ExData2
		{
			public string token = "";
			public byte[] data = null;
			public int len = 0;
			public bool closed = false;
		}

		public enum s2h_errors{
			s2h_ok = 0,
			s2h_error = 1,
			s2h_client_not_authenticated = 2,
			s2h_host_unknown = 3,
			s2h_send_error = 4,
			s2h_recv_error = 5,
			s2h_unknown_connection_token = 6,
			s2h_connection_timeout = 7,
			s2h_connection_refused = 8,
			s2h_network_error = 9,
			s2h_invalid_credentials = 10,
			s2h_not_exists = 11,
			s2h_not_implemented = 12,

		};
		public struct encryption_info
		{
			public bool Enable;						// true to disable encription; all other fields should be ignored
			public int algo_id;
			public byte[] encrypted_key;		// session key encrypted using server certificate
		};

		ApplicationStorage m_config;
		UserManager m_userManager;
		public s2h()
		{
			//CODEGEN: This call is required by the ASP.NET Web Services Designer
			InitializeComponent();
			m_config = (ApplicationStorage)Application["s2hConfig"];
			m_userManager = new UserManager((string)Application["s2hUsersPath"]);
		}

		#region Component Designer generated code
		
		//Required by the Web Services Designer 
		private IContainer components = null;
				
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if(disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);		
		}
		
		#endregion


		[WebMethod]
		public long Auth(string usr, string pwd, out string auth)
		{
			S2hState s2hState = (S2hState)Application["s2hState"];
			s2hClient Client = new s2hClient();
			auth = null;

			UserManager.user_info user = m_userManager.GetUser(usr);
			if (user == null || pwd != user.pass)	
				return (long)s2h_errors.s2h_invalid_credentials;

			auth = Client.AuthToken = s2hState.GetNewAuthToken();
			Client.ClientAddress = IPAddress.Parse(this.Context.Request.UserHostAddress);
			s2hState.PutClient(auth, Client);

			return (long)s2h_errors.s2h_ok;
		}
		
		[WebMethod]
		public long GetServerCertificate(out byte[] certificate)
		{
			certificate = null;
			string certPath = m_config.strGet("certificate_path");
			if (!File.Exists(certPath))
				return (long)s2h_errors.s2h_not_exists;
			try
			{
				FileStream fs = File.OpenRead(certPath);
				certificate = new byte[fs.Length];
				fs.Read(certificate, 0, certificate.Length);
			}
			catch (Exception)
			{
				return (long)s2h_errors.s2h_not_exists;
			}
			return (long)s2h_errors.s2h_ok;
		}

		// Enable/disable or reset the encryption
		[WebMethod]
		public long NegotiateEncryption(string auth, encryption_info info)
		{
			return (long)s2h_errors.s2h_not_implemented;
		}

		[WebMethod]
		public long ConnFast(string auth, string host, int port, string Token, out byte[] iplAddr, out int lPort, out byte[] iprAddr, out int rPort)
		{
			iprAddr = iplAddr = new byte[4];
			lPort = 0;
			rPort = port;
			S2hState s2hState = (S2hState)Application["s2hState"];
			s2hClient Client = s2hState.GetClient(auth);
			if (Client == null)
				return (long)s2h_errors.s2h_client_not_authenticated;
			s2hConnection connection = null;
			s2h_errors error = s2hState.Exchanger.CreateConnectionFast(host, port, Token, out connection);
			if (error != s2h_errors.s2h_ok)
				return (long) error;
			
			if (connection.socket.LocalEndPoint is IPEndPoint)
			{
				IPEndPoint ip = (IPEndPoint) connection.socket.LocalEndPoint;
				iplAddr = ip.Address.GetAddressBytes();
				lPort = ip.Port;
			}
			if (connection.socket.RemoteEndPoint is IPEndPoint)
			{
				IPEndPoint ip = (IPEndPoint) connection.socket.RemoteEndPoint;
				iprAddr = ip.Address.GetAddressBytes();
				rPort = ip.Port;
			}
			Client.AddConnection(Token, connection);
			connection.bReady = true;
			return (long) s2h_errors.s2h_ok;
		}

		[WebMethod]
		public long Conn(string auth, string host, int port, out string Token, out byte[] iplAddr, out int lPort, out byte[] iprAddr, out int rPort)
		{
			iprAddr = iplAddr = new byte[4];
			lPort = 0;
			rPort = port;
			Token = "";
			S2hState s2hState = (S2hState)Application["s2hState"];
			s2hClient Client = s2hState.GetClient(auth);
			if (Client == null)
				return (long)s2h_errors.s2h_client_not_authenticated;
			s2hConnection connection = null;

			s2h_errors error = s2hState.Exchanger.CreateConnection(host, port, out connection);
			if (error != s2h_errors.s2h_ok)
				return (long) error;
			Client.AddConnection(connection.handle, connection);
			
			Token = connection.handle;
			if (connection.socket.LocalEndPoint is IPEndPoint)
			{
				IPEndPoint ip = (IPEndPoint) connection.socket.LocalEndPoint;
				iplAddr = ip.Address.GetAddressBytes();
				lPort = ip.Port;
			}
			if (connection.socket.RemoteEndPoint is IPEndPoint)
			{
				IPEndPoint ip = (IPEndPoint) connection.socket.RemoteEndPoint;
				iprAddr = ip.Address.GetAddressBytes();
				rPort = ip.Port;
			}
			return (long) s2h_errors.s2h_ok;
		}	

		[WebMethod]
		public long Ready(string auth, string Token)
		{
			S2hState s2hState = (S2hState)Application["s2hState"];
			s2hClient Client = s2hState.GetClient(auth);
			if (Client == null)
				return (long)s2h_errors.s2h_client_not_authenticated;
			
			s2hConnection connection = Client.GetConnection(Token);
			if (connection == null)
				return (long) s2h_errors.s2h_unknown_connection_token;
			
			connection.bReady = true;
			
			return (long) s2h_errors.s2h_ok;
		}

		[WebMethod]
		public long Close(string auth, string[] Tokens)
		{
			long lRes = (long) s2h_errors.s2h_ok;
			S2hState s2hState = (S2hState)Application["s2hState"];
			s2hClient Client = s2hState.GetClient(auth);
			if (Client == null)
				return (long)s2h_errors.s2h_client_not_authenticated;
			
			foreach(string t in Tokens)
			{
				s2hConnection connection = Client.GetConnection(t);
				if (connection == null)
					lRes = (long) s2h_errors.s2h_unknown_connection_token;
				s2hState.Exchanger.CloseConnection(t);
				Client.RemoveConnection(connection.handle);
			}
			return lRes;
		}

		[WebMethod]
		public long CloseAll(string auth)
		{
			S2hState s2hState = (S2hState)Application["s2hState"];
			s2hClient Client = s2hState.GetClient(auth);
			if (Client == null)
				return (long)s2h_errors.s2h_client_not_authenticated;
			Client = null;
			s2hState.RemoveClient(auth);
			return (long) s2h_errors.s2h_ok;
		}

		[WebMethod]
		public long Send(string a, ExData[] d, int Seq)
		{
			System.Diagnostics.Debug.WriteLine(this.ToString() + ": sent seq=" + Seq.ToString());
			S2hState s2hState = (S2hState)Application["s2hState"];
			s2hClient Client = s2hState.GetClient(a);

			if (Client == null)
				return (long)s2h_errors.s2h_client_not_authenticated;

			if (Seq == Client.LastSendSequenceId)
				return (long) s2h_errors.s2h_ok; // already processed data

			// just send recv data to the data exchanger
			s2hState.Exchanger.EnqueueData(d);

			return (long)s2h_errors.s2h_ok;
		}

		[WebMethod]
		public long Recv(string a, int Seq, int WaitTimeout, out ExData[] d)
		{
			System.Diagnostics.Debug.WriteLine(this.ToString() + ": requested seq=" + Seq.ToString());
			d = null;
			int tmpWaitTimeout = WaitTimeout;
			S2hState s2hState = (S2hState)Application["s2hState"];
			s2hClient Client = s2hState.GetClient(a);

			// auth error
			if (Client == null)
				return (long)s2h_errors.s2h_client_not_authenticated;

			lock (Client)
			{
				// the client wants the last packet; last communication failed?
				if (Seq == Client.LastRecvSequenceId)
				{
					d = Client.LastRecvData;
					return (long) s2h_errors.s2h_ok; // sending last recieved data
				}
			}

			// try to recieve data on all active connections
			while (true)
			{
				s2hState.Exchanger.DeQueueData(out d);

				if ((d == null || d.Length == 0) && tmpWaitTimeout > 0)	
				{
					System.Threading.Thread.Sleep(tmpWaitTimeout<100?tmpWaitTimeout:100);
					tmpWaitTimeout -= 100;
				}
				else
					break;
			}


			// remember the last sent packet
			lock(Client)
			{
				// NOTE: store current value, also if the packet is null
				Client.LastRecvData = d;
				Client.LastRecvSequenceId = Seq;
			}
			return (long)s2h_errors.s2h_ok;
		}

	}
}

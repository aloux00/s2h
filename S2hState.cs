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
using System.ComponentModel;
using System.Web;
using System.Web.SessionState;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Security.Cryptography;

namespace SocksOverHttp
{
	[Synchronization()]
	public class S2hState: IDisposable
	{
		s2hDataExchanger m_DataExchanger = new s2hDataExchanger();
		private bool m_disposed = false;
		//private SHA1 m_sha = new SHA1CryptoServiceProvider(); 
		Random m_rng = new Random();
		//private long m_AuthCounter = 1;
		System.Collections.Specialized.HybridDictionary m_Clients = new HybridDictionary();
		public S2hState()
		{
			m_DataExchanger.Initialize();
			m_DataExchanger.WaitUntilRunning(Timeout.Infinite);
		}
		~S2hState()      
		{
			Dispose(false);
		}

		public System.Collections.Specialized.HybridDictionary Clients
		{
			get{return m_Clients;}
		}

		public virtual void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
			
		}
		private void Dispose(bool disposing)
		{
			if(!m_disposed)
			{
				if(disposing)
				{
					m_DataExchanger.Terminate();
				}   
			}
			m_disposed = true;
		}
		
		public s2hDataExchanger Exchanger
		{
			get {return m_DataExchanger;}
		}

		public void PutClient(string auth, s2hClient Client)
		{
			lock(m_Clients)
			{
				m_Clients.Add(auth, Client);
			}
		}

		public void RemoveClient(string auth)
		{
			s2hClient Client = GetClient(auth);
			if (Client != null)
			{
				lock(m_Clients)
				{
					string [] connectionHandles = Client.ConnectionHandles;
					foreach(string s in connectionHandles)
						m_DataExchanger.CloseConnection(s);
					Client.CloseAllConnections();
					m_Clients.Remove(auth);
				}
			}
		}
		
		public void RemoveAllClients()
		{
			lock(m_Clients)
			{
				foreach (s2hClient client in m_Clients)
				{
					client.CloseAllConnections();
					m_Clients.Remove(client.AuthToken);
				}
			}
		}
		public s2hClient GetClient(string auth)
		{
			lock (m_Clients)
			{
				if (m_Clients.Contains(auth))
				{
					s2hClient client = (s2hClient) m_Clients[auth];
					long nowIs = DateTime.Now.ToFileTime();
					if ((( nowIs - client.LastSeen) / (10*1000000)) > 60*5)
					{
						client.CloseAllConnections();
						m_Clients.Remove(client.AuthToken);
						return null;
					}
					client.ResetLastSeen();
					return client;
				}
				else 
					return null;
			}
		}

		public string GetNewAuthToken()
		{
			string a;
			byte[] b = new byte[12];
			lock (m_Clients)
			{
				do
				{
					m_rng.NextBytes(b);
					a = Convert.ToBase64String(b);
				}while(m_Clients.Contains(a));
			}
			return a;
		}
	};
}

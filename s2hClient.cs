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

namespace SocksOverHttp 
{
	[Synchronization()]
	public class s2hClient
	{
		System.DateTime m_lastSeen = System.DateTime.Now;

		string m_AuthToken = "";
		long m_LastSendSequenceId = -1;

		long m_LastRecvSequenceId = -1;
		s2h.ExData [] m_LastRecvData;

		System.Net.IPAddress m_ClientAddress = IPAddress.Any;
		HybridDictionary m_Connections = new HybridDictionary();

		public long LastSeen
		{
			get{return m_lastSeen.ToFileTime();}
		}
		public void ResetLastSeen()
		{
			m_lastSeen = System.DateTime.Now;
		}

		public s2hClient()
		{
		}
		
		public long ConnectionCount
		{
			get
			{
				lock(m_Connections)
				{
					return m_Connections.Count;
				}
			}
		}
		public System.Net.IPAddress ClientAddress
		{
			get 
			{
				return m_ClientAddress;
			}
			set
			{
				m_ClientAddress = value;
			}
		}
		public string AuthToken
		{
			get 
			{
				return m_AuthToken;
			}
			set
			{
				m_AuthToken = value;
			}
		}

		public long LastSendSequenceId
		{
			get 
			{
				return m_LastSendSequenceId;
			}
			set
			{
				m_LastSendSequenceId = value;
			}
		}

		public long LastRecvSequenceId
		{
			get 
			{
				return m_LastRecvSequenceId;
			}
			set
			{
				m_LastRecvSequenceId = value;
			}
		}
	
		public s2h.ExData [] LastRecvData
		{
			get 
			{
				return m_LastRecvData;
			}
			set
			{
				m_LastRecvData = value;
			}
		}

		public s2hConnection GetConnection(string handle)
		{
			lock(m_Connections)
			{
				if (m_Connections.Contains(handle))
					return (s2hConnection)m_Connections[handle];
				else
					return null;
			}
		}

		public void AddConnection(string handle, s2hConnection conn)
		{
			lock(m_Connections)
			{
				lock(conn)
					m_Connections.Add(handle, conn);
			}
		}

		public void RemoveConnection(string handle)
		{
			lock(m_Connections)
			{
				if  (m_Connections.Contains(handle))
					m_Connections.Remove(handle);
			}
		}

		public string[] ConnectionHandles
		{
			get
			{
				string [] outArray = null;
				lock(m_Connections)
				{
					long i = 0;
					outArray = new string[m_Connections.Keys.Count];
					foreach(string s in m_Connections.Keys)
						outArray[i++] = s;
				}
				return outArray;
			}
		}

		public void CloseAllConnections()
		{
			lock(m_Connections)
			{
				foreach(s2hConnection conn in m_Connections.Values)
				{
					try
					{
						lock(conn)
							conn.Close();
					}
					catch(Exception){}
					
				}
				m_Connections.Clear();
			}
		}
		
	};
};
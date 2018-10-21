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
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Contexts;
using System.Threading;

namespace SocksOverHttp
{

	public class s2hConnectionData
	{
		public s2hConnectionData(byte[] ndata)
		{
			data = ndata;
			index = 0;
		}
		public s2hConnectionData(byte[] ndata, int nindex)
		{
			data = ndata;
			index = nindex;
		}
		public byte[] data;
		public int index;
	}

	/// <summary>
	/// This object manage a thread that send and receive data on the remote sockets.
	/// </summary>

	[Synchronization()]
	public class s2hDataExchanger
	{
		bool m_bRunning = false;
		System.Collections.Specialized.HybridDictionary m_Connections = new HybridDictionary(128);
		Thread m_LocalSendRecvThread;
		int m_iWsTransmitBufferLen = 1024* 64;
		int m_iMaxRecvDataQueueLen = 128;

		public System.Collections.Specialized.HybridDictionary Connections
		{
			get {return m_Connections;}
		}
		public s2hDataExchanger()
		{
		}

		public bool Running 
		{
			get{return m_bRunning;}
		}

		public s2h.s2h_errors Initialize()
		{
			s2h.s2h_errors iRes = s2h.s2h_errors.s2h_ok;

			if (m_bRunning)
				Terminate();

			m_LocalSendRecvThread = new Thread(new ThreadStart(SendReceiveLocalSocketsLoop));
			m_LocalSendRecvThread.Start();
			m_bRunning = true;
			return iRes;
		}
		
		public void Terminate()
		{
			try
			{
				if (m_LocalSendRecvThread != null)
				{
					m_LocalSendRecvThread.Interrupt();
					if (!m_LocalSendRecvThread.Join(500))
						m_LocalSendRecvThread.Abort();

				}
			}
			catch(Exception){}

			this.CloseAllConnections();
		
			m_bRunning = false;

		}

		public bool WaitUntilRunning(int timeout)
		{
			bool bForever = timeout == Timeout.Infinite;
			if (m_bRunning)
				return true;
			while(timeout >0 || bForever)
			{
				Thread.Sleep(50);
				timeout -= 50;
				if (m_bRunning)
					return true;
			}
			return m_bRunning;
		}

		public void CloseAllConnections()
		{
			lock(m_Connections)
			{
				// close all pending connections;
				foreach (s2hConnection conn in m_Connections.Values)
				{
					try
					{
						conn.Close();
					}
					catch(Exception){}
				}
				m_Connections.Clear();
			}	
		}

		public s2h.s2h_errors CreateConnection(string host, int port,
			out s2hConnection conn)
		{
			SocksOverHttp.s2h.s2h_errors iRes;
			Socket socket = null;
			conn = new s2hConnection();
			iRes = conn.Connect(host, port, out socket);
			if (iRes != s2h.s2h_errors.s2h_ok)
			{
				try
				{
					conn.Close();
				}
				catch(Exception){}
				conn = null;
			}
			lock(m_Connections)
			{
				m_Connections.Add(conn.handle, conn);
			}
			return iRes;
		}

		public s2h.s2h_errors CreateConnectionFast(string host, int port, string handle,
			out s2hConnection conn)
		{
			SocksOverHttp.s2h.s2h_errors iRes;
			Socket socket = null;
			conn = new s2hConnection();
			iRes = conn.Connect(host, port, out socket);
			if (iRes != s2h.s2h_errors.s2h_ok)
			{
				try
				{
					conn.Close();
				}
				catch(Exception){}
				conn = null;
			}
			lock(m_Connections)
			{
				conn.handle = handle;
				m_Connections.Add(conn.handle, conn);
			}
			return iRes;
		}

		public bool CloseConnection(string handle)
		{
			lock(m_Connections)
			{
				//System.Diagnostics.Debug.WriteLine("Connections before:" + m_Connections.Count.ToString());
				if (m_Connections.Contains(handle))
				{
					s2hConnection conn = (s2hConnection)m_Connections[handle];
					m_Connections.Remove(handle);
					try
					{
						conn.Close();
					}
					catch(Exception){}
					//System.Diagnostics.Debug.WriteLine("Connections after:" + m_Connections.Count.ToString());	
					return true;
				}
			}
			return false;
		}		

		public void EnqueueData(s2h.ExData[] exData)
		{
			// put all received data on the connections queue
			// Problem: the client continue to push data also if the socket if full...
			// This is not a big problem until the connection from client to web server is slower than
			// the connection from web server to remote host
			lock(m_Connections)
			{
				s2hConnection conn = null;
				foreach (s2h.ExData d in exData)
				{
					if (d.token == null)
					{
						System.Diagnostics.Debug.WriteLine(
										"*********** null Token detected in received data");
						continue;
					}

					if (m_Connections.Contains(d.token))
					{
						conn = (s2hConnection) m_Connections[d.token];
						conn.DataToSendQueue.Enqueue( new s2hConnectionData(d.data, 0) );
					}
					else
						System.Diagnostics.Debug.WriteLine(this.ToString()+ ": Connection token " + d.token + " unknown");
				}
			}
		}

		public void DeQueueData(out s2h.ExData[] exData)
		{
			// remove all received data from the queues and prepare it for the client
			ArrayList l = new ArrayList(128);
			ArrayList toRemove = new ArrayList(16);
			exData = null;
			lock(m_Connections)
			{
				foreach (s2hConnection conn in m_Connections.Values)
				{
					int iCount = 0;
					lock(conn)
					{
						// NOTE: m_iWsTransmitBufferLen is a "SOFT" limit; that is, when the sum of
						// recv packets size exceed m_iWsTransmitBufferLen, the packet is sent.
						while(conn.DataRecievedQueue.Count > 0 && iCount < m_iWsTransmitBufferLen)
						{
							s2hConnectionData d =(s2hConnectionData)conn.DataRecievedQueue.Dequeue();
							if (d == null)
								continue;
							s2h.ExData exd = new s2h.ExData();
							exd.token = conn.handle;
							exd.data = d.data;
							l.Add(exd);
							if (d.data == null) 
							{
								// a null packet in the queue 
								// means connection closed by remote peer
								toRemove.Add(conn);
								break;
							}
							iCount += d.data.Length;
						}
					}
				}
				if (toRemove.Count >0)
				{
					foreach(s2hConnection s in toRemove)
						m_Connections.Remove(s);
					toRemove.Clear();
				}
			}


			if (l.Count > 0)
			{
				exData = (s2h.ExData[])l.ToArray(typeof(s2h.ExData));
			}
		}
		

		public void SendReceiveLocalSocketsLoop()
		{
			ArrayList toRemove = new ArrayList(128);
			int iSentData = 0;
			int iRecvData = 0;
			int iDataToSend = 0;
			bool bSomethingDone = false;
			byte [] recvData = new byte[1024*4];
			try
			{
				for (;;)
				{
					bSomethingDone = false;
					lock(m_Connections)
					{
						foreach(s2hConnection conn in m_Connections.Values)
						{

							lock(conn)
							{
								// a connection not yet set ready by the client should be ignored
								if (!conn.bReady)
									continue;
								if (!conn.Connected && !conn.NullPacketSent)
								{
									// connection lost: push a null packet on the queue
									// to notify the WS Client
									conn.DataRecievedQueue.Enqueue(new s2hConnectionData(null,  0));
									conn.NullPacketSent = true;
									continue;
								}

								/////////////////////////////////////////////
								// send the pending data to the local socket
								/////////////////////////////////////////////
								if (conn.CanSend && conn.DataToSendQueue.Count > 0)
								{
									bSomethingDone = true;
									// peek first packet
									s2hConnectionData buf = 
										(s2hConnectionData) conn.DataToSendQueue.Peek();
									if (buf.data == null)
									{
										// null packet: this means that the connection 
										// has been closed on the WS Client side
										conn.Close();
										toRemove.Add(conn.handle);
									}
									else
									{
										iDataToSend = buf.data.Length - buf.index;
										iSentData = conn.SendSimple(buf.data, buf.index, iDataToSend);
										if (iSentData <  iDataToSend)
											buf.index += iSentData; // partial data was sent
										else
											conn.DataToSendQueue.Dequeue(); // full data was sent
									}
								}
						
								/////////////////////////////////////////////
								// buffer the data received from local socket
								// NOTE: max of m_iMaxRecvDataQueueLen packets
								// are enqueued
								/////////////////////////////////////////////
								if (conn.CanRecv && conn.DataRecievedQueue.Count < m_iMaxRecvDataQueueLen)
								{
									bSomethingDone = true;
									iRecvData = conn.RecvSimple(recvData, 0, recvData.Length);
									if (iRecvData == 0)
									{
										// connection was closed by remote peer
										// push a null packet on the queue
										// to notify the WS Client
										conn.Close();
										conn.DataRecievedQueue.Enqueue(new s2hConnectionData(null,  0));
									}
									else
									{
										byte[] data = new byte[iRecvData];
										Array.Copy(recvData, 0, data, 0, iRecvData);
										s2hConnectionData buf = new s2hConnectionData(data,  0);
										conn.DataRecievedQueue.Enqueue(buf);
									}
								}
							
							}
						}
						// purge the closed connections
						foreach(string s in toRemove)
							m_Connections.Remove(s);
						toRemove.Clear();
					
					}

					// sleep in case of inactivity; avoid 100% CPU
					// and allow context switch to other WebServices Recv/Send threads
					if (!bSomethingDone)
						Thread.Sleep(20); 
				}
			}
			catch(ThreadInterruptedException)
			{
				return;
			}
			catch(ThreadAbortException)
			{
				Thread.ResetAbort();
				return;
			}
		}
	}
}




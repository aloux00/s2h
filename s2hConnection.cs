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
	public class s2hConnection
	{
		bool m_bNullPacketSent = false;
		public const int WSAEWOULDBLOCK = 10035;
		Queue m_DataToSendQueue = Queue.Synchronized(new Queue(128)); // contains data received from the ws-client and to send to remote host
		Queue m_DataRecievedQueue = Queue.Synchronized(new Queue(128)); // contains data received from remote host and to send to the ws-client

		bool m_bClosed = false;
		string m_handle;			// the client recieve it as reference to this connection
		Socket m_socket;			// 
		bool m_bReady = false;		// receive data only if true
		int m_iRecvBuffer = 1024*16;
		int m_iSendBuffer = 1024*256;
		byte[] m_recvBuffer;
		UInt64 m_uiSentData = 0;
		UInt64 m_uiRecvData = 0;

		public bool NullPacketSent
		{
			get {return m_bNullPacketSent;}
			set {m_bNullPacketSent = value;}
		}

		public s2hConnection()
		{
			m_recvBuffer = new byte[m_iRecvBuffer];
		}
		public s2hConnection(int iRecvBuffer, int iSendBuffer)
		{
			m_iSendBuffer = iSendBuffer;
			m_iRecvBuffer = iRecvBuffer;
			m_recvBuffer = new byte[m_iRecvBuffer];
		}



		public Queue DataToSendQueue
		{
			get{return m_DataToSendQueue;}
		}

		public Queue DataRecievedQueue
		{
			get{return m_DataRecievedQueue;}
		}

		public string handle
		{
			get 
			{
				return m_handle;
			}
			set
			{
				m_handle = value;
			}
		}

		public Socket socket
		{
			get 
			{
				return m_socket;
			}
		}

		public bool Connected
		{
			get
			{
				try
				{
					lock(this)
						return !m_bClosed && !InError && m_socket.Connected;
				}
				catch(Exception){return false;}
			}
		}

		public bool CanSend
		{
			get
			{
				try
				{
					lock(this)
						return m_socket.Poll(1000, SelectMode.SelectWrite);
				}
				catch(Exception) {return false;}
			}
		}

		public bool CanRecv
		{
			get
			{
				try{
				lock(this)
					return m_socket.Poll(1000, SelectMode.SelectRead);
				}
				catch(SocketException) {return true;}
				catch(Exception){return false;}
			}
		}

		public bool InError
		{
			get
			{
				try
				{
					lock(this)
						return m_socket.Poll(1000, SelectMode.SelectError);
				}
				catch(Exception) {return true;}
			}
		}

		public bool bReady
		{
			get 
			{
				return m_bReady;
			}
			set
			{
				m_bReady = value;
			}
		}

		public SocksOverHttp.s2h.s2h_errors Connect(string host, int port, out Socket socket)
		{
			socket = null;
			lock(this)
			{
				try
				{
					TcpClientEx tcpClient = new TcpClientEx();
					tcpClient.NoDelay = true;
					LingerOption lingerOption = new LingerOption(true, 10);
					tcpClient.LingerState = lingerOption;
					tcpClient.ReceiveBufferSize = m_recvBuffer.Length;
					tcpClient.SendBufferSize = m_iSendBuffer;
					tcpClient.Connect(host, port);
					m_socket = tcpClient.socket;
					m_socket.Blocking = true;
					tcpClient.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					tcpClient.Close();
					tcpClient = null;
				}
				catch (SocketException e)
				{
					return winsock_to_s2h_error.Convert(e.ErrorCode);
				}
				catch(Exception)
				{
					return SocksOverHttp.s2h.s2h_errors.s2h_error;
				}
				socket = m_socket;
				m_handle = m_socket.Handle.ToString();
			}
			return SocksOverHttp.s2h.s2h_errors.s2h_ok;
		}
		
		public void Close()
		{
			lock(this)
			{
					if (m_socket != null)
					{
						try
						{
							m_socket.Close();
							m_bClosed = true;
						}
						catch(Exception){}
					}
			}
		}

		public int SendSimple(byte[] buf, int offset, int len)
		{
			lock(this)
			{
				try
				{

					int iRv = m_socket.Send(buf, offset, len, SocketFlags.Partial);
					m_uiSentData += (ulong)iRv;
					return iRv;
				}
				catch (SocketException ex)
				{
					if (ex.ErrorCode != WSAEWOULDBLOCK)
					{
						m_bClosed = true;
						m_socket.Close();
					}
					return 0;
				}
			}
		}

		public int RecvSimple(byte[] buf, int offset, int len)
		{
			lock(this)
			{
				try
				{
					int iRv = m_socket.Receive(buf, offset, len, SocketFlags.Partial);
					m_uiRecvData += (ulong)iRv;
					return iRv;
				}
				catch (SocketException ex)
				{
					if (ex.ErrorCode != WSAEWOULDBLOCK)
					{
						m_bClosed = true;
						m_socket.Close();
					}
					return 0;
				}
			}
		}

	};
};
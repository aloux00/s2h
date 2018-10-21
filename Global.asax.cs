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
	public class TcpClientEx : TcpClient
	{
		public const int WSAEWOULDBLOCK = 10035;
		public const int WSAECONNRESET = 10054;

		public Socket socket 
		{
			get
			{
				return this.Client;
			}
			set
			{
				this.Client = value;
			}
		}
	}

	public class Global : System.Web.HttpApplication
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		private S2hState m_s2hState=null;
		const string m_ConfigFileName = "socksoverhttp.config";
		const string m_UsersFileName = "users.config";
		ApplicationStorage m_config;

		public Global()
		{
			InitializeComponent();
		}	
		
		protected void Application_Start(Object sender, EventArgs e)
		{
			m_config = new ApplicationStorage(System.IO.Path.Combine(this.Server.MapPath(""), m_ConfigFileName));
			m_s2hState = new S2hState();
			Application.Add("s2hState", m_s2hState);
			Application.Add("s2hConfig", m_config);
			Application.Add("s2hUsersPath", System.IO.Path.Combine(this.Server.MapPath(""), m_UsersFileName));
			System.Diagnostics.Debug.WriteLine("Application_Start: Midori's HTTP tunnel started...");
		}
 
		protected void Session_Start(Object sender, EventArgs e)
		{

		}

		protected void Application_BeginRequest(Object sender, EventArgs e)
		{

		}

		protected void Application_EndRequest(Object sender, EventArgs e)
		{

		}

		protected void Application_AuthenticateRequest(Object sender, EventArgs e)
		{

		}

		protected void Application_Error(Object sender, EventArgs e)
		{

		}

		protected void Session_End(Object sender, EventArgs e)
		{

		}

		protected void Application_End(Object sender, EventArgs e)
		{
			S2hState s2hState = (S2hState)Application["s2hState"];
			Application.Remove("s2hState");
			s2hState.RemoveAllClients();
			System.Diagnostics.Debug.WriteLine("Application_End: Midori's HTTP tunnel stopped...");
		}
			
		#region Web Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{    
			this.components = new System.ComponentModel.Container();
		}
		#endregion
	}
}


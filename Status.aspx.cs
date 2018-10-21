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
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

namespace SocksOverHttp
{
	/// <summary>
	/// Summary description for WebForm1.
	/// </summary>
	public class Status : System.Web.UI.Page
	{
		protected System.Web.UI.WebControls.Table Table1;
		protected System.Web.UI.WebControls.Table Table2;
	
		private void Page_Load(object sender, System.EventArgs e)
		{
			TableRow r;
			TableCell c;
			System.Net.IPEndPoint rep;
			S2hState s2hState = (S2hState)Application["s2hState"];
			HybridDictionary conns = s2hState.Exchanger.Connections;
			HybridDictionary clients = s2hState.Clients;

			r = new TableRow();
			c = new TableCell();
			c.Text = "Connetion ID";
			c.BackColor = System.Drawing.Color.Gray;
			r.Cells.Add(c);
			c = new TableCell();
			c.Text = "Remote End Point";
			c.BackColor = System.Drawing.Color.Gray;
			r.Cells.Add(c);

			Table1.Rows.Add(r);

			lock(conns)
			{
				foreach (s2hConnection conn in conns.Values)
				{
					lock (conn)
					{
						r = new TableRow();
						c = new TableCell();
						c.Text = conn.handle;
						r.Cells.Add(c);
						c = new TableCell();
						if (conn.Connected)
						{
							rep = ((System.Net.IPEndPoint)conn.socket.RemoteEndPoint);
							c.Text = rep.Address.ToString()
								+ ":" + rep.Port.ToString();
						}
						else
							c.Text = "Disconnected...";
						r.Cells.Add(c);
						Table1.Rows.Add(r);
					}
				}
			}

			r = new TableRow();
			c = new TableCell();
			c.Text = "Client ID";
			c.BackColor = System.Drawing.Color.Gray;
			r.Cells.Add(c);
			c = new TableCell();
			c.Text = "Client Address";
			c.BackColor = System.Drawing.Color.Gray;
			r.Cells.Add(c);
			Table2.Rows.Add(r);
			lock(clients)
			{
				foreach (s2hClient client in clients.Values)
				{
					lock (client)
					{
						r = new TableRow();
						c = new TableCell();
						c.Text = client.AuthToken;
						r.Cells.Add(c);
						c = new TableCell();
							c.Text = client.ClientAddress.ToString();
						r.Cells.Add(c);
						Table2.Rows.Add(r);
					}
				}
			}	
		}


		#region Web Form Designer generated code
		override protected void OnInit(EventArgs e)
		{
			//
			// CODEGEN: This call is required by the ASP.NET Web Form Designer.
			//
			InitializeComponent();
			base.OnInit(e);
		}
		
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{    
			this.Load += new System.EventHandler(this.Page_Load);

		}
		#endregion
	}
}

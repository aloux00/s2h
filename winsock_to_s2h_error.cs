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

namespace SocksOverHttp
{
	public class winsock_to_s2h_error
	{
		public static SocksOverHttp.s2h.s2h_errors Convert(int error)
		{
			switch(error)
			{
				case 1001: // WSAHOST_NOT_FOUND
					return SocksOverHttp.s2h.s2h_errors.s2h_host_unknown;

				case 10051: // WSAENETUNREACH
					return SocksOverHttp.s2h.s2h_errors.s2h_network_error;

				case 10053: // WSAECONNABORTED
					return SocksOverHttp.s2h.s2h_errors.s2h_connection_refused;

				case 10054: // WSAECONNRESET
					return SocksOverHttp.s2h.s2h_errors.s2h_network_error;

				case 10060: //WSAETIMEDOUT
					return SocksOverHttp.s2h.s2h_errors.s2h_connection_timeout;
				default:
					return SocksOverHttp.s2h.s2h_errors.s2h_error;
			}
		}

	}
}

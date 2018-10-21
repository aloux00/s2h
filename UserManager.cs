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
using System.IO;
using System.Collections;
using System.Collections.Specialized;

namespace SocksOverHttp
{
	/// <summary>
	/// Summary description for UserManager.
	/// </summary>
	public class UserManager
	{
		public class user_info
		{
			public string name = "";
			public string pass = "";
		};
		const string m_strEscapable = ":=%";
		static char[] m_escapable = m_strEscapable.ToCharArray();
		string m_config_file_path;
		public UserManager(string config_file_path)
		{
			m_config_file_path = config_file_path;
		}

		public static user_info ParseUser(string user)
		{
			user_info result = null;
			char[] separator1 = new char[]{':'};
			int iPos = user.LastIndexOf("#"); // remove comments
			if (iPos >= 0)
				user = user.Substring(0, iPos);
			user = user.Trim();
			if (user.Length == 0)
				return null; // empty line

			string [] user_details = user.Split(separator1);
			if (user_details.Length >= 2)	// pass cam be blank, BUT at least 2 fields mut be present
			{
				result = new user_info();
				result.name = unescape(user_details[0]);
				result.pass = unescape(user_details[1]);
			}
			return result;
		}

		public static string HashPassword(string password)
		{
			return "";
		}

		public static string unescape(string escaped)
		{
			string result = escaped;
			int iPos;
			foreach(char c in m_escapable)
			{
				iPos = 0;
				while ((iPos = result.IndexOf(((int)c).ToString("%00X"), iPos) ) > 0)
				{
					result = result.Substring(0, iPos) + c + result.Substring(iPos + 2);
				}
			}
			return result;
		}

		public static string escape(string toescape)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder(toescape.Length);
			foreach (char c in toescape)
			{
				if (m_strEscapable.IndexOf(c) >0)
					sb.Append(((int)c).ToString("%00X"));
				else
					sb.Append(c);
			}
			return sb.ToString();

		}

		public user_info[] GetUsers()
		{
			StreamReader sr = null;
			try
			{
				lock (m_config_file_path)
				{
					user_info user;
					ArrayList lines = new ArrayList(32);
					ArrayList users = new ArrayList(32);
					sr = File.OpenText(m_config_file_path);
					string line;
					while ( (line = sr.ReadLine()) != null)
					{
						lines.Add(line);
					}
					foreach (string l in lines)
					{
						user = ParseUser(l);
						if (user != null)
							users.Add(user);
					}
					return (user_info[])users.ToArray(typeof(user_info));
				}

			}
			catch(Exception)
			{
				return null;
			}
			finally
			{
				if (sr != null)
					sr.Close();
			}
		}

		public user_info GetUser(string username)
		{
			user_info[] users = GetUsers();
			foreach (user_info u in users)
			{
				if (username == u.name)
				{
					return u;
				}
			}
			return null;
		}
	}
}

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
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Soap;


namespace SocksOverHttp
{
	[Serializable]
	public class ApplicationStorage : Hashtable
	{
		#region Private fields
	
		// File name. Let us use the entry assembly name with .dat as the extension.
		private string settingsFileName;

		#endregion
    
		#region Constructor
	
		// The default constructor.
		public ApplicationStorage(string filePath)
		{
			settingsFileName = filePath;
			settingsFileName = settingsFileName.Replace("file:///", "");
			settingsFileName = settingsFileName.Replace("/", @"\");
			LoadData();
		}
		public ApplicationStorage()
		{
			settingsFileName = settingsFileName.Replace("file:///", "");
			settingsFileName = settingsFileName.Replace("/", @"\");
			LoadData();
		}
	
		// This constructor is required for deserializing our class from persistent storage.
		protected ApplicationStorage (SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		#endregion
	
		#region Private methods
	
		private void LoadData()
		{
			FileStream stream = null;
			try
			{
				stream = new FileStream(settingsFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
			}
			catch(Exception)
			{}
			if ( stream != null )
			{
				try
				{
					// DeSerialize the Hashtable from stream.
					IFormatter formatter = new SoapFormatter();
					Hashtable appData = ( Hashtable ) formatter.Deserialize(stream);

					// Enumerate through the collection and load our base Hashtable.
					IDictionaryEnumerator enumerator = appData.GetEnumerator();
					while ( enumerator.MoveNext() )
					{
						this[enumerator.Key] = enumerator.Value;
					}
				}
				catch(Exception)
				{}
				finally
				{
					// We are done with it.
					stream.Close();
				}
			}
		}
	
		#endregion

		#region Public Methods
	
		public void ReLoad()
		{
			LoadData();
		}

		/// <summary>
		/// Saves the configuration data to the persistent storage.
		/// </summary>
		public void Save()
		{
			FileStream stream = new FileStream(settingsFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
			if ( stream != null )
			{
				try
				{
					// Serialize the Hashtable into the IsolatedStorage.
					IFormatter formatter = new SoapFormatter();
					formatter.Serialize( stream, (Hashtable)this );
				}
				finally
				{
					stream.Close();
				}
			}
		}
	
		public string strGet(string key)
		{
			return (string) this[key];
		}

		#endregion
	}
}
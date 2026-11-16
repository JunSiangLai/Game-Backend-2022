using System;
using System.Data;
using System.Reflection;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Diagnostics;


namespace MyFirstPlugin
{
	public class Database
	{
		MySqlConnection conn = new MySqlConnection();
		public void Connect(string host, int port, string db, string user,
		string password)
		{
			conn.ConnectionString = $"server = { host}; user ={ user}; database ={ db}; port ={ port}; password ={ password}";
			conn.Open();
		}
		public void Disconnect()
		{
			conn.Close();
		}
		public DataTable Query(string query)
		{
			DataTable results = new DataTable();
			try
			{
				MySqlCommand cmd = new MySqlCommand(query, conn);
				results.Load(cmd.ExecuteReader());
			}
			catch (Exception e)
			{
				Debug.WriteLine(e.ToString());
			}

			return results;
		}
		public bool NonQuery(string SQLCommand, out string error)
		{
			error = "";
			try
			{
				MySqlCommand cmd = new MySqlCommand(SQLCommand, conn);
				cmd.ExecuteNonQuery();
				return true;
			}
			catch (Exception e)
			{
				Debug.WriteLine(e.ToString());
				Debug.WriteLine("Error Code: " + e.HResult.ToString());
				switch (e.HResult)
                {
					case -2147467259:
						error = "Username already taken";
						break;
                }
				return false;
			}
		}
		public static List<T> DataTableToList<T>(DataTable dt)
		{
			List<T> data = new List<T>();
			foreach (DataRow row in dt.Rows)
			{
				T item = GetItem<T>(row);
				data.Add(item);
			}
			return data;
		}

		public static T GetItem<T>(DataRow dr)
		{
			Type temp = typeof(T);
			T obj = Activator.CreateInstance<T>();
			foreach (DataColumn column in dr.Table.Columns)
			{
				foreach (PropertyInfo pro in temp.GetProperties())
				{
					if (pro.Name == column.ColumnName)
						pro.SetValue(obj, dr[column.ColumnName], null);
					else
						continue;
				}
			}
			return obj;
		}
	}
}

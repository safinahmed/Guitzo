using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Prediction
{
    class Connection
    {
        private static SqlConnection myConnection = null;

        private static string connString = "user id=sa;" +
                                           "password=macaco;server=localhost;" +
                                           "Trusted_Connection=yes;" +
                                           "database=Prediction; " +
                                           "connection timeout=30";

        public static void setConnectionString(string connectionString)
        {
            connString = connectionString;
        }

        public static SqlConnection getConnection()
        {
            if (myConnection == null)
            {
                myConnection = new SqlConnection(connString);
            }
            if(myConnection.State != ConnectionState.Open)
                myConnection.Open();
            return myConnection;
        }

        public static SqlCommand getCommand(String s)
        {
            SqlCommand res = new SqlCommand(s, getConnection());
            while (res.Connection.State != ConnectionState.Open);
            return res;
        }

        public static SqlDataReader ExecuteReader(String s)
        {
            SqlCommand res = getCommand(s);
            return res.ExecuteReader();
        }

        public static int ExecuteNonQuery(String s)
        {
            SqlCommand res = getCommand(s);
            return res.ExecuteNonQuery();
            //return 1;
        }

        public static void Close()
        {
            myConnection.Close();
        }
    }
}

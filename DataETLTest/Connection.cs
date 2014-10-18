using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace DataETLTest
{
    class Connection
    {
        private static SqlConnection myConnection = null;

        private static string connString = "user id=sa;" +
                                           "password=macaco;server=localhost;" +
                                           "Trusted_Connection=yes;" +
                                           "database=Prediction; " +
                                           "connection timeout=30";

        public Connection() {}

        public void setConnectionString(string connectionString)
        {
            connString = connectionString;
        }

        public SqlConnection getConnection()
        {
            if (myConnection == null)
                myConnection = new SqlConnection(connString);

            if(myConnection.State != ConnectionState.Open)
                myConnection.Open();

            return myConnection;
        }

        public SqlCommand getCommand(String s)
        {
            SqlCommand res = new SqlCommand(s, getConnection());
            while (res.Connection.State != System.Data.ConnectionState.Open);
            return res;
        }

        public SqlDataReader ExecuteReader(String s)
        {
            SqlCommand res = getCommand(s);
            return res.ExecuteReader();
        }

        public int ExecuteNonQuery(String s)
        {
            SqlCommand res = getCommand(s);
            return res.ExecuteNonQuery();
            //return 1;
        }

        public void Close()
        {
            myConnection.Close();
        }
    }
}

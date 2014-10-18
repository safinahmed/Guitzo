using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace CompaniesCrawler
{
    class Connection
    {
        private static SqlConnection myConnection = null;

        public static SqlConnection getConnection()
        {
            if(myConnection == null)
                myConnection = new SqlConnection("user id=sa;" +
                                       "password=macaco;server=localhost;" +
                                       "Trusted_Connection=yes;" +
                                       "database=companies; " +
                                       "connection timeout=30");
            try
            {
                myConnection.Open();
            } catch(Exception ex) {}
            return myConnection;
        }

        public static SqlCommand getCommand(String s)
        {
            SqlCommand res = new SqlCommand(s,getConnection());
            while (res.Connection.State != System.Data.ConnectionState.Open) ;
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
    }
}

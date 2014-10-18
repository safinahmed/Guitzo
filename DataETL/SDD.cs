using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace DataETLTest
{
    public class SDD
    {
        private static Dictionary<int,string> _sdds;

        private static string _connectionString = "user id=sa;" +
                           "password=macaco;server=localhost;" +
                           "Trusted_Connection=yes;" +
                           "database=Prediction; " +
                           "connection timeout=30";

        public static string ConnectionString
        {
            set { _connectionString = value; }
        }

        public SDD()
        {
            
        }

        private static void Initialize()
        {
            _sdds = new Dictionary<int, string>();

            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                sqlConnection.Open();
                SqlCommand command = new SqlCommand("SELECT sddNumber, name FROM SDD", sqlConnection);
                SqlDataReader sqlDataReader = command.ExecuteReader();

                while (sqlDataReader.Read())
                {
                    int sddNumber = sqlDataReader.GetInt32(0);
                    String name = sqlDataReader.GetString(1);

                    _sdds.Add(sddNumber,name);

                }
                sqlDataReader.Close();
            }
        }

        public static void Invalidate()
        {
            if(_sdds != null)
                _sdds.Clear();

            Initialize();
        }

        public static string Get(string sddNumber)
        {
            if (_sdds == null)
                Initialize();

            int sddInt = -1;

            string result = "";
            try
            {
                sddInt = Convert.ToInt32(sddNumber);
                result = _sdds[sddInt];
            }
            catch (FormatException)
            {

            }
            catch (KeyNotFoundException)
            {
                Put(sddNumber,"");
            }
            return result;
        }

        public static void Put(string sddNumber, string name)
        {

            if(_sdds == null)
                Initialize();

            int sddInt = 0;
            try
            {
                sddInt = Convert.ToInt32(sddNumber);
            }
            catch (FormatException)
            {
                return;
            }

            //If value doesn't exist, add it
            if (!_sdds.ContainsKey(sddInt))
            {
                using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
                {
                    sqlConnection.Open();
                    SqlCommand command =
                        new SqlCommand("INSERT INTO SDD(sddNumber,name,isNew) VALUES ('" + sddNumber + "','" + name.Replace("'", "''") + "',1)",
                                       sqlConnection);
                    command.ExecuteNonQuery();
                    _sdds.Add(sddInt, name);
                }
            }
        }

    }
}

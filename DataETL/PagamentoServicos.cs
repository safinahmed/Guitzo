using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace DataETLTest
{
    public class PagamentoServicos
    {
        private static Dictionary<int, string> _pagamentoServicos;

        private static string _connectionString = "user id=sa;" +
                                                  "password=macaco;server=localhost;" +
                                                  "Trusted_Connection=yes;" +
                                                  "database=Prediction; " +
                                                  "connection timeout=30";

        public static string ConnectionString
        {
            set { _connectionString = value; }
        }

        public PagamentoServicos()
        {

        }

        private static void Initialize()
        {
            _pagamentoServicos = new Dictionary<int, string>();

            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                sqlConnection.Open();
                SqlCommand command = new SqlCommand("SELECT entityNumber, name FROM PAG_SERV", sqlConnection);
                SqlDataReader sqlDataReader = command.ExecuteReader();

                while (sqlDataReader.Read())
                {
                    int entityNumber = sqlDataReader.GetInt32(0);
                    String name = sqlDataReader.GetString(1);

                    _pagamentoServicos.Add(entityNumber, name);

                }
                sqlDataReader.Close();
            }
        }

        public static void Invalidate()
        {
            if (_pagamentoServicos != null)
                _pagamentoServicos.Clear();

            Initialize();
        }

        public static string Get(string entityNumber)
        {
            if (_pagamentoServicos == null)
                Initialize();

            int entityInt = -1;

            string result = "";
            try
            {
                entityInt = Convert.ToInt32(entityNumber);
                result = _pagamentoServicos[entityInt];
            }
            catch (FormatException)
            {

            }
            catch (KeyNotFoundException)
            {
                Put(entityNumber, "");
            }
            return result;
        }

        public static void Put(string entityNumber, string name)
        {
            if (_pagamentoServicos == null)
                Initialize();

            int entityInt = 0;
            try
            {
                entityInt = Convert.ToInt32(entityNumber);
            }
            catch (FormatException)
            {
                return;
            }

            if (!_pagamentoServicos.ContainsKey(entityInt))
            {
                //If value doesn't exist, add it to DB

                using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
                {
                    sqlConnection.Open();
                    SqlCommand command =
                        new SqlCommand(
                            "INSERT INTO PAG_SERV(entityNumber,name,isNew) VALUES ('" + entityNumber + "','" +
                            name.Replace("'", "''") + "',1)",
                            sqlConnection);
                    command.ExecuteNonQuery();
                    _pagamentoServicos.Add(entityInt, name);
                }
            }
        }
    }
}

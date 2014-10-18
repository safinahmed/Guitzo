using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Prediction
{
    public static class PredictionEngine
    {

        private static double _mininimumConfidenceLevel = 0.35;

        public static double MinimumConfidenceLevel
        {
            set { _mininimumConfidenceLevel = value; }
        }

        private static string _connectionString = "user id=sa;" +
                                   "password=macaco;server=localhost;" +
                                   "Trusted_Connection=yes;" +
                                   "database=Prediction; " +
                                   "connection timeout=30";

        public static string ConnectionString
        {
            set { _connectionString = value; }
        }
        
        private static NaiveBayes _naiveBayes;

        public static NaiveBayes NaiveBayes
        {
            get { return _naiveBayes; }
        }

        public const int NAIVE_BAYES = 1;
        public const int DIRECT = 2;

        public static void NaiveBayesInitialize()
        {
            _naiveBayes = new NaiveBayes(NaiveBayes.FeatureType.AugmentDictionaryWithWordPairs10);

            using(SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                sqlConnection.Open();
                SqlCommand command = new SqlCommand("SELECT categoria, nome FROM categorization", sqlConnection);
                SqlDataReader sqlDataReader = command.ExecuteReader();

                while (sqlDataReader.Read())
                {
                    String cat = sqlDataReader.GetString(0);
                    String est = sqlDataReader.GetString(1);

                    _naiveBayes.AddSample(est, cat);

                }
                sqlDataReader.Close();
            }
        }

        public static string Predict(string sample, int type)
        {
            String result = "";
            sample = sample.ToUpper();
            switch (type)
            {
                case NAIVE_BAYES:
                    {
                       
                        if (_naiveBayes == null)
                            NaiveBayesInitialize();

                        //First tries to get the complete String
                        result = _naiveBayes.GetGlobalSample(sample);

                        if (string.IsNullOrEmpty(result))
                        {
                            double dbl;
                            List<double> lst;

                            String tentative = _naiveBayes.Classify(sample, out dbl, out lst);

                            //We only want results above a minimum threshold
                            if (dbl >= _mininimumConfidenceLevel)
                                result = tentative;
                        }

                        break;
                    }
            }
            return result;
        }

        public static float GetHitRate(List<String> samples, List<String> correctCategories, float minimumProbability, out float categorizedHitRate)
        {
            if(_naiveBayes == null)
                NaiveBayesInitialize();

            return _naiveBayes.GetHitRate(samples, correctCategories, minimumProbability, out categorizedHitRate);
        }

        //Adds sample to NaiveBayes
        //If update flag is true, the existing sample will be updated
        //Returns true if sample already existed
        public static bool Teach(String sample, String category, bool update = false)
        {
            bool result = false;

            if (_naiveBayes == null)
                NaiveBayesInitialize();

            if(!_naiveBayes.ContainsSample(sample))
            {
                using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
                {
                    _naiveBayes.AddGlobalSample(sample, category);   
                    sample = sample.Replace("'", "''");
                    sqlConnection.Open();
                    string insertString = "INSERT INTO categorization(nome,categoria,nif) VALUES('" + sample + "','" +
                                          category + "','NEW')";
                    SqlCommand command = new SqlCommand(insertString, sqlConnection);
                    command.ExecuteNonQuery();
                }
            } 
            else
            {
                if(update)
                {
                    using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
                    {
                        _naiveBayes.UpdateSample(sample, category);
                        sample = sample.Replace("'", "''");
                        sqlConnection.Open();
                        string updateString = "UPDATE categorization SET categoria = '" + category +
                                              "', nif = 'UPDATED', cae = categoria WHERE nome = '" + sample + "'";
                        SqlCommand command = new SqlCommand(updateString, sqlConnection);
                        command.ExecuteNonQuery();
                    }
                    result = true;                    
                }
            }
            return result;
        }
    }
}

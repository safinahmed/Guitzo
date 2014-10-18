using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Prediction;

namespace DataETLTest
{
    class SampleTests
    {
        public SampleTests()
        {
        }

        public void Test(String fileName, string  bankName)
        {

            List<String> samples = new List<string>();
            List<String> correctCategories = new List<string>();

            Dictionary<string,string> dict = new Dictionary<string, string>();

            float minimumProbablity = 0.35f;

            int recCount = 0;
            int successCount = 0;
            int hadFailedCount = 0;

            bool isSuccess = false;
            double successAcu = 0.0;
            double failureAcu = 0.0;

            CSVUtil csv = CSVUtil.FromFileName(fileName, ",",Encoding.Default);
            csv.Next();
            while (csv.Next())
            {
                recCount++;

                string isAuto = csv[1];
                string type = csv[2];
                string category = csv[3];
                string parsedDesc = csv[4];
                string orgDesc = csv[5];
                string bank = csv[6];

                if (type != "Despesa")
                    continue;


                /* GATHER NON EXISTING DATA
                string cat = PredictionEngine.Predict(parsedDesc, PredictionEngine.DIRECT);

                if(string.IsNullOrEmpty(cat))
                {
                    if(!dict.ContainsKey(parsedDesc.ToUpper()))
                        dict.Add(parsedDesc.ToUpper(),category);
                }
                */


                samples.Add(parsedDesc);
                correctCategories.Add(category);
                
                
                //double accuracy = 0.0;
                if (PredictionEngine.NaiveBayes == null)
                    PredictionEngine.NaiveBayesInitialize();
                PredictionEngine.NaiveBayes.k = 0.1f;

                string res = PredictionEngine.Predict(parsedDesc, PredictionEngine.NAIVE_BAYES);

                
                //if (res.ToUpper() == "CAFÉ / PASTELARIA")
                //    res = "RESTAURANTE";

               // if (category.ToUpper() == "CAFÉ / PASTELARIA")
                //    category = "RESTAURANTE";

                if (res.ToUpper().Equals(category.ToUpper()) && isAuto == "0")
                {
                    hadFailedCount++;
                    successCount++;
                    //successAcu += accuracy;
                    isSuccess = true;
                }


                else if (res.ToUpper() == category.ToUpper() && isAuto == "1")
                {
                    successCount++;
                    //successAcu += accuracy;
                    isSuccess = true;
                }
                else
                {
                    //failureAcu += accuracy;
                    isSuccess = false;
                    //Console.WriteLine(category + " - " + res + " - " + orgDesc + " - " + bank);
                    //Console.ReadKey();
                }
                //Console.WriteLine(accuracy + "," + isSuccess);
                 
            }

            /* ADD NON EXISTING ENTRIES
            foreach (KeyValuePair<string, string> keyValuePair in dict)
            {
                Console.WriteLine("INSERT INTO categorization VALUES ('" + keyValuePair.Value + "','" + keyValuePair.Key.Replace("'", "''") + "','','')");
            }*/

            /*
            float maxHitRate = 0.0f;
            float specialK = 0.0f;

            pe.NaiveBayesInitialize();
            pe.NaiveBayes.k = 0.1f;
            for(int i=0;i<200;i++)
            {
                float categorizedHitRate = 0.0f;
                float curHitRate = pe.GetHitRate(samples, correctCategories, minimumProbablity, out categorizedHitRate);
                Console.WriteLine("Total Hit Rate (" + pe.NaiveBayes.k + " / " + i + "): " + curHitRate);
                Console.WriteLine("Categorized Hite Rate: " + categorizedHitRate);

                if (curHitRate > maxHitRate)
                {
                    maxHitRate = curHitRate;
                    specialK = pe.NaiveBayes.k;
                }
                pe.NaiveBayes.k = pe.NaiveBayes.k*1.05f;
            }

            Console.WriteLine("Best k: "+ specialK + " with HitRate: " + maxHitRate);
            */
            
            float categorizedHitRate;
            Console.WriteLine("Total Hit Rate : " + PredictionEngine.GetHitRate(samples, correctCategories, minimumProbablity, out categorizedHitRate));
            Console.WriteLine("Categorized Only Hit Rate : " + categorizedHitRate);
            Console.WriteLine(successCount + " out of " + recCount + " with " + hadFailedCount + " new ones");
            //Console.WriteLine("Success Accuracy" + (successAcu / successCount) + " Failure Accuracy " + (failureAcu / (recCount - successCount)));
             

            Console.WriteLine("\n\n----- FINISHED -------");
            Console.ReadLine();
        }
    }
}

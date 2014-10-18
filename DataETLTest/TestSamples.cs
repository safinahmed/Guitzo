using System;
using System.Text;
using Prediction;

namespace DataETLTest
{
    class TestSamples
    {
        public FileDataRecord fdr;
        public TestSamples() {}


        public void Load(string fileName, string bankName)
        {
            StringBuilder sb = new StringBuilder();

            fdr = new FileDataRecord("",0,0);
            CSVUtil csv = CSVUtil.FromFileName(fileName, ",",Encoding.Default);
            
            while(csv.Next())
            {
                if (csv[6] == bankName)
                {
                    TransactionRecord tr = new TransactionRecord(csv[5], 0, DateTime.Now, 1);
                    fdr.AddTransactionRecord(tr);
                }
            }
            csv.Close();
            BPIConnector dataConnector = new BPIConnector();
            dataConnector.dataRecord = fdr;
            dataConnector.Transform();

            csv = CSVUtil.FromFileName(fileName, ",", Encoding.Default);
            csv.Next();
            int idx = 0;
            while(csv.Next())
            {
                if (csv[6] != bankName)
                    continue;

                TransactionRecord tr = fdr[idx++];

                Console.WriteLine("OriginalDesc: " + tr.OriginalDescription + "\nParsedDesc:" + tr.ParsedDescription.ToUpper() + " \nExtra Info:" + tr.ExtraInfo + "\nType: " + tr.Type + "\n");
                //Console.WriteLine("Predition : " + PredictionEngine.Predict(tr.ParsedDescription.ToUpper(),PredictionEngine.NAIVE_BAYES) + " \nOld Prediction: " + csv[3] + "\nISAUTO: " + csv[1] + "\n");
                Console.ReadKey();
            }
            Console.WriteLine("---FINISHED LOADING---");


            Console.ReadLine();
        }

        public void Predict()
        {
            

            foreach (TransactionRecord tr in fdr)
            {
                string category = PredictionEngine.Predict(tr.ParsedDescription, PredictionEngine.NAIVE_BAYES);
                Console.WriteLine(category + " - " + tr.ParsedDescription);
                Console.ReadKey();
            }
            Console.WriteLine("---FINISHED PREDICTING---");
            Console.ReadLine();
        }
    }
}

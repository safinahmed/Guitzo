using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Prediction;

namespace DataETLTest
{
    class Others
    {
        public Others() {}

        public void DoWork()
        {
            
            /*
            byte[] bytes = null;
            FileStream fs = null;

            try
            {

                fs = File.OpenRead(@"C:\Users\sahmed\Downloads\hotword.csv");
                bytes = new byte[fs.Length];
                fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
                fs.Close();
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
            Connection con = new Connection();
            CSVUtil csv = new CSVUtil(bytes,";");
            String word;
            while(csv.Next())
            {
                word = csv[1];
                con.ExecuteNonQuery("INSERT INTO STOP VALUES ('" + word + "')");
            }*/

            /*
             *             byte[] bytes = null;
            FileStream fs = null;

            try
            {

                fs = File.OpenRead(@"C:\Users\sahmed\Downloads\SDD.csv");
                bytes = new byte[fs.Length];
                fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
                fs.Close();
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
            Connection con = new Connection();
            CSVUtil csv = new CSVUtil(bytes,",");

            while(csv.Next())
            {
                con.ExecuteNonQuery("INSERT INTO SDD VALUES ('','"+ csv[1].Replace("'","''") + "','" + csv[2] + "')");
            }
            con.Close();
             */


            
            Connection con1 = new Connection();
            Connection con2 = new Connection();

            SqlDataReader sql = con1.ExecuteReader("select distinct palavra from wordCount where cnt > 4 order by palavra");
            while (sql.Read())
            {

                String palavra = sql.GetString(0).Replace("'", "''");

                SqlDataReader data =
                    con2.ExecuteReader("select categoria, palavra, cnt from wordCount where palavra = '" + palavra +
                                       "' order by cnt desc");

                data.Read();
                String cat = data.GetString(0).Replace("'", "''");
                String word = data.GetString(1).Replace("'", "''");
                int cnt = data.GetInt32(2);

                int temp = 0;

                while(data.Read())
                {
                    temp += data.GetInt32(2);
                }
                data.Close();

                if (temp > cnt)
                    continue;

                
                double ratio = Convert.ToDouble(cnt) / (temp+cnt) * 100;

                int intRatio = (int) ratio;
                if (ratio < 80)
                    continue;

                con2.ExecuteNonQuery("INSERT INTO hotWords VALUES ('" + cat + "','" + word + "'," + intRatio + ")");



                /*
                String categoria = sql.GetString(0);
                String palavra = sql.GetString(1);

                string[] strg = palavra.Split(' ');
                foreach (var s in strg)
                {
                    if (s.Length < 3)
                        continue;
                    string word = s.Replace("'", "''");

                    Connection con3 = new Connection();
                    SqlDataReader sqlD = con3.ExecuteReader("select * from STOP where stopWord = '" + word + "'");
                    if (sqlD.HasRows)
                    {
                        sqlD.Close();
                        con3.Close();
                        continue;
                    } 
                               sqlD.Close();
                        con3.Close();

                    SqlDataReader sqlData = con2.ExecuteReader("SELECT id, cnt, categoria from wordCount where palavra = '" + word + "' and categoria = '" + categoria + "'");
                    if(sqlData.HasRows)
                    {
                        sqlData.Read();
                        int id = sqlData.GetInt32(0);
                        int cnt = sqlData.GetInt32(1);
                        string newCat = sqlData.GetString(2);
                        sqlData.Close();

                        con2.ExecuteNonQuery("UPDATE wordCount SET cnt = " + ++cnt + " WHERE id = " + id);

                        /*
                        if (newCat == "Trash")
                            continue;
                        
                        if(categoria == newCat)
                            con2.ExecuteNonQuery("UPDATE wordCount SET cnt = " + ++cnt + " WHERE id = " + id);
                        else
                            con2.ExecuteNonQuery("UPDATE wordCount SET categoria = 'Trash' WHERE id = " + id);
                         
                    }
                    else
                    {
                        sqlData.Close();
                        con2.ExecuteNonQuery("INSERT INTO wordCount VALUES ('" + categoria + "','" + word + "',1)");
                    }
                    con2.Close();   
           
                }*/

            }
            con1.Close();
            con2.Close();
        }
    }
}

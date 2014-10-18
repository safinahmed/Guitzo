using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using Prediction;

namespace DataETLTest
{
    internal class Loader
    {
        public Loader()
        {
        }

        public void Load(String file, String category)
        {
            Connection con1 = new Connection();
            Connection con2 = new Connection();

            byte[] bytes = null;
            FileStream fs = null;

            try
            {

                fs = File.OpenRead(file);
                bytes = new byte[fs.Length];
                fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
                fs.Close();
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }

            CSVUtil csv = new CSVUtil(bytes, ",");

            while (csv.Next())
            {
                String name = csv[0].Replace("'", "''");

                if (name.StartsWith("ANTONIO") || name.StartsWith("MARIA") || name.StartsWith("MARIO") ||
                    name.StartsWith("JOÃO") || name.StartsWith("ABILIO") || name.StartsWith("MARTA") ||
                    name.StartsWith("ABEL") || name.StartsWith("ALBERTO") || name.StartsWith("ALBINO") ||
                    name.StartsWith("CARLOS") || name.StartsWith("FERNANDO") || name.StartsWith("FRANCISCO") ||
                    name.StartsWith("JOAQUIM") || name.StartsWith("JORGE") || name.StartsWith("JOSE") ||
                    name.StartsWith("LUIS") || name.StartsWith("MANUEL") || name.StartsWith("PAULO") ||
                    name.StartsWith("PEDRO"))
                    continue;

                SqlDataReader sdr = con1.ExecuteReader("SELECT * FROM finalData WHERE NOME LIKE '" + name + "'");

                if (sdr.HasRows)
                {
                    sdr.Read();
                    int id = sdr.GetInt32(0);
                    String cat = sdr.GetString(1);

                    Console.WriteLine("Found: " + name + " - " + cat);
                    sdr.Close();
                    con1.Close();
                    ConsoleKeyInfo ck = Console.ReadKey();
                    if (ck.KeyChar == 'O' || ck.KeyChar == 'o')
                    {
                        con2.ExecuteNonQuery("DELETE finalData WHERE ID = " + id);
                        con2.Close();
                        con2.ExecuteNonQuery("INSERT INTO finalData VALUES('" + category + "','" + name + "','" + csv[3] +
                                             "','" + csv[2] + "')");
                        con2.Close();
                    }

                }
                else
                {
                    sdr.Close();
                    con1.Close();
                    con2.ExecuteNonQuery("INSERT INTO finalData VALUES('" + category + "','" + name + "','" + csv[3] +
                                         "','" + csv[2] + "')");
                    con2.Close();
                }
            }
            Console.WriteLine("FINSIHED");
            Console.ReadKey();
        }
    }

}

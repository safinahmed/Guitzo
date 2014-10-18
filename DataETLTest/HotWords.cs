using System.Collections.Generic;
using System.Data.SqlClient;

namespace DataETLTest
{
    class HotWords
    {
        public HotWords() {}

        public void GatherRatioWords()
        {
            SortedDictionary<string, SortedDictionary<string, int>> allWords = new SortedDictionary<string, SortedDictionary<string, int>>();

            SortedDictionary<string,KeyValuePair<string,float>> wordTopRatio = new SortedDictionary<string, KeyValuePair<string, float>>();

            Connection con = new Connection();

            SqlDataReader rdr = con.ExecuteReader("SELECT categoria,word,count FROM ALL_WORDS order by categoria");

            //For each result, add word to noClashWords, if it already exists then add it also to removeWord (so it doesn't count)
            while (rdr.Read())
            {
                string categoria = rdr.GetString(0);
                string word = rdr.GetString(1);
                int cnt = rdr.GetInt32(2);

                if (!allWords.ContainsKey(word))
                {
                    SortedDictionary<string, int> tmp = new SortedDictionary<string, int>();
                    tmp.Add(categoria, cnt);
                    allWords.Add(word, tmp);
                } else
                {
                    allWords[word].Add(categoria,cnt);
                }
            }

            rdr.Close();
            con.Close();

            foreach (KeyValuePair<string, SortedDictionary<string, int>> keyValuePair in allWords)
            {
                int max = 0;
                int total = 0;
                KeyValuePair<string, float> maxKvp = new KeyValuePair<string, float>("",0.0f);
                SortedDictionary<string, int> sd = keyValuePair.Value;

                foreach(KeyValuePair<string,int> kvp in sd)
                {
                    total += kvp.Value;
                    if (max < kvp.Value)
                    {
                        max = kvp.Value;
                        maxKvp = new KeyValuePair<string, float>(kvp.Key,(float) kvp.Value);
                    }
                }

                if (total < 5) continue;

                float ratio = maxKvp.Value/total;

                wordTopRatio.Add(keyValuePair.Key,new KeyValuePair<string, float>(maxKvp.Key,ratio));
            }


            foreach (KeyValuePair<string, KeyValuePair<string, float>> keyValuePair in wordTopRatio)
            {
                KeyValuePair<string, float> kvp = keyValuePair.Value;
                string nome = keyValuePair.Key;

                    con.ExecuteNonQuery("INSERT INTO HOT_WORDS VALUES('" + kvp.Key + "','" + nome.Replace("'", "''") +
                                        "'," + kvp.Value + ")");
                    con.Close();

                
            }
        }

        public void GatherNoClashWord()
        {
            SortedDictionary<string,KeyValuePair<string,int>> noClashWords = new SortedDictionary<string, KeyValuePair<string,int>>();
            List<string> removeWord = new List<string>();

            Connection con = new Connection();

            SqlDataReader rdr = con.ExecuteReader("SELECT categoria,word,count FROM ALL_WORDS order by categoria");

            //For each result, add word to noClashWords, if it already exists then add it also to removeWord (so it doesn't count)
            while (rdr.Read())
            {
                string categoria = rdr.GetString(0);
                string word = rdr.GetString(1);
                int cnt = rdr.GetInt32(2);

                if(!noClashWords.ContainsKey(word))
                {
                    noClashWords.Add(word,new KeyValuePair<string, int>(categoria,cnt));
                } else
                {
                    KeyValuePair<string, int> kvp = noClashWords[word];

                        if (!removeWord.Contains(word))
                            removeWord.Add(word);
                    
                }
            }

            rdr.Close();
            con.Close();

            foreach (KeyValuePair<string, KeyValuePair<string,int>> keyValuePair in noClashWords)
            {
                KeyValuePair<string,int> kvp = keyValuePair.Value;
                string nome = keyValuePair.Key;

                if(!removeWord.Contains(nome))
                {
                    con.ExecuteNonQuery("INSERT INTO NO_CLASH_WORDS VALUES('" + kvp.Key + "','" + nome.Replace("'", "''") +
                                        "'," + kvp.Value + ")");
                    con.Close();

                }
            }
        }

        //Gather all words from DB
        public void GatherAllWords()
        {
            SortedDictionary<string, SortedDictionary<string, int>> allSamples =
                new SortedDictionary<string, SortedDictionary<string, int>>();



            Connection con = new Connection();

            SqlDataReader rdr = con.ExecuteReader("SELECT categoria,nome FROM categorization order by categoria");

            //For each result, will create a SortedDictionary with the category, and for each category we have the word and the count
            while(rdr.Read())
            {

                string categoria = rdr.GetString(0);
                string nome = rdr.GetString(1);

                SortedDictionary<string,int> aux = new SortedDictionary<string, int>();

                if(!allSamples.ContainsKey(categoria))
                {
                    allSamples.Add(categoria,aux);
                } else
                {
                    aux = allSamples[categoria];
                }

                string[] vals = nome.Split(' ');

                foreach (string val in vals)
                {
                    if (val.Length < 3) continue;

                    if(aux.ContainsKey(val))
                    {
                        aux[val]++;
                    } else
                    {
                        aux.Add(val,1);
                    }
                }
            }
            rdr.Close();
            con.Close();

            foreach (KeyValuePair<string, SortedDictionary<string, int>> keyValuePair in allSamples)
            {
                string categoria = keyValuePair.Key;

                foreach (KeyValuePair<string, int> valuePair in keyValuePair.Value)
                {
                    string word = valuePair.Key;
                    int cnt = valuePair.Value;

                    if (word.Length > 50) continue; 

                    con.ExecuteNonQuery("INSERT INTO ALL_WORDS VALUES ('" + categoria + "','" + word.Replace("'", "''") +
                                        "'," + cnt + ")");
                    con.Close();
                }
            }
        }
    }
}

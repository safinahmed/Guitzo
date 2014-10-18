using System;
using System.Collections.Generic;


namespace Prediction
{
    /// <summary>Implementation of Naive Bayes classifier</summary>
    public class NaiveBayes
    {
        /// <summary>Type of feature to use in the Naive Bayes classifier</summary>
        public enum FeatureType
        {
            /// <summary>Use regular word features</summary>
            Regular,
            /// <summary>Use regular word features removing words with less than 3 chracters</summary>
            RegularRemovingSmallWordsAndNumbers,
            /// <summary>Uses words and word pairs up to 10 words ahead</summary>
            AugmentDictionaryWithWordPairs10,
            /// <summary>Uses words and word pairs up to 30 words ahead</summary>
            AugmentDictionaryWithWordPairs30,

            /// <summary>Uses words and word triads up to 5 words ahead</summary>
            AugmentDictionaryWithWordTriads5,

        }
        private FeatureType featType;


        /// <summary>Creates a new Naive Bayes text classifier</summary>
        public NaiveBayes()
        {
            this.k = 1;
            this.featType = FeatureType.RegularRemovingSmallWordsAndNumbers;
        }

        /// <summary>Creates a new Naive Bayes text classifier</summary>
        public NaiveBayes(FeatureType f)
        {
            this.k = 1;
            this.featType = f;
        }

        /// <summary>Creates a new Naive Bayes text classifier</summary>
        /// <param name="K">Laplace smoothing parameter</param>
        public NaiveBayes(float K, FeatureType f)
        {
            this.k = K;
                this.featType = f;
        }

        #region Model information
        /// <summary>Laplace smoothing constant</summary>
        public float k;

        /// <summary>Total number of samples</summary>
        private int TotalSamples;

        /// <summary>Stores all possible categories</summary>
        private List<string> AllCategories = new List<string>();
        /// <summary>Stores occurrences of categories</summary>
        private List<int> CatOccurrences = new List<int>();

        /// <summary>Stores dictionaries corresponding to the categories</summary>
        public List<SortedDictionary<string, int>> _Dictionaries = new List<SortedDictionary<string, int>>();

        /// <summary>Total amount of words in a category</summary>
        private List<int> TotalWords = new List<int>();

        /// <summary>Global dictionary</summary>
        //private List<string> GlobalDictionary = new List<string>();
        public SortedDictionary<string, int> GlobalDictionary = new SortedDictionary<string, int>();

        //Will store all samples in the DB
        public SortedDictionary<string,string> GlobalSamples = new SortedDictionary<string, string>(); 
        #endregion

        #region Add samples to the model
        /// <summary>Adds a new sample to the library</summary>
        /// <param name="sample">Sample string</param>
        /// <param name="category">Classification of this sample</param>
        public void AddSample(string sample, string category)
        {
            int ind = GetCategoryIndex(category.ToLower());
            AddSample(sample, ind, category);
        }

        /// <summary>Splitter characters</summary>
        char[] cc = new char[] { ' ', '.', ',', ';', ':', '?', '!', '/', '&', 
                '|', '[', ']', '{', '}', '(', ')', '<', '>', '"', '\\', '=', '%', '-', '#', '+' };

       // char[] cc = new char[] { ' ', '.', ',', ';', ':', '?', '!', '¿', '¡', '/', '&', 
        //        '|', '[', ']', '{', '}', '(', ')', '<', '>', '"', '\r', '\n', '\t', '_', '\\', '¡', '¿', '\v','', '=', '%', '-', '#', '+' };

        /// <summary>Adds a new sample to the library
        /// Updates number of occurances for this category (adds category if necessary), and total words in it
        /// Adds a Dictionary of all words and their occurances related to that category
        /// Cleans and tokenizes samples (according to featType)
        /// 
        /// </summary>
        /// <param name="sample">Sample string</param>
        /// <param name="curCateg">Classification index</param>
        /// <param name="category">category of this sample</param>
        public void AddSample(string sample, int curCateg, string category)
        {
            TotalSamples++;
            sample = sample.ToLower();
            category = category.ToLower();

            if (curCateg < 0)
            {
                AllCategories.Add(category); //Adds Category to AllCategories

                _Dictionaries.Add(new SortedDictionary<string, int>()); //Adds placeholder

                CatOccurrences.Add(0); //init 
                TotalWords.Add(0); //init
                curCateg = AllCategories.Count - 1; //to be generic (see next instruction)
            }

            CatOccurrences[curCateg]++; //inc counter of ocurrances of category

            //Current category name and dictionary
            string strCateg = AllCategories[curCateg];
            SortedDictionary<string, int> curDict = _Dictionaries[curCateg];

            //List<string> curDic = Dictionaries[curCateg];
            //List<int> curWordOccur = WordOccurrences[curCateg];


            //Splits sample words
            string[] words = sample.Split(cc, StringSplitOptions.RemoveEmptyEntries); //also removes .
            words = GetFeatures(words); //get used words from sample depending on chosen algorithm

            TotalWords[curCateg] += words.Length; //add number of ocurrances for this category


            //adds words to the dictionary
            foreach (string s in words)
            {
                if (!curDict.ContainsKey(s)) curDict.Add(s, 1);
                else curDict[s] = curDict[s] + 1;

                if (!GlobalDictionary.ContainsKey(s)) GlobalDictionary.Add(s, 1);
                else GlobalDictionary[s]++;

            }


            //Store in GlobalSamples
            AddGlobalSample(sample,category);

        }

        //Keeps a list of all entries in the DB
        public void AddGlobalSample(string sample, string category)
        {
            sample = sample.ToLower();
            category = category.ToLower();
            if(!GlobalSamples.ContainsKey(sample))
                GlobalSamples.Add(sample,category);
        }

        //Gets Category for this sample
        public string GetGlobalSample(string sample)
        {
            sample = sample.ToLower();
            string result = "";
            try
            {
                result = GlobalSamples[sample.ToLower()];
            } catch(Exception ex)
            {

            }
            return result;

        }

        public bool ContainsSample(string sample)
        {
            sample = sample.ToLower();
            return GlobalSamples.ContainsKey(sample);
        }

        public void UpdateSample(string sample, string category)
        {
            sample = sample.ToLower();
            category = category.ToLower();

            int oldCategoryIndex = GetCategoryIndex(GlobalSamples[sample]);
            int newCategoryIndex = GetCategoryIndex(category);

            //Reduce  count on old category
            CatOccurrences[oldCategoryIndex]--;

            //If new category does not exist, create it
            if(newCategoryIndex < 0)
            {
                AllCategories.Add(category);
                CatOccurrences.Add(1);
                _Dictionaries.Add(new SortedDictionary<string, int>()); //Adds placeholder
                newCategoryIndex = AllCategories.Count - 1; //Fix index
            } 
            else
            {
                //If category already exists, update count
                CatOccurrences[newCategoryIndex]++;
            }

            //Replace category on sample
            GlobalSamples[sample] = category;

            //Splits sample words
            string[] words = sample.Split(cc, StringSplitOptions.RemoveEmptyEntries); //also removes .
            words = GetFeatures(words); //get used words from sample depending on chosen algorithm

            SortedDictionary<string, int> oldDict = _Dictionaries[oldCategoryIndex];
            SortedDictionary<string, int> newDict = _Dictionaries[newCategoryIndex];

            //adds words to the dictionary
            foreach (string s in words)
            {
                //Found the value in the old dictionary
                if (oldDict.ContainsKey(s))
                {
                    //Reduce count
                    oldDict[s]--;
                    //If count is now 0, remove the value
                    if (oldDict[s] < 1)
                        oldDict.Remove(s);
                }
                
                //If word is already in the new category, update count, otherwise add word
                if(newDict.ContainsKey(s))
                {
                    newDict[s]++;
                }
                else
                {
                    newDict.Add(s,1);
                }

            }

        }
        #endregion

        #region Joint probabilities and classification

        /// <summary>Gets Probab(Classification)</summary>
        /// <param name="ind">Index of category</param>
        public float GetCategoryProbability(int ind)
        {
            int catOccurrences = 0;
            if (ind >= 0) catOccurrences = CatOccurrences[ind];

            return ((float)catOccurrences + k) / ((float)TotalSamples + k * (float)AllCategories.Count);
        }

        /// <summary>Computes P(word | Category)</summary>
        /// <param name="word">Word</param>
        /// <param name="ind">Index of category</param>
        public float GetWordProbability(string word, int ind)
        {
            if (ind < 0) return 0;

            int wordOccur = 0;
            if (_Dictionaries[ind].ContainsKey(word)) wordOccur = _Dictionaries[ind][word];

            return ((float)wordOccur + k) / ((float)TotalWords[ind] + k * (float)GlobalDictionary.Count);
        }

        /// <summary>Gets category index number</summary>
        /// <param name="cat">Classification to look for</param>
        public int GetCategoryIndex(string cat)
        {
            return AllCategories.IndexOf(cat);
        }

        /// <summary>Classifies a sample text into one of the known categories</summary>
        /// <param name="sample">Sample to be classified</param>
        public string Classify(string sample)
        {
            double probab; List<double> distrib;
            return Classify(sample, out probab, out distrib);
        }

        /// <summary>Classifies a sample text into one of the known categories</summary>
        /// <param name="sample">Sample to be classified</param>
        /// <param name="Probability">Maximum probability found</param>
        /// <param name="ProbabDistribution">Probability distribution among all possibilities. This will be the log of the probabilities if they underflow.</param>
        public string Classify(string sample, out double Probability, out List<double> ProbabDistribution)
        {
            //Splits sample words
            string[] words = sample.ToLower().Split(cc, StringSplitOptions.RemoveEmptyEntries);
            words = GetFeatures(words);

            ProbabDistribution = new List<double>();
            double totalProbab = 0;

            for (int i = 0; i < AllCategories.Count; i++)
            {
                double probab = Math.Log(GetCategoryProbability(i));
                for (int j = 0; j < words.Length; j++)
                {
                    probab += Math.Log(GetWordProbability(words[j], i));
                }
                totalProbab += Math.Exp(probab);
                ProbabDistribution.Add(probab);
            }

            int indMax = 0;
            double max = ProbabDistribution[0];
            if (totalProbab != 0) totalProbab = 1.0 / totalProbab;

            for (int i = 0; i < AllCategories.Count; i++)
            {
                if (ProbabDistribution[i] > max)
                {
                    max = ProbabDistribution[i];
                    indMax = i;
                }

                if (totalProbab != 0) ProbabDistribution[i] = Math.Exp(ProbabDistribution[i]) * totalProbab;
            }

            Probability = ProbabDistribution[indMax];
            if (totalProbab == 0)
            {
                //1/pb = exp(lnp1)/exp(lnPmax) + exp(lnp2)/exp(lnPmax) +...
                double pb = 0;
                for (int i = 0; i < ProbabDistribution.Count; i++)
                {
                    if (i != indMax) pb += Math.Exp(ProbabDistribution[i] - ProbabDistribution[indMax]);
                }
                pb += 1;
                pb = 1 / pb;
                Probability = pb;
            }

            if (Probability > 1.0) Probability = 1.0;

            return AllCategories[indMax];
        }

        #endregion

        #region Dictionary operation functions

        /// <summary>Extract varied features from the words</summary>
        /// <param name="words">Word list to extract features from</param>
        private string[] GetFeatures(string[] words)
        {
            if (this.featType == FeatureType.Regular) return words;
            List<string> feats = new List<string>();

            if (featType == FeatureType.RegularRemovingSmallWordsAndNumbers)
            {
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].Length > 2)
                    {
                        int n;
                        if (!int.TryParse(words[i], out n)) feats.Add(words[i]);
                    }
                }
            }
            else if (featType == FeatureType.AugmentDictionaryWithWordPairs30 || featType == FeatureType.AugmentDictionaryWithWordPairs10)
            {
                int n = 10;
                if (featType == FeatureType.AugmentDictionaryWithWordPairs30) n = 30;
                if (featType == FeatureType.AugmentDictionaryWithWordPairs10) n = 10;

                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].Length > 2)
                    {
                        feats.Add(words[i]);
                        for (int j = 1; j < n && i + j < words.Length; j++)
                        {
                            if (words[i + j].Length > 2)
                            {
                                if (words[i + j].CompareTo(words[i]) > 0) feats.Add(words[i] + " " + words[i + j]);
                                else feats.Add(words[i + j] + " " + words[i]);
                            }
                        }
                    }
                }
            }
            else if (featType == FeatureType.AugmentDictionaryWithWordTriads5)
            {
                int n = 5;

                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].Length > 2)
                    {
                        feats.Add(words[i]);
                        for (int j = 1; j < n && i + j < words.Length; j++)
                        {
                            if (words[i + j].Length > 2)
                            {
                                if (words[i + j].CompareTo(words[i]) > 0) feats.Add(words[i] + " " + words[i + j]);
                                else feats.Add(words[i + j] + " " + words[i]);

                                for (int k = 1; k < n && i + j + k < words.Length; k++)
                                {
                                    if (words[i + j + k].Length > 2)
                                    {
                                        List<string> lst = new List<string>() { words[i], words[i + j], words[i + j + k] };
                                        lst.Sort();
                                        feats.Add(lst[0] + " " + lst[1] + " " + lst[2]);
                                    }
                                }

                            }
                        }
                    }
                }
            }

            return feats.ToArray();
        }

        #endregion

        #region Calibration of Laplacian smoothing parameter K

        /// <summary>Attempts to calibrate laplacian smoothing factor K</summary>
        /// <param name="CalibSet">Calibration set</param>
        /// <param name="Labels">Correct labels for calibration set</param>
        public void CalibrateK(List<string> CalibSet, List<string> Labels)
        {
            float desiredProbab = 1.15f / (float)this.AllCategories.Count;
            CalibrateK(CalibSet, Labels, 0.1f, 1.05f, desiredProbab, CalibSet.Count >> 2);
        }

        /// <summary>Attempts to calibrate laplacian smoothing factor K</summary>
        /// <param name="CalibSet">Calibration set</param>
        /// <param name="Labels">Correct labels for calibration set</param>
        /// <param name="k0">Initial laplacian smoothing K to try</param>
        /// <param name="IncreaseFactor">Increment factor: at each step K = K*IncreaseFactor</param>
        /// <param name="CalibProbability">Minimum probability to include classification as valid.</param>
        /// <param name="MinSamples">Minimum number of samples considered valid to continue iterating.</param>
        public void CalibrateK(List<string> CalibSet, List<string> Labels, float k0, float IncreaseFactor, float CalibProbability, int MinSamples)
        {
            if (IncreaseFactor < 1.0f) throw new Exception("Increase factor must be > 1");
            if (CalibSet.Count != Labels.Count) throw new Exception("Labels.Count must be equal to CalibSet.Count");

            //if (CalibProbability < 0.5f) throw new Exception("Desired classification probability should be > 0.5");

            float kTry = k0;

            bool finished = false;
            float bestK = 0.1f; float besthit = 0; int bestCount;
            while (!finished)
            {
                this.k = kTry;

                //Check hit rate in test set
                List<string> classif = new List<string>();
                float hitRate = 0; float totalCount = 0;
                for (int kk = 0; kk < CalibSet.Count; kk++)
                {
                    double p; List<double> d;
                    string c = this.Classify(CalibSet[kk], out p, out d);
                    classif.Add(c);

                    if (p > CalibProbability)
                    {
                        totalCount += 1;
                        if (c == Labels[kk].ToLower()) hitRate += 1.0f;
                        else
                        {
                        }
                    }
                }
                hitRate /= totalCount;

                if (besthit < hitRate)
                {
                    besthit = hitRate;
                    bestK = kTry;
                    bestCount = (int)totalCount;
                }

                if (totalCount < MinSamples) finished = true;
                kTry *= IncreaseFactor;
            }

            this.k = bestK;
        }

        #endregion

        #region Hit rate calculation


        /// <summary>Computes hit rate for a given set.</summary>
        /// <param name="TestSet">Test set</param>
        /// <param name="Labels">Correct labels</param>
        public float GetHitRate(List<string> TestSet, List<string> Labels)
        {
            float CategorizedHitRate;
            return GetHitRate(TestSet, Labels, 0, out CategorizedHitRate);
        }

        /// <summary>Computes hit rate for a given set.</summary>
        /// <param name="TestSet">Test set</param>
        /// <param name="Labels">Correct labels</param>
        /// <param name="TestProbability">Probabilities to consider. Set to ZERO for global hit rate</param>
        public float GetHitRate(List<string> TestSet, List<string> Labels, float TestProbability, out float CategorizedHitRate)
        {
            int ExamplesUsed;
            return GetHitRate(TestSet, Labels, TestProbability, out ExamplesUsed, out CategorizedHitRate);
        }

        /// <summary>Computes hit rate for a given set. (which is above TestProbability and is correct)</summary>
        /// <param name="TestSet">Test set</param>
        /// <param name="Labels">Correct labels</param>
        /// <param name="TestProbability">Probabilities to consider. Set to ZERO for global hit rate</param>
        /// <param name="ExamplesUsed">How many of the examples were used?</param>
        /// <param name="Categorized Hit Rate">How many correct categorizations where probability is above TestProbability</param>
        public float GetHitRate(List<string> TestSet, List<string> Labels, float TestProbability, out int ExamplesUsed, out float CategorizedHitRate)
        {
            ExamplesUsed = 0;
            //Check hit rate in test set
            List<string> classif = new List<string>();
            float hitRate = 0; float totalCount = 0;
            for (int kk = 0; kk < TestSet.Count; kk++)
            {
                double p; List<double> d;
                string c = this.Classify(TestSet[kk], out p, out d);
                classif.Add(c);

                if (p > TestProbability)
                {
                    totalCount += 1;
                    ExamplesUsed++;
                    if (c.ToLower() == Labels[kk].ToLower()) hitRate += 1.0f;
                }
            }
            CategorizedHitRate = hitRate / totalCount;

            hitRate /= TestSet.Count;



            return hitRate;
        }

        #endregion
    }
}

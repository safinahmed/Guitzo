using System;
using System.IO;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace DataETLTest
{
    /// <summary>
    /// Utility class responsible for CSV file handling
    /// </summary>
    public class CSVUtil
    {
        private TextFieldParser _parser;
        private string[] _currentValues;

        public string this[int i]
        {
            get { if (_currentValues == null)
                    return "";
                return _currentValues[i];
            }
        }

        public CSVUtil(byte[] fileData, string delimiter, Encoding encoding)
        {
            MemoryStream memoryStream = new MemoryStream(fileData, false);
            _parser = new TextFieldParser(memoryStream, encoding);
            _parser.Delimiters = new string[] { delimiter };
        }

        public CSVUtil(byte[] fileData, string delimiter) : this(fileData, delimiter, Encoding.Default)
        {
        }

        public static CSVUtil FromFileName(string fileName, string delimiter, Encoding encoding)
        {
            byte[] bytes = null;
            FileStream fs = null;

            try
            {

                fs = File.OpenRead(fileName);
                bytes = new byte[fs.Length];
                fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
                fs.Close();
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }

            if (bytes == null)
                throw new FileLoadException("Could not open file " + fileName);

            return new CSVUtil(bytes, delimiter, encoding);
        }

        public void SkipLines(int numberLines)
        {
            for (int i = 0; i < numberLines; i++)
                _parser.ReadLine();
        }

        public bool Next()
        {
            if(_parser == null || _parser.EndOfData)
                return false;

            _currentValues = _parser.ReadFields();
            return true;
        }

        public void Close()
        {
            _parser.Close();
        }
    }
}

namespace DataETLTest
{
    /// <summary>
    /// Interface that specifies what each ETLConnector should do
    /// An ETLConnector will be a class that can read the contents of a file and extract, transform and load all relevant data
    /// </summary>
    internal interface IETLConnector
    {
        /// <summary>
        /// Service responsible for reading and extracting relevant data from a file
        /// </summary>
        /// <param name="fileData">Bytes of the file</param>
        /// <returns>All relevant data</returns>
        FileDataRecord Extract(byte[] fileData);

        /// <summary>
        /// This service will be responsible from transforming the data extracted with business rules
        /// This transformation can be Parsing Description or selecting Categories, etc
        /// </summary>
        /// <returns></returns>
        FileDataRecord Transform();


        FileDataRecord Categorize();

        /// <summary>
        /// This service will be responsible for loading the data into persistence mechanism
        /// </summary>
        void Load();
    }
}

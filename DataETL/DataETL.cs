using System;

namespace DataETLTest
{
    /// <summary>
    /// Controller class that should be used for ETL
    /// !!! Maybe we need to add different methods only for E,T or L, or change existing method to allow for single action only (E, T or L)
    /// </summary>
    public class DataETL
    {
        /// <summary>
        /// This method is the main method to perform ETL on a given file
        /// Throws a ETLException in case of errors
        /// </summary>
        /// <param name="fileData">Content of the file</param>
        /// <param name="entityName">Each entityName represents different processing rules for the file</param>
        /// <param name="categorize">Will categorize the transactions</param>
        /// <returns>FileDataRecord with all relevant information found</returns>
        public FileDataRecord ProcessData(byte[] fileData, string entityName, bool categorize = false)
        {
            FileDataRecord result = null;
            IETLConnector etlConnector = null;

            try
            {
                if (entityName == null)
                {
                    throw new ETLException("entityName cannot be null");
                }
                else if (entityName.Equals("BES"))
                {
                    etlConnector = new BESConnector();
                }
                else if (entityName.Equals("CGD"))
                {
                    etlConnector = new CGDConnector();
                }
                else if (entityName.Equals("BCP"))
                {
                    etlConnector = new BCPConnector();
                }
                else if (entityName.Equals("Montepio"))
                {
                    etlConnector = new MontepioConnector();
                }
                else if (entityName.Equals("BPI"))
                {
                    etlConnector = new BPIConnector();
                }
                else if (entityName.Equals("BEST"))
                {
                    etlConnector = new BestConnector();
                }
                else
                {
                    throw new ETLException("entityName not supported");
                }

                etlConnector.Extract(fileData);
                result = etlConnector.Transform();

                if (categorize)
                    result = etlConnector.Categorize();
            }
            catch (Exception exception)
            {
                //TODO: Log
                Logger.log("Error parsing  file for " + entityName + " : " + exception.Message,"DataETL");
                throw new ETLException(exception.Message, exception);
            }

            return result;
        }

        public FileDataRecord ProcessUnknownData(byte[] fileData, out string entityName, bool categorize = false )
        {
            IETLConnector etlConnector = null;
            FileDataRecord result = null;
            entityName = "";

            try
            {
                etlConnector = new BESConnector();
                etlConnector.Extract(fileData);
                entityName = "BES";
            } catch(Exception)
            {
            }

            try
            {
                etlConnector = new BCPConnector();
                etlConnector.Extract(fileData);
                entityName = "BCP";
            }
            catch (Exception)
            {
            }

            try
            {
                etlConnector = new BPIConnector();
                etlConnector.Extract(fileData);
                entityName = "BPI";
            }
            catch (Exception)
            {
            }

            try
            {
                etlConnector = new CGDConnector();
                etlConnector.Extract(fileData);
                entityName = "CGD";
            }
            catch (Exception)
            {
            }

            try
            {
                etlConnector = new MontepioConnector();
                etlConnector.Extract(fileData);
                entityName = "Montepio";
            }
            catch (Exception)
            {
            }

            try
            {
                etlConnector = new BestConnector();
                etlConnector.Extract(fileData);
                entityName = "BEST";
            }
            catch (Exception)
            {
            }

            if (entityName.Length > 0)
            {
                try
                {
                    result = etlConnector.Transform();
                    if (categorize)
                        result = etlConnector.Categorize();
                }
                catch (Exception ex)
                {
                    Logger.log("Error processing unkown data for " + entityName + " : " + ex.Message, "DataETL");
                    throw new ETLException("Could not find appropriate Bank", ex);
                }
            }
            else
            {
                throw new ETLException("Could not find appropriate Bank");
            }

            return result;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Net;
using System.Net.Sockets;
using System.Data.SqlClient;
using System.Data;

namespace LCAUtilsCL
{
    public class LCADatabaseUtils
    {
        public enum LCADBTypes
        {
            DB_Config = 1,
            DB_Data = 2,
            DB_Ctrl = 3,
            DB_Price = 4
        }

        public class LCADatabaseSettings
        {
            public string dbServer = "";
            public string dbConfigDatabase = "";
            public string dbDataDatabase = "";
            public string dbCtrlDatabase = "";
            public string dbPriceDatabase = "";
            public bool   dbNeedUser = false;
            public string dbUser = "";
            public string dbPass = "";

            public LCADatabaseSettings()
            {
            }

            public LCADatabaseSettings(string server, string cfgDataBase, string dataDataBase, string ctrlDataBase, string priceDataBase, bool needUser, string user, string pass)
            {
                init(server, cfgDataBase, dataDataBase, ctrlDataBase, priceDataBase, needUser, user, pass);
            }
            public LCADatabaseSettings(string server, string cfgDataBase, string dataDataBase, string ctrlDataBase, bool needUser, string user, string pass)
            {
                init(server, cfgDataBase, dataDataBase, ctrlDataBase, null, needUser, user, pass);
            }
            public LCADatabaseSettings(string server, string dataBase, bool needUser, string user, string pass)
            {
                init(server, null, dataBase, null, null, needUser, user, pass);
            }
            
            private void init(string server, string cfgDataBase, string dataDataBase, string ctrlDataBase, string priceDataBase, bool needUser, string user, string pass) 
            {
                dbServer = server;
                dbConfigDatabase = cfgDataBase;
                dbDataDatabase = dataDataBase;
                dbCtrlDatabase = ctrlDataBase;
                dbPriceDatabase = priceDataBase;

                dbNeedUser = needUser;
                dbUser = user;
                dbPass = pass;
            }
        }

        public static string generateEntityConnectionString(string modelName, LCADatabaseSettings dbSettings)
        {
            return generateEntityConnectionString(modelName, dbSettings, LCADBTypes.DB_Data);
        }

        public static string generateEntityConnectionString(string modelName, LCADatabaseSettings dbSettings, LCADBTypes dbType)
        {
            System.Data.EntityClient.EntityConnectionStringBuilder cStr = new System.Data.EntityClient.EntityConnectionStringBuilder();
            string pcs = "";

            if (dbSettings.dbServer.ToUpper() == "LOCAL")
            {
                // Replace the word local with the actual ip address of this machine
                if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    return "";
                }
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                dbSettings.dbServer = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
            }

            // Build up an entity connection string for the database settings provided
            // Metadata taken from generated App.Config code
            cStr.Metadata = "res://*/" + modelName + ".csdl|res://*/" + modelName + ".ssdl|res://*/" + modelName + ".msl";
            cStr.Provider = "System.Data.SqlClient";
            // Actual sql server connection string with options provided on the config screen
            pcs = "data source=" + dbSettings.dbServer + ";initial catalog=";
            switch (dbType)
            {
                case LCADBTypes.DB_Config:
                    pcs += dbSettings.dbConfigDatabase;
                    break;
                case LCADBTypes.DB_Data:
                    pcs += dbSettings.dbDataDatabase;
                    break;
                case LCADBTypes.DB_Ctrl:
                    pcs += dbSettings.dbCtrlDatabase;
                    break;
                case LCADBTypes.DB_Price:
                    pcs += dbSettings.dbPriceDatabase;
                    break;
            }
                
            if (dbSettings.dbNeedUser == true)
            {
                pcs += ";integrated security=false;User ID=" + dbSettings.dbUser + ";password=" + dbSettings.dbPass + ";";
            }
            else
            {
                pcs += ";integrated security=True;";
            }
            pcs += "Connection Timeout=20;multipleactiveresultsets=True;App=EntityFramework;";
            cStr.ProviderConnectionString = pcs;

            return cStr.ConnectionString;
        }

        //public static DataSet runQuery(string dbCnStr, string query)
        //{
        //    DataSet result = null;

        //    using (SqlConnection cn = new SqlConnection(dbCnStr))
        //    {
        //        cn.Open();

        //        SqlCommand cmd = new SqlCommand(query, cn);
        //        if (cmd.Connection.State == System.Data.ConnectionState.Open)
        //        {
        //            SqlDataReader reader =  cmd.ExecuteReader();
                    
        //            reader.
        //        }


        //    }

        //    return result;
        //}

        public static string generateConnectionString(LCADatabaseSettings dbSettings)
        {
            return generateConnectionString(dbSettings, LCADBTypes.DB_Data);
        }

        public static string generateConnectionString(LCADatabaseSettings dbSettings, LCADBTypes dbType)
        {
            System.Data.EntityClient.EntityConnectionStringBuilder cStr = new System.Data.EntityClient.EntityConnectionStringBuilder();
            string pcs = "";

            if (dbSettings.dbServer.ToUpper() == "LOCAL")
            {
                // Replace the word local with the actual ip address of this machine
                if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    return "";
                }
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                dbSettings.dbServer = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
            }

            // Build up an entity connection string for the database settings provided
            // Metadata taken from generated App.Config code
            cStr.Metadata = "";
            cStr.Provider = "System.Data.SqlClient";
            // Actual sql server connection string with options provided on the config screen
            pcs = "data source=" + dbSettings.dbServer + ";initial catalog=";
            switch (dbType)
            {
                case LCADBTypes.DB_Config:
                    pcs += dbSettings.dbConfigDatabase;
                    break;
                case LCADBTypes.DB_Data:
                    pcs += dbSettings.dbDataDatabase;
                    break;
                case LCADBTypes.DB_Ctrl:
                    pcs += dbSettings.dbCtrlDatabase;
                    break;
                case LCADBTypes.DB_Price:
                    pcs += dbSettings.dbPriceDatabase;
                    break;

            }

            if (dbSettings.dbNeedUser == true)
            {
                pcs += ";integrated security=SSPI;User ID=" + dbSettings.dbUser + ";password=" + dbSettings.dbPass + ";";
            }
            else
            {
                pcs += ";integrated security=True;";
            }
            pcs += "Connection Timeout=20;multipleactiveresultsets=True;App=EntityFramework;";
            cStr.ProviderConnectionString = pcs;

            return cStr.ConnectionString;
        }

        public static string LookupDsnConnectStr(string dsn)
        {
            string cnStr = "";

            RegistryKey reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\LCA\Connections");
            if (reg != null)
            {
                string[] subKeyNames = reg.GetSubKeyNames();

                for (int i = 0; i < subKeyNames.Count(); i++)
                {
                    RegistryKey thisKey = reg.OpenSubKey(subKeyNames[i]);

                    string name = (String)thisKey.GetValue("DBName");

                    if (name.CompareTo(dsn) == 0)
                    {
                        cnStr = (String)thisKey.GetValue("ConnectionString");
                        break;
                    }
                }
            }

            return cnStr;
        }

        private static System.Collections.SortedList getDataSourceNames(Microsoft.Win32.RegistryKey baseReg)
        {
            System.Collections.SortedList dsnList = new System.Collections.SortedList();

            // get system dsn's
            Microsoft.Win32.RegistryKey reg = baseReg.OpenSubKey("Software");
            if (reg != null)
            {
                reg = reg.OpenSubKey("ODBC");
                if (reg != null)
                {
                    reg = reg.OpenSubKey("ODBC.INI");
                    if (reg != null)
                    {
                        reg = reg.OpenSubKey("ODBC Data Sources");
                        if (reg != null)
                        {
                            // Get all DSN entries defined in DSN_LOC_IN_REGISTRY.
                            foreach (string sName in reg.GetValueNames())
                            {
                                dsnList.Add(sName, sName);
                            }
                        }
                        try
                        {
                            reg.Close();
                        }
                        catch { /* ignore this exception if we couldn't close */ }
                    }
                }
            }
            return dsnList;
        }

        public static System.Collections.SortedList getSystemDSNNames()
        {
            return getDataSourceNames(Microsoft.Win32.Registry.LocalMachine);
        }

        public static System.Collections.SortedList getUserDSNNames()
        {
            return getDataSourceNames(Microsoft.Win32.Registry.CurrentUser);
        }

        public static System.Collections.SortedList getAllDSNNames()
        {
            System.Collections.SortedList fullList = new System.Collections.SortedList();

            System.Collections.SortedList systemDsns = getSystemDSNNames();
            System.Collections.SortedList userDsns = getUserDSNNames();

            for (int i = 0; i < systemDsns.Count; i++)
            {
                fullList.Add(systemDsns.GetKey(i), systemDsns.GetByIndex(i));
            }

            for (int i = 0; i < userDsns.Count; i++)
            {
                fullList.Add(userDsns.GetKey(i), userDsns.GetByIndex(i));
            }

            return fullList;

        }
    }
}

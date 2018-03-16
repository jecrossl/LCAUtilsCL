using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace LCAUtilsCL.EDI
{
  public class LCAEDIFileType
  {
    public LCAEDIConnection connection;
    public string fileName;
    public string folder;
    public string fullName;

    //public int settingsIDX;
    //public int pointIDX;
    //public int dTypeIDX;
    //public int versionIDX;

    public string localFileName;
    public string localFolder;
  }

  public class LCAEDIPoint
  {
    /// <summary>
    /// Provides detailed information about the different file versions and release timing b
    /// ased on the report type we're looking for
    /// </summary>
    public class LCAEDIDataType
    {
      public enum EDIDataTypes
      {
        UNKNOWN,
        TMD,
        TMD_WL,
        VMD
      }

      /// <summary>
      /// Detailed information about the EDI file version
      /// </summary>
      public class versionInfo
      {
        /// <summary>
        /// Version number
        /// </summary>
        public int versionNum;
        /// <summary>
        /// Suffix to be added to the file name (ie. "_v1")
        /// </summary>
        public string versionSuffix;
        public string versionName;

        /// <summary>
        /// Calendar day lag (report is released this # of calendar days behind report date)
        /// Note that some report versions have Calendar Day AND Business Day lag
        /// </summary>
        public int publishDayLag;
        /// <summary>
        /// Business day lag (report is released this # of business days (mon-fri) behind report date) 
        /// Note that some report versions have Calendar Day AND Business Day lag
        /// </summary>
        public int publishBDayLag;

        /// <summary>
        /// The time at which this report is expected to be published (HH:mm:ss")
        /// </summary>
        public string publishTime;
        public bool isActive = false;

        public string makeFileName(string fileNameStructure, DateTime dt, string accountName)
        {
          string file = fileNameStructure;

          file = file.Replace("!~ACCOUNTNAME~!", accountName);
          file = file.Replace("!~DATE:YYYYMMDD~!", dt.ToString("yyyyMMdd"));
          file = file + versionSuffix + ".zip";

          return file;
        }

        public versionInfo(int vNum, string vName, string vSuffix, int pDayLag, int pBDayLag, string pTime)
        {
          versionNum = vNum;
          versionName = vName;
          versionSuffix = vSuffix;
          publishTime = pTime;
          publishDayLag = pDayLag;
          publishBDayLag = pBDayLag;
        }
      }

      /// <summary>
      /// Full text name of this data type
      /// </summary>
      public string typeName;
      /// <summary>
      /// Name of the sub folder which contains these reports 
      /// (append "private"/"IESO Account"/"folderName" to get the full Path)
      /// </summary>
      public string folderName;
      /// <summary>
      /// Template which describes how the file name is formed for this report
      /// Can contain placeholders for accout name, date, etc using the "!~...~!" delimiter
      /// </summary>
      public string fileNameStructure;
      /// <summary>
      /// The extension at the end of the file name
      /// </summary>
      public string fileExtension = ".zip";

      /// <summary>
      /// List of the file version configurations that are available for the specified report type
      /// </summary>
      public List<versionInfo> versions = new List<versionInfo>();

      private EDIDataTypes m_DataType;
      /// <summary>
      /// Type of data file we are dealing with
      /// </summary>
      public EDIDataTypes dataType
      {
        get
        {
          return m_DataType;
        }
        set
        {
          m_DataType = value;
          getSettings();
        }
      }

      /// <summary>
      /// Populates the version settings
      /// </summary>
      private void getSettings()
      {
        versionInfo version;
        versions = new List<versionInfo>();

        switch (m_DataType)
        {
          case (EDIDataTypes.TMD):
            typeName = "Totalized Meter Data";
            folderName = "MMP-TMD";
            fileNameStructure = "CNF-!~ACCOUNTNAME~!_MMP-TMD_!~DATE:YYYYMMDD~!";

            version = new versionInfo(1, "V1", "_v1", -2, 0, "06:00:00");
            versions.Add(version);
            version = new versionInfo(2, "V2", "_v2", -1, -8, "06:00:00");
            versions.Add(version);
            version = new versionInfo(3, "V3", "_v3", -1, -18, "06:00:00");
            versions.Add(version);
            version = new versionInfo(4, "V4", "_v1", -1, -5, "06:00:00");
            versions.Add(version);
            break;

          case (EDIDataTypes.TMD_WL):
            typeName = "Totalized Meter Data With Losses";
            folderName = "MMP-TMD-WL";
            fileNameStructure = "CNF-!~ACCOUNTNAME~!_MMP-TMD-WL_!~DATE:YYYYMMDD~!";

            version = new versionInfo(1, "V1", "_v1", -2, 0, "06:00:00");
            versions.Add(version);
            version = new versionInfo(2, "V2", "_v2", -1, -8, "06:00:00");
            versions.Add(version);
            version = new versionInfo(3, "V3", "_v3", -1, -18, "06:00:00");
            versions.Add(version);
            version = new versionInfo(4, "V4", "_v1", -1, -5, "06:00:00");
            versions.Add(version);
            break;

          case (EDIDataTypes.VMD):
            typeName = "Verified Meter Data";
            folderName = "MMP-VMD";
            fileNameStructure = "CNF-!~ACCOUNTNAME~!_MMP-VMD_!~DATE:YYYYMMDD~!";

            version = new versionInfo(1, "V1", "_v1", -1, 0, "08:00:00");
            versions.Add(version);
            version = new versionInfo(2, "V2", "_v2", -1, -8, "08:00:00");
            versions.Add(version);
            version = new versionInfo(3, "V3", "_v3", -1, -18, "08:00:00");
            versions.Add(version);
            version = new versionInfo(4, "V4", "_v1", -1, -5, "06:00:00");
            versions.Add(version);
            break;
        }
      }
    }

    /// <summary>
    /// Abitrarily assigned point name (for reference only)
    /// </summary>
    public string name;
    /// <summary>
    /// IESO Location ID (ie: 100110)
    /// </summary>
    public string locationID;
    /// <summary>
    /// EDI Channel that contains the data we want
    /// </summary>
    public int channel;
    /// <summary>
    /// Point ID for the LCA Database (positive is external, negative is internal)
    /// </summary>
    public int lcaID;
    /// <summary>
    /// List of the data types we want to read for this point
    /// </summary>
    public List<LCAEDIDataType> dataTypes = new List<LCAEDIDataType>();


  }

  public class LCAEDIConnection
  {
    public enum EDIConnectionTypes
    {
      SFTP
    }
    /// <summary>
    /// Protocol we are using to download the reports
    /// </summary>
    public EDIConnectionTypes protocol;

    /// <summary>
    /// Server address
    /// </summary>
    public string server;
    /// <summary>
    /// IESO assigned user name 
    /// </summary>
    public string user;
    /// <summary>
    /// Password for this user
    /// </summary>
    public string pWord;

    public bool useProxy = false;
    public string proxyIP = "";
    public int proxyPort = 0;
    public string proxyUser = "";
    public string proxyPass = "";
  }

  public class LCAEDISettings
  {
    /// <summary>
    /// Name given to this configuration file (for reference only, not used in code)
    /// </summary>
    public string name;
    /// <summary>
    /// Account name assigned to this customer by the IESO (ie. FALCONBRIDGE)
    /// </summary>
    public string accountName;

    public LCAEDIConnection connection;

    public List<LCAEDIPoint> points = new List<LCAEDIPoint>();
  }

  public class LCAEDIConfig
  {
    /// <summary>
    /// List of EDI configurations that have been loaded
    /// </summary>
    public List<LCAEDISettings> settings = new List<LCAEDISettings>();

    /// <summary>
    /// Clear out the current configuration in preparation for loading a new one
    /// (not necessary on first load after object creation)
    /// </summary>
    public void clearConfig()
    {
      settings.Clear();
      settings = new List<LCAEDISettings>();
    }

    /// <summary>
    /// Gets a list of the files that should be available on the specified date.
    /// This will include file names for all active versions of the file, for their appropriate dates
    /// </summary>
    /// <param name="curDT">Date/Time of the search day (ie. today for real-time)</param>
    /// <returns>TRUE if some files need to be downloaded</returns>
    public List<LCAEDIFileType> queueFilesReleasedToday(List<DateTime> holidays)
    {
      int sIdx = 0;
      int pIdx = 0;
      int dtIdx = 0;
      int vIdx = 0;
      List<LCAEDIFileType> dlQueue = new List<LCAEDIFileType>();

      foreach (LCAEDISettings setting in settings)
      {
        foreach (LCAEDIPoint point in setting.points)
        {
          foreach (LCAEDIPoint.LCAEDIDataType dataType in point.dataTypes)
          {
            foreach (LCAEDIPoint.LCAEDIDataType.versionInfo version in dataType.versions)
            {
              if (version.isActive)
              {
                DateTime publishTime;
                try {
                  publishTime = DateTime.ParseExact(version.publishTime, "HH:mm:ss", null);
                }
                catch (Exception e) {
                  publishTime = DateTime.Now.AddHours(-1);
                }

                if (publishTime.TimeOfDay <= DateTime.Now.TimeOfDay) 
                {
                  LCAEDIFileType thisFile = new LCAEDIFileType();

                  DateTime fileDate = DateTime.Now;

                  fileDate = LCATimeUtils.AddBusinessDays(fileDate, version.publishBDayLag, holidays);
                  fileDate = fileDate.AddDays(version.publishDayLag);

                  string fName = version.makeFileName(dataType.fileNameStructure, fileDate, setting.accountName);

                  thisFile.connection = setting.connection;
                  thisFile.fileName = fName;
                  thisFile.folder = "/private/" + setting.accountName + "/" + dataType.folderName;
                  thisFile.fullName = thisFile.folder + "/" + fName;
                
                  if (dlQueue.FirstOrDefault(a => a.fullName == thisFile.fullName) == null) dlQueue.Add(thisFile);
                }
              }
              vIdx++;
            }
            dtIdx++;
          }
          pIdx++;
        }
        sIdx++;
      }
      return dlQueue;
    }

    /// <summary>
    /// Get the file names for a specific day. It's historical, so we'll return the file names for any versions
    /// which should be available on the given date.
    /// </summary>
    /// <param name="histDT">Day to file historically</param>
    /// <returns>List of available file names on the IESO server</returns>
    public List<LCAEDIFileType> queueHistorical(DateTime sDT, DateTime eDT, List<DateTime> holidays)
    {
      int sIdx = 0;
      int pIdx = 0;
      int dtIdx = 0;
      int vIdx = 0;
      int curBestVer;
      List<LCAEDIFileType> dlQueue = new List<LCAEDIFileType>();

      foreach (LCAEDISettings setting in settings)
      {
        foreach (LCAEDIPoint point in setting.points)
        {
          foreach (LCAEDIPoint.LCAEDIDataType dataType in point.dataTypes)
          {
            int days = (eDT.Date - sDT.Date).Days + 1;
            DateTime curDate = sDT;

            for (int i = 0; i < days; i++) 
            {
              curDate = sDT.AddDays(i);
              curBestVer = 0;

              for (int j = 0; j < dataType.versions.Count(); j++)
              {
                if (dataType.versions[j].isActive)
                {
                  if (dataType.versions[j].versionNum > curBestVer)
                  {
                    LCAEDIFileType thisFile = new LCAEDIFileType();

                    DateTime releaseDate = LCATimeUtils.AddBusinessDays(curDate, -1 * dataType.versions[j].publishBDayLag, holidays);
                    releaseDate = releaseDate.AddDays(-1 * dataType.versions[j].publishDayLag);

                    if (releaseDate.Date <= DateTime.Now.Date)
                    {
                      curBestVer = dataType.versions[j].versionNum;
                      vIdx = j;
                    }
                  }
                }
              }

              if (curBestVer > 0)
              {
                string fName = dataType.versions[vIdx].makeFileName(dataType.fileNameStructure, curDate, setting.accountName);

                LCAEDIFileType thisFile = new LCAEDIFileType();

                thisFile.connection = setting.connection;
                thisFile.fileName = fName;
                thisFile.folder = "/private/" + setting.accountName + "/" + dataType.folderName;
                thisFile.fullName = thisFile.folder + "/" + fName;
                thisFile.localFileName = null;

                if (dlQueue.FirstOrDefault(a => a.fullName == thisFile.fullName) == null) dlQueue.Add(thisFile);
              }
            }
            dtIdx++;
          }
          pIdx++;
        }
        sIdx++;
      }
      return dlQueue;
    }

    /// <summary>
    /// Read the single specified config file
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public bool readConfigXML(string fileName)
    {
      bool bRet = false;
      string tempStr;

      LCAEDISettings thisConfig = new LCAEDISettings();

      // Read the XML configuration file
      try
      {
        XmlDocument xml = new XmlDocument();
        xml.Load(fileName);

        thisConfig.name = xml["EDICONFIG"]["CFGNAME"].InnerText;
        thisConfig.accountName = xml["EDICONFIG"]["MPSHORTNAME"].InnerText;

        thisConfig.connection = new LCAEDIConnection();
        tempStr = xml["EDICONFIG"]["CONNECTION"]["PROTOCOL"].InnerText;
        switch (tempStr.ToUpper())
        {
          case "SFTP":
            thisConfig.connection.protocol = LCAEDIConnection.EDIConnectionTypes.SFTP;
            break;
        }
        thisConfig.connection.server = xml["EDICONFIG"]["CONNECTION"]["SERVER"].InnerText;
        thisConfig.connection.user = xml["EDICONFIG"]["CONNECTION"]["USER"].InnerText;
        thisConfig.connection.pWord = xml["EDICONFIG"]["CONNECTION"]["PWORD"].InnerText;

        try
        {
          tempStr = xml["EDICONFIG"]["CONNECTION"]["USEPROXY"].InnerText;
          if (tempStr.ToUpper().StartsWith("Y"))
          {
            thisConfig.connection.useProxy = true;
            thisConfig.connection.proxyIP = xml["EDICONFIG"]["CONNECTION"]["PROXYIP"].InnerText;
            bRet = int.TryParse(xml["EDICONFIG"]["CONNECTION"]["PROXYPORT"].InnerText, out thisConfig.connection.proxyPort);
            thisConfig.connection.proxyUser = xml["EDICONFIG"]["CONNECTION"]["PROXYUSER"].InnerText;
            thisConfig.connection.proxyPass = xml["EDICONFIG"]["CONNECTION"]["PROXYPASS"].InnerText;
          }
          else thisConfig.connection.useProxy = false;
        }
        catch (Exception e)
        {
          thisConfig.connection.useProxy = false;
        }

        XmlNodeList points;
        points = xml.GetElementsByTagName("POINT");
        foreach (XmlNode point in points)
        {
          LCAEDIPoint thisPoint = new LCAEDIPoint();
          thisPoint.dataTypes = new List<LCAEDIPoint.LCAEDIDataType>();

          thisPoint.name = point["NAME"].InnerText;
          thisPoint.locationID = point["LOCID"].InnerText;
          thisPoint.channel = int.Parse(point["CHANNEL"].InnerText);
          thisPoint.lcaID = int.Parse(point["LCAID"].InnerText);

          XmlNodeList dataTypes = point.SelectNodes("DATATYPES/DATATYPE");
          foreach (XmlNode dataType in dataTypes)
          {
            LCAEDIPoint.LCAEDIDataType dt = new LCAEDIPoint.LCAEDIDataType();

            switch (dataType["NAME"].InnerText.ToUpper())
            {
              case "TMD":
                dt.dataType = LCAEDIPoint.LCAEDIDataType.EDIDataTypes.TMD;
                break;

              case "TMD-WL":
                dt.dataType = LCAEDIPoint.LCAEDIDataType.EDIDataTypes.TMD_WL;
                break;

              case "VMD":
                dt.dataType = LCAEDIPoint.LCAEDIDataType.EDIDataTypes.VMD;
                break;

              default:
                dt.dataType = LCAEDIPoint.LCAEDIDataType.EDIDataTypes.UNKNOWN;
                break;
            }

            XmlNodeList versions = dataType.SelectNodes("VERSIONS/VERSION");
            foreach (XmlNode ver in versions)
            {
              for (int i = 0; i < dt.versions.Count(); i++)
              {
                if (dt.versions[i].versionName == ver.InnerText.ToUpper())
                {
                  dt.versions[i].isActive = true;
                  break;
                }
              }
            }

            thisPoint.dataTypes.Add(dt);
          }

          thisConfig.points.Add(thisPoint);
        }

        settings.Add(thisConfig);
        bRet = true;
      }
      catch (Exception e)
      {
        bRet = false;
      }
      return bRet;
    }

    /// <summary>
    /// Reads all of the xml configuration files within a given folder
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public int readConfigFolder(string path)
    {
      List<string> files = Directory.EnumerateFiles(path, "*.xml", SearchOption.TopDirectoryOnly).ToList<string>();

      foreach (string file in files)
      {
        readConfigXML(file);
      }

      return settings.Count();
    }

    /// <summary>
    /// Returns a sample of what the EDI XML Config file should look like
    /// </summary>
    /// <returns></returns>
    public string getSampleFile()
    {
      StringBuilder sample = new StringBuilder();

      sample.AppendLine("<EDICONFIG>");
      sample.AppendLine(" <CFGNAME>gc-edi</CFGNAME>");
      sample.AppendLine(" <MPSHORTNAME>FALCONBRIDGE</MPSHORTNAME>");
      sample.AppendLine(" <CONNECTION>");
      sample.AppendLine("   <PROTOCOL>SFTP</PROTOCOL>");
      sample.AppendLine("   <SERVER>reports-sandbox2.ieso.ca</SERVER>");
      sample.AppendLine("   <USER>crosslp</USER>");
      sample.AppendLine("   <PWORD>Sm28jc16!</PWORD>");
      sample.AppendLine(" </CONNECTION>");
      sample.AppendLine(" <POINTS>");
      sample.AppendLine("  <POINT>");
      sample.AppendLine("     <NAME>d-100110-ONA</NAME>");
      sample.AppendLine("     <LOCID>100110</LOCID>");
      sample.AppendLine("     <CHANNEL>1</CHANNEL>");
      sample.AppendLine("     <LCAID>52</LCAID>");
      sample.AppendLine("     <DATATYPES>");
      sample.AppendLine("       <DATATYPE>");
      sample.AppendLine("         <NAME>TMD-WL</NAME>");
      sample.AppendLine("         <VERSIONS>");
      sample.AppendLine("           <VERSION>V1</VERSION>");
      sample.AppendLine("           <VERSION>V2</VERSION>");
      sample.AppendLine("           <VERSION>V3</VERSION>");
      sample.AppendLine("         </VERSIONS>");
      sample.AppendLine("       </DATATYPE>");
      sample.AppendLine("       <DATATYPE>");
      sample.AppendLine("         <NAME>TMD</NAME>");
      sample.AppendLine("         <VERSIONS>");
      sample.AppendLine("           <VERSION>V1</VERSION>");
      sample.AppendLine("           <VERSION>V2</VERSION>");
      sample.AppendLine("           <VERSION>V3</VERSION>");
      sample.AppendLine("         </VERSIONS>");
      sample.AppendLine("       </DATATYPE>");
      sample.AppendLine("       <DATATYPE>");
      sample.AppendLine("         <NAME>VMD</NAME>");
      sample.AppendLine("         <VERSIONS>");
      sample.AppendLine("           <VERSION>V1</VERSION>");
      sample.AppendLine("         </VERSIONS>");
      sample.AppendLine("       </DATATYPE>");
      sample.AppendLine("     </DATATYPES>");
      sample.AppendLine("   </POINT>");
      sample.AppendLine(" </POINTS>");
      sample.AppendLine("</EDICONFIG>");

      return sample.ToString();

    }
  }
}

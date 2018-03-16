using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LCAUtilsCL
{
  public class ParserSchedule
  {
    public bool ReadOK = false;

    public short Index = -1;
    public short StatusFlag = 0;
    public short Priority = 0;
    public double UnixTimeOfRequest = 0;
    public string DateOfReport = "";
    public string TimeOfReport = "";
    public string SettingFile = "";
    public string ResultFile = "";
    public short Report = 0;
    public int TypeID = 0;
    public string TypeCode = "";
    public string ConfigString = "";
  }
  public class BidSchedSettings
  {
    public bool readOK = false;

    public string Market;
    public string Resource;
    public string StandingFlag;
    public string DayType;
    public string ExpiryDate;
    public string Detail;
  }
  public class BidINISettings
  {
    public string manHostStr;
    public string manHostPort;
    public string dirHostStr;
    public string dirHostPort;
    public string webHostStr;
    public string webHostPort;
    public string epf;
    public string pw;
  }

  public class LCAParserSchedHandler
  {
    public const int REP_BID_REPORT = 12;
    public const int REP_BID_SUBMIT = 8;
    public const int REP_MRKT_MSGS = 14;

    public enum SchedFolderType
    {
      SCHED_NORMAL,
      SCHED_PRIORITY,
      SCHED_BOTH
    }

    public List<FileInfo> GetSchedFiles(string schedRootFolder, SchedFolderType folderType, string fileMask)
    {
      List<FileInfo> foundFiles = new List<FileInfo>();
      string rootSearchFolder;
      string[] searchFolder;
      string mask;

      try
      {
        mask = "*.req";
        if (fileMask != null)
        {
          if (fileMask != "") mask = fileMask;
        }

        rootSearchFolder = schedRootFolder.EndsWith("\\") ? schedRootFolder : schedRootFolder + "\\";
        switch (folderType)
        {
          case SchedFolderType.SCHED_NORMAL:
            searchFolder = new string[1];
            searchFolder[0] = rootSearchFolder;
            break;
          case SchedFolderType.SCHED_PRIORITY:
            searchFolder = new string[1];
            searchFolder[0] = rootSearchFolder + "PrioritySched\\";
            break;
          case SchedFolderType.SCHED_BOTH:
            searchFolder = new string[2];
            searchFolder[0] = rootSearchFolder;
            searchFolder[1] = rootSearchFolder + "PrioritySched\\";
            break;

          default:
            searchFolder = null;
            break;
        }

        foreach (string folder in searchFolder)
        {
          DirectoryInfo dir = new DirectoryInfo(folder);
          FileInfo[] files = dir.GetFiles(mask);

          foundFiles.AddRange(files);
        }
      }
      catch (Exception e)
      {
        foundFiles = null;
      }

      return foundFiles;
    }

    public BidINISettings readBidINISettings(FileInfo fi)
    {
      BidINISettings ini = null;

      return ini;
    }

    public BidSchedSettings readBidSettings(FileInfo fi)
    {
      BidSchedSettings settings = new BidSchedSettings();

      try
      {
        using (FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
          using (StreamReader br = new StreamReader(fs))
          {
            string readStr = br.ReadToEnd();
            if (readStr != "")
            {
              LCAConfigString cfgStr = new LCAConfigString(readStr);
              settings.Market = cfgStr.GetCfgStrParam("MARKET");
              settings.Resource = cfgStr.GetCfgStrParam("RESOURCE_ID");
              settings.StandingFlag = cfgStr.GetCfgStrParam("STANDING_FLAG");
              settings.DayType = cfgStr.GetCfgStrParam("DAY_TYPE");
              settings.ExpiryDate = cfgStr.GetCfgStrParam("EXPIRY_DATE");
              settings.Detail = cfgStr.GetCfgStrParam("DETAIL");
            }
          }
        }
      }
      catch (Exception e)
      {

        settings.readOK = false;
      }

      return settings;
    }

    public ParserSchedule readParserSchedFile(FileInfo fi)
    {
      ParserSchedule sched = new ParserSchedule();

      try
      {
        using (FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
          using (BinaryReader br = new BinaryReader(fs))
          {
            int strLen;

            sched.Index = br.ReadInt16();
            sched.StatusFlag = br.ReadInt16();
            sched.Priority = br.ReadInt16();
            sched.UnixTimeOfRequest = br.ReadDouble();
            strLen = br.ReadInt16();
            sched.DateOfReport = Encoding.UTF8.GetString(br.ReadBytes(strLen));
            strLen = br.ReadInt16();
            sched.TimeOfReport = Encoding.UTF8.GetString(br.ReadBytes(strLen));
            strLen = br.ReadInt16();
            sched.SettingFile = Encoding.UTF8.GetString(br.ReadBytes(strLen));
            strLen = br.ReadInt16();
            sched.ResultFile = Encoding.UTF8.GetString(br.ReadBytes(strLen));
            sched.Report = br.ReadInt16();
            sched.TypeID = br.ReadInt32();
            strLen = br.ReadInt16();
            sched.TypeCode = Encoding.UTF8.GetString(br.ReadBytes(strLen));
            strLen = br.ReadInt16();
            sched.ConfigString = Encoding.UTF8.GetString(br.ReadBytes(strLen));

            sched.ReadOK = true;
          }
        }
      }
      catch (Exception e)
      {
        sched.ConfigString = e.Message;
      }

      return sched;
    }
  }
}

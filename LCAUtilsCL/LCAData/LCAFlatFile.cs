using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LCAUtilsCL.LCAData
{
  public class LCAFlatFile
  {
    public enum FlatFileTypeEnum
    {
      FF_ONEMIN,
      FF_FIVEMIN,
      FF_TENSEC,
      FF_TENSECRAW,
      FF_ONEMINPRED
    }

    public class IntervalData
    {
      public double value;
      public LCAUnixTime recordUnix;

      public IntervalData(double val, LCAUnixTime unix)
      {
        value = val;
        recordUnix = unix;
      }
    }

    private string m_DataPath;
    
    public LCAFlatFile(string dataPath)
    {
      m_DataPath = dataPath;
      if (!m_DataPath.EndsWith("\\")) m_DataPath += "\\";
    }

    private string getFileName(FlatFileTypeEnum interval, long pointID, bool isInternal, DateTime dt)
    {
      string fName;

      fName = m_DataPath;
      fName += dt.ToString("yyyyMM") + "data\\";

      try { Directory.CreateDirectory(fName); }
      catch (Exception e)
      {
        // TODO : handle directory could not be created 
        Console.Write(e.Message);
      }


      fName += dt.ToString("yyyyMM") + pointID.ToString();

      switch (interval)
      {
        case FlatFileTypeEnum.FF_ONEMIN:        // One minute file
          fName += "One Minute";
          break;
        
        case FlatFileTypeEnum.FF_ONEMINPRED:        // One minute control prediction file
          if (isInternal) fName += "Internal Pred";
          else fName += "Pred";
          
          break;

        case FlatFileTypeEnum.FF_FIVEMIN:        // Five minute file
          fName += "Five Minute";
          break;
        
        case FlatFileTypeEnum.FF_TENSEC:         // 10 second File
          fName += "Ten Second";
          break;
        
        case FlatFileTypeEnum.FF_TENSECRAW:        // Raw File
          fName += "One Minute Raw";
          break;
      }

      if ((isInternal) && (interval != FlatFileTypeEnum.FF_ONEMINPRED)) fName += " Internal";

      return fName + ".dat";
    }

    private void createFile(string fName, int recCount)
    {
      double val = -1;

      try
      {
        FileInfo finfo = new FileInfo(fName);

        if (!finfo.Exists)
        {
          FileStream fs = new FileStream(fName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite);
          BinaryWriter bw = new BinaryWriter(fs);

          for (int i = 0; i < recCount; i++)
          {
            bw.Write(val);
          }

          bw.Flush();
          fs.Flush();
          fs.Close();
        }
        else
        {
          long diff = (finfo.Length / 8) - (recCount);

          if (diff < 0)
          {
            FileStream fs = new FileStream(fName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            BinaryWriter bw = new BinaryWriter(fs);

            for (int i = 0; i < Math.Abs(diff); i++)
            {
              bw.Write(val);
            }

            bw.Flush();
            fs.Flush();
            fs.Close();
          }
        }
      }
      catch (Exception e)
      {
        Console.Write("Error creating flat file : " + e.Message);
      }
    }

    public bool writeFlatFile(string fileName, LCAUnixTime sUnix, List<double> data)
    {
      try
      {
        FlatFileTypeEnum interval = FlatFileTypeEnum.FF_ONEMIN;
        long pointID = 1;
        double count = 1;
        bool isRaw = false;
        bool done = false;
        string temp = "";
        int pos = 0;
        string fileNamePortion;
        string tempDate;
        string thisChar;
        bool isInternal = false;

        fileNamePortion = fileName.Substring(fileName.LastIndexOf("\\")).ToUpper();

        if (fileNamePortion.Contains("ONE MINUTE")) interval = FlatFileTypeEnum.FF_ONEMIN;
        else if (fileNamePortion.Contains("FIVE MINUTE")) interval = FlatFileTypeEnum.FF_FIVEMIN;
        else if (fileNamePortion.Contains("TEN SECOND")) interval = FlatFileTypeEnum.FF_TENSEC;

        if (fileNamePortion.Contains("ONE MINUTE RAW")) isRaw = true;

        if (fileNamePortion.Contains("INTERNAL")) isInternal = true;


        pos = 7;
        while (!done)
        {
          thisChar = fileNamePortion.Substring(pos, 1);

          try
          {
            int.Parse(thisChar);
            temp += thisChar;
          }
          catch
          {
            done = true;
          }

          pos++;
          if (pos > fileNamePortion.Length) done = true;
        }
        try
        {
          pointID = int.Parse(temp);
        }
        catch { }

        return writeFlatFile(interval, pointID, isInternal, sUnix.AsDateTime, data, isRaw);
      }
      catch (Exception e)
      {
        return false;
      }
    }

    public bool writeFlatFile(FlatFileTypeEnum interval, long pointID, bool isInternal, DateTime sDateTime, List<double> data, bool isRaw)
    {
      bool bRet = true;
      string fName;
      int recInFile;
      int curRec;
      DateTime curDateTime;
      DateTime lastDateTime;
      int secPerRec = 60;

      try
      {
        if (!isRaw) fName = getFileName(interval, pointID, isInternal, sDateTime);
        else fName = getFileName(FlatFileTypeEnum.FF_TENSECRAW, pointID, isInternal, sDateTime);

        switch (interval)
        {
          case FlatFileTypeEnum.FF_ONEMIN:
          case FlatFileTypeEnum.FF_ONEMINPRED :
            secPerRec = 60;
            if (sDateTime.Second != 0) sDateTime = sDateTime.AddSeconds(sDateTime.Second * -1);
            break;
          case FlatFileTypeEnum.FF_FIVEMIN:
            secPerRec = 300;
            if (sDateTime.Second != 0) sDateTime = sDateTime.AddSeconds(sDateTime.Second * -1);
            if (sDateTime.Minute % 5 != 0)
            {
              sDateTime = sDateTime.AddMinutes(5 - (sDateTime.Minute % 5));
            }
            break;
          case FlatFileTypeEnum.FF_TENSEC:
            secPerRec = 10;
            if (sDateTime.Second % 10 != 0)
            {
              sDateTime = sDateTime.AddSeconds(10 - (sDateTime.Second % 10));
            }
            break;
        }

        recInFile = DateTime.DaysInMonth(sDateTime.Year, sDateTime.Month) * (86400 / secPerRec);
        createFile(fName, recInFile);

        lastDateTime = sDateTime;
        curDateTime = sDateTime;

        FileStream fs = new FileStream(fName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
        BinaryWriter bw = new BinaryWriter(fs);

        curRec = calcRecordNum(interval, sDateTime);
        bw.Seek(curRec * 8, SeekOrigin.Begin);

        lastDateTime = sDateTime;

        bool midnightRoll = false;

        for (int i = 0; i < data.Count(); i++)
        {
          if (curDateTime.Month != lastDateTime.Month)
          {
            // The month has changed so we nee to get the next month's file
            bw.Flush();
            fs.Flush();
            bw.Close();

            fName = getFileName(interval, pointID, isInternal, curDateTime);
            createFile(fName, recInFile);

            fs = new FileStream(fName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            bw = new BinaryWriter(fs);
          }

          bw.Write(data.ElementAt(i));

          lastDateTime = curDateTime;
          curDateTime = curDateTime.AddSeconds(secPerRec);
          if (midnightRoll == true)
          {
            curDateTime = curDateTime.AddDays(1);
            midnightRoll = false;
          }

          if ((curDateTime.Hour == 0) && (curDateTime.Minute == 0))
          {
            curDateTime = curDateTime.AddDays(-1);
            midnightRoll = true;
          }
        }

        bw.Flush();
        fs.Flush();
        bw.Close();
      }
      catch (Exception e)
      {
        Console.Write("Error writing flat file: " + e.Message);
        bRet = false;
      }

      return bRet;
    }


    public int calcRecordNum(FlatFileTypeEnum interval, DateTime sDateTime)
    {
      int rec = 0;

      if ((sDateTime.Hour == 0) && (sDateTime.Minute == 0) && (sDateTime.Second == 0))
      {
        switch (interval)
        {
          case FlatFileTypeEnum.FF_ONEMIN:
          case FlatFileTypeEnum.FF_ONEMINPRED:
            rec = (sDateTime.Day) * 1440; // Get the last record of the current day
            break;
          case FlatFileTypeEnum.FF_FIVEMIN:
            rec = (sDateTime.Day) * 288; // Get the last record of the current day
            break;
          case FlatFileTypeEnum.FF_TENSEC:
            rec = (sDateTime.Day) * 8640; // Get the last record of the current day
            break;
        }
      }
      else
      {
        switch (interval)
        {
          case FlatFileTypeEnum.FF_ONEMIN:
          case FlatFileTypeEnum.FF_ONEMINPRED:
            rec = ((sDateTime.Day - 1) * 1440) + ((sDateTime.Hour) * 60) + sDateTime.Minute;
            break;
          case FlatFileTypeEnum.FF_FIVEMIN:
            rec = ((sDateTime.Day - 1) * 288) + ((sDateTime.Hour) * 12) + (sDateTime.Minute / 5);
            break;
          case FlatFileTypeEnum.FF_TENSEC:
            rec = ((sDateTime.Day - 1) * 8640) + ((sDateTime.Hour) * 360) + (sDateTime.Minute * 6) + (sDateTime.Second / 10);
            break;
        }
      }
      rec--;

      return rec;
    }

    public List<double> readDataFromFlatFile(string fileName, LCAUnixTime sUnix, LCAUnixTime eUnix)
    {
      try {
        FlatFileTypeEnum interval = FlatFileTypeEnum.FF_ONEMIN;
        long pointID = 1;
        double count = 1;
        bool isRaw = false;
        bool done = false;
        string temp = "";
        int pos = 0;
        string fileNamePortion;
        string tempDate;
        string thisChar;
        bool isInternal = false;

        fileNamePortion = fileName.Substring(fileName.LastIndexOf("\\")).ToUpper();

        if (fileNamePortion.Contains("ONE MINUTE")) interval = FlatFileTypeEnum.FF_ONEMIN;
        else if (fileNamePortion.Contains("FIVE MINUTE")) interval = FlatFileTypeEnum.FF_FIVEMIN;
        else if (fileNamePortion.Contains("TEN SECOND")) interval = FlatFileTypeEnum.FF_TENSEC;

        if (fileNamePortion.Contains("ONE MINUTE RAW")) isRaw = true;

        if (fileNamePortion.Contains("INTERNAL")) isInternal = true;


        pos = 7;
        while (!done)
        {
          thisChar = fileNamePortion.Substring(pos, 1);

          try
          {
            int.Parse(thisChar);
            temp += thisChar;
          }
          catch
          {
            done = true;
          }

          pos++;
          if (pos > fileNamePortion.Length) done = true;
        }
        try
        {
          pointID = int.Parse(temp);
        }
        catch { }

        long seconds = eUnix.AsUnixTime - sUnix.AsUnixTime;

        switch (interval)
        {
          case FlatFileTypeEnum.FF_ONEMIN:
            count = seconds / 60;
            break;
          case FlatFileTypeEnum.FF_FIVEMIN:
            count = seconds / 300;
            break;
          case FlatFileTypeEnum.FF_TENSEC:
            count = seconds / 10;
            break;
        }
        count++;

        return readDataFromFlatFile(interval, pointID, isInternal, sUnix.AsDateTime, count, isRaw, fileName);
      }
      catch (Exception e)
      {
        return new List<double>();
      }
    }

    public void createFiveMinFromOneMin(LCAUnixTime fiveSUnix, LCAUnixTime fiveEUnix, long pointID, bool isInternal)
    {
      double[] fiveData;
      List<double> oneData = new List<double>();
      long oneCount;
      long fiveCount;
      long fivePos = 0;
      int onePos = 0;
      
      fiveSUnix.setToNearestInterval(5, 0);
      fiveEUnix.setToNearestInterval(5, 0);

      oneCount = ((fiveEUnix.AsUnixTime - fiveSUnix.AsUnixTime) / 60) + 1;

      fiveCount = (((fiveEUnix.AsUnixTime - fiveSUnix.AsUnixTime) / 60) / 5) + 1;
      fiveData = new double[fiveCount];

      oneData = readDataFromFlatFile(FlatFileTypeEnum.FF_ONEMIN, pointID, isInternal, fiveSUnix.AsDateTime, oneCount, false);

      if (oneData.Count != null)
      {
        for (int i = 0; i < 5; i++)
        {
          fiveData[fivePos] += oneData[onePos];
          onePos++;
        }
        fivePos++;
      }

      writeFlatFile(FlatFileTypeEnum.FF_FIVEMIN, pointID, isInternal, fiveSUnix.AsDateTime, fiveData.ToList<double>(), false);
    }

    public List<IntervalData> readHourlyDataFromFF(FlatFileTypeEnum interval, long pointID, bool isInternal, double multiplier,  DateTime sDateTime, DateTime eDateTime)
    {
      int curPos, numDays, intPerDay, intPerHour = 0;
      double sum;
      List<double> intData = new List<double>();
      List<IntervalData> data = new List<IntervalData>();
      LCAUnixTime curUnix = new LCAUnixTime(sDateTime);

      TimeSpan ts = (eDateTime.Date - sDateTime.Date);
      
      numDays = ts.Days + 1;

      switch (interval)
      {
        case FlatFileTypeEnum.FF_ONEMIN :
          curUnix = new LCAUnixTime(new DateTime(sDateTime.Year, sDateTime.Month, sDateTime.Day, 0, 1, 0));
          intPerHour = 60;
          intPerDay = 24 * 60;
          break;
        
        case FlatFileTypeEnum.FF_FIVEMIN:
          curUnix = new LCAUnixTime(new DateTime(sDateTime.Year, sDateTime.Month, sDateTime.Day, 0, 5, 0));
          intPerHour = 12;
          intPerDay = 24 * 12;
          break;
        
        case FlatFileTypeEnum.FF_TENSEC:
          intPerHour = 360;
          curUnix = new LCAUnixTime(new DateTime(sDateTime.Year, sDateTime.Month, sDateTime.Day, 0, 0, 10));
          intPerDay = 24 * 360;
          break;
        
        default:
          intPerDay = -1;
          numDays = -1;
          break;
      }

      for (int i = 0; i < numDays; i++)
      {
        // Read the interval data
        curPos = 0;
        intData = readDataFromFlatFile(interval, pointID, isInternal, curUnix.AsDateTime, intPerDay, false);
        
        // sum up the hours
        for (int j = 1; j <= 24; j++)
        {
          sum = 0;
          int hour = j;
          if (hour == 24) hour = 0;

          LCAUnixTime hourUnix = new LCAUnixTime(new DateTime(curUnix.AsDateTime.Year, curUnix.AsDateTime.Month, curUnix.AsDateTime.Day, hour, 0, 0));

          for (int k = 0; k < intPerHour; k++)
          {
            if (intData[curPos] != -1) sum += intData[curPos];
            curPos++;
          }
          sum *= multiplier;
          data.Add(new IntervalData(sum, hourUnix));
        }

        curUnix.addHours(24);
      }
      
      return data;
    }

    private List<double> readDataFromFlatFile(FlatFileTypeEnum interval, long pointID, bool isInternal, DateTime sDateTime, double numRecToRead, bool isRaw, string fixedFileName)
    {
      try
      {
        string fName;
        int curRec;
        int secPerRec = 60;
        DateTime lastDateTime;
        DateTime curDateTime;
        int recInFile;

        List<double> data = new List<double>();

        if (fixedFileName == null)
        {
          if (!isRaw) fName = getFileName(interval, pointID, isInternal, sDateTime);
          else fName = getFileName(FlatFileTypeEnum.FF_TENSECRAW, pointID, isInternal, sDateTime);
        }
        else fName = fixedFileName;

        switch (interval)
        {
          case FlatFileTypeEnum.FF_ONEMIN:
          case FlatFileTypeEnum.FF_ONEMINPRED:
            secPerRec = 60;
            if (sDateTime.Second != 0) sDateTime = sDateTime.AddSeconds(sDateTime.Second * -1);
            break;
          case FlatFileTypeEnum.FF_FIVEMIN:
            secPerRec = 300;
            if (sDateTime.Second != 0) sDateTime = sDateTime.AddSeconds(sDateTime.Second * -1);
            if (sDateTime.Minute % 5 != 0)
            {
              sDateTime = sDateTime.AddMinutes(5 - (sDateTime.Minute % 5));
            }
            break;
          case FlatFileTypeEnum.FF_TENSEC:
            secPerRec = 10;
            if (sDateTime.Second % 10 != 0)
            {
              sDateTime = sDateTime.AddSeconds(10 - (sDateTime.Second % 10));
            }
            break;
        }

        recInFile = DateTime.DaysInMonth(sDateTime.Year, sDateTime.Month) * (86400 / secPerRec);
        createFile(fName, recInFile);

        curRec = calcRecordNum(interval, sDateTime);
        if (curRec == -1) { throw new IndexOutOfRangeException("Record could not be located"); }

        lastDateTime = sDateTime;
        curDateTime = sDateTime;

        FileStream fs = new FileStream(fName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        BinaryReader br = new BinaryReader(fs);

        br.BaseStream.Seek((curRec * 8), SeekOrigin.Begin);

        bool midnightRoll = false;

        for (int i = 0; i < numRecToRead; i++)
        {
          lastDateTime = curDateTime;

          data.Add(br.ReadDouble());

          if (i == 280)
          {
            Console.Write("");
          }

          if (midnightRoll)
          {
            curDateTime = curDateTime.AddDays(1);
            midnightRoll = false;
          }

          curDateTime = curDateTime.AddSeconds(secPerRec);
          if ((curDateTime.Hour == 0) && (curDateTime.Minute == 0))
          {
            curDateTime = curDateTime.AddDays(-1);
            midnightRoll = true;
          }
          if (curDateTime.Month != lastDateTime.Month)
          {
            // The month has changed so we nee to get the next month's file
            br.Close();
            fs.Close();

            fName = getFileName(interval, pointID, isInternal, curDateTime);
            createFile(fName, recInFile);

            fs = new FileStream(fName, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
            br = new BinaryReader(fs);
          }

        }

        return data;
      }
      catch (Exception e)
      {
        LCAFileUtils.logErrorDetails("readDataFromFlatFile", "LCAFlatFileErr.txt", e);

        return new List<double>();
      }
    }

    public List<double> readDataFromFlatFile(FlatFileTypeEnum interval, long pointID, bool isInternal, DateTime sDateTime, double numRecToRead, bool isRaw)
    {
      return readDataFromFlatFile(interval, pointID, isInternal, sDateTime, numRecToRead, isRaw, null);
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LCAUtilsCL.EDI
{
  public class LCAEDIData
  {
    public enum EDIDataTypeEnum
    {
      /// <summary>
      /// Any unhandled data type
      /// </summary>
      EDIDATA_UNKNOWN,
      /// <summary>
      /// kWh
      /// </summary>
      EDIDATA_KH, 
      /// <summary>
      /// Kilovolt Amperes Reactive hours
      /// </summary>
      EDIDATA_K3,
      /// <summary>
      /// Amperes (Ampere squared hours)
      /// </summary>
      EDIDATA_68,
      /// <summary>
      /// Volts (Volt Squared Hours)
      /// </summary>
      EDIDATA_70
    }

    /// <summary>
    /// Number of minutes between records (from the *REF*MT*xxxx* line)
    /// </summary>
    public int dataInterval = 0;
    /// <summary>
    /// Type of data, from the *REF*MT*xxxxx* line
    /// </summary>
    public EDIDataTypeEnum dataType;
    /// <summary>
    /// Start date & time from the DTM*150****DT*yyyymmddHHmm line 
    /// </summary>
    public LCAUnixTime startUnix;
    /// <summary>
    /// End date & time from the DTM*151****DT*yyyymmddHHmm line 
    /// </summary>
    public LCAUnixTime endUnix;
    /// <summary>
    /// The data values 
    /// </summary>
    public List<double> values = new List<double>();
    public string dataQuality;

    /// <summary>
    /// Calculates the date/time of a record based on its index. Return a date of 1990/01/01 00:00:00 if something is wrong
    /// </summary>
    /// <param name="idx"></param>
    /// <returns></returns>
    public LCAUnixTime getDTFromIDX(int idx)
    {
      if (startUnix != null)
      {
        LCAUnixTime retUnix = new LCAUnixTime(startUnix.AsUnixTime);

        if ((idx >= 0) && (idx <= values.Count()))
        {
          if (dataInterval > 0)
          {
            try
            {
              int numMins = (idx * dataInterval);
              retUnix.addMinutes(numMins);
            }
            catch (Exception e)
            {
              retUnix = null;
            }
          }
        }

        return retUnix;
      }
      else
      {
        return null;
      }
    }
  }

  public class LCAEDIChannel
  {
    /// <summary>
    /// Channel number from the REF*6W*1* line
    /// </summary>
    public int channelNum;
    /// <summary>
    /// Data contained in this channel
    /// </summary>
    public LCAEDIData data = new LCAEDIData();
  }

  public class LCAEDIMeter
  {
    /// <summary>
    /// Location ID from the REF*LU*xxxxx* line. Normally we use this to identify the meter
    /// </summary>
    public string locationID;
    /// <summary>
    /// Meter ID from the REF*MG*xxxxx* line. Included for possible future needs?
    /// </summary>
    public string meterID;

    public List<LCAEDIChannel> channels = new List<LCAEDIChannel>();
  }

  public class LCAEDIFile
  {
    public enum EDIFileTypeEnum
    {
      /// <summary>
      /// Totalized meter data file
      /// </summary>
      EDIFILE_TMD,
      /// <summary>
      /// Totalized meter data with losses file
      /// </summary>
      EDIFILE_TMD_WL,
      /// <summary>
      /// Verified meter data file
      /// </summary>
      EDIFILE_VMD
    }

    /// <summary>
    /// Full filename with path to the EDI file we're loading
    /// </summary>
    public string fileName;
    /// <summary>
    /// Type of file we're dealing with
    /// </summary>
    public EDIFileTypeEnum fileType;
    /// <summary>
    /// The version of the data file (from the _v1 portion of the file name)
    /// </summary>
    public int fileVersion;

    public int      intervalMins;

    public List<LCAEDIMeter> meters = new List<LCAEDIMeter>();
  }

  public class LCAEDIParser
  {
    public LCAEDIFile EDI = new LCAEDIFile();

    /// <summary>
    /// Searches for a location ID in the meters list. Adds a new entry if it doesn't exist. Returns the index of the meter object
    /// </summary>
    /// <param name="locationID">Location ID from the *REF*LU line</param>
    /// <returns></returns>
    private int getOrAddMeter(string locationID)
    {
      int idx = -1;

      for (int i = 0; i < EDI.meters.Count(); i++) 
      {
        if (EDI.meters[i].locationID == locationID)
        {
          // We found the meter
          idx = i;
          break;
        }
      }
      
      if (idx == -1)
      {
        // The meter was not found, so we need to add it
        LCAEDIMeter newMeter = new LCAEDIMeter();

        newMeter.locationID = locationID;
        newMeter.channels = new List<LCAEDIChannel>();
        newMeter.meterID = "";

        EDI.meters.Add(newMeter);
        idx = EDI.meters.Count() - 1;
      }
      return idx;
    }

    private int getOrAddMeterChannel(int meterIDX, int channel)
    {
      int idx = -1;

      for (int i = 0; i < EDI.meters[meterIDX].channels.Count(); i++)
      {
        if (EDI.meters[meterIDX].channels[i].channelNum == channel)
        {
          // We found the channel
          idx = i;
          break;
        }
      }

      if (idx == -1)
      {
        // The meter was not found, so we need to add it
        LCAEDIChannel newC = new LCAEDIChannel();

        newC.channelNum = channel;
        newC.data = new LCAEDIData();
        newC.data.values = new List<double>();

        EDI.meters[meterIDX].channels.Add(newC);
        idx = EDI.meters[meterIDX].channels.Count() - 1;
      }
      return idx;
    }

    /// <summary>
    /// Splits an EDI Line into it's individual parameters and return the value at the index specified
    /// </summary>
    /// <param name="ediLine">The EDI data line</param>
    /// <param name="paramNum">The zero based parameter index</param>
    /// <returns></returns>
    private string getEDIParam(string ediLine, int paramNum)
    {
      string value = "";
      string[] split;

      split = ediLine.Split('*');

      if (split.Count() >= paramNum)
      {
        value = split[paramNum];
      }

      return value;
    }

    /// <summary>
    /// Searches an EDI section for a line starting with the specified string
    /// </summary>
    /// <param name="section">Full text of the EDI section</param>
    /// <param name="startPos">Character position to start the search at</param>
    /// <param name="startsWith">The string we are searching for</param>
    /// <param name="lineStartIDX">Return value of the start position found</param>
    /// <param name="lineEndIDX">Return value of the end position found</param>
    /// <returns></returns>
    private string findEDISectionLine(string section, int startPos, string startsWith, out int lineStartIDX, out int lineEndIDX)
    {
      string retLine = "";
      int sIdx;
      int eIdx = 0;

      sIdx = section.IndexOf(startsWith, startPos);
      if (sIdx >= 0)
      {
        eIdx = section.IndexOf("\r\n", sIdx);

        if (eIdx >= 0)
        {
          retLine = section.Substring(sIdx, eIdx - sIdx);
          eIdx += 2;
        }
        else
        {
          retLine = section.Substring(sIdx);
        }

      }

      lineStartIDX = sIdx;
      lineEndIDX = eIdx;

      return retLine;
    }

    private bool parseEDISection(string section)
    {
      int mIDX = -1;
      int cIDX = -1;
      int channel;
      string curLocID;
      string dataLine;
      bool bRet;
      bool datesOK;
      int sIDX, eIDX;

      try 
      {
        // we need to get the identifying information out of the headers before we move on to parsing the actual data
      
        // Find the REF*LU line to get the location ID
        dataLine = findEDISectionLine(section, 0, "REF*LU", out sIDX, out eIDX);
        if (dataLine.Length > 0) 
        {
          // This is the location id
          curLocID = getEDIParam(dataLine, 2);
          if (curLocID.Length > 0)
          {
            mIDX = getOrAddMeter(curLocID);
          }
        }
        // If we didnt' find a location identifier, we'll just get out of here and not parse anything else
        if (mIDX == -1) return false;

        // The CHANNEL number line
        dataLine = findEDISectionLine(section, 0, "REF*6W", out sIDX, out eIDX);
        if (dataLine.Length > 0) 
        {
          bRet = int.TryParse(getEDIParam(dataLine, 2), out channel);
          if (bRet == true) 
          {
            cIDX = getOrAddMeterChannel(mIDX, channel);
          }
        }
        if (cIDX == -1) return false;

        // The METER ID line 
        dataLine = findEDISectionLine(section, 0, "REF*MG", out sIDX, out eIDX);
        if (dataLine.Length > 0) 
        {
          EDI.meters[mIDX].meterID = getEDIParam(dataLine, 2);
        }

        // The data type line
        dataLine = findEDISectionLine(section, 0, "REF*MT", out sIDX, out eIDX);
        if (dataLine.Length > 0) 
        {
          string dataType = getEDIParam(dataLine, 2);
          if (dataType.Length > 0) 
          {
            string typePortion = dataType.Substring(0, 2);
            string intPortion = dataType.Substring(2, 3);

            switch (typePortion)
            {
              case "KH":
                EDI.meters[mIDX].channels[cIDX].data.dataType = LCAEDIData.EDIDataTypeEnum.EDIDATA_KH;
                break;
              case "K3":
                EDI.meters[mIDX].channels[cIDX].data.dataType = LCAEDIData.EDIDataTypeEnum.EDIDATA_K3;
                break;
              case "68":
                EDI.meters[mIDX].channels[cIDX].data.dataType = LCAEDIData.EDIDataTypeEnum.EDIDATA_68;
                break;
              case "70":
                EDI.meters[mIDX].channels[cIDX].data.dataType = LCAEDIData.EDIDataTypeEnum.EDIDATA_70;
                break;
              default :
                EDI.meters[mIDX].channels[cIDX].data.dataType = LCAEDIData.EDIDataTypeEnum.EDIDATA_UNKNOWN;
                break;
            }

            bRet = int.TryParse(intPortion, out EDI.meters[mIDX].channels[cIDX].data.dataInterval);
            if (bRet == false) EDI.meters[mIDX].channels[cIDX].data.dataInterval = 0;
          }
          if (EDI.meters[mIDX].channels[cIDX].data.dataInterval == 0) return false;

          datesOK = true;

          LCAEDIData dataRef = EDI.meters[mIDX].channels[cIDX].data;

          // The data quality
          dataLine = findEDISectionLine(section, 0, "MEA**MU", out sIDX, out eIDX);
          if (dataLine.Length > 0)
          {
            string quality = getEDIParam(dataLine, 7);
            EDI.meters[mIDX].channels[cIDX].data.dataQuality = quality;
          }

          // Get the start date / time
          dataLine = findEDISectionLine(section, 0, "DTM*151", out sIDX, out eIDX);
          if (dataLine.Length > 0)
          {
            try
            {
              dataRef.startUnix = new LCAUnixTime(DateTime.ParseExact(getEDIParam(dataLine, 6), "yyyyMMddHHmm", null));
              //dataRef.startUnix.addMinutes(dataRef.dataInterval);
            }
            catch (Exception e) 
            {
              datesOK = false;
            }
          }

          double val;

          eIDX = 0;
          do
          {
            // Find the next QTY record
            dataLine = findEDISectionLine(section, eIDX, "QTY*", out sIDX, out eIDX);
            if (dataLine.Length > 0)
            {
              bRet = double.TryParse(getEDIParam(dataLine, 2), out val);
              if (bRet) dataRef.values.Add(val);
              
            }
          } while (sIDX >= 0);

          dataRef.endUnix = new LCAUnixTime(dataRef.startUnix.AsUnixTime);
          dataRef.endUnix.addMinutes(dataRef.dataInterval * (dataRef.values.Count() - 1));
        }
        return true;
      }
      catch (Exception e) 
      {
        return false;
      }

    }

    public bool findMeterAndChannel(string locID, int channel, out int meterIDX, out int channelIDX)
    {
      bool bRet = false;

      meterIDX = -1;
      channelIDX = -1;

      for (int i = 0; i < EDI.meters.Count(); i++)
      {
        if (EDI.meters[i].locationID == locID)
        {
          for (int j = 0; j < EDI.meters[i].channels.Count(); j++)
          {
            if (EDI.meters[i].channels[j].channelNum == channel)
            {
              meterIDX = i;
              channelIDX = j;
              bRet = true;
              break;
            }
          }
        }
        
        if (bRet) break;
      }

      return bRet;
    }

    /// <summary>
    /// Load the data from an EDI file
    /// </summary>
    /// <param name="fileName">Full file path</param>
    public bool readEDIFile(string fileName)
    {
      bool bRet = false;

      try
      {
        StringBuilder sb = new StringBuilder();
        string ediSection = "";
        string dataLine;

        using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
          using (StreamReader sr = new StreamReader(fs))
          {
            while (!sr.EndOfStream)
            {
              dataLine = sr.ReadLine();

              // First we're looking for a PTD*PM***OZ*EL line to indicate the start of a data section
              if (dataLine.StartsWith("REF*6W"))
              {
                ediSection = "";

                sb = new StringBuilder();

                sb.AppendLine(dataLine);

                while (!sr.EndOfStream) 
                {
                  dataLine = sr.ReadLine();
                  if (dataLine != null) 
                  {
                    sb.AppendLine(dataLine);
                  }
                  if ((dataLine == "PTD*PM***OZ*EL") || (sr.EndOfStream == true))
                  {
                    // This section is done
                    ediSection = sb.ToString();
                    break;
                  }
                }
              }

              if (ediSection != "")
              {
                parseEDISection(ediSection);
                bRet = true;
              }
            }

            sr.Close();
          }
          
          fs.Close();
        }
      }
      catch (Exception e)
      {
        bRet = false;
      }
      
      return bRet;
    }

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace LCAUtilsCL
{

  public class LCAGeneralUtil
  {
    public static void GetHourIntervalFromTime(DateTime inDT, ref int hour, ref int interval)
    {
      // Round to the next 5 minute mark
      if (inDT.Minute % 5 != 0)
      {
        inDT = inDT.AddMinutes(5 - (inDT.Minute % 5));
      }

      if (inDT.Minute != 0)
      {
        hour = inDT.Hour + 1;
        interval = inDT.Minute / 5;
      }
      else
      {
        hour = inDT.Hour;
        interval = 12;
      }
      if (hour == 0) hour = 24;
    }

    public static string GetTimeFromHourInterval(int hour, int interval)
    {
      string retTime = "";

      if (interval == 12 || interval == 0)
      {
        if (hour == 24) hour = 0;

        retTime = hour.ToString("00") + ":00:00";
      }
      else
      {
        int mins = interval * 5;
        hour = hour - 1;

        retTime = hour.ToString("00") + ":" + mins.ToString("00") + ":00";
      }

      return retTime;
    }
  }

  /// <summary>
  /// LCA Unixtime representation of a DateTime
  /// Can convert from DateTime or to DateTime
  /// </summary>
  public class LCAUnixTime
  {
    private long _AsUnixSeconds;
    public long AsUnixSeconds
    {
      get
      {
        return _AsUnixSeconds;
      }
    }

    private long _AsUnixTime;
    public long AsUnixTime
    {
      get
      {
        return _AsUnixTime;
      }
      set
      {
        _AsUnixTime = value;
        _AsDateTime = UnixToDateTime(value);
      }
    }

    private DateTime _AsDateTime;
    public DateTime AsDateTime
    {
      get
      {
        return _AsDateTime;
      }
      set
      {
        _AsDateTime = value;
        _AsUnixTime = DateTimeToUnix(value);
      }
    }

    public static bool operator ==(LCAUnixTime x, LCAUnixTime y)
    {
      if (object.ReferenceEquals(x, y))
      {
        return true;
      }
      if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
      {
        return false;
      }
      return x.AsUnixTime == y.AsUnixTime;
    }

    public static bool operator !=(LCAUnixTime x, LCAUnixTime y)
    {
      if (object.ReferenceEquals(x, y))
      {
        return false;
      }
      if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
      {
        return true;
      }
      return x.AsUnixTime != y.AsUnixTime;
    }

    public bool Equals(LCAUnixTime other)
    {
      return this == other;
    }

    public override bool Equals(object obj)
    {
      // Delegate...
      return Equals(obj as LCAUnixTime);
    }

    public static TimeSpan operator -(LCAUnixTime x, LCAUnixTime y)
    {
      if ((x.AsDateTime.Hour == 0) && (x.AsDateTime.Minute == 0))
      {
        x = new LCAUnixTime(x.AsDateTime.AddDays(1));
      }

      TimeSpan result = (x.AsDateTime - y.AsDateTime);

      return result;
    }

    public void addHours(int hours)
    {
      AsUnixTime += (hours * 3600);
    }
    public void addMinutes(int minutes)
    {
      AsUnixTime += (minutes * 60);
    }
    public void addSeconds(int seconds)
    {
      AsUnixTime += (seconds);
    }

    public void setToFirstSecOfHour()
    {
      AsDateTime = new DateTime(AsDateTime.Year, AsDateTime.Month, AsDateTime.Day, AsDateTime.Hour, 0, 1);
    }
    public void setToFirstMinOfHour()
    {
      AsDateTime = new DateTime(AsDateTime.Year, AsDateTime.Month, AsDateTime.Day, AsDateTime.Hour, 1, 0);
    }
    public void setToFirstFiveMinOfHour()
    {
      AsDateTime = new DateTime(AsDateTime.Year, AsDateTime.Month, AsDateTime.Day, AsDateTime.Hour, 5, 0);
    }
    public void setToEndOfHourEnding()
    {
      DateTime tempDT = AsDateTime;

      tempDT = tempDT.AddHours(1);
      if (tempDT.Hour == 0) tempDT = tempDT.AddDays(-1);

      AsDateTime = new DateTime(tempDT.Year, tempDT.Month, tempDT.Day, tempDT.Hour, 0, 0);
    }
    /// <summary>
    /// Sets the time portion represented by the unix time to 00:00:01
    /// </summary>
    public void setToStartOfDay()
    {
      AsDateTime = new DateTime(AsDateTime.Year, AsDateTime.Month, AsDateTime.Day, 0, 0, 1);
    }
    /// <summary>
    /// Sets the time portion represented by the unix time to 01:00:00
    /// </summary>
    public void setToFirstHourOfDay()
    {
      AsDateTime = new DateTime(AsDateTime.Year, AsDateTime.Month, AsDateTime.Day, 1, 0, 0);
    }
    public void setToEndOfDay()
    {
      AsDateTime = new DateTime(AsDateTime.Year, AsDateTime.Month, AsDateTime.Day, 0, 0, 0);
    }
    public void setToHourEnding(int HE)
    {
      HE = (HE == 24) ? 0 : HE;

      AsDateTime = new DateTime(AsDateTime.Year, AsDateTime.Month, AsDateTime.Day, HE, 0, 0);
    }

    public void setToNearestInterval(int intervalMins, short prevOrNext)
    {
      DateTime dt;
      int minDiff;

      dt = new DateTime(AsDateTime.Year, AsDateTime.Month, AsDateTime.Day, AsDateTime.Hour, AsDateTime.Minute, 0);

      minDiff = (dt.Minute % intervalMins);
      if (minDiff != 0)
      {
        if (prevOrNext == 0)
        {
          minDiff = minDiff * -1;
        }
        else
        {
          minDiff = (intervalMins - minDiff);
        }
        dt = dt.AddMinutes(minDiff);
        
        if ((dt.Hour == 0) && (dt.Minute == 0))
        {
          dt = dt.AddDays(-1);
        }
      }
      AsDateTime = dt;
    }

    /// <summary>
    /// Converts the given DateTime to LCA Unixtime
    /// </summary>
    /// <param name="inDT">DateTime to convert</param>
    /// <returns>Unixtime value of the DateTime</returns>
    private long DateTimeToUnix(DateTime inDT)
    {
      int tHour, tMin, tSec, tDay, tMonth, tYear, i;
      long calcUnix = 0;
      long temp1 = 0;
      long timePortion = 0;

      tHour = inDT.Hour;
      tMin = inDT.Minute;
      tSec = inDT.Second;

      if ((tHour + tMin + tSec) == 0) tHour = 24;

      tDay = inDT.Day;
      tMonth = inDT.Month;
      tYear = inDT.Year;

      // Calculate the year portion
      for (i = 1990; i < tYear; i++)
      {
        if (DateTime.IsLeapYear(i) == true)
        {
          calcUnix += 366;
        }
        else
        {
          calcUnix += 365;
        }
      }
      calcUnix *= 86400;

      // Calculate the Month\Day Portion
      for (i = 1; i < tMonth; i++)
      {
        temp1 += DateTime.DaysInMonth(tYear, i);
      }

      calcUnix += temp1 * 86400;
      calcUnix += (tDay - 1) * 86400;

      timePortion += tHour * 3600;
      timePortion += tMin * 60;
      timePortion += tSec + 1;

      calcUnix += timePortion;

      _AsUnixSeconds = timePortion - 2;

      return calcUnix;
    }

    /// <summary>
    /// Converts the given LCA UnixTime value to a DateTime
    /// </summary>
    /// <param name="UnixTime">LCA Unixtime to convert</param>
    /// <returns>Converted Datetime</returns>
    private DateTime UnixToDateTime(long UnixTime)
    {
      int tHour, tMin, tSec, tDay, tMonth, tYear, temp1;
      long calcUnix, curSum, thisVal;
      DateTime calcDT = new DateTime();

      calcUnix = UnixTime;

      // Find the year
      temp1 = 1989;
      curSum = 0;
      while (calcUnix > 1)
      {
        temp1++;
        if (DateTime.IsLeapYear(temp1) == true) thisVal = (86400 * 366);
        else thisVal = (86400 * 365);

        calcUnix -= thisVal;
        if (calcUnix > 1) curSum += thisVal;
      }
      tYear = temp1;

      calcUnix = UnixTime - curSum;
      temp1 = 0;
      // Find the month
      while (calcUnix > 1)
      {
        temp1++;
        thisVal = DateTime.DaysInMonth(tYear, temp1) * 86400;
        calcUnix -= thisVal;
        if (calcUnix > 1) curSum += thisVal;
      }
      tMonth = temp1;

      // Find the Day
      temp1 = 0;
      calcUnix = UnixTime - curSum;
      // Find the month
      while (calcUnix > 1)
      {
        temp1++;
        calcUnix -= (86400);
        if (calcUnix > 1) curSum += 86400;
      }
      tDay = temp1;

      // Find the Hour
      temp1 = -1;
      calcUnix = UnixTime - curSum;
      while (calcUnix >= 1)
      {
        temp1++;
        calcUnix -= (3600);
        if (calcUnix >= 1) curSum += 3600;
      }
      tHour = temp1;

      // Find the minute
      temp1 = -1;
      calcUnix = UnixTime - curSum;
      while (calcUnix >= 1)
      {
        temp1++;
        calcUnix -= (60);
        if (calcUnix >= 1) curSum += 60;
      }
      tMin = temp1;

      tSec = (int)(UnixTime - curSum - 1);

      if (tHour == 24) tHour = 0;

      calcDT = new DateTime(tYear, tMonth, tDay, tHour, tMin, tSec);

      _AsUnixSeconds = (tHour * 3600) + (tMin * 60) + (tSec + 1);

      return calcDT;
    }

    public LCAUnixTime(decimal? UnixTime)
    {
      if (UnixTime != null)
      {
        _AsUnixTime = (long)UnixTime;
        _AsDateTime = UnixToDateTime((long)UnixTime);
      }
    }

    public LCAUnixTime(decimal UnixTime)
    {
      _AsUnixTime = (long)UnixTime;
      _AsDateTime = UnixToDateTime((long)UnixTime);
    }
    public LCAUnixTime(double UnixTime)
    {
      _AsUnixTime = (long)UnixTime;
      _AsDateTime = UnixToDateTime((long)UnixTime);
    }

    public LCAUnixTime(long UnixTime)
    {
      _AsUnixTime = UnixTime;
      _AsDateTime = UnixToDateTime(UnixTime);
    }

    public LCAUnixTime(DateTime InDT)
    {
      _AsDateTime = InDT;
      _AsUnixTime = DateTimeToUnix(InDT);
    }

    public LCAUnixTime(String InDT)
    {
      _AsDateTime = DateTime.Parse(InDT);
      _AsUnixTime = DateTimeToUnix(_AsDateTime);
    }

    public LCAUnixTime()
    {
    }
  }

  /// <summary>
  /// Handler for LCA style XML Config strings
  /// </summary>
  public class LCAConfigString
  {
    private string _CfgStr;
    public string CfgStr
    {
      get { return _CfgStr; }
      set
      {
        _CfgStr = value;
      }
    }

    public LCAConfigString() : this("") { }

    public LCAConfigString(string cfgStr)
    {
      _CfgStr = cfgStr;
    }

    /// <summary>
    /// Searchs a config string for the specified parameter and returns its value
    /// </summary>
    /// <param name="CfgStr">LCA Configuration String</param>
    /// <param name="ParamName">Parameter to get the value for</param>
    /// <returns>The value of the parameter</returns>
    public string GetCfgStrParam(string ParamName)
    {
      string xmlCfgStr = "";
      string readStr = "";

      if (_CfgStr == null) return null;

      XmlReaderSettings settings = new XmlReaderSettings();

      settings.ConformanceLevel = ConformanceLevel.Fragment;
      settings.IgnoreWhitespace = true;
      settings.IgnoreComments = true;

      if (_CfgStr.ToLower().Contains(@"<?xml version") == false)
      {
        // Wrap the configstring in xml header and root parameter
        xmlCfgStr = @"<?xml version=""1.0""?><LCAConfigString>" + _CfgStr + "</LCAConfigString>";
      }
      else xmlCfgStr = _CfgStr;

      // Create an xml reader so we can search the configstring, we need to pass this as a StringReader
      XmlReader reader = XmlReader.Create(new System.IO.StringReader(xmlCfgStr), settings);

      reader.ReadToFollowing(ParamName);
      readStr = reader.ReadString();

      reader.Close();

      return readStr;
    }
  }
}

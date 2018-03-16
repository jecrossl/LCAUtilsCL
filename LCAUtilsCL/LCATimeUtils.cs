using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
namespace LCAUtilsCL
{
  public class LCATimeUtils
  {
    [DllImport("kernel32.dll", EntryPoint = "SetSystemTime", SetLastError = true)]
    private extern static uint SetSystemTime(ref SYSTEMTIME lpSystemTime);

    [DllImport("kernel32.dll", EntryPoint = "GetSystemTime", SetLastError = true)]
    private extern static void GetSystemTime(ref SYSTEMTIME lpSystemTime);
    
    private struct SYSTEMTIME
    {
      public ushort wYear;
      public ushort wMonth;
      public ushort wDayOfWeek;
      public ushort wDay;
      public ushort wHour;
      public ushort wMinute;
      public ushort wSecond;
      public ushort wMilliseconds;
    }

    public static void setSystemDateTime(DateTime dt)
    {
      // Call the native GetSystemTime method 
      // with the defined structure.
      dt = dt.ToUniversalTime();

      SYSTEMTIME systime = new SYSTEMTIME();
      GetSystemTime(ref systime);

      // Set the system clock ahead one hour.
      systime.wYear = (ushort)dt.Year;
      systime.wMonth = (ushort)dt.Month;
      systime.wDay = (ushort)dt.Day;
      systime.wHour = (ushort)dt.Hour;
      systime.wMinute = (ushort)dt.Minute;
      systime.wSecond = (ushort)dt.Second;
      systime.wMilliseconds = 0;

      SetSystemTime(ref systime);
    }

    public static bool GetOntarioDSTPeriod(int year, out DateTime sDT, out DateTime eDT)
    {
      bool bRet = false;

      try
      {
        int daysToAdd;

        sDT = new DateTime(year, 3, 1, 2, 1, 0);

        daysToAdd = (sDT.DayOfWeek == DayOfWeek.Sunday) ? 7 : ((7 - (int)sDT.DayOfWeek) + 7);
        sDT = sDT.AddDays(daysToAdd);

        eDT = new DateTime(year, 11, 1, 2, 1, 0);
        daysToAdd = (eDT.DayOfWeek == DayOfWeek.Sunday) ? 0 : ((7 - (int)eDT.DayOfWeek));
        eDT = eDT.AddDays(daysToAdd);

        bRet = true;
      }
      catch (Exception e)
      {
        sDT = new DateTime(1999, 1, 1, 1, 0, 0);
        eDT = new DateTime(1999, 1, 1, 1, 0, 0);
        bRet = false;
      }

      return bRet;
    }

    public static string getTimeFromIESOHEI(int HE, int interval)
    {
      string time;
      int hour;
      int minute;

      minute = (interval == 12) ? 0 : (5 * interval);

      hour = (minute == 0) ? HE : (HE - 1);
      hour = (hour == 24) ? 0 : hour;

      time = hour.ToString("00") + ":" + minute.ToString("00") + ":00";
      return time;
    }

    public static void getIESOHEInterval(LCAUnixTime fromDT, out int HE, out int Interval)
    {
      getIESOHEInterval(fromDT.AsDateTime, out HE, out Interval);
    }

    /// <summary>
    /// Gets the IESO Hour Ending and Interval from a DateTime 
    /// </summary>
    /// <param name="fromUnix"></param>
    /// <param name="HE"></param>
    /// <param name="Interval"></param>
    public static void getIESOHEInterval(DateTime fromDT, out int HE, out int Interval)
    {
      HE = (fromDT.Minute == 0) ? fromDT.Hour : fromDT.Hour + 1;
      if (HE == 0) HE = 24;
      Interval = (fromDT.Minute == 0) ? 12 : (fromDT.Minute / 5);
    }

    /// <summary>
    /// Checks if the specified date/time fits into the Ontario DST period
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static bool IsDTInOntarioDST(DateTime dt)
    {
      bool bRet = false;

      try
      {
        DateTime dstStart,dstStop;
 
        GetOntarioDSTPeriod(dt.Year, out dstStart, out dstStop);

        if ((dt >= dstStart) && (dt <= dstStop))
        {
          bRet = true;
        }
      }
      catch (Exception e)
      {
      }

      return bRet;
    }

    public static DateTime AddBusinessDays(DateTime startDate, int businessDays)
    {
      return AddBusinessDays(startDate, businessDays, null);
    }

    public static DateTime AddBusinessDays(DateTime startDate, int businessDays, List<DateTime> holidays)
    {
      if (businessDays == 0) return startDate;

      DateTime curDate = new DateTime(startDate.Ticks);
      bool isBDay;

      int direction = Math.Sign(businessDays);
      while (businessDays != 0)
      {
        curDate = curDate.AddDays(direction);
        isBDay = true;
        if ((curDate.DayOfWeek == DayOfWeek.Saturday) || (curDate.DayOfWeek == DayOfWeek.Sunday))
        {
          isBDay = false;
        }
        else if (holidays != null)
        {
          if (holidays.Contains(curDate.Date))
          {
            isBDay = false;
          }
        }
        if (isBDay)
        {
          businessDays += (direction * -1);
        }
      }

      return curDate;

      //if (direction == 1)
      //{
      //  if (startDate.DayOfWeek == DayOfWeek.Saturday)
      //  {
      //    startDate = startDate.AddDays(2);
      //    businessDays = businessDays - 1;
      //  }
      //  else if (startDate.DayOfWeek == DayOfWeek.Sunday)
      //  {
      //    startDate = startDate.AddDays(1);
      //    businessDays = businessDays - 1;
      //  }
      //}
      //else
      //{
      //  if (startDate.DayOfWeek == DayOfWeek.Saturday)
      //  {
      //    startDate = startDate.AddDays(-1);
      //    businessDays = businessDays + 1;
      //  }
      //  else if (startDate.DayOfWeek == DayOfWeek.Sunday)
      //  {
      //    startDate = startDate.AddDays(-2);
      //    businessDays = businessDays + 1;
      //  }
      //}

      //int initialDayOfWeek = Convert.ToInt32(startDate.DayOfWeek);

      //int weeksBase = Math.Abs(businessDays / 5);
      //int addDays = Math.Abs(businessDays % 5);

      //if ((direction == 1 && addDays + initialDayOfWeek > 5) || (direction == -1 && addDays >= initialDayOfWeek))
      //{
      //  addDays += 2;
      //}

      //int totalDays = (weeksBase * 7) + addDays;
      //return startDate.AddDays(totalDays * direction);
    }

    public static void get15MinPeriod(DateTime inDT, out LCAUnixTime periodSUnix, out LCAUnixTime periodEUnix)
    {
      int period;
      int periodMin;
      DateTime periodDT;

      if (inDT.Minute % 15 == 0)
      {
        periodMin = inDT.Minute;
      }
      else
      {
        period = (int)(inDT.Minute / 15);
        periodMin = (period * 15) + 15;
        if (periodMin == 60) periodMin = 0;
      }
      periodDT = new DateTime(inDT.Year, inDT.Month, inDT.Day, inDT.Hour, periodMin, 0);
      if ((periodDT.Hour == 0) && (periodDT.Minute == 0)) periodDT = periodDT.AddDays(-1);

      periodSUnix = new LCAUnixTime(periodDT.AddMinutes(-14));
      periodEUnix = new LCAUnixTime(periodDT);
    }

    public static void getHourEndingPeriod(DateTime inDT, out LCAUnixTime periodSUnix, out LCAUnixTime periodEUnix)
    {
      DateTime periodDT;

      if ((inDT.Minute == 0) && (inDT.Second == 0))
      {
        // If we're given a time with minute and second = 0 it's already a valid hour ending time
        periodDT = inDT;
      }
      else periodDT = inDT.AddHours(1);

      periodDT = new DateTime(periodDT.Year, periodDT.Month, periodDT.Day, periodDT.Hour, 0, 0);
      if (periodDT.Hour == 0) periodDT = periodDT.AddDays(-1);

      periodSUnix = new LCAUnixTime(periodDT.AddMinutes(-59));
      periodEUnix = new LCAUnixTime(periodDT);

    }

    public static DateTime convertTimeZone(DateTime localDT, string destZone, bool applyDST, out bool convertOK)
    {
      DateTime convertedDT;

      try
      {
        // Convert our to UTC, then convert it to Pacific Time
        DateTime hourEndUTCTime = DateTime.Parse(localDT.ToUniversalTime().ToString("MMM-dd-yyyy HH:mm:ss"));

        TimeZoneInfo destTimeZone = TimeZoneInfo.FindSystemTimeZoneById(destZone);
        convertedDT = TimeZoneInfo.ConvertTimeFromUtc(hourEndUTCTime, destTimeZone);

        //if (applyDST)
        //{
        //    // Convert the time into DST
        //    bool dst = destTimeZone.IsDaylightSavingTime(convertedDT);
        //    string dstZone = destTimeZone.DaylightName;
        //    TimeZoneInfo dstTimeZone = TimeZoneInfo.FindSystemTimeZoneById(dstZone);
        //    convertedDT = TimeZoneInfo.ConvertTime(convertedDT, destTimeZone, dstTimeZone);
        //}

        convertOK = true;
      }
      catch (Exception e)
      {
        convertOK = false;
        convertedDT = new DateTime(1999, 1, 1, 1, 0, 0);
      }

      return convertedDT;
    }
  }
}

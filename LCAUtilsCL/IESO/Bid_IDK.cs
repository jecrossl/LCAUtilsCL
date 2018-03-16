using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LCAUtilsCL.IESO
{
  public class Bid_IDK
  {
    public LCAUnixTime submittedUnix;
    public LCAUnixTime savedUnix;
    public int accepted;
    public int operApproval;
    public string feedback;

    private bool m_Use3PartBidding = false;  // If TRUE use generator style bidding with SNL and SUC

    public IMOBidFileHeader fileHeader;
    public IMOBidHeader bidHeader;
    public IMOBidBody bidBody;

    public string rawString;

    /// <summary>
    /// This constructor is useful for creating a new bid from scratch or with the intention of using ProcessFullBidString
    /// to parse an entire bid file.
    /// </summary>
    /// <param name="use3PartBidding"></param>
    /// <param name="market"></param>
    public Bid_IDK(bool use3PartBidding, string market)
    {
      m_Use3PartBidding = use3PartBidding;

      fileHeader = new IMOBidFileHeader();
      bidHeader = new IMOBidHeader(market);
      bidBody = new IMOBidBody(use3PartBidding, market);
    }

    public Bid_IDK(bool use3PartBidding, FileInfo parserBidFile)
    {
      string bidContent = LCAFileUtils.getTextFileContents(parserBidFile.FullName);
      m_Use3PartBidding = use3PartBidding;

      processFullBidString(bidContent);
    }

    /// <summary>
    /// This constructor is useful for handling bids from the LCA bid transfer/bid history table fields
    /// </summary>
    /// <param name="use3PartBidding"></param>
    /// <param name="fHeader"></param>
    /// <param name="bHeader"></param>
    /// <param name="bBody"></param>
    public Bid_IDK(bool use3PartBidding, string fHeader, string bHeader, string bBody)
    {
      m_Use3PartBidding = use3PartBidding;

      fileHeader = new IMOBidFileHeader();

      fileHeader.processString(fHeader);

      bidHeader = new IMOBidHeader(fileHeader.market);
      bidHeader.processString(bHeader);

      bidBody = new IMOBidBody(use3PartBidding, fileHeader.market);
      bidBody.processString(bBody);
    }

    /// <summary>
    /// Process full IESO IDK bid text into it's fileheader,bidheader,bidbody
    /// </summary>
    /// <param name="data"></param>
    public void processFullBidString(string data)
    {
      try
      {
        string[] splitBid = data.Split(new char[] { '*' });

        fileHeader = new IMOBidFileHeader();
        fileHeader.processString(splitBid[0].Replace("\r\n", ""));

        bidHeader = new IMOBidHeader(fileHeader.market);
        string[] subSplit = splitBid[1].Split(new char[] { ';' });
        bidHeader.processString(subSplit[0].Replace("\r\n", ""));
        string bodyStr = "";
        for (int i = 1; i < subSplit.Count(); i++)
        {
          bodyStr += subSplit[i];
        }

        bidBody = new IMOBidBody(m_Use3PartBidding, fileHeader.market);
        bidBody.processString(bodyStr);
      }
      catch (Exception e)
      {
        Console.Write("ASDF");
      }
    }

    /// <summary>
    /// Creates the IESO's IDK formatted text for submitting the current bid
    /// </summary>
    /// <returns></returns>
    public string generateBidText()
    {
      string bid = "";

      try
      {
        bid = fileHeader.generateString() + "\r\n*\r\n";
        bid += bidHeader.generateString() + "\r\n";
        bid += bidBody.generateString() + "\r\n";
        bid += "*\r\n*\r\n";
      }
      catch (Exception e)
      {
        Console.Write("ASDF");
      }

      return bid;
    }

    public class IMOBidFileHeader
    {
      public string rawString;
      public string market = "";
      public string deliveryDateStr = "";
      public string participantID = "";
      public string userID = "";

      public DateTime deliveryDateDT;

      /// <summary>
      /// Generate an IDK formatted file header string from the current property settings of this class
      /// </summary>
      /// <returns></returns>
      public string generateString()
      {
        string retStr = "";

        retStr = "PM, " + market + ", " + deliveryDateStr + ", " + participantID + ", " + userID + ", , NORMAL;";

        return retStr;
      }

      /// <summary>
      /// Process an IDK formatted file header string into the properties of this class
      /// </summary>
      /// <param name="data"></param>
      public void processString(string data)
      {
        try
        {
          rawString = data;

          data = data.Replace(";", "");
          string[] splitStr = data.Split(new char[] { ',' });

          if (splitStr.Count() == 7)
          {
            market = splitStr[1];
            deliveryDateStr = splitStr[2];
            participantID = splitStr[3];
            userID = splitStr[4];

            deliveryDateDT = DateTime.ParseExact(deliveryDateStr, "yyyyMMdd", null);
          }
        }
        catch (Exception e)
        {
          Console.Write("ASDF");
        }
      }
    }

    public class IMOBidHeader
    {
      private string m_Market;

      public string rawString;

      public string bidType; // LOAD, DISPLOAD, GENERATOR
      public string resourceID;
      public string tiePointID;
      public string dailyLimitStr;
      public string opResRampRateStr;
      public string submitCancel;
      public string standingNormal; // Standing or Normal
      public string standingDay;
      public string standingExpiryStr;
      public string opResClass;

      public IMOBidHeader(string market)
      {
        m_Market = market;
      }

      /// <summary>
      /// Generates an IDK format bid header string from the properties in this class
      /// </summary>
      /// <returns></returns>
      public string generateString()
      {
        string retStr = "";

        if (m_Market == "RTEM")
        {
          if (standingNormal == "STANDING")
            if (standingExpiryStr != "")
              retStr = bidType + ", " + resourceID + ", " + tiePointID + ", " + dailyLimitStr + ", " + opResRampRateStr + ", " + submitCancel + ", " + standingNormal + ", " + standingDay + ", " + standingExpiryStr + ";";
            else
              retStr = bidType + ", " + resourceID + ", " + tiePointID + ", " + dailyLimitStr + ", " + opResRampRateStr + ", " + submitCancel + ", " + standingNormal + ", " + standingDay + ";";
          else
            retStr = bidType + ", " + resourceID + ", " + tiePointID + ", " + dailyLimitStr + ", " + opResRampRateStr + ", " + submitCancel + ", " + standingNormal + ";";
        }
        else
        {
          if (standingNormal == "STANDING")
            if (standingExpiryStr != "")
              retStr = bidType + ", " + resourceID + ", " + tiePointID + ", " + opResClass + ", " + submitCancel + ", " + standingNormal + ", " + standingDay + ", " + standingExpiryStr + ";";
            else
              retStr = bidType + ", " + resourceID + ", " + tiePointID + ", " + opResClass + ", " + submitCancel + ", " + standingNormal + ", " + standingDay + ";";
          else
            retStr = bidType + ", " + resourceID + ", " + tiePointID + ", " + opResClass + ", " + submitCancel + ", " + standingNormal + ";";

        }
        return retStr;
      }

      /// <summary>
      /// Process an IDK format Bid header string into the properties of this class
      /// </summary>
      /// <param name="data"></param>
      public void processString(string data)
      {
        rawString = data;

        try
        {
          string[] splitStr = data.Split(new char[] { ',' });

          bidType = splitStr[0];
          resourceID = splitStr[1];
          tiePointID = splitStr[2];

          if (m_Market == "RTEM")
          {
            dailyLimitStr = splitStr[3];
            opResRampRateStr = splitStr[4];
            submitCancel = splitStr[5];
            standingNormal = splitStr[6];
            if (splitStr.Count() >= 8)
            {
              standingDay = splitStr[7];
            }
            if (splitStr.Count() >= 9)
            {
              standingExpiryStr = splitStr[8];
            }
          }
          else
          {
            opResClass = splitStr[3];
            submitCancel = splitStr[4];
            standingNormal = splitStr[5];
            if (splitStr.Count() >= 7)
            {
              standingDay = splitStr[6];
            }
            if (splitStr.Count() >= 8)
            {
              standingExpiryStr = splitStr[7];
            }
          }
        }
        catch (Exception e)
        {
          Console.Write("ASDF");
        }
      }
    }

    public class IMOBidBody
    {
      public string rawString;
      public IMOBidHour[] bidHours;
      private bool m_Use3PartBidding = false;  // If TRUE use generator style bidding with SNL and SUC
      private string m_Market = "";
      public double opResLoadingPoint;

      public IMOBidBody(bool use3PartBidding, string market)
      {
        m_Use3PartBidding = use3PartBidding;
        m_Market = market;
      }

      /// <summary>
      /// Generates the Bid Body text in IDK format based on the current property settings
      /// </summary>
      /// <returns></returns>
      public string generateString()
      {
        string retStr = "";

        try
        {
          foreach (IMOBidHour hour in bidHours)
          {
            if (m_Market == "RTEM")
            {
              retStr += hour.hourEnding.ToString() + ",,{" + hour.generatePQPairStr() + "},{" + hour.generateRampStr() + "}" + ",N,N,N";
              if (m_Use3PartBidding)
              {
                retStr += "," + hour.SNL + "," + hour.SUC;
              }
            }
            else
            {
              retStr += hour.hourEnding.ToString() + ",,{" + hour.generatePQPairStr() + "}," + opResLoadingPoint.ToString("#####.#") + "";
            }

            if (hour.hasReason)
            {
              retStr += "," + hour.reason;
              if (hour.otherReason != "") retStr += "," + hour.otherReason;
            }
            retStr += ";\r\n";
          }
          if (retStr.EndsWith("\r\n")) retStr = retStr.Substring(0, retStr.Length - 2);
        }
        catch (Exception e)
        {
          Console.Write("ASDF");
          retStr = "";
        }

        return retStr;
      }

      // Parse an IDK formatted bid body string
      public void processString(string data)
      {
        rawString = data;
        try
        {
          //1,,{(2000,0),(2000,25),(1999,125)},{(100,.1,100)},N,N,N;  2,,{(2000,0),(2000,25),(1999,125)},{(100,.1,100)},N,N,N;
          string[] hourSplit;
          List<string> singleHours = new List<string>();

          data = data.Replace("\n", "");
          hourSplit = data.Split(new string[] { "\r" }, StringSplitOptions.RemoveEmptyEntries);

          // We need to find lines that contain multiple hours (like 1-3) and split them into individual hours
          for (int i = 0; i < hourSplit.Count(); i++)
          {
            string[] subSplit = hourSplit[i].Split(new char[] { ',' });
            string[] hours;
            int startHour,endHour;

            string hour = subSplit[0];

            // If this hour string has a - then it has multiple hours
            if (hour.Contains('-'))
            {
              // Get the rest of the bid line so we can build it up in case of multiple hours
              string remains = hourSplit[i].Substring(hourSplit[i].IndexOf(','));

              hours = hour.Split(new char[] { '-' });
              startHour = int.Parse(hours[0]);
              endHour = int.Parse(hours[1]);

              for (int j = 0; j <= (endHour - startHour); j++)
              {
                singleHours.Add((startHour + j).ToString() + remains);
              }
            }
            else
            {
              // There's only one hour on this line, so just add the full line back into the list
              singleHours.Add(hourSplit[i]);
            }
          }
          // Convert the new list of single hour bid lines back into an array for further processing
          hourSplit = singleHours.ToArray<string>();

          bidHours = new IMOBidHour[hourSplit.Count()];

          // Process each hour of the bid 
          for (int i = 0; i < hourSplit.Count(); i++)
          {
            int sPos, ePos;
            string pqPairStr = "", rampStr = "";
            string tempStr = "";
            string[] remainingSplit;
            sPos = 0;
            ePos = 0;
            
            string thisHourStr = hourSplit[i];

            // For RTEM we process twice for pqPairs and Ramp rates. OR only has pq pairs
            int processMax = (m_Market == "RTEM") ? 1 : 0;


            // We search the string for the {} bracketed sections (pqpairs and ramp rate (RTEM only))
            for (int j = 0; j <= processMax; j++)
            {
              sPos = thisHourStr.IndexOf("{", 0);
              if (sPos >= 0)
              {
                ePos = thisHourStr.IndexOf("}", sPos + 1);
                if (ePos > 0)
                {
                  tempStr = thisHourStr.Substring(sPos + 1, ePos - sPos - 1);

                  if (j == 0) pqPairStr = tempStr;
                  else rampStr = tempStr;

                  // We remove this section from the original string
                  thisHourStr = thisHourStr.Replace("{" + tempStr + "}", "");
                }
              }
            }

            // PQPairs and Ramp Rate are now removed
            // We split the rest of the string 
            remainingSplit = thisHourStr.Split(new char[] { ',' },  StringSplitOptions.RemoveEmptyEntries);

            bidHours[i] = new IMOBidHour(m_Market);

            bidHours[i].hourEnding = int.Parse(remainingSplit[0]);
            bidHours[i].hasReason = false;

            if (pqPairStr != "") bidHours[i].splitPQPairStr(pqPairStr);

            if (m_Market == "RTEM")
            {
              // RTEM Bid
              bidHours[i].splitRampStr(rampStr);
              if (m_Use3PartBidding == true)
              {
                // 3 Part bidding is in use, parse the SUC and SNL
                try
                {
                  bidHours[i].SNL = int.Parse(remainingSplit[4]);
                  bidHours[i].SUC = int.Parse(remainingSplit[5]);
                }
                catch (Exception e)
                {
                  bidHours[i].SNL = -1;
                  bidHours[i].SUC = -1;
                }

                if (remainingSplit.Count() == 7)
                {
                  bidHours[i].reason = remainingSplit[6];
                  bidHours[i].hasReason = true;
                }
                else if (remainingSplit.Count() >= 8)
                {
                  bidHours[i].reason = remainingSplit[6];
                  for (int j = 7; j < remainingSplit.Count(); j++)
                  {
                    bidHours[i].otherReason += remainingSplit[j];
                  }
                  bidHours[i].hasReason = true;
                }
              }
              else
              {
                if (remainingSplit.Count() == 5)
                {
                  bidHours[i].reason = remainingSplit[4];
                  bidHours[i].hasReason = true;
                }
                else if (remainingSplit.Count() >= 6)
                {
                  bidHours[i].reason = remainingSplit[4];
                  for (int j = 5; j < remainingSplit.Count(); j++)
                  {
                    bidHours[i].otherReason += remainingSplit[j];
                  }
                  bidHours[i].hasReason = true;
                }
              }
            }
            else
            {
              // OR Bid
              if (remainingSplit[1] != "") bidHours[i].reserveLoadingPoint = decimal.Parse(remainingSplit[1]);

              if (remainingSplit.Count() == 3)
              {
                bidHours[i].reason = remainingSplit[2];
                bidHours[i].hasReason = true;
              }
              else if (remainingSplit.Count() >= 4)
              {
                bidHours[i].reason = remainingSplit[2];
                for (int j = 3; j < remainingSplit.Count(); j++)
                {
                  bidHours[i].otherReason += remainingSplit[j];
                }
                bidHours[i].hasReason = true;
              }

            }
          }
        }
        catch (Exception e)
        {
          Console.Write("ASDF");
        }
      }
    }

    public class IMOBidHour
    {
      public class BidPQPair
      {
        public decimal price;
        public decimal quantity;
      }

      public class BidRampRate
      {
        public decimal breakPoint = 0;
        public decimal rateUP = 0;
        public decimal rateDown = 0;
      }

      private string m_BidMarket = "";
      public int hourEnding;
      public BidPQPair[] pqPairs;
      public BidRampRate[] rampRate;
      public int SNL, SUC;

      public bool hasReason = false;
      public string reason = "";
      public string otherReason = "";
      public decimal reserveLoadingPoint;

      public IMOBidHour(string market)
      {
        m_BidMarket = market;
      }

      public string generatePQPairStr()
      {
        string retStr = "";

        foreach (BidPQPair pair in pqPairs)
        {
          retStr += "(" + pair.price.ToString("###0.00") + "," + pair.quantity.ToString("###0.0") + "),";
        }
        if (retStr.EndsWith(",")) retStr = retStr.Substring(0, retStr.Length - 1);

        return retStr;
      }

      public string generateRampStr()
      {
        string retStr = "";

        foreach (BidRampRate ramp in rampRate)
        {
          retStr += "(";
          retStr = ramp.breakPoint.ToString("###0.0");
          retStr += ",";
          retStr += ramp.rateUP.ToString("##0.0");
          retStr += ",";
          retStr += ramp.rateDown.ToString("##0.0");
          retStr += "),";
        }
        if (retStr.EndsWith(",")) retStr = retStr.Substring(0, retStr.Length - 1);

        return retStr;
      }

      public void splitRampStr(string rampStr)
      {
        rampRate = null;
        int pos = 0;

        try
        {
          rampStr = rampStr.Replace("(", "");
          rampStr = rampStr.Replace(")", "");
          string[] rampSplit = rampStr.Split(new char[] { ',' });

          rampRate = new BidRampRate[(rampSplit.Count() / 3)];
          for (int i = 0; i < rampSplit.Count(); i += 3)
          {
            rampRate[pos] = new BidRampRate();

            rampRate[pos].breakPoint = decimal.Parse(rampSplit[i]);
            rampRate[pos].rateUP = decimal.Parse(rampSplit[i + 1]);
            rampRate[pos].rateDown = decimal.Parse(rampSplit[i + 2]);

            pos++;
          }
        }
        catch (Exception e)
        {
          Console.Write("ASDF");
        }
      }

      public void splitPQPairStr(string pqPairStr)
      {
        string tempStr;
        string[] fullSplit;
        int pos = 0;
        pqPairs = null;

        try
        {
          tempStr = pqPairStr.Replace("(", "");
          tempStr = tempStr.Replace(")", "");
          fullSplit = tempStr.Split(new char[] { ',' });

          pqPairs = new BidPQPair[(fullSplit.Count() / 2)];
          for (int i = 0; i < fullSplit.Count(); i += 2)
          {
            pqPairs[pos] = new BidPQPair();

            pqPairs[pos].price = decimal.Parse(fullSplit[i]);
            pqPairs[pos].quantity = decimal.Parse(fullSplit[i + 1]);

            pos++;
          }
        }
        catch (Exception e)
        {
          Console.Write("ASDF");
        }
      }
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LCAUtilsCL
{
  public class LCACSVReader
  {
    private List<string[]> m_dataValues = new List<string[]>(); // String array of data by row

    private bool m_HasHeader = true;
    private int m_HeaderRowNum = 0;
    private string m_FileName;
    private string[] m_ColNames;
    private char m_Delimiter = ',';
    private bool m_ValuesWrapped = false;
    private char m_ValuesWrappedChar = '"';

    private FileStream m_Fs;
    private StreamReader m_Sr;
    private BufferedStream m_Bs;

    public int colCount;

    public LCACSVReader(string fileName)
    {
      init(fileName, false, 0, ',', false, '"');
    }
    public LCACSVReader(string fileName, bool hasHeader, int headerRowNum)
    {
      init(fileName, hasHeader, headerRowNum, ',', false, '"');
    }
    public LCACSVReader(string fileName, bool hasHeader, int headerRowNum, char delimiter)
    {
      init(fileName, hasHeader, headerRowNum, delimiter, false, '"');
    }
    public LCACSVReader(string fileName, bool hasHeader, int headerRowNum, char delimiter, bool valuesWrapped, char valuesWrappedChar)
    {
      init(fileName, hasHeader, headerRowNum, delimiter, valuesWrapped, valuesWrappedChar);
    }

    private void init(string fileName, bool hasHeader, int headerRowNum, char delimiter, bool valuesWrapped, char valuesWrappedChar)
    {
      m_FileName = fileName;
      m_HasHeader = hasHeader;
      m_HeaderRowNum = headerRowNum;
      m_Delimiter = delimiter;
      m_ValuesWrapped = valuesWrapped;
      m_ValuesWrappedChar = valuesWrappedChar;
    }

    public int dataRowCount() { return m_dataValues.Count(); }
    public int dataColCount(int row)
    {
      string[] data;
      data = m_dataValues.ElementAt(row);
      return data.Length;
    }
    public int dataRowColCount(int row) { return m_dataValues.ElementAt(row).Length; }
    public string dataValue(int row, int col)
    {
      string[] data;
      
      data = m_dataValues.ElementAt(row);
      return data[col];
    }

    public string colName(int col)
    {
      try
      {
        return m_ColNames[col];
      }
      catch (Exception e)
      {
        return col.ToString();
      }
    }

    /// <summary>
    /// Open the file and associated stream. If you don't manually open/close, it will happen automatically with each read function called
    /// </summary>
    /// <returns>TRUE if the file opens ok</returns>
    public bool open()
    {
      bool bRet = false;

      try
      {
        m_Fs = new FileStream(m_FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        m_Bs = new BufferedStream(m_Fs);
        m_Sr = new StreamReader(m_Bs);

        bRet = true;
      }
      catch (Exception e)
      {
        bRet = false;
      }

      return bRet;
    }
    /// <summary>
    /// Close the open file and dispose of the streams 
    /// </summary>
    public void close()
    {
      try
      {
        m_Sr.Close();
        m_Bs.Close();
        m_Fs.Close();

        m_Sr.Dispose();
        m_Bs.Dispose();
        m_Fs.Dispose();
      }
      catch { }
    }

    /// <summary>
    /// Parses a line of CSV data into an array of columns
    /// </summary>
    /// <param name="row">row number</param>
    /// <param name="lineText"></param>
    /// <returns></returns>
    private bool parseCSVLine(string lineText, out string[] dataValues)
    {
      string[] data;
      int colCount;
      string[] split;
      bool bRet = false;

      try
      {
        if (!m_ValuesWrapped)
        {
          // The values are not wrapped, so there is no potential for delimiter character appearing in the data
          split = lineText.Split(m_Delimiter);
          colCount = split.Length;
        }
        else
        {
          // The values are wrapped, so we need to watch for delimiter characters within the wrapped data strings
          List<int> delimPos = new List<int>();
          bool inWrapper = false;
          int pos = 0;

          // Go through each character in the line
          foreach (char c in lineText)
          {
            // If the character is a wrapper character, toggle the flag indicating if we are inside a wrapper or not
            if (c == m_ValuesWrappedChar)
            {
              inWrapper = !inWrapper;
            }
            else if (c == m_Delimiter)
            {
              // If this is a delimiter character, and we're not inside a wrapper, record the position
              if (!inWrapper)
              {
                delimPos.Add(pos);
              }
            }
            pos++;
          }

          // Split the line at the delimiter positions found above
          colCount = delimPos.Count() + 1;
          split = new string[colCount];
          pos = 0;
          for (int i = 0; i < (colCount - 1); i++)
          {
            split[i] = lineText.Substring(pos, delimPos[i] - pos);
            pos = delimPos[i] + 1;
          }
          split[colCount - 1] = lineText.Substring(pos);
        }

        // Build the csv data array
        data = new string[colCount];
        for (int i = 0; i < split.Length; i++)
        {
          if (m_ValuesWrapped)
          {
            data[i] = split[i].Replace(m_ValuesWrappedChar.ToString(), "");
          }
          else
          {
            data[i] = split[i];
          }
        }

        dataValues = data;
        bRet = true;
      }
      catch (Exception e)
      {
        bRet = false;
        dataValues = null;
      }

      return bRet;
    }

    private bool checkOpen()
    {
      bool shouldClose = false;

      if (m_Fs != null)
      {
        if (!m_Fs.CanRead)
        {
          open();
          shouldClose = true;
        }
      }
      else
      {
        open();
        shouldClose = true;
      }

      return shouldClose;
    }

    private bool moveToRow(int rowNum)
    {
      bool bRet;
      bool shouldClose = checkOpen();
      string data;

      try
      {
        close();
        open();

        if (m_Fs.CanRead)
        {
          for (int i = 0; i < rowNum; i++)
          {
            data = m_Sr.ReadLine();
            if (data == null) return false;
          }
        }

        bRet = true;
      }
      catch (Exception e)
      {
        bRet = false;
      }

      return bRet;
    }

    /// <summary>
    /// Reads a single row from the CSV file 
    /// </summary>
    /// <param name="rowNum"></param>
    /// <returns>number of rows successfully read</returns>
    private int readRow(int rowNum)
    {
      string lineData;
      string[] split;
      bool bRet;

      bool shouldClose = checkOpen();

      if (m_Fs.CanRead)
      {
        // Move the stream to the line before the one we want
        moveToRow(rowNum);
        // Read the line we wanted
        lineData = m_Sr.ReadLine();
        if (lineData != null)
        {
          bRet = parseCSVLine(lineData, out split);
          if (split != null) m_dataValues.Add(split);
        }
        else bRet = false;

        if (shouldClose) close();

        if (bRet)
        {
          return 1;
        }
      }
      return 0;
    }

    /// <summary>
    /// Reads the header lines and gets the column names
    /// </summary>
    public void readHeader()
    {
      int read;

      m_dataValues = new List<string[]>();

      read = readRow(m_HeaderRowNum);
      if (read == 1)
      {
        string[] row = m_dataValues.ElementAt(0);

        m_ColNames = new string[row.Length];
        for (int i = 0; i < row.Length; i++)
        {
          m_ColNames[i] = row[i];
        }
      }
    }

    /// <summary>
    /// Reads a range of data from the file
    /// </summary>
    /// <param name="startRow">Row to start reading at</param>
    /// <param name="rowCount">Number of rows to read</param>
    /// <returns>number of rows read</returns>
    public int readDataRange(int startRow, int rowCount)
    {
      int rowsRead = 0;
      int i = 0;
      string data = "";
      bool bRet;
      string[] split;

      bool shouldClose = checkOpen();

      m_dataValues = new List<string[]>();

      bRet = moveToRow(startRow);
      if (bRet)
      {
        while ((i < rowCount) && (data != null))
        {
          data = m_Sr.ReadLine();
          if (data != null)
          {
            bRet = parseCSVLine(data, out split);
            if (bRet)
            {
              m_dataValues.Add(split);
            }
          }

          i++;
        }
      }

      if (shouldClose) close();

      return i;
    }

    /// <summary>
    /// Read all of the data rows from the CSV file, starting with the header if one exists
    /// </summary>
    /// <returns>number of rows read</returns>
    public int readAllRows()
    {
      int startRow = 0;
      int rowCount = 0;
      string rowText;
      string[] split;

      bool shouldClose = checkOpen();

      if (m_Fs.CanRead)
      {
        // init the data array
        m_dataValues = new List<string[]>();
        if (m_HasHeader)
        {
          // Read the header line if we have one
          readHeader();
          startRow = m_HeaderRowNum + 1;
        }
        // Seek to the appropriate row
        moveToRow(startRow);

        do
        {
          // Read a line of data
          rowText = m_Sr.ReadLine();
          if (rowText != null)
          {
            if (rowText != "")
            {
              // Parse the csv line
              parseCSVLine(rowText, out split);
              if (split != null) m_dataValues.Add(split);
              rowCount++;
            }
          }
        } while (rowText != null);
      }

      if (shouldClose) close();

      return rowCount;
    }

    /// <summary>
    /// Searches the csv file for rows which contain the column data definied in the dictionary
    /// </summary>
    /// <param name="colsToMatch"></param>
    /// <returns>The number of matching rows</returns>
    public int findLineByColData(Dictionary<int, string> colsToMatch, bool findMultiple)
    {
      bool isLine = false;
      string line;
      bool bRet;
      string[] split;

      bool shouldClose = checkOpen();

      m_dataValues = new List<string[]>();

      try
      {
        moveToRow(0);

        while ((line = m_Sr.ReadLine()) != null)
        {
          //string[] splitLine = line.Split(m_Delimiter);
          bRet = parseCSVLine(line, out split);
          if (bRet)
          {
            try
            {
              isLine = true;
              foreach (var d in colsToMatch)
              {
                if (split[d.Key].ToUpper() != d.Value.ToUpper())
                {
                  isLine = false;
                  break;
                }
              }

              if (isLine)
              {
                m_dataValues.Add(split);
                if (!findMultiple) break;
              }
            }
            catch { }
          }
        }
      }
      catch (Exception e)
      {
        // TODO
      }

      if (shouldClose) close();

      return m_dataValues.Count();
    }
  }

}

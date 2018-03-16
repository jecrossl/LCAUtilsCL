using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using System.Data;
using System.Data.OleDb;
using Excel;

namespace LCAUtilsCL
{
  public class LCAFileUtils
  {

    public static DataTable readXLSSheet(string fileName)
    {
      return readXLSSheet(fileName, "Sheet1$");
    }

    public static DataSet readExcelFile(string fileName)
    {
      DataSet result = null;

      using (FileStream stream = File.Open(fileName, FileMode.Open, FileAccess.Read))
      {
        //Choose one of either 1 or 2
        IExcelDataReader excelReader = null;

        if (fileName.ToLower().EndsWith(".xls"))
        {
          //1. Reading from a binary Excel file ('97-2003 format; *.xls)
          excelReader = ExcelReaderFactory.CreateBinaryReader(stream);
        }
        else
        {
          //2. Reading from a OpenXml Excel file (2007 format; *.xlsx)
          excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
        }

        if (excelReader != null)
        {
          //Choose one of either 3, 4, or 5
          //3. DataSet - The result of each spreadsheet will be created in the result.Tables
          result = excelReader.AsDataSet();
        }
        //6. Free resources (IExcelDataReader is IDisposable)
        excelReader.Close();
        excelReader.Dispose();
      }

      return result;
    }

    public static DataTable readXLSSheet(string fileName, string sheetName)
    {
      // Microsoft.ACE.OLEDB.12.0 
      try
      {
        OleDbCommand selectCommand = new OleDbCommand();
        OleDbConnection connection = new OleDbConnection();
        OleDbDataAdapter adapter = new OleDbDataAdapter();

        string connectionString = string.Empty;

        connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fileName +
            ";Extended Properties=\"Excel 12.0;HDR=No;IMEX=1\";";
        //if (Path.GetExtension(fileName) == ".xlsx")
        //{
        //    connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fileName +
        //        ";Extended Properties=\"Excel 12.0;HDR=Yes;IMEX=1\";";
        //}
        //else
        //{
        //    //                    connectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + fileName + ";Extended Properties=Excel 8.0;HDR=No";
        //    connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fileName +
        //        ";Extended Properties=\"Excel 8.0;HDR=No\"";
        //}

        connection.ConnectionString = connectionString;
        if (connection.State != ConnectionState.Open) connection.Open();

        //                DataTable dtSchema = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

        selectCommand.CommandText = "SELECT * FROM [" + sheetName + "]";
        selectCommand.Connection = connection;
        adapter.SelectCommand = selectCommand;

        DataTable Sheet = new DataTable();
        Sheet.TableName = "Sheet1";
        adapter.Fill(Sheet);

        return Sheet;
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
        return null;
      }
    }

    public static string getTextFileContents(string fName)
    {
      string pageData = null;

      try
      {
        using (FileStream fs = new FileStream(fName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
          if (fs.CanRead)
          {
            using (StreamReader sr = new StreamReader(fs))
            {
              pageData = sr.ReadToEnd();

              fs.Flush();

              sr.Close();
              fs.Close();
            }
          }
        }
      }
      catch (Exception e)
      {
      }

      return pageData;
    }

    public static System.IO.FileInfo getAppPathFileInfo(string fName, string applicationPortion, bool makeUniqueFName)
    {
      int count = 1;
      FileInfo tempFile = getAppPathFileInfo(fName, applicationPortion);

      if (makeUniqueFName)
      {
        // Make sure we are using a unique file name. Append numbers to the end until we find an unused name
        // Normally, there shouldn't be any file hanging around anyway
        while (tempFile.Exists)
        {
          tempFile = getAppPathFileInfo(fName.Replace(tempFile.Extension, "_" + count.ToString() + tempFile.Extension), applicationPortion);
          count++;
        }
      }
      return tempFile;
    }

    public static string getAppPath(string applicationPortion)
    {
      StringBuilder sb = new StringBuilder();

      sb.Append(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
      if (!sb.ToString().EndsWith(@"\")) sb.Append(@"\");

      sb.Append(applicationPortion);
      if (!sb.ToString().EndsWith(@"\")) sb.Append(@"\");

      if (!Directory.Exists(sb.ToString())) Directory.CreateDirectory(sb.ToString());

      return sb.ToString();
    }

    public static System.IO.FileInfo getAppPathFileInfo(string fName)
    {
      return getAppPathFileInfo(fName, "");
    }

    /// <summary>
    /// Returns a FileInfo to the ProgramData folder and file name 
    /// </summary>
    /// <param name="fName"></param>
    /// <param name="applicationPortion"></param>
    /// <returns></returns>
    public static System.IO.FileInfo getAppPathFileInfo(string fName, string applicationPortion)
    {
      StringBuilder sb = new StringBuilder();

      sb.Append(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
      if (!sb.ToString().EndsWith(@"\")) sb.Append(@"\");

      sb.Append(applicationPortion);
      if (!sb.ToString().EndsWith(@"\")) sb.Append(@"\");

      if (!Directory.Exists(sb.ToString())) Directory.CreateDirectory(sb.ToString());

      sb.Append(fName);

      System.IO.FileInfo finf = new System.IO.FileInfo(sb.ToString());

      return finf;
    }

    public static void writeToTextFile(string fileName, string text)
    {
      try
      {
        using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
        {
          using (StreamWriter sw = new StreamWriter(fs))
          {
            sw.Write(text);
          }
        }
      }
      catch (Exception e)
      {
        System.Diagnostics.Debug.Print(e.Message);
      }
    }

    public static void writeToLogFile(string fileName, string text)
    {
      writeToLogFile(fileName, text, 5000000, true);
    }


    public static void writeToLogFile(string fileName, string text, long sizeLimit, Boolean keepBackup)
    {
      FileInfo file = getAppPathFileInfo(fileName);

      try
      {
        if (!file.Exists)
        {
          file.Create();
        }

        try
        {
          if (file.Length >= sizeLimit)
          {
            if (keepBackup)
            {
              file.CopyTo(file.FullName + String.Format("{0:MMddyyyy.hhmmss}", DateTime.Now) + ".bak");
            }

            file.Delete();

            file.Create();
          }
        }
        catch (Exception e)
        {

        }

        StreamWriter swr = new StreamWriter(file.FullName, true);

        swr.WriteLine(String.Format("{0:MMM-dd-yyyy hh:mm:ss}", DateTime.Now) + ", " + text);

        swr.Flush();
        swr.Close();
      }
      catch (Exception e)
      {
        System.Diagnostics.Debug.Print(e.Message);
      }
    }

    public static void logErrorDetails(string funcName, string fileName, Exception e)
    {
      try
      {
        string msg = funcName + ", ";

        msg += e.Message;
        if (e.InnerException != null)
        {
          msg += ", " + e.InnerException.Message;
        }

        msg += "\r\n" + e.StackTrace;

        LCAFileUtils.logErrorToExePath(fileName, msg);
      }
      catch (Exception ex)
      {
      }

    }

    public static void logErrorToExePath(string fileName, string msg)
    {
      try
      {
        Console.Write(msg + "\n");

        string folder = Directory.GetCurrentDirectory();
        string fullName = folder + "\\" + fileName;

        FileInfo fi = new FileInfo(fullName);
        if (fi.Exists)
        {
          if (fi.Length >= 5000000)
          {
            int count = 1;
            string backupName = fullName.Replace(".log", "-backup.log");
            FileInfo backupFI = new FileInfo(backupName);
            while (backupFI.Exists)
            {
              backupName = fullName.Replace(".log", "-backup_" + count.ToString("000") + ".log");
              backupFI = new FileInfo(backupName);
              count++;
            }

            try
            {
              fi.CopyTo(backupFI.FullName);
              fi.Delete();
            }
            catch (Exception e)
            {
              Console.Write("Error during backup of log file : " + e.Message + "\n");
            }

          }
        }

        using (FileStream fs = new FileStream(fullName, FileMode.Append, FileAccess.Write))
        {
          using (StreamWriter sw = new StreamWriter(fs))
          {
            sw.WriteLine(DateTime.Now.ToString("MM/dd/yyyy,HH:mm:ss") + "," + msg);
          }
        }
      }
      catch (Exception e)
      {
        Console.Write("Error while writing error log file : " + e.Message + "\n");
      }
    }
    public static string[] extractZip(string zipFileName, string outFolder)
    {
      int i = 0;
      ZipFile zf = null;
      string[] retFiles;

      try
      {
        FileStream fs = File.OpenRead(zipFileName);

        zf = new ZipFile(fs);

        //if (!String.IsNullOrEmpty(password))
        //{
        //  zf.Password = password;		// AES encrypted entries are handled automatically
        //}

        retFiles = new string[zf.Count];

        foreach (ZipEntry zipEntry in zf)
        {
          if (!zipEntry.IsFile)
          {
            continue;			// Ignore directories
          }
          String entryFileName = zipEntry.Name;
          // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
          // Optionally match entrynames against a selection list here to skip as desired.
          // The unpacked length is available in the zipEntry.Size property.

          byte[] buffer = new byte[4096];		// 4K is optimum
          Stream zipStream = zf.GetInputStream(zipEntry);

          // Manipulate the output filename here as desired.
          String fullZipToPath = Path.Combine(outFolder, entryFileName);

          string directoryName = Path.GetDirectoryName(fullZipToPath);
          if (directoryName.Length > 0) Directory.CreateDirectory(directoryName);

          retFiles[i] = fullZipToPath;
          i++;

          // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
          // of the file, but does not waste memory.
          // The "using" will close the stream even if an exception occurs.
          using (FileStream streamWriter = File.Create(fullZipToPath))
          {
            StreamUtils.Copy(zipStream, streamWriter, buffer);
          }
        }
      }
      finally
      {
        if (zf != null)
        {
          zf.IsStreamOwner = true; // Makes close also shut the underlying stream
          zf.Close(); // Ensure we release resources
        }
      }

      return retFiles;
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;

namespace LCAUtilsCL
{
  public class LCAEmailUtils
  {
    public class LCAEmailSettings
    {
      public string server = "";
      public int port = 0;
      public bool useCred = false;
      public string userName = "";
      public string pWord = "";

      public string fromAddr = "";
      public string[] destAddrs;
      public string[] CCAddrs;
      public string subject;
      public string body;

      public string[] attachFileNames;
    }

    /// <summary>
    /// Sends an email using the settings passed in.
    /// </summary>
    /// <param name="settings"></param>
    /// <returns>Empty string on success, error message on failure</returns>
    public static string SendEmail(LCAEmailSettings settings)
    {
      string retStr = "";

      try
      {
        MailMessage mail = new MailMessage();

        SmtpClient SmtpServer = new SmtpClient(settings.server, settings.port);
        if ((settings.port == 587) || (settings.port == 465)) SmtpServer.EnableSsl = true;
        else SmtpServer.EnableSsl = false;

        if (settings.useCred) SmtpServer.Credentials = new System.Net.NetworkCredential(settings.userName, settings.pWord);
        
        mail.From = new MailAddress(settings.fromAddr);

        if (settings.destAddrs != null)
        {
          foreach (string addr in settings.destAddrs)
          {
            mail.To.Add(addr);
          }
        }

        if (settings.CCAddrs != null)
        {
          foreach (string addr in settings.CCAddrs)
          {
            mail.CC.Add(addr);
          }
        }

        mail.Subject = settings.subject;
        mail.Body = settings.body;

        if (settings.attachFileNames != null)
        {
          foreach (string attach in settings.attachFileNames)
          {
            System.Net.Mail.Attachment attachment;
            attachment = new System.Net.Mail.Attachment(attach);
            mail.Attachments.Add(attachment);
          }
        }
        SmtpServer.Send(mail);

        retStr = "";
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
        retStr = "Email send failed : " + ex.Message;
      }

      return retStr;
    }

  }
}


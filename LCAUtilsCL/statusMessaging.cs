using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LCAUtilsCL
{
  public class statusMessaging
  {
    private ListBox m_StatusList;
    private Form m_ListOwner;

    private delegate void ThreadSafeControlChangeCallback(String text);

    public statusMessaging(Form listOwner, ListBox statusList)
    {
      try
      {
        m_ListOwner = listOwner;
        m_StatusList = statusList;
      }
      catch { }
    }

    private void addItemThreadSafe(string text)
    {
      try
      {
        // InvokeRequired required compares the thread ID of the
        // calling thread to the thread ID of the creating thread.
        // If these threads are different, it returns true.
        if (m_StatusList.InvokeRequired)
        {
          ThreadSafeControlChangeCallback d = new ThreadSafeControlChangeCallback(addItemThreadSafe);
          m_ListOwner.Invoke(d, new object[] { text });
        }
        else
        {
          if (m_StatusList.Items.Count > 5000) m_StatusList.Items.Clear();

          m_StatusList.Items.Add(text);
          m_StatusList.SetSelected(m_StatusList.Items.Count - 1, true);
        }
      }
      catch { }
    }

    public void addStatus(string text)
    {
      try
      {
        if ((m_StatusList == null) || (m_ListOwner == null)) return;

        // Adds a timestamped status message to the status list
        System.DateTime now = System.DateTime.Now;
        text = String.Format("{0:HH:mm:ss}", now) + "  " + text;
        //lstStatus.Items.Add(text);
        addItemThreadSafe(text);
      }
      catch { }
    }

    public void copyContentToClipboard()
    {
      Clipboard.SetText(string.Join("\r\n", m_StatusList.Items.Cast<string>().ToArray<string>()));
    }
  }
}

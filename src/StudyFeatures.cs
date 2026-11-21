using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public static class ReadingScroll {
 [DllImport("user32.dll",EntryPoint="SendMessageW")]static extern IntPtr SendMessage(IntPtr window,int message,IntPtr unused,ref Point point);
 public static Point Get(RichTextBox box){var point=new Point();if(box.IsHandleCreated&&!box.IsDisposed)SendMessage(box.Handle,0x04DD,IntPtr.Zero,ref point);return point;}
 public static void Set(RichTextBox box,Point point){if(box.IsHandleCreated&&!box.IsDisposed)SendMessage(box.Handle,0x04DE,IntPtr.Zero,ref point);}
}

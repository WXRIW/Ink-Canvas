using System;
using System.IO;

namespace Ink_Canvas.Helpers
{
    class LogHelper
    {
        public static string LogFileName = "Log.txt";
        public static string LogFile = "Log.txt";

        public static void NewLog(string str)
        {
            WriteLogToFile(str, LogType.Info);
        }

        public static void NewLog(Exception ex)
        {

        }

        public static void WriteLogToFile(string str, LogType logType = LogType.Info)
        {
            string strLogType = "Info";
            switch (logType)
            {
                case LogType.Event:
                    strLogType = "Event";
                    break;
                case LogType.Trace:
                    strLogType = "Trace";
                    break;
                case LogType.Error:
                    strLogType = "Error";
                    break;
            }
            StreamWriter sw = new StreamWriter(LogFile, true);
            sw.WriteLine(string.Format("{0} [{1}] {2}", DateTime.Now.ToString("O"), strLogType, str));
            sw.Close();
        }

        public enum LogType
        {
            Info,
            Trace,
            Error,
            Event
        }
    }
}

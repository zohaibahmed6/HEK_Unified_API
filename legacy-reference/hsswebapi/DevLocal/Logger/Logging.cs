using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    public class Logging
    {
        static Logging instance = null;

        public static string exceptionPath = "Logs\\IntegrationServices\\ExceptionLogs\\";
        public static string logPath = "Logs\\IntegrationServices\\EventLogs\\";
        public static string pendingFilesPath = "Logs\\IntegrationServices\\Pending\\";
        public static string logRoot = string.Empty;

        public static Logging Instance
        {
            get
            {
                if (instance == null)
                    instance = new Logging();

                return instance;
            }
        }

        public Logging()
        {
            CreateLoggingFiles();
        }

        public void CreateLoggingFiles()
        {
            logRoot = Convert.ToString(ConfigurationManager.AppSettings["LogRoot"]);

            if (!string.IsNullOrEmpty(logRoot)
               && !Directory.Exists(logRoot))
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logRoot));
                }
                catch { logRoot = string.Empty; }
            }

            if (string.IsNullOrEmpty(logRoot))
                logRoot = AppDomain.CurrentDomain.BaseDirectory;

            //add required trailing slashes
            logRoot = logRoot.TrimEnd('\\') + @"\";

            try
            {
                for (int i = 0; i < 2; i++)
                {
                    string FilePath = string.Empty;

                    switch (i)
                    {
                        case 0:
                            FilePath = logRoot + exceptionPath + DateTime.Now.ToString("dd-MMM-yyyy") + ".txt";
                            break;

                        case 1:
                            FilePath = logRoot + logPath + DateTime.Now.ToString("dd-MMM-yyyy") + ".txt";
                            break;
                    }

                    if (!Directory.Exists(FilePath))
                        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

                    if (!File.Exists(FilePath))
                    {
                        FileStream fs = File.Create(FilePath);
                        fs.Dispose();
                    }
                }
            }
            catch { }
        }

        public void WriteExceptionLog(Exception ex)
        {
            WriteExceptionLog(string.Empty, ex);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"> Exception </param>
        /// <param name="LogID"> Unique ID to track the exception</param>
        public void WriteExceptionLog(Exception ex, string LogID)
        {
            WriteExceptionLog(LogID, ex);
        }

        public void WriteExceptionLog(string error, Exception ex)
        {
            WriteExceptionLog(error, TypeEnums.LogType.Default, ex);
        }

        public void WriteExceptionLog(string error, TypeEnums.LogType logType, Exception ex)
        {
            if (ex == null)
                ex = new Exception();

            string message = DateTime.Now.ToString() + " >> " + error + " " + Convert.ToString(ex.Message) + Environment.NewLine + "Stacktrace: " + ex.StackTrace + Environment.NewLine;
            message += "----------------------------------------------------------------------------------------------------------------------------------" + Environment.NewLine;

            Byte[] info = new UTF8Encoding(true).GetBytes(message);

            if (!logType.Equals(TypeEnums.LogType.Default))
                exceptionPath = exceptionPath.Replace("\\IntegrationServices\\", "\\" + Utility.Instance.ToString(logType) + "\\");

            if (!Directory.Exists(logRoot + exceptionPath))
                Directory.CreateDirectory(logRoot + exceptionPath);

            string FilePath = logRoot + exceptionPath + DateTime.Now.ToString("dd-MMM-yyyy") + ".txt";

            try
            {
                if (!File.Exists(FilePath))
                {
                    using (FileStream fs = File.Create(FilePath))
                    {
                        fs.Write(info, 0, info.Length);
                    }
                }
                else
                {
                    using (FileStream fs = File.Open(FilePath, FileMode.Append))
                    {
                        fs.Write(info, 0, info.Length);
                    }
                }
            }
            catch { }
        }

        public void WriteEventLog(string strMessage)
        {
            WriteEventLog(strMessage, TypeEnums.LogType.Default);
        }

        public void WriteEventLog(string strMessage, TypeEnums.LogType logType)
        {
            Byte[] info = new UTF8Encoding(true).GetBytes(DateTime.Now.ToString() + " >> " + strMessage + Environment.NewLine);

            try
            {
                if (!logType.Equals(TypeEnums.LogType.Default))
                    logPath = logPath.Replace("\\IntegrationServices\\", "\\" + Utility.Instance.ToString(logType) + "\\");

                if (!Directory.Exists(logRoot + logPath))
                    Directory.CreateDirectory(logRoot + logPath);

                string FilePath = logRoot + logPath + DateTime.Now.ToString("dd-MMM-yyyy") + ".txt";

                if (!File.Exists(FilePath))
                {
                    using (FileStream fs = File.Create(FilePath))
                    {
                        fs.Write(info, 0, info.Length);
                    }
                }
                else
                {
                    using (FileStream fs = File.Open(FilePath, FileMode.Append))
                    {
                        fs.Write(info, 0, info.Length);
                    }
                }
            }
            catch { }
        }

        public void WriteFile(string strMessage, string error)
        {
            Byte[] info = new UTF8Encoding(true).GetBytes(strMessage + Environment.NewLine + Environment.NewLine + "-----" + error);

            try
            {
                string FilePath = logRoot + pendingFilesPath + DateTime.Now.ToString("dd-MMM-yyyy-HH-mm-ss") + ".txt";

                if (!Directory.Exists(FilePath))
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

                if (!File.Exists(FilePath))
                {
                    using (FileStream fs = File.Create(FilePath))
                    {
                        fs.Write(info, 0, info.Length);
                    }
                }
            }
            catch { }
        }
    }
}

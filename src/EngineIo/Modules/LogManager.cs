using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace EngineIo.Modules
{
    public class LogManager
    {
        private const string myFileName = "XunitTrace.txt";

        private static readonly LogManager EmptyLogger = new LogManager(null);
        private static readonly Regex _invalidCharacters = new Regex("([\ud800-\udbff](?![\udc00-\udfff]))|((?<![\ud800-\udbff])[\udc00-\udfff])", RegexOptions.Compiled);

        private static StreamWriter file;

        public static bool Enabled;

        private readonly string MyType;

        #region Statics

        public static void SetupLogManager()
        {
        }

        public static LogManager GetLogger(string type = default)
        {
            return new LogManager(type ?? GetCallerName());
        }

        public static LogManager GetLogger(Type type)
        {
            return GetLogger(type.ToString());
        }

        public static LogManager GetLogger(MethodBase methodBase)
        {
#if DEBUG
            var type = methodBase.DeclaringType == null ? "" : methodBase.DeclaringType.ToString();
            var type1 = $"{type}#{methodBase.Name}";
            return GetLogger(type1);
#else
            return EmptyLogger;
#endif
        }

        private static string GetCallerName([CallerMemberName]string caller = "", [CallerLineNumber]int number = 0, [CallerFilePath]string path = "")
        {
            var fileName = path.Split('\\')
                .LastOrDefault();

            if (path.Contains("SocketIo.Tests"))
            {
                path = "SocketIo.Tests";
            }
            else if (path.Contains("SocketIo"))
            {
                path = "SocketIo";
            }
            else if (path.Contains("EngineIo.Tests"))
            {
                path = "EngineIo.Tests";
            }
            else if (path.Contains("EngineIo"))
            {
                path = "EngineIo";
            }

            return $"{path}-{fileName}:{caller}#{number}";
        }

        #endregion

        public LogManager(string type)
        {
            MyType = type;
        }

        [Conditional("DEBUG")]
        public void Info(string msg)
        {
            // Trace.WriteLine(string.Format("{0} [{3}] {1} - {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff"), MyType, msg, System.Threading.Thread.CurrentThread.ManagedThreadId));
            if (!Enabled)
            {
                return;
            }

            if (file == null)
            {
                var logFile = File.Create(myFileName);
                file = new StreamWriter(logFile)
                {
                    AutoFlush = true
                };
            }

            // http://stackoverflow.com/questions/8767103/how-to-remove-invalid-code-points-from-a-string
            // System.Threading.Thread.CurrentThread.ManagedThreadId);
            file.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")} [{""}] {MyType} - {_invalidCharacters.Replace(msg, "")}");
        }

        [Conditional("DEBUG")]
        public void Error(string p, Exception exception)
        {
            Info($"ERROR {p} {exception.Message} {exception.StackTrace}");
            if (exception.InnerException != null)
            {
                Info($"ERROR exception.InnerException {p} {exception.InnerException.Message} {exception.InnerException.StackTrace}");
            }
        }

        [Conditional("DEBUG")]
        internal void Error(Exception e)
        {
            Error("", e);
        }
    }
}
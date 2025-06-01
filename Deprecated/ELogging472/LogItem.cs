using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ELogging
{
    public class LogItem
    {
        public class LogThreadInfo
        {
            public int Id { get; private set; }

            public string Name { get; private set; }

            private LogThreadInfo() { }

            public static LogThreadInfo Create()
            {
                Thread t = Thread.CurrentThread;
                LogThreadInfo ret = new LogThreadInfo()
                {
                    Name = t.Name,
                    Id = t.ManagedThreadId
                };
                return ret;
            }
        }

        public LogLevel Level { get; set; }

        public string Message { get; set; }

        public object Sender { get; set; }

        public string SenderName { get; set; }

        public LogThreadInfo ThreadInfo { get; set; }

        public LogItem(object sender, string senderName, LogLevel level, string message)
        {
            Sender = sender ?? throw new ArgumentNullException(nameof(sender));
            SenderName = senderName ?? throw new ArgumentNullException(nameof(senderName));
            Level = level;
            ThreadInfo = LogThreadInfo.Create();
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }
}

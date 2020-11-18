using ICSharpCode.SharpZipLib.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace IngestTask.Tools
{
    abstract class MathConstants//继承
    {
        public const double Pi = 3.14;
    }
    public class IngestTaskConfig
    {
        public int TaskSchedulePrevious { get; set; }
        public int TaskStartPrevious { get; set; }
        public int TaskStopBehind { get; set; }
        public int TaskRedispatchSpan { get; set; }
    }
    public class ApplicationConfig: IngestTaskConfig
    {
        public string ConnectionString { get; set; }
        public string IngestDBUrl { get; set; }
        public string IngestMatrixUrl { get; set; }
        public string IngestVtrUrl { get; set; }
        public string VIP { get; set; }
        public string CMServerUrl { get; set; }
        public string CMServerWindowsUrl { get; set; }
        public string KafkaUrl { get; set; }
    }
}

using ICSharpCode.SharpZipLib.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace IngestTask.Tools
{
    public class IngestTaskConfig
    {
        //public int Task
    }
    public class ApplicationConfig
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

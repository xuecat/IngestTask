using System;
using System.Collections.Generic;
using System.Text;

namespace IngestTask.Dto
{
    public class CompleteSyncTask
    {
        /// <summary>暂时无</summary>
        /// <example>false</example>
        public bool IsFinish { get; set; }
        /// <summary>暂时无</summary>
        /// <example>false</example>
        public bool Perodic2Next { get; set; }
        /// <summary>暂时无</summary>
        /// <example>0</example>
        public int TaskID { get; set; }
        /// <summary>暂时无</summary>
        /// <example>0</example>
        public int TaskState { get; set; }
        /// <summary>暂时无</summary>
        /// <example>0</example>
        public int DispatchState { get; set; }
        /// <summary>暂时无</summary>
        /// <example>0</example>
        public int SynState { get; set; }
    }
}

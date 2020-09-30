using IngestTask.Tools.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace IngestTask.Abstraction.Grains
{
    [Serializable]
    public class TaskEvent
    {
        public int TaskId { get; set; }
        public opType OpType { get; set; }

    }
}

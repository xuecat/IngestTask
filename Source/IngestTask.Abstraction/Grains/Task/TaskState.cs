using IngestTask.Tools.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace IngestTask.Abstraction.Grains
{
    [Serializable]
    public class TaskState
    {
        public int TaskId { get; set; }
        public taskState TaskStatus { get; set; }
    }
}

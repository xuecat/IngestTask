using IngestTask.Tools.Dto;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace IngestTask.Abstraction.Grains
{
    [ProtoContract]
    public class TaskState
    {
        [ProtoMember(1)]
        public int TaskId { get; set; }
        [ProtoMember(2)]
        public taskState TaskStatus { get; set; }
    }
}

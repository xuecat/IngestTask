using System;
using System.Collections.Generic;
using System.Text;

namespace IngestTask.Dto
{
    [Serializable]
    public class TaskFullInfo : TaskAllInfo
    {
        public bool HandleTask { get; set; }
        public bool StartOrStop { get; set; }
        public int RetryTimes { get; set; }
        public int OldChannelId { get; set; }

        public dispatchState DispatchState { get; set; }

        public syncState SyncState { get; set; }

        public opType OpType { get; set; }

        public DateTime NewBeginTime { get; set; } = DateTime.MinValue;

        public DateTime NewEndTime { get; set; } = DateTime.MinValue;
    }

    [Serializable]
    public class CheckTaskContent : TaskContent
    {
        public int SyncTimes { get; set; }
    }

    /*
     * 服务和分发在用，其它都用taskcontent. 为啥通知出来是这个结构体呢，主要兼容v1 v2不同的添加信息，而且还需要oldchannel等信息，所以就使用俩个结构体
     * 就当成分发层结构体多些，执行层少些
     */
    [Serializable]
    public partial class DispatchTask
    {
        public int Taskid { get; set; }
        public string Taskname { get; set; }
        public int? Recunitid { get; set; }
        public string Usercode { get; set; }
        public int? Signalid { get; set; }
        public int? Channelid { get; set; }
        public int? OldChannelid { get; set; }
        public int? State { get; set; }
        public DateTime Starttime { get; set; }
        public DateTime Endtime { get; set; }
        public DateTime NewBegintime { get; set; }
        public DateTime NewEndtime { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public int? Tasktype { get; set; }
        public int? Backtype { get; set; }
        public int? DispatchState { get; set; }
        public int? SyncState { get; set; }
        public int? OpType { get; set; }
        public string Tasklock { get; set; }
        public string Taskguid { get; set; }
        public int? Backupvtrid { get; set; }
        public int? Taskpriority { get; set; }
        public int? Stamptitleindex { get; set; }
        public int? Stampimagetype { get; set; }
        public int? Sgroupcolor { get; set; }
    }

    [Serializable]
    public class TaskContent
    {
        /// <summary>任务id</summary>
        /// <example>1</example>
        public int TaskId { get; set; }
        /// <summary>任务名</summary>
        /// <example>name</example>
        public string TaskName { get; set; }
        /// <summary>任务描述，也放任务图片</summary>
        /// <example>任务描述，也任务图片</example>
        public string TaskDesc { get; set; }
        /// <summary>任务周期属性</summary>
        /// <example>任务周期属性</example>
        public string Classify { get; set; }
        /// <summary>任务通道id</summary>
        /// <example>0</example>
        public int ChannelId { get; set; }
        /// <summary>任务</summary>
        /// <example>0</example>
        public int Unit { get; set; }
        /// <summary>用户的usercode</summary>
        /// <example>123456</example>
        public string UserCode { get; set; }
        /// <summary>信号源id</summary>
        /// <example>0</example>
        public int SignalId { get; set; }
        /// <summary>任务开始时间</summary>
        /// <example>2020-4-02 16:19:33</example>
        public string Begin { get; set; } = DateTime.MinValue.ToString("yyyy-MM-dd HH:mm:ss");
        /// <summary>任务结束时间</summary>
        /// <example>2020-4-02 16:19:33</example>
        public string End { get; set; } = DateTime.MinValue.ToString("yyyy-MM-dd HH:mm:ss");
        /// <summary>任务类型</summary>
        /// <example>TT_NORMAL</example>
        public TaskType TaskType { get; set; } = TaskType.TT_NORMAL;
        /// <summary>任务备份</summary>
        /// <example>2020-4-02 16:19:33</example>
        public CooperantType CooperantType { get; set; } = CooperantType.emPureTask;
        /// <summary>任务状态</summary>
        /// <example>normal</example>
        public taskState State { get; set; }
        /// <summary>任务图片</summary>
        /// <example>path</example>
        public string StampImage { get; set; }
        /// <summary>任务guid</summary>
        /// <example>guid</example>
        public string TaskGuid { get; set; }
        /// <summary>备份vtrid</summary>
        /// <example>1</example>
        public int BackupVtrId { get; set; }
        /// <summary>任务调度</summary>
        /// <example>TP_Normal</example>
        public TaskPriority Priority { get; set; } = TaskPriority.TP_Normal;
        /// <summary>任务分段图片位置</summary>
        /// <example>0</example>
        public int StampTitleIndex { get; set; }
        /// <summary>任务图片类型</summary>
        /// <example>1</example>
        public int StampImageType { get; set; }
        /// <summary>成组颜色 rgb值</summary>
        /// <example>0</example>
        public int GroupColor { get; set; }//注意没有S
    }
    public class TaskMaterialMeta
    {
        /// <summary>生成素材title</summary>
        /// <example>string</example>
        public string Title { get; set; }
        /// <summary>生成素材materialid</summary>
        /// <example>string</example>
        public string MaterialId { get; set; }
        /// <summary>权限</summary>
        /// <example>string</example>
        public string Rights { get; set; }
        /// <summary>评论</summary>
        /// <example>string</example>
        public string Comments { get; set; }
        /// <summary>生成素材路径目录</summary>
        /// <example>string</example>
        public string Destination { get; set; }
        /// <summary>生成素材目录类型</summary>
        /// <example>0</example>
        public int FolderId { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string ItemName { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string JournaList { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string CateGory { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string ProgramName { get; set; }
        /// <summary>暂时无</summary>
        /// <example>0</example>
        public int Datefolder { get; set; }
    }
    public class TaskSplit
    {
        public string VtrStart { get; set; }
    }

    public class PeriodParam
    {
        public string BeginDate { get; set; }
        public string EndDate { get; set; }
        public int AppDate { get; set; }
        public string AppDateFormat { get; set; }
        public int Mode { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:集合属性应为只读", Justification = "<挂起>")]
        public List<int> Params { get; set; }//DAY
    }
    public class TaskContentMeta
    {
        /// <summary>暂时无</summary>
        /// <example>0</example>
        public int HouseTc { get; set; }
        /// <summary>暂时无</summary>
        /// <example>0</example>
        public int PresetStamp { get; set; }
        /// <summary>暂时无</summary>
        public PeriodParam PeriodParam { get; set; }
        /// <summary>暂时无</summary>
        /// <example>0</example>
        public int SixteenToNine { get; set; }
        /// <summary>暂时无</summary>
        /// <example>0</example>
        public int SourceTapeID { get; set; }
        /// <summary>暂时无</summary>
        /// <example>0</example>
        public int DeleteFlag { get; set; }
        /// <summary>暂时无</summary>
        /// <example>0</example>
        public int SourceTapeBarcode { get; set; }
        /// <summary>暂时无</summary>
        /// <example>0</example>
        public int BackTapeId { get; set; }
        /// <summary>暂时无</summary>
        /// <example>0</example>
        public int UserMediaId { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string UserToken { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string VtrStart { get; set; }
        /// <summary>暂时无</summary>
        /// <example>0</example>
        public int TcMode { get; set; }
        /// <summary>暂时无</summary>
        /// <example>0</example>
        public int ClipSum { get; set; } = -1;
        /// <summary>暂时无</summary>
        /// <example>0</example>
        public int TransState { get; set; }
        public int AudioChannels { get; set; } = -1;
        public int AudioChannelAttribute { get; set; }
        public int ASRmask { get; set; } = -1;

        public string SignalRtmpUrl { get; set; }

    }

    public class TaskPlanning
    {
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string PlanGuid { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string PlanName { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string CreaToRName { get; set; }//这个很坑，但是要改客户端算了
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string CreateDate { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string ModifyName { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string ModifyDate { get; set; }
        /// <summary>暂时无</summary>
        /// <example>0</example>
        public int Version { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string Place { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string PlanningDate { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string Director { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string Photographer { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string Reporter { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string Other { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string Equipment { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string ContactInfo { get; set; }
        /// <summary>暂时无</summary>
        /// <example>string</example>
        public string PlanningXml { get; set; }
    }

    public class TaskAllInfo
    {
        /// <summary>是否备份任务</summary>
        /// <example>false</example>
        public bool BackUpTask { get; set; }
        /// <summary>任务来源</summary>
        /// <example>emMSVUploadTask</example>
        public TaskSource TaskSource { get; set; }
        /// <summary>任务基础元数据</summary>
        public TaskContent TaskContent { get; set; }
        /// <summary>任务素材元数据</summary>
        public TaskMaterialMeta MaterialMeta { get; set; }
        /// <summary>任务content元数据</summary>
        public TaskContentMeta ContentMeta { get; set; }
        /// <summary>任务planning元数据</summary>
        public TaskPlanning PlanningMeta { get; set; }
        /// <summary>任务split元数据，分裂的</summary>
        public TaskSplit SplitMeta { get; set; }
        /// <summary>任务采集参数</summary>
        /// <example>string</example>
        public string CaptureMeta { get; set; }
    }


    public class TaskSimpleTime
    {
        public int TaskId { get; set; }
        public DateTime Begin { get; set; }
        public DateTime End { get; set; }
    }
}

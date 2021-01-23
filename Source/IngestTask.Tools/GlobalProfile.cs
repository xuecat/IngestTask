
namespace IngestTask.Tool
{
    using AutoMapper;
    using IngestTask.Dto;
    using IngestTask.Tool;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class DateTimeTypeConverter : ITypeConverter<string, DateTime>
    {
        public DateTime Convert(string source, DateTime destination, ResolutionContext context)
        {
            return DateTimeFormat.DateTimeFromString(source);
        }
    }

    public class DateTimeStringTypeConverter : ITypeConverter<DateTime, string>
    {
        public string Convert(DateTime source, string destination, ResolutionContext context)
        {
            return DateTimeFormat.DateTimeToString(source);
        }
    }
    public class GlobalProfile: Profile
    {
        
        public GlobalProfile()
        {
            CreateMap<string, DateTime>().ConvertUsing(new DateTimeTypeConverter());
            CreateMap<DateTime, string>().ConvertUsing(new DateTimeStringTypeConverter());

            CreateMap<DeviceInfo, ChannelInfo>().ReverseMap();
            CreateMap<MsvChannelState, ChannelInfo>().ReverseMap();
            CreateMap<TaskContent, DispatchTask>()
                .ForMember(a => a.Recunitid, (map) => map.MapFrom(b => b.Unit))
                .ForMember(a => a.Category, (map) => map.MapFrom(b => b.Classify))
                .ForMember(a => a.Description, (map) => map.MapFrom(b => b.TaskDesc))
                .ForMember(a => a.Starttime, (map) => map.MapFrom(b => b.Begin))
                .ForMember(a => a.Endtime, (map) => map.MapFrom(b => b.End))
                .ForMember(a => a.Sgroupcolor, (map) => map.MapFrom(b => b.GroupColor))
                .ForMember(a => a.Stampimagetype, (map) => map.MapFrom(b => b.StampImageType))
                .ForMember(a => a.Taskpriority, (map) => map.MapFrom(b => b.Priority))
                .ForMember(a => a.Backtype, (map) => map.MapFrom(b => b.CooperantType)).ReverseMap();

            CreateMap<CheckTaskContent, TaskContent>().ReverseMap();

            CreateMap<TaskFullInfo, DispatchTask>()
                .ForMember(d => d.Taskid, (map) => map.MapFrom(b => b.TaskContent.TaskId))
                .ForMember(d => d.Taskname, (map) => map.MapFrom(b => b.TaskContent.TaskName))
                .ForMember(d => d.Recunitid, (map) => map.MapFrom(b => b.TaskContent.Unit))
                .ForMember(d => d.Usercode, (map) => map.MapFrom(b => b.TaskContent.UserCode))
                .ForMember(d => d.Signalid, (map) => map.MapFrom(b => b.TaskContent.SignalId))
                .ForMember(d => d.Channelid, (map) => map.MapFrom(b => b.TaskContent.ChannelId))
                .ForMember(d => d.OldChannelid, (map) => map.MapFrom(b => b.OldChannelId))
                .ForMember(d => d.State, (map) => map.MapFrom(b => b.TaskContent.State))
                .ForMember(d => d.Starttime, (map) => map.MapFrom(b => b.TaskContent.Begin))
                .ForMember(d => d.Endtime, (map) => map.MapFrom(b => b.TaskContent.End))
                .ForMember(d => d.NewBegintime, (map) => map.MapFrom(b => b.NewBeginTime))
                .ForMember(d => d.NewEndtime, (map) => map.MapFrom(b => b.NewEndTime))
                .ForMember(d => d.Category, (map) => map.MapFrom(b => b.TaskContent.Classify))
                .ForMember(d => d.Description, (map) => map.MapFrom(b => b.TaskContent.TaskDesc))
                .ForMember(d => d.Tasktype, (map) => map.MapFrom(b => b.TaskContent.TaskType))
                .ForMember(d => d.Backtype, (map) => map.MapFrom(b => b.BackUpTask))
                .ForMember(d => d.DispatchState, (map) => map.MapFrom(b => b.DispatchState))
                .ForMember(d => d.SyncState, (map) => map.MapFrom(b => b.SyncState))
                .ForMember(d => d.OpType, (map) => map.MapFrom(b => b.OpType))
                .ForMember(d => d.Taskguid, (map) => map.MapFrom(b => b.TaskContent.TaskGuid));
        }
    }
}

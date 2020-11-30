
namespace IngestTask.Server
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
        }
    }
}

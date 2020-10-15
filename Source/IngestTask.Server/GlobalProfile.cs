using AutoMapper;
using IngestTask.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IngestTask.Server
{
    public class GlobalProfile: Profile
    {
        public GlobalProfile()
        {
            CreateMap<DeviceInfo, ChannelInfo>().ReverseMap();
            CreateMap<MsvChannelState, ChannelInfo>().ReverseMap();
        }
    }
}



namespace IngestTask.Grain
{
    using IngestTask.Dto;
    using Orleans.Streams;
    using Sobey.Core.Log;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    public class StreamObserver : IAsyncObserver<ChannelInfo>
    {
        private ILogger logger;
        private Func<ChannelInfo, bool> _ActionFunc;
        public StreamObserver(ILogger logger, Func<ChannelInfo, bool> func)
        {
            this.logger = logger;
            _ActionFunc = func;

        }

        public Task OnCompletedAsync()
        {
            this.logger.Info("message stream received stream completed event");
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex)
        {
            this.logger.Info($"is experiencing message delivery failure, ex :{ex}");
            return Task.CompletedTask;
        }

        public Task OnNextAsync(ChannelInfo item, StreamSequenceToken token = null)
        {
            if (token != null)
            {
                
            }
            return Task.CompletedTask;
        }
    }
}

namespace IngestTask.Grains
{
    using System;
    using System.Threading.Tasks;
    using Orleans;
    using Orleans.Runtime;
    using IngestTask.Abstractions.Constants;
    using IngestTask.Abstractions.Grains;

    public class ReminderGrain : Grain, IReminderGrain, IRemindable
    {
        private const string ReminderName = "SomeReminder";
        private string reminder;

        public Task SetReminderAsync(string reminder)
        {
            this.reminder = reminder;
            return Task.CompletedTask;
        }

        public override async Task OnActivateAsync()
        {
            // 每各一段时间推一次流，时间间隔应该再大点毕竟是reminder
            await this.RegisterOrUpdateReminder(
                    ReminderName,
                    TimeSpan.FromSeconds(150),
                    TimeSpan.FromSeconds(60))
                .ConfigureAwait(true);
            await base.OnActivateAsync().ConfigureAwait(true);
        }

        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            if (string.Equals(ReminderName, reminderName, StringComparison.Ordinal))
            {
                if (!string.IsNullOrEmpty(this.reminder))
                {
                    await this.PublishReminderAsync(this.reminder).ConfigureAwait(true);
                }
            }
        }

        private Task PublishReminderAsync(string reminder)
        {
            var streamProvider = this.GetStreamProvider(StreamProviderName.Default);
            var stream = streamProvider.GetStream<string>(Guid.Empty, StreamName.Reminder);
            return stream.OnNextAsync(reminder);
        }
    }
}

﻿using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrleansDashboard.Model;
using OrleansDashboard.Model.History;
using OrleansDashboard.Metrics.Details;
using OrleansDashboard.Metrics.History;
using OrleansDashboard.Metrics.TypeFormatting;
using System.Reflection;
using OrleansDashboard.Abstraction;

namespace OrleansDashboard
{
    [Reentrant]
    public class DashboardGrain : Grain, IDashboardGrain
    {
        const int DefaultTimerIntervalMs = 1000; // 1 second
        private readonly ITraceHistory history = new TraceHistory();
        private readonly ISiloDetailsProvider siloDetailsProvider;
        private readonly DashboardCounters counters = new DashboardCounters();
        private readonly TimeSpan updateInterval;
        private bool isUpdating;
        private DateTime startTime = DateTime.UtcNow;
        private DateTime lastRefreshTime = DateTime.UtcNow;

        public DashboardGrain(IOptions<DashboardOptions> options, ISiloDetailsProvider siloDetailsProvider)
        {
            this.siloDetailsProvider = siloDetailsProvider;

            updateInterval = TimeSpan.FromMilliseconds(Math.Max(options.Value.CounterUpdateIntervalMs, DefaultTimerIntervalMs));
        }

        private async Task EnsureCountersAreUpToDate()
        {
            if (isUpdating)
            {
                return;
            }

            var now = DateTime.UtcNow;

            if ((now - lastRefreshTime) < updateInterval)
            {
                return;
            }

            isUpdating = true;
            try
            {
                var metricsGrain = GrainFactory.GetGrain<IManagementGrain>(0);
                var activationCountTask = metricsGrain.GetTotalActivationCount();
                var simpleGrainStatsTask = metricsGrain.GetDetailedGrainStatistics();
                var siloDetailsTask = siloDetailsProvider.GetSiloDetails();

                await Task.WhenAll(activationCountTask, simpleGrainStatsTask, siloDetailsTask);

                RecalculateCounters(activationCountTask.Result, siloDetailsTask.Result, simpleGrainStatsTask.Result);

                lastRefreshTime = now;
            }
            finally
            {
                isUpdating = false;
            }
        }

        internal void RecalculateCounters(int activationCount, SiloDetails[] hosts,
            IList<DetailedGrainStatistic> simpleGrainStatistics)
        {
            counters.TotalActivationCount = activationCount;

            counters.TotalActiveHostCount = hosts.Count(x => x.SiloStatus == SiloStatus.Active);
            counters.TotalActivationCountHistory = counters.TotalActivationCountHistory.Enqueue(activationCount).Dequeue();
            counters.TotalActiveHostCountHistory = counters.TotalActiveHostCountHistory.Enqueue(counters.TotalActiveHostCount).Dequeue();

            // TODO - whatever max elapsed time
            var elapsedTime = Math.Min((DateTime.UtcNow - startTime).TotalSeconds, 100);

            foreach (var item in counters.Hosts)
            {
                foreach (var item2 in hosts)
                {
                    if (item2.SiloAddress == item.SiloAddress)
                    {
                        item2.ExtraData = item.ExtraData;
                    }
                }
            }

            counters.Hosts = hosts;

            var aggregatedTotals = history.GroupByGrainAndSilo().ToLookup(x => (x.Grain, x.SiloAddress));

            var grainstatts = simpleGrainStatistics.Select(x =>
            {
                var grainName = TypeFormatter.Parse(x.GrainType);
                
                if (MultiGrainAttribute.IsRecordClass(x.GrainType))
                {
                    if (x.GrainIdentity.PrimaryKeyString != null)
                    {
                        grainName += $":{x.GrainIdentity.PrimaryKeyString}";
                    }
                    else
                    {
                        grainName += $":{x.GrainIdentity.PrimaryKeyLong}";
                    }
                    
                }
                
                var siloAddress = x.SiloAddress.ToParsableString();

                var result = new SimpleGrainStatisticCounter
                {
                    //ActivationCount = x.ActivationCount,
                    ActivationCount = 1,
                    GrainType = grainName,
                    SiloAddress = siloAddress,
                    TotalSeconds = elapsedTime
                };

                foreach (var item in aggregatedTotals[(grainName, siloAddress)])
                {
                    result.TotalAwaitTime += item.ElapsedTime;
                    result.TotalCalls += item.Count;
                    result.TotalExceptions += item.ExceptionCount;
                }

                return result;
            }).ToArray();

            foreach (var item in counters.SimpleGrainStats)
            {
                if (item.ExtraData != null)
                {
                    foreach (var item2 in grainstatts)
                    {
                        if (item2.GrainType == item.GrainType && item2.SiloAddress == item.SiloAddress)
                        {
                            item2.ExtraData = item.ExtraData;
                            break;
                        }
                    }
                }
                
            }
            counters.SimpleGrainStats = grainstatts;
        }

        private async Task RecalculateTask(string grain)
        {
            foreach (var item in counters.SimpleGrainStats)
            {
                if (item.GrainType == grain)
                {
                    string grainname = item.GrainType;
                    var grainsinfo = grain.Split(":");
                    if (grainsinfo.Length > 1)
                    {
                        grainname = grainsinfo[0];
                    }
                    var type = TraceGrainAttribute.GetRecordTrace(grainname);
                    if (type != TaskTraceEnum.No)
                    {
                        var grin = GrainFactory.GetGrain<IDashboardTaskGrain>(0);
                        if (grin != null)
                        {
                            item.ExtraData = await grin.GetTaskTrace(item.GrainType, type);
                        }
                    }
                }
                
            }
        }

        public override Task OnActivateAsync()
        {
            startTime = DateTime.UtcNow;

            return base.OnActivateAsync();
        }

        public async Task<Immutable<DashboardCounters>> GetCounters()
        {
            await EnsureCountersAreUpToDate();

            return counters.AsImmutable();
        }

        public async Task<Immutable<Dictionary<string, Dictionary<string, GrainTraceEntry>>>> GetGrainTracing(string grain)
        {
            await EnsureCountersAreUpToDate();
            await RecalculateTask(grain);
            return history.QueryGrain(grain).AsImmutable();
        }

        public async Task<Immutable<Dictionary<string, GrainTraceEntry>>> GetClusterTracing()
        {
            await EnsureCountersAreUpToDate();

            return history.QueryAll().AsImmutable();
        }

        public async Task<Immutable<Dictionary<string, GrainTraceEntry>>> GetSiloTracing(string address)
        {
            await EnsureCountersAreUpToDate();

            return history.QuerySilo(address).AsImmutable();
        }

        public async Task<Immutable<Dictionary<string, GrainMethodAggregate[]>>> TopGrainMethods()
        {
            await EnsureCountersAreUpToDate();

            const int numberOfResultsToReturn = 5;
            
            var values = history.AggregateByGrainMethod().ToList();
            
            return new Dictionary<string, GrainMethodAggregate[]>{
                { "calls", values.OrderByDescending(x => x.Count).Take(numberOfResultsToReturn).ToArray() },
                { "latency", values.OrderByDescending(x => x.ElapsedTime / (double) x.Count).Take(numberOfResultsToReturn).ToArray() },
                { "errors", values.Where(x => x.ExceptionCount > 0 && x.Count > 0).OrderByDescending(x => x.ExceptionCount / x.Count).Take(numberOfResultsToReturn).ToArray() },
            }.AsImmutable();
        }
      
        public Task Init()
        {
            // just used to activate the grain
            return Task.CompletedTask;
        }

        public Task SubmitTracing(string siloAddress, Immutable<SiloGrainTraceEntry[]> grainTrace, object extradata = null)
        {
            history.Add(DateTime.UtcNow, siloAddress, grainTrace.Value);
            if (extradata != null)
            {
                foreach (var item in counters.Hosts)
                {
                    if (item.SiloAddress == siloAddress)
                    {
                        item.ExtraData = extradata;
                        break;
                    }
                }
                
            }

            return Task.CompletedTask;
        }
    }
}

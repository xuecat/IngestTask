

namespace IngestTask.Grain
{
    using Orleans.Internal;
    using Orleans.Placement;
    using Orleans.Runtime;
    using Orleans.Runtime.Placement;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ScheduleTaskPlacementSiloDirector : IPlacementDirector
    {
        private readonly SafeRandom random = new SafeRandom();
        public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
        {
            var silos = context.GetCompatibleSilos(target).OrderBy(s => s).ToArray();
            //尽量把每一个隔开
            long grainid = target.GrainIdentity.PrimaryKeyLong;
            if (silos.Length > grainid)
            {
                return Task.FromResult(silos[grainid]);
            }
            else
                return Task.FromResult(silos[random.Next(silos.Length)]);
        }
    }

    [Serializable]
    public class ScheduleTaskPlacementStrategy : PlacementStrategy
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ScheduleTaskPlacementStrategyAttribute : PlacementAttribute
    {
        public ScheduleTaskPlacementStrategyAttribute() :
            base(new ScheduleTaskPlacementStrategy())
        {
        }
    }
}

namespace IngestTask.Server
{
    using System;
    using Orleans.Hosting;

    /// <summary>
    /// <see cref="ISiloBuilder"/> extension methods.
    /// </summary>
    public static class SiloBuilderExtensions
    {
       
        public static ISiloBuilder UseIf(
            this ISiloBuilder siloBuilder,
            bool condition,
            Func<ISiloBuilder, ISiloBuilder> action)
        {
            if (siloBuilder is null)
            {
                throw new ArgumentNullException(nameof(siloBuilder));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (condition)
            {
                siloBuilder = action(siloBuilder);
            }

            return siloBuilder;
        }

       
        public static ISiloBuilder UseIf(
            this ISiloBuilder siloBuilder,
            Func<ISiloBuilder, bool> condition,
            Func<ISiloBuilder, ISiloBuilder> action)
        {
            if (siloBuilder is null)
            {
                throw new ArgumentNullException(nameof(siloBuilder));
            }

            if (condition is null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (condition(siloBuilder))
            {
                siloBuilder = action(siloBuilder);
            }

            return siloBuilder;
        }
    }
}

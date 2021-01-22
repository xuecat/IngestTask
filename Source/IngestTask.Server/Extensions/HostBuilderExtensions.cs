namespace IngestTask.Server
{
    using System;
    using Microsoft.Extensions.Hosting;

    
    public static class HostBuilderExtensions
    {
        
        public static IHostBuilder UseIf(
            this IHostBuilder hostBuilder,
            bool condition,
            Func<IHostBuilder, IHostBuilder> action)
        {
            if (hostBuilder is null)
            {
                throw new ArgumentNullException(nameof(hostBuilder));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (condition)
            {
                hostBuilder = action(hostBuilder);
            }

            return hostBuilder;
        }

       
        public static IHostBuilder UseIf(
            this IHostBuilder hostBuilder,
            Func<IHostBuilder, bool> condition,
            Func<IHostBuilder, IHostBuilder> action)
        {
            if (hostBuilder is null)
            {
                throw new ArgumentNullException(nameof(hostBuilder));
            }

            if (condition is null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (condition(hostBuilder))
            {
                hostBuilder = action(hostBuilder);
            }

            return hostBuilder;
        }

       
        public static IHostBuilder UseIfElse(
            this IHostBuilder hostBuilder,
            bool condition,
            Func<IHostBuilder, IHostBuilder> ifAction,
            Func<IHostBuilder, IHostBuilder> elseAction)
        {
            if (hostBuilder is null)
            {
                throw new ArgumentNullException(nameof(hostBuilder));
            }

            if (ifAction is null)
            {
                throw new ArgumentNullException(nameof(ifAction));
            }

            if (elseAction is null)
            {
                throw new ArgumentNullException(nameof(elseAction));
            }

            if (condition)
            {
                hostBuilder = ifAction(hostBuilder);
            }
            else
            {
                hostBuilder = elseAction(hostBuilder);
            }

            return hostBuilder;
        }

       
        public static IHostBuilder UseIfElse(
            this IHostBuilder hostBuilder,
            Func<IHostBuilder, bool> condition,
            Func<IHostBuilder, IHostBuilder> ifAction,
            Func<IHostBuilder, IHostBuilder> elseAction)
        {
            if (hostBuilder is null)
            {
                throw new ArgumentNullException(nameof(hostBuilder));
            }

            if (condition is null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            if (ifAction is null)
            {
                throw new ArgumentNullException(nameof(ifAction));
            }

            if (elseAction is null)
            {
                throw new ArgumentNullException(nameof(elseAction));
            }

            if (condition(hostBuilder))
            {
                hostBuilder = ifAction(hostBuilder);
            }
            else
            {
                hostBuilder = elseAction(hostBuilder);
            }

            return hostBuilder;
        }
    }
}

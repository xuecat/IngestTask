namespace IngestTask.Server
{
    using System;
    using Microsoft.Extensions.Configuration;

   
    public static class ConfigurationBuilderExtensions
    {
        
        public static IConfigurationBuilder AddIf(
            this IConfigurationBuilder configurationBuilder,
            bool condition,
            Func<IConfigurationBuilder, IConfigurationBuilder> action)
        {
            if (configurationBuilder is null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (condition)
            {
                configurationBuilder = action(configurationBuilder);
            }

            return configurationBuilder;
        }

        
        public static IConfigurationBuilder AddIfElse(
            this IConfigurationBuilder configurationBuilder,
            bool condition,
            Func<IConfigurationBuilder, IConfigurationBuilder> ifAction,
            Func<IConfigurationBuilder, IConfigurationBuilder> elseAction)
        {
            if (configurationBuilder is null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
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
                configurationBuilder = ifAction(configurationBuilder);
            }
            else
            {
                configurationBuilder = elseAction(configurationBuilder);
            }

            return configurationBuilder;
        }
    }
}

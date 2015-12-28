using Microsoft.Azure.WebJobs.Extensions.GroupQueueTrigger;
using Microsoft.Azure.WebJobs.Host.Config;
using System;

namespace Microsoft.Azure.WebJobs
{
    /// <summary>
    /// JobHost Configuration para [GroupQueueTrigger]
    /// </summary>
    public static class GroupQueueTriggerJobHostConfigurationExtensions
    {
        public static void UseGroupQueueTriggers(this JobHostConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            config.RegisterExtensionConfigProvider(new GroupQueueTriggerConfig());
        }

        private class GroupQueueTriggerConfig : IExtensionConfigProvider
        {
            public void Initialize(ExtensionConfigContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException("context");
                }
                
                context.Config.RegisterBindingExtensions(new GroupQueueTriggerAttributeBindingProvider(context.Config.StorageConnectionString));                
            }
        }
    }

}

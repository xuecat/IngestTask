using System;
using System.Collections.Generic;
using System.Text;

namespace IngestTask.Tools
{
    public class ApplicationContext : ApplicationConfig
    {
        public static ApplicationContext Current { get; private set; }

        public ApplicationContext()
        {
            Current = this;
        }

    }
}

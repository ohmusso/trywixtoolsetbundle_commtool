using System;
using System.Collections.Generic;
using System.Text;

namespace WpfApp1
{
    using WixToolset.BootstrapperApplicationApi;
    // using WixToolset.BootstrapperApplications.Managed;

    public class Program
    {
        public static int Main(string[] args)
        {
            var app = new CustomBA();

            ManagedBootstrapperApplication.Run(app);

            return 0;
        }
    }
}

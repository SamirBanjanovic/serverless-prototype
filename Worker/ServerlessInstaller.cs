using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Serverless.Worker
{
    [RunInstaller(true)]
    public class ServerlessInstaller : Installer
    {
        private ServiceInstaller serviceInstaller;
        private ServiceProcessInstaller serviceProcessInstaller;

        public ServerlessInstaller()
        {
            serviceInstaller = new ServiceInstaller
            {
                StartType = ServiceStartMode.Manual,
                ServiceName = "Azureless Service"
            };

            this.Installers.Add(value: serviceInstaller);

            serviceProcessInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem
            };

            this.Installers.Add(value: serviceProcessInstaller);
        }
    }
}

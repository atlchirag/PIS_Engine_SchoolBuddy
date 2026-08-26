using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceProcess;
using System.Configuration.Install;
using System.ComponentModel;

namespace PIS_Engine
{
    [RunInstaller(true)]
    public class CustomServiceInstaller : Installer
    {
        private ServiceProcessInstaller process;
        private ServiceInstaller service;

        public CustomServiceInstaller()
        {
           
            process = new ServiceProcessInstaller();
            process.Account = ServiceAccount.LocalSystem;

            service = new ServiceInstaller();
            service.ServiceName = "PISEngine";
            service.DisplayName = "PISEngine";
            service.StartType = ServiceStartMode.Automatic;

            Installers.Add(process);
            Installers.Add(service);
        }

        protected override void OnCommitted(System.Collections.IDictionary savedState)
        {
            ServiceController sc = new ServiceController("PISEngine");
            sc.Start();
        }

      
    }
}

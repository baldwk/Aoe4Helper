using AduSkin.Demo.Servers;
using AduSkin.Demo.Servers.Extensions;
using AduSkin.Demo.ViewModel;
using Aoe4Helper.Servers.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Reflection;
using System.Text;
using System.Windows;

namespace Aoe4Helper
{
   public partial class App : Application
   {
      public static Assembly Asssembly => Assembly.GetExecutingAssembly();

      private static readonly IHost _host = Host.CreateDefaultBuilder()
          .ConfigureAppConfiguration(c =>
          {
             _ = c.SetBasePath(AppContext.BaseDirectory);
          })
         .ConfigureServices((context, services) =>
         {
            services.AddHostedService<ApplicationHostService>();

            services.AddSingleton<IWindow, MainWindow>();
            services.AddSingleton<MainViewModel>();

            services.AddTransientFromNamespace("AduSkin.Demo.Views", Asssembly);

            services.AddTransientFromNamespace("AduSkin.Demo.ViewModel", Asssembly);

         }).Build();
      public App()
      {
         Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      }
      protected override void OnStartup(StartupEventArgs e)
      {
         _host.Start();
      }
      protected override void OnExit(ExitEventArgs e)
      {
         _host.StopAsync().Wait();
         _host.Dispose();
      }
   }
}
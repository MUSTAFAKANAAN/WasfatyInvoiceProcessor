using AutoUpdaterDotNET;
using System.Windows;

namespace WasfatyInvoiceProcessor;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AutoUpdater.Start("https://raw.githubusercontent.com/MUSTAFAKANAAN/WasfatyInvoiceProcessor/master/WasfatyInvoiceProcessor/update.xml");
    }
}

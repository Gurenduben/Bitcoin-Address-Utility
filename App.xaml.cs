using System.Windows;

namespace BtcAddress
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            var mainForm = new BtcAddress.Forms.KeyCollectionView();
            mainForm.FormClosed += (_, __) => Shutdown();
            mainForm.Show();
        }
    }
}

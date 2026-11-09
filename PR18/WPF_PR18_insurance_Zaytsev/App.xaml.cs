using System.Windows;
using WPF_PR18_insurance_Zaytsev.ApplicationData;
using WPF_PR18_insurance_Zaytsev.MainPage;

namespace WPF_PR18_insurance_Zaytsev;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var repository = new InsuranceRepository();

        while (new LoginWindow(repository).ShowDialog() == true)
        {
            var workspace = new WorkspaceWindow(repository);
            MainWindow = workspace;
            workspace.ShowDialog();
            repository.SignOut();
            if (!workspace.SignedOut) break;
        }

        Shutdown();
    }
}

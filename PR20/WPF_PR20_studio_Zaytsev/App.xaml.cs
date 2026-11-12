using System.Windows;
using WPF_PR20_studio_Zaytsev.ApplicationData;
using WPF_PR20_studio_Zaytsev.MainPage;

namespace WPF_PR20_studio_Zaytsev;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var repository = new StudioRepository();

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

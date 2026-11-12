using System.ComponentModel;
using System.Windows;
using WPF_PR20_studio_Zaytsev.ApplicationData;

namespace WPF_PR20_studio_Zaytsev.MainPage;

public partial class LoginWindow : Window
{
    private readonly StudioRepository repository;
    private bool busy;

    internal LoginWindow(StudioRepository repository)
    {
        this.repository = repository;
        InitializeComponent();
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        string login = LoginBox.Text.Trim();
        string password = PasswordBox.Password;
        if (login.Length == 0 || password.Length == 0)
        {
            ErrorText.Text = "Введите логин и пароль.";
            return;
        }

        busy = true;
        Form.IsEnabled = false;
        ErrorText.Text = "";
        bool signedIn = false;
        try
        {
            signedIn = await repository.SignInAsync(login, password);
            if (!signedIn)
                ErrorText.Text = "Неверный логин или пароль.";
        }
        catch (Exception exception)
        {
            UiMessages.ShowError(this, exception);
        }
        finally
        {
            busy = false;
            Form.IsEnabled = true;
            PasswordBox.Clear();
        }

        if (signedIn)
            DialogResult = true;
        else
            PasswordBox.Focus();
    }

    private void Window_Closing(object? sender, CancelEventArgs e) => e.Cancel = busy;
}

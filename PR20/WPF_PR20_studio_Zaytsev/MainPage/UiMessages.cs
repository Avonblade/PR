using System.Windows;
using Microsoft.Data.SqlClient;
using WPF_PR20_studio_Zaytsev.ApplicationData;

namespace WPF_PR20_studio_Zaytsev.MainPage;

internal static class UiMessages
{
    public static string ErrorText(Exception exception) => exception switch
    {
        StudioRuleException => exception.Message,
        SqlException { Number: 2601 or 2627 } => "Запись уже существует. Проверьте название, логин или номер чека.",
        SqlException { Number: 547 } => "Запись противоречит ограничениям базы данных.",
        SqlException { Number: 1205 or 50010 } => "Данные заняты. Повторите операцию.",
        SqlException => "Ошибка SQL Server. Проверьте подключение к базе.",
        _ => exception.Message
    };

    public static void ShowError(Window owner,Exception exception) =>
        MessageBox.Show(owner,ErrorText(exception),owner.Title,MessageBoxButton.OK,MessageBoxImage.Warning);
}

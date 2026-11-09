using System.Windows;
using Microsoft.Data.SqlClient;
using WPF_PR19_fitness_Zaytsev.ApplicationData;

namespace WPF_PR19_fitness_Zaytsev.MainPage;

internal static class UiMessages
{
    public static string ErrorText(Exception exception) => exception switch
    {
        FitnessRuleException => exception.Message,
        SqlException { Number: 2601 or 2627 } => "Такая запись уже существует. Проверьте карту, логин или название.",
        SqlException { Number: 547 } => "Запись противоречит ограничениям базы данных.",
        SqlException { Number: 1205 or 50010 } => "Данные заняты. Повторите операцию.",
        SqlException => "Ошибка SQL Server. Проверьте подключение к базе.",
        _ => exception.Message
    };

    public static void ShowError(Window owner, Exception exception) =>
        MessageBox.Show(owner, ErrorText(exception), owner.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
}

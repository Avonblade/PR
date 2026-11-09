using System.Windows;
using Microsoft.Data.SqlClient;

namespace WPF_PR18_insurance_Zaytsev.MainPage;

internal static class UiMessages
{
    public static void ShowError(Window owner, Exception exception)
    {
        string message = exception switch
        {
            SqlException { Number: 2601 or 2627 } => "Запись с таким номером или названием уже существует.",
            SqlException { Number: 547 } => "Нарушены условия записи или существуют связанные данные.",
            SqlException { Number: 1205 } => "Другой сотрудник одновременно изменил данные. Повторите операцию.",
            SqlException => "Ошибка SQL Server. Проверьте подключение к базе.",
            _ => exception.Message
        };
        MessageBox.Show(owner, message, owner.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}

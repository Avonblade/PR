using System.Windows;
using Microsoft.Data.SqlClient;

namespace WPF_PR17_restaurant_Zaytsev.MainPage;

internal static class UiMessages
{
    public static void ShowError(Window owner, Exception exception)
    {
        string message = exception is SqlException
            ? "Ошибка SQL Server. Проверьте, что сервер запущен, база установлена и строка подключения верна."
            : exception.Message;
        MessageBox.Show(owner, message, owner.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}

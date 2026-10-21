using Microsoft.Data.SqlClient;
using System.Windows;

namespace WPF_PR16_clothes_ZaytsevVI.MainPage;

public static class UiMessages
{
    public static string ErrorText(Exception exception) => exception switch
    {
        SqlException { Number: 2601 or 2627 } => "Товар с таким артикулом уже существует. Укажите другой артикул.",
        SqlException { Number: 547 } => "Не удалось сохранить данные: проверьте выбранные справочники, цену, размер и цвет товара.",
        SqlException => "Нет доступа к базе данных. Проверьте SQL Server, выполните Database\\01_CreateAndSeed.sql и проверьте connection.txt рядом с EXE.\n\n" + exception.Message,
        _ => exception.Message
    };

    public static void ShowError(Window owner, Exception exception) =>
        MessageBox.Show(owner, ErrorText(exception), "Не удалось выполнить действие", MessageBoxButton.OK, MessageBoxImage.Warning);
}

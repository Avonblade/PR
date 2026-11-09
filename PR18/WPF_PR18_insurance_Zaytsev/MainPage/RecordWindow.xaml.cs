using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using WPF_PR18_insurance_Zaytsev.ApplicationData;

namespace WPF_PR18_insurance_Zaytsev.MainPage;

public partial class RecordWindow : Window
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");
    private readonly InsuranceRepository repository;
    private readonly Entity entity;
    private readonly RecordRow? original;
    private readonly Control[] inputs;
    private bool saving;

    internal RecordWindow(InsuranceRepository repository, Entity entity, RecordRow? row, EditData data)
    {
        this.repository = repository;
        this.entity = entity;
        original = row;
        InitializeComponent();
        Title = row is null ? "Добавление записи" : "Редактирование записи";
        SectionText.Text = entity.Title.ToUpper(Russian);
        inputs = new Control[entity.Fields.Length];

        int gridRow = 0;
        int column = 0;
        for (int i = 0; i < entity.Fields.Length; i++)
        {
            var field = entity.Fields[i];
            var input = CreateInput(field, data.Values[i], data.Lookups);
            inputs[i] = input;
            bool fullWidth = field.Length >= 150;
            if (fullWidth && column == 1)
            {
                gridRow++;
                column = 0;
            }
            if (FieldsPanel.RowDefinitions.Count == gridRow)
                FieldsPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            string label = field.Kind == FieldKind.Date ? $"{field.Label} (ДД.ММ.ГГГГ)" : field.Label;
            var cell = new StackPanel { Margin = new Thickness(0, 0, column == 0 && !fullWidth ? 12 : 0, 9) };
            cell.Children.Add(new Label { Content = field.Required ? $"{label} *" : label, Target = input });
            cell.Children.Add(input);
            Grid.SetRow(cell, gridRow);
            Grid.SetColumn(cell, column);
            if (fullWidth) Grid.SetColumnSpan(cell, 2);
            FieldsPanel.Children.Add(cell);

            if (column == 1 || fullWidth)
            {
                gridRow++;
                column = 0;
            }
            else
                column = 1;
        }

        Loaded += (_, _) => inputs[0].Focus();
    }

    private static Control CreateInput(Field field, object? value, IReadOnlyDictionary<string, List<ChoiceItem>> lookups)
    {
        if (field.Kind == FieldKind.Flag)
            return new CheckBox { Content = "Подтверждён бухгалтерией", IsChecked = value is true, MinHeight = 38 };

        if (field.Kind is FieldKind.Lookup or FieldKind.Choice)
        {
            var items = field.Kind == FieldKind.Lookup
                ? new List<ChoiceItem>(lookups[field.Name])
                : field.Options!.Select(option => new ChoiceItem(option, option)).ToList();
            if (!field.Required) items.Insert(0, new ChoiceItem(null, "—"));
            return new ComboBox
            {
                ItemsSource = items,
                DisplayMemberPath = nameof(ChoiceItem.Caption),
                SelectedItem = items.FirstOrDefault(item => Equals(item.Value, value)),
                ToolTip = field.Label
            };
        }

        bool multiline = field.Length >= 150;
        return new TextBox
        {
            Text = value switch
            {
                DateTime date => date.ToString("dd.MM.yyyy", Russian),
                decimal amount => amount.ToString("0.00", Russian),
                string text => text,
                _ => ""
            },
            MaxLength = field.Kind switch { FieldKind.Date => 10, FieldKind.Money => 13, _ => field.Length },
            MinHeight = multiline ? 67 : 38,
            AcceptsReturn = multiline,
            TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            VerticalContentAlignment = multiline ? VerticalAlignment.Top : VerticalAlignment.Center,
            VerticalScrollBarVisibility = multiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden
        };
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var values = new object?[inputs.Length];
        for (int i = 0; i < inputs.Length; i++)
        {
            var field = entity.Fields[i];
            if (TryRead(field, inputs[i], out values[i], out string error)) continue;
            ErrorText.Text = error;
            inputs[i].Focus();
            return;
        }

        if (entity.Table == "GovernmentRecords")
        {
            int kind = Array.FindIndex(entity.Fields, field => field.Name == "Kind");
            int amount = Array.FindIndex(entity.Fields, field => field.Name == "Amount");
            bool isTax = Equals(values[kind], "Налоговое отчисление");
            if (isTax != (values[amount] is not null))
            {
                ErrorText.Text = isTax ? "Укажите сумму налогового отчисления." : "Очистите сумму для статистического отчёта.";
                inputs[amount].Focus();
                return;
            }
        }

        saving = true;
        Form.IsEnabled = false;
        ErrorText.Text = "";
        bool saved = false;
        try
        {
            await repository.SaveAsync(entity, original, values);
            saved = true;
        }
        catch (InvalidOperationException exception)
        {
            ErrorText.Text = exception.Message;
        }
        catch (Exception exception)
        {
            UiMessages.ShowError(this, exception);
        }
        finally
        {
            saving = false;
            Form.IsEnabled = true;
        }

        if (saved) DialogResult = true;
    }

    private static bool TryRead(Field field, Control input, out object? value, out string error)
    {
        error = $"Заполните поле «{field.Label}».";
        value = null;
        if (input is CheckBox flag)
        {
            value = flag.IsChecked == true;
            return true;
        }
        if (input is ComboBox choice)
        {
            value = (choice.SelectedItem as ChoiceItem)?.Value;
            return value is not null || !field.Required;
        }

        string text = ((TextBox)input).Text.Trim();
        if (text.Length == 0) return !field.Required;
        if (field.Kind == FieldKind.Date)
        {
            if (DateTime.TryParseExact(text, "dd.MM.yyyy", Russian, DateTimeStyles.None, out DateTime date) &&
                (field.Name != "BirthDate" || date <= DateTime.Today))
            {
                value = date;
                return true;
            }
            error = field.Name == "BirthDate"
                ? "Введите дату рождения в формате ДД.ММ.ГГГГ. Она не может быть в будущем."
                : $"Введите «{field.Label}» в формате ДД.ММ.ГГГГ.";
            return false;
        }
        if (field.Kind == FieldKind.Money)
        {
            text = text.Replace('.', ',');
            int separator = text.IndexOf(',');
            if (decimal.TryParse(text, NumberStyles.AllowDecimalPoint, Russian, out decimal amount) &&
                amount is > 0 and <= 9_999_999_999.99m && (separator < 0 || text.Length - separator <= 3))
            {
                value = amount;
                return true;
            }
            error = $"Укажите положительную сумму в поле «{field.Label}», не больше двух знаков после запятой.";
            return false;
        }
        value = text;
        return true;
    }

    private void Window_Closing(object? sender, CancelEventArgs e) => e.Cancel = saving;
}

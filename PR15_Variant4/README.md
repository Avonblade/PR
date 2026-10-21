# Практическая работа 15 — вариант 4

Мебель для кухни. 18 товаров, 6 категорий, 5 материалов. Добавление, изменение и удаление товаров, поиск, фильтрация и сортировка. Отдельное модальное окно товара. Папки Resorce, ApplicationData и MainPage соответствуют заданию. Структура базы описана в DATABASE.md.

## Запуск

Требуются Windows, .NET SDK 9 и MS SQL Server / Express. Выполняйте команды из этой папки.

1. Создайте базу, выполнив весь `Database/01_CreateAndSeed.sql` в SSMS либо:

```powershell
sqlcmd -S '.\SQLEXPRESS' -E -C -b -f 65001 -i '.\Database\01_CreateAndSeed.sql'
```

2. При необходимости измените имя сервера в `WPF_PR15_Mebel_ZaytsevVI/connection.txt`. База: `PR15_KitchenFurniture`. Используется Windows-аутентификация. Учётной записи нужны права чтения и изменения данных; для установки базы — права её создания.

3. Соберите и запустите:

```powershell
dotnet build .\WPF_PR15_Mebel_ZaytsevVI -c Release
dotnet run --project .\WPF_PR15_Mebel_ZaytsevVI
```

Или откройте `WPF_PR15_Mebel_ZaytsevVI.sln` в Visual Studio с поддержкой .NET 9 и WPF.

## Состав

- `WPF_PR15_Mebel_ZaytsevVI/` — исходники WPF и ресурсы.
- `WPF_PR15_Mebel_ZaytsevVI.sln` — решение Visual Studio.
- `Database/01_CreateAndSeed.sql` — схема и начальное наполнение базы.
- `Install-Database.ps1` — установка через sqlcmd.

Для первичной установки SQL-скрипт создаёт отдельную базу и таблицы. Повторный импорт в существующие таблицы запрещён; имеющиеся данные сохраняются. NuGet-пакеты восстанавливаются при сборке. Не публикуйте пароли SQL Server в connection.txt; по умолчанию в нём нет секретов.

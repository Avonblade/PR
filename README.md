# Практические работы: WPF + MS SQL Server

Каждая работа оформлена отдельным проектом с исходниками WPF, SQL-скриптом и инструкцией запуска. Сборки EXE и автоматически создаваемые bin/obj в репозиторий не включены.

| Папка | Работа | Проект | База данных |
| --- | --- | --- | --- |
| [Task09](Task09/README.md) | Сессия 1 — языковая школа | SchoolApp | Task09School |
| [PR15_Variant4](PR15_Variant4/README.md) | ПР 15, вариант 4 — мебель для кухни | WPF_PR15_Mebel_ZaytsevVI | PR15_KitchenFurniture |
| [PR16_Variant4](PR16_Variant4/README.md) | ПР 16, вариант 4 — куртки, джемперы, жакеты | WPF_PR16_clothes_ZaytsevVI | PR16_AlisaClothes |
| [PR17](PR17/README.md) | ПР 17 — ресторан «Мармеладная зебра» | WPF_PR17_restaurant_Zaytsev | PR17_ZebraRestaurant |
| [PR18](PR18/README.md) | ПР 18 — страховая медицинская компания | WPF_PR18_insurance_Zaytsev | PR18_MedicalInsurance |
| [PR19](PR19/README.md) | ПР 19 — «Фитнес-учёт» | WPF_PR19_fitness_Zaytsev | PR19_Fitness |

## Требования

Windows, .NET SDK 9 с поддержкой WPF, SQL Server / SQL Server Express. Также можно открыть solution в Visual Studio с компонентом разработки классических приложений .NET. NuGet-пакеты восстанавливаются при сборке.

## Запуск

1. Откройте инструкцию нужной работы.
2. Выполните её SQL-скрипт через SSMS или sqlcmd. Каждая работа использует отдельную базу.
3. Проверьте connection.txt в папке проекта. По умолчанию сервер `.\SQLEXPRESS`, Windows-аутентификация.
4. Выполните `dotnet run --project <папка проекта>` либо запустите проект в Visual Studio.

SQL-скрипты предназначены для первичного создания и наполнения базы. При обнаружении существующих таблиц импорт останавливается; удаление пользовательских данных не выполняется.

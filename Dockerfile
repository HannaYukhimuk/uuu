# Используем официальный образ SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем файлы проекта и восстанавливаем зависимости
COPY *.csproj .
RUN dotnet restore

# Копируем все файлы и собираем приложение
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Используем образ рантайма для финального образа
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Копируем собранное приложение из промежуточного образа
COPY --from=build /app/publish .

# Открываем порт, который использует приложение
EXPOSE 80
EXPOSE 443

# Устанавливаем переменную среды для ASP.NET Core
ENV ASPNETCORE_URLS=http://+:80

# Запускаем приложение
ENTRYPOINT ["dotnet", "UserManagementApp.dll"] # замените YourProject на имя вашего проекта
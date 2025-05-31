# Используем образ SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Копируем csproj и восстанавливаем зависимости
COPY *.csproj ./
RUN dotnet restore

# Копируем остальной код и собираем
COPY . ./
RUN dotnet publish -c Release -o out

# Используем runtime-образ для запуска
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

# Открываем порт
EXPOSE 80

# Устанавливаем переменные окружения (если нужно)
ENV ASPNETCORE_URLS=http://+:80

# Запуск
ENTRYPOINT ["dotnet", "UserManagementApp.dll"]

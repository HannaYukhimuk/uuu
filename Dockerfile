# -------- Сборка проекта --------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем проектные файлы и восстанавливаем зависимости
COPY *.csproj ./
RUN dotnet restore

# Копируем остальное и публикуем
COPY . ./
RUN dotnet publish -c Release -o /app/out

# -------- Runtime-слой --------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/out ./

# Указываем порт, который откроет Render
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

# Точка входа
ENTRYPOINT ["dotnet", "UserManagementApp.dll"]

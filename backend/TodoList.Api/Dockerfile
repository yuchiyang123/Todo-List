FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY TodoList.Api/TodoList.Api.csproj TodoList.Api/
RUN dotnet restore "TodoList.Api/TodoList.Api.csproj"
COPY TodoList.Api/. TodoList.Api/
WORKDIR /src/TodoList.Api
RUN dotnet publish "TodoList.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/data
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TodoList.Api.dll"]

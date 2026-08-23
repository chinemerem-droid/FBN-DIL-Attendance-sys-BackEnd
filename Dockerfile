FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Employee_History.csproj .
RUN dotnet restore Employee_History.csproj
COPY . .
RUN dotnet publish Employee_History.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Render (and most PaaS) inject PORT; default to 8080 locally.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet Employee_History.dll"]

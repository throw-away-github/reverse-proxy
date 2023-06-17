FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["CF.AccessProxy/CF.AccessProxy.csproj", "CF.AccessProxy/"]
RUN dotnet restore "CF.AccessProxy/CF.AccessProxy.csproj"
COPY . .
WORKDIR "/src/CF.AccessProxy"
RUN dotnet build "CF.AccessProxy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CF.AccessProxy.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production
ENV CFAccess__ClientId=""
ENV CFAccess__ClientSecret=""
ENV CFAccess__Proxies=""

ENTRYPOINT ["dotnet", "CF.AccessProxy.dll"]

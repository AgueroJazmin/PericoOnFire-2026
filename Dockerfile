FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "PericoOnFire-2026.Server/PericoOnFire-2026.Server/PericoOnFire-2026.Server.csproj"
RUN dotnet publish "PericoOnFire-2026.Server/PericoOnFire-2026.Server/PericoOnFire-2026.Server.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "PericoOnFire-2026.Server.dll"]
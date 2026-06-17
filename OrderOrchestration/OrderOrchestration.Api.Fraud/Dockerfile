# Build context esperado: la carpeta OrderOrchestration/ (raíz de la solución .NET).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore OrderOrchestration.Api.Fraud/OrderOrchestration.Api.Fraud.csproj
RUN dotnet publish OrderOrchestration.Api.Fraud/OrderOrchestration.Api.Fraud.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderOrchestration.Api.Fraud.dll"]

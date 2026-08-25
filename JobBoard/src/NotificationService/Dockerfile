FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY JobBoard.Contracts/JobBoard.Contracts.csproj JobBoard.Contracts/
COPY NotificationService/NotificationService.Domain/NotificationService.Domain.csproj NotificationService/NotificationService.Domain/
COPY NotificationService/NotificationService.Application/NotificationService.Application.csproj NotificationService/NotificationService.Application/
COPY NotificationService/NotificationService.Infrastructure/NotificationService.Infrastructure.csproj NotificationService/NotificationService.Infrastructure/
COPY NotificationService/NotificationService.Api/NotificationService.Api.csproj NotificationService/NotificationService.Api/

RUN dotnet restore NotificationService/NotificationService.Api/NotificationService.Api.csproj -p:Configuration=Release

COPY JobBoard.Contracts/ JobBoard.Contracts/
COPY NotificationService/ NotificationService/

RUN dotnet publish NotificationService/NotificationService.Api/NotificationService.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "NotificationService.Api.dll"]

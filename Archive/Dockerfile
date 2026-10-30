# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["BGT.Order.slnx", "./"]
COPY ["src/BGT.Order.Domain/BGT.Order.Domain.csproj", "src/BGT.Order.Domain/"]
COPY ["src/BGT.Order.Application/BGT.Order.Application.csproj", "src/BGT.Order.Application/"]
COPY ["src/BGT.Order.Infrastructure/BGT.Order.Infrastructure.csproj", "src/BGT.Order.Infrastructure/"]
COPY ["src/BGT.Order.Api/BGT.Order.Api.csproj", "src/BGT.Order.Api/"]
COPY ["tests/BGT.Order.Tests/BGT.Order.Tests.csproj", "tests/BGT.Order.Tests/"]

RUN dotnet restore "BGT.Order.slnx"

COPY . .
RUN dotnet publish "src/BGT.Order.Api/BGT.Order.Api.csproj" \
    -c Release \
    -o /app/publish 

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "BGT.Order.Api.dll"]

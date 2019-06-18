FROM mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["EventBot/EventBot.csproj", "EventBot/"]
RUN dotnet restore "EventBot/EventBot.csproj"
COPY . .
WORKDIR "/src/EventBot"
RUN dotnet build "EventBot.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "EventBot.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "EventBot.dll"]
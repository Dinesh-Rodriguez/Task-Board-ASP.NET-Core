# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Copy project file and restore dependencies separately to leverage layer caching
COPY ["src/TaskBoard.Api/TaskBoard.Api.csproj", "src/TaskBoard.Api/"]
RUN dotnet restore "src/TaskBoard.Api/TaskBoard.Api.csproj"

# Copy source and publish
COPY . .
WORKDIR "/repo/src/TaskBoard.Api"
RUN dotnet publish "TaskBoard.Api.csproj" -c Release -o /app/publish --no-restore

# Runtime stage — smaller image, no SDK
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Run as non-root user
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "TaskBoard.Api.dll"]

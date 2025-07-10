FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first
COPY ["RotAndRuin.Api/RotAndRuin.Api.csproj", "RotAndRuin.Api/"]
COPY ["RotAndRuin.Application/RotAndRuin.Application.csproj", "RotAndRuin.Application/"]
COPY ["RotAndRuin.Infrastructure/RotAndRuin.Infrastructure.csproj", "RotAndRuin.Infrastructure/"]
COPY ["RotAndRuin.Domain/RotAndRuin.Domain.csproj", "RotAndRuin.Domain/"]

# Restore packages
RUN dotnet restore "RotAndRuin.Api/RotAndRuin.Api.csproj"

# Copy everything else
COPY . .

# Build and publish
RUN dotnet build "RotAndRuin.Api/RotAndRuin.Api.csproj" -c Release -o /app/build
RUN dotnet publish "RotAndRuin.Api/RotAndRuin.Api.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "RotAndRuin.Api.dll"]
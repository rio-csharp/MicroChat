# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY MicroChat.slnx .
COPY MicroChat/MicroChat.csproj MicroChat/
COPY MicroChat.Client/MicroChat.Client.csproj MicroChat.Client/

# Restore dependencies
RUN dotnet restore MicroChat/MicroChat.csproj

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR /src/MicroChat
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 3000

# Copy published files
COPY --from=publish /app/publish .

# Set environment variable for port
ENV ASPNETCORE_URLS=http://+:3000

ENTRYPOINT ["dotnet", "MicroChat.dll"]

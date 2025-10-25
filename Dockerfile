# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj and restore
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and publish
COPY . ./
RUN dotnet publish -c Release -o /out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

# Copy published output from build stage
COPY --from=build /out ./

# Add root CA certificate
COPY ./certs/rootca.cer /usr/local/share/ca-certificates/rootca.crt
RUN update-ca-certificates

# Set environment variable
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Run the app
ENTRYPOINT ["dotnet", "BackgroundService.dll"]

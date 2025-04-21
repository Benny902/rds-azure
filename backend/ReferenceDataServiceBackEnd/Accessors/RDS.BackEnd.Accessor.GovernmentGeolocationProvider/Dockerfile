# See https://aka.ms/customizecontainer to learn how to customize your debug container 
# and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Base image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Build stage - Used to compile the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["Accessors/RDS.BackEnd.Accessor.GovernmentGeolocationProvider/RDS.BackEnd.Accessor.GovernmentGeolocationProvider.csproj", "Accessors/RDS.BackEnd.Accessor.GovernmentGeolocationProvider/"]
RUN dotnet restore "./Accessors/RDS.BackEnd.Accessor.GovernmentGeolocationProvider/RDS.BackEnd.Accessor.GovernmentGeolocationProvider.csproj"

# Copy the remaining files and build the application
COPY . .
WORKDIR "/src/Accessors/RDS.BackEnd.Accessor.GovernmentGeolocationProvider"
RUN dotnet build "./RDS.BackEnd.Accessor.GovernmentGeolocationProvider.csproj" -c Release -o /app/build

# Publish stage - Used to create the final deployable application
FROM build AS publish
RUN dotnet publish "./RDS.BackEnd.Accessor.GovernmentGeolocationProvider.csproj" -c Release -o /app/publish /p:UseAppHost=false


# Final stage - Used for running the application
FROM base AS final

ARG GIT_COMMIT
ARG BUILD_TIME
ARG VERSION

ENV GIT_COMMIT=$GIT_COMMIT
ENV BUILD_TIME=$BUILD_TIME
ENV VERSION=$VERSION

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RDS.BackEnd.Accessor.GovernmentGeolocationProvider.dll"]
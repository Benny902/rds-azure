# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

WORKDIR /app
EXPOSE 80
EXPOSE 443


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Managers/RDS.BackEnd.Manager.Geolocation/RDS.BackEnd.Manager.Geolocation.csproj", "Managers/RDS.BackEnd.Manager.Geolocation/"]
RUN dotnet restore "./Managers/RDS.BackEnd.Manager.Geolocation/RDS.BackEnd.Manager.Geolocation.csproj"
COPY . .
WORKDIR "/src/Managers/RDS.BackEnd.Manager.Geolocation"
RUN dotnet build "./RDS.BackEnd.Manager.Geolocation.csproj" -c Release -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
RUN dotnet publish "./RDS.BackEnd.Manager.Geolocation.csproj" -c Release -o /app/publish /p:UseAppHost=false



# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final


ARG GIT_COMMIT
ARG BUILD_TIME
ARG VERSION   

ENV GIT_COMMIT=$GIT_COMMIT
ENV BUILD_TIME=$BUILD_TIME
ENV VERSION=$VERSION 


WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RDS.BackEnd.Manager.Geolocation.dll"]
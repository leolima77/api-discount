FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

WORKDIR /app
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
WORKDIR /src
COPY . ./
RUN ls -la ./

RUN dotnet restore --arch $TARGETARCH
COPY . .
RUN ls -la 

WORKDIR "/src/."
RUN ls -la src/

RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/build --arch $TARGETARCH
 
FROM --platform=$BUILDPLATFORM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish --arch $TARGETARCH
 
FROM base AS final
WORKDIR /app

RUN apt-get update && apt-get -y upgrade
RUN apt-get --yes install curl
RUN apt-get --yes install inetutils-traceroute

ARG CACHEBUST=$(date +%s)

COPY --from=publish /app/publish .

RUN useradd --uid $(shuf -i 2000-65000 -n 1) app-discount
USER app-discount

ENTRYPOINT ["dotnet", "ApiDiscount.dll"]
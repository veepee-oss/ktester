FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /app
COPY KafkaTester.csproj .
RUN dotnet restore

FROM restore AS build
COPY . /app
RUN dotnet build --no-restore --configuration Release

FROM build AS publish
RUN dotnet publish --no-restore --no-build --configuration Release --output artifacts

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/artifacts /app
EXPOSE 5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT dotnet KafkaTester.dll
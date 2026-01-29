# BUILD
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DataWeb/DataWeb.csproj DataWeb/
RUN dotnet restore DataWeb/DataWeb.csproj

COPY . .
WORKDIR /src/DataWeb

RUN dotnet publish -c Release -o /app/publish

# RUNTIME
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5267

ENV ASPNETCORE_URLS=http://+:5267
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "DataWeb.dll"]

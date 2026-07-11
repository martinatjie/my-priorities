FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY PrioritizationApp.Core/PrioritizationApp.Core.csproj PrioritizationApp.Core/
COPY PrioritizationApp.Web/PrioritizationApp.Web.csproj PrioritizationApp.Web/
RUN dotnet restore PrioritizationApp.Web/PrioritizationApp.Web.csproj

COPY . ./
RUN dotnet publish PrioritizationApp.Web/PrioritizationApp.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "PrioritizationApp.Web.dll"]

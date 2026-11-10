FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/RagWorkspace.Api/RagWorkspace.Api.csproj", "src/RagWorkspace.Api/"]
RUN dotnet restore "src/RagWorkspace.Api/RagWorkspace.Api.csproj"
COPY . .
RUN dotnet publish "src/RagWorkspace.Api/RagWorkspace.Api.csproj" -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "RagWorkspace.Api.dll"]

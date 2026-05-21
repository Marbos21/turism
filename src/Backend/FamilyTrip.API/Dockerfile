FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Backend/FamilyTrip.API/FamilyTrip.API.csproj", "src/Backend/FamilyTrip.API/"]
COPY ["src/Backend/FamilyTrip.Application/FamilyTrip.Application.csproj", "src/Backend/FamilyTrip.Application/"]
COPY ["src/Backend/FamilyTrip.Infrastructure/FamilyTrip.Infrastructure.csproj", "src/Backend/FamilyTrip.Infrastructure/"]
COPY ["src/Backend/FamilyTrip.Domain/FamilyTrip.Domain.csproj", "src/Backend/FamilyTrip.Domain/"]

RUN dotnet restore "src/Backend/FamilyTrip.API/FamilyTrip.API.csproj"

COPY . .
WORKDIR "/src/src/Backend/FamilyTrip.API"
RUN dotnet publish "FamilyTrip.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FamilyTrip.API.dll"]

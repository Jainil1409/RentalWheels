FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["vehicle management system mvc.csproj", "./"]
RUN dotnet restore "vehicle management system mvc.csproj"

# Copy everything and publish. Set a safe AssemblyName (no spaces).
COPY . .
RUN dotnet publish "vehicle management system mvc.csproj" -c Release -o /app/publish -p:AssemblyName=vehicle_management_system_mvc

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "vehicle_management_system_mvc.dll"]

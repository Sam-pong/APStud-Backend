# Build stage - .NET 9 SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# Runtime stage - .NET 9 ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
# Allow environment variables from Render to flow into the container
ENV JWT_SECRET=${JWT_SECRET}
ENV SUPABASE_URL=${SUPABASE_URL}
ENV SUPABASE_SERVICE_KEY=${SUPABASE_SERVICE_KEY}


COPY --from=build /app .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "APStud.Backend.dll"]

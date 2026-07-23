FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["ItransitionCourseProject/ItransitionCourseProject.csproj", "ItransitionCourseProject/"]
RUN dotnet restore "ItransitionCourseProject/ItransitionCourseProject.csproj"

COPY . .
WORKDIR /src/ItransitionCourseProject
RUN dotnet publish "ItransitionCourseProject.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["sh", "-c", "dotnet ItransitionCourseProject.dll --urls http://0.0.0.0:${PORT:-10000}"]

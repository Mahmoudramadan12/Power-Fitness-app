# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first so this layer is cached until a project file changes.
COPY GymManagement.sln ./
COPY GymManagementDAL/GymManagementDAL.csproj GymManagementDAL/
COPY GymManagementBLL/GymManagementBLL.csproj GymManagementBLL/
COPY GymManagementPL/GymManagementPL.csproj GymManagementPL/
RUN dotnet restore GymManagementPL/GymManagementPL.csproj

COPY GymManagementDAL/ GymManagementDAL/
COPY GymManagementBLL/ GymManagementBLL/
COPY GymManagementPL/ GymManagementPL/
RUN dotnet publish GymManagementPL/GymManagementPL.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app .

# Uploaded photos and data-protection keys must outlive the container: mount volumes here.
RUN mkdir -p /app/MembersPictures /keys && chown -R $APP_UID /app/MembersPictures /keys
VOLUME ["/app/MembersPictures", "/keys"]

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DataProtection__KeysPath=/keys
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "GymManagementPL.dll"]

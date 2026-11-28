#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /src

ARG nuget_url
ARG nuget_user=user

# The feed URL is not secret and stable, so this layer stays cached.
RUN dotnet nuget add source "$nuget_url" -n internalfeed

COPY . .

# One restore + publish step. The feed token comes in as a build secret (never stored in a layer,
# never part of the cache key) and the NuGet package cache persists between builds, so a code
# change re-downloads nothing. A build with unchanged sources is a full cache hit.
RUN --mount=type=secret,id=nuget_password \
    --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    NuGetPackageSourceCredentials_internalfeed="Username=${nuget_user};Password=$(cat /run/secrets/nuget_password)" \
    dotnet publish "Utilities.Billing.Api/Utilities.Billing.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

# Build info. With imageLayerCache the pipeline re-stamps these on the finished image (buildah
# config), because buildah 1.33 reuses a cached `ENV x=$arg` step even when the arg changed.
ARG app_version
ARG git_commit
ARG build_number

ENV app_version=$app_version
ENV git_commit=$git_commit
ENV build_number=$build_number

ENTRYPOINT ["dotnet", "Utilities.Billing.Api.dll"]

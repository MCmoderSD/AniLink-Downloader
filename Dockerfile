# ---- Stage 1: Frontend ----
FROM node:26-alpine AS frontend
WORKDIR /app

COPY frontend/package.json frontend/package-lock.json ./
RUN --mount=type=cache,target=/root/.npm \
    npm ci --no-audit --no-fund

COPY frontend/ ./
RUN npm run build

# ---- Stage 2: Backend ----
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS backend
WORKDIR /src

COPY backend/AniLink.Api/AniLink.Api.csproj ./
RUN dotnet restore AniLink.Api.csproj

COPY backend/AniLink.Api/ ./
RUN dotnet publish AniLink.Api.csproj -c Release -o /app/publish --no-restore

# ---- Stage 3: Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN apk add --no-cache 7zip \
    && 7zz i | grep -q Rar5

RUN addgroup -S anilink && adduser -S anilink -G anilink \
    && mkdir /downloads && chown anilink:anilink /downloads

COPY --from=backend --chown=anilink:anilink /app/publish .
COPY --from=frontend --chown=anilink:anilink /app/dist/AniLink/browser ./wwwroot

ENV Downloads__Root=/downloads
ENV Archive__SevenZipPath=7zz

ENV HOME=/tmp

USER anilink

EXPOSE 8080
VOLUME /downloads

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget -q -O /dev/null http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "AniLink.Api.dll"]
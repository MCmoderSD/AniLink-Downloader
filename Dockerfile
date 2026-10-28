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

# ---- Stage 3: 7-Zip ----
# Alpine's own "7zip" package is built without RAR support, so the official static build is used instead.
# 7zzs is the statically linked binary (7zz needs glibc and does not run on Alpine/musl).
FROM alpine:3 AS sevenzip
ARG TARGETARCH
# Releases are published on GitHub (ip7z/7zip); 7-zip.org/a/ stopped having them with 26.05. 26.04 fixed CVE-2026-58052,
# and this program unpacks archives from the internet, so the version is kept current.
ARG SEVENZIP_VERSION=2605
ARG SEVENZIP_SHA256_AMD64=22cf82fcc6eb7474f1d5bb4f02c0f8ebd573a718089d6833dc30f8350a3b232e
ARG SEVENZIP_SHA256_ARM64=0b71f6cfc34669b3bf69f2347efd3429f77020665003a13b9cdc53b784270421

RUN apk add --no-cache curl xz \
    && case "$TARGETARCH" in \
         amd64) arch=x64;   sha="$SEVENZIP_SHA256_AMD64" ;; \
         arm64) arch=arm64; sha="$SEVENZIP_SHA256_ARM64" ;; \
         *) echo "Unsupported architecture: $TARGETARCH" >&2; exit 1 ;; \
       esac \
    && curl -fsSL --retry 3 -o /tmp/7z.tar.xz "https://github.com/ip7z/7zip/releases/download/${SEVENZIP_VERSION%??}.${SEVENZIP_VERSION#??}/7z${SEVENZIP_VERSION}-linux-${arch}.tar.xz" \
    && echo "${sha}  /tmp/7z.tar.xz" | sha256sum -c - \
    && mkdir /tmp/7z \
    && tar -xJf /tmp/7z.tar.xz -C /tmp/7z 7zzs \
    && install -m 0755 /tmp/7z/7zzs /usr/local/bin/7zz \
    && 7zz i | grep -q Rar5

# ---- Stage 4: Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

COPY --from=sevenzip /usr/local/bin/7zz /usr/local/bin/7zz

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
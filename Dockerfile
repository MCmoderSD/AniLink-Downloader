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
# Alpine's own "7zip" package is built without RAR support, so the official static build is used instead (7zzs: the
# statically linked binary; the regular 7zz needs glibc and does not run on Alpine/musl).
#
# It is always the newest release, which keeps the fixes of a program that unpacks archives from the internet. The tag
# comes from the redirect of /releases/latest, the file name from the tag (26.05 -> 7z2605). The stage cannot succeed
# without the Rar5 format, and the file is unpacked by xz instead of `tar -J`, because BusyBox's decoder cannot read
# 7-Zip's archive. SEVENZIP_REFRESH only changes the cache key (the echo is what uses it, BuildKit ignores an argument
# the command does not mention): CI passes the run id, so every pipeline run looks for a newer release again, while a
# local build reuses the layer (use --no-cache to look again).
FROM alpine:3 AS sevenzip
ARG TARGETARCH
ARG SEVENZIP_REFRESH=0

RUN apk add --no-cache curl xz \
    && arch="$([ "$TARGETARCH" = arm64 ] && echo arm64 || echo x64)" \
    && tag="$(curl -fsSLI -o /dev/null -w '%{url_effective}' https://github.com/ip7z/7zip/releases/latest | sed 's|.*/||')" \
    && echo "7-Zip $tag for $arch (refresh $SEVENZIP_REFRESH)" \
    && curl -fsSL --retry 3 "https://github.com/ip7z/7zip/releases/download/$tag/7z$(echo "$tag" | tr -d .)-linux-$arch.tar.xz" \
        | xz -dc | tar -x 7zzs \
    && install -m 0755 7zzs /usr/local/bin/7zz \
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
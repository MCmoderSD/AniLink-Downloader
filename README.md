# [AniLink-Downloader](https://www.GitHub.com/MCmoderSD/AniLink-Downloader/)

[Description](#description) · [Disclaimer](#disclaimer) · [Requirements](#requirements) · [Setup](#setup) ·
[Configuration](#configuration) · [Usage](#usage) · [Running the container](#running-the-container) ·
[Troubleshooting](#troubleshooting) · [HTTP API](#http-api-and-health-checks) · [Development](#development)

## Description
AniLink-Downloader downloads and unpacks media from single or multipart archives (mostly `.rar`, which may be password
protected), and runs as one Docker container with a web page. Paste the hoster links of a season, an episode or a
film, and it

- creates the direct download links with [Debrid-Link](https://debrid-link.com/premium),
- downloads the archive parts (several at a time, with live progress),
- unpacks them with 7-Zip, trying your default passwords before it asks you,
- renames the video (and its `.nfo`) after the episode or the film and puts both into a folder named after the show.

Everything stays local: there is no account, no login and no database. 7-Zip is part of the image, so Docker is the
only thing you have to install.

### How it works
Every episode (a film counts as one) goes through the same stages, and the web page shows the current one as a chip:

| Shown as        | What happens                                                                                    |
| --------------- | ----------------------------------------------------------------------------------------------- |
| Creating links  | Each hoster link goes to Debrid-Link, which answers with a direct download link.                |
| Downloading     | The archive parts are fetched into `archives/`. An interrupted part continues where it stopped. |
| Unpacking       | 7-Zip looks at the archive, finds the password and unpacks it into `output/`.                   |
| Password needed | None of the known passwords opens the archive. The episode waits until you enter one.           |
| Sorting files   | The video and its `.nfo` are moved out of `output/` and renamed.                                |
| Done            | The files are in place.                                                                         |
| Failed          | Something went wrong, and the episode shows what. **Retry** starts it again.                    |
| Canceled        | The episode was canceled. **Retry** starts it again.                                            |
| Paused          | The download was paused. **Resume** carries on.                                                 |

A few rules follow from that:
- Links are created one after the other, with the pause `Debrid_Request_Delay` between two requests. An episode starts
  downloading as soon as all of *its* links exist, while the other episodes keep getting theirs.
- At most `Concurrent_Downloads` archive parts are downloaded at the same time, counted over all downloads together.
- As many archives are unpacked at the same time as the server has CPU threads, because 7-Zip unpacks one archive on one
  thread. An episode that finds every place taken waits for one. This is not a setting, and a Docker CPU limit lowers
  it.
- When every episode is done, `archives/` and `output/` are removed (see [What ends up on disk](#what-ends-up-on-disk)).

<br>

## Disclaimer
This project **does not promote or endorse piracy**.
Please only use AniLink-Downloader with **legally obtained, licensed, or copyright-free content**.
The examples in this README are purely illustrative.

For your own protection, always use a **VPN**, since not all websites or hosters are safe and trustworthy.
Only download and process content that is **public domain, self-created, or explicitly free of copyright restrictions**.

The web page has no authentication, because it is meant for your own machine or network. **Do not expose port 8080 to
the internet**; put it behind a VPN or a reverse proxy with authentication if it has to be reachable from outside.

<br>

## Requirements
- [Docker](https://docs.docker.com/get-docker/) with Compose v2 (the `docker compose` command): Docker Desktop on
  Windows and macOS, or Docker Engine on Linux. The image exists for `linux/amd64`, `linux/arm64` and `linux/arm/v7`,
  so it also runs on a Raspberry Pi 2 or newer, with a 32-bit or a 64-bit system.
- [Debrid-Link Premium](https://debrid-link.com/premium) and a private API key from
  [debrid-link.com/webapp/apikey](https://debrid-link.com/webapp/apikey).
- Free disk space of about twice the size of what you download: the archives stay until everything is unpacked and
  sorted.

<br>

## Setup
There are two ways to get the image: run the one that is published (A), or build it from the source code yourself (B).
Configuration and usage are the same for both.

### A. Run the published image
GitHub Actions builds the image for `amd64`, `arm64` and `arm/v7` on every push to the `master` branch and
publishes it as `mcmodersd/anilink-downloader:latest` on Docker Hub and as
`ghcr.io/mcmodersd/anilink-downloader:latest` on GitHub.

#### 1. Get the compose file and create the downloads folder
```bash
mkdir anilink && cd anilink
curl -O https://raw.githubusercontent.com/MCmoderSD/AniLink-Downloader/master/docker-compose.yaml
curl -o .env https://raw.githubusercontent.com/MCmoderSD/AniLink-Downloader/master/.env.example
mkdir downloads
```
In Windows PowerShell write `curl.exe` instead of `curl` (there, `curl` is an alias of another command), or download
the two files by hand.

The compose file also contains a `build: .` line. It is only used when you build the image yourself (B) and can stay
as it is.

Create the `downloads` folder yourself, before the first start. On Linux, Docker would otherwise create it for
`root`, and the container, which runs as user `1000`, could not write into it.

#### 2. Fill in `.env`
```dotenv
DEBRID_API_KEY=your-api-key
ARCHIVE_PASSWORD_0=www.example.org
```

#### 3. Start it
```bash
docker compose up -d
```
Open [http://localhost:8080](http://localhost:8080). Within a minute `docker compose ps` shows the container as
`healthy`. If it does not come up, `docker compose logs` tells why (see [Troubleshooting](#troubleshooting)).

### B. Build the image yourself
Do this to run a version that is not published yet, to try your own changes, or to avoid pulling a ready-made image.
The repository contains the `Dockerfile`, and the compose file points at it with `build: .`.

```bash
git clone https://github.com/MCmoderSD/AniLink-Downloader.git
cd AniLink-Downloader
cp .env.example .env          # on Windows: copy .env.example .env
mkdir downloads
# now fill in .env as described in step 2 above
docker compose up -d --build
```
`--build` builds the `Dockerfile`, tags the result as `mcmodersd/anilink-downloader:latest` and starts exactly that
image. Nothing is pulled. The build runs four stages, and only the last one ends up in the image, which takes about
120 MB on disk and about 55 MB to download (Docker Desktop adds the two up and lists about 190 MB):

| Stage      | Base image                  | What it does                                                                                   |
| ---------- | --------------------------- | ---------------------------------------------------------------------------------------------- |
| `frontend` | `node:26-alpine`            | Installs the npm packages and builds the web page. Runs on the CPU of the machine that builds. |
| `backend`  | `dotnet/sdk:10.0-alpine`    | Restores the NuGet packages and publishes the API. Runs on the CPU of the machine that builds. |
| `sevenzip` | `alpine:3`                  | Downloads the newest official 7-Zip for the CPU and checks that it reads RAR5.                 |
| `runtime`  | `dotnet/aspnet:10.0-alpine` | Takes the results of the three, runs as user `1000:1000` and starts the API.                   |

The build needs internet access (Docker Hub for the base images, the npm and NuGet registries, and GitHub for 7-Zip).
The first build takes a few minutes; later ones reuse every layer that did not change, so editing the backend does not
install the npm packages again. Your `.env`, the `downloads` folder and `appsettings.Local.json` are excluded by
`.dockerignore`, so no secret can end up in an image layer.

Commands you will need to build and update:

| Command                             | What it does                                                         |
| ----------------------------------- | -------------------------------------------------------------------- |
| `docker compose build`              | Builds the image and does not start anything.                        |
| `docker compose up -d --build`      | Builds, then (re)creates and starts the container.                   |
| `docker compose up -d --pull never` | Starts the image already on this machine, without asking a registry. |
| `docker compose build --pull`       | Builds, and fetches newer base images first.                         |
| `docker compose build --no-cache`   | Builds from scratch, including the newest 7-Zip.                     |

The 7-Zip version is looked up while the image is built, and Docker reuses a cached stage, so a cached build keeps the
7-Zip it had. To fetch the newest one and keep everything else cached, pass any number that changed since the last
build:
```bash
docker compose build --build-arg SEVENZIP_REFRESH=$(date +%s)
```
In PowerShell, use `$(Get-Date -UFormat %s)` for `$(date +%s)`.

To update your own build to a newer version of the code:
```bash
git pull
docker compose up -d --build
```

> [!WARNING]
> After building, start the container with `docker compose up -d --build` or `docker compose up -d --pull never`,
> not with a plain `docker compose up -d`. The compose file says `pull_policy: always`, so a plain `up` pulls the
> published image first, and once that image exists, it replaces the one you built.

To make a plain `docker compose up -d` use only your own build, put a file named `docker-compose.override.yaml` next to
`docker-compose.yaml`. Compose reads it automatically and it is yours, so do not commit it:
```yaml
services:
  anilink:
    image: anilink-downloader:local
    pull_policy: never
```
Now `docker compose up -d` builds the image `anilink-downloader:local` if it does not exist yet, and never pulls.
After `git pull`, rebuild it with `docker compose build`.

To build for another CPU, for example for a Raspberry Pi on your PC, use Buildx and name the CPU: `linux/arm64` for a
64-bit system, `linux/arm/v7` for a 32-bit one. The web page and the API are built on your own CPU, and only a few small
steps run in emulation:
```bash
docker buildx build --platform linux/arm64 -t anilink-downloader:arm64 --load .
docker buildx build --platform linux/arm/v7 -t anilink-downloader:armv7 --load .
```
Docker Desktop includes the emulation. On Linux, install it once with
`docker run --privileged --rm tonistiigi/binfmt --install all`.

<br>

## Configuration
Two places hold the settings: `.env` for what is secret, and `docker-compose.yaml` for the rest.

### `.env`
| Variable             | Required | Meaning                                                                      |
| -------------------- | -------- | ---------------------------------------------------------------------------- |
| `DEBRID_API_KEY`     | yes      | Your private Debrid-Link API key. The container stops right away without it. |
| `ARCHIVE_PASSWORD_0` | no       | First default password.                                                      |
| `ARCHIVE_PASSWORD_1` | no       | Second default password.                                                     |

The default passwords are tried in the order of their numbers, and empty ones are ignored. For more than two, add a
line `Archive_Password_2: ${ARCHIVE_PASSWORD_2:-}` (then `_3`, and so on) to `environment:` in the compose file and the
matching `ARCHIVE_PASSWORD_2=` to `.env`. Put a value with spaces, `#` or `$` in single quotes:
`ARCHIVE_PASSWORD_0='my pass#word'`.

### `docker-compose.yaml`
The rest is written into the compose file itself. Change it there if you need to:

| Setting                | Default     | Allowed               | Meaning                                                        |
| ---------------------- | ----------- | --------------------- | -------------------------------------------------------------- |
| `Debrid_Request_Delay` | `500`       | 0 to 60000            | Pause in milliseconds between two Debrid-Link requests.        |
| `Concurrent_Downloads` | `3`         | 1 to the thread count | How many archive parts are downloaded at the same time.        |
| `user`                 | `1000:1000` | `uid:gid`             | The user the container runs as, and so the owner of the files. |

- **`Debrid_Request_Delay`**: raise it if Debrid-Link flags the account for flooding.
- **`Concurrent_Downloads`**: counts archive parts, not episodes. The upper limit is the number of threads the server
  has, which is the number of CPUs available to the container and follows a Docker CPU limit. A higher value is lowered
  to that number, and the log says so.
- **`user`**: on Linux, use your own ids from `id -u` and `id -g`. On Docker Desktop (Windows and macOS) the default
  normally works as it is.

You can change more in the compose file:
- **The port**: `8080:8080` is `host:container`. For port 9090 on the host, write `9090:8080`.
- **The downloads folder**: `./downloads:/downloads` is `host folder:container folder`. For example,
  `/mnt/media/anime:/downloads` puts everything on another disk. Leave the part after the colon as it is.
- **The log**: the level is `Information`. Add `Logging__LogLevel__Default: Debug` to `environment:` for more.

`Downloads__Root` (`/downloads`) and `Archive__SevenZipPath` (`7zz`) are set by the image and are not meant to be
changed. The settings of ASP.NET also work under their long names, `DebridLink__ApiKey`, `DebridLink__RequestDelayMs`,
`Downloads__MaxConcurrency` and `Archive__Passwords__0`; if both are set, the short name wins.

<br>

## Usage

### The Downloads page
Every download is a card with its name, the number of episodes, a status (*Running*, *Paused*, *Password needed*,
*Failed*, *Canceled* or *Done*), a bar for the Debrid-Link creation and a bar for the download (size, speed and time
left). Below them are the folder it is saved to and the buttons:

| Button         | Shown when                         | What it does                                                           |
| -------------- | ---------------------------------- | ---------------------------------------------------------------------- |
| Enter password | an episode waits for a password    | Asks for it and tries it on every waiting episode.                     |
| Retry          | an episode failed or was canceled  | Starts those episodes again. Parts that arrived are kept.              |
| Pause          | the download is running            | Stops requests, downloads and unpacking. What arrived stays on disk.   |
| Resume         | the download is paused             | Carries on from there. An unpacking starts over.                       |
| Cancel         | running, paused or password needed | Ends the download for good, after asking. Files on disk stay.          |
| Remove         | always                             | Drops it from the list after asking (stops it if it runs). Files stay. |
| Show details   | always                             | Opens a row per episode, its parts and the activity log.               |

**Show details** lists every episode with its own bar and stage. A click on *N parts* shows the files of that episode
with their state (*Waiting for link*, *Creating link*, *Link ready*, *Downloading*, *Downloaded* or *Failed*) and how
much has arrived. Below the episodes, the activity log keeps the latest messages. Finished files and problems also
show up as notes in the corner, and *Live* at the top right says that the page is connected to the server.

With more than one episode, every row also has its own buttons: **Retry**, **Pause** or **Resume**, **Cancel** and
**Remove**, as far as they apply to its state. They act on that one episode, and the rest of the download carries on.
**Cancel** and **Remove** ask first. **Resume** on the card also carries on an episode that was paused on its own.
Removing the last episode removes the whole download. Finished files stay on disk.

### Starting a download
**New download** takes:

| Field         | Applies to | What to enter                                                                            |
| ------------- | ---------- | ---------------------------------------------------------------------------------------- |
| Type          | both       | *Season* for several episodes, *Movie* for one release (a single episode counts as one). |
| Name          | both       | Becomes the folder, and the file name of a film.                                         |
| Season format | Season     | The text in front of the episode number, e.g. `Frieren.S01E`. Found automatically.       |
| Season        | Season     | The number of the folder (`01`). Read from the format, editable.                         |
| Links         | both       | One per line, in any order.                                                              |
| Password      | both       | Optional. Tried before the default passwords.                                            |

While you type, the page checks the links and shows which episodes and parts it found before you start. Only `http`
and `https` links count. Duplicates and empty lines are dropped, and `.html` at the end of a link is not part of the
file name. The part number comes from `part1`, `part01` or an old-style ending such as `.r00`. For a *Season* with a
format, the number after it decides which episode a link belongs to. A *Movie* puts all links into one release.

A download only starts when every link can be assigned. If one cannot, the page says which. It also refuses a second
download into a folder that is still being filled.

Limits that keep names inside what file systems allow:

| Field         | Limit                | Why                                                                                    |
| ------------- | -------------------- | -------------------------------------------------------------------------------------- |
| Name          | 251 bytes in UTF-8   | A file name may have 255 bytes, and `.mkv` or `.nfo` needs 4 of them.                  |
| Season format | 255 characters       | The length of a file name.                                                             |
| Links         | 1,000,000 characters | About 7,700 links of the usual length (960 episodes with 8 parts). Split larger lists. |
| Password      | 127 characters       | The longest password RAR allows.                                                       |

Plain letters are one byte each, accented ones two, and most Japanese characters three. The page counts for you and
warns before you start.

### What ends up on disk
A season goes into `Name/Season`, with the season as two digits:
```
downloads/
└── Frieren/
    └── 01/
        ├── 1.mkv             ← episode 1
        ├── 1.nfo
        ├── 2.mkv
        ├── Downloads.txt     ← all links, in order
        ├── .anilink.json     ← the state, until everything is done
        ├── archives/         ← removed again when everything succeeded
        └── output/           ← removed again when it is empty
```
A film goes directly into a folder of its own and is named after the film:
```
downloads/
└── Some Film/
    ├── Some Film.mkv
    ├── Some Film.nfo
    └── Downloads.txt
```
Inside `archives/` and `output/`, a season has a folder per episode (`1`, `2`, …), a film has its files right there.
Characters that file systems do not allow (`\ / : * ? " < > |`) become `-` in the folder name. Each archive must hold
exactly one `.mkv` or `.mp4`; its `.nfo` is taken along when there is one, and files ending in `.md5`, `.download`,
`.url` or `.uri` are deleted without a notice. If anything else is left in `output/`, or an episode failed, `archives/`
and `output/` are kept, so nothing is lost with them.

### Passwords
For every archive, the passwords are tried in this order: the one that already worked for the download, the ones you
typed (the latest first), then the defaults from `.env`. Only if none of them opens the archive, the download shows
**Enter password**, and the unpacking is tried again with what you enter. A password that worked is remembered for the
rest of that download.

### Restarts
The state of every unfinished download is kept in a `.anilink.json` next to its files, including the links Debrid-Link
created. After a restart, the container carries on where it was: finished parts are not fetched again, half
downloaded ones are continued, and no link is asked for twice. Progress is written to that file every two seconds and
when the container stops, so after a crash at most the last two seconds are done again.
A paused download stays paused until you resume it.

<br>

## Running the container
| Task                                                      | Command                                            |
| --------------------------------------------------------- | -------------------------------------------------- |
| Start, or update to the newest published image            | `docker compose up -d`                             |
| Follow the log                                            | `docker compose logs -f`                           |
| See whether it is healthy                                 | `docker compose ps`                                |
| Restart                                                   | `docker compose restart`                           |
| Stop and start again later                                | `docker compose stop`, then `docker compose start` |
| Remove the container and the network (the downloads stay) | `docker compose down`                              |

Unfinished downloads carry on after every one of these. The files in `./downloads` are never removed by Docker
Compose; to get rid of a download, delete its folder.

<br>

## Troubleshooting
`docker compose logs` is the first place to look. The most common problems:

| What you see                                     | Likely cause and fix                                                         |
| ------------------------------------------------ | ---------------------------------------------------------------------------- |
| Log: `Missing configuration 'DebridLink:ApiKey'` | `DEBRID_API_KEY` is missing or empty in `.env`.                              |
| `unhealthy`, or a folder "cannot be prepared"    | `./downloads` belongs to another user. Fix its owner or the `user:` setting. |
| The page does not open                           | Port 8080 is taken. Use `9090:8080` in the compose file, open port 9090.     |
| *Reconnecting* at the top right                  | The server is unreachable or restarting. It reconnects by itself.            |
| *Password needed* on an episode                  | No default password fits. **Enter password**, or add it to `.env`.           |
| *N of M links could not be assigned*             | The season format is not in every file name. Type it, or split the list.     |
| Unpacking stays at 0%                            | The log shows every 20 s how much 7-Zip has written and what it printed.     |
| *The archive contains no .mkv or .mp4 file*      | The release holds something else, or several videos. Unpack it by hand.      |
| *… already exists in …* when sorting             | That episode's file is in the folder already. Move it away, then **Retry**.  |
| *… is already being downloaded into …*           | The same name and season is still active. Finish, cancel or remove it first. |
| Errors or limits from Debrid-Link                | Raise `Debrid_Request_Delay`, e.g. to `1000`.                                |

<br>

## HTTP API and health checks
The web page talks to a JSON API that you can use for scripts as well. It has no authentication either, so the warning
in the [Disclaimer](#disclaimer) applies.

| Request                                    | Purpose                                                           |
| ------------------------------------------ | ----------------------------------------------------------------- |
| `GET /api/config`                          | Limits, the number of default passwords and the downloads folder. |
| `POST /api/downloads/analyze`              | Checks a list of links, as the form does while you type.          |
| `POST /api/downloads`                      | Starts a download.                                                |
| `GET /api/downloads`                       | Lists all downloads.                                              |
| `GET /api/downloads/{id}`                  | One download with all its episodes and parts.                     |
| `POST /api/downloads/{id}/pause`           | Pauses it (`/resume` resumes it).                                 |
| `POST /api/downloads/{id}/cancel`          | Cancels it (`/retry` starts failed or canceled episodes again).   |
| `POST /api/downloads/{id}/password`        | Sends a password for the waiting episodes.                        |
| `POST /api/downloads/{id}/items/{n}/pause` | Pauses episode `n` (`/resume`, `/cancel` and `/retry` too).       |
| `DELETE /api/downloads/{id}/items/{n}`     | Removes episode `n` from the download. Files on disk stay.        |
| `DELETE /api/downloads/{id}`               | Removes it from the list. Files on disk stay.                     |
| `GET /api/downloads/stream`                | Server-Sent Events: `job`, `notice` and `removed` messages.       |
| `GET /health/live`                         | The process answers.                                              |
| `GET /health/ready`                        | The downloads folder is writable and 7-Zip works.                 |
| `GET /health`                              | All checks.                                                       |

A download is started like this:
```bash
curl -X POST http://localhost:8080/api/downloads -H "Content-Type: application/json" \
  -d '{"name": "Frieren", "kind": "Season", "season": 1, "format": null, "password": null, "links": "https://…\nhttps://…"}'
```
The container calls `/health/live` itself every 30 seconds, and that is what `healthy` in `docker compose ps` means.

<br>

## Development
| Part     | Address               | Needs                                                             |
| -------- | --------------------- | ----------------------------------------------------------------- |
| Backend  | http://localhost:5088 | The .NET 10 SDK and an installed [7-Zip](https://www.7-zip.org/). |
| Frontend | http://localhost:4200 | Node 26. Forwards `/api` and `/health` to the backend.            |

```bash
# Backend, on http://localhost:5088
cd backend/AniLink.Api
dotnet run
```
Put the API key into `backend/AniLink.Api/appsettings.Local.json` (git-ignored; user secrets and environment variables
such as `Debrid_ApiKey` work too):
```json
{
  "DebridLink": { "ApiKey": "your-api-key" },
  "Archive": { "Passwords": [ "www.example.org" ] }
}
```
In development, downloads go to `downloads/` in the repository, and 7-Zip is expected at
`C:\Program Files\7-Zip\7z.exe`. On Linux and macOS, set `Archive:SevenZipPath` to `7zz` or `7z`.

```bash
# Frontend, on http://localhost:4200 (forwards /api to the backend)
cd frontend
npm ci
npm start
```
`npm run build` is the check that the frontend compiles, `dotnet build backend/AniLink.slnx` the one for the backend,
and `docker compose build` the one for the image.

What is where:

| Folder or file         | Contents                                                             |
| ---------------------- | -------------------------------------------------------------------- |
| `backend/AniLink.Api/` | The .NET API: controllers call services, services call repositories. |
| `frontend/`            | The Angular web page (Angular Material, signals).                    |
| `Dockerfile`           | Builds both and bundles 7-Zip into one image.                        |
| `docker-compose.yaml`  | How the container is started.                                        |
| `.env.example`         | The template for `.env`.                                             |

<br>

## Additional Tools
Some websites use URL shorteners or redirection services, which makes it hard to get the direct hoster links.
- [AniLink-Skipper](https://www.GitHub.com/MCmoderSD/AniLink-Skipper) skips those redirects
  ([Chrome Web Store](https://chromewebstore.google.com/detail/anilink-skipper/cohhlekeocnodlohgbfhkmhpjjpccpkb)).
- [TabScraper](https://www.GitHub.com/MCmoderSD/TabScraper) collects the links of all open tabs, filtered by a regex
  ([Chrome Web Store](https://chromewebstore.google.com/detail/tab-scraper/ahdhhonppgdiglmppkcjckijelfdalho)).
  For example, to keep only RapidGator archive links: `^https://rapidgator.net/file/.+.(?:part\d+.)?rar(?:.html)?$`

<br>

## License
BSD 3-Clause, see [LICENSE](LICENSE).
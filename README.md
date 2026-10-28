# [AniLink-Downloader](https://www.GitHub.com/MCmoderSD/AniLink-Downloader/)

## Description
AniLink-Downloader downloads and unpacks media from single or multipart `.rar` archives (which may be password
protected), and runs as one Docker container with a web page. Paste the hoster links of a season, an episode or a
film, and it

- creates the direct download links with [Debrid-Link](https://debrid-link.com/premium),
- downloads the archive parts (several at a time, with live progress),
- unpacks them with 7-Zip, trying your default passwords before it asks you,
- renames the video (and its `.nfo`) after the episode or the film and puts it where you want it.

Everything stays local: there is no account, no login and no database.

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
- [Docker](https://docs.docker.com/get-docker/) with Compose
- [Debrid-Link Premium](https://debrid-link.com/premium) and a private API key from
  [debrid-link.com/webapp/apikey](https://debrid-link.com/webapp/apikey)

<br>

## Setup

#### 1. Get the compose file and create the downloads folder
```bash
mkdir anilink && cd anilink
curl -O https://raw.githubusercontent.com/MCmoderSD/AniLink-Downloader/master/docker-compose.yaml
curl -o .env https://raw.githubusercontent.com/MCmoderSD/AniLink-Downloader/master/.env.example
mkdir downloads
```

#### 2. Fill in `.env`
```dotenv
DEBRID_API_KEY=your-api-key
ARCHIVE_PASSWORD_0=www.example.org
```

#### 3. Start it
```bash
docker compose up -d
```
Open [http://localhost:8080](http://localhost:8080).

<br>

## Configuration
`.env` only holds what is secret:

| Variable | Meaning |
|---|---|
| `DEBRID_API_KEY` | Private Debrid-Link API key (required). The container refuses to start without it. |
| `ARCHIVE_PASSWORD_0`, `_1` | Default passwords, tried in this order. Add more as `Archive_Password_2`, `Archive_Password_3`, … in the compose file. |

The rest is written into `docker-compose.yaml` itself; change it there if you need to:

| Setting | Value | Meaning |
|---|---|---|
| `Debrid_Request_Delay` | `500` | Minimum pause between two Debrid-Link requests. Raise it if the account is flagged for flooding. |
| `Concurrent_Downloads` | `3` | How many archive **parts** are downloaded at the same time (1–16). |
| `user` | `1000:1000` | The user the container runs as, and so the owner of the files in `./downloads`. On Linux, use your own `id -u` and `id -g`, and create the folder before the first start. |

<br>

## Usage

**Downloads** lists every download with a bar for the Debrid-Link creation, a bar for the download (size, speed,
time left) and one row per episode. Finished files and problems also show up as messages in the corner.

**Pause** stops the Debrid-Link requests, the downloads and an unpacking in progress; what has arrived stays on
disk, and **Resume** carries on from there (an unpacking starts over). **Cancel** ends the download for good, **Retry**
picks up what failed or was canceled.

**New download** takes:

| Field | |
|---|---|
| Name | Becomes the folder (and the file name of a film). |
| Type | *Season* for several episodes, *Movie* for a single release (an episode of its own counts as one). |
| Season format | The text in front of the episode number, e.g. `Frieren.S01E`. Found automatically; type it only if the page says it could not. |
| Season | The folder number (`01`). Read from the format, editable. |
| Links | One per line, in any order. The page shows which episodes and parts it found before you start. |
| Password | Optional. Tried before the defaults. |

### What ends up on disk
```
downloads/
└── Frieren/
    └── 01/                  ← a film goes directly into its own folder
        ├── 1.mkv
        ├── 1.nfo
        ├── 2.mkv
        ├── Downloads.txt    ← all links, in order
        ├── archives/        ← removed again when everything succeeded
        └── output/          ← removed again when it is empty
```
The video of a season is named after its episode number, a film after its name. If anything is left in `output/`
(or an episode failed), `archives/` and `output/` are kept, so nothing is lost with them.

### Passwords
For every archive, the passwords are tried in this order: the one that already worked for the download, the one you
typed in the form, then the defaults from `.env`. Only if none of them opens the archive, the download shows
**Enter password**, and the unpacking is tried again with what you enter.

### Restarts
The state of every unfinished download is kept in a `.anilink.json` next to its files, including the links Debrid-Link
created. After a restart, the container carries on where it was: finished parts are not fetched again, half
downloaded ones are continued, and no link is asked for twice.
A paused download stays paused until you resume it.

<br>

## Development
Requires the .NET 10 SDK, Node 26 and an installed [7-Zip](https://www.7-zip.org/).

```bash
# Backend, on http://localhost:5088
cd backend/AniLink.Api
dotnet run
```
Put the API key into `backend/AniLink.Api/appsettings.Local.json` (git-ignored; user secrets work too):
```json
{
  "DebridLink": { "ApiKey": "your-api-key" },
  "Archive": { "Passwords": [ "www.example.org" ] }
}
```
In development, downloads go to `downloads/` in the repository and 7-Zip is expected at
`C:\Program Files\7-Zip\7z.exe` (change `Archive:SevenZipPath` if it lives elsewhere).

```bash
# Frontend, on http://localhost:4200 (forwards /api to the backend)
cd frontend
npm ci
npm start
```
`npm run build` is the check that the frontend compiles; `dotnet build backend/AniLink.slnx` the one for the backend.
The Docker image is built from the repository root with `docker build -t anilink-downloader .`.

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

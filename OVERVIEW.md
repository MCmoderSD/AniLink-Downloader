# AniLink-Downloader

AniLink-Downloader downloads and unpacks media from single or multipart archives (mostly `.rar`, which may be password
protected), and runs as one container with a web page. Paste the hoster links of a season, an episode or a film, and it
creates the direct download links with [Debrid-Link](https://debrid-link.com/premium), downloads the archive parts,
unpacks them with 7-Zip and puts the renamed video into a folder named after the show.

Everything stays local: there is no account, no login and no database. 7-Zip is part of the image, so Docker is the
only thing you have to install.

## Quick reference
- **Source code, issues and releases:** [github.com/MCmoderSD/AniLink-Downloader](https://github.com/MCmoderSD/AniLink-Downloader)
- **Documentation:** [the wiki](https://github.com/MCmoderSD/AniLink-Downloader/wiki), with
  [Installation](https://github.com/MCmoderSD/AniLink-Downloader/wiki/Installation),
  [Configuration](https://github.com/MCmoderSD/AniLink-Downloader/wiki/Configuration),
  [Usage](https://github.com/MCmoderSD/AniLink-Downloader/wiki/Usage),
  [Troubleshooting](https://github.com/MCmoderSD/AniLink-Downloader/wiki/Troubleshooting) and the
  [HTTP API](https://github.com/MCmoderSD/AniLink-Downloader/wiki/HTTP-API)
- **Also published as:** `ghcr.io/mcmodersd/anilink-downloader`
- **Architectures:** `linux/amd64`, `linux/arm64` and `linux/arm/v7` (a Raspberry Pi 2 or newer)
- **You need:** Docker with Compose v2, [Debrid-Link Premium](https://debrid-link.com/premium) and an
  [API key](https://debrid-link.com/webapp/apikey)
- **License:** BSD 3-Clause, see [LICENSE](https://github.com/MCmoderSD/AniLink-Downloader/blob/master/LICENSE)

## Tags
| Tag      | Meaning                                                                        |
| -------- | ------------------------------------------------------------------------------ |
| `latest` | The newest release.                                                            |
| `3`      | The newest 3.x release: updates that stay within version 3.                    |
| `3.0`    | The newest 3.0.x release: bug fixes only.                                      |
| `3.0.2`  | Exactly this release. It never changes.                                        |
| `edge`   | The newest state of the `master` branch. It is not a release and may be rough. |

## Quick start
Create a folder, for example `anilink`, with an empty folder `downloads` inside it. Create the `downloads` folder
yourself, before the first start: on Linux, Docker would otherwise create it for `root`, and the container, which runs
as user `1000`, could not write into it. Next to it, create the file `docker-compose.yaml`:
```yaml
services:
  anilink:
    image: mcmodersd/anilink-downloader:latest
    container_name: AniLink-Downloader
    restart: unless-stopped
    pull_policy: always
    user: 1000:1000
    build: .
    environment:
      Concurrent_Downloads: 3
      Debrid_Request_Delay: 500
      Debrid_ApiKey: ${DEBRID_API_KEY}
      Archive_Password_0: ${ARCHIVE_PASSWORD_0:-}
      Archive_Password_1: ${ARCHIVE_PASSWORD_1:-}
    ports:
      - 8080:8080/tcp
    volumes:
      - ./downloads:/downloads

networks:
  default:
    name: AniLink-Network
```
The `build: .` line is only used when you build the image yourself, and can stay as it is.

And the file `.env`, with your key. The default archive passwords are optional:
```dotenv
DEBRID_API_KEY=your-api-key

ARCHIVE_PASSWORD_0=www.example.org
ARCHIVE_PASSWORD_1=
```

Start it from that folder and open [http://localhost:8080](http://localhost:8080):
```bash
docker compose up -d
```
Within a minute `docker compose ps` shows the container as `healthy`.

## Configuration
| Setting                | Where                 | Default       | Meaning                                                                        |
| ---------------------- | --------------------- | ------------- | ------------------------------------------------------------------------------ |
| `DEBRID_API_KEY`       | `.env`                | none          | Your private Debrid-Link API key. Required: the container stops without it.    |
| `ARCHIVE_PASSWORD_0`   | `.env`                | empty         | First default password for protected archives.                                 |
| `ARCHIVE_PASSWORD_1`   | `.env`                | empty         | Second default password. Tried after the first, before you are asked.          |
| `Debrid_Request_Delay` | `docker-compose.yaml` | `500`         | Pause in milliseconds between two Debrid-Link requests (0 to 60000).           |
| `Concurrent_Downloads` | `docker-compose.yaml` | `3`           | How many archive parts are downloaded at the same time.                        |
| `user`                 | `docker-compose.yaml` | `1000:1000`   | The user the container runs as, and so the owner of the files.                 |
| Port                   | `docker-compose.yaml` | `8080`        | `8080:8080` is `host:container`. For port 9090 on the host, write `9090:8080`. |
| Volume                 | `docker-compose.yaml` | `./downloads` | `./downloads:/downloads` is `host folder:container folder`.                    |

More than two default passwords, the log level and everything else are on the
[Configuration](https://github.com/MCmoderSD/AniLink-Downloader/wiki/Configuration) page.

## Good to know
- Unfinished downloads carry on after a restart of the container: their state is kept next to the files.
- The image has a health check on `/health/live`; `docker compose ps` shows the result.
- The web page and the API have no authentication, because they are meant for your own machine or network. **Do not
  expose port 8080 to the internet**; put it behind a VPN or a reverse proxy with authentication if it has to be
  reachable from outside.

## Disclaimer
This project **does not promote or endorse piracy**. Please only use AniLink-Downloader with **legally obtained,
licensed, or copyright-free content**. For your own protection, always use a **VPN**, since not all websites or hosters
are safe and trustworthy.
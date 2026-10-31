# [AniLink-Downloader](https://www.GitHub.com/MCmoderSD/AniLink-Downloader/)

AniLink-Downloader downloads and unpacks media from single or multipart archives (mostly `.rar`, which may be password
protected), and runs as one Docker container with a web page. Paste the hoster links of a season, an episode or a
film, and it creates the direct download links with [Debrid-Link](https://debrid-link.com/premium), downloads the
archive parts, unpacks them with 7-Zip and puts the renamed video into a folder named after the show.

Everything stays local: there is no account, no login and no database. 7-Zip is part of the image, so Docker is the
only thing you have to install.

**The documentation is in the [wiki](https://github.com/MCmoderSD/AniLink-Downloader/wiki):**
[Installation](https://github.com/MCmoderSD/AniLink-Downloader/wiki/Installation) ·
[Configuration](https://github.com/MCmoderSD/AniLink-Downloader/wiki/Configuration) ·
[Usage](https://github.com/MCmoderSD/AniLink-Downloader/wiki/Usage) ·
[Troubleshooting](https://github.com/MCmoderSD/AniLink-Downloader/wiki/Troubleshooting) ·
[HTTP API](https://github.com/MCmoderSD/AniLink-Downloader/wiki/HTTP-API) ·
[Development](https://github.com/MCmoderSD/AniLink-Downloader/wiki/Development)

## Quick start
You need [Docker](https://docs.docker.com/get-docker/) with Compose v2 (the `docker compose` command),
[Debrid-Link Premium](https://debrid-link.com/premium) and an [API key](https://debrid-link.com/webapp/apikey).

Create a folder, for example `anilink`, with an empty folder `downloads` inside it. Create the `downloads` folder
yourself, before the first start: on Linux, Docker would otherwise create it for `root`, and the container could not
write into it. Next to it, create the file `docker-compose.yaml`:
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

## Disclaimer
This project **does not promote or endorse piracy**.
Please only use AniLink-Downloader with **legally obtained, licensed, or copyright-free content**.
The examples in this README and in the wiki are purely illustrative.

For your own protection, always use a **VPN**, since not all websites or hosters are safe and trustworthy.
Only download and process content that is **public domain, self-created, or explicitly free of copyright restrictions**.

The web page has no authentication, because it is meant for your own machine or network. **Do not expose port 8080 to
the internet**; put it behind a VPN or a reverse proxy with authentication if it has to be reachable from outside.

## License
BSD 3-Clause, see [LICENSE](LICENSE).
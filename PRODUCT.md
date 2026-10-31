# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Self-hosters who run AniLink-Downloader on their own machine, home server or Raspberry Pi and work with its web page at a desktop. Their job is a short burst of setup followed by waiting: paste the hoster links of a season, an episode or a film, enter a password when none of the known ones opens an archive, and then keep an eye on the progress while doing something else. The page has no authentication because it is meant for the user's own machine or network.

Mobile layouts exist in the code, but a phone or tablet is not a confirmed primary device.

## Product Purpose

AniLink-Downloader turns hoster links into finished files. It creates direct links with Debrid-Link, downloads the archive parts (mostly `.rar`, possibly multipart and password protected), unpacks them with 7-Zip, and renames the video and its `.nfo` into a folder named after the show.

Success is a clean, correctly named result in the `downloads/` folder. What happens to it afterwards (a media library, a player) is the user's own business and is not part of the product.

## Positioning

Two claims are confirmed:

- One paste of a whole season ends in finished, named files: link creation, download, unpacking with password trials, and renaming run by themselves, and the page asks only when it is blocked.
- It is one Docker container with no account, no login and no database. 7-Zip is part of the image, and after a restart the work continues where it stopped, using the manifest it keeps.

Per-episode control (pause, resume, cancel, retry, remove) exists, but the owner did not choose it as part of the positioning.

## Operating Context

- Runs as one Docker container (Compose v2) on Windows, macOS or Linux, for `linux/amd64`, `linux/arm64` and `linux/arm/v7`. The page is served on port 8080 and must not be exposed to the internet.
- Needs a Debrid-Link Premium account and its API key, set in `.env`. Default archive passwords are also set there and tried in order of their numbers before the user is asked.
- Needs free disk space of about twice the size of the download, because the archives stay until everything is unpacked and sorted.
- Every episode (a film counts as one) goes through the stages Creating links, Downloading, Unpacking, Password needed, Sorting files and Done, with Failed, Canceled and Paused as side states. The page shows the current stage as a chip.
- Archive parts are downloaded several at a time over all downloads together, and as many archives are unpacked at once as the server has CPU threads. Neither is a per-download setting.
- On Docker Desktop for Windows, the shared `downloads` folder limits unpacking to roughly 100 to 200 MB/s. This is a property of the environment, not of the app.

## Capabilities and Constraints

- Pages: Downloads (jobs and their episodes, live progress, activity log) and New download (links, season or film, season format, password handling).
- Controls on a whole download and on a single episode: pause, resume, cancel, retry, remove.
- Terminology: a download is a job; a job is a season or a film; a season has episodes, a film is a single item; an episode has archive parts.
- The user interface is English.
- The README states that the project does not promote or endorse piracy and is meant for legally obtained, licensed or copyright-free content. Copy on the page should stay consistent with that.
- Undecided: whether the output is ever tailored to a specific media library. The owner's current answer is "only a folder on the disk".

## Brand Commitments

The name is AniLink-Downloader, and it is what the browser tab always says on every page; the navbar shows the short form AniLink. The mark is the owner's own `frontend/public/favicon.svg` (a katakana glyph with a download arrow); it is used in the browser tab and in the navbar and is not to be redrawn.

## Evidence on Hand

- The wiki (https://github.com/MCmoderSD/AniLink-Downloader/wiki): stages, rules, configuration, HTTP API, what ends up on disk. `README.md` holds the short description, the quick start and the disclaimer.
- `frontend/CLAUDE.md`: the owner's accessibility and component standards.
- There is no user research, analytics, testimonials or screenshots of real use. Future work must not invent any.

## Product Principles

- A paste in, finished files out. Every screen exists to get a named file into the folder with as little attention as possible.
- The page is a window onto work that goes on without it. It reports the true state of every episode and asks for attention only when the process is blocked, such as a missing password or a failure.
- Nothing to sign up for. No account, no cloud and no database; nothing in the interface suggests any of them.

## Accessibility & Inclusion

The owner's instructions require that the web page passes all AXE checks and meets WCAG AA minimums, including focus management, colour contrast and ARIA attributes.

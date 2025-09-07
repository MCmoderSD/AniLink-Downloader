# [AniLink-Downloader](https://www.GitHub.com/MCmoderSD/AniLink-Downloader/)


<br>


## Description
AniLink-Downloader is a tool specifically designed to download and extract `.mkv` files from single or multipart `.rar` archives (which may be password-protected). 
If no password is required, the configured password will simply be ignored.

It:
- Accepts multiple links as input and automatically orders them correctly.
- Downloads files from one of debrid-link supported hosters
- Extracts single or multipart `.rar` archives (including password-protected ones).
- Extracts the subtitles from the`.mkv` files.


<br>


## Disclaimer
This project **does not promote or endorse piracy**. 
Please only use AniLink-Downloader with **legally obtained, licensed, or copyright-free content**. 
The examples provided in this README are purely illustrative.

For your own protection, always use a **VPN**, since not all websites or hosters are safe and trustworthy. 
Websites like `www.anime-loads.org` **must not be used**, as it cannot be guaranteed that the content offered there is license-free.

Only download and process content that is **public domain, self-created, or explicitly free of copyright restrictions**.


<br>


## Requirements
- [Java 21 JDK](https://www.oracle.com/de/java/technologies/downloads/#java21)
- [Debrid-Link Premium](https://debrid-link.com/premium)
- [7zip](https://www.7-zip.org/)
- [MKVToolNix](https://mkvtoolnix.org/)


<br>


## Additional Tools
Since you often need all links for each part of an episode or file, you can use my [TabScraper](https://github.com/MCmoderSD/tabscraper?tab=readme-ov-file) browser extension to scrape all links. 
You can install it from the [Chrome Web Store](https://chromewebstore.google.com/detail/tab-scraper/ahdhhonppgdiglmppkcjckijelfdalho).

Then simply open each link in a new tab, click the TabScraper icon in the toolbar and enter a regex to filter the links.

For example, to filter all RapidGator links, you can use:
```regexp
^https:\/\/rapidgator\.net\/file\/.+\.(?:part\d+\.)?rar(?:\.html)?$
```
Then click **Scrape Tabs** and save the links to a text file. 
You can then use this text file with the `--import` argument to process all files.


<br>


## Setup

#### 1. Clone the repository:
```bash
git clone https://www.github.com/MCmoderSD/AniLink-Downloader.git
```

#### 2. Create a `config.json` file in the `src/main/resources` folder with the following structure:
```json
{
  "debug": false,
  "delay": 1000,
  "7zip": "C:\\Program Files\\7-Zip\\7z.exe",
  "mkvToolNix": "C:\\Program Files\\MKVToolNix",
  "password": "www.anime-loads.org",
  "apikey": "YOUR_DEBRID_LINK_API_KEY"
}
```
- Edit the paths to `7z.exe` and `mkvmerge.exe` according to your installation.
- Replace `YOUR_DEBRID_LINK_API_KEY` with your actual Debrid-Link Premium API key.
- The `password` field is an example for archives that require extraction keys (e.g., `www.anime-loads.org`).

#### 3. Compile the artifact to a runnable JAR file using your preferred IDE and run it:
```bash
java -jar AniLink-Downloader.jar
```


<br>


## Usage
AniLink-Downloader can be used in three modes:

#### 1. Automatic Download (Default Mode)
```bash
java -jar AniLink-Downloader.jar
```
- You will be prompted to enter a file/season prefix (e.g. `S01E`).
- Paste all the part links (one per line) and finish input by pressing Enter on an empty line.
- The script will group, download, and extract the files.

#### 2. Batch Download from a Text File (Import Mode)
```bash
java -jar AniLink-Downloader.jar --import links.txt
```
- `links.txt` should contain one link per line (order does not matter).
- The script will download, extract, and organize the files.

#### 3. Manual Mode
```bash
java -jar AniLink-Downloader.jar --manual
```
- Enter the name of the file/episode manually.
- Paste all part links (one per line).
- The script will download and extract the file.


<br>


## Run Arguments
- `--help, -h`                 Show help message
- `--version, -v`              Show version information
- `--debug, -d`                Enable debug mode
- `--delay, -w`                Override delay between requests (in ms)
- `--7zip, -z`                 Override 7-Zip path
- `--mkv-tool-nix, -t`         Override MKVToolNix path
- `--password, -p`             Override archive password
- `--apikey, -a`               Override API key
- `--import, -i`               Import links from a text file
- `--manual, -m`               Run in manual mode
- `--cleanup, -c`              Cleanup function (move files, delete empty folders)
- `--subtitle, -s`             Extract subtitles from MKV files

Example with multiple arguments:
```bash
java -jar AniLink-Downloader.jar --debug --delay 2000 --manual
```

<br>

There is also a cleanup function, that will move all files the current directory and delete the empty folders.

You can run it with the `--cleanup` or `-c` argument:
```bash
java -jar Anime-Loads-Downloader.jar --cleanup
```

<br>

You can also run the subtitle extractor in the current directory with the `--subtitle` or `-s` argument:
```bash
java -jar Anime-Loads-Downloader.jar --subtitle
```
This will again extract all subtitles from the mkv files in the current directory.
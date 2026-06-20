# [AniLink-Downloader](https://www.GitHub.com/MCmoderSD/AniLink-Downloader/)

<br>

## Description
AniLink-Downloader is a tool specifically designed to download and extract `.mkv` files from single or multipart `.rar` archives (which may be password-protected).
If no password is required, the configured password will simply be ignored.

It:
- Accepts multiple links as input and automatically orders them correctly.
- Downloads files from one of debrid-link supported hosters
- Extracts single or multipart `.rar` archives (including password-protected ones).

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
- [Java 25](https://www.oracle.com/de/java/technologies/downloads/#java25)
- [Debrid-Link Premium](https://debrid-link.com/premium)

<br>

## Additional Tools
Since you often need all links for each part of an episode or file, you can use my [TabScraper](https://github.com/MCmoderSD/tabscraper?tab=readme-ov-file) browser extension to scrape all links.
You can install it from the [Chrome Web Store](https://chromewebstore.google.com/detail/tab-scraper/ahdhhonppgdiglmppkcjckijelfdalho).

Then simply open each link in a new tab, click the TabScraper icon in the toolbar and enter a regex to filter the links.

For example, to filter all RapidGator links, you can use:
```regexp
^https://rapidgator.net/file/.+.(?:part\d+.)?rar(?:.html)?$
```
Then click **Scrape Tabs** and save the links to a text file.
You can then use this text file with the `--import` argument to process all files.

<br>

## Setup

### Option 1: Using the Runnable JAR

#### 1. Download the latest release from the [Releases](https://www.github.com/MCmoderSD/AniLink-Downloader/releases) page and run the JAR file:
```bash
java -jar AniLink-Downloader.jar
```
You will be prompted to enter delay (default: 500ms), decryption password, and your Debrid-Link API key.
You will have to enter those values every time you run the script, so if you want to avoid that, you can use [Option 2](#option-2-building-from-source).


### Option 2: Building from Source

#### 1. Clone the repository:
```bash
git clone https://www.github.com/MCmoderSD/AniLink-Downloader.git
```

#### 2. Create a `config.json` file in the `src/main/resources` folder with the following structure:
```json
{
  "delay": 500,
  "password": "www.anime-loads.org",
  "apiKey": "YOUR_DEBRID_LINK_API_KEY"
}
```
- The `password` field is an example for archives that require extraction keys (e.g., `www.anime-loads.org`).
- Replace `YOUR_DEBRID_LINK_API_KEY` with your actual Debrid-Link Premium API key.

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

#### 3. Manual Download (Manual Mode)
```bash
java -jar AniLink-Downloader.jar --manual
```
- You will be prompted to enter a name for the file.
- Then, you can paste the links for each part of the file one by one, pressing Enter after each link.
- After entering all links, finish input by pressing Enter on an empty line.
- The script will download and extract the file, saving it with the specified name.

#### 4. Movie Batch Download (Movie Mode)
```bash
java -jar AniLink-Downloader.jar --movie
```
- The script will scan the current directory for folders containing a `links.txt` file.
- Each `links.txt` file should contain one link per line (order does not matter).
- The script will download and extract the files, saving them with the folder name as the file name.

<br>

## Run Arguments
- `--version, -v`              Show version information
- `--import, -i`               Import links from a text file
- `--manual, -m`               Run in manual mode
- `--movie, -mo`               Run in movie batch mode
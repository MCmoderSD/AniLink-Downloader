---
name: AniLink-Downloader
description: A dark, instrument-like web page that turns pasted hoster links into finished files and shows the true state of every episode while it works.
colors:
  tidewater-cyan: "#0db7ed"
  tidewater-ink: "#00131a"
  tidewater-deep: "#014d66"
  tidewater-mist: "#cae7f8"
  tidewater-shade: "#013041"
  night-base: "#0d0e10"
  night-lowest: "#06090a"
  night-low: "#101417"
  night-raised: "#14181b"
  night-high: "#1d2225"
  night-highest: "#272c2f"
  night-bright: "#31363a"
  paper-text: "#e5e6e9"
  paper-dim: "#b6bec3"
  steel-outline: "#82898e"
  steel-hairline: "#3d464b"
  go-green: "#4ade80"
  go-green-deep: "#0f3d24"
  caution-amber: "#fbbf24"
  caution-amber-deep: "#453000"
  fault-coral: "#ff8a88"
  fault-coral-deep: "#5e121c"
typography:
  headline:
    fontFamily: "Roboto, sans-serif"
    fontSize: "1.75rem"
    fontWeight: 400
    lineHeight: "2.25rem"
  title-large:
    fontFamily: "Roboto, sans-serif"
    fontSize: "1.375rem"
    fontWeight: 400
    lineHeight: "1.75rem"
  title-medium:
    fontFamily: "Roboto, sans-serif"
    fontSize: "1rem"
    fontWeight: 500
    lineHeight: "1.5rem"
  title-small:
    fontFamily: "Roboto, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 500
    lineHeight: "1.25rem"
  body:
    fontFamily: "Roboto, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 400
    lineHeight: "1.25rem"
  body-small:
    fontFamily: "Roboto, sans-serif"
    fontSize: "0.75rem"
    fontWeight: 400
    lineHeight: "1rem"
  label-large:
    fontFamily: "Roboto, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 500
    lineHeight: "1.25rem"
    letterSpacing: "0.1px"
  label-medium:
    fontFamily: "Roboto, sans-serif"
    fontSize: "0.75rem"
    fontWeight: 500
    lineHeight: "1rem"
  links-mono:
    fontFamily: "ui-monospace, 'Cascadia Mono', 'SF Mono', Consolas, monospace"
    fontSize: "0.6875rem"
    fontWeight: 400
    lineHeight: "1.0625rem"
  icons:
    fontFamily: "'Material Icons', sans-serif"
    fontSize: "1.5rem"
    fontWeight: 400
    lineHeight: 1
rounded:
  sm: "0.5rem"
  md: "0.75rem"
  lg: "1.125rem"
  xl: "1.5rem"
  pill: "999px"
spacing:
  xs: "0.25rem"
  sm: "0.5rem"
  md: "1rem"
  lg: "1.25rem"
  xl: "1.5rem"
  page-max: "64rem"
  navbar: "4rem"
components:
  button-primary:
    backgroundColor: "{colors.tidewater-cyan}"
    textColor: "{colors.tidewater-ink}"
    typography: "{typography.label-large}"
    rounded: "{rounded.pill}"
    height: "2.5rem"
    padding: "0 1.5rem"
  button-text:
    backgroundColor: "transparent"
    textColor: "{colors.tidewater-cyan}"
    typography: "{typography.label-large}"
    rounded: "{rounded.pill}"
    height: "2.5rem"
    padding: "0 1rem"
  panel:
    backgroundColor: "{colors.night-low}"
    textColor: "{colors.paper-text}"
    rounded: "{rounded.lg}"
    padding: "1.25rem 1.5rem"
  episode-row:
    backgroundColor: "{colors.night-raised}"
    textColor: "{colors.paper-text}"
    rounded: "{rounded.md}"
    padding: "0.875rem 1rem"
  chip:
    backgroundColor: "{colors.night-high}"
    textColor: "{colors.paper-dim}"
    typography: "{typography.label-medium}"
    rounded: "{rounded.pill}"
    padding: "0.125rem 0.625rem"
  chip-success:
    backgroundColor: "{colors.go-green-deep}"
    textColor: "{colors.go-green}"
  chip-warning:
    backgroundColor: "{colors.caution-amber-deep}"
    textColor: "{colors.caution-amber}"
  chip-danger:
    backgroundColor: "{colors.fault-coral-deep}"
    textColor: "{colors.fault-coral}"
  chip-info:
    backgroundColor: "{colors.tidewater-shade}"
    textColor: "{colors.tidewater-cyan}"
  progress-track:
    backgroundColor: "{colors.night-highest}"
    rounded: "{rounded.pill}"
    height: "0.625rem"
  progress-fill:
    backgroundColor: "{colors.tidewater-cyan}"
    rounded: "{rounded.pill}"
    height: "0.625rem"
  text-field:
    backgroundColor: "transparent"
    textColor: "{colors.paper-text}"
    rounded: "{rounded.md}"
    height: "3.5rem"
  segmented-switch:
    backgroundColor: "transparent"
    textColor: "{colors.paper-dim}"
    typography: "{typography.label-large}"
    rounded: "{rounded.pill}"
    padding: "0.25rem"
  notification:
    backgroundColor: "{colors.night-highest}"
    textColor: "{colors.paper-text}"
    typography: "{typography.body}"
    rounded: "{rounded.md}"
    padding: "0.5rem 0.5rem 0.5rem 0.75rem"
---

# Design System: AniLink-Downloader

## Overview

**Creative North Star: "The Night Shift Console"**

AniLink is a dark control panel for work that carries on by itself. A person pastes a season, sets it going and looks away; the page is what they glance at later. So it behaves like an instrument rather than a storefront: calm, precise and quiet, with one clear signal colour and a handful of status colours that each mean one thing. Nothing animates to be noticed. Motion confirms a change (a bar that fills, a switch that slides, a page that fades to the next) and stops.

Surfaces are near-black with a faint cool tint taken from the brand cyan, stepped in small, even tonal increments, and separated by 1px hairlines instead of shadows. Content sits in rounded panels that hold rows of episodes. Controls are pills and big, solid, high-contrast buttons, so a state is never in doubt: the filled cyan button is the action, a tinted pill is the selection, a coloured chip with a word in it is the state.

The system is dark only. Light theme is not planned and the system does not define one.

**Key Characteristics:**
- Dark only, near-black ground with a cool tint from the brand hue.
- Depth through tonal steps plus a 1px hairline; shadows only for what floats.
- One signal colour (Tidewater Cyan) for "active or actionable", four status colours for meaning.
- Pills for every interactive control, 18px panels, 12px rows.
- Tabular figures on every number that changes; chips and labels always carry the state in words.
- Short, decelerating motion (120 and 220 ms) that is reduced to nothing under `prefers-reduced-motion`.

## Colors

A cool near-black ground, one bright cyan signal, and four status colours that never act as decoration.

### Primary
- **Tidewater Cyan** (#0db7ed): the signal. Filled buttons, the skip link, the active progress bar, the selected option of a switch, focus rings, text buttons in the navbar, the icon of an empty state, the 404 code. Roughly 8.3:1 against the page, so it also works as text.
- **Tidewater Ink** (#00131a): the text and icons on Tidewater Cyan. White on the cyan would reach only about 2.3:1; the ink reaches about 8.1:1.
- **Tidewater Deep** (#014d66) and **Tidewater Mist** (#cae7f8): the container pair of the primary role, pinned for Material components that read it. No bespoke component uses it directly.
- **Tidewater Shade** (#013041): the ground of an informational chip, with Tidewater Cyan as its text (about 6.0:1).

### Neutral
- **Night Base** (#0d0e10): the page and the navbar (at 88% over the page).
- **Night Lowest** (#06090a): the deepest step, pinned for Material surfaces; no bespoke component reads it yet.
- **Night Low** (#101417): panels (job cards, form sections).
- **Night Raised** (#14181b): rows inside a panel, such as an episode.
- **Night High** (#1d2225): chips at rest.
- **Night Highest** (#272c2f): progress tracks and notifications.
- **Night Bright** (#31363a): Material's brightest surface step, pinned for its components; no bespoke component reads it yet.
- **Paper Text** (#e5e6e9): main text, about 15.5:1 on the page.
- **Paper Dim** (#b6bec3): secondary text, hints, metadata.
- **Steel Outline** (#82898e): the outline of fields and the switch, and the neutral progress bar.
- **Steel Hairline** (#3d464b): the 1px border of panels, notifications and dividers, about 2.0:1 against the page.

### Status
- **Go Green** (#4ade80) on **Go Green Deep** (#0f3d24): done, success.
- **Caution Amber** (#fbbf24) on **Caution Amber Deep** (#453000): paused, the server cannot be reached, needs attention.
- **Fault Coral** (#ff8a88) on **Fault Coral Deep** (#5e121c): failed, error, not assigned.
- **Tidewater Cyan** on **Tidewater Shade**: informational chips and the default notice. Information shares the brand cyan, so the system has a single blue.

The theme also generates a lilac tertiary role (#c8c2ea). No component reads it; treat it as unassigned.

### Named Rules
**The Status Light Rule.** Colour means state. Cyan marks what is active, can be acted on or informs; green, amber and coral mean done, attention and failure. A colour is never added for decoration.

**The Say It Too Rule.** A state is never told by colour alone. Every chip, every activity entry and every bar carries the state in a word (Unpacking, Failed, Paused), so the colour only confirms it.

**The Ink On Cyan Rule.** Anything laid on Tidewater Cyan uses Tidewater Ink. White text on the cyan is not allowed.

**The Dark Only Rule.** There is no light theme and no light surface. This is confirmed by the owner.

## Typography

**Display Font:** none; the page has no hero type.
**Body Font:** Roboto (with sans-serif), self-hosted through `@fontsource/roboto`.
**Label/Mono Font:** the system monospace stack (`ui-monospace`, Cascadia Mono, SF Mono, Consolas) for pasted links and paths.
**Icon Font:** Material Icons, self-hosted through `@fontsource/material-icons`, 1.5rem by default and 1.25rem inside dense rows.

**Character:** One plain family at modest sizes, weighted 400 for reading and 500 for anything that labels or acts. Hierarchy comes from size and weight, never from a second typeface.
### Hierarchy
- **Headline** (400, 1.75rem, 2.25rem): the page title ("Downloads", "New download").
- **Title Large** (400, 1.375rem, 1.75rem): a job's name, the brand name in the navbar, the empty state's title.
- **Title Medium** (500, 1rem, 1.5rem): section titles inside forms.
- **Title Small** (500, 0.875rem, 1.25rem): an episode's name and small block titles.
- **Body** (400, 0.875rem, 1.25rem): running text and subtitles. Keep lines under about 65 characters; the page container stops at 64rem.
- **Body Small** (400, 0.75rem, 1rem): metadata, hints, part rows, the activity log.
- **Label Large** (500, 0.875rem, 1.25rem, 0.1px tracking): buttons, bar labels, percentages.
- **Label Medium** (500, 0.75rem, 1rem): chips and small toggles.
- **Links Mono** (400, 0.6875rem, 1.0625rem): the pasted link list, so a long hoster link stays mostly visible.

### Named Rules
**The Tabular Rule.** Every number that changes (percent, bytes, speed, time) uses tabular figures, so a value updating never shifts its neighbours.

**The One Family Rule.** Roboto carries everything except link lists and paths. Do not add a display or accent typeface.

## Layout

A single centred column, 64rem at most, with 1.25rem side padding and 2rem above and 4rem below. A sticky navbar 4rem high sits on top; the page scrolls under it, and the app draws its own scroll bar over the right edge instead of the platform's. A footer closes the page: at the bottom of the screen on a short page, below the content on a long one. Pages are stacks of panels with 1.25rem between them. Inside a panel the rhythm is 1.25rem between blocks, 0.5 to 1rem between related items and 0.25rem between rows of a list.

Density is moderate and information-forward: a job card shows its bars and actions at once and folds its episodes and activity log behind a toggle that opens with a height animation. Forms use flexible rows that wrap (`flex: 1 1 18rem` for wide fields, `0 1 11rem` for narrow ones).

Below 40rem the page tightens: page and panel padding drop to 1rem, and the navbar hides the brand name. A download card's buttons share its width in filled rows, with a hairline around the plain ones, and its unpack setting is a row of the same frame and height, the words at the start and the switch at the end, which a finger can hit anywhere along the row. The second "New download" button of the page header goes, because the navbar has it. An episode row keeps its name and state on the first line and puts the part count on the left and the actions on the right of the second; its unpack setting is then the first of those actions, a button that is lit while the episode is unpacked when it is downloaded, instead of a switch with its words. A part row is one line: the file name takes what is left, cut in the middle so that the end (".part2.rar") stays, followed by the share that is downloaded and a symbol for the state, a different shape for each so that colour is still not the only sign; the word of the state is its title and its text for a screen reader. The episode preview collapses to fewer columns (it drops its file names), the form's buttons stack at the full width with the download button on top, and notifications take the width of the page. On Decode DLC the two buttons of the result share one row, each taking half of it, and below 30rem they lose their icons like the navbar's. Below 30rem the navbar buttons lose their icons, except "Files", which loses its word and keeps its folder, and "Decode DLC" and "New download" shorten to "DLC" and "New" (the hidden words stay for a screen reader), so that the mark and four buttons fit into 320px. Below 30rem of height, a phone held sideways, the navbar is 3rem instead of 4rem. There is no other breakpoint.

On a touch screen (`pointer: coarse`) a target is at least 44px: buttons are 44px high, the icon buttons of an episode 44px square, the label of a switch 44px high and the close button of a notification 44px square. The chevrons that step the season number are hidden, since a number keypad opens for the field, and the link box keeps its small type everywhere except on iOS, where it uses 16px, because iOS zooms into a field with smaller type when it gets focus. When the page becomes visible again after the phone slept, the list of downloads is read anew.

## Elevation & Depth

Depth is tonal. A panel is one step lighter than the page, a row inside it one step lighter again, and a 1px Steel Hairline draws each edge. On a near-black ground a drop shadow is close to invisible, so shadows only soften what actually floats.

### Shadow Vocabulary
- **Raised** (`box-shadow: 0 1px 2px rgb(0 0 0 / 0.4)`): panels, together with the hairline.
- **Lifted** (`box-shadow: 0 2px 8px rgb(0 0 0 / 0.45)`): notifications, the small pieces that float above the page.
- **Floating** (`box-shadow: 0 8px 24px rgb(0 0 0 / 0.5)`): defined in the tokens for larger floating pieces; no component uses it yet.

### Named Rules
**The Lift, Not Shadow Rule.** Separate surfaces by tone and a hairline first. A shadow is a finishing touch for things that float above the page, never the thing that makes a card look like a card.

## Shapes

Generously rounded, with a clear ladder: 8px for small blocks inside a row (errors, part rows), 12px for rows, fields and notifications, 18px for panels, 24px for the largest surfaces (the step Material's large corner maps onto), and a full pill for everything you press, select or read as a label (buttons, chips, the switch, the progress bar). Borders are 1px hairlines, and the only thick stroke is a 3px status bar on the left edge of an activity entry. Focus is a 2px ring in Tidewater Cyan, offset 2px.

## Components

Tactile and confident: solid fills, full pills, strong contrast between rest and active, and states that cannot be misread.

### Buttons
- **Shape:** full pill (999px), 40px high.
- **Primary (filled):** Tidewater Cyan with Tidewater Ink, 0 24px padding, Label Large. One per view where possible (New download).
- **Danger (filled):** Fault Coral on its dark container instead of cyan, only for the button that confirms a deletion that cannot be undone ("Delete permanently"). The dialog's focus starts on the dialog, never on this button.
- **Text:** transparent, cyan label, 0 16px padding. The page the user is on reads as pressed with a 6% white tint.
- **Icon buttons** (per-episode controls): 36px square with a 20px icon, 6% white tint on hover, 10% when pressed, always with an aria-label.
- **Hover / Focus:** a neutral white tint rather than a brand tint, so a hover never looks like a selection; focus is the global 2px cyan ring.

### Chips
- **Style:** pill, Label Medium, 2px by 10px padding. At rest Night High with Paper Dim. With a tone, the deep status colour behind the bright one (see Status).
- **State:** a chip is a read-only label of state, never a button.

### Cards / Containers
- **Corner Style:** 18px for panels, 12px for episode rows, 8px for blocks inside rows.
- **Background:** Night Low for panels, Night Raised for rows.
- **Shadow Strategy:** the Raised shadow plus a hairline; see Elevation & Depth.
- **Border:** 1px Steel Hairline on panels. Rows have none.
- **Internal Padding:** 1.25rem by 1.5rem for panels (1rem on small screens), 0.875rem by 1rem for rows.

### Inputs / Fields
- **Style:** Material outlined fields, 56px high, 12px corners, a Steel Outline stroke, floating label in Paper Dim. Number fields use the app's own stepper (two small chevrons) instead of the browser's.
- **Focus:** the outline and label turn to Tidewater Cyan.
- **Error / Disabled:** hints and errors use Fault Coral text; a disabled chevron dims to Steel Hairline instead of vanishing, so the field does not twitch at its limits.
- **File drop (link list):** a text file dropped on the link list adds every `http` and `https` link in it below the links already there, skipping those that are in the field already (the same link with `http` instead of `https`, or with the host in other capitals, counts as in the field). While a file is held over the list, a dashed Tidewater Cyan outline on Night Low covers the field and its hint, with the upload icon and "Drop a text or DLC file to add its links"; dragged text does not trigger it, so it still lands in the field as text. The result is a note in the corner: how many links were added, or that the file held none, only known ones, is larger in bytes than the characters a download may hold, or would make the field longer than that; the field stays as it is in the last two cases. A `.dlc` file is decoded first, as on Decode DLC ("Decoding …" stands under the field meanwhile): its links go into the field like those of a text file, an empty name field gets the name of the package, its passwords join those the form already holds (the hint under the password field counts them), and the type switch follows the links. Of a container with several packages the first one with links is taken and a note says so; only one DLC file is decoded at a time, and a text file dropped with it is added afterwards.
- **DLC drop (Decode DLC page):** a 2px dashed Steel Outline frame with 12px corners, holding the upload icon, "Drop a .dlc file here", "or", a filled "Choose file" button and the largest size the server takes. While a file is held over it, the frame, the icon and the text turn Tidewater Cyan on Night Raised. On a touch screen the drop wording goes and only the button stays. Only a `.dlc` file that is not empty and not larger than the limit is sent, one at a time: of several files the first `.dlc` counts, and a note says so. While the server decodes, the button is disabled and "Decoding …" stands under the frame; a refusal or an error of the server is a Fault Coral callout in the same place. A hint under the frame says that the key part of the file goes to the JDownloader key service. Once a file is decoded, the whole panel with the frame gives way to the result, and a text button "Back" (arrow and word) at the start of the result's first row brings it back, with the focus on "Choose file"; after a decode the focus is on the result's heading.
- **Type switch (New download):** Season or Movie follows the links as they arrive, by what the file names say: Movie when every link belongs to one archive or one file (a single episode counts as one release), Season as soon as two different episodes show (`S01E05`, `1x05`, `E05`, `Episode 5`, `Folge 5`), and no change when neither holds, as for anime names without a marker. It stops following the moment the person presses one of its options, and the same check serves a list that is pasted, dropped as a file or handed over from Decode DLC.

### Navigation
- A sticky bar on Night Base (88% over the page) with a 12px backdrop blur and a hairline underneath. The mark and the name "AniLink" sit on the left; "Downloads", "Files" and "Decode DLC" are text buttons, "New download" is the filled button. While the server cannot be reached, a full-width amber strip (2.5rem, a cloud-off icon and "The server is not reachable.") sits under the bar as part of the sticky header; nothing is shown while all is well. On small screens only the mark remains on the left.

### Footer
- A hairline above it, on the page itself, in the column of the navbar. At the left edge of the mark stands "AniLink-Downloader v3.3.0" in Label Medium, a link to the release of the version that is running (the number is read from package.json when the page is built). At the right edge of the buttons stand three links to the GitHub repository, the Docker Hub image and the wiki, each with the mark of the service in front of it: the GitHub mark and the book from Octicons, the whale from Simple Icons. They are SVG files in public/icons, drawn as a mask over the colour of the text, so an icon turns with its link. They are plain text in the muted colour, like any link: when pointed at or focused, they take the primary text colour and are underlined. All of them open in a new tab, which a screen reader is told. There are no legal pages: the tool is hosted by its owner and is not meant to be reachable from outside. Below 40rem the name and the version go and the three links are centred in one row, which fits 320px; on a touch screen the links are 44px high without a frame.

### Decode DLC (page)
- A panel for the file and one panel per package, 1.25rem apart. The file panel starts with the "Back" button and the file name, and holds two buttons, both disabled while there are no links: the outlined "Download links", which opens the browser's save dialog with Downloads.txt as the name (a browser without one just downloads the file), and the filled "Add download", which opens New download with the package's name, type, links and passwords filled in (with several packages, the file panel keeps only "Download links" and every package panel has its own "Add download"), chips for the packages, files and size (a file has at most one link, so links are not counted separately; an amber chip counts the files without one) and every value of the container's header that is there, one row each: generator app, generator version, generator address, tribute name and DLC XML version, with the label above the value on small screens. A package panel has its name with a chip for its number of files, its category and comment as label and value rows, its passwords and its files. The passwords are a list of Night Raised rows, one per password, each hidden as eight bullets until the eye button at its end shows it (a toggle with `aria-pressed`, labelled "Show password" or "Hide password", numbered when there are several); a second icon button copies it, also over plain http where the browser has no clipboard API, and a note says "Password copied.". A new file hides every password again. The files are Night Raised rows: the file name and size on the first line (the size in bytes as its title), the link in mono below. The rows scroll inside 20rem (16rem on small screens) with the app's own scroll bar. A missing name or link reads in italics ("Unnamed file", "No link in the container"). Downloads.txt holds every link, one per line, in the order of the container. On New download, a hint under the password field says how many passwords of the DLC file are saved with the download and tried first.

### Files (page)
- A title, a line under it with the folders, files and size of what is shown, a row with five icon buttons and the path as a breadcrumb, a folder tree beside the list, and one panel with a toolbar and a table. The buttons are Back, Forward, Up one level, Refresh and a toggle for the folder tree (`aria-pressed`, lit like the navbar's current page), as in a file manager, each with an aria-label; Back and Forward walk through the folders that were opened on the page (opening a folder drops what lay ahead), Up opens the parent and is off in the downloads folder, and Refresh reads the folder again, which the page also does by itself. Below 40rem the row wraps, so that the breadcrumb takes its own line. The breadcrumb is a `nav` with an ordered list: the downloads folder, each folder above, and the folder shown as plain text with `aria-current="page"`.
- The toolbar is a group (not a toolbar widget) with "Nothing selected" or "2 selected · 4.1 GiB in 31 files" at the start, a text button "Clear" and an outlined "Delete" at the end. Both stay focusable while nothing is selected.
- The table is a native `table` with a hidden caption ("Contents of Dr. Stone"), a select-all checkbox in the header (partly checked when only some rows are) and one row per entry on Night Raised, 12px corners, 0.25rem apart. A row has a checkbox, the icon and name, the number of files (a folder only), the size (every entry has one, so it sits next to the time) and the modification time as "20 Nov 2026, 14:33" in a `time` element, all numbers in tabular figures. Sizes are binary and say so: 1024 bytes are one KiB, not one KB, and the exact number of bytes is the title of the size. A folder's name is a link that opens it; a file's name is cut in the middle so that its end stays, with the full name as its title. A screen reader hears "Folder:" or "File:" before a name.
- The columns beside the name are as wide as their widest content, so only small gaps stay: 8px between number of files and size, 12px between size and time, 8px between the checkbox and the icon and between the icon and the name. The checkbox is 32px (its touch target stays 48px). A folder row is one target: the link on its name stretches over the whole row, the checkbox sits above it, and keyboard focus draws a 2px cyan ring with 12px corners around the row. A file or a link has no target yet.
- The icon says what an entry is, all in the same cyan: a solid folder; `video_file` (mkv, mp4, avi, …), `audio_file`, `image`, `subtitles` (srt, ass, vtt, …), `description` for text (txt, log, nfo, md, md5), `data_object` for json, xml, yaml and csv, `picture_as_pdf`, `language` for web links (url, uri, webloc), `archive` (zip, rar, 7z, …), `downloading` for an unfinished download (download, part) and a plain `insert_drive_file` for everything else; a link on disk keeps `link`. The word in front of the name for a screen reader names the same type ("Video: 1.mkv"). The folder tree uses the same icons for its files. One list in `data/file-types.ts` holds the extensions, so a new type is one line.
- The tree and the list are never lower than 18rem; from 60rem the minimum is the height of the window minus 13rem, at most 36rem. Beside the list the tree has the height of the list and scrolls inside, so moving between a short and a long folder does not change the row.
- An entry that a download uses carries a line under its name, a lock and the words "In use by Dr. Stone (season 1) · Running", and its checkbox is disabled and described by that line. A folder is locked when it is, holds or lies in the folder of such a download. A link on disk is shown with a link icon and a size of 0; it is never followed, and deleting it removes only the link.
- Below 40rem a row becomes two lines on one card: checkbox and name, then size, number of files and time; the column headers except the select-all stay for a screen reader. The explicit table roles keep the semantics when the display changes.
- Deleting asks first, in the dialog with the Danger button: the title names the entry or counts them, the text gives the size and the number of files and says that it cannot be undone, and up to ten names are listed. A toast then says what happened, and an entry that could not be deleted says why. The page updates by itself when something changes, so there is no reload button; while the server cannot be reached, a hint says that the list is not updating.
- A folder that disappears while it is shown sends the page one level up and says so in a note.
- The folder tree is a panel of 15rem on the left of the list from 60rem on, and above the list on narrower screens, where it is closed until the toggle opens it (at most 18rem high, scrolling). It shows the whole downloads folder, folders and files, each with a 1.25rem icon, the folders of the path to the shown one opened and the shown one lit by a 16% cyan wash. A row is a `treeitem` in a `tree` with `aria-level`, `aria-expanded` and `aria-selected`, and the tree is one tab stop: the arrow keys move, Right opens or steps into a folder, Left closes it or steps out, Home and End jump, Enter opens the folder (for a file, the folder it is in). Clicking a folder shows it in the list and opens it; the chevron only opens or closes. The tree loads a folder when it is opened, asks only for names (no sizes), and reads the open folders again when the page hears that something changed.

### Progress Bar (signature)
A 10px pill track on Night Highest with a pill fill. The fill is cyan while work is active and carries a slow light sweep (1.6 s, linear) that says "still working" when the number has not moved yet. Done is green, paused amber, failed coral, idle Steel Outline. Percentage and detail text use tabular figures; a compact variant is 6px high.

### Segmented Switch (signature)
Two or three options in a pill with a 1px Steel Outline. The selected option is lit by a 22% cyan pill that slides to it, stretching on the way with a springy curve (360 ms), and its label turns cyan and bold without changing the switch's width.

### Notifications
Stacked from the bottom right, three at most, on Night Highest with a hairline and the Lifted shadow. A notification is one line of up to three: an 18px status icon in the status colour, whose shape (info, check, warning, error) says the state without the colour, the message, and a round close button with an icon and no word. They rise 8px while they fade in (220 ms) and fade out (120 ms). A message keeps its time while it is hovered or holds focus, and the stack is one live region, so every message is announced. A message of more than three lines is cut, its full text is its title.

Only what the person would otherwise miss is announced: a finished download, a password that is asked for, a failure, and the answer to something done on the page itself. Pausing, resuming, creating links and the like are not announced; they stay in the activity log of the card.

### Motion
Two durations (120 ms for what follows the pointer, 220 ms for what the eye follows) and one curve, `cubic-bezier(0.2, 0, 0, 1)`. A view transition fades the page out and drifts it up by 8px (160 ms) while the next fades in from 12px below (280 ms); the navbar stays put. Under `prefers-reduced-motion` the durations collapse to 1 ms, and the progress sweep stops.

## Do's and Don'ts

### Do:
- **Do** take colours from the tokens (`--mat-sys-*`, `--app-*`) and pick the nearest tonal step for a new surface; never introduce a free-standing hex.
- **Do** put Tidewater Ink on Tidewater Cyan, and keep the cyan for what is active or can be acted on.
- **Do** pair every status colour with a word in the same element.
- **Do** use tabular figures for any number that updates while the page is open.
- **Do** keep controls at pill shape, 40px for buttons and 36px for icon buttons in dense rows.
- **Do** draw edges with 1px Steel Hairline and tone, and keep shadows for floating things.
- **Do** keep keyboard focus visible with the 2px cyan ring and keep every icon-only button labelled.
- **Do** honour `prefers-reduced-motion` for anything that moves.
- **Do** give every list that scrolls a tab stop and a name, so it can be read with the keyboard.

### Don't:
- **Don't** add a light theme or any light surface; the system is dark only.
- **Don't** put white text on Tidewater Cyan.
- **Don't** use colour as the only signal of a state.
- **Don't** make a card out of a shadow; a panel is a lighter tone plus a hairline.
- **Don't** add a second typeface or a display size; the page has no hero.
- **Don't** let a hover tint use the brand colour; it would read as "selected".

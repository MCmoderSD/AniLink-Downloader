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
    padding: "0.75rem 0.75rem 0.75rem 1rem"
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
- **Go Green** (#4ade80) on **Go Green Deep** (#0f3d24): done, connected, success.
- **Caution Amber** (#fbbf24) on **Caution Amber Deep** (#453000): paused, reconnecting, needs attention.
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

A single centred column, 64rem at most, with 1.25rem side padding and 2rem above and 4rem below. A sticky navbar 4rem high sits on top; the page scrolls under it, and the app draws its own scroll bar over the right edge instead of the platform's. Pages are stacks of panels with 1.25rem between them. Inside a panel the rhythm is 1.25rem between blocks, 0.5 to 1rem between related items and 0.25rem between rows of a list.

Density is moderate and information-forward: a job card shows its bars and actions at once and folds its episodes and activity log behind a toggle that opens with a height animation. Forms use flexible rows that wrap (`flex: 1 1 18rem` for wide fields, `0 1 11rem` for narrow ones).

Below 40rem the page tightens: panel padding drops from 1.25–1.5rem to 1rem, the navbar hides the brand name and the connection status, part rows and episode previews collapse to fewer columns, and the episode controls wrap under the title. There is no other breakpoint.

## Elevation & Depth

Depth is tonal. A panel is one step lighter than the page, a row inside it one step lighter again, and a 1px Steel Hairline draws each edge. On a near-black ground a drop shadow is close to invisible, so shadows only soften what actually floats.

### Shadow Vocabulary
- **Raised** (`box-shadow: 0 1px 2px rgb(0 0 0 / 0.4)`): panels, together with the hairline.
- **Lifted** (`box-shadow: 0 2px 8px rgb(0 0 0 / 0.45)`): defined in the tokens for small floating pieces; no component uses it yet.
- **Floating** (`box-shadow: 0 8px 24px rgb(0 0 0 / 0.5)`): notifications.

### Named Rules
**The Lift, Not Shadow Rule.** Separate surfaces by tone and a hairline first. A shadow is a finishing touch for things that float above the page, never the thing that makes a card look like a card.

## Shapes

Generously rounded, with a clear ladder: 8px for small blocks inside a row (errors, part rows), 12px for rows, fields and notifications, 18px for panels, 24px for the largest surfaces (the step Material's large corner maps onto), and a full pill for everything you press, select or read as a label (buttons, chips, the switch, the progress bar). Borders are 1px hairlines, and the only thick stroke is a 4px status bar on the left edge of a notification and a 3px one on an activity entry. Focus is a 2px ring in Tidewater Cyan, offset 2px.

## Components

Tactile and confident: solid fills, full pills, strong contrast between rest and active, and states that cannot be misread.

### Buttons
- **Shape:** full pill (999px), 40px high.
- **Primary (filled):** Tidewater Cyan with Tidewater Ink, 0 24px padding, Label Large. One per view where possible (New download).
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

### Navigation
- A sticky bar on Night Base (88% over the page) with a 12px backdrop blur and a hairline underneath. The mark and the name "AniLink" sit on the left; "Downloads" is a text button, "New download" is the filled button, and a live-connection status (green dot "Live", amber "Reconnecting") closes the row. On small screens only the mark remains on the left.

### Progress Bar (signature)
A 10px pill track on Night Highest with a pill fill. The fill is cyan while work is active and carries a slow light sweep (1.6 s, linear) that says "still working" when the number has not moved yet. Done is green, paused amber, failed coral, idle Steel Outline. Percentage and detail text use tabular figures; a compact variant is 6px high.

### Segmented Switch (signature)
Two or three options in a pill with a 1px Steel Outline. The selected option is lit by a 22% cyan pill that slides to it, stretching on the way with a springy curve (360 ms), and its label turns cyan and bold without changing the switch's width.

### Notifications
Stacked from the bottom right, on Night Highest with a hairline, a 4px status bar on the left, a Floating shadow and a "Dismiss" button. They enter by sliding from the right (260 ms) and under reduced motion only fade.

### Motion
Two durations (120 ms for what follows the pointer, 220 ms for what the eye follows) and one curve, `cubic-bezier(0.2, 0, 0, 1)`. A view transition fades the page out and drifts it up by 8px (160 ms) while the next fades in from 12px below (280 ms); the navbar stays put. Under `prefers-reduced-motion` the durations collapse to 1 ms, the progress sweep stops, and notifications only fade.

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

### Don't:
- **Don't** add a light theme or any light surface; the system is dark only.
- **Don't** put white text on Tidewater Cyan.
- **Don't** use colour as the only signal of a state.
- **Don't** make a card out of a shadow; a panel is a lighter tone plus a hairline.
- **Don't** add a second typeface or a display size; the page has no hero.
- **Don't** let a hover tint use the brand colour; it would read as "selected".

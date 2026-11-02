---
name: AniLink-Downloader
description: A calm, black, hairline dashboard that turns pasted hoster links into finished files and shows the true state of every episode while it works.
colors:
  brand-cyan: "#0db7ed"
  brand-ink: "#00131a"
  brand-deep: "#014d66"
  brand-mist: "#cae7f8"
  info-ground: "#04283a"
  page-black: "#000000"
  panel-ash: "#0a0a0a"
  row-graphite: "#111111"
  tag-slate: "#1a1a1a"
  track-slate: "#1f1f1f"
  bright-slate: "#2e2e2e"
  hairline: "#2e2e2e"
  button-outline: "#454545"
  field-outline: "#6b6b6b"
  field-outline-hover: "#8f8f8f"
  text-primary: "#ededed"
  text-secondary: "#a1a1a1"
  success: "#4ade80"
  success-ground: "#0d2b1b"
  warning: "#fbbf24"
  warning-ground: "#33260a"
  danger: "#ff8a88"
  danger-ground: "#4a141b"
typography:
  headline:
    fontFamily: "Geist Variable, ui-sans-serif, system-ui, -apple-system, Segoe UI, sans-serif"
    fontSize: "2rem"
    fontWeight: 600
    lineHeight: "2.5rem"
    letterSpacing: "-0.025em"
  title-large:
    fontFamily: "Geist Variable, ui-sans-serif, system-ui, -apple-system, Segoe UI, sans-serif"
    fontSize: "1.25rem"
    fontWeight: 600
    lineHeight: "1.75rem"
    letterSpacing: "-0.015em"
  title-medium:
    fontFamily: "Geist Variable, ui-sans-serif, system-ui, -apple-system, Segoe UI, sans-serif"
    fontSize: "1rem"
    fontWeight: 600
    lineHeight: "1.5rem"
    letterSpacing: "-0.005em"
  title-small:
    fontFamily: "Geist Variable, ui-sans-serif, system-ui, -apple-system, Segoe UI, sans-serif"
    fontSize: "0.9375rem"
    fontWeight: 500
    lineHeight: "1.5rem"
  body:
    fontFamily: "Geist Variable, ui-sans-serif, system-ui, -apple-system, Segoe UI, sans-serif"
    fontSize: "0.9375rem"
    fontWeight: 400
    lineHeight: "1.5rem"
  body-small:
    fontFamily: "Geist Variable, ui-sans-serif, system-ui, -apple-system, Segoe UI, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 400
    lineHeight: "1.25rem"
  label-large:
    fontFamily: "Geist Variable, ui-sans-serif, system-ui, -apple-system, Segoe UI, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 500
    lineHeight: "1.25rem"
  label-medium:
    fontFamily: "Geist Variable, ui-sans-serif, system-ui, -apple-system, Segoe UI, sans-serif"
    fontSize: "0.8125rem"
    fontWeight: 500
    lineHeight: "1.25rem"
  mono:
    fontFamily: "Geist Mono Variable, ui-monospace, Cascadia Mono, SF Mono, Consolas, monospace"
    fontSize: "0.875rem"
    fontWeight: 400
    lineHeight: "1.25rem"
  icons:
    fontFamily: "'Material Icons', sans-serif"
    fontSize: "1.5rem"
    fontWeight: 400
    lineHeight: 1
rounded:
  xs: "2px"
  sm: "4px"
  md: "6px"
  lg: "8px"
  xl: "12px"
  pill: "999px"
spacing:
  xs: "0.25rem"
  sm: "0.5rem"
  md: "1rem"
  lg: "1.5rem"
  xl: "3rem"
  page-max: "64rem"
  navbar: "4rem"
components:
  button-primary:
    backgroundColor: "{colors.brand-cyan}"
    textColor: "{colors.brand-ink}"
    typography: "{typography.label-large}"
    rounded: "{rounded.md}"
    height: "2.5rem"
    padding: "0 1.25rem"
  button-outlined:
    backgroundColor: "transparent"
    textColor: "{colors.text-primary}"
    typography: "{typography.label-large}"
    rounded: "{rounded.md}"
    height: "2.5rem"
    padding: "0 1.25rem"
  button-text:
    backgroundColor: "transparent"
    textColor: "{colors.text-primary}"
    typography: "{typography.label-large}"
    rounded: "{rounded.md}"
    height: "2.5rem"
    padding: "0 1rem"
  panel:
    backgroundColor: "{colors.panel-ash}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.lg}"
    padding: "1.5rem"
  episode-row:
    backgroundColor: "{colors.row-graphite}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.md}"
    padding: "1rem 1.25rem"
  chip:
    backgroundColor: "{colors.tag-slate}"
    textColor: "{colors.text-secondary}"
    typography: "{typography.label-medium}"
    rounded: "{rounded.sm}"
    padding: "0.125rem 0.5rem"
  chip-success:
    backgroundColor: "{colors.success-ground}"
    textColor: "{colors.success}"
  chip-warning:
    backgroundColor: "{colors.warning-ground}"
    textColor: "{colors.warning}"
  chip-danger:
    backgroundColor: "{colors.danger-ground}"
    textColor: "{colors.danger}"
  chip-info:
    backgroundColor: "{colors.info-ground}"
    textColor: "{colors.brand-cyan}"
  progress-track:
    backgroundColor: "{colors.track-slate}"
    rounded: "{rounded.xs}"
    height: "0.375rem"
  progress-fill:
    backgroundColor: "{colors.brand-cyan}"
    rounded: "{rounded.xs}"
    height: "0.375rem"
  text-field:
    backgroundColor: "transparent"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.md}"
    height: "3.5rem"
  segmented-switch:
    backgroundColor: "transparent"
    textColor: "{colors.text-secondary}"
    typography: "{typography.label-large}"
    rounded: "{rounded.lg}"
    padding: "0.25rem"
  notification:
    backgroundColor: "{colors.tag-slate}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body}"
    rounded: "{rounded.lg}"
    padding: "0.875rem 0.875rem 0.875rem 1rem"
---

# Design System: AniLink-Downloader

## Overview

**Creative North Star: "The Quiet Dashboard"**

AniLink is a black page with hairline panels for work that carries on by itself. A person pastes a season, sets it going and looks away; the page is what they glance at later, to see which downloads run, which wait for a password and which failed. It is built in the category's own grammar, at the craft level of a professional dashboard: square-ish controls, plain grey surfaces, one signal colour, and nothing that asks to be looked at.

Surfaces are neutral greys in small even steps over a pure black page. A panel is told from the page by its hairline, a row from its panel by one tone. Controls have 6px corners, and only the one action of a view is filled in the brand cyan; every other button is outlined or plain. State is a small light in front of a word, and the word always says it too. Motion confirms a change (a bar that fills, a switch that glides, a page that fades to the next) and stops.

The system is dark only; the owner has confirmed there is no light theme. The interface is in English. The mark is unchanged and shared with the favicon.

**Key Characteristics:**
- Dark only, pure black page, neutral grey steps, no tint from the brand hue.
- Depth through tone plus a 1px hairline; shadows only for what floats.
- One signal colour (brand cyan) for the action, the progress fill, focus and the selected option; four status hues with words.
- Corners of 2, 4, 6, 8 and 12px; full pills only for the switch and the scroll bar.
- One family, Geist, in 400, 500 and 600, with Geist Mono for links and paths.
- Short, decelerating motion that collapses to nothing under `prefers-reduced-motion`.

## Colors

A black ground, a ladder of plain greys, one cyan signal and four status colours that never decorate.

### Primary
- **Brand Cyan** (#0db7ed): the signal. The single filled button, the progress fill while work is active, the focus ring, the selected option of the segmented switch, the info tone, the skip link, text selection (at 35%), the browser accent. The theme palette is derived from it; it is tone 70 of the primary palette, and Material's own role (tone 80) is overridden to this value.
- **Brand Ink** (#00131a): text and icons on Brand Cyan. White on the cyan reaches only 2.3:1.
- **Brand Deep** (#014d66) and **Brand Mist** (#cae7f8): the container pair of the primary role, pinned for Material components that read it. No bespoke component uses it directly.
- **Info Ground** (#04283a): the ground of an information chip, with Brand Cyan as its text.

### Neutral
- **Page Black** (#000000): the page, and the navbar (at 88% over the page, blurred).
- **Panel Ash** (#0a0a0a): panels (job cards, form sections) and dialogs.
- **Row Graphite** (#111111): rows inside a panel, such as an episode.
- **Tag Slate** (#1a1a1a): chips at rest, notifications.
- **Track Slate** (#1f1f1f): progress tracks.
- **Hairline** (#2e2e2e): the 1px border of panels, rows, dialogs and dividers. Also Material's brightest surface step.
- **Button Outline** (#454545): the frame of an outlined button.
- **Field Outline** (#6b6b6b): the outline of fields, the segmented switch, and the neutral progress bar; it has to be seen against the panel (3.6:1). Hover brightens it to #8f8f8f.
- **Text Primary** (#ededed): main text.
- **Text Secondary** (#a1a1a1): subtitles, hints, metadata, labels at rest.

### Status
- **Success** (#4ade80) on **Success Ground** (#0d2b1b): done.
- **Warning** (#fbbf24) on **Warning Ground** (#33260a): paused, the server cannot be reached, needs attention.
- **Danger** (#ff8a88) on **Danger Ground** (#4a141b): failed, errors.
- **Brand Cyan** on **Info Ground**: informational chips and the default notice. Information shares the brand cyan, so the system has a single blue.

The theme also generates a lilac tertiary role; no component reads it. Treat it as unassigned.

### Named Rules
**The One Action Rule.** Brand Cyan fills at most one button per view. The others are outlined or plain, so a row of buttons has one thing to press first. Even the navbar's filled button goes quiet when its own page is open.

**The Status Light Rule.** Colour means state, shown as a 6px light in front of a word. Green, amber and red mean done, attention and failure; a colour is never added for decoration.

**The Say It Too Rule.** A state is never told by colour alone. Every chip, notification, activity entry and bar carries the state in a word, so the colour only confirms it.

**The Ink On Cyan Rule.** Anything laid on Brand Cyan uses Brand Ink.

**The Dark Only Rule.** There is no light theme and no light surface. Confirmed by the owner.

**The Neutral Tint Rule.** Hover and pressed tints are white at 6% and 10%, never the brand colour, which would read as "selected".

## Typography

**Display Font:** none; the page has no hero type.
**Body Font:** Geist Variable (with ui-sans-serif, system-ui, Segoe UI), self-hosted through `@fontsource-variable/geist`.
**Label/Mono Font:** Geist Mono Variable (with ui-monospace, Cascadia Mono, Consolas) for pasted links and paths, self-hosted.
**Icon Font:** Material Icons, self-hosted through `@fontsource/material-icons`, 1.5rem by default and 1.25rem in dense rows.

**Character:** One plain family at readable sizes, weighted 400 to read, 500 to label and 600 to title, with tight tracking on the larger sizes. Hierarchy comes from size and weight, never from a second typeface.

### Hierarchy
- **Headline** (600, 2rem, 2.5rem, -0.025em; 1.625rem/2rem under 40rem): the page title.
- **Title Large** (600, 1.25rem, 1.75rem, -0.015em): a job's name and the brand name in the navbar.
- **Title Medium** (600, 1rem, 1.5rem): section titles inside forms.
- **Title Small** (500, 0.9375rem, 1.5rem): an episode's name and small block titles.
- **Body** (400, 0.9375rem, 1.5rem): running text and subtitles; the base of the page.
- **Body Small** (400, 0.875rem, 1.25rem): metadata, hints, part rows, activity log.
- **Label Large** (500, 0.875rem, 1.25rem): buttons, bar labels, percentages.
- **Label Medium** (500, 0.8125rem, 1.25rem): chips and small toggles.
- **Mono** (400, Geist Mono at the surrounding size): links and the download path.

### Named Rules
**The Tabular Rule.** Every number that changes (percent, bytes, speed, time) uses tabular figures, so an updating value never shifts its neighbours.

**The One Family Rule.** Geist carries everything except links and paths. Do not add a display or accent typeface.

## Layout

A single centred column, 64rem at most, with 1.5rem side padding, 3rem above and 5rem below. A sticky navbar 4rem high (plus a 1px hairline) sits on top; the page scrolls under it and the app draws its own scroll bar over the right edge. Pages are stacks of panels; inside a job panel the rhythm is 1.5rem between blocks, 1rem between bars, 0.5rem between buttons and rows. Panels are padded 1.5rem and episode rows 1rem by 1.25rem. A page header holds the title and one counts line, with 2rem below it.

Density is moderate: a job card shows its bars, meta line and actions at once and folds the episodes and activity log behind a toggle that opens by height.

Responsive rules, all present in the build:
- Below 40rem: page padding drops to 1.5rem 1rem 3rem, the title to 1.625rem, the navbar hides the brand name, job and episode padding drops, the job header may wrap, job buttons share the row width (plain ones gain a hairline) and the switch takes a full row, episode heads become two-column grids, part rows lose a column, notifications span the screen width.
- Below 30rem: the navbar drops its button icons and tightens the padding so the mark and two labelled buttons fit 320px.
- Height up to 30rem (a phone held sideways): the navbar is 3rem.
- Coarse pointers: buttons are 44px high, episode icon buttons 44px, notification dismiss 44px, switch labels padded to 44px.
- `prefers-reduced-motion`: motion durations collapse to 1ms, the progress sweep and view transitions stop, notifications only fade.

## Elevation & Depth

Depth is tonal. The page is black, a panel one grey step above it, a row one step above that, and a 1px hairline draws each edge. Panels have no shadow at rest. Only what floats has one.

### Shadow Vocabulary
- **Rest** (`box-shadow: none`): panels.
- **Small float** (`box-shadow: 0 2px 8px rgb(0 0 0 / 0.5)`): defined; no component uses it yet.
- **Float** (`box-shadow: 0 8px 24px rgb(0 0 0 / 0.6)`): notifications.

### Named Rules
**The Hairline Not Shadow Rule.** Separate surfaces by tone and a hairline. A shadow belongs to things that float over the page.

## Shapes

Square-ish and quiet, on a ladder: 2px for the progress bar, 4px for a tag or a block inside a row, 6px for every control and row, 8px for panels and notifications and the switch frame, 12px for what floats (dialogs, menus). Full pills are kept for things round by nature: the toggle switch and the scroll bar. Borders are 1px hairlines. State is never a side stripe; it is a 6px round light in front of the word. Focus is a 2px Brand Cyan ring offset 2px, on every control.

## Components

Precise and restrained: flat fills, hairlines, one filled button.

### Buttons
- **Shape:** 6px corners, 40px high (44px on touch).
- **Primary (filled):** Brand Cyan with Brand Ink, 600 weight, 1.25rem padding. One per view.
- **Outlined:** Text Primary with a Button Outline (#454545) frame, 1.25rem padding.
- **Text:** Text Primary, 1rem padding. The current page in the navbar reads as pressed with a 6% white tint.
- **Hover / Focus:** a neutral white tint (6% hover, 10% pressed); focus is the global cyan ring.
- **Icon buttons** (per-episode): 36px square with a 20px icon, always with an aria-label.

### Chips
- **Style:** 4px corners, Label Medium, 0.125rem by 0.5rem padding, a 1px border of the text colour at 22%. At rest Tag Slate with Text Secondary. With a tone, the status ground behind the status colour, with a 6px light in front of the word.
- **State:** a chip is a read-only label of state, never a button.

### Cards / Containers
- **Corner Style:** 8px for panels, 6px for episode rows, 4px for blocks inside rows.
- **Background:** Panel Ash for panels, Row Graphite for rows.
- **Shadow Strategy:** none at rest; see Elevation & Depth.
- **Border:** 1px Hairline on panels, rows and dialogs.
- **Internal Padding:** 1.5rem for panels (1rem under 40rem), 1rem by 1.25rem for rows.

### Inputs / Fields
- **Style:** Material outlined fields, 56px high, 6px corners, Field Outline stroke, floating label in Text Secondary. Number fields use the app's own stepper. An icon button in a field's suffix gets 0.5rem of room on its right.
- **Focus:** the outline and label turn Brand Cyan.
- **Error / Disabled:** hints are Text Secondary; errors use Danger text on Danger Ground.

### Navigation
A sticky bar on Page Black (88%, 12px backdrop blur) with a hairline underneath. The mark (2rem) and "AniLink" in Title Large on the left; "Downloads" as a text button and "New download" as the filled button on the right. While the server cannot be reached, a full-width warning strip (2.5rem) sits under the bar. On narrow screens only the mark remains on the left.

### Progress Bar (signature)
A 6px track on Track Slate with a 2px-cornered fill (compact variant 4px). The fill is cyan while active and carries a slow light sweep (1.6s, linear) that says "still working"; done is green, paused amber, failed red, idle Field Outline. Percentage and detail use tabular figures.

### Segmented Switch (signature)
Two or three options in an 8px-cornered frame with a Field Outline border. The selected option is lit by a 4px-cornered highlight of Brand Cyan at 22% that glides to it with a long deceleration, the leading edge ahead of the trailing one (300 to 440ms); its label turns cyan and 600 without changing the switch's width.

### Toggle Switch
Material's slide toggle scaled to 0.625, with a handle that glides over 400ms and stretches on the way once flipped by hand; no hover halo, only the focus ring.

### Notifications
Stacked from the bottom right on Tag Slate with a hairline, 8px corners, the Float shadow, a 6px status light before the text and a "Dismiss" button. They slide in from the right (260ms), only fade under reduced motion, and keep their time while hovered or focused.

### Motion
Fast 120ms for what follows the pointer, base 220ms for what the eye follows, glide 400ms for a shape that is watched all the way. Curves: `cubic-bezier(0.2, 0, 0, 1)` and a softer `cubic-bezier(0.22, 1, 0.36, 1)`. A view transition fades the page out and drifts it up 8px (160ms) while the next fades in from 12px below (280ms); the navbar stays. Blocks that open (details) animate by height with a fade.

## Do's and Don'ts

### Do:
- **Do** take colours from the tokens (`--mat-sys-*`, `--app-*`); pick the nearest grey step for a new surface.
- **Do** keep Brand Cyan for the one action, the active progress, focus and the selected option, with Brand Ink on top.
- **Do** put a status light and a word in the same element for every state.
- **Do** use tabular figures for any number that updates.
- **Do** keep controls at 6px corners, 40px high (44px on touch), and rows and panels on the 6px and 8px steps.
- **Do** draw edges with a 1px Hairline and tone; keep shadows for floating things.
- **Do** keep the 2px cyan focus ring visible and every icon-only button labelled.
- **Do** honour `prefers-reduced-motion` for anything that moves.

### Don't:
- **Don't** add a light theme or a light surface.
- **Don't** put white text on Brand Cyan or fill a second button in cyan.
- **Don't** use colour as the only signal of a state, or mark state with a side stripe.
- **Don't** tint greys with the brand hue or let a hover tint use the brand colour.
- **Don't** make pills or large rounded shapes for controls, chips or panels.
- **Don't** add a second typeface or a display size; the page has no hero.
- **Don't** let a card get its edge from a shadow.

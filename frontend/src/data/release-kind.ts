import { type ReleaseKind } from './download';

const MAX_LINKS: number = 1000;

const WEB_LINK: RegExp = /^https?:\/\//i;

const PART_AND_EXTENSION: RegExp = /(?:\.part\d+)?\.(?:rar|zip|7z|r\d{2,3}|z\d{2}|\d{3}|[a-z0-9]{2,4})$/i;

const SEASON_AND_EPISODE: readonly RegExp[] = [
  /S(\d{1,3})E(\d{1,4})/i,
  /(?<![0-9A-Za-z])(\d{1,2})x(\d{2,3})(?![0-9A-Za-z])/i
];

const EPISODE_ONLY: RegExp = /(?<![0-9A-Za-z])(?:E|Ep|Episode|Folge)[ ._-]?(\d{1,4})(?![0-9A-Za-z])/i;

function decode(name: string): string {
  try {
    return decodeURIComponent(name);
  } catch {
    return name;
  }
}

function fileNameOf(link: string): string {
  const path: string = link.split(/[?#]/)[0].replace(/\/+$/, '');

  return decode(path.slice(path.lastIndexOf('/') + 1)).replace(/\.html$/i, '');
}

function episodeKey(name: string): string | null {
  for (const pattern of SEASON_AND_EPISODE) {
    const match: RegExpExecArray | null = pattern.exec(name);
    if (match) return `${Number(match[1])}x${Number(match[2])}`;
  }

  const single: RegExpExecArray | null = EPISODE_ONLY.exec(name);

  return single ? `e${Number(single[1])}` : null;
}

export function suggestKind(lines: readonly string[]): ReleaseKind | null {
  const releases: Set<string> = new Set<string>();
  const episodes: Set<string> = new Set<string>();
  let seen: number = 0;

  for (const line of lines) {
    const link: string = line.trim();
    if (!WEB_LINK.test(link)) continue;
    if (++seen > MAX_LINKS) break;

    const name: string = fileNameOf(link);
    const key: string | null = episodeKey(name);

    releases.add(name.replace(PART_AND_EXTENSION, ''));
    if (key) episodes.add(key);

    if (episodes.size > 1) return 'Season';
  }

  return releases.size === 1 ? 'Movie' : null;
}
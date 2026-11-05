const LINK: RegExp = /https?:\/\/[^\s"'<>,]+/gi;

export interface AppendedLinks {
  text: string;
  added: number;
}

export function extractLinks(text: string): string[] {
  return text.match(LINK) ?? [];
}

const SCHEME_AND_HOST: RegExp = /^https?:\/\/([^/?#]*)/i;

function dedupeKey(link: string): string {
  return link.replace(SCHEME_AND_HOST, (_head: string, host: string): string => host.toLowerCase());
}

export function appendLinks(current: string, found: readonly string[]): AppendedLinks {
  const known: Set<string> = new Set(current.split('\n').map((line: string): string => dedupeKey(line.trim())));
  const fresh: string[] = [];

  for (const link of found) {
    const key: string = dedupeKey(link);
    if (known.has(key)) continue;

    known.add(key);
    fresh.push(link);
  }

  if (fresh.length === 0) return { text: current, added: 0 };

  const kept: string = current.trimEnd();

  return { text: (kept ? `${kept}\n` : '') + fresh.join('\n'), added: fresh.length };
}
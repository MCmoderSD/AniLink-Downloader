const LINK: RegExp = /https?:\/\/[^\s"'<>,]+/gi;

export interface AppendedLinks {
  text: string;
  added: number;
}

export function extractLinks(text: string): string[] {
  return text.match(LINK) ?? [];
}

export function appendLinks(current: string, found: readonly string[]): AppendedLinks {
  const known: Set<string> = new Set(current.split('\n').map((line: string): string => line.trim()));
  const fresh: string[] = [...new Set(found)].filter((link: string): boolean => !known.has(link));

  if (fresh.length === 0) return { text: current, added: 0 };

  const kept: string = current.trimEnd();

  return { text: (kept ? `${kept}\n` : '') + fresh.join('\n'), added: fresh.length };
}
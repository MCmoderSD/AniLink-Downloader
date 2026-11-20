import { Item, ItemStage, Job, Part, Progress, Tone } from './download';
import { formatBytes, formatEta, formatLeft, formatSpeed } from './format';

export interface PhaseBar {
  label: string;
  detail: string;
  fraction: number;
  tone: Tone;
  active: boolean;
}

export interface JobPhases {
  finished: string;
  bars: PhaseBar[];
}

export type CellKind = 'links' | 'queued' | 'downloading' | 'ready' | 'unpacking' | 'done' | 'paused' | 'password' | 'failed' | 'cancelled';

export const CELL_ORDER: readonly CellKind[] = ['downloading', 'queued', 'links', 'unpacking', 'ready', 'done', 'paused', 'password', 'failed', 'cancelled'];

export const CELL_LABELS: Record<CellKind, string> = {
  links: 'waiting for links',
  queued: 'queued for download',
  downloading: 'downloading',
  ready: 'ready to unpack',
  unpacking: 'unpacking',
  done: 'done',
  paused: 'paused',
  password: 'need a password',
  failed: 'failed',
  cancelled: 'canceled'
};

const DOWNLOADED: ReadonlySet<ItemStage> = new Set<ItemStage>(['Downloaded', 'PasswordRequired', 'Extracting', 'Organizing', 'Completed']);
const UNPACKING: ReadonlySet<ItemStage> = new Set<ItemStage>(['Extracting', 'Organizing']);
const UNPACK_PENDING: ReadonlySet<ItemStage> = new Set<ItemStage>(['ResolvingLinks', 'Downloading', 'Downloaded', 'PasswordRequired', 'Paused']);

export function cellKind(item: Item): CellKind {
  switch (item.stage) {
    case 'ResolvingLinks':
      return 'links';

    case 'Downloading':
      return item.parts.some((part: Part): boolean => part.state === 'Downloading') ? 'downloading' : 'queued';

    case 'Downloaded':
      return 'ready';

    case 'Extracting':
    case 'Organizing':
      return 'unpacking';

    case 'Completed':
      return 'done';

    case 'Paused':
      return 'paused';

    case 'PasswordRequired':
      return 'password';

    case 'Failed':
      return 'failed';

    case 'Cancelled':
      return 'cancelled';
  }
}

export function jobPhases(job: Job): JobPhases {
  const linksDone: boolean = job.links.total > 0 && job.links.done >= job.links.total;
  const downloadDone: boolean = linksDone && job.items.length > 0 && job.items.every((item: Item): boolean => DOWNLOADED.has(item.stage));
  const unpacked: boolean = job.items.length > 0 && job.items.every((item: Item): boolean => item.stage === 'Completed');

  const finished: string[] = [];
  const bars: PhaseBar[] = [];

  if (linksDone) finished.push(`${job.links.total} ${job.links.total === 1 ? 'link' : 'links'} created`);
  else bars.push(linksBar(job));

  if (downloadDone) finished.push(`${formatBytes(job.download.total)} downloaded`);
  else bars.push(downloadBar(job));

  if (unpacked) {
    finished.push('unpacked');
  } else {
    const unpack: PhaseBar | null = unpackBar(job);
    if (unpack) bars.push(unpack);
  }

  return { finished: finished.join(' · '), bars };
}

function barTone(job: Job): Tone {
  switch (job.status) {
    case 'Completed':
    case 'Downloaded':
      return 'success';

    case 'Failed':
      return 'danger';

    case 'Cancelled':
      return 'neutral';

    case 'Paused':
      return 'warning';

    default:
      return 'info';
  }
}

function linksBar(job: Job): PhaseBar {
  const progress: Progress = job.links;
  const running: boolean = job.status === 'Active';
  const remaining: number = progress.total - progress.done;
  const text: string = `${progress.done} of ${progress.total} links`;
  const waiting: number = Math.max(job.linksAhead, remaining);

  return {
    label: 'Debrid links',
    detail: running && remaining > 0 && job.linkSecondsEach > 0 ? `${text} · ${formatLeft(Math.ceil(waiting * job.linkSecondsEach))}` : text,
    fraction: progress.fraction,
    tone: barTone(job),
    active: running && progress.fraction < 1
  };
}

function downloadBar(job: Job): PhaseBar {
  const progress: Progress = job.download;
  const running: boolean = job.status === 'Active';
  const parts: string[] = [];

  if (progress.total <= 0) {
    parts.push('waiting for the first links');
  } else {
    parts.push(`${formatBytes(progress.done)} of ${formatBytes(progress.total, progress.estimated)}`);

    if (running && job.speedBytesPerSecond > 0) {
      parts.push(formatSpeed(job.speedBytesPerSecond));

      const eta: string | null = formatEta(progress.total - progress.done, job.speedBytesPerSecond, progress.estimated);
      if (eta) parts.push(eta);
    }
  }

  return { label: 'Download', detail: parts.join(' · '), fraction: progress.fraction, tone: barTone(job), active: running && job.speedBytesPerSecond > 0 };
}

function unpackBar(job: Job): PhaseBar | null {
  const working: Item[] = job.items.filter((item: Item): boolean => UNPACKING.has(item.stage));
  if (working.length === 0) return null;

  const finished: number = job.items.filter((item: Item): boolean => item.stage === 'Completed').length;
  const pending: number = job.items.filter((item: Item): boolean => item.unpack && UNPACK_PENDING.has(item.stage)).length;
  const scope: number = finished + working.length + pending;
  const progress: number = working.reduce((sum: number, item: Item): number => sum + (item.stage === 'Organizing' ? 1 : item.extractPercent / 100), finished);
  const extracting: Item[] = working.filter((item: Item): boolean => item.stage === 'Extracting');

  const parts: string[] = [];

  if (job.items.length > 1) parts.push(`${finished} of ${scope} ${job.kind === 'Movie' ? 'files' : 'episodes'} unpacked`);
  if (extracting.length === 1 && extracting[0].extractSecondsLeft > 0) parts.push(formatLeft(extracting[0].extractSecondsLeft));

  return { label: extracting.length > 0 ? 'Unpacking' : 'Sorting files', detail: parts.join(' · '), fraction: Math.min(progress / scope, 1), tone: 'info', active: true };
}
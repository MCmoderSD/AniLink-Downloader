export type ReleaseKind = 'Season' | 'Movie';

export type JobStatus = 'Active' | 'Paused' | 'PasswordRequired' | 'Failed' | 'Cancelled' | 'Downloaded' | 'Completed';

export type ItemStage =
  | 'ResolvingLinks'
  | 'Downloading'
  | 'Downloaded'
  | 'Extracting'
  | 'PasswordRequired'
  | 'Organizing'
  | 'Completed'
  | 'Failed'
  | 'Cancelled'
  | 'Paused';

export type PartState = 'Pending' | 'Resolving' | 'Resolved' | 'Downloading' | 'Downloaded' | 'Failed';

export type NoticeLevel = 'Info' | 'Success' | 'Warning' | 'Error';

export type Tone = 'neutral' | 'info' | 'success' | 'warning' | 'danger';

export interface Progress {
  done: number;
  total: number;
  fraction: number;
}

export interface Part {
  index: number;
  fileName: string;
  state: PartState;
  bytesDone: number;
  bytesTotal: number;
  attempts: number;
  error: string | null;
  host: string | null;
}

export interface Item {
  number: number;
  label: string;
  stage: ItemStage;
  error: string | null;
  links: Progress;
  download: Progress;
  extractPercent: number;
  files: string[];
  parts: Part[];
  unpack: boolean;
  canChangeUnpack: boolean;
}

export interface Notice {
  at: string;
  level: NoticeLevel;
  text: string;
}

export interface Job {
  id: string;
  name: string;
  kind: ReleaseKind;
  season: number | null;
  format: string | null;
  status: JobStatus;
  createdAt: string;
  directory: string;
  links: Progress;
  download: Progress;
  speedBytesPerSecond: number;
  itemsCompleted: number;
  items: Item[];
  activity: Notice[];
}

export interface AnalyzedPart {
  fileName: string;
  url: string;
  partNumber: number;
}

export interface AnalyzedEpisode {
  number: number;
  parts: AnalyzedPart[];
}

export interface Analysis {
  format: string | null;
  formatDetected: boolean;
  season: number | null;
  linkCount: number;
  episodes: AnalyzedEpisode[];
  unmatched: string[];
}

export interface AnalyzeRequest {
  kind: ReleaseKind;
  links: string;
  format: string | null;
}

export interface CreateDownloadRequest extends AnalyzeRequest {
  name: string;
  season: number | null;
  password: string | null;
  unpack: boolean;
}

export interface Limits {
  nameBytes: number;
  formatLength: number;
  passwordLength: number;
  linksLength: number;
}

export interface AppConfig {
  maxConcurrency: number;
  defaultPasswords: number;
  downloadsRoot: string;
  limits: Limits;
}

export interface JobMessage {
  type: 'job';
  job: Job;
}

export interface NoticeMessage {
  type: 'notice';
  jobId: string;
  job: string;
  at: string;
  level: NoticeLevel;
  text: string;
}

export interface RemovedMessage {
  type: 'removed';
  jobId: string;
}

export type StreamMessage = JobMessage | NoticeMessage | RemovedMessage;

export const STATUS_LABELS: Record<JobStatus, string> = {
  Active: 'Running',
  Paused: 'Paused',
  PasswordRequired: 'Password needed',
  Failed: 'Failed',
  Cancelled: 'Canceled',
  Downloaded: 'Downloaded',
  Completed: 'Done'
};

export const STATUS_TONES: Record<JobStatus, Tone> = {
  Active: 'info',
  Paused: 'warning',
  PasswordRequired: 'warning',
  Failed: 'danger',
  Cancelled: 'neutral',
  Downloaded: 'success',
  Completed: 'success'
};

export const STAGE_LABELS: Record<ItemStage, string> = {
  ResolvingLinks: 'Creating links',
  Downloading: 'Downloading',
  Downloaded: 'Downloaded',
  Extracting: 'Unpacking',
  PasswordRequired: 'Password needed',
  Organizing: 'Sorting files',
  Completed: 'Done',
  Failed: 'Failed',
  Cancelled: 'Canceled',
  Paused: 'Paused'
};

export const STAGE_TONES: Record<ItemStage, Tone> = {
  ResolvingLinks: 'info',
  Downloading: 'info',
  Downloaded: 'success',
  Extracting: 'info',
  PasswordRequired: 'warning',
  Organizing: 'info',
  Completed: 'success',
  Failed: 'danger',
  Cancelled: 'neutral',
  Paused: 'warning'
};

export const PART_LABELS: Record<PartState, string> = {
  Pending: 'Waiting for link',
  Resolving: 'Creating link',
  Resolved: 'Link ready',
  Downloading: 'Downloading',
  Downloaded: 'Downloaded',
  Failed: 'Failed'
};

export const PART_TONES: Record<PartState, Tone> = {
  Pending: 'neutral',
  Resolving: 'info',
  Resolved: 'neutral',
  Downloading: 'info',
  Downloaded: 'success',
  Failed: 'danger'
};

export const NOTICE_TONES: Record<NoticeLevel, Tone> = {
  Info: 'neutral',
  Success: 'success',
  Warning: 'warning',
  Error: 'danger'
};

export function jobSubtitle(job: Job): string {
  if (job.kind === 'Movie') return 'Movie or single episode';

  const season: string = job.season === null ? '' : `Season ${job.season} · `;
  const count: number = job.items.length;

  return `${season}${count} ${count === 1 ? 'episode' : 'episodes'}`;
}
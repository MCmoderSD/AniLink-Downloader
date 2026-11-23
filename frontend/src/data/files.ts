import { JobStatus, STATUS_LABELS } from './download';
import { NotificationKind } from './notification';

export const ROOT_NAME: string = '/';

export type EntryKind = 'Folder' | 'File' | 'Link';

export interface FileLock {
  jobId: string;
  jobName: string;
  season: number | null;
  status: JobStatus;
}

export interface FileEntry {
  name: string;
  path: string;
  kind: EntryKind;
  size: number;
  files: number | null;
  modifiedAt: string;
  lockedBy: FileLock[];
}

export interface FolderListing {
  path: string;
  entries: FileEntry[];
}

export type FileOutcome = 'Done' | 'Locked' | 'Missing' | 'Invalid' | 'Failed';

export interface FileResult {
  path: string;
  outcome: FileOutcome;
  reason: string | null;
  lockedBy: FileLock[];
}

export interface FileBatch {
  results: FileResult[];
}

export interface TreeNode {
  name: string;
  path: string;
  kind: EntryKind;
  hasChildren: boolean;
}

export interface FolderTree {
  path: string;
  nodes: TreeNode[];
}

export interface TreeRow {
  node: TreeNode;
  depth: number;
  open: boolean;
  loading: boolean;
}

export type FileStreamMessage = { type: 'changed'; paths: string[] } | { type: 'reset' };

export interface FileChange {
  id: number;
  paths: readonly string[] | null;
}

export type FilesPhase = 'loading' | 'ready' | 'missing' | 'invalid' | 'unreachable';

export interface Crumb {
  name: string;
  path: string;
}

export interface Summary {
  kind: NotificationKind;
  text: string;
}

const COLLATOR: Intl.Collator = new Intl.Collator(undefined, { numeric: true, sensitivity: 'base' });

export function affects(current: string, changed: string): boolean {
  return changed === '' || current === '' || current === changed || changed.startsWith(`${current}/`) || current.startsWith(`${changed}/`);
}

export function parentPath(path: string): string {
  const cut: number = path.lastIndexOf('/');

  return cut < 0 ? '' : path.slice(0, cut);
}

export function lastName(path: string): string {
  return path.slice(path.lastIndexOf('/') + 1);
}

export function breadcrumbs(path: string): Crumb[] {
  if (path === '') return [];

  const names: string[] = path.split('/');

  return names.map((name: string, index: number): Crumb => ({ name, path: names.slice(0, index + 1).join('/') }));
}

export function compareEntries<T extends { name: string; kind: EntryKind }>(a: T, b: T): number {
  return Number(a.kind !== 'Folder') - Number(b.kind !== 'Folder') || COLLATOR.compare(a.name, b.name);
}

export function isLocked(entry: FileEntry): boolean {
  return entry.lockedBy.length > 0;
}

export function lockText(locks: readonly FileLock[]): string {
  const names: string = locks.map((lock: FileLock): string => lock.season === null ? lock.jobName : `${lock.jobName} (season ${lock.season})`).join(', ');

  return locks.length === 1 ? `In use by ${names} · ${STATUS_LABELS[locks[0].status]}` : `In use by ${names}`;
}

export function fileCount(entry: FileEntry): number {
  return entry.kind === 'Folder' ? entry.files ?? 0 : entry.kind === 'File' ? 1 : 0;
}

export function summarizeDeletion(results: readonly FileResult[]): Summary {
  const gone: number = results.filter((result: FileResult): boolean => result.outcome === 'Done' || result.outcome === 'Missing').length;
  const problems: FileResult[] = results.filter((result: FileResult): boolean => result.outcome !== 'Done' && result.outcome !== 'Missing');

  if (problems.length === 0) {
    return { kind: 'success', text: results.length === 1 ? `Deleted ${lastName(results[0].path)}.` : `Deleted ${results.length} items.` };
  }

  const first: FileResult = problems[0];
  const more: string = problems.length > 1 ? ` ${problems.length - 1} more could not be deleted.` : '';
  const reason: string = `${lastName(first.path)}: ${first.reason ?? 'It could not be deleted.'}${more}`;

  return gone > 0
    ? { kind: 'warning', text: `Deleted ${gone} of ${results.length} items. ${reason}` }
    : { kind: 'failure', text: reason };
}
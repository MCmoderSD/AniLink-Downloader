import { EntryKind } from './files';

export interface FileType {
  icon: string;
  label: string;
}

interface FileGroup {
  type: FileType;
  extensions: string;
}

const FOLDER: FileType = { icon: 'folder', label: 'Folder' };
const LINK: FileType = { icon: 'link', label: 'Link' };
const FILE: FileType = { icon: 'insert_drive_file', label: 'File' };

const GROUPS: readonly FileGroup[] = [
  { type: { icon: 'video_file', label: 'Video' }, extensions: 'mkv mp4 m4v avi mov wmv flv webm mpg mpeg m2ts mts ts ogv 3gp' },
  { type: { icon: 'audio_file', label: 'Audio' }, extensions: 'mp3 flac aac m4a ogg opus wav wma mka' },
  { type: { icon: 'image', label: 'Image' }, extensions: 'jpg jpeg png gif webp avif bmp svg ico tif tiff' },
  { type: { icon: 'subtitles', label: 'Subtitles' }, extensions: 'srt ass ssa vtt sub idx sup' },
  { type: { icon: 'description', label: 'Text file' }, extensions: 'txt log nfo md rtf md5 sfv sha1 sha256' },
  { type: { icon: 'data_object', label: 'Data file' }, extensions: 'json xml yaml yml csv toml ini cfg conf' },
  { type: { icon: 'picture_as_pdf', label: 'PDF document' }, extensions: 'pdf' },
  { type: { icon: 'language', label: 'Web link' }, extensions: 'url uri webloc' },
  { type: { icon: 'archive', label: 'Archive' }, extensions: 'zip rar 7z tar gz tgz bz2 xz zst iso' },
  { type: { icon: 'downloading', label: 'Unfinished download' }, extensions: 'download part crdownload tmp' }
];

const BY_EXTENSION: ReadonlyMap<string, FileType> = new Map(
  GROUPS.flatMap((group: FileGroup): [string, FileType][] => group.extensions.split(' ').map((extension: string): [string, FileType] => [extension, group.type]))
);

function extensionOf(name: string): string {
  const dot: number = name.lastIndexOf('.');

  return dot > 0 ? name.slice(dot + 1).toLowerCase() : '';
}

export function fileType(kind: EntryKind, name: string): FileType {
  if (kind === 'Folder') return FOLDER;
  if (kind === 'Link') return LINK;

  return BY_EXTENSION.get(extensionOf(name)) ?? FILE;
}
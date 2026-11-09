interface SaveFileOptions {
  suggestedName: string;
  types: { description: string; accept: Record<string, string[]> }[];
}

interface SaveFileHandle {
  name: string;
  createWritable(): Promise<{ write(data: string): Promise<void>; close(): Promise<void> }>;
}

type SavePickerWindow = Window & { showSaveFilePicker?: (options: SaveFileOptions) => Promise<SaveFileHandle> };

export async function saveTextFile(document: Document, fileName: string, text: string): Promise<string | null> {
  const picker: SavePickerWindow['showSaveFilePicker'] = (document.defaultView as SavePickerWindow | null)?.showSaveFilePicker;

  if (picker) {
    try {
      const handle: SaveFileHandle = await picker.call(document.defaultView, {
        suggestedName: fileName,
        types: [{ description: 'Text file', accept: { 'text/plain': ['.txt'] } }]
      });
      const writable: Awaited<ReturnType<SaveFileHandle['createWritable']>> = await handle.createWritable();

      await writable.write(text);
      await writable.close();

      return handle.name;
    } catch (error: unknown) {
      if (error instanceof DOMException && error.name === 'AbortError') return null;
    }
  }

  const url: string = URL.createObjectURL(new Blob([text], { type: 'text/plain;charset=utf-8' }));
  const anchor: HTMLAnchorElement = document.createElement('a');

  anchor.href = url;
  anchor.download = fileName;
  anchor.click();

  setTimeout((): void => URL.revokeObjectURL(url));

  return null;
}
export async function copyText(document: Document, text: string): Promise<boolean> {
  const clipboard: Clipboard | undefined = document.defaultView?.navigator.clipboard;

  if (clipboard) {
    try {
      await clipboard.writeText(text);
      return true;
    } catch {
      // Refused, so the selection below is tried.
    }
  }

  // Over plain http on a local network the browser offers no clipboard API; selecting a hidden field still works there.
  const active: HTMLElement | null = document.activeElement as HTMLElement | null;
  const field: HTMLTextAreaElement = document.createElement('textarea');

  field.value = text;
  field.setAttribute('readonly', '');
  field.style.position = 'fixed';
  field.style.opacity = '0';

  document.body.appendChild(field);
  field.select();

  try {
    return document.execCommand('copy');
  } finally {
    field.remove();
    active?.focus();
  }
}
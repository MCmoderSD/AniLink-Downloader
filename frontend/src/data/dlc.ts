export interface DlcHeader {
  generatorApp?: string;
  generatorVersion?: string;
  generatorUrl?: string;
  tributeName?: string;
  dlcXmlVersion?: string;
}

export interface DlcFile {
  url?: string;
  fileName?: string;
  size?: number;
}

export interface DlcPackage {
  name?: string;
  passwords: string[];
  comment?: string;
  category?: string;
  files: DlcFile[];
}

export interface DlcContainer {
  header: DlcHeader;
  packages: DlcPackage[];
}

export interface DlcDetail {
  label: string;
  value: string;
}

export function isDlcFile(file: File): boolean {
  return file.name.toLowerCase().endsWith('.dlc');
}

export function filesOf(container: DlcContainer): DlcFile[] {
  return container.packages.flatMap((pack: DlcPackage): DlcFile[] => pack.files);
}

export function packageLinks(pack: DlcPackage): string[] {
  return pack.files
    .map((file: DlcFile): string | undefined => file.url)
    .filter((url: string | undefined): url is string => !!url);
}

export function linksOf(container: DlcContainer): string[] {
  return container.packages.flatMap(packageLinks);
}

export function sizeOf(container: DlcContainer): number {
  return filesOf(container).reduce((total: number, file: DlcFile): number => total + (file.size ?? 0), 0);
}

export function downloadsText(container: DlcContainer): string {
  const links: string[] = linksOf(container);

  return links.length > 0 ? `${links.join('\n')}\n` : '';
}

function present(details: DlcDetail[]): DlcDetail[] {
  return details.filter((detail: DlcDetail): boolean => detail.value !== '');
}

export function headerDetails(header: DlcHeader): DlcDetail[] {
  return present([
    { label: 'Generator app', value: header.generatorApp ?? '' },
    { label: 'Generator version', value: header.generatorVersion ?? '' },
    { label: 'Generator address', value: header.generatorUrl ?? '' },
    { label: 'Tribute name', value: header.tributeName ?? '' },
    { label: 'DLC XML version', value: header.dlcXmlVersion ?? '' }
  ]);
}

export function packageDetails(pack: DlcPackage): DlcDetail[] {
  return present([
    { label: 'Category', value: pack.category ?? '' },
    { label: 'Comment', value: pack.comment ?? '' }
  ]);
}
const UNITS: readonly string[] = ['B', 'KB', 'MB', 'GB', 'TB'];

export function formatBytes(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes <= 0) return '0 B';

  const exponent: number = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), UNITS.length - 1);
  const value: number = bytes / 1024 ** exponent;

  return `${value >= 100 || exponent === 0 ? value.toFixed(0) : value.toFixed(1)} ${UNITS[exponent]}`;
}

export function formatSpeed(bytesPerSecond: number): string {
  return `${formatBytes(bytesPerSecond)}/s`;
}

export function formatPercent(fraction: number): string {
  return `${Math.round(Math.min(Math.max(fraction, 0), 1) * 100)}%`;
}

export function formatEta(remainingBytes: number, bytesPerSecond: number): string | null {
  if (remainingBytes <= 0 || bytesPerSecond < 1) return null;

  const seconds: number = Math.ceil(remainingBytes / bytesPerSecond);
  if (seconds < 60) return `${seconds}s left`;

  const minutes: number = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ${seconds % 60}s left`;

  const hours: number = Math.floor(minutes / 60);
  return `${hours}h ${minutes % 60}m left`;
}
import { HttpErrorResponse } from '@angular/common/http';

// A failed call as text for the user. The backend answers with a plain string for its own refusals, and with a
// problem-details object when the request did not pass validation, so both shapes are read.
export function errorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) return fallback;

  if (error.status === 0) return 'The server cannot be reached.';

  const body: unknown = error.error;

  if (typeof body === 'string') return body.trim() || fallback;

  const problem: { message?: unknown; title?: unknown; errors?: unknown } | null = body as { message?: unknown; title?: unknown; errors?: unknown } | null;

  if (problem?.errors && typeof problem.errors === 'object') {
    const first: unknown = Object.values(problem.errors as Record<string, unknown>).flat()[0];
    if (typeof first === 'string' && first.trim()) return first.trim();
  }

  const message: unknown = problem?.message ?? problem?.title;

  return typeof message === 'string' && message.trim() ? message.trim() : fallback;
}

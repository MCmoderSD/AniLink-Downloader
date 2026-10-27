import { Service } from '@angular/core';
import { Analysis, AnalyzeRequest, CreateDownloadRequest, Job } from '../data/download';
import { ApiService } from './api.service';

@Service()
export class DownloadService extends ApiService {

  constructor() {
    super('downloads');
  }

  analyze(request: AnalyzeRequest): Promise<Analysis> {
    return this.post<Analysis>('analyze', request);
  }

  create(request: CreateDownloadRequest): Promise<Job> {
    return this.post<Job>('', request);
  }

  list(): Promise<Job[]> {
    return this.get<Job[]>();
  }

  cancel(id: string): Promise<Job> {
    return this.post<Job>(`${id}/cancel`);
  }

  pause(id: string): Promise<Job> {
    return this.post<Job>(`${id}/pause`);
  }

  resume(id: string): Promise<Job> {
    return this.post<Job>(`${id}/resume`);
  }

  retry(id: string): Promise<Job> {
    return this.post<Job>(`${id}/retry`);
  }

  submitPassword(id: string, password: string): Promise<Job> {
    return this.post<Job>(`${id}/password`, { password });
  }

  remove(id: string): Promise<void> {
    return this.delete(id);
  }
}

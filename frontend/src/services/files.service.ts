import { HttpParams } from '@angular/common/http';
import { Service } from '@angular/core';
import { FileBatch, FileResult, FolderListing, FolderTree } from '../data/files';
import { ApiService } from './api.service';

// The most paths one request may carry (FileLimits.MaxBatch on the server); a larger selection goes out in several requests.
const DELETE_BATCH: number = 500;

@Service()
export class FilesService extends ApiService {

  constructor() {
    super('files');
  }

  list(path: string): Promise<FolderListing> {
    return this.get<FolderListing>('', new HttpParams().set('path', path));
  }

  tree(path: string): Promise<FolderTree> {
    return this.get<FolderTree>('tree', new HttpParams().set('path', path));
  }

  async remove(paths: readonly string[]): Promise<FileBatch> {
    const results: FileResult[] = [];

    for (let start: number = 0; start < paths.length; start += DELETE_BATCH) {
      const batch: FileBatch = await this.post<FileBatch>('delete', { paths: paths.slice(start, start + DELETE_BATCH) });

      results.push(...batch.results);
    }

    return { results };
  }
}
import { HttpParams } from '@angular/common/http';
import { Service } from '@angular/core';
import { FileBatch, FolderListing, FolderTree } from '../data/files';
import { ApiService } from './api.service';

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

  remove(paths: readonly string[]): Promise<FileBatch> {
    return this.post<FileBatch>('delete', { paths });
  }
}
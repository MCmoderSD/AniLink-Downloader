import { Service } from '@angular/core';
import { DlcContainer } from '../data/dlc';
import { ApiService } from './api.service';

@Service()
export class DlcService extends ApiService {

  constructor() {
    super('dlc');
  }

  decode(file: File): Promise<DlcContainer> {
    const body: FormData = new FormData();
    body.append('file', file, file.name);

    return this.post<DlcContainer>('decode', body);
  }
}
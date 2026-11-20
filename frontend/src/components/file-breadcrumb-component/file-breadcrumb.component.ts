import { Component, computed, input, InputSignal, Signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { Crumb, breadcrumbs } from '../../data/files';

@Component({
  selector: 'app-file-breadcrumb',
  templateUrl: './file-breadcrumb.component.html',
  styleUrls: ['./file-breadcrumb.component.scss'],
  imports: [MatIconModule, RouterLink]
})
export class FileBreadcrumbComponent {

  readonly path: InputSignal<string> = input.required<string>();
  readonly rootName: InputSignal<string> = input.required<string>();

  protected readonly crumbs: Signal<Crumb[]> = computed((): Crumb[] => breadcrumbs(this.path()));
}
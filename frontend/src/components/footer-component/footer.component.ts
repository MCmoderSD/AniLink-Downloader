import { Component } from '@angular/core';
import { version } from '../../../package.json';

interface FooterLink {
  label: string;
  href: string;
  icon: string;
}

const REPOSITORY: string = 'https://github.com/MCmoderSD/AniLink-Downloader';

@Component({
  selector: 'app-footer',
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.scss']
})
export class FooterComponent {

  protected readonly version: string = version;

  protected readonly release: string = `${REPOSITORY}/releases/tag/${version}`;

  protected readonly links: readonly FooterLink[] = [
    { label: 'GitHub', href: REPOSITORY, icon: 'url(icons/github.svg)' },
    { label: 'Docker Hub', href: 'https://hub.docker.com/r/mcmodersd/anilink-downloader', icon: 'url(icons/docker.svg)' },
    { label: 'Wiki', href: `${REPOSITORY}/wiki`, icon: 'url(icons/wiki.svg)' }
  ];
}
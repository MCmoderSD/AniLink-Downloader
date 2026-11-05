import { Component } from '@angular/core';
import { version } from '../../../package.json';

interface FooterLink {
  label: string;
  href: string;
  icon: string;
}

@Component({
  selector: 'app-footer',
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.scss']
})
export class FooterComponent {

  protected readonly version: string = version;

  protected readonly release: string = `https://github.com/MCmoderSD/AniLink-Downloader/releases/tag/${version}`;

  protected readonly links: readonly FooterLink[] = [
    { label: 'GitHub', href: 'https://github.com/MCmoderSD/AniLink-Downloader', icon: 'url(icons/github.svg)' },
    { label: 'Docker Hub', href: 'https://hub.docker.com/r/mcmodersd/anilink-downloader', icon: 'url(icons/docker.svg)' },
    { label: 'Wiki', href: 'https://github.com/MCmoderSD/AniLink-Downloader/wiki', icon: 'url(icons/wiki.svg)' }
  ];
}
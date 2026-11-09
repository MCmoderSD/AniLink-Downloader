import { suggestKind } from './release-kind';

function links(names: readonly string[]): string[] {
  return names.map((name: string, index: number): string => `https://hoster.example/file/${String(index).padStart(32, 'a')}/${name}.html`);
}

function parts(base: string, count: number, extension: string = '.rar'): string[] {
  return Array.from({ length: count }, (_: unknown, index: number): string => `${base}.part${index + 1}${extension}`);
}

describe('suggestKind', (): void => {

  describe('Movie', (): void => {
    it('for the parts of one archive', (): void => {
      expect(suggestKind(links(parts('Some.Movie.2020.German.1080p', 4)))).toBe('Movie');
    });

    it('for a single episode, which counts as one release', (): void => {
      expect(suggestKind(links(parts('Show.S01E05.German.1080p.x264-GRP', 4)))).toBe('Movie');
    });

    it('for one file', (): void => {
      expect(suggestKind(links(['Movie.2013.German.2160p.mkv']))).toBe('Movie');
    });

    it('for the volumes of a 7z or an old-style rar', (): void => {
      expect(suggestKind(links(['Movie.2013.7z.001', 'Movie.2013.7z.002', 'Movie.2013.7z.003']))).toBe('Movie');
      expect(suggestKind(links(['Movie.2013.rar', 'Movie.2013.r00', 'Movie.2013.r01']))).toBe('Movie');
    });

    it('when a codec in the name looks like an episode', (): void => {
      expect(suggestKind(links(parts('Movie.x264', 2)))).toBe('Movie');
    });

    it('ignores a title line above the links', (): void => {
      expect(suggestKind(['Dr. Stone Special', ...links(parts('Dr.Stone.S00E02.German', 4))])).toBe('Movie');
    });
  });

  describe('Season', (): void => {
    it('for two episodes', (): void => {
      expect(suggestKind(links([...parts('Show.S01E05.German', 2), ...parts('Show.S01E06.German', 2)]))).toBe('Season');
    });

    it('for the same episode number in two seasons', (): void => {
      expect(suggestKind(links(['Show.S01E05.mkv', 'Show.S02E05.mkv']))).toBe('Season');
    });

    it('for the markers 1x05, E05, Episode 5 and Folge 5', (): void => {
      expect(suggestKind(links(['Show.1x05.mkv', 'Show.1x06.mkv']))).toBe('Season');
      expect(suggestKind(links(['Show.E05.mkv', 'Show.E06.mkv']))).toBe('Season');
      expect(suggestKind(links(['Show Episode 05.mkv', 'Show Episode 06.mkv']))).toBe('Season');
      expect(suggestKind(links(['Show.Folge.01.mkv', 'Show.Folge.02.mkv']))).toBe('Season');
    });

    it('for links with a query, a fragment and a stray percent sign', (): void => {
      expect(suggestKind([
        'https://host.example/d/abc/Show%20S01E01.part1.rar?x=1',
        'https://host.example/d/def/Show%20S01E02.part1.rar#top',
        'https://host.example/d/bad%/Show%.rar'
      ])).toBe('Season');
    });

    it('for a very long list, deciding after the first episodes', (): void => {
      const names: string[] = Array.from({ length: 5000 }, (_: unknown, index: number): string => `Show.S01E${String(index + 1).padStart(4, '0')}.part1.rar`);

      expect(suggestKind(links(names))).toBe('Season');
    });
  });

  describe('no suggestion', (): void => {
    it('for names without an episode marker, as with anime', (): void => {
      expect(suggestKind(links(['[Group] Show - 01 [1080p].mkv', '[Group] Show - 02 [1080p].mkv', '[Group] Show - 03 [1080p].mkv']))).toBeNull();
    });

    it('for two different films', (): void => {
      expect(suggestKind(links([...parts('Movie.One.2020.German', 3), ...parts('Movie.Two.2021.German', 3)]))).toBeNull();
    });

    it('for two releases that differ only in a year at the end', (): void => {
      expect(suggestKind(links(['Movie.2024', 'Movie.2025']))).toBeNull();
    });

    it('when a resolution looks like an episode', (): void => {
      expect(suggestKind(links(['Movie.1920x1080.German.mkv', 'Other.1280x720.German.mkv']))).toBeNull();
    });

    it('for no links and for text without links', (): void => {
      expect(suggestKind([])).toBeNull();
      expect(suggestKind(['Dr. Stone', 'some title'])).toBeNull();
    });
  });
});
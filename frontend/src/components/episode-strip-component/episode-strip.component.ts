import { Component, computed, input, InputSignal, Signal } from '@angular/core';
import { Item } from '../../data/download';
import { CELL_LABELS, CELL_ORDER, CellKind, cellKind } from '../../data/job-phases';

interface Cell {
  number: number;
  kind: CellKind;
  title: string;
}

interface Count {
  count: number;
  text: string;
}

@Component({
  selector: 'app-episode-strip',
  templateUrl: './episode-strip.component.html',
  styleUrls: ['./episode-strip.component.scss']
})
export class EpisodeStripComponent {

  readonly items: InputSignal<Item[]> = input.required<Item[]>();

  protected readonly cells: Signal<Cell[]> = computed((): Cell[] =>
    this.items().map((item: Item): Cell => {
      const kind: CellKind = cellKind(item);
      return { number: item.number, kind, title: `${item.label}: ${CELL_LABELS[kind]}` };
    }));

  protected readonly legend: Signal<string> = computed((): string => {
    const cells: Cell[] = this.cells();

    return CELL_ORDER
      .map((kind: CellKind): Count => ({ count: cells.filter((cell: Cell): boolean => cell.kind === kind).length, text: CELL_LABELS[kind] }))
      .filter((entry: Count): boolean => entry.count > 0)
      .map((entry: Count): string => `${entry.count} ${entry.text}`)
      .join(' · ');
  });
}
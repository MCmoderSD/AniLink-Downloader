import { Component, inject, signal, Signal, WritableSignal, computed } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { firstValueFrom } from 'rxjs';
import { openDialog } from '../../services/dialog';
import { ScrollBarComponent } from '../scroll-bar-component/scroll-bar.component';

export interface PasswordPromptDialogData {
  jobName: string;
  waiting: string[];
}

@Component({
  selector: 'app-password-prompt-dialog',
  templateUrl: './password-prompt-dialog.component.html',
  styleUrls: ['./password-prompt-dialog.component.scss'],
  imports: [MatButtonModule, MatDialogModule, MatFormFieldModule, MatIconModule, MatInputModule, ScrollBarComponent]
})
export class PasswordPromptDialogComponent {

  private readonly dialogRef: MatDialogRef<PasswordPromptDialogComponent, string> =
    inject<MatDialogRef<PasswordPromptDialogComponent, string>>(MatDialogRef);

  private readonly entered: WritableSignal<string> = signal('');
  private readonly shown: WritableSignal<boolean> = signal(false);

  protected readonly data: PasswordPromptDialogData = inject<PasswordPromptDialogData>(MAT_DIALOG_DATA);

  protected readonly visible: Signal<boolean> = this.shown.asReadonly();
  protected readonly valid: Signal<boolean> = computed((): boolean => this.entered().length > 0);

  protected readonly summary: string = this.data.waiting.length > 3
    ? `${this.data.waiting.slice(0, 3).join(', ')} and ${this.data.waiting.length - 3} more`
    : this.data.waiting.join(', ');

  static async ask(dialog: MatDialog, data: PasswordPromptDialogData): Promise<string | undefined> {
    const ref: MatDialogRef<PasswordPromptDialogComponent, string> = openDialog<PasswordPromptDialogComponent, PasswordPromptDialogData, string>(
      dialog, PasswordPromptDialogComponent, data,
      { width: '28rem', minWidth: 'min(20rem, 92vw)' });

    return await firstValueFrom(ref.afterClosed());
  }

  protected type(event: Event): void {
    this.entered.set((event.target as HTMLInputElement).value);
  }

  protected toggleVisible(): void {
    this.shown.update((value: boolean): boolean => !value);
  }

  protected submit(): void {
    if (this.valid()) this.dialogRef.close(this.entered());
  }

  protected cancel(): void {
    this.dialogRef.close();
  }
}
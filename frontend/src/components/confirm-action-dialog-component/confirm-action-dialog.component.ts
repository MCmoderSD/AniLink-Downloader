import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';
import { openDialog } from '../../services/dialog';
import { ScrollBarComponent } from '../scroll-bar-component/scroll-bar.component';

export interface ConfirmActionDialogData {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
}

@Component({
  selector: 'app-confirm-action-dialog',
  templateUrl: './confirm-action-dialog.component.html',
  styleUrls: ['./confirm-action-dialog.component.scss'],
  imports: [MatButtonModule, MatDialogModule, ScrollBarComponent]
})
export class ConfirmActionDialogComponent {

  private readonly dialogRef: MatDialogRef<ConfirmActionDialogComponent, boolean> =
    inject<MatDialogRef<ConfirmActionDialogComponent, boolean>>(MatDialogRef);

  protected readonly data: ConfirmActionDialogData = inject<ConfirmActionDialogData>(MAT_DIALOG_DATA);

  static async confirm(dialog: MatDialog, data: ConfirmActionDialogData): Promise<boolean> {
    const ref: MatDialogRef<ConfirmActionDialogComponent, boolean> = openDialog<ConfirmActionDialogComponent, ConfirmActionDialogData, boolean>(
      dialog, ConfirmActionDialogComponent, data,
      { width: '28rem', minWidth: 'min(20rem, 92vw)', autoFocus: 'dialog' }
    );

    return await firstValueFrom(ref.afterClosed()) === true;
  }

  protected confirm(): void {
    this.dialogRef.close(true);
  }

  protected cancel(): void {
    this.dialogRef.close(false);
  }
}
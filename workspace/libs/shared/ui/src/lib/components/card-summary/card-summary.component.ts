import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
} from '@angular/core';

import { AlertType } from '../alert/alert.component';

@Component({
  selector: 'ui-card-summary',
  templateUrl: './card-summary.component.html',
  styleUrls: ['./card-summary.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardSummaryComponent {
  @Input() public icon?: string;
  @Input() public heading!: string;
  @Input() public order?: number;
  @Input() public statusType?: AlertType;
  @Input() public child?: boolean;
  @Input() public status?: string;
  @Input() public actionLabel?: string;
  @Input() public actionDisabled?: boolean;
  @Input() public panelHidden?: boolean;
  @Input() public launchURL?: string;
  @Input() public launchString?: string;
  @Input() public launchDisabled?: boolean;
  @Input() public launchHidden?: boolean;

  @Output() public action: EventEmitter<void>;

  public constructor() {
    this.action = new EventEmitter<void>();
  }

  public hasActions(): boolean {
    return (this.actionLabel && this.actionLabel?.length > 0) || false;
  }

  public hasLaunchAction(): boolean {
    return (this.launchURL && this.launchURL?.length > 0) || false;

  }

  public getStatusTypeClass(): string {
    const isChild = (this.order && this.order % 1 !== 0) || 0;
    return isChild ? this.statusType + ' indent' : '' + this.statusType;
  }

  public onLaunch(): void {
    window.open(this.launchURL, "_blank");
  }

  public onAction(): void {
    this.action.emit();
  }
}

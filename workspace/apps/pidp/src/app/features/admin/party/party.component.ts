import { Component, Inject, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';

import { EMPTY, exhaustMap } from 'rxjs';

import {
  ConfirmDialogComponent,
  DialogOptions,
  HtmlComponent,
} from '@bcgov/shared/ui';

import { APP_CONFIG, AppConfig } from '@app/app.config';
import { ToastService } from '@app/core/services/toast.service';

import { AdminResource } from '../shared/resources/admin-resource.service';
import { PartyModel } from './party.model';

@Component({
  selector: 'app-admin-party',
  templateUrl: './party.component.html',
})
export class PartyComponent implements OnInit {
  public partyID!: string;
  public partyDetail!: PartyModel;
  // eslint-disable-next-line @typescript-eslint/explicit-member-accessibility

  public constructor(
    @Inject(APP_CONFIG) private config: AppConfig,
    private adminResource: AdminResource,
    private dialog: MatDialog,
    private toastService: ToastService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  public ngOnInit(): void {
    this.partyID = this.route.snapshot.paramMap.get('partyID') || '';
    this.adminResource
      .getUserDetails(this.partyID)
      .subscribe((party: PartyModel) => {
        this.partyDetail = party;
      });

    // get all details of the party
  }

  public onBack(): void {
    this.navigateToRoot();
  }

  public resetAccessRequest(): void {
    const data: DialogOptions = {
      title: 'Reset user access request',
      component: HtmlComponent,
      data: {
        content: `Are you sure you want to reset the access request for party ${this.partyID}. Continue?`,
      },
    };
    this.dialog
      .open(ConfirmDialogComponent, { data })
      .afterClosed()
      .pipe(
        exhaustMap((result) =>
          result ? this.adminResource.resetAccessRequest(this.partyID) : EMPTY
        )
      )
      .subscribe({
        complete: () => {
          this.toastService.openSuccessToast(
            'User access request has been reset - user may now on-board again'
          );
        },
        error: (err) => {
          console.error('Failed to reset %o', err);
          this.toastService.openErrorToast(`Failed to reset access request`);
        },
      });
  }

  private navigateToRoot(): void {
    this.router.navigate(['/admin/parties']);
  }
}

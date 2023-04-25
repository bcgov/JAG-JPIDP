import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';

import { Observable } from 'rxjs';

import { APP_CONFIG, AppConfig } from '@app/app.config';
import { AbstractFormPage } from '@app/core/classes/abstract-form-page.class';
import { PartyService } from '@app/core/party/party.service';
import { DocumentService } from '@app/core/services/document.service';
import { FormUtilsService } from '@app/core/services/form-utils.service';
import { LoggerService } from '@app/core/services/logger.service';
import { PartyUserTypeResource } from '@app/features/admin/shared/usertype-resource.service';
import { AccessTokenService } from '@app/features/auth/services/access-token.service';
import { AuthorizedUserService } from '@app/features/auth/services/authorized-user.service';

import { BcpsAuthResourceService } from '../auth/bcps-auth-resource.service';
import { DigitalEvidenceResource } from '../digital-evidence-resource.service';
import { DigitalEvidenceCounselFormState } from './digital-evidence-counsel-form-state';

@Component({
  selector: 'app-digital-evidence-counsel',
  templateUrl: './digital-evidence-counsel.page.html',
  styleUrls: ['./digital-evidence-counsel.page.scss'],
})
export class DigitalEvidenceCounselPage
  extends AbstractFormPage<DigitalEvidenceCounselFormState>
  implements OnInit
{
  public formState: DigitalEvidenceCounselFormState;
  public title: string;

  public constructor(
    @Inject(APP_CONFIG) private config: AppConfig,
    private route: ActivatedRoute,
    protected dialog: MatDialog,
    private router: Router,
    protected formUtilsService: FormUtilsService,
    private partyService: PartyService,
    private resource: DigitalEvidenceResource,
    private usertype: PartyUserTypeResource,
    private userOrgunit: BcpsAuthResourceService,
    private logger: LoggerService,
    documentService: DocumentService,
    accessTokenService: AccessTokenService,
    private authorizedUserService: AuthorizedUserService,
    fb: FormBuilder
  ) {
    super(dialog, formUtilsService);
    const routeData = this.route.snapshot.data;

    this.title = routeData.title;
    this.formState = new DigitalEvidenceCounselFormState(fb);
  }

  public ngOnInit(): void {
    console.log('Loading known inboxes');
  }

  private navigateToRoot(): void {
    this.router.navigate([this.route.snapshot.data.routes.root]);
  }

  protected performSubmission(): Observable<unknown> {
    throw new Error('Method not implemented.');
  }
}

import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';

import { EMPTY, Observable, catchError, noop, of, tap } from 'rxjs';

import { APP_CONFIG, AppConfig } from '@app/app.config';
import { AbstractFormPage } from '@app/core/classes/abstract-form-page.class';
import { PartyService } from '@app/core/party/party.service';
import { DocumentService } from '@app/core/services/document.service';
import { FormUtilsService } from '@app/core/services/form-utils.service';
import { LoggerService } from '@app/core/services/logger.service';
import { IdentityProvider } from '@app/features/auth/enums/identity-provider.enum';
import { AccessTokenService } from '@app/features/auth/services/access-token.service';
import { AuthorizedUserService } from '@app/features/auth/services/authorized-user.service';
import { StatusCode } from '@app/features/portal/enums/status-code.enum';

import { JamPorResource } from './jam-por-resource.service';
import { JamPorFormState } from './jampor-form-state';

@Component({
  selector: 'app-jam-por',
  templateUrl: './jampor.page.html',
  styleUrls: ['./jampor.page.scss'],
})
export class JamPorPage
  extends AbstractFormPage<JamPorFormState>
  implements OnInit
{
  public formState: JamPorFormState;
  public title: string;
  public panelOpenState = false;

  public identityProvider$: Observable<IdentityProvider>;
  //public userType$: Observable<OrganizationUserType[]>;
  public IdentityProvider = IdentityProvider;
  public completed: boolean | null;
  public pending: boolean | null;
  public result: string;
  public userIsBCPS?: boolean;
  public userIsLawyer?: boolean;
  public showDataMismatchError: boolean;
  public userIsPublic?: boolean;
  public folioId?: number;
  public validatingUser: boolean;
  public userCodeStatus: string;
  public userValidationMessage?: string;
  public OOCCodeInvalid: boolean;
  public refreshCount: number;
  public publicAccessDenied: boolean;

  public selectedOption = 0;
  public accessRequestFailed: boolean;

  public constructor(
    @Inject(APP_CONFIG) private config: AppConfig,
    private route: ActivatedRoute,
    protected dialog: MatDialog,
    private router: Router,
    protected formUtilsService: FormUtilsService,
    private partyService: PartyService,
    private resource: JamPorResource,
    private logger: LoggerService,
    documentService: DocumentService,
    accessTokenService: AccessTokenService,
    private authorizedUserService: AuthorizedUserService,
    fb: FormBuilder
  ) {
    super(dialog, formUtilsService);
    const routeData = this.route.snapshot.data;
    this.title = routeData.title;
    this.userIsBCPS = false;
    this.userIsLawyer = false;
    this.userIsPublic = false;
    this.validatingUser = false;
    this.publicAccessDenied = false;
    this.OOCCodeInvalid = false;
    this.completed = routeData.jamPorStatusCode === StatusCode.COMPLETED;
    this.pending = routeData.jamPorStatusCode === StatusCode.PENDING;
    this.showDataMismatchError = false;
    this.identityProvider$ = this.authorizedUserService.identityProvider$;
    this.result = '';
    this.refreshCount = 0;
    this.userCodeStatus = '';
    this.formState = new JamPorFormState(fb);
    this.accessRequestFailed = false;
    accessTokenService.decodeToken().subscribe((n) => {
      if (n !== null) {
        this.result = n.identity_provider;
      }
    });
  }

  public performSubmission(): Observable<void> {
    const partyId = this.partyService.partyId;
    return partyId ? this.resource.requestAccess(partyId, 'JAM_POR') : EMPTY;
  }

  public onRequestAccess(): void {
    this.resource
      .requestAccess(this.partyService.partyId, 'JAM_POR')
      .pipe(
        tap(() => (this.pending = true)),

        catchError((error: HttpErrorResponse) => {
          if (error.status === HttpStatusCode.NotFound) {
            this.navigateToRoot();
          }
          this.accessRequestFailed = true;
          return of(noop());
        })
      )
      .subscribe({
        complete: () => {
          this.userValidationMessage = '';
          this.userCodeStatus = '';
        },
      });
  }

  public ngOnInit(): void {
    const partyId = this.partyService.partyId;
    console.log('Party %o', partyId);
  }

  public onBack(): void {
    this.navigateToRoot();
  }

  private navigateToRoot(): void {
    this.router.navigate([this.route.snapshot.data.routes.root]);
  }
}

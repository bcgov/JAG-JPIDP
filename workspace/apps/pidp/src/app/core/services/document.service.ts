import { Inject, Injectable, Type } from '@angular/core';

import { APP_CONFIG, AppConfig } from '@app/app.config';
import { UserAccessAgreementDocumentComponent } from '@app/features/profile/pages/user-access-agreement/components/user-access-agreement-document/user-access-agreement-document.component';

export enum DocumentType {
  PIDP_COLLECTION_NOTICE = 'pidp-collection-notice',
  SA_EFORMS_COLLECTION_NOTICE = 'sa-eforms-collection-notice',
  DRIVER_FITNESS_COLLECTION_NOTICE = 'driver-fitness-collection-notice',
  DIGITAL_EVIDENCE_COLLECTION_NOTICE = 'digital-evidence-collection-notice',
  USER_ACCESS_AGREEMENT = 'user-access-agreement',
  UCI_COLLECTION_NOTICE = 'uci-collection-notice',
  MS_TEAMS_DECLARATION_AGREEMENT = 'ms-teams-declaration-agreement',
  MS_TEAMS_DETAILS_AGREEMENT = 'ms-teams-details-agreement',
  MS_TEAMS_IT_SECURITY_AGREEMENT = 'ms-teams-it-security-agreement',
}

export interface IDocumentMetaData {
  type: DocumentType;
  title: string;
}

export type ComponentType<T = unknown> = Type<T>;

export interface IDocument extends IDocumentMetaData {
  content: ComponentType | string;
}

@Injectable({
  providedIn: 'root',
})
export class DocumentService {
  private documents: IDocumentMetaData[];

  public constructor(@Inject(APP_CONFIG) private config: AppConfig) {
    this.documents = [
      {
        type: DocumentType.PIDP_COLLECTION_NOTICE,
        title: 'DIAM Collection Notice',
      },
      {
        type: DocumentType.SA_EFORMS_COLLECTION_NOTICE,
        title: 'SA eForms Collection Notice',
      },
      {
        type: DocumentType.DRIVER_FITNESS_COLLECTION_NOTICE,
        title: 'Driver Medical Fitness Collection Notice',
      },
      {
        type: DocumentType.DIGITAL_EVIDENCE_COLLECTION_NOTICE,
        title: 'Digital Evidance Management System Collection Notice',
      },
      {
        type: DocumentType.USER_ACCESS_AGREEMENT,
        title: 'Access Harmonization User Access Agreement',
      },
      {
        type: DocumentType.UCI_COLLECTION_NOTICE,
        title: 'UCI Collection Notice',
      },
      {
        type: DocumentType.MS_TEAMS_DECLARATION_AGREEMENT,
        title:
          'Private Practice Access to FH Secure Messaging via MS Teams Privacy and Security Declaration',
      },
      {
        type: DocumentType.MS_TEAMS_DETAILS_AGREEMENT,
        title: 'Details on Privacy & Security Requirements',
      },
      {
        type: DocumentType.MS_TEAMS_IT_SECURITY_AGREEMENT,
        title: 'Private Practice Clinic IT Security Checklist',
      },
    ];
  }

  public getDocuments(): IDocumentMetaData[] {
    return this.documents;
  }

  public getDocumentByType(documentType: DocumentType): IDocument {
    switch (documentType) {
      case DocumentType.PIDP_COLLECTION_NOTICE:
        return {
          ...this.getDocumentMetaData(documentType),
          content: this.getPIdPCollectionNotice(),
        };
      case DocumentType.SA_EFORMS_COLLECTION_NOTICE:
        return {
          ...this.getDocumentMetaData(documentType),
          content: this.getSAeFormsCollectionNotice(),
        };
      case DocumentType.DRIVER_FITNESS_COLLECTION_NOTICE:
        return {
          ...this.getDocumentMetaData(documentType),
          content: this.getDriverFitnessCollectionNotice(),
        };
      case DocumentType.DIGITAL_EVIDENCE_COLLECTION_NOTICE:
        return {
          ...this.getDocumentMetaData(documentType),
          content: this.getDigitalEvidenceCollectionNotice(),
        };
      case DocumentType.USER_ACCESS_AGREEMENT:
        return {
          ...this.getDocumentMetaData(documentType),
          content: UserAccessAgreementDocumentComponent,
        };
      case DocumentType.UCI_COLLECTION_NOTICE:
        return {
          ...this.getDocumentMetaData(documentType),
          content: this.getUciCollectionNotice(),
        };
      case DocumentType.MS_TEAMS_DECLARATION_AGREEMENT:
        return {
          ...this.getDocumentMetaData(documentType),
          content: this.getMsTeamsDeclarationAgreement(),
        };
      case DocumentType.MS_TEAMS_DETAILS_AGREEMENT:
        return {
          ...this.getDocumentMetaData(documentType),
          content: this.getMsTeamsDetailsAgreement(),
        };
      case DocumentType.MS_TEAMS_IT_SECURITY_AGREEMENT:
        return {
          ...this.getDocumentMetaData(documentType),
          content: this.getMsTeamsITSecurityAgreement(),
        };
      default:
        throw new Error('Document type does not exist');
    }
  }

  public getDefenceCounselOnboardingNotice(): string {
    return `
      By entering your BC Law Society Verified Credential you are requesting access to the BCPS Disclosure Portal.<br/>
      You will be provided with a folio which will be an access point for you to view and retrieve disclosure for your clients.<br/>
      By registering for access and using the Disclosure Portal you acknowledge that you are bound by the implied undertakings which attach to disclosure: to safeguard the disclosure and not to use it for any other purpose or in any other context other than for the purpose of making full answer and defence in the criminal case for which it was provided.<br/>
      You will also notify BCPS if you no longer represent a client for whom you have been given disclosure or if disclosure has been erroneously given to you.
    `;
  }

  public getDefenceCounselDutyNotice(): string {
    return `
      By entering your BC Law Society Member Digital Credential and requesting access to a Duty Counsel Bail Package folio you acknowledge that you are assigned as Duty Counsel for the court location(s) and on the date(s) you have provided.<p/><p/>
      You will limit your access to the Duty Counsel Bail Package folio to those times when you are acting as Duty Counsel, and you will be responsible for ensuring that you do not access any Bail Package for individuals with whom you may have a conflict.<p/>
      You acknowledge that you are bound by the implied undertaking which attaches to these materials and will safeguard the information contained therein. You will not use the accessed material for any purpose or in any context other than for the purpose of representing as Duty Counsel those accused for whom the materials were prepared.
    `;
  }

  public getPIdPCollectionNotice(): string {
    return `
      The Digital Identity Access Management Portal collects personal information for the purposes of verification and access
      to participating court systems. This is collected by the Ministry of Attorney General under sections 26(c) and
      27(1)(b) of the Freedom of Information and Protection of Privacy Act. Should you have any questions
      about the collection of this personal information, contact
      <a href="mailto:${this.config.emails.providerIdentitySupport}">${this.config.emails.providerIdentitySupport}</a>.
    `;
  }

  public getSAeFormsCollectionNotice(): string {
    return `
      The personal information you provide to enrol for access to the Special Authority eForms application
      is collected by the British Columbia Justice Sector and Public Safety under the authority of s. 26(a) and 26(c) of
      the Freedom of Information and Protection of Privacy Act (FOIPPA) and s. 22(1)(b) of the Pharmaceutical
      Services Act for the purpose of managing your access to, and use of, the Special Authority eForms
      application. If you have any questions about the collection or use of this information, contact
      <a href="mailto:${this.config.emails.specialAuthorityEformsSupport}">${this.config.emails.specialAuthorityEformsSupport}</a>.
    `;
  }

  public getDriverFitnessCollectionNotice(): string {
    return `
      The personal information you provide to enrol for access to Driver Medical Fitness lorem ipsum dolor sit amet
      consectetur adipisicing elit. Velit quaerat, beatae libero, ullam consequuntur laudantium aliquid voluptatum
      fugit pariatur dolore repudiandae ad fuga sed, ducimus voluptates quisquam quasi perferendis possimus, contact
      <a href="mailto:${this.config.emails.driverFitnessSupport}">${this.config.emails.driverFitnessSupport}</a>.
    `;
  }
  public getDigitalEvidenceCollectionNotice(): string {
    return `
      The personal information you provide to enroll for access to Digital Evidence Management System is collected by the British Columbia Justice Sector and Public Safety under the authority of s. 26(a) and 26(c) of
      the Freedom of Information and Protection of Privacy Act (FOIPPA) for the purpose of managing your access to, and use of, the Digital Evidence Management System. If you have any questions about the collection or use of this information, contact
      <a href="mailto:${this.config.emails.digitalEvidenceSupport}">${this.config.emails.digitalEvidenceSupport}</a>.
    `;
  }

  public getUciCollectionNotice(): string {
    return `
      Unifying Clinical Information collects personal information for the purposes of verification and access to
      participating health systems. This is collected by the Ministry of Health under sections 26(c) and 27(1)(b)
      of the Freedom of Information and Protection of Privacy Act. Should you have any questions about the collection
      of this personal information, contact <a href="mailto:${this.config.emails.uciSupport}">${this.config.emails.uciSupport}</a>.
    `;
  }
  public getMsTeamsDeclarationAgreement(): string {
    return `
      Private Practice Access to FH Secure Messaging via MS Teams Privacy and Security Declaration<br><br>
      Secure Messaging from your private practice may provide you and clinic staff with direct access to
      a significant amount of clinical data about your patients from within BC Health Authorities and Ministry
      of Health systems. This data (along with data from your own EMR) can be targeted by organized criminals
      and data breaches can have a significant impact on your clinic and wider system, potentially harming your
      reputation and reducing patient trust. Implementing appropriate privacy and security safeguards reduces
      the risk of patient information breaches. This document details the requirements for granting access to
      FH secure messaging via MS Teams, and is informed by provincial legislation (PIPA - Personal Information
      Protection Act), the Privacy and Security Toolkit created by the Doctors of BC, the College of Physicians
      and Surgeons, and the Office of the Information and Privacy Commissioner, and Ministry of Health and Fraser
      Health Privacy and Security resources.<br><br>
      I acknowledge that:<br><br>
      <ol>
        <li>The member of my clinic staff who is ultimately responsible for maintaining users access and our
          privacy and security policies is:?????</li><br>
        <li>Documented privacy and security policies are communicated to all staff and external parties (e.g. vendors,
          suppliers, and partners) who have access to the clinic's computer systems.</li><br>
        <li>Security awareness training is provided to clinic staff and reviewed yearly.</li><br>
        <li>My staff is aware of malicious emails and have been informed not to click links or open attachments that
          appear suspicious.</li><br>
        <li>My staff is aware of risks associated with using USB drivers and other portable devices that may compromise
          my network.</li><br>
        <li>My staff is aware that passwords used for access to MS Teams are not permitted to be shared with other
          individuals or re-used for other servies, and that the "Save Password" feature in browsers is not utilized to
          access MS Teams.</li><br>
        <li>My clinic agrees to notify the FH eHealth Team when one of my staff no longer requires access to MS Teams.</li><br>
        <li>My clinic will retain a record for two years of the support activities (e.g. invoice/receipt with name of vendor
          and date of service) of all technical support provided by external vendors that have been conducted on computers that
          access MS Teams or my clinic's network, either directly or remotely.</li><br>
        <li>I acknowledge that my IT Support and/or myself have completed the Private Practice Clinic IT Security Checklist
          and ensured all technical requirements for accessing MS Teams have been addressed.</li><br><br>
      </ol>
      I further acknowledge that:<br><br>
      <ol>
        <li>My clinic's use of MS Teams will be audited by the FH Privacy Office. If a breach is suspected, my clinic will cooperate
          with FH staff in the investigation.</li><br>
        <li>My clinic may be selected to participate in a Privacy and Security Review conducted by the Doctors of BC's Doctors Technology
          Office, on behalf of the Ministry of Health. The purpose is to enhance educational and support process, and program evaluation.
          The findings will be anonymized.</li><br>
      </ol>
    `;
  }

  public getMsTeamsDetailsAgreement(): string {
    return `
      Details on Privacy & Security Requirements<br><br>
      <strong>Privacy & Security Resources for Private Practice Physicians:</strong><br><br>
      Education, resources and support on Clinic Privacy and Security are available via the
      <a href="${this.config.urls.doctorsTechnologyOffice}" target="_blank" rel="noopener noreferrer">Doctors Technology Office (DTO)</a>.<br><br>
      <a href="mailto:${this.config.emails.doctorsTechnologyOfficeSupport}">${this.config.emails.doctorsTechnologyOfficeSupport}</a> or 604-638-5841.<br><br>
      The
        <a href="https://www.doctorsofbc.ca/sites/default/files/physician_office_security_guide_2018_august_0.pdf" target="_blank" rel="noopener noreferrer">
        Physician Office IT Security Guide</a>
      outlines basic administrative, physical and technological safeguards you take to implement
      with the help of your Local IT provider in your practice. Most relevant tools can be found under the Physician Office IT Security
      and the Physician Privacy Tool Kit section of the Doctors Technology Office Website.<br><br>
      <strong>Privacy & Security Declaration Information:</strong><br><br>
      <ol>
        <li>Personal Information Protection Act (PIPA) requires that each clinic identifies the most responsible physician and appoints
          them as the Privacy Officer accountable for privacy and security. Security measures can be delegated to others such as your
          local IT. The Privacy Officer is also responsible for establishing the Privacy Policy and Security Policies, procedures, and
          forms. For more information, on this role, refer to the BC Physician Privacy Toolkit: A Guide for Physicians.</li><br>
        <li>Privacy and Security Policies and related documents should be communicated to staff and any individuals accessing the
          clinic eSystems. Assistance in creating such policies are available from the Doctors of BC.</li><br>
        <li> All clinic staff need to complete Privacy Training and Security Awareness Training comprised of new employee orientation and
          regularly scheduled refreshers. The content should include:<br>
          <ol type="a">
            <li>Privacy and Security policies and related procedures including any changes introduced</li>
            <li>Overview of the clinic's security safeguards and staff responsibilities</li>
            <li>Risk mitigation strategies to protect patient information security</li>
            <li>Steps required for managing a breach in emergency situations</li>
          </ol>
        </li><br>
        <li>Clinic should provide employees with instructions for e-mails, text messaging, and web browsing. These guidelines must alert staff
          to possible fraud and prevent them from clicking on attachments or links that can download malicious software (malware) to be installed
          on the user's computer and potentially spread to the entire network.</li><br>
        <li>Use of non-encrypted USB drives and mobile storage devices to store or transfer patient information is not recommended. If not properly
          protected, they can be compromised (lost of stolen) or be intercepted with malware. Malicious software can be spread to user's computer and
          potentially entire network.</li><br>
        <li>Clinic should provide employees with instructions about password protection practises to reduce the risk of unauthorized access into the
          eHealth application. Passwords must not be shared with other even as temporary workaround and not used for access to any other services (e.g.
          Gmail, Facebook, LinkedIn). The "save password" feature for any account is not safe because any user on that computer can then use the stored
          password. Each user should change their passwords semi-annually. See password management requirements on page 3 of this guide. For tips on
          creating secure passwords, visit the Physician Office IT Security Guide.</li><br>
        <li>Notifying the FHA mHealth secure messaging Team ensures that FH access accounts remain current. This will prevent the risk of someone accessing
          information to which they are no longer eligible. To contact the secure messaging team, email
          <a href="mailto:${this.config.emails.msTeamsSupport}">${this.config.emails.msTeamsSupport}</a></li><br>
        <li>During the course of providing technical support, there is a chance that unauthorized access to clinical information can occur. Keep invoices
          and/or service receipts for at least two years. In case of a privacy breach or investigation, the clinic should be able to provide details about
          technology and physical safeguards implemented. Refer to the DTO Health Technology Guide - Selecting IT Support for what can be expected from your
          IT service provider.</li><br>
      </ol>
    `;
  }

  public getMsTeamsITSecurityAgreement(): string {
    return `
    Private Practice Clinic IT Security Checklist<br><br>
    <strong>There are a number of basic technology requirements that need to be in place to safeguard patient information within your practice.</strong>
    The checklist below details the minimum clinic IT security requirements defined by FHA and the Ministry of Health for protecting your clinic from
    local threats.<br><br>
    Education, resources and supports on Clinic Privacy and Security are available via the
    <a href="${this.config.urls.doctorsTechnologyOffice}" target="_blank" rel="noopener noreferrer">Doctors Technology Office (DTO)</a>.<br><br>
    <a href="mailto:${this.config.emails.doctorsTechnologyOfficeSupport}">${this.config.emails.doctorsTechnologyOfficeSupport}</a> or 604-638-5841<br><br>
    <strong>Physical Access Control</strong><br><br>
    <ul style="list-style-type:square">
      <li>Clinic site is equipped with a monitored alarm system</li><br>
      <li>Server/Network equipment is physically secured from public access</li><br>
    </ul><br>
    <strong>User Account</strong><br><br>
    <ul style="list-style-type:square">
      <li>Each user has a unique account and password to access systems within clinic's network</li><br>
      <li>User accounts are not shared among multiple users</li><br>
      <li>A separate user account is used for system administration</li><br>
    </ul><br>
    <strong>Password Management</strong><br><br>
    <ul style="list-style-type:square">
      <li>Minimum password length is 8 characters</li><br>
      <li>Passwords contain characters from three of the following categories (Uppercase characters, Lowercase characters, Numerals,
        Non-alphanumeric keyboard symbols)</li><br>
      <li>Passwords are changed at a minimum semi-annually</li><br>
    </ul><br>
    <strong>WiFi Network</strong><br><br>
    <ul style="list-style-type:square">
      <li>SSID, WPA2/WPA3 and WiFi password settings are as per DTO Technical Bulletin</li><br>
      <li>Guest WiFi access is completely isolated from the Clinic Lan/WiFi network</li><br>
    </ul><br>
    <strong>Anti-Virus Software</strong><br><br>
    <ul style="list-style-type:square">
      <li>Anti-virus software installed and enabled fro auto update (screenshot of configuration must be attached)</li><br>
    </ul><br>
    <strong>Operating System</strong><br><br>
    <ul style="list-style-type:square">
      <li>There are no legacy/end-of-support operating systems in use (e.g. Windows XP, MacOS older than the latest three versions)</li><br>
      <li>The Operating System is enabled fro auto updates or manually patched at a minimum semi-annually</li><br>
    </ul><br>
    <strong>Application Patching</strong><br><br>
    Where it doesn't conflict with my EMR's system requirements,
    <ul style="list-style-type:square">
      <li>Desktop software, e.g. MS Office / Other applications are patched at a minimum semi-annually</li><br>
      <li>Browser plugin (Adobe Flash, PDF, Java) are patched at a minimum semi-annually</li><br>
      <li>Such patching conflicts with my EMR system requirements</li><br>
    </ul><br>
    `;
  }

  private getDocumentMetaData(documentType: DocumentType): IDocumentMetaData {
    const metadata = this.documents.find(
      (document) => document.type === documentType
    );

    if (!metadata) {
      throw new Error('Document does not exist');
    }

    return metadata;
  }
}

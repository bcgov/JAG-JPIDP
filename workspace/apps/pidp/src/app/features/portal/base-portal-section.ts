import { Section } from './state/section.model';

export class BasePortalSection {
  public GetOrder(profileStatus: Section): number {
    return profileStatus.order;
  }

}

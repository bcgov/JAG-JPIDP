export class AdminRoutes {
  public static MODULE_PATH = 'admin';

  public static PARTIES = 'parties';
  public static PARTY = 'party';
  public static IDP = 'IDP';
  public static SUBMITTING_AGENCY: 'submitting-agency';
  public static COURT_LOCATION: 'court-location';

  /**
   * @description
   * Useful for redirecting to module root-level routes.
   */
  public static routePath(route: string): string {
    return `/${AdminRoutes.MODULE_PATH}/${route}`;
  }
}

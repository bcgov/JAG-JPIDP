/**
 * @description
 * Properties of a portal section launch action.
 */
export interface PortalSectionLaunchAction {
  /**
   * Label of the action, such as Update,
   * View, or Request.
   */
  label?: string;
  /**
   * @description
   * Route the action will navigate to
   * when invoked.
   */
  url?: string;

  newWindow: boolean;
  /**
   * @description
   * Whether the action is enable or disabled
   * typically based on state.
   */
  disabled: boolean;

  /**
   * @description
   * Whether the action is hidden
   * typically based on state.
   */
  hidden?: boolean;
}

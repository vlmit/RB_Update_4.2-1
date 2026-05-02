import { StringHelper } from '@tessa/core';

/**
 * Helper for AI tools.
 * @helper
 */
export namespace AiToolHelper {
  /**
   * Returns the name of the counterparty, consisting of the full and short form.
   * @param shortName Short name.
   * @param fullName Full name.
   * @returns Created name.
   */
  export function formatPartnerName(
    shortName: string | null,
    fullName: string | null
  ): string | null {
    if (StringHelper.equals(shortName, fullName)) {
      return shortName;
    }

    if (!shortName) {
      return fullName;
    }

    if (!fullName) {
      return shortName;
    }

    return `${fullName} (${shortName})`;
  }
}

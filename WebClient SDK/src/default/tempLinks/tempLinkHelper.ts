import moment, { Duration } from 'moment';
import { IApiClient } from '@tessa/application';
import { ICardSingletonCache, PlatformResourceTypes } from '@tessa/platform';

/**
 * Helper for file/version temp links.
 * @helper
 */
export namespace TempLinkHelper {
  /**
   * Forms temporary link to access given content.
   * @param apiClient - api client.
   * @param contentId - content identifier.
   * @param token - content access token value.
   * @param isFileVersion - content type flag.
   * @returns link to content.
   */
  export async function getTempLink(
    apiClient: IApiClient,
    contentId: string,
    token: string,
    isFileVersion: boolean
  ): Promise<string> {
    return await apiClient.getUrl(`/content/${getContentType(isFileVersion)}/${contentId}`, {
      searchParams: { token: token }
    });
  }

  /**
   * Provides file content type.
   * @param isFileVersion - is it file or file version.
   * @returns content type.
   */
  export function getContentType(isFileVersion: boolean): string {
    return isFileVersion ? PlatformResourceTypes.fileVersions : PlatformResourceTypes.files;
  }

  /**
   * Get server max temp file link lifetime.
   * @returns Server max temp file link lifetime.
   */
  export async function getTempFileMaxLifetime(cache: ICardSingletonCache): Promise<Duration> {
    const serverInstanceCard = await cache.getCard('ServerInstance');
    const defaultDuration = moment.duration(365, 'd');
    if (!serverInstanceCard) {
      return defaultDuration;
    }
    const serverValue = serverInstanceCard
      ?.tryGetSections()
      ?.tryGet('ServerInstances')
      ?.fields.tryGet<number>('TempFileLinkMaxPeriod');
    if (!serverValue || serverValue <= 0) {
      return defaultDuration;
    }
    return moment.duration(serverValue, 'd');
  }
}

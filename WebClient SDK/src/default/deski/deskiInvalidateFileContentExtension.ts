import { extension } from '@tessa/application';
import { CardRequestExtension, CardResponse, ICardRequestExtensionContext } from '@tessa/platform';
import { DeskiManager } from 'tessa/deski';

@extension({ name: 'DeskiInvalidateFileContentExtension' })
export class DeskiInvalidateFileContentExtension extends CardRequestExtension {
  //#region CardRequestExtension

  async beforeRequest(context: ICardRequestExtensionContext): Promise<void> {
    const request = context.request;
    if (
      context.requestType !== '0e7fae2d-5603-4438-911a-313a2d2c8760' || // WebInvalidateContentRequest
      !request.fileVersionId
    ) {
      return;
    }

    const response = await DeskiManager.instance.removeFile(request.fileVersionId, true);
    context.response = new CardResponse();
    if (!response.success) {
      context.response.validationResult.add(response.result);
    }
  }

  //#endregion
}

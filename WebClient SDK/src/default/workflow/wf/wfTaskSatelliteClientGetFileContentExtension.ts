import {
  CardGetFileContentExtension,
  ICardGetFileContentExtensionContext
} from 'tessa/cards/extensions';
import { UIContext } from 'tessa/ui';
import { ICardModel } from 'tessa/ui/cards';
import { TypedField } from 'tessa/platform';
import { extension } from '@tessa/application';
import { CardHelper } from '@tessa/platform';
import { StorageHelper } from '@tessa/core';

@extension({ name: 'WfTaskSatelliteClientGetFileContentExtension' })
export class WfTaskSatelliteClientGetFileContentExtension extends CardGetFileContentExtension {
  public async beforeRequest(context: ICardGetFileContentExtensionContext): Promise<void> {
    const editor = UIContext.current.cardEditor;
    const request = context.request;
    let model: ICardModel;
    if (
      editor &&
      (model = editor.cardModel!) &&
      model.cardType.id === CardHelper.WfTaskCardTypeID &&
      !StorageHelper.tryGet(request.tryGetInfo()!, '.digest')
    ) {
      const digest = model.digest;
      if (digest) {
        request.info['.digest'] = TypedField.createString(digest);
      }
      return;
    }
  }
}

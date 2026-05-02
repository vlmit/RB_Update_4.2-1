import { observable } from 'mobx';
import { Guid } from '@tessa/core';
import { inject, injectable } from '@tessa/application';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import {
  EmailMessenger,
  IMessengerRecordEditor,
  IMessengerRecordEditor$,
  IMessengersProvider,
  IMessengersProvider$,
  MessengerListDataSource,
  MessengerRecordSerializable,
  MessengersList,
  MessengersListViewModel,
  MobileMessenger,
  PhoneMessenger
} from 'tessa/ui/messengers';

@injectable()
export class MessengersArticle extends PlaygroundArticle {
  constructor(
    @inject(IMessengersProvider$)
    private readonly _messengersProvider: IMessengersProvider,
    @inject(IMessengerRecordEditor$)
    private readonly _messengerRecordEditor: IMessengerRecordEditor
  ) {
    super();
  }

  override getSettings(): PlaygroundArticleSettings {
    return { name: 'Messengers' };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Messengers list in table mode',
      props: async () => {
        const dataSource = createListData();

        const list = new MessengersListViewModel({
          dataSource: dataSource,
          messengers: this._messengersProvider,
          editor: this._messengerRecordEditor,
          options: {
            defaultPhone: '+7 925 710 28-09'
          }
        });

        list.captionSettings.caption = 'Мессенджеры';
        list.captionSettings.leftCaption = false;

        await list.initialize();

        return { list };
      },
      view: ({ list }) => {
        return (
          <DemoForm
            customStyles={css => css`
              flex-direction: column;
              gap: 10px;
            `}
          >
            <MessengersList viewModel={list} />
          </DemoForm>
        );
      }
    });

    this.addBlock({
      caption: 'Messengers list in table mode with additional contact info',
      props: async () => {
        const dataSource = createListData(true);

        const list = new MessengersListViewModel({
          dataSource: dataSource,
          messengers: this._messengersProvider,
          editor: this._messengerRecordEditor,
          options: {
            defaultPhone: '+7 925 710 28-09'
          }
        });

        list.captionSettings.caption = 'Мессенджеры';
        list.captionSettings.leftCaption = false;

        await list.initialize();

        return { list };
      },
      view: ({ list }) => {
        return (
          <DemoForm
            customStyles={css => css`
              flex-direction: column;
              gap: 10px;
            `}
          >
            <MessengersList viewModel={list} />
          </DemoForm>
        );
      }
    });

    this.addBlock({
      caption: 'Add sort func',
      props: async () => {
        const dataSource = createListData();

        const list = new MessengersListViewModel({
          dataSource: dataSource,
          messengers: this._messengersProvider,
          editor: this._messengerRecordEditor,
          options: {
            defaultPhone: '+7 925 710 28-09'
          }
        });

        list.captionSettings.caption = 'Мессенджеры';
        list.captionSettings.leftCaption = false;
        list.sortFunc = (a, b) => a.messenger.code.localeCompare(b.messenger.code);

        await list.initialize();

        return { list };
      },
      view: ({ list }) => {
        return (
          <DemoForm
            customStyles={css => css`
              flex-direction: column;
              gap: 10px;
            `}
          >
            <MessengersList viewModel={list} />
          </DemoForm>
        );
      }
    });

    this.addBlock({
      caption: 'Messengers list in icons mode',
      props: async () => {
        const dataSource = createListData();

        const list = new MessengersListViewModel({
          dataSource: dataSource,
          messengers: this._messengersProvider
        });

        list.displayType = 'icons';
        list.captionSettings.caption = 'Мессенджеры';
        list.captionSettings.leftCaption = false;
        await list.initialize();

        return { list };
      },
      view: ({ list }) => {
        return (
          <DemoForm
            customStyles={css => css`
              flex-direction: column;
              gap: 10px;
            `}
          >
            <MessengersList viewModel={list} />
          </DemoForm>
        );
      }
    });

    this.addBlock({
      caption: 'Messengers list in icons mode with additional contact info',
      props: async () => {
        const dataSource = createListData(true);

        const list = new MessengersListViewModel({
          dataSource: dataSource,
          messengers: this._messengersProvider
        });

        list.displayType = 'icons';
        list.captionSettings.caption = 'Мессенджеры';
        list.captionSettings.leftCaption = false;
        await list.initialize();

        return { list };
      },
      view: ({ list }) => {
        return (
          <DemoForm
            customStyles={css => css`
              flex-direction: column;
              gap: 10px;
            `}
          >
            <MessengersList viewModel={list} />
          </DemoForm>
        );
      }
    });
  }
}

function createListData(addSystemMessengers = false): MessengerListDataSource {
  const messengersRecords: MessengerRecordSerializable[] = observable.array([
    new MessengerRecordSerializable({
      messengerCode: 'tg',
      rowId: '1',
      params: {
        phone: '+79156140659',
        normalizedPhone: '79156140659',
        username: 'tessa_user'
      }
    }),
    new MessengerRecordSerializable({
      messengerCode: 'wa',
      rowId: '2',
      params: {
        phone: '+79156140659',
        normalizedPhone: '79156140659'
      }
    }),
    new MessengerRecordSerializable({
      messengerCode: 'vb',
      rowId: '3',
      params: {
        phone: '+79156140659',
        normalizedPhone: '79156140659'
      }
    }),
    new MessengerRecordSerializable({
      messengerCode: 'fb',
      rowId: '4',
      params: {
        username: 'tessa_user'
      }
    })
  ]);

  if (addSystemMessengers) {
    messengersRecords.splice(
      0,
      0,
      new MessengerRecordSerializable({
        rowId: Guid.newGuid(),
        messengerCode: EmailMessenger.code,
        params: {
          username: 'tessa_user@mail.ru'
        }
      }),
      new MessengerRecordSerializable({
        rowId: Guid.newGuid(),
        messengerCode: PhoneMessenger.code,
        params: {
          phone: '+749556783123'
        }
      }),
      new MessengerRecordSerializable({
        rowId: Guid.newGuid(),
        messengerCode: MobileMessenger.code,
        params: {
          phone: '+79156140659'
        }
      })
    );
  }

  return new MessengerListDataSource(messengersRecords);
}

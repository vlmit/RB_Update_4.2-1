import { useState } from 'react';
import { TypedJsonConverter } from '@tessa/core';
import { inject, injectable } from '@tessa/application';
import { IUserInfo, IUserInfoFactory, IUserInfoFactory$ } from '@tessa/platform';
import { UserInfoViewer } from 'ui/userInfo/userInfoViewer';
import { UserInfoTooltip } from 'ui/userInfo/userInfoTooltip';
import { useVirtualUserInfoViewModel } from 'ui/userInfo/useVirtualUserInfoViewModel';
import {
  IUserInfoPopoverViewModelFactory$,
  IUserInfoViewModelFactory$
} from 'ui/userInfo/userInfoInjects';
import {
  IUserInfoPopoverViewModelFactory,
  IUserInfoViewModelFactory
} from 'ui/userInfo/userInfoType';
import { UserInfoPopover } from 'ui/userInfo/userInfoPopover';
import { Button } from 'ui/button/button';
import { UIButton } from 'tessa/ui/uiButton';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { MessengerRecordSerializable, MessengersHelper } from 'tessa/ui/messengers';

@injectable()
export class UserInfoArticle extends PlaygroundArticle {
  constructor(
    @inject(IUserInfoViewModelFactory$)
    private readonly _userInfoViewModelFactory: IUserInfoViewModelFactory,
    @inject(IUserInfoPopoverViewModelFactory$)
    private readonly _userInfoPopoverViewModelFactory: IUserInfoPopoverViewModelFactory,
    @inject(IUserInfoFactory$)
    private readonly _userInfoFactory: IUserInfoFactory
  ) {
    super();
  }

  private _defaultUserInfo: IUserInfo;

  override getSettings(): PlaygroundArticleSettings {
    return { name: 'UserInfo', description: 'Show default information about user.' };
  }

  override async initialize(): Promise<void> {
    const messengers: MessengerRecordSerializable[] = [
      new MessengerRecordSerializable({
        rowId: '1',
        messengerCode: 'tg',
        params: {
          username: 'testovy_user'
        }
      }),
      new MessengerRecordSerializable({
        rowId: '2',
        messengerCode: 'wa',
        params: {
          phone: '+7 (975) 220 35-37',
          normalizedPhone: MessengersHelper.normalizePhoneNumber('+7 (975) 220 35-37')
        }
      })
    ];

    this._defaultUserInfo = this._userInfoFactory({
      userInfo: {
        id: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
        name: 'Тестовый И.А.',
        position: 'Глава отдела тестирования',
        departmentName: 'Отдел тестирования',
        contacts: [
          { type: 'email', text: 'random@mail.com', value: 'random@mail.com' },
          { type: 'phone', text: '8 (985) 745-65-67', value: '89857456567' },
          { type: 'mobile', text: '8 (975) 220-35-37', value: '89752203537' }
        ],
        messengers: TypedJsonConverter.serialize(messengers.map(m => m.serializeToStorage({})))
      }
    });

    this.addBlock({
      caption: 'Basic',
      props: async () => {
        const userInfo1 = this._userInfoViewModelFactory();
        const userInfo2 = this._userInfoViewModelFactory();
        await userInfo2.setUser(this._defaultUserInfo);

        return {
          userInfo1,
          userInfo2
        };
      },
      view: ({ userInfo1, userInfo2 }) => {
        const VirtualUserInfo = () => {
          const userInfo = useVirtualUserInfoViewModel(
            this._userInfoFactory({
              userInfo: {
                id: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
                name: 'Длиннотекстовый-Невский И.А.',
                position:
                  'Трёхкратный обладатель титула «Мистер Вселенная», сценарист, бодибилдер, культурист, продюсер, писатель, офицер запаса, советник губернатора Тульской области по спорту, обладатель клички «Русский Шварцнеггер», мукомол, десантник, рак, мрак, танцор, которому ничего не мешает, хозяин клички «Наш в Голливуде», мизофоб, филантроп, боксер',
                departmentName: 'Отдел тестирования',
                contacts: [
                  { type: 'email', text: 'random@mail.com', value: 'random@mail.com' },
                  { type: 'phone', text: '8 (985) 745-65-67', value: '89857456567' }
                ]
              }
            }),
            null,
            vm => (vm.size = 'compact')
          );
          return <UserInfoViewer viewModel={userInfo} />;
        };

        return (
          <DemoForm
            customStyles={css => css`
              gap: 10px;
            `}
          >
            <UserInfoViewer viewModel={userInfo1} />
            <UserInfoViewer viewModel={userInfo2} />
            <VirtualUserInfo />
          </DemoForm>
        );
      }
    });

    this.addBlock({
      caption: 'Popover',
      props: async () => {
        const userInfo1 = this._userInfoPopoverViewModelFactory();
        const btn1 = UIButton.create({
          caption: 'Неизвестный С.М.',
          theme: 'transparent',
          buttonAction: btn => {
            userInfo1.popoverSettings = {
              getRoot: () => btn.tryGetReactComponentRef()?.current
            };
            userInfo1.show();
          }
        });

        const userInfo2 = this._userInfoPopoverViewModelFactory();
        await userInfo2.setUser(this._defaultUserInfo);
        const btn2 = UIButton.create({
          caption: 'Тестовый И.А.',
          theme: 'transparent',
          buttonAction: btn => {
            userInfo2.popoverSettings = {
              getRoot: () => btn.tryGetReactComponentRef()?.current
            };
            userInfo2.show();
          }
        });

        return {
          userInfo1,
          btn1,
          userInfo2,
          btn2
        };
      },
      view: ({ userInfo1, btn1, userInfo2, btn2 }) => {
        return (
          <DemoForm
            customStyles={css => css`
              gap: 10px;
            `}
          >
            <Button viewModel={btn1} />
            <UserInfoPopover viewModel={userInfo1} />
            <Button viewModel={btn2} />
            <UserInfoPopover viewModel={userInfo2} />
          </DemoForm>
        );
      }
    });

    this.addBlock({
      caption: 'Tooltip',
      props: async () => {
        const userInfo1 = this._userInfoPopoverViewModelFactory();
        const userInfo2 = this._userInfoPopoverViewModelFactory();
        await userInfo2.setUser(this._defaultUserInfo);
        const userInfo3 = this._userInfoPopoverViewModelFactory();
        await userInfo3.setUser(this._defaultUserInfo);

        return {
          userInfo1,
          userInfo2,
          userInfo3
        };
      },
      view: ({ userInfo1, userInfo2, userInfo3 }) => {
        return (
          <DemoForm
            customStyles={css => css`
              gap: 10px;
            `}
          >
            <UserInfoTooltip viewModel={userInfo1}>
              <Button theme="transparent">{'Неизвестный С.М.'}</Button>
            </UserInfoTooltip>
            <UserInfoTooltip viewModel={userInfo2}>
              <Button theme="transparent">{'Тестовый И.А.'}</Button>
            </UserInfoTooltip>
            <UserInfoTooltip viewModel={userInfo3} useOpenByClick={true}>
              <Button theme="transparent">{'Тестовый И.А.'}</Button>
            </UserInfoTooltip>
          </DemoForm>
        );
      }
    });

    this.addBlock({
      caption: 'Controlled tooltip',
      props: async () => {
        const userInfo1 = this._userInfoPopoverViewModelFactory();
        await userInfo1.setUser(this._defaultUserInfo);

        return {
          userInfo1
        };
      },
      view: ({ userInfo1 }) => {
        const DemoTooltip = () => {
          const [isOpen, setIsOpen] = useState(false);
          return (
            <UserInfoTooltip viewModel={userInfo1} isOpen={isOpen} onClose={() => setIsOpen(false)}>
              <Button theme="transparent" onClick={() => setIsOpen(!isOpen)}>
                {'Тестовый И.А.'}
              </Button>
            </UserInfoTooltip>
          );
        };

        return (
          <DemoForm
            customStyles={css => css`
              gap: 10px;
            `}
          >
            <DemoTooltip />
          </DemoForm>
        );
      }
    });
  }
}

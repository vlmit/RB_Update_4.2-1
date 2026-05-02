import moment, { Moment } from 'moment';
import { ISession$, ISession, inject, extension, localize } from '@tessa/application';
import { Application } from 'tessa';
import { MenuAction, SeparatorMenuAction, showConfirm, showViewModelDialog } from 'tessa/ui';
import { ApplicationExtension } from 'tessa/applicationExtension';
import { IApplicationExtensionMetadataContext } from 'tessa/applicationExtensionContext';
import { AccountButtonViewModel } from 'tessa/ui/appPanel/buttonAccount/accountButtonViewModel';
import { SessionExpireDialog, SessionExpireDialogViewModel } from './sessionExpireDialog';
import { SessionExpireDialogResultVariant, SessionInternalSettings } from './sessionExpireEnums';
import type { UserAccountItemMenuContext } from 'tessa/ui/appPanel/definitions';

/**
 * Расширение уведомляет пользователя, что сессия скоро истечет.
 *
 * Результат работы расширения - за 1 час до истечения сессии:
 * - показывает диалог, что сессия истечет в HH:mm и будет предложение перезайти в систему.
 * - добавляет в кнопку пользователя: предупреждающую иконку.
 * - добавляет в дропдаун кнопки пользователя: элемент (Обновить сессию), при нажатии на который, появится 1 диалог.
 *
 * ---
 *
 * В 1 диалоге (**Сессия истекает в HH:mm. Повторить вход в систему?**) есть 3 кнопки:
 * - Да:
 *   - при первом заходе в систему, когда пользователь не успел сделать какие-либо действия - произойдет выход из системы.
 *   - если диалог появился во время работы в системе - появится 2 диалог.
 *   - если диалог появился после нажатия на кнопку "Обновить сессию" - появится 2 диалог.
 * - Нет - диалог закроется. Если уведомление не отключено - 1 диалог появится снова через 5 мин.
 * - Нет, больше не уведомлять - диалог закроется и отключится повторный показ диалога через 5 мин.
 * Вызвать 1 диалог можно будет только через кнопку "Обновить сессию".
 *
 * ---
 *
 * Во 2 диалоге (**Сейчас будет произведен выход из системы. Несохраненные данные будут потеряны. Продолжить?**) есть 2 кнопки:
 * - Да - произойдет выход из системы.
 * - Нет - диалог закроется. Если уведомление не отключено, то через 5 мин появится 1 диалог.
 */
@extension({ name: 'SessionExpireAlertExtension' })
export class SessionExpireAlertExtension extends ApplicationExtension {
  static sessionRefreshGenerator?: (ctx: UserAccountItemMenuContext) => void;

  constructor(@inject(ISession$) private _session: ISession) {
    super();
  }

  // За сколько минут до конца сессии начать оповещать о просрочке сессии
  private _timeSessionAlertMin = SessionInternalSettings.ReopenBeforeExpirationTimeSpan * 60;

  // С какой периодичностью показывать диалог о просрочке сессии (в минутах)
  private _timeIntervalCheckSessionMin = SessionInternalSettings.ReopenAfterFailedTimeSpan;

  private _sessionExpires: Moment;

  private _intervalId: number;

  private _isShowingAlert = false;

  private needToShowAlert(): boolean {
    if (this._isShowingAlert) {
      return false;
    }

    const currentTime = moment();
    const differenceMinutes = this._sessionExpires.diff(currentTime, 'minutes');

    return differenceMinutes <= this._timeSessionAlertMin;
  }

  private logout(): void {
    Application.shutdown({ force: true, logout: true, disableAutoLogin: true });
  }

  private addAlertInUserButton(): void {
    const userButtonViewModel = AccountButtonViewModel.instance;

    if (userButtonViewModel.hasAlertIcon) {
      return;
    }

    userButtonViewModel.hasAlertIcon = true;

    const sessionRefreshGenerator = (ctx: UserAccountItemMenuContext) =>
      ctx.menuActions.push(
        new SeparatorMenuAction(),
        MenuAction.create({
          type: 'normal',
          name: 'SessionAlert',
          caption: '$UI_Misc_Refresh_Session',
          className: 'dropdown-item-session',
          icon: 'm-refresh',
          action: () => {
            this.showAlert();
          }
        })
      );

    SessionExpireAlertExtension.sessionRefreshGenerator = sessionRefreshGenerator;
    userButtonViewModel.itemUserAccountMenuGenerators.push(sessionRefreshGenerator);
  }

  private async showAlert(isSkipConfirmDialog = false): Promise<void> {
    if (!this.needToShowAlert()) {
      return;
    }

    this._isShowingAlert = true;

    this.addAlertInUserButton();

    const sessionExpiresTime = this._sessionExpires.format('HH:mm');
    const viewModel = new SessionExpireDialogViewModel(
      localize('$UI_Login_ConfirmRelogin', sessionExpiresTime)
    );
    const dialogResultVariant = await showViewModelDialog<SessionExpireDialogResultVariant>(
      viewModel,
      SessionExpireDialog
    );

    switch (dialogResultVariant) {
      case SessionExpireDialogResultVariant.Ok: {
        if (isSkipConfirmDialog) {
          this.logout();
          break;
        }

        const isConfirm = await showConfirm('$UI_Misc_Confirm_Logout_System');
        if (isConfirm) {
          this.logout();
        }
        break;
      }

      case SessionExpireDialogResultVariant.Cancel: {
        break;
      }

      case SessionExpireDialogResultVariant.CancelNoNotify: {
        clearInterval(this._intervalId);
        break;
      }
    }

    this._isShowingAlert = false;
  }

  public async afterMetadataReceived(
    _context: IApplicationExtensionMetadataContext
  ): Promise<void> {
    this._sessionExpires = moment(this._session.sessionToken?.expires);

    this.showAlert(true);

    this._intervalId = setInterval(() => {
      this.showAlert();
    }, this._timeIntervalCheckSessionMin * 60_000);
  }
}

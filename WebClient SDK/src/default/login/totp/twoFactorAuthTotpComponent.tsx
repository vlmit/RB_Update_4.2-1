import { observer } from 'mobx-react-lite';
import { QRCodeSVG } from 'qrcode.react';
import { FieldType, StorageHelper } from '@tessa/core';
import TextField from 'ui/textField/textField';
import ControlContainer from 'ui/controlContainer/controlContainer';
import { ILoginComponentProps } from 'tessa/ui/login/component/loginComponentViewModel';
import { TwoFactorAuthTotpViewModel } from './twoFactorAuthTotpViewModel';
import {
  LoginButtons,
  LoginFields,
  LoginHeader,
  LoginInfo,
  LoginMessage
} from 'tessa/ui/login/form/loginForm';

/* Компонент для двухфакторной аутентификации с использованием одноразового пароля на основе времени. */
export const TwoFactorAuthTotpComponent = observer<
  ILoginComponentProps<TwoFactorAuthTotpViewModel>
>(function TwoFactorAuthTotpComponent({ viewModel }) {
  const info = viewModel.result?.info;
  const key = StorageHelper.tryGetValue(info, 'Key', FieldType.String);
  const uri = StorageHelper.tryGetValue(info, 'Uri', FieldType.String);
  const messages = StorageHelper.tryGetValue(info, 'Message', FieldType.String)?.split('\n');

  return (
    <>
      <LoginInfo>
        <LoginHeader text={viewModel.header} />
        {messages && <LoginMessage message={messages} />}
        {uri && <QRCodeSVG style={{ alignSelf: 'center' }} value={uri} />}
        {key && (
          <ControlContainer border="permanent">
            <TextField type="text" value={key} disabled={true} />
          </ControlContainer>
        )}
        <LoginFields fields={viewModel.fields} />
        <LoginMessage
          message={viewModel.message}
          localize={(alias, defaultValue) => viewModel.localize(alias, defaultValue)}
        />
      </LoginInfo>
      <LoginButtons buttons={viewModel.buttons} />
    </>
  );
});

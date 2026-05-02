import { observer } from 'mobx-react-lite';
import { FieldType, StorageHelper } from '@tessa/core';
import { ILoginComponentProps } from 'tessa/ui/login/component/loginComponentViewModel';
import { TwoFactorAuthEmailViewModel } from './twoFactorAuthEmailViewModel';
import {
  LoginButtons,
  LoginFields,
  LoginHeader,
  LoginInfo,
  LoginMessage
} from 'tessa/ui/login/form/loginForm';

/** Компонент для двухфакторной аутентификации на основе электронной почты. */
export const TwoFactorAuthEmailComponent = observer<
  ILoginComponentProps<TwoFactorAuthEmailViewModel>
>(function TwoFactorAuthEmailComponent({ viewModel }) {
  const info = viewModel.result?.info;
  const messages = StorageHelper.tryGetValue(info, 'Message', FieldType.String)?.split('\n');

  return (
    <>
      <LoginInfo>
        <LoginHeader text={viewModel.header} />
        {messages && <LoginMessage message={messages} />}
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

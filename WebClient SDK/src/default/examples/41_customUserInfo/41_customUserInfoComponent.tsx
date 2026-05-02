import { observer } from 'mobx-react-lite';
import { StorageHelper } from '@tessa/core';
import {
  UserInfoAvatar,
  UserInfoBio,
  UserInfoContacts,
  UserInfoContainer,
  UserInfoMain,
  useUserInfoViewModel,
  userUserInfoSkeleton
} from 'ui/userInfo/userInfoViewer';
import { UserInfoViewerProps, UserInfoViewerRef } from 'ui/userInfo/userInfoType';

// В кастомном компоненте можно переиспользовать основные блоки типового user info
export const CustomUserInfo = observer<UserInfoViewerProps, UserInfoViewerRef>(
  function UserInfoViewerDefault({ viewModel }, forwardedRef) {
    const mainRef = useUserInfoViewModel(viewModel, forwardedRef);
    // если skeleton не null, то вью-модель находится в процессе загрузки
    const skeleton = userUserInfoSkeleton(viewModel);
    // достаем доп. информацию из инфо
    const office = StorageHelper.tryGet<string>(viewModel.userInfo?.info, 'office');

    const content = skeleton ?? (
      <>
        <UserInfoAvatar viewModel={viewModel} />
        <UserInfoMain>
          <UserInfoBio userInfo={viewModel.userInfo!}>
            {office && (
              <span style={{ color: '#243763', fontSize: 'var(--font-size-secondary)' }}>
                Офис: {office}
              </span>
            )}
          </UserInfoBio>
          <UserInfoContacts viewModel={viewModel} userInfo={viewModel.userInfo!} />
        </UserInfoMain>
      </>
    );

    return (
      <UserInfoContainer viewModel={viewModel} ref={mainRef}>
        {content}
      </UserInfoContainer>
    );
  },
  { forwardRef: true }
);

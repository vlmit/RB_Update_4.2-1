import { useCallback, useMemo, useState } from 'react';
import { injectable } from '@tessa/application';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { Alert, AlertType, AlertViewModel, AlertCtorProps } from 'tessa/ui/alerts';
import { AlertComponent } from 'tessa/ui/alerts/alertComponent';
import { observer } from 'mobx-react-lite';
import { UIButton, UIButtonComponent } from 'tessa/ui';
import { AlertArticlePropertyGrid } from './alertPropertyGrid';
import { Toolbar } from 'ui/toolbar';
import { group } from 'ui/toolbar/helpers';
import { ValidationResult, ValidationResultBuilder, ValidationLevel } from '@tessa/core';

@injectable()
export class AlertArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Alert',
      description: 'Alerts notify the user of consequences of their actions.',
      keywords: ['notification', 'notify', 'popup']
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Usage',
      props: async () => {
        const button = UIButton.create({
          caption: 'Call alert',
          type: 'small',
          theme: 'primary',
          buttonAction: async () => {
            const alert = await Alert.show({
              text: 'This is a test alert',
              type: AlertType.Success,
              onClick: () => console.log('alert body clicked')
            });

            alert.onClose.add(() => console.log('alert close button clicked'));
          }
        });

        return { button };
      },
      view: ({ button }) => {
        return (
          <DemoForm>
            <UIButtonComponent viewModel={button} />
          </DemoForm>
        );
      },
      code: `
~~~js
const button = UIButton.create({
  buttonAction: async () => {
    const alert = await Alert.show({
      text: 'This is a test alert',
      type: AlertType.Success,
      onClick: () => console.log('alert body clicked')
    });
    // do anything with the alert
    alert.onClose.add(() => console.log('alert close button clicked'));
  }
});

// shorthand syntax for an alert of any type with default settings:
const alert = await Alert.success('This is a test alert');
~~~`
    });

    this.addBlock({
      caption: 'Alert with settings',
      props: async () => {
        const alert = new AlertViewModel({
          text: 'Alert text',
          type: AlertType.Success
        });

        await alert.initialize();

        return {
          vm: alert
        };
      },
      view: observer(({ vm }) => {
        const [alert, setAlert] = useState(vm);

        const callAlertBtn = useMemo(
          () =>
            UIButton.create({
              name: 'callAlert',
              caption: 'Call alert',
              tooltip: 'Invoke a real alert with settings below applied.',
              type: 'small',
              theme: 'primary',
              buttonAction: () => Alert.show(getAlertSettings(alert))
            }),
          [alert]
        );

        const recreate = useCallback(async () => {
          // its purpose is to create a new alert when appearance direction changes,
          // so that alert's appearance animation restarts
          const newAlert = new AlertViewModel(getAlertSettings(alert));
          alert.dispose();
          await newAlert.initialize();
          setAlert(newAlert);
        }, [alert]);

        return (
          <div style={{ display: 'flex', flexDirection: 'column' }}>
            <DemoForm
              customStyles={css => css`
                gap: 10px;
                display: block !important;
                margin: 0 !important;
                height: 200px;
              `}
            >
              <div
                className="alerts-host"
                style={{
                  position: 'relative',
                  border: '1px #bbb dashed',
                  borderRadius: 12,
                  zIndex: 1
                }}
              >
                <div
                  className={`alert-container ${alert.effectivePosition.point}`}
                  style={{
                    translate: `${alert.effectivePosition.offset.left ?? '0'} ${alert.effectivePosition.offset.top ?? '0'}`
                  }}
                >
                  <AlertComponent viewModel={alert} key={alert.uiId} />
                </div>
              </div>
            </DemoForm>
            <div style={{ padding: 10 }}>
              <Toolbar items={[callAlertBtn]} groups={[group('callAlert', ['callAlert'])]} />
            </div>
            <AlertArticlePropertyGrid alert={alert} recreate={recreate} />
          </div>
        );
      })
    });

    this.addBlock({
      caption: 'From validationResult',
      props: async () => {
        const button = UIButton.create({
          caption: 'Call alert',
          type: 'small',
          theme: 'primary',
          buttonAction: () => {
            const result = new ValidationResultBuilder()
              .add(ValidationResult.fromText('message1'))
              .add(ValidationResult.fromError(Error('error1')))
              .build();
            Alert.show({
              validationResult: result,
              validationLevel: ValidationLevel.Message,
              type: AlertType.Info
            });
          }
        });

        return { button };
      },
      view: ({ button }) => {
        return (
          <DemoForm>
            <UIButtonComponent viewModel={button} />
          </DemoForm>
        );
      },
      code: `
~~~js
const result = new ValidationResultBuilder()
  .add(ValidationResult.fromText('message1'))
  .add(ValidationResult.fromError(Error('error1')))
  .build();

const button = UIButton.create({
  buttonAction: () =>
    Alert.show({
      validationResult,
      // all optional from here below
      validationLevel: ValidationLevel.Detailed,
      type: AlertType.Info
    })
});
~~~`
    });
  }
}

const getAlertSettings = (alert: AlertViewModel): AlertCtorProps => ({
  text: alert.text,
  type: alert.type,
  size: alert.size,
  position: {
    point: alert.effectivePosition.point,
    appearsFrom: alert.effectivePosition.appearsFrom,
    offset: {
      left: alert.effectivePosition.offset.left,
      top: alert.effectivePosition.offset.top
    }
  },
  closeable: alert.closeable,
  onClick: alert.onClick
});

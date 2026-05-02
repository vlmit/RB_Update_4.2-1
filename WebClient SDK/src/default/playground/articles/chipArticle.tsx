import { injectable } from '@tessa/application';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import Chip from 'ui/chip/chip';
import { Alert, AlertType } from 'tessa/ui/alerts';
import { Icon } from 'ui/icon/icon';

@injectable()
export class ChipArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Chip component',
      order: 1
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Chips examples',
      props: async () => {
        const alert = async (value: string) => {
          await Alert.show({
            text: value,
            type: AlertType.Info
          });
        };

        return {
          alert
        };
      },
      view: ({ alert }) => (
        <DemoForm>
          <div style={{ display: 'flex', gap: '20px', flexWrap: 'wrap', alignItems: 'center' }}>
            <Chip label="Chip outlined" showBorder={true} corners="straight" />
            <Chip
              label="Chip primary"
              theme="primary"
              onClick={e => alert(e.currentTarget.textContent ?? 'Click')}
            />
            <Chip label="Chip success" theme="success" startIcon="m-emoji" />
            <Chip
              label="Chip info"
              theme="info"
              startIcon={Icon({ icon: 'm-align-justify', size: 'm' }) ?? 'm-link'}
            />
            <Chip
              label="Chip error"
              theme="error"
              onClose={e => alert(e.currentTarget.parentElement?.textContent ?? 'Click')}
              corners="straight"
            />
            <Chip
              label="Chip warning"
              theme="warning"
              endIcon={<i className={`icon-m m-text-block-clear`} />}
              startIcon="m-code-block"
              onClick={() => alert('Click on warning')}
              onClose={() => alert('Close warning')}
              corners="round"
            />
            <Chip
              label="Chip custom style"
              style={{ background: '#97a9e6', color: '#4164cb', border: '1px solid #4164cb' }}
              corners="round"
            />
            <Chip
              label="Chip compact"
              theme="primary"
              type="compact"
              style={{ background: '#ffa577', color: 'white' }}
              showCounter={false}
              corners="straight"
              counter={7}
            />
            <Chip
              label="Chip indicator color"
              showIndicator={true}
              corners="round"
              style={{ background: '#803f97' }}
              showBorder={true}
              counter={12}
            />
            <Chip label="Chip indicator default" corners="straight" counter={144} />
            <Chip
              label="Chip indicator color"
              showIndicator={true}
              style={{ background: '#77973f' }}
              showCounter={true}
              counter={55}
              showBorder={true}
            />
            <Chip
              label="Chip outlined background"
              showBorder={true}
              style={{ background: '#ffa577' }}
              corners="straight"
            />
            <Chip
              label="Chip indicator border"
              showIndicator={true}
              corners="round"
              showBorder={true}
              style={{ background: '#841613' }}
              counter={78945}
            />
            <Chip
              label="Chip compact"
              type="compact"
              style={{ background: '#ffa577', color: 'white' }}
              showCounter={false}
              showIndicator={true}
              counter={7}
            />
          </div>
        </DemoForm>
      )
    });
  }
}

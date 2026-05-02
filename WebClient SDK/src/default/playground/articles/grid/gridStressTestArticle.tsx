import { FieldType } from '@tessa/core';
import { injectable } from '@tessa/application';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { Grid, GridFactory, GridHelper, GridDataSource } from 'ui/grid';
import { MockDataHelper } from './data/mockDataHelper';
import { Button } from 'ui/button/buttonViewModel';

@injectable()
export class GridStressTestArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Stress test'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Client stress test',
      props: async () => {
        const columns = GridHelper.ensureTypedMetadata<TestData>([
          {
            id: 'Login',
            caption: 'Login',
            dataSourceKey: 'login',
            dataType: FieldType.String
          },
          {
            id: 'Age',
            caption: 'Age',
            dataSourceKey: 'age',
            dataType: FieldType.Int
          },
          {
            id: 'LastSeen',
            caption: 'Last seen',
            dataSourceKey: 'lastSeen',
            dataType: FieldType.DateTime
          },
          {
            id: 'Salary',
            caption: 'Salary',
            dataSourceKey: 'salary',
            dataType: FieldType.Decimal
          }
        ]);

        const data = generateTestData(1000);
        const dataSource = new GridDataSource(data);

        const grid = GridFactory.createDefault({
          dataSource: dataSource,
          options: {
            columnsMetadata: columns,
            pagingOptions: {
              mode: 'optional'
            },
            multiselect: true
          }
        });

        const addButton = grid.mainPanel.items.find(
          i => i instanceof Button && i.name === 'AddRowButton'
        ) as Button;

        if (addButton) {
          addButton.buttonAction = async () => {
            if (!addButton.disabled) {
              const row = generateTestRow();
              grid.addRow({
                Login: row.login,
                Age: row.age,
                LastSeen: row.lastSeen,
                Salary: row.salary
              });
            }
          };
        }

        await grid.initialize();

        return {
          grid
        };
      },
      view: ({ grid }) => (
        <DemoForm
          customStyles={css => css`
            gap: 10px;
            min-width: 350px;
            display: flex;
            flex-direction: column;
            max-height: 500px;
          `}
        >
          <Grid viewModel={grid} />
        </DemoForm>
      )
    });
  }
}

type TestData = {
  login: string;
  age: number;
  lastSeen: string;
  salary: string;
};

function generateTestData(count: number): TestData[] {
  const result: TestData[] = [];

  for (let i = 0; i < count; ++i) {
    result.push(generateTestRow());
  }

  return result;
}

function generateTestRow(): TestData {
  return {
    login: MockDataHelper.getRandomString(5),
    age: MockDataHelper.getRandomInt(1, 99),
    lastSeen: MockDataHelper.getRandomDate(new Date(2012, 0, 1), new Date()).toString(),
    salary: MockDataHelper.getRandomDecimal(10000, 1000000, 2)
  };
}

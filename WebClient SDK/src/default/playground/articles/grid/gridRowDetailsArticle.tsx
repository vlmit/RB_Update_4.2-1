import { useRef } from 'react';
import { observer } from 'mobx-react-lite';
import { observable } from 'mobx';
import { injectable } from '@tessa/application';
import { FieldType } from '@tessa/core';
import { ComponentsRegistry, useViewModel, InitializableViewModelBase } from '@tessa/ui';
import { delay } from 'tessa/platform';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import {
  IGridColumnMetadata,
  GridDataSource,
  GridHelper,
  GridToggleRowDetailsExtension,
  GridUserInfoExtension,
  GridDefaultRowDetails,
  GridViewModel,
  Grid,
  GridFactory,
  IGridCellViewModel
} from 'ui/grid';
import { Chip } from 'ui/chip';
import './gridRowDetailsArticle.scss';

@injectable()
export class GridRowDetailsArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Grid/Row details'
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Display row details below the row',
      props: async () => {
        const dataSource = new GridDataSource<DocumentInfo>(observable.array(data));

        const grid = new GridViewModel({
          dataSource,
          options: {
            columnsMetadata: topMetadata
          }
        });

        grid.extensionContainer
          .addExtension(GridUserInfoExtension, {
            settings: {
              targetColumnId: 'Owner',
              userId: cell => cell.row.getSource<DocumentInfo>().ownerId,
              modifyAvatar: avatar => {
                avatar.size = 'xs';
              }
            }
          })
          .addExtension(GridToggleRowDetailsExtension, {
            settings: {
              columnId: 'ExpandButton'
            }
          });

        grid.extensionContainer.addHooks({
          rowInitializing: async context => {
            const row = context.row;
            context.row.rowDetails = new GridDefaultRowDetails({
              row,
              contentFactory: async () =>
                new MeetingNotesDetails(context.row.getSource<DocumentInfo>().notes)
            });
            context.row.rowDetails.className.add('grid-article-document-info-row-details');
          },
          cellInitializing: async context => {
            if (context.cell.column.id === 'Status') {
              context.cell.contentOverride = (cell, defaultContent) => {
                const status = cell.getValue<DocumentStatus>();

                if (!cell.formattedValue || !status) {
                  return defaultContent;
                }

                const theme = getStatusTheme(status);
                return (
                  <Chip
                    label={cell.formattedValue}
                    corners="round"
                    theme={theme}
                    className="grid-article-document-status"
                  />
                );
              };
            }
          }
        });

        await grid.initialize();

        grid.permissionsContainer.setReadonly(true);

        return { grid };
      },
      view: ({ grid }) => (
        <DemoForm
          customStyles={css => css`
            gap: 10px;
            min-width: 350px;
            display: flex;
            flex-direction: column;
          `}
        >
          <Grid viewModel={grid} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Lazy loading and custom loader',
      description:
        'You can load the details content asynchronously. While the content is loading, a placeholder will be displayed in its place.',
      props: async () => {
        const dataSource = new GridDataSource<DocumentInfo>(observable.array(data));

        const grid = new GridViewModel({
          dataSource,
          options: {
            columnsMetadata: topMetadata
          }
        });

        grid.extensionContainer
          .addExtension(GridUserInfoExtension, {
            settings: {
              targetColumnId: 'Owner',
              userId: cell => cell.row.getSource<DocumentInfo>().ownerId,
              modifyAvatar: avatar => {
                avatar.size = 'xs';
              }
            }
          })
          .addExtension(GridToggleRowDetailsExtension, {
            settings: {
              columnId: 'ExpandButton'
            }
          });

        grid.extensionContainer.addHooks({
          rowInitializing: async context => {
            const row = context.row;

            context.row.rowDetails = new GridDefaultRowDetails({
              row,
              contentFactory: async () => {
                await delay(3000);

                return new MeetingNotesDetails(context.row.getSource<DocumentInfo>().notes);
              },
              loadingPlaceholder: row.uiId % 2 === 0 ? () => <CustomLoader /> : undefined
            });
            context.row.rowDetails.className.add('grid-article-document-info-row-details');
          },
          cellInitializing: async context => {
            if (context.cell.column.id === 'Status') {
              context.cell.contentOverride = (cell, defaultContent) => {
                const status = cell.getValue<DocumentStatus>();

                if (!cell.formattedValue || !status) {
                  return defaultContent;
                }

                const theme = getStatusTheme(status);
                return (
                  <Chip
                    label={cell.formattedValue}
                    corners="round"
                    theme={theme}
                    className="grid-article-document-status"
                  />
                );
              };
            }
          }
        });

        await grid.initialize();

        grid.permissionsContainer.setReadonly(true);

        return { grid };
      },
      view: ({ grid }) => (
        <DemoForm
          customStyles={css => css`
            gap: 10px;
            min-width: 350px;
            display: flex;
            flex-direction: column;
          `}
        >
          <Grid viewModel={grid} />
        </DemoForm>
      )
    });
  }
}

export function registerRowDetails(): void {
  ComponentsRegistry.instance.register(MeetingNotesDetails, MeetingNotesDetailsComponent);
}

type DocumentStatus = 'Approved' | 'Pending' | 'In Review' | 'Draft';

type MeetingNote = {
  sectionId: string;
  sectionTitle: string;
  authorId: string;
  authorName: string;
  lastModified: string;
  comments: string;
};

type DocumentInfo = {
  fullNumber: string;
  title: string;
  ownerId: string;
  ownerName: string;
  status: DocumentStatus;
  notes: MeetingNote[];
};

class MeetingNotesDetails extends InitializableViewModelBase {
  //#region ctor

  constructor(readonly notes: MeetingNote[]) {
    super();

    this.grid = this.createGrid();
  }

  //#endregion

  //#region props

  readonly grid: GridViewModel;

  //#endregion

  //#region methods

  protected override async initializeCore(): Promise<void> {
    await this.grid.initialize();
  }

  protected override disposeCore(): void {
    this.grid.dispose();
  }

  private createGrid(): GridViewModel {
    const metadata = GridHelper.ensureTypedMetadata<MeetingNote>([
      {
        id: 'SectionID',
        caption: 'Section ID',
        dataSourceKey: 'sectionId',
        dataType: FieldType.String
      },
      {
        id: 'SectionTitle',
        caption: 'Section Title',
        dataSourceKey: 'sectionTitle',
        dataType: FieldType.String
      },
      {
        id: 'Author',
        caption: 'Author',
        dataSourceKey: 'authorName',
        dataType: FieldType.String
      },
      {
        id: 'LastModified',
        caption: 'Last Modified',
        dataSourceKey: 'lastModified',
        dataType: FieldType.DateTime,
        formattingSettings: {
          specifier: 'd'
        }
      },
      {
        id: 'Comments',
        caption: 'Comments',
        dataSourceKey: 'comments',
        dataType: FieldType.String
      }
    ]);

    const dataSource = new GridDataSource(this.notes);

    const grid = GridFactory.createDefault({
      dataSource,
      options: {
        columnsMetadata: metadata
      }
    });

    grid.captionSettings.caption = 'Meeting details';

    grid.extensionContainer.addExtension(GridUserInfoExtension, {
      settings: {
        targetColumnId: 'Author',
        userId: (cell: IGridCellViewModel) => cell.row.getSource<MeetingNote>().authorId,
        modifyAvatar: avatar => {
          avatar.size = 'xs';
        }
      }
    });

    grid.extensionContainer.addHooks({
      gridPermissionsInitializing: async context => {
        if (context.type === 'grid') {
          context.permissionsContainer.setReadonly(true);
        }
      }
    });

    return grid;
  }

  //#endregion
}

const MeetingNotesDetailsComponent = observer<{ viewModel: MeetingNotesDetails }>(
  ({ viewModel }) => {
    const ref = useRef<HTMLDivElement>(null);

    useViewModel(viewModel, ref);

    return (
      <div className="grid-article-meeting-nodes-container" ref={ref}>
        <Grid viewModel={viewModel.grid} />
      </div>
    );
  }
);

function getStatusTheme(
  status: DocumentStatus
): 'primary' | 'success' | 'info' | 'warning' | 'error' {
  switch (status) {
    case 'Approved':
      return 'success';
    case 'Draft':
      return 'primary';
    case 'In Review':
      return 'info';
    case 'Pending':
      return 'warning';
    default:
      return 'primary';
  }
}

const data: DocumentInfo[] = [
  {
    fullNumber: 'DOC-001',
    title: 'Project Proposal',
    ownerId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
    ownerName: 'Alice Smith',
    status: 'Approved',
    notes: [
      {
        sectionId: 'SEC-001',
        sectionTitle: 'Executive Summary',
        authorId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
        authorName: 'Alice Smith',
        lastModified: '2025-09-18',
        comments: 'Finalized'
      },
      {
        sectionId: 'SEC-002',
        sectionTitle: 'Objectives',
        authorId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
        authorName: 'Alice Smith',
        lastModified: '2025-09-19',
        comments: 'Needs board approval'
      },
      {
        sectionId: 'SEC-003',
        sectionTitle: 'Budget',
        authorId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
        authorName: 'Bob Johnson',
        lastModified: '2025-09-20',
        comments: 'Under review'
      }
    ]
  },
  {
    fullNumber: 'DOC-002',
    title: 'Financial Report Q3',
    ownerId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
    ownerName: 'Bob Johnson',
    status: 'Pending',
    notes: [
      {
        sectionId: 'SEC-004',
        sectionTitle: 'Revenue Summary',
        authorId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
        authorName: 'Bob Johnson',
        lastModified: '2025-09-21',
        comments: 'Pending CFO confirmation'
      },
      {
        sectionId: 'SEC-005',
        sectionTitle: 'Expenses Report',
        authorId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
        authorName: 'Carol White',
        lastModified: '2025-09-22',
        comments: 'Cross-check required'
      },
      {
        sectionId: 'SEC-006',
        sectionTitle: 'Profit Analysis',
        authorId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
        authorName: 'Bob Johnson',
        lastModified: '2025-09-23',
        comments: 'Draft version'
      }
    ]
  },
  {
    fullNumber: 'DOC-003',
    title: 'DesignSpecification',
    ownerId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
    ownerName: 'Carol White',
    status: 'In Review',
    notes: [
      {
        sectionId: 'SEC-007',
        sectionTitle: 'UI Guidelines',
        authorId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
        authorName: 'Carol White',
        lastModified: '2025-09-18',
        comments: 'Reviewed by UX team'
      },
      {
        sectionId: 'SEC-008',
        sectionTitle: 'API Contracts',
        authorId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
        authorName: 'David Brown',
        lastModified: '2025-09-20',
        comments: 'Needs security review'
      },
      {
        sectionId: 'SEC-009',
        sectionTitle: 'Data Models',
        authorId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
        authorName: 'Carol White',
        lastModified: '2025-09-21',
        comments: 'In progress'
      }
    ]
  },
  {
    fullNumber: 'DOC-004',
    title: 'Contract Agreement',
    ownerId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
    ownerName: 'David Brown',
    status: 'Approved',
    notes: [
      {
        sectionId: 'SEC-010',
        sectionTitle: 'Terms & Conditions',
        authorId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
        authorName: 'David Brown',
        lastModified: '2025-09-19',
        comments: 'Finalized'
      },
      {
        sectionId: 'SEC-011',
        sectionTitle: 'Payment Terms',
        authorId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
        authorName: 'Bob Johnson',
        lastModified: '2025-09-20',
        comments: 'Reviewed'
      },
      {
        sectionId: 'SEC-012',
        sectionTitle: 'Termination Clause',
        authorId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
        authorName: 'David Brown',
        lastModified: '2025-09-21',
        comments: 'Needs legal approval'
      }
    ]
  },
  {
    fullNumber: 'DOC-005',
    title: 'Meeting Notes',
    ownerId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
    ownerName: 'Eve Adams',
    status: 'Draft',
    notes: [
      {
        sectionId: 'SEC-013',
        sectionTitle: 'Introduction',
        authorId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
        authorName: 'Eve Adams',
        lastModified: '2025-09-20',
        comments: 'Needs summary added'
      },
      {
        sectionId: 'SEC-014',
        sectionTitle: 'Action Items',
        authorId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
        authorName: 'Frank Miller',
        lastModified: '2025-09-21',
        comments: 'Check deadlines'
      },
      {
        sectionId: 'SEC-015',
        sectionTitle: 'Decisions Made',
        authorId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
        authorName: 'Alice Smith',
        lastModified: '2025-09-22',
        comments: 'Reviewed and OK'
      },
      {
        sectionId: 'SEC-016',
        sectionTitle: 'Next Steps',
        authorId: '3db19fa0-228a-497f-873a-0250bf0a4ccb',
        authorName: 'Eve Adams',
        lastModified: '2025-09-23',
        comments: 'Draft version only'
      }
    ]
  }
];

const topMetadata: IGridColumnMetadata[] = [
  {
    id: 'Number',
    caption: 'Number',
    dataSourceKey: 'fullNumber',
    dataType: FieldType.String
  },
  {
    id: 'Title',
    caption: 'Title',
    dataSourceKey: 'title',
    dataType: FieldType.String
  },
  {
    id: 'Owner',
    caption: 'Owner',
    dataSourceKey: 'ownerName',
    dataType: FieldType.String
  },
  {
    id: 'Status',
    caption: 'Status',
    dataSourceKey: 'status',
    dataType: FieldType.String
  },
  {
    id: 'ExpandButton',
    caption: '',
    dataSourceKey: '',
    dataType: FieldType.Unknown,
    canSort: false
  }
];

const CustomLoader = () => {
  return <span className="grid-article-custom-loader">Loading data...</span>;
};

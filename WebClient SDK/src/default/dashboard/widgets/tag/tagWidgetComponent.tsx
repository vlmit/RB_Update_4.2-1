import { useCallback } from 'react';
import { observer } from 'mobx-react-lite';
import { getRgbaFromDecimal } from 'tessa/ui/uiHelper';
import { TagComponent } from 'tessa/ui/tags/tagComponent';
import { TagWidget } from './tagWidget';
import './tagStyle.scss';

/** Компонент виджета {@link TagWidget}. */
export const TagWidgetComponent = observer<{ viewModel: TagWidget }>(function TagWidgetComponent({
  viewModel
}) {
  const handleClick = useCallback(() => viewModel.action(), [viewModel]);
  const { tagInfo, recordsCount } = viewModel.tagData ?? TagWidget.unknownTagData;

  return (
    <div className="tag-widget-container" onClick={handleClick}>
      <TagComponent
        type="normal"
        id={tagInfo.id}
        key={tagInfo.id}
        name={tagInfo.name}
        icon={tagInfo.icon}
        iconSize="l"
        title={viewModel.caption}
        caption={viewModel.caption}
        counter={recordsCount}
        background={getRgbaFromDecimal(tagInfo.background)}
        clickMode={tagInfo.clickMode}
        isCommon={tagInfo.isCommon}
      />
    </div>
  );
});

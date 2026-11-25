import { t } from '../../shared/i18n/fa';
import { Chip } from '../../shared/ui/Chip';
import type { Stage } from '../production/stagesApi';
import { lockedStage, stageChoices, toggleStage } from './stagePicks';

interface StagePickerProps {
  /** The whole stage list, in the order of the work, inactive stages included. */
  stages: readonly Stage[];
  ticked: readonly string[];
  onChange: (ticked: string[]) => void;
  /**
   * The last ticked stage cannot be taken off: a contractor's page saves every tick at once, and
   * a contractor does at least one stage (ADR 0019). The new-contractor form checks it on saving
   * instead, so a wrong first tick can be taken back.
   */
  keepOne?: boolean;
  disabled?: boolean;
}

/**
 * The stages a contractor or a worker does, ticked by pressing them, in the order of the work.
 * An inactive stage shows only while it stays ticked, marked «غیرفعال» (ADR 0022).
 */
export function StagePicker({ stages, ticked, onChange, keepOne = false, disabled = false }: StagePickerProps) {
  const choices = stageChoices(stages, ticked);
  if (choices.length === 0) {
    return <p className="text-sm text-stone-500">{t('parties.noActiveStages')}</p>;
  }

  const lastOne = lockedStage(ticked, keepOne);

  return (
    <div className="flex flex-wrap gap-2">
      {choices.map((stage) => (
        <Chip
          key={stage.id}
          label={stage.name}
          on={ticked.includes(stage.id)}
          locked={stage.id === lastOne}
          disabled={disabled}
          title={stage.id === lastOne ? t('parties.lastStage') : undefined}
          note={stage.isActive ? undefined : t('parties.stageInactive')}
          onToggle={() => onChange(toggleStage(stages, ticked, stage.id))}
        />
      ))}
    </div>
  );
}

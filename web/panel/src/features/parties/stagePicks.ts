import type { Stage } from '../production/stagesApi';
import type { PartyKind } from './partiesApi';

/**
 * The stages offered on a contractor's or a worker's form, in the order of the work: the active
 * ones, and an inactive one only while it is still ticked, because a stage ticked before it was
 * deactivated stays ticked, but no one takes it newly (ADR 0022).
 */
export function stageChoices(stages: readonly Stage[], ticked: readonly string[]): Stage[] {
  return stages.filter((stage) => stage.isActive || ticked.includes(stage.id));
}

/** The ticks after pressing one stage, kept in the order of the work. */
export function toggleStage(stages: readonly Stage[], ticked: readonly string[], stageId: string): string[] {
  const next = ticked.includes(stageId) ? ticked.filter((id) => id !== stageId) : [...ticked, stageId];
  return stages.filter((stage) => next.includes(stage.id)).map((stage) => stage.id);
}

/**
 * Whether a person's page keeps the last ticked stage: a contractor does at least one stage,
 * because the work-order screen lists contractors by stage and one with none would appear in no
 * list (ADR 0019). A worker may have none.
 */
export function keepsOneStage(kind: PartyKind): boolean {
  return kind === 'contractors';
}

/** The stage that cannot be unticked, when the last one is kept and only one is ticked. */
export function lockedStage(ticked: readonly string[], keepOne: boolean): string | undefined {
  return keepOne && ticked.length === 1 ? ticked[0] : undefined;
}

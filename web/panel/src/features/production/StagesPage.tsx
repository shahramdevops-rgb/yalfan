import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { upsert } from '../../shared/api/listCache';
import { fieldCode, toProblem } from '../../shared/api/problem';
import { attempt, refusalMessage } from '../../shared/api/refusal';
import { t } from '../../shared/i18n/fa';
import { AddNameForm } from '../../shared/ui/AddNameForm';
import { NameList } from '../../shared/ui/NameList';
import { ListSkeleton, LoadError } from '../../shared/ui/QueryStates';
import { moveItem } from '../../shared/ui/sortable';
import { stagesApi, stagesKey, type Stage } from './stagesApi';

const outdated = 'production.stage_order_outdated';

/**
 * The stages in the order of the work, which every other list of stages follows. The operator
 * drags a stage into place (ADR 0022); a new one is added at the end.
 */
export function StagesPage() {
  const queryClient = useQueryClient();
  const list = useQuery({ queryKey: stagesKey, queryFn: stagesApi.list });
  const [moving, setMoving] = useState(false);
  const [moveError, setMoveError] = useState<string>();

  const put = (stage: Stage) => queryClient.setQueryData<Stage[]>(stagesKey, (stages) => upsert(stages, stage));

  // Shown in the new order at once; put back if the API refuses it.
  async function move(from: number, to: number) {
    await queryClient.cancelQueries({ queryKey: stagesKey });
    const before = queryClient.getQueryData<Stage[]>(stagesKey);
    if (!before) {
      return;
    }

    const after = moveItem(before, from, to);
    queryClient.setQueryData(stagesKey, after);
    setMoving(true);
    setMoveError(undefined);

    try {
      queryClient.setQueryData(stagesKey, await stagesApi.reorder(after.map((stage) => stage.id)));
    } catch (error) {
      queryClient.setQueryData(stagesKey, before);
      setMoveError(refusalMessage(error, 'stageIds'));

      // Another user changed the list meanwhile: bring the new one, to be dragged again.
      const problem = toProblem(error);
      if ((fieldCode(problem, 'stageIds') ?? problem.code) === outdated) {
        void queryClient.invalidateQueries({ queryKey: stagesKey });
      }
    } finally {
      setMoving(false);
    }
  }

  return (
    <div className="flex max-w-3xl flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t('stages.title')}</h1>
        <p className="mt-1 text-stone-500">{t('stages.hint')}</p>
      </div>

      <section className="flex flex-col gap-6 rounded-2xl bg-white p-4 shadow-sm ring-1 ring-stone-200 sm:p-6">
        <AddNameForm label={t('stages.add')} onAdd={(name) => attempt(() => stagesApi.create(name), put, 'name')} />

        {moveError && (
          <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-800">
            {moveError}
          </p>
        )}
        {list.isPending && <ListSkeleton rows={3} />}
        {list.isError && <LoadError error={list.error} onRetry={() => void list.refetch()} />}
        {list.data && (
          <NameList
            items={list.data}
            onRename={(id, name) => attempt(() => stagesApi.rename(id, name), put, 'name')}
            onSetActive={(id, isActive) => attempt(() => stagesApi.setActive(id, isActive), put)}
            onMove={(from, to) => void move(from, to)}
            moving={moving}
            emptyText={t('stages.empty')}
          />
        )}
      </section>
    </div>
  );
}

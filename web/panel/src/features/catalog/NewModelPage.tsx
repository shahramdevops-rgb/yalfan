import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router';
import { fieldCode, toProblem } from '../../shared/api/problem';
import { refusalMessage } from '../../shared/api/refusal';
import { toPersianDigits } from '../../shared/format/digits';
import { errorMessage, t } from '../../shared/i18n/fa';
import { Button } from '../../shared/ui/Button';
import { FormCard } from '../../shared/ui/Card';
import { FormActions, FormFields, FormSection } from '../../shared/ui/Form';
import { Notice } from '../../shared/ui/Notice';
import { PageHeader } from '../../shared/ui/PageHeader';
import { ListSkeleton, LoadError } from '../../shared/ui/QueryStates';
import { Switch } from '../../shared/ui/Switch';
import { TextField } from '../../shared/ui/TextField';
import { BagPicker } from './BagPicker';
import { bagCount, newBags, type Picks } from './bags';
import { catalogListKey, catalogLists } from './catalogListsApi';
import { CategorySelect } from './CategorySelect';
import { productSearchesKey, productsApi } from './productsApi';
import { SizeNamesField } from './SizeNamesField';

interface Errors {
  name?: string;
  sizes?: string;
  form?: string;
}

/**
 * A new model in one form and one save, so a half-entered model is never stored (ADR 0016): its
 * name, an optional category, its sizes, and the colours sewn in each fabric. The description is
 * left to the model's page, because the store is the first to need it (decided 1405/09/03).
 */
export function NewModelPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const fabrics = useQuery({ queryKey: catalogListKey('fabrics'), queryFn: catalogLists.fabrics.list });
  const colours = useQuery({ queryKey: catalogListKey('colours'), queryFn: catalogLists.colours.list });
  const categories = useQuery({ queryKey: catalogListKey('categories'), queryFn: catalogLists.categories.list });

  const [name, setName] = useState('');
  const [categoryId, setCategoryId] = useState<string | null>(null);
  const [hasSizes, setHasSizes] = useState(false);
  const [sizes, setSizes] = useState<string[]>([]);
  const [picks, setPicks] = useState<Picks>([]);
  const [errors, setErrors] = useState<Errors>({});
  const [saving, setSaving] = useState(false);

  const sizeNames = hasSizes ? sizes : [];
  const count = bagCount(picks, sizeNames.length);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (name.trim() === '') {
      setErrors({ name: errorMessage('catalog.name_required') });
      return;
    }

    if (hasSizes && sizes.length === 0) {
      setErrors({ sizes: t('newModel.sizesMissing') });
      return;
    }

    setSaving(true);
    setErrors({});
    try {
      const created = await productsApi.create({
        name,
        description: null,
        categoryId,
        sizes: sizeNames,
        variants: newBags(picks, sizeNames),
      });
      void queryClient.invalidateQueries({ queryKey: productSearchesKey });
      void navigate(`/models/${created.id}`, { replace: true });
    } catch (error) {
      const problem = toProblem(error);
      const nameCode = fieldCode(problem, 'name') ?? (problem.code === 'catalog.name_taken' ? problem.code : undefined);
      setErrors(nameCode ? { name: errorMessage(nameCode) } : { form: refusalMessage(error) });
      setSaving(false);
    }
  }

  const lists = [fabrics, colours, categories];
  const failed = lists.find((list) => list.isError);

  return (
    <div className="flex max-w-3xl flex-col gap-6">
      <PageHeader title={t('newModel.title')} back={{ to: '/models', label: t('models.back') }} />

      <FormCard onSubmit={(event) => void submit(event)} gap={8}>
        <FormFields>
          <TextField
            label={t('newModel.name')}
            value={name}
            onChange={(event) => setName(event.target.value)}
            error={errors.name}
            maxLength={100}
            autoComplete="off"
            autoFocus
          />
          {categories.data && (
            <CategorySelect categories={categories.data} value={categoryId} onChange={setCategoryId} />
          )}
        </FormFields>

        <FormSection title={t('newModel.sizes')}>
          <div className="flex items-center gap-3">
            <Switch checked={hasSizes} onChange={setHasSizes} label={t('newModel.hasSizes')} />
            <span className="text-base text-ink-soft" aria-hidden>
              {t('newModel.hasSizes')}
            </span>
          </div>
          <p className="text-sm text-muted">{hasSizes ? t('newModel.sizesHint') : t('newModel.noSizesHint')}</p>
          {hasSizes && <SizeNamesField sizes={sizes} onChange={setSizes} error={errors.sizes} />}
        </FormSection>

        <FormSection title={t('newModel.bags')}>
          <p className="text-sm text-muted">{t('newModel.bagsHint')}</p>
          {failed && <LoadError error={failed.error} onRetry={() => lists.forEach((list) => void list.refetch())} />}
          {(fabrics.isPending || colours.isPending) && !failed && <ListSkeleton rows={2} />}
          {fabrics.data && colours.data && (
            <BagPicker fabrics={fabrics.data} colours={colours.data} picks={picks} onChange={setPicks} />
          )}
        </FormSection>

        <FormActions>
          <p className="text-sm text-body" aria-live="polite">
            {count === 0 ? t('newModel.noBagsYet') : t('newModel.bagCount', { count: toPersianDigits(String(count)) })}
          </p>
          {errors.form && <Notice tone="danger">{errors.form}</Notice>}
          <Button type="submit" busy={saving} className="self-start">
            {t('newModel.save')}
          </Button>
        </FormActions>
      </FormCard>
    </div>
  );
}

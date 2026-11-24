import { useId } from 'react';
import { byPersianName } from '../../shared/format/persianOrder';
import { t } from '../../shared/i18n/fa';
import type { CatalogListItem } from './catalogListsApi';

interface CategorySelectProps {
  /** The whole list, inactive ones included: the model may already be in one. */
  categories: readonly CatalogListItem[];
  value: string | null;
  onChange: (categoryId: string | null) => void;
  disabled?: boolean;
}

/**
 * A model's category, optional (ADR 0016). Only active categories can be chosen; the one the
 * model is already in stays shown even when it has been deactivated since, marked so (ADR 0022).
 */
export function CategorySelect({ categories, value, onChange, disabled = false }: CategorySelectProps) {
  const id = useId();
  const shown = byPersianName(categories.filter((category) => category.isActive || category.id === value));

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium text-stone-700">
        {t('model.category')}
      </label>
      <select
        id={id}
        value={value ?? ''}
        onChange={(event) => onChange(event.target.value || null)}
        disabled={disabled}
        className="h-12 rounded-lg border border-stone-300 bg-white px-3 text-base outline-none focus:border-brand-600 focus:ring-2 focus:ring-brand-100 disabled:opacity-60 sm:max-w-xs"
      >
        <option value="">{t('model.noCategory')}</option>
        {shown.map((category) => (
          <option key={category.id} value={category.id}>
            {category.isActive ? category.name : t('model.inactiveOption', { name: category.name })}
          </option>
        ))}
      </select>
    </div>
  );
}

import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useId, useState } from 'react';
import { useParams } from 'react-router';
import { fieldCode, toProblem } from '../../shared/api/problem';
import { attempt, refusalMessage } from '../../shared/api/refusal';
import { useSerialWrites, type Write } from '../../shared/api/serialWrites';
import { toPersianDigits } from '../../shared/format/digits';
import { t } from '../../shared/i18n/fa';
import { AddNameForm } from '../../shared/ui/AddNameForm';
import { Button } from '../../shared/ui/Button';
import { Card, CardHeader } from '../../shared/ui/Card';
import { Field, FieldError } from '../../shared/ui/Form';
import { InlineName } from '../../shared/ui/InlineName';
import { NameList } from '../../shared/ui/NameList';
import { Notice } from '../../shared/ui/Notice';
import { PageHeader } from '../../shared/ui/PageHeader';
import { ListSkeleton, LoadError } from '../../shared/ui/QueryStates';
import { moveItem } from '../../shared/ui/sortable';
import { StatusBadge } from '../../shared/ui/StatusBadge';
import { Switch } from '../../shared/ui/Switch';
import { HistoryLink } from '../history/HistoryLink';
import { RateCardSection } from '../production/RateCardSection';
import { BagPicker } from './BagPicker';
import { bagsToAdd, existingPicks, type Picks } from './bags';
import { catalogListKey, catalogLists, type CatalogListItem } from './catalogListsApi';
import { CategorySelect } from './CategorySelect';
import {
  productKey,
  productSearchesKey,
  productsApi,
  profileChange,
  type ProductDetails,
  type ProductProfile,
} from './productsApi';

type Bag = ProductDetails['variants'][number];

/** Sends one change of the model, in turn with the page's other changes (ADR 0025). */
type ModelWrite = Write<ProductDetails>;

const ignore = () => undefined;

const digits = (value: number) => toPersianDigits(String(value));

/** The longest size name the API takes (ProductSize.NameMaxLength). */
const sizeMaxLength = 30;

/**
 * One model: its number, which never changes; its name, category and description, each saved as
 * soon as it is changed, with no save button (decided 1405/09/03); a switch for the model and one
 * for each bag; its contractors' piece rates, right under the name, because they are what a new
 * model still lacks when the operator lands here (ADR 0024); its sizes, renamed in place and
 * dragged into order; and more bags (ADR 0023).
 */
export function ModelPage() {
  const { id = '' } = useParams();
  const queryClient = useQueryClient();
  const key = productKey(id);
  const product = useQuery({ queryKey: key, queryFn: () => productsApi.get(id) });
  // One change after another, each from the last answer: leaving the description by choosing a
  // category starts two saves in one gesture, and each sends the name, description and category.
  // Every answer also refreshes the model lists on their next look.
  const write: ModelWrite = useSerialWrites<ProductDetails>(key, () => {
    void queryClient.invalidateQueries({ queryKey: productSearchesKey });
  });

  if (product.isPending) {
    return <ListSkeleton rows={6} />;
  }

  if (product.isError) {
    const missing = toProblem(product.error).code === 'catalog.product_not_found';
    return (
      <div className="flex max-w-3xl flex-col gap-4">
        <PageHeader back={back} />
        {missing ? (
          <p className="text-body">{t('model.notFound')}</p>
        ) : (
          <LoadError error={product.error} onRetry={() => void product.refetch()} />
        )}
      </div>
    );
  }

  const model = product.data;
  // Keyed by the model, so an open form or picks made for one model never carry over to another
  // when the page goes from model to model without leaving.
  return (
    <div key={model.id} className="flex max-w-3xl flex-col gap-6">
      <PageHeader back={back} actions={<HistoryLink kind="models" id={model.id} />} />
      <Profile product={model} write={write} />
      <RateCardSection productId={model.id} productIsActive={model.isActive} />
      <Sizes product={model} write={write} />
      <Bags product={model} write={write} />
    </div>
  );
}

const back = { to: '/models', label: t('models.back') };

function Profile({ product, write }: { product: ProductDetails; write: ModelWrite }) {
  const categories = useQuery({ queryKey: catalogListKey('categories'), queryFn: catalogLists.categories.list });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();

  async function run(send: () => Promise<ProductDetails>) {
    setBusy(true);
    setError(await attempt(send, ignore));
    setBusy(false);
  }

  // The profile as the last change left it, not as this render saw it.
  const update = (change: Partial<ProductProfile>) => write(profileChange(change));
  const save = (change: Partial<ProductProfile>, field: string) => attempt(() => update(change), ignore, field);

  return (
    <Card>
      <div className="flex items-start gap-4">
        <div className="shrink-0 text-center">
          <p className="text-xs text-muted">{t('model.number')}</p>
          <p className={`text-3xl font-bold ${product.isActive ? 'text-brand-strong' : 'text-faint'}`}>
            {digits(product.number)}
          </p>
        </div>
        <div className="flex min-w-0 flex-1 flex-col gap-1 pt-3">
          <InlineName
            large
            heading
            value={product.name}
            muted={!product.isActive}
            onSave={(name) => save({ name }, 'name')}
          />
        </div>
        <div className="flex shrink-0 flex-col items-center gap-1 pt-1">
          <span className="text-xs text-muted">{t('list.columnActive')}</span>
          <Switch
            checked={product.isActive}
            onChange={(isActive) => void run(() => write(() => productsApi.setActive(product.id, isActive)))}
            label={t('model.activeLabel', { name: product.name })}
            disabled={busy}
          />
        </div>
      </div>

      {!product.isActive && <Notice tone="warning">{t('model.inactiveNote')}</Notice>}

      {categories.data && (
        <CategorySelect
          categories={categories.data}
          value={product.category?.id ?? null}
          disabled={busy}
          onChange={(categoryId) => void run(() => update({ categoryId }))}
        />
      )}

      <Field label={t('model.description')}>
        <InlineName
          multiline
          allowEmpty
          maxLength={1000}
          value={product.description ?? ''}
          label={t('model.description')}
          placeholder={t('model.descriptionEmpty')}
          onSave={(description) => save({ description: description || null }, 'description')}
        />
      </Field>

      {error && <FieldError>{error}</FieldError>}
    </Card>
  );
}

function Sizes({ product, write }: { product: ProductDetails; write: ModelWrite }) {
  const queryClient = useQueryClient();
  const headingId = useId();
  const key = productKey(product.id);
  const [moving, setMoving] = useState(false);
  const [moveError, setMoveError] = useState<string>();

  // Shown in the new order at once; put back if the API refuses it (as the stages, ADR 0022).
  async function move(from: number, to: number) {
    await queryClient.cancelQueries({ queryKey: key });
    const before = queryClient.getQueryData<ProductDetails>(key);
    if (!before) {
      return;
    }

    const sizes = moveItem(before.sizes, from, to);
    queryClient.setQueryData(key, { ...before, sizes });
    setMoving(true);
    setMoveError(undefined);

    try {
      await write(() =>
        productsApi.reorderSizes(
          product.id,
          sizes.map((size) => size.id),
        ),
      );
    } catch (error) {
      // Only the order goes back: whatever another change saved meanwhile stays on the page.
      const order = before.sizes.map((size) => size.id);
      const place = (sizeId: string) => (order.includes(sizeId) ? order.indexOf(sizeId) : order.length);
      queryClient.setQueryData<ProductDetails>(
        key,
        (current) => current && { ...current, sizes: [...current.sizes].sort((a, b) => place(a.id) - place(b.id)) },
      );
      setMoveError(refusalMessage(error, 'sizeIds'));

      // Another user changed the sizes meanwhile: bring the model again, to be dragged again.
      const problem = toProblem(error);
      if ((fieldCode(problem, 'sizeIds') ?? problem.code) === 'catalog.size_order_outdated') {
        void queryClient.invalidateQueries({ queryKey: key });
      }
    } finally {
      setMoving(false);
    }
  }

  const hasBags = product.variants.length > 0;
  const hasSizes = product.sizes.length > 0;
  const addHint = hasBags ? (hasSizes ? t('model.nextSizeHint') : t('model.firstSizeHint')) : undefined;

  return (
    <Card labelledBy={headingId}>
      <CardHeader id={headingId} title={t('model.sizes')} hint={hasSizes ? t('model.sizesHint') : undefined} />

      {hasSizes ? (
        <NameList
          items={product.sizes.map((size) => ({ ...size, isActive: true }))}
          onRename={(sizeId, name) =>
            attempt(() => write(() => productsApi.renameSize(product.id, sizeId, name)), ignore, 'name')
          }
          onMove={product.sizes.length > 1 ? (from, to) => void move(from, to) : undefined}
          moving={moving}
          emptyText=""
        />
      ) : (
        <p className="text-muted">{t('model.noSizes')}</p>
      )}

      {moveError && <Notice tone="danger">{moveError}</Notice>}

      {product.isActive ? (
        <AddNameForm
          label={t('model.addSize')}
          hint={addHint}
          maxLength={sizeMaxLength}
          onAdd={(name) => attempt(() => write(() => productsApi.addSize(product.id, name)), ignore, 'name')}
        />
      ) : (
        <p className="text-sm text-muted">{t('model.inactiveNoAdd')}</p>
      )}
    </Card>
  );
}

function Bags({ product, write }: { product: ProductDetails; write: ModelWrite }) {
  const headingId = useId();
  const fabrics = useQuery({ queryKey: catalogListKey('fabrics'), queryFn: catalogLists.fabrics.list });
  const colours = useQuery({ queryKey: catalogListKey('colours'), queryFn: catalogLists.colours.list });
  const [picks, setPicks] = useState<Picks>([]);
  const [adding, setAdding] = useState(false);
  const [addError, setAddError] = useState<string>();

  const toAdd = bagsToAdd(
    picks,
    product.variants,
    product.sizes.map((size) => size.id),
  );

  async function add() {
    setAdding(true);
    setAddError(
      await attempt(
        () => write(() => productsApi.addVariants(product.id, toAdd)),
        () => setPicks([]),
      ),
    );
    setAdding(false);
  }

  return (
    <Card labelledBy={headingId}>
      <CardHeader
        id={headingId}
        title={
          <>
            {t('model.bags')}
            {product.variants.length > 0 && (
              <span className="ms-2 text-base font-normal text-muted">
                {t('models.bagCount', { count: digits(product.variants.length) })}
              </span>
            )}
          </>
        }
      />

      {product.variants.length === 0 ? (
        <p className="text-muted">{t('model.noBags')}</p>
      ) : (
        <ul className="divide-y divide-line-soft">
          {product.variants.map((bag) => (
            <BagRow key={bag.id} productId={product.id} bag={bag} write={write} />
          ))}
        </ul>
      )}

      {!product.isActive ? (
        <p className="text-sm text-muted">{t('model.inactiveNoAdd')}</p>
      ) : (
        <div className="flex flex-col gap-4 border-t border-line-soft pt-5">
          <div>
            <h3 className="font-bold">{t('model.addBags')}</h3>
            <p className="mt-1 text-sm text-muted">
              {product.sizes.length > 0 ? t('model.addBagsSizesHint') : t('model.addBagsHint')}
            </p>
          </div>
          {fabrics.data && colours.data ? (
            <BagPicker
              fabrics={fabrics.data}
              colours={colours.data}
              picks={picks}
              onChange={setPicks}
              locked={existingPicks(product.variants)}
            />
          ) : (
            <ListSkeleton rows={2} />
          )}
          {addError && <Notice tone="danger">{addError}</Notice>}
          <Button className="self-start" busy={adding} disabled={toAdd.length === 0} onClick={() => void add()}>
            {toAdd.length === 0 ? t('model.addBagsNone') : t('model.addBagsButton', { count: digits(toAdd.length) })}
          </Button>
        </div>
      )}
    </Card>
  );
}

function BagRow({ productId, bag, write }: { productId: string; bag: Bag; write: ModelWrite }) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();

  async function toggle(isActive: boolean) {
    setBusy(true);
    setError(await attempt(() => write(() => productsApi.setVariantActive(productId, bag.id, isActive)), ignore));
    setBusy(false);
  }

  const named = (item: CatalogListItem) => (item.isActive ? item.name : t('model.inactiveOption', { name: item.name }));
  const parts = [named(bag.fabric), named(bag.colour), bag.size?.name].filter(Boolean);

  return (
    <li className="flex flex-col gap-1 py-2">
      <div className="flex min-h-10 items-center gap-3">
        {/* The code reads left to right, as it is written on paper and on a label. */}
        <span dir="ltr" className={`w-16 shrink-0 text-end font-bold ${bag.isActive ? 'text-ink' : 'text-faint'}`}>
          {toPersianDigits(bag.sku)}
        </span>
        <span className={`min-w-0 flex-1 text-base ${bag.isActive ? 'text-ink-soft' : 'text-faint'}`}>
          {parts.join(t('models.separator'))}
        </span>
        {!bag.isActive && <StatusBadge>{t('list.inactive')}</StatusBadge>}
        <Switch
          checked={bag.isActive}
          onChange={(isActive) => void toggle(isActive)}
          label={t('model.bagActiveLabel', { sku: toPersianDigits(bag.sku) })}
          disabled={busy}
        />
      </div>
      {error && <FieldError>{error}</FieldError>}
    </li>
  );
}

import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useId, useState, type ReactNode } from 'react';
import { Link, useParams } from 'react-router';
import { fieldCode, toProblem } from '../../shared/api/problem';
import { attempt, refusalMessage } from '../../shared/api/refusal';
import { toPersianDigits } from '../../shared/format/digits';
import { t } from '../../shared/i18n/fa';
import { AddNameForm } from '../../shared/ui/AddNameForm';
import { Button } from '../../shared/ui/Button';
import { InlineName } from '../../shared/ui/InlineName';
import { NameList } from '../../shared/ui/NameList';
import { ListSkeleton, LoadError } from '../../shared/ui/QueryStates';
import { moveItem } from '../../shared/ui/sortable';
import { Switch } from '../../shared/ui/Switch';
import { BagPicker } from './BagPicker';
import { bagsToAdd, existingPicks, type Picks } from './bags';
import { catalogListKey, catalogLists, type CatalogListItem } from './catalogListsApi';
import { CategorySelect } from './CategorySelect';
import { productKey, productSearchesKey, productsApi, profileOf, type ProductDetails } from './productsApi';

type Bag = ProductDetails['variants'][number];

/** Saves the answer of a change: the page shows it, and the model lists show it on their next look. */
type Put = (product: ProductDetails) => void;

const digits = (value: number) => toPersianDigits(String(value));

/** The longest size name the API takes (ProductSize.NameMaxLength). */
const sizeMaxLength = 30;

/**
 * One model: its number, which never changes; its name, category and description, each saved as
 * soon as it is changed, with no save button (decided 1405/09/03); a switch for the model and one
 * for each bag; its sizes, renamed in place and dragged into order; and more bags (ADR 0023).
 */
export function ModelPage() {
  const { id = '' } = useParams();
  const queryClient = useQueryClient();
  const key = productKey(id);
  const product = useQuery({ queryKey: key, queryFn: () => productsApi.get(id) });

  const put: Put = (details) => {
    queryClient.setQueryData(key, details);
    void queryClient.invalidateQueries({ queryKey: productSearchesKey });
  };

  if (product.isPending) {
    return <ListSkeleton rows={6} />;
  }

  if (product.isError) {
    const missing = toProblem(product.error).code === 'catalog.product_not_found';
    return (
      <div className="flex max-w-3xl flex-col gap-4">
        <BackLink />
        {missing ? (
          <p className="text-stone-600">{t('model.notFound')}</p>
        ) : (
          <LoadError error={product.error} onRetry={() => void product.refetch()} />
        )}
      </div>
    );
  }

  const model = product.data;
  return (
    <div className="flex max-w-3xl flex-col gap-6">
      <BackLink />
      <Profile product={model} put={put} />
      <Sizes product={model} put={put} />
      <Bags product={model} put={put} />
    </div>
  );
}

function BackLink() {
  return (
    <Link to="/models" className="self-start text-sm text-brand-700 hover:underline">
      {t('models.back')}
    </Link>
  );
}

function Card({ children, labelledBy }: { children: ReactNode; labelledBy?: string }) {
  return (
    <section
      aria-labelledby={labelledBy}
      className="flex flex-col gap-5 rounded-2xl bg-white p-4 shadow-sm ring-1 ring-stone-200 sm:p-6"
    >
      {children}
    </section>
  );
}

function Profile({ product, put }: { product: ProductDetails; put: Put }) {
  const categories = useQuery({ queryKey: catalogListKey('categories'), queryFn: catalogLists.categories.list });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();

  async function run(send: () => Promise<ProductDetails>) {
    setBusy(true);
    setError(await attempt(send, put));
    setBusy(false);
  }

  const save = (change: Parameters<typeof profileOf>[1], field: string) =>
    attempt(() => productsApi.update(product.id, profileOf(product, change)), put, field);

  return (
    <Card>
      <div className="flex items-start gap-4">
        <div className="shrink-0 text-center">
          <p className="text-xs text-stone-500">{t('model.number')}</p>
          <p className={`text-3xl font-bold ${product.isActive ? 'text-brand-800' : 'text-stone-400'}`}>
            {digits(product.number)}
          </p>
        </div>
        <div className="flex min-w-0 flex-1 flex-col gap-1 pt-3">
          <InlineName large value={product.name} muted={!product.isActive} onSave={(name) => save({ name }, 'name')} />
        </div>
        <div className="flex shrink-0 flex-col items-center gap-1 pt-1">
          <span className="text-xs text-stone-500">{t('list.columnActive')}</span>
          <Switch
            checked={product.isActive}
            onChange={(isActive) => void run(() => productsApi.setActive(product.id, isActive))}
            label={t('model.activeLabel', { name: product.name })}
            disabled={busy}
          />
        </div>
      </div>

      {!product.isActive && (
        <p className="rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-900">{t('model.inactiveNote')}</p>
      )}

      {categories.data && (
        <CategorySelect
          categories={categories.data}
          value={product.category?.id ?? null}
          disabled={busy}
          onChange={(categoryId) => void run(() => productsApi.update(product.id, profileOf(product, { categoryId })))}
        />
      )}

      <div className="flex flex-col gap-1.5">
        <p className="text-sm font-medium text-stone-700">{t('model.description')}</p>
        <InlineName
          multiline
          allowEmpty
          maxLength={1000}
          value={product.description ?? ''}
          label={t('model.description')}
          placeholder={t('model.descriptionEmpty')}
          onSave={(description) => save({ description: description || null }, 'description')}
        />
      </div>

      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </Card>
  );
}

function Sizes({ product, put }: { product: ProductDetails; put: Put }) {
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
      put(
        await productsApi.reorderSizes(
          product.id,
          sizes.map((size) => size.id),
        ),
      );
    } catch (error) {
      queryClient.setQueryData(key, before);
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
      <div>
        <h2 id={headingId} className="text-lg font-bold">
          {t('model.sizes')}
        </h2>
        {hasSizes && <p className="mt-1 text-sm text-stone-500">{t('model.sizesHint')}</p>}
      </div>

      {hasSizes ? (
        <NameList
          items={product.sizes.map((size) => ({ ...size, isActive: true }))}
          onRename={(sizeId, name) => attempt(() => productsApi.renameSize(product.id, sizeId, name), put, 'name')}
          onMove={product.sizes.length > 1 ? (from, to) => void move(from, to) : undefined}
          moving={moving}
          emptyText=""
        />
      ) : (
        <p className="text-stone-500">{t('model.noSizes')}</p>
      )}

      {moveError && (
        <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-800">
          {moveError}
        </p>
      )}

      {product.isActive ? (
        <AddNameForm
          label={t('model.addSize')}
          hint={addHint}
          maxLength={sizeMaxLength}
          onAdd={(name) => attempt(() => productsApi.addSize(product.id, name), put, 'name')}
        />
      ) : (
        <p className="text-sm text-stone-500">{t('model.inactiveNoAdd')}</p>
      )}
    </Card>
  );
}

function Bags({ product, put }: { product: ProductDetails; put: Put }) {
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
        () => productsApi.addVariants(product.id, toAdd),
        (details) => {
          put(details);
          setPicks([]);
        },
      ),
    );
    setAdding(false);
  }

  return (
    <Card labelledBy={headingId}>
      <h2 id={headingId} className="text-lg font-bold">
        {t('model.bags')}
        {product.variants.length > 0 && (
          <span className="ms-2 text-base font-normal text-stone-500">
            {t('models.bagCount', { count: digits(product.variants.length) })}
          </span>
        )}
      </h2>

      {product.variants.length === 0 ? (
        <p className="text-stone-500">{t('model.noBags')}</p>
      ) : (
        <ul className="divide-y divide-stone-100">
          {product.variants.map((bag) => (
            <BagRow key={bag.id} productId={product.id} bag={bag} put={put} />
          ))}
        </ul>
      )}

      {!product.isActive ? (
        <p className="text-sm text-stone-500">{t('model.inactiveNoAdd')}</p>
      ) : (
        <div className="flex flex-col gap-4 border-t border-stone-100 pt-5">
          <div>
            <h3 className="font-bold">{t('model.addBags')}</h3>
            <p className="mt-1 text-sm text-stone-500">
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
          {addError && (
            <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-800">
              {addError}
            </p>
          )}
          <Button className="self-start" busy={adding} disabled={toAdd.length === 0} onClick={() => void add()}>
            {toAdd.length === 0 ? t('model.addBagsNone') : t('model.addBagsButton', { count: digits(toAdd.length) })}
          </Button>
        </div>
      )}
    </Card>
  );
}

function BagRow({ productId, bag, put }: { productId: string; bag: Bag; put: Put }) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();

  async function toggle(isActive: boolean) {
    setBusy(true);
    setError(await attempt(() => productsApi.setVariantActive(productId, bag.id, isActive), put));
    setBusy(false);
  }

  const named = (item: CatalogListItem) => (item.isActive ? item.name : t('model.inactiveOption', { name: item.name }));
  const parts = [named(bag.fabric), named(bag.colour), bag.size?.name].filter(Boolean);

  return (
    <li className="flex flex-col gap-1 py-2">
      <div className="flex min-h-10 items-center gap-3">
        {/* The code reads left to right, as it is written on paper and on a label. */}
        <span
          dir="ltr"
          className={`w-16 shrink-0 text-end font-bold ${bag.isActive ? 'text-stone-900' : 'text-stone-400'}`}
        >
          {toPersianDigits(bag.sku)}
        </span>
        <span className={`min-w-0 flex-1 text-base ${bag.isActive ? 'text-stone-800' : 'text-stone-400'}`}>
          {parts.join(t('models.separator'))}
        </span>
        {!bag.isActive && (
          <span className="shrink-0 rounded-full bg-stone-100 px-2 py-0.5 text-xs text-stone-500">
            {t('list.inactive')}
          </span>
        )}
        <Switch
          checked={bag.isActive}
          onChange={(isActive) => void toggle(isActive)}
          label={t('model.bagActiveLabel', { sku: toPersianDigits(bag.sku) })}
          disabled={busy}
        />
      </div>
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </li>
  );
}

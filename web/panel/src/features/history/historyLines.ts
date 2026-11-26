import { formatDay } from '../../shared/format/dates';
import { toPersianDigits } from '../../shared/format/digits';
import { formatMobile } from '../../shared/format/mobile';
import { formatRial } from '../../shared/format/money';
import { t, type MessageKey } from '../../shared/i18n/fa';
import type { HistoryChange, HistoryField, HistoryFieldName } from './historyApi';

/** One line of a save in the history, in the workshop's words. */
export interface HistoryLine {
  text: string;
  /** A Rial amount, shown under the line with its words (ADR 0002 point 9). */
  amount?: string;
  /** What a new model or person was made with, or the bags folded under one line. */
  details?: string[];
  /** The details stay closed until tapped: a new model can bring hundreds of bags in one save. */
  folded?: boolean;
}

/** More bag lines than this in one save are folded under one line. */
export const foldBagsAfter = 6;

/**
 * Kept left to right inside a Persian line, as the model's and the lists' pages show them: a bag's
 * code or a mobile would otherwise read in reverse groups («۲۲۳۳ ۱۱۱ ۰۹۱۲»). A Unicode isolate,
 * because the lines are text.
 */
export const leftToRight = (text: string) => `\u2066${text}\u2069`;

const fieldLabels: Record<HistoryFieldName, MessageKey> = {
  name: 'history.field.name',
  description: 'model.description',
  category: 'model.category',
  active: 'history.field.active',
  position: 'history.field.position',
  fabric: 'history.field.fabric',
  colour: 'history.field.colour',
  size: 'history.field.size',
  amount: 'history.field.amount',
  startsOn: 'history.field.startsOn',
  fullName: 'parties.fullName',
  mobileNumber: 'parties.mobile',
  notes: 'parties.notes',
  startDate: 'parties.startDate',
  iban: 'history.field.iban',
};

type RecordPart = 'model' | 'contractor' | 'worker';

/** A line, and whether it is about a bag or the order of the sizes, to fold or merge afterwards. */
interface Drafted extends HistoryLine {
  about?: 'bag' | 'sizeOrder';
}

/**
 * The lines of one save, in the API's order: the record itself, then its sizes, bags, rates and
 * stages. The sizes dragged into a new order become one line; more than a few bags fold under one.
 */
export function describeSave(changes: readonly HistoryChange[]): HistoryLine[] {
  const drafted = changes.flatMap(describeChange);

  const lines: HistoryLine[] = [];
  const bags = drafted.filter((line) => line.about === 'bag');
  const foldBags = bags.length > foldBagsAfter;
  let sizeOrderShown = false;
  let bagsShown = false;

  for (const { about, ...line } of drafted) {
    if (about === 'sizeOrder') {
      if (!sizeOrderShown) {
        lines.push(line);
        sizeOrderShown = true;
      }
    } else if (about === 'bag' && foldBags) {
      if (!bagsShown) {
        lines.push({ text: bagsSummary(changes, bags.length), details: bags.map((bag) => bag.text), folded: true });
        bagsShown = true;
      }
    } else {
      lines.push(line);
    }
  }

  return lines;
}

function describeChange(change: HistoryChange): Drafted[] {
  switch (change.part) {
    case 'model':
    case 'contractor':
    case 'worker':
      return describeRecord(change.part, change);
    case 'size':
      return describeSize(change);
    case 'bag':
      return describeBag(change).map((text) => ({ text, about: 'bag' }));
    case 'rate':
      return [describeRate(change)];
    case 'stage':
      return [
        {
          text: t(change.action === 'deleted' ? 'history.stageRemoved' : 'history.stageAdded', {
            name: change.label ?? '',
          }),
        },
      ];
  }
}

function describeRecord(part: RecordPart, change: HistoryChange): Drafted[] {
  if (change.action === 'created') {
    return [
      {
        text: t(`history.created.${part}`),
        details: change.fields.map((field) =>
          field.name === 'iban'
            ? t('history.ibanSet')
            : t('history.value', { field: label(field.name), value: shown(field.name, field.new) }),
        ),
      },
    ];
  }

  return change.fields.map((field) => {
    if (field.name === 'active') {
      return { text: t(field.new === 'true' ? `history.on.${part}` : `history.off.${part}`) };
    }

    if (field.name === 'iban') {
      return { text: ibanLine(field) };
    }

    return { text: changed(field) };
  });
}

function describeSize(change: HistoryChange): Drafted[] {
  const name = (field: HistoryFieldName) => change.fields.find((f) => f.name === field);

  if (change.action === 'created') {
    return [{ text: t('history.sizeAdded', { name: name('name')?.new ?? change.label ?? '' }) }];
  }

  return change.fields.map((field) => {
    if (field.name === 'position') {
      return { text: t('history.sizesReordered'), about: 'sizeOrder' };
    }

    if (field.name === 'name') {
      return { text: t('history.sizeRenamed', { old: field.old ?? '', new: field.new ?? '' }) };
    }

    return { text: t('history.ofSize', { name: change.label ?? '', change: changed(field) }) };
  });
}

function describeBag(change: HistoryChange): string[] {
  const code = leftToRight(toPersianDigits(change.label ?? ''));

  if (change.action === 'created') {
    // Fabric, colour and the size if it has one, in that order, as the bag is named on its page.
    const made = (['fabric', 'colour', 'size'] as const)
      .map((name) => change.fields.find((field) => field.name === name)?.new)
      .filter((value): value is string => Boolean(value));
    return [t('history.bagAdded', { code, made: made.join('، ') })];
  }

  return change.fields.map((field) => {
    if (field.name === 'active') {
      return t(field.new === 'true' ? 'history.on.bag' : 'history.off.bag', { code });
    }

    if (field.name === 'size' && field.old === null) {
      return t('history.bagSized', { code, size: field.new ?? '' });
    }

    return t('history.ofBag', { code, change: changed(field) });
  });
}

function describeRate(change: HistoryChange): Drafted {
  const startsOn = change.fields.find((field) => field.name === 'startsOn')?.new;
  const amount = change.fields.find((field) => field.name === 'amount')?.new;
  const text = t('history.rateAdded', {
    stage: change.label ?? '',
    date: startsOn ? formatDay(startsOn) : '',
  });

  return amount ? { text, amount } : { text };
}

/** Set, replaced or removed; an entry from before ADR 0027 knows only that it changed. */
function ibanLine(field: HistoryField): string {
  if (field.old === 'false') {
    return t('history.ibanSet');
  }

  if (field.new === 'false') {
    return t('history.ibanRemoved');
  }

  return t('history.ibanChanged');
}

function bagsSummary(changes: readonly HistoryChange[], count: number): string {
  const bags = changes.filter((change) => change.part === 'bag');
  const allNew = bags.every((change) => change.action === 'created');
  return t(allNew ? 'history.bagsAdded' : 'history.bagsChanged', { count: toPersianDigits(String(count)) });
}

/** «نام: کیف دستی ← کیف دستی چرمی»: the old value, then the new, read from the right. */
function changed(field: HistoryField): string {
  return t('history.changed', {
    field: label(field.name),
    old: shown(field.name, field.old),
    new: shown(field.name, field.new),
  });
}

function label(name: HistoryFieldName): string {
  return t(fieldLabels[name]);
}

/** A value as the panel shows it elsewhere; nothing is «—». */
function shown(name: HistoryFieldName, value: string | null): string {
  if (value === null || value === '') {
    return t('history.none');
  }

  switch (name) {
    case 'mobileNumber':
      return leftToRight(formatMobile(value));
    case 'startDate':
    case 'startsOn':
      return formatDay(value);
    case 'amount':
      return formatRial(value as `${number}`);
    case 'active':
      return t(value === 'true' ? 'list.columnActive' : 'list.inactive');
    case 'position':
      return toPersianDigits(value);
    default:
      return value;
  }
}

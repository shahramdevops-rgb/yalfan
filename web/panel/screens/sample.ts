// The sample data the screenshots are taken with (ADR 0028). Made up, like the workshop's own
// words but no real person: the names are common ones and every mobile is 0912 000 0xxx. Typed by
// the contract, so a change to the API that these answers no longer fit fails the type check.
import type { components } from '../src/shared/api/schema';

type Schemas = components['schemas'];

/** A fixed id per sample record, so the addresses in the tests stay the same. */
const id = (n: number) => `00000000-0000-4000-8000-${String(n).padStart(12, '0')}`;

/** The frozen moment of every screenshot: Wednesday 1405/09/04, ten in the morning in Tehran. */
export const now = new Date('2026-11-25T06:30:00Z');
export const today = '2026-11-25';

const option = (item: Schemas['CatalogListItem']): Schemas['CatalogOptionSummary'] => ({
  id: item.id,
  name: item.name,
});

export const owner: Schemas['UserSummary'] = { id: id(1), fullName: 'رضا محمدی', role: 'owner' };

export const session: Schemas['SessionResponse'] = {
  accessToken: 'screens-sample-token',
  accessTokenExpiresAt: '2026-11-25T06:40:00Z',
  user: owner,
};

// Stages in the order of the work (ADR 0018); one switched off.
const cutting = { id: id(201), name: 'برش', isActive: true };
const sewing = { id: id(202), name: 'دوخت', isActive: true };
const fitting = { id: id(203), name: 'یراق‌کاری', isActive: true };
const packing = { id: id(204), name: 'بسته‌بندی', isActive: true };
const printing = { id: id(205), name: 'چاپ', isActive: false };
export const stages: Schemas['StageSummary'][] = [cutting, sewing, fitting, printing, packing];

const canvas = { id: id(301), name: 'برزنت', isActive: true };
const cotton = { id: id(302), name: 'کتان', isActive: true };
const leatherette = { id: id(303), name: 'چرم مصنوعی', isActive: true };
const denim = { id: id(304), name: 'جین', isActive: true };
const velvet = { id: id(305), name: 'مخمل', isActive: false };
export const fabrics: Schemas['CatalogListItem'][] = [canvas, denim, leatherette, cotton, velvet];

const black = { id: id(401), name: 'مشکی', isActive: true };
const navy = { id: id(402), name: 'سرمه‌ای', isActive: true };
const brown = { id: id(403), name: 'قهوه‌ای', isActive: true };
const grey = { id: id(404), name: 'طوسی', isActive: true };
const crimson = { id: id(405), name: 'زرشکی', isActive: true };
const orange = { id: id(406), name: 'نارنجی', isActive: false };
export const colours: Schemas['CatalogListItem'][] = [crimson, navy, grey, brown, black, orange];

const backpacks = { id: id(501), name: 'کوله‌پشتی', isActive: true };
const handbags = { id: id(502), name: 'کیف دستی', isActive: true };
const schoolBags = { id: id(503), name: 'کیف مدرسه', isActive: true };
const laptopBags = { id: id(504), name: 'کیف لپ‌تاپ', isActive: true };
export const categories: Schemas['CatalogListItem'][] = [backpacks, handbags, schoolBags, laptopBags];

const small = { id: id(601), name: 'کوچک' };
const large = { id: id(602), name: 'بزرگ' };

const bag = (
  number: number,
  fabric: Schemas['CatalogListItem'],
  colour: Schemas['CatalogListItem'],
  size: Schemas['CatalogOptionSummary'],
  isActive = true,
): Schemas['VariantDetails'] => ({ id: id(7000 + number), sku: `101-${number}`, fabric, colour, size, isActive });

/** Model 101 with two sizes, two fabrics and one bag switched off: the page with the most on it. */
export const model: Schemas['ProductDetails'] = {
  id: id(101),
  number: 101,
  name: 'کوله شهری',
  description: 'کوله سبک با جیب لپ‌تاپ و بند قابل تنظیم',
  category: backpacks,
  isActive: true,
  sizes: [small, large],
  variants: [
    bag(1, canvas, black, small),
    bag(2, canvas, black, large),
    bag(3, canvas, navy, small),
    bag(4, canvas, navy, large),
    bag(5, cotton, grey, small),
    bag(6, cotton, grey, large, false),
  ],
};

const listedModel = (
  number: number,
  name: string,
  category: Schemas['CatalogListItem'] | null,
  variantCount: number,
  isActive = true,
): Schemas['ProductListItem'] => ({
  id: id(number),
  number,
  name,
  category: category && option(category),
  variantCount,
  isActive,
});

export const models: Schemas['PagedListOfProductListItem'] = {
  items: [
    listedModel(101, model.name, backpacks, model.variants.length),
    listedModel(102, 'کیف مدرسه‌ای دو زیپ', schoolBags, 8),
    listedModel(103, 'کیف لپ‌تاپ اداری', laptopBags, 3),
    listedModel(104, 'کیف دستی زنانه', handbags, 10),
    listedModel(105, 'ساک ورزشی', null, 4),
    listedModel(106, 'کوله کوهنوردی', backpacks, 0, false),
  ],
  totalCount: 6,
  page: 1,
  pageSize: 20,
};

export const rateCard: Schemas['RateCardDetails'] = {
  productId: model.id,
  productNumber: model.number,
  productName: model.name,
  on: today,
  stages: [
    { stage: cutting, amount: 150_000, startsOn: '2026-11-01' },
    { stage: sewing, amount: 420_000, startsOn: '2026-11-15' },
    { stage: fitting, amount: 85_500, startsOn: '2026-11-01' },
    { stage: packing, amount: null, startsOn: null },
  ],
};

export const rateHistory: Schemas['PieceRateEntry'][] = [
  {
    id: id(801),
    stage: sewing,
    amount: 420_000,
    startsOn: '2026-11-15',
    enteredAt: '2026-11-14T07:10:00Z',
    isReplaced: false,
  },
  {
    id: id(802),
    stage: sewing,
    amount: 380_000,
    startsOn: '2026-11-01',
    enteredAt: '2026-10-30T08:00:00Z',
    isReplaced: false,
  },
  {
    id: id(803),
    stage: cutting,
    amount: 150_000,
    startsOn: '2026-11-01',
    enteredAt: '2026-10-30T08:02:00Z',
    isReplaced: false,
  },
  {
    id: id(804),
    stage: fitting,
    amount: 85_500,
    startsOn: '2026-11-01',
    enteredAt: '2026-10-30T08:05:00Z',
    isReplaced: false,
  },
  {
    id: id(805),
    stage: fitting,
    amount: 58_500,
    startsOn: '2026-11-01',
    enteredAt: '2026-10-30T08:04:00Z',
    isReplaced: true,
  },
];

const maskedIban = (last: string) => `IR** **** **** **** **** **${last.slice(0, 2)} ${last.slice(2)}`;

type ContractorItem = Schemas['ContractorListItem'];
const contractor = (
  n: number,
  fullName: string,
  stageList: Schemas['StageSummary'][],
  iban: string | null,
  isActive = true,
): ContractorItem => ({
  id: id(900 + n),
  fullName,
  mobileNumber: `09120000${String(100 + n)}`,
  stages: stageList,
  maskedIban: iban && maskedIban(iban),
  isActive,
});

export const contractorList: ContractorItem[] = [
  contractor(1, 'زهرا حسینی', [sewing], '1001'),
  contractor(2, 'علی رضایی', [sewing, fitting], '2002'),
  contractor(3, 'فاطمه کریمی', [cutting], null),
  contractor(4, 'محمد جعفری', [fitting, printing], '4004'),
  contractor(5, 'مهدی نوری', [sewing], null, false),
];

export const contractorDetails: Schemas['ContractorDetails'] = {
  ...contractorList[1]!,
  notes: 'فقط کوله و کیف مدرسه می‌دوزد؛ چرخ خودش را دارد.',
  createdAt: '2026-11-20T09:15:00Z',
};

type WorkerItem = Schemas['WorkerListItem'];
const worker = (
  n: number,
  fullName: string,
  stageList: Schemas['StageSummary'][],
  startDate: string,
  iban: string | null,
  isActive = true,
): WorkerItem => ({
  id: id(950 + n),
  fullName,
  mobileNumber: `09120000${String(150 + n)}`,
  stages: stageList,
  startDate,
  maskedIban: iban && maskedIban(iban),
  isActive,
});

export const workerList: WorkerItem[] = [
  worker(1, 'حسن مرادی', [cutting], '2019-03-21', '5005'),
  worker(2, 'مریم احمدی', [sewing, packing], '2022-09-23', '6006'),
  worker(3, 'سعید امینی', [packing], '2025-06-22', null),
  worker(4, 'نرگس صادقی', [cutting, sewing], '2018-01-10', '7007', false),
];

export const workerDetails: Schemas['WorkerDetails'] = {
  ...workerList[1]!,
  notes: null,
  createdAt: '2026-11-21T10:00:00Z',
};

const number = (n: number, isActive = true): Schemas['LoginNumberDetails'] => ({
  id: id(1100 + n),
  mobileNumber: `09120000${String(n).padStart(3, '0')}`,
  isActive,
});

export const users: Schemas['StaffUserDetails'][] = [
  {
    id: owner.id,
    fullName: owner.fullName,
    role: 'owner',
    isActive: true,
    isOwnerAccount: true,
    loginNumbers: [number(1), number(2)],
    createdAt: '2026-11-15T05:00:00Z',
  },
  {
    id: id(2),
    fullName: 'پشتیبان سامانه',
    role: 'owner',
    isActive: true,
    isOwnerAccount: true,
    loginNumbers: [number(3)],
    createdAt: '2026-11-15T05:00:00Z',
  },
  {
    id: id(3),
    fullName: 'امیر کاظمی',
    role: 'manager',
    isActive: true,
    isOwnerAccount: false,
    loginNumbers: [number(4)],
    createdAt: '2026-11-16T06:00:00Z',
  },
  {
    id: id(4),
    fullName: 'لیلا رحیمی',
    role: 'operator',
    isActive: true,
    isOwnerAccount: false,
    loginNumbers: [number(5)],
    createdAt: '2026-11-16T06:05:00Z',
  },
  {
    id: id(5),
    fullName: 'کاوه شریفی',
    role: 'operator',
    isActive: false,
    isOwnerAccount: false,
    loginNumbers: [number(6, false)],
    createdAt: '2026-11-16T06:10:00Z',
  },
];

/** The change history of contractor 2: made, corrected, an IBAN set and replaced, a stage ticked. */
export const contractorHistory: Schemas['HistoryPage'] = {
  saves: [
    {
      at: '2026-11-24T11:20:00Z',
      by: owner.fullName,
      changes: [
        { part: 'contractor', action: 'updated', label: null, fields: [{ name: 'iban', old: 'true', new: 'true' }] },
      ],
    },
    {
      at: '2026-11-22T08:45:00Z',
      by: 'لیلا رحیمی',
      changes: [
        { part: 'stage', action: 'created', label: fitting.name, fields: [] },
        {
          part: 'contractor',
          action: 'updated',
          label: null,
          fields: [{ name: 'mobileNumber', old: '09120000199', new: contractorDetails.mobileNumber }],
        },
      ],
    },
    {
      at: '2026-11-20T09:15:00Z',
      by: 'لیلا رحیمی',
      changes: [
        {
          part: 'contractor',
          action: 'created',
          label: null,
          fields: [
            { name: 'fullName', old: null, new: contractorDetails.fullName },
            { name: 'mobileNumber', old: null, new: '09120000199' },
            { name: 'iban', old: 'false', new: 'true' },
          ],
        },
        { part: 'stage', action: 'created', label: sewing.name, fields: [] },
      ],
    },
  ],
  before: null,
};

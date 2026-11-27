import type { Page } from '@playwright/test';
import * as sample from './sample';

interface Answer {
  status?: number;
  body?: unknown;
}

type Handler = (match: RegExpExecArray, query: URLSearchParams) => Answer;

/** What the API answers to a record it does not have: a ProblemDetails with its code (ADR 0011). */
const notFound = (code: string): Answer => ({ status: 404, body: { code } });

const paged = <T>(items: T[]) => ({ items, totalCount: items.length, page: 1, pageSize: 20 });

/** «isActive=true» is how a list asks for the active people only (ADR 0025). */
const activeOnly = <T extends { isActive: boolean }>(items: T[], query: URLSearchParams) =>
  query.get('isActive') === 'true' ? items.filter((item) => item.isActive) : items;

function routes(signedIn: boolean): [method: string, path: RegExp, handler: Handler][] {
  const uuid = '([0-9a-f-]{36})';
  return [
    [
      'POST',
      /^\/panel\/auth\/refresh$/,
      () => (signedIn ? { body: sample.session } : { status: 401, body: { code: 'identity.refresh_invalid' } }),
    ],
    ['GET', /^\/panel\/auth\/me$/, () => ({ body: sample.owner })],
    ['GET', /^\/panel\/catalog\/categories$/, () => ({ body: sample.categories })],
    ['GET', /^\/panel\/catalog\/fabrics$/, () => ({ body: sample.fabrics })],
    ['GET', /^\/panel\/catalog\/colours$/, () => ({ body: sample.colours })],
    ['GET', /^\/panel\/catalog\/products$/, () => ({ body: sample.models })],
    [
      'GET',
      new RegExp(`^/panel/catalog/products/${uuid}$`),
      ([, id]) => (id === sample.model.id ? { body: sample.model } : notFound('catalog.product_not_found')),
    ],
    ['GET', /^\/panel\/production\/stages$/, () => ({ body: sample.stages })],
    ['GET', /^\/panel\/production\/rates$/, () => ({ body: sample.rateCard })],
    ['GET', /^\/panel\/production\/rates\/history$/, () => ({ body: sample.rateHistory })],
    [
      'GET',
      /^\/panel\/parties\/contractors$/,
      (_, query) => ({ body: paged(activeOnly(sample.contractorList, query)) }),
    ],
    [
      'GET',
      new RegExp(`^/panel/parties/contractors/${uuid}$`),
      ([, id]) =>
        id === sample.contractorDetails.id
          ? { body: sample.contractorDetails }
          : notFound('parties.contractor_not_found'),
    ],
    ['GET', /^\/panel\/parties\/workers$/, (_, query) => ({ body: paged(activeOnly(sample.workerList, query)) })],
    [
      'GET',
      new RegExp(`^/panel/parties/workers/${uuid}$`),
      ([, id]) =>
        id === sample.workerDetails.id ? { body: sample.workerDetails } : notFound('parties.worker_not_found'),
    ],
    ['GET', /^\/panel\/users$/, () => ({ body: sample.users })],
    // The writes the behaviour tests make (overlays.spec.ts). Each answers what the API would,
    // without keeping anything: a later read answers the sample data again.
    [
      'DELETE',
      new RegExp(`^/panel/parties/contractors/${uuid}/iban$`),
      ([, id]) =>
        id === sample.contractorDetails.id
          ? { body: { ...sample.contractorDetails, maskedIban: null } }
          : notFound('parties.contractor_not_found'),
    ],
    ['POST', /^\/panel\/users$/, () => ({ body: sample.newUser })],
    [
      'POST',
      new RegExp(`^/panel/users/${uuid}/numbers/${uuid}/deactivate$`),
      ([, userId, numberId]) => {
        const user = sample.users.find((each) => each.id === userId);
        return user
          ? {
              body: {
                ...user,
                loginNumbers: user.loginNumbers.map((each) =>
                  each.id === numberId ? { ...each, isActive: false } : each,
                ),
              },
            }
          : notFound('identity.user_not_found');
      },
    ],
    ['POST', /^\/panel\/auth\/logout$/, () => ({ status: 204 })],
    [
      'GET',
      new RegExp(`^/panel/history/contractors/${uuid}$`),
      ([, id]) =>
        id === sample.contractorDetails.id
          ? { body: sample.contractorHistory }
          : notFound('parties.contractor_not_found'),
    ],
  ];
}

/**
 * Answers every request to the API from the sample data, in the browser, so a screenshot needs
 * no database, SMS or backend and every run sees the same page (ADR 0028). A request with no
 * answer here is answered 500 and recorded, and the test fails naming it: a page that starts
 * calling something new must get its sample answer, never a silent error state in the picture.
 */
export async function answerApi(page: Page, { signedIn }: { signedIn: boolean }): Promise<string[]> {
  const unanswered: string[] = [];
  const table = routes(signedIn);

  await page.route('**/panel/**', async (route) => {
    const request = route.request();
    const url = new URL(request.url());
    for (const [method, path, handler] of table) {
      const match = method === request.method() ? path.exec(url.pathname) : null;
      if (match) {
        const answer = handler(match, url.searchParams);
        await route.fulfill({ status: answer.status ?? 200, json: answer.body });
        return;
      }
    }

    unanswered.push(`${request.method()} ${url.pathname}${url.search}`);
    await route.fulfill({ status: 500, json: { code: 'screens.unanswered' } });
  });

  return unanswered;
}
